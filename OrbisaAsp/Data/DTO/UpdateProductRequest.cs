using OrbisaAsp.Data.Models;

namespace OrbisaAsp.Data.DTO
{
    public class UpdateProductRequest
    {
        public string name { get; set; } = string.Empty;
        public SizeType size { get; set; }
        public string color { get; set; } = string.Empty;
        public GenderType gender { get; set; }
        public string description { get; set; } = string.Empty;
        public string tag { get; set; } = string.Empty;
        public double price {  get; set; }
        public bool isAbled { get; set; }
        public List<IFormFile>? images { get; set; }
    }
}
