using BelayrChat.Models;
using Microsoft.AspNetCore.SignalR;
using Hub = Microsoft.AspNetCore.SignalR.Hub;

namespace BelayrChat.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IDictionary<string, UserRoomConnection> _connection;

        public ChatHub(IDictionary<string, UserRoomConnection> connection)
        {
            _connection = connection;
        }

        public async Task JoinRoom(UserRoomConnection connection)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, connection.Room);
            _connection[Context.ConnectionId] = connection;
            await Clients.Group(connection.Room)
                .SendAsync("ReceiveMessage", "Belayr Bot", $"{connection.User} dołączył do pokoju", DateTime.Now);
            await SendConnectedUser(connection.Room);
        }

        public async Task SendMessage(string message)
        {
            if (_connection.TryGetValue(Context.ConnectionId, out UserRoomConnection userRoomConnection))
            {
                await Clients.Group(userRoomConnection.Room)
                    .SendAsync("ReceiveMessage", userRoomConnection.User, message, DateTime.Now);
            }
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connection.TryGetValue(Context.ConnectionId, out UserRoomConnection? userRoomConnection) == false)
            {
                return base.OnDisconnectedAsync(exception);
            }

            _connection.Remove(Context.ConnectionId);
            Clients.Group(userRoomConnection.Room).SendAsync("ReceiveMessage", "Belayr Bot",
                $"{userRoomConnection.User} opuścił pokój", DateTime.Now);
            SendConnectedUser(userRoomConnection.Room);
            return base.OnDisconnectedAsync(exception);
        }

        public Task SendConnectedUser(string room)
        {
            var users = _connection.Values.Where(x => x.Room == room).Select(x => x.User);
            return Clients.Group(room).SendAsync("ConnectedUser", users);
        }
    }
}