// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Runtime.ExceptionServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using SafeNwHandle = Interop.SafeNwHandle;
using NetworkFrameworkStatusUpdates = Interop.NetworkFramework.StatusUpdates;
using NwOSStatus = Interop.AppleCrypto.OSStatus;
using ResettableValueTaskSource = System.Net.Quic.ResettableValueTaskSource;

namespace System.Net.Security
{
    /// <summary>
    /// Network Framework-specific SSL/TLS context implementation for macOS.
    /// This class provides secure connection management using Apple's modern
    /// Network Framework APIs while presenting a synchronous interface
    /// consistent with SecureTransport.
    /// </summary>
    internal sealed class SafeDeleteNwContext : SafeDeleteContext
    {
        // AppContext switch to enable Network Framework usage
        internal static bool IsSwitchEnabled { get; } = AppContextSwitchHelper.GetBooleanConfig(
            "System.Net.Security.UseNetworkFramework",
            "DOTNET_SYSTEM_NET_SECURITY_USENETWORKFRAMEWORK");
        private static readonly Lazy<bool> s_isNetworkFrameworkAvailable = new Lazy<bool>(CheckNetworkFrameworkAvailability);

        // Network Framework handles
        private readonly SafeNwHandle _connectionHandle;
        private SafeNwHandle? _framerHandle;
        private SafeX509ChainHandle? _peerCertChainHandle;
        internal SafeX509ChainHandle? PeerX509ChainHandle => _peerCertChainHandle;
        internal SafeNwHandle ConnectionHandle => _connectionHandle;

        // Keep-Alive Handles
        private readonly GCHandle _thisHandle;

        // Lock object
        private readonly object _lockObject = new();

        // Handshake state machine
        private enum HandshakeState
        {
            NotStarted,
            InProgress,
            WaitingForPeer,
            Completed,
            Failed
        }

        private readonly Stream _transportStream;
        private readonly SslAuthenticationOptions _sslAuthenticationOptions;
        private TaskCompletionSource<Exception?> _handshakeCompletionSource = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        private Task? _transportReadTask;
        private ArrayBuffer _appReceiveBuffer = new(InitialBufferSize);
        private ResettableValueTaskSource _appReceiveBufferTcs = new ResettableValueTaskSource();
        private CancellationTokenSource _shutdownCts = new CancellationTokenSource();

        // Buffers
        private const int InitialBufferSize = 2 * 1024;
        private ArrayBuffer _outputBuffer = new(InitialBufferSize);

        private bool _disposed;
        private bool _clientCertificateRequested;
        private int _challengeCallbackCompleted;  // 0 = not called, 1 = called
        private IntPtr _selectedClientCertificate;  // Cached result from challenge callback
        // TODO: Bind every tcs to the message, so we can complete it when the specific message is sent (avoiding wrong completions by post-handshake message etc.).
        private TaskCompletionSource? _currentWriteCompletionSource;

        public SafeDeleteNwContext(SslAuthenticationOptions sslAuthenticationOptions, Stream transportStream) : base(IntPtr.Zero)
        {
            _connectionHandle = Interop.NetworkFramework.Tls.CreateContext(sslAuthenticationOptions.IsServer);
            _transportStream = transportStream;
            _sslAuthenticationOptions = sslAuthenticationOptions;
            if (_connectionHandle.IsInvalid)
            {
                throw new Exception("Failed to create Network Framework connection"); // TODO: Make this string resource
            }

            ValidateSslAuthenticationOptions(sslAuthenticationOptions);
            _thisHandle = GCHandle.Alloc(this, GCHandleType.Normal);
            SetTlsOptions(sslAuthenticationOptions);
        }

        public static bool IsNetworkFrameworkAvailable => IsSwitchEnabled && s_isNetworkFrameworkAvailable.Value;

        internal bool ClientCertificateRequested => _clientCertificateRequested;

        internal async Task<Exception?> HandshakeAsync(CancellationToken cancellationToken)
        {
            Interop.NetworkFramework.Tls.StartTlsHandshake(_connectionHandle, GCHandle.ToIntPtr(_thisHandle));

            using CancellationTokenRegistration registration = cancellationToken.UnsafeRegister(static (state, token) =>
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "Handshake cancellation requested");
                ((TaskCompletionSource<Exception?>)state!).TrySetCanceled(token);
            }, _handshakeCompletionSource);

            _transportReadTask = Task.Run(async () =>
            {
                try
                {
                    byte[] buffer = new byte[16 * 1024];
                    Memory<byte> readBuffer = new Memory<byte>(buffer);

                    while (!_shutdownCts.IsCancellationRequested)
                    {
                        // Read data from the transport stream
                        int bytesRead = await _transportStream.ReadAsync(readBuffer, _shutdownCts.Token).ConfigureAwait(false);

                        if (bytesRead > 0)
                        {
                            // Process the read data
                            await WriteInboundWireDataAsync(readBuffer.Slice(0, bytesRead)).ConfigureAwait(false);
                        }
                        else
                        {
                            // EOF reached, signal completion
                            // TODO: can this race with actual handshake completion?
                            Interop.NetworkFramework.Tls.CancelConnection(_connectionHandle);
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Handle cancellation gracefully
                }
                catch (Exception ex)
                {
                    // Propagate transport stream exceptions to the handshake
                    _handshakeCompletionSource.TrySetException(ex);
                    _currentWriteCompletionSource?.TrySetException(ex);
                }
            }, cancellationToken);

            return await _handshakeCompletionSource.Task.ConfigureAwait(false);
        }

        internal async Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"App sending {buffer.Length} bytes");

            TaskCompletionSource transportWriteCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _currentWriteCompletionSource = transportWriteCompletion;

            TaskCompletionSource<long> tcs = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);

            using CancellationTokenRegistration registration = cancellationToken.UnsafeRegister(static (state, token) =>
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "Cancellation requested for WriteAsync");
                ((TaskCompletionSource<long>)state!).TrySetCanceled(token);
            }, tcs);

            GCHandle handle = GCHandle.Alloc(tcs, GCHandleType.Normal);

            using MemoryHandle memoryHandle = buffer.Pin();
            unsafe
            {
                Interop.NetworkFramework.Tls.SendToConnection(_connectionHandle, GCHandle.ToIntPtr(_thisHandle), memoryHandle.Pointer, buffer.Length, GCHandle.ToIntPtr(handle), &CompletionCallback);
            }

            long status = await tcs.Task.ConfigureAwait(false);

            if (status != 0)
            {
                // SendToConnection failed, complete the transport write
                _currentWriteCompletionSource = null;
                transportWriteCompletion.TrySetException(Interop.AppleCrypto.CreateExceptionForOSStatus((int)status));
            }

            // Wait for the transport write to complete
            await transportWriteCompletion.Task.ConfigureAwait(false);

            [UnmanagedCallersOnly]
            static void CompletionCallback(IntPtr context, long status)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Completing WriteAsync", nameof(WriteAsync));
                GCHandle handle = GCHandle.FromIntPtr(context);
                TaskCompletionSource<long> tcs = (TaskCompletionSource<long>)handle.Target!;

                tcs.TrySetResult(status);
                handle.Free();
            }
        }

        internal ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (_appReceiveBuffer.ActiveLength > 0)
            {
                // fast path, data available
                int length = Math.Min(_appReceiveBuffer.ActiveLength, buffer.Length);
                _appReceiveBuffer.ActiveSpan.Slice(0, length).CopyTo(buffer.Span);
                _appReceiveBuffer.Discard(length);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Read {length} bytes");
                return ValueTask.FromResult(length);
            }

            return ReadAsyncInternal(buffer, cancellationToken);

            async ValueTask<int> ReadAsyncInternal(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Internal buffer empty, refilling.");
                await FillAppDataBufferAsync(cancellationToken).ConfigureAwait(false);

                if (_appReceiveBuffer.ActiveLength == 0)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "ReadAsync returning 0 bytes, end of stream reached");
                    return 0; // EOF
                }

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"ReadAsync filled buffer with {_appReceiveBuffer.ActiveLength} bytes");

                int length = Math.Min(_appReceiveBuffer.ActiveLength, buffer.Length);
                _appReceiveBuffer.ActiveSpan.Slice(0, length).CopyTo(buffer.Span);
                _appReceiveBuffer.Discard(length);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Read {length} bytes");
                return length;
            }
        }

        internal ValueTask FillAppDataBufferAsync(CancellationToken cancellationToken)
        {
            if (!_appReceiveBufferTcs.TryGetValueTask(out ValueTask valueTask, null, cancellationToken))
            {
                return ValueTask.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(new InvalidOperationException(SR.Format(SR.net_io_invalidnestedcall, "write"))));
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Waiting for read from connection");
            unsafe
            {
                Interop.NetworkFramework.Tls.ReadFromConnection(_connectionHandle, GCHandle.ToIntPtr(_thisHandle), 16 * 1024, GCHandle.ToIntPtr(_thisHandle), &CompletionCallback);
            }

            return valueTask;

            [UnmanagedCallersOnly]
            static unsafe void CompletionCallback(IntPtr context, long status, byte* data, int length)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Completing ConnectionRead, status: {status}, len: {length}", nameof(FillAppDataBufferAsync));
                GCHandle handle = GCHandle.FromIntPtr(context);
                SafeDeleteNwContext thisContext = (SafeDeleteNwContext)handle.Target!;
                ref ArrayBuffer buffer = ref thisContext._appReceiveBuffer;

                if (status != 0)
                {
                    thisContext._appReceiveBufferTcs.TrySetException(ExceptionDispatchInfo.SetCurrentStackTrace(Interop.AppleCrypto.CreateExceptionForOSStatus((int)status)));
                }
                else
                {
                    buffer.EnsureAvailableSpace(length);
                    new Span<byte>(data, length).CopyTo(buffer.AvailableSpan);
                    buffer.Commit(length);
                    thisContext._appReceiveBufferTcs.TrySetResult();
                }
            }
        }

        private static bool CheckNetworkFrameworkAvailability()
        {
            try
            {
                unsafe
                {
                    // Call Init with null callbacks to check if Network Framework is available
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "Checking Network Framework availability...");
                    return !Interop.NetworkFramework.Tls.Init(&StatusUpdateCallback, &WriteOutboundWireData, &ChallengeCallback);
                }
            }
            catch
            {
                return false;
            }
        }

        private static void ValidateSslAuthenticationOptions(SslAuthenticationOptions options)
        {
            switch (options.EncryptionPolicy)
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
                    throw new PlatformNotSupportedException(SR.Format(SR.net_encryptionpolicy_notsupported, options.EncryptionPolicy));
            }
        }

        private void SetTlsOptions(SslAuthenticationOptions options)
        {
            Interop.NetworkFramework.Tls.SetTlsOptions(_connectionHandle, GCHandle.ToIntPtr(_thisHandle), options);
        }

        protected override bool ReleaseHandle()
        {
            if (_thisHandle.IsAllocated)
            {
                _thisHandle.Free();
            }
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                lock (_lockObject)
                {
                    if (!_disposed)
                    {
                        _handshakeCompletionSource.TrySetResult(new ObjectDisposedException(nameof(SafeDeleteNwContext)));
                        Interop.NetworkFramework.Tls.CancelConnection(_connectionHandle);
                        // TODO: Wait for connection cancellation through status update with TCS.
                        _disposed = true;
                        // Complete any pending writes with disposed exception
                        TaskCompletionSource? writeCompletion = _currentWriteCompletionSource;
                        if (writeCompletion != null)
                        {
                            _currentWriteCompletionSource = null;
                            writeCompletion.TrySetException(new ObjectDisposedException(nameof(SafeDeleteNwContext)));
                        }

                        _connectionHandle.Dispose();
                        _outputBuffer.Dispose();
                        _peerCertChainHandle?.Dispose();
                    }
                }
            }
            base.Dispose(disposing);
        }

        private static SafeDeleteNwContext? ResolveThisHandle(IntPtr thisHandle)
        {
            try
            {
                return (SafeDeleteNwContext?)GCHandle.FromIntPtr(thisHandle).Target;
            }
            catch
            {
                return null;
            }
        }

        private void EnsureKeepAlive()
        {
            GC.KeepAlive(this);
        }

        [UnmanagedCallersOnly]
        private static unsafe int WriteOutboundWireData(IntPtr thisHandle, byte* data, void** dataLength)
        {
            SafeDeleteNwContext? nwContext = ResolveThisHandle(thisHandle);
            if (nwContext == null || nwContext._disposed)
            {
                return (int)NwOSStatus.WritErr;
            }

            try
            {
                nwContext.EnsureKeepAlive();
                ulong length = (ulong)*dataLength;
                Debug.Assert(length <= int.MaxValue);

                nwContext.WriteOutboundWireData(new ReadOnlySpan<byte>(data, (int)length));

                return (int)NwOSStatus.NoErr;
            }
            catch (Exception e)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(nwContext, $"WriteOutboundWireData Failed: {e.Message}");
                }

                // Complete the write operation with the exception
                TaskCompletionSource? writeCompletion = nwContext._currentWriteCompletionSource;
                if (writeCompletion != null)
                {
                    nwContext._currentWriteCompletionSource = null;
                    writeCompletion.TrySetException(e);
                }

                return (int)NwOSStatus.WritErr;
            }
            finally
            {
                nwContext?.EnsureKeepAlive();
                nwContext?._currentWriteCompletionSource?.TrySetResult();
                nwContext?._currentWriteCompletionSource = null;
            }
        }

        [UnmanagedCallersOnly]
        private static IntPtr ChallengeCallback(IntPtr thisHandle, IntPtr acceptableIssuersHandle, IntPtr remoteCertificateHandle)
        {
            SafeDeleteNwContext? nwContext = ResolveThisHandle(thisHandle);
            if (nwContext == null || nwContext._disposed)
            {
                return IntPtr.Zero;
            }

            try
            {
                nwContext.EnsureKeepAlive();

                // Check if we've already processed the challenge callback
                if (Interlocked.CompareExchange(ref nwContext._challengeCallbackCompleted, 1, 0) != 0)
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        NetEventSource.Info(nwContext, $"ChallengeCallback already processed, returning cached result: {nwContext._selectedClientCertificate}");
                    }
                    return nwContext._selectedClientCertificate;
                }

                nwContext._clientCertificateRequested = true;

                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(nwContext, $"ChallengeCallback invoked, acceptableIssuersHandle: {acceptableIssuersHandle}, remoteCertificateHandle: {remoteCertificateHandle}");
                }

                if (nwContext._sslAuthenticationOptions.CertificateContext != null)
                {
                    X509Certificate2 cert = nwContext._sslAuthenticationOptions.CertificateContext.TargetCertificate;
                    if (cert.HasPrivateKey)
                    {
                        nwContext._selectedClientCertificate = cert.Handle;
                        return nwContext._selectedClientCertificate;
                    }
                }

                if (nwContext._sslAuthenticationOptions.CertSelectionDelegate != null)
                {
                    X509Certificate2? selectedCert = null;
                    X509Certificate2? remoteCertificate = null;
                    try
                    {
                        // Get the local certificates collection
                        X509CertificateCollection localCertificates = nwContext._sslAuthenticationOptions.ClientCertificates ?? new X509CertificateCollection();

                        // Extract acceptable issuers from the CFArrayRef
                        string[] acceptableIssuers = ExtractAcceptableIssuers(acceptableIssuersHandle);

                        if (remoteCertificateHandle != IntPtr.Zero)
                        {
                            remoteCertificate = new X509Certificate2(remoteCertificateHandle);
                        }

                        X509Certificate? selected = nwContext._sslAuthenticationOptions.CertSelectionDelegate(
                            nwContext._sslAuthenticationOptions, // TODO: Should we pass SslStream instance here?
                            nwContext._sslAuthenticationOptions.TargetHost,
                            localCertificates,
                            remoteCertificate,
                            acceptableIssuers);

                        if (selected is X509Certificate2 cert2)
                        {
                            selectedCert = cert2;
                        }
                        else if (selected != null)
                        {
                            selectedCert = new X509Certificate2(selected);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (NetEventSource.Log.IsEnabled())
                        {
                            NetEventSource.Error(nwContext, $"LocalCertificateSelectionCallback failed: {ex.Message}");
                        }
                    }
                    finally
                    {
                        remoteCertificate?.Dispose();
                    }

                    if (selectedCert != null && selectedCert.HasPrivateKey)
                    {
                        nwContext._selectedClientCertificate = selectedCert.Handle;
                        return nwContext._selectedClientCertificate;
                    }
                }

                // No certificate selected
                nwContext._selectedClientCertificate = IntPtr.Zero;
                return nwContext._selectedClientCertificate;
            }
            catch (Exception e)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(nwContext, $"ChallengeCallback Failed: {e.Message}");
                }
                nwContext._selectedClientCertificate = IntPtr.Zero;
                return nwContext._selectedClientCertificate;
            }
            finally
            {
                nwContext?.EnsureKeepAlive();
            }
        }

        private void WriteOutboundWireData(ReadOnlySpan<byte> data)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Sending {data.Length} bytes");

            lock (_lockObject)
            {
                _transportStream.Write(data);
            }
        }

        private async Task WriteInboundWireDataAsync(ReadOnlyMemory<byte> buf)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Receiving {buf.Length} bytes");

            if (_framerHandle != null && buf.Length > 0)
            {
                TaskCompletionSource tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                GCHandle handle = GCHandle.Alloc(tcs, GCHandleType.Normal);
                using MemoryHandle memoryHandle = buf.Pin();

                unsafe
                {
                    Interop.NetworkFramework.Tls.ProcessInputData(_connectionHandle, _framerHandle, (byte*)memoryHandle.Pointer, buf.Length, GCHandle.ToIntPtr(handle), &CompletionCallback);
                }

                await tcs.Task.ConfigureAwait(false);
            }

            [UnmanagedCallersOnly]
            static void CompletionCallback(IntPtr context, long status)
            {
                Debug.Assert(status == 0, $"CompletionCallback called with status {status}");

                GCHandle handle = GCHandle.FromIntPtr(context);
                TaskCompletionSource tcs = (TaskCompletionSource)handle.Target!;

                tcs.SetResult(); // Signal completion
                handle.Free();
            }
        }

        public override bool IsInvalid => _connectionHandle.IsInvalid || (_framerHandle?.IsInvalid ?? true);

        [UnmanagedCallersOnly]
        private static unsafe void StatusUpdateCallback(IntPtr thisHandle, NetworkFrameworkStatusUpdates statusUpdate, IntPtr data, IntPtr data2)
        {
            SafeDeleteNwContext? nwContext = null;

            if (thisHandle != IntPtr.Zero)
            {
                nwContext = ResolveThisHandle(thisHandle);
                if (nwContext == null)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, "Failed to resolve context handle");
                    return;
                }
            }

            if (NetEventSource.Log.IsEnabled() && statusUpdate != NetworkFrameworkStatusUpdates.DebugLog) NetEventSource.Info(nwContext, $"Received status update: {statusUpdate}");

            try
            {
                nwContext?.EnsureKeepAlive();
                switch (statusUpdate)
                {
                    case NetworkFrameworkStatusUpdates.FramerStart:
                        nwContext?.FramerStartCallback(new SafeNwHandle(Interop.NetworkFramework.Retain(data), true));
                        break;
                    case NetworkFrameworkStatusUpdates.FramerStop:
                        nwContext?.FramerStopCallback();
                        break;
                    case NetworkFrameworkStatusUpdates.HandshakeFinished:
                        nwContext?.HandshakeFinished();
                        break;
                    case NetworkFrameworkStatusUpdates.ConnectionFailed:
                        nwContext?.ConnectionFailed(data.ToInt32());
                        break;
                    case NetworkFrameworkStatusUpdates.ConnectionCancelled:
                        nwContext?.ConnectionClosed();
                        break;
                    case NetworkFrameworkStatusUpdates.CertificateAvailable:
                        //handle.SetHandle(data);
                        global::System.Runtime.InteropServices.Marshalling.SafeHandleMarshaller<global::Microsoft.Win32.SafeHandles.SafeX509ChainHandle>.ManagedToUnmanagedOut marshaller = new();
                        marshaller.FromUnmanaged(data);
                        nwContext?.CertificateAvailable(marshaller.ToManaged());
                        marshaller.Free();
                        break;
                    case NetworkFrameworkStatusUpdates.DebugLog:
                        HandleDebugLog(nwContext, data, data2);
                        break;
                    default: // We shouldn't hit here.
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(nwContext, $"Unknown status update: {statusUpdate}");
                        Debug.Assert(false);
                        break;
                }
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(nwContext, $"Exception: {ex.Message}");
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(nwContext, $"StatusUpdateCallback failed: {ex}");
                }
            }
            finally
            {
                nwContext?.EnsureKeepAlive();
            }
        }
        private void FramerStartCallback(SafeNwHandle framerHandle)
        {
            _framerHandle = framerHandle;
        }

        private void FramerStopCallback()
        {
            _framerHandle?.Dispose();
            _framerHandle = null;
        }

        private void HandshakeFinished()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "TLS handshake completed successfully");
            _handshakeCompletionSource.TrySetResult(null);
        }

        private void ConnectionFailed(int errorCode)
        {
            Exception ex = Interop.AppleCrypto.CreateExceptionForOSStatus(errorCode);
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"TLS handshake failed with error code: {errorCode} ({ex.Message})");
            _handshakeCompletionSource.TrySetResult(ExceptionDispatchInfo.SetCurrentStackTrace(ex));
        }

        private void ConnectionClosed()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Connection was cancelled");
            _handshakeCompletionSource.TrySetResult(ExceptionDispatchInfo.SetCurrentStackTrace(
                new IOException(SR.net_io_eof)));
            // Complete any pending writes with connection closed exception
            TaskCompletionSource? writeCompletion = _currentWriteCompletionSource;
            if (writeCompletion != null)
            {
                _currentWriteCompletionSource = null;
                writeCompletion.TrySetException(new IOException(SR.net_io_eof));
            }
        }

        private void CertificateAvailable(SafeX509ChainHandle peerCertChainHandle)
        {
            _peerCertChainHandle = peerCertChainHandle;
        }

        private static void HandleDebugLog(SafeDeleteNwContext? nwContext, IntPtr _, IntPtr data)
        {
            if (NetEventSource.Log.IsEnabled())
                NetEventSource.Info(nwContext, Marshal.PtrToStringAnsi(data)!, "Native");
        }

        private static string[] ExtractAcceptableIssuers(IntPtr acceptableIssuersHandle)
        {
            if (acceptableIssuersHandle == IntPtr.Zero)
            {
                return Array.Empty<string>();
            }

            var issuers = new List<string>();

            try
            {
                using var arrayHandle = new Microsoft.Win32.SafeHandles.SafeCFArrayHandle(acceptableIssuersHandle, ownsHandle: false);
                int count = (int)Interop.CoreFoundation.CFArrayGetCount(arrayHandle);

                for (int i = 0; i < count; i++)
                {
                    IntPtr dataRef = Interop.CoreFoundation.CFArrayGetValueAtIndex(arrayHandle, i);
                    if (dataRef != IntPtr.Zero)
                    {
                        // Create a non-owning handle for the CFData
                        using var dataHandle = new Microsoft.Win32.SafeHandles.SafeCFDataHandle(dataRef, ownsHandle: false);
                        // Get the DER-encoded DN data
                        byte[] derData = Interop.CoreFoundation.CFGetData(dataHandle);

                        if (derData.Length > 0)
                        {
                            // Convert DER-encoded DN to string using X500DistinguishedName
                            try
                            {
                                X500DistinguishedName dn = new X500DistinguishedName(derData);
                                string issuerDn = dn.Name;
                                issuers.Add(issuerDn);
                            }
                            catch (Exception ex)
                            {
                                if (NetEventSource.Log.IsEnabled())
                                {
                                    NetEventSource.Error(null, $"Failed to parse DN at index {i}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(null, $"Failed to extract acceptable issuers: {ex.Message}");
                }
            }

            return issuers.ToArray();
        }
    }
}
