/// <copyright file="ServerPropertyControl.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>07/05/2016</date>
/// <summary>Control representing a server property</summary>

using System.Windows;
using System.Windows.Controls;

namespace Minecraft_Server_Starter.Controls
{
    public delegate void PropertyChangedEventHandler(ServerProperty property);

    public class ServerPropertyControl : UserControl
    {
        public ServerProperty Property { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        void onPropertyChanged() => PropertyChanged?.Invoke(Property);

        Grid mainGrid;
        public ServerPropertyControl(ServerProperty property, PropertyChangedEventHandler handler = null)
        {
            Property = property;
            mainGrid = new Grid { Margin = new Thickness(4) };

            if (ServerProperties.IsTrueFalse(property.OriginalName))
                AddCheckBox(property.Name, property.Value == "true");

            else
            {
                SetDualGrid(property.Name);

                if (ServerProperties.IsInteger(property.OriginalName))
                    AddIntBox(property.Value);

                else if (ServerProperties.IsMultiChoice(property.OriginalName))
                    AddComboBox(ServerProperties.GetMultiChoices(property.OriginalName), property.Value);

                else
                    AddTextBox(property.Value);

                Grid.SetColumn(mainGrid.Children[mainGrid.Children.Count - 1], 1);
            }

            AddChild(mainGrid);
            PropertyChanged = handler;
        }

        void SetDualGrid(string firstGridName)
        {
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            mainGrid.Children.Add(new TextBlock
            {
                Text = firstGridName,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            });
        }

        void AddCheckBox(string name, bool isChecked)
        {
            var checkBox = new CheckBox();
            checkBox.Content = name;
            checkBox.IsChecked = isChecked;

            checkBox.Checked += (s, e) =>
            {
                Property.Value = "true";
                onPropertyChanged();
            };
            checkBox.Unchecked += (s, e) =>
            {
                Property.Value = "false";
                onPropertyChanged();
            };

            mainGrid.Children.Add(checkBox);
        }

        void AddComboBox(string[] choices, string value)
        {
            var comboBox = new ComboBox();
            foreach (var choice in choices)
                comboBox.Items.Add(choice);

            int selectedIndex;
            if (int.TryParse(value, out selectedIndex))
                comboBox.SelectedIndex = selectedIndex;

            comboBox.SelectionChanged += (s, e) =>
            {
                Property.Value = comboBox.SelectedIndex.ToString();
                onPropertyChanged();
            };
            mainGrid.Children.Add(comboBox);
        }

        void AddIntBox(string value)
        {
            var textBox = new NumericTextBox();
            textBox.Text = value;
            textBox.TextChanged += (s, e) =>
            {
                Property.Value = textBox.Text;
                onPropertyChanged();
            };
            mainGrid.Children.Add(textBox);
        }

        void AddTextBox(string value)
        {
            var textBox = new TextBox();
            textBox.Text = value;
            textBox.TextChanged += (s, e) =>
            {
                Property.Value = textBox.Text;
                onPropertyChanged();
            };
            mainGrid.Children.Add(textBox);
        }
    }
}
