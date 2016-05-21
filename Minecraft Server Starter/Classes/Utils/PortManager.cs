/// <copyright file="PortManager.cs" company="LonamiWebs">
///   Copyright (c) 2016 All Rights Reserved
/// </copyright>
/// <author>Lonami Exo</author>
/// <date>February 2016</date>
/// <summary>Class used to manage ports</summary>

using Open.Nat;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Minecraft_Server_Starter
{
    // thanks to http://www.codeproject.com/Articles/807861/Open-NAT-A-NAT-Traversal-library-for-NET-and-Mono
    public static class PortManager
    {
        static NatDevice device;

        public static async Task<bool> Init()
        {
            if (device != null) return true;
            
            var nat = new NatDiscoverer();
            var cts = new CancellationTokenSource(5000);
            try
            {
                device = await nat.DiscoverDeviceAsync(PortMapper.Upnp, cts);
                return true;
            }
            catch
            {
                try
                {
                    device = await nat.DiscoverDeviceAsync(PortMapper.Pmp, cts);
                    return true;
                }
                catch { }
            }

            return false;
        }

        public static async Task<IPAddress> GetExternalIP()
        {
            return await device.GetExternalIPAsync();
        }

        public static async Task OpenPort(ushort port)
        {
            await device.CreatePortMapAsync(new Mapping(Protocol.Tcp, port, port, "Minecraft Server Starter"));
        }



        //var device = await nat.DiscoverDeviceAsync(PortMapper.Upnp, cts);

        //await device.CreatePortMapAsync(new Mapping(Protocol.Tcp, 1600, 1700, "Open.Nat (temporary)"));
        //await device.CreatePortMapAsync(new Mapping(Protocol.Tcp, 1601, 1701, "Open.Nat (Session lifetime)"));
        //await device.CreatePortMapAsync(new Mapping(Protocol.Tcp, 1602, 1702, 0, "Open.Nat (Permanent lifetime)"));
        //await device.CreatePortMapAsync(new Mapping(Protocol.Tcp, 1603, 1703, 20, "Open.Nat (Manual lifetime)"));

        public static async Task ClosePort(ushort port)
        {
            await device.DeletePortMapAsync(new Mapping(Protocol.Tcp, port, port, "Minecraft Server Starter"));
        }

        public static async Task<IEnumerable<Mapping>> GetOpenPorts()
        {
            return await device.GetAllMappingsAsync();
        }
    }
}
