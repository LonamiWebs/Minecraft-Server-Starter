/// <copyright file="PageTransition.xaml.cs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Aron Weiler (http://www.codeproject.com/Articles/197132/Simple-WPF-Page-Transitions)</author>
/// <date>20/05/20111</date>
/// <summary>Control used to slide between pages with a beautiful animation</summary>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;

namespace Minecraft_Server_Starter
{
    #region Converters

    public class CenterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        { return (double)value / 2; }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        { return (double)value * 2; }
    }

    #endregion

    public partial class PageTransition : UserControl
    {
        Stack<UserControl> pages = new Stack<UserControl>();

        public UserControl CurrentPage;

        public PageTransition()
        {
            InitializeComponent();
        }

        public void ShowPage(UserControl newPage)
        {
            pages.Push(newPage);

            Task.Factory.StartNew(() => ShowNewPage());
        }

        void ShowNewPage()
        {
            Dispatcher.Invoke(() =>
            {
                if (contentPresenter.Content != null)
                {
                    var oldPage = (UserControl)contentPresenter.Content;
                    oldPage.Loaded -= page_Loaded;
                    UnloadPage(oldPage);
                }
                else
                {
                    ShowNextPage();
                }
            });
        }

        void ShowNextPage()
        {
            var newPage = pages.Pop();

            newPage.Loaded += page_Loaded;

            contentPresenter.Content = newPage;
        }

        void UnloadPage(UserControl page)
        {
            var hidePage =
                ((Storyboard)(Resources["SlideAndFadeOut"])).Clone();

            hidePage.Completed += hidePage_Completed;

            hidePage.Begin(contentPresenter);
        }

        void page_Loaded(object sender, RoutedEventArgs e)
        {
            var showNewPage =
                ((Storyboard)(Resources["SlideAndFadeIn"])).Clone();

            showNewPage.Begin(contentPresenter);

            CurrentPage = (UserControl)sender;
        }

        void hidePage_Completed(object sender, EventArgs e)
        {
            contentPresenter.Content = null;

            ShowNextPage();
        }
    }
}