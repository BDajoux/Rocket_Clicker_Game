namespace GameServerApi.Models
{
    public class Game
    {
        public int Count { get; set; }
        public int Multiplier { get; set; }

        public Game(int count, int multiplier)
        {
            Count = count;
            Multiplier = multiplier;
        }

        public int getCount() { return Count; }
        public int getMultiplier() { return Multiplier; }
    }
}
