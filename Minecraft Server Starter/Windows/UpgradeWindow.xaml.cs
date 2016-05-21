/// <copyright file="UpgradeWindow.xaml.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>01/04/2016 21:23</date>
/// <summary>Window used to upgrade (or downgrade) a server</summary>

using System;
using System.Net;
using System.Threading;
using System.Windows;

namespace Minecraft_Server_Starter
{
    public partial class UpgradeWindow : Window
    {
        readonly Server server;

        MinecraftVersion current;
        Latest latest;

        public UpgradeWindow(Server server)
        {
            this.server = server;

            InitializeComponent();
        }

        async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            currentVersion2Run.Text = await ServerJar.GetServerVersion(server.ServerJar);
            current = new MinecraftVersion(currentVersion2Run.Text);

            currentVersion1Run.Text = "Current version:";

            CheckIfNewIsAvailable();
        }

        void SelectJarPage_ListUpdated(Latest latest)
        {
            this.latest = latest;

            CheckIfNewIsAvailable();
        }

        void CheckIfNewIsAvailable()
        {
            if (current == null || latest == null)
                return;

            var releaseComparision = MinecraftVersions.Compare(
                current.ID, latest.Release, selectJarPage.MinecraftVersions);

            var snapshotComparision = MinecraftVersions.Compare(
                current.ID, latest.Snapshot, selectJarPage.MinecraftVersions);

            if (releaseComparision > 0)
                canUpgradeBlock.Text = "There is a new higher version available. It is recommended to upgrade your server.";

            else if (snapshotComparision > 0)
                canUpgradeBlock.Text = "There is a new snapshot available. It is recommended to backup your worlds first if you wish to upgrade your server.";

            else
                canUpgradeBlock.Text = "You're up to date! If you want to downgrade your server, it is recommended to perform a backup first.";
        }

        void cancelClick(object sender, RoutedEventArgs e)
        {
            downloadCts.Cancel();
            Close();
        }

        async void upgradeClick(object sender, RoutedEventArgs e)
        {
            upgradeButton.IsEnabled = false;

            var progress = new Progress<DownloadProgressChangedEventArgs>();
            progress.ProgressChanged += (s, de) =>
                Title = $"Descargando... {de.ProgressPercentage}%";

            var result = await selectJarPage.GetSelectedJar(server.ServerJar, progress, downloadCts);
            if (!result.HasValue) // operation cancelled
                return;

            if (result.Value) // success
            {
                DialogResult = true;
                Close();
            }
            else // failed
            {
                selectJarPage.ShowNoVersionAvailable();
                upgradeButton.IsEnabled = true;
            }
        }

        // cancellation token if downloading a server
        CancellationTokenSource downloadCts = new CancellationTokenSource();
    }
}
