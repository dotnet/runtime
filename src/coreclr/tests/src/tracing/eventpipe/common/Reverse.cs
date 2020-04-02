// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Tracing.Tests.Common
{
    /**
     * ==ADVERTISE PROTOCOL==
     * Before standard IPC Protocol communication can occur on a client-mode connection
     * the runtime must advertise itself over the connection.  ALL SUBSEQUENT COMMUNICATION 
     * IS STANDARD DIAGNOSTICS IPC PROTOCOL COMMUNICATION.
     * 
     * The flow for Advertise is a one-way burst of 32 bytes consisting of
     * 8 bytes  - "ADVR_V1\0" (ASCII chars + null byte)
     * 16 bytes - CLR Instance Cookie (little-endian)
     * 8 bytes  - PID (little-endian)
     */

    public class IpcAdvertise
    {
        public static int Size_V1 => 32;
        public static byte[] Magic_V1 => System.Text.Encoding.ASCII.GetBytes("ADVR_V1" + '\0');
        public static int MagicSize_V1 => 8;

        public byte[] Magic = Magic_V1;
        public UInt64 ProcessId;
        public Guid RuntimeInstanceCookie;

        /// <summary>
        ///
        /// </summary>
        /// <returns> (pid, clrInstanceId) </returns>
        public static IpcAdvertise Parse(Stream stream)
        {
            var binaryReader = new BinaryReader(stream);
            var advertise = new IpcAdvertise()
            {
                Magic = binaryReader.ReadBytes(Magic_V1.Length),
                RuntimeInstanceCookie = new Guid(binaryReader.ReadBytes(16)),
                ProcessId = binaryReader.ReadUInt64()
            };

            for (int i = 0; i < Magic_V1.Length; i++)
                if (advertise.Magic[i] != Magic_V1[i])
                    throw new Exception("Invalid advertise message from client connection");

            // FUTURE: switch on incoming magic and change if version ever increments
            return advertise;
        }

        override public string ToString()
        {
            return $"{{ Magic={Magic}; ClrInstanceId={RuntimeInstanceCookie}; ProcessId={ProcessId}; }}";
        }
    }
    public class ReverseServer
    {
        public static string MakeServerAddress()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.GetRandomFileName();
            }
            else
            {
                return Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            }
        }

        private object _server; // _server ::= socket | NamedPipeServerStream
        private Socket _clientSocket; // only used on non-Windows
        private string _serverAddress;

        public ReverseServer(string serverAddress)
        {
            _serverAddress = serverAddress;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _server = new NamedPipeServerStream(serverAddress);
            }
            else
            {
                if (File.Exists(serverAddress))
                    File.Delete(serverAddress);
                var remoteEP = new UnixDomainSocketEndPoint(serverAddress);

                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                socket.Bind(remoteEP);
                socket.Listen(255);
                socket.LingerState.Enabled = false;
                _server = socket;
            }
        }

        public async Task<Stream> AcceptAsync()
        {
            switch (_server)
            {
                case NamedPipeServerStream serverStream:
                    await serverStream.WaitForConnectionAsync();
                    return serverStream;
                case Socket socket:
                    _clientSocket = await socket.AcceptAsync();
                    return new NetworkStream(_clientSocket);
                default:
                    throw new ArgumentException("Invalid server type");
            }
        }

        public void Shutdown()
        {
            switch (_server)
            {
                case NamedPipeServerStream serverStream:
                    serverStream.Disconnect();
                    serverStream.Dispose();
                    break;
                case Socket socket:
                    try
                    {
                        socket.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception e) {}
                    finally
                    {
                        _clientSocket?.Close();
                        socket.Close();
                        socket.Dispose();
                        _clientSocket?.Dispose();
                        if (File.Exists(_serverAddress))
                            File.Delete(_serverAddress);
                    }
                    break;
                default:
                    throw new ArgumentException("Invalid server type");
            }
        }

        // Creates the server, listens, and closes the server
        public static async Task<IpcAdvertise> CreateServerAndReceiveAdvertisement(string serverAddress)
        {
            var server = new ReverseServer(serverAddress);
            Logger.logger.Log("Waiting for connection");
            Stream stream = await server.AcceptAsync();
            Logger.logger.Log("Got a connection");
            IpcAdvertise advertise = IpcAdvertise.Parse(stream);
            server.Shutdown();
            return advertise;
            // if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            // {
            //     using var serverStream = new NamedPipeServerStream(serverAddress);
            //     Logger.logger.Log("Waiting for connection");
            //     await serverStream.WaitForConnectionAsync();
            //     Logger.logger.Log("Got a connection");
            //     IpcAdvertise advertise = IpcAdvertise.Parse(serverStream);
            //     serverStream.Disconnect();
            //     return advertise;
            // }
            // else
            // {
            //     if (File.Exists(serverAddress))
            //         File.Delete(serverAddress);
            //     var remoteEP = new UnixDomainSocketEndPoint(serverAddress);

            //     using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            //     socket.Bind(remoteEP);
            //     socket.Listen(255);
            //     socket.LingerState.Enabled = false;
            //     Logger.logger.Log("Waiting for connection");
            //     using Socket clientSocket = await socket.AcceptAsync();
            //     Logger.logger.Log("Got a connection");
            //     using var socketStream = new NetworkStream(clientSocket);
            //     IpcAdvertise advertise = IpcAdvertise.Parse(socketStream);
            //     try
            //     {
            //         socket.Shutdown(SocketShutdown.Both);
            //     }
            //     catch (Exception e) {}
            //     finally
            //     {
            //         clientSocket.Close();
            //         socket.Close();
            //         if (File.Exists(serverAddress))
            //             File.Delete(serverAddress);
            //     }

            //     return advertise;
            // }
        }
    }
}