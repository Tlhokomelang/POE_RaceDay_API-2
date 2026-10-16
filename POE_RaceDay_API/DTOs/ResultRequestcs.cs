namespace POE_RaceDay_API.DTOs
{
    // Request model
    public class ResultRequest
    {
        public string FinishTime { get; set; } = string.Empty;
        public int Position { get; set; }
        public int EnrolmentID { get; set; }
    }
}
