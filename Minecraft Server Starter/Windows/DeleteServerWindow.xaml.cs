/// <copyright file="DeleteServerWindow.xaml.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Window used to delete a server from disk</summary>

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Minecraft_Server_Starter
{
    public partial class DeleteServerWindow : Window
    {
        #region Public properties

        public bool Deleted { get; private set; }

        #endregion

        #region Private fields

        Server server;
        bool isPerforming; // is an operation being performed?

        #endregion

        #region Constructor

        public DeleteServerWindow(Server server)
        {
            InitializeComponent();

            this.server = server;
            serverName.Text = server.Name;
        }

        #endregion

        #region Options hover

        // delete from list only
        void deleteFromListHover(object sender, MouseEventArgs e) => hideInfosExcept(infoDeleteFromListOnly);
        // backup and delete
        void backupDeleteServerHover(object sender, MouseEventArgs e) => hideInfosExcept(infoBackupDelete);
        // full delete
        void deleteServerHover(object sender, MouseEventArgs e) => hideInfosExcept(infoDelete);
        // cancel operation
        void cancelHover(object sender, MouseEventArgs e) => hideInfosExcept(infoCancel);

        #endregion

        #region Options click

        // delete from list only
        void deleteFromListClick(object sender, RoutedEventArgs e)
        {
            startPerforming();
            endPerforming(server.Delete());
        }

        // backup and delete
        async void backupDeleteClick(object sender, RoutedEventArgs e)
        {
            startPerforming(); // server can't be running if we got this far
            if (!await new Backup(server).Save(false))
                endPerforming(false);

            else
                endPerforming(server.DeleteAll());
        }

        // full delete
        void deleteClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Res.GetStr("sureDeleteServer"),
                Res.GetStr("thinkTwice"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            startPerforming();
            endPerforming(server.DeleteAll());
        }

        // cancel operation
        void cancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
        void windowClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = isPerforming;
        }

        #endregion

        #region Information

        void hideInfosExcept(TextBlock dontHide)
        {
            if (dontHide != null)
                dontHide.Visibility = Visibility.Visible;

            foreach (TextBlock info in infos.Children)
                if (info != dontHide)
                    info.Visibility = Visibility.Collapsed;
        }

        void serverLocationLinkClick(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(server.Location))
                Process.Start(server.Location);
        }

        #endregion

        #region Start or end performing an operation

        void startPerforming()
        {
            isPerforming = true;
            buttons.IsEnabled = false;
            hideInfosExcept(infoPerforming);
        }

        void endPerforming(bool success)
        {
            isPerforming = false;

            if (success)
            {
                MessageBox.Show(Res.GetStr("serverDeletedSuccess"),
                    Res.GetStr("success"), MessageBoxButton.OK, MessageBoxImage.Information);

                Deleted = true;
                Close();
            }
            else
            {
                MessageBox.Show(Res.GetStr("errorDeletingServer"),
                    Res.GetStr("error"), MessageBoxButton.OK, MessageBoxImage.Error);

                buttons.IsEnabled = true;
                hideInfosExcept(null);
            }
        }

        #endregion
    }
}
