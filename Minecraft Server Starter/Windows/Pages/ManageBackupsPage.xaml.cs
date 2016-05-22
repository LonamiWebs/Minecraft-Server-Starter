/// <copyright file="Minecraft_Server_Starter" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Page used to manage the existing backups</summary>

using System.Windows;
using System.Windows.Controls;

namespace Minecraft_Server_Starter
{
    public partial class ManageBackupsPage : UserControl
    {
        #region Private fields

        static string BackupSizeInfo => Res.GetStr("fileSizeAndContents");

        Server server;

        #endregion

        #region Constructor

        public ManageBackupsPage(Server server)
        {
            InitializeComponent();

            this.server = server;
            reloadBackupsList();
        }

        #endregion

        #region Get and load backup

        public Backup GetBackup()
        {
            return new Backup(null,
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

        #region Backups list

        void reloadBackupsList()
        {
            backupsList.Items.Clear();

            foreach (var backup in Backup.GetBackups(server.Name))
                backupsList.Items.Add(backup);
        }

        void backupsListChanged(object sender, SelectionChangedEventArgs e)
        {
            if (backupsList.SelectedIndex < 0)
            {
                info.Text = string.Empty;
                delete.IsEnabled = false;
            }
            else
            {
                LoadBackup((Backup)backupsList.SelectedItem);
                info.Text = string.Format(BackupSizeInfo, ((Backup)backupsList.SelectedItem).Size);
                delete.IsEnabled = true;
            }
        }

        #endregion

        #region Button click

        void deleteClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Res.GetStr("sureDeleteBackup"), Res.GetStr("confirmDelete"),
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (((Backup)backupsList.SelectedItem).Delete())
                    MessageBox.Show(Res.GetStr("backupDeletedContent"), Res.GetStr("backupDeletedTitle"),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show(Res.GetStr("backupDeleteError"), Res.GetStr("error"),
                        MessageBoxButton.OK, MessageBoxImage.Error);

                reloadBackupsList();
            }
        }

        #endregion
    }
}
