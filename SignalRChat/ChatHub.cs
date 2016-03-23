using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace SignalRChat
{
    public class ChatHub : Hub
    {
        public void Send(string name, string message, string value)
        {
            // Call the broadcastMessage method to update clients.
            Clients.All.broadcastMessage(name, message, value);
        }
        public void UpdateTime(string time)
        {
            Clients.All.broadcastMessage(Context.ConnectionId.ToString(), time, "");
            // Clients.Group("webClients").broadcastMessage(Context.ConnectionId.ToString(), time, "");
        }
        public override Task OnConnected()
        {
            //Clients.All.onConnected(Context.ConnectionId);
            // Clients.All.createRecord(Context.ConnectionId);
            return base.OnConnected();
        }
        public override Task OnDisconnected(bool stopCalled)
        {
            // Will also disconnect WEB clients not only XIL clients that created a record
            Clients.All.deleteRecord(Context.ConnectionId);
            return base.OnDisconnected(stopCalled);
        }
        public Task JoinWebClients()
        {
            return Groups.Add(Context.ConnectionId, "webClients");
        }
        public Task JoinXilServers()
        {
            return Groups.Add(Context.ConnectionId, "xilServers");
        }
        public void CreateRecord()
        {
            Clients.All.createRecord(Context.ConnectionId);
            // Clients.Group("xilServers").createRecord(Context.ConnectionId);
            // Clients.All.createRecord(Context.ConnectionId);
            // Clients.All.createRecord(record);
        }
        public void DeleteRecord()
        {
            Clients.All.deleteRecord(Context.ConnectionId);
            // Clients.Group("xilServers").createRecord(Context.ConnectionId);
            // Clients.All.createRecord(Context.ConnectionId);
            // Clients.All.createRecord(record);
        }
    }
}
