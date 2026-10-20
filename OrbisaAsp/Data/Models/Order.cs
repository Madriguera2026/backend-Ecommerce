namespace OrbisaAsp.Data.Models
{
    public class Order
    {

        public string id { get; set; } = string.Empty;
        public StateType state { get; set; } = StateType.CART;
        public DateTime date { get; set; } = DateTime.UtcNow;
        public User? user { get; set; }
        public List<Product>? products { get; set; } = new();

        public enum StateType
        {
            CART = 1,
            PENDING = 2,
            ON_PROCESS = 3,
            COMPLETED = 4,
            CANCELLED = 5
        }
    }
}
