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
    public static class ReverseServer
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

        // Creates the server, listens, and closes the server
        public static async Task<IpcAdvertise> CreateServerAndReceiveAdvertisement(string serverAddress)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var serverStream = new NamedPipeServerStream(serverAddress);
                Logger.logger.Log("Waiting for connection");
                await serverStream.WaitForConnectionAsync();
                Logger.logger.Log("Got a connection");
                IpcAdvertise advertise = IpcAdvertise.Parse(serverStream);
                serverStream.Disconnect();
                return advertise;
            }
            else
            {
                if (File.Exists(serverAddress))
                    File.Delete(serverAddress);
                var remoteEP = new UnixDomainSocketEndPoint(serverAddress);

                using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                socket.Bind(remoteEP);
                socket.Listen(255);
                socket.LingerState.Enabled = false;
                Logger.logger.Log("Waiting for connection");
                using Socket clientSocket = await socket.AcceptAsync();
                Logger.logger.Log("Got a connection");
                using var socketStream = new NetworkStream(clientSocket);
                IpcAdvertise advertise = IpcAdvertise.Parse(socketStream);
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception e) {}
                finally
                {
                    clientSocket.Close();
                    socket.Close();
                    if (File.Exists(serverAddress))
                        File.Delete(serverAddress);
                }

                return advertise;
            }
        }
    }
}