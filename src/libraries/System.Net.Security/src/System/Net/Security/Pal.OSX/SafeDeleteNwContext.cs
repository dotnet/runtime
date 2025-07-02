// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Runtime.InteropServices;
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
        private Task<SecurityStatusPalErrorCode>? _pendingHandshakeTask;
        private Exception? _handshakeException;

        // TaskSourceCompletion objects for decrypt operations
        internal TaskCompletionSource<SecurityStatusPalErrorCode>? DecryptTask { get; private set; }

        // Read/Write Status
        private volatile int _readStatus;
        private volatile int _writeStatus;

        // Buffers
        private const int InitialBufferSize = 2 * 1024;
        private ArrayBuffer _outputBuffer = new(InitialBufferSize);

        private bool _disposed;

        public SafeDeleteNwContext(SslAuthenticationOptions sslAuthenticationOptions) : base(IntPtr.Zero)
        {
            _connectionHandle = Interop.NetworkFramework.Tls.CreateContext(sslAuthenticationOptions.IsServer);
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

        private static ReadOnlySpan<SslProtocols> OrderedSslProtocols =>
        [
#pragma warning disable 0618
            SslProtocols.Ssl2,
            SslProtocols.Ssl3,
#pragma warning restore 0618
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
            SslProtocols.Tls,
            SslProtocols.Tls11,
#pragma warning restore SYSLIB0039
            SslProtocols.Tls12,
            SslProtocols.Tls13
        ];

        private static (SslProtocols, SslProtocols) GetMinMaxProtocols(SslProtocols protocols)
        {
            (int minIndex, int maxIndex) = protocols.ValidateContiguous(OrderedSslProtocols);
            SslProtocols minProtocolId = OrderedSslProtocols[minIndex];
            SslProtocols maxProtocolId = OrderedSslProtocols[maxIndex];

            return (minProtocolId, maxProtocolId);
        }

        private void SetTlsOptions(SslAuthenticationOptions options)
        {
            SslProtocols minProtocol = SslProtocols.Tls12;
            SslProtocols maxProtocol = SslProtocols.Tls13;
            if (options.EnabledSslProtocols != SslProtocols.None)
            {
                (minProtocol, maxProtocol) = GetMinMaxProtocols(options.EnabledSslProtocols);
            }

            Interop.NetworkFramework.Tls.SetTlsOptions(_connectionHandle, GCHandle.ToIntPtr(_thisHandle), options.TargetHost, options.ApplicationProtocols, minProtocol, maxProtocol);
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
                _outputBuffer.EnsureAvailableSpace(data.Length);
                data.CopyTo(_outputBuffer.AvailableSpan);
                _outputBuffer.Commit(data.Length);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Buffered {data.Length} bytes, total output buffer: {_outputBuffer.ActiveLength}", "WriteOutboundWireData");
            }

            // Signal that data is available
            _writeWaiter.Set();
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Signaled _writeWaiter", "WriteOutboundWireData");
        }


        internal unsafe Task WriteInboundWireDataAsync(ReadOnlyMemory<byte> buf)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Called with {buf.Length} bytes", "WriteInboundWireDataAsync");

            if (_framerHandle != null && buf.Length > 0)
            {
                TaskCompletionSource tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                GCHandle handle = GCHandle.Alloc(tcs, GCHandleType.Normal);
                fixed (byte* ptr = &MemoryMarshal.GetReference(buf.Span))
                {
                    Interop.NetworkFramework.Tls.ProcessInputData(_connectionHandle, _framerHandle, ptr, buf.Length, GCHandle.ToIntPtr(handle), &CompletionCallback);

                }

                return tcs.Task;
            }

            return Task.CompletedTask;

            [UnmanagedCallersOnly]
            static void CompletionCallback(IntPtr context)
            {
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

        internal unsafe int Decrypt(Span<byte> buffer)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Called with {buffer.Length} bytes", "Decrypt");
            return -1;
            // if (_framerHandle is null || _framerHandle.IsInvalid)
            // {
            //     // We are shutting down
            //     return -1;
            // }

            // // notify TLS we received EOF
            // if (buffer.Length == 0)
            // {
            //     Interop.NetworkFramework.Tls.ProcessInputData(_connectionHandle, _framerHandle, null, 0);
            //     return 0;
            // }

            // fixed (byte* ptr = buffer)
            // {
            //     Interop.NetworkFramework.Tls.ProcessInputData(_connectionHandle, _framerHandle, ptr, buffer.Length);
            // }
            // return 0;
        }

        internal unsafe void Encrypt(void* buffer, int bufferLength, ref ProtocolToken token)
        {
            _writeWaiter!.Reset();
            Interop.NetworkFramework.Tls.SendToConnection(_connectionHandle, GCHandle.ToIntPtr(_thisHandle), buffer, bufferLength);
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
                                        Interop.AppleCrypto.CreateExceptionForOSStatus(_writeStatus));
            }
        }

        // // returns of available decrypted bytes or -1 if EOF was reached
        // internal int BytesReadyFromConnection
        // {
        //     get
        //     {
        //         lock (_lockObject)
        //         {
        //             if (_inputBuffer.ActiveLength > 0)
        //             {
        //                 return _inputBuffer.ActiveLength;
        //             }

        //             return _readStatus == (int)NwOSStatus.NoErr ? 0 : -1;
        //         }
        //     }
        // }

        internal int BytesReadyForConnection => _outputBuffer.ActiveLength;

        internal void ReadPendingWrites(ref ProtocolToken token)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Called - checking for data", "ReadPendingWrites");

            lock (_writeWaiter)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Output buffer length: {_outputBuffer.ActiveLength}", "ReadPendingWrites");

                if (_outputBuffer.ActiveLength == 0)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "No data available, returning empty token", "ReadPendingWrites");
                    token.Size = 0;
                    token.Payload = null;
                    return;
                }

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Extracting {_outputBuffer.ActiveLength} bytes from output buffer", "ReadPendingWrites");
                token.SetPayload(_outputBuffer.ActiveSpan);
                _outputBuffer.Discard(_outputBuffer.ActiveLength);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Token size: {token.Size}", "ReadPendingWrites");
            }
        }

        public unsafe Task<SecurityStatusPalErrorCode> StartDecrypt()
        {
            if (DecryptTask == null)
            {
                DecryptTask = new TaskCompletionSource<SecurityStatusPalErrorCode>(TaskCreationOptions.RunContinuationsAsynchronously);
                Interop.NetworkFramework.Tls.ReadFromConnection(_connectionHandle, GCHandle.ToIntPtr(_thisHandle));
            }

            return DecryptTask.Task;
        }

        public async ValueTask<SecurityStatusPal> PerformHandshakeAsync()
        {
            await Task.Yield();

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"State: {_handshakeState}", "PerformHandshake");
            ObjectDisposedException.ThrowIf(_disposed, this);

            switch (_handshakeState)
            {
                case HandshakeState.NotStarted:
                    return StartHandshake();

                case HandshakeState.InProgress:
                    // return new SecurityStatusPal(SecurityStatusPalErrorCode.ContinueNeeded);
                    return ContinueHandshake();

                case HandshakeState.WaitingForPeer:
                // return await ProcessPeerDataAsync().ConfigureAwait(false);

                case HandshakeState.Completed:
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Handshake already completed", "PerformHandshake");
                    return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);

                case HandshakeState.Failed:
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, "Handshake failed", "PerformHandshake");
                    return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, _handshakeException);

                default:
                    throw new InvalidOperationException($"Invalid handshake state: {_handshakeState}");
            }
        }

        private SecurityStatusPal StartHandshake()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Starting TLS handshake", "StartHandshake");

            bool add = false;
            this.DangerousAddRef(ref add);

            try
            {
                // Start async handshake but don't wait
                _pendingHandshakeTask = StartHandshakeAsync();
                _handshakeState = HandshakeState.InProgress;

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Handshake started async", "StartHandshake");
                return new SecurityStatusPal(SecurityStatusPalErrorCode.ContinueNeeded);
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"StartHandshake failed: {ex}", "StartHandshake");
                _handshakeException = ex;
                _handshakeState = HandshakeState.Failed;
                return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, ex);
            }
        }

        private SecurityStatusPal ContinueHandshake()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Continuing handshake", "ContinueHandshake");

            // Check if async handshake completed
            if (_pendingHandshakeTask?.IsCompleted == true)
            {
                if (_pendingHandshakeTask.IsFaulted)
                {
                    _handshakeException = _pendingHandshakeTask.Exception?.GetBaseException();
                    _handshakeState = HandshakeState.Failed;
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"Handshake task faulted: {_handshakeException}", "ContinueHandshake");
                    return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, _handshakeException);
                }

                var result = _pendingHandshakeTask.Result;
                _pendingHandshakeTask = null;

                if (result == SecurityStatusPalErrorCode.OK)
                {
                    _handshakeState = HandshakeState.Completed;
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Handshake completed successfully", "ContinueHandshake");
                    return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
                }
                else if (result == SecurityStatusPalErrorCode.ContinueNeeded)
                {
                    _handshakeState = HandshakeState.WaitingForPeer;
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Handshake needs peer data", "ContinueHandshake");
                    return new SecurityStatusPal(SecurityStatusPalErrorCode.ContinueNeeded);
                }
                else
                {
                    _handshakeState = HandshakeState.Failed;
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"Handshake failed with status: {result}", "ContinueHandshake");
                    return new SecurityStatusPal(result);
                }
            }

            // Still in progress
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Handshake still in progress", "ContinueHandshake");
            return new SecurityStatusPal(SecurityStatusPalErrorCode.ContinueNeeded);
        }

        private async Task<SecurityStatusPalErrorCode> StartHandshakeAsync()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Starting async handshake operation", "StartHandshakeAsync");

            try
            {
                Interop.NetworkFramework.Tls.StartTlsHandshake(_connectionHandle, GCHandle.ToIntPtr(_thisHandle));

                // Wait a short time for initial handshake progress
                await Task.Delay(1).ConfigureAwait(false);

                return SecurityStatusPalErrorCode.ContinueNeeded;
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"Async handshake failed: {ex}", "StartHandshakeAsync");
                throw;
            }
        }


        [UnmanagedCallersOnly]
        private static unsafe void StatusUpdateCallback(IntPtr thisHandle, NetworkFrameworkStatusUpdates statusUpdate, IntPtr data, IntPtr data2)
        {
            if (NetEventSource.Log.IsEnabled() && statusUpdate != NetworkFrameworkStatusUpdates.DebugLog) NetEventSource.Info(null, $"Received status update: {statusUpdate}", "StatusUpdateCallback");
            SafeDeleteNwContext? nwContext = ResolveThisHandle(thisHandle);
            if (nwContext == null)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, "Failed to resolve context handle", "StatusUpdateCallback");
                return;
            }

            try
            {
                nwContext.EnsureKeepAlive();
                switch (statusUpdate)
                {
                    case NetworkFrameworkStatusUpdates.FramerStart:
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(nwContext, "FramerStart", "StatusUpdateCallback");
                        nwContext.FramerStartCallback(new SafeNwHandle(data, true));
                        break;
                    case NetworkFrameworkStatusUpdates.FramerStop:
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(nwContext, "FramerStop", "StatusUpdateCallback");
                        nwContext.FramerStopCallback();
                        break;
                    case NetworkFrameworkStatusUpdates.HandshakeFinished:
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(nwContext, "HandshakeFinished", "StatusUpdateCallback");
                        nwContext.HandshakeFinished();
                        break;
                    case NetworkFrameworkStatusUpdates.HandshakeFailed:
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(nwContext, $"HandshakeFailed - errorCode: {data.ToInt32()}", "StatusUpdateCallback");
                        nwContext.HandshakeFailed(data.ToInt32());
                        break;
                    case NetworkFrameworkStatusUpdates.ConnectionReadFinished:
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(nwContext, $"ConnectionReadFinished - dataLength: {data.ToInt32()}", "StatusUpdateCallback");
                        nwContext.ConnectionReadFinished(new ReadOnlySpan<byte>((void*)data2, data.ToInt32()));
                        break;
                    case NetworkFrameworkStatusUpdates.ConnectionWriteFinished:
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(nwContext, "ConnectionWriteFinished", "StatusUpdateCallback");
                        nwContext.SetConnectionWriteStatus(data.ToInt32());
                        break;
                    case NetworkFrameworkStatusUpdates.ConnectionWriteFailed:
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(nwContext, $"ConnectionWriteFailed - errorCode: {data.ToInt32()}", "StatusUpdateCallback");
                        nwContext.SetConnectionWriteStatus(data.ToInt32());
                        break;
                    case NetworkFrameworkStatusUpdates.ConnectionCancelled:
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(nwContext, "ConnectionCancelled", "StatusUpdateCallback");
                        nwContext.ConnectionCancelled();
                        break;
                    case NetworkFrameworkStatusUpdates.DebugLog:
                        HandleDebugLog(data.ToInt32(), data2.ToInt32());
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
            _pendingHandshakeTask = null;
        }

        private void HandshakeFailed(int errorCode)
        {
            Exception ex = Interop.AppleCrypto.CreateExceptionForOSStatus(errorCode);
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"TLS handshake failed with error code: {errorCode} ({ex.Message})", "HandshakeFailed");
            _handshakeException = ex;
            _handshakeState = HandshakeState.Failed;
            _pendingHandshakeTask = null;
            _readStatus = errorCode;
        }

        private void ConnectionReadFinished(ReadOnlySpan<byte> data)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Received {data.Length} bytes from connection", "ConnectionReadFinished");
            TaskCompletionSource<SecurityStatusPalErrorCode>? completion;
            lock (_lockObject)
            {
                if (data.Length > 0)
                {
                    WriteInboundWireDataAsync(data.ToArray()).AsTask().GetAwaiter().GetResult();
                }
                else if (_readStatus == (int)NwOSStatus.NoErr)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "EOF reached", "ConnectionReadFinished");
                    _readStatus = (int)NwOSStatus.EOFErr;
                }

                completion = DecryptTask;
                DecryptTask = null;
            }

            completion?.TrySetResult(SecurityStatusPalErrorCode.OK);
            _readWaiter.Set();
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
            _pendingHandshakeTask = null;
            DecryptTask?.TrySetException(ex);
            DecryptTask = null;
            _writeStatus = (int)NwOSStatus.SecUserCanceled;
            _readStatus = (int)NwOSStatus.SecUserCanceled;
            _writeWaiter.Set();
            _readWaiter.Set();
        }

        private static void HandleDebugLog(int logType, int data)
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
                default: if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, $"Unknown debug log type: {logType}, data: {data}", "Native"); break;
            }
        }
    }
}
