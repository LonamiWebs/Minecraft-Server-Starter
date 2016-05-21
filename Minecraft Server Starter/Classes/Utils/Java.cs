/// <copyright file="Java.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Class used to interop with Java</summary>

using System;
using System.IO;

namespace Minecraft_Server_Starter
{
    public static class Java
    {
        // returns null if the path wasn't found
        public static string FindJavaPath()
        {
            var path = GetFullPath("java.exe");
            if (!string.IsNullOrEmpty(path))
                return path;

            var javaFolder = Path.Combine(Environment.GetFolderPath
                (Environment.SpecialFolder.ProgramFiles), "Java");

            if (!Directory.Exists(javaFolder))
                return string.Empty;

            string greatestDir = null;
            Version greatestVersion = null;
            foreach (var dir in Directory.EnumerateDirectories(javaFolder))
            {
                if (dir.StartsWith("jre"))
                {
                    var version = new Version(dir.Substring(3).Replace('_', '.'));
                    if (greatestVersion == null || version > greatestVersion)
                    {
                        greatestDir = dir;
                        greatestVersion = version;
                    }
                }
            }
            if (string.IsNullOrEmpty(greatestDir)) return string.Empty;

            var bin = Path.Combine(javaFolder, greatestDir, "bin");
            if (!Directory.Exists(bin)) return string.Empty;

            var java = Path.Combine(bin, "java.exe");
            if (!File.Exists(java)) return string.Empty;

            return java;
        }

        static string GetFullPath(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(';'))
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }
    }
}
