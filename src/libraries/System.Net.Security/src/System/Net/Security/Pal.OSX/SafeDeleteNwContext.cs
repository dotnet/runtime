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
        internal static bool IsSwitchEnabled => LocalAppContextSwitches.UseNetworkFramework;
        private static readonly Lazy<bool> s_isNetworkFrameworkAvailable = new Lazy<bool>(CheckNetworkFrameworkAvailability);

        private const int InitialReceiveBufferSize = 2 * 1024;

        // Upper bound on a single TLS record, used while the handshake is read one record at a
        // time. RFC 5246 requires rejecting a TLSCiphertext longer than 2^14 + 2048.
        private const int MaxTlsRecordSize = (1 << 14) + 2048 + TlsFrameHelper.HeaderSize;

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

        // Provides backreference from native code. This GC handle is expected
        // to be always valid and is only freed once we are sure there will be
        // no more callbacks from the native code
        private readonly GCHandle _thisHandle;

        internal IntPtr StateHandle => _thisHandle.IsAllocated ? GCHandle.ToIntPtr(_thisHandle) : IntPtr.Zero;

        private TaskCompletionSource<Exception?> _handshakeCompletionSource = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Cancelled as soon as the handshake completes, so a transport read the loop parked while
        // still in the handshake phase is abandoned before it can consume application data.
        private readonly CancellationTokenSource _handshakeReadCts = new CancellationTokenSource();

        // Completed once the framer is available. The transport read loop starts as soon as the
        // connection does, so inbound data can arrive before Network.framework reports FramerStart.
        private readonly TaskCompletionSource _framerReadyTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Completed when the application performs its first read or write after the handshake.
        // Until then the transport is left untouched, see the read loop in HandshakeAsync.
        private readonly TaskCompletionSource _appIoStartedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        private Task? _transportReadTask;
        private ResettableValueTaskSource _transportReadTcs = new ResettableValueTaskSource()
        {
            CancellationAction = target =>
            {
                if (target is SafeDeleteNwContext nwContext)
                {
                    nwContext._transportReadTcs.TrySetException(new OperationCanceledException());
                }
            }
        };
        private ArrayBuffer _appReceiveBuffer = new(InitialReceiveBufferSize);
        private ResettableValueTaskSource _appReceiveBufferTcs = new ResettableValueTaskSource();
        private Task? _pendingAppReceiveBufferFillTask;
        private readonly TaskCompletionSource _connectionClosedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        private CancellationTokenSource _shutdownCts = new CancellationTokenSource();

        private bool _disposed;
        private int _challengeCallbackCompleted;  // 0 = not called, 1 = called
        private IntPtr _selectedClientCertificate;  // Cached result from challenge callback
        private volatile bool _transportEofWithinTlsRecord;
        private ResettableValueTaskSource _appWriteTcs = new ResettableValueTaskSource()
        {
            CancellationAction = target =>
            {
                if (target is SafeDeleteNwContext nwContext)
                {
                    nwContext._appWriteTcs.TrySetException(new OperationCanceledException());
                }
            }
        };

        private TaskCompletionSource? _currentWriteCompletionSource;

        // Completed once an application send has observed every native callback it triggered
        // (the nw_connection_send completion and the framer output write). Both resolve the
        // GCHandle, so Dispose must not free it while one is outstanding.
        private TaskCompletionSource? _pendingSendCompletion;

        public SafeDeleteNwContext(SslStream stream) : base(IntPtr.Zero)
        {
            _sslStream = stream;
            _thisHandle = GCHandle.Alloc(this, GCHandleType.Normal);
            ConnectionHandle = CreateConnectionHandle(SslAuthenticationOptions, _thisHandle);

            if (ConnectionHandle.IsInvalid)
            {
                throw new Exception("Failed to create Network Framework connection"); // TODO: Make this string resource
            }
        }

        public static bool IsNetworkFrameworkAvailable => IsSwitchEnabled && s_isNetworkFrameworkAvailable.Value;

        internal bool ClientCertificateRequested => _challengeCallbackCompleted == 1 && _selectedClientCertificate != IntPtr.Zero;
        internal bool IsServer => SslAuthenticationOptions.IsServer;

        internal void SetServerTargetHost(string targetHost)
        {
            Debug.Assert(IsServer);
            SslAuthenticationOptions.TargetHost = targetHost;
        }

        internal async Task<Exception?> HandshakeAsync(CancellationToken cancellationToken)
        {
            Interop.NetworkFramework.Tls.NwConnectionStart(ConnectionHandle, StateHandle);

            using CancellationTokenRegistration registration = cancellationToken.UnsafeRegister(static (state, token) =>
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "Handshake cancellation requested");
                ((TaskCompletionSource<Exception?>)state!).TrySetCanceled(token);
            }, _handshakeCompletionSource);

            _transportReadTask = Task.Run(async () =>
            {
                try
                {
                    byte[] buffer = ArrayPool<byte>.Shared.Rent(MaxTlsRecordSize);
                    try
                    {
                        Memory<byte> readBuffer = new Memory<byte>(buffer);
                        bool handshakePhase = true;

                        using CancellationTokenSource handshakeReadCts = CancellationTokenSource.CreateLinkedTokenSource(
                            _shutdownCts.Token, _handshakeReadCts.Token);
                        TlsRecordFramingTracker recordTracker = default;

                        while (!_shutdownCts.IsCancellationRequested)
                        {
                            int bytesRead;

                            if (handshakePhase)
                            {
                                // While the handshake is in flight, consume exactly one TLS record per
                                // iteration. The application may re-frame the inner stream as soon as the
                                // handshake completes - SqlClient's TLS-over-TDS stream leaves TDS
                                // encapsulation only once AuthenticateAsClient returns - so anything read
                                // past the final handshake record would be interpreted with the wrong
                                // framing. Network.framework never asks for input, so this read has to be
                                // speculative; it is cancelled the moment the handshake completes, which
                                // abandons it before it can consume a re-framed record.
                                try
                                {
                                    bytesRead = await ReadSingleTlsRecordAsync(readBuffer, handshakeReadCts.Token).ConfigureAwait(false);
                                }
                                catch (OperationCanceledException) when (!_shutdownCts.IsCancellationRequested)
                                {
                                    handshakePhase = false;
                                    await _appIoStartedTcs.Task.WaitAsync(_shutdownCts.Token).ConfigureAwait(false);
                                    continue;
                                }
                            }
                            else
                            {
                                // Once the application drives I/O, bulk reads are safe again and keep NW
                                // supplied with data it never explicitly asks for.
                                bytesRead = await TransportStream.ReadAsync(readBuffer, _shutdownCts.Token).ConfigureAwait(false);
                            }

                            if (bytesRead > 0)
                            {
                                if (!handshakePhase)
                                {
                                    recordTracker.Track(readBuffer.Span.Slice(0, bytesRead));
                                }

                                // Process the read data
                                await WriteInboundWireDataAsync(readBuffer.Slice(0, bytesRead)).ConfigureAwait(false);

                                if (handshakePhase && _handshakeCompletionSource.Task.IsCompleted)
                                {
                                    if (_handshakeCompletionSource.Task is { IsCompletedSuccessfully: true, Result: null })
                                    {
                                        // The record just delivered completed the handshake. Leave the
                                        // transport alone until the application starts its own I/O, by
                                        // which point it has finished re-framing the stream.
                                        handshakePhase = false;
                                        await _appIoStartedTcs.Task.WaitAsync(_shutdownCts.Token).ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        // The handshake failed or was cancelled, so no application I/O
                                        // will ever start. Stop pumping instead of parking forever.
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                _transportReadTcs.TrySetResult(final: true);
                                _transportEofWithinTlsRecord = recordTracker.IsMidRecord;
                                if (_transportEofWithinTlsRecord)
                                {
                                    _appReceiveBufferTcs.TrySetException(ExceptionDispatchInfo.SetCurrentStackTrace(new IOException(SR.net_io_eof)));
                                }
                                Interop.NetworkFramework.Tls.NwConnectionCancel(ConnectionHandle);
                                break;
                            }
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Handle cancellation gracefully
                }
                catch (Exception ex)
                {
                    // Propagate transport stream exceptions to the handshake / pending write.
                    // Swallow on this task so a fire-and-forget Dispose can't surface an
                    // UnobservedTaskException; the exception is observed through the TCSes.
                    _handshakeCompletionSource.TrySetException(ex);
                    _currentWriteCompletionSource?.TrySetException(ex);
                    _appReceiveBufferTcs.TrySetException(ex);
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"Transport read loop terminated: {ex}");
                }
            }, cancellationToken);

            return await _handshakeCompletionSource.Task.ConfigureAwait(false);
        }

        internal async Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // The application is driving I/O now, so the transport read loop may resume.
            _appIoStartedTcs.TrySetResult();

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"App sending {buffer.Length} bytes");

            // Published before the native send is issued so a concurrent Dispose can see that
            // callbacks resolving the GCHandle are still outstanding.
            TaskCompletionSource sendCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingSendCompletion = sendCompletion;

            try
            {
                TaskCompletionSource transportWriteCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _currentWriteCompletionSource = transportWriteCompletion;

                bool success = _appWriteTcs.TryGetValueTask(out ValueTask valueTask, this, CancellationToken.None);
                Debug.Assert(success, "Concurrent WriteAsync detected");

                using MemoryHandle memoryHandle = buffer.Pin();
                unsafe
                {
                    Interop.NetworkFramework.Tls.NwConnectionSend(ConnectionHandle, StateHandle, memoryHandle.Pointer, buffer.Length, &CompletionCallback);
                }
                try
                {
                    await valueTask.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _currentWriteCompletionSource = null;
                    transportWriteCompletion.TrySetException(ex);
                }

                // Wait for the transport write to complete
                await transportWriteCompletion.Task.ConfigureAwait(false);
            }
            finally
            {
                // Never faulted: this only reports that the native callbacks are done with the
                // handle, so Dispose can wait on it without observing a write failure.
                _pendingSendCompletion = null;
                sendCompletion.TrySetResult();
            }

            [UnmanagedCallersOnly]
            static unsafe void CompletionCallback(IntPtr context, Interop.NetworkFramework.NetworkFrameworkError* error)
            {
                SafeDeleteNwContext thisContext = ResolveThisHandle(context);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(thisContext, $"Completing WriteAsync", nameof(WriteAsync));

                if (error != null)
                {
                    thisContext._appWriteTcs.TrySetException(Interop.NetworkFramework.CreateExceptionForNetworkFrameworkError(in *error));
                }
                else
                {
                    thisContext._appWriteTcs.TrySetResult();
                }
            }
        }

        internal ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

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
                // Create a linked token that respects both the user's cancellation token and our shutdown token
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdownCts.Token);

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Internal buffer empty, refilling.");

                // Since the native nw_connection_receive is asynchronous and
                // not cancellable, we save reference to the pending task. In
                // case of cancellation, we cancel only waiting on the task, and
                // on the next ReadAsync call, we don't issue another native
                // nw_connection_receive call, but rather wait for the pending
                // task to complete.
                _pendingAppReceiveBufferFillTask ??= FillAppDataBufferAsync();
                try
                {
                    // Wait for the pending task to complete, which will fill the buffer
                    await _pendingAppReceiveBufferFillTask.WaitAsync(linkedCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Throw with correct cancellation token if cancellation was
                    // requested by user
                    cancellationToken.ThrowIfCancellationRequested();

                    // otherwise we are tearing down the connection, simulate EOS
                    Debug.Assert(_shutdownCts.IsCancellationRequested, "Expected shutdown cancellation token to be triggered");

                    ObjectDisposedException.ThrowIf(_disposed, _sslStream);
                }
                // other exception types are expected to be fatal and it is okay to not
                // clear the pending task and rethrow the same exception

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
            // The application is driving I/O now, so the transport read loop may resume.
            _appIoStartedTcs.TrySetResult();

            bool success = _appReceiveBufferTcs.TryGetValueTask(out ValueTask valueTask, this, CancellationToken.None);
            Debug.Assert(success, "Concurrent FillAppDataBufferAsync detected");

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Waiting for read from connection");
            unsafe
            {
                Interop.NetworkFramework.Tls.NwConnectionReceive(ConnectionHandle, StateHandle, 16 * 1024, &CompletionCallback);
            }

            return _pendingAppReceiveBufferFillTask = valueTask.AsTask();

            [UnmanagedCallersOnly]
            static unsafe void CompletionCallback(IntPtr context, Interop.NetworkFramework.NetworkFrameworkError* error, byte* data, int length)
            {
                SafeDeleteNwContext thisContext = ResolveThisHandle(context);
                Debug.Assert(thisContext != null, "Expected thisContext to be non-null");

                if (NetEventSource.Log.IsEnabled())
                    NetEventSource.Info(thisContext, $"Completing ConnectionRead, status: {(error != null ? error->ErrorCode : 0)}, len: {length}", nameof(FillAppDataBufferAsync));

                ref ArrayBuffer buffer = ref thisContext._appReceiveBuffer;

                try
                {
                    if (error != null && error->ErrorCode != 0)
                    {
                        if (error->ErrorDomain == (int)Interop.NetworkFramework.NetworkFrameworkErrorDomain.POSIX &&
                            error->ErrorCode == (int)Interop.NetworkFramework.NWErrorDomainPOSIX.OperationCanceled)
                        {
                            if (thisContext._transportEofWithinTlsRecord)
                            {
                                thisContext._appReceiveBufferTcs.TrySetException(ExceptionDispatchInfo.SetCurrentStackTrace(new IOException(SR.net_io_eof)));
                                return;
                            }

                            // We cancelled the connection, so this is expected as pending read will be cancelled.
                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(thisContext, "Connection read cancelled, no data to process");
                            thisContext._appReceiveBufferTcs.TrySetResult();
                            return;
                        }
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
                catch (Exception ex)
                {
                    // This runs on a Network.framework thread, so an escaping exception would
                    // cross back into native code and terminate the process. Report it through
                    // the pending read instead.
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(thisContext, $"Failed to complete connection read: {ex}");
                    thisContext._appReceiveBufferTcs.TrySetException(ExceptionDispatchInfo.SetCurrentStackTrace(ex));
                }
            }
        }

        internal void Shutdown()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Shutting down Network Framework context");
            _shutdownCts.Cancel();
            Interop.NetworkFramework.Tls.NwConnectionCancel(ConnectionHandle);
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

        private static SafeNwHandle CreateConnectionHandle(SslAuthenticationOptions options, GCHandle thisHandle)
        {
            int alpnLength = GetAlpnProtocolListSerializedLength(options.ApplicationProtocols);

            SslProtocols minProtocol = SslProtocols.None;
            SslProtocols maxProtocol = SslProtocols.None;

            if (options.EnabledSslProtocols != SslProtocols.None)
            {
                (minProtocol, maxProtocol) = GetMinMaxProtocols(options.EnabledSslProtocols);
            }

            byte[]? alpnBuffer = null;
            try
            {
                const int StackAllocThreshold = 256;
                Span<byte> alpn = alpnLength == 0
                    ? Span<byte>.Empty
                    : alpnLength <= StackAllocThreshold
                        ? stackalloc byte[StackAllocThreshold]
                        : (alpnBuffer = ArrayPool<byte>.Shared.Rent(alpnLength));

                if (alpnLength > 0)
                {
                    SerializeAlpnProtocolList(options.ApplicationProtocols!, alpn.Slice(0, alpnLength));
                }

                Span<uint> ciphers = options.CipherSuitesPolicy is null
                   ? Span<uint>.Empty
                   : options.CipherSuitesPolicy.Pal.TlsCipherSuites;

                string idnHost = TargetHostNameHelper.NormalizeHostName(options.TargetHost);

                // For server-side TLS, hand the SecIdentityRef of the server certificate down to the
                // native layer. On macOS, X509Certificate2.Handle returns the SecIdentityRef when the
                // certificate has an associated private key.
                IntPtr serverIdentity = options.IsServer
                    ? options.CertificateContext?.TargetCertificate.Handle ?? IntPtr.Zero
                    : IntPtr.Zero;

                unsafe
                {
                    fixed (byte* alpnPtr = alpn)
                    fixed (uint* ciphersPtr = ciphers)
                    {
                        return Interop.NetworkFramework.Tls.NwConnectionCreate(options.IsServer, GCHandle.ToIntPtr(thisHandle), idnHost, alpnPtr, alpnLength, minProtocol, maxProtocol, ciphersPtr, ciphers.Length, serverIdentity, options.IsServer && options.RemoteCertRequired);
                    }
                }
            }
            finally
            {
                if (alpnBuffer != null)
                {
                    ArrayPool<byte>.Shared.Return(alpnBuffer);
                }
            }

            //
            // Native API accepts only a single ALPN protocol at a time
            // (null-terminated string). We serialize all used app protocols
            // into a single buffer in the format <len><protocol><0>
            //

            static int GetAlpnProtocolListSerializedLength(List<SslApplicationProtocol>? applicationProtocols)
            {
                if (applicationProtocols is null)
                {
                    return 0;
                }

                int protocolSize = 0;
                int wireSize = 0;

                foreach (SslApplicationProtocol protocol in applicationProtocols)
                {
                    protocolSize += protocol.Protocol.Length + 2;

                    wireSize += protocol.Protocol.Length + 1;
                    if (wireSize > ushort.MaxValue)
                    {
                        throw new ArgumentException(SR.net_ssl_app_protocols_invalid, nameof(applicationProtocols));
                    }
                }

                return protocolSize;
            }

            static void SerializeAlpnProtocolList(List<SslApplicationProtocol> applicationProtocols, Span<byte> buffer)
            {
                Debug.Assert(GetAlpnProtocolListSerializedLength(applicationProtocols) == buffer.Length);

                int offset = 0;

                foreach (SslApplicationProtocol protocol in applicationProtocols)
                {
                    buffer[offset] = (byte)protocol.Protocol.Length; // preffix len
                    protocol.Protocol.Span.CopyTo(buffer.Slice(offset + 1)); // ALPN
                    buffer[offset + protocol.Protocol.Length + 1] = 0; // null-terminator

                    offset += protocol.Protocol.Length + 2;
                }
            }

            static (SslProtocols, SslProtocols) GetMinMaxProtocols(SslProtocols protocols)
            {
                ReadOnlySpan<SslProtocols> orderedProtocols = [
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

                (int minIndex, int maxIndex) = protocols.ValidateContiguous(orderedProtocols);
                SslProtocols minProtocolId = orderedProtocols[minIndex];
                SslProtocols maxProtocolId = orderedProtocols[maxIndex];

                return (minProtocolId, maxProtocolId);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                Shutdown();

                // Bounded wait: the task usually exits within a few ms after Shutdown()
                // (NwConnectionCancel unblocks any framer await). If the loop is parked
                // in TransportStream.ReadAsync on a stream that ignores cancellation,
                // it will unwind once the inner stream is closed (base.Dispose below
                // when leaveInnerStreamOpen=false, or by the caller). The task swallows
                // all exceptions internally so it cannot raise UnobservedTaskException.
                bool transportCompleted = true;
                if (_transportReadTask is Task transportTask)
                {
                    try
                    {
                        transportCompleted = transportTask.Wait(TimeSpan.FromMilliseconds(250));
                    }
                    catch
                    {
                        transportCompleted = transportTask.IsCompleted;
                    }
                }

                // Wait for any pending app receive tasks so that we may safely dispose the app receive buffer.
                if (_pendingAppReceiveBufferFillTask is Task t)
                {
                    t.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing).GetAwaiter().GetResult();
                }

                // wait for callback signalling connection has been truly closed.
                _connectionClosedTcs.Task.GetAwaiter().GetResult();

                // Only release the receive buffer once the connection reports that it is fully
                // cancelled. A native receive completion writes straight into this buffer and can
                // still be in flight until then, even when no managed read is outstanding.
                _appReceiveBuffer.Dispose();

                // Complete all pending operations with ObjectDisposedException
                var disposedException = new ObjectDisposedException(nameof(SafeDeleteNwContext));

                _appReceiveBufferTcs.TrySetException(disposedException);
                _handshakeCompletionSource.TrySetException(disposedException);

                // Complete any pending writes with disposed exception
                TaskCompletionSource? writeCompletion = _currentWriteCompletionSource;
                if (writeCompletion != null)
                {
                    _currentWriteCompletionSource = null;
                    writeCompletion.TrySetException(new ObjectDisposedException(nameof(SafeDeleteNwContext)));
                }

                ConnectionHandle?.Dispose();
                _framerHandle?.Dispose();
                _peerCertChainHandle?.Dispose();
                _handshakeReadCts?.Dispose();
                _shutdownCts?.Dispose();

                // The GCHandle is the resolution target for native callbacks (framer completion,
                // status updates, send completions). Only free it once every native call that
                // resolves it has been observed. The send wait deliberately happens here, after
                // the pending write above was faulted: an application write parked on that source
                // would otherwise never release, and Dispose would wait on itself.
                Task? pendingSend = _pendingSendCompletion?.Task;
                bool sendCompleted = true;
                if (pendingSend is not null)
                {
                    try
                    {
                        sendCompleted = pendingSend.Wait(TimeSpan.FromMilliseconds(250));
                    }
                    catch
                    {
                        sendCompleted = pendingSend.IsCompleted;
                    }
                }

                // If either did not finish in the bounded waits, defer the free via a continuation
                // (same pattern used by SocketsHttpHandler for orphaned tasks).
                if (transportCompleted && sendCompleted)
                {
                    if (_thisHandle.IsAllocated)
                    {
                        _thisHandle.Free();
                    }
                }
                else
                {
                    GCHandle handleToFree = _thisHandle;
                    Task drain = Task.WhenAll(
                        _transportReadTask ?? Task.CompletedTask,
                        pendingSend ?? Task.CompletedTask);

                    drain.ContinueWith(static (completed, state) =>
                    {
                        _ = completed.Exception;
                        GCHandle h = (GCHandle)state!;
                        if (h.IsAllocated)
                        {
                            h.Free();
                        }
                    }, handleToFree, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }
            }
            base.Dispose(disposing);
        }

        private static SafeDeleteNwContext ResolveThisHandle(IntPtr thisHandle)
        {
            GCHandle handle = GCHandle.FromIntPtr(thisHandle);
            return (SafeDeleteNwContext)handle.Target!;
        }

        [UnmanagedCallersOnly]
        private static unsafe void WriteOutboundWireData(IntPtr thisHandle, byte* data, ulong dataLength)
        {
            SafeDeleteNwContext? nwContext = null;
            try
            {
                nwContext = ResolveThisHandle(thisHandle);
                Debug.Assert(dataLength <= int.MaxValue);

                nwContext.WriteOutboundWireData(new ReadOnlySpan<byte>(data, (int)dataLength));
            }
            catch (Exception e)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(nwContext, $"WriteOutboundWireData Failed: {e.Message}");
                }

                // Complete the write operation with the exception
                TaskCompletionSource? writeCompletion = nwContext?._currentWriteCompletionSource;
                if (writeCompletion != null)
                {
                    nwContext?._currentWriteCompletionSource = null;
                    writeCompletion.TrySetException(e);
                }

                // The failing write may be part of the handshake, where no application write is
                // pending to observe it. Surface it there too, otherwise the peer never receives
                // this flight and the handshake waits forever. The exception already carries its
                // original stack trace, so it is propagated as-is.
                nwContext?._handshakeCompletionSource.TrySetResult(e);
                nwContext?._appReceiveBufferTcs.TrySetException(e);
            }
            finally
            {
                nwContext?._currentWriteCompletionSource?.TrySetResult();
                nwContext?._currentWriteCompletionSource = null;
            }
        }

        [UnmanagedCallersOnly]
        private static IntPtr ChallengeCallback(IntPtr thisHandle, IntPtr acceptableIssuersHandle)
        {
            try
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
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(null, $"ChallengeCallback exception: {ex}");
                }
                return IntPtr.Zero;
            }
        }

        // Reads exactly one TLS record from the transport without consuming a single byte beyond
        // it, so the caller can stop cleanly at the end of the handshake.
        private async ValueTask<int> ReadSingleTlsRecordAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            int read = await TransportStream.ReadAtLeastAsync(
                buffer.Slice(0, TlsFrameHelper.HeaderSize), TlsFrameHelper.HeaderSize, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);
            if (read < TlsFrameHelper.HeaderSize)
            {
                return 0;
            }

            TlsFrameHeader header = default;
            if (!TlsFrameHelper.TryGetFrameHeader(buffer.Span.Slice(0, read), ref header) ||
                header.Length < TlsFrameHelper.HeaderSize ||
                header.Length > buffer.Length)
            {
                throw new AuthenticationException(SR.net_frame_read_size);
            }

            int remaining = header.Length - read;
            if (remaining > 0)
            {
                int body = await TransportStream.ReadAtLeastAsync(
                    buffer.Slice(read, remaining), remaining, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);
                if (body < remaining)
                {
                    // Truncated record, surface it to the caller as a transport EOF.
                    return 0;
                }

                read += body;
            }

            return read;
        }

        private void WriteOutboundWireData(ReadOnlySpan<byte> data)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Sending {data.Length} bytes");

            // This runs on a Network.framework queue, not on the caller's thread, so there is no
            // synchronous contract to honour towards the transport. Drive it through the async API:
            // streams that reject synchronous operations would otherwise fail the handshake.
            byte[] buffer = ArrayPool<byte>.Shared.Rent(data.Length);
            try
            {
                data.CopyTo(buffer);
                TransportStream.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, data.Length))
                    .AsTask().GetAwaiter().GetResult();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private async Task WriteInboundWireDataAsync(ReadOnlyMemory<byte> buf)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Receiving {buf.Length} bytes");

            if (buf.Length == 0)
            {
                return;
            }

            if (_framerHandle == null)
            {
                // The transport read loop starts with the connection, but the framer only becomes
                // available when Network.framework invokes the start handler. A peer's first flight
                // can arrive before that, in particular over an in-memory transport where the write
                // is visible immediately. Wait for the framer instead of discarding those bytes:
                // they are never retransmitted, so dropping them stalls the handshake permanently.
                await _framerReadyTcs.Task.WaitAsync(_shutdownCts.Token).ConfigureAwait(false);

                if (_framerHandle == null)
                {
                    return;
                }
            }

            // the data needs to be pinned until the callback fires
            using MemoryHandle memoryHandle = buf.Pin();

            bool success = _transportReadTcs.TryGetValueTask(out ValueTask valueTask, this, CancellationToken.None);
            Debug.Assert(success, "Concurrent WriteInboundWireDataAsync detected");

            unsafe
            {
                int status = Interop.NetworkFramework.Tls.NwFramerDeliverInput(_framerHandle, StateHandle, (byte*)memoryHandle.Pointer, buf.Length, &CompletionCallback);
                if (status != 0)
                {
                    // The native side does not invoke the completion callback when it fails to
                    // create the framer message, which happens when the connection is failing or
                    // being cancelled underneath us. Nothing else will ever complete this source,
                    // so fault it here rather than awaiting forever.
                    _transportReadTcs.TrySetException(ExceptionDispatchInfo.SetCurrentStackTrace(
                        new IOException(SR.net_io_eof)));
                }
            }

            await valueTask.ConfigureAwait(false);

            [UnmanagedCallersOnly]
            static unsafe void CompletionCallback(IntPtr context, Interop.NetworkFramework.NetworkFrameworkError* error)
            {
                Debug.Assert(error == null || error->ErrorCode == 0, $"CompletionCallback called with error {(error != null ? error->ErrorCode : 0)}");

                SafeDeleteNwContext thisContext = ResolveThisHandle(context);
                Debug.Assert(thisContext != null, "Expected thisContext to be non-null");

                thisContext._transportReadTcs.TrySetResult();
            }
        }

        // Tracks encrypted TLS record boundaries across the transport reads so a transport EOF
        // landing mid-record can be told apart from one on a record boundary. Only the two length
        // bytes of the 5-byte header are retained, so no buffer is needed to straddle reads.
        private struct TlsRecordFramingTracker
        {
            private int _headerBytesSeen;
            private int _pendingRecordLength;
            private int _recordBytesRemaining;

            public readonly bool IsMidRecord => _headerBytesSeen != 0 || _recordBytesRemaining != 0;

            public void Track(ReadOnlySpan<byte> data)
            {
                while (!data.IsEmpty)
                {
                    if (_recordBytesRemaining > 0)
                    {
                        int consumed = Math.Min(_recordBytesRemaining, data.Length);
                        _recordBytesRemaining -= consumed;
                        data = data.Slice(consumed);
                        continue;
                    }

                    byte value = data[0];
                    data = data.Slice(1);

                    // TLSCiphertext header: type(1) + version(2) + length(2).
                    if (_headerBytesSeen == 3)
                    {
                        _pendingRecordLength = value << 8;
                    }
                    else if (_headerBytesSeen == 4)
                    {
                        _pendingRecordLength |= value;
                    }

                    if (++_headerBytesSeen == TlsFrameHelper.HeaderSize)
                    {
                        _recordBytesRemaining = _pendingRecordLength;
                        _pendingRecordLength = 0;
                        _headerBytesSeen = 0;
                    }
                }
            }
        }

        public override bool IsInvalid => ConnectionHandle is null || ConnectionHandle.IsInvalid || (_framerHandle?.IsInvalid ?? true);

        [UnmanagedCallersOnly]
        private static unsafe void StatusUpdateCallback(IntPtr thisHandle, NetworkFrameworkStatusUpdates statusUpdate, IntPtr data, IntPtr data2, Interop.NetworkFramework.NetworkFrameworkError* error)
        {
            try
            {
                SafeDeleteNwContext? nwContext = null;

                if (thisHandle != IntPtr.Zero)
                {
                    nwContext = ResolveThisHandle(thisHandle);
                }

                if (nwContext == null)
                {
                    Debug.Assert(statusUpdate == NetworkFrameworkStatusUpdates.DebugLog,
                        "StatusUpdateCallback called with null thisHandle, but not a DebugLog update");
                }

                if (NetEventSource.Log.IsEnabled() && statusUpdate != NetworkFrameworkStatusUpdates.DebugLog)
                    NetEventSource.Info(nwContext, $"Received status update: {statusUpdate}");

                switch (statusUpdate)
                {
                    case NetworkFrameworkStatusUpdates.FramerStart:
                        nwContext!.FramerStartCallback(new SafeNwHandle(Interop.NetworkFramework.Retain(data), true));
                        break;
                    case NetworkFrameworkStatusUpdates.HandshakeFinished:
                        nwContext!.HandshakeFinished();
                        break;
                    case NetworkFrameworkStatusUpdates.ConnectionFailed:
                        Debug.Assert(error != null, "ConnectionFailed should have an error");
                        nwContext!.ConnectionFailed(in *error);
                        break;
                    case NetworkFrameworkStatusUpdates.ConnectionCancelled:
                        nwContext!.ConnectionClosed();
                        break;
                    case NetworkFrameworkStatusUpdates.CertificateAvailable:
                        global::System.Runtime.InteropServices.Marshalling.SafeHandleMarshaller<global::Microsoft.Win32.SafeHandles.SafeX509ChainHandle>.ManagedToUnmanagedOut marshaller = new();
                        marshaller.FromUnmanaged(data);
                        nwContext!.CertificateAvailable(marshaller.ToManaged());
                        marshaller.Free();
                        break;
                    case NetworkFrameworkStatusUpdates.DebugLog:
                        if (NetEventSource.Log.IsEnabled())
                            NetEventSource.Info(nwContext, Marshal.PtrToStringAnsi(data)!, "Native");
                        break;
                    default: // We shouldn't hit here.
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(nwContext, $"Unknown status update: {statusUpdate}");
                        Debug.Assert(false);
                        break;
                }
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(null, $"StatusUpdateCallback failed for {statusUpdate}: {ex}");
                }
            }
        }
        private void FramerStartCallback(SafeNwHandle framerHandle)
        {
            _framerHandle = framerHandle;
            _framerReadyTcs.TrySetResult();
        }

        private void HandshakeFinished()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "TLS handshake completed successfully");
            // Abandon any transport read the loop parked while still treating the stream as
            // handshake traffic, before the application re-frames the transport underneath it.
            _handshakeReadCts.Cancel();
            _handshakeCompletionSource.TrySetResult(null);
        }

        private void ConnectionFailed(in Interop.NetworkFramework.NetworkFrameworkError error)
        {
            Exception ex = Interop.NetworkFramework.CreateExceptionForNetworkFrameworkError(in error);
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"TLS handshake failed with error: {ex.Message}");
            _handshakeCompletionSource.TrySetResult(ExceptionDispatchInfo.SetCurrentStackTrace(ex));
            _appReceiveBufferTcs.TrySetException(ex);
            // The framer may never start now; release anyone waiting to deliver inbound data.
            _framerReadyTcs.TrySetResult();
        }

        private void ConnectionClosed()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Connection was cancelled");
            _connectionClosedTcs.TrySetResult();
            _framerReadyTcs.TrySetResult();
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
