// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace System.Net
{
    internal class TlsStream : NetworkStream
    {
        private SslStream _sslStream;
        private string _host;
        private X509CertificateCollection _clientCertificates;

        public TlsStream(NetworkStream stream, Socket socket) : base(socket)
        {
            _sslStream = new SslStream(stream);
        }

        public TlsStream(NetworkStream stream, Socket socket, string host, X509CertificateCollection clientCertificates) : base(socket)
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

        public IAsyncResult BeginAuthenticateAsClient(AsyncCallback asyncCallback, object state)
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

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            return _sslStream.BeginWrite(buffer, offset, size, callback, state);
        }

        public override void EndWrite(IAsyncResult result)
        {
            _sslStream.EndWrite(result);
        }

        public override void Write(byte[] buffer, int offset, int size)
        {
            _sslStream.Write(buffer, offset, size);
        }

        public override int Read(byte[] buffer, int offset, int size)
        {
            return _sslStream.Read(buffer, offset, size);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
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

            if (_sslStream != null)
            {
                _sslStream.Close();
            }
        }
    }
}
