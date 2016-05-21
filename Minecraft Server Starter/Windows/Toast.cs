/// <copyright file="Toast.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Class used to show toast messages</summary>

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Minecraft_Server_Starter
{
    public partial class Toast : Window
    {
        #region Constant values

        // default toast color
        static readonly Brush color = new SolidColorBrush(Color.FromArgb(0xff, 0x24, 0x24, 0x24));
        static readonly Brush textColor = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0xff, 0xff));

        // default toast location
        static readonly Location defaultLocation = Location.TopLeft;

        // default border thickness and corner radius
        static readonly Thickness borderThickness = new Thickness(1);
        static readonly CornerRadius cornerRadius = new CornerRadius(1);

        // default image and textblocks thickness
        static readonly Thickness imageMargin = new Thickness(10, 10, 0, 10);
        static readonly Thickness titleMargin = new Thickness(10, 5, 10, 0);
        static readonly Thickness textMargin = new Thickness(10, 0, 10, 10);

        // fade (show/hide) duration
        const int fadeDuration = 300;

        // sizes
        const double minPanelHeight = 120;
        const double minPanelWidth = 280;
        const double maxTextWidth = 280;
        const double imageSize = 100;
        const double locationMargin = 20;
        const double titleSize = 22;
        const double textSize = 18;

        // durations
        public const int LENGTH_SHORT = 2500;
        public const int LENGTH_LONG = 5000;

        #endregion

        #region Enumerations

        [Flags]
        public enum Location
        {
            TopLeft = 1 << 0,
            TopRight = 1 << 1,
            BottomLeft = 1 << 2,
            BottomRight = 1 << 3,

            TopMiddle = TopLeft | TopRight,
            BottomMiddle = BottomLeft | BottomRight,

            RightCenter = TopRight | BottomRight,
            LeftCenter = TopLeft | BottomLeft
        }

        #endregion

        #region Control variables

        // where will it be shown
        Location location;

        // how long will it be shown
        int duration;

        // to keep track of how long we've waited so far
        DispatcherTimer hideTimer;

        // are we hiding yet?
        bool hiding;

        // controls
        Border cBorder;
        StackPanel cMainPanel;
        StackPanel cContentPanel;
        Image cImage;
        TextBlock cTitle;
        TextBlock cText;

        string player;

        #endregion

        #region Constructor

        public static void makeText(string title, string text) { makeText(title, text, null); }
        public static void makeText(string title, string text, string player)
        {
            if (Settings.GetValue<bool>("notificationEnabled"))
                new Toast(title, text, player).Show();
        }

        Toast(string title, string text, string player)
        {
            location = (Location)Settings.GetValue<int>("notificationLoc");
            duration = LENGTH_SHORT;
            this.player = player;

            // Initialize component
            /* this = */
            ResizeMode = ResizeMode.NoResize; ShowInTaskbar = false; SizeToContent = SizeToContent.WidthAndHeight; WindowStyle = WindowStyle.None; ShowActivated = false; Topmost = true; AllowsTransparency = true; Background = Brushes.Transparent; Opacity = 0;
            cBorder = new Border { BorderThickness = borderThickness, BorderBrush = color, CornerRadius = cornerRadius };
            cMainPanel = new StackPanel { Orientation = Orientation.Horizontal, Background = color, MinHeight = minPanelHeight, MinWidth = minPanelWidth };
            cImage = new Image { Margin = imageMargin, Width = imageSize, Height = imageSize, Visibility = Visibility.Collapsed }; RenderOptions.SetBitmapScalingMode(cImage, BitmapScalingMode.NearestNeighbor); RenderOptions.SetEdgeMode(cImage, EdgeMode.Aliased);
            cContentPanel = new StackPanel { MaxWidth = maxTextWidth };
            cTitle = new TextBlock { Margin = titleMargin, TextWrapping = TextWrapping.Wrap, Foreground = textColor, FontSize = titleSize, FontFamily = new FontFamily("Segoe UI") };
            cText = new TextBlock { Margin = textMargin, TextWrapping = TextWrapping.Wrap, Foreground = textColor, FontSize = textSize, FontFamily = new FontFamily("Segoe UI") };

            // build tree
            AddChild(cBorder);
            cBorder.Child = cMainPanel;
            cMainPanel.Children.Add(cImage);
            cMainPanel.Children.Add(cContentPanel);
            cContentPanel.Children.Add(cTitle);
            cContentPanel.Children.Add(cText);

            if (string.IsNullOrEmpty(title)) cTitle.Visibility = Visibility.Collapsed;
            else cTitle.Text = title;

            if (string.IsNullOrEmpty(text)) cText.Visibility = Visibility.Collapsed;
            else cText.Text = text;

            hideTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
            { Interval = TimeSpan.FromMilliseconds(duration) };

            Loaded += Toast_Loaded;
            MouseDown += Toast_MouseDown;
            hideTimer.Tick += hideTimer_Tick;

            Closed += Toast_Closed;
        }

        #endregion

        #region Locate in screen

        void locateInScreen(Location location)
        {
            switch (location)
            {
                default:
                case Location.TopLeft:
                    Top = locationMargin;
                    Left = locationMargin;
                    break;
                case Location.TopRight:
                    Top = locationMargin;
                    Left = getRight();
                    break;
                case Location.BottomLeft:
                    Top = getBottom();
                    Left = locationMargin;
                    break;
                case Location.BottomRight:
                    Top = getBottom();
                    Left = getRight();
                    break;

                case Location.TopMiddle:
                    Top = locationMargin;
                    Left = getMiddle();
                    break;
                case Location.BottomMiddle:
                    Top = getBottom();
                    Left = getMiddle();
                    break;

                case Location.RightCenter:
                    Top = getCenter();
                    Left = getRight();
                    break;
                case Location.LeftCenter:
                    Top = getCenter();
                    Left = locationMargin;
                    break;
            }
        }
        double getRight() { return SystemParameters.WorkArea.Width - ActualWidth - locationMargin; }
        double getBottom() { return SystemParameters.WorkArea.Height - ActualHeight - locationMargin; }

        double getCenter() { return getBottom() / 2d; }
        double getMiddle() { return getRight() / 2d; }

        #endregion

        #region Events

        async void Toast_Loaded(object sender, RoutedEventArgs e)
        {
            // after load, locate, fade, and show
            locateInScreen(location);

            BitmapSource head;
            if (!string.IsNullOrEmpty(player) && (head = await Heads.GetPlayerHead(player)) != null)
            {
                cImage.Visibility = Visibility.Visible;
                cImage.Source = head;
            }

            await fade(true);
            hideTimer.IsEnabled = true;
        }

        async void Toast_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // force hiding, if it isn't hiding yet
            if (hiding) return;
            await fade(false);
        }

        async void hideTimer_Tick(object sender, EventArgs e)
        {
            hideTimer.IsEnabled = false;
            hideTimer = null;
            await fade(false);
        }

        void Toast_Closed(object sender, EventArgs e)
        {
            cImage.Source = null;
            cImage = null;
        }

        #endregion

        #region Fade

        async Task fade(bool show)
        {
            if (hiding) return;
            if (!show) hiding = true;

            Visibility = Visibility.Visible;

            var anim = new DoubleAnimation
            {
                From = show ? 0 : 1,
                To = show ? 1 : 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(fadeDuration)),
                EasingFunction = new CubicEase()
            };

            BeginAnimation(FrameworkElement.OpacityProperty, anim);
            await Task.Factory.StartNew(() => Thread.Sleep(fadeDuration));

            if (!show)
            {
                Visibility = Visibility.Hidden;
                Close();
            }
        }

        #endregion
    }
}