// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Quic;
using static System.Net.Quic.MsQuicHelpers;
using static Microsoft.Quic.MsQuic;

using START_COMPLETE = Microsoft.Quic.QUIC_STREAM_EVENT._Anonymous_e__Union._START_COMPLETE_e__Struct;
using RECEIVE = Microsoft.Quic.QUIC_STREAM_EVENT._Anonymous_e__Union._RECEIVE_e__Struct;
using SEND_COMPLETE = Microsoft.Quic.QUIC_STREAM_EVENT._Anonymous_e__Union._SEND_COMPLETE_e__Struct;
using PEER_SEND_ABORTED = Microsoft.Quic.QUIC_STREAM_EVENT._Anonymous_e__Union._PEER_SEND_ABORTED_e__Struct;
using PEER_RECEIVE_ABORTED = Microsoft.Quic.QUIC_STREAM_EVENT._Anonymous_e__Union._PEER_RECEIVE_ABORTED_e__Struct;
using SEND_SHUTDOWN_COMPLETE = Microsoft.Quic.QUIC_STREAM_EVENT._Anonymous_e__Union._SEND_SHUTDOWN_COMPLETE_e__Struct;
using SHUTDOWN_COMPLETE = Microsoft.Quic.QUIC_STREAM_EVENT._Anonymous_e__Union._SHUTDOWN_COMPLETE_e__Struct;

namespace System.Net.Quic;

/// <summary>
/// Represents a QUIC stream, see <see href="https://www.rfc-editor.org/rfc/rfc9000.html#name-streams">RFC 9000: Streams</see> for more details.
/// <see cref="QuicStream" /> can be <see cref="QuicStreamType.Unidirectional">unidirectional</see>, i.e.: write-only for the opening side,
/// or <see cref="QuicStreamType.Bidirectional">bidirectional</see> which allows both side to write.
/// </summary>
/// <remarks>
/// <see cref="QuicStream"/> can be used in a same way as any other <see cref="Stream"/>.
/// Apart from stream API, <see cref="QuicStream"/> also exposes QUIC specific features:
/// <list type="bullet">
/// <item>
/// <term><see cref="WriteAsync(System.ReadOnlyMemory{byte},bool,System.Threading.CancellationToken)"/></term>
/// <description>Allows to close the writing side of the stream as a single operation with the write itself.</description>
/// </item>
/// <item>
/// <term><see cref="CompleteWrites"/></term>
/// <description>Close the writing side of the stream.</description>
/// </item>
/// <item>
/// <term><see cref="Abort"/></term>
/// <description>Aborts either the writing or the reading side of the stream.</description>
/// </item>
/// <item>
/// <term><see cref="WritesClosed"/></term>
/// <description>A <see cref="Task"/> that will get completed when the stream writing side has been closed (gracefully or abortively).</description>
/// </item>
/// <item>
/// <term><see cref="ReadsClosed"/></term>
/// <description>A <see cref="Task"/> that will get completed when the stream reading side has been closed (gracefully or abortively).</description>
/// </item>
/// </list>
/// </remarks>
public sealed partial class QuicStream
{
    /// <summary>
    /// Handle to MsQuic connection object.
    /// </summary>
    private readonly MsQuicContextSafeHandle _handle;

    /// <summary>
    /// Set to non-zero once disposed. Prevents double and/or concurrent disposal.
    /// </summary>
    private int _disposed;

    private readonly ValueTaskSource _startedTcs = new ValueTaskSource();
    private readonly ValueTaskSource _shutdownTcs = new ValueTaskSource();

    private readonly ResettableValueTaskSource _receiveTcs = new ResettableValueTaskSource()
    {
        CancellationAction = target =>
        {
            if (target is QuicStream stream)
            {
                stream.Abort(QuicAbortDirection.Read, stream._defaultErrorCode);
            }
        }
    };
// [ActiveIssue("https://github.com/dotnet/roslyn-analyzers/issues/5750")] Structs can have parameterless ctor now and thus the behavior differs from just defaulting the struct to zeros.
#pragma warning disable CA1805
    private ReceiveBuffers _receiveBuffers = new ReceiveBuffers();
#pragma warning restore CA1805
    private int _receivedNeedsEnable;

    private readonly ResettableValueTaskSource _sendTcs = new ResettableValueTaskSource()
    {
        CancellationAction = target =>
        {
            if (target is QuicStream stream)
            {
                stream.Abort(QuicAbortDirection.Write, stream._defaultErrorCode);
            }
        }
    };
// [ActiveIssue("https://github.com/dotnet/roslyn-analyzers/issues/5750")] Structs can have parameterless ctor now and thus the behavior differs from just defaulting the struct to zeros.
#pragma warning disable CA1805
    private MsQuicBuffers _sendBuffers = new MsQuicBuffers();
#pragma warning restore CA1805

    private readonly long _defaultErrorCode;

    private bool _canRead;
    private bool _canWrite;

    private long _id = -1;
    private QuicStreamType _type;

    /// <summary>
    /// Stream id, see <see href="https://www.rfc-editor.org/rfc/rfc9000.html#name-stream-types-and-identifier" />.
    /// </summary>
    public long Id => _id;

    /// <summary>
    /// Stream type, see <see href="https://www.rfc-editor.org/rfc/rfc9000.html#name-stream-types-and-identifier" />.
    /// </summary>
    public QuicStreamType Type => _type;

    /// <summary>
    /// A <see cref="Task"/> that will get completed once reading side has been closed.
    /// Which might be by reading till end of stream (<see cref="ReadAsync(System.Memory{byte},System.Threading.CancellationToken)"/> will return <c>0</c>),
    /// or when <see cref="Abort"/> for <see cref="QuicAbortDirection.Read"/> is called,
    /// or when the peer called <see cref="Abort"/> for <see cref="QuicAbortDirection.Write"/>.
    /// </summary>
    public Task ReadsClosed => _receiveTcs.GetFinalTask();

    /// <summary>
    /// A <see cref="Task"/> that will get completed once writing side has been closed.
    /// Which might be by closing the write side via <see cref="CompleteWrites"/>
    /// or <see cref="WriteAsync(System.ReadOnlyMemory{byte},bool,System.Threading.CancellationToken)"/> with <c>completeWrites: true</c> and getting acknowledgement from the peer for it,
    /// or when <see cref="Abort"/> for <see cref="QuicAbortDirection.Write"/> is called,
    /// or when the peer called <see cref="Abort"/> for <see cref="QuicAbortDirection.Read"/>.
    /// </summary>
    public Task WritesClosed => _sendTcs.GetFinalTask();

    /// <inheritdoc cref="ToString"/>
    public override string ToString() => _handle.ToString();

    /// <summary>
    /// Initializes a new instance of an outbound <see cref="QuicStream" />.
    /// </summary>
    /// <param name="connectionHandle"><see cref="QuicConnection"/> safe handle, used to increment/decrement reference count with each associated stream.</param>
    /// <param name="type">The type of the stream to open.</param>
    /// <param name="defaultErrorCode">Error code used when the stream needs to abort read or write side of the stream internally.</param>
    internal unsafe QuicStream(MsQuicContextSafeHandle connectionHandle, QuicStreamType type, long defaultErrorCode)
    {
        GCHandle context = GCHandle.Alloc(this, GCHandleType.Weak);
        try
        {
            QUIC_HANDLE* handle;
            ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.ApiTable->StreamOpen(
                connectionHandle.QuicHandle,
                type == QuicStreamType.Unidirectional ? QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL : QUIC_STREAM_OPEN_FLAGS.NONE,
                &NativeCallback,
                (void*)GCHandle.ToIntPtr(context),
                &handle),
                "StreamOpen failed");
            _handle = new MsQuicContextSafeHandle(handle, context, MsQuicApi.Api.ApiTable->StreamClose, SafeHandleType.Stream, connectionHandle);
        }
        catch
        {
            context.Free();
            throw;
        }

        _defaultErrorCode = defaultErrorCode;

        _canRead = type == QuicStreamType.Bidirectional;
        _canWrite = true;
        if (!_canRead)
        {
            _receiveTcs.TrySetResult(final: true);
        }
        _type = type;
    }

    /// <summary>
    /// Initializes a new instance of an inbound <see cref="QuicStream" />.
    /// </summary>
    /// <param name="connectionHandle"><see cref="QuicConnection"/> safe handle, used to increment/decrement reference count with each associated stream.</param>
    /// <param name="handle">Native handle.</param>
    /// <param name="flags">Related data from the PEER_STREAM_STARTED connection event.</param>
    /// <param name="defaultErrorCode">Error code used when the stream needs to abort read or write side of the stream internally.</param>
    internal unsafe QuicStream(MsQuicContextSafeHandle connectionHandle, QUIC_HANDLE* handle, QUIC_STREAM_OPEN_FLAGS flags, long defaultErrorCode)
    {
        GCHandle context = GCHandle.Alloc(this, GCHandleType.Weak);
        try
        {
            delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_STREAM_EVENT*, int> nativeCallback = &NativeCallback;
            MsQuicApi.Api.ApiTable->SetCallbackHandler(
                handle,
                nativeCallback,
                (void*)GCHandle.ToIntPtr(context));
            _handle = new MsQuicContextSafeHandle(handle, context, MsQuicApi.Api.ApiTable->StreamClose, SafeHandleType.Stream, connectionHandle);
        }
        catch
        {
            context.Free();
            throw;
        }

        _defaultErrorCode = defaultErrorCode;

        _canRead = true;
        _canWrite = !flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL);
        if (!_canWrite)
        {
            _sendTcs.TrySetResult(final: true);
        }
        _id = (long)GetMsQuicParameter<ulong>(_handle, QUIC_PARAM_STREAM_ID);
        _type = flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL) ? QuicStreamType.Unidirectional : QuicStreamType.Bidirectional;
        _startedTcs.TrySetResult();
    }

    /// <summary>
    /// Starts the stream, but doesn't send anything to the peer yet.
    /// If no more concurrent streams can be opened at the moment, the operation will wait until it can,
    /// either by closing some existing streams or receiving more available stream ids from the peer.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An asynchronous task that completes with the opened <see cref="QuicStream" />.</returns>
    internal ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        _startedTcs.TryInitialize(out ValueTask valueTask, this, cancellationToken);
        {
            unsafe
            {
                int status = MsQuicApi.Api.ApiTable->StreamStart(
                    _handle.QuicHandle,
                    QUIC_STREAM_START_FLAGS.SHUTDOWN_ON_FAIL | QUIC_STREAM_START_FLAGS.INDICATE_PEER_ACCEPT);
                if (ThrowHelper.TryGetStreamExceptionForMsQuicStatus(status, out Exception? exception))
                {
                    _startedTcs.TrySetException(exception);
                }
            }
        }

        return valueTask;
    }

    /// <inheritdoc cref="ReadAsync(System.Memory{byte},System.Threading.CancellationToken)"/>
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        if (!_canRead)
        {
            throw new InvalidOperationException(SR.net_quic_reading_notallowed);
        }

        if (NetEventSource.Log.IsEnabled())
        {
            NetEventSource.Info(this, $"{this} Stream reading into memory of '{buffer.Length}' bytes.");
        }

        if (_receiveTcs.IsCompleted)
        {
            // Special case exception type for pre-canceled token while we've already transitioned to a final state and don't need to abort read.
            // It must happen before we try to get the value task, since the task source is versioned and each instance must be awaited.
            cancellationToken.ThrowIfCancellationRequested();
        }

        // The following loop will repeat at most twice depending whether some data are readily available in the buffer (one iteration) or not.
        // In which case, it'll wait on RECEIVE or any of PEER_SEND_(SHUTDOWN|ABORTED) event and attempt to copy data in the second iteration.
        int totalCopied = 0;
        do
        {
            // Concurrent call, this one lost the race.
            if (!_receiveTcs.TryGetValueTask(out ValueTask valueTask, this, cancellationToken))
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidnestedcall, "read"));
            }

            // Copy data from the buffer, reduce target and increment total.
            int copied = _receiveBuffers.CopyTo(buffer, out bool complete, out bool empty);
            buffer = buffer.Slice(copied);
            totalCopied += copied;

            // Make sure the task transitions into final state before the method finishes.
            if (complete)
            {
                _receiveTcs.TrySetResult(final: true);
            }

            // Unblock the next await to end immediately, i.e. there were/are any data in the buffer.
            if (totalCopied > 0 || !empty)
            {
                _receiveTcs.TrySetResult();
            }

            // This will either wait for RECEIVE event (no data in buffer) or complete immediately and reset the task.
            await valueTask.ConfigureAwait(false);

            // This is the last read, finish even despite not copying anything.
            if (complete)
            {
                break;
            }
        } while (!buffer.IsEmpty && totalCopied == 0);  // Exit the loop if target buffer is full we at least copied something.

        if (totalCopied > 0 && Interlocked.CompareExchange(ref _receivedNeedsEnable, 0, 1) == 1)
        {
            unsafe
            {
                ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.ApiTable->StreamReceiveSetEnabled(
                    _handle.QuicHandle,
                    1),
                "StreamReceivedSetEnabled failed");
            }
        }

        return totalCopied;
    }

    /// <inheritdoc cref="WriteAsync(System.ReadOnlyMemory{byte},System.Threading.CancellationToken)"/>
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        => WriteAsync(buffer, completeWrites: false, cancellationToken);


    /// <inheritdoc cref="WriteAsync(System.ReadOnlyMemory{byte},System.Threading.CancellationToken)"/>
    /// <param name="buffer">The region of memory to write data from.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
    /// <param name="completeWrites">Notifies the peer about gracefully closing the write side, i.e.: sends FIN flag with the data.</param>
    public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool completeWrites, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        if (!_canWrite)
        {
            throw new InvalidOperationException(SR.net_quic_writing_notallowed);
        }

        if (NetEventSource.Log.IsEnabled())
        {
            NetEventSource.Info(this, $"{this} Stream writing memory of '{buffer.Length}' bytes while {(completeWrites ? "completing" : "not completing")} writes.");
        }

        if (_sendTcs.IsCompleted)
        {
            // Special case exception type for pre-canceled token while we've already transitioned to a final state and don't need to abort write.
            // It must happen before we try to get the value task, since the task source is versioned and each instance must be awaited.
            cancellationToken.ThrowIfCancellationRequested();
        }

        // Concurrent call, this one lost the race.
        if (!_sendTcs.TryGetValueTask(out ValueTask valueTask, this, cancellationToken))
        {
            throw new InvalidOperationException(SR.Format(SR.net_io_invalidnestedcall, "write"));
        }

        // No need to call anything since we already have a result, most likely an exception.
        if (valueTask.IsCompleted)
        {
            return valueTask;
        }

        // For an empty buffer complete immediately, close the writing side of the stream if necessary.
        if (buffer.IsEmpty)
        {
            _sendTcs.TrySetResult();
            if (completeWrites)
            {
                CompleteWrites();
            }
            return valueTask;
        }

        _sendBuffers.Initialize(buffer);
        unsafe
        {
            int status = MsQuicApi.Api.ApiTable->StreamSend(
                _handle.QuicHandle,
                _sendBuffers.Buffers,
                (uint)_sendBuffers.Count,
                completeWrites ? QUIC_SEND_FLAGS.FIN : QUIC_SEND_FLAGS.NONE,
                null);
            if (ThrowHelper.TryGetStreamExceptionForMsQuicStatus(status, out Exception? exception))
            {
                _sendBuffers.Reset();
                _sendTcs.TrySetException(exception, final: true);
            }
        }

        return valueTask;
    }

    /// <summary>
    /// Aborts either <see cref="QuicAbortDirection.Read">reading</see>, <see cref="QuicAbortDirection.Write">writing</see> or <see cref="QuicAbortDirection.Both">both</see> sides of the stream.
    /// </summary>
    /// <remarks>
    /// Corresponds to <see href="https://www.rfc-editor.org/rfc/rfc9000.html#frame-stop-sending">STOP_SENDING</see>
    /// and <see href="https://www.rfc-editor.org/rfc/rfc9000.html#frame-reset-stream">RESET_STREAM</see> QUIC frames.
    /// </remarks>
    /// <param name="abortDirection">The direction of the stream to abort.</param>
    /// <param name="errorCode">The error code with which to abort the stream, this value is application protocol (layer above QUIC) dependent.</param>
    public void Abort(QuicAbortDirection abortDirection, long errorCode)
    {
        if (_disposed == 1)
        {
            return;
        }

        QUIC_STREAM_SHUTDOWN_FLAGS flags = QUIC_STREAM_SHUTDOWN_FLAGS.NONE;
        if (abortDirection.HasFlag(QuicAbortDirection.Read))
        {
            flags |= QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_RECEIVE;
            if (_receiveTcs.TrySetException(ThrowHelper.GetOperationAbortedException(SR.net_quic_reading_aborted), final: true))
            {
                flags |= QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_RECEIVE;
            }
        }
        if (abortDirection.HasFlag(QuicAbortDirection.Write))
        {
            if (_sendTcs.TrySetException(ThrowHelper.GetOperationAbortedException(SR.net_quic_writing_aborted), final: true))
            {
                flags |= QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_SEND;
            }
        }
        // Nothing to abort, the requested sides to abort are already closed.
        if (flags == QUIC_STREAM_SHUTDOWN_FLAGS.NONE)
        {
            return;
        }

        unsafe
        {
            ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.ApiTable->StreamShutdown(
                _handle.QuicHandle,
                flags,
                (ulong)errorCode),
                "StreamShutdown failed");
        }
    }

    /// <summary>
    /// Gracefully completes the writing side of the stream.
    /// Equivalent to using <see cref="WriteAsync(System.ReadOnlyMemory{byte},bool,System.Threading.CancellationToken)"/> with <c>completeWrites: true</c>.
    /// </summary>
    /// <remarks>
    /// Corresponds to an empty <see href="https://www.rfc-editor.org/rfc/rfc9000.html#frame-stream">STREAM</see> frame with <c>FIN</c> flag set to <c>true</c>.
    /// </remarks>
    public void CompleteWrites()
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        if (_shutdownTcs.TryInitialize(out _, this))
        {
            unsafe
            {
                ThrowHelper.ThrowIfMsQuicError(MsQuicApi.Api.ApiTable->StreamShutdown(
                    _handle.QuicHandle,
                    QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL,
                    default),
                    "StreamShutdown failed");
            }
        }
    }

    private unsafe int HandleEventStartComplete(ref START_COMPLETE data)
    {
        _id = unchecked((long)data.ID);
        if (StatusSucceeded(data.Status))
        {
            if (data.PeerAccepted != 0)
            {
                _startedTcs.TrySetResult();
            }
            // If PeerAccepted == 0, we will later receive PEER_ACCEPTED event, which will complete the _startedTcs.
        }
        else
        {
            if (ThrowHelper.TryGetStreamExceptionForMsQuicStatus(data.Status, out Exception? exception))
            {
                _startedTcs.TrySetException(exception);
            }
        }

        return QUIC_STATUS_SUCCESS;
    }
    private unsafe int HandleEventReceive(ref RECEIVE data)
    {
        ulong totalCopied = (ulong)_receiveBuffers.CopyFrom(
            new ReadOnlySpan<QUIC_BUFFER>(data.Buffers, (int) data.BufferCount),
            (int) data.TotalBufferLength,
            data.Flags.HasFlag(QUIC_RECEIVE_FLAGS.FIN));
        if (totalCopied < data.TotalBufferLength)
        {
            Volatile.Write(ref _receivedNeedsEnable, 1);
        }

        _receiveTcs.TrySetResult();

        data.TotalBufferLength = totalCopied;
        return (_receiveBuffers.HasCapacity() && Interlocked.CompareExchange(ref _receivedNeedsEnable, 0, 1) == 1) ? QUIC_STATUS_CONTINUE : QUIC_STATUS_SUCCESS;
    }
    private unsafe int HandleEventSendComplete(ref SEND_COMPLETE data)
    {
        _sendBuffers.Reset();
        if (data.Canceled == 0)
        {
            _sendTcs.TrySetResult();
        }
        // If Canceled != 0, we either aborted write, received PEER_RECEIVE_ABORTED or will receive SHUTDOWN_COMPLETE(ConnectionClose) later, all of which completes the _sendTcs.
        return QUIC_STATUS_SUCCESS;
    }
    private unsafe int HandleEventPeerSendShutdown()
    {
        // Same as RECEIVE with FIN flag. Remember that no more RECEIVE events will come.
        // Don't set the task to its final state yet, but wait for all the buffered data to get consumed first.
        _receiveBuffers.SetFinal();
        _receiveTcs.TrySetResult();
        return QUIC_STATUS_SUCCESS;
    }
    private unsafe int HandleEventPeerSendAborted(ref PEER_SEND_ABORTED data)
    {
        _receiveTcs.TrySetException(ThrowHelper.GetStreamAbortedException((long)data.ErrorCode), final: true);
        return QUIC_STATUS_SUCCESS;
    }
    private unsafe int HandleEventPeerReceiveAborted(ref PEER_RECEIVE_ABORTED data)
    {
        _sendTcs.TrySetException(ThrowHelper.GetStreamAbortedException((long)data.ErrorCode), final: true);
        return QUIC_STATUS_SUCCESS;
    }
    private unsafe int HandleEventSendShutdownComplete(ref SEND_SHUTDOWN_COMPLETE data)
    {
        if (data.Graceful != 0)
        {
            _sendTcs.TrySetResult(final: true);
        }
        // If Graceful == 0, we either aborted write, received PEER_RECEIVE_ABORTED or will receive SHUTDOWN_COMPLETE(ConnectionClose) later, all of which completes the _sendTcs.
        return QUIC_STATUS_SUCCESS;
    }
    private unsafe int HandleEventShutdownComplete(ref SHUTDOWN_COMPLETE data)
    {
        if (data.ConnectionShutdown != 0)
        {
            bool shutdownByApp = data.ConnectionShutdownByApp != 0;
            bool closedRemotely = data.ConnectionClosedRemotely != 0;
            Exception exception = (shutdownByApp, closedRemotely) switch
            {
                // It's remote shutdown by app, peer's side called QuicConnection.CloseAsync, throw QuicError.ConnectionAborted.
                (shutdownByApp: true, closedRemotely: true) => ThrowHelper.GetConnectionAbortedException((long)data.ConnectionErrorCode),
                // It's local shutdown by app, this side called QuicConnection.CloseAsync, throw QuicError.OperationAborted.
                (shutdownByApp: true, closedRemotely: false) => ThrowHelper.GetOperationAbortedException(),
                // It's remote shutdown by transport, (TODO: we should propagate transport error code), throw QuicError.InternalError.
                // https://github.com/dotnet/runtime/issues/72666
                (shutdownByApp: false, closedRemotely: true) => ThrowHelper.GetExceptionForMsQuicStatus(QUIC_STATUS_INTERNAL_ERROR, $"Shutdown by transport {data.ConnectionErrorCode}"),
                // It's local shutdown by transport, assuming idle connection (TODO: we should get Connection.CloseStatus), throw QuicError.ConnectionIdle.
                // https://github.com/dotnet/runtime/issues/72666
                (shutdownByApp: false, closedRemotely: false) => ThrowHelper.GetExceptionForMsQuicStatus(QUIC_STATUS_CONNECTION_IDLE),
            };
            _startedTcs.TrySetException(exception);
            _receiveTcs.TrySetException(exception, final: true);
            _sendTcs.TrySetException(exception, final: true);
        }
        _shutdownTcs.TrySetResult();
        return QUIC_STATUS_SUCCESS;
    }
    private unsafe int HandleEventPeerAccepted()
    {
        _startedTcs.TrySetResult();
        return QUIC_STATUS_SUCCESS;
    }

    private unsafe int HandleStreamEvent(ref QUIC_STREAM_EVENT streamEvent)
        => streamEvent.Type switch
        {
            QUIC_STREAM_EVENT_TYPE.START_COMPLETE => HandleEventStartComplete(ref streamEvent.START_COMPLETE),
            QUIC_STREAM_EVENT_TYPE.RECEIVE => HandleEventReceive(ref streamEvent.RECEIVE),
            QUIC_STREAM_EVENT_TYPE.SEND_COMPLETE => HandleEventSendComplete(ref streamEvent.SEND_COMPLETE),
            QUIC_STREAM_EVENT_TYPE.PEER_SEND_SHUTDOWN => HandleEventPeerSendShutdown(),
            QUIC_STREAM_EVENT_TYPE.PEER_SEND_ABORTED => HandleEventPeerSendAborted(ref streamEvent.PEER_SEND_ABORTED),
            QUIC_STREAM_EVENT_TYPE.PEER_RECEIVE_ABORTED => HandleEventPeerReceiveAborted(ref streamEvent.PEER_RECEIVE_ABORTED),
            QUIC_STREAM_EVENT_TYPE.SEND_SHUTDOWN_COMPLETE => HandleEventSendShutdownComplete(ref streamEvent.SEND_SHUTDOWN_COMPLETE),
            QUIC_STREAM_EVENT_TYPE.SHUTDOWN_COMPLETE => HandleEventShutdownComplete(ref streamEvent.SHUTDOWN_COMPLETE),
            QUIC_STREAM_EVENT_TYPE.PEER_ACCEPTED => HandleEventPeerAccepted(),
            _ => QUIC_STATUS_SUCCESS
        };

#pragma warning disable CS3016
    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) })]
#pragma warning restore CS3016
    private static unsafe int NativeCallback(QUIC_HANDLE* connection, void* context, QUIC_STREAM_EVENT* streamEvent)
    {
        GCHandle stateHandle = GCHandle.FromIntPtr((IntPtr)context);

        // Check if the instance hasn't been collected.
        if (!stateHandle.IsAllocated || stateHandle.Target is not QuicStream instance)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Error(null, $"Received event {streamEvent->Type} while connection is already disposed");
            }
            return QUIC_STATUS_INVALID_STATE;
        }

        try
        {
            // Process the event.
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(instance, $"{instance} Received event {streamEvent->Type}");
            }
            return instance.HandleStreamEvent(ref *streamEvent);
        }
        catch (Exception ex)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Error(instance, $"{instance} Exception while processing event {streamEvent->Type}: {ex}");
            }
            return QUIC_STATUS_INTERNAL_ERROR;
        }
    }

    /// <summary>
    /// If the read side is not fully consumed, i.e.: <see cref="ReadsClosed"/> is not completed and/or <see cref="ReadAsync(Memory{byte}, CancellationToken)"/> hasn't returned <c>0</c>,
    /// dispose will abort the read side with provided <see cref="QuicConnectionOptions.DefaultStreamErrorCode"/>.
    /// If the write side hasn't been closed, it'll be closed gracefully as if <see cref="CompleteWrites"/> was called.
    /// Finally, all resources associated with the stream will be released.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        ValueTask valueTask;

        // If the stream wasn't started successfully, gracelessly abort it.
        if (!_startedTcs.IsCompletedSuccessfully)
        {
            // Check if the stream has been shut down and if not, shut it down.
            if (_shutdownTcs.TryInitialize(out valueTask, this))
            {
                StreamShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.ABORT | QUIC_STREAM_SHUTDOWN_FLAGS.IMMEDIATE, _defaultErrorCode);
            }
        }
        else
        {
            // Abort the read side of the stream if it hasn't been fully consumed.
            if (_receiveTcs.TrySetException(ThrowHelper.GetOperationAbortedException(), final: true))
            {
                StreamShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_RECEIVE, _defaultErrorCode);
            }
            // Check if the stream has been shut down and if not, shut it down.
            if (_shutdownTcs.TryInitialize(out valueTask, this))
            {
                StreamShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL, default);
            }
        }

        // Wait for SHUTDOWN_COMPLETE, the last event, so that all resources can be safely released.
        await valueTask.ConfigureAwait(false);
        _handle.Dispose();

        // TODO: memory leak if not disposed
        _sendBuffers.Dispose();

        unsafe void StreamShutdown(QUIC_STREAM_SHUTDOWN_FLAGS flags, long errorCode)
        {
            int status = MsQuicApi.Api.ApiTable->StreamShutdown(
                _handle.QuicHandle,
                flags,
                (ulong)errorCode);
            if (StatusFailed(status))
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(this, $"{this} StreamShutdown({flags}) failed: {ThrowHelper.GetErrorMessageForStatus(status)}.");
                }
            }
        }
    }
}
