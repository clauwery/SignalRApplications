﻿using System;
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
            // Clients.Group("xilServers").onConnected(Context.ConnectionId);
            return base.OnConnected();
        }
        public Task JoinWebClients()
        {
            return Groups.Add(Context.ConnectionId, "webClients");
        }
        public void CreateRecord(object record)
        {
            Clients.All.createRecord(record);
        }
    }
}
