using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Collections.Specialized;
using Microsoft.Win32;
using System.Reflection;
using System.Management;
using System.Diagnostics;

namespace RUST_server_wrapper
{
    class utils
    {

        public static void info(String inf)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n[INFO] {inf}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void warn(String war)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[INFO] {war}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void error(String err)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[INFO] {err}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void wget(String url, String path)
        {
            info($"Downloading {url} ..");
            using (WebClient webClient = new WebClient())
            {
                webClient.DownloadFile(url, path);
            }
        }

        public static void unzip(String file, String path)
        {
            info($"Unzipping: {file}");
            System.IO.Compression.ZipFile.ExtractToDirectory(file, path);
            File.Delete(file);
        }

        public static void runOnStartup()
        {
            info("Wrapper will now run on windows startup.");
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey
            ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            registryKey.SetValue("ServerWrapper", Assembly.GetEntryAssembly().Location);
        }

        public static void removeRunOnStartup()
        {
            info("Wrapper won't run windows startup anymore.");
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey
            ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            registryKey.DeleteValue("ServerWrapper");
        }

        public static void setupEnv()
        {
            if (!Directory.Exists("C:/ServerWrapper"))
            {
                info("Setting up server directory at: C:/ServerWrapper \n Server files are located at: C:/ServerWrapper/server");

                Directory.CreateDirectory(@"C:/ServerWrapper");
                utils.wget("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip", "C:/ServerWrapper/steamcmd.zip");
                utils.unzip("C:/ServerWrapper/steamcmd.zip", "C:/ServerWrapper");
            }
        }

        public static void killProcessAndChildrens(int pid)
        {
            ManagementObjectSearcher processSearcher = new ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection processCollection = processSearcher.Get();

            try
            {
                Process proc = Process.GetProcessById(pid);
                if (!proc.HasExited) proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }

            if (processCollection != null)
            {
                foreach (ManagementObject mo in processCollection)
                {
                    killProcessAndChildrens(Convert.ToInt32(mo["ProcessID"])); //kill child processes(also kills childrens of childrens etc.)
                }
            }
        }

    }
}
