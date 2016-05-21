/// <copyright file="SkinDownloader.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Class used to download Minecraft skins given an username</summary>

using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Minecraft_Server_Starter
{
    static class SkinDownloader
    {
        #region Constant fields

        const string baseUrl = "http://skins.Minecraft.net/MinecraftSkins/{0}.png";

        #endregion

        #region Public methods

        /// <summary>
        /// Returns the head of the specified username skin
        /// </summary>
        /// <param name="username">The username to chop their head off</param>
        /// <returns>Their chopped head</returns>
        public static async Task<BitmapSource> GetSkinHead(string username)
        {
            var skin = await GetSkin(username);
            if (skin == null) return null;

            // head_location(8, 8); head_size(8, 8)
            return new CroppedBitmap(skin, new Int32Rect(8, 8, 8, 8));
        }

        /// <summary>
        /// Returns the skin of the specified username
        /// </summary>
        /// <param name="username">The username you wish to extract the skin from</param>
        /// <returns>Their skin</returns>
        public static async Task<BitmapImage> GetSkin(string username)
        {
            using (var wc = new WebClient())
                try { return loadImage(await wc.DownloadDataTaskAsync(getUrl(username))); }
                catch (WebException) { return null; } // probably 404
        }

        #endregion

        #region Private methods

        // username -> url
        static string getUrl(string username)
        {
            return string.Format(baseUrl, username);
        }

        // byte[] -> BitmapImage
        static BitmapImage loadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        #endregion
    }
}