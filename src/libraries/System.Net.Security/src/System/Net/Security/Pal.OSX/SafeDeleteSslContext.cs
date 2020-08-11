// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private const int InitialBufferSize = 2048;
        private SafeSslHandle _sslContext;
        private Interop.AppleCrypto.SSLReadFunc _readCallback;
        private Interop.AppleCrypto.SSLWriteFunc _writeCallback;
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

                unsafe
                {
                    _readCallback = ReadFromConnection;
                    _writeCallback = WriteToConnection;
                }

                _sslContext = CreateSslContext(credential, sslAuthenticationOptions.IsServer);

                osStatus = Interop.AppleCrypto.SslSetIoCallbacks(
                    _sslContext,
                    _readCallback,
                    _writeCallback);

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

                if (sslAuthenticationOptions.ApplicationProtocols != null)
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
                SafeSslHandle sslContext = _sslContext;
                if (null != sslContext)
                {
                    _inputBuffer.Dispose();
                    _outputBuffer.Dispose();
                    sslContext.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        private unsafe int WriteToConnection(void* connection, byte* data, void** dataLength)
        {
            ulong length = (ulong)*dataLength;
            Debug.Assert(length <= int.MaxValue);

            int toWrite = (int)length;
            var inputBuffer = new ReadOnlySpan<byte>(data, toWrite);

            _outputBuffer.EnsureAvailableSpace(toWrite);
            inputBuffer.CopyTo(_outputBuffer.AvailableSpan);
            _outputBuffer.Commit(toWrite);

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

            if (_inputBuffer.ActiveLength == 0)
            {
                *dataLength = (void*)0;
                return errSSLWouldBlock;
            }

            int limit = Math.Min((int)toRead, _inputBuffer.ActiveLength);

            _inputBuffer.ActiveSpan.Slice(0, limit).CopyTo(new Span<byte>(data, limit));
            _inputBuffer.Discard(limit);
            transferred = (uint)limit;

            *dataLength = (void*)transferred;
            return noErr;
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

        private static readonly SslProtocols[] s_orderedSslProtocols = new SslProtocols[5]
        {
#pragma warning disable 0618
            SslProtocols.Ssl2,
            SslProtocols.Ssl3,
#pragma warning restore 0618
            SslProtocols.Tls,
            SslProtocols.Tls11,
            SslProtocols.Tls12
        };

        private static void SetProtocols(SafeSslHandle sslContext, SslProtocols protocols)
        {
            // A contiguous range of protocols is required.  Find the min and max of the range,
            // or throw if it's non-contiguous or if no protocols are specified.

            // First, mark all of the specified protocols.
            SslProtocols[] orderedSslProtocols = s_orderedSslProtocols;
            Span<bool> protocolSet = stackalloc bool[orderedSslProtocols.Length];
            for (int i = 0; i < orderedSslProtocols.Length; i++)
            {
                protocolSet[i] = (protocols & orderedSslProtocols[i]) != 0;
            }

            SslProtocols minProtocolId = (SslProtocols)(-1);
            SslProtocols maxProtocolId = (SslProtocols)(-1);

            // Loop through them, starting from the lowest.
            for (int min = 0; min < protocolSet.Length; min++)
            {
                if (protocolSet[min])
                {
                    // We found the first one that's set; that's the bottom of the range.
                    minProtocolId = orderedSslProtocols[min];

                    // Now loop from there to look for the max of the range.
                    for (int max = min + 1; max < protocolSet.Length; max++)
                    {
                        if (!protocolSet[max])
                        {
                            // We found the first one after the min that's not set; the top of the range
                            // is the one before this (which might be the same as the min).
                            maxProtocolId = orderedSslProtocols[max - 1];

                            // Finally, verify that nothing beyond this one is set, as that would be
                            // a discontiguous set of protocols.
                            for (int verifyNotSet = max + 1; verifyNotSet < protocolSet.Length; verifyNotSet++)
                            {
                                if (protocolSet[verifyNotSet])
                                {
                                    throw new PlatformNotSupportedException(SR.Format(SR.net_security_sslprotocol_contiguous, protocols));
                                }
                            }

                            break;
                        }
                    }

                    break;
                }
            }

            // If no protocols were set, throw.
            if (minProtocolId == (SslProtocols)(-1))
            {
                throw new PlatformNotSupportedException(SR.net_securityprotocolnotsupported);
            }

            // If we didn't find an unset protocol after the min, go all the way to the last one.
            if (maxProtocolId == (SslProtocols)(-1))
            {
                maxProtocolId = orderedSslProtocols[orderedSslProtocols.Length - 1];
            }

            // Finally set this min and max.
            Interop.AppleCrypto.SslSetMinProtocolVersion(sslContext, minProtocolId);
            Interop.AppleCrypto.SslSetMaxProtocolVersion(sslContext, maxProtocolId);
        }

        private static void SetCertificate(SafeSslHandle sslContext, SslStreamCertificateContext context)
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
                    intermediateCert = new X509Certificate2(intermediateCert.RawData);
                }

                ptrs[i + 1] = intermediateCert.Handle;
            }

            ptrs[0] = context!.Certificate!.Handle;

            Interop.AppleCrypto.SslSetCertificate(sslContext, ptrs);
        }
    }
}
