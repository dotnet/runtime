// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO;
using System.Net.Quic.Implementations;
using System.Net.Quic.Implementations.MsQuic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Quic;

namespace System.Net.Quic;

public sealed partial class QuicStream
{
    /*/// <summary>
    /// Handle to MsQuic connection object.
    /// </summary>
    private MsQuicContextSafeHandle _handle;*/

    /// <summary>
    /// Set to non-zero once disposed. Prevents double and/or concurrent disposal.
    /// </summary>
    private int _disposed;

    //private readonly ValueTaskSource _startedTcs = new ValueTaskSource();
    //private readonly ValueTaskSource _shutdownTcs = new ValueTaskSource();

    //private readonly ResettableValueTaskSource _receiveTcs = new ResettableValueTaskSource();
    //private ReceiveBuffers _receiveBuffers = new ReceiveBuffers();
    //private int _receivedNeedsEnable = 0;

    //private readonly ResettableValueTaskSource _sendTcs = new ResettableValueTaskSource();
    //private MsQuicBuffers _sendBuffers = new MsQuicBuffers();

    private bool _canRead = true;
    private bool _canWrite = true;

    //private long _streamId = -1;

    /*/// <summary>
    /// Set when PEER_SEND_ABORTED is received.
    /// </summary>
    private long _abortErrorCode = -1;*/

    public long Id { get; } // https://www.rfc-editor.org/rfc/rfc9000.html#name-stream-types-and-identifier
    public QuicStreamType Type { get; }  // https://github.com/dotnet/runtime/issues/55816, not necessary per se, CanRead and CanWrite should suffice

    // In 6.0 we had bool ReadsCompleted (tailored for ASP.NET) that would get set only in graceful case. The task gives the ability to distinguish error cases and is analogous to WritesCompleted.
    public Task ReadsClosed { get; } = Task.CompletedTask; // gets set when STREAM frame with FIN bit (=EOF, =ReadAsync returning 0) is received or when the peer aborts the sending side by sending RESET_STREAM frame. Inspired by channel - might be ValueTask.

    // In 6.0 we had a method Task WaitForWriteCompletionAsync(). We need a Task that is removed from the operation kicking the write completion but gets completed when the sending side gets closed (either by CompleteWrites, endStream=true, Abort(Write) or Abort(Read) from the peer).
    public Task WritesClosed { get; } = Task.CompletedTask; // gets set when the peer acknowledges STREAM frame with FIN bit (=EOF) or RESET_STREAM frame or when the peer aborts the receiving side by sending STOP_SENDING frame. Inspired by channel - might be ValueTask.

    internal QuicStream(QuicConnection.State connectionState, MsQuicContextSafeHandle connectionHandle, QuicStreamType streamType)
    {}
    internal unsafe QuicStream(QuicConnection.State connectionState, MsQuicContextSafeHandle connectionHandle, QUIC_HANDLE* handle, QUIC_STREAM_OPEN_FLAGS flags)
    {}

    internal ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);
        throw new Exception();
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => throw new Exception();

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        => throw new Exception();

    // Overload with completeWrites.
    public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool completeWrites, CancellationToken cancellationToken = default)
        => throw new Exception();


    // In 6.0 we had separate methods AbortRead and AbortWrite. This allows aborting both directions at the same time. It can remain split, it's not strictly necessary to have it combined.
    public void Abort(QuicAbortDirection abortDirection, long errorCode) {} // abortively ends either sending or receiving or both sides of the stream, i.e.: RESET_STREAM frame or STOP_SENDING frame

    // In 6.0 we had void Shutdown(), it was really badly named. The new name comes from DuplexStream API review, where CompleteWrites was chosen,
    public void CompleteWrites() {} // https://github.com/dotnet/runtime/issues/43290, gracefully ends the sending side, equivalent to WriteAsync with endStream set to true.

    public override ValueTask DisposeAsync()
        => throw new Exception();
}
