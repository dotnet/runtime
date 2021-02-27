// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Net.WebSockets
{
    internal partial class ManagedWebSocket
    {
        private enum ReceiveResultType
        {
            Text = WebSocketMessageType.Text,
            Binary = WebSocketMessageType.Binary,

            ConnectionClose,
            ControlMessage,

            ProtocolError = WebSocketCloseStatus.ProtocolError,
            InvalidPayloadData = WebSocketCloseStatus.InvalidPayloadData
        }

        [StructLayout(LayoutKind.Auto)]
        private readonly struct ReceiveResult
        {
            /// <summary>
            /// 1 bit EOF, 31 bits Count
            /// </summary>
            private readonly uint _count;

            public ReceiveResult(ReceiveResultType resultType)
            {
                ResultType = resultType;
                _count = default;
            }

            public ReceiveResult(int count, MessageOpcode opcode, bool eof)
            {
                ResultType = (ReceiveResultType)(opcode - 1);
                _count = (uint)count;

                if (eof)
                {
                    _count |= 0b10000000_00000000_00000000_00000000;
                }
            }

            public int Count => (int)(_count & 0b01111111_11111111_11111111_11111111);
            public bool EndOfMessage => (_count & 0b10000000_00000000_00000000_00000000) != 0;
            public ReceiveResultType ResultType { get; }
        }

        private sealed class Receiver : IDisposable, IValueTaskSource<ReceiveResult>, IValueTaskSource<bool>
        {
            private readonly bool _isServer;
            private readonly Stream _stream;

            /// <summary>
            /// The last header received in a ReceiveAsync. If ReceiveAsync got a header but then
            /// returned fewer bytes than was indicated in the header, subsequent ReceiveAsync calls
            /// will use the data from the header to construct the subsequent receive results, and
            /// the payload length in this header will be decremented to indicate the number of bytes
            /// remaining to be received for that header.  As a result, between fragments, the payload
            /// length in this header should be 0.
            /// </summary>
            private MessageOpcode _lastOpcode = MessageOpcode.Text;
            private long _remainingPayloadLength;
            private bool _lastFrameFin = true;
            private int _lastFrameMask;

            /// <summary>
            /// Buffer used for reading message header and control message payloads from the stream.
            /// Not readonly here because the buffer is mutable and is a struct.
            /// </summary>
            private ControlBuffer _controlBuffer;

            /// <summary>
            /// When dealing with partially read fragments of binary/text messages, a mask previously received may still
            /// apply, and the first new byte received may not correspond to the 0th position in the mask.  This value is
            /// the next offset into the mask that should be applied.
            /// </summary>
            private int _receivedMaskOffset;

            /// <summary>
            /// When parsing message header if an error occurs the websocket is notified and this
            /// will contain the error message.
            /// </summary>
            private string? _headerOrPayloadError;

            /// <summary>
            /// Tracks the state of the validity of the UTF8 encoding of text payloads.  Text may be split across fragments.
            /// </summary>
            private Utf8MessageState? _utf8TextState;

            // Because we support only single receive at a time, we can store the input
            // parameters so we can avoid having to allocate async state machines to do this for us.
            private Memory<byte> _output;
            private int _outputWrittenByteCount;
            private CancellationToken _cancellationToken;

            // Manual state machine implementation details to avoid any allocations
            private ManualResetValueTaskSourceCore<ReceiveResult> _taskSource;
            private ConfiguredValueTaskAwaitable<bool>.ConfiguredValueTaskAwaiter _headerOrPayloadAwaiter;

            private ManualResetValueTaskSourceCore<bool> _streamTaskSource;
            private ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter _streamAwaiter;

            private readonly Action _headerReceived;
            private readonly Action _payloadReceived;
            private readonly Action _streamReadHeaderCompleted;
            private readonly Action _streamReadPayloadCompleted;

            public Receiver(Stream stream, bool isServer)
            {
                _stream = stream;
                _isServer = isServer;

                // Create a buffer just large enough to handle received packet headers (at most 14 bytes) and
                // control payloads (at most 125 bytes). Message payloads are read directly into the buffer
                // supplied to ReceiveAsync.
                _controlBuffer = new ControlBuffer(MaxControlPayloadLength);

                _headerReceived = OnHeaderReceived;
                _payloadReceived = OnPayloadReceived;
                _streamReadHeaderCompleted = () => OnStreamReadCompleted(payload: false);
                _streamReadPayloadCompleted = () => OnStreamReadCompleted(payload: true);
            }

            public void Dispose()
            {
            }

            public string? GetErrorMessage() => _headerOrPayloadError;

            /// <summary>Issues a read on the stream to wait for EOF.</summary>
            public async ValueTask WaitForServerToCloseConnectionAsync(CancellationToken cancellationToken)
            {
                _controlBuffer.Reset();

                // Per RFC 6455 7.1.1, try to let the server close the connection.  We give it up to a second.
                // We simply issue a read and don't care what we get back; we could validate that we don't get
                // additional data, but at this point we're about to close the connection and we're just stalling
                // to try to get the server to close first.
                ValueTask<int> finalReadTask = _stream.ReadAsync(_controlBuffer.FreeMemory, cancellationToken);

                if (!finalReadTask.IsCompletedSuccessfully)
                {
                    // Wait an arbitrary amount of time to give the server (same as netfx, 1 second)
                    using var cts = finalReadTask.IsCompleted ? null : new CancellationTokenSource(TimeSpan.FromSeconds(1));
                    using var cancellation = cts is not null ? cts.Token.UnsafeRegister(static s => ((Stream)s!).Dispose(), _stream) : default;

                    // TODO: Once this is merged https://github.com/dotnet/runtime/issues/47525
                    // use configure await with the option to suppress exceptions and remove the try catch
                    try
                    {
                        await finalReadTask.ConfigureAwait(false);
                    }
                    catch
                    {
                        // Eat any resulting exceptions. We were going to close the connection, anyway.
                    }
                }
            }

            public async ValueTask<ControlMessage?> ReceiveControlMessageAsync(CancellationToken cancellationToken)
            {
                Debug.Assert(_lastOpcode > MessageOpcode.Binary);

                if (_remainingPayloadLength == 0)
                {
                    return new ControlMessage(_lastOpcode, ReadOnlyMemory<byte>.Empty);
                }
                _controlBuffer.Reset();

                while (_remainingPayloadLength > _controlBuffer.AvailableLength)
                {
                    int byteCount = await _stream.ReadAsync(_controlBuffer.FreeMemory, cancellationToken).ConfigureAwait(false);
                    if (byteCount <= 0)
                    {
                        return null;
                    }
                    ApplyMask(_controlBuffer.FreeMemory.Span.Slice(0, (int)Math.Min(_remainingPayloadLength, byteCount)));
                    _controlBuffer.Commit(byteCount);
                }

                // Update the payload length in the header to indicate
                // that we've received everything we need.
                ReadOnlyMemory<byte> payload = _controlBuffer.AvailableMemory.Slice(0, (int)_remainingPayloadLength);

                _remainingPayloadLength = 0;

                return new ControlMessage(_lastOpcode, payload);
            }

            public ValueTask<ReceiveResult> ReceiveAsync(Memory<byte> output, CancellationToken cancellationToken)
            {
                _output = output;
                _cancellationToken = cancellationToken;
                _taskSource.Reset();

                bool resetInputState = true;

                try
                {
                    // When there's nothing left over to receive, start a new
                    if (_remainingPayloadLength == 0)
                    {
                        ValueTask<bool> headerTask = ReceiveHeaderAsync();

                        if (!headerTask.IsCompleted)
                        {
                            _headerOrPayloadAwaiter = headerTask.ConfigureAwait(false).GetAwaiter();
                            _headerOrPayloadAwaiter.UnsafeOnCompleted(_headerReceived);
                            resetInputState = false;
                            return new ValueTask<ReceiveResult>(this, _taskSource.Version);
                        }

                        if (!headerTask.GetAwaiter().GetResult())
                        {
                            return new ValueTask<ReceiveResult>(new ReceiveResult(_headerOrPayloadError is not null ?
                                ReceiveResultType.ProtocolError : ReceiveResultType.ConnectionClose));
                        }

                        if (_lastOpcode > MessageOpcode.Binary)
                        {
                            // The received message is a control message and it's up to the websocket how to handle it.
                            return new ValueTask<ReceiveResult>(new ReceiveResult(ReceiveResultType.ControlMessage));
                        }
                    }

                    if (_output.IsEmpty || _remainingPayloadLength == 0)
                    {
                        return new ValueTask<ReceiveResult>(Result(count: 0));
                    }

                    Debug.Assert(_remainingPayloadLength > 0);
                    ValueTask<bool> receiveTask = ReceiveUncompressedAsync();
                    if (!receiveTask.IsCompleted)
                    {
                        _headerOrPayloadAwaiter = receiveTask.ConfigureAwait(false).GetAwaiter();
                        _headerOrPayloadAwaiter.UnsafeOnCompleted(_payloadReceived);
                        resetInputState = false;
                        return new ValueTask<ReceiveResult>(this, _taskSource.Version);
                    }

                    if (!receiveTask.GetAwaiter().GetResult())
                    {
                        return new ValueTask<ReceiveResult>(new ReceiveResult(ReceiveResultType.ConnectionClose));
                    }

                    return new ValueTask<ReceiveResult>(Result(_outputWrittenByteCount));
                }
                finally
                {
                    if (resetInputState)
                    {
                        ResetInputState();
                    }
                }
            }

            private void ResetInputState()
            {
                _output = default;
                _outputWrittenByteCount = 0;
                _cancellationToken = default;
            }

            private void OnHeaderReceived()
            {
                try
                {
                    if (!GetResult(ref _headerOrPayloadAwaiter))
                    {
                        SetReceiveResult(_headerOrPayloadError is not null ?
                            ReceiveResultType.ProtocolError : ReceiveResultType.ConnectionClose);
                    }
                    else if (_lastOpcode > MessageOpcode.Binary)
                    {
                        SetReceiveResult(ReceiveResultType.ControlMessage);
                    }
                    else if (_output.IsEmpty || _remainingPayloadLength == 0)
                    {
                        SetReceiveResult();
                    }
                    else
                    {
                        ValueTask<bool> receiveTask = ReceiveUncompressedAsync();
                        if (!receiveTask.IsCompleted)
                        {
                            _headerOrPayloadAwaiter = receiveTask.ConfigureAwait(false).GetAwaiter();
                            _headerOrPayloadAwaiter.UnsafeOnCompleted(_payloadReceived);
                        }
                        else
                        {
                            if (!receiveTask.GetAwaiter().GetResult())
                            {
                                SetReceiveResult(ReceiveResultType.ConnectionClose);
                            }
                            else
                            {
                                SetReceiveResult();
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    SetReceiveResult(exc);
                }
            }

            private void OnPayloadReceived()
            {
                try
                {
                    if (GetResult(ref _headerOrPayloadAwaiter))
                    {
                        SetReceiveResult();
                    }
                    else
                    {
                        SetReceiveResult(ReceiveResultType.ConnectionClose);
                    }
                }
                catch (Exception exc)
                {
                    SetReceiveResult(exc);
                }
            }

            private void OnStreamReadCompleted(bool payload)
            {
                try
                {
                    int bytesRead = GetResult(ref _streamAwaiter);
                    bool? success = payload ? OnPayloadReadCompleted(bytesRead) :
                                              OnHeaderReadCompleted(bytesRead);

                    if (success is not null)
                    {
                        _streamTaskSource.SetResult(success.GetValueOrDefault());
                    }
                }
                catch (Exception ex)
                {
                    _streamTaskSource.SetException(ex);
                }
            }

            private bool? OnHeaderReadCompleted(int bytesRead)
            {
                if (bytesRead <= 0)
                {
                    return false;
                }
                _controlBuffer.Commit(bytesRead);

                while (_controlBuffer.NeedsMoreDataForHeader(_isServer, out int byteCount))
                {
                    // More data is neeed to parse the header
                    ValueTask<int> readTask = _stream.ReadAsync(_controlBuffer.FreeMemory.Slice(0, byteCount), _cancellationToken);

                    if (!readTask.IsCompleted)
                    {
                        // It is important here that we do NOT reset the value task source
                        // because it will break the original value task that initiated this.
                        _streamAwaiter = readTask.ConfigureAwait(false).GetAwaiter();
                        _streamAwaiter.UnsafeOnCompleted(_streamReadHeaderCompleted);
                        return null;
                    }

                    bytesRead = readTask.GetAwaiter().GetResult();

                    if (bytesRead <= 0)
                    {
                        return false;
                    }
                    _controlBuffer.Commit(bytesRead);
                }

                _headerOrPayloadError = ParseMessageHeader();
                return _headerOrPayloadError is null;
            }

            private bool OnPayloadReadCompleted(int bytesRead)
            {
                if (bytesRead > 0)
                {
                    _remainingPayloadLength -= bytesRead;
                    _outputWrittenByteCount += bytesRead;

                    ApplyMask(_output.Span.Slice(0, bytesRead));

                    return true;
                }
                return false;
            }

            private ValueTask<bool> ReceiveUncompressedAsync()
            {
                ValueTask<int> readTask;

                if (_output.Length > _remainingPayloadLength)
                {
                    // We don't want to receive more than we need
                    readTask = _stream.ReadAsync(_output.Slice(0, (int)_remainingPayloadLength), _cancellationToken);
                }
                else
                {
                    readTask = _stream.ReadAsync(_output, _cancellationToken);
                }

                if (!readTask.IsCompleted)
                {
                    _streamTaskSource.Reset();
                    _streamAwaiter = readTask.ConfigureAwait(false).GetAwaiter();
                    _streamAwaiter.UnsafeOnCompleted(_streamReadPayloadCompleted);

                    return new ValueTask<bool>(this, _streamTaskSource.Version);
                }

                int bytesRead = readTask.GetAwaiter().GetResult();
                return new ValueTask<bool>(OnPayloadReadCompleted(bytesRead));
            }

            private ValueTask<bool> ReceiveHeaderAsync()
            {
                Debug.Assert(_remainingPayloadLength == 0);

                _receivedMaskOffset = 0;
                _controlBuffer.Reset();

                while (_controlBuffer.NeedsMoreDataForHeader(_isServer, out int byteCount))
                {
                    // More data is neeed to parse the header
                    ValueTask<int> readTask = _stream.ReadAsync(_controlBuffer.FreeMemory.Slice(0, byteCount), _cancellationToken);
                    if (!readTask.IsCompleted)
                    {
                        _streamTaskSource.Reset();
                        _streamAwaiter = readTask.ConfigureAwait(false).GetAwaiter();
                        _streamAwaiter.UnsafeOnCompleted(_streamReadHeaderCompleted);

                        return new ValueTask<bool>(this, _streamTaskSource.Version);
                    }

                    byteCount = readTask.GetAwaiter().GetResult();
                    if (byteCount <= 0)
                    {
                        return new ValueTask<bool>(false);
                    }
                    _controlBuffer.Commit(byteCount);
                }

                _headerOrPayloadError = ParseMessageHeader();
                return new ValueTask<bool>(_headerOrPayloadError is null);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ReceiveResult Result(int count)
            {
                bool eof = _lastFrameFin && _remainingPayloadLength == 0;

                // If this a text message, validate that it contains valid UTF8.
                if (count > 0 && _lastOpcode == MessageOpcode.Text)
                {
                    _utf8TextState ??= new Utf8MessageState();

                    if (!TryValidateUtf8(_output.Span.Slice(0, count), eof, _utf8TextState))
                    {
                        return new ReceiveResult(ReceiveResultType.InvalidPayloadData);
                    }
                }

                return new ReceiveResult(count, _lastOpcode, eof);
            }

            private void ApplyMask(Span<byte> input)
            {
                if (_isServer)
                {
                    _receivedMaskOffset = ManagedWebSocket.ApplyMask(input, _lastFrameMask, _receivedMaskOffset);
                }
            }

            /// <summary>Parses a message header from the buffer.</summary>
            private string? ParseMessageHeader()
            {
                ReadOnlySpan<byte> buffer = _controlBuffer.AvailableSpan;

                // Check first for reserved bits that should always be unset
                if ((buffer[0] & 0b0111_0000) != 0)
                {
                    return SR.net_Websockets_ReservedBitsSet;
                }
                bool fin = (buffer[0] & 0x80) != 0;
                MessageOpcode opcode = (MessageOpcode)(buffer[0] & 0xF);

                bool masked = (buffer[1] & 0x80) != 0;
                if (masked && !_isServer)
                {
                    return SR.net_Websockets_ClientReceivedMaskedFrame;
                }
                else if (_isServer && !masked)
                {
                    return SR.net_Websockets_ServerReceivedUnmaskedFrame;
                }
                long payloadLength = buffer[1] & 0x7F;

                int consumedBytes = 2;

                // Read the remainder of the payload length, if necessary
                if (payloadLength == 126)
                {
                    payloadLength = (buffer[2] << 8) | buffer[3];
                    consumedBytes = 4;
                }
                else if (payloadLength == 127)
                {
                    payloadLength = 0;
                    for (int i = 2; i < 10; ++i)
                    {
                        payloadLength = (payloadLength << 8) | buffer[i];
                    }
                    consumedBytes = 10;
                }

                if (masked)
                {
                    _lastFrameMask = BitConverter.ToInt32(buffer.Slice(consumedBytes));
                }

                // Do basic validation of the header
                switch (opcode)
                {
                    case MessageOpcode.Continuation:
                        if (_lastFrameFin)
                        {
                            // Can't continue from a final message
                            return SR.net_Websockets_ContinuationFromFinalFrame;
                        }
                        break;

                    case MessageOpcode.Binary:
                    case MessageOpcode.Text:
                        if (!_lastFrameFin)
                        {
                            // Must continue from a non-final message
                            return SR.net_Websockets_NonContinuationAfterNonFinalFrame;
                        }
                        break;

                    case MessageOpcode.Close:
                    case MessageOpcode.Ping:
                    case MessageOpcode.Pong:
                        if (payloadLength > MaxControlPayloadLength || !fin)
                        {
                            // Invalid control messgae
                            return SR.net_Websockets_InvalidControlMessage;
                        }
                        break;

                    default:
                        // Unknown opcode
                        return SR.Format(SR.net_Websockets_UnknownOpcode, opcode);
                }

                // If this is a continuation, replace the opcode with the one of the message it's continuing
                if (opcode == MessageOpcode.Continuation)
                {
                    opcode = _lastOpcode;
                }

                _lastOpcode = opcode;
                _remainingPayloadLength = payloadLength;
                _lastFrameFin = fin;

                return null;
            }

            /// <summary>
            /// Gets the result from the awaiter and resets the instance.
            /// </summary>
            private static T GetResult<T>(ref ConfiguredValueTaskAwaitable<T>.ConfiguredValueTaskAwaiter awaiter)
            {
                try
                {
                    return awaiter.GetResult();
                }
                finally
                {
                    awaiter = default;
                }
            }

            private void SetReceiveResult(Exception exc)
            {
                ResetInputState();
                _taskSource.SetException(exc);
            }

            private void SetReceiveResult(ReceiveResultType resultType)
            {
                ResetInputState();
                _taskSource.SetResult(new ReceiveResult(resultType));
            }

            private void SetReceiveResult()
            {
                ReceiveResult result = Result(_outputWrittenByteCount);
                ResetInputState();
                _taskSource.SetResult(result);
            }

            ReceiveResult IValueTaskSource<ReceiveResult>.GetResult(short token)
                => _taskSource.GetResult(token);

            ValueTaskSourceStatus IValueTaskSource<ReceiveResult>.GetStatus(short token)
                => _taskSource.GetStatus(token);

            void IValueTaskSource<ReceiveResult>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
                => _taskSource.OnCompleted(continuation, state, token, flags);

            bool IValueTaskSource<bool>.GetResult(short token) => _streamTaskSource.GetResult(token);

            ValueTaskSourceStatus IValueTaskSource<bool>.GetStatus(short token) => _streamTaskSource.GetStatus(token);

            void IValueTaskSource<bool>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
                => _streamTaskSource.OnCompleted(continuation, state, token, flags);

            [StructLayout(LayoutKind.Auto)]
            private struct ControlBuffer
            {
                private readonly byte[] _bytes;
                private int _available;

                public ControlBuffer(int capacity)
                {
                    _bytes = GC.AllocateUninitializedArray<byte>(capacity, pinned: true);
                    _available = 0;
                }

                public int AvailableLength => _available;

                public ReadOnlySpan<byte> AvailableSpan => new ReadOnlySpan<byte>(_bytes, start: 0, length: _available);

                public Memory<byte> AvailableMemory => new Memory<byte>(_bytes, start: 0, length: _available);

                public Memory<byte> FreeMemory => _bytes.AsMemory(_available);

                public void Commit(int count)
                {
                    _available += count;
                }

                public void Reset()
                {
                    _available = 0;
                }

                public bool NeedsMoreDataForHeader(bool isServer, out int byteCount)
                {
                    byteCount = isServer ? 6/*All frames from client must include a mask*/ : 2;

                    if (_available < 2)
                    {
                        return true;
                    }

                    // Make sure masked is set correctly
                    if (isServer && (_bytes[1] & 0x80) == 0)
                    {
                        // There is no need to try and receive any more because
                        // parsing of the header would fail - we've received client frame without mask
                        return false;
                    }

                    int payloadLength = _bytes[1] & 0x7F;

                    if (payloadLength == 126)
                    {
                        byteCount += 2;
                    }
                    else if (payloadLength == 127)
                    {
                        byteCount += 8;
                    }

                    if (byteCount > _available)
                    {
                        byteCount -= _available;
                        return true;
                    }

                    return false;
                }
            }
        }
    }
}
