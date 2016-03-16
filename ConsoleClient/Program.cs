using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.AspNet.SignalR.Client;

namespace ConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("Start");
            string ServerName;
            if (args.Length==0){ ServerName = "MyServer"; }
            else { ServerName = args[0]; }
            Init(ServerName);
            Console.WriteLine("DoneInit");
            Console.WriteLine("End");
            Console.ReadKey();
        }

        static async void Init(string ServerName)
        {
            var connection = new HubConnection("http://localhost:50387");
            Console.WriteLine("Connection state :{0} ", connection.State);
            var chat = connection.CreateHubProxy("chatHub");
            connection.Start().Wait(); // Make connection "connected"
            Console.WriteLine("Connection state :{0} ", connection.State);
            while (true)
            {
                string Message = "Time";
                string Value = DateTime.Now.ToString();
                await chat.Invoke("send", new string[] { ServerName, Message, Value });
                Thread.Sleep(100);
            }
        }
    }
}
