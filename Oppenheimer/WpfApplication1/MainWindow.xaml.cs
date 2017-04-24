using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using WinForms = System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

namespace Oppenheimer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private ContextMenu m_menu;
        public static char c1 = (char)10;
        public static string L = c1.ToString();
        public string isEnabledTemp = "true";
        bool keepKilling = false;
        public int agentCycleTime = 5000;



        public ObservableCollection<CheckedListItem<Application>> applications { get; set; }
        private WinForms.NotifyIcon notifier = new WinForms.NotifyIcon();
        public delegate void UpdateLogCallback(string message);
        public delegate void UpdateLogsCallback(List<string> message);

        public MainWindow()
        {

            //-----------------DEBUG------------------------
            //Properties.Settings.Default.MinOnOpen = false;
            //----------------------------------------------

            InitializeComponent();

            loadList();
            updateFields();
            Utilities.CreateLogFile();

            if (ckbOpenMinimized.IsChecked.GetValueOrDefault())
            {
               Minimize();
            }

            if(ckbAgentsOnOpen.IsChecked.GetValueOrDefault())
            {
                Deploy();
            }


            WriteToLog("Welcome to Oppenheimer.");

            try
            {
                System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();

                string executableLocation = System.IO.Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location);
                string icoLocation = System.IO.Path.Combine(executableLocation, "favicon.ico");

                ni.Icon = new System.Drawing.Icon(icoLocation);
                this.notifier.MouseDown += new WinForms.MouseEventHandler(notifier_MouseDown);
                this.notifier.Icon = ni.Icon;
                this.notifier.Visible = true;

                //ni.Visible = true;
                ni.DoubleClick +=
                    delegate (object sender, EventArgs args)
                    {
                        this.Show();
                        this.WindowState = WindowState.Normal;
                    };
            }
            catch (Exception){}

        }

        #region logging functions
        public void WriteToLog(string Message)
        {
            Message = DateTime.Now.ToString() + " - " + Message;
            txtLog.AppendText(Message + L);
            Utilities.LogToFile(Message);
            txtLog.ScrollToEnd();
        }

        public void WriteToLog(List<string> Messages)
        {
            foreach (var Message in Messages)
            {
                WriteToLog(Message);
            }
        }

        public void LogFromThread(string Message)
        {
            txtLog.Dispatcher.Invoke(
            new UpdateLogCallback(this.WriteToLog),
            new object[] { Message });
        }

        public void LogsFromThread(List<string> Message)
        {
            txtLog.Dispatcher.Invoke(
            new UpdateLogsCallback(this.WriteToLog),
            new object[] { Message });
        }
        #endregion

        #region tray functions
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
                this.Hide();
            base.OnStateChanged(e);
        }

        void notifier_MouseDown(object sender, WinForms.MouseEventArgs e)
        {
            if (e.Button == WinForms.MouseButtons.Right)
            {
                ContextMenu menu = (ContextMenu)this.FindResource("NotifierContextMenu");
                menu.IsOpen = true;
            }
        }

        private void Menu_Open(object sender, RoutedEventArgs e)
        {
            killApps();
        }

        private void Menu_Expand(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void Minimize()
        {
            this.Hide();
            this.WindowState = WindowState.Minimized;

        }

        private void Menu_Cancel(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = (ContextMenu)this.FindResource("NotifierContextMenu");
            menu.IsOpen = false;
        }

        private void Menu_Close(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Close();
            }
            catch (Exception) { }
        }
        #endregion

        #region kill functions
        public void killApps()
        {
            LogFromThread("Killing Targets...");
            foreach (var app in applications)
            {
                if (app.IsChecked)
                {
                    killApp(app.Item.imagename, app.Item.timeInt, app.Item.timeType);
                }
            }
        }

        public void agentKillApps()
        {
            while (keepKilling)
            {
                LogFromThread("Agent running...");
                foreach (var app in applications)
                {
                    if (app.IsChecked && app.Item.hasAgent == "true")
                    {
                        LogFromThread("Agent launched for: " + app.Item.imagename);
                        killApp(app.Item.imagename, app.Item.timeInt, app.Item.timeType);
                    }
                }
                Thread.Sleep(agentCycleTime);
            }
        }

        public void killApp(string ProcessName, string timeInt, string timeType)
        {
            double ageToKill = Utilities.ToSeconds(timeInt, timeType);

            if(ProcessName.Contains(@"/"))
            {
                LogFromThread("Did you mean to use a forward slash in the path: " + ProcessName + "?");
                ProcessName = "";//Ignore bad paths
            }

            if (ProcessName.Contains(@"\")) //Deal with files and folders
            {

                string lastchar = ProcessName.Substring(ProcessName.Length - 1);
                if (lastchar != @"\")
                {
                    ProcessName = ProcessName + @"\";
                }
                //Saftey Nets:
                if (ProcessName.ToUpper() == @"C:\" || ProcessName.ToUpper() == @"C:\PROGRAM FILES\" || ProcessName.ToUpper() == @"C:\PROGRAM FILES (X86)\" || ProcessName.ToUpper() == @"C:\PROGRAMDATA\" || ProcessName.ToUpper() == @"C:\USERS\" || ProcessName.ToUpper() == @"C:\WINDOWS\")
                {
                    LogFromThread("Cannot delete the contents of: " + ProcessName + " are you out of your mind???");
                }
                else
                {
                    LogFromThread("Deleting the contents of: " + ProcessName);
                    LogsFromThread(Utilities.RemoveFiles(ProcessName, ageToKill));
                }
            }
            else
            {
                if (ProcessName != "")//Deal with processes
                {
                    try
                    {
                        for (int i = 0; i < Convert.ToInt32(txtRetryCount.Text); i++)
                        {


                            Process[] myProcesses;
                            myProcesses = Process.GetProcessesByName(ProcessName);

                            if (myProcesses.Length == 0)
                            {
                                LogFromThread("Process: " + ProcessName + ".exe not found.");
                            }

                            foreach (Process p in myProcesses)
                            {

                                TimeSpan span = DateTime.Now.Subtract(p.StartTime);
                                if (span.TotalSeconds > ageToKill)
                                {
                                    LogFromThread("Killing process " + p.ProcessName + ": " + p.Id + " Age: " + span.TotalMinutes.ToString() + " min.");
                                    p.Kill();
                                }
                                else
                                {
                                    LogFromThread("Skipping process " + p.ProcessName + ": " + p.Id + " Not old enough; Age: " + span.TotalMinutes.ToString() + " min.");
                                }

                            }

                            Thread.Sleep(Convert.ToInt32(txtRetryTime.Text));
                        }
                    }
                    catch (Exception ex) { }
                }
            }
        }

        #endregion

        public void updateFields()
        {
            txtLogPath.Text = Properties.Settings.Default.LogPath;
            ckbOpenMinimized.IsChecked = Properties.Settings.Default.MinOnOpen;
            txtCycle.Text = Properties.Settings.Default.CycleTimeInt;
            cboTimeTypeCycle.Text = Properties.Settings.Default.CycleTimeType;
            ckbAgentsOnOpen.IsChecked = Properties.Settings.Default.AgentOnOpen;
            txtRetryTime.Text = Properties.Settings.Default.RetryTime;
            txtRetryCount.Text = Properties.Settings.Default.RetryCount;

        }
        public void updateSettings()
        {

            Utilities.RemoveAllApps();

            foreach (var app in applications)
            {
                string check;
                if (app.IsChecked)
                {
                    check = "true";
                }
                else
                {
                    check = "false";
                }

                Utilities.AddApp(app.Item.name, app.Item.imagename, check, app.Item.timeInt, app.Item.timeType, app.Item.hasAgent);
            }

            Properties.Settings.Default.LogPath = txtLogPath.Text;
            Properties.Settings.Default.MinOnOpen = ckbOpenMinimized.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.CycleTimeInt = txtCycle.Text;
            Properties.Settings.Default.CycleTimeType = cboTimeTypeCycle.Text;
            Properties.Settings.Default.AgentOnOpen = ckbAgentsOnOpen.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.RetryTime = txtRetryTime.Text;
            Properties.Settings.Default.RetryCount = txtRetryCount.Text;
            Properties.Settings.Default.Save();
            Properties.Settings.Default.Reload();


        }

        public void loadList()
        {
            applications = new ObservableCollection<CheckedListItem<Application>>();

            List<Application> apps = Utilities.GetApps();

            foreach (var app in apps)
            {
                applications.Add(new CheckedListItem<Application>(new Application() { name = app.name, imagename = app.imagename, isCheckedString = app.isCheckedString, timeInt = app.timeInt, timeType = app.timeType, hasAgent = app.hasAgent}));
            }

            DataContext = this;

            foreach (var app in applications)
            {
                app.IsChecked = Convert.ToBoolean(app.Item.isCheckedString);
            }

            lstApps.ItemsSource = null;
            lstApps.ItemsSource = applications;

            updateSettings();
        }

        private static void Restart()
        {
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.WindowStyle = ProcessWindowStyle.Hidden;
            proc.FileName = "cmd";
            proc.Arguments = "/C shutdown -f -r -t 3";
            Process.Start(proc);
        }



        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateSettings();

            foreach (CheckedListItem<Application> item in lstApps.SelectedItems)
            {
                try
                {
                    txtDisplayName.Text = item.Item.name;
                    txtImageName.Text = item.Item.imagename;
                    isEnabledTemp = item.Item.isCheckedString;
                    txtTimeInt.Text = item.Item.timeInt;
                    cboTimeType.Text = item.Item.timeType;
                    ckbHasAgent.IsChecked = Convert.ToBoolean(item.Item.hasAgent);
                    ckbEnableTarget.IsChecked = item.IsChecked;
                }
                catch (Exception){}

            }
            //loadList();
        }

        private void btnKill_Click(object sender, RoutedEventArgs e)
        {
            killApps();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            updateSettings();
            string proc = txtImageName.Text.Replace(".exe", "");
            string checkHasAgent;
            if (ckbHasAgent.IsChecked.GetValueOrDefault())
            {
                checkHasAgent = "true";
            }
            else
            {
                checkHasAgent = "false";
            }

            string checkEnabled;
            if (ckbEnableTarget.IsChecked.GetValueOrDefault())
            {
                checkEnabled = "true";
            }
            else
            {
                checkEnabled = "false";
            }

            Utilities.AddApp(txtDisplayName.Text, proc, checkEnabled, txtTimeInt.Text, cboTimeType.Text, checkHasAgent);
            WriteToLog("Adding target: " + txtDisplayName.Text + " with process name of: " + proc + ".exe");
            loadList();
 

        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            updateSettings();

            foreach (CheckedListItem<Application> item in lstApps.SelectedItems)
            {
                Utilities.RemoveApp(item.Item.name, item.Item.imagename, item.Item.isCheckedString, item.Item.timeInt, item.Item.timeType, item.Item.hasAgent);
                WriteToLog("Removing target: " + item.Item.name + " with process name of: " + item.Item.imagename + ".exe");
            }
            loadList();


        }

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            txtLog.Text = "";
        }

        private void btnOpenLog_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("notepad.exe", Properties.Settings.Default.LogPath + "Oppenheimer_Log.txt");
            WriteToLog("Log file opened: " + Properties.Settings.Default.LogPath + "Oppenheimer_Log.txt");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            updateSettings();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            Minimize();
        }

        private void ckbOpenMinimized_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ckbOpenMinimized_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.MinOnOpen = ckbOpenMinimized.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.Save();
            Properties.Settings.Default.Reload();
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            txtExport.Text = Properties.Settings.Default.Apps;
            Clipboard.SetText(Properties.Settings.Default.Apps);
            lblStatus.Content = "Exported targets copied to clipboard...";
            WriteToLog("Exporting targets: '" + Properties.Settings.Default.Apps + "'");

        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            if (Utilities.TestAppString(txtExport.Text))
            {
                Properties.Settings.Default.Apps = txtExport.Text;
                Properties.Settings.Default.Save();
                Properties.Settings.Default.Reload();
                loadList();
                lblStatus.Content = "Import Successful...";
                WriteToLog("Importing targets: '" + txtExport.Text + "'");
            }
            else
            {
                lblStatus.Content = "Could not import. Bad string structure. Try using an exported string";
                WriteToLog("Error importing targets: '" + txtExport.Text + "'");
            }

            loadList();
        }

        private void txtImageName_Copy_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
                e.Handled = true;
        }

        public void Deploy()
        {
            updateSettings();

            agentCycleTime = Utilities.GetCycleTime();

            if (btnDeploy.Content.ToString() == "Deploy Agents")
            {
                LogFromThread("Deploying automation agents. Agents will launch every " + txtCycle.Text + " " + cboTimeTypeCycle.Text);
                Thread killThread = new Thread(agentKillApps);
                keepKilling = true;
                killThread.Start();
                btnDeploy.Content = "Stop Agents";
            }
            else
            {
                LogFromThread("Stopping automation agents...");
                keepKilling = false;
                btnDeploy.Content = "Deploy Agents";
            }
        }

        private void btnDeploy_Click(object sender, RoutedEventArgs e)
        {

            Deploy();

        }

        private void ckbAgentsOnOpen_Click(object sender, RoutedEventArgs e)
        {
            updateSettings();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            updateSettings();
        }

        private void btnRestart_Click(object sender, RoutedEventArgs e)
        {
            Restart();
        }
    }
}
