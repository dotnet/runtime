// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

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

        private readonly Lock _lock = new Lock();

        private ArrayBuffer _inputBuffer = new ArrayBuffer(InitialBufferSize);
        private ArrayBuffer _outputBuffer = new ArrayBuffer(InitialBufferSize);

        public SslStream.JavaProxy SslStreamProxy { get; }

        public SafeSslHandle SslContext => _sslContext;

        private volatile bool _disposed;

        public SafeDeleteSslContext(SslAuthenticationOptions authOptions)
            : base(IntPtr.Zero)
        {
            SslStreamProxy = authOptions.SslStreamProxy
                ?? throw new ArgumentNullException(nameof(authOptions.SslStreamProxy));

            try
            {
                _sslContext = CreateSslContext(SslStreamProxy, authOptions);
                InitializeSslContext(_sslContext, authOptions);
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
            lock (_lock)
            {
                if (!_disposed)
                {
                    _disposed = true;

                    // First dispose the SSL context to trigger native cleanup
                    _sslContext.Dispose();

                    if (disposing)
                    {
                        // Then dispose the buffers
                        _inputBuffer.Dispose();
                        _outputBuffer.Dispose();
                    }
                }
            }

            base.Dispose(disposing);
        }

        [UnmanagedCallersOnly]
        private static unsafe void WriteToConnection(IntPtr connection, byte* data, int dataLength)
        {
            WeakGCHandle<SafeDeleteSslContext> h = WeakGCHandle<SafeDeleteSslContext>.FromIntPtr(connection);
            if (!h.TryGetTarget(out SafeDeleteSslContext? context))
            {
                Debug.Write("WriteToConnection: failed to get target context");
                return;
            }

            lock (context._lock)
            {
                if (context._disposed)
                {
                    Debug.Write("WriteToConnection: context is disposed");
                    return;
                }

                var inputBuffer = new ReadOnlySpan<byte>(data, dataLength);

                context._outputBuffer.EnsureAvailableSpace(dataLength);
                inputBuffer.CopyTo(context._outputBuffer.AvailableSpan);
                context._outputBuffer.Commit(dataLength);
            }
        }

        [UnmanagedCallersOnly]
        private static unsafe PAL_SSLStreamStatus ReadFromConnection(IntPtr connection, byte* data, int* dataLength)
        {
            WeakGCHandle<SafeDeleteSslContext> h = WeakGCHandle<SafeDeleteSslContext>.FromIntPtr(connection);
            if (!h.TryGetTarget(out SafeDeleteSslContext? context))
            {
                Debug.Write("ReadFromConnection: failed to get target context");
                *dataLength = 0;
                return PAL_SSLStreamStatus.Error;
            }

            lock (context._lock)
            {
                if (context._disposed)
                {
                    Debug.Write("ReadFromConnection: context is disposed");
                    *dataLength = 0;
                    return PAL_SSLStreamStatus.Error;
                }

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
        }

        [UnmanagedCallersOnly]
        private static void CleanupManagedContext(IntPtr managedContextHandle)
        {
            if (managedContextHandle != IntPtr.Zero)
            {
                WeakGCHandle<SafeDeleteSslContext> handle = WeakGCHandle<SafeDeleteSslContext>.FromIntPtr(managedContextHandle);
                handle.Dispose();
            }
        }

        internal void Write(ReadOnlySpan<byte> buf)
        {
            lock (_lock)
            {
                _inputBuffer.EnsureAvailableSpace(buf.Length);
                buf.CopyTo(_inputBuffer.AvailableSpan);
                _inputBuffer.Commit(buf.Length);
            }
        }

        internal int BytesReadyForConnection => _outputBuffer.ActiveLength;

        internal void ReadPendingWrites(ref ProtocolToken token)
        {
            lock (_lock)
            {
                if (_outputBuffer.ActiveLength == 0)
                {
                    token.Size = 0;
                    token.Payload = null;
                    return;
                }

                token.SetPayload(_outputBuffer.ActiveSpan);
                _outputBuffer.Discard(_outputBuffer.ActiveLength);
            }
        }

        internal int ReadPendingWrites(byte[] buf, int offset, int count)
        {
            Debug.Assert(buf != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(count <= buf.Length - offset);

            lock (_lock)
            {
                int limit = Math.Min(count, _outputBuffer.ActiveLength);

                _outputBuffer.ActiveSpan.Slice(0, limit).CopyTo(new Span<byte>(buf, offset, limit));
                _outputBuffer.Discard(limit);

                return limit;
            }
        }

        private static SafeSslHandle CreateSslContext(SslStream.JavaProxy sslStreamProxy, SslAuthenticationOptions authOptions)
        {
            // targetHost is passed to the platform's DotnetProxyTrustManager for hostname-aware
            // certificate validation. IP literals are excluded because SNIHostName doesn't accept them.
            string? targetHost = !authOptions.IsServer
                && !string.IsNullOrEmpty(authOptions.TargetHost)
                && !IPAddress.IsValid(authOptions.TargetHost)
                    ? authOptions.TargetHost
                    : null;

            // Custom trust roots are passed to the platform's TrustManagerFactory KeyStore.
            // The platform's trust verdict is combined with managed validation to be more strict
            // (see VerifyRemoteCertificate in SslStream.Android.cs).
            IntPtr[]? trustCerts = GetTrustCertHandles(authOptions);
            IntPtr keyManagers = authOptions.CertificateContext is not null
                ? CreateKeyManagers(authOptions.CertificateContext)
                : IntPtr.Zero;

            try
            {
                return Interop.AndroidCrypto.SSLStreamCreate(sslStreamProxy, targetHost, trustCerts, keyManagers);
            }
            finally
            {
                // keyManagers is a JNI global ref that was created to survive across
                // P/Invoke boundaries. Release it now that SSLStreamCreate has consumed it.
                if (keyManagers != IntPtr.Zero)
                {
                    Interop.JObjectLifetime.DeleteGlobalReference(keyManagers);
                }
            }

            static IntPtr CreateKeyManagers(SslStreamCertificateContext context)
            {
                X509Certificate2 cert = context.TargetCertificate;
                Debug.Assert(cert.HasPrivateKey);

                IntPtr keyManagers;
                if (Interop.AndroidCrypto.IsKeyStorePrivateKeyEntry(cert.Handle))
                {
                    keyManagers = Interop.AndroidCrypto.SSLStreamCreateKeyManagersFromKeyStoreEntry(cert.Handle);
                }
                else
                {
                    PAL_KeyAlgorithm algorithm;
                    byte[] keyBytes;
                    using (AsymmetricAlgorithm key = GetPrivateKeyAlgorithm(cert, out algorithm))
                    {
                        keyBytes = key.ExportPkcs8PrivateKey();
                    }
                    IntPtr[] ptrs = new IntPtr[context.IntermediateCertificates.Count + 1];
                    ptrs[0] = cert.Handle;
                    for (int i = 0; i < context.IntermediateCertificates.Count; i++)
                    {
                        ptrs[i + 1] = context.IntermediateCertificates[i].Handle;
                    }

                    keyManagers = Interop.AndroidCrypto.SSLStreamCreateKeyManagers(keyBytes, algorithm, ptrs);
                }

                if (keyManagers == IntPtr.Zero)
                {
                    throw new Interop.AndroidCrypto.SslException();
                }

                return keyManagers;
            }

            static IntPtr[]? GetTrustCertHandles(SslAuthenticationOptions authOptions)
            {
                // Collect custom trust root certificates to pass to the platform's
                // TrustManagerFactory. There are two mutually exclusive sources — when
                // CertificateChainPolicy is set it takes precedence (see SslStream.Protocol.cs):
                //
                // 1. CertificateChainPolicy.CustomTrustStore (when TrustMode is CustomRootTrust)
                // 2. SslCertificateTrust (via CertificateContext.Trust) — older API
                //
                // Note: CertificateChainPolicy.ExtraStore intermediates are NOT passed to the
                // platform because Java's KeyStore.setCertificateEntry would elevate them to
                // trust anchors. When ExtraStore is populated, the managed chain builder is
                // authoritative (see VerifyRemoteCertificate in SslStream.Android.cs).
                X509Certificate2Collection? certs;
                if (authOptions.CertificateChainPolicy is not null)
                {
                    certs = authOptions.CertificateChainPolicy.TrustMode == X509ChainTrustMode.CustomRootTrust
                        ? authOptions.CertificateChainPolicy.CustomTrustStore
                        : null;
                }
                else
                {
                    var trust = authOptions.CertificateContext?.Trust;
                    certs = trust?._store?.Certificates ?? trust?._trustList;
                }

                if (certs is null || certs.Count == 0)
                {
                    return null;
                }

                IntPtr[] handles = new IntPtr[certs.Count];
                for (int i = 0; i < certs.Count; i++)
                {
                    handles[i] = certs[i].Handle;
                }

                return handles;
            }
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
            SslAuthenticationOptions authOptions)
        {
            switch (authOptions.EncryptionPolicy)
            {
                case EncryptionPolicy.RequireEncryption:
#pragma warning disable SYSLIB0040 // NoEncryption and AllowNoEncryption are obsolete
                case EncryptionPolicy.AllowNoEncryption:
                    break;
#pragma warning restore SYSLIB0040
                default:
                    throw new PlatformNotSupportedException(SR.Format(SR.net_encryptionpolicy_notsupported, authOptions.EncryptionPolicy));
            }

            bool isServer = authOptions.IsServer;

            if (authOptions.CipherSuitesPolicy != null)
            {
                // TODO: [AndroidCrypto] Handle non-system-default options
                throw new NotImplementedException(nameof(SafeDeleteSslContext));
            }

            // Make sure the class instance is associated to the session and is provided in the Read/Write callback connection parameter
            // Additionally, all calls should be synchronous so there's no risk of the managed object being collected while native code is executing.
            IntPtr managedContextHandle = GCHandle.ToIntPtr(GCHandle.Alloc(this, GCHandleType.Weak));
            string? peerHost = !isServer && !string.IsNullOrEmpty(authOptions.TargetHost) ? authOptions.TargetHost : null;
            Interop.AndroidCrypto.SSLStreamInitialize(handle, isServer, managedContextHandle, &ReadFromConnection, &WriteToConnection, &CleanupManagedContext, InitialBufferSize, peerHost);

            if (authOptions.EnabledSslProtocols != SslProtocols.None)
            {
                SslProtocols protocolsToEnable = authOptions.EnabledSslProtocols & s_supportedSslProtocols.Value;
                if (protocolsToEnable == 0)
                {
                    throw new PlatformNotSupportedException(SR.Format(SR.net_security_sslprotocol_notsupported, authOptions.EnabledSslProtocols));
                }

                (int minIndex, int maxIndex) = protocolsToEnable.ValidateContiguous(s_orderedSslProtocols);
                Interop.AndroidCrypto.SSLStreamSetEnabledProtocols(handle, s_orderedSslProtocols.AsSpan(minIndex, maxIndex - minIndex + 1));
            }

            if (authOptions.ApplicationProtocols != null && authOptions.ApplicationProtocols.Count != 0
                && Interop.AndroidCrypto.SSLSupportsApplicationProtocolsConfiguration())
            {
                ValidateAlpnProtocolListSize(authOptions.ApplicationProtocols);
                // Set application protocols if the platform supports it. Otherwise, we will silently ignore the option.
                Interop.AndroidCrypto.SSLStreamSetApplicationProtocols(handle, authOptions.ApplicationProtocols);
            }

            if (isServer && authOptions.RemoteCertRequired)
            {
                Interop.AndroidCrypto.SSLStreamRequestClientAuthentication(handle);
            }

            if (!isServer && !string.IsNullOrEmpty(authOptions.TargetHost) && !IPAddress.IsValid(authOptions.TargetHost))
            {
                Interop.AndroidCrypto.SSLStreamSetTargetHost(handle, authOptions.TargetHost);
            }
        }

        private static void ValidateAlpnProtocolListSize(List<SslApplicationProtocol> applicationProtocols)
        {
            int protocolListSize = 0;
            foreach (SslApplicationProtocol protocol in applicationProtocols)
            {
                protocolListSize += protocol.Protocol.Length + 1;
                if (protocolListSize > ushort.MaxValue)
                {
                    throw new ArgumentException(SR.net_ssl_app_protocols_invalid, nameof(applicationProtocols));
                }
            }
        }
    }
}
