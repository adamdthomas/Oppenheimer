﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oppenheimer
{
    public static class Utilities
    {

        private static string LogPath = Oppenheimer.Properties.Settings.Default.LogPath;
        public static string LogFullPath;
        public static List<Application> GetApps()
        {
            List<Application> apps = new List<Application>();
            string appString = Properties.Settings.Default.Apps;

            string[] appArray = appString.Split(',');

            foreach (var app in appArray)
            {
                try
                {
                    string[] appDetailsArray = app.Split('-');
                    Application curApp = new Application();
                    curApp.name = appDetailsArray[0];
                    curApp.imagename = appDetailsArray[1];
                    curApp.isCheckedString = appDetailsArray[2];
                    apps.Add(curApp);
                }
                catch (Exception)
                {

                  
                }
            
            }

            return apps;
        }

        public static void AddApp(string displayName, string imageName)
        {
            string firstDelim = "";
            string appString = Properties.Settings.Default.Apps;
            if (appString != "")
            {
                firstDelim = ",";
            }

            if (!appString.Contains(displayName))
            {
                Properties.Settings.Default.Apps = appString + firstDelim + displayName + "-" + imageName + "-true";
                Properties.Settings.Default.Save();
                Properties.Settings.Default.Reload();
            }

        }

        public static void AddApp(string displayName, string imageName, string isChecked)
        {
            string firstDelim = "";
            string appString = Properties.Settings.Default.Apps;
            if (appString != "")
            {
                firstDelim = ",";
            }

            if (!appString.Contains(displayName))
            {
                Properties.Settings.Default.Apps = appString + firstDelim + displayName + "-" + imageName + "-" + isChecked;
                Properties.Settings.Default.Save();
                Properties.Settings.Default.Reload();
            }

        }

        public static void RemoveApp(string displayName, string imageName, string isCheckedString)
        {
            string appString = Properties.Settings.Default.Apps;
            appString = appString.Replace(displayName + "-" + imageName + "-" + isCheckedString + ",", "");
            appString = appString.Replace(displayName + "-" + imageName + "-" + isCheckedString, "");
            Properties.Settings.Default.Apps = appString;
            Properties.Settings.Default.Save();
            Properties.Settings.Default.Reload();
        }

        public static void RemoveAllApps()
        {
            Properties.Settings.Default.Apps = "";
            Properties.Settings.Default.Save();
            Properties.Settings.Default.Reload();
        }

        public static void CreateLogFile()
        {
            LogFullPath = LogPath + "Oppenheimer_Log.txt";
            System.IO.Directory.CreateDirectory(LogPath);
            if (!File.Exists(LogFullPath))
            {
                // Create a file to write to. 
                using (StreamWriter sw = File.CreateText(LogFullPath))
                {
                    sw.WriteLine(DateTime.Now.ToString() + " - Log file initiated.");
                    sw.Close();
                }
            }
        }

        

        public static void LogToFile(string Message)
        {
            try
            {
                using (StreamWriter sw = File.AppendText(LogFullPath))
                {
                    sw.WriteLine(Message);
                    sw.Close();
                }
            }
            catch (Exception l)
            {
                l.ToString();
            }
        }



    }
}