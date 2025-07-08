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
        internal static bool IsSwitchEnabled { get; } = AppContextSwitchHelper.GetBooleanConfig(
            "System.Net.Security.UseNetworkFramework",
            "DOTNET_SYSTEM_NET_SECURITY_USENETWORKFRAMEWORK");
        private static readonly Lazy<bool> s_isNetworkFrameworkAvailable = new Lazy<bool>(CheckNetworkFrameworkAvailability);

        // Network Framework handles
        private readonly Interop.SafeNwHandle _connectionHandle;
        private Interop.SafeNwHandle? _framerHandle;
        internal Interop.SafeNwHandle ConnectionHandle => _connectionHandle;

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
        private CancellationTokenSource _shutdownCts = new CancellationTokenSource();

        // Buffers
        private const int InitialBufferSize = 2 * 1024;
        private ArrayBuffer _outputBuffer = new(InitialBufferSize);

        private bool _disposed;
        private bool _clientCertificateRequested;

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
                throw Interop.AppleCrypto.CreateExceptionForOSStatus((int)status);
            }

            [UnmanagedCallersOnly]
            static void CompletionCallback(IntPtr context, long status)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Completing WriteAsync", nameof(WriteAsync));
                GCHandle handle = GCHandle.FromIntPtr(context);
                TaskCompletionSource<long> tcs = (TaskCompletionSource<long>)handle.Target!;

                tcs.SetResult(status);
                handle.Free();
            }
        }

        internal async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (_appReceiveBuffer.ActiveLength == 0)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Internal buffer empty, refilling.");
                int read = await FillAppDataBufferAsync(_appReceiveBuffer.AvailableMemory, cancellationToken).ConfigureAwait(false);

                if (read == 0)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "ReadAsync returning 0 bytes, end of stream reached");
                    return 0; // EOF
                }

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"ReadAsync filled buffer with {read} bytes");

                _appReceiveBuffer.Commit(read);
            }

            // If we have data in the buffer, copy it directly
            int length = Math.Min(_appReceiveBuffer.ActiveLength, buffer.Length);
            _appReceiveBuffer.ActiveSpan.Slice(0, length).CopyTo(buffer.Span);
            _appReceiveBuffer.Discard(length);
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Read {length} bytes");
            return length;
        }

        internal async Task<int> FillAppDataBufferAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            using CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Cancellation requested for {nameof(FillAppDataBufferAsync)}");
                tcs.TrySetCanceled();
            });

            Tuple<TaskCompletionSource<int>, Memory<byte>> state = new(tcs, buffer);
            GCHandle handle = GCHandle.Alloc(state, GCHandleType.Normal);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Waiting for read from connection");
            unsafe
            {
                Interop.NetworkFramework.Tls.ReadFromConnection(_connectionHandle, GCHandle.ToIntPtr(_thisHandle), buffer.Length, GCHandle.ToIntPtr(handle), &CompletionCallback);
            }

            return await tcs.Task.ConfigureAwait(false);

            [UnmanagedCallersOnly]
            static unsafe void CompletionCallback(IntPtr context, long status, byte* buffer, int length)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Completing ConnectionRead, status: {status}, len: {length}", nameof(FillAppDataBufferAsync));
                GCHandle handle = GCHandle.FromIntPtr(context);
                (TaskCompletionSource<int> tcs, Memory<byte> state) = (Tuple<TaskCompletionSource<int>, Memory<byte>>)handle.Target!;

                if (status != 0)
                {
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
                        Interop.NetworkFramework.Tls.CancelConnection(_connectionHandle);
                        // TODO: Wait for connection cancellation through status update with TCS.
                        _disposed = true;
                        _connectionHandle.Dispose();
                        _outputBuffer.Dispose();
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

        [UnmanagedCallersOnly]
        private static IntPtr ChallengeCallback(IntPtr thisHandle)
        {
            SafeDeleteNwContext? nwContext = ResolveThisHandle(thisHandle);
            if (nwContext == null || nwContext._disposed)
            {
                return IntPtr.Zero;
            }

            try
            {
                nwContext.EnsureKeepAlive();

                nwContext._clientCertificateRequested = true;

                if (nwContext._sslAuthenticationOptions.CertificateContext != null)
                {
                    X509Certificate2 cert = nwContext._sslAuthenticationOptions.CertificateContext.TargetCertificate;
                    if (cert.HasPrivateKey)
                    {
                        return cert.Handle;
                    }
                }

                if (nwContext._sslAuthenticationOptions.CertSelectionDelegate != null)
                {
                    X509Certificate2? selectedCert = null;
                    try
                    {
                        // Get the local certificates collection
                        X509CertificateCollection localCertificates = nwContext._sslAuthenticationOptions.ClientCertificates ?? new X509CertificateCollection();

                        // TODO: Get the remote certificate and acceptable issuers from the TLS metadata
                        // This would require accessing sec_protocol_metadata_access_distinguished_names
                        // from the native challenge block and passing that information here.
                        // For now, pass null for remote certificate and empty array for issuers
                        X509Certificate? remoteCertificate = null;
                        string[] acceptableIssuers = Array.Empty<string>();

                        X509Certificate? selected = nwContext._sslAuthenticationOptions.CertSelectionDelegate(
                            nwContext,
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

                    if (selectedCert != null && selectedCert.HasPrivateKey)
                    {
                        if (selectedCert.HasPrivateKey)
                        {
                            return selectedCert.Handle;
                        }
                    }
                }

                return IntPtr.Zero;
            }
            catch (Exception e)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(nwContext, $"ChallengeCallback Failed: {e.Message}");
                }
                return IntPtr.Zero;
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
        }

        private static void HandleDebugLog(SafeDeleteNwContext? nwContext, IntPtr _, IntPtr data)
        {
            if (NetEventSource.Log.IsEnabled())
                NetEventSource.Info(nwContext, Marshal.PtrToStringAnsi(data)!, "Native");
        }
    }
}
