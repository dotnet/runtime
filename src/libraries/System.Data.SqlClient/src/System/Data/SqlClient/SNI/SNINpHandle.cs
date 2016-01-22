using System.IO;
using System.IO.Pipes;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace System.Data.SqlClient.SNI
{
    /// <summary>
    /// Named Pipe connection handle
    /// </summary>
    internal class SNINpHandle : SNIHandle
    {
        private const int BUFFER_SIZE = 4096;
        private const int MAX_PIPE_INSTANCES = 255;

        private readonly string _targetServer;
        private readonly object _callbackObject;
        private readonly uint _status = TdsEnums.SNI_UNINITIALIZED;
        private readonly Guid _connectionId = Guid.NewGuid();
        private readonly TaskScheduler _writeScheduler;
        private readonly TaskFactory _writeTaskFactory;
        private bool _validateCert = true;

        private Stream _stream;
        private NamedPipeClientStream _pipeStream;
        private SslOverTdsStream _sslOverTdsStream;
        private SslStream _sslStream;
        private SNIAsyncCallback _receiveCallback;
        private SNIAsyncCallback _sendCallback;

        public SNINpHandle(string serverName, string pipeName, long timerExpire, object callbackObject, bool async)
        {
            _targetServer = serverName;
            _callbackObject = callbackObject;
            _writeScheduler = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;
            _writeTaskFactory = new TaskFactory(_writeScheduler);

            try
            {
                _pipeStream = new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut, (async ? PipeOptions.Asynchronous : PipeOptions.WriteThrough), Security.Principal.TokenImpersonationLevel.None);

                bool isInfiniteTimeOut = long.MaxValue == timerExpire;
                if (isInfiniteTimeOut)
                {
                    _pipeStream.Connect(Threading.Timeout.Infinite);
                }
                else
                {
                    TimeSpan ts = DateTime.FromFileTime(timerExpire) - DateTime.Now;
                    ts = ts.Ticks < 0 ? TimeSpan.FromTicks(0) : ts;

                    Console.WriteLine("NP Timeout Set to: " + ts.TotalMilliseconds);
                    _pipeStream.Connect((int)ts.TotalMilliseconds);
                }
            }
            catch(TimeoutException te)
            {
                SNICommon.ReportSNIError(SNIProviders.NP_PROV, 0, SNICommon.InternalExceptionError, te.Message);
                _status = TdsEnums.SNI_WAIT_TIMEOUT;
                return;
            }
            catch(IOException ioe)
            {
                SNICommon.ReportSNIError(SNIProviders.NP_PROV, 0, SNICommon.InternalExceptionError, ioe.Message);
                _status = TdsEnums.SNI_ERROR;
                return;
            }

            if (!_pipeStream.IsConnected || !_pipeStream.CanWrite || !_pipeStream.CanRead)
            {
                SNICommon.ReportSNIError(SNIProviders.NP_PROV, 0, SNICommon.InternalExceptionError, "Named Pipe connection failed to open.");
                _status = TdsEnums.SNI_ERROR;
                return;
            }

            _sslOverTdsStream = new SslOverTdsStream(_pipeStream);
            _sslStream = new SslStream(_sslOverTdsStream, true, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);

            _stream = _pipeStream;
            _status = TdsEnums.SNI_SUCCESS;
        }

        public override Guid ConnectionId
        {
            get
            {
                return _connectionId;
            }
        }

        public override uint Status
        {
            get
            {
                return _status;
            }
        }

        public override uint CheckConnection()
        {
            if (!_stream.CanWrite || !_stream.CanRead)
            {
                return TdsEnums.SNI_ERROR;
            }
            else
            {
                return TdsEnums.SNI_SUCCESS;
            }
        }

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

                if (_pipeStream != null)
                {
                    _pipeStream.Dispose();
                    _pipeStream = null;
                }
            }
        }

        public override uint Receive(out SNIPacket packet, int timeout)
        {
            lock (this)
            {
                packet = null;
                try
                {
                    packet = new SNIPacket(null);
                    packet.Allocate(BUFFER_SIZE);
                    packet.ReadFromStream(_stream);

                    if (packet.Length == 0)
                    {
                        packet.Release();
                        return SNICommon.ReportSNIError(SNIProviders.NP_PROV, 0, SNICommon.InternalExceptionError, "Named Pipe connection was terminated");
                    }
                }
                catch (ObjectDisposedException ode)
                {
                    packet.Release();
                    return SNICommon.ReportSNIError(SNIProviders.NP_PROV, 0, SNICommon.InternalExceptionError, "Named Pipes: " + ode.Message);
                }
                catch (IOException ioe)
                {
                    packet.Release();
                    return SNICommon.ReportSNIError(SNIProviders.NP_PROV, 0, SNICommon.InternalExceptionError, "Named Pipes: " + ioe.Message);
                }

                return TdsEnums.SNI_SUCCESS;
            }
        }

        public override uint ReceiveAsync(ref SNIPacket packet)
        {
            lock (this)
            {
                packet = new SNIPacket(null);
                packet.Allocate(BUFFER_SIZE);

                try
                {
                    packet.ReadFromStreamAsync(_stream, _receiveCallback);
                    return TdsEnums.SNI_SUCCESS_IO_PENDING;
                }
                catch (ObjectDisposedException ode)
                {
                    packet.Release();
                    return SNICommon.ReportSNIError(SNIProviders.NP_PROV, 0, SNICommon.InternalExceptionError, "Named Pipes: " + ode.Message);
                }
                catch (IOException ioe)
                {
                    packet.Release();
                    return SNICommon.ReportSNIError(SNIProviders.NP_PROV, 0, SNICommon.InternalExceptionError, "Named Pipes: " + ioe.Message);
                }
            }
        }

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
                    return SNICommon.ReportSNIError(SNIProviders.NP_PROV, 0, SNICommon.InternalExceptionError, "Named Pipes: " + ode.Message);
                }
                catch (IOException ioe)
                {
                    return SNICommon.ReportSNIError(SNIProviders.NP_PROV, 0, SNICommon.InternalExceptionError, "Named Pipes: " + ioe.Message);
                }
            }
        }

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
                    SNICommon.ReportSNIError(SNIProviders.NP_PROV, 0, 0, "Named Pipes: " + e.Message);

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

        public override void SetAsyncCallbacks(SNIAsyncCallback receiveCallback, SNIAsyncCallback sendCallback)
        {
            _receiveCallback = receiveCallback;
            _sendCallback = sendCallback;
        }

        public override uint EnableSsl(uint options)
        {
            _validateCert = (options & TdsEnums.SNI_SSL_VALIDATE_CERTIFICATE) != 0;

            try
            {
                _sslStream.AuthenticateAsClientAsync(_targetServer).GetAwaiter().GetResult();
                _sslOverTdsStream.FinishHandshake();
            }
            catch (AuthenticationException aue)
            {
                return SNICommon.ReportSNIError(SNIProviders.NP_PROV, 0, SNICommon.InternalExceptionError, aue.Message);
            }
            catch (InvalidOperationException ioe)
            {
                return SNICommon.ReportSNIError(SNIProviders.NP_PROV, 0, SNICommon.InternalExceptionError, ioe.Message);
            }

            _stream = _sslStream;
            return TdsEnums.SNI_SUCCESS;
        }

        public override void DisableSsl()
        {
            _sslStream.Dispose();
            _sslStream = null;
            _sslOverTdsStream.Dispose();
            _sslOverTdsStream = null;

            _stream = _pipeStream;
        }

        /// <summary>
        /// Validate server certificate
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="cert">X.509 certificate</param>
        /// <param name="chain">X.509 chain</param>
        /// <param name="policyErrors">Policy errors</param>
        /// <returns>true if valid</returns>
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
        public override void SetBufferSize(int bufferSize)
        {
        }

#if DEBUG
        /// <summary>
        /// Test handle for killing underlying connection
        /// </summary>
        public override void KillConnection()
        {
            _pipeStream.Dispose();
            _pipeStream = null;
        }
#endif
    }
}