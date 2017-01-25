// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace System.Data.SqlClient.SNI
{
    /// <summary>
    /// Managed SNI proxy implementation. Contains many SNI entry points used by SqlClient.
    /// </summary>
    internal class SNIProxy
    {
        private static readonly char SemicolonSeparator = ';';
        private static readonly char CommaSeparator = ',';
        private static readonly char BackSlashSeparator = '\\';
        private const int SqlServerBrowserPort = 1434;
        private const int DefaultSqlServerPort = 1433;
        private const string DefaultHostname = "localhost";
        private const string DefaultSqlServerInstanceName = "MSSQLSERVER";

        public static readonly SNIProxy Singleton = new SNIProxy();

        /// <summary>
        /// Terminate SNI
        /// </summary>
        public void Terminate()
        {
        }

        /// <summary>
        /// Enable MARS support on a connection
        /// </summary>
        /// <param name="handle">Connection handle</param>
        /// <returns>SNI error code</returns>
        public uint EnableMars(SNIHandle handle)
        {
            if (SNIMarsManager.Singleton.CreateMarsConnection(handle) == TdsEnums.SNI_SUCCESS_IO_PENDING)
            {
                return TdsEnums.SNI_SUCCESS;
            }

            return TdsEnums.SNI_ERROR;
        }

        /// <summary>
        /// Enable SSL on a connection
        /// </summary>
        /// <param name="handle">Connection handle</param>
        /// <returns>SNI error code</returns>
        public uint EnableSsl(SNIHandle handle, uint options)
        {
            try
            {
                return handle.EnableSsl(options);
            }
            catch (Exception e)
            {
                return SNICommon.ReportSNIError(SNIProviders.SSL_PROV, SNICommon.HandshakeFailureError, e);
            }
        }

        /// <summary>
        /// Disable SSL on a connection
        /// </summary>
        /// <param name="handle">Connection handle</param>
        /// <returns>SNI error code</returns>
        public uint DisableSsl(SNIHandle handle)
        {
            handle.DisableSsl();
            return TdsEnums.SNI_SUCCESS;
        }

        /// <summary>
        /// Generate SSPI context
        /// </summary>
        /// <param name="handle">SNI connection handle</param>
        /// <param name="receivedBuff">Receive buffer</param>
        /// <param name="receivedLength">Received length</param>
        /// <param name="sendBuff">Send buffer</param>
        /// <param name="sendLength">Send length</param>
        /// <param name="serverName">Service Principal Name buffer</param>
        /// <param name="serverNameLength">Length of Service Principal Name</param>
        /// <returns>SNI error code</returns>
        public uint GenSspiClientContext(SNIHandle handle, byte[] receivedBuff, uint receivedLength, byte[] sendBuff, ref uint sendLength, byte[] serverName, uint serverNameLength)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Initialize SSPI
        /// </summary>
        /// <param name="maxLength">Max length of SSPI packet</param>
        /// <returns>SNI error code</returns>
        public uint InitializeSspiPackage(ref uint maxLength)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Set connection buffer size
        /// </summary>
        /// <param name="handle">SNI handle</param>
        /// <param name="bufferSize">Buffer size</param>
        /// <returns>SNI error code</returns>
        public uint SetConnectionBufferSize(SNIHandle handle, uint bufferSize)
        {
            handle.SetBufferSize((int)bufferSize);
            return TdsEnums.SNI_SUCCESS;
        }

        /// <summary>
        /// Get packet data
        /// </summary>
        /// <param name="packet">SNI packet</param>
        /// <param name="inBuff">Buffer</param>
        /// <param name="dataSize">Data size</param>
        /// <returns>SNI error status</returns>
        public uint PacketGetData(SNIPacket packet, byte[] inBuff, ref uint dataSize)
        {
            int dataSizeInt = 0;
            packet.GetData(inBuff, ref dataSizeInt);
            dataSize = (uint)dataSizeInt;

            return TdsEnums.SNI_SUCCESS;
        }

        /// <summary>
        /// Read synchronously
        /// </summary>
        /// <param name="handle">SNI handle</param>
        /// <param name="packet">SNI packet</param>
        /// <param name="timeout">Timeout</param>
        /// <returns>SNI error status</returns>
        public uint ReadSyncOverAsync(SNIHandle handle, out SNIPacket packet, int timeout)
        {
            return handle.Receive(out packet, timeout);
        }

        /// <summary>
        /// Get SNI connection ID
        /// </summary>
        /// <param name="handle">SNI handle</param>
        /// <param name="clientConnectionId">Client connection ID</param>
        /// <returns>SNI error status</returns>
        public uint GetConnectionId(SNIHandle handle, ref Guid clientConnectionId)
        {
            clientConnectionId = handle.ConnectionId;

            return TdsEnums.SNI_SUCCESS;
        }

        /// <summary>
        /// Send a packet
        /// </summary>
        /// <param name="handle">SNI handle</param>
        /// <param name="packet">SNI packet</param>
        /// <param name="sync">true if synchronous, false if asynchronous</param>
        /// <returns>SNI error status</returns>
        public uint WritePacket(SNIHandle handle, SNIPacket packet, bool sync)
        {
            if (sync)
            {
                return handle.Send(packet.Clone());
            }
            else
            {
                return handle.SendAsync(packet.Clone());
            }
        }

        /// <summary>
        /// Reset a packet
        /// </summary>
        /// <param name="handle">SNI handle</param>
        /// <param name="write">true if packet is for write</param>
        /// <param name="packet">SNI packet</param>
        public void PacketReset(SNIHandle handle, bool write, SNIPacket packet)
        {
            packet.Reset();
        }

        /// <summary>
        /// Create a SNI connection handle
        /// </summary>
        /// <param name="callbackObject">Asynchronous I/O callback object</param>
        /// <param name="fullServerName">Full server name from connection string</param>
        /// <param name="ignoreSniOpenTimeout">Ignore open timeout</param>
        /// <param name="timerExpire">Timer expiration</param>
        /// <param name="instanceName">Instance name</param>
        /// <param name="spnBuffer">SPN</param>
        /// <param name="flushCache">Flush packet cache</param>
        /// <param name="async">Asynchronous connection</param>
        /// <param name="parallel">Attempt parallel connects</param>
        /// <returns>SNI handle</returns>
        public SNIHandle CreateConnectionHandle(object callbackObject, string fullServerName, bool ignoreSniOpenTimeout, long timerExpire, out byte[] instanceName, byte[] spnBuffer, bool flushCache, bool async, bool parallel)
        {
            instanceName = new byte[1];
            instanceName[0] = 0;

            string[] serverNameParts = fullServerName.Split(':');

            if (serverNameParts.Length > 2)
            {
                SNILoadHandle.SingletonInstance.LastError = new SNIError(SNIProviders.INVALID_PROV, 0, SNICommon.InvalidConnStringError, string.Empty);
                return null;
            }

            // Default to using tcp if no protocol is provided
            if (serverNameParts.Length == 1)
            {
                return CreateTcpHandle(serverNameParts[0], timerExpire, callbackObject, parallel);
            }

            switch (serverNameParts[0])
            {
                case TdsEnums.TCP:
                    return CreateTcpHandle(serverNameParts[1], timerExpire, callbackObject, parallel);

                case TdsEnums.NP:
                    return CreateNpHandle(serverNameParts[1], timerExpire, callbackObject, parallel);

                default:
                    if (parallel)
                    {
                        SNICommon.ReportSNIError(SNIProviders.INVALID_PROV, 0, SNICommon.MultiSubnetFailoverWithNonTcpProtocol, string.Empty);
                    }
                    else
                    {
                        SNICommon.ReportSNIError(SNIProviders.INVALID_PROV, 0, SNICommon.ProtocolNotSupportedError, string.Empty);
                    }
                    return null;
            }
        }

        /// <summary>
        /// Creates an SNITCPHandle object
        /// </summary>
        /// <param name="fullServerName">Server string. May contain a comma delimited port number.</param>
        /// <param name="timerExpire">Timer expiration</param>
        /// <param name="callbackObject">Asynchronous I/O callback object</param>
        /// <param name="parallel">Should MultiSubnetFailover be used</param>
        /// <returns>SNITCPHandle</returns>
        private SNITCPHandle CreateTcpHandle(string fullServerName, long timerExpire, object callbackObject, bool parallel)
        {
            // TCP Format: 
            // tcp:<host name>\<instance name>
            // tcp:<host name>,<TCP/IP port number>

            if (string.IsNullOrWhiteSpace(fullServerName))
            {
                int defaultInstancePort = TryToGetDefaultInstancePort(DefaultHostname);
                return new SNITCPHandle(DefaultHostname, (defaultInstancePort > 0 ? defaultInstancePort : DefaultSqlServerPort), timerExpire, callbackObject, parallel);
            }

            string[] serverNamePartsByComma = fullServerName.Split(CommaSeparator);
            string[] serverNamePartsByBackSlash = fullServerName.Split(BackSlashSeparator);

            if (serverNamePartsByComma.Length < 2 && serverNamePartsByBackSlash.Length < 2)
            {
                return new SNITCPHandle(fullServerName, DefaultSqlServerPort, timerExpire, callbackObject, parallel);
            }
            else if (serverNamePartsByComma.Length == 2 && serverNamePartsByBackSlash.Length < 2)
            {
                string hostName = serverNamePartsByComma[0];
                int portNumber = -1;
                try
                {
                    portNumber = ushort.Parse(serverNamePartsByComma[1]);
                    return new SNITCPHandle(hostName, portNumber, timerExpire, callbackObject, parallel);
                }
                catch (Exception e)
                {
                    SNILoadHandle.SingletonInstance.LastError = new SNIError(SNIProviders.TCP_PROV, SNICommon.InvalidConnStringError, e);
                    return null;
                }
            }
            else if (serverNamePartsByComma.Length < 2 && serverNamePartsByBackSlash.Length == 2)
            {
                string hostName = serverNamePartsByBackSlash[0];
                string instanceName = serverNamePartsByBackSlash[1];
                int portNumber = -1;
                try
                {
                    portNumber = GetPortByInstanceName(hostName, instanceName);
                    return new SNITCPHandle(hostName, portNumber, timerExpire, callbackObject, parallel);
                }
                catch (Exception e)
                {
                    SNILoadHandle.SingletonInstance.LastError = new SNIError(SNIProviders.TCP_PROV, SNICommon.InvalidConnStringError, e);
                    return null;
                }
            }
            else
            {
                SNILoadHandle.SingletonInstance.LastError = new SNIError(SNIProviders.TCP_PROV, 0, SNICommon.InvalidConnStringError, string.Empty);
                return null;
            }
        }

        /// <summary>
        /// Sends CLNT_UCAST_INST request for given instance name to SQL Sever Browser, and receive SVR_RESP from the Browser.
        /// </summary>
        /// <param name="browserHostname">SQL Sever Browser hostname</param>
        /// <param name="instanceName">instance name for CLNT_UCAST_INST request</param>
        /// <returns>SVR_RESP packets from SQL Sever Browser</returns>
        private static byte[] SendInstanceInfoRequest(string browserHostname, string instanceName)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(browserHostname));
            Debug.Assert(!string.IsNullOrWhiteSpace(instanceName));

            byte[] instanceInfoRequest = CreateInstanceInfoRequest(instanceName);
            byte[] responsePacket = SendUDPRequest(browserHostname, SqlServerBrowserPort, instanceInfoRequest);

            const byte SvrResp = 0x05;
            if (responsePacket == null || responsePacket.Length <= 3 || responsePacket[0] != SvrResp ||
                BitConverter.ToUInt16(responsePacket, 1) != responsePacket.Length - 3)
            {
                throw new SocketException();
            }

            return responsePacket;
        }

        /// <summary>
        /// Finds port number for given instance name.
        /// </summary>
        /// <param name="browserHostname">SQL Sever Browser hostname</param>
        /// <param name="instanceName">instance name to find port number</param>
        /// <returns>port number for given instance name</returns>
        private static int GetPortByInstanceName(string browserHostname, string instanceName)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(browserHostname));
            Debug.Assert(!string.IsNullOrWhiteSpace(instanceName));

            byte[] responsePacket = SendInstanceInfoRequest(browserHostname, instanceName);

            string serverMessage = Encoding.ASCII.GetString(responsePacket, 3, responsePacket.Length - 3);
            string[] elements = serverMessage.Split(SemicolonSeparator);
            int tcpIndex = Array.IndexOf(elements, "tcp");
            if (tcpIndex < 0 || tcpIndex == elements.Length - 1)
            {
                throw new SocketException();
            }

            return ushort.Parse(elements[tcpIndex + 1]);
        }

        /// <summary>
        /// Finds port of SQL Server instance MSSQLSERVER by querying to SQL Server Browser
        /// </summary>
        /// <param name="browserHostname">SQL Server hostname</param>
        /// <returns>default SQL Server instance port</returns>
        private static int TryToGetDefaultInstancePort(string browserHostname)
        {
            int defaultInstancePort = -1;
            try
            {
                defaultInstancePort = GetPortByInstanceName(browserHostname, DefaultSqlServerInstanceName);
            }
            catch { }
            return defaultInstancePort;
        }

        /// <summary>
        /// Creates UDP request of CLNT_UCAST_INST payload in SSRP to get information about SQL Server instance
        /// </summary>
        /// <param name="instanceName">SQL Server instance name</param>
        /// <returns>CLNT_UCAST_INST request packet</returns>
        private static byte[] CreateInstanceInfoRequest(string instanceName)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(instanceName));

            const byte ClntUcastInst = 0x04;
            int byteCount = Encoding.ASCII.GetByteCount(instanceName);
            byte[] requestPacket = new byte[byteCount + 1];
            requestPacket[0] = ClntUcastInst;
            Encoding.ASCII.GetBytes(instanceName, 0, instanceName.Length, requestPacket, 1);
            return requestPacket;
        }

        /// <summary>
        /// Sends UDP request to server, and receive response.
        /// </summary>
        /// <param name="browserHostname">UDP server hostname</param>
        /// <param name="port">UDP server port</param>
        /// <param name="requestPacket">request packet</param>
        /// <returns>response packet from UDP server</returns>
        private static byte[] SendUDPRequest(string browserHostname, int port, byte[] requestPacket)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(browserHostname));
            Debug.Assert(port >= 0 || port <= 65535);
            Debug.Assert(requestPacket != null && requestPacket.Length > 0);

            const int sendTimeOut = 1000;
            const int receiveTimeOut = 1000;
            byte[] responsePacket = null;
            using (UdpClient client = new UdpClient(AddressFamily.InterNetwork))
            {
                Task<int> sendTask = client.SendAsync(requestPacket, requestPacket.Length, browserHostname, port);
                Task<UdpReceiveResult> receiveTask = null;
                if (sendTask.Wait(sendTimeOut) && (receiveTask = client.ReceiveAsync()).Wait(receiveTimeOut))
                {
                    responsePacket = receiveTask.Result.Buffer;
                }
            }

            return responsePacket;
        }

        /// <summary>
        /// Creates an SNINpHandle object
        /// </summary>
        /// <param name="fullServerName">Server string representing a UNC pipe path.</param>
        /// <param name="timerExpire">Timer expiration</param>
        /// <param name="callbackObject">Asynchronous I/O callback object</param>
        /// <param name="parallel">Should MultiSubnetFailover be used. Only returns an error for named pipes.</param>
        /// <returns>SNINpHandle</returns>
        private SNINpHandle CreateNpHandle(string fullServerName, long timerExpire, object callbackObject, bool parallel)
        {
            if (parallel)
            {
                SNICommon.ReportSNIError(SNIProviders.NP_PROV, 0, SNICommon.MultiSubnetFailoverWithNonTcpProtocol, string.Empty);
                return null;
            }

            if (fullServerName.Length == 0 || fullServerName.Contains("/")) // Pipe paths only allow back slashes
            {
                SNILoadHandle.SingletonInstance.LastError = new SNIError(SNIProviders.NP_PROV, 0, SNICommon.InvalidConnStringError, string.Empty);
                return null;
            }

            string serverName, pipeName;
            if (!fullServerName.Contains(@"\"))
            {
                serverName = fullServerName;
                pipeName = SNINpHandle.DefaultPipePath;
            }
            else
            {
                try
                {
                    Uri pipeURI = new Uri(fullServerName);
                    string resourcePath = pipeURI.AbsolutePath;

                    string pipeToken = "/pipe/";
                    if (!resourcePath.StartsWith(pipeToken))
                    {
                        SNILoadHandle.SingletonInstance.LastError = new SNIError(SNIProviders.NP_PROV, 0, SNICommon.InvalidConnStringError, string.Empty);
                        return null;
                    }
                    pipeName = resourcePath.Substring(pipeToken.Length);
                    serverName = pipeURI.Host;
                }
                catch (UriFormatException)
                {
                    SNILoadHandle.SingletonInstance.LastError = new SNIError(SNIProviders.NP_PROV, 0, SNICommon.InvalidConnStringError, string.Empty);
                    return null;
                }
            }

            return new SNINpHandle(serverName, pipeName, timerExpire, callbackObject);
        }

        /// <summary>
        /// Create MARS handle
        /// </summary>
        /// <param name="callbackObject">Asynchronous I/O callback object</param>
        /// <param name="physicalConnection">SNI connection handle</param>
        /// <param name="defaultBufferSize">Default buffer size</param>
        /// <param name="async">Asynchronous connection</param>
        /// <returns>SNI error status</returns>
        public SNIHandle CreateMarsHandle(object callbackObject, SNIHandle physicalConnection, int defaultBufferSize, bool async)
        {
            SNIMarsConnection connection = SNIMarsManager.Singleton.GetConnection(physicalConnection);
            return connection.CreateSession(callbackObject, async);
        }

        /// <summary>
        /// Read packet asynchronously
        /// </summary>
        /// <param name="handle">SNI handle</param>
        /// <param name="packet">Packet</param>
        /// <returns>SNI error status</returns>
        public uint ReadAsync(SNIHandle handle, ref SNIPacket packet)
        {
            packet = new SNIPacket(null);

            return handle.ReceiveAsync(ref packet);
        }

        /// <summary>
        /// Set packet data
        /// </summary>
        /// <param name="packet">SNI packet</param>
        /// <param name="data">Data</param>
        /// <param name="length">Length</param>
        public void PacketSetData(SNIPacket packet, byte[] data, int length)
        {
            packet.SetData(data, length);
        }

        /// <summary>
        /// Release packet
        /// </summary>
        /// <param name="packet">SNI packet</param>
        public void PacketRelease(SNIPacket packet)
        {
            packet.Release();
        }

        /// <summary>
        /// Check SNI handle connection
        /// </summary>
        /// <param name="handle"></param>
        /// <returns>SNI error status</returns>
        public uint CheckConnection(SNIHandle handle)
        {
            return handle.CheckConnection();
        }

        /// <summary>
        /// Get last SNI error on this thread
        /// </summary>
        /// <returns></returns>
        public SNIError GetLastError()
        {
            return SNILoadHandle.SingletonInstance.LastError;
        }
    }
}
