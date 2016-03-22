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
    class Port_Basics
    {
        private string MAPortConfigFile = @"MAPortConfig.xml";
        private string vendorName = "dSPACE GmbH";
        private string productName = "XIL API";
        private string productVersion = "2015-B";
        private IEnumerator<string> variableNames;
        public IMAPort MAPort;
        public string Initialise()
        {
            string message = "";
            try
            {
                ITestbenchFactory TBFactory = new TestbenchFactory();
                ITestbench TB = TBFactory.CreateVendorSpecificTestbench(vendorName, productName, productVersion);
                MAPort = TB.MAPortFactory.CreateMAPort("MAPort");
                IMAPortConfig maPortConfig = MAPort.LoadConfiguration(MAPortConfigFile);
                MAPort.Configure(maPortConfig, false);
                variableNames = MAPort.VariableNames.GetEnumerator();
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
                MAPort.Dispose();
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
                if (MAPort != null && MAPort.State == MAPortState.eSIMULATION_RUNNING && MAPort.VariableNames.Contains(variableName) && MAPort.IsReadable(variableName))
                {
                    IFloatValue value = (IFloatValue)MAPort.Read(variableName);
                    return value.Value;
                }
                return -1;
            }
            catch (Exception ex)
            {
                return -1;
            }
        }
    }
}

namespace WpfApplicationClient
{
    public class Server
    {
        public string identifier { get; set; }
        public string ip { get; set; }
        public string ping { get; set; }
    }
    public partial class MainWindow : Window
    {
        // OK to have this "GLOBAL" ?
        // Why must proxy be STATIC ?
        // Why / how does IISExpress automatically startup when debugging this project (not related to SignalRChat in which server runs)
        static HubConnection connection = new HubConnection("http://localhost:50387");
        static IHubProxy proxy = connection.CreateHubProxy("chatHub");

        static XilApiTools.Connection_Basics XilConnection = new XilApiTools.Connection_Basics();
        static XilApiTools.Port_Basics PortConnection = new XilApiTools.Port_Basics();

        public MainWindow()
        {
            InitializeComponent();
            connection.StateChanged += new Action<StateChange>(OnMyEvent);
            // Populate platform types
            // WHERE SHOULD I DO THIS?
            update_platform_listBox();
        }
        private void OnMyEvent(StateChange obj)
        {
            switch (obj.NewState)
            {
                case Microsoft.AspNet.SignalR.Client.ConnectionState.Connected:
                    textBlock.Dispatcher.BeginInvoke(new Action(() => textBlock.Text = "Connected"));
                    break;
                case Microsoft.AspNet.SignalR.Client.ConnectionState.Connecting:
                    textBlock.Dispatcher.BeginInvoke(new Action(() => textBlock.Text = "Connecting"));
                    break;
                case Microsoft.AspNet.SignalR.Client.ConnectionState.Disconnected:
                    textBlock.Dispatcher.BeginInvoke(new Action(() => textBlock.Text = "Disconnected"));
                    break;
                case Microsoft.AspNet.SignalR.Client.ConnectionState.Reconnecting:
                    textBlock.Dispatcher.BeginInvoke(new Action(() => textBlock.Text = "Reconnecting"));
                    break;
            }
        }
        async void Connect()
        {
            await connection.Start();
            await proxy.Invoke("UpdateTime", new string[] {"TIME"});
            Server record = new Server();
            record.identifier = "ezohfzeh";
            await proxy.Invoke("CreateRecord", record);
            // await proxy.Invoke("CreateRecord", new string[] { "NAME" });
        }
        void Disconnect()
        {
            connection.Stop();
        }
        private void connect_hub_button_Click(object sender, RoutedEventArgs e)
        {
            // dataGrid.Items.Add("test");
            Connect();
        }
        private void disconnect_hub_button_Click(object sender, RoutedEventArgs e)
        {
            Disconnect();
        }
        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void Send_Click(object sender, RoutedEventArgs e)
        {
            string[] data = new string[] { "ServerName", "Message", "Value" };
            try
            {
                proxy.Invoke("send", data);
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
            variable_listBox.Items.Clear();
            foreach (string item in PortConnection.MAPort.VariableNames)
                {
                    variable_listBox.Items.Add(item);
                }
            variable_listBox.SelectedIndex = 0;
        }
        private void read_xil_variable_button_Click(object sender, RoutedEventArgs e)
        {
            // string variableName = "ds1401()://currentTime";
            string variableName = variable_listBox.SelectedItem.ToString();
            read_xil_variable_text.Text = PortConnection.Read(variableName).ToString();
        }
        private void connect_maport_button_Click(object sender, RoutedEventArgs e)
        {
            {
                status_message_text.Text = PortConnection.Initialise();
                update_variable_listBox();
            }
        }
    }
}
