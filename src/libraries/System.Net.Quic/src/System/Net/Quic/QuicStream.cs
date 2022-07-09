// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Quic;
using static System.Net.Quic.MsQuicHelpers;
using static System.Net.Quic.QuicDefaults;
using static Microsoft.Quic.MsQuic;

using START_COMPLETE = Microsoft.Quic.QUIC_STREAM_EVENT._Anonymous_e__Union._START_COMPLETE_e__Struct;
using RECEIVE = Microsoft.Quic.QUIC_STREAM_EVENT._Anonymous_e__Union._RECEIVE_e__Struct;
using SEND_COMPLETE = Microsoft.Quic.QUIC_STREAM_EVENT._Anonymous_e__Union._SEND_COMPLETE_e__Struct;
using PEER_SEND_ABORTED = Microsoft.Quic.QUIC_STREAM_EVENT._Anonymous_e__Union._PEER_SEND_ABORTED_e__Struct;
using PEER_RECEIVE_ABORTED = Microsoft.Quic.QUIC_STREAM_EVENT._Anonymous_e__Union._PEER_RECEIVE_ABORTED_e__Struct;
using SEND_SHUTDOWN_COMPLETE = Microsoft.Quic.QUIC_STREAM_EVENT._Anonymous_e__Union._SEND_SHUTDOWN_COMPLETE_e__Struct;
using SHUTDOWN_COMPLETE = Microsoft.Quic.QUIC_STREAM_EVENT._Anonymous_e__Union._SHUTDOWN_COMPLETE_e__Struct;

namespace System.Net.Quic;

public sealed partial class QuicStream
{
    /// <summary>
    /// Handle to MsQuic connection object.
    /// </summary>
    private MsQuicContextSafeHandle _handle;

    /// <summary>
    /// Set to non-zero once disposed. Prevents double and/or concurrent disposal.
    /// </summary>
    private int _disposed;

    private readonly ValueTaskSource _startedTcs = new ValueTaskSource();
    private readonly ValueTaskSource _shutdownTcs = new ValueTaskSource();

    private readonly ResettableValueTaskSource _receiveTcs = new ResettableValueTaskSource();
// [ActiveIssue("https://github.com/dotnet/roslyn-analyzers/issues/5750")] Structs can have parameterless ctor now and thus the behavior differs from just defaulting the struct to zeros.
#pragma warning disable CA1805
    private ReceiveBuffers _receiveBuffers = new ReceiveBuffers();
#pragma warning restore CA1805
    private int _receivedNeedsEnable;

    private readonly ResettableValueTaskSource _sendTcs = new ResettableValueTaskSource();
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

    public Task ReadsClosed => _receiveTcs.GetFinalTask();

    public Task WritesClosed => _sendTcs.GetFinalTask();

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
            ThrowIfFailure(MsQuicApi.Api.ApiTable->StreamOpen(
                connectionHandle.QuicHandle,
                type == QuicStreamType.Unidirectional ? QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL : QUIC_STREAM_OPEN_FLAGS.NONE,
                &NativeCallback,
                (void*)GCHandle.ToIntPtr(context),
                &handle));
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

    internal ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        if (_startedTcs.TryInitialize(out ValueTask valueTask, this, cancellationToken))
        {
            unsafe
            {
                ThrowIfFailure(MsQuicApi.Api.ApiTable->StreamStart(
                    _handle.QuicHandle,
                    QUIC_STREAM_START_FLAGS.SHUTDOWN_ON_FAIL | QUIC_STREAM_START_FLAGS.INDICATE_PEER_ACCEPT));
            }
            // TODO: aborted and setting up the startedTcs
        }

        return valueTask;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        if (!_canRead)
        {
            throw new InvalidOperationException();
        }

        if (NetEventSource.Log.IsEnabled())
        {
            NetEventSource.Info(this, $"{this} Stream reading into memory of '{buffer.Length}' bytes.");
        }

        int totalCopied = 0;
        do
        {
            // Concurrent call, this one lost the race.
            if (!_receiveTcs.TryGetValueTask(out ValueTask valueTask, this, cancellationToken))
            {
                throw new InvalidOperationException();
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

            // This will either either wait for RECEIVE event (no data in buffer) or complete immediately and reset the task.
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
                ThrowIfFailure(MsQuicApi.Api.ApiTable->StreamReceiveSetEnabled(
                    _handle.QuicHandle,
                    1));
            }
        }

        return totalCopied;
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        => WriteAsync(buffer, completeWrites: false, cancellationToken);

    public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool completeWrites, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        if (!_canWrite)
        {
            throw new InvalidOperationException();
        }

        if (NetEventSource.Log.IsEnabled())
        {
            NetEventSource.Info(this, $"{this} Stream writing memory of '{buffer.Length}' bytes while {(completeWrites ? "completing" : "not completing")} writes.");
        }

        // Concurrent call, this one lost the race.
        if (!_sendTcs.TryGetValueTask(out ValueTask valueTask, this, cancellationToken))
        {
            throw new InvalidOperationException();
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

        try
        {
            _sendBuffers.Initialize(buffer);
            unsafe
            {
                ThrowIfFailure(MsQuicApi.Api.ApiTable->StreamSend(
                    _handle.QuicHandle,
                    _sendBuffers.Buffers,
                    (uint)_sendBuffers.Count,
                    completeWrites ? QUIC_SEND_FLAGS.FIN : QUIC_SEND_FLAGS.NONE,
                    null));
            }
        }
        catch (Exception ex)
        {
            _sendTcs.TrySetException(ex, final: true);
            _sendBuffers.Reset();
            throw;
        }

        return valueTask;
    }


    public void Abort(QuicAbortDirection abortDirection, long errorCode)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        QUIC_STREAM_SHUTDOWN_FLAGS flags = QUIC_STREAM_SHUTDOWN_FLAGS.NONE;
        if (abortDirection.HasFlag(QuicAbortDirection.Read))
        {
            flags |= QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_RECEIVE;
        }
        if (abortDirection.HasFlag(QuicAbortDirection.Write))
        {
            flags |= QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_SEND;
        }
        unsafe
        {
            ThrowIfFailure(MsQuicApi.Api.ApiTable->StreamShutdown(
                _handle.QuicHandle,
                flags,
                (ulong)errorCode));
        }
    }


    public void CompleteWrites()
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        if (_shutdownTcs.TryInitialize(out _))
        {
            unsafe
            {
                ThrowIfFailure(MsQuicApi.Api.ApiTable->StreamShutdown(
                    _handle.QuicHandle,
                    QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL,
                    default));
            }
        }
    }

    private unsafe int HandleEventStartComplete(ref START_COMPLETE data)
    {
        _id = (long)data.ID;
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
            _startedTcs.TrySetException(new MsQuicException(data.Status));
            // TODO: aborted and exception type
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
        return QUIC_STATUS_SUCCESS;
    }
    private unsafe int HandleEventSendComplete(ref SEND_COMPLETE data)
    {
        _sendBuffers.Reset();
        if (data.Canceled != 0)
        {
            // TODO: exception type
            _sendTcs.TrySetException(new OperationCanceledException());
        }
        else
        {
            _sendTcs.TrySetResult();
        }

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
        _receiveBuffers.SetFinal();
        _receiveTcs.TrySetException(new QuicStreamAbortedException((long)data.ErrorCode), final: true);
        return QUIC_STATUS_SUCCESS;
    }
    private unsafe int HandleEventPeerReceiveAborted(ref PEER_RECEIVE_ABORTED data)
    {
        _sendTcs.TrySetException(new QuicStreamAbortedException((long)data.ErrorCode), final: true);
        return QUIC_STATUS_SUCCESS;
    }
    private unsafe int HandleEventSendShutdownComplete(ref SEND_SHUTDOWN_COMPLETE data)
    {
        if (data.Graceful != 0)
        {
            _sendTcs.TrySetResult(final: true);
        }
        else
        {
            // TODO: exception type
            _sendTcs.TrySetException(new QuicStreamAbortedException(0), final: true);
        }

        return QUIC_STATUS_SUCCESS;
    }
    private unsafe int HandleEventShutdownComplete(ref SHUTDOWN_COMPLETE data)
    {
        if (data.ConnectionShutdown != 0)
        {
            _shutdownTcs.TrySetException(new QuicConnectionAbortedException((long)data.ConnectionErrorCode));
            return QUIC_STATUS_SUCCESS;
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
                NetEventSource.Error(null, $"Received event {streamEvent->Type}");
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
    /// If the read side is not fully consumed, i.e.: <see cref="ReadsClosed"/> is completed and/or <see cref="ReadAsync(Memory{byte}, CancellationToken)"/> returned <c>0</c>,
    /// dispose will abort the read side with provided <see cref="QuicConnectionOptions.DefaultStreamErrorCode"/>.
    /// If the write side hasn't been closed, it'll be closed gracefully as if <see cref="CompleteWrites"/> was called.    ///
    /// Finally, all resources associated with the stream will be released.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        // Flush the queue and dispose all remaining streams.
        if (_receiveTcs.TrySetException(new QuicOperationAbortedException(), final: true))
        {
            unsafe
            {
                int status = MsQuicApi.Api.ApiTable->StreamShutdown(
                    _handle.QuicHandle,
                    QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_RECEIVE,
                    (ulong)_defaultErrorCode);
                if (StatusFailed(status))
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        NetEventSource.Error(this, $"{this} StreamShutdown(ABORT_RECEIVE) failed: {MsQuicException.GetErrorCodeForStatus(status)}.");
                    }
                }
            }
        }

        // Check if the stream has been shut down and if not, shut it down.
        if (_shutdownTcs.TryInitialize(out ValueTask valueTask, this))
        {
            unsafe
            {
                int status = MsQuicApi.Api.ApiTable->StreamShutdown(
                    _handle.QuicHandle,
                    QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL,
                    default);
                if (StatusFailed(status))
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        NetEventSource.Error(this, $"{this} StreamShutdown(GRACEFUL) failed: {MsQuicException.GetErrorCodeForStatus(status)}.");
                    }
                }
            }
        }

        await valueTask.ConfigureAwait(false);
        _handle.Dispose();

        // TODO: memory leak if not disposed
        _sendBuffers.Dispose();
    }
}
