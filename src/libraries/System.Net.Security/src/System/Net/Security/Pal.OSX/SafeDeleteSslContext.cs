// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace System.Net
{
    internal sealed class SafeDeleteSslContext : SafeDeleteContext
    {
        // mapped from OSX error codes
        private const int OSStatus_writErr = -20;
        private const int OSStatus_readErr = -19;
        private const int OSStatus_noErr = 0;
        private const int OSStatus_errSSLWouldBlock = -9803;
        private const int InitialBufferSize = 2048;
        private SafeSslHandle _sslContext;
        private ArrayBuffer _inputBuffer = new ArrayBuffer(InitialBufferSize);
        private ArrayBuffer _outputBuffer = new ArrayBuffer(InitialBufferSize);

        public SafeSslHandle SslContext => _sslContext;

        public SafeDeleteSslContext(SafeFreeSslCredentials credential, SslAuthenticationOptions sslAuthenticationOptions)
            : base(credential)
        {
            Debug.Assert((null != credential) && !credential.IsInvalid, "Invalid credential used in SafeDeleteSslContext");

            try
            {
                int osStatus;

                _sslContext = CreateSslContext(credential, sslAuthenticationOptions.IsServer);

                // Make sure the class instance is associated to the session and is provided
                // in the Read/Write callback connection parameter
                SslSetConnection(_sslContext);

                unsafe
                {
                    osStatus = Interop.AppleCrypto.SslSetIoCallbacks(
                        _sslContext,
                        &ReadFromConnection,
                        &WriteToConnection);
                }

                if (osStatus != 0)
                {
                    throw Interop.AppleCrypto.CreateExceptionForOSStatus(osStatus);
                }

                if (sslAuthenticationOptions.CipherSuitesPolicy != null)
                {
                    uint[] tlsCipherSuites = sslAuthenticationOptions.CipherSuitesPolicy.Pal.TlsCipherSuites;

                    unsafe
                    {
                        fixed (uint* cipherSuites = tlsCipherSuites)
                        {
                            osStatus = Interop.AppleCrypto.SslSetEnabledCipherSuites(
                                _sslContext,
                                cipherSuites,
                                tlsCipherSuites.Length);

                            if (osStatus != 0)
                            {
                                throw Interop.AppleCrypto.CreateExceptionForOSStatus(osStatus);
                            }
                        }
                    }
                }

                if (sslAuthenticationOptions.ApplicationProtocols != null && sslAuthenticationOptions.ApplicationProtocols.Count != 0)
                {
                    // On OSX coretls supports only client side. For server, we will silently ignore the option.
                    if (!sslAuthenticationOptions.IsServer)
                    {
                        Interop.AppleCrypto.SslCtxSetAlpnProtos(_sslContext, sslAuthenticationOptions.ApplicationProtocols);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Write("Exception Caught. - " + ex);
                Dispose();
                throw;
            }

            if (!string.IsNullOrEmpty(sslAuthenticationOptions.TargetHost) && !sslAuthenticationOptions.IsServer)
            {
                Interop.AppleCrypto.SslSetTargetName(_sslContext, sslAuthenticationOptions.TargetHost);
            }

            if (sslAuthenticationOptions.CertificateContext == null && sslAuthenticationOptions.CertSelectionDelegate != null)
            {
                // certificate was not provided but there is user callback. We can break handshake if server asks for certificate
                // and we can try to get it based on remote certificate and trusted issuers.
                Interop.AppleCrypto.SslBreakOnCertRequested(_sslContext, true);
            }

            if (sslAuthenticationOptions.IsServer)
            {
                if (sslAuthenticationOptions.RemoteCertRequired)
                {
                    Interop.AppleCrypto.SslSetAcceptClientCert(_sslContext);
                }

                if (sslAuthenticationOptions.CertificateContext?.Trust?._sendTrustInHandshake == true)
                {
                    SslCertificateTrust trust = sslAuthenticationOptions.CertificateContext!.Trust!;
                    X509Certificate2Collection certList = (trust._trustList ?? trust._store!.Certificates);

                    Debug.Assert(certList != null, "certList != null");
                    Span<IntPtr> handles = certList.Count <= 256
                        ? stackalloc IntPtr[256]
                        : new IntPtr[certList.Count];

                    for (int i = 0; i < certList.Count; i++)
                    {
                        handles[i] = certList[i].Handle;
                    }

                    Interop.AppleCrypto.SslSetCertificateAuthorities(_sslContext, handles.Slice(0, certList.Count), true);
                }
            }
        }

        private static SafeSslHandle CreateSslContext(SafeFreeSslCredentials credential, bool isServer)
        {
            switch (credential.Policy)
            {
                case EncryptionPolicy.RequireEncryption:
#pragma warning disable SYSLIB0040 // NoEncryption and AllowNoEncryption are obsolete
                case EncryptionPolicy.AllowNoEncryption:
                    // SecureTransport doesn't allow TLS_NULL_NULL_WITH_NULL, but
                    // since AllowNoEncryption intersect OS-supported isn't nothing,
                    // let it pass.
                    break;
#pragma warning restore SYSLIB0040
                default:
                    throw new PlatformNotSupportedException(SR.Format(SR.net_encryptionpolicy_notsupported, credential.Policy));
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

                if (credential.CertificateContext != null)
                {
                    SetCertificate(sslContext, credential.CertificateContext);
                }

                Interop.AppleCrypto.SslBreakOnCertRequested(sslContext, true);
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

        private void SslSetConnection(SafeSslHandle sslContext)
        {
            GCHandle handle = GCHandle.Alloc(this, GCHandleType.Weak);

            Interop.AppleCrypto.SslSetConnection(sslContext, GCHandle.ToIntPtr(handle));
        }

        public override bool IsInvalid => _sslContext?.IsInvalid ?? true;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SafeSslHandle sslContext = _sslContext;
                if (null != sslContext)
                {
                    lock (_sslContext)
                    {
                        _inputBuffer.Dispose();
                        _outputBuffer.Dispose();
                    }
                    sslContext.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        [UnmanagedCallersOnly]
        private static unsafe int WriteToConnection(IntPtr connection, byte* data, void** dataLength)
        {
            SafeDeleteSslContext? context = (SafeDeleteSslContext?)GCHandle.FromIntPtr(connection).Target;
            Debug.Assert(context != null);

            // We don't pool these buffers and we can't because there's a race between their us in the native
            // read/write callbacks and being disposed when the SafeHandle is disposed. This race is benign currently,
            // but if we were to pool the buffers we would have a potential use-after-free issue.
            try
            {
                lock (context)
                {
                    ulong length = (ulong)*dataLength;
                    Debug.Assert(length <= int.MaxValue);

                    int toWrite = (int)length;
                    var inputBuffer = new ReadOnlySpan<byte>(data, toWrite);

                    context._outputBuffer.EnsureAvailableSpace(toWrite);
                    inputBuffer.CopyTo(context._outputBuffer.AvailableSpan);
                    context._outputBuffer.Commit(toWrite);
                    // Since we can enqueue everything, no need to re-assign *dataLength.

                    return OSStatus_noErr;
                }
            }
            catch (Exception e)
            {
                if (NetEventSource.Log.IsEnabled())
                    NetEventSource.Error(context, $"WritingToConnection failed: {e.Message}");
                return OSStatus_writErr;
            }
        }

        [UnmanagedCallersOnly]
        private static unsafe int ReadFromConnection(IntPtr connection, byte* data, void** dataLength)
        {
            SafeDeleteSslContext? context = (SafeDeleteSslContext?)GCHandle.FromIntPtr(connection).Target;
            Debug.Assert(context != null);

            try
            {
                lock (context)
                {
                    ulong toRead = (ulong)*dataLength;

                    if (toRead == 0)
                    {
                        return OSStatus_noErr;
                    }

                    uint transferred = 0;

                    if (context._inputBuffer.ActiveLength == 0)
                    {
                        *dataLength = (void*)0;
                        return OSStatus_errSSLWouldBlock;
                    }

                    int limit = Math.Min((int)toRead, context._inputBuffer.ActiveLength);

                    context._inputBuffer.ActiveSpan.Slice(0, limit).CopyTo(new Span<byte>(data, limit));
                    context._inputBuffer.Discard(limit);
                    transferred = (uint)limit;

                    *dataLength = (void*)transferred;
                    return OSStatus_noErr;
                }
            }
            catch (Exception e)
            {
                if (NetEventSource.Log.IsEnabled())
                    NetEventSource.Error(context, $"ReadFromConnectionfailed: {e.Message}");
                return OSStatus_readErr;
            }
        }

        internal void Write(ReadOnlySpan<byte> buf)
        {
            lock (_sslContext)
            {
                _inputBuffer.EnsureAvailableSpace(buf.Length);
                buf.CopyTo(_inputBuffer.AvailableSpan);
                _inputBuffer.Commit(buf.Length);
            }
        }

        internal int BytesReadyForConnection => _outputBuffer.ActiveLength;

        internal byte[]? ReadPendingWrites()
        {
            lock (_sslContext)
            {
                if (_outputBuffer.ActiveLength == 0)
                {
                    return null;
                }

                byte[] buffer = _outputBuffer.ActiveSpan.ToArray();
                _outputBuffer.Discard(_outputBuffer.ActiveLength);

                return buffer;
            }
        }

        internal int ReadPendingWrites(byte[] buf, int offset, int count)
        {
            Debug.Assert(buf != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(count <= buf.Length - offset);

            lock (_sslContext)
            {
                int limit = Math.Min(count, _outputBuffer.ActiveLength);

                _outputBuffer.ActiveSpan.Slice(0, limit).CopyTo(new Span<byte>(buf, offset, limit));
                _outputBuffer.Discard(limit);

                return limit;
            }
        }

        private static readonly SslProtocols[] s_orderedSslProtocols = new SslProtocols[5]
        {
#pragma warning disable 0618
            SslProtocols.Ssl2,
            SslProtocols.Ssl3,
#pragma warning restore 0618
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
            SslProtocols.Tls,
            SslProtocols.Tls11,
#pragma warning restore SYSLIB0039
            SslProtocols.Tls12
        };

        private static void SetProtocols(SafeSslHandle sslContext, SslProtocols protocols)
        {
            (int minIndex, int maxIndex) = protocols.ValidateContiguous(s_orderedSslProtocols);
            SslProtocols minProtocolId = s_orderedSslProtocols[minIndex];
            SslProtocols maxProtocolId = s_orderedSslProtocols[maxIndex];

            // Set the min and max.
            Interop.AppleCrypto.SslSetMinProtocolVersion(sslContext, minProtocolId);
            Interop.AppleCrypto.SslSetMaxProtocolVersion(sslContext, maxProtocolId);
        }

        internal static void SetCertificate(SafeSslHandle sslContext, SslStreamCertificateContext context)
        {
            Debug.Assert(sslContext != null, "sslContext != null");


            IntPtr[] ptrs = new IntPtr[context!.IntermediateCertificates!.Length + 1];

            for (int i = 0; i < context.IntermediateCertificates.Length; i++)
            {
                X509Certificate2 intermediateCert = context.IntermediateCertificates[i];

                if (intermediateCert.HasPrivateKey)
                {
                    // In the unlikely event that we get a certificate with a private key from
                    // a chain, clear it to the certificate.
                    //
                    // The current value of intermediateCert is still in elements, which will
                    // get Disposed at the end of this method.  The new value will be
                    // in the intermediate certs array, which also gets serially Disposed.
                    intermediateCert = new X509Certificate2(intermediateCert.RawDataMemory.Span);
                }

                ptrs[i + 1] = intermediateCert.Handle;
            }

            ptrs[0] = context!.Certificate!.Handle;

            Interop.AppleCrypto.SslSetCertificate(sslContext, ptrs);
        }
    }
}
