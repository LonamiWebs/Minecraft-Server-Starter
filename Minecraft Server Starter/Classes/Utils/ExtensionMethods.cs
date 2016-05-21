/// <copyright file="ExtensionMethods.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Some extension methods</summary>

using System;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace ExtensionMethods
{
    public static class Images
    {
        /// <summary>
        /// Resizes a bitmap to the new desired size
        /// </summary>
        /// <param name="bitmapSource">Bitmap to resize</param>
        /// <param name="newWidth">New bitmap width</param>
        /// <param name="newHeight">New bitmap height</param>
        /// <returns>The resized bitmap</returns>
        public static BitmapSource Resize(this BitmapSource bitmapSource, double newWidth, double newHeight)
        {
            if (bitmapSource == null) return null;

            return new TransformedBitmap(bitmapSource, new ScaleTransform(
                newWidth / bitmapSource.PixelWidth, newHeight / bitmapSource.PixelHeight));
        }

        /// <summary>
        /// Saves a BitmapSource to disk
        /// </summary>
        /// <param name="bitmapSource">Bitmap to save</param>
        /// <param name="fileName">Location where the bitmap wil be saved</param>
        public static bool Save(this BitmapSource bitmapSource, string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || bitmapSource == null)
                return false;

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));

                using (var filestream = new FileStream(fileName, FileMode.Create))
                    encoder.Save(filestream);
            }
            catch { return false; }
            return true;
        }
    }

    public static class Zips
    {
        public static void AddDirectory(this ZipArchive zip, string dir, string basePath = null)
        {
            var zipPath = string.IsNullOrEmpty(basePath) ?
                Path.GetFileName(dir) :
                basePath + "/" + Path.GetFileName(dir);
            
            foreach (var subFile in Directory.GetFiles(dir))
                zip.AddFile(subFile, zipPath);

            foreach (var subDir in Directory.GetDirectories(dir))
                zip.AddDirectory(subDir, zipPath);
        }

        public static void AddFile(this ZipArchive zip, string file, string basePath = null)
        {
            var zipPath = string.IsNullOrEmpty(basePath) ?
                Path.GetFileName(file) :
                basePath + "/" + Path.GetFileName(file);

            zip.CreateEntryFromFile(file, zipPath);
        }

        public static bool ExtractToDirectory(this ZipArchive zip, string folder, bool overwrite)
        {
            try
            {
                foreach (var entry in zip.Entries)
                    entry.ExtractToFileSafe(Path.Combine(folder, entry.FullName), overwrite);

                return true;
            }
            catch { return false; }
        }

        public static bool ExtractToFileSafe(this ZipArchiveEntry entry, string file, bool overwrite)
        {
            try
            {
                var dir = Path.GetDirectoryName(file);
                if (!Directory.Exists(Path.GetDirectoryName(file)))
                    Directory.CreateDirectory(Path.GetDirectoryName(file));

                entry.ExtractToFile(file, overwrite);
                return true;
            }
            catch  { return false; }
        }
    }

    public static class Longs
    {
        static readonly string[] fileSizes = new string[] {
            "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"
        };

        public static string ToFileSizeString(this long value)
        {
            double newValue = value;
            int fsIndex = 0; // file size index

            while (newValue > 1024d)
            {
                ++fsIndex;
                newValue /= 1024d;
            }

            return newValue.ToString("0.##") + " " + fileSizes[fsIndex];
        }
    }

    public static class Directories
    {
        public static void CopyDirectory(this DirectoryInfo src, DirectoryInfo dst)
        {
            if (!Directory.Exists(dst.FullName))
                Directory.CreateDirectory(dst.FullName);
            
            foreach (FileInfo fi in src.GetFiles())
                fi.CopyTo(Path.Combine(dst.FullName, fi.Name), true);
            
            foreach (DirectoryInfo diSourceSubDir in src.GetDirectories())
                diSourceSubDir.CopyDirectory(dst.CreateSubdirectory(diSourceSubDir.Name));
        }
    }

    public static class Uris
    {
        public static BitmapSource LoadImage(this Uri uri)
        {
            var image = new BitmapImage();
            using (FileStream stream = File.OpenRead(uri.LocalPath))
            {
                image.BeginInit();
                image.StreamSource = stream;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit(); // load the image from the stream
            } // close the stream
            return image;
        }

        public static BitmapSource LoadImage(this Uri uri, double width, double height)
            => uri.LoadImage().Resize(width, height);
    }

    public static class Animations
    {
        #region Fade

        public static void Fade(this UIElement element, bool? fadeIn, double duration = 500)
        {
            if (!fadeIn.HasValue) // if null, fade in will be true if opacity is 0
                fadeIn = element.Opacity == 0;

            var anim = new DoubleAnimation()
            {
                From = fadeIn.Value ? 0 : 1,
                To = fadeIn.Value ? 1 : 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(duration)),
                EasingFunction = new CubicEase()
            };

            element.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        #endregion
    }

    public static class Strings
    {
        public static bool Contains(this string str, string value, StringComparison comparision)
        {
            return str.IndexOf(value, comparision) > -1;
        }
    }
}
