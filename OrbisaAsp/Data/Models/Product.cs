namespace OrbisaAsp.Data.Models
{
    public class Product
    {
        public string id {  get; set; }
        public List<string> images { get; set; } = new();
        public string name { get; set; }
        public SizeType size { get; set; }
        public string color { get; set; }
        public GenderType gender { get; set; }
        public string description { get; set; }
        public string tag { get; set; }
        public double price { get; set; }
        public bool isAbled { get; set; }
    }

    public enum SizeType
    {
        XS = 1,
        S = 2,
        M = 3,
        L = 4,
        XL = 5,
        XXL = 6
    }

    public enum GenderType
    {
        MALE = 1,
        FEMALE = 2,
        UNISEX = 3
    }
}
