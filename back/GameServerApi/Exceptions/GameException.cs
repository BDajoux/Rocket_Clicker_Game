namespace GameServerApi.Exceptions
{
    public class GameException : Exception
    {
        public required string Code { get; set; } = string.Empty;
        public required int StatusCode { get; set; } = 0;
    }
}
