//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//   Ben Motmans <ben.motmans@gmail.com>
//   Lucas Ontivero lucasontivero@gmail.com 
//
// Copyright (C) 2006 Alan McGovern
// Copyright (C) 2007 Ben Motmans
// Copyright (C) 2014 Lucas Ontivero
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Open.Nat
{
    internal sealed class UpnpNatDevice : NatDevice
	{
	    internal readonly UpnpNatDeviceInfo DeviceInfo;
        private readonly SoapClient _soapClient;

        internal UpnpNatDevice (UpnpNatDeviceInfo deviceInfo)
		{
            Touch();
            DeviceInfo = deviceInfo;
            _soapClient = new SoapClient(DeviceInfo.ServiceControlUri, DeviceInfo.ServiceType);
		}

		public override async Task<IPAddress> GetExternalIPAsync()
	    {
            NatDiscoverer.TraceSource.LogInfo("GetExternalIPAsync - Getting external IP address");
            var message = new GetExternalIPAddressRequestMessage();
            var responseData = await _soapClient
                .InvokeAsync("GetExternalIPAddress", message.ToXml())
                .TimeoutAfter(TimeSpan.FromSeconds(4));

            var response = new GetExternalIPAddressResponseMessage(responseData, DeviceInfo.ServiceType);
            return response.ExternalIPAddress;
        }

        public override async Task CreatePortMapAsync(Mapping mapping)
        {
            Guard.IsNotNull(mapping, "mapping");
            if(mapping.PrivateIP.Equals(IPAddress.None)) mapping.PrivateIP =  DeviceInfo.LocalAddress;

            NatDiscoverer.TraceSource.LogInfo("CreatePortMapAsync - Creating port mapping {0}", mapping);
            try
            {
                var message = new CreatePortMappingRequestMessage(mapping);
                await _soapClient
                    .InvokeAsync("AddPortMapping", message.ToXml())
                    .TimeoutAfter(TimeSpan.FromSeconds(4));
                RegisterMapping(mapping);
            }
            catch(MappingException me)
            {
                switch (me.ErrorCode)
                {
                    case UpnpConstants.OnlyPermanentLeasesSupported:
                        NatDiscoverer.TraceSource.LogWarn("Only Permanent Leases Supported - There is no warranty it will be closed");
                        mapping.Lifetime = 0;
                        // We create the mapping anyway. It must be released on shutdown.
                        mapping.LifetimeType = MappingLifetime.ForcedSession;
                        await CreatePortMapAsync(mapping);
                        break;
                    case UpnpConstants.SamePortValuesRequired:
                        NatDiscoverer.TraceSource.LogWarn("Same Port Values Required - Using internal port {0}", mapping.PrivatePort);
                        mapping.PublicPort = mapping.PrivatePort;
                        await CreatePortMapAsync(mapping);
                        break;
                    case UpnpConstants.RemoteHostOnlySupportsWildcard:
                        NatDiscoverer.TraceSource.LogWarn("Remote Host Only Supports Wildcard");
                        mapping.PublicIP = IPAddress.None;
                        await CreatePortMapAsync(mapping);
                        break;
                    case UpnpConstants.ExternalPortOnlySupportsWildcard:
                        NatDiscoverer.TraceSource.LogWarn("External Port Only Supports Wildcard");
                        throw;
                    case UpnpConstants.ConflictInMappingEntry:
                        NatDiscoverer.TraceSource.LogWarn("Conflict with an already existing mapping");
                        throw;

                    default:
                        throw;
                }
            }
        }

		public override async Task DeletePortMapAsync(Mapping mapping)
		{
            Guard.IsNotNull(mapping, "mapping");

            if (mapping.PrivateIP.Equals(IPAddress.None)) mapping.PrivateIP = DeviceInfo.LocalAddress;
            
            NatDiscoverer.TraceSource.LogInfo("DeletePortMapAsync - Deleteing port mapping {0}", mapping);

            try
		    {
                var message = new DeletePortMappingRequestMessage(mapping);
                await _soapClient
                    .InvokeAsync("DeletePortMapping", message.ToXml())
                    .TimeoutAfter(TimeSpan.FromSeconds(4));
                UnregisterMapping(mapping);
            }
		    catch (MappingException e)
		    {
                if(e.ErrorCode != UpnpConstants.NoSuchEntryInArray) throw; 
		    }
        }

		public override async Task<IEnumerable<Mapping>> GetAllMappingsAsync()
		{
            var index = 0;
		    var mappings = new List<Mapping>();

            NatDiscoverer.TraceSource.LogInfo("GetAllMappingsAsync - Getting all mappings");
            while (true)
            {
                try
                {
                    var message = new GetGenericPortMappingEntry(index++);

                    var responseData = await _soapClient
                        .InvokeAsync("GetGenericPortMappingEntry", message.ToXml())
                        .TimeoutAfter(TimeSpan.FromSeconds(4));

                    var responseMessage = new GetPortMappingEntryResponseMessage(responseData, DeviceInfo.ServiceType, true);

                    IPAddress internalClientIp;
                    if(!IPAddress.TryParse(responseMessage.InternalClient, out internalClientIp))
                    {
                        NatDiscoverer.TraceSource.LogWarn("InternalClient is not an IP address. Mapping ignored!");
                        continue;
                    }

                    var mapping = new Mapping(responseMessage.Protocol
                        , internalClientIp
                        , responseMessage.InternalPort
                        , responseMessage.ExternalPort
                        , responseMessage.LeaseDuration
                        , responseMessage.PortMappingDescription);
                    mappings.Add(mapping);
                }
                catch (MappingException e)
                {
                    // there are no more mappings
                    if (e.ErrorCode == UpnpConstants.SpecifiedArrayIndexInvalid
                     || e.ErrorCode == UpnpConstants.NoSuchEntryInArray
                     // DD-WRT Linux base router (and others probably) fails with 402-InvalidArgument when index is out of range
                     || e.ErrorCode == UpnpConstants.InvalidArguments
                     // LINKSYS WRT1900AC AC1900 it returns errocode 501-PAL_UPNP_SOAP_E_ACTION_FAILED
                     || e.ErrorCode == UpnpConstants.ActionFailed)
                    {
                        NatDiscoverer.TraceSource.LogWarn("Router failed with {0}-{1}. No more mappings is assumed.", e.ErrorCode, e.ErrorText);
                        break; 
                    }
                    throw;
                }
            }

            return mappings.ToArray();
        }

		public override async Task<Mapping> GetSpecificMappingAsync (Protocol protocol, int port)
		{
            Guard.IsTrue(protocol == Protocol.Tcp || protocol == Protocol.Udp, "protocol");
            Guard.IsInRange(port, 0, ushort.MaxValue, "port");

            NatDiscoverer.TraceSource.LogInfo("GetSpecificMappingAsync - Getting mapping for protocol: {0} port: {1}", Enum.GetName(typeof(Protocol), protocol), port);

            try
            {
                var message = new GetSpecificPortMappingEntryRequestMessage(protocol, port);
                var responseData = await _soapClient
                    .InvokeAsync("GetSpecificPortMappingEntry", message.ToXml())
                    .TimeoutAfter(TimeSpan.FromSeconds(4));

                var messageResponse = new GetPortMappingEntryResponseMessage(responseData, DeviceInfo.ServiceType, false);

                return new Mapping(messageResponse.Protocol
                    , IPAddress.Parse(messageResponse.InternalClient)
                    , messageResponse.InternalPort
                    , messageResponse.ExternalPort
                    , messageResponse.LeaseDuration
                    , messageResponse.PortMappingDescription);
            }
            catch (MappingException e)
            {
                // there are no more mappings
                if (e.ErrorCode == UpnpConstants.SpecifiedArrayIndexInvalid
                 || e.ErrorCode == UpnpConstants.NoSuchEntryInArray
                    // DD-WRT Linux base router (and others probably) fails with 402-InvalidArgument when index is out of range
                 || e.ErrorCode == UpnpConstants.InvalidArguments
                    // LINKSYS WRT1900AC AC1900 it returns errocode 501-PAL_UPNP_SOAP_E_ACTION_FAILED
                 || e.ErrorCode == UpnpConstants.ActionFailed)
                {
                    NatDiscoverer.TraceSource.LogWarn("Router failed with {0}-{1}. No more mappings is assumed.", e.ErrorCode, e.ErrorText);
                    return null;
                }
                throw;
            }
        }

        public override string ToString( )
        {
            //GetExternalIP is blocking and can throw exceptions, can't use it here.
            return String.Format( 
                "EndPoint: {0}\nControl Url: {1}\nService Type: {2}\nLast Seen: {3}",
                DeviceInfo.HostEndPoint, DeviceInfo.ServiceControlUri,  DeviceInfo.ServiceType, LastSeen);
        }
	}
}