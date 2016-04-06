extern alias AutomationDevicesInterfaces10;
extern alias dSPACEInterfaceDefinitionsPlatformManagementAutomation10;

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

using AutomationDevicesInterfaces10.dSPACE.PlatformManagement.Automation;
using dSPACEInterfaceDefinitionsPlatformManagementAutomation10.dSPACE.PlatformManagement.Automation;

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
        static private string productVersion = "2015-A";

        // Create platform management
        static private Type serverType = Type.GetTypeFromProgID("DSPlatformManagementAPI2");
        static private IPmPlatformManagement platformManagement = Activator.CreateInstance(serverType) as IPmPlatformManagement;

        // Create MAPort
        static private ITestbenchFactory TBFactory = new TestbenchFactory();
        static private ITestbench TB = TBFactory.CreateVendorSpecificTestbench(vendorName, productName, productVersion);
        static private IMAPort maPort = TB.MAPortFactory.CreateMAPort("MAPort");

        // Create hubConnection and hubProxy
        static HubConnection hubConnection = new HubConnection("http://localhost:50387");
        static IHubProxy hubProxy = hubConnection.CreateHubProxy("chatHub");

        // PlatformManagement functions
        public void RegisterPlatform(IPmPlatformManagement platformManagement, string platformName)
        {
            PlatformType platformType = (PlatformType)Enum.Parse(typeof(PlatformType), platformName);
            if (platformName == "DS1103")
            {
                IPmDS1103RegisterInfo registrationInfo = (IPmDS1103RegisterInfo)platformManagement.CreatePlatformRegistrationInfo(platformType);
                platformManagement.RegisterPlatform(registrationInfo);
            }
            if (platformName=="DS1006")
            {
                // platformManagement.ClearSystem(false);
                IPmDS1006RegisterInfo registrationInfo = (IPmDS1006RegisterInfo)platformManagement.CreatePlatformRegistrationInfo(platformType);
                registrationInfo.ConnectionType = InterfaceConnectionType.Bus;
                registrationInfo.PortAddress = 768;
                platformManagement.RegisterPlatform(registrationInfo);
            }
            if (platformName=="MABX")
            {
                IPmMABXRegisterInfo registrationInfo = (IPmMABXRegisterInfo)platformManagement.CreatePlatformRegistrationInfo(platformType);
                platformManagement.RegisterPlatform(registrationInfo);
            }
        }
        public void RegisterPlatformEvents(IPmPlatformManagement platformManagement)
        {
            IPmPlatformManagementEvents platformManagementEvents = (IPmPlatformManagementEvents)platformManagement;
            platformManagementEvents.PlatformAdded += PlatformManagementEvents_PlatformAdded;
            platformManagementEvents.PlatformRemoving += PlatformManagementEvents_PlatformRemoving;
            platformManagementEvents.PlatformConnected += PlatformManagementEvents_PlatformConnected;
            platformManagementEvents.PlatformDisconnected += PlatformManagementEvents_PlatformDisconnected;
            platformManagementEvents.RealTimeApplicationStarted += PlatformManagementEvents_RealTimeApplicationStarted;
            platformManagementEvents.RealTimeApplicationStopped += PlatformManagementEvents_RealTimeApplicationStopped;
        }

        // Mainwindow
        public MainWindow()
        {

            // Events
            RegisterPlatformEvents(platformManagement);
            hubConnection.StateChanged += new Action<StateChange>(hubConnectionStateChangedEvent);
            // calibrationManagementEvents = (IXaCalibrationManagementEvents)this.ControlDeskApplication.CalibrationManagement

            // Initialise GUI
            InitializeComponent();

            // Update GUI
            update_platform_comboBox();
            update_platform_listBox();
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
   
        // PlatformManagementEvents
        private void PlatformManagementEvents_RealTimeApplicationStopped(object Platform)
        {
            update_read_xil_variable_button(false);
            update_variable_listBox();
        }
        private void PlatformManagementEvents_RealTimeApplicationStarted(object Platform)
        {
            update_read_xil_variable_button(true);
            update_variable_listBox();
        }
        private void PlatformManagementEvents_PlatformConnected(object Platform)
        {
            update_platform_listBox();
        }
        private void PlatformManagementEvents_PlatformDisconnected(object Platform)
        {
            update_platform_listBox();
        }
        private void PlatformManagementEvents_PlatformRemoving(object Platform)
        {
            update_platform_listBox();
        }
        private void PlatformManagementEvents_PlatformAdded(object Platform)
        {
            update_platform_listBox();
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
        private void update_platform_listBox()
        {
            platform_listBox.Dispatcher.Invoke(new Action(() => platform_listBox.Items.Clear()));
            foreach (string item in (IPmPlatformNames)platformManagement.Platforms.UniqueNames)
            {
                platform_listBox.Dispatcher.Invoke(new Action(() => platform_listBox.Items.Add(item)));
            }
            platform_listBox.Dispatcher.Invoke(new Action(() => platform_listBox.SelectedIndex = 0));
        }
        private void update_platform_comboBox()
        {
            foreach (string item in Enum.GetNames(typeof(PlatformType)))
            {
                platform_comboBox.Items.Add(item);
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
                    // string variableName = "ds1401()://currentTime";
                    if (maPort.State == MAPortState.eSIMULATION_RUNNING)
                    {
                        record.ip = "CONNECTED";
                        // record.time = maPort.Read("ds1401()://currentTime").ToString();
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
        private void register_xil_button_Click(object sender, RoutedEventArgs e)
        {
            if (platform_comboBox.SelectedItem!=null)
            {
                string platformName = platform_comboBox.SelectedItem.ToString();
                RegisterPlatform(platformManagement, platformName);
                update_platform_listBox();
            }
            
        }
        private void clear_xil_button_Click(object sender, RoutedEventArgs e)
        {
            platformManagement.ClearSystem(true);
        }
        private void read_xil_variable_button_Click(object sender, RoutedEventArgs e)
        {
            if (maPort.State==MAPortState.eSIMULATION_RUNNING & variable_listBox.SelectedItem!=null)
            {
                // string variableName = "ds1401()://currentTime";
                string variableName = variable_listBox.SelectedItem.ToString();
                IFloatValue value = (IFloatValue)maPort.Read(variableName);
                read_xil_variable_text.Text = value.Value.ToString();
            }
        }
        private void connect_maport_button_Click(object sender, RoutedEventArgs e)
        {
            string MAPortConfigFile = @"MAPortConfig.xml";
            IMAPortConfig maPortConfig = maPort.LoadConfiguration(MAPortConfigFile);
            maPort.Configure(maPortConfig, false);
            update_variable_listBox();
        }
        private void disconnect_maport_button_Click(object sender, RoutedEventArgs e)
        {
            {
                maPort.Disconnect();
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
