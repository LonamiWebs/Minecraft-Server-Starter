/// <copyright file="EditServerWindow.xaml.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Window used to edit various server settings</summary>

using Microsoft.Win32;
using System.Windows;
using System.IO;
using ExtensionMethods;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Minecraft_Server_Starter
{
    public partial class EditServerWindow : Window
    {
        #region Private fields

        readonly Server server;

        bool loading = true;

        #endregion

        #region Properties

        string ServerIconPath
        {
            get
            {
                var dir = Path.GetDirectoryName(jar.Text);
                if (string.IsNullOrEmpty(dir))
                    return null;

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                return Path.Combine(dir, "server-icon.png");
            }
        }

        #endregion

        #region Constructor and loading

        public EditServerWindow(Server server)
        {
            InitializeComponent();
            this.server = server;

            name.Text = server.Name;
            jar.Text = server.ServerJar;
            description.Text = server.Description;
            creationDate.Text = server.CreationDate.ToString("yyyy-MM-dd HH:mm:ss");
            lastPlayed.Text = server.LastUseDate.ToString("yyyy-MM-dd HH:mm:ss");

            motdBox.SetMOTD(server.Properties["motd"].Value);
        }

        async void loaded(object sender, RoutedEventArgs e)
        {
            reloadServerIcon();

            var levelName = server.Properties["level-name"].Value;
            await reloadWorldsList(string.IsNullOrEmpty(levelName) ? "world" : levelName);

            loading = false;
        }

        async Task reloadWorldsList(string selectName)
        {
            worldsList.Items.Clear();
            foreach (var world in await MinecraftWorld.ListWorlds(server.Location))
                worldsList.Items.Add(world);

            if (worldsList.Items.Count == 0)
            {
                worldWillGenerate.Visibility = Visibility.Visible;
                deleteSelectedWorld.Visibility = Visibility.Collapsed;
            }

            worldsList.Text = selectName;
        }

        #endregion

        #region Server jar

        void searchJarClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = Res.GetStr("sJarFileFilter") };
            if (ofd.ShowDialog() ?? false)
                jar.Text = ofd.FileName;
        }

        #endregion

        #region Icon

        void changeIconClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = Res.GetStr("sImageFilter") };
            if (ofd.ShowDialog() ?? false)
            {
                new Uri(ofd.FileName).LoadImage(64, 64).Save(ServerIconPath);
                reloadServerIcon();
            }
        }

        void reloadServerIcon()
        {
            if (File.Exists(ServerIconPath))
                icon.Source = new Uri(ServerIconPath).LoadImage();
        }

        #endregion

        #region Cancel and accept

        void cancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        void acceptClick(object sender, RoutedEventArgs e)
        {
            server.Name = name.Text;
            server.ServerJar = jar.Text;
            server.Description = description.Text;
            server.Save();

            Close();
        }

        #endregion

        #region More windows
        
        void editPropertiesClick(object sender, RoutedEventArgs e)
        {
            PropertiesEditorWindow.EditServer(server);
        }

        void openContainingFolderClick(object sender, RoutedEventArgs e)
        {
            Process.Start(server.Location);
        }

        #endregion

        async void addWorldClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog {
                Title = "Please select a .zip file containing a world",
                Filter = "Zip file|*.zip"
            };

            if (ofd.ShowDialog() ?? false)
            {
                if (MinecraftWorld.IsValidWorld(ofd.FileName))
                {
                    await reloadWorldsList(MinecraftWorld.ExtractWorldZip(ofd.FileName, server.Location));
                }
                else
                {
                    MessageBox.Show("You have selected an invalid .zip file!", "Invalid world file",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        void updateLevelNameProperty()
        {
            if (loading) return;
            server.UpdateServersProperty("level-name", worldsList.Text);
        }

        void worldsListTextChanged(object sender, RoutedEventArgs e)
        {
            worldWillGenerate.Visibility = worldsList.SelectedIndex < 0 ?
                Visibility.Visible : Visibility.Collapsed;

            deleteSelectedWorld.Visibility = worldsList.SelectedIndex < 0 ?
                Visibility.Collapsed : Visibility.Visible;

            updateLevelNameProperty();
        }

        void deleteSelectedWorldClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("This world will be lost forever. Do you wish to continue?",
                "World will be lost", MessageBoxButton.OKCancel, MessageBoxImage.Information)
                == MessageBoxResult.OK)
            {
                using (WaitCursor.New)
                {
                    var world = Path.Combine(server.Location, worldsList.Text);
                    foreach (var file in Directory.EnumerateFiles(world))
                        try { File.Delete(file); } catch { }

                    try { Directory.Delete(world, true); } catch { }
                }

                MessageBox.Show("The world has been deleted", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        void motdChanged(object sender, TextChangedEventArgs e)
        {
            server.UpdateServersProperty("motd", motdBox.GeneratedCode);
        }
    }
}
