// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Data.SqlClient.SNI
{
    /// <summary>
    /// MARS handle
    /// </summary>
    internal class SNIMarsHandle : SNIHandle
    {
        private const uint ACK_THRESHOLD = 2;

        private readonly SNIMarsConnection _connection;
        private readonly uint _status = TdsEnums.SNI_UNINITIALIZED;
        private readonly Queue<SNIPacket> _receivedPacketQueue = new Queue<SNIPacket>();
        private readonly Queue<SNIMarsQueuedPacket> _sendPacketQueue = new Queue<SNIMarsQueuedPacket>();
        private readonly object _callbackObject;
        private readonly Guid _connectionId = Guid.NewGuid();
        private readonly ushort _sessionId;
        private readonly ManualResetEventSlim _packetEvent = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim _ackEvent = new ManualResetEventSlim(false);
        private readonly SNISMUXHeader _currentHeader = new SNISMUXHeader();

        private uint _sendHighwater = 4;
        private int _asyncReceives = 0;
        private uint _receiveHighwater = 4;
        private uint _receiveHighwaterLastAck = 4;
        private uint _sequenceNumber;
        private SNIError _connectionError;

        /// <summary>
        /// Connection ID
        /// </summary>
        public override Guid ConnectionId
        {
            get
            {
                return _connectionId;
            }
        }

        /// <summary>
        /// Handle status
        /// </summary>
        public override uint Status
        {
            get
            {
                return _status;
            }
        }

        /// <summary>
        /// Dispose object
        /// </summary>
        public override void Dispose()
        {
            try
            {
                SendControlPacket(SNISMUXFlags.SMUX_FIN);
            }
            catch (Exception e)
            {
                SNICommon.ReportSNIError(SNIProviders.SMUX_PROV, SNICommon.InternalExceptionError, e);
                throw;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection">MARS connection</param>
        /// <param name="sessionId">MARS session ID</param>
        /// <param name="callbackObject">Callback object</param>
        /// <param name="async">true if connection is asynchronous</param>
        public SNIMarsHandle(SNIMarsConnection connection, ushort sessionId, object callbackObject, bool async)
        {
            _sessionId = sessionId;
            _connection = connection;
            _callbackObject = callbackObject;
            SendControlPacket(SNISMUXFlags.SMUX_SYN);
            _status = TdsEnums.SNI_SUCCESS;
        }

        /// <summary>
        /// Send control packet
        /// </summary>
        /// <param name="flags">SMUX header flags</param>
        private void SendControlPacket(SNISMUXFlags flags)
        {
            byte[] headerBytes = null;

            lock (this)
            {
                GetSMUXHeaderBytes(0, (byte)flags, ref headerBytes);
            }

            SNIPacket packet = new SNIPacket(null);
            packet.SetData(headerBytes, SNISMUXHeader.HEADER_LENGTH);
            
            _connection.Send(packet);
        }

        /// <summary>
        /// Generate SMUX header 
        /// </summary>
        /// <param name="length">Packet length</param>
        /// <param name="flags">Packet flags</param>
        /// <param name="headerBytes">Header in bytes</param>
        private void GetSMUXHeaderBytes(int length, byte flags, ref byte[] headerBytes)
        {
            headerBytes = new byte[SNISMUXHeader.HEADER_LENGTH];

            _currentHeader.SMID = 83;
            _currentHeader.flags = flags;
            _currentHeader.sessionId = _sessionId;
            _currentHeader.length = (uint)SNISMUXHeader.HEADER_LENGTH + (uint)length;
            _currentHeader.sequenceNumber = ((flags == (byte)SNISMUXFlags.SMUX_FIN) || (flags == (byte)SNISMUXFlags.SMUX_ACK)) ? _sequenceNumber - 1 : _sequenceNumber++;
            _currentHeader.highwater = _receiveHighwater;
            _receiveHighwaterLastAck = _currentHeader.highwater;

            BitConverter.GetBytes(_currentHeader.SMID).CopyTo(headerBytes, 0);
            BitConverter.GetBytes(_currentHeader.flags).CopyTo(headerBytes, 1);
            BitConverter.GetBytes(_currentHeader.sessionId).CopyTo(headerBytes, 2);
            BitConverter.GetBytes(_currentHeader.length).CopyTo(headerBytes, 4);
            BitConverter.GetBytes(_currentHeader.sequenceNumber).CopyTo(headerBytes, 8);
            BitConverter.GetBytes(_currentHeader.highwater).CopyTo(headerBytes, 12);
        }

        /// <summary>
        /// Generate a packet with SMUX header
        /// </summary>
        /// <param name="packet">SNI packet</param>
        /// <returns>Encapsulated SNI packet</returns>
        private SNIPacket GetSMUXEncapsulatedPacket(SNIPacket packet)
        {
            uint xSequenceNumber = _sequenceNumber;
            byte[] headerBytes = null;
            GetSMUXHeaderBytes(packet.Length, (byte)SNISMUXFlags.SMUX_DATA, ref headerBytes);

            SNIPacket smuxPacket = new SNIPacket(null);
            smuxPacket.Description = string.Format("({0}) SMUX packet {1}", packet.Description == null ? "" : packet.Description, xSequenceNumber);
            smuxPacket.Allocate(16 + packet.Length);
            smuxPacket.AppendData(headerBytes, 16);
            smuxPacket.AppendPacket(packet);

            return smuxPacket;
        }

        /// Send a packet synchronously
        /// </summary>
        /// <param name="packet">SNI packet</param>
        /// <returns>SNI error code</returns>
        public override uint Send(SNIPacket packet)
        {
            while (true)
            {
                lock (this)
                {
                    if (_sequenceNumber < _sendHighwater)
                    {
                        break;
                    }
                }

                _ackEvent.Wait();

                lock (this)
                {
                    _ackEvent.Reset();
                }
            }

            return _connection.Send(GetSMUXEncapsulatedPacket(packet));
        }

        /// <summary>
        /// Send packet asynchronously
        /// </summary>
        /// <param name="packet">SNI packet</param>
        /// <param name="callback">Completion callback</param>
        /// <returns>SNI error code</returns>
        private uint InternalSendAsync(SNIPacket packet, SNIAsyncCallback callback)
        {
            SNIPacket encapsulatedPacket = null;

            lock (this)
            {
                if (_sequenceNumber >= _sendHighwater)
                {
                    return TdsEnums.SNI_QUEUE_FULL;
                }

                encapsulatedPacket = GetSMUXEncapsulatedPacket(packet);

                if (callback != null)
                {
                    encapsulatedPacket.SetCompletionCallback(callback);
                }
                else
                {
                    encapsulatedPacket.SetCompletionCallback(HandleSendComplete);
                }

                return _connection.SendAsync(encapsulatedPacket, callback);
            }
        }

        /// <summary>
        /// Send pending packets
        /// </summary>
        /// <returns>SNI error code</returns>
        private uint SendPendingPackets()
        {
            SNIMarsQueuedPacket packet = null;

            while (true)
            {
                lock (this)
                {
                    if (_sequenceNumber < _sendHighwater)
                    {
                        if (_sendPacketQueue.Count != 0)
                        {
                            packet = _sendPacketQueue.Peek();
                            uint result = InternalSendAsync(packet.Packet, packet.Callback);

                            if (result != TdsEnums.SNI_SUCCESS && result != TdsEnums.SNI_SUCCESS_IO_PENDING)
                            {
                                return result;
                            }

                            _sendPacketQueue.Dequeue();
                            continue;
                        }
                        else
                        {
                            _ackEvent.Set();
                        }
                    }

                    break;
                }
            }

            return TdsEnums.SNI_SUCCESS;
        }

        /// <summary>
        /// Send a packet asynchronously
        /// </summary>
        /// <param name="packet">SNI packet</param>
        /// <param name="callback">Completion callback</param>
        /// <returns>SNI error code</returns>
        public override uint SendAsync(SNIPacket packet, SNIAsyncCallback callback = null)
        {
            lock (this)
            {
                _sendPacketQueue.Enqueue(new SNIMarsQueuedPacket(packet, callback != null ? callback : HandleSendComplete));
            }

            SendPendingPackets();
            return TdsEnums.SNI_SUCCESS_IO_PENDING;
        }

        /// <summary>
        /// Receive a packet asynchronously
        /// </summary>
        /// <param name="packet">SNI packet</param>
        /// <returns>SNI error code</returns>
        public override uint ReceiveAsync(ref SNIPacket packet)
        {
            lock (_receivedPacketQueue)
            {
                int queueCount = _receivedPacketQueue.Count;

                if (_connectionError != null)
                {
                    return SNICommon.ReportSNIError(_connectionError);
                }

                if (queueCount == 0)
                {
                    _asyncReceives++;
                    return TdsEnums.SNI_SUCCESS_IO_PENDING;
                }

                packet = _receivedPacketQueue.Dequeue();

                if (queueCount == 1)
                {
                    _packetEvent.Reset();
                }
            }

            lock (this)
            {
                _receiveHighwater++;
            }

            SendAckIfNecessary();
            return TdsEnums.SNI_SUCCESS;
        }

        /// <summary>
        /// Handle receive error
        /// </summary>
        public void HandleReceiveError(SNIPacket packet)
        {
            lock (_receivedPacketQueue)
            {
                _connectionError = SNILoadHandle.SingletonInstance.LastError;
                _packetEvent.Set();
            }

            ((TdsParserStateObject)_callbackObject).ReadAsyncCallback(packet, 1);
        }

        /// <summary>
        /// Handle send completion
        /// </summary>
        /// <param name="packet">SNI packet</param>
        /// <param name="sniErrorCode">SNI error code</param>
        public void HandleSendComplete(SNIPacket packet, uint sniErrorCode)
        {
            lock (this)
            {
                Debug.Assert(_callbackObject != null);

                ((TdsParserStateObject)_callbackObject).WriteAsyncCallback(packet, sniErrorCode);
            }
        }

        /// <summary>
        /// Handle SMUX acknowledgement
        /// </summary>
        /// <param name="highwater">Send highwater mark</param>
        public void HandleAck(uint highwater)
        {
            lock (this)
            {
                if (_sendHighwater != highwater)
                {
                    _sendHighwater = highwater;
                    SendPendingPackets();
                }
            }
        }

        /// <summary>
        /// Handle receive completion
        /// </summary>
        /// <param name="packet">SNI packet</param>
        /// <param name="header">SMUX header</param>
        public void HandleReceiveComplete(SNIPacket packet, SNISMUXHeader header)
        {
            lock (this)
            {
                if (_sendHighwater != header.highwater)
                {
                    HandleAck(header.highwater);
                }

                lock (_receivedPacketQueue)
                {
                    if (_asyncReceives == 0)
                    {
                        _receivedPacketQueue.Enqueue(packet);
                        _packetEvent.Set();
                        return;
                    }

                    _asyncReceives--;
                    Debug.Assert(_callbackObject != null);

                    ((TdsParserStateObject)_callbackObject).ReadAsyncCallback(packet, 0);
                }
            }

            lock (this)
            {
                _receiveHighwater++;
            }

            SendAckIfNecessary();
        }

        /// <summary>
        /// Send ACK if we've hit highwater threshold
        /// </summary>
        private void SendAckIfNecessary()
        {
            uint receiveHighwater;
            uint receiveHighwaterLastAck;

            lock (this)
            {
                receiveHighwater = _receiveHighwater;
                receiveHighwaterLastAck = _receiveHighwaterLastAck;
            }

            if (receiveHighwater - receiveHighwaterLastAck > ACK_THRESHOLD)
            {
                SendControlPacket(SNISMUXFlags.SMUX_ACK);
            }
        }

        /// <summary>
        /// Receive a packet synchronously
        /// </summary>
        /// <param name="packet">SNI packet</param>
        /// <param name="timeoutInMilliseconds">Timeout in Milliseconds</param>
        /// <returns>SNI error code</returns>
        public override uint Receive(out SNIPacket packet, int timeoutInMilliseconds)
        {
            packet = null;
            int queueCount;
            uint result = TdsEnums.SNI_SUCCESS_IO_PENDING;

            while (true)
            {
                lock (_receivedPacketQueue)
                {
                    if (_connectionError != null)
                    {
                        return SNICommon.ReportSNIError(_connectionError);
                    }

                    queueCount = _receivedPacketQueue.Count;

                    if (queueCount > 0)
                    {
                        packet = _receivedPacketQueue.Dequeue();

                        if (queueCount == 1)
                        {
                            _packetEvent.Reset();
                        }

                        result = TdsEnums.SNI_SUCCESS;
                    }
                }

                if (result == TdsEnums.SNI_SUCCESS)
                {
                    lock (this)
                    {
                        _receiveHighwater++;
                    }

                    SendAckIfNecessary();
                    return result;
                }

                if (!_packetEvent.Wait(timeoutInMilliseconds))
                {
                    SNILoadHandle.SingletonInstance.LastError = new SNIError(SNIProviders.SMUX_PROV, 0, SNICommon.ConnTimeoutError, string.Empty);
                    return TdsEnums.SNI_WAIT_TIMEOUT;
                }
            }
        }

        /// <summary>
        /// Check SNI handle connection
        /// </summary>
        /// <param name="handle"></param>
        /// <returns>SNI error status</returns>
        public override uint CheckConnection()
        {
            return _connection.CheckConnection();
        }

        /// <summary>
        /// Set async callbacks
        /// </summary>
        /// <param name="receiveCallback">Receive callback</param>
        /// <param name="sendCallback">Send callback</param>
        public override void SetAsyncCallbacks(SNIAsyncCallback receiveCallback, SNIAsyncCallback sendCallback)
        {
        }

        /// <summary>
        /// Set buffer size
        /// </summary>
        /// <param name="bufferSize">Buffer size</param>
        public override void SetBufferSize(int bufferSize)
        {
        }

        /// <summary>
        /// Enable SSL
        /// </summary>
        public override uint EnableSsl(uint options)
        {
            return _connection.EnableSsl(options);
        }

        /// <summary>
        /// Disable SSL
        /// </summary>
        public override void DisableSsl()
        {
            _connection.DisableSsl();
        }

#if DEBUG
        /// <summary>
        /// Test handle for killing underlying connection
        /// </summary>
        public override void KillConnection()
        {
            _connection.KillConnection();
        }
#endif
    }
}
