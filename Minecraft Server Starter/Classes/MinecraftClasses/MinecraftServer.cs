/// <copyright file="MinecraftServer.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Class used to interop with a Minecraft server</summary>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Minecraft_Server_Starter
{
    public enum Status { Opening, Open, Closing, Closed }
    
    public class MinecraftServer
    {
        #region Private fields

        #region String analysis regexs

        // two possible formats for time:
        //   yyyy-MM-dd hh:mm:ss
        //   [hh:mm:ss]
        Regex timeRegex = new Regex(@"(?:\[\d{2}:\d{2}:\d{2}\])|(?:\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})", RegexOptions.Compiled);
        // three possible formats for type:
        //   [(INFO|WARN|ERROR)]
        //   [Server thread/(INFO|WARN|ERROR)]
        //   [Server Shutdown Thread/INFO]
        Regex typeRegex = new Regex(@"\[(?:Server(?: Shutdown)? (?:t|T)hread\/)?(INFO|WARN|WARNING|ERROR)\]", RegexOptions.Compiled);

        // only one format: Done (d...
        Regex loadedRegex = new Regex(@"Done \(\d", RegexOptions.Compiled);
        
        Regex commandBlockRegex = new Regex(@"\[(.+?)\]", RegexOptions.Compiled);
        const string serverName = "Server"; // avoid these on command blocks

        Regex playerJoinedRegex = new Regex(@"(\w+) ?\[\/", RegexOptions.Compiled);
        Regex playerLeftRegex = new Regex(@"(\w+) lost connection", RegexOptions.Compiled);

        // spigot format
        Regex spigotTimeTypeRegex = new Regex(@"\[(\d{2}:\d{2}:\d{2}) (INFO|WARN|ERROR)\]: ", RegexOptions.Compiled);

        #endregion

        #region Holders

        // status
        Status _ServerStatus = Status.Closed;

        // the process holding the server
        Process server;
        // the streamwriter to the server process
        StreamWriter input;

        List<string> onlinePlayers = new List<string>();

        #endregion

        #endregion

        #region Public properties

        // returns the current server status
        public Status ServerStatus
        {
            get { return _ServerStatus; }
            set
            {
                var lstValue = _ServerStatus;
                _ServerStatus = value;

                if (value == Status.Opening && value != lstValue)
                    onServerMessage(Res.GetStr("initializingServer"));

                if (value == Status.Closed && value != lstValue)
                    onServerMessage(Res.GetStr("serverHasClosed"));

                onServerStatusChanged(value);
            }
        }

        // selected server
        public Server Server { get; private set; }

        // online players
        public List<string> OnlinePlayers { get { return new List<string>(onlinePlayers); } }

        // ignore commands block
        public bool IgnoreCommandBlocks {
            get { return Settings.GetValue<bool>("ignoreCommandsBlock"); }
            set { Settings.SetValue<bool>("ignoreCommandsBlock", value); }
        }

        #endregion

        #region Events

        // occurs when the server changes it's status
        public delegate void ServerStatusChangedEventHandler(Status status);
        public event ServerStatusChangedEventHandler ServerStatusChanged;
        void onServerStatusChanged(Status status)
        {
            if (ServerStatusChanged != null)
                ServerStatusChanged(status);
        }

        // occurs when the server's output receives content
        public delegate void ServerMessageEventHandler(string time, string type, string typeName, string message);
        public event ServerMessageEventHandler ServerMessage;
        void onServerMessage(string msg)
        {
            if (ServerMessage != null)
            {
                var tuple = analyseMessage(msg);
                if (tuple != null) // might be invalid input, such as command blocks when they're disabled
                    ServerMessage(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
            }
        }

        // occurs when an analysed message represents a player joining/leaving
        public delegate void PlayerEventHandler(bool joined, string player);
        public event PlayerEventHandler Player;
        void onPlayer(bool joined, string player)
        {
            if (joined) onlinePlayers.Add(player);
            else onlinePlayers.Remove(player);

            if (Player != null)
                Player(joined, player);
        }

        #endregion

        #region Constructors

        public MinecraftServer(Server server)
        {
            Server = server;
            EULA.CreateEULA(Server.Location);
        }

        #endregion

        #region Current server management

        // start the server
        public void Start()
        {
            if (ServerStatus != Status.Closed)
                return; // if it's not closed, we can't "unclose" it because it already is

            // notify server is opening
            ServerStatus = Status.Opening;

            Server.Use(); // update last use date

            // create a new server process
            server = Process.Start(getPsi());

            // attach event handlers
            server.OutputDataReceived += server_OutputDataReceived;
            server.ErrorDataReceived += server_OutputDataReceived;
            server.Exited += server_Exited;

            // set streamwriter to it's stdin
            input = server.StandardInput;

            // begin read async
            server.BeginOutputReadLine();
            server.BeginErrorReadLine();
        }

        // kill the server
        public void Kill()
        {
            if (ServerStatus == Status.Closed)
                return; // already closed, return

            // stop receiving poop
            server.OutputDataReceived -= server_OutputDataReceived;
            server.ErrorDataReceived -= server_OutputDataReceived;
            server.Exited -= server_Exited;

            // DIE, DIE!! MWAHAHAHA... right
            server.Kill();
            ServerStatus = Status.Closed;
        }

        #endregion

        #region Commands

        // send a command
        public void SendCommand(string command) { input.WriteLine(command); }

        // stop (saving first)
        public void Stop() { SendCommand("stop"); }

        // say something!
        public void Say(string msg) { SendCommand("say " + msg); }

        // kick a noob
        public void Kick(string player) { SendCommand("kick " + player); }

        // ban a very noob
        public void Ban(string player) { SendCommand("ban " + player); }

        // pardon (unban) an innocent noob
        public void Pardon(string player) { SendCommand("pardon " + player); }

        // op a pro player
        public void Op(string player) { SendCommand("op " + player); }

        // deop a traitor
        public void Deop(string player) { SendCommand("deop " + player); }

        // toggle rain
        public void ToggleRain() { SendCommand("toggledownfall"); }

        // invoke the sun
        public void SetDay() { SendCommand("time set 0"); }

        // invoke the moon
        public void SetNight() { SendCommand("time set 14000"); }

        // save the world
        public void Save() { SendCommand("save-all"); }

        #endregion

        #region Server output handling

        // the server wrote something to it's stdout
        void server_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                ServerStatus = Status.Closed;
                return;
            }
            else
                onServerMessage(e.Data);
        }

        // oops, someone died, not me (._ . ")
        void server_Exited(object sender, EventArgs e)
        {
            ServerStatus = Status.Closed;
        }

        #endregion

        #region Server messages analysis

        // return a tuple formed by <time, type, typeName, message>
        Tuple<string, string, string, string> analyseMessage(string msg)
        {
            // initialize result variables
            string time, type, typeName, message;
            time = type = typeName = string.Empty;

            // find the most basic matches
            var timeMatch = timeRegex.Match(msg);
            var typeMatch = typeRegex.Match(msg);

            // check index to ensure no player is trolling us
            if (timeMatch.Success && timeMatch.Index == 0)
            {
                time = timeMatch.Value + " ";

                // if typeMatch is right after timeMatch, success
                if (typeMatch.Success && typeMatch.Index == timeMatch.Length + 1)
                {
                    type = typeMatch.Value;
                    typeName = typeMatch.Groups[1].Value;
                    message = msg.Replace(timeMatch.Value, "").Replace(typeMatch.Value, "").Trim();

                    var addLength = 2; // ": ".Length
                    if (!message.StartsWith(": "))
                    {
                        addLength = 1; // the server message is closer than expected (old Minecraft version)
                        message = ": " + message;
                    }

                    // to ensure it isn't a player trolling us!
                    var matchIndex = typeMatch.Index + typeMatch.Length + addLength;

                    if (!checkCommonRegexs(msg, matchIndex))
                        return null;
                }
                else // if no match found, only set a plain message
                    message = msg.Replace(timeMatch.Value, "").Trim();
            }
            else // if absolutely no match found, it might be spigot
            {
                var spigotTimeTypeMatch = spigotTimeTypeRegex.Match(msg);
                if (spigotTimeTypeMatch.Success && spigotTimeTypeMatch.Index == 0) // yes! spigot!
                {
                    time = "[" + spigotTimeTypeMatch.Groups[1].Value + "]"; // time
                    typeName = spigotTimeTypeMatch.Groups[2].Value; // type
                    type = " [" + typeName + "]: ";
                    message = msg.Replace(spigotTimeTypeMatch.Value, "");

                    if (!checkCommonRegexs(msg, spigotTimeTypeMatch.Length))
                        return null;
                }
                else
                {
                    // not even spigot, only set a plain message
                    message = msg;
                }
            }

            return new Tuple<string, string, string, string>(time, type, typeName, message);
        }

        // check common (normal server or spigot) matches, return true if output is ok
        bool checkCommonRegexs(string msg, int matchIndex)
        {
            // if server isn't open yet...
            if (ServerStatus != Status.Open)
            {
                // check if it's a "Done!" message (server is loaded)
                var loadedMatch = loadedRegex.Match(msg, matchIndex);
                if (loadedMatch.Success && loadedMatch.Index == matchIndex)
                {
                    ServerStatus = Status.Open;
                    server.PriorityClass = (ProcessPriorityClass)Settings.GetValue<int>("priority");
                }
            }
            else
            {
                if (IgnoreCommandBlocks)
                {
                    // check if it's a "[@: ...]" message (command block output)
                    var commandBlockMatch = commandBlockRegex.Match(msg, matchIndex);
                    if (commandBlockMatch.Success &&
                        commandBlockMatch.Index == matchIndex &&
                        commandBlockMatch.Groups[1].Value != serverName)
                    {
                        return false; // output is not ok
                    }
                }

                // match for player joining/leaving
                var playerJoinedMatch = playerJoinedRegex.Match(msg, matchIndex);
                var playerLeftMatch = playerLeftRegex.Match(msg, matchIndex);

                // we may need to fire a player status
                if (playerJoinedMatch.Success && playerJoinedMatch.Index == matchIndex)
                    onPlayer(true, playerJoinedMatch.Groups[1].Value);

                else if (playerLeftMatch.Success && playerLeftMatch.Index == matchIndex)
                    onPlayer(false, playerLeftMatch.Groups[1].Value);
            }
            return true; // output is ok!
        }

        #endregion

        #region Private methods

        // get the process start info to start the selected server
        ProcessStartInfo getPsi()
        {
            var arguments = new StringBuilder();
            arguments.Append("-Xms");
            arguments.Append(Settings.GetValue<int>("minRam"));
            arguments.Append("M -Xmx");
            arguments.Append(Settings.GetValue<int>("maxRam"));
            arguments.Append("M -jar \"");
            arguments.Append(Server.ServerJar);
            arguments.Append("\" nogui");

            return new ProcessStartInfo
            {
                FileName = Settings.GetValue<string>("javaPath"),
                Arguments = arguments.ToString(),
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Server.Location
            };
        }

        #endregion
    }
}
