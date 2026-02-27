using System.Windows.Forms;
using System.Drawing;
using NAudio.CoreAudioApi;
using Windows.Devices.Enumeration;
using Windows.Networking.Connectivity;

namespace MuteController
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            NotifyIcon trayIcon = new NotifyIcon()
            {
                Icon = new Icon("mute_controller.ico"),
                Visible = true,
                Text = "Mute Controller"
            };

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Exit", null, (sender, args) => {
                trayIcon.Visible = false;
                Application.Exit();
            });
            trayIcon.ContextMenuStrip = menu;

            SetupMuteController(trayIcon);

            Application.Run();
        }
        
        static void SetupMuteController(NotifyIcon trayIcon)
        {
            // Watchers to trigger Mute Status Evaluation
            string aqsFilter = "(System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\")";
            DeviceWatcher watcher = DeviceInformation.CreateWatcher(aqsFilter, null, DeviceInformationKind.AssociationEndpoint);

            NetworkInformation.NetworkStatusChanged += (sender) =>
            {
                EvaluateAudioRules(trayIcon);
            };

            watcher.Added += (sender, deviceInfo) =>
            {
                EvaluateAudioRules(trayIcon);
            };

            watcher.Removed += (sender, deviceInfoUpdate) =>
            {
                EvaluateAudioRules(trayIcon);
            };

            watcher.Start();
            // ############################################### //
        }

        // Helper Functions
        static bool CheckWifiConnection() // return true if the system is connected to university or office wifi
        {
            String[] needToMuteWifis = { "qm-visitor", "eduroam" }; // list of wifi names in your university or office
            var wifiProfile = NetworkInformation.GetInternetConnectionProfile();

            if (wifiProfile != null && wifiProfile.IsWlanConnectionProfile)
            {
                var ssid = wifiProfile.WlanConnectionProfileDetails.GetConnectedSsid().ToLower();
                return needToMuteWifis.Contains(ssid);
            }

            return false;
        }

        // Core Evaluate Functions
        static void EvaluateAudioRules(NotifyIcon trayIcon)
        {
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            MMDevice currentDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            bool isHeadphonesConnected = currentDevice.FriendlyName.Contains("Sidharth");
            bool needToMute = CheckWifiConnection();

            /*    Console.WriteLine(
                    $"Headphone: {currentDevice.FriendlyName}\n" +
                    $"Headphone-Status: {isHeadphonesConnected}\n" +
                    $"Mute Based on Wifi: {needToMute}"
                    );*/

            if (isHeadphonesConnected)
            {
                currentDevice.AudioEndpointVolume.Mute = false;
                trayIcon.ShowBalloonTip(3000, "Bluetooth Connection Detected", "Unmuting Audio", ToolTipIcon.Info);
                return;
            }
            else
            {
                currentDevice.AudioEndpointVolume.Mute = needToMute;
                if (needToMute)
                {
                    trayIcon.ShowBalloonTip(3000, "MuteGuard", "Muting Device", ToolTipIcon.Info);
                }
                else
                {
                    trayIcon.ShowBalloonTip(3000, "MuteGuard", "Unmuting Device", ToolTipIcon.Info);
                }
            }
        }
    }
}