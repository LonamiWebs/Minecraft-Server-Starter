/// <copyright file="MainWindow.xaml.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>The main program window</summary>

using ExtensionMethods;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Minecraft_Server_Starter
{
    public partial class MainWindow : Window
    {
        #region Private fields

        MinecraftServer ms;

        // current server status
        Status currentStatus = Status.Closed;

        // can the ports be opened?
        bool canOpenPorts;

        // are the ports open at the moment?
        bool arePortsOpen;

        // don't open multiple settings instances
        SettingsWindow settingsWindow;

        #endregion

        #region Constructor and loading

        public MainWindow()
        {
            InitializeComponent();
        }

        // load window
        async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Settings.GetValue<bool>("eulaAccepted"))
            {
                if (MessageBox.Show(Res.GetStr("smcAgreeEula"),
                    Res.GetStr("smtAgreeEula"), MessageBoxButton.YesNo, MessageBoxImage.Information)
                    != MessageBoxResult.Yes)
                {
                    Close();
                    return;
                }
                Settings.SetValue("eulaAccepted", true);
            }
            reloadServers();
            canOpenPorts = await PortManager.Init();
        }

        #endregion

        #region Server related
        
        #region Server operations

        // delete server
        void deleteServerClick(object sender, RoutedEventArgs e)
        {
            // check if running
            if (CheckServerRunning())
                return;

            // else show a new window, and if the srever was deleted, reload the servers
            var dsw = new DeleteServerWindow((Server)serverList.SelectedItem);
            dsw.ShowDialog();

            if (dsw.Deleted)
                reloadServers();
        }

        // manage backups
        void manageBackupsClick(object sender, RoutedEventArgs e)
        {
            if (ms == null) // if no server is open, open a backup window with false
                new BackupWindow(isServerOpen: false, server: (Server)serverList.SelectedItem).ShowDialog();

            else // else, if the currently selected server is equal to the servers, and it's open, isServerOpen will be true
                new BackupWindow(ms.Server == ((Server)serverList.SelectedItem)
                    && currentStatus == Status.Open, (Server)serverList.SelectedItem).ShowDialog();
        }

        // edit the current server
        void editServerClick(object sender, RoutedEventArgs e)
        {
            new EditServerWindow((Server)serverList.SelectedItem).Show();
        }

        // upgrade (or downgrade) the current server
        void upgradeServerClick(object sender, RoutedEventArgs e)
        {
            // check if running
            if (CheckServerRunning()) // TODO use a different text!
                return;

            // else we can upgrde
            new UpgradeWindow((Server)serverList.SelectedItem).ShowDialog();
        }

        bool CheckServerRunning()
        {
            // if the currently selected server is equal to the running one, and it's not stopped, show error
            var server = (Server)serverList.SelectedItem;
            if (ms != null && (server == ms.Server) &&
                currentStatus != Status.Closed)
            {
                MessageBox.Show(Res.GetStr("smcCannotDeleteRunning"),
                    Res.GetStr("smcCannotDeleteRunning"), MessageBoxButton.OK, MessageBoxImage.Error);

                return true;
            }

            return false;
        }

        // add server
        void addServerClick(object sender, RoutedEventArgs e)
        {
            showAddServer();
        }

        void showAddServer(string worldPath = null)
        {
            var asw = new AddServerWindow(worldPath);
            if (asw.ShowDialog() ?? false)
            {
                asw.Result.Save();
                reloadServers();
            }
        }

        #endregion

        #region Server management

        // reload servers list
        void reloadServers()
        {
            serverList.Items.Clear();
            foreach (var sv in Server.GetServers())
                serverList.Items.Add(sv);

            if (serverList.Items.Count > 0) // if there's any, select it
            {
                serverList.SelectedIndex = 0;
                serverSelectedOptionsPanel.Visibility =
                    startStopPanel.Visibility = serverList.Visibility = Visibility.Visible;
            }
            else
                serverSelectedOptionsPanel.Visibility =
                    startStopPanel.Visibility = serverList.Visibility = Visibility.Collapsed;
        }

        // show server tooltip
        void serverListMouseEnter(object sender, MouseEventArgs e)
        {
            if (serverList.Items.Count == 0) return;

            var sv = (Server)serverList.SelectedItem;
            serverList.ToolTip = Res.GetStr("sCreatedThe", sv.CreationDate.ToLongDateString());
            if (!string.IsNullOrEmpty(sv.Description))
                serverList.ToolTip += Environment.NewLine + sv.Description;
        }

        // start/stop the server
        void startStopClick(object sender, RoutedEventArgs e)
        {
            switch (currentStatus)
            {
                case Status.Closed:

                    if (!File.Exists(Settings.GetValue<string>("javaPath")))
                    {
                        MessageBox.Show(Res.GetStr("smcJavaNotFound"),
                            Res.GetStr("smtJavaNotFound"), MessageBoxButton.OK, MessageBoxImage.Error);

                        break;
                    }

                    ms = new MinecraftServer((Server)serverList.SelectedItem);
                    ms.ServerMessage += log;
                    ms.ServerStatusChanged += serverStatusChanged;
                    ms.Player += playerEvent;

                    serverStatusChanged(Status.Opening);
                    logBox.Clear();

                    ms.Start();
                    break;
                case Status.Opening:
                    serverStatusChanged(Status.Closed);
                    ms.Kill();
                    break;
                case Status.Open:
                    serverStatusChanged(Status.Closing);

                    ms.Stop();
                    break;
                case Status.Closing:
                    serverStatusChanged(Status.Closed);
                    ms.Kill();
                    break;
            }
        }

        #endregion

        #region Server events

        void playerEvent(bool joined, string player)
        {
            Dispatcher.Invoke(() =>
            {
                Toast.makeText(Res.GetStr(joined ? "sPlayerJoined" : "sPlayerLeft", player),
                    Res.GetStr(joined ? "sPlayerJoinedFull" : "sPlayerLeftFull", player), player);

                if (joined)
                {
                    playerList.Items.Add(player);
                    playerList.SelectedItem = player;
                }
                else
                    playerList.Items.Remove(player);

                playersGrid.Visibility = playerList.Items.Count > 0 ?
                    Visibility.Visible : Visibility.Collapsed;
            });
        }

        #endregion
        
        #region Server status

        async void serverStatusChanged(Status status)
        {
            bool mustClosePorts = false;
            currentStatus = status;

            Dispatcher.Invoke(() =>
            {
                Title = Res.GetStr("appTitle");

                switch (status)
                {
                    case Status.Opening:
                    case Status.Closing:
                        Title += Res.GetStr("sServerRunning", ms.Server.Name);
                        startStopBlock.Text = Res.GetStr("sForceClose");
                        startStopImage.Source = Res.GetRes<BitmapImage>("StopImg");

                        commandGrid.Visibility = Visibility.Visible;

                        sayGrid.Visibility = moreServerAction.Visibility =
                            moreServerActionMenu.Visibility = Visibility.Collapsed;

                        portButton.Visibility = canOpenPorts ? Visibility.Visible : Visibility.Collapsed;

                        break;
                    case Status.Open:
                        Title += Res.GetStr("sServerRunning", ms.Server.Name);
                        startStopBlock.Text = Res.GetStr("sCloseSaving");
                        startStopImage.Source = Res.GetRes<BitmapImage>("StopImg");

                        commandGrid.Visibility = sayGrid.Visibility =
                            moreServerAction.Visibility = moreServerActionMenu.Visibility = Visibility.Visible;

                        portButton.Visibility = canOpenPorts ? Visibility.Visible : Visibility.Collapsed;

                        break;
                    case Status.Closed:
                        startStopBlock.Text = Res.GetStr("sOpenServer");
                        startStopImage.Source = Res.GetRes<BitmapImage>("PlayImg");

                        commandGrid.Visibility = sayGrid.Visibility =
                            moreServerAction.Visibility = moreServerActionMenu.Visibility =
                            portButton.Visibility = Visibility.Collapsed;

                        if (arePortsOpen)
                            mustClosePorts = true;

                        break;
                }
            });

            if (mustClosePorts)
                await togglePorts();
        }

        #endregion

        #region Server commands

        // commands
        void commandBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                doSendCommand();
            }
        }
        void sendCommandClick(object sender, RoutedEventArgs e) => doSendCommand();
        void doSendCommand()
        {
            if (!string.IsNullOrWhiteSpace(commandBox.Text))
            {
                ms.SendCommand(commandBox.Text.Trim());
                commandBox.Clear();
            }
        }

        // say
        void sayBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                doSay();
            }
        }
        void sendMessageClick(object sender, RoutedEventArgs e) => doSay();
        void doSay()
        {
            if (!string.IsNullOrWhiteSpace(sayBox.Text))
            {
                ms.Say(sayBox.Text.Trim());
                sayBox.Clear();
            }
        }

        // more actions menu
        void moreServerActionClick(object sender, RoutedEventArgs e)
        {
            moreServerActionMenu.IsOpen = true;
        }

        // -> save
        void saveWorldClick(object sender, RoutedEventArgs e)
        {
            moreServerActionMenu.IsOpen = false;
            ms.Save();
            moreServerActionMenu.IsOpen = false;
        }

        // -> kill
        void closeNoSaveClick(object sender, RoutedEventArgs e)
        {
            moreServerActionMenu.IsOpen = false;
            ms.Kill();
            moreServerActionMenu.IsOpen = false;
        }

        #endregion

        #endregion

        #region Player management

        // op
        void opClick(object sender, RoutedEventArgs e)
            => ms.Op((string)playerList.SelectedItem);

        // deop
        void deopClick(object sender, RoutedEventArgs e)
            => ms.Deop((string)playerList.SelectedItem);

        // ban
        void banClick(object sender, RoutedEventArgs e)
            => ms.Ban((string)playerList.SelectedItem);

        // pardon
        void pardonClick(object sender, RoutedEventArgs e)
            => ms.Pardon((string)playerList.SelectedItem);

        // kick
        void kickClick(object sender, RoutedEventArgs e)
            => ms.Kick((string)playerList.SelectedItem);

        // tell
        void tellPlayerClick(object sender, RoutedEventArgs e)
        {
            commandBox.Focus();
            commandBox.SelectAll();
            commandBox.Text = $"tell {(string)playerList.SelectedItem} ";
            commandBox.SelectionStart = commandBox.Text.Length;
        }

        // reload the player head
        async void playerSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            playerHead.Source = playerList.SelectedIndex < 0 ? null :
                await Heads.GetPlayerHead((string)playerList.SelectedItem);
        }

        #endregion

        #region Log

        void log(string time, string type, string typeName, string msg)
        {
            Dispatcher.Invoke(() =>
            {
                logBox.AppendText(time, Brushes.LightBlue);
                logBox.AppendText(type, GetColorForType(typeName));
                logBox.AppendLine(msg);
                logBox.ScrollToEnd();
            });
        }

        void copyLogClick(object sender, RoutedEventArgs e)
        {
            logBoxMenu.IsOpen = false;

            if (logBox.Selection.IsEmpty)
                Clipboard.SetText(logBox.Text);
            else
                Clipboard.SetText(logBox.Selection.Text);
        }

        void saveLogClick(object sender, RoutedEventArgs e)
        {
            logBoxMenu.IsOpen = false;
            var sfd = new SaveFileDialog()
            {
                FileName = "mss_log_" + DateTime.Now.ToShortDateString(),
                Filter = Res.GetStr("sTextFileFilter")
            };
            if (sfd.ShowDialog() ?? false)
            {
                if (logBox.Selection.IsEmpty)
                    File.WriteAllText(sfd.FileName, logBox.Text);
                else
                    File.WriteAllText(sfd.FileName, logBox.Selection.Text);
            }
        }

        void clearLogClick(object sender, RoutedEventArgs e)
        {
            logBoxMenu.IsOpen = false;
            logBox.Clear();
        }

        Brush GetColorForType(string typeName)
        {
            switch (typeName)
            {
                case "INFO": return Brushes.Blue;
                case "WARNING": case "WARN": return Brushes.DarkOrange;
                case "ERROR": return Brushes.Red;
                default: return Brushes.Violet;
            }
        }

        #endregion

        #region Drag and drop worlds

        // drag enter
        void dragEnter(object sender, DragEventArgs e)
        {
            Console.WriteLine("enter");
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (paths != null && paths.Length == 1 && MinecraftWorld.IsValidWorld(paths[0]))
            {
                e.Effects = DragDropEffects.Copy;
                toggleDragDropGrid(true);
            }
            else
            {
                e.Effects = DragDropEffects.None;
                toggleDragDropGrid(false, true);
            }
        }

        // drag leave
        void dragLeave(object sender, DragEventArgs e)
        {
            Console.WriteLine("leave");
            toggleDragDropGrid(false);
        }

        void dragMouseMove(object sender, MouseEventArgs e)
        {
            Console.WriteLine("move");
            // handle awkward cases!
            if (Mouse.LeftButton == MouseButtonState.Released && dragDropGrid.Opacity > 0)
                toggleDragDropGrid(false);
        }

        // drag drop
        void dragDrop(object sender, DragEventArgs e)
        {
            Console.WriteLine("drop");
            var a = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
            toggleDragDropGrid(false);
            if ((e.Effects & DragDropEffects.Copy) != 0)
                showAddServer(((string[])e.Data.GetData(DataFormats.FileDrop))[0]);
        }

        // toggle grid
        void toggleDragDropGrid(bool shown, bool quick = false)
        {
            // TODO fix this, after toggling it hit test is gone! :(
            Console.WriteLine("toggle");
            if (quick)
                dragDropGrid.Opacity = shown ? 1 : 0;
            else
                dragDropGrid.Fade(shown);
        }

        #endregion

        #region Settings

        void settingsClick(object sender, RoutedEventArgs e)
        {
            if (settingsWindow == null)
            {
                settingsWindow = new SettingsWindow();
                settingsWindow.Closed += (ss, se) => settingsWindow = null;
                settingsWindow.Show();
            }
            else
                settingsWindow.Activate();
        }

        #endregion

        #region Ports

        async void portClick(object sender, RoutedEventArgs e)
        {
            await togglePorts();
        }

        async Task togglePorts()
        {
            ushort port;
            ushort.TryParse(ms.Server.Properties["server-port"].Value, out port);
            if (port == 0)
                port = 25565;

            if (arePortsOpen) // close them
            {
                await PortManager.ClosePort(port);
                portsText.Text = "Open port";
            }
            else // open them
            {
                await PortManager.OpenPort(port);
                portsText.Text = "Close port";
            }

            arePortsOpen = !arePortsOpen;
        }

        #endregion

        #region Handle forced closing

        void Window_Closing(object sender, CancelEventArgs e)
        {
            switch (currentStatus)
            {
                case Status.Opening:
                case Status.Closing:

                    switch (MessageBox.Show(Res.GetStr("smcServerBusy"), Res.GetStr("smtServerBusy"),
                        MessageBoxButton.OKCancel, MessageBoxImage.Question))
                    {
                        case MessageBoxResult.OK:
                            ms.Kill();
                            return;

                        default:
                            e.Cancel = true;
                            return;
                    }

                case Status.Open:

                    switch (MessageBox.Show(Res.GetStr("smcWorldUnsaved"), Res.GetStr("smtWorldUnsaved"),
                        MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
                    {
                        case MessageBoxResult.Yes:
                            ms.Stop();
                            return;
                        case MessageBoxResult.No:
                            ms.Kill();
                            return;
                        default:
                            e.Cancel = true;
                            return;
                    }
            }
        }

        #endregion
    }
}
