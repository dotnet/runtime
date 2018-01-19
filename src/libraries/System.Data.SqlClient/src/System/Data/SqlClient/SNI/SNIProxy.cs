// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace System.Data.SqlClient.SNI
{
    /// <summary>
    /// Managed SNI proxy implementation. Contains many SNI entry points used by SqlClient.
    /// </summary>
    internal class SNIProxy
    {
        private const int DefaultSqlServerPort = 1433;
        private const int DefaultSqlServerDacPort = 1434;
        private const string SqlServerSpnHeader = "MSSQLSvc";

        internal class SspiClientContextResult
        {
            internal const uint OK = 0;
            internal const uint Failed = 1;
            internal const uint KerberosTicketMissing = 2;
        }

        public static readonly SNIProxy Singleton = new SNIProxy();

        /// <summary>
        /// Terminate SNI
        /// </summary>
        public void Terminate()
        {
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
        public void GenSspiClientContext(SspiClientContextStatus sspiClientContextStatus, byte[] receivedBuff, ref byte[] sendBuff, byte[] serverName)
        {
            SafeDeleteContext securityContext = sspiClientContextStatus.SecurityContext;
            ContextFlagsPal contextFlags = sspiClientContextStatus.ContextFlags;
            SafeFreeCredentials credentialsHandle = sspiClientContextStatus.CredentialsHandle;

            string securityPackage = NegotiationInfoClass.Negotiate;

            if (securityContext == null)
            {
                credentialsHandle = NegotiateStreamPal.AcquireDefaultCredential(securityPackage, false);
            }

            SecurityBuffer[] inSecurityBufferArray = null;
            if (receivedBuff != null)
            {
                inSecurityBufferArray = new SecurityBuffer[] { new SecurityBuffer(receivedBuff, SecurityBufferType.SECBUFFER_TOKEN) };
            }
            else
            {
                inSecurityBufferArray = new SecurityBuffer[] { };
            }

            int tokenSize = NegotiateStreamPal.QueryMaxTokenSize(securityPackage);
            SecurityBuffer outSecurityBuffer = new SecurityBuffer(tokenSize, SecurityBufferType.SECBUFFER_TOKEN);

            ContextFlagsPal requestedContextFlags = ContextFlagsPal.Connection
                | ContextFlagsPal.Confidentiality
                | ContextFlagsPal.Delegate
                | ContextFlagsPal.MutualAuth;

            string serverSPN = System.Text.Encoding.UTF8.GetString(serverName);

            SecurityStatusPal statusCode = NegotiateStreamPal.InitializeSecurityContext(
                       credentialsHandle,
                       ref securityContext,
                       serverSPN,
                       requestedContextFlags,
                       inSecurityBufferArray,
                       outSecurityBuffer,
                       ref contextFlags);

            if (statusCode.ErrorCode == SecurityStatusPalErrorCode.CompleteNeeded ||
                statusCode.ErrorCode == SecurityStatusPalErrorCode.CompAndContinue)
            {
                inSecurityBufferArray = new SecurityBuffer[] { outSecurityBuffer };
                statusCode = NegotiateStreamPal.CompleteAuthToken(ref securityContext, inSecurityBufferArray);
                outSecurityBuffer.token = null;
            }

            sendBuff = outSecurityBuffer.token;
            if (sendBuff == null)
            {
                sendBuff = Array.Empty<byte>();
            }

            sspiClientContextStatus.SecurityContext = securityContext;
            sspiClientContextStatus.ContextFlags = contextFlags;
            sspiClientContextStatus.CredentialsHandle = credentialsHandle;

            if (IsErrorStatus(statusCode.ErrorCode))
            {
                // Could not access Kerberos Ticket.
                //
                // SecurityStatusPalErrorCode.InternalError only occurs in Unix and always comes with a GssApiException,
                // so we don't need to check for a GssApiException here.
                if (statusCode.ErrorCode == SecurityStatusPalErrorCode.InternalError)
                {
                    throw new InvalidOperationException(SQLMessage.KerberosTicketMissingError() + "\n" + statusCode);
                }
                else
                {
                    throw new InvalidOperationException(SQLMessage.SSPIGenerateError() + "\n" + statusCode);
                }
            }
        }

        private static bool IsErrorStatus(SecurityStatusPalErrorCode errorCode)
        {
            return errorCode != SecurityStatusPalErrorCode.NotSet &&
                errorCode != SecurityStatusPalErrorCode.OK &&
                errorCode != SecurityStatusPalErrorCode.ContinueNeeded &&
                errorCode != SecurityStatusPalErrorCode.CompleteNeeded &&
                errorCode != SecurityStatusPalErrorCode.CompAndContinue &&
                errorCode != SecurityStatusPalErrorCode.ContextExpired &&
                errorCode != SecurityStatusPalErrorCode.CredentialsNeeded &&
                errorCode != SecurityStatusPalErrorCode.Renegotiate;
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
        public SNIHandle CreateConnectionHandle(object callbackObject, string fullServerName, bool ignoreSniOpenTimeout, long timerExpire, out byte[] instanceName, ref byte[] spnBuffer, bool flushCache, bool async, bool parallel, bool isIntegratedSecurity)
        {
            instanceName = new byte[1];

            bool errorWithLocalDBProcessing;
            string localDBDataSource = GetLocalDBDataSource(fullServerName, out errorWithLocalDBProcessing);

            if (errorWithLocalDBProcessing)
            {
                return null;
            }

            // If a localDB Data source is available, we need to use it.
            fullServerName = localDBDataSource ?? fullServerName;

            DataSource details = DataSource.ParseServerName(fullServerName);
            if (details == null)
            {
                return null;
            }

            SNIHandle sniHandle = null;
            switch (details.ConnectionProtocol)
            {
                case DataSource.Protocol.Admin:
                case DataSource.Protocol.None: // default to using tcp if no protocol is provided
                case DataSource.Protocol.TCP:
                    sniHandle = CreateTcpHandle(details, timerExpire, callbackObject, parallel);
                    break;
                case DataSource.Protocol.NP:
                    sniHandle = CreateNpHandle(details, timerExpire, callbackObject, parallel);
                    break;
                default:
                    Debug.Fail($"Unexpected connection protocol: {details.ConnectionProtocol}");
                    break;
            }

            if (isIntegratedSecurity)
            {
                try
                {
                    spnBuffer = GetSqlServerSPN(details);
                }
                catch (Exception e)
                {
                    SNILoadHandle.SingletonInstance.LastError = new SNIError(SNIProviders.INVALID_PROV, SNICommon.ErrorSpnLookup, e);
                }
            }

            return sniHandle;
        }

        private static byte[] GetSqlServerSPN(DataSource dataSource)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(dataSource.ServerName));

            string hostName = dataSource.ServerName;
            string postfix = null;
            if (dataSource.Port != -1)
            {
                postfix = dataSource.Port.ToString();
            }
            else if (!string.IsNullOrWhiteSpace(dataSource.InstanceName))
            {
                postfix = dataSource.InstanceName;
            }
            // For handling tcp:<hostname> format
            else if (dataSource.ConnectionProtocol == DataSource.Protocol.TCP)
            {
                postfix = DefaultSqlServerPort.ToString();
            }

            return GetSqlServerSPN(hostName, postfix);
        }

        private static byte[] GetSqlServerSPN(string hostNameOrAddress, string portOrInstanceName)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(hostNameOrAddress));
            IPHostEntry hostEntry = Dns.GetHostEntry(hostNameOrAddress);
            string fullyQualifiedDomainName = hostEntry.HostName;
            string serverSpn = SqlServerSpnHeader + "/" + fullyQualifiedDomainName;
            if (!string.IsNullOrWhiteSpace(portOrInstanceName))
            {
                serverSpn += ":" + portOrInstanceName;
            }
            return Encoding.UTF8.GetBytes(serverSpn);
        }

        /// <summary>
        /// Creates an SNITCPHandle object
        /// </summary>
        /// <param name="fullServerName">Server string. May contain a comma delimited port number.</param>
        /// <param name="timerExpire">Timer expiration</param>
        /// <param name="callbackObject">Asynchronous I/O callback object</param>
        /// <param name="parallel">Should MultiSubnetFailover be used</param>
        /// <returns>SNITCPHandle</returns>
        private SNITCPHandle CreateTcpHandle(DataSource details, long timerExpire, object callbackObject, bool parallel)
        {
            // TCP Format: 
            // tcp:<host name>\<instance name>
            // tcp:<host name>,<TCP/IP port number>

            string hostName = details.ServerName;
            if (string.IsNullOrWhiteSpace(hostName))
            {
                SNILoadHandle.SingletonInstance.LastError = new SNIError(SNIProviders.TCP_PROV, 0, SNICommon.InvalidConnStringError, string.Empty);
                return null;
            }

            int port = -1;
            bool isAdminConnection = details.ConnectionProtocol == DataSource.Protocol.Admin;
            if (details.IsSsrpRequired)
            {
                try
                {
                    port = isAdminConnection ?
                            SSRP.GetDacPortByInstanceName(hostName, details.InstanceName) :
                            SSRP.GetPortByInstanceName(hostName, details.InstanceName);
                }
                catch (SocketException se)
                {
                    SNILoadHandle.SingletonInstance.LastError = new SNIError(SNIProviders.TCP_PROV, SNICommon.InvalidConnStringError, se);
                    return null;
                }
            }
            else if (details.Port != -1)
            {
                port = details.Port;
            }
            else
            {
                port = isAdminConnection ? DefaultSqlServerDacPort : DefaultSqlServerPort;
            }

            return new SNITCPHandle(hostName, port, timerExpire, callbackObject, parallel);
        }



        /// <summary>
        /// Creates an SNINpHandle object
        /// </summary>
        /// <param name="fullServerName">Server string representing a UNC pipe path.</param>
        /// <param name="timerExpire">Timer expiration</param>
        /// <param name="callbackObject">Asynchronous I/O callback object</param>
        /// <param name="parallel">Should MultiSubnetFailover be used. Only returns an error for named pipes.</param>
        /// <returns>SNINpHandle</returns>
        private SNINpHandle CreateNpHandle(DataSource details, long timerExpire, object callbackObject, bool parallel)
        {
            if (parallel)
            {
                SNICommon.ReportSNIError(SNIProviders.NP_PROV, 0, SNICommon.MultiSubnetFailoverWithNonTcpProtocol, string.Empty);
                return null;
            }
            return new SNINpHandle(details.PipeHostName, details.PipeName, timerExpire, callbackObject);
        }

        /// <summary>
        /// Read packet asynchronously
        /// </summary>
        /// <param name="handle">SNI handle</param>
        /// <param name="packet">Packet</param>
        /// <returns>SNI error status</returns>
        public uint ReadAsync(SNIHandle handle, out SNIPacket packet)
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

        /// <summary>
        /// Gets the Local db Named pipe data source if the input is a localDB server. 
        /// </summary>
        /// <param name="fullServerName">The data source</param>
        /// <param name="error">Set true when an error occurred while getting LocalDB up</param>
        /// <returns></returns>
        private string GetLocalDBDataSource(string fullServerName, out bool error)
        {
            string localDBConnectionString = null;
            bool isBadLocalDBDataSource;
            string localDBInstance = DataSource.GetLocalDBInstance(fullServerName, out isBadLocalDBDataSource);

            if (isBadLocalDBDataSource)
            {
                error = true;
                return null;
            }

            else if (!string.IsNullOrEmpty(localDBInstance))
            {
                // We have successfully received a localDBInstance which is valid.
                Debug.Assert(!string.IsNullOrWhiteSpace(localDBInstance), "Local DB Instance name cannot be empty.");
                localDBConnectionString = LocalDB.GetLocalDBConnectionString(localDBInstance);

                if (fullServerName == null)
                {
                    // The Last error is set in LocalDB.GetLocalDBConnectionString. We don't need to set Last here.
                    error = true;
                    return null;
                }
            }
            error = false;
            return localDBConnectionString;
        }
    }

    internal class DataSource
    {
        private const char CommaSeparator = ',';
        private const char BackSlashSeparator = '\\';
        private const string DefaultHostName = "localhost";
        private const string DefaultSqlServerInstanceName = "mssqlserver";
        private const string PipeBeginning = @"\\";
        private const string PipeToken = "pipe";
        private const string LocalDbHost = "(localdb)";
        private const string NamedPipeInstanceNameHeader = "mssql$";
        private const string DefaultPipeName = "sql\\query";

        internal enum Protocol { TCP, NP, None, Admin };

        internal Protocol ConnectionProtocol = Protocol.None;

        /// <summary>
        /// Provides the HostName of the server to connect to for TCP protocol. 
        /// This information is also used for finding the SPN of SqlServer
        /// </summary>
        internal string ServerName { get; private set; }

        /// <summary>
        /// Provides the port on which the TCP connection should be made if one was specified in Data Source
        /// </summary>
        internal int Port { get; private set; } = -1;

        /// <summary>
        /// Provides the inferred Instance Name from Server Data Source
        /// </summary>
        public string InstanceName { get; internal set; }

        /// <summary>
        /// Provides the pipe name in case of Named Pipes
        /// </summary>
        public string PipeName { get; internal set; }

        /// <summary>
        /// Provides the HostName to connect to in case of Named pipes Data Source
        /// </summary>
        public string PipeHostName { get; internal set; }

        private string _workingDataSource;
        private string _dataSourceAfterTrimmingProtocol;
        internal bool IsBadDataSource { get; private set; } = false;

        internal bool IsSsrpRequired { get; private set; } = false;

        private DataSource(string dataSource)
        {
            // Remove all whitespaces from the datasource and all operations will happen on lower case.
            _workingDataSource = dataSource.Trim().ToLowerInvariant();

            int firstIndexOfColon = _workingDataSource.IndexOf(':');

            PopulateProtocol();

            _dataSourceAfterTrimmingProtocol = (firstIndexOfColon > -1) && ConnectionProtocol != DataSource.Protocol.None
                ? _workingDataSource.Substring(firstIndexOfColon + 1).Trim() : _workingDataSource;

            if (_dataSourceAfterTrimmingProtocol.Contains("/")) // Pipe paths only allow back slashes
            {
                if (ConnectionProtocol == DataSource.Protocol.None)
                    ReportSNIError(SNIProviders.INVALID_PROV);
                else if (ConnectionProtocol == DataSource.Protocol.NP)
                    ReportSNIError(SNIProviders.NP_PROV);
                else if (ConnectionProtocol == DataSource.Protocol.TCP)
                    ReportSNIError(SNIProviders.TCP_PROV);
            }
        }

        private void PopulateProtocol()
        {
            string[] splitByColon = _workingDataSource.Split(':');

            if (splitByColon.Length <= 1)
            {
                ConnectionProtocol = DataSource.Protocol.None;
            }
            else
            {
                // We trim before switching because " tcp : server , 1433 " is a valid data source
                switch (splitByColon[0].Trim())
                {
                    case TdsEnums.TCP:
                        ConnectionProtocol = DataSource.Protocol.TCP;
                        break;
                    case TdsEnums.NP:
                        ConnectionProtocol = DataSource.Protocol.NP;
                        break;
                    case TdsEnums.ADMIN:
                        ConnectionProtocol = DataSource.Protocol.Admin;
                        break;
                    default:
                        // None of the supported protocols were found. This may be a IPv6 address
                        ConnectionProtocol = DataSource.Protocol.None;
                        break;
                }
            }
        }

        public static string GetLocalDBInstance(string dataSource, out bool error)
        {
            string instanceName = null;

            string workingDataSource = dataSource.ToLowerInvariant();

            string[] tokensByBackSlash = workingDataSource.Split(BackSlashSeparator);

            error = false;

            // All LocalDb endpoints are of the format host\instancename where host is always (LocalDb) (case-insensitive)
            if (tokensByBackSlash.Length == 2 && LocalDbHost.Equals(tokensByBackSlash[0].TrimStart()))
            {
                if (!string.IsNullOrWhiteSpace(tokensByBackSlash[1]))
                {
                    instanceName = tokensByBackSlash[1].Trim();
                }
                else
                {
                    SNILoadHandle.SingletonInstance.LastError = new SNIError(SNIProviders.INVALID_PROV, 0, SNICommon.LocalDBNoInstanceName, string.Empty);
                    error = true;
                    return null;
                }
            }

            return instanceName;
        }

        public static DataSource ParseServerName(string dataSource)
        {
            DataSource details = new DataSource(dataSource);

            if (details.IsBadDataSource)
            {
                return null;
            }

            if (details.InferNamedPipesInformation())
            {
                return details;
            }

            if (details.IsBadDataSource)
            {
                return null;
            }

            if (details.InferConnectionDetails())
            {
                return details;
            }

            return null;
        }

        private void InferLocalServerName()
        {
            // If Server name is empty or localhost, then use "localhost"
            if (string.IsNullOrEmpty(ServerName) || IsLocalHost(ServerName))
            {
                ServerName = ConnectionProtocol == DataSource.Protocol.Admin ?
                    Environment.MachineName : DefaultHostName;
            }
        }

        private bool InferConnectionDetails()
        {
            string[] tokensByCommaAndSlash = _dataSourceAfterTrimmingProtocol.Split(BackSlashSeparator, ',');
            ServerName = tokensByCommaAndSlash[0].Trim();

            int commaIndex = _dataSourceAfterTrimmingProtocol.IndexOf(',');

            int backSlashIndex = _dataSourceAfterTrimmingProtocol.IndexOf(BackSlashSeparator);

            // Check the parameters. The parameters are Comma separated in the Data Source. The parameter we really care about is the port
            // If Comma exists, the try to get the port number
            if (commaIndex > -1)
            {
                string parameter = backSlashIndex > -1
                        ? ((commaIndex > backSlashIndex) ? tokensByCommaAndSlash[2].Trim() : tokensByCommaAndSlash[1].Trim())
                        : tokensByCommaAndSlash[1].Trim();

                // Bad Data Source like "server, "
                if (string.IsNullOrEmpty(parameter))
                {
                    ReportSNIError(SNIProviders.INVALID_PROV);
                    return false;
                }

                // For Tcp and Only Tcp are parameters allowed.
                if (ConnectionProtocol == DataSource.Protocol.None)
                {
                    ConnectionProtocol = DataSource.Protocol.TCP;
                }
                else if (ConnectionProtocol != DataSource.Protocol.TCP)
                {
                    // Parameter has been specified for non-TCP protocol. This is not allowed.
                    ReportSNIError(SNIProviders.INVALID_PROV);
                    return false;
                }

                int port;
                if (!int.TryParse(parameter, out port))
                {
                    ReportSNIError(SNIProviders.TCP_PROV);
                    return false;
                }

                // If the user explicitly specified a invalid port in the connection string.
                if (port < 1)
                {
                    ReportSNIError(SNIProviders.TCP_PROV);
                    return false;
                }

                Port = port;
            }
            // Instance Name Handling. Only if we found a '\' and we did not find a port in the Data Source
            else if (backSlashIndex > -1)
            {
                // This means that there will not be any part separated by comma. 
                InstanceName = tokensByCommaAndSlash[1].Trim();

                if (string.IsNullOrWhiteSpace(InstanceName))
                {
                    ReportSNIError(SNIProviders.INVALID_PROV);
                    return false;
                }

                if (DefaultSqlServerInstanceName.Equals(InstanceName))
                {
                    ReportSNIError(SNIProviders.INVALID_PROV);
                    return false;
                }

                IsSsrpRequired = true;
            }

            InferLocalServerName();

            return true;
        }

        private void ReportSNIError(SNIProviders provider)
        {
            SNILoadHandle.SingletonInstance.LastError = new SNIError(provider, 0, SNICommon.InvalidConnStringError, string.Empty);
            IsBadDataSource = true;
        }

        private bool InferNamedPipesInformation()
        {
            // If we have a datasource beginning with a pipe or we have already determined that the protocol is Named Pipe
            if (_dataSourceAfterTrimmingProtocol.StartsWith(PipeBeginning) || ConnectionProtocol == Protocol.NP)
            {
                // If the data source is "np:servername"
                if (!_dataSourceAfterTrimmingProtocol.Contains(BackSlashSeparator))
                {
                    PipeHostName = ServerName = _dataSourceAfterTrimmingProtocol;
                    InferLocalServerName();
                    PipeName = SNINpHandle.DefaultPipePath;
                    return true;
                }

                try
                {
                    string[] tokensByBackSlash = _dataSourceAfterTrimmingProtocol.Split(BackSlashSeparator);

                    // The datasource is of the format \\host\pipe\sql\query [0]\[1]\[2]\[3]\[4]\[5]
                    // It would at least have 6 parts. 
                    // Another valid Sql named pipe for an named instance is \\.\pipe\MSSQL$MYINSTANCE\sql\query
                    if (tokensByBackSlash.Length < 6)
                    {
                        ReportSNIError(SNIProviders.NP_PROV);
                        return false;
                    }

                    string host = tokensByBackSlash[2];

                    if (string.IsNullOrEmpty(host))
                    {
                        ReportSNIError(SNIProviders.NP_PROV);
                        return false;
                    }

                    //Check if the "pipe" keyword is the first part of path
                    if (!PipeToken.Equals(tokensByBackSlash[3]))
                    {
                        ReportSNIError(SNIProviders.NP_PROV);
                        return false;
                    }

                    if (tokensByBackSlash[4].StartsWith(NamedPipeInstanceNameHeader))
                    {
                        InstanceName = tokensByBackSlash[4].Substring(NamedPipeInstanceNameHeader.Length);
                    }

                    StringBuilder pipeNameBuilder = new StringBuilder();

                    for (int i = 4; i < tokensByBackSlash.Length - 1; i++)
                    {
                        pipeNameBuilder.Append(tokensByBackSlash[i]);
                        pipeNameBuilder.Append(Path.DirectorySeparatorChar);
                    }
                    // Append the last part without a "/"
                    pipeNameBuilder.Append(tokensByBackSlash[tokensByBackSlash.Length - 1]);
                    PipeName = pipeNameBuilder.ToString();

                    if (string.IsNullOrWhiteSpace(InstanceName) && !DefaultPipeName.Equals(PipeName))
                    {
                        InstanceName = PipeToken + PipeName;
                    }

                    ServerName = IsLocalHost(host) ? Environment.MachineName : host;
                    // Pipe hostname is the hostname after leading \\ which should be passed down as is to open Named Pipe.
                    // For Named Pipes the ServerName makes sense for SPN creation only.
                    PipeHostName = host;
                }
                catch (UriFormatException)
                {
                    ReportSNIError(SNIProviders.NP_PROV);
                    return false;
                }

                // DataSource is something like "\\pipename"
                if (ConnectionProtocol == DataSource.Protocol.None)
                {
                    ConnectionProtocol = DataSource.Protocol.NP;
                }
                else if (ConnectionProtocol != DataSource.Protocol.NP)
                {
                    // In case the path began with a "\\" and protocol was not Named Pipes
                    ReportSNIError(SNIProviders.NP_PROV);
                    return false;
                }
                return true;
            }
            return false;
        }

        private static bool IsLocalHost(string serverName)
            => ".".Equals(serverName) || "(local)".Equals(serverName) || "localhost".Equals(serverName);
    }
}
