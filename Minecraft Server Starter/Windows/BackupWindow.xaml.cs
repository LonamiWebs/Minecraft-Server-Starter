/// <copyright file="BackupWindow.xaml.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Window used to backup a server</summary>

using System.Windows;

namespace Minecraft_Server_Starter
{
    public partial class BackupWindow : Window
    {
        #region Private fields

        bool isServerOpen;
        Server server;

        #endregion

        #region Constructor

        public BackupWindow(bool isServerOpen, Server server)
        {
            InitializeComponent();

            this.isServerOpen = isServerOpen;
            this.server = server;
        }

        #endregion

        #region Select option

        void generateBackupChecked(object sender, RoutedEventArgs e)
        {
            pages.ShowPage(new GenerateBackupPage(isServerOpen, server));
        }

        void loadBackupChecked(object sender, RoutedEventArgs e)
        {
            if (isServerOpen)
                MessageBox.Show("The server must be closed before loading a backup",
                    "Server is open", MessageBoxButton.OK, MessageBoxImage.Warning);

            else
                pages.ShowPage(new LoadBackupPage(server));
        }

        void manageBackupsChecked(object sender, RoutedEventArgs e)
        {
            pages.ShowPage(new ManageBackupsPage(server));
        }

        #endregion
    }
}
