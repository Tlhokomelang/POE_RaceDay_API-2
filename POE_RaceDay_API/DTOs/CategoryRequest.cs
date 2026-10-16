namespace POE_RaceDay_API.DTOs
{
    // Request model
    public class CategoryRequest
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Distance { get; set; }
        public decimal EntryFee { get; set; } = 0;
        public int EventID { get; set; }
    }
}
