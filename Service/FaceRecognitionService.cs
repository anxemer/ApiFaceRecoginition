using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;
using DataAccess;
using Image = Amazon.Rekognition.Model.Image;

public class FaceRecognitionService
{
    private readonly IAmazonRekognition _rekognitionClient;
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _collectionId;
    private readonly StudentDbContext _dbContext;

    public FaceRecognitionService(IAmazonRekognition rekognitionClient, IAmazonS3 s3Client,
        IConfiguration configuration, StudentDbContext dbContext)
    {
        _rekognitionClient = rekognitionClient;
        _s3Client = s3Client;
        _bucketName = configuration["S3:BucketName"];
        _collectionId = configuration["AWS:CollectionId"];
        _dbContext = dbContext;
    }

    public async Task<Student> SearchFace(Stream imageStream)
    {
        try
        {
            var searchRequest = new SearchFacesByImageRequest
            {
                CollectionId = _collectionId,
                Image = new Image
                {
                    Bytes = new MemoryStream()
                },
                MaxFaces = 1,
                FaceMatchThreshold = 70F
            };

            imageStream.CopyTo(searchRequest.Image.Bytes);
            searchRequest.Image.Bytes.Position = 0;

            var searchResponse = await _rekognitionClient.SearchFacesByImageAsync(searchRequest);

            if (searchResponse.FaceMatches.Count > 0)
            {
                var faceMatch = searchResponse.FaceMatches[0];
                var s3ImageKey = faceMatch.Face.ExternalImageId;

                var userInfo = await _dbContext.Students
                    .FirstOrDefaultAsync(u => u.S3ImageKey == s3ImageKey);

                return userInfo;
            }
            else
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }

    // Thêm phương thức này để tải ảnh lên S3 và thêm vào collection
    public async Task<bool> AddFaceToCollection(Stream imageStream, string s3ImageKey, Student userInfo)
    {
        try
        {
            // Upload ảnh lên S3
            await _s3Client.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest
            {
                BucketName = _bucketName,
                Key = s3ImageKey,
                InputStream = imageStream
            });

            // Thêm ảnh vào collection Rekognition
            var indexRequest = new IndexFacesRequest
            {
                CollectionId = _collectionId,
                Image = new Image
                {
                    S3Object = new Amazon.Rekognition.Model.S3Object
                    {
                        Bucket = _bucketName,
                        Name = s3ImageKey
                    }
                },
                ExternalImageId = s3ImageKey,
                DetectionAttributes = new List<string> { "ALL" }
            };

            var indexResponse = await _rekognitionClient.IndexFacesAsync(indexRequest);

            if (indexResponse.FaceRecords.Count > 0)
            {
                // Lưu thông tin người dùng vào database
                userInfo.S3ImageKey = s3ImageKey;
                _dbContext.Students.Add(userInfo);
                await _dbContext.SaveChangesAsync();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }
}