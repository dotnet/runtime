// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    internal class WebSocketBuffer : IDisposable
    {
        private const int NativeOverheadBufferSize = 144;
        internal const int MinSendBufferSize = 16;
        internal const int MinReceiveBufferSize = 256;
        internal const int MaxBufferSize = 64 * 1024;
        private static readonly int s_SizeOfUInt = Marshal.SizeOf(typeof(uint));
        private static readonly int s_SizeOfBool = Marshal.SizeOf(typeof(bool));
        private static readonly int s_PropertyBufferSize = 2 * s_SizeOfUInt + s_SizeOfBool + IntPtr.Size;

        private readonly int m_ReceiveBufferSize;

        // Indicates the range of the pinned byte[] that can be used by the WSPC (nativeBuffer + pinnedSendBuffer)
        private readonly long m_StartAddress;
        private readonly long m_EndAddress;
        private readonly GCHandle m_GCHandle;
        private readonly ArraySegment<byte> m_InternalBuffer;
        private readonly ArraySegment<byte> m_NativeBuffer;
        private readonly ArraySegment<byte> m_PayloadBuffer;
        private readonly ArraySegment<byte> m_PropertyBuffer;
        private readonly int m_SendBufferSize;
        private volatile int m_PayloadOffset;
        private volatile WebSocketReceiveResult m_BufferedPayloadReceiveResult;
        private long m_PinnedSendBufferStartAddress;
        private long m_PinnedSendBufferEndAddress;
        private ArraySegment<byte> m_PinnedSendBuffer;
        private GCHandle m_PinnedSendBufferHandle;
        private int m_StateWhenDisposing = int.MinValue;
        private int m_SendBufferState;

        private WebSocketBuffer(ArraySegment<byte> internalBuffer, int receiveBufferSize, int sendBufferSize)
        {
            Debug.Assert(internalBuffer != null, "'internalBuffer' MUST NOT be NULL.");
            Debug.Assert(receiveBufferSize >= MinReceiveBufferSize,
                "'receiveBufferSize' MUST be at least " + MinReceiveBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");
            Debug.Assert(sendBufferSize >= MinSendBufferSize,
                "'sendBufferSize' MUST be at least " + MinSendBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");
            Debug.Assert(receiveBufferSize <= MaxBufferSize,
                "'receiveBufferSize' MUST NOT exceed " + MaxBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");
            Debug.Assert(sendBufferSize <= MaxBufferSize,
                "'sendBufferSize' MUST NOT exceed  " + MaxBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");

            m_ReceiveBufferSize = receiveBufferSize;
            m_SendBufferSize = sendBufferSize;
            m_InternalBuffer = internalBuffer;
            m_GCHandle = GCHandle.Alloc(internalBuffer.Array, GCHandleType.Pinned);
            // Size of the internal buffer owned exclusively by the WSPC.
            int nativeBufferSize = m_ReceiveBufferSize + m_SendBufferSize + NativeOverheadBufferSize;
            m_StartAddress = Marshal.UnsafeAddrOfPinnedArrayElement(internalBuffer.Array, internalBuffer.Offset).ToInt64();
            m_EndAddress = m_StartAddress + nativeBufferSize;
            m_NativeBuffer = new ArraySegment<byte>(internalBuffer.Array, internalBuffer.Offset, nativeBufferSize);
            m_PayloadBuffer = new ArraySegment<byte>(internalBuffer.Array,
                m_NativeBuffer.Offset + m_NativeBuffer.Count,
                m_ReceiveBufferSize);
            m_PropertyBuffer = new ArraySegment<byte>(internalBuffer.Array,
                m_PayloadBuffer.Offset + m_PayloadBuffer.Count,
                s_PropertyBufferSize);
            m_SendBufferState = SendBufferState.None;
        }

        public int ReceiveBufferSize
        {
            get { return m_ReceiveBufferSize; }
        }

        public int SendBufferSize
        {
            get { return m_SendBufferSize; }
        }

        internal static WebSocketBuffer CreateServerBuffer(ArraySegment<byte> internalBuffer, int receiveBufferSize)
        {
            int sendBufferSize = GetNativeSendBufferSize(MinSendBufferSize, true);
            Debug.Assert(internalBuffer.Count >= GetInternalBufferSize(receiveBufferSize, sendBufferSize, true),
                "Array 'internalBuffer' is TOO SMALL. Call Validate before instantiating WebSocketBuffer.");

            return new WebSocketBuffer(internalBuffer, receiveBufferSize, sendBufferSize);
        }

        public void Dispose(WebSocketState webSocketState)
        {
            if (Interlocked.CompareExchange(ref m_StateWhenDisposing, (int)webSocketState, int.MinValue) != int.MinValue)
            {
                return;
            }

            this.CleanUp();
        }

        public void Dispose()
        {
            this.Dispose(WebSocketState.None);
        }

        internal WebSocketProtocolComponent.Property[] CreateProperties(bool useZeroMaskingKey)
        {
            ThrowIfDisposed();

            // serialize marshaled property values in the property segment of the internal buffer
            // m_GCHandle.AddrOfPinnedObject() points to the address of m_InternalBuffer.Array
            IntPtr internalBufferPtr = m_GCHandle.AddrOfPinnedObject();
            int offset = m_PropertyBuffer.Offset;
            Marshal.WriteInt32(internalBufferPtr, offset, m_ReceiveBufferSize);
            offset += s_SizeOfUInt;
            Marshal.WriteInt32(internalBufferPtr, offset, m_SendBufferSize);
            offset += s_SizeOfUInt;
            Marshal.WriteIntPtr(internalBufferPtr, offset, internalBufferPtr + m_InternalBuffer.Offset);
            offset += IntPtr.Size;
            Marshal.WriteInt32(internalBufferPtr, offset, useZeroMaskingKey ? (int)1 : (int)0);

            int propertyCount = useZeroMaskingKey ? 4 : 3;
            WebSocketProtocolComponent.Property[] properties =
                new WebSocketProtocolComponent.Property[propertyCount];

            // Calculate the pointers to the positions of the properties within the internal buffer
            offset = m_PropertyBuffer.Offset;
            properties[0] = new WebSocketProtocolComponent.Property()
            {
                Type = WebSocketProtocolComponent.PropertyType.ReceiveBufferSize,
                PropertySize = (uint)s_SizeOfUInt,
                PropertyData = IntPtr.Add(internalBufferPtr, offset)
            };
            offset += s_SizeOfUInt;

            properties[1] = new WebSocketProtocolComponent.Property()
            {
                Type = WebSocketProtocolComponent.PropertyType.SendBufferSize,
                PropertySize = (uint)s_SizeOfUInt,
                PropertyData = IntPtr.Add(internalBufferPtr, offset)
            };
            offset += s_SizeOfUInt;

            properties[2] = new WebSocketProtocolComponent.Property()
            {
                Type = WebSocketProtocolComponent.PropertyType.AllocatedBuffer,
                PropertySize = (uint)m_NativeBuffer.Count,
                PropertyData = IntPtr.Add(internalBufferPtr, offset)
            };
            offset += IntPtr.Size;

            if (useZeroMaskingKey)
            {
                properties[3] = new WebSocketProtocolComponent.Property()
                {
                    Type = WebSocketProtocolComponent.PropertyType.DisableMasking,
                    PropertySize = (uint)s_SizeOfBool,
                    PropertyData = IntPtr.Add(internalBufferPtr, offset)
                };
            }

            return properties;
        }

        // This method is not thread safe. It must only be called after enforcing at most 1 outstanding send operation
        internal void PinSendBuffer(ArraySegment<byte> payload, out bool bufferHasBeenPinned)
        {
            bufferHasBeenPinned = false;
            WebSocketHelpers.ValidateBuffer(payload.Array, payload.Offset, payload.Count);
            int previousState = Interlocked.Exchange(ref m_SendBufferState, SendBufferState.SendPayloadSpecified);

            if (previousState != SendBufferState.None)
            {
                Debug.Assert(false, "'m_SendBufferState' MUST BE 'None' at this point.");
                // Indicates a violation in the API contract that could indicate 
                // memory corruption because the pinned sendbuffer is shared between managed and native code
                throw new AccessViolationException();
            }
            m_PinnedSendBuffer = payload;
            m_PinnedSendBufferHandle = GCHandle.Alloc(m_PinnedSendBuffer.Array, GCHandleType.Pinned);
            bufferHasBeenPinned = true;
            m_PinnedSendBufferStartAddress =
                Marshal.UnsafeAddrOfPinnedArrayElement(m_PinnedSendBuffer.Array, m_PinnedSendBuffer.Offset).ToInt64();
            m_PinnedSendBufferEndAddress = m_PinnedSendBufferStartAddress + m_PinnedSendBuffer.Count;
        }

        // This method is not thread safe. It must only be called after enforcing at most 1 outstanding send operation
        internal IntPtr ConvertPinnedSendPayloadToNative(ArraySegment<byte> payload)
        {
            return ConvertPinnedSendPayloadToNative(payload.Array, payload.Offset, payload.Count);
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

            Debug.Assert(Marshal.UnsafeAddrOfPinnedArrayElement(m_PinnedSendBuffer.Array,
                m_PinnedSendBuffer.Offset).ToInt64() == m_PinnedSendBufferStartAddress,
                "'m_PinnedSendBuffer.Array' MUST be pinned during the entire send operation.");

            return new IntPtr(m_PinnedSendBufferStartAddress + offset - m_PinnedSendBuffer.Offset);
        }

        // This method is not thread safe. It must only be called after enforcing at most 1 outstanding send operation
        internal ArraySegment<byte> ConvertPinnedSendPayloadFromNative(WebSocketProtocolComponent.Buffer buffer,
            WebSocketProtocolComponent.BufferType bufferType)
        {
            if (!IsPinnedSendPayloadBuffer(buffer, bufferType))
            {
                // Indicates a violation in the API contract that could indicate 
                // memory corruption because the pinned sendbuffer is shared between managed and native code
                throw new AccessViolationException();
            }

            Debug.Assert(Marshal.UnsafeAddrOfPinnedArrayElement(m_PinnedSendBuffer.Array,
                m_PinnedSendBuffer.Offset).ToInt64() == m_PinnedSendBufferStartAddress,
                "'m_PinnedSendBuffer.Array' MUST be pinned during the entire send operation.");

            IntPtr bufferData;
            uint bufferSize;

            UnwrapWebSocketBuffer(buffer, bufferType, out bufferData, out bufferSize);

            int internalOffset = (int)(bufferData.ToInt64() - m_PinnedSendBufferStartAddress);

            return new ArraySegment<byte>(m_PinnedSendBuffer.Array, m_PinnedSendBuffer.Offset + internalOffset, (int)bufferSize);
        }

        // This method is not thread safe. It must only be called after enforcing at most 1 outstanding send operation
        private bool IsPinnedSendPayloadBuffer(byte[] buffer, int offset, int count)
        {
            if (m_SendBufferState != SendBufferState.SendPayloadSpecified)
            {
                return false;
            }

            return object.ReferenceEquals(buffer, m_PinnedSendBuffer.Array) &&
                offset >= m_PinnedSendBuffer.Offset &&
                offset + count <= m_PinnedSendBuffer.Offset + m_PinnedSendBuffer.Count;
        }

        // This method is not thread safe. It must only be called after enforcing at most 1 outstanding send operation
        internal bool IsPinnedSendPayloadBuffer(WebSocketProtocolComponent.Buffer buffer,
            WebSocketProtocolComponent.BufferType bufferType)
        {
            if (m_SendBufferState != SendBufferState.SendPayloadSpecified)
            {
                return false;
            }

            IntPtr bufferData;
            uint bufferSize;

            UnwrapWebSocketBuffer(buffer, bufferType, out bufferData, out bufferSize);

            long nativeBufferStartAddress = bufferData.ToInt64();
            long nativeBufferEndAddress = nativeBufferStartAddress + bufferSize;

            return nativeBufferStartAddress >= m_PinnedSendBufferStartAddress &&
                nativeBufferEndAddress >= m_PinnedSendBufferStartAddress &&
                nativeBufferStartAddress <= m_PinnedSendBufferEndAddress &&
                nativeBufferEndAddress <= m_PinnedSendBufferEndAddress;
        }

        // This method is only thread safe for races between Abort and at most 1 uncompleted send operation
        internal void ReleasePinnedSendBuffer()
        {
            int previousState = Interlocked.Exchange(ref m_SendBufferState, SendBufferState.None);

            if (previousState != SendBufferState.SendPayloadSpecified)
            {
                return;
            }

            if (m_PinnedSendBufferHandle.IsAllocated)
            {
                m_PinnedSendBufferHandle.Free();
            }

            m_PinnedSendBuffer = WebSocketHelpers.EmptyPayload;
        }

        internal void BufferPayload(ArraySegment<byte> payload,
            int unconsumedDataOffset,
            WebSocketMessageType messageType,
            bool endOfMessage)
        {
            ThrowIfDisposed();
            int bytesBuffered = payload.Count - unconsumedDataOffset;

            Debug.Assert(m_PayloadOffset == 0,
                "'m_PayloadOffset' MUST be '0' at this point.");
            Debug.Assert(m_BufferedPayloadReceiveResult == null || m_BufferedPayloadReceiveResult.Count == 0,
                "'m_BufferedPayloadReceiveResult.Count' MUST be '0' at this point.");

            Buffer.BlockCopy(payload.Array,
                payload.Offset + unconsumedDataOffset,
                m_PayloadBuffer.Array,
                m_PayloadBuffer.Offset,
                bytesBuffered);

            m_BufferedPayloadReceiveResult =
                new WebSocketReceiveResult(bytesBuffered, messageType, endOfMessage);

            this.ValidateBufferedPayload();
        }

        internal bool ReceiveFromBufferedPayload(ArraySegment<byte> buffer, out WebSocketReceiveResult receiveResult)
        {
            ThrowIfDisposed();
            ValidateBufferedPayload();

            int bytesTransferred = Math.Min(buffer.Count, m_BufferedPayloadReceiveResult.Count);

            receiveResult = new WebSocketReceiveResult(
                bytesTransferred,
                m_BufferedPayloadReceiveResult.MessageType,
                bytesTransferred == 0 && m_BufferedPayloadReceiveResult.EndOfMessage,
                m_BufferedPayloadReceiveResult.CloseStatus,
                m_BufferedPayloadReceiveResult.CloseStatusDescription);

            Buffer.BlockCopy(m_PayloadBuffer.Array,
                m_PayloadBuffer.Offset + m_PayloadOffset,
                buffer.Array,
                buffer.Offset,
                bytesTransferred);

            bool morePayloadBuffered;
            if (m_BufferedPayloadReceiveResult.Count == 0)
            {
                m_PayloadOffset = 0;
                m_BufferedPayloadReceiveResult = null;
                morePayloadBuffered = false;
            }
            else
            {
                m_PayloadOffset += bytesTransferred;
                morePayloadBuffered = true;
                this.ValidateBufferedPayload();
            }

            return morePayloadBuffered;
        }

        internal ArraySegment<byte> ConvertNativeBuffer(WebSocketProtocolComponent.Action action,
            WebSocketProtocolComponent.Buffer buffer,
            WebSocketProtocolComponent.BufferType bufferType)
        {
            ThrowIfDisposed();

            IntPtr bufferData;
            uint bufferLength;

            UnwrapWebSocketBuffer(buffer, bufferType, out bufferData, out bufferLength);

            if (bufferData == IntPtr.Zero)
            {
                return WebSocketHelpers.EmptyPayload;
            }

            if (this.IsNativeBuffer(bufferData, bufferLength))
            {
                return new ArraySegment<byte>(m_InternalBuffer.Array,
                    this.GetOffset(bufferData),
                    (int)bufferLength);
            }

            Debug.Assert(false, "'buffer' MUST reference a memory segment within the pinned InternalBuffer.");
            // Indicates a violation in the contract with native Websocket.dll and could indicate 
            // memory corruption because the internal buffer is shared between managed and native code
            throw new AccessViolationException();
        }

        internal void ConvertCloseBuffer(WebSocketProtocolComponent.Action action,
            WebSocketProtocolComponent.Buffer buffer,
            out WebSocketCloseStatus closeStatus,
            out string reason)
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
                    reasonBlob = new ArraySegment<byte>(m_InternalBuffer.Array,
                        this.GetOffset(bufferData),
                        (int)bufferLength);
                }
                else
                {
                    Debug.Assert(false, "'buffer' MUST reference a memory segment within the pinned InternalBuffer.");
                    // Indicates a violation in the contract with native Websocket.dll and could indicate 
                    // memory corruption because the internal buffer is shared between managed and native code
                    throw new AccessViolationException();
                }

                // No need to wrap DecoderFallbackException for invalid UTF8 chacters, because
                // Encoding.UTF8 will not throw but replace invalid characters instead.
                reason = Encoding.UTF8.GetString(reasonBlob.Array, reasonBlob.Offset, reasonBlob.Count);
            }
        }


        internal void ValidateNativeBuffers(WebSocketProtocolComponent.Action action,
            WebSocketProtocolComponent.BufferType bufferType,
            WebSocketProtocolComponent.Buffer[] dataBuffers,
            uint dataBufferCount)
        {
            Debug.Assert(dataBufferCount <= (uint)int.MaxValue,
                "'dataBufferCount' MUST NOT be bigger than Int32.MaxValue.");
            Debug.Assert(dataBuffers != null, "'dataBuffers' MUST NOT be NULL.");

            ThrowIfDisposed();
            if (dataBufferCount > dataBuffers.Length)
            {
                Debug.Assert(false, "'dataBufferCount' MUST NOT be bigger than 'dataBuffers.Length'.");
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
                WebSocketProtocolComponent.Buffer dataBuffer = dataBuffers[i];

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
                        Debug.Assert(false,
                        "'dataBuffer.BufferLength' MUST NOT be bigger than 'm_ReceiveBufferSize' and 'm_SendBufferSize'.");
                        // Indicates a violation in the contract with native Websocket.dll and could indicate 
                        // memory corruption because the internal buffer is shared between managed and native code
                        throw new AccessViolationException();
                    }
                }

                if (!isPinnedSendPayloadBuffer && !IsNativeBuffer(bufferData, bufferLength))
                {
                    Debug.Assert(false,
                        "WebSocketGetAction MUST return a pointer within the pinned internal buffer.");
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
                Debug.Assert(false, "At least one 'dataBuffer.Buffer' MUST NOT be NULL.");
            }
        }

        private static int GetNativeSendBufferSize(int sendBufferSize, bool isServerBuffer)
        {
            return isServerBuffer ? MinSendBufferSize : sendBufferSize;
        }

        internal static void UnwrapWebSocketBuffer(WebSocketProtocolComponent.Buffer buffer,
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
                    Debug.Assert(false,
                        string.Format(CultureInfo.InvariantCulture,
                            "BufferType '{0}' is invalid/unknown.",
                            bufferType));
                    break;
            }
        }

        private void ThrowIfDisposed()
        {
            switch (m_StateWhenDisposing)
            {
                case int.MinValue:
                    return;
                case (int)WebSocketState.Closed:
                case (int)WebSocketState.Aborted:
                    throw new WebSocketException(WebSocketError.InvalidState,
                        SR.Format(SR.net_WebSockets_InvalidState_ClosedOrAborted, typeof(WebSocketBase), m_StateWhenDisposing));
                default:
                    throw new ObjectDisposedException(GetType().FullName);
            }
        }

        [Conditional("DEBUG")]
        private void ValidateBufferedPayload()
        {
            Debug.Assert(m_BufferedPayloadReceiveResult != null,
                "'m_BufferedPayloadReceiveResult' MUST NOT be NULL.");
            Debug.Assert(m_BufferedPayloadReceiveResult.Count >= 0,
                "'m_BufferedPayloadReceiveResult.Count' MUST NOT be negative.");
            Debug.Assert(m_PayloadOffset >= 0, "'m_PayloadOffset' MUST NOT be smaller than 0.");
            Debug.Assert(m_PayloadOffset <= m_PayloadBuffer.Count,
                "'m_PayloadOffset' MUST NOT be bigger than 'm_PayloadBuffer.Count'.");
            Debug.Assert(m_PayloadOffset + m_BufferedPayloadReceiveResult.Count <= m_PayloadBuffer.Count,
                "'m_PayloadOffset + m_PayloadBytesBuffered' MUST NOT be bigger than 'm_PayloadBuffer.Count'.");
        }

        private int GetOffset(IntPtr pBuffer)
        {
            Debug.Assert(pBuffer != IntPtr.Zero, "'pBuffer' MUST NOT be IntPtr.Zero.");
            int offset = (int)(pBuffer.ToInt64() - m_StartAddress + m_InternalBuffer.Offset);

            Debug.Assert(offset >= 0, "'offset' MUST NOT be negative.");
            return offset;
        }

        private int GetMaxBufferSize()
        {
            return Math.Max(m_ReceiveBufferSize, m_SendBufferSize);
        }

        // This method is actually checking whether the array "buffer" is located
        // within m_NativeBuffer not m_InternalBuffer
        internal bool IsInternalBuffer(byte[] buffer, int offset, int count)
        {
            Debug.Assert(buffer != null, "'buffer' MUST NOT be NULL.");
            Debug.Assert(m_NativeBuffer.Array != null, "'m_NativeBuffer.Array' MUST NOT be NULL.");
            Debug.Assert(offset >= 0, "'offset' MUST NOT be negative.");
            Debug.Assert(count >= 0, "'count' MUST NOT be negative.");
            Debug.Assert(offset + count <= buffer.Length, "'offset + count' MUST NOT exceed 'buffer.Length'.");

            return object.ReferenceEquals(buffer, m_NativeBuffer.Array) &&
                offset >= m_NativeBuffer.Offset &&
                offset + count <= m_NativeBuffer.Offset + m_NativeBuffer.Count;
        }

        internal IntPtr ToIntPtr(int offset)
        {
            Debug.Assert(offset >= 0, "'offset' MUST NOT be negative.");
            Debug.Assert(m_StartAddress + offset - m_InternalBuffer.Offset <= m_EndAddress, "'offset' is TOO BIG.");

            return new IntPtr(m_StartAddress + offset - m_InternalBuffer.Offset);
        }

        private bool IsNativeBuffer(IntPtr pBuffer, uint bufferSize)
        {
            Debug.Assert(pBuffer != IntPtr.Zero, "'pBuffer' MUST NOT be NULL.");
            Debug.Assert(bufferSize <= GetMaxBufferSize(),
                "'bufferSize' MUST NOT be bigger than 'm_ReceiveBufferSize' and 'm_SendBufferSize'.");

            long nativeBufferStartAddress = pBuffer.ToInt64();
            long nativeBufferEndAddress = bufferSize + nativeBufferStartAddress;

            Debug.Assert(Marshal.UnsafeAddrOfPinnedArrayElement(m_InternalBuffer.Array, m_InternalBuffer.Offset).ToInt64() == m_StartAddress,
                "'m_InternalBuffer.Array' MUST be pinned for the whole lifetime of a WebSocket.");

            if (nativeBufferStartAddress >= m_StartAddress &&
                nativeBufferStartAddress <= m_EndAddress &&
                nativeBufferEndAddress >= m_StartAddress &&
                nativeBufferEndAddress <= m_EndAddress)
            {
                return true;
            }

            return false;
        }

        private void CleanUp()
        {
            if (m_GCHandle.IsAllocated)
            {
                m_GCHandle.Free();
            }

            ReleasePinnedSendBuffer();
        }

        internal static ArraySegment<byte> CreateInternalBufferArraySegment(int receiveBufferSize, int sendBufferSize, bool isServerBuffer)
        {
            Debug.Assert(receiveBufferSize >= MinReceiveBufferSize,
                "'receiveBufferSize' MUST be at least " + MinReceiveBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");
            Debug.Assert(sendBufferSize >= MinSendBufferSize,
                "'sendBufferSize' MUST be at least " + MinSendBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");

            int internalBufferSize = GetInternalBufferSize(receiveBufferSize, sendBufferSize, isServerBuffer);
            return new ArraySegment<byte>(new byte[internalBufferSize]);
        }

        internal static void Validate(int count, int receiveBufferSize, int sendBufferSize, bool isServerBuffer)
        {
            Debug.Assert(receiveBufferSize >= MinReceiveBufferSize,
                "'receiveBufferSize' MUST be at least " + MinReceiveBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");
            Debug.Assert(sendBufferSize >= MinSendBufferSize,
                "'sendBufferSize' MUST be at least " + MinSendBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");

            int minBufferSize = GetInternalBufferSize(receiveBufferSize, sendBufferSize, isServerBuffer);
            if (count < minBufferSize)
            {
                throw new ArgumentOutOfRangeException("internalBuffer",
                    SR.Format(SR.net_WebSockets_ArgumentOutOfRange_InternalBuffer, minBufferSize));
            }
        }

        private static int GetInternalBufferSize(int receiveBufferSize, int sendBufferSize, bool isServerBuffer)
        {
            Debug.Assert(receiveBufferSize >= MinReceiveBufferSize,
                "'receiveBufferSize' MUST be at least " + MinReceiveBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");
            Debug.Assert(sendBufferSize >= MinSendBufferSize,
                "'sendBufferSize' MUST be at least " + MinSendBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");

            Debug.Assert(receiveBufferSize <= MaxBufferSize,
                "'receiveBufferSize' MUST be less than or equal to " + MaxBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");
            Debug.Assert(sendBufferSize <= MaxBufferSize,
                "'sendBufferSize' MUST be at less than or equal to " + MaxBufferSize.ToString(NumberFormatInfo.InvariantInfo) + ".");

            int nativeSendBufferSize = GetNativeSendBufferSize(sendBufferSize, isServerBuffer);
            return 2 * receiveBufferSize + nativeSendBufferSize + NativeOverheadBufferSize + s_PropertyBufferSize;
        }

        private static class SendBufferState
        {
            public const int None = 0;
            public const int SendPayloadSpecified = 1;
        }
    }
}