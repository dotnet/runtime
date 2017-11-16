// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace System.Net
{
    internal sealed class SafeDeleteSslContext : SafeDeleteContext
    {
        private SafeSslHandle _sslContext;
        private Interop.AppleCrypto.SSLReadFunc _readCallback;
        private Interop.AppleCrypto.SSLWriteFunc _writeCallback;
        private Queue<byte> _fromConnection = new Queue<byte>();
        private Queue<byte> _toConnection = new Queue<byte>();

        public SafeSslHandle SslContext => _sslContext;

        public SafeDeleteSslContext(SafeFreeSslCredentials credential, SslAuthenticationOptions sslAuthenticationOptions)
            : base(credential)
        {
            Debug.Assert((null != credential) && !credential.IsInvalid, "Invalid credential used in SafeDeleteSslContext");

            try
            {
                unsafe
                {
                    _readCallback = ReadFromConnection;
                    _writeCallback = WriteToConnection;
                }

                _sslContext = CreateSslContext(credential, sslAuthenticationOptions.IsServer);

                int osStatus = Interop.AppleCrypto.SslSetIoCallbacks(
                    _sslContext,
                    _readCallback,
                    _writeCallback);

                if (osStatus != 0)
                {
                    throw Interop.AppleCrypto.CreateExceptionForOSStatus(osStatus);
                }
            }
            catch (Exception ex)
            {
                Debug.Write("Exception Caught. - " + ex);
                Dispose();
                throw;
            }
        }

        private static SafeSslHandle CreateSslContext(SafeFreeSslCredentials credential, bool isServer)
        {
            switch (credential.Policy)
            {
                case EncryptionPolicy.RequireEncryption:
                case EncryptionPolicy.AllowNoEncryption:
                    // SecureTransport doesn't allow TLS_NULL_NULL_WITH_NULL, but
                    // since AllowNoEncryption intersect OS-supported isn't nothing,
                    // let it pass.
                    break;
                default:
                    throw new PlatformNotSupportedException(SR.net_encryptionpolicy_notsupported);
            }

            SafeSslHandle sslContext = Interop.AppleCrypto.SslCreateContext(isServer ? 1 : 0);

            try
            {
                if (sslContext.IsInvalid)
                {
                    // This is as likely as anything.  No error conditions are defined for
                    // the OS function, and our shim only adds a NULL if isServer isn't a normalized bool.
                    throw new OutOfMemoryException();
                }

                // Let None mean "system default"
                if (credential.Protocols != SslProtocols.None)
                {
                    SetProtocols(sslContext, credential.Protocols);
                }

                if (credential.Certificate != null)
                {
                    SetCertificate(sslContext, credential.Certificate);
                }

                Interop.AppleCrypto.SslBreakOnServerAuth(sslContext, true);
                Interop.AppleCrypto.SslBreakOnClientAuth(sslContext, true);
            }
            catch
            {
                sslContext.Dispose();
                throw;
            }

            return sslContext;
        }

        public override bool IsInvalid => _sslContext?.IsInvalid ?? true;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (null != _sslContext)
                {
                    _sslContext.Dispose();
                    _sslContext = null;
                }

                _toConnection = null;
                _fromConnection = null;
                _writeCallback = null;
                _readCallback = null;
            }

            base.Dispose(disposing);
        }

        private unsafe int WriteToConnection(void* connection, byte* data, void** dataLength)
        {
            ulong toWrite = (ulong)*dataLength;
            byte* readFrom = data;

            lock (_toConnection)
            {
                while (toWrite > 0)
                {
                    _toConnection.Enqueue(*readFrom);
                    readFrom++;
                    toWrite--;
                }
            }

            // Since we can enqueue everything, no need to re-assign *dataLength.
            const int noErr = 0;
            return noErr;
        }

        private unsafe int ReadFromConnection(void* connection, byte* data, void** dataLength)
        {
            const int noErr = 0;
            const int errSSLWouldBlock = -9803;

            ulong toRead = (ulong)*dataLength;

            if (toRead == 0)
            {

                return noErr;
            }

            uint transferred = 0;

            lock (_fromConnection)
            {

                if (_fromConnection.Count == 0)
                {

                    *dataLength = (void*)0;
                    return errSSLWouldBlock;
                }

                byte* writePos = data;

                while (transferred < toRead && _fromConnection.Count > 0)
                {
                    *writePos = _fromConnection.Dequeue();
                    writePos++;
                    transferred++;
                }
            }

            *dataLength = (void*)transferred;
            return noErr;
        }

        internal void Write(byte[] buf, int offset, int count)
        {
            Debug.Assert(buf != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(count <= buf.Length - offset);


            lock (_fromConnection)
            {
                for (int i = 0; i < count; i++)
                {
                    _fromConnection.Enqueue(buf[offset + i]);
                }
            }

        }

        internal int BytesReadyForConnection => _toConnection.Count;

        internal byte[] ReadPendingWrites()
        {
            lock (_toConnection)
            {
                if (_toConnection.Count == 0)
                {
                    return null;
                }

                byte[] data = _toConnection.ToArray();
                _toConnection.Clear();

                return data;
            }
        }

        internal int ReadPendingWrites(byte[] buf, int offset, int count)
        {
            Debug.Assert(buf != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(count <= buf.Length - offset);

            lock (_toConnection)
            {
                int limit = Math.Min(count, _toConnection.Count);

                for (int i = 0; i < limit; i++)
                {
                    buf[offset + i] = _toConnection.Dequeue();
                }

                return limit;
            }
        }

        private static void SetProtocols(SafeSslHandle sslContext, SslProtocols protocols)
        {
            const SslProtocols SupportedProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
            SslProtocols minProtocolId;
            SslProtocols maxProtocolId;

            switch (protocols & SupportedProtocols)
            {
                case SslProtocols.None:
                    throw new PlatformNotSupportedException(SR.net_securityprotocolnotsupported);
                case SslProtocols.Tls:
                    minProtocolId = SslProtocols.Tls;
                    maxProtocolId = SslProtocols.Tls;
                    break;
                case SslProtocols.Tls11:
                    minProtocolId = SslProtocols.Tls11;
                    maxProtocolId = SslProtocols.Tls11;
                    break;
                case SslProtocols.Tls12:
                    minProtocolId = SslProtocols.Tls12;
                    maxProtocolId = SslProtocols.Tls12;
                    break;
                case SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12:
                    minProtocolId = SslProtocols.Tls;
                    maxProtocolId = SslProtocols.Tls12;
                    break;
                case SslProtocols.Tls11 | SslProtocols.Tls12:
                    minProtocolId = SslProtocols.Tls11;
                    maxProtocolId = SslProtocols.Tls12;
                    break;
                case SslProtocols.Tls | SslProtocols.Tls11:
                    minProtocolId = SslProtocols.Tls;
                    maxProtocolId = SslProtocols.Tls11;
                    break;
                default:
                    throw new PlatformNotSupportedException(SR.net_security_sslprotocol_contiguous);
            }

            Interop.AppleCrypto.SslSetMinProtocolVersion(sslContext, minProtocolId);
            Interop.AppleCrypto.SslSetMaxProtocolVersion(sslContext, maxProtocolId);
        }

        private static void SetCertificate(SafeSslHandle sslContext, X509Certificate2 certificate)
        {
            Debug.Assert(sslContext != null, "sslContext != null");
            Debug.Assert(certificate != null, "certificate != null");
            Debug.Assert(certificate.HasPrivateKey, "certificate.HasPrivateKey");

            X509Chain chain = TLSCertificateExtensions.BuildNewChain(
                certificate,
                includeClientApplicationPolicy: false);

            using (chain)
            {
                X509ChainElementCollection elements = chain.ChainElements;

                // We need to leave off the EE (first) and root (last) certificate from the intermediates.
                X509Certificate2[] intermediateCerts = elements.Count < 3
                    ? Array.Empty<X509Certificate2>()
                    : new X509Certificate2[elements.Count - 2];

                // Build an array which is [
                //   SecIdentityRef for EE cert,
                //   SecCertificateRef for intermed0,
                //   SecCertificateREf for intermed1,
                //   ...
                // ]
                IntPtr[] ptrs = new IntPtr[intermediateCerts.Length + 1];

                for (int i = 0; i < intermediateCerts.Length; i++)
                {
                    X509Certificate2 intermediateCert = elements[i + 1].Certificate;

                    if (intermediateCert.HasPrivateKey)
                    {
                        // In the unlikely event that we get a certificate with a private key from
                        // a chain, clear it to the certificate.
                        //
                        // The current value of intermediateCert is still in elements, which will
                        // get Disposed at the end of this method.  The new value will be
                        // in the intermediate certs array, which also gets serially Disposed.
                        intermediateCert = new X509Certificate2(intermediateCert.RawData);
                    }

                    intermediateCerts[i] = intermediateCert;
                    ptrs[i + 1] = intermediateCert.Handle;
                }

                ptrs[0] = certificate.Handle;

                Interop.AppleCrypto.SslSetCertificate(sslContext, ptrs);

                // The X509Chain created all new certs for us, so Dispose them.
                // And since the intermediateCerts could have been new instances, Dispose them, too
                for (int i = 0; i < elements.Count; i++)
                {
                    elements[i].Certificate.Dispose();

                    if (i < intermediateCerts.Length)
                    {
                        intermediateCerts[i].Dispose();
                    }
                }
            }
        }
    }
}
