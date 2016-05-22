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

            currentVersion1Run.Text = Res.GetStr("currentVersionColon");

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
                canUpgradeBlock.Text = Res.GetStr("higherVersionAvailable");

            else if (snapshotComparision > 0)
                canUpgradeBlock.Text = Res.GetStr("higherSnapshotAvailable");

            else
                canUpgradeBlock.Text = Res.GetStr("noHigherAvailable");
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
                Title = Res.GetStr("downloadingPercentage", de.ProgressPercentage);

            var result = await selectJarPage.GetSelectedJarToLocalFile(server.ServerJar, progress, downloadCts);
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
