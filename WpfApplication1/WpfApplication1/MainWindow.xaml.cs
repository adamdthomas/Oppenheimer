using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using WinForms = System.Windows.Forms;
using System.Diagnostics;

namespace Oppenheimer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ContextMenu m_menu;
        public static char c1 = (char)10;
        public static string L = c1.ToString();

        public ObservableCollection<CheckedListItem<Application>> applications { get; set; }
        private WinForms.NotifyIcon notifier = new WinForms.NotifyIcon();
        public delegate void UpdateLogCallback(string message);

        public MainWindow()
        {
            InitializeComponent();
            Utilities.CreateLogFile();
            //m_menu = new ContextMenu();
            loadList();
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

        public void WriteToLog(string Message)
        {
            Message = DateTime.Now.ToString() + " - " + Message;
            txtLog.AppendText(Message + L);
            Utilities.LogToFile(Message);
            txtLog.ScrollToEnd();
        }

        public void LogFromThread(string Message)
        {
            txtLog.Dispatcher.Invoke(
            new UpdateLogCallback(this.WriteToLog),
            new object[] { Message });
        }

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
            kill();
        }

        private void Menu_Expand(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
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


        public void kill()
        {
            foreach (var app in applications)
            {
                if (app.IsChecked)
                {
                    Boom(app.Item.imagename);
                }
            }
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

                Utilities.AddApp(app.Item.name, app.Item.imagename, check);
            }
        }

        public void Boom(string ProcessName)
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
                    LogFromThread("Killing process "+ p.ProcessName + ": " + p.Id);
                    p.Kill();
                }
            }
            catch (Exception ex) { }
        }

        public void loadList()
        {
            applications = new ObservableCollection<CheckedListItem<Application>>();

            List<Application> apps = Utilities.GetApps();

            foreach (var app in apps)
            {
                applications.Add(new CheckedListItem<Application>(new Application() { name = app.name, imagename = app.imagename, isCheckedString = app.isCheckedString }));
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

        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btnKill_Click(object sender, RoutedEventArgs e)
        {
            kill();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            updateSettings();
            string proc = txtImageName.Text.Replace(".exe", "");
            Utilities.AddApp(txtDisplayName.Text, proc);
            WriteToLog("Adding target: " + txtDisplayName.Text + " with process name of: " + proc + ".exe");
            loadList();
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            updateSettings();

            foreach (CheckedListItem<Application> item in lstApps.SelectedItems)
            {
                Utilities.RemoveApp(item.Item.name, item.Item.imagename, item.Item.isCheckedString);
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
    }
}
