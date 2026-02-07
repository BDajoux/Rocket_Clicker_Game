using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GameServerApi.Hubs
{
    public class ChatHub : Hub
    {
        private static int _userCount = 0;
        private static readonly ConcurrentDictionary<string, int> _connectedUsers = new();
        
        public async Task Login(int userId)
        {
            _connectedUsers[Context.ConnectionId] = userId;
            await Clients.Caller.SendAsync("Login", userId);
        }

        public override async Task OnConnectedAsync()
        {
            _userCount++;
            await Clients.All.SendAsync("UpdateUserCount", _userCount);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            _userCount--;
            await Clients.All.SendAsync("UpdateUserCount", _userCount);

            _connectedUsers.TryRemove(Context.ConnectionId, out var userId);

            await base.OnDisconnectedAsync(exception);
        }

        public async Task PlayerReset(string userName, int score)
        {
            await Clients.All.SendAsync("PlayerReset", userName, score);
        }
        
        public async Task NewHIghScore(string userName, int score)
        {
            await Clients.All.SendAsync("NewHIghScore", userName, score);
        }
        
        public static IEnumerable<int> GetConnectedUserIds()
        {
            return _connectedUsers.Values.Distinct();
        }
        public static string? GetConnectionId(int userId)
        {
            return _connectedUsers.FirstOrDefault(x => x.Value == userId).Key;
        }
    }
}