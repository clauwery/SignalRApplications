using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.AspNet.SignalR.Client;

namespace WpfApplicationClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnMyEvent(StateChange obj)
        {
            switch (obj.NewState)
            {
                case ConnectionState.Connected:
                    textBlock.Text = "Connected";
                    break;
                case ConnectionState.Connecting:
                    textBlock.Text = "Connecting";
                    break;
                case ConnectionState.Disconnected:
                    textBlock.Text = "Disconnected";
                    break;
                case ConnectionState.Reconnecting:
                    textBlock.Text = "Reconnecting";
                    break;
            }
        }

        void Init(string ServerName)
        {
            var connection = new HubConnection("http://localhost:50387");
            connection.StateChanged += new Action<StateChange>(OnMyEvent);
            var proxy = connection.CreateHubProxy("chatHub");
            connection.Start().Wait();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            dataGrid.Items.Add("test");
            Init("ServerName1");


        }

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
