using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace AvailabilityChecker
{
    public class AvailabilityChecker
    {

        public const int INTERVAL_MINUTES = 5;
        private const String MQTT_HOST = "MQTT BROKER IP";
        private const String MQTT_TOPIC = "TOPIC";
        private const String MQTT_USER = "BROKER USERNAME";
        private const String MQTT_PASSWORD = "BROKER PASSWORD";
        private static readonly String LOG_PATH = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AvailabilityChecker\\logs\\";

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetDesktopWindow();


        static NotifyIcon notifyIcon;
        static IntPtr processHandle;
        static IntPtr WinShell;
        static IntPtr WinDesktop;
        static MenuItem Logs;
        static MenuItem ToogleAvailability;
        static MenuItem SetAvailable;
        static MenuItem SetAway;


        static void Main()
        {
            notifyIcon = new NotifyIcon
            {
                Icon = Properties.Resources.trayicon,
                Text = "Availability Checker",
                Visible = true
            };

            ContextMenu menu = new ContextMenu();

            Logs = new MenuItem("Open Logs", new EventHandler(OpenLogDirectory));
            ToogleAvailability = new MenuItem("Toggle Availability");
            SetAvailable = new MenuItem("Change to Available", new EventHandler(PublishAvailable));
            SetAway = new MenuItem("Change to in Meeting", new EventHandler(PublishAway));

            ToogleAvailability.MenuItems.Add(SetAvailable);
            ToogleAvailability.MenuItems.Add(SetAway);
            menu.MenuItems.Add(Logs);
            menu.MenuItems.Add(ToogleAvailability);
            menu.MenuItems.Add(new MenuItem("Exit", new EventHandler(CleanExit)));

            notifyIcon.ContextMenu = menu;

            Task.Factory.StartNew(Run);

            processHandle = Process.GetCurrentProcess().MainWindowHandle;

            WinShell = GetShellWindow();

            WinDesktop = GetDesktopWindow();

            HideWindow(false);

            Application.Run();
        }

        static void Run()
        {
            var client = new MqttClient(MQTT_HOST);

            // Write console to file
            Directory.CreateDirectory(Path.GetDirectoryName(LOG_PATH));
            FileStream fs = new FileStream(LOG_PATH + "usage_log-" + DateTime.Now.ToString("dd-MM-yyyy-HH") + ".log", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            StreamWriter sw = new StreamWriter(fs)
            {
                AutoFlush = true
            };
            Console.SetOut(sw);

            var autoEvent = new AutoResetEvent(false);
            var statusChecker = new MicStatusChecker(client, MQTT_TOPIC, MQTT_USER, MQTT_PASSWORD);
            Console.WriteLine("{0:h:mm:ss.fff} Creating timer.\n",
                          DateTime.Now);
            var stateTimer = new System.Threading.Timer(statusChecker.CheckMicStatus,
                                      autoEvent, TimeSpan.Zero, TimeSpan.FromMinutes(INTERVAL_MINUTES));

            autoEvent.WaitOne();
            stateTimer.Dispose();
            Console.WriteLine("{0:h:mm:ss.fff} Destroying timer.",
                          DateTime.Now);
        }


        private static void CleanExit(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            Application.Exit();
            Environment.Exit(1);
        }


        static void OpenLogDirectory(object sender, EventArgs e)
        {
            Process.Start(LOG_PATH);
        }

        static void PublishAvailable(object sender, EventArgs e)
        {
            var client = new MqttClient(MQTT_HOST);
            string clientId = Guid.NewGuid().ToString();
            client.Connect(clientId, MQTT_USER, MQTT_PASSWORD);
            client.Publish(MQTT_TOPIC, Encoding.UTF8.GetBytes("0"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
            Console.WriteLine("{0:h:mm:ss.fff} Manual status change: ✓ - Available\n",
                          DateTime.Now);
        }

        static void PublishAway(object sender, EventArgs e)
        {
            var client = new MqttClient(MQTT_HOST);
            string clientId = Guid.NewGuid().ToString();
            client.Connect(clientId, MQTT_USER, MQTT_PASSWORD);
            client.Publish(MQTT_TOPIC, Encoding.UTF8.GetBytes("1"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
            Console.WriteLine("{0:h:mm:ss.fff} Manual status change: ✗ - NOT Available\n",
                          DateTime.Now);
        }

        static void HideWindow(bool Restore = true)
        {
            if (Restore)
            {
                SetParent(processHandle, WinDesktop);
            }
            else
            {
                SetParent(processHandle, WinShell);
            }
        }
    }
}