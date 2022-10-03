// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
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
        private static readonly SslProtocols[] s_orderedSslProtocols = new SslProtocols[]
        {
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
            SslProtocols.Tls,
            SslProtocols.Tls11,
#pragma warning restore SYSLIB0039
            SslProtocols.Tls12,
            SslProtocols.Tls13,
        };
        private static readonly Lazy<SslProtocols> s_supportedSslProtocols = new Lazy<SslProtocols>(Interop.AndroidCrypto.SSLGetSupportedProtocols);

        private readonly SafeSslHandle _sslContext;

        private ArrayBuffer _inputBuffer = new ArrayBuffer(InitialBufferSize);
        private ArrayBuffer _outputBuffer = new ArrayBuffer(InitialBufferSize);

        public SafeSslHandle SslContext => _sslContext;

        public SafeDeleteSslContext(SafeFreeSslCredentials credential, SslAuthenticationOptions authOptions)
            : base(credential)
        {
            Debug.Assert((credential != null) && !credential.IsInvalid, "Invalid credential used in SafeDeleteSslContext");

            try
            {
                _sslContext = CreateSslContext(credential);
                InitializeSslContext(_sslContext, credential, authOptions);
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

        [UnmanagedCallersOnly]
        private static unsafe void WriteToConnection(IntPtr connection, byte* data, int dataLength)
        {
            SafeDeleteSslContext? context = (SafeDeleteSslContext?)GCHandle.FromIntPtr(connection).Target;
            Debug.Assert(context != null);

            var inputBuffer = new ReadOnlySpan<byte>(data, dataLength);

            context._outputBuffer.EnsureAvailableSpace(dataLength);
            inputBuffer.CopyTo(context._outputBuffer.AvailableSpan);
            context._outputBuffer.Commit(dataLength);
        }

        [UnmanagedCallersOnly]
        private static unsafe PAL_SSLStreamStatus ReadFromConnection(IntPtr connection, byte* data, int* dataLength)
        {
            SafeDeleteSslContext? context = (SafeDeleteSslContext?)GCHandle.FromIntPtr(connection).Target;
            Debug.Assert(context != null);

            int toRead = *dataLength;
            if (toRead == 0)
                return PAL_SSLStreamStatus.OK;

            if (context._inputBuffer.ActiveLength == 0)
            {
                *dataLength = 0;
                return PAL_SSLStreamStatus.NeedData;
            }

            toRead = Math.Min(toRead, context._inputBuffer.ActiveLength);

            context._inputBuffer.ActiveSpan.Slice(0, toRead).CopyTo(new Span<byte>(data, toRead));
            context._inputBuffer.Discard(toRead);

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

        private unsafe void InitializeSslContext(
            SafeSslHandle handle,
            SafeFreeSslCredentials credential,
            SslAuthenticationOptions authOptions)
        {
            switch (credential.Policy)
            {
                case EncryptionPolicy.RequireEncryption:
#pragma warning disable SYSLIB0040 // NoEncryption and AllowNoEncryption are obsolete
                case EncryptionPolicy.AllowNoEncryption:
                    break;
#pragma warning restore SYSLIB0040
                default:
                    throw new PlatformNotSupportedException(SR.Format(SR.net_encryptionpolicy_notsupported, credential.Policy));
            }

            bool isServer = authOptions.IsServer;

            if (authOptions.CipherSuitesPolicy != null)
            {
                // TODO: [AndroidCrypto] Handle non-system-default options
                throw new NotImplementedException(nameof(SafeDeleteSslContext));
            }

            // Make sure the class instance is associated to the session and is provided
            // in the Read/Write callback connection parameter
            IntPtr managedContextHandle = GCHandle.ToIntPtr(GCHandle.Alloc(this, GCHandleType.Weak));
            Interop.AndroidCrypto.SSLStreamInitialize(handle, isServer, managedContextHandle, &ReadFromConnection, &WriteToConnection, InitialBufferSize);

            if (credential.Protocols != SslProtocols.None)
            {
                SslProtocols protocolsToEnable = credential.Protocols & s_supportedSslProtocols.Value;
                if (protocolsToEnable == 0)
                {
                    throw new PlatformNotSupportedException(SR.Format(SR.net_security_sslprotocol_notsupported, credential.Protocols));
                }

                (int minIndex, int maxIndex) = protocolsToEnable.ValidateContiguous(s_orderedSslProtocols);
                Interop.AndroidCrypto.SSLStreamSetEnabledProtocols(handle, s_orderedSslProtocols.AsSpan(minIndex, maxIndex - minIndex + 1));
            }

            if (authOptions.ApplicationProtocols != null && authOptions.ApplicationProtocols.Count != 0
                && Interop.AndroidCrypto.SSLSupportsApplicationProtocolsConfiguration())
            {
                // Set application protocols if the platform supports it. Otherwise, we will silently ignore the option.
                Interop.AndroidCrypto.SSLStreamSetApplicationProtocols(handle, authOptions.ApplicationProtocols);
            }

            if (isServer && authOptions.RemoteCertRequired)
            {
                Interop.AndroidCrypto.SSLStreamRequestClientAuthentication(handle);
            }

            if (!isServer && !string.IsNullOrEmpty(authOptions.TargetHost))
            {
                Interop.AndroidCrypto.SSLStreamSetTargetHost(handle, authOptions.TargetHost);
            }
        }
    }
}
