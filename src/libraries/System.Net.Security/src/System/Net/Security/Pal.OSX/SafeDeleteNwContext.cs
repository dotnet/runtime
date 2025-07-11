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
using SafeCFStringHandle = Microsoft.Win32.SafeHandles.SafeCFStringHandle;
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

        private const int InitialReceiveBufferSize = 2 * 1024;

        // backreference to the SslStream instance
        private readonly SslStream _sslStream;
        private Stream TransportStream => _sslStream.InnerStream;
        private SslAuthenticationOptions SslAuthenticationOptions => _sslStream._sslAuthenticationOptions;

        // Underlying nw_connection_t handle
        internal readonly SafeNwHandle ConnectionHandle;
        // nw_framer_t handle for tunneling messages
        private SafeNwHandle? _framerHandle;

        // Temporary storage for data that are available only during callback
        private string[] _acceptableIssuers = Array.Empty<string>();
        internal string[] AcceptableIssuers => _acceptableIssuers;

        // peer certificate chain (obtained from callback once available)
        private SafeX509ChainHandle? _peerCertChainHandle;
        internal SafeX509ChainHandle? PeerX509ChainHandle => _peerCertChainHandle;

        // Provides backreference from native code
        private readonly GCHandle _thisHandle;

        private TaskCompletionSource<Exception?> _handshakeCompletionSource = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        private Task? _transportReadTask;
        private ResettableValueTaskSource _transportReadTcs = new ResettableValueTaskSource();
        private ArrayBuffer _appReceiveBuffer = new(InitialReceiveBufferSize);
        private ResettableValueTaskSource _appReceiveBufferTcs = new ResettableValueTaskSource();
        private Task? _pendingAppReceiveBufferFillTask;
        private readonly TaskCompletionSource _connectionClosedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        private CancellationTokenSource _shutdownCts = new CancellationTokenSource();

        private bool _disposed;
        private int _challengeCallbackCompleted;  // 0 = not called, 1 = called
        private IntPtr _selectedClientCertificate;  // Cached result from challenge callback
        // TODO: Bind every tcs to the message, so we can complete it when the specific message is sent (avoiding wrong completions by post-handshake message etc.).
        private TaskCompletionSource? _currentWriteCompletionSource;

        public SafeDeleteNwContext(SslStream stream) : base(IntPtr.Zero)
        {
            _sslStream = stream;
            ConnectionHandle = Interop.NetworkFramework.Tls.CreateContext(SslAuthenticationOptions.IsServer);
            if (ConnectionHandle.IsInvalid)
            {
                throw new Exception("Failed to create Network Framework connection"); // TODO: Make this string resource
            }

            ValidateSslAuthenticationOptions(SslAuthenticationOptions);
            _thisHandle = GCHandle.Alloc(this, GCHandleType.Normal);
            SetTlsOptions(SslAuthenticationOptions);
        }

        public static bool IsNetworkFrameworkAvailable => IsSwitchEnabled && s_isNetworkFrameworkAvailable.Value;

        internal bool ClientCertificateRequested => _challengeCallbackCompleted == 1 && _selectedClientCertificate != IntPtr.Zero;

        internal async Task<Exception?> HandshakeAsync(CancellationToken cancellationToken)
        {
            Interop.NetworkFramework.Tls.StartTlsHandshake(ConnectionHandle, GCHandle.ToIntPtr(_thisHandle));

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
                        int bytesRead = await TransportStream.ReadAsync(readBuffer, _shutdownCts.Token).ConfigureAwait(false);

                        if (bytesRead > 0)
                        {
                            // Process the read data
                            await WriteInboundWireDataAsync(readBuffer.Slice(0, bytesRead)).ConfigureAwait(false);
                        }
                        else
                        {
                            // EOF reached, signal completion
                            _transportReadTcs.TrySetResult(final: true);

                            // TODO: can this race with actual handshake completion?
                            Interop.NetworkFramework.Tls.CancelConnection(ConnectionHandle);
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

            TaskCompletionSource tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            using CancellationTokenRegistration registration = cancellationToken.UnsafeRegister(static (state, token) =>
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "Cancellation requested for WriteAsync");
                ((TaskCompletionSource)state!).TrySetCanceled(token);
            }, tcs);

            GCHandle handle = GCHandle.Alloc(tcs, GCHandleType.Normal);

            using MemoryHandle memoryHandle = buffer.Pin();
            unsafe
            {
                Interop.NetworkFramework.Tls.SendToConnection(ConnectionHandle, GCHandle.ToIntPtr(_thisHandle), memoryHandle.Pointer, buffer.Length, GCHandle.ToIntPtr(handle), &CompletionCallback);
            }
            try
            {
                await tcs.Task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _currentWriteCompletionSource = null;
                transportWriteCompletion.TrySetException(ex);
            }

            // Wait for the transport write to complete
            await transportWriteCompletion.Task.ConfigureAwait(false);

            [UnmanagedCallersOnly]
            static unsafe void CompletionCallback(IntPtr context, Interop.NetworkFramework.NetworkFrameworkError* error)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Completing WriteAsync", nameof(WriteAsync));
                GCHandle handle = GCHandle.FromIntPtr(context);
                TaskCompletionSource tcs = (TaskCompletionSource)handle.Target!;
                if (error != null)
                {
                    tcs.TrySetException(Interop.NetworkFramework.CreateExceptionForNetworkFrameworkError(in *error));
                }
                else
                {
                    tcs.TrySetResult();
                }
                handle.Free();
            }
        }

        internal ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (_pendingAppReceiveBufferFillTask == null && _appReceiveBuffer.ActiveLength > 0)
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

                // Previous read may have been canceled, leaving a pending read
                // in the underlying native layer. In such cases, we await the
                // pending read. Otherwise, we issue a new one.
                _pendingAppReceiveBufferFillTask ??= FillAppDataBufferAsync();
                await _pendingAppReceiveBufferFillTask.WaitAsync(cancellationToken).ConfigureAwait(false);
                _pendingAppReceiveBufferFillTask = null;

                if (_appReceiveBuffer.ActiveLength == 0)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "ReadAsync returning 0 bytes, end of stream reached");
                    _appReceiveBufferTcs.TrySetResult(final: true);
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

        internal Task FillAppDataBufferAsync()
        {
            bool success = _appReceiveBufferTcs.TryGetValueTask(out ValueTask valueTask, null, CancellationToken.None);
            Debug.Assert(success, "Concurrent FillAppDataBufferAsync detected");

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Waiting for read from connection");
            unsafe
            {
                Interop.NetworkFramework.Tls.ReadFromConnection(ConnectionHandle, GCHandle.ToIntPtr(_thisHandle), 16 * 1024, GCHandle.ToIntPtr(_thisHandle), &CompletionCallback);
            }

            return _pendingAppReceiveBufferFillTask = valueTask.AsTask();

            [UnmanagedCallersOnly]
            static unsafe void CompletionCallback(IntPtr context, Interop.NetworkFramework.NetworkFrameworkError* error, byte* data, int length)
            {
                GCHandle handle = GCHandle.FromIntPtr(context);
                SafeDeleteNwContext thisContext = (SafeDeleteNwContext)handle.Target!;
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(thisContext, $"Completing ConnectionRead, status: {(error != null ? error->ErrorCode : 0)}, len: {length}", nameof(FillAppDataBufferAsync));

                ref ArrayBuffer buffer = ref thisContext._appReceiveBuffer;

                if (error != null && error->ErrorCode != 0)
                {
                    thisContext._appReceiveBufferTcs.TrySetException(ExceptionDispatchInfo.SetCurrentStackTrace(Interop.NetworkFramework.CreateExceptionForNetworkFrameworkError(in *error)));
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

        internal void Shutdown()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Shutting down Network Framework context");

            Interop.NetworkFramework.Tls.CancelConnection(ConnectionHandle);
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
            Interop.NetworkFramework.Tls.SetTlsOptions(ConnectionHandle, GCHandle.ToIntPtr(_thisHandle), options);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                if (!_disposed)
                {
                    _disposed = true;

                    _shutdownCts.Cancel();
                    if (_pendingAppReceiveBufferFillTask is Task t)
                    {
                        t.Wait();
                    }

                    _handshakeCompletionSource.TrySetResult(new ObjectDisposedException(nameof(SafeDeleteNwContext)));
                    Interop.NetworkFramework.Tls.CancelConnection(ConnectionHandle);

                    // wait for callback signalling connection has been truly closed.
                    _connectionClosedTcs.Task.GetAwaiter().GetResult();

                    // TODO: Wait for connection cancellation through status update with TCS.
                    // Complete any pending writes with disposed exception
                    TaskCompletionSource? writeCompletion = _currentWriteCompletionSource;
                    if (writeCompletion != null)
                    {
                        _currentWriteCompletionSource = null;
                        writeCompletion.TrySetException(new ObjectDisposedException(nameof(SafeDeleteNwContext)));
                    }

                    ConnectionHandle.Dispose();
                    _peerCertChainHandle?.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private static SafeDeleteNwContext ResolveThisHandle(IntPtr thisHandle)
        {
            return (SafeDeleteNwContext)GCHandle.FromIntPtr(thisHandle).Target!;
        }

        [UnmanagedCallersOnly]
        private static unsafe int WriteOutboundWireData(IntPtr thisHandle, byte* data, void** dataLength)
        {
            SafeDeleteNwContext nwContext = ResolveThisHandle(thisHandle);

            try
            {
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
                nwContext?._currentWriteCompletionSource?.TrySetResult();
                nwContext?._currentWriteCompletionSource = null;
            }
        }

        [UnmanagedCallersOnly]
        private static IntPtr ChallengeCallback(IntPtr thisHandle, IntPtr acceptableIssuersHandle, IntPtr remoteCertificateHandle)
        {
            SafeDeleteNwContext nwContext = ResolveThisHandle(thisHandle);

            // the callback may end up being called multiple times for some reason.
            // check if we've already processed the challenge callback
            if (Interlocked.CompareExchange(ref nwContext._challengeCallbackCompleted, 1, 0) != 0)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(nwContext, $"ChallengeCallback already processed, returning cached result: {nwContext._selectedClientCertificate}");
                }
                return nwContext._selectedClientCertificate;
            }

            nwContext._acceptableIssuers = ExtractAcceptableIssuers(acceptableIssuersHandle);
            Debug.Assert(nwContext._peerCertChainHandle != null, "Peer certificate chain handle should be set before challenge callback");

            byte[]? dummy = null;
            nwContext._sslStream.AcquireClientCredentials(ref dummy, true);
            return nwContext._selectedClientCertificate = nwContext.SslAuthenticationOptions.CertificateContext?.TargetCertificate.Handle ?? IntPtr.Zero;
        }

        private void WriteOutboundWireData(ReadOnlySpan<byte> data)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Sending {data.Length} bytes");

            TransportStream.Write(data);
        }

        private async Task WriteInboundWireDataAsync(ReadOnlyMemory<byte> buf)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Receiving {buf.Length} bytes");

            if (_framerHandle != null && buf.Length > 0)
            {
                // the data needs to be pinned until the callback fires
                using MemoryHandle memoryHandle = buf.Pin();

                bool success = _transportReadTcs.TryGetValueTask(out ValueTask valueTask, null, CancellationToken.None);
                Debug.Assert(success, "Concurrent WriteInboundWireDataAsync detected");

                unsafe
                {
                    Interop.NetworkFramework.Tls.ProcessInputData(ConnectionHandle, _framerHandle, (byte*)memoryHandle.Pointer, buf.Length, GCHandle.ToIntPtr(_thisHandle), &CompletionCallback);
                }

                await valueTask.ConfigureAwait(false);
            }

            [UnmanagedCallersOnly]
            static unsafe void CompletionCallback(IntPtr context, Interop.NetworkFramework.NetworkFrameworkError* error)
            {
                Debug.Assert(error == null || error->ErrorCode == 0, $"CompletionCallback called with error {(error != null ? error->ErrorCode : 0)}");

                GCHandle handle = GCHandle.FromIntPtr(context);
                SafeDeleteNwContext thisContext = (SafeDeleteNwContext)handle.Target!;
                thisContext._transportReadTcs.TrySetResult();
            }
        }

        public override bool IsInvalid => ConnectionHandle.IsInvalid || (_framerHandle?.IsInvalid ?? true);

        [UnmanagedCallersOnly]
        private static unsafe void StatusUpdateCallback(IntPtr thisHandle, NetworkFrameworkStatusUpdates statusUpdate, IntPtr data, IntPtr data2, Interop.NetworkFramework.NetworkFrameworkError* error)
        {
            SafeDeleteNwContext? nwContext = null;

            if (thisHandle != IntPtr.Zero)
            {
                nwContext = ResolveThisHandle(thisHandle);
            }

            if (NetEventSource.Log.IsEnabled() && statusUpdate != NetworkFrameworkStatusUpdates.DebugLog) NetEventSource.Info(nwContext, $"Received status update: {statusUpdate}");

            try
            {
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
                        if (error != null)
                        {
                            nwContext?.ConnectionFailed(in *error);
                        }
                        break;
                    case NetworkFrameworkStatusUpdates.ConnectionCancelled:
                        nwContext?.ConnectionClosed();
                        break;
                    case NetworkFrameworkStatusUpdates.CertificateAvailable:
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

        private void ConnectionFailed(in Interop.NetworkFramework.NetworkFrameworkError error)
        {
            Exception ex = Interop.NetworkFramework.CreateExceptionForNetworkFrameworkError(in error);
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"TLS handshake failed with error: {ex.Message}");
            _handshakeCompletionSource.TrySetResult(ExceptionDispatchInfo.SetCurrentStackTrace(ex));
        }

        private void ConnectionClosed()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Connection was cancelled");
            _connectionClosedTcs.TrySetResult();
            _handshakeCompletionSource.TrySetResult(ExceptionDispatchInfo.SetCurrentStackTrace(
                new IOException(SR.net_io_eof)));
            // Complete any pending writes with connection closed exception
            TaskCompletionSource? writeCompletion = _currentWriteCompletionSource;
            if (writeCompletion != null)
            {
                _currentWriteCompletionSource = null;
                writeCompletion.TrySetException(new IOException(SR.net_io_eof));
            }

            _thisHandle.Free();
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
