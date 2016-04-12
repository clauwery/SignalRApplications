using System;
using System.Reflection;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.AspNet.SignalR.Client;
using System.Threading;

using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;

using ASAM.XIL.Interfaces.Testbench;
using ASAM.XIL.Interfaces.Testbench.Common.Error;
using ASAM.XIL.Interfaces.Testbench.Common.ValueContainer;
using ASAM.XIL.Implementation.TestbenchFactory.Testbench;
using ASAM.XIL.Interfaces.Testbench.MAPort.Enum;
using ASAM.XIL.Interfaces.Testbench.MAPort;

namespace WpfApplicationClient
{
    public class Server
    {
        public string identifier { get; set; }
        public string ip { get; set; }
        public string ping { get; set; }
        public string time { get; set; }
    }
    public partial class MainWindow : Window
    {
        // OK to have this "GLOBAL" ?
        // Why must proxy be STATIC ?
        // Why / how does IISExpress automatically startup when debugging this project (not related to SignalRChat in which server runs)

        // Dynamic data point
        ObservableDataSource<Point> source1 = null;

        // Related to referenced XIL API ASAM assemblies
        static private string vendorName = "dSPACE GmbH";
        static private string productName = "XIL API";
        static private string productVersion = "2015-B";

        // Create MAPort
        static private ITestbenchFactory TBFactory = new TestbenchFactory();
        static private ITestbench TB = TBFactory.CreateVendorSpecificTestbench(vendorName, productName, productVersion);
        static private IMAPort maPort = TB.MAPortFactory.CreateMAPort("MAPort");

        // Create hubConnection and hubProxy
        static HubConnection hubConnection = new HubConnection("http://localhost:50387");
        static IHubProxy hubProxy = hubConnection.CreateHubProxy("chatHub");

        // Mainwindow
        public MainWindow()
        {

            hubConnection.StateChanged += new Action<StateChange>(hubConnectionStateChangedEvent);

            // Initialise GUI
            InitializeComponent();

            // Update GUI
            update_variable_listBox();

            // D3 DEMO
            source1 = new ObservableDataSource<Point>();
            source1.SetXYMapping(p => p);
            plotter.AddLineGraph(source1);

            try
            {
                hubConnection.Start();
            }
            catch (Exception ex)
            {
                status_message_text.Text = ex.Message;
            }
        }
   
        // HubConnectionEvents
        private async void hubConnectionStateChangedEvent(StateChange obj)
        {
            switch (obj.NewState)
            {
                case Microsoft.AspNet.SignalR.Client.ConnectionState.Connected:
                    await textBlock.Dispatcher.BeginInvoke(new Action(() => textBlock.Text = "Connected"));
                    // Worker hubConnectionWorker = new Worker(hubConnection, hubProxy, maPort, source1);
                    // AWAIT VERSUS THREADING  ???
                    //await hubConnectionWorker.DoWork();
                    await DoWork();
                    // workerThread.Start();
                    break;
                case Microsoft.AspNet.SignalR.Client.ConnectionState.Connecting:
                    await textBlock.Dispatcher.BeginInvoke(new Action(() => textBlock.Text = "Connecting"));
                    break;
                case Microsoft.AspNet.SignalR.Client.ConnectionState.Disconnected:
                    await textBlock.Dispatcher.BeginInvoke(new Action(() => textBlock.Text = "Disconnected"));
                    break;
                case Microsoft.AspNet.SignalR.Client.ConnectionState.Reconnecting:
                    await textBlock.Dispatcher.BeginInvoke(new Action(() => textBlock.Text = "Reconnecting"));
                    break;
            }
        }

        // GUI updates
        private void update_read_xil_variable_button(Boolean enabled)
        {
            read_xil_variable_button.Dispatcher.Invoke(new Action(() => read_xil_variable_button.IsEnabled = enabled));
        }
        private void update_variable_listBox()
        {
            variable_listBox.Dispatcher.Invoke(new Action(() => variable_listBox.Items.Clear()));
            if (maPort.State==MAPortState.eSIMULATION_RUNNING)
            {
                foreach (string item in maPort.VariableNames.AsEnumerable())
                {
                    variable_listBox.Dispatcher.Invoke(new Action(() => variable_listBox.Items.Add(item)));
                }
            }
        }

        // Shutdown
        private void Shutdown()
        {
            // Gracefully shutdown
            maPort.Disconnect();
            hubConnection.Stop();
            hubConnection.Dispose();
        }

        // Worker threat
        public async Task DoWork()
        {
            Point point1 = new Point(0, 1);
            double index = 0.0;
            Server record = new Server();
            hubProxy.Invoke("createRecord").Wait();
            while (hubConnection.State == Microsoft.AspNet.SignalR.Client.ConnectionState.Connected)
            {
                try
                {
                    // Check XIL connection
                    // Pass complete DOM structure to this method to access all elements?
                    // string variableName = variable_listBox.SelectedItem.ToString();
                    // Update record
                    if (maPort.State == MAPortState.eSIMULATION_RUNNING)
                    {
                        record.ip = "CONNECTED";
                        record.time = maPort.DAQClock.ToString();
                    }
                    else
                    {
                        record.ip = "NOT CONNECTED";
                        record.time = "";
                    }

                    point1.X = index++;
                    point1.Y = -point1.Y;
                    source1.AppendAsync(Dispatcher, point1);

                    record.ping = "0";
                    await hubProxy.Invoke("UpdateRecord", record);
                    Thread.Sleep(500);

                    record.ping = "1";
                    await hubProxy.Invoke("UpdateRecord", record);
                    Thread.Sleep(500);
                }
                catch
                {
                    // Client disconnected but still in while loop
                }
            }
        }

        // GUI callbacks
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Shutdown();
        }
        private async void connect_hub_button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await hubConnection.Start();
            }
            catch (Exception ex)
            {

                status_message_text.Text = ex.Message;
            }
        }
        private void disconnect_hub_button_Click(object sender, RoutedEventArgs e)
        {
            hubConnection.Stop();
        }
        private void Send_Click(object sender, RoutedEventArgs e)
        {
            string[] data = new string[] { "ServerName", "Message", "Value" };
            try
            {
                hubProxy.Invoke("send", data);
            }
            catch (Exception)
            {
                // throw;
            }  
        }
        private void read_xil_variable_button_Click(object sender, RoutedEventArgs e)
        {
            if (maPort.State==MAPortState.eSIMULATION_RUNNING & variable_listBox.SelectedItem!=null)
            {
                string variableName = variable_listBox.SelectedItem.ToString();
                IFloatValue value = (IFloatValue)maPort.Read(variableName);
                read_xil_variable_text.Text = value.Value.ToString();
            }
        }
        private void connect_maport_button_Click(object sender, RoutedEventArgs e)
        {
            string MAPortConfigFile;
            bool forceConfig = false;
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".xml";
            dlg.Filter = "XML Files (*.xml)|*.xml";
            // Display OpenFileDialog by calling ShowDialog method 
            bool? result = dlg.ShowDialog();
            // Get the selected file name and display in a TextBox
            if (result == false) { return; }

            string sMessageBoxText = "Do you want to reload the variable description file?";
            string sCaption = "MAPort configuration";

            // Reload application?
            MessageBoxButton btnMessageBox = MessageBoxButton.YesNoCancel;
            MessageBoxImage icnMessageBox = MessageBoxImage.Warning;
            MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, icnMessageBox);
            switch (rsltMessageBox)
            {
                case MessageBoxResult.Yes:
                    forceConfig = true;
                    break;
                case MessageBoxResult.No:
                    forceConfig = false;
                    break;
                case MessageBoxResult.Cancel:
                    return;
            }
            MAPortConfigFile = dlg.FileName;
            try
            {
                IMAPortConfig maPortConfig = maPort.LoadConfiguration(MAPortConfigFile);
                maPort.Configure(maPortConfig, forceConfig);
                maPort.StartSimulation();
            }
            catch (Exception ex)
            {
                status_message_text.Text = ex.Message;
            }
            update_variable_listBox();
        }
        private void disconnect_maport_button_Click(object sender, RoutedEventArgs e)
        {
            {
                try
                {
                    maPort.Disconnect();
                }
                catch (Exception ex)
                {
                    status_message_text.Text = ex.Message;
                }
                update_variable_listBox();
            }
        }
        private void exit_button_Click(object sender, RoutedEventArgs e)
        {
            Shutdown();
            GetWindow(this).Close();
        }
    }
}
