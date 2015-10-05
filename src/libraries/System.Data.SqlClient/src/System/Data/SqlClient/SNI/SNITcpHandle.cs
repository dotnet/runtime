// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace System.Data.SqlClient.SNI
{
    /// <summary>
    /// TCP connection handle
    /// </summary>
    internal class SNITCPHandle : SNIHandle
    {
        private readonly string _targetServer;
        private readonly object _callbackObject;
        private readonly Socket _socket;
        private readonly NetworkStream _tcpStream;
        private readonly TaskScheduler _writeScheduler;
        private readonly TaskFactory _writeTaskFactory;

        private Stream _stream;
        private TcpClient _tcpClient;
        private SslStream _sslStream;
        private SslOverTdsStream _sslOverTdsStream;
        private SNIAsyncCallback _receiveCallback;
        private SNIAsyncCallback _sendCallback;

        private bool _validateCert = true;
        private int _bufferSize = TdsEnums.DEFAULT_LOGIN_PACKET_SIZE;
        private uint _status = TdsEnums.SNI_UNINITIALIZED;
        private Guid _connectionId = Guid.NewGuid();

        /// <summary>
        /// Dispose object
        /// </summary>
        public override void Dispose()
        {
            lock (this)
            {
                if (_sslOverTdsStream != null)
                {
                    _sslOverTdsStream.Dispose();
                    _sslOverTdsStream = null;
                }

                if (_sslStream != null)
                {
                    _sslStream.Dispose();
                    _sslStream = null;
                }

                if (_tcpClient != null)
                {
                    _tcpClient.Dispose();
                    _tcpClient = null;
                }
            }
        }

        /// <summary>
        /// Connection ID
        /// </summary>
        public override Guid ConnectionId
        {
            get
            {
                return _connectionId;
            }
        }

        /// <summary>
        /// Connection status
        /// </summary>
        public override uint Status
        {
            get
            {
                return _status;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serverName">Server name</param>
        /// <param name="port">TCP port number</param>
        /// <param name="timerExpire">Connection timer expiration</param>
        /// <param name="callbackObject">Callback object</param>
        public SNITCPHandle(string serverName, int port, long timerExpire, object callbackObject)
        {
            _writeScheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;
            _writeTaskFactory = new TaskFactory(_writeScheduler);
            _callbackObject = callbackObject;
            _targetServer = serverName;

            try
            {
                _tcpClient = new TcpClient();

                IAsyncResult result = _tcpClient.BeginConnect(serverName, port, null, null);

                TimeSpan ts;

                // In case the Timeout is Infinite, we will receive the max value of Int64 as the tick count
                // The infinite Timeout is a function of ConnectionString Timeout=0
                bool isInfiniteTimeOut = long.MaxValue == timerExpire;
                if (!isInfiniteTimeOut)
                {
                    ts = DateTime.FromFileTime(timerExpire) - DateTime.Now;
                    ts = ts.Ticks < 0 ? TimeSpan.FromTicks(0) : ts;
                }

                if (!(isInfiniteTimeOut ? result.AsyncWaitHandle.WaitOne(-1) : result.AsyncWaitHandle.WaitOne(ts)))
                {
                    ReportTcpSNIError(0, 40, SR.SNI_ERROR_40);
                    return;
                }

                _tcpClient.EndConnect(result);

                _tcpClient.NoDelay = true;
                _tcpStream = _tcpClient.GetStream();
                _socket = _tcpClient.Client;

                _sslOverTdsStream = new SslOverTdsStream(_tcpStream);
                _sslStream = new SslStream(_sslOverTdsStream, true, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
            }
            catch (SocketException se)
            {
                ReportTcpSNIError(se.Message);
                return;
            }
            catch (Exception e)
            {
                ReportTcpSNIError(e.Message);
                return;
            }

            _stream = _tcpStream;
            _status = TdsEnums.SNI_SUCCESS;
        }

        /// <summary>
        /// Enable SSL
        /// </summary>
        public override uint EnableSsl(uint options)
        {
            _validateCert = (options & TdsEnums.SNI_SSL_VALIDATE_CERTIFICATE) != 0;

            try
            {
                _sslStream.AuthenticateAsClient(_targetServer);
                _sslOverTdsStream.FinishHandshake();
            }
            catch (AuthenticationException aue)
            {
                return ReportTcpSNIError(aue.Message);
            }
            catch (InvalidOperationException ioe)
            {
                return ReportTcpSNIError(ioe.Message);
            }

            _stream = _sslStream;
            return TdsEnums.SNI_SUCCESS;
        }

        /// <summary>
        /// Disable SSL
        /// </summary>
        public override void DisableSsl()
        {
            _sslStream.Dispose();
            _sslStream = null;
            _sslOverTdsStream.Dispose();
            _sslOverTdsStream = null;

            _stream = _tcpStream;
        }

        /// <summary>
        /// Validate server certificate callback
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="cert">X.509 certificate</param>
        /// <param name="chain">X.509 chain</param>
        /// <param name="policyErrors">Policy errors</param>
        /// <returns>True if certificate is valid</returns>
        private bool ValidateServerCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors policyErrors)
        {
            if (!_validateCert)
            {
                return true;
            }

            return SNICommon.ValidateSslServerCertificate(_targetServer, sender, cert, chain, policyErrors);
        }

        /// <summary>
        /// Set buffer size
        /// </summary>
        /// <param name="bufferSize">Buffer size</param>
        public void SetBufferSize(int bufferSize)
        {
            _bufferSize = bufferSize;
            _socket.SendBufferSize = bufferSize;
            _socket.ReceiveBufferSize = bufferSize;
        }

        /// <summary>
        /// Send a packet synchronously
        /// </summary>
        /// <param name="packet">SNI packet</param>
        /// <returns>SNI error code</returns>
        public override uint Send(SNIPacket packet)
        {
            lock (this)
            {
                try
                {
                    packet.WriteToStream(_stream);
                    return TdsEnums.SNI_SUCCESS;
                }
                catch (ObjectDisposedException ode)
                {
                    return ReportTcpSNIError(ode.Message);
                }
                catch (SocketException se)
                {
                    return ReportTcpSNIError(se.Message);
                }
                catch (IOException ioe)
                {
                    return ReportTcpSNIError(ioe.Message);
                }
            }
        }

        /// <summary>
        /// Receive a packet synchronously
        /// </summary>
        /// <param name="packet">SNI packet</param>
        /// <param name="timeout">Timeout</param>
        /// <returns>SNI error code</returns>
        public override uint Receive(ref SNIPacket packet, int timeout)
        {
            lock (this)
            {
                try
                {
                    _tcpClient.ReceiveTimeout = (timeout != 0) ? timeout : 1;
                    packet = new SNIPacket(null);
                    packet.Allocate(_bufferSize);
                    packet.ReadFromStream(_stream);

                    if (packet.Length == 0)
                    {
                        return ReportErrorAndReleasePacket(packet, "Connection was terminated");
                    }

                    return TdsEnums.SNI_SUCCESS;
                }
                catch (ObjectDisposedException ode)
                {
                    return ReportErrorAndReleasePacket(packet, ode.Message);
                }
                catch (SocketException se)
                {
                    return ReportErrorAndReleasePacket(packet, se.Message);
                }
                catch (IOException ioe)
                {
                    uint errorCode = ReportErrorAndReleasePacket(packet, ioe.Message);
                    if (ioe.InnerException is SocketException && ((SocketException)(ioe.InnerException)).SocketErrorCode == SocketError.TimedOut)
                    {
                        errorCode = TdsEnums.SNI_WAIT_TIMEOUT;
                    }

                    return errorCode;
                }
                finally
                {
                    _tcpClient.ReceiveTimeout = 0;
                }
            }
        }

        /// <summary>
        /// Set async callbacks
        /// </summary>
        /// <param name="receiveCallback">Receive callback</param>
        /// <param name="sendCallback">Send callback</param>
        /// <summary>
        public override void SetAsyncCallbacks(SNIAsyncCallback receiveCallback, SNIAsyncCallback sendCallback)
        {
            _receiveCallback = receiveCallback;
            _sendCallback = sendCallback;
        }

        /// <summary>
        /// Send a packet asynchronously
        /// </summary>
        /// <param name="packet">SNI packet</param>
        /// <param name="callback">Completion callback</param>
        /// <returns>SNI error code</returns>
        public override uint SendAsync(SNIPacket packet, SNIAsyncCallback callback = null)
        {
            SNIPacket newPacket = packet;

            _writeTaskFactory.StartNew(() =>
            {
                try
                {
                    lock (this)
                    {
                        packet.WriteToStream(_stream);
                    }
                }
                catch (Exception e)
                {
                    SNILoadHandle.SingletonInstance.LastError = new SNIError(SNIProviders.TCP_PROV, 0, 0, e.Message);

                    if (callback != null)
                    {
                        callback(packet, TdsEnums.SNI_ERROR);
                    }
                    else
                    {
                        _sendCallback(packet, TdsEnums.SNI_ERROR);
                    }

                    return;
                }

                if (callback != null)
                {
                    callback(packet, TdsEnums.SNI_SUCCESS);
                }
                else
                {
                    _sendCallback(packet, TdsEnums.SNI_SUCCESS);
                }
            });

            return TdsEnums.SNI_SUCCESS_IO_PENDING;
        }

        /// <summary>
        /// Receive a packet asynchronously
        /// </summary>
        /// <param name="packet">SNI packet</param>
        /// <returns>SNI error code</returns>
        public override uint ReceiveAsync(ref SNIPacket packet)
        {
            lock (this)
            {
                packet = new SNIPacket(null);
                packet.Allocate(_bufferSize);

                try
                {
                    packet.ReadFromStreamAsync(_stream, _receiveCallback);
                    return TdsEnums.SNI_SUCCESS_IO_PENDING;
                }
                catch (ObjectDisposedException ode)
                {
                    return ReportErrorAndReleasePacket(packet, ode.Message);
                }
                catch (SocketException se)
                {
                    return ReportErrorAndReleasePacket(packet, se.Message);
                }
                catch (IOException ioe)
                {
                    return ReportErrorAndReleasePacket(packet, ioe.Message);
                }
            }
        }

        /// <summary>
        /// Check SNI handle connection
        /// </summary>
        /// <param name="handle"></param>
        /// <returns>SNI error status</returns>
        public override uint CheckConnection()
        {
            try
            {
                if (!_socket.Connected || _socket.Poll(0, SelectMode.SelectError))
                {
                    return TdsEnums.SNI_ERROR;
                }
            }
            catch (SocketException se)
            {
                return ReportTcpSNIError(se.Message);
            }
            catch (ObjectDisposedException ode)
            {
                return ReportTcpSNIError(ode.Message);
            }

            return TdsEnums.SNI_SUCCESS;
        }

        private uint ReportTcpSNIError(uint nativeError, uint sniError, string errorMessage)
        {
            _status = TdsEnums.SNI_ERROR;
            return SNICommon.ReportSNIError(SNIProviders.TCP_PROV, nativeError, sniError, errorMessage);
        }

        private uint ReportTcpSNIError(string errorMessage)
        {
            return ReportTcpSNIError(0, 0, errorMessage);
        }

        private uint ReportErrorAndReleasePacket(SNIPacket packet, string errorMessage)
        {
            packet.Release();
            return ReportTcpSNIError(0, 0, errorMessage);
        }

#if DEBUG
        /// <summary>
        /// Test handle for killing underlying connection
        /// </summary>
        public override void KillConnection()
        {
            _socket.Shutdown(SocketShutdown.Both);
        }
#endif		
    }
}