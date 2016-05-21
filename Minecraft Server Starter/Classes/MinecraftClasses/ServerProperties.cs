/// <copyright file="ServerProperties.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Class representing a server.properties file</summary>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Minecraft_Server_Starter
{
    public class ServerProperties
    {
        #region Indexer

        /// <summary>
        /// Gets or sets the server property with a given a name
        /// </summary>
        /// <param name="name">The name of the property</param>
        /// <returns>The server property with that name</returns>
        public ServerProperty this[string name]
        {
            get
            {
                return Properties.FirstOrDefault(p => p.MatchsName(name))
                  ?? new ServerProperty(name, string.Empty);
            }
            set
            {
                bool done = false;
                for (int i = 0; i < Properties.Count; i++)
                {
                    if (Properties[i].MatchsName(name))
                    {
                        Properties[i] = value;
                        break;
                    }
                }
                if (!done)
                {
                    Properties.Add(value);
                }
            }
        }

        #endregion

        #region Constant fields

        // the default name of the server.properties file
        public const string PropertiesName = "server.properties";

        // the header of the server.properties
        const string header = "#Minecraft server properties";
        // a base template of the server.properties
        const string baseTemplate = @"#Minecraft server properties
#Day Mon dd hh:mm:ss CET year
allow-flight=false
allow-nether=true
announce-player-achievements=true
broadcast-console-to-ops=true
difficulty=1
enable-command-block=false
enable-query=false
enable-rcon=false
force-gamemode=false
gamemode=0
generate-structures=true
generator-settings=
hardcore=false
level-name=world
level-seed=
level-type=DEFAULT
max-build-height=256
max-players=20
max-tick-time=60000
max-world-size=29999984
motd=A Minecraft Server
network-compression-threshold=256
online-mode=true
op-permission-level=4
player-idle-timeout=0
pvp=true
resource-pack-hash=
resource-pack-sha1=
resource-pack=
server-ip=
server-port=25565
snooper-enabled=true
spawn-animals=true
spawn-monsters=true
spawn-npcs=true
view-distance=10
white-list=false
";

        #endregion

        #region Public fields

        /// <summary>
        /// Properties in the server.properties
        /// </summary>
        public List<ServerProperty> Properties = new List<ServerProperty>();

        #endregion

        #region Constructors

        // create a new instance
        ServerProperties() { }

        /// <summary>
        /// Gets a default (empty) server properties
        /// </summary>
        public static ServerProperties Empty => FromString(baseTemplate);

        /// <summary>
        /// Gets a server properties from a given file
        /// </summary>
        /// <param name="file">The server.properties file</param>
        /// <returns>The server properties representing the file</returns>
        public static ServerProperties FromFile(string file)
        {
            return FromString(File.ReadAllText(file));
        }

        /// <summary>
        /// Gets a server properties from a given server.properties string
        /// </summary>
        /// <param name="str">The server.properties contents</param>
        /// <returns>The server properties representing the contents of the server.properties file</returns>
        public static ServerProperties FromString(string str)
        {
            var svProperties = new ServerProperties();

            var lines = str.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.StartsWith("#") || !line.Contains("="))
                    continue; // ignore comment

                var spl = line.Split(new char[] { '=' }, StringSplitOptions.None);
                if (spl.Length < 2) continue; // ignore invalid line

                // add property
                svProperties.Properties.Add(new ServerProperty(spl[0], spl[1]));
            }

            return svProperties;
        }

        #endregion

        #region Defaults

        public static bool IsMultiChoice(string propertyName)
        {
            switch (propertyName)
            {
                case "difficulty":
                case "gamemode":
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsInteger(string propertyName)
        {
            switch (propertyName)
            {
                case "max-build-height":
                case "max-players":
                case "max-tick-time":
                case "max-world-size":
                case "network-compression-threshold":
                case "op-permission-level":
                case "player-idle-timeout":
                case "server-port":
                case "view-distance":
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsTrueFalse(string propertyName)
        {
            switch (propertyName)
            {
                case "allow-flight":
                case "allow-nether":
                case "announce-player-achievements":
                case "broadcast-console-to-ops":
                case "enable-command-block":
                case "enable-query":
                case "enable-rcon":
                case "force-gamemode":
                case "generate-structures":
                case "hardcore":
                case "online-mode":
                case "pvp":
                case "snooper-enabled":
                case "spawn-animals":
                case "spawn-monsters":
                case "spawn-npcs":
                case "white-list":
                    return true;
                    
                default:
                    return false;
            }
        }

        public static string[] GetMultiChoices(string propertyName)
        {
            switch (propertyName)
            {
                case "difficulty":
                    return new string[] { "Peaceful", "Easy", "Normal", "Hard" };

                case "gamemode":
                    return new string[] { "Survival", "Creative", "Adventure", "Spectator" };

                default:
                    throw new InvalidOperationException("There are no multiple choices for this property");
            }
        }

        #endregion

        #region Encode

        /// <summary>
        /// Encodes the current properties as a server.properties file
        /// </summary>
        /// <returns>The encoded value</returns>
        public string Encode()
        {
            var sb = new StringBuilder();

            // append header and date
            sb.AppendLine(header);
            sb.Append("#");
            sb.AppendLine(UnixDate.GetUniversalDate());

            // append every property
            foreach (var property in Properties)
                sb.AppendLine(property.ToString());

            // extra line
            sb.AppendLine();

            return sb.ToString();
        }

        #endregion
    }
}
