// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

using PAL_KeyAlgorithm = Interop.AndroidCrypto.PAL_KeyAlgorithm;
using PAL_SSLStreamStatus = Interop.AndroidCrypto.PAL_SSLStreamStatus;

namespace System.Net
{
    internal sealed class SafeDeleteSslContext : SafeDeleteContext
    {
        private const int InitialBufferSize = 2048;

        private readonly SafeSslHandle _sslContext;
        private readonly Interop.AndroidCrypto.SSLReadCallback _readCallback;
        private readonly Interop.AndroidCrypto.SSLWriteCallback _writeCallback;

        private ArrayBuffer _inputBuffer = new ArrayBuffer(InitialBufferSize);
        private ArrayBuffer _outputBuffer = new ArrayBuffer(InitialBufferSize);

        public SafeSslHandle SslContext => _sslContext;

        public SafeDeleteSslContext(SafeFreeSslCredentials credential, SslAuthenticationOptions authOptions)
            : base(credential)
        {
            Debug.Assert((credential != null) && !credential.IsInvalid, "Invalid credential used in SafeDeleteSslContext");

            try
            {
                unsafe
                {
                    _readCallback = ReadFromConnection;
                    _writeCallback = WriteToConnection;
                }

                _sslContext = CreateSslContext(credential);
                InitializeSslContext(_sslContext, _readCallback, _writeCallback, credential, authOptions);
            }
            catch (Exception ex)
            {
                Debug.Write("Exception Caught. - " + ex);
                Dispose();
                throw;
            }
        }

        public override bool IsInvalid => _sslContext?.IsInvalid ?? true;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SafeSslHandle sslContext = _sslContext;
                if (sslContext != null)
                {
                    _inputBuffer.Dispose();
                    _outputBuffer.Dispose();
                    sslContext.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        private unsafe void WriteToConnection(byte* data, int dataLength)
        {
            var inputBuffer = new ReadOnlySpan<byte>(data, dataLength);

            _outputBuffer.EnsureAvailableSpace(dataLength);
            inputBuffer.CopyTo(_outputBuffer.AvailableSpan);
            _outputBuffer.Commit(dataLength);
        }

        private unsafe PAL_SSLStreamStatus ReadFromConnection(byte* data, int* dataLength)
        {
            int toRead = *dataLength;
            if (toRead == 0)
                return PAL_SSLStreamStatus.OK;

            if (_inputBuffer.ActiveLength == 0)
            {
                *dataLength = 0;
                return PAL_SSLStreamStatus.NeedData;
            }

            toRead = Math.Min(toRead, _inputBuffer.ActiveLength);

            _inputBuffer.ActiveSpan.Slice(0, toRead).CopyTo(new Span<byte>(data, toRead));
            _inputBuffer.Discard(toRead);

            *dataLength = toRead;
            return PAL_SSLStreamStatus.OK;
        }

        internal void Write(ReadOnlySpan<byte> buf)
        {
            _inputBuffer.EnsureAvailableSpace(buf.Length);
            buf.CopyTo(_inputBuffer.AvailableSpan);
            _inputBuffer.Commit(buf.Length);
        }

        internal int BytesReadyForConnection => _outputBuffer.ActiveLength;

        internal byte[]? ReadPendingWrites()
        {
            if (_outputBuffer.ActiveLength == 0)
            {
                return null;
            }

            byte[] buffer = _outputBuffer.ActiveSpan.ToArray();
            _outputBuffer.Discard(_outputBuffer.ActiveLength);

            return buffer;
        }

        internal int ReadPendingWrites(byte[] buf, int offset, int count)
        {
            Debug.Assert(buf != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(count <= buf.Length - offset);

            int limit = Math.Min(count, _outputBuffer.ActiveLength);

            _outputBuffer.ActiveSpan.Slice(0, limit).CopyTo(new Span<byte>(buf, offset, limit));
            _outputBuffer.Discard(limit);

            return limit;
        }

        private static SafeSslHandle CreateSslContext(SafeFreeSslCredentials credential)
        {
            if (credential.CertificateContext == null)
            {
                return Interop.AndroidCrypto.SSLStreamCreate();
            }

            SslStreamCertificateContext context = credential.CertificateContext;
            X509Certificate2 cert = context.Certificate;
            Debug.Assert(context.Certificate.HasPrivateKey);

            PAL_KeyAlgorithm algorithm;
            byte[] keyBytes;
            using (AsymmetricAlgorithm key = GetPrivateKeyAlgorithm(cert, out algorithm))
            {
                keyBytes = key.ExportPkcs8PrivateKey();
            }
            IntPtr[] ptrs = new IntPtr[context.IntermediateCertificates.Length + 1];
            ptrs[0] = cert.Handle;
            for (int i = 0; i < context.IntermediateCertificates.Length; i++)
            {
                ptrs[i + 1] = context.IntermediateCertificates[i].Handle;
            }

            return Interop.AndroidCrypto.SSLStreamCreateWithCertificates(keyBytes, algorithm, ptrs);
        }

        private static AsymmetricAlgorithm GetPrivateKeyAlgorithm(X509Certificate2 cert, out PAL_KeyAlgorithm algorithm)
        {
            AsymmetricAlgorithm? key = cert.GetRSAPrivateKey();
            if (key != null)
            {
                algorithm = PAL_KeyAlgorithm.RSA;
                return key;
            }
            key = cert.GetECDsaPrivateKey();
            if (key != null)
            {
                algorithm = PAL_KeyAlgorithm.EC;
                return key;
            }
            key = cert.GetDSAPrivateKey();
            if (key != null)
            {
                algorithm = PAL_KeyAlgorithm.DSA;
                return key;
            }
            throw new NotSupportedException(SR.net_ssl_io_no_server_cert);
        }

        private static void InitializeSslContext(
            SafeSslHandle handle,
            Interop.AndroidCrypto.SSLReadCallback readCallback,
            Interop.AndroidCrypto.SSLWriteCallback writeCallback,
            SafeFreeSslCredentials credential,
            SslAuthenticationOptions authOptions)
        {
            bool isServer = authOptions.IsServer;

            if (authOptions.ApplicationProtocols != null
                || authOptions.CipherSuitesPolicy != null
                || credential.Protocols != SslProtocols.None
                || (isServer && authOptions.RemoteCertRequired))
            {
                // TODO: [AndroidCrypto] Handle non-system-default options
                throw new NotImplementedException(nameof(SafeDeleteSslContext));
            }

            Interop.AndroidCrypto.SSLStreamInitialize(handle, isServer, readCallback, writeCallback, InitialBufferSize);

            if (!isServer && !string.IsNullOrEmpty(authOptions.TargetHost))
            {
                Interop.AndroidCrypto.SSLStreamConfigureParameters(handle, authOptions.TargetHost);
            }
        }
    }
}
