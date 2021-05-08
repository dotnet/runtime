// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
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
     * 2 bytes  - unused for futureproofing
     */

    public class IpcAdvertise
    {
        public static int Size_V1 => 34;
        public static byte[] Magic_V1 => System.Text.Encoding.ASCII.GetBytes("ADVR_V1" + '\0');
        public static int MagicSize_V1 => 8;

        public byte[] Magic = Magic_V1;
        public UInt64 ProcessId;
        public Guid RuntimeInstanceCookie;
        public UInt16 Unused;

        /// <summary>
        ///
        /// </summary>
        /// <returns> (pid, clrInstanceId) </returns>
        public static IpcAdvertise Parse(Stream stream)
        {
            using (var binaryReader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                var advertise = new IpcAdvertise()
                {
                    Magic = binaryReader.ReadBytes(Magic_V1.Length),
                    RuntimeInstanceCookie = new Guid(binaryReader.ReadBytes(16)),
                    ProcessId = binaryReader.ReadUInt64(),
                    Unused = binaryReader.ReadUInt16()
                };

                for (int i = 0; i < Magic_V1.Length; i++)
                    if (advertise.Magic[i] != Magic_V1[i])
                        throw new Exception("Invalid advertise message from client connection");

                // FUTURE: switch on incoming magic and change if version ever increments
                return advertise;
            }
        }

        override public string ToString()
        {
            return $"{{ Magic={Magic}; ClrInstanceId={RuntimeInstanceCookie}; ProcessId={ProcessId}; Unused={Unused}; }}";
        }
    }

    public class ReverseServer
    {
        public static string MakeServerAddress()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "DOTNET_TRACE_TESTS_" + Path.GetRandomFileName();
            }
            else
            {
                return Path.Combine(Path.GetTempPath(), "DOTNET_TRACE_TESTS_" + Path.GetRandomFileName());
            }
        }

        private object _server; // _server ::= socket | NamedPipeServerStream
        private int _bufferSize;
        private Socket _clientSocket; // only used on non-Windows
        private string _serverAddress;

        public ReverseServer(string serverAddress, int bufferSize = 16 * 1024)
        {
            _serverAddress = serverAddress;
            _bufferSize = bufferSize;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _server = GetNewNamedPipeServer();
            }
            else
            {
                if (File.Exists(serverAddress))
                    File.Delete(serverAddress);
                var remoteEP = new UnixDomainSocketEndPoint(serverAddress);

                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                // socket(7) states that SO_RCVBUF has a minimum of 128 and SO_SNDBUF has minimum of 1024
                socket.SendBufferSize = Math.Max(bufferSize, 1024);
                socket.ReceiveBufferSize = Math.Max(bufferSize, 128);
                socket.Bind(remoteEP);
                socket.Listen(255);
                _server = socket;
            }
        }

        public async Task<Stream> AcceptAsync()
        {
            switch (_server)
            {
                case NamedPipeServerStream serverStream:
                    try
                    {
                        await serverStream.WaitForConnectionAsync();
                        return serverStream;
                    }
                    catch (ObjectDisposedException)
                    {
                        _server = GetNewNamedPipeServer();
                        return await AcceptAsync();
                    }
                    break;
                case Socket socket:
                    _clientSocket = await socket.AcceptAsync();
                    return new NetworkStream(_clientSocket);
                default:
                    throw new ArgumentException("Invalid server type");
            }
        }

        private NamedPipeServerStream GetNewNamedPipeServer()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new NamedPipeServerStream(
                    _serverAddress,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.None,
                    _bufferSize,
                    _bufferSize);
            }
            else
            {
                return null;
            }
        }

        public void Shutdown()
        {
            Logger.logger.Log($"Shutting down Reverse Server at {_serverAddress}");
            switch (_server)
            {
                case NamedPipeServerStream serverStream:
                    try
                    {
                        serverStream.Disconnect();
                    }
                    catch {}
                    finally
                    {
                        serverStream.Dispose();
                    }
                    break;
                case Socket socket:
                    if (File.Exists(_serverAddress))
                        File.Delete(_serverAddress);
                    socket.Close();
                    socket.Dispose();
                    _clientSocket?.Shutdown(SocketShutdown.Both);
                    _clientSocket?.Close();
                    _clientSocket?.Dispose();
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
            using Stream stream = await server.AcceptAsync();
            Logger.logger.Log("Got a connection");
            IpcAdvertise advertise = IpcAdvertise.Parse(stream);
            server.Shutdown();
            return advertise;
        }
    }
}
