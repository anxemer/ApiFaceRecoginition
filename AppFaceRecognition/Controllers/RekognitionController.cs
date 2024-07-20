using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.S3;
using Amazon.S3.Model;
using AppFaceRecognition;
using Azure.Core;
using DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

[ApiController]
[Route("[controller]")]
public class RecognitionController : ControllerBase
{
    private readonly IAmazonRekognition _rekognitionClient;
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly StudentDbContext _dbContext;
    private readonly ILogger<RecognitionController> _logger;

    public RecognitionController(IAmazonRekognition rekognitionClient, IAmazonS3 s3Client, IConfiguration configuration, StudentDbContext dbContext, ILogger<RecognitionController> logger)
    {
        _rekognitionClient = rekognitionClient;
        _s3Client = s3Client;
        _bucketName = configuration["AwsOptions:BucketName"];
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost("recognize-face")]
    public async Task<IActionResult> RecognizeFace(IFormFile image)
    {
        if (image == null)
        {
            return BadRequest("Image file is required.");
        }

        try
        {
            // Upload ảnh lên S3
            string uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            using (var stream = image.OpenReadStream())
            {
                var putRequest = new Amazon.S3.Model.PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = uniqueFileName,
                    InputStream = stream
                };

                var putResponse = await _s3Client.PutObjectAsync(putRequest);
                _logger.LogInformation($"Image uploaded successfully to S3. Bucket: {_bucketName}, Key: {uniqueFileName}, Response: {putResponse.HttpStatusCode}");
            }

            // So sánh khuôn mặt
            var matchingStudent = await CompareFacesWithS3Images(uniqueFileName);

            if (matchingStudent != null)
            {
                var studentInfo = new
                {
                    matchingStudent.HoTen,
                    matchingStudent.Tuoi,
                    matchingStudent.MaSoSinhVien,
                    matchingStudent.NganhHoc,
                    matchingStudent.KiHoc
                };

                // Xóa ảnh vừa upload sau khi so sánh
                await _s3Client.DeleteObjectAsync(_bucketName, uniqueFileName);

                return Ok(studentInfo);
            }

            // Xóa ảnh vừa upload nếu không tìm thấy kết quả
            await _s3Client.DeleteObjectAsync(_bucketName, uniqueFileName);

            return NotFound("No matching student found.");
        }
        catch (AmazonS3Exception s3Ex)
        {
            _logger.LogError($"S3 error: {s3Ex.Message}, Status Code: {s3Ex.StatusCode}, Error Code: {s3Ex.ErrorCode}, Request ID: {s3Ex.RequestId}");
            return StatusCode(500, $"S3 error: {s3Ex.Message}");
        }
        catch (AmazonRekognitionException rekognitionEx)
        {
            _logger.LogError($"Rekognition error: {rekognitionEx.Message}, Error Code: {rekognitionEx.ErrorCode}, Request ID: {rekognitionEx.RequestId}");
            return StatusCode(500, $"Rekognition error: {rekognitionEx.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing image: {ex.Message}");
            return StatusCode(500, $"Error processing image: {ex.Message}");
        }
    }

    private async Task<Student> CompareFacesWithS3Images(string sourceImageKey)
    {
        // Lấy danh sách tất cả sinh viên từ cơ sở dữ liệu
        var students = await _dbContext.Students.ToListAsync();
        _logger.LogInformation($"Comparing source image {sourceImageKey} with {students.Count} student images");

        foreach (var student in students)
        {
            try
            {
                var compareRequest = new CompareFacesRequest
                {
                    SourceImage = new Amazon.Rekognition.Model.Image
                    {
                        S3Object = new Amazon.Rekognition.Model.S3Object
                        {
                            Bucket = _bucketName,
                            Name = sourceImageKey
                        }
                    },
                    TargetImage = new Amazon.Rekognition.Model.Image
                    {
                        S3Object = new Amazon.Rekognition.Model.S3Object
                        {
                            Bucket = _bucketName,
                            Name = student.S3ImageKey
                        }
                    },
                    SimilarityThreshold = 70F  // Giảm ngưỡng xuống 70%
                };

                _logger.LogInformation($"Comparing with student {student.ID}, image key: {student.S3ImageKey}");

                var compareResponse = await _rekognitionClient.CompareFacesAsync(compareRequest);

                if (compareResponse.FaceMatches.Count > 0)
                {
                    _logger.LogInformation($"Match found! Student ID: {student.ID}, Similarity: {compareResponse.FaceMatches[0].Similarity}%");
                    return student;
                }
                else
                {
                    _logger.LogInformation($"No match found for student {student.ID}");
                }
            }
            catch (AmazonRekognitionException e)
            {
                _logger.LogError($"Rekognition error for student {student.ID}: {e.Message}");
            }
            catch (Exception e)
            {
                _logger.LogError($"Unexpected error for student {student.ID}: {e.Message}");
            }
        }

        _logger.LogWarning("No matching student found after comparing with all images.");
        return null;
    }
    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage([FromForm] CreateStudentRequest createStudentRequest)
    {
        if (createStudentRequest.file.Length > 0)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(createStudentRequest.file.FileName);
            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName,
                InputStream = createStudentRequest.file.OpenReadStream()
            };

            await _s3Client.PutObjectAsync(putRequest);

            var student = new Student
            {
                HoTen = createStudentRequest.HoTen,
                Tuoi = createStudentRequest.Tuoi,
                MaSoSinhVien = createStudentRequest.MaSoSinhVien,
                NganhHoc = createStudentRequest.NganhHoc,
                KiHoc = createStudentRequest.KiHoc,
                S3ImageKey = fileName
            };
            if (createStudentRequest.file != null)
            {
                var filePath = Path.Combine("path/to/save", createStudentRequest.file.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await createStudentRequest.file.CopyToAsync(stream);
                }
            }
            _dbContext.Students.Add(student);
            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "File uploaded successfully" });
        }

        return BadRequest("No file uploaded");
    }
}
