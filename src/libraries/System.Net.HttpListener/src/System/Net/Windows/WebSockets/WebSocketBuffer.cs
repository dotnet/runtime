// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System.Net.WebSockets
{
    // This class helps to abstract the internal WebSocket buffer, which is used to interact with the native WebSocket
    // protocol component (WSPC). It helps to shield the details of the layout and the involved pointer arithmetic.
    // The internal WebSocket buffer also contains a segment, which is used by the WebSocketBase class to buffer
    // payload (parsed by WSPC already) for the application, if the application requested fewer bytes than the
    // WSPC returned. The internal buffer is pinned for the whole lifetime if this class.
    // LAYOUT:
    // | Native buffer              | PayloadReceiveBuffer | PropertyBuffer |
    // | RBS + SBS + 144            | RBS                  | PBS            |
    // | Only WSPC may modify       | Only WebSocketBase may modify         |
    //
    // *RBS = ReceiveBufferSize, *SBS = SendBufferSize
    // *PBS = PropertyBufferSize (32-bit: 16, 64 bit: 20 bytes)
    internal sealed class WebSocketBuffer : IDisposable
    {
        private const int NativeOverheadBufferSize = 144;
        private static readonly int s_PropertyBufferSize = 3 * sizeof(uint) + IntPtr.Size;

        private readonly int _receiveBufferSize;

        // Indicates the range of the pinned byte[] that can be used by the WSPC (nativeBuffer + pinnedSendBuffer)
        private readonly long _startAddress;
        private readonly long _endAddress;
        private GCHandle _gcHandle;
        private readonly ArraySegment<byte> _internalBuffer;
        private readonly ArraySegment<byte> _nativeBuffer;
        private readonly ArraySegment<byte> _payloadBuffer;
        private readonly ArraySegment<byte> _propertyBuffer;
        private readonly int _sendBufferSize;
        private volatile int _payloadOffset;
        private volatile PayloadReceiveResult? _bufferedPayloadReceiveResult;
        private long _pinnedSendBufferStartAddress;
        private long _pinnedSendBufferEndAddress;
        private ArraySegment<byte> _pinnedSendBuffer;
        private GCHandle _pinnedSendBufferHandle;
        private int _stateWhenDisposing = int.MinValue;
        private int _sendBufferState;

        private WebSocketBuffer(ArraySegment<byte> internalBuffer, int receiveBufferSize, int sendBufferSize)
        {
            Debug.Assert(internalBuffer.Array != null, "'internalBuffer.Array' MUST NOT be NULL.");
            Debug.Assert(receiveBufferSize >= HttpWebSocket.MinReceiveBufferSize, $"'receiveBufferSize' MUST be at least {HttpWebSocket.MinReceiveBufferSize}.");
            Debug.Assert(sendBufferSize >= HttpWebSocket.MinSendBufferSize, $"'sendBufferSize' MUST be at least {HttpWebSocket.MinSendBufferSize}.");
            Debug.Assert(receiveBufferSize <= HttpWebSocket.MaxBufferSize, $"'receiveBufferSize' MUST NOT exceed {HttpWebSocket.MaxBufferSize}.");
            Debug.Assert(sendBufferSize <= HttpWebSocket.MaxBufferSize, $"'sendBufferSize' MUST NOT exceed {HttpWebSocket.MaxBufferSize}.");

            _receiveBufferSize = receiveBufferSize;
            _sendBufferSize = sendBufferSize;
            _internalBuffer = internalBuffer;
            _gcHandle = GCHandle.Alloc(internalBuffer.Array, GCHandleType.Pinned);
            // Size of the internal buffer owned exclusively by the WSPC.
            int nativeBufferSize = _receiveBufferSize + _sendBufferSize + NativeOverheadBufferSize;
            _startAddress = Marshal.UnsafeAddrOfPinnedArrayElement(internalBuffer.Array, internalBuffer.Offset).ToInt64();
            _endAddress = _startAddress + nativeBufferSize;
            _nativeBuffer = new ArraySegment<byte>(internalBuffer.Array, internalBuffer.Offset, nativeBufferSize);
            _payloadBuffer = new ArraySegment<byte>(internalBuffer.Array,
                _nativeBuffer.Offset + _nativeBuffer.Count,
                _receiveBufferSize);
            _propertyBuffer = new ArraySegment<byte>(internalBuffer.Array,
                _payloadBuffer.Offset + _payloadBuffer.Count,
                s_PropertyBufferSize);
            _sendBufferState = SendBufferState.None;
        }

        public int ReceiveBufferSize
        {
            get { return _receiveBufferSize; }
        }

        public int SendBufferSize
        {
            get { return _sendBufferSize; }
        }

        internal static WebSocketBuffer CreateServerBuffer(ArraySegment<byte> internalBuffer, int receiveBufferSize)
        {
            int sendBufferSize = GetNativeSendBufferSize(HttpWebSocket.MinSendBufferSize, true);
            Debug.Assert(internalBuffer.Count >= GetInternalBufferSize(receiveBufferSize, sendBufferSize, true),
                "Array 'internalBuffer' is TOO SMALL. Call Validate before instantiating WebSocketBuffer.");

            return new WebSocketBuffer(internalBuffer, receiveBufferSize, sendBufferSize);
        }

        public void Dispose(WebSocketState webSocketState)
        {
            if (Interlocked.CompareExchange(ref _stateWhenDisposing, (int)webSocketState, int.MinValue) != int.MinValue)
            {
                return;
            }

            this.CleanUp();
        }

        public void Dispose()
        {
            this.Dispose(WebSocketState.None);
        }

        internal Interop.WebSocket.Property[] CreateProperties(bool useZeroMaskingKey)
        {
            ThrowIfDisposed();

            // serialize marshaled property values in the property segment of the internal buffer
            // m_GCHandle.AddrOfPinnedObject() points to the address of m_InternalBuffer.Array
            IntPtr internalBufferPtr = _gcHandle.AddrOfPinnedObject();
            int offset = _propertyBuffer.Offset;
            Marshal.WriteInt32(internalBufferPtr, offset, _receiveBufferSize);
            offset += sizeof(uint);
            Marshal.WriteInt32(internalBufferPtr, offset, _sendBufferSize);
            offset += sizeof(uint);
            Marshal.WriteIntPtr(internalBufferPtr, offset, internalBufferPtr + _internalBuffer.Offset);
            offset += IntPtr.Size;
            Marshal.WriteInt32(internalBufferPtr, offset, useZeroMaskingKey ? (int)1 : (int)0);

            int propertyCount = useZeroMaskingKey ? 4 : 3;
            Interop.WebSocket.Property[] properties =
                new Interop.WebSocket.Property[propertyCount];

            // Calculate the pointers to the positions of the properties within the internal buffer
            offset = _propertyBuffer.Offset;
            properties[0] = new Interop.WebSocket.Property()
            {
                Type = WebSocketProtocolComponent.PropertyType.ReceiveBufferSize,
                PropertySize = (uint)sizeof(uint),
                PropertyData = IntPtr.Add(internalBufferPtr, offset)
            };
            offset += sizeof(uint);

            properties[1] = new Interop.WebSocket.Property()
            {
                Type = WebSocketProtocolComponent.PropertyType.SendBufferSize,
                PropertySize = (uint)sizeof(uint),
                PropertyData = IntPtr.Add(internalBufferPtr, offset)
            };
            offset += sizeof(uint);

            properties[2] = new Interop.WebSocket.Property()
            {
                Type = WebSocketProtocolComponent.PropertyType.AllocatedBuffer,
                PropertySize = (uint)_nativeBuffer.Count,
                PropertyData = IntPtr.Add(internalBufferPtr, offset)
            };
            offset += IntPtr.Size;

            if (useZeroMaskingKey)
            {
                properties[3] = new Interop.WebSocket.Property()
                {
                    Type = WebSocketProtocolComponent.PropertyType.DisableMasking,
                    PropertySize = (uint)sizeof(uint),
                    PropertyData = IntPtr.Add(internalBufferPtr, offset)
                };
            }

            return properties;
        }

        // This method is not thread safe. It must only be called after enforcing at most 1 outstanding send operation
        internal void PinSendBuffer(ArraySegment<byte> payload, out bool bufferHasBeenPinned)
        {
            bufferHasBeenPinned = false;
            WebSocketValidate.ValidateBuffer(payload.Array!, payload.Offset, payload.Count);
            int previousState = Interlocked.Exchange(ref _sendBufferState, SendBufferState.SendPayloadSpecified);

            if (previousState != SendBufferState.None)
            {
                Debug.Fail("'m_SendBufferState' MUST BE 'None' at this point.");
                // Indicates a violation in the API contract that could indicate
                // memory corruption because the pinned sendbuffer is shared between managed and native code
                throw new AccessViolationException();
            }
            _pinnedSendBuffer = payload;
            _pinnedSendBufferHandle = GCHandle.Alloc(_pinnedSendBuffer.Array, GCHandleType.Pinned);
            bufferHasBeenPinned = true;
            _pinnedSendBufferStartAddress =
                Marshal.UnsafeAddrOfPinnedArrayElement(_pinnedSendBuffer.Array!, _pinnedSendBuffer.Offset).ToInt64();
            _pinnedSendBufferEndAddress = _pinnedSendBufferStartAddress + _pinnedSendBuffer.Count;
        }

        // This method is not thread safe. It must only be called after enforcing at most 1 outstanding send operation
        internal IntPtr ConvertPinnedSendPayloadToNative(ArraySegment<byte> payload)
        {
            return ConvertPinnedSendPayloadToNative(payload.Array!, payload.Offset, payload.Count);
        }

        // This method is not thread safe. It must only be called after enforcing at most 1 outstanding send operation
        internal IntPtr ConvertPinnedSendPayloadToNative(byte[] buffer, int offset, int count)
        {
            if (!IsPinnedSendPayloadBuffer(buffer, offset, count))
            {
                // Indicates a violation in the API contract that could indicate
                // memory corruption because the pinned sendbuffer is shared between managed and native code
                throw new AccessViolationException();
            }

            Debug.Assert(Marshal.UnsafeAddrOfPinnedArrayElement(_pinnedSendBuffer.Array!,
                _pinnedSendBuffer.Offset).ToInt64() == _pinnedSendBufferStartAddress,
                "'m_PinnedSendBuffer.Array' MUST be pinned during the entire send operation.");

            return new IntPtr(_pinnedSendBufferStartAddress + offset - _pinnedSendBuffer.Offset);
        }

        // This method is not thread safe. It must only be called after enforcing at most 1 outstanding send operation
        internal ArraySegment<byte> ConvertPinnedSendPayloadFromNative(Interop.WebSocket.Buffer buffer,
            WebSocketProtocolComponent.BufferType bufferType)
        {
            if (!IsPinnedSendPayloadBuffer(buffer, bufferType))
            {
                // Indicates a violation in the API contract that could indicate
                // memory corruption because the pinned sendbuffer is shared between managed and native code
                throw new AccessViolationException();
            }

            Debug.Assert(Marshal.UnsafeAddrOfPinnedArrayElement(_pinnedSendBuffer.Array!,
                _pinnedSendBuffer.Offset).ToInt64() == _pinnedSendBufferStartAddress,
                "'m_PinnedSendBuffer.Array' MUST be pinned during the entire send operation.");

            IntPtr bufferData;
            uint bufferSize;

            UnwrapWebSocketBuffer(buffer, bufferType, out bufferData, out bufferSize);

            int internalOffset = (int)(bufferData.ToInt64() - _pinnedSendBufferStartAddress);

            return new ArraySegment<byte>(_pinnedSendBuffer.Array!, _pinnedSendBuffer.Offset + internalOffset, (int)bufferSize);
        }

        // This method is not thread safe. It must only be called after enforcing at most 1 outstanding send operation
        private bool IsPinnedSendPayloadBuffer(byte[] buffer, int offset, int count)
        {
            if (_sendBufferState != SendBufferState.SendPayloadSpecified)
            {
                return false;
            }

            return object.ReferenceEquals(buffer, _pinnedSendBuffer.Array) &&
                offset >= _pinnedSendBuffer.Offset &&
                offset + count <= _pinnedSendBuffer.Offset + _pinnedSendBuffer.Count;
        }

        // This method is not thread safe. It must only be called after enforcing at most 1 outstanding send operation
        internal bool IsPinnedSendPayloadBuffer(Interop.WebSocket.Buffer buffer,
            WebSocketProtocolComponent.BufferType bufferType)
        {
            if (_sendBufferState != SendBufferState.SendPayloadSpecified)
            {
                return false;
            }

            IntPtr bufferData;
            uint bufferSize;

            UnwrapWebSocketBuffer(buffer, bufferType, out bufferData, out bufferSize);

            long nativeBufferStartAddress = bufferData.ToInt64();
            long nativeBufferEndAddress = nativeBufferStartAddress + bufferSize;

            return nativeBufferStartAddress >= _pinnedSendBufferStartAddress &&
                nativeBufferEndAddress >= _pinnedSendBufferStartAddress &&
                nativeBufferStartAddress <= _pinnedSendBufferEndAddress &&
                nativeBufferEndAddress <= _pinnedSendBufferEndAddress;
        }

        // This method is only thread safe for races between Abort and at most 1 uncompleted send operation
        internal void ReleasePinnedSendBuffer()
        {
            int previousState = Interlocked.Exchange(ref _sendBufferState, SendBufferState.None);

            if (previousState != SendBufferState.SendPayloadSpecified)
            {
                return;
            }

            if (_pinnedSendBufferHandle.IsAllocated)
            {
                _pinnedSendBufferHandle.Free();
            }

            _pinnedSendBuffer = ArraySegment<byte>.Empty;
        }

        internal void BufferPayload(ArraySegment<byte> payload,
            int unconsumedDataOffset,
            WebSocketMessageType messageType,
            bool endOfMessage)
        {
            ThrowIfDisposed();
            int bytesBuffered = payload.Count - unconsumedDataOffset;

            Debug.Assert(_payloadOffset == 0,
                "'m_PayloadOffset' MUST be '0' at this point.");
            Debug.Assert(_bufferedPayloadReceiveResult == null || _bufferedPayloadReceiveResult.Count == 0,
                "'_bufferedPayloadReceiveResult.Count' MUST be '0' at this point.");

            Buffer.BlockCopy(payload.Array!,
                payload.Offset + unconsumedDataOffset,
                _payloadBuffer.Array!,
                _payloadBuffer.Offset,
                bytesBuffered);

            _bufferedPayloadReceiveResult =
                new PayloadReceiveResult(bytesBuffered, messageType, endOfMessage);

            this.ValidateBufferedPayload();
        }

        internal bool ReceiveFromBufferedPayload(ArraySegment<byte> buffer, out WebSocketReceiveResult receiveResult)
        {
            ThrowIfDisposed();
            ValidateBufferedPayload();

            int bytesTransferred = Math.Min(buffer.Count, _bufferedPayloadReceiveResult!.Count);

            _bufferedPayloadReceiveResult.Count -= bytesTransferred;

            receiveResult = new WebSocketReceiveResult(
                bytesTransferred,
                _bufferedPayloadReceiveResult.MessageType,
                _bufferedPayloadReceiveResult.Count == 0 && _bufferedPayloadReceiveResult.EndOfMessage);

            Buffer.BlockCopy(_payloadBuffer.Array!,
                _payloadBuffer.Offset + _payloadOffset,
                buffer.Array!,
                buffer.Offset,
                bytesTransferred);

            bool morePayloadBuffered;
            if (_bufferedPayloadReceiveResult.Count == 0)
            {
                _payloadOffset = 0;
                _bufferedPayloadReceiveResult = null;
                morePayloadBuffered = false;
            }
            else
            {
                _payloadOffset += bytesTransferred;
                morePayloadBuffered = true;
                this.ValidateBufferedPayload();
            }

            return morePayloadBuffered;
        }

        internal ArraySegment<byte> ConvertNativeBuffer(WebSocketProtocolComponent.Action action,
            Interop.WebSocket.Buffer buffer,
            WebSocketProtocolComponent.BufferType bufferType)
        {
            ThrowIfDisposed();

            IntPtr bufferData;
            uint bufferLength;

            UnwrapWebSocketBuffer(buffer, bufferType, out bufferData, out bufferLength);

            if (bufferData == IntPtr.Zero)
            {
                return ArraySegment<byte>.Empty;
            }

            if (this.IsNativeBuffer(bufferData, bufferLength))
            {
                return new ArraySegment<byte>(_internalBuffer.Array!,
                    this.GetOffset(bufferData),
                    (int)bufferLength);
            }

            Debug.Fail("'buffer' MUST reference a memory segment within the pinned InternalBuffer.");
            // Indicates a violation in the contract with native Websocket.dll and could indicate
            // memory corruption because the internal buffer is shared between managed and native code
            throw new AccessViolationException();
        }

        internal void ConvertCloseBuffer(WebSocketProtocolComponent.Action action,
            Interop.WebSocket.Buffer buffer,
            out WebSocketCloseStatus closeStatus,
            out string? reason)
        {
            ThrowIfDisposed();
            IntPtr bufferData;
            uint bufferLength;
            closeStatus = (WebSocketCloseStatus)buffer.CloseStatus.CloseStatus;

            UnwrapWebSocketBuffer(buffer, WebSocketProtocolComponent.BufferType.Close, out bufferData, out bufferLength);

            if (bufferData == IntPtr.Zero)
            {
                reason = null;
            }
            else
            {
                ArraySegment<byte> reasonBlob;
                if (this.IsNativeBuffer(bufferData, bufferLength))
                {
                    reasonBlob = new ArraySegment<byte>(_internalBuffer.Array!,
                        this.GetOffset(bufferData),
                        (int)bufferLength);
                }
                else
                {
                    Debug.Fail("'buffer' MUST reference a memory segment within the pinned InternalBuffer.");
                    // Indicates a violation in the contract with native Websocket.dll and could indicate
                    // memory corruption because the internal buffer is shared between managed and native code
                    throw new AccessViolationException();
                }

                // No need to wrap DecoderFallbackException for invalid UTF8 chacters, because
                // Encoding.UTF8 will not throw but replace invalid characters instead.
                reason = Encoding.UTF8.GetString(reasonBlob.Array!, reasonBlob.Offset, reasonBlob.Count);
            }
        }


        internal void ValidateNativeBuffers(WebSocketProtocolComponent.Action action,
            WebSocketProtocolComponent.BufferType bufferType,
            Interop.WebSocket.Buffer[] dataBuffers,
            uint dataBufferCount)
        {
            Debug.Assert(dataBufferCount <= (uint)int.MaxValue,
                "'dataBufferCount' MUST NOT be bigger than Int32.MaxValue.");
            Debug.Assert(dataBuffers != null, "'dataBuffers' MUST NOT be NULL.");

            ThrowIfDisposed();
            if (dataBufferCount > dataBuffers.Length)
            {
                Debug.Fail("'dataBufferCount' MUST NOT be bigger than 'dataBuffers.Length'.");
                // Indicates a violation in the contract with native Websocket.dll and could indicate
                // memory corruption because the internal buffer is shared between managed and native code
                throw new AccessViolationException();
            }

            int count = dataBuffers.Length;
            bool isSendActivity = action == WebSocketProtocolComponent.Action.IndicateSendComplete ||
                action == WebSocketProtocolComponent.Action.SendToNetwork;

            if (isSendActivity)
            {
                count = (int)dataBufferCount;
            }

            bool nonZeroBufferFound = false;
            for (int i = 0; i < count; i++)
            {
                Interop.WebSocket.Buffer dataBuffer = dataBuffers[i];

                IntPtr bufferData;
                uint bufferLength;
                UnwrapWebSocketBuffer(dataBuffer, bufferType, out bufferData, out bufferLength);

                if (bufferData == IntPtr.Zero)
                {
                    continue;
                }

                nonZeroBufferFound = true;

                bool isPinnedSendPayloadBuffer = IsPinnedSendPayloadBuffer(dataBuffer, bufferType);

                if (bufferLength > GetMaxBufferSize())
                {
                    if (!isSendActivity || !isPinnedSendPayloadBuffer)
                    {
                        Debug.Fail("'dataBuffer.BufferLength' MUST NOT be bigger than 'm_ReceiveBufferSize' and 'm_SendBufferSize'.");
                        // Indicates a violation in the contract with native Websocket.dll and could indicate
                        // memory corruption because the internal buffer is shared between managed and native code
                        throw new AccessViolationException();
                    }
                }

                if (!isPinnedSendPayloadBuffer && !IsNativeBuffer(bufferData, bufferLength))
                {
                    Debug.Fail("WebSocketGetAction MUST return a pointer within the pinned internal buffer.");
                    // Indicates a violation in the contract with native Websocket.dll and could indicate
                    // memory corruption because the internal buffer is shared between managed and native code
                    throw new AccessViolationException();
                }
            }

            if (!nonZeroBufferFound &&
                action != WebSocketProtocolComponent.Action.NoAction &&
                action != WebSocketProtocolComponent.Action.IndicateReceiveComplete &&
                action != WebSocketProtocolComponent.Action.IndicateSendComplete)
            {
                Debug.Fail("At least one 'dataBuffer.Buffer' MUST NOT be NULL.");
            }
        }

        private static int GetNativeSendBufferSize(int sendBufferSize, bool isServerBuffer)
        {
            return isServerBuffer ? HttpWebSocket.MinSendBufferSize : sendBufferSize;
        }

        internal static void UnwrapWebSocketBuffer(Interop.WebSocket.Buffer buffer,
            WebSocketProtocolComponent.BufferType bufferType,
            out IntPtr bufferData,
            out uint bufferLength)
        {
            bufferData = IntPtr.Zero;
            bufferLength = 0;

            switch (bufferType)
            {
                case WebSocketProtocolComponent.BufferType.Close:
                    bufferData = buffer.CloseStatus.ReasonData;
                    bufferLength = buffer.CloseStatus.ReasonLength;
                    break;
                case WebSocketProtocolComponent.BufferType.None:
                case WebSocketProtocolComponent.BufferType.BinaryFragment:
                case WebSocketProtocolComponent.BufferType.BinaryMessage:
                case WebSocketProtocolComponent.BufferType.UTF8Fragment:
                case WebSocketProtocolComponent.BufferType.UTF8Message:
                case WebSocketProtocolComponent.BufferType.PingPong:
                case WebSocketProtocolComponent.BufferType.UnsolicitedPong:
                    bufferData = buffer.Data.BufferData;
                    bufferLength = buffer.Data.BufferLength;
                    break;
                default:
                    Debug.Fail($"BufferType '{bufferType}' is invalid/unknown.");
                    break;
            }
        }

        private void ThrowIfDisposed()
        {
            switch (_stateWhenDisposing)
            {
                case int.MinValue:
                    return;
                case (int)WebSocketState.Closed:
                case (int)WebSocketState.Aborted:
                    throw new WebSocketException(WebSocketError.InvalidState,
                        SR.Format(SR.net_WebSockets_InvalidState_ClosedOrAborted, typeof(WebSocketBase), _stateWhenDisposing));
                default:
                    throw new ObjectDisposedException(GetType().FullName);
            }
        }

        [Conditional("DEBUG")]
        private void ValidateBufferedPayload()
        {
            Debug.Assert(_bufferedPayloadReceiveResult != null,
                "'_bufferedPayloadReceiveResult' MUST NOT be NULL.");
            Debug.Assert(_bufferedPayloadReceiveResult.Count >= 0,
                "'_bufferedPayloadReceiveResult.Count' MUST NOT be negative.");
            Debug.Assert(_payloadOffset >= 0, "'m_PayloadOffset' MUST NOT be smaller than 0.");
            Debug.Assert(_payloadOffset <= _payloadBuffer.Count,
                "'m_PayloadOffset' MUST NOT be bigger than 'm_PayloadBuffer.Count'.");
            Debug.Assert(_payloadOffset + _bufferedPayloadReceiveResult.Count <= _payloadBuffer.Count,
                "'m_PayloadOffset + m_PayloadBytesBuffered' MUST NOT be bigger than 'm_PayloadBuffer.Count'.");
        }

        private int GetOffset(IntPtr pBuffer)
        {
            Debug.Assert(pBuffer != IntPtr.Zero, "'pBuffer' MUST NOT be IntPtr.Zero.");
            int offset = (int)(pBuffer.ToInt64() - _startAddress + _internalBuffer.Offset);

            Debug.Assert(offset >= 0, "'offset' MUST NOT be negative.");
            return offset;
        }

        private int GetMaxBufferSize()
        {
            return Math.Max(_receiveBufferSize, _sendBufferSize);
        }

        // This method is actually checking whether the array "buffer" is located
        // within m_NativeBuffer not m_InternalBuffer
        internal bool IsInternalBuffer(byte[] buffer, int offset, int count)
        {
            Debug.Assert(buffer != null, "'buffer' MUST NOT be NULL.");
            Debug.Assert(_nativeBuffer.Array != null, "'m_NativeBuffer.Array' MUST NOT be NULL.");
            Debug.Assert(offset >= 0, "'offset' MUST NOT be negative.");
            Debug.Assert(count >= 0, "'count' MUST NOT be negative.");
            Debug.Assert(offset + count <= buffer.Length, "'offset + count' MUST NOT exceed 'buffer.Length'.");

            return object.ReferenceEquals(buffer, _nativeBuffer.Array) &&
                offset >= _nativeBuffer.Offset &&
                offset + count <= _nativeBuffer.Offset + _nativeBuffer.Count;
        }

        internal IntPtr ToIntPtr(int offset)
        {
            Debug.Assert(offset >= 0, "'offset' MUST NOT be negative.");
            Debug.Assert(_startAddress + offset - _internalBuffer.Offset <= _endAddress, "'offset' is TOO BIG.");

            return new IntPtr(_startAddress + offset - _internalBuffer.Offset);
        }

        private bool IsNativeBuffer(IntPtr pBuffer, uint bufferSize)
        {
            Debug.Assert(pBuffer != IntPtr.Zero, "'pBuffer' MUST NOT be NULL.");
            Debug.Assert(bufferSize <= GetMaxBufferSize(),
                "'bufferSize' MUST NOT be bigger than 'm_ReceiveBufferSize' and 'm_SendBufferSize'.");

            long nativeBufferStartAddress = pBuffer.ToInt64();
            long nativeBufferEndAddress = bufferSize + nativeBufferStartAddress;

            Debug.Assert(Marshal.UnsafeAddrOfPinnedArrayElement(_internalBuffer.Array!, _internalBuffer.Offset).ToInt64() == _startAddress,
                "'m_InternalBuffer.Array' MUST be pinned for the whole lifetime of a WebSocket.");

            if (nativeBufferStartAddress >= _startAddress &&
                nativeBufferStartAddress <= _endAddress &&
                nativeBufferEndAddress >= _startAddress &&
                nativeBufferEndAddress <= _endAddress)
            {
                return true;
            }

            return false;
        }

        private void CleanUp()
        {
            if (_gcHandle.IsAllocated)
            {
                _gcHandle.Free();
            }

            ReleasePinnedSendBuffer();
        }

        internal static ArraySegment<byte> CreateInternalBufferArraySegment(int receiveBufferSize, int sendBufferSize, bool isServerBuffer)
        {
            Debug.Assert(receiveBufferSize >= HttpWebSocket.MinReceiveBufferSize, $"'receiveBufferSize' MUST be at least {HttpWebSocket.MinReceiveBufferSize}.");
            Debug.Assert(sendBufferSize >= HttpWebSocket.MinSendBufferSize, $"'sendBufferSize' MUST be at least {HttpWebSocket.MinSendBufferSize}.");

            int internalBufferSize = GetInternalBufferSize(receiveBufferSize, sendBufferSize, isServerBuffer);
            return new ArraySegment<byte>(new byte[internalBufferSize]);
        }

        internal static void Validate(int count, int receiveBufferSize, int sendBufferSize, bool isServerBuffer)
        {
            Debug.Assert(receiveBufferSize >= HttpWebSocket.MinReceiveBufferSize, $"'receiveBufferSize' MUST be at least {HttpWebSocket.MinReceiveBufferSize}.");
            Debug.Assert(sendBufferSize >= HttpWebSocket.MinSendBufferSize, $"'sendBufferSize' MUST be at least {HttpWebSocket.MinSendBufferSize}.");

            int minBufferSize = GetInternalBufferSize(receiveBufferSize, sendBufferSize, isServerBuffer);
            if (count < minBufferSize)
            {
                throw new ArgumentOutOfRangeException("internalBuffer",
                    SR.Format(SR.net_WebSockets_ArgumentOutOfRange_InternalBuffer, minBufferSize));
            }
        }

        private static int GetInternalBufferSize(int receiveBufferSize, int sendBufferSize, bool isServerBuffer)
        {
            Debug.Assert(receiveBufferSize >= HttpWebSocket.MinReceiveBufferSize, $"'receiveBufferSize' MUST be at least {HttpWebSocket.MinReceiveBufferSize}.");
            Debug.Assert(sendBufferSize >= HttpWebSocket.MinSendBufferSize, $"'sendBufferSize' MUST be at least {HttpWebSocket.MinSendBufferSize}.");

            Debug.Assert(receiveBufferSize <= HttpWebSocket.MaxBufferSize, $"'receiveBufferSize' MUST be less than or equal to {HttpWebSocket.MaxBufferSize}.");
            Debug.Assert(sendBufferSize <= HttpWebSocket.MaxBufferSize, $"'sendBufferSize' MUST be at less than or equal to {HttpWebSocket.MaxBufferSize}.");

            int nativeSendBufferSize = GetNativeSendBufferSize(sendBufferSize, isServerBuffer);
            return 2 * receiveBufferSize + nativeSendBufferSize + NativeOverheadBufferSize + s_PropertyBufferSize;
        }

        private static class SendBufferState
        {
            public const int None = 0;
            public const int SendPayloadSpecified = 1;
        }

        private sealed class PayloadReceiveResult
        {
            public int Count { get; set; }
            public bool EndOfMessage { get; }
            public WebSocketMessageType MessageType { get; }

            public PayloadReceiveResult(int count, WebSocketMessageType messageType, bool endOfMessage)
            {
                if (count < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                Count = count;
                EndOfMessage = endOfMessage;
                MessageType = messageType;
            }
        }
    }
}
