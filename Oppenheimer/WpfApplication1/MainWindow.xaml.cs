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

        public static char c1 = (char)10;
        public static string L = c1.ToString();
        public static int retryCount;
        public static int retryDelay;
        public string isEnabledTemp = "true";
        bool keepKilling = false;
        public int agentCycleTime = 5000;
        public bool retryInProgress = false;



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
            updateFields();
            loadList();
            
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

                WinForms.NotifyIcon ni = new WinForms.NotifyIcon();

                string executableLocation = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string icoLocation = System.IO.Path.Combine(executableLocation, "favicon.ico");

                ni.Icon = new System.Drawing.Icon(icoLocation);
                notifier.MouseDown += new WinForms.MouseEventHandler(notifier_MouseDown);
                notifier.Icon = ni.Icon;
                notifier.Visible = true;

       
                //ni.Visible = true;
                notifier.DoubleClick +=
                    delegate (object sender, EventArgs args)
                    { 
                        Show();
                        WindowState = WindowState.Normal;
                        Activate();
                        
                    };
            }
            catch (Exception e){}

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
                Hide();
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
            killMain();
        }

        private void Menu_Restart(object sender, RoutedEventArgs e)
        {
            Restart();
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
                Close();
            }
            catch (Exception) { }
        }
        #endregion

        #region kill functions
        public void killMain()
        {
            retryInProgress = false;
            retryCount = Convert.ToInt32(txtRetryCount.Text);
            retryDelay = Convert.ToInt32(txtRetryTime.Text);

            if (retryCount > 1)
            {
                retryInProgress = true;
                LogFromThread("Killing Targets. Will retry " + txtRetryCount.Text + " times with a delay of " + txtRetryTime.Text + " milliseconds.");
                Thread killRetry = new Thread(retryKillApps);
                killRetry.Start();
            }
            else
            {
                LogFromThread("Killing Targets...");
                Thread killOnce = new Thread(killApps);
                killOnce.Start();
            }
        }

        public void detectMain()
        {
            LogFromThread("Detecting Targets...");
            Thread detectOnce = new Thread(detectApps);
            detectOnce.Start();
        }

        public void killApps()
        {
            Thread.CurrentThread.IsBackground = true;
            foreach (var app in applications)
            {
                if (app.IsChecked)
                {
                    killApp(app.Item.imagename, app.Item.timeInt, app.Item.timeType);

                    if (!retryInProgress)
                    {
                        if (app.Item.willRestart.ToLower() == "true")
                        {
                            Process.Start(app.Item.restartPath);
                            LogFromThread("Attempting to restart " + app.Item.name + " by opening: " + app.Item.restartPath);
                        }
                    }
                   
                }
            }
        }

        public void detectApps()
        {
            Thread.CurrentThread.IsBackground = true;
            foreach (var app in applications)
            {
                if (app.IsChecked)
                {
                    detectApp(app.Item.imagename, app.Item.timeInt, app.Item.timeType);
                }
            }
        }

        public void retryKillApps()
        {
            Thread.CurrentThread.IsBackground = true;
            for (int i = 0; i < retryCount; i++)
            {
                if (i == retryCount - 1) { retryInProgress = false; }
                LogFromThread("Retry count: " + (i+1).ToString());
                killApps();
                Thread.Sleep(retryDelay);
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
                                    LogFromThread("Killing " + p.ProcessName + ", Process ID: " + p.Id + " Age: " + span.ToString(@"hh\h\:mm\m\:ss\s"));
                                    p.Kill();
                                }
                                else
                                {
                                    LogFromThread("Skipping " + p.ProcessName + ", Process ID: " + p.Id + " Not old enough; Age: " + span.ToString(@"hh\h\:mm\m\:ss\s"));
                                }

                            }

                    }
                    catch (Exception ex) { }
                }
            }
        }



        public void detectApp(string ProcessName, string timeInt, string timeType)
        {
            double ageToKill = Utilities.ToSeconds(timeInt, timeType);

            if (ProcessName.Contains(@" / "))
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
                    LogFromThread("Would not be able to delete the contents of: " + ProcessName + " because it would be detrimental.");
                }
                else
                {
                    LogFromThread("Would delete the contents of: " + ProcessName);
                    LogsFromThread(Utilities.DetectFiles(ProcessName, ageToKill));
                }
            }
            else
            {
                if (ProcessName != "")//Deal with processes
                {
                    try
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
                                LogFromThread("Would kill " + p.ProcessName + ", Process ID: " + p.Id + " Age: " + span.ToString(@"hh\h\:mm\m\:ss\s"));
                            }
                            else
                            {
                                LogFromThread("Would skip " + p.ProcessName + ", Process ID: " + p.Id + " Not old enough; Age: " + span.ToString(@"hh\h\:mm\m\:ss\s"));
                            }

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

                Utilities.AddApp(app.Item.name, app.Item.imagename, check, app.Item.timeInt, app.Item.timeType, app.Item.hasAgent, app.Item.willRestart, app.Item.restartPath);
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

            OptionsSaved();


        }




        public void loadList()
        {
            applications = new ObservableCollection<CheckedListItem<Application>>();

            List<Application> apps = Utilities.GetApps();

            foreach (var app in apps)
            {
                applications.Add(new CheckedListItem<Application>(new Application() { name = app.name, imagename = app.imagename, isCheckedString = app.isCheckedString, timeInt = app.timeInt, timeType = app.timeType, hasAgent = app.hasAgent, willRestart = app.willRestart, restartPath = app.restartPath }));
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
            handleList();
        }

        private void handleList()
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
                    ckbRestart.IsChecked = Convert.ToBoolean(item.Item.willRestart);
                    txtRestartPath.Text = item.Item.restartPath;
                }
                catch (Exception e2) { LogFromThread(e2.ToString()); }

            }
            TargetSaved();
        }

        private void btnKill_Click(object sender, RoutedEventArgs e)
        {
            killMain();
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

            string checkWillRestart;
            if (ckbRestart.IsChecked.GetValueOrDefault())
            {
                checkWillRestart = "true";
            }
            else
            {
                checkWillRestart = "false";
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

            Utilities.AddApp(txtDisplayName.Text, proc, checkEnabled, txtTimeInt.Text, cboTimeType.Text, checkHasAgent, checkWillRestart, txtRestartPath.Text);
            WriteToLog("Adding target: " + txtDisplayName.Text + " with process name of: " + proc + ".exe");
            loadList();

            TargetSaved();



        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            updateSettings();

            foreach (CheckedListItem<Application> item in lstApps.SelectedItems)
            {
                Utilities.RemoveApp(item.Item.name, item.Item.imagename, item.Item.isCheckedString, item.Item.timeInt, item.Item.timeType, item.Item.hasAgent, item.Item.willRestart, item.Item.restartPath);
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
            Process.Start("notepad.exe", Properties.Settings.Default.LogPath + @"\Oppenheimer_Log.txt");
            WriteToLog("Log file opened: " + Properties.Settings.Default.LogPath + @"\Oppenheimer_Log.txt");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            
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
            OptionsChanged();
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
            OptionsChanged();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            updateSettings();
        }

        private void btnRestart_Click(object sender, RoutedEventArgs e)
        {
            Restart();
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            txtImageName.Text = "";
            txtDisplayName.Text = "";
            ckbHasAgent.IsChecked = false;
            ckbEnableTarget.IsChecked = true;
            ckbRestart.IsChecked = false;
            txtRestartPath.Text = "";
            txtTimeInt.Text = "0";
         
        }

        private void lstApps_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            handleList();
        }

        private void btnDetect_Click(object sender, RoutedEventArgs e)
        {
            detectMain();
        }

        private void btnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();



            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".exe";
            dlg.Filter = "Applications (*.exe)|*.exe";


            // Display OpenFileDialog by calling ShowDialog method 
            bool? result = dlg.ShowDialog();


            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                txtRestartPath.Text = dlg.FileName;
            }
        }

        private void btnSelectFolderLog_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WinForms.FolderBrowserDialog();
            dialog.ShowDialog();
            txtLogPath.Text = dialog.SelectedPath;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Shutdown();
        }

        public void Shutdown()
        {
            notifier.Visible = false;
            updateSettings();
            System.Windows.Application.Current.Shutdown();
        }

        public void OptionsChanged()
        {
            try
            {
                btnSaveOptions.Content = "Save Options";
            }
            catch (Exception)
            {

            }
           
            
  
        }

        public void OptionsSaved()
        {

            try
            {
                btnSaveOptions.Content = "Options Saved";
            }
            catch (Exception)
            {

            }

        }


        public void TargetChanged()
        {
            try
            {
                
                btnAdd.Content = "Save*";
            }
            catch (Exception)
            {

            }



        }

        public void TargetSaved()
        {

            try
            {
                btnAdd.Content = "Saved";
            }
            catch (Exception)
            {

            }

        }

        private void txtCycle_TextChanged(object sender, TextChangedEventArgs e)
        {
            OptionsChanged();
        }

        private void cboTimeTypeCycle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OptionsChanged();
        }

        private void txtRetryCount_TextChanged(object sender, TextChangedEventArgs e)
        {
            OptionsChanged();
        }

        private void txtRetryTime_TextChanged(object sender, TextChangedEventArgs e)
        {
            OptionsChanged();
        }

        private void txtLogPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            OptionsChanged();
        }

        private void txtDisplayName_TextChanged(object sender, TextChangedEventArgs e)
        {
            TargetChanged();
        }

        private void txtRestartPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            TargetChanged();

        }

        private void txtImageName_TextChanged(object sender, TextChangedEventArgs e)
        {
            TargetChanged();

        }

        private void txtTimeInt_TextChanged(object sender, TextChangedEventArgs e)
        {
            TargetChanged();

        }

        private void cboTimeType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TargetChanged();

        }

        private void ckbHasAgent_Click(object sender, RoutedEventArgs e)
        {
            TargetChanged();

        }

        private void ckbEnableTarget_Click(object sender, RoutedEventArgs e)
        {
            TargetChanged();

        }

        private void ckbRestart_Click(object sender, RoutedEventArgs e)
        {
            TargetChanged();

        }
    }
}
