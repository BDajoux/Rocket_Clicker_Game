namespace GameServerApi.Models
{

    public class Progression
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int Count { get; set; }
        public int Multiplier { get; set; }
        public int BestScore { get; set; }
        public int TotalClickValue { get; set; }
    }
}
