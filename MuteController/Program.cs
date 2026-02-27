using System;
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

            setupMuteController();

            Application.Run();
        }
        
        static void setupMuteController()
        {
            // Helper Functions
            static bool checkWifiConnection() // return true if the system is connected to university or office wifi
            {
                String[] needToMuteWifis = { "QM-visiter", "eduroam" }; // list of wifi names in your university or office
                var wifiProfile = NetworkInformation.GetInternetConnectionProfile();

                if (wifiProfile != null && wifiProfile.IsWlanConnectionProfile)
                {
                    var ssid = wifiProfile.WlanConnectionProfileDetails.GetConnectedSsid();
                    return needToMuteWifis.Contains(ssid);
                }

                return false;
            }
            // ############################################### //

            // Core Evaluate Functions
            static void EvaluateAudioRules()
            {
                MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
                MMDevice currentDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

                bool isHeadphonesConnected = currentDevice.FriendlyName.Contains("Sidharth");
                bool needToMute = checkWifiConnection();

                /*    Console.WriteLine(
                        $"Headphone: {currentDevice.FriendlyName}\n" +
                        $"Headphone-Status: {isHeadphonesConnected}\n" +
                        $"Mute Based on Wifi: {needToMute}"
                        );*/

                if (isHeadphonesConnected)
                {
                    currentDevice.AudioEndpointVolume.Mute = false;
                    return;
                }
                else
                {
                    currentDevice.AudioEndpointVolume.Mute = needToMute;
                }
            }
            // ############################################### //

            // Watchers to trigger Mute Status Evaluation
            string aqsFilter = "(System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\")";
            DeviceWatcher watcher = DeviceInformation.CreateWatcher(aqsFilter, null, DeviceInformationKind.AssociationEndpoint);

            NetworkInformation.NetworkStatusChanged += (sender) =>
            {
                EvaluateAudioRules();
            };

            watcher.Added += (sender, deviceInfo) =>
            {
                EvaluateAudioRules();
            };

            watcher.Removed += (sender, deviceInfoUpdate) =>
            {
                EvaluateAudioRules();
            };

            watcher.Start();
            // ############################################### //
        }
    }
}