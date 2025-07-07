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
        private const string EnableNetworkFrameworkSwitch = "System.Net.Security.UseNetworkFramework";
        private const string EnableNetworkFrameworkEnvironmentVariable = "DOTNET_SYSTEM_NET_SECURITY_USENETWORKFRAMEWORK";

        private static bool? s_isNetworkFrameworkEnabled;
        private static readonly Lazy<bool> s_isNetworkFrameworkAvailable = new Lazy<bool>(CheckNetworkFrameworkAvailability);

        // Network Framework handles
        private readonly Interop.SafeNwHandle _connectionHandle;
        private Interop.SafeNwHandle? _framerHandle;
        internal Interop.SafeNwHandle ConnectionHandle => _connectionHandle;

        // Keep-Alive Handles
        private readonly GCHandle _thisHandle;

        // Lock object
        private readonly object _lockObject = new();
        private readonly ManualResetEventSlim _readWaiter = new();
        private readonly ManualResetEventSlim _writeWaiter = new();

        // Handshake state machine
        private enum HandshakeState
        {
            NotStarted,
            InProgress,
            WaitingForPeer,
            Completed,
            Failed
        }

        private HandshakeState _handshakeState = HandshakeState.NotStarted;
        private Exception? _handshakeException;
        private readonly Stream _transportStream;
        private TaskCompletionSource<Exception?> _handshakeCompletionSource = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        private Task? _transportReadTask;
        private CancellationTokenSource _shutdownCts = new CancellationTokenSource();

        // TaskSourceCompletion objects for decrypt operations
        internal TaskCompletionSource<SecurityStatusPalErrorCode>? DecryptTask { get; private set; }

        // Read/Write Status
        private volatile int _readStatus;
        private volatile int _writeStatus;

        // Buffers
        private const int InitialBufferSize = 2 * 1024;
        private ArrayBuffer _outputBuffer = new(InitialBufferSize);

        private bool _disposed;

        public SafeDeleteNwContext(SslAuthenticationOptions sslAuthenticationOptions, Stream transportStream) : base(IntPtr.Zero)
        {
            _connectionHandle = Interop.NetworkFramework.Tls.CreateContext(sslAuthenticationOptions.IsServer);
            _transportStream = transportStream;
            if (_connectionHandle.IsInvalid)
            {
                throw new Exception("Failed to create Network Framework connection"); // TODO: Make this string resource
            }

            ValidateSslAuthenticationOptions(sslAuthenticationOptions);
            _thisHandle = GCHandle.Alloc(this, GCHandleType.Normal);
            SetTlsOptions(sslAuthenticationOptions);
        }

        public static bool IsNetworkFrameworkAvailable
        {
            get
            {
                // First check if Network Framework is enabled via switch
                if (!IsNetworkFrameworkEnabled())
                    return false;

                // Then check if it's technically available
                return s_isNetworkFrameworkAvailable.Value;
            }
        }

        internal Task<Exception?> HandshakeAsync(CancellationToken cancellationToken)
        {
            Interop.NetworkFramework.Tls.StartTlsHandshake(_connectionHandle, GCHandle.ToIntPtr(_thisHandle));

            // TODO: Handle cancellation token

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
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Handle cancellation gracefully
                }
            }, cancellationToken);

            return _handshakeCompletionSource.Task;
        }

        internal async Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"App sending {buffer.Length} bytes");

            TaskCompletionSource<long> tcs = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);

            using CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "Cancellation requested for WriteAsync");
                tcs.TrySetCanceled();
            });

            GCHandle handle = GCHandle.Alloc(tcs, GCHandleType.Normal);

            using MemoryHandle memoryHandle = buffer.Pin();
            unsafe
            {
                Interop.NetworkFramework.Tls.SendToConnection(_connectionHandle, GCHandle.ToIntPtr(_thisHandle), memoryHandle.Pointer, buffer.Length, GCHandle.ToIntPtr(handle), &CompletionCallback);
            }

            long status = await tcs.Task.ConfigureAwait(false);

            if (status != 0)
            {
                throw Interop.AppleCrypto.CreateExceptionForOSStatus(_writeStatus);
            }

            [UnmanagedCallersOnly]
            static void CompletionCallback(IntPtr context, long status)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Completing WriteAsync", "WriteAsync");
                GCHandle handle = GCHandle.FromIntPtr(context);
                TaskCompletionSource<long> tcs = (TaskCompletionSource<long>)handle.Target!;

                tcs.SetResult(status);
                handle.Free();
            }
        }

        internal async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Called with {buffer.Length} bytes", "ReadAsync");

            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            using CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "Cancellation requested for ReadAsync");
                tcs.TrySetCanceled();
            });

            Tuple<TaskCompletionSource<int>, Memory<byte>> state = new(tcs, buffer);
            GCHandle handle = GCHandle.Alloc(state, GCHandleType.Normal);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Waiting for read from connection");
            unsafe
            {
                Interop.NetworkFramework.Tls.ReadFromConnection(_connectionHandle, GCHandle.ToIntPtr(_thisHandle), buffer.Length, GCHandle.ToIntPtr(handle), &CompletionCallback);
            }

            return await tcs.Task.ConfigureAwait(false);

            [UnmanagedCallersOnly]
            static unsafe void CompletionCallback(IntPtr context, long status, byte* buffer, int length)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Completing ReadAsync, status: {status}, len: {length}", "ReadAsync");
                GCHandle handle = GCHandle.FromIntPtr(context);
                (TaskCompletionSource<int> tcs, Memory<byte> state) = (Tuple<TaskCompletionSource<int>, Memory<byte>>)handle.Target!;

                if (status != 0)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, $"ReadAsync failed with status: {status}", "ReadAsync");
                    tcs.TrySetException(ExceptionDispatchInfo.SetCurrentStackTrace(Interop.AppleCrypto.CreateExceptionForOSStatus((int)status)));
                }
                else
                {
                    new Span<byte>(buffer, length).CopyTo(state.Span);
                    tcs.TrySetResult(length);
                }

                handle.Free();
            }
        }

        private static bool IsNetworkFrameworkEnabled()
        {
            // Cache the result
            if (s_isNetworkFrameworkEnabled.HasValue)
                return s_isNetworkFrameworkEnabled.Value;

            // Check AppContext switch first
            if (AppContext.TryGetSwitch(EnableNetworkFrameworkSwitch, out bool isEnabled))
            {
                s_isNetworkFrameworkEnabled = isEnabled;
                return isEnabled;
            }

            // Fall back to environment variable
            string? envVar = Environment.GetEnvironmentVariable(EnableNetworkFrameworkEnvironmentVariable);
            if (!string.IsNullOrEmpty(envVar))
            {
                if (envVar == "1" || envVar.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    s_isNetworkFrameworkEnabled = true;
                    return true;
                }
                else if (envVar == "0" || envVar.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    s_isNetworkFrameworkEnabled = false;
                    return false;
                }
            }

            // Default to disabled if no explicit setting
            s_isNetworkFrameworkEnabled = false;
            return false;
        }

        private static bool CheckNetworkFrameworkAvailability()
        {
            try
            {
                unsafe
                {
                    // Call Init with null callbacks to check if Network Framework is available
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "Checking Network Framework availability...");
                    return !Interop.NetworkFramework.Tls.Init(&StatusUpdateCallback, &WriteOutboundWireData, &WriteOutboundWireData);
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
                        Interop.NetworkFramework.Tls.CancelConnection(_connectionHandle);
                        // TODO: Wait for connection cancellation through status update with TCS.
                        _disposed = true;
                        _connectionHandle.Dispose();
                        _framerHandle?.Dispose();
                        _outputBuffer.Dispose();
                        _readWaiter.Dispose();
                        _writeWaiter.Dispose();
                    }
                }
            }
            base.Dispose(disposing);
        }

        private static SafeDeleteNwContext? ResolveThisHandle(IntPtr thisHandle)
        {
            return (SafeDeleteNwContext?)GCHandle.FromIntPtr(thisHandle).Target;
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
                return (int)NwOSStatus.WritErr;
            }
            finally
            {
                nwContext?.EnsureKeepAlive();
            }
        }

        private void WriteOutboundWireData(ReadOnlySpan<byte> data)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Called with {data.Length} bytes", "WriteOutboundWireData");

            lock (_lockObject)
            {
                _transportStream.Write(data);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Sent {data.Length} bytes,", "WriteOutboundWireData");
            }
        }

        internal async Task WriteInboundWireDataAsync(ReadOnlyMemory<byte> buf)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Called with {buf.Length} bytes", "WriteInboundWireDataAsync");

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

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Completing", "WriteInboundWireDataAsync");
                GCHandle handle = GCHandle.FromIntPtr(context);
                TaskCompletionSource tcs = (TaskCompletionSource)handle.Target!;

                tcs.SetResult(); // Signal completion
                handle.Free();
            }
        }


        internal int Read(Span<byte> buf)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Called with {buf.Length} bytes", "Read");

            lock (_lockObject)
            {
                // int length = Math.Min(_inputBuffer.ActiveLength, buf.Length);
                // _inputBuffer.ActiveSpan.Slice(0, length).CopyTo(buf);
                // _inputBuffer.Discard(length);
                return 0;
            }
        }

        public override bool IsInvalid => _connectionHandle.IsInvalid || (_framerHandle?.IsInvalid ?? true);

        [UnmanagedCallersOnly]
        private static unsafe void StatusUpdateCallback(IntPtr thisHandle, NetworkFrameworkStatusUpdates statusUpdate, IntPtr data, IntPtr data2)
        {
            if (NetEventSource.Log.IsEnabled() && statusUpdate != NetworkFrameworkStatusUpdates.DebugLog) NetEventSource.Info(null, $"Received status update: {statusUpdate}", "StatusUpdateCallback");
            SafeDeleteNwContext? nwContext = null;

            if (thisHandle != IntPtr.Zero)
            {
                nwContext = ResolveThisHandle(thisHandle);
                if (nwContext == null)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, "Failed to resolve context handle", "StatusUpdateCallback");
                    return;
                }
            }

            try
            {
                nwContext?.EnsureKeepAlive();
                switch (statusUpdate)
                {
                    case NetworkFrameworkStatusUpdates.FramerStart:
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(nwContext, "FramerStart", "StatusUpdateCallback");
                        nwContext?.FramerStartCallback(new SafeNwHandle(data, true));
                        break;
                    case NetworkFrameworkStatusUpdates.FramerStop:
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(nwContext, "FramerStop", "StatusUpdateCallback");
                        nwContext?.FramerStopCallback();
                        break;
                    case NetworkFrameworkStatusUpdates.HandshakeFinished:
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(nwContext, "HandshakeFinished", "StatusUpdateCallback");
                        nwContext?.HandshakeFinished();
                        break;
                    case NetworkFrameworkStatusUpdates.HandshakeFailed:
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(nwContext, $"HandshakeFailed - errorCode: {data.ToInt32()}", "StatusUpdateCallback");
                        nwContext?.ConnectionFailed(data.ToInt32());
                        break;
                    case NetworkFrameworkStatusUpdates.ConnectionReadFinished:
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(nwContext, $"ConnectionReadFinished - dataLength: {data.ToInt32()}", "StatusUpdateCallback");
                        //nwContext.ConnectionReadFinished(new ReadOnlySpan<byte>((void*)data2, data.ToInt32()));
                        break;
                    case NetworkFrameworkStatusUpdates.ConnectionWriteFinished:
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(nwContext, "ConnectionWriteFinished", "StatusUpdateCallback");
                        nwContext?.SetConnectionWriteStatus(data.ToInt32());
                        break;
                    case NetworkFrameworkStatusUpdates.ConnectionWriteFailed:
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(nwContext, $"ConnectionWriteFailed - errorCode: {data.ToInt32()}", "StatusUpdateCallback");
                        nwContext?.SetConnectionWriteStatus(data.ToInt32());
                        break;
                    case NetworkFrameworkStatusUpdates.ConnectionCancelled:
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(nwContext, "ConnectionCancelled", "StatusUpdateCallback");
                        nwContext?.ConnectionCancelled();
                        break;
                    case NetworkFrameworkStatusUpdates.DebugLog:
                        HandleDebugLog(data, data2);
                        break;
                    default: // We shouldn't hit here.
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(nwContext, $"Unknown status update: {statusUpdate}", "StatusUpdateCallback");
                        Debug.Assert(false);
                        break;
                }
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(nwContext, $"Exception: {ex.Message}", "StatusUpdateCallback");
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
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Framer started", "FramerStartCallback");
            _framerHandle = framerHandle;
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Framer handle assigned", "FramerStartCallback");

            // The framer is now ready and output handler is set up
            // Network Framework should automatically generate ClientHello and call framer_output_handler
            // which should call WriteToConnection to buffer the data
            if (_handshakeState != HandshakeState.NotStarted && _handshakeState != HandshakeState.Completed)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Handshake in progress, framer ready", "FramerStartCallback");
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Waiting for Network Framework to generate ClientHello automatically", "FramerStartCallback");
            }
        }

        private void FramerStopCallback()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Framer stopped", "FramerStopCallback");
            _framerHandle?.Dispose();
            _framerHandle = null;
        }

        private void HandshakeFinished()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "TLS handshake completed successfully", "HandshakeFinished");
            _handshakeState = HandshakeState.Completed;
            _handshakeCompletionSource.TrySetResult(null);
        }

        private void ConnectionFailed(int errorCode)
        {
            Exception ex = Interop.AppleCrypto.CreateExceptionForOSStatus(errorCode);
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"TLS handshake failed with error code: {errorCode} ({ex.Message})", "HandshakeFailed");
            _handshakeException = ex;
            _handshakeState = HandshakeState.Failed;
            _handshakeCompletionSource.TrySetResult(ExceptionDispatchInfo.SetCurrentStackTrace(ex));
            _readStatus = errorCode;
        }

        private void SetConnectionWriteStatus(int statusCode)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Write status: {statusCode}", "SetConnectionWriteStatus");
            _writeStatus = statusCode;
            _writeWaiter.Set();
        }

        private void ConnectionCancelled()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Connection was cancelled", "ConnectionCancelled");
            OperationCanceledException ex = new();
            _handshakeException = ex;
            _handshakeState = HandshakeState.Failed;
            DecryptTask?.TrySetException(ex);
            DecryptTask = null;
            _writeStatus = (int)NwOSStatus.SecUserCanceled;
            _readStatus = (int)NwOSStatus.SecUserCanceled;
            _writeWaiter.Set();
            _readWaiter.Set();
        }

        private static void HandleDebugLog(IntPtr logType, IntPtr data)
        {
            switch (logType)
            {
                case 1: if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"framer_output_handler called with message_length: {data}", "Native"); break;
                case 2: if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"calling writeFunc with buffer_length: {data}", "Native"); break;
                case 3: if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "writeFunc completed", "Native"); break;
                case 4: if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "parse output completed", "Native"); break;
                case 10: if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "framer_start called", "Native"); break;
                case 11: if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "setting output handler", "Native"); break;
                case 12: if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "setting stop/cleanup handlers", "Native"); break;
                case 13: if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "returning nw_framer_start_result_ready", "Native"); break;
                case 20: if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "StartTlsHandshake called", "Native"); break;
                case 21: if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"connection state changed to: {data}", "Native"); break;
                case 22: if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "setting queue and starting connection", "Native"); break;
                case 23: if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "connection started", "Native"); break;
                case 30: if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"minTlsProtocol: {data}", "Native"); break;
                case 31: if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"maxTlsProtocol: {data}", "Native"); break;
                case 32: if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"native min TLS version: {data}", "Native"); break;
                case 33: if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"native max TLS version: {data}", "Native"); break;
                default: if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, Marshal.PtrToStringAnsi(data)!, "Native"); break;
            }
        }
    }
}
