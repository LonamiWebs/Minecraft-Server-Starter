/// <copyright file="PropertiesEditorWindow.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Window used to edit a server.properties file</summary>

using Minecraft_Server_Starter.Controls;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Minecraft_Server_Starter
{
    public partial class PropertiesEditorWindow : Window
    {
        #region Private fields

        // which file are we editing?
        string file;
        // did we save this file yet?
        bool saved = true;
        
        // currently open server properties
        ServerProperties svProperties;

        #endregion

        #region Public static methods

        // edit a server.properties file
        public static void EditFile(string file)
        {
            if (File.Exists(file))
                new PropertiesEditorWindow(file).Show();
        }

        public static void EditServer(Server server)
        {
            var path = server.PropertiesPath;
            if (!File.Exists(path))
                File.WriteAllText(path, ServerProperties.Empty.Encode());

            EditFile(path);
        }

        #endregion

        #region Constructor

        PropertiesEditorWindow(string file)
        {
            InitializeComponent();

            this.file = file;
            svProperties = ServerProperties.FromFile(file);
            LoadProperties();
        }

        void LoadProperties()
        {
            leftColumnPanel.Children.Clear();
            rightColumnPanel.Children.Clear();

            bool leftColumn = true;
            foreach (var property in svProperties.Properties)
            {
                if (!property.PassesSearch(searchBox.Text))
                    continue;
                
                if (leftColumn)
                    leftColumnPanel.Children.Add(new ServerPropertyControl(property, PropertyChanged));
                else
                    rightColumnPanel.Children.Add(new ServerPropertyControl(property, PropertyChanged));

                leftColumn = !leftColumn;
            }
        }

        void PropertyChanged(ServerProperty property)
        {
            File.WriteAllText(file, svProperties.Encode());
        }

        #endregion

        #region Events

        // save or close
        void saveClick(object sender, RoutedEventArgs e)
        {
            doSave();
            Close();
        }

        void cancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // notify user if they didn't save
        void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!saved)
                switch (MessageBox.Show("No has guardado el archivo. ¿Desea guardarlo ahora?",
                    "Archivo sin guardar", MessageBoxButton.YesNo, MessageBoxImage.Warning))
                {
                    case MessageBoxResult.Yes:
                        doSave();
                        break;
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        break;
                }
        }

        #endregion

        #region Save

        // save the file
        void doSave()
        {
            saved = true;
            var encoded = svProperties.Encode();

            // try saving until success
            bool retry = false;
            do
            {
                try { File.WriteAllText(file, encoded); }
                catch
                {
                    retry = MessageBox.Show("No se pudo guardar el archivo. ¿Quieres intentarlo otra vez?",
                        "Error", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes;
                }
            }
            while (retry);
        }

        #endregion

        void searchBox_KeyUp(object sender, KeyEventArgs e)
        {
            LoadProperties();
        }
    }
}
