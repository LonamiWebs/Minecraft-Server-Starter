/// <copyright file="MinecraftVersions.cs" company="LonamiWebs">
///   Copyright (c) $year$ All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>february 2016</date>
/// <summary>Classes determining the Minecraft version number plus the download URL</summary>

using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Minecraft_Server_Starter
{
    /// <summary>
    /// Minecraft versions, latest and all the available
    /// </summary>
    [DataContract]
    public class MinecraftVersions
    {
        #region Constant fields

        // "__comment": "This URL is being phased out! Please update your scripts to check https://launchermeta.mojang.com/mc/game/version_manifest.json instead. Thanks <3 —Dinnerbone",
        //const string versionsUrl = "https://www.npmjs.com/package/minecraft-versions";

        const string versionsUrl = "https://launchermeta.mojang.com/mc/game/version_manifest.json";

        #endregion

        #region Properties

        /// <summary>
        /// Latest versions IDs (names)
        /// </summary>
        [DataMember(Name = "latest")]
        public Latest Latest { get; set; }

        /// <summary>
        /// All available versions
        /// </summary>
        [DataMember(Name = "versions")]
        public MinecraftVersion[] Versions { get; set; }

        #endregion

        #region JSON

        public string ToJSON()
        {
            using (var ms = new MemoryStream())
            using (var sr = new StreamReader(ms))
            {
                var serializer = new DataContractJsonSerializer(typeof(MinecraftVersions));
                serializer.WriteObject(ms, this);

                ms.Position = 0;
                return sr.ReadToEnd();
            }
        }

        public static MinecraftVersions FromJSON(string json)
        {
            using (Stream stream = new MemoryStream())
            {
                byte[] data = Encoding.UTF8.GetBytes(json);
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                var deserializer = new DataContractJsonSerializer(typeof(MinecraftVersions));

                return (MinecraftVersions)deserializer.ReadObject(stream);
            }
        }

        #endregion

        #region Get versions

        /// <summary>
        /// Gets all the available Minecraft versions. Requires an internet connection
        /// </summary>
        /// <returns>All the Minecraft versions</returns>
        public static async Task<MinecraftVersions> GetLatests()
        {
            using (var wc = new WebClient())
                return FromJSON(await wc.DownloadStringTaskAsync(versionsUrl));
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return $"{Versions.Length} version(s); {Latest}";
        }

        #endregion

        #region Comparision

        public static async Task<int> Compare(string versionId1, string versionId2)
        {
            return Compare(versionId1, versionId2, await GetLatests());
        }

        /// <summary>
        /// Determines which version is greater, if left (-1) or right (+1). Returns 0 if unknown or equal
        /// </summary>
        public static int Compare(string leftId, string rightId, MinecraftVersions versions)
        {
            bool leftEquals = false;
            bool rightEquals = false;

            for (int i = 0; i < versions.Versions.Length; i++)
            {
                leftEquals = versions.Versions[i].ID.Equals(leftId);
                rightEquals = versions.Versions[i].ID.Equals(rightId);

                if (leftEquals && rightEquals)
                    return 0;

                if (leftEquals)
                    return -1;

                if (rightEquals)
                    return 1;
            }

            return 0;
        }

        #endregion
    }

    /// <summary>
    /// Latest Minecraft version snapshot and release
    /// </summary>
    [DataContract]
    public class Latest
    {
        /// <summary>
        /// Latest snapshot version
        /// </summary>
        [DataMember(Name = "snapshot")]
        public string Snapshot { get; set; }

        /// <summary>
        /// Latest release version
        /// </summary>
        [DataMember(Name = "release")]
        public string Release { get; set; }

        public override string ToString()
        {
            return $"Latest snapshot: {Snapshot}; Latest release: {Release}";
        }
    }

    /// <summary>
    /// Minecraft version
    /// </summary>
    [DataContract]
    public class MinecraftVersion
    {
        #region Constant fields

        // base minecraft server urls
        const string baseServerUrl = "https://s3.amazonaws.com/Minecraft.Download/versions/{0}/minecraft_server.{0}.jar";
        const string baseClientUrl = "https://s3.amazonaws.com/Minecraft.Download/versions/{0}/{0}.jar";

        
        // old minecraft server urls
        const string old12Url = "http://assets.minecraft.net/1_2/minecraft_server.jar";
        const string old10Url = "https://s3.amazonaws.com/MinecraftDownload/launcher/minecraft_server.jar";

        #endregion

        #region Properties

        string _ID;
        /// <summary>
        /// The ID of the Minecraft version (a.k.a. version name)
        /// </summary>
        [DataMember(Name = "id")]
        public string ID
        {
            get { return _ID; }
            set
            {
                _ID = value;
                // server
                switch (_ID)
                {
                    case "1.2.4":
                    case "1.2.3":
                    case "1.2.2":
                    case "1.2.1":
                        ServerUrl = old12Url; break;

                    case "1.0":
                        ServerUrl = old10Url; break;

                    default:
                        if ( // if id starts with any of these
                            _ID[0] == 'a' || // alpha
                            _ID[0] == 'b' || // beta
                            _ID[0] == 'c' || // old_alpha
                            _ID[0] == 'r' || // old_alpha
                            _ID[0] == 'i')   // old_alpha
                            break; // do nothing

                        ServerUrl = string.Format(baseServerUrl, _ID); break;
                }
                // client
                switch (_ID)
                {
                    default: // always works
                        ClientUrl = string.Format(baseClientUrl, _ID);
                        break;
                }
            }
        }

        /// <summary>
        /// When was this version released?
        /// </summary>
        [DataMember(Name = "releaseTime")]
        public string ReleaseTime { get; set; }

        /// <summary>
        /// Version time
        /// </summary>
        [DataMember(Name = "time")]
        public string Time { get; set; }

        /// <summary>
        /// Version type: "release", "snapshot", "old_beta" or "old_alpha"
        /// </summary>
        [DataMember(Name = "type")]
        public string Type { get; set; }
        
        /// <summary>
        /// Url pointing to a .json file containing more information of this version
        /// </summary>
        [DataMember(Name = "url")]
        public string Url { get; set; }

        /// <summary>
        /// Url pointing to the minecraft_server.jar file corresponding to this version. It can be null
        /// </summary>
        public string ServerUrl { get; set; }
        
        /// <summary>
        /// Url pointing to the client .jar file corresponding to this version
        /// </summary>
        public string ClientUrl { get; set; }

        #endregion

        #region Constructors

        public MinecraftVersion() { }

        public MinecraftVersion(string version)
        {
            ID = version;

            switch(ID[0])
            {
                // alpha
                case 'a': Type = "alpha"; break;

                // beta or old_beta
                case 'b': Type = "beta"; break;

                // old_alpha
                case 'c':
                case 'r':
                case 'i': Type = "old_alpha";  break;

                // release or snapshot
                default: Type = ID.Contains("pre") || ID.Contains("w") ? "snapshot" : "release";
                    break;
            }
        }

        #endregion

        #region Server downloading and caching

        const string serverName = "minecraft_server.{0}.jar";
        static string cacheFolder => Path.Combine(Settings.GetValue<string>("mssFolder"), "Cache");

        /// <summary>
        /// Copy a cached minecraft_server.jar file to the desired location.
        /// If it doesn't exist, it will be downloaded
        /// </summary>
        /// <param name="copyTo">Where the minecraft_server.jar will be copied</param>
        /// <param name="progress">A progress to keep track of the current download</param>
        /// <param name="cts">A cancellation token to cancel the file from being downloaded</param>
        /// <returns>True if the operation was successful</returns>
        public async Task<bool> CopyServer(string copyTo,
            IProgress<DownloadProgressChangedEventArgs> progress = null, CancellationTokenSource cts = null)
        {
            var cached = Path.Combine(cacheFolder, string.Format(serverName, ID));

            try
            {
                if (!File.Exists(cached))
                {
                    if (!Directory.Exists(cacheFolder))
                        Directory.CreateDirectory(cacheFolder);

                    using (var wc = new WebClient())
                    {
                        if (progress != null) // update progress
                            wc.DownloadProgressChanged += (s, e) => progress.Report(e);

                        if (cts != null) // set cancellation token
                            cts.Token.Register(wc.CancelAsync);

                        await wc.DownloadFileTaskAsync(ServerUrl, cached);
                    }
                }

                CopyServer(cached, copyTo);
                return true;
            }
            catch
            {
                if (cts.IsCancellationRequested)
                    try { File.Delete(cached); } catch { }

                return false;
            }
        }

        /// <summary>
        /// Copies a minecraft_server.jar file to the desired location, performing all the required checks
        /// </summary>
        /// <param name="file">The original server file</param>
        /// <param name="copyTo">The destination</param>
        public static void CopyServer(string file, string copyTo)
        {
            //var cache = Path.Combine(cacheFolder, Path.GetFileNameWithoutExtension(file)) +
            //    "_" + UnixDate.DateTimeToUnixTime(DateTime.Now) + ".jar";
            
            if (!Directory.Exists(Path.GetDirectoryName(copyTo)))
                Directory.CreateDirectory(Path.GetDirectoryName(copyTo));

            File.Copy(file, copyTo, true);
        }

        #endregion

        #region Cache management

        public static string GetSizeUsedByCache()
        {
            var sizes = new string[] { "B", "KB", "MB", "GB", "TB" };

            double size = 0d;
            foreach (var file in Directory.EnumerateFiles(cacheFolder))
                size += new FileInfo(file).Length;

            int sizeIndex = 0;
            while (size > 1024)
            {
                ++sizeIndex;
                size /= 1024;
            }

            return $"{size.ToString("0.##")}{sizes[sizeIndex]}";
        }

        public static void ClearCache()
        {
            foreach (var file in Directory.EnumerateFiles(cacheFolder))
                try { File.Delete(file); }
                catch { }
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return $"{ID} {Type}";
        }

        #endregion
    }
}
