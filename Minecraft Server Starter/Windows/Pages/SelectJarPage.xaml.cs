/// <copyright file="Minecraft_Server_Starter" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Page used to select a Minecraft server jar, by either downloading it or copying it from disk</summary>

using Microsoft.Win32;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Minecraft_Server_Starter
{
    public delegate void NameUpdateRequestEventHandler(string newName);
    public delegate void ListUpdatedEventHandler(Latest latest);

    public partial class SelectJarPage : UserControl
    {
        public event NameUpdateRequestEventHandler NameUpdateRequest;
        void onNameUpdateRequest(string name) => NameUpdateRequest?.Invoke(name);

        public event ListUpdatedEventHandler ListUpdated;
        void onListUpdatedEventHandler(Latest latest) => ListUpdated?.Invoke(latest);

        #region Public properties

        public MinecraftVersions MinecraftVersions { get; private set; }

        #endregion
        
        public SelectJarPage()
        {
            InitializeComponent();
        }

        async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            MinecraftVersions = await MinecraftVersions.GetLatests();
            reloadVersions();

            onListUpdatedEventHandler(MinecraftVersions.Latest);
        }

        /// <summary>
        /// Downloads the selected version and saves it to the desired 
        /// location or copies the selected .jar to the desired location
        /// </summary>
        /// <returns>True if the operation was successful, false if it was not, null if cancelled</returns>
        public async Task<bool?> GetSelectedJar(
            string serverJar, IProgress<DownloadProgressChangedEventArgs> progress, CancellationTokenSource cts)
        {
            try
            {
                if (jarAutoSelection.IsChecked.Value)
                {
                    MinecraftVersion version;
                    if (versionList.SelectedIndex < 0)
                        version = new MinecraftVersion { ID = versionList.Text };
                    else
                        version = (MinecraftVersion)versionList.SelectedItem;
                    
                    if (!await version.CopyServer(serverJar, progress, cts))
                    {
                        if (cts.IsCancellationRequested)
                            return null; // operation cancelled

                        return false;
                    }
                }
                else
                {
                    MinecraftVersion.CopyServer(jarLoc.Text, serverJar);
                }

                return true;
            }
            catch (OperationCanceledException) { return false; }
        }

        public void ShowNoVersionAvailable()
        {
            MessageBox.Show("This version doesn't have a server available. Please try with another version, or download it manually",
                "Version not found", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #region Events

        void onlyReleasesChanged(object sender, RoutedEventArgs e)
        {
            if (MinecraftVersions != null)
                reloadVersions(onlyReleasesCheckBox.IsChecked.Value);
        }

        // search a jar
        void searchJarClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = Res.GetStr("sJarFileFilter") };
            if (ofd.ShowDialog() ?? false)
                jarLoc.Text = ofd.FileName;
        }
        
        void jarLocChanged(object sender, TextChangedEventArgs e)
        {
            jarCustomSelection.IsChecked = File.Exists(jarLoc.Text);
            if (jarCustomSelection.IsChecked ?? false)
                onNameUpdateRequest(Path.GetFileName(Path.GetDirectoryName(jarLoc.Text)));

            jarAutoSelection.IsChecked = !jarCustomSelection.IsChecked;
        }

        #endregion
        
        #region Versions

        void reloadVersions(bool releasesOnly = true)
        {
            versionList.Items.Clear();

            foreach (var version in MinecraftVersions.Versions)
                if (!string.IsNullOrEmpty(version.ServerUrl) && // it must have a server url
                    (!releasesOnly || version.Type == "release")) // and then satisfy this condition
                {
                    versionList.Items.Add(version);
                }

            if (versionList.Items.Count > 0)
                versionList.SelectedIndex = 0;
        }

        #endregion

        void customJarChecked(object sender, RoutedEventArgs e)
        {
            if (jarAutoSelection != null)
                jarAutoSelection.IsChecked = false;
        }

        void autoJarChecked(object sender, RoutedEventArgs e)
        {
            if (jarCustomSelection != null)
                jarCustomSelection.IsChecked = false;
        }
    }
}
