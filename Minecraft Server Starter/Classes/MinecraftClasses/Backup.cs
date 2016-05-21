/// <copyright file="Backup.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Class representing a backup</summary>

using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ExtensionMethods;
using System.Collections.Generic;

using SProperties = Minecraft_Server_Starter.ServerProperties;

namespace Minecraft_Server_Starter
{
    [DataContract]
    public class Backup
    {
        #region Constant fields

        // the extension used to save Backup files
        const string fileExtension = ".info";

        // where the backups are stored
        static string backupsFolder =>
            Path.Combine(Settings.GetValue<string>("mssFolder"), "Backups");

        // the base location for this backup
        string baseLocation => Path.Combine(backupsFolder, ID);

        #endregion

        #region Public properties

        [DataMember]
        public Server Server { get; set; }

        public string ID { get { return creationDate.ToString(); } }
        public string DisplayName
        {
            get
            {
                return Server.Name + " " +
                    CreationDate.ToShortDateString() + " " +
                    CreationDate.ToLongTimeString();
            }
        }

        public string Size
            => new FileInfo(baseLocation + ".zip").Length.ToFileSizeString();

        [DataMember]
        public bool Worlds { get; set; }
        [DataMember]
        public bool ServerProperties { get; set; }
        [DataMember]
        public bool WhiteList { get; set; }
        [DataMember]
        public bool Ops { get; set; }
        [DataMember]
        public bool Banned { get; set; }
        [DataMember]
        public bool Logs { get; set; }

        [DataMember]
        public bool Everything { get; set; }

        public DateTime CreationDate { get { return UnixDate.UnixTimeToDateTime(creationDate); } }
        [DataMember]
        long creationDate { get; set; }

        [DataMember]
        string worldsName { get; set; }

        #endregion

        #region Constructors

        public Backup(Server server) : this(server,
            true, true, true, true, true, true, true)
        { }

        public Backup(Server server, bool worlds, bool serverProperties, bool whiteList,
            bool ops, bool banned, bool logs, bool everything)
        {
            Server = server;

            Worlds = worlds;
            ServerProperties = serverProperties;
            WhiteList = whiteList;
            Ops = ops;
            Banned = banned;
            Logs = logs;

            Everything = everything;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Sets the creation date to now
        /// </summary>
        public void SetCreation()
        {
            creationDate = UnixDate.DateTimeToUnixTime(DateTime.Now);
        }

        /// <summary>
        /// Generates a new backup from an old backup
        /// </summary>
        /// <param name="backup">The old backup without the creation date set</param>
        /// <returns>The backup with the creation dae set</returns>
        public static Backup Generate(Backup backup)
        {
            backup.creationDate = UnixDate.DateTimeToUnixTime(DateTime.Now);
            return backup;
        }

        #endregion

        #region Private methods

        // what files do we need to copy from the given folder with the current settings?
        IEnumerable<string> GetRequiredCopyFiles(string folder)
        {
            var propertiesPath = Path.Combine(folder, SProperties.PropertiesName);
            string tmpFile;

            if (ServerProperties)
            {
                yield return propertiesPath;
            }
            if (WhiteList)
            {
                //                                new server        old server
                tmpFile = GetIfExistsFile(folder, "whitelist.json", "white-list.txt");
                if (!string.IsNullOrEmpty(tmpFile))
                    yield return tmpFile;
            }
            if (Ops)
            {
                //                                new server  old server
                tmpFile = GetIfExistsFile(folder, "ops.json", "ops.txt");
                if (!string.IsNullOrEmpty(tmpFile))
                    yield return tmpFile;
            }
            if (Banned)
            {
                //                                new server         old server
                tmpFile = GetIfExistsFile(folder, "banned-ips.json", "banned-ips.txt");
                if (!string.IsNullOrEmpty(tmpFile))
                    yield return tmpFile;

                //                                new server             old server
                tmpFile = GetIfExistsFile(folder, "banned-players.json", "banned-players.txt");
                if (!string.IsNullOrEmpty(tmpFile))
                    yield return tmpFile;
            }
            if (Logs)
            {
                //                                old server
                tmpFile = GetIfExistsFile(folder, "server.log");
                if (!string.IsNullOrEmpty(tmpFile))
                    yield return tmpFile;
            }
        }

        // what directories do we need to copy from the given folder with the current settings?
        IEnumerable<string> GetRequiredCopyDirectories(string folder)
        {
            var propertiesPath = Path.Combine(folder, SProperties.PropertiesName);
            var properties = SProperties.FromFile(propertiesPath);

            if (Worlds)
                yield return Path.Combine(folder, (worldsName = properties["level-name"].Value));

            if (Logs)
            {
                //                             new server
                var dir = Path.Combine(folder, "logs");
                if (Directory.Exists(dir))
                    yield return dir;
            }
        }

        // gets the first existing file from one of the given possibilities
        static string GetIfExistsFile(string folder, params string[] possibilities)
        {
            foreach (var possibility in possibilities)
                if (File.Exists(Path.Combine(folder, possibility)))
                    return Path.Combine(folder, possibility);

            return null;
        }

        // saves the current backup to the specified file
        void Save(string file) => Serializer.Serialize(this, file);

        // loads a backup from the specified file
        static Backup Load(string file) => Serializer.Deserialize<Backup>(file);

        #endregion

        #region Saving

        /// <summary>
        /// Saves the backup, determining whether the server is running (to backup a copy instead the original) or not
        /// </summary>
        /// <param name="isServerRunning">If the server is running, a temporary copy of itself will be created so no errors are thrown</param>
        /// <returns>True if the operation was successful</returns>
        public async Task<bool> Save(bool isServerRunning)
        {
            SetCreation();
            bool success = true;

            await Task.Factory.StartNew(() =>
            {
                var zipLocation = baseLocation + ".zip";
                var bakLocation = baseLocation + fileExtension;

                try
                {
                    if (!Directory.Exists(Path.GetDirectoryName(baseLocation)))
                        Directory.CreateDirectory(Path.GetDirectoryName(baseLocation));
                    
                    var folder = Server.Location;
                    var folderName = Path.GetFileName(folder);

                    // so we can perform the backup even with server running
                    var tmpFolder = Path.Combine(Path.GetTempPath(),
                        Path.GetFileNameWithoutExtension(Path.GetTempFileName()), folderName);

                    if (Everything)
                    {
                        if (isServerRunning) // if the server is open, copy the server and then perform the backup
                        {
                            new DirectoryInfo(folder).CopyDirectory(new DirectoryInfo(tmpFolder));

                            ZipFile.CreateFromDirectory(tmpFolder, zipLocation);

                            try { Directory.Delete(tmpFolder, true); }
                            catch { };
                        }
                        else // otherwise perform the backup directly
                        {
                            ZipFile.CreateFromDirectory(folder, zipLocation);
                        }
                    }
                    else
                    {
                        using (ZipArchive zip = ZipFile.Open(zipLocation, ZipArchiveMode.Create))
                        {
                            if (isServerRunning)
                            {
                                Directory.CreateDirectory(tmpFolder);

                                // copy all the files and directories to a temporary location before adding them to the zip file
                                foreach (var cfile in GetRequiredCopyFiles(folder))
                                {
                                    var tmpFile = Path.Combine(tmpFolder, Path.GetFileName(cfile));
                                    File.Copy(cfile, tmpFile);
                                    zip.AddFile(tmpFile);
                                }

                                foreach (var cdir in GetRequiredCopyDirectories(folder))
                                {
                                    var tmpDir = Path.Combine(tmpFolder, Path.GetFileName(cdir));
                                    new DirectoryInfo(cdir).CopyDirectory(new DirectoryInfo(tmpDir));
                                    zip.AddDirectory(tmpDir);
                                }

                                // clear the temporary directory
                                try { Directory.Delete(tmpFolder, true); }
                                catch { };
                            }
                            else
                            {
                                // if the server is closed, directly add all the files and directories to the zip
                                foreach (var cfile in GetRequiredCopyFiles(folder))
                                    zip.AddFile(cfile);

                                foreach (var cdir in GetRequiredCopyDirectories(folder))
                                    zip.AddDirectory(cdir);
                            }
                        }
                    }

                    // if we got this far, save the backup information
                    Save(baseLocation + fileExtension);
                }
                catch // perhaps the server log is being used, or it doesn't exist; clear failed files
                {
                    if (File.Exists(baseLocation + ".zip"))
                        try { File.Delete(baseLocation + ".zip"); }
                        catch { }

                    success = false;
                }
            });

            return success;
        }

        #endregion

        #region Loading

        /// <summary>
        /// Loads a backup
        /// </summary>
        /// <param name="server">For which server?</param>
        /// <param name="worlds">Should the worlds be loaded?</param>
        /// <param name="serverProperties">Should the server properties be loaded?</param>
        /// <param name="whiteList">Should the white list be loaded?</param>
        /// <param name="ops">Should the op list be loaded?</param>
        /// <param name="banned">Should the banned players list be loaded?</param>
        /// <param name="logs">Should the logs be loaded?</param>
        /// <param name="everything">Should just everything be loaded?</param>
        public void Load(Server server, bool worlds, bool serverProperties, bool whiteList,
            bool ops, bool banned, bool logs, bool everything)
        {
            // if all the options match, we want to extract everything from the backup
            bool all = Worlds == worlds &&
                ServerProperties == serverProperties &&
                WhiteList == whiteList &&
                Ops == ops &&
                Banned == banned &&
                Logs == logs;

            using (ZipArchive zip = ZipFile.Open(baseLocation + ".zip", ZipArchiveMode.Read))
            {
                if (all)
                    zip.ExtractToDirectory(server.Location, true);

                // else, extract only those options which we want
                else
                {
                    foreach (var entry in zip.Entries)
                    {
                        var entryName = entry.FullName.Contains("/") ?
                            entry.FullName.Substring(0, entry.FullName.IndexOf('/') + 1) :
                            entry.FullName;

                        if (
                            (worlds && entryName == worldsName + "/") ||
                            (logs && (entryName == "logs/" || entryName == "server.log")) ||
                            (serverProperties && entryName == "server.properties") ||
                            (whiteList && (entryName == "whitelist.json" || entryName == "white-list.txt")) ||
                            (ops && (entryName == "ops.json" || entryName == "ops.txt")) ||
                            (banned && (entryName == "banned-ips.json" || entryName == "banned-ips.txt" ||
                            entryName == "banned-players.json" || entryName == "banned-players.txt")))
                        {
                            entry.ExtractToFileSafe(Path.Combine(server.Location, entry.FullName), true);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves a backup given its id
        /// </summary>
        /// <param name="id">The id of the backup to retrieve</param>
        /// <returns>The retrieved backup</returns>
        public static Backup GetBackup(string id)
        {
            var file = Path.Combine(backupsFolder, id + fileExtension);
            if (File.Exists(file))
                return Load(file);

            return null;
        }

        /// <summary>
        /// Gets all the available backups
        /// </summary>
        /// <param name="prioritySortingName">What backup name should be shown first?</param>
        /// <returns>All the saved backups</returns>
        public static List<Backup> GetBackups(string prioritySortingName)
        {
            try
            {
                if (Directory.Exists(backupsFolder))
                {
                    var files = Directory.GetFiles(backupsFolder, "*" + fileExtension);
                    var backups = new List<Backup>(files.Length);
                    foreach (var file in files)
                        backups.Add(Load(file));

                    backups.Sort(new BackupSort(prioritySortingName));

                    return backups;
                }

                Directory.CreateDirectory(backupsFolder);
            }
            catch { }

            return new List<Backup>();
        }

        #endregion

        #region Deleting

        public bool Delete()
        {
            if (File.Exists(baseLocation + fileExtension))
                try { File.Delete(baseLocation + fileExtension); }
                catch { return false; }

            if (File.Exists(baseLocation + ".zip"))
                try { File.Delete(baseLocation + ".zip"); }
                catch { return false; }

            return true;
        }

        #endregion
    }
}
