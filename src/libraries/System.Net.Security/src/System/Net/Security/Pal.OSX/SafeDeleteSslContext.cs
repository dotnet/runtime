// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

using PAL_NwStatusUpdates = Interop.AppleCrypto.PAL_NwStatusUpdates;


namespace System.Net
{
    internal sealed class SafeDeleteSslContext : SafeDeleteContext
    {
        public static readonly unsafe bool CanUseNwFramework = Interop.AppleCrypto.NwInit(&FramerStatusUpdate, &ReadFromConnection, &WriteToConnection) == 0;
        public readonly bool UseNwFramework;
        // mapped from OSX error codes
        private const int OSStatus_writErr = -20;
        private const int OSStatus_readErr = -19;

        private const int OSStatus_eofErr = - 39;
        private const int OSStatus_noErr = 0;
        private const int OSStatus_errSSLWouldBlock = -9803;

        private const int OSStatus_errSecUserCanceled = -128;
        private const int InitialBufferSize = 2048;
        private readonly SafeSslHandle _sslContext;
        private ArrayBuffer _inputBuffer = new ArrayBuffer(InitialBufferSize);
        private ArrayBuffer _outputBuffer = new ArrayBuffer(InitialBufferSize);

        public GCHandle gcHandle;

        private  ManualResetEventSlim? _writeWaiter;
        private int _writeStatus;
        public ManualResetEventSlim? _readWaiter;
        private int _readStatus;
        public IntPtr _framer;
        public bool handshakeStarted;

        public TaskCompletionSource<SecurityStatusPalErrorCode>? Tcs;

        public SafeSslHandle SslContext => _sslContext;
        public SslApplicationProtocol SelectedApplicationProtocol;
        public bool IsServer;

        private bool _handshakeDone;
        private bool _disposed;

        public unsafe SafeDeleteSslContext(SslAuthenticationOptions sslAuthenticationOptions)
            : base(IntPtr.Zero)
        {
            try
            {
                int osStatus;

                switch (sslAuthenticationOptions.EncryptionPolicy)
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
                        throw new PlatformNotSupportedException(SR.Format(SR.net_encryptionpolicy_notsupported, sslAuthenticationOptions.EncryptionPolicy));
                }

                // TBD make this opt-in
                // NW freamewoprk still does not support all features and server side
                UseNwFramework = CanUseNwFramework && sslAuthenticationOptions.IsClient &&
                                    sslAuthenticationOptions.CipherSuitesPolicy == null &&
                                    sslAuthenticationOptions.ApplicationProtocols == null &&
                                    sslAuthenticationOptions.ClientCertificates == null &&
                                    sslAuthenticationOptions.CertificateContext == null &&
                                    sslAuthenticationOptions.CertSelectionDelegate == null;

                if (UseNwFramework)
                {
                    gcHandle = GCHandle.Alloc(this, GCHandleType.Weak);
                    _sslContext = Interop.AppleCrypto.NwCreateContext(0);
                    Tcs = new TaskCompletionSource<SecurityStatusPalErrorCode>();
                    _writeWaiter = new ManualResetEventSlim();
                    _readWaiter = new ManualResetEventSlim();

                    SslProtocols minProtocolId = SslProtocols.None;
                    SslProtocols maxProtocolId = SslProtocols.None;

                    if (sslAuthenticationOptions.EnabledSslProtocols != SslProtocols.None)
                    {
                        //(minProtocolId, maxProtocolId) = GetMinMaxProtocols(sslAuthenticationOptions.EnabledSslProtocols);
                    }



                    osStatus = Interop.AppleCrypto.NwSetTlsOptions(_sslContext, GCHandle.ToIntPtr(gcHandle),
                                    sslAuthenticationOptions.TargetHost,
                                    minProtocolId, maxProtocolId);

                    if (osStatus != 0)
                    {
                        throw Interop.AppleCrypto.CreateExceptionForOSStatus(osStatus);
                    }

                    return;
                }

                _sslContext = CreateSslContext(sslAuthenticationOptions);
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
                    if (sslAuthenticationOptions.IsClient)
                    {
                        // On macOS coreTls supports only client side.
                        Interop.AppleCrypto.SslCtxSetAlpnProtos(_sslContext, sslAuthenticationOptions.ApplicationProtocols);
                    }
                    else
                    {
                        // For Server, we do the selection in SslStream and we set it later
                        Interop.AppleCrypto.SslBreakOnClientHello(_sslContext, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Write("Exception Caught. - " + ex);
                Dispose();
                throw;
            }

            if (!string.IsNullOrEmpty(sslAuthenticationOptions.TargetHost) && !sslAuthenticationOptions.IsServer && !TargetHostNameHelper.IsValidAddress(sslAuthenticationOptions.TargetHost))
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
                IsServer = true;

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

        private static SafeSslHandle CreateSslContext(SslAuthenticationOptions sslAuthenticationOptions)
        {
            SafeSslHandle sslContext = Interop.AppleCrypto.SslCreateContext(sslAuthenticationOptions.IsServer ? 1 : 0);

            try
            {
                if (sslContext.IsInvalid)
                {
                    // This is as likely as anything.  No error conditions are defined for
                    // the OS function, and our shim only adds a NULL if isServer isn't a normalized bool.
                    throw new OutOfMemoryException();
                }

                // Let None mean "system default"
                if (sslAuthenticationOptions.EnabledSslProtocols != SslProtocols.None)
                {
                    SetProtocols(sslContext, sslAuthenticationOptions.EnabledSslProtocols);
                }

                // SslBreakOnCertRequested does not seem to do anything when we already provide the cert here.
                // So we set it only for server in order to reliably detect whether the peer asked for it on client.
                if (sslAuthenticationOptions.CertificateContext != null && sslAuthenticationOptions.IsServer)
                {
                    SetCertificate(sslContext, sslAuthenticationOptions.CertificateContext);
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
                    if (UseNwFramework && !_disposed)
                    {
                        lock (SslContext)
                        {
                            // alternativelly we can inject Framer errro
                            Interop.AppleCrypto.NwCancelConnection(_sslContext);
                        }
                    }

                    lock (_sslContext)
                    {
                        _inputBuffer.Dispose();
                        _outputBuffer.Dispose();
                    }
                    sslContext.Dispose();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }

        protected override bool ReleaseHandle()
        {
            if (gcHandle.IsAllocated)
            {
                gcHandle.Free();
            }
            return true;
        }

        [UnmanagedCallersOnly]
        private static unsafe int FramerStatusUpdate(IntPtr gcHandle, PAL_NwStatusUpdates status, IntPtr data1, IntPtr data2)
        {
            // we should not ever throw in unmanaged callback
            try
            {
                SafeDeleteSslContext? context = (SafeDeleteSslContext?)GCHandle.FromIntPtr(gcHandle).Target;
                if (context == null)
                {
                    return -1;
                }
                switch (status)
                {
                    case PAL_NwStatusUpdates.FramerStart:
                            context._framer = data1;
                            break;
                    case PAL_NwStatusUpdates.HandshakeFinished:
                        context.Tcs!.TrySetResult(SecurityStatusPalErrorCode.OK);
                        context.Tcs = null;
                        context._handshakeDone = true;
                        break;
                    case PAL_NwStatusUpdates.HandshakeFailed:
                        int osStatus = data1.ToInt32();
                        context.Tcs!.TrySetException(Interop.AppleCrypto.CreateExceptionForOSStatus(osStatus));
                        context.Tcs = null;
                        context._handshakeDone = true;
                        // this can happen also later and related to decryptin
                        context._readStatus = osStatus;
                        break;
                    case PAL_NwStatusUpdates.ConnectionCancelled:
                        context.Tcs?.TrySetException(new OperationCanceledException());
                        context.Tcs = null;
                        context._handshakeDone = true;
                        context._writeStatus = OSStatus_errSecUserCanceled;
                        context._writeWaiter?.Set();
                        context._readStatus = OSStatus_errSecUserCanceled;
                        context._readWaiter?.Set();
                        break;
                    case PAL_NwStatusUpdates.ConnectionReadFinished:
                        Span<byte> data = new Span<byte>((void*)data2, data1.ToInt32());
                        lock (context)
                        {
                            if (data1 > 0)
                            {
                                context.Write(data);
                            }
                            else if (context._readStatus == OSStatus_noErr)
                            {
                                context._readStatus = OSStatus_eofErr;
                            }
                            var tcs = context.Tcs;
                            // need to set it before signallig as the continuation may run before the next assigment
                            context.Tcs = new TaskCompletionSource<SecurityStatusPalErrorCode>();
                            tcs?.TrySetResult(SecurityStatusPalErrorCode.OK);
                        }

                        if (data1 > 0)
                        {
                            // schedule next read unless we got EOF
                            Interop.AppleCrypto.NwReadFromConnection(context.SslContext, GCHandle.ToIntPtr(context.gcHandle));
                        }

                        context._readWaiter!.Set();
                        break;
                    case PAL_NwStatusUpdates.ConnectionWriteFinished:
                    case PAL_NwStatusUpdates.ConnectionWriteFailed:
                        context._writeStatus = data1.ToInt32();
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }

                return 0;
            }
            catch
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly]
        private static unsafe int WriteToConnection(IntPtr connection, byte* data, void** dataLength)
        {

            GCHandle gcHandle = GCHandle.FromIntPtr(connection);

            SafeDeleteSslContext? context = (SafeDeleteSslContext?)GCHandle.FromIntPtr(connection).Target;
            if (context == null || context._disposed)
            {
                //*dataLength = 0;
                return -1;
            }

            // We don't pool these buffers and we can't because there's a race between their us in the native
            // read/write callbacks and being disposed when the SafeHandle is disposed. This race is benign currently,
            // but if we were to pool the buffers we would have a potential use-after-free issue.
            try
            {
                ulong length = (ulong)*dataLength;
                Debug.Assert(length <= int.MaxValue);

                int toWrite = (int)length;
                var inputBuffer = new ReadOnlySpan<byte>(data, toWrite);

                if (context.UseNwFramework)
                {
                        lock (context._writeWaiter!)
                        {
                            context._outputBuffer.EnsureAvailableSpace(toWrite);
                            inputBuffer.CopyTo(context._outputBuffer.AvailableSpan);
                            context._outputBuffer.Commit(toWrite);


                             if (!context._handshakeDone)
                            {
                                // get new TCS before signalling completion to avoild race condition
                                var Tcs = context.Tcs;
                                context.Tcs = new TaskCompletionSource<SecurityStatusPalErrorCode>();
                                Tcs!.TrySetResult(SecurityStatusPalErrorCode.ContinuePendig);
                            }
                        }
                        context._writeWaiter!.Set();
                }
                else
                {
                    lock (context)
                    {
                        context._outputBuffer.EnsureAvailableSpace(toWrite);
                        inputBuffer.CopyTo(context._outputBuffer.AvailableSpan);
                        context._outputBuffer.Commit(toWrite);
                    }
                }

                return OSStatus_noErr;
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

        internal int Write(ReadOnlySpan<byte> buf)
        {
            lock (this)
            {
                if (_disposed)
                {
                    return -1;
                }

                _inputBuffer.EnsureAvailableSpace(buf.Length);
                buf.CopyTo(_inputBuffer.AvailableSpan);
                _inputBuffer.Commit(buf.Length);
                return 0;
            }
        }

        internal int Read(Span<byte> buf)
        {
            lock (this)
            {
                int length = Math.Min(_inputBuffer.ActiveLength, buf.Length);
                _inputBuffer.ActiveSpan.Slice(0, length).CopyTo(buf);
                _inputBuffer.Discard(length);
                return length;
            }
        }

        internal unsafe int Decrypt(Span<byte> buffer)
        {
            // notify TLS we received EOF
            if (buffer.Length == 0)
            {
                Interop.AppleCrypto.NwProcessInputData(SslContext, _framer, null, 0);
                return 0;
            }

            if (_framer == IntPtr.Zero)
            {
                // We are shutting down
                return -1;
            }
            fixed (byte* ptr = buffer)
            {
                Interop.AppleCrypto.NwProcessInputData(SslContext, _framer, ptr, buffer.Length);
            }
            return 0;
        }

        internal unsafe void Encrypt(void*  buffer, int bufferLength, ref ProtocolToken token)
        {
            _writeWaiter!.Reset();
            Interop.AppleCrypto.NwSendToConnection(SslContext, GCHandle.ToIntPtr(gcHandle), buffer, bufferLength);
            // this will be updated by WriteCompleted
            _writeWaiter!.Wait();

            if (_writeStatus == 0)
            {
                token.Status = new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
                ReadPendingWrites(ref token);
            }
            else
            {
                token.Status = new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError,
                                        Interop.AppleCrypto.CreateExceptionForOSStatus((int)_writeStatus));
            }
        }

        // returns of available decrypted bytes or -1 if EOF was reached
        internal int BytesReadyFromConnection {
            get {
                lock (this)
                {
                    if (_inputBuffer.ActiveLength > 0)
                    {
                        return  _inputBuffer.ActiveLength;
                    }

                    return _readStatus == OSStatus_noErr ? 0 : -1 ;
                }
            }
        }

        internal int BytesReadyForConnection => _outputBuffer.ActiveLength;

        internal void ReadPendingWrites(ref ProtocolToken token)
        {
            object lockObject = UseNwFramework ? _writeWaiter! : _sslContext;
            lock (lockObject)
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

        private static (SslProtocols, SslProtocols) GetMinMaxProtocols(SslProtocols protocols)
        {
             (int minIndex, int maxIndex) = protocols.ValidateContiguous(s_orderedSslProtocols);
            SslProtocols minProtocolId = s_orderedSslProtocols[minIndex];
            SslProtocols maxProtocolId = s_orderedSslProtocols[maxIndex];

            return (minProtocolId, maxProtocolId);
        }

        private static void SetProtocols(SafeSslHandle sslContext, SslProtocols protocols)
        {
            (SslProtocols minProtocolId, SslProtocols maxProtocolId) = GetMinMaxProtocols(protocols);
            // Set the min and max.
            Interop.AppleCrypto.SslSetMinProtocolVersion(sslContext, minProtocolId);
            Interop.AppleCrypto.SslSetMaxProtocolVersion(sslContext, maxProtocolId);
        }

        internal static void SetCertificate(SafeSslHandle sslContext, SslStreamCertificateContext context)
        {
            Debug.Assert(sslContext != null, "sslContext != null");

            IntPtr[] ptrs = new IntPtr[context!.IntermediateCertificates.Count + 1];

            for (int i = 0; i < context.IntermediateCertificates.Count; i++)
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
                    intermediateCert = X509CertificateLoader.LoadCertificate(intermediateCert.RawDataMemory.Span);
                }

                ptrs[i + 1] = intermediateCert.Handle;
            }

            ptrs[0] = context!.TargetCertificate.Handle;

            Interop.AppleCrypto.SslSetCertificate(sslContext, ptrs);
        }

        public unsafe Task<SecurityStatusPalErrorCode> StartDecrypt()
        {
            //Debug.Assert(size > 0);

            if (Tcs == null)
            {
                Tcs = new TaskCompletionSource<SecurityStatusPalErrorCode>();
                Interop.AppleCrypto.NwReadFromConnection(SslContext, GCHandle.ToIntPtr(gcHandle));
            }

            return Tcs.Task;
        }

        public unsafe SecurityStatusPal PerformNwHandshake(ReadOnlySpan<byte> inputBuffer)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (handshakeStarted && inputBuffer.Length == 0)
            {
                // We may be asked to generate Alter tokens and that is not supported on macOS
                return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
            }

            if (_handshakeDone)
            {
                return new SecurityStatusPal(SecurityStatusPalErrorCode.ContinueNeeded);
            }
            if (_framer != IntPtr.Zero && inputBuffer.Length > 0)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                fixed (byte* ptr = &MemoryMarshal.GetReference(inputBuffer))
                {
                    Interop.AppleCrypto.NwProcessInputData(SslContext, _framer, ptr, inputBuffer.Length);
                }

                return new SecurityStatusPal(SecurityStatusPalErrorCode.ContinuePendig);
            }

            if (inputBuffer.Length == 0)
            {
                Debug.Assert(handshakeStarted == false);

                handshakeStarted = true;
                bool add = false;
                // We grab reference to prevent disposal while handshake is pending.
                this.DangerousAddRef(ref add);
                ObjectDisposedException.ThrowIf(_disposed, this);
                Interop.AppleCrypto.NwStartHandshake(SslContext, GCHandle.ToIntPtr(gcHandle));
            }
            return new SecurityStatusPal(SecurityStatusPalErrorCode.ContinuePendig);
        }
    }
}
