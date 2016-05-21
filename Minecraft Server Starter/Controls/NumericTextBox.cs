/// <copyright file="NumericTextBox.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>TextBox which only allows numeric input</summary>

using System.Windows.Controls;
using System.Windows.Input;

namespace Minecraft_Server_Starter
{
    class NumericTextBox : TextBox
    {
        protected override void OnKeyDown(KeyEventArgs e)
        {
            e.Handled = handle(e.Key);
        }

        bool handle(Key key)
        {
            if (key == Key.Back)
                return false;
            
            if ((key < Key.D0 || key > Key.D9) &&
                (key < Key.NumPad0 || key > Key.NumPad9))
                return true;

            return false;
        }
    }
}
