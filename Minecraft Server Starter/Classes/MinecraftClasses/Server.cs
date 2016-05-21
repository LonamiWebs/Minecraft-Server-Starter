/// <copyright file="Server.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>
/// A class representing a Minecraft server file.
/// This class stores the name, description and .jar location of a Minecraft server
/// </summary>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Minecraft_Server_Starter
{
    [DataContract]
    public class Server
    {
        #region Constant fields

        // the base name for all the Minecraft server jar files
        const string jarBaseName = "minecraft_server.jar";

        // the extension used to save Server files
        const string fileExtension = ".sv";

        // where the servers are saved
        static string serversFolder =>
            Path.Combine(Settings.GetValue<string>("mssFolder"), "Servers");

        #endregion

        #region Public properties

        /// <summary>
        /// The server name
        /// </summary>
        [DataMember]
        public string Name { get; set; }
        /// <summary>
        /// The server description
        /// </summary>
        [DataMember]
        public string Description { get; set; }
        /// <summary>
        /// The server .jar location
        /// </summary>
        [DataMember]
        public string ServerJar { get; set; }

        /// <summary>
        /// The folder containing the server
        /// </summary>
        public string Location => Path.GetDirectoryName(ServerJar);

        /// <summary>
        /// The server-icon.png path
        /// </summary>
        public string IconPath => Path.Combine(Location, "server-icon.png"); // TODO USE EVERYWHERE

        /// <summary>
        /// The server.properties path
        /// </summary>
        public string PropertiesPath => Path.Combine(Location, "server.properties");

        /// <summary>
        /// The server properties
        /// </summary>
        public ServerProperties Properties
        {
            get
            {
                ServerProperties result = null;

                try
                {
                    var propertiesPath = PropertiesPath;
                    if (File.Exists(propertiesPath))
                        result = ServerProperties.FromFile(propertiesPath);
                    else
                        File.WriteAllText(propertiesPath, (result = ServerProperties.Empty).Encode());
                }
                catch { }

                return result ?? ServerProperties.Empty;
            }
            set
            {
                File.WriteAllText(PropertiesPath, value.Encode());
            }
        }

        /// <summary>
        /// The creation date of the server
        /// </summary>
        public DateTime CreationDate { get { return UnixDate.UnixTimeToDateTime(creationDate); } }
        /// <summary>
        /// The date of when the server was last used
        /// </summary>
        public DateTime LastUseDate { get { return UnixDate.UnixTimeToDateTime(lastUseDate); } }

        /// <summary>
        /// An unique ID identifying this server
        /// </summary>
        public string ID { get { return creationDate.ToString(); } }

        #endregion

        #region Private fields

        // creation and last use date (in unix time)
        [DataMember]
        long creationDate;
        [DataMember]
        long lastUseDate;

        string serverFileLoc
            => Path.Combine(serversFolder, ID + fileExtension);

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new instance of a Server
        /// </summary>
        public Server()
        {
            creationDate = lastUseDate = UnixDate.DateTimeToUnixTime(DateTime.Now);
        }

        /// <summary>
        /// Creates a new instance of a Server given a name and a description. The minecraft_server.jar location is set automatically
        /// </summary>
        /// <param name="name">The name of the server</param>
        /// <param name="description">The description of the server</param>
        public Server(string name, string description) : this()
        {
            Name = name;
            Description = description;
            ServerJar = Path.Combine(serversFolder, ID, jarBaseName);
        }

        /// <summary>
        /// Creates a new instance of a Server given a name, a description and a minecraft_server.jar file
        /// </summary>
        /// <param name="name">The name of the server</param>
        /// <param name="description">The description of the server</param>
        /// <param name="jar">The minecraft_server.jar location</param>
        public Server(string name, string description, string jar) : this(name, description)
        {
            ServerJar = jar;
        }

        /// <summary>
        /// Create a new instance of a Server given an encoded file
        /// </summary>
        /// <param name="encodedFile">The encoded Server file</param>
        /// <returns>The decoded Server</returns>
        public static Server FromFile(string encodedFile)
        {
            return Decode(File.ReadAllText(encodedFile, Encoding.UTF8));
        }

        /// <summary>
        /// Create a new instance of a Server given an encoded file
        /// </summary>
        /// <param name="encodedFile">The encoded Server file</param>
        /// <returns>The decoded Server</returns>
        public static Server FromServerID(string serverId)
        {
            var file = Path.Combine(serversFolder, serverId + fileExtension);
            if (File.Exists(file))
                return Server.FromFile(file);

            return null;
        }

        #endregion

        #region Use and delete

        /// <summary>
        /// Use this server, updating it's last use date
        /// </summary>
        public void Use()
        {
            lastUseDate = UnixDate.DateTimeToUnixTime(DateTime.Now);
            Save();
        }

        /// <summary>
        /// Delete this server whilst keeping all the other files
        /// </summary>
        public bool Delete()
        {
            try
            {
                File.Delete(serverFileLoc);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Delete all the files related to this folder
        /// </summary>
        public bool DeleteAll()
        {
            if (!Delete())
                return false;

            try
            {
                Directory.Delete(Location, true);
                return true;
            }
            catch { return false; }
        }

        #endregion

        #region Saving

        /// <summary>
        /// Save the server info
        /// </summary>
        public void Save()
        {
            if (!Directory.Exists(Path.GetDirectoryName(serverFileLoc)))
                Directory.CreateDirectory(Path.GetDirectoryName(serverFileLoc));

            File.WriteAllText(serverFileLoc, Encode(), Encoding.UTF8);
        }

        #endregion

        #region Server properties

        /// <summary>
        /// Updates a server property in the server.properties file
        /// </summary>
        /// <param name="name">The property name</param>
        /// <param name="value">The property value</param>
        public void UpdateServersProperty(string name, string value)
        {
            var properties = Properties;
            properties[name].Value = value;
            File.WriteAllText(PropertiesPath, properties.Encode());
        }

        #endregion

        #region Servers listing

        /// <summary>
        /// Returns a list of all the saved servers
        /// </summary>
        /// <returns>The saved servers</returns>
        public static List<Server> GetServers()
        {
            if (!Directory.Exists(serversFolder))
            {
                Directory.CreateDirectory(serversFolder);
                return new List<Server>();
            }
            var files = Directory.GetFiles(serversFolder, "*" + fileExtension);
            var servers = new List<Server>(files.Length);
            foreach (var file in files)
                servers.Add(FromFile(file));

            return servers.OrderByDescending(sv => sv.LastUseDate).ToList();
        }

        #endregion

        #region Encode and decode

        // TOD MAYBE I CAN TO FILE DIRECTLY OR STH
        /// <summary>
        /// Encode the current server and return an encoded string of it
        /// </summary>
        /// <returns>The encoded server string</returns>
        public string Encode()
        {
            return Serializer.SerializeToString(this);
        }

        /// <summary>
        /// Decode the current server from it's encoded string
        /// </summary>
        /// <param name="str">The encoded server string</param>
        /// <returns>The decoded Server</returns>
        public static Server Decode(string str)
        {
            return Serializer.DeserializeFromString<Server>(str);
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Checks equality against another object
        /// </summary>
        /// <param name="obj">The object against which the server will be checked</param>
        /// <returns>True if they're equal</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Server)) return false;
            return this == (Server)obj;
        }

        /// <summary>
        /// Get the hash code of the server
        /// </summary>
        /// <returns>The hash code of the server</returns>
        public override int GetHashCode()
        {
            return creationDate.GetHashCode();
        }

        /// <summary>
        /// Checks equality against two servers
        /// </summary>
        /// <param name="a">First server</param>
        /// <param name="b">Second server</param>
        /// <returns>True if they're equal</returns>
        public static bool operator ==(Server a, Server b)
        {
            if (ReferenceEquals(a, b)) return true;

            if (((object)a == null) || ((object)b == null)) return false;

            return a.creationDate == b.creationDate;
        }

        /// <summary>
        /// Checks non equality against two servers
        /// </summary>
        /// <param name="a">First server</param>
        /// <param name="b">Second server</param>
        /// <returns>True if they're not equal</returns>
        public static bool operator !=(Server a, Server b)
        {
            return !(a == b);
        }

        #endregion
    }
}
