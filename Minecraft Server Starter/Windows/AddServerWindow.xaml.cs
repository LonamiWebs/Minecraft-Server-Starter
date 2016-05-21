/// <copyright file="AddServerWindow.xaml.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Window used to add a new server</summary>

using Microsoft.Win32;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Runtime.InteropServices;
using System;
using System.Windows.Interop;
using System.Threading;
using ExtensionMethods;
using System.Windows.Media.Imaging;

namespace Minecraft_Server_Starter
{
    public partial class AddServerWindow : Window
    {
        #region Public properties

        public Server Result { get; set; }

        #endregion

        #region Private fields

        // if the user wants to use any existing world
        string worldPath;

        // cancellation token if downloading a server
        CancellationTokenSource downloadCts = new CancellationTokenSource();

        bool autoMotdUpdating; // determines whether the MOTD is automatically being updated by a name change
        bool motdWasHandUpdated; // determines whether the MOTD has been changed by hand, i.e. not by a name change

        #endregion

        #region Constructor and loading

        public AddServerWindow(string worldPath = null)
        {
            InitializeComponent();

            this.worldPath = worldPath;
            if (!string.IsNullOrEmpty(worldPath))
                name.Text = Path.GetFileNameWithoutExtension(worldPath);
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
            name.Focus();
        }

        #endregion

        #region Others

        // nop, no show, only ShowDialog allowed!
        new void Show() { }

        // hide close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        #endregion

        #region Events

        void nameChanged(object sender, TextChangedEventArgs e)
        {
            checkEnableAccept();

            // if the motd has never been updated by hand
            if (!motdWasHandUpdated)
            {
                // notify we're auto updating and update
                autoMotdUpdating = true;
                motdBox.SetMOTD(name.Text);
                autoMotdUpdating = false;
            }
        }


        void motdChanged(object sender, TextChangedEventArgs e)
        {
            // if we're not auto updating, it's a hand change
            if (!autoMotdUpdating)
                motdWasHandUpdated = true;
        }

        void checkEnableAccept() => accept.IsEnabled = !string.IsNullOrWhiteSpace(name.Text);

        #endregion

        #region Accept and cancel

        async void acceptClick(object sender, RoutedEventArgs e)
        {
            accept.IsEnabled = false;

            Result = new Server(name.Text, description.Text);

            var progress = new Progress<DownloadProgressChangedEventArgs>();
            progress.ProgressChanged += (s, de) =>
                Title = $"Descargando... {de.ProgressPercentage}%";

            var result = await selectJarPage.GetSelectedJar(Result.ServerJar, progress, downloadCts);
            if (!result.HasValue) // operation cancelled
                return;

            if (result.Value) // success
            {
                if (!string.IsNullOrEmpty(worldPath))
                {
                    if (Directory.Exists(worldPath))
                    {
                        new DirectoryInfo(worldPath).CopyDirectory(
                            new DirectoryInfo(Path.Combine(Result.Location, ServerProperties.Empty["level-name"].Value)));
                    }
                    else if (File.Exists(worldPath) && worldPath.EndsWith(".zip"))
                    {
                        Result.UpdateServersProperty("level-name", MinecraftWorld.ExtractWorldZip(worldPath, Result.Location));
                    }
                }

                // set properties
                if (serverIcon.Source != null)
                    ((BitmapSource)serverIcon.Source).Save(Result.IconPath);

                Result.UpdateServersProperty("difficulty", difficultyComboBox.SelectedIndex.ToString());
                Result.UpdateServersProperty("gamemode", gameModeComboBox.SelectedIndex.ToString());
                Result.UpdateServersProperty("max-players", maxPlayersTextBox.Text);

                Result.UpdateServersProperty("hardcore", hardcoreCheckBox.IsChecked.Value ? "true" : "false");
                Result.UpdateServersProperty("online-mode", onlineModeCheckBox.IsChecked.Value ? "true" : "false");
                Result.UpdateServersProperty("enable-command-block", enableCommandBlockCheckBox.IsChecked.Value ? "true" : "false");

                Result.UpdateServersProperty("motd", motdBox.GeneratedCode);

                // finish
                DialogResult = true;
                Close();
            }
            else // failed
            {
                selectJarPage.ShowNoVersionAvailable();
                accept.IsEnabled = true;
            }
        }

        void cancelClick(object sender, RoutedEventArgs e)
        {
            downloadCts.Cancel();
            DialogResult = false;
            Close();
        }

        #endregion

        #region Icon

        string iconPath;
        void changeIcon_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = Res.GetStr("sImageFilter") };
            if (ofd.ShowDialog() ?? false)
            {
                iconPath = ofd.FileName;
                serverIcon.Source = new Uri(ofd.FileName).LoadImage(64, 64);
            }
        }

        #endregion

        void selectJarPageNameUpdateRequest(string newName)
        {
            if (string.IsNullOrWhiteSpace(name.Text))
                name.Text = newName;
        }
    }
}
