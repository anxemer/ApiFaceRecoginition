using Microsoft.AspNetCore.Http;

namespace AppFaceRecognition
{
    public class ImageRequest
    {
        public IFormFile sourceImage { get; set; }
        public IFormFile targetImage { get; set; }
    }
}
