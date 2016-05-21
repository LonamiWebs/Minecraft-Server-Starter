/// <copyright file="MinecraftWorld.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Class used to interop with Minecraft worlds</summary>

using ExtensionMethods;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Minecraft_Server_Starter
{
    public class MinecraftWorld
    {
        /// <summary>
        /// Full path of the folder containing the Minecraft world
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// The name of the Minecraft world
        /// </summary>
        public string Name { get; set; }

        public MinecraftWorld(string path)
        {
            FullPath = path.TrimEnd(Path.DirectorySeparatorChar);
            Name = Path.GetFileName(FullPath);
        }

        /// <summary>
        /// Determines whether a .zip file or a folder is a valid Minecraft world
        /// </summary>
        /// <param name="path">The path to the world</param>
        /// <returns>True if it's valid</returns>
        public static bool IsValidWorld(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (Directory.Exists(path))
            {
                return File.Exists(Path.Combine(path, "level.dat"));
            }
            else if (File.Exists(path) && Path.GetExtension(path)
                    .Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return !string.IsNullOrEmpty(findLevelData(path));
            }

            return false;
        }

        /// <summary>
        /// Extracts a .zip file containing a world to the desired base path
        /// </summary>
        /// <param name="worldZip">The world .zip file</param>
        /// <param name="basePath">The base path</param>
        /// <returns>The world name</returns>
        public static string ExtractWorldZip(string worldZip, string basePath)
        {
            string worldName = null;
            using (ZipArchive zip = ZipFile.Open(worldZip, ZipArchiveMode.Read))
            {
                var worldRoot = Path.GetDirectoryName(findLevelData(worldZip));
                if (string.IsNullOrEmpty(worldRoot)) // level.dat is directly in the root of the .zip >.<
                    worldName = Path.GetFileNameWithoutExtension(worldZip);
                else
                    worldName = Path.GetFileName(worldRoot);

                basePath = Path.Combine(basePath, worldName);

                // every world file must start by the world root path + "/"
                var mustStartWith = string.IsNullOrEmpty(worldRoot) ? string.Empty :
                    worldRoot.Replace(Path.DirectorySeparatorChar, '/') + "/";
                foreach (var entry in zip.Entries)
                    if (!entry.FullName.EndsWith("/") && // if it's not dir and it starts with,
                        entry.FullName.StartsWith(mustStartWith))
                    {
                        // the path is equal to the desired path + the relative path EXCEPT the start
                        var newpath = string.IsNullOrEmpty(mustStartWith) ?
                            Path.Combine(basePath, entry.FullName).Replace('/', Path.DirectorySeparatorChar) :
                            Path.Combine(basePath, entry.FullName.Replace(mustStartWith, string.Empty))
                            .Replace('/', Path.DirectorySeparatorChar);

                        entry.ExtractToFileSafe(newpath, true);
                    }
            }
            return worldName;
        }

        static string findLevelData(string zipFile)
        {
            using (var zip = ZipFile.OpenRead(zipFile))
                foreach (var entry in zip.Entries)
                    if (entry.Name == "level.dat")
                        return entry.FullName;

            return null;
        }

        /// <summary>
        /// List all available worlds in a base folder
        /// </summary>
        /// <param name="basePath">The base folder path containing the worlds</param>
        /// <returns>All available worlds</returns>
        public static async Task<List<MinecraftWorld>> ListWorlds(string basePath)
        {
            var worlds = new List<MinecraftWorld>();

            await Task.Factory.StartNew(() =>
            {
                foreach (var file in Directory.EnumerateFiles(basePath, "level.dat", SearchOption.AllDirectories))
                    worlds.Add(new MinecraftWorld(Path.GetDirectoryName(file)));
            });

            return worlds;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
