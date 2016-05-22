/// <copyright file="Minecraft_Server_Starter" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Page used to load an existing backup</summary>

using System.Windows;
using System.Windows.Controls;

namespace Minecraft_Server_Starter
{
    public partial class LoadBackupPage : UserControl
    {
        #region Fields

        Server server;

        #endregion

        #region Constructor

        public LoadBackupPage(Server server)
        {
            InitializeComponent();

            this.server = server;
            reloadBackupsList();
        }

        #endregion

        #region Get and load backup

        public Backup GetBackup()
        {
            return new Backup(server,
                bWorlds.IsChecked.Value, bServerProperties.IsChecked.Value, bWhiteList.IsChecked.Value,
                bOps.IsChecked.Value, bBanned.IsChecked.Value, bLogs.IsChecked.Value, bEverything.IsChecked.Value);
        }

        public void LoadBackup(Backup backup)
        {
            foreach (CheckBox checkBox in checkBoxes.Children)
                checkBox.IsEnabled = false;

            bWorlds.IsChecked = bWorlds.IsEnabled = backup.Worlds;
            bServerProperties.IsChecked = bServerProperties.IsEnabled = backup.ServerProperties;
            bWhiteList.IsChecked = bWhiteList.IsEnabled = backup.WhiteList;
            bOps.IsChecked = bOps.IsEnabled = backup.Ops;
            bBanned.IsChecked = bBanned.IsEnabled = backup.Banned;
            bLogs.IsChecked = bLogs.IsEnabled = backup.Logs;
            bEverything.IsChecked = bEverything.IsEnabled = backup.Everything;
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

        #region Backups list

        void reloadBackupsList()
        {
            backupsList.Items.Clear();

            foreach (var backup in Backup.GetBackups(server.Name))
                backupsList.Items.Add(backup);
        }

        void backupsListChanged(object sender, SelectionChangedEventArgs e)
        {
            loadBackup.IsEnabled = backupsList.SelectedIndex >= 0;
            if (loadBackup.IsEnabled)
                LoadBackup((Backup)backupsList.SelectedItem);
        }

        #endregion

        #region Button click

        void loadBackupClick(object sender, RoutedEventArgs e)
        {
            var selectedOptions = GetBackup();
            ((Backup)backupsList.SelectedItem).Load(server,
                selectedOptions.Worlds, selectedOptions.ServerProperties,
                selectedOptions.WhiteList, selectedOptions.Ops, selectedOptions.Banned,
                selectedOptions.Logs, selectedOptions.Everything);

            MessageBox.Show(Res.GetStr("backupLoadedContent"), Res.GetStr("success"),
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}
