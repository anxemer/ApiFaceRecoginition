namespace AppFaceRecognition
{
    public class CreateStudentRequest
    {
        public string HoTen { get; set; }
        public int Tuoi { get; set; }
        public string MaSoSinhVien { get; set; }
        public string NganhHoc { get; set; }
        public int KiHoc { get; set; }
        public IFormFile file { get; set; }
    }
}
