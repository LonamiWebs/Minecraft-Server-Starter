/// <copyright file="PropertiesEditorWindow.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Window used to edit a server.properties file</summary>

using Minecraft_Server_Starter.Controls;
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

        // search
        void searchBox_KeyUp(object sender, KeyEventArgs e)
        {
            LoadProperties();
        }

        #endregion
    }
}
