/// <copyright file="Heads.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>A simple class to get hum... Minecraft players heads from their skins</summary>

using ExtensionMethods;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Minecraft_Server_Starter
{
    class Heads
    {
        #region Constant fields

        // where the cached heads are stored
        static string headsFolder =>
            Path.Combine(Settings.GetValue<string>("mssFolder"), "Heads");

        #endregion

        #region Public methods

        /// <summary>
        /// Return a player's head, and caches it in disk if it didn't exist yet
        /// </summary>
        /// <param name="player">The username to get the head from</param>
        /// <returns>The player head</returns>
        public static async Task<BitmapSource> GetPlayerHead(string player)
        {
            var file = Path.Combine(headsFolder, player + ".png");
            if (File.Exists(file))
                return new BitmapImage(new Uri(file));

            if (!Directory.Exists(headsFolder))
                Directory.CreateDirectory(headsFolder);

            var skin = await SkinDownloader.GetSkinHead(player);
            skin.Save(file);
            return skin;
        }

        /// <summary>
        /// Clears the player heads from disk. Returns false if some couldn't be deleted
        /// </summary>
        /// <returns></returns>
        public static bool ClearPlayerHeads()
        {
            bool success = true;
            if (!Directory.Exists(headsFolder)) return success;

            foreach (var head in Directory.GetFiles(headsFolder, "*.png"))
            {
                try { File.Delete(head); }
                catch { success = false; }
            }
            return success;
        }

        #endregion
    }
}
