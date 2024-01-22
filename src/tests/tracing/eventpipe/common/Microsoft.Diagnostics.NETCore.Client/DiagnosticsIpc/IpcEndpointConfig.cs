// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.NETCore.Client
{
    public class IpcEndpointConfig
    {
        public enum PortType
        {
            Connect,
            Listen
        }

        public enum TransportType
        {
            NamedPipe,
            UnixDomainSocket,
#if DIAGNOSTICS_RUNTIME
            TcpSocket
#endif
        }

        PortType _portType;

        TransportType _transportType;

        // For TcpSocket TransportType, the address format will be <hostname_or_ip>:<port>
        public string Address { get; }

        public bool IsConnectConfig => _portType == PortType.Connect;

        public bool IsListenConfig => _portType == PortType.Listen;

        public TransportType Transport => _transportType;

        const string NamedPipeSchema = "namedpipe";
        const string UnixDomainSocketSchema = "uds";
        const string NamedPipeDefaultIPCRoot = @"\\.\pipe\";
        const string NamedPipeSchemaDefaultIPCRootPath = "/pipe/";

        public IpcEndpointConfig(string address, TransportType transportType, PortType portType)
        {
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException("Address is null or empty.");

            switch (transportType)
            {
                case TransportType.NamedPipe:
                {
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        throw new PlatformNotSupportedException($"{NamedPipeSchema} is only supported on Windows.");
                    break;
                }
                case TransportType.UnixDomainSocket:
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        throw new PlatformNotSupportedException($"{UnixDomainSocketSchema} is not supported on Windows, use {NamedPipeSchema}.");
                    break;
                }
#if DIAGNOSTICS_RUNTIME
                case TransportType.TcpSocket:
                {
                    break;
                }
#endif
                default:
                {
                    throw new NotSupportedException($"{transportType} not supported.");
                }
            }

            Address = address;
            _transportType = transportType;
            _portType = portType;
        }

        // Config format: [Address],[PortType]
        //
        // Address in UnixDomainSocket formats:
        // myport => myport
        // uds:myport => myport
        // /User/mrx/myport.sock => /User/mrx/myport.sock
        // uds:/User/mrx/myport.sock => /User/mrx/myport.sock
        // uds://authority/User/mrx/myport.sock => /User/mrx/myport.sock
        // uds:///User/mrx/myport.sock => /User/mrx/myport.sock
        //
        // Address in NamedPipe formats:
        // myport => myport
        // namedpipe:myport => myport
        // \\.\pipe\myport => myport (dropping \\.\pipe\ is inline with implemented namedpipe client/server)
        // namedpipe://./pipe/myport => myport (dropping authority and /pipe/ is inline with implemented namedpipe client/server)
        // namedpipe:/pipe/myport  => myport (dropping /pipe/ is inline with implemented namedpipe client/server)
        // namedpipe://authority/myport => myport
        // namedpipe:///myport => myport
        //
        // PortType: Listen|Connect, default Listen.
        public static bool TryParse(string config, out IpcEndpointConfig result)
        {
            try
            {
                result = Parse(config);
            }
            catch(Exception)
            {
                result = null;
            }
            return result != null;
        }

        public static IpcEndpointConfig Parse(string config)
        {
            if (string.IsNullOrEmpty(config))
                throw new FormatException("Missing IPC endpoint config.");

            string address = "";
            PortType portType = PortType.Connect;
            TransportType transportType = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? TransportType.NamedPipe : TransportType.UnixDomainSocket;

            if (!string.IsNullOrEmpty(config))
            {
                var parts = config.Split(',');
                if (parts.Length > 2)
                    throw new FormatException($"Unknow IPC endpoint config format, {config}.");

                if (string.IsNullOrEmpty(parts[0]))
                    throw new FormatException($"Missing IPC endpoint config address, {config}.");

                portType = PortType.Listen;
                address = parts[0];

                if (parts.Length == 2)
                {
                    if (string.Equals(parts[1], "connect", StringComparison.OrdinalIgnoreCase))
                    {
                        portType = PortType.Connect;
                    }
                    else if (string.Equals(parts[1], "listen", StringComparison.OrdinalIgnoreCase))
                    {
                        portType = PortType.Listen;
                    }
                    else
                    {
                        throw new FormatException($"Unknow IPC endpoint config keyword, {parts[1]} in {config}.");
                    }
                }
            }

            if (Uri.TryCreate(address, UriKind.Absolute, out Uri parsedAddress))
            {
                if (string.Equals(parsedAddress.Scheme, NamedPipeSchema, StringComparison.OrdinalIgnoreCase))
                {
                    transportType = TransportType.NamedPipe;
                    address = parsedAddress.AbsolutePath;
                }
                else if (string.Equals(parsedAddress.Scheme, UnixDomainSocketSchema, StringComparison.OrdinalIgnoreCase))
                {
                    transportType = TransportType.UnixDomainSocket;
                    address = parsedAddress.AbsolutePath;
                }
                else if (string.Equals(parsedAddress.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
                {
                    address = parsedAddress.AbsolutePath;
                }
                else if (!string.IsNullOrEmpty(parsedAddress.Scheme))
                {
                    throw new FormatException($"{parsedAddress.Scheme} not supported.");
                }
            }
            else
            {
                if (address.StartsWith(NamedPipeDefaultIPCRoot, StringComparison.OrdinalIgnoreCase))
                    transportType = TransportType.NamedPipe;
            }

            if (transportType == TransportType.NamedPipe)
            {
                if (address.StartsWith(NamedPipeDefaultIPCRoot, StringComparison.OrdinalIgnoreCase))
                    address = address.Substring(NamedPipeDefaultIPCRoot.Length);
                else if (address.StartsWith(NamedPipeSchemaDefaultIPCRootPath, StringComparison.OrdinalIgnoreCase))
                    address = address.Substring(NamedPipeSchemaDefaultIPCRootPath.Length);
                else if (address.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                    address = address.Substring("/".Length);
            }

            return new IpcEndpointConfig(address, transportType, portType);
        }
    }
}
