// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    internal sealed class TlsStream : NetworkStream
    {
        private readonly SslStream _sslStream;
        private readonly string _host;
        private readonly X509CertificateCollection? _clientCertificates;

        public TlsStream(NetworkStream stream, Socket socket, string host, X509CertificateCollection? clientCertificates) : base(socket)
        {
            _sslStream = new SslStream(stream, false, ServicePointManager.ServerCertificateValidationCallback);
            _host = host;
            _clientCertificates = clientCertificates;
        }

        public void AuthenticateAsClient()
        {
            _sslStream.AuthenticateAsClient(
                _host,
                _clientCertificates,
                (SslProtocols)ServicePointManager.SecurityProtocol, // enums use same values
                ServicePointManager.CheckCertificateRevocationList);
        }

        public IAsyncResult BeginAuthenticateAsClient(AsyncCallback? asyncCallback, object? state)
        {
            return _sslStream.BeginAuthenticateAsClient(
                _host,
                _clientCertificates,
                (SslProtocols)ServicePointManager.SecurityProtocol, // enums use same values
                ServicePointManager.CheckCertificateRevocationList,
                asyncCallback,
                state);
        }

        public void EndAuthenticateAsClient(IAsyncResult asyncResult)
        {
            _sslStream.EndAuthenticateAsClient(asyncResult);
        }

        public override void Write(byte[] buffer, int offset, int size)
        {
            _sslStream.Write(buffer, offset, size);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback? callback, object? state)
        {
            return _sslStream.BeginWrite(buffer, offset, size, callback, state);
        }

        public override void EndWrite(IAsyncResult result)
        {
            _sslStream.EndWrite(result);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _sslStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int size)
        {
            return _sslStream.Read(buffer, offset, size);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _sslStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return _sslStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _sslStream.EndRead(asyncResult);
        }

        public override void Close()
        {
            base.Close();

            _sslStream?.Close();
        }
    }
}
