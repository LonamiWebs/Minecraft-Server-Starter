/// <copyright file="SettingsWindow.xaml.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Window used to configure various settings</summary>

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.ComponentModel;
using Microsoft.VisualBasic.Devices;

namespace Minecraft_Server_Starter
{
    public partial class SettingsWindow : Window
    {
        #region Fields

        const float ramSafeLimit = 0.70f; // 75% of total ram
        const int baseRam = 256;
        bool loaded;

        #endregion

        #region Constructor

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        #endregion

        #region Load settings

        void LoadSettings()
        {
            loaded = false;

            minSelectedRam = Settings.GetValue<int>("minRam");
            maxSelectedRam = Settings.GetValue<int>("maxRam");

            javaPath.Text = Settings.GetValue<string>("javaPath").ToString();

            priority.SelectedIndex = PriorityToIdx((ProcessPriorityClass)Settings.GetValue<int>("priority"));

            notifEnabled.IsChecked = notifLocation.IsEnabled = tryNotification.IsEnabled = Settings.GetValue<bool>("notificationEnabled");
            notifLocation.SelectedIndex = LocationToIdx((Toast.Location)Settings.GetValue<int>("notificationLoc"));

            ignoreCommandsBlockCheckBox.IsChecked = Settings.GetValue<bool>("ignoreCommandsBlock");

            loaded = true;
        }

        #endregion

        #region Set settings

        // server min ram
        void MinRamChanged(object sender, TextChangedEventArgs e)
        {
            if (!loaded) return;
            Settings.SetValue("minRam", minSelectedRam);
            if (maxSelectedRam < minSelectedRam)
                maxSelectedRam = minSelectedRam;
        }

        // server max ram
        void MaxRamChanged(object sender, TextChangedEventArgs e)
        {
            if (!loaded) return;
            Settings.SetValue("maxRam", maxSelectedRam);
            if (minSelectedRam > maxSelectedRam)
                minSelectedRam = maxSelectedRam;
        }

        // java path
        void JavaPathChanged(object sender, TextChangedEventArgs e)
        {
            if (!loaded) return;
            Settings.SetValue("javaPath", javaPath.Text);
        }

        // server priority
        void PriorityChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!loaded) return;
            Settings.SetValue("priority", (int)IdxToPriority(priority.SelectedIndex));
        }

        // show notifications?
        void NotifEnabledChanged(object sender, RoutedEventArgs e)
        {
            if (!loaded) return;

            Settings.SetValue("notificationEnabled", notifEnabled.IsChecked ?? false);
            notifLocation.IsEnabled = tryNotification.IsEnabled = notifEnabled.IsChecked ?? false;
        }

        // notification location
        void NotifLocationChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!loaded) return;
            Settings.SetValue("notificationLoc", (int)IdxToLocation(notifLocation.SelectedIndex));
        }

        // ignore commands block
        void ignoreCommandsBlockCheck(object sender, RoutedEventArgs e)
        {
            Settings.SetValue("ignoreCommandsBlock", ignoreCommandsBlockCheckBox.IsChecked.Value);
        }

        #endregion

        #region Converters

        // priority
        static ProcessPriorityClass IdxToPriority(int idx)
        {
            switch (idx)
            {
                case 0: return ProcessPriorityClass.High;
                case 1: return ProcessPriorityClass.AboveNormal;
                default:
                case 2: return ProcessPriorityClass.Normal;
                case 3: return ProcessPriorityClass.BelowNormal;
                case 4: return ProcessPriorityClass.Idle;
            }
        }

        static int PriorityToIdx(ProcessPriorityClass pri)
        {
            switch (pri)
            {
                case ProcessPriorityClass.High: return 0;
                case ProcessPriorityClass.AboveNormal: return 1;
                default:
                case ProcessPriorityClass.Normal: return 2;
                case ProcessPriorityClass.BelowNormal: return 3;
                case ProcessPriorityClass.Idle: return 4;
            }
        }

        // notification location
        static Toast.Location IdxToLocation(int idx)
        {
            switch (idx)
            {
                default:
                case 0: return Toast.Location.TopLeft;
                case 1: return Toast.Location.TopRight;
                case 2: return Toast.Location.BottomLeft;
                case 3: return Toast.Location.BottomRight;
                case 4: return Toast.Location.TopMiddle;
                case 5: return Toast.Location.BottomMiddle;
                case 6: return Toast.Location.RightCenter;
                case 7: return Toast.Location.LeftCenter;
            }
        }
        static int LocationToIdx(Toast.Location loc)
        {
            switch (loc)
            {
                default:
                case Toast.Location.TopLeft: return 0;
                case Toast.Location.TopRight: return 1;
                case Toast.Location.BottomLeft: return 2;
                case Toast.Location.BottomRight: return 3;
                case Toast.Location.TopMiddle: return 4;
                case Toast.Location.BottomMiddle: return 5;
                case Toast.Location.RightCenter: return 6;
                case Toast.Location.LeftCenter: return 7;
            }
        }

        #endregion

        #region Helpers

        // this helps by giving an example
        void TryNotificationClick(object sender, RoutedEventArgs e)
        {
            Toast.makeText(Res.GetStr("descRconPassword"), Res.GetStr("sTestNotif"));
        }

        // this helps by letting you search the java path
        void JavaSearchClick(object sender, RoutedEventArgs e)
        {
            var javaFolder = Path.Combine(Environment.GetFolderPath
                (Environment.SpecialFolder.ProgramFiles), "Java");

            var ofd = new OpenFileDialog { Filter = "Java|java.exe" };

            if (Directory.Exists(javaFolder))
                ofd.InitialDirectory = javaFolder;

            if (ofd.ShowDialog() ?? false)
                javaPath.Text = ofd.FileName;
        }

        #endregion

        #region RAM management

        // get or set the min ram by using minRam textbox
        int minSelectedRam
        {
            get
            {
                int value;
                return int.TryParse(minRam.Text, out value) ? value : baseRam;
            }
            set
            {
                if (value >= baseRam)
                    minRam.Text = value.ToString();
            }
        }

        // get or set the max ram by using maxRam textbox
        int maxSelectedRam
        {
            get
            {
                int value;
                return int.TryParse(maxRam.Text, out value) ? value : baseRam;
            }
            set
            {
                if (value >= baseRam)
                    maxRam.Text = value.ToString();
            }
        }

        // readjust the ram so it's a multiple of baseRam
        void adjustRamToBeMultiple()
        {
            if (minSelectedRam % baseRam != 0)
            {
                minSelectedRam = minSelectedRam - minSelectedRam % baseRam;
                if (minSelectedRam < baseRam)
                    minSelectedRam = baseRam;

                Settings.SetValue("minRam", minSelectedRam);
            }

            if (maxSelectedRam % baseRam != 0)
            {
                maxSelectedRam = maxSelectedRam - maxSelectedRam % baseRam;
                if (maxSelectedRam < baseRam)
                    maxSelectedRam = baseRam;

                Settings.SetValue("maxRam", maxSelectedRam);
            }
        }
        // readjust the ram so it's safe
        void adjustRamToBeSafe()
        {
            int totalRamSafeLimit = getRamSafeLimit();

            if (minSelectedRam > totalRamSafeLimit)
            {
                minSelectedRam = totalRamSafeLimit;
                Settings.SetValue("minRam", minSelectedRam);
            }

            if (maxSelectedRam > totalRamSafeLimit)
            {
                maxSelectedRam = totalRamSafeLimit;
                Settings.SetValue("maxRam", maxSelectedRam);
            }

            if (minSelectedRam > maxSelectedRam)
            {
                minSelectedRam = maxSelectedRam;
                Settings.SetValue("minRam", minSelectedRam);
            }

            adjustRamToBeMultiple();
        }
        // which is the safe limit for the RAM?
        int getRamSafeLimit()
        {
            uint totalRam = (uint)(new ComputerInfo().TotalPhysicalMemory / (1024 * 1024));
            return (int)(totalRam * ramSafeLimit); // won't give negative values
        }

        #endregion

        #region Closing warnings

        void closing(object sender, CancelEventArgs e)
        {
            int totalRamSafeLimit = getRamSafeLimit();
            if (minSelectedRam > totalRamSafeLimit || maxSelectedRam > totalRamSafeLimit)
            {
                switch (MessageBox.Show(Res.GetStr("smcHighRAM", ramSafeLimit.ToString("0.##%")),
                    Res.GetStr("smtHighRAM"), MessageBoxButton.YesNoCancel, MessageBoxImage.Information))
                {
                    case MessageBoxResult.Yes:
                        adjustRamToBeSafe();
                        break;
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }

            if (minSelectedRam % baseRam != 0 || maxSelectedRam % baseRam != 0)
            {
                switch (MessageBox.Show(Res.GetStr("smcNotMultiple", baseRam),
                    Res.GetStr("smtNotMultiple"), MessageBoxButton.YesNoCancel, MessageBoxImage.Information))
                {
                    case MessageBoxResult.Yes:
                        adjustRamToBeMultiple();
                        break;
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
        }

        #endregion

        #region Reset and accept

        void uninstallClick(object sender, RoutedEventArgs e)
        {
            var folder = Settings.GetValue<string>("mssFolder");
            if (MessageBox.Show(string.Format("Uninstalling Minecraft Server Starter will delete EVERYTHING (servers, backups, cache) from your disk and the application will be closed. Are you sure you want to continue? Please make sure you don't have any personal files under {0}", folder), "Uninstall?",
                MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {

                if (!Directory.Exists(folder))
                {
                    Application.Current.Shutdown();
                    return;
                }

                switch (MessageBox.Show("Please make sure you don't have any personal files. Do you wish to open the folder now instead uninstalling everything?", "Think it twice", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning))
                {
                    case MessageBoxResult.Yes:
                        Process.Start(folder);
                        break;

                    case MessageBoxResult.No:
                        bool success = true;
                        using (WaitCursor.New)
                        {
                            foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
                                try { File.Delete(file); } catch { success = false; }

                            try { Directory.Delete(folder, true); } catch { success = false; }
                        }

                        MessageBox.Show("Minecraft Server Starter has been uninstalled from your computer.",
                            success ? "Uninstall success" : "Uninstall incomplete", MessageBoxButton.OK, MessageBoxImage.Information);

                        Application.Current.Shutdown();
                        break;

                    case MessageBoxResult.Cancel:
                        return;
                }
            }
        }

        void resetSettingsClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Res.GetStr("smcResetSettings"), Res.GetStr("smtResetSettings"),
                MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var eulaWasAccepted = Settings.GetValue<bool>("eulaAccepted");
                Settings.Reset();
                Settings.SetValue("eulaAccepted", eulaWasAccepted); // this is more likely always be true if we're here
                LoadSettings();

                MessageBox.Show(Res.GetStr("smcSettingsReset"), Res.GetStr("smtSettingsReset"),
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        void acceptClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        void clearCacheClick(object sender, RoutedEventArgs e)
        {

        }
    }
}
