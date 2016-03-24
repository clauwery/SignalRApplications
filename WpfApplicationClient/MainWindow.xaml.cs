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
using System.Threading;

using ASAM.XIL.Interfaces.Testbench;
using ASAM.XIL.Interfaces.Testbench.Common.Error;
using ASAM.XIL.Interfaces.Testbench.Common.ValueContainer;
using ASAM.XIL.Implementation.TestbenchFactory.Testbench;
using ASAM.XIL.Interfaces.Testbench.MAPort.Enum;
using ASAM.XIL.Interfaces.Testbench.MAPort;

using dSPACE.PlatformManagement.Automation;
using ASAM.XIL.Interfaces.Testbench.Common.Capturing;
using ASAM.XIL.Interfaces.Testbench.Common.MetaInfo;
using ASAM.XIL.Interfaces.Testbench.Common.SignalGenerator;
using ASAM.XIL.Interfaces.Testbench.Common.ValueContainer.Enum;

namespace XilApiTools
{
    class Connection_Basics
    {
        static Type serverType = Type.GetTypeFromProgID("DSPlatformManagementAPI2");
        public IPmPlatformManagement PlatformManagement = Activator.CreateInstance(serverType) as IPmPlatformManagement;
        public string RegisterPlatform(string platformName)
        {
            string Message = "";
            try
            {
                PlatformType platformType = (PlatformType)Enum.Parse(typeof(PlatformType), platformName);
                var RegisterInfo = PlatformManagement.CreatePlatformRegistrationInfo(platformType);
                PlatformManagement.RegisterPlatform(RegisterInfo);
            }
            catch(Exception ex)
            {
                Message = ex.Message;
            }
            return Message;
        }
        public string Clear()
        {
            string Message = "";
            try
            {
                PlatformManagement.ClearSystem(true);
            }
            catch(System.Exception ex)
            {
                Message = ex.Message;
            }
            return Message;
        }
    }
    public class MAPort
    {
        private IMAPort maPort;
        public MAPort(string vendorName, string productName, string productVersion)
        {
            ITestbenchFactory TBFactory = new TestbenchFactory();
            ITestbench TB = TBFactory.CreateVendorSpecificTestbench(vendorName, productName, productVersion);
            try
            {
                maPort = TB.MAPortFactory.CreateMAPort("MAPort");
            }
            catch (Exception)
            {
                throw;
            }
        }
        public string Configure(string MAPortConfigFile)
        {
            string message = "";
            try
            {
                IMAPortConfig maPortConfig = maPort.LoadConfiguration(MAPortConfigFile);
                maPort.Configure(maPortConfig, false);
            }
/*
            catch (TestbenchPortException ex)
            {
                Console.WriteLine("Error description: {0}", ex.CodeDescription);
                Console.WriteLine("Detailed error description: {0}", ex.VendorCodeDescription);
            }
*/
            catch (System.Exception ex)
            {
                message = ex.Message;
                maPort.Dispose();
            }
            /*
                        finally
                        {
                            // Dispose any instances of MAPort, Capture, and EESPort
                        }
              */
            return message;
        }
        public double Read(string variableName)
        {
            try
            {
                if (maPort.State == MAPortState.eSIMULATION_RUNNING && maPort.VariableNames.Contains(variableName) && maPort.IsReadable(variableName))
                {
                    IFloatValue value = (IFloatValue)maPort.Read(variableName);
                    return value.Value;
                }
                return -1;
            }
            catch (Exception ex)
            {
                return -1;
            }
        }
        public bool IsRunning()
        {
            return maPort.State == MAPortState.eSIMULATION_RUNNING;            
        }
        public IList<string> VariableNames()
        {
            return maPort.VariableNames;
        }
        public void Disconnect()
        {
            maPort.Disconnect();
        }
        public double DAQClock()
        {
            return maPort.DAQClock;
        }
    }
}

namespace WpfApplicationClient
{
    public class Worker
    {
        private IConnection hubConnection; // connection to HUB
        private IHubProxy hubProxy; // proxy to talk to HUB
        private Server record; // Record to build table
        private XilApiTools.MAPort maPort; // MAPort to talk to hardware
        public Worker(HubConnection hubConnection, IHubProxy hubProxy, XilApiTools.MAPort maPort)
        {
            this.hubConnection = hubConnection;
            this.hubProxy = hubProxy;
            this.maPort = maPort;
            this.record = new Server();
        }
        public async Task DoWork()
        {
            hubProxy.Invoke("createRecord").Wait();
            while (hubConnection.State== Microsoft.AspNet.SignalR.Client.ConnectionState.Connected)
            {
                try
                {
                    // Check XIL connection
                    // Pass complete DOM structure to this method to access all elements?
                    // string variableName = variable_listBox.SelectedItem.ToString();
                    // Update record
                    // string variableName = "ds1401()://currentTime";
                    if (maPort.IsRunning())
                    {
                        record.ip = "CONNECTED";
                        // record.time = maPort.Read("ds1401()://currentTime").ToString();
                        record.time = maPort.DAQClock().ToString();
                    }
                    else
                    {
                        record.ip = "NOT CONNECTED";
                        record.time = "";
                    }

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
        public void RequestStop()
        {
            _shouldStop = true;
        }
        // Volatile is used as hint to the compiler that this data 
        // member will be accessed by multiple threads. 
        private volatile bool _shouldStop;
    }
    public class Server
    {
        public string identifier { get; set; }
        public string ip { get; set; }
        public string ping { get; set; }
        public string time { get; set; }
    }
    public partial class MainWindow : Window
    {
        static private string vendorName = "dSPACE GmbH";
        static private string productName = "XIL API";
        static private string productVersion = "2015-B";

        // OK to have this "GLOBAL" ?
        // Why must proxy be STATIC ?
        // Why / how does IISExpress automatically startup when debugging this project (not related to SignalRChat in which server runs)
        static HubConnection hubConnection = new HubConnection("http://localhost:50387");
        static IHubProxy hubProxy = hubConnection.CreateHubProxy("chatHub");
        static XilApiTools.Connection_Basics XilConnection = new XilApiTools.Connection_Basics();
        static XilApiTools.MAPort maPort = new XilApiTools.MAPort(vendorName, productName, productVersion);

        public MainWindow()
        {
            InitializeComponent();
            hubConnection.StateChanged += new Action<StateChange>(hubConnectionStateChangedEvent);
            // Populate platform types
            // WHERE SHOULD I DO THIS?
            update_platform_listBox();
        }
        private async void hubConnectionStateChangedEvent(StateChange obj)
        {
            switch (obj.NewState)
            {
                case Microsoft.AspNet.SignalR.Client.ConnectionState.Connected:
                    await textBlock.Dispatcher.BeginInvoke(new Action(() => textBlock.Text = "Connected"));
                    Worker hubConnectionWorker = new Worker(hubConnection, hubProxy, maPort);
                    // AWAIT VERSUS THREADING  ???
                    await hubConnectionWorker.DoWork();
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
        private async void connect_hub_button_Click(object sender, RoutedEventArgs e)
        {
            await hubConnection.Start();
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
        private void register_xil_button_Click(object sender, RoutedEventArgs e)
        {
            // string platformName = comboBox.SelectedItem.ToString();
            string platformName = "MABX";
            status_message_text.Text = XilConnection.RegisterPlatform(platformName);
            update_platform_listBox();
        }
        private void clear_xil_button_Click(object sender, RoutedEventArgs e)
        {
            status_message_text.Text = XilConnection.Clear();
            update_platform_listBox();
        }
        private void update_platform_listBox()
        {
            platform_listBox.Items.Clear();
            // Auto get type of platform !!! not always MABX
            foreach (IPmMABXPlatform item in (IPmSeekedPlatforms)XilConnection.PlatformManagement.Platforms)
            {
                platform_listBox.Items.Add(item.DisplayName);
            }
            platform_listBox.SelectedIndex = 0;
        }
        private void update_variable_listBox()
        {
            if (maPort.IsRunning())
            {
                variable_listBox.Items.Clear();
                foreach (string item in maPort.VariableNames())
                {
                    variable_listBox.Items.Add(item);
                }
                variable_listBox.SelectedIndex = 0;
            }
        }
        private void read_xil_variable_button_Click(object sender, RoutedEventArgs e)
        {
            // string variableName = "ds1401()://currentTime";
            string variableName = variable_listBox.SelectedItem.ToString();
            read_xil_variable_text.Text = maPort.Read(variableName).ToString();
        }
        private void connect_maport_button_Click(object sender, RoutedEventArgs e)
        {
            {
                string MAPortConfigFile = @"MAPortConfig.xml";
                status_message_text.Text = maPort.Configure(MAPortConfigFile);
                update_variable_listBox();
            }
        }
        private void disconnect_maport_button_Click(object sender, RoutedEventArgs e)
        {
            {
                maPort.Disconnect();
            }
        }
        private void exit_button_Click(object sender, RoutedEventArgs e)
        {
            maPort.Disconnect();
            hubConnection.Stop();
            hubConnection.Dispose();
            GetWindow(this).Close();
        }
    }
}
