/// <copyright file="ServerJar.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>01/04/2016</date>
/// <summary>Class used to determine a Minecraft server jar version</summary>

using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Minecraft_Server_Starter
{
    public static class ServerJar
    {
        /// <summary>
        /// Determines the minecraft_server.jar version. Returns null if the version could not be determined
        /// </summary>
        /// <param name="serverJar">The minecraft_server.jar location</param>
        /// <returns>The version of the server</returns>
        public static async Task<string> GetServerVersion(string serverJar)
        {
            string version = null;

            if (!File.Exists(serverJar))
                return Res.GetStr("unexisting");

            await Task.Run(() =>
            {
                using (var zip = ZipFile.Open(serverJar, ZipArchiveMode.Read))
                {
                    foreach (var entry in zip.Entries)
                    {
                        // skip these files, as they don't contain the version string
                        if (entry.Name.Contains('$') || !entry.Name.EndsWith(".class"))
                            continue;

                        // open the compressed stream
                        using (var stream = entry.Open())
                        {
                            // find this string in the stream
                            var idx = (int)indexOfInStream(stream, "Starting minecraft server version ");
                            if (idx < 0)
                                continue;

                            using (var sr = new StreamReader(stream))
                            {
                                // read the following characters to get the actual version characters
                                var buffer = new char[32];
                                sr.Read(buffer, 0, buffer.Length);

                                int i = 0;
                                for (; i < buffer.Length; i++)
                                {
                                    // if character is less than the first readable character (space), stop
                                    if (buffer[i] < 32)
                                        break;
                                }

                                // return a string with the version
                                version = new string(buffer, 0, i);
                            }
                        }

                        if (!string.IsNullOrEmpty(version))
                            break;
                    }
                }
            });

            return version ?? Res.GetStr("unknown");
        }

        // keep in mind this moves the stream position
        static long indexOfInStream(Stream stream, string value)
        {
            // start here (since we want the start of the string, instead of substracting it after we can do it now)
            int index = -value.Length;
            for (int i = 0; i < value.Length;)
            {
                // try reading the next byte
                var b = stream.ReadByte();
                ++index;

                if (b < 0) // end of stream
                    return -1;

                // convert the byte to a character
                var c = (char)b;

                if (c == value[i]) // if the character equals to the next character in the string to search for
                    ++i; // increment i to match the next character
                else // else, start again
                    i = 0;
            }

            return index;
        }
    }
}
