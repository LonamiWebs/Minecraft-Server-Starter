/// <copyright file="EULA.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Class used to prompt Mojang's EULA message</summary>

using System.IO;

namespace Minecraft_Server_Starter
{
    /// <summary>
    /// Class to interop with Minecraft EULA
    /// </summary>
    public static class EULA
    {
        // a string representing the Minecraft eula
        const string eulaString =
@"#By changing the setting below to TRUE you are indicating your agreement to our EULA (https://account.mojang.com/documents/Minecraft_eula).
#{0}
eula=true
";

        /// <summary>
        /// Create an EULA file if it's set to false or it doesn't exist
        /// </summary>
        public static void CreateEULA(string dir)
        {
            if (string.IsNullOrEmpty(dir))
                return;//

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string eula = Path.Combine(dir, "eula.txt");
            if (!File.Exists(eula) || File.ReadAllText(eula).Contains("eula=false"))
                File.WriteAllText(eula, string.Format(eulaString, UnixDate.GetUniversalDate()));
        }
    }
}
