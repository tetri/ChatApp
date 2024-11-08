using ChatApp.Data;
using ChatApp.Models;

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ChatContext _context;

        public ChatHub(ChatContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            // Enviar mensagens persistidas ao cliente quando ele se conectar
            var messages = await _context.Messages
                .Include(m => m.User)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            foreach (var message in messages)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", message.User.Username, message.Text);
            }

            await base.OnConnectedAsync();
        }

        public async Task SendMessage(string username, string messageText)
        {
            // Verificar se o usuário já existe
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                user = new User { Username = username };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // Criar e salvar a mensagem
            var message = new Message
            {
                Text = messageText,
                SentAt = DateTime.UtcNow,
                UserId = user.Id,
                User = user
            };
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Enviar a mensagem para todos os clientes
            await Clients.All.SendAsync("ReceiveMessage", user.Username, message.Text);
        }
    }
}
