/// <copyright file="Minecraft_Server_Starter" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Page used to generate a new backup</summary>

using System.Windows;
using System.Windows.Controls;

namespace Minecraft_Server_Starter
{
    public partial class GenerateBackupPage : UserControl
    {
        #region Private fields

        bool isServerOpen;
        Server server;

        #endregion

        #region Constructor

        public GenerateBackupPage(bool isServerOpen, Server server)
        {
            InitializeComponent();

            this.isServerOpen = isServerOpen;
            this.server = server;
        }

        #endregion

        #region Get and load backup

        public Backup GetBackup(Server server)
        {
            return new Backup(server,
                bWorlds.IsChecked.Value, bServerProperties.IsChecked.Value, bWhiteList.IsChecked.Value,
                bOps.IsChecked.Value, bBanned.IsChecked.Value, bLogs.IsChecked.Value, bEverything.IsChecked.Value);
        }

        public void LoadBackup(Backup backup)
        {
            bWorlds.IsChecked = backup.Worlds;
            bServerProperties.IsChecked = backup.ServerProperties;
            bWhiteList.IsChecked = backup.WhiteList;
            bOps.IsChecked = backup.Ops;
            bBanned.IsChecked = backup.Banned;
            bLogs.IsChecked = backup.Logs;
            bEverything.IsChecked = backup.Everything;
        }

        #endregion

        #region Button click

        async void generateBackup(object sender, RoutedEventArgs e)
        {
            generateBackupButton.IsEnabled = false;
            generateBackupButton.Content = "Generating backup...";

            if (await GetBackup(server).Save(isServerOpen))
            {
                MessageBox.Show("The backup has been generated successfully",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to save the backup", "Backup not saved",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            generateBackupButton.IsEnabled = true;
            generateBackupButton.Content = "Generate backup";
        }

        #endregion

        #region Checking

        // unchecking any
        void anyUnchecked(object sender, RoutedEventArgs e)
        {
            if (bEverything.IsChecked.Value)
                bEverything.IsChecked = false;
        }

        // checking everything
        void everythingChecked(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox cb in checkBoxes.Children)
                cb.IsChecked = true;
        }

        void everythingUnchecked(object sender, RoutedEventArgs e)
        {
            bool allChecked = true;
            foreach (CheckBox cb in checkBoxes.Children)
                if (!cb.IsChecked.Value && cb != bEverything)
                {
                    allChecked = false;
                    break;
                }

            if (allChecked)
                foreach (CheckBox cb in checkBoxes.Children)
                    cb.IsChecked = false;
        }

        #endregion
    }
}
