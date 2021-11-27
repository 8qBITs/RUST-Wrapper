using System;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Linq;
using System.Configuration;

namespace RUST_server_wrapper
{
    class Program
    {
        static Process serverProcess;

        static Boolean stopGameServer = false;
        static Boolean work = true;
        static Boolean stopScheduler = false;
        static Boolean stopConsole = false;

        static String last_msg = "";

        static void Main(string[] args)
        {
            Console.Title = "RUST Dedicated Wrapper";
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(@"
 __      __                               ____ 
/  \    /  \____________  ______   ___  _/_   |
\   \/\/   /\_  __ \__  \ \____ \  \  \/ /|   |
 \        /  |  | \// __ \|  |_> >  \   / |   |
  \__/\  /   |__|  (____  /   __/    \_/  |___|
       \/               \/|__|                 
");
            Console.ForegroundColor = ConsoleColor.White;

            if (!Directory.Exists("C:/ServerWrapper"))
            {
                Directory.CreateDirectory(@"C:/ServerWrapper");
                utils.wget("https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip", "C:/ServerWrapper/steamcmd.zip");
                utils.unzip("C:/ServerWrapper/steamcmd.zip", "C:/ServerWrapper");
            }

            checkForUpdate().WaitForExit();
            Thread server = new Thread(new ThreadStart(startServer));
            server.Start();

            Thread scheduler = new Thread(new ThreadStart(serverRestartLoop));
            scheduler.Start();

            console();
        }

        private static void console()
        {
            utils.info("Console listener has started.");

            while (!stopConsole)
            {
                switch (Console.ReadLine())
                {
                    case "stop":
                        terminate();
                        break;
                    case "reboot":
                        rebootServer();
                        break;
                    case "startup":
                        utils.runOnStartup();
                        break;
                    case "nostartup":
                        utils.removeRunOnStartup();
                        break;
                    default:
                        sendToServer(Console.ReadLine().ToString());
                        break;
                }
            }
        }

        private static void sendToServer(String cmd)
        {
            serverProcess.StandardInput.WriteLine(cmd);
        }

        private static void rebootServer()
        {
            utils.warn($"RUST Dedicated Server is restarting, time is: {DateTime.Now.ToString("HH:mm")}");
            stopGameServer = true;
            Thread.Sleep(4000);

            checkForUpdate().WaitForExit();

            stopGameServer = false;
            Thread server = new Thread(new ThreadStart(startServer));
            server.Start();
        }

        private static void terminate()
        {
            utils.error("Program terminating!");
            Thread.Sleep(1000);
            stopGameServer = true;
            stopScheduler = true;
            stopConsole = true;

            Environment.Exit(0);
            //Environment.FailFast("Exited.");
        }

        private static void serverRestartLoop()
        {
            utils.info("Restart scheduler started!");

            while (!stopScheduler)
            {
                String[] times = { "01.00", "05.00", "09.00", "13.00", "17.00", "21.00" };
                var current_time = DateTime.Now.ToString("HH:mm");

                if (times.Contains(current_time)) {
                    rebootServer();
                }

                //utils.info($"Current time is: {current_time}");
            }

            return;
        }

        private static Process checkForUpdate()
        {
            Directory.SetCurrentDirectory("C:/ServerWrapper");

            Process p = new Process();

            utils.info("Checking for RUST Dedicated Server updates..");

            p.StartInfo.FileName = "C:/ServerWrapper/steamcmd.exe";
            p.StartInfo.Arguments = "+force_install_dir server +login anonymous +app_update 258550 validate +quit";
            p.Start();

            return p;
        }

        private static void startServer()
        {
            Directory.SetCurrentDirectory("C:/ServerWrapper/server/");
            Process p = new Process();

            utils.info("Starting RUST Dedicated Server..");

            String args = $"-batchmode -nographics +server.port {ConfigurationManager.AppSettings.Get("server_port")} +rcon.port {ConfigurationManager.AppSettings.Get("rcon_port")} +rcon.password \"{ConfigurationManager.AppSettings.Get("rcon_pass")}\" +app.port {ConfigurationManager.AppSettings.Get("app_port")} +server.identity \"rust_server\" +server.maxplayers 120 +server.hostname \"{ConfigurationManager.AppSettings.Get("hostname")}\" +server.level \"Procedural Map\" +server.tickrate 10 +server.seed {ConfigurationManager.AppSettings.Get("seed")} +server.worldsize {ConfigurationManager.AppSettings.Get("world_size")} +server.saveinterval 300 +server.globalchat true +server.secure true +server.official true +server.description \"{ConfigurationManager.AppSettings.Get("description")}\"";
            String server = "C:/ServerWrapper/server/RustDedicated.exe";

            serverProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = server,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            serverProcess.Start();

            while (!serverProcess.StandardOutput.EndOfStream && work)
            {

                if (serverProcess.HasExited)
                {
                    utils.error("RUST Dedicated Server CRASHED rebooting!");
                    work = false;
                    rebootServer();
                }

                if (stopGameServer)
                {
                    serverProcess.Close();
                    serverProcess.Kill();
                    utils.warn("Stopping RUST Dedicated Server!");
                    work = false;
                    return;
                }

                var line = serverProcess.StandardOutput.ReadLine();

                if(!line.Contains("bindings.h"))
                {
                    if(!line.Equals(last_msg))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(line);

                        last_msg = line;
                    }

                }
            }
        }


    }
}
