using OrbisaAsp.Data.Models;

namespace OrbisaAsp.Data.DTO
{
    public class CreateProductRequest
    {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public SizeType size { get; set; }
        public string color { get; set; } = string.Empty;
        public GenderType gender { get; set; }
        public string description { get; set; } = string.Empty;
        public string tag { get; set; } = string.Empty;
        public double price { get; set; }
        public bool isAbled { get; set; } = true;
        public List<IFormFile>? images { get; set; }
    }
}
