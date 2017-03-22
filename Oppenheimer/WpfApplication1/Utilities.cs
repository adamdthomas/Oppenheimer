using System;
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
                    string[] appDetailsArray = app.Split('~');
                    Application curApp = new Application();
                    curApp.name = appDetailsArray[0];
                    curApp.imagename = appDetailsArray[1];
                    curApp.isCheckedString = appDetailsArray[2];

                    curApp.timeInt = appDetailsArray[3];
                    curApp.timeType = appDetailsArray[4];
                    curApp.hasAgent = appDetailsArray[5];

                    apps.Add(curApp);
                }
                catch (Exception)
                {

                  
                }
            
            }

            return apps;
        }

        public static bool TestAppString(string appString)
        {
            bool testPassed = true;
                try
                {
                    string[] appArray = appString.Split(',');

                    foreach (var app in appArray)
                    {
                            string[] appDetailsArray = app.Split('~');
                            string iamaname = appDetailsArray[0];
                            string iamanimage = appDetailsArray[1];
                            string iamachecked = appDetailsArray[2];
                            string iamatimeint = appDetailsArray[3];
                            string iamatimetype = appDetailsArray[4];
                            string iamanagent = appDetailsArray[5];
                }
            }
            catch (Exception)
            {

                testPassed = false;
            }

            return testPassed;
        }

        public static void AddApp(string displayName, string imageName, string timeInt, string timeType, string hasAgent)
        {
            string appStr = Properties.Settings.Default.Apps;
            appStr = appStr.Replace(GetAppStringByDisplayName(displayName), "");
            Properties.Settings.Default.Apps = appStr;
            Properties.Settings.Default.Save();
            Properties.Settings.Default.Reload();

            string firstDelim = "";
            string appString = Properties.Settings.Default.Apps;
            if (appString != "")
            {
                firstDelim = ",";
            }

            if (!appString.Contains(displayName))
            {
                Properties.Settings.Default.Apps = appString + firstDelim + displayName + "~" + imageName + "~true~" + timeInt + "~" + timeType + "~" + hasAgent ;
                Properties.Settings.Default.Save();
                Properties.Settings.Default.Reload();
            }
       
        }

        public static string GetAppStringByDisplayName(string displayName)
        {
            string AppStr = "";
            
            try
            {
                string appString = Properties.Settings.Default.Apps;
                string[] appArray = appString.Split(',');

                foreach (var app in appArray)
                {

                    string[] appDetailsArray = app.Split('~');

                    if (displayName.ToUpper() == appDetailsArray[0].ToUpper())
                    {
                        AppStr = appDetailsArray[0] + "~" + appDetailsArray[1] + "~" + appDetailsArray[2] + "~" + appDetailsArray[3] + "~" + appDetailsArray[4] + "~" + appDetailsArray[5];
                        break;
                    }

 
                }
            }
            catch (Exception) {}


            return AppStr;
        }

        public static void AddApp(string displayName, string imageName, string isChecked,string timeInt, string timeType, string hasAgent)
        {
            string appStr = Properties.Settings.Default.Apps;
            try
            {
                appStr = appStr.Replace(GetAppStringByDisplayName(displayName), "");
                Properties.Settings.Default.Apps = appStr;
                Properties.Settings.Default.Save();
                Properties.Settings.Default.Reload();
            }
            catch (Exception){}


            string firstDelim = "";
            string appString = Properties.Settings.Default.Apps;
            if (appString != "")
            {
                firstDelim = ",";
            }

            if (!appString.Contains(displayName))
            {
                Properties.Settings.Default.Apps = appString + firstDelim + displayName + "~" + imageName + "~" + isChecked + "~" + timeInt + "~" + timeType + "~" + hasAgent;
                Properties.Settings.Default.Save();
                Properties.Settings.Default.Reload();
            }

        }

        public static void RemoveApp(string displayName, string imageName, string isCheckedString, string timeInt, string timeType, string hasAgent)
        {
            string appString = Properties.Settings.Default.Apps;
            appString = appString.Replace(displayName + "~" + imageName + "~" + isCheckedString + "~" + timeInt + "~" + timeType + "~" + hasAgent + ",", "");
            appString = appString.Replace(displayName + "~" + imageName + "~" + isCheckedString + "~" + timeInt + "~" + timeType + "~" + hasAgent, "");
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

        public static List<string> RemoveFiles(string Path, double ageToKill)
        {

            List<string> deleteInfo = new List<string>();

            try
            {
                System.IO.DirectoryInfo directory = new DirectoryInfo(Path);
                foreach (FileInfo file in directory.GetFiles())
                {

                    TimeSpan span = DateTime.Now.Subtract(file.LastWriteTime);
                    if (span.TotalSeconds > ageToKill)
                    {
                        deleteInfo.Add("Deleting file: " + file.FullName + " Age: " + span.TotalMinutes.ToString() + " min.");
                        file.Delete();
                    }else
                    {
                        deleteInfo.Add("Skipping file: " + file.FullName + " Not old enough; Age: " + span.TotalMinutes.ToString() + " min.");
                    }
                }
                foreach (DirectoryInfo currentDirectory in directory.GetDirectories())
                {
                    TimeSpan span = DateTime.Now.Subtract(currentDirectory.LastWriteTime);
                    if (span.TotalSeconds > ageToKill)
                    {
                        deleteInfo.Add("Deleting folder: " + currentDirectory.FullName + " Age: " + span.TotalMinutes.ToString() + " min.");
                        currentDirectory.Delete(true);
                    }
                    else
                    {
                        deleteInfo.Add("Skipping folder: " + currentDirectory.FullName + " Not old enough; Age: " + span.TotalMinutes.ToString() + " min.");
                    }
                }
            }
            catch (Exception e){
                deleteInfo.Add("Error: " + e.ToString());
            }

            return deleteInfo;
        }

        public static double ToSeconds(string timeInt, string timeType)
        {
            int sec;

            switch (timeType.ToUpper())
            {
                case "DAYS":
                    sec = int.Parse(timeInt) * 86400;
                    break;
                case "HOURS":
                    sec = int.Parse(timeInt) * 3600;
                    break;
                case "MINUTES":
                    sec = int.Parse(timeInt) * 60;
                    break;
                case "SECONDS":
                    sec = int.Parse(timeInt);
                    break;
                default:
                    sec = 0;
                    break;
            }
            return sec;
        }

        public static int GetCycleTime()
        {
            return Convert.ToInt32(ToSeconds(Properties.Settings.Default.CycleTimeInt, Properties.Settings.Default.CycleTimeType)) * 1000;
        }

    }
}
