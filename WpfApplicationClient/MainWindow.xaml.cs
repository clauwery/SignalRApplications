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
        IPmPlatformManagement PlatformManagement = Activator.CreateInstance(serverType) as IPmPlatformManagement;
        public IPmSeekedPlatforms Platforms;
        public string RegisterPlatform(string platformName)
        {
            string Message = "";
            try
            {
                PlatformType platformType = (PlatformType)Enum.Parse(typeof(PlatformType), platformName);
                IPmMABXRegisterInfo RegisterInfo = (IPmMABXRegisterInfo)PlatformManagement.CreatePlatformRegistrationInfo(platformType);
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
        public void GetPlatforms()
        {
            Platforms = (IPmSeekedPlatforms)PlatformManagement.Platforms;
        }

/*        public string[] GetPlatformTypes()
        {
            foreach(PlatformType foo in PlatformType.GetValues(typeof(PlatformType)))
            {
                platformType[] = foo.ToString();
            }
            
        }
        */
        
    }

    class Port_Basics
    {
        private string MAPortConfigFile = @"..\Models\MAPortConfig.xml";
        private string vendorName = "dSPACE GmbH";
        private string productName = "XIL API";
        private string productVersion = "2015-B";
        private IList<string> VariableNames;
        public IMAPort MAPort;
        public void Initialise()
        {
            try
            {
                ITestbenchFactory TBFactory = new TestbenchFactory();
                ITestbench TB = TBFactory.CreateVendorSpecificTestbench(vendorName, productName, productVersion);
                MAPort = TB.MAPortFactory.CreateMAPort("MAPort");
                IMAPortConfig maPortConfig = MAPort.LoadConfiguration(MAPortConfigFile);
                MAPort.Configure(maPortConfig, false);
            }
            catch (TestbenchPortException ex)
            {
                Console.WriteLine("A XIL API TestbenchPortException occurred:");
                Console.WriteLine("Error description: {0}", ex.CodeDescription);
                Console.WriteLine("Detailed error description: {0}", ex.VendorCodeDescription);
                Console.ReadLine();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("A system exception occurred:");
                Console.WriteLine("Error description: {0}", ex.Message);
            }
            finally
            {
                // Dispose any instances of MAPort, Capture, and EESPort
            }
        }
        public double Read(string variableName)
        {
            if (MAPort!=null && MAPort.State==MAPortState.eSIMULATION_RUNNING && MAPort.VariableNames.Contains(variableName) && MAPort.IsReadable(variableName))
            {
                IFloatValue value = (IFloatValue)MAPort.Read(variableName);
                return value.Value;
            }
            else
            {
                return -1;
            }
        }
    }

}

namespace WpfApplicationClient
{
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
            foreach (PlatformType s in Enum.GetValues(typeof(PlatformType)))
                comboBox.Items.Add(s.ToString());
            comboBox.SelectedIndex = 0;
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
            string platformName = comboBox.SelectedItem.ToString();
            status_message_text.Text = XilConnection.RegisterPlatform(platformName);
            update_platform_list();
            PortConnection.Initialise();
        }
        private void clear_xil_button_Click(object sender, RoutedEventArgs e)
        {
            status_message_text.Text = XilConnection.Clear();
            update_platform_list();
        }
        private void update_platform_list()
        {
            if (XilConnection.Platforms != null)
            {
                platforms.Text = XilConnection.Platforms.Count.ToString();
            }
        }
        private void read_xil_variable_button_Click(object sender, RoutedEventArgs e)
        {
            string variableName = "Platform()://currentTime";
            read_xil_variable_text.Text = PortConnection.Read(variableName).ToString();
            
        }
    }
}
