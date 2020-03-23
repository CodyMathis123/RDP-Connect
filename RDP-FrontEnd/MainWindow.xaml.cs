using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using System.DirectoryServices;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.ComponentModel;
using System.Management;
using System.Collections.ObjectModel;

namespace RDP_FrontEnd
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public enum ADObjectClass
        {
            Group,
            Computer,
            DomainController,
            User,
            OrganizationalUnit
        }
        
        private string company = Properties.Settings.Default.Company.ToString();
        private string sqlServer = Properties.Settings.Default.SQLServer.ToString();
        private string sqlInstance = Properties.Settings.Default.SQLInstance.ToString();
        private string sqlDB = Properties.Settings.Default.SQLdb.ToString();
        private string sqlTable = Properties.Settings.Default.SQLtable.ToString();
        private string runningOn = System.Environment.GetEnvironmentVariable("COMPUTERNAME");
        private string whoseRunning = System.Environment.GetEnvironmentVariable("USERNAME");
        private bool rdp_ComputerGroupEnabled = Properties.Settings.Default.RDP_ComputerGroup_Enabled;
        private string rdp_ComputerGroup = Properties.Settings.Default.RDP_ComputerGroup.ToString();
        private string rdp_UserGroup = Properties.Settings.Default.RDP_UserGroup.ToString();
        private bool rdp_UserGroupEnabled = Properties.Settings.Default.RDP_UserGroup_Enabled;
        List<Endpoint> endpoints = new List<Endpoint>();
        private bool userGroupMembership = false;
        ADObject rdp_UserGroupObj = new ADObject(null,null,null);

        public MainWindow()
        {
            InitializeComponent();
            string AppName = string.Format("{0} RDP Connect", company);
            this.Title = AppName;
            WriteInfo(string.Format("{0} launched {1}", whoseRunning, AppName), 1000);
            ValidateUser();
            LoadUsersEndpoints();
        }

        private void LoadUsersEndpoints()
        {
            ProgressRing.IsActive = true;
            ProgressRing.Visibility = Visibility.Visible;
            BackgroundWorker worker = new BackgroundWorker();

            worker.DoWork += delegate (object s, DoWorkEventArgs args)
            {
                endpoints = GetUserEndpoints(whoseRunning);
            };

            // RunWorkerCompleted will fire on the UI thread when the background process is complete
            worker.RunWorkerCompleted += delegate (object s, RunWorkerCompletedEventArgs args)
            {
                if (args.Error != null)
                {
                    WriteError((string.Format("Failed to load endpoints from database for user {0}", whoseRunning)), 1200);
                }
                List<string> userNames = GetUserNames(whoseRunning);
                foreach (string userName in userNames)
                {
                    ComboBoxItem cbi = new ComboBoxItem
                    {
                        Content = userName
                    };
                    ComboBox_UsernameOptions.Items.Add(cbi);
                }
                foreach (ComboBoxItem item in ComboBox_UsernameOptions.Items)
                {
                    string userLookup = string.Format("\\{0}", whoseRunning);
                    if (item.ToString().Contains(userLookup))
                    {
                        item.IsSelected = true;
                    }
                }
                if (ComboBox_UsernameOptions.SelectedItem == null)
                    ComboBox_UsernameOptions.SelectedItem = ComboBox_UsernameOptions.Items[0];
                ProgressRing.IsActive = false;
                ProgressRing.Visibility = Visibility.Collapsed;
            };
            worker.RunWorkerAsync();
        }

        private void ValidateUser()
        {
            if (rdp_UserGroupEnabled)
            {
                ADObject user = GetADObject(whoseRunning, ADObjectClass.User);
                try
                {
                    rdp_UserGroupObj = GetADObject(rdp_UserGroup, ADObjectClass.Group);
                }
                catch
                {
                    rdp_UserGroupObj = new ADObject(null, null, null);
                }
                if (rdp_UserGroupObj.DistingishedName != null)
                {
                    userGroupMembership = GetADGroupNestedMemberOf(user.DistingishedName, rdp_UserGroupObj.DistingishedName);
                }
                if (!userGroupMembership)
                {
                    WriteWarning(string.Format("User {0} is not in the necessary AD group {1}", whoseRunning, rdp_UserGroup), 1100);
                    MessageBox.Show(string.Format("User {0} is not in the necessary AD group {1}", whoseRunning, rdp_UserGroup), "RDP user access exception", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Environment.Exit(0);
                }
            }
        }

        private void Button_Connect_Click(object sender, RoutedEventArgs e)
        {
            Endpoint endpoint = (DataGrid_KnownEndpoints.SelectedItem as Endpoint);

            ConditionalRDPConnection(endpoint);
        }

        private string GetADDefaultNamingContext()
        {
            string defaultNamingContext;
            using (DirectoryEntry rootDSE = new DirectoryEntry("LDAP://RootDSE"))
            {
                defaultNamingContext = rootDSE.Properties["defaultNamingContext"].Value.ToString();
            }

            return string.Format("LDAP://{0}", defaultNamingContext);
        }

        private ADObject GetADObject(string name, ADObjectClass objectClass)
        {
            //' Set empty value for return object and search result
            ADObject returnValue = new ADObject(null,null,null);
            SearchResult searchResult = null;

            //' Get default naming context of current domain
            string defaultNamingContext = GetADDefaultNamingContext();

            //' Construct directory entry for directory searcher
            DirectoryEntry domain = new DirectoryEntry(defaultNamingContext);
            DirectorySearcher directorySearcher = new DirectorySearcher(domain);
            directorySearcher.PropertiesToLoad.Add("distinguishedName");
            directorySearcher.PropertiesToLoad.Add("name");
            directorySearcher.PropertiesToLoad.Add("description");

            switch (objectClass)
            {
                case ADObjectClass.DomainController:
                    directorySearcher.Filter = string.Format("(&(objectClass=computer)((dNSHostName={0})))", name);
                    break;
                case ADObjectClass.Computer:
                    directorySearcher.Filter = string.Format("(&(objectClass=computer)((sAMAccountName={0}$)))", name);
                    break;
                case ADObjectClass.Group:
                    directorySearcher.Filter = string.Format("(&(objectClass=group)((sAMAccountName={0})))", name);
                    break;
                case ADObjectClass.User:
                    directorySearcher.Filter = string.Format("(&(objectClass=user)((sAMAccountName={0})))", name);
                    break;
                case ADObjectClass.OrganizationalUnit:
                    directorySearcher.Filter = string.Format("(&(objectClass=organizationalUnit)((distinguishedName={0})))", name);
                    break;
            }

            //' Invoke directory searcher
            try
            {
                searchResult = directorySearcher.FindOne();
            }
            catch (Exception ex)
            {
                WriteError((string.Format("Failed to perfom Directory Search with error: {0}", ex)), 1210);
            }

            //' Return selected object type value
            if (searchResult != null)
            {
                DirectoryEntry directoryObject = searchResult.GetDirectoryEntry();
                returnValue = new ADObject(
                    string.Format("{0}", directoryObject.Properties["name"].Value), 
                    string.Format("{0}", directoryObject.Properties["distinguishedName"].Value), 
                    string.Format("{0}", directoryObject.Properties["description"].Value)
                    );
            }

            //' Dispose objects
            directorySearcher.Dispose();
            domain.Dispose();

            return returnValue;
        }

        private bool GetADGroupNestedMemberOf(string objectDN, string groupDN)
        {
            //' LDAP query for memberOf including nested
            string filter = string.Format("(&(distinguishedName={0})(memberOf:1.2.840.113556.1.4.1941:={1}))", objectDN, groupDN);

            DirectorySearcher searcher = new DirectorySearcher(filter);
            SearchResult result = searcher.FindOne();

            return result != null;
        }

        private List<Endpoint> GetUserEndpoints(string userName)
        {
            List<Endpoint> endPoints = new List<Endpoint>();
            List<string> citrixClient = CitrixRunningOn();
            string accountNumber = Regex.Replace(userName.ToString(), @"[^\d]", string.Empty);
            try
            {
                //' Get connection string
                SqlConnectionStringBuilder connectionString = GetSqlConnectionString();

                //' Connect to SQL server instance
                SqlConnection connection = new SqlConnection
                {
                    ConnectionString = connectionString.ConnectionString
                };
                connection.Open();

                //' Invoke SQL command
                SqlCommand command = connection.CreateCommand();
                command.CommandText = string.Format("SELECT Hostname,OperatingSystem,LastLoggedOnUser FROM [dbo].[{0}] WHERE (LastLoggedOnUser LIKE '%{1}' or LastLoggedOnUser LIKE '%{2}') AND HostName NOT LIKE 'S____V-XA7%'", sqlTable, accountNumber, userName);
                SqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows == true)
                {
                    while (reader.Read())
                    {
                        Endpoint data = new Endpoint(reader["Hostname"].ToString(), reader["OperatingSystem"].ToString(), null, reader["LastLoggedOnUser"].ToString(), false );
                        if (!citrixClient.Contains(data.Hostname) && data.Hostname != runningOn)
                        {
                            endPoints.Add(data);
                        }
                    }
                    reader.Close();
                    connection.Close();
                    if (rdp_ComputerGroupEnabled)
                    {
                        ADObject rdp_ComputerGroupObj = GetADObject(rdp_ComputerGroup, ADObjectClass.Group);
                        Parallel.ForEach(endPoints, (endpoint) =>
                        {
                            bool computerGroupMembership = false;
                            string computerDescription = string.Empty;
                            ADObject computer = GetADObject(endpoint.Hostname, ADObjectClass.Computer);
                            if (computer.DistingishedName != null)
                            {
                                computerGroupMembership = GetADGroupNestedMemberOf(computer.DistingishedName, rdp_ComputerGroupObj.DistingishedName);
                            }
                            if (computer.Description != null)
                            {
                                computerDescription = computer.Description;
                            }
                            endpoint.Description = computerDescription;
                            endpoint.RDPGW_Allow = computerGroupMembership;

                        });
                    }
                    else
                    {
                        Parallel.ForEach(endPoints, (endpoint) =>
                        {
                            bool computerGroupMembership = false;
                            string computerDescription = string.Empty;
                            ADObject computer = GetADObject(endpoint.Hostname, ADObjectClass.Computer);
                            if (computer.DistingishedName != null)
                            {
                                computerGroupMembership = true;
                            }
                            if (computer.Description != null)
                            {
                                computerDescription = computer.Description;
                            }
                            endpoint.Description = computerDescription;
                            endpoint.RDPGW_Allow = computerGroupMembership;

                        });
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError((string.Format("Failed to query database for endpoints based on the username {0} with error {1}", userName, ex)), 1220);
            }
            Endpoint customEndpoint = new Endpoint("Custom", "N/A", "N/A", "N/A", false );
            endPoints.Add(customEndpoint);
            return endPoints;
        }
        
        private List<string> GetUserNames(string userName)
        {
            List<string> userNames = new List<string>();
            ObservableCollection<User> users = new ObservableCollection<User>();
            string number = Regex.Replace(userName, @"[^\d]", string.Empty);
            try
            {
                //' Get connection string
                SqlConnectionStringBuilder connectionString = GetSqlConnectionString();

                //' Connect to SQL server instance
                SqlConnection connection = new SqlConnection
                {
                    ConnectionString = connectionString.ConnectionString
                };
                connection.Open();

                //' Invoke SQL command
                SqlCommand command = connection.CreateCommand();
                command.CommandText = string.Format("SELECT DISTINCT LastLoggedOnUser FROM [dbo].[{0}] WHERE LastLoggedOnUser LIKE '%{1}'", sqlTable, number);
                SqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows == true)
                {
                    while (reader.Read())
                    {
                        string fullUN = reader["LastLoggedOnUser"].ToString();
                        string domainName = fullUN.Split('\\')[0];
                        string un = fullUN.Split('\\')[1];
                        users.Add(new User(domainName, un, fullUN));
                        userNames.Add(fullUN);
                    }
                    reader.Close();
                    connection.Close();
                    userNames.Sort();
                }
                else
                {
                    users.Add(new User("N/A", "N/A", userName));
                    userNames.Add(userName);
                }
            }
            catch
            {
                users.Add(new User("N/A", "N/A", userName));
                userNames.Add(userName);
            }
            return userNames;
        }

        private SqlConnectionStringBuilder GetSqlConnectionString()
        {
            //' Set database connection string
            SqlConnectionStringBuilder connectionString = new SqlConnectionStringBuilder();
            if (sqlInstance == null || sqlInstance == string.Empty || sqlInstance == "DEFAULT")
            {
                connectionString.DataSource = sqlServer;
                connectionString.InitialCatalog = sqlDB;
                connectionString.IntegratedSecurity = true;
            }
            else
            {
                connectionString.DataSource = string.Format("{0}\\{1}", sqlServer, sqlInstance);
                connectionString.InitialCatalog = sqlDB;
                connectionString.IntegratedSecurity = true;
            }

            //' Set general properties for connection string
            connectionString.ConnectTimeout = 15;

            return connectionString;
        }

        private void ComboBox_UsernameOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataGrid_KnownEndpoints.Items.Clear();
            string selectedUser = (ComboBox_UsernameOptions.SelectedItem as ComboBoxItem).Content.ToString();
            foreach (Endpoint endpoint in endpoints)
            {
                if ((endpoint.LastLoggedOnUser.ToString() == selectedUser & endpoint.RDPGW_Allow) || endpoint.Hostname == "Custom")
                {
                    DataGrid_KnownEndpoints.Items.Add(endpoint);
                }
            }
        }

        private void DataGrid_KnownEndpoints_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string hostName = string.Empty;
            Endpoint endpoint = (DataGrid_KnownEndpoints.SelectedItem as Endpoint);
            if (endpoint != null)
            {
               hostName = endpoint.Hostname.ToString();
                if(hostName == "Custom")
                {
                    Textbox_Computer.IsReadOnly = false;
                    hostName = string.Empty;
                    Textbox_Computer.Focus();
                }
                else
                    Textbox_Computer.IsReadOnly = true;
            }
            Textbox_Computer.Text = hostName;
        }

        bool IsPortOpen(string host, int port, int timeout)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect(host, port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(timeout));
                    if (!success)
                    {
                        return false;
                    }

                    client.EndConnect(result);
                }

            }
            catch
            {
                return false;
            }
            return true;
        }

        bool IsPingable(string host)
        {
            try
            {
                Ping ping = new Ping();
                PingReply pingReply = ping.Send(host, 5000);

                if (pingReply.Status == IPStatus.Success)
                {
                    return true;
                }
                else
                {
                    return false;

                }
            }
            catch
            {
                return false;
            }
        }

        private List<string> CitrixRunningOn()
        {
            List<string> connectingClient = new List<String>();
            try
            {
                ManagementScope scopeCitrix = new ManagementScope(@"\\localhost\root\Citrix\euem");

                SelectQuery citrixClientConnect = new SelectQuery("Select ClientMachineName From citrix_euem_clientConnect");
                ManagementObjectSearcher citrixClientSearcher = new ManagementObjectSearcher(scopeCitrix, citrixClientConnect);
                using (ManagementObjectCollection citrixWMI = citrixClientSearcher.Get())
                {
                    foreach (ManagementObject client in citrixWMI)
                    {
                        connectingClient.Add(client["ClientMachineName"].ToString());
                        
                    }
                }
            }
            catch
            {
                connectingClient = new List<String>();
            }
            return connectingClient;
        }

        private void ConditionalRDPConnection(Endpoint endpoint)
        {

            if (endpoint != null)
            {
                string hostName = string.Empty;
                bool computerGroupMembership = false;

                hostName = endpoint.Hostname.ToString();

                if (hostName == "Custom")
                {
                    hostName = Textbox_Computer.Text.ToString();
                    ADObject computer = GetADObject(hostName, ADObjectClass.Computer);
                    if (computer.DistingishedName != null & computer.DistingishedName != string.Empty)
                    {
                        ADObject rdp_ComputerGroupObj = GetADObject(rdp_ComputerGroup, ADObjectClass.Group);
                        computerGroupMembership = GetADGroupNestedMemberOf(computer.DistingishedName, rdp_ComputerGroupObj.DistingishedName);
                    }
                    else
                    {
                        WriteWarning(string.Format("Computer {0} selected for conection was not found in AD", hostName), 1110);
                        MessageBox.Show(string.Format("Computer {0} selected for conection was not found in AD", hostName), "LDAP Search Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    computerGroupMembership = endpoint.RDPGW_Allow;
                }

                if (computerGroupMembership)
                {
                    if (IsPingable(hostName))
                    {
                        if (IsPortOpen(hostName, 3389, 5))
                        {
                            WriteInfo(string.Format("User {0} initiated connection to Computer {1}", whoseRunning, hostName), 1010);
                            Process mstsc = new Process();
                            mstsc.StartInfo.FileName = "mstsc.exe";
                            mstsc.StartInfo.Arguments = string.Format("/v:{0} /public {1} {2}", hostName, (MenuItem_MultiMon.IsChecked == true ? "/MultiMon" : string.Empty), (MenuItem_AdminSession.IsChecked == true ? "/admin" : string.Empty));
                            mstsc.Start();
                            System.Environment.Exit(0);
                        }
                        else
                        {
                            WriteWarning(string.Format("Computer {0} is not listening to RDP requests", hostName), 1120);
                            MessageBox.Show(string.Format("Computer {0} is not listening to RDP requests", hostName), "RDP port access exception", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        WriteWarning(string.Format("Computer {0} is not responding to ping", hostName), 1130);
                        MessageBox.Show(string.Format("Computer {0} is not responding to ping", hostName), "Client ping exception", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    WriteWarning(string.Format("Computer {0} is not in the necessary AD Group {1}", hostName, rdp_ComputerGroup), 1140);
                    MessageBox.Show(string.Format("Computer {0} is not the necessary AD Group {1}", hostName, rdp_ComputerGroup), "AD Group Membership Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DataGrid_KnownEndpoints_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataGrid_KnownEndpoints.SelectedItem == null) return;
            var selectedEndpoint = DataGrid_KnownEndpoints.SelectedItem as Endpoint;
            if (selectedEndpoint.Hostname == "Custom")
            {
                Textbox_Computer.Focus();
                return;
            }
            ConditionalRDPConnection(selectedEndpoint);
        }

        private void WriteLog(string message, EventLogEntryType eventLogEntryType, Int32 eventID)
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = string.Format("{0} RDP Connect", company);
                eventLog.WriteEntry(message, eventLogEntryType, eventID);
            }
        }

        private void WriteInfo(string message, Int32 eventID)
        {
            WriteLog(message, EventLogEntryType.Information, eventID);
        }

        private void WriteError(string message, Int32 eventID)
        {
            WriteLog(message, EventLogEntryType.Error, eventID);
        }

        private void WriteWarning(string message, Int32 eventID)
        {
            WriteLog(message, EventLogEntryType.Warning, eventID);
        }
    }
}
