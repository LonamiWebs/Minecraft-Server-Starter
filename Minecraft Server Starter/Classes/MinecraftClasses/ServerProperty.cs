/// <copyright file="ServerProperty.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Class representing a server.properties property</summary>

using System;
using System.Collections.Generic;
using System.Globalization;
using ExtensionMethods;

namespace Minecraft_Server_Starter
{
    public class ServerProperty
    {
        #region Properties

        /// <summary>
        /// The original name of the property
        /// </summary>
        public string OriginalName { get; set; }

        /// <summary>
        /// The name of the property
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The value of the property
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// The description, if available, of the property
        /// </summary>
        public string Description
        {
            get
            {
                string description;
                if (!descriptions.TryGetValue(OriginalName, out description))
                    description = Res.GetStr("noDescriptionAvailable");

                return description;
            }
        }

        /// <summary>
        /// Is the property a boolean value?
        /// </summary>
        public bool IsBoolean => Value == "true" || Value == "false";

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize a new instance of this class
        /// </summary>
        /// <param name="oriName">The original name of the property</param>
        /// <param name="value">The default value of the property</param>
        public ServerProperty(string oriName, string value)
        {
            OriginalName = oriName;

            // "beautify" the name
            Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(oriName.Replace("-", " ").Replace("_", " "));
            Value = value;
        }

        #endregion

        #region Comparing

        /// <summary>
        /// Does the given value match with the name of the property?
        /// </summary>
        public bool MatchsName(string value)
        {
            return Name.Equals(value, StringComparison.OrdinalIgnoreCase) ||
                   OriginalName.Equals(value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether the property passes a search or not
        /// </summary>
        public bool PassesSearch(string search)
        {
            if (string.IsNullOrEmpty(search))
                return true;

            return
                Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                OriginalName.Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Constant fields

        // took from http://Minecraft.gamepedia.com/Server.properties
        static readonly Dictionary<string, string> descriptions =
            new Dictionary<string, string>
            {
                // allow-flight
                { "allow-flight", Res.GetStr("descAllowFlight") },

                // allow-nether
                { "allow-nether", Res.GetStr("descAllowNether") },

                //announce-player-achievements
                { "announce-player-achievements", Res.GetStr("descAnnounceAchievements") },

                //difficulty
                { "difficulty", Res.GetStr("descDifficulty") },

                //enable-query
                { "enable-query", Res.GetStr("descEnableQuery") },

                //enable-rcon
                { "enable-rcon", Res.GetStr("descEnableRcon") },

                //enable-command-block
                { "enable-command-block", Res.GetStr("descEnableCommandBlock") },

                //force-gamemode
                { "force-gamemode", Res.GetStr("descForceGamemode") },

                //gamemode
                { "gamemode", Res.GetStr("descGamemode") },

                //generate-structures
                { "generate-structures", Res.GetStr("descGenerateStructures") },

                //generator-settings
                { "generator-settings", Res.GetStr("descGeneratorSettings") },

                //hardcore
                { "hardcore", Res.GetStr("descHardcore") },

                //level-name
                { "level-name", Res.GetStr("descLevelName") },

                //level-seed
                { "level-seed", Res.GetStr("descLevelSeed") },

                //level-type
                { "level-type", Res.GetStr("descLevelType") },

                //max-build-height
                { "max-build-height", Res.GetStr("descMaxBuildHeight") },

                //max-players
                { "max-players", Res.GetStr("descMaxPlayers") },

                //max-tick-time
                { "max-tick-time", Res.GetStr("descMaxTickTime") },

                //max-world-size
                { "max-world-size", Res.GetStr("descMaxWorldSize") },

                //motd
                { "motd", Res.GetStr("descMotd") },

                //network-compression-threshold
                { "network-compression-threshold", Res.GetStr("descNetworkCompression") },

                //online-mode
                { "online-mode", Res.GetStr("descOnlineMode") },

                //op-permission-level
                { "op-permission-level", Res.GetStr("descOpPermissionLevel") },

                //player-idle-timeout
                { "player-idle-timeout", Res.GetStr("descPlayerIdleTimeout") },

                //pvp
                { "pvp", Res.GetStr("descPvp") },

                //query.port
                { "query.port", Res.GetStr("descQueryPort") },

                //rcon.password
                { "rcon.password", Res.GetStr("descRconPassword") },

                //rcon.port
                { "rcon.port", Res.GetStr("descRconPort") },

                //resource-pack
                { "resource-pack", Res.GetStr("descResourcePack") },

                //resource-pack-hash
                { "resource-pack-hash", Res.GetStr("descResourcePackHash") },

                //server-ip
                { "server-ip", Res.GetStr("descServerIp") },

                //server-port
                { "server-port", Res.GetStr("descServerPort") },

                //snooper-enabled
                { "snooper-enabled", Res.GetStr("descSnooper") },

                //spawn-animals
                { "spawn-animals", Res.GetStr("descSpawnAnimals") },

                //spawn-monsters
                { "spawn-monsters", Res.GetStr("descSpawnMonsters") },

                //spawn-npcs
                { "spawn-npcs", Res.GetStr("descSpawnNpcs") },

                //spawn-protection
                { "spawn-protection", Res.GetStr("descSpawnProtection") },

                //use-native-transport
                { "use-native-transport", Res.GetStr("descUseNativeSupport") },

                //view-distance
                { "view-distance", Res.GetStr("descViewDistance") },

                //white-list
                { "white-list", Res.GetStr("descWhiteList") } };

        #endregion

        #region Overrides

        /// <summary>
        /// Encode the property ready to be saved in a server.properties file
        /// </summary>
        /// <returns>The encoded value</returns>
        public override string ToString()
        {
            return OriginalName + "=" + Value;
        }

        #endregion

    }
}
