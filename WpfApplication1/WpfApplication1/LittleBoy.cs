using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oppenheimer
{
    public static class LittleBoy
    {
        public static void Boom(string ProcessName)
        {
            try
            {
                Process[] myProcesses;
                myProcesses = Process.GetProcessesByName(ProcessName);
                foreach (Process p in myProcesses)
                {
                    p.Kill();
                }
            } catch (Exception ex){}
        }
    }
}
