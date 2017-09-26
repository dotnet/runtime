// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Data.SqlClient.SNI
{
    /// <summary>
    /// SNI MARS connection. Multiple MARS streams will be overlaid on this connection.
    /// </summary>
    internal class SNIMarsConnection
    {
        private readonly Guid _connectionId = Guid.NewGuid();
        private readonly Dictionary<int, SNIMarsHandle> _sessions = new Dictionary<int, SNIMarsHandle>();
        private readonly byte[] _headerBytes = new byte[SNISMUXHeader.HEADER_LENGTH];

        private SNIHandle _lowerHandle;
        private ushort _nextSessionId = 0;
        private int _currentHeaderByteCount = 0;
        private int _dataBytesLeft = 0;
        private SNISMUXHeader _currentHeader;
        private SNIPacket _currentPacket;

        /// <summary>
        /// Connection ID
        /// </summary>
        public Guid ConnectionId
        {
            get
            {
                return _connectionId;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="lowerHandle">Lower handle</param>
        public SNIMarsConnection(SNIHandle lowerHandle)
        {
            _lowerHandle = lowerHandle;
            _lowerHandle.SetAsyncCallbacks(HandleReceiveComplete, HandleSendComplete);
        }

        public SNIMarsHandle CreateMarsSession(object callbackObject, bool async)
        {
            lock (this)
            {
                ushort sessionId = _nextSessionId++;
                SNIMarsHandle handle = new SNIMarsHandle(this, sessionId, callbackObject, async);
                _sessions.Add(sessionId, handle);
                return handle;
            }
        }

        /// <summary>
        /// Start receiving
        /// </summary>
        /// <returns></returns>
        public uint StartReceive()
        {
            SNIPacket packet = null;

            if (ReceiveAsync(ref packet) == TdsEnums.SNI_SUCCESS_IO_PENDING)
            {
                return TdsEnums.SNI_SUCCESS_IO_PENDING;
            }

            return SNICommon.ReportSNIError(SNIProviders.SMUX_PROV, 0, SNICommon.ConnNotUsableError, string.Empty);
        }

        /// <summary>
        /// Send a packet synchronously
        /// </summary>
        /// <param name="packet">SNI packet</param>
        /// <returns>SNI error code</returns>
        public uint Send(SNIPacket packet)
        {
            lock (this)
            {
                return _lowerHandle.Send(packet);
            }
        }

        /// <summary>
        /// Send a packet asynchronously
        /// </summary>
        /// <param name="packet">SNI packet</param>
        /// <param name="callback">Completion callback</param>
        /// <returns>SNI error code</returns>
        public uint SendAsync(SNIPacket packet, SNIAsyncCallback callback)
        {
            lock (this)
            {
                return _lowerHandle.SendAsync(packet, callback);
            }
        }

        /// <summary>
        /// Receive a packet asynchronously
        /// </summary>
        /// <param name="packet">SNI packet</param>
        /// <returns>SNI error code</returns>
        public uint ReceiveAsync(ref SNIPacket packet)
        {
            lock (this)
            {
                return _lowerHandle.ReceiveAsync(ref packet);
            }
        }

        /// <summary>
        /// Check SNI handle connection
        /// </summary>
        /// <param name="handle"></param>
        /// <returns>SNI error status</returns>
        public uint CheckConnection()
        {
            lock (this)
            {
                return _lowerHandle.CheckConnection();
            }
        }

        /// <summary>
        /// Process a receive error
        /// </summary>
        public void HandleReceiveError(SNIPacket packet)
        {
            Debug.Assert(Monitor.IsEntered(this), "HandleReceiveError was called without being locked.");
            foreach (SNIMarsHandle handle in _sessions.Values)
            {
                handle.HandleReceiveError(packet);
            }
        }

        /// <summary>
        /// Process a send completion
        /// </summary>
        /// <param name="packet">SNI packet</param>
        /// <param name="sniErrorCode">SNI error code</param>
        public void HandleSendComplete(SNIPacket packet, uint sniErrorCode)
        {
            packet.InvokeCompletionCallback(sniErrorCode);
        }

        /// <summary>
        /// Process a receive completion
        /// </summary>
        /// <param name="packet">SNI packet</param>
        /// <param name="sniErrorCode">SNI error code</param>
        public void HandleReceiveComplete(SNIPacket packet, uint sniErrorCode)
        {
            SNISMUXHeader currentHeader = null;
            SNIPacket currentPacket = null;
            SNIMarsHandle currentSession = null;

            if (sniErrorCode != TdsEnums.SNI_SUCCESS)
            {
                lock (this)
                {
                    HandleReceiveError(packet);
                    return;
                }
            }

            while (true)
            {
                lock (this)
                {
                    if (_currentHeaderByteCount != SNISMUXHeader.HEADER_LENGTH)
                    {
                        currentHeader = null;
                        currentPacket = null;
                        currentSession = null;

                        while (_currentHeaderByteCount != SNISMUXHeader.HEADER_LENGTH)
                        {
                            int bytesTaken = packet.TakeData(_headerBytes, _currentHeaderByteCount, SNISMUXHeader.HEADER_LENGTH - _currentHeaderByteCount);
                            _currentHeaderByteCount += bytesTaken;

                            if (bytesTaken == 0)
                            {
                                sniErrorCode = ReceiveAsync(ref packet);

                                if (sniErrorCode == TdsEnums.SNI_SUCCESS_IO_PENDING)
                                {
                                    return;
                                }

                                HandleReceiveError(packet);
                                return;
                            }
                        }

                        _currentHeader = new SNISMUXHeader()
                        {
                            SMID = _headerBytes[0],
                            flags = _headerBytes[1],
                            sessionId = BitConverter.ToUInt16(_headerBytes, 2),
                            length = BitConverter.ToUInt32(_headerBytes, 4) - SNISMUXHeader.HEADER_LENGTH,
                            sequenceNumber = BitConverter.ToUInt32(_headerBytes, 8),
                            highwater = BitConverter.ToUInt32(_headerBytes, 12)
                        };

                        _dataBytesLeft = (int)_currentHeader.length;
                        _currentPacket = new SNIPacket(null);
                        _currentPacket.Allocate((int)_currentHeader.length);
                    }

                    currentHeader = _currentHeader;
                    currentPacket = _currentPacket;

                    if (_currentHeader.flags == (byte)SNISMUXFlags.SMUX_DATA)
                    {
                        if (_dataBytesLeft > 0)
                        {
                            int length = packet.TakeData(_currentPacket, _dataBytesLeft);
                            _dataBytesLeft -= length;

                            if (_dataBytesLeft > 0)
                            {
                                sniErrorCode = ReceiveAsync(ref packet);

                                if (sniErrorCode == TdsEnums.SNI_SUCCESS_IO_PENDING)
                                {
                                    return;
                                }

                                HandleReceiveError(packet);
                                return;
                            }
                        }
                    }

                    _currentHeaderByteCount = 0;

                    if (!_sessions.ContainsKey(_currentHeader.sessionId))
                    {
                        SNILoadHandle.SingletonInstance.LastError = new SNIError(SNIProviders.SMUX_PROV, 0, SNICommon.InvalidParameterError, string.Empty);
                        HandleReceiveError(packet);
                        _lowerHandle.Dispose();
                        _lowerHandle = null;
                        return;
                    }

                    if (_currentHeader.flags == (byte)SNISMUXFlags.SMUX_FIN)
                    {
                        _sessions.Remove(_currentHeader.sessionId);
                    }
                    else
                    {
                        currentSession = _sessions[_currentHeader.sessionId];
                    }
                }

                if (currentHeader.flags == (byte)SNISMUXFlags.SMUX_DATA)
                {
                    currentSession.HandleReceiveComplete(currentPacket, currentHeader);
                }

                if (_currentHeader.flags == (byte)SNISMUXFlags.SMUX_ACK)
                {
                    try
                    {
                        currentSession.HandleAck(currentHeader.highwater);
                    }
                    catch (Exception e)
                    {
                        SNICommon.ReportSNIError(SNIProviders.SMUX_PROV, SNICommon.InternalExceptionError, e);
                    }
                }

                lock (this)
                {
                    if (packet.DataLeft == 0)
                    {
                        sniErrorCode = ReceiveAsync(ref packet);

                        if (sniErrorCode == TdsEnums.SNI_SUCCESS_IO_PENDING)
                        {
                            return;
                        }

                        HandleReceiveError(packet);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Enable SSL
        /// </summary>
        public uint EnableSsl(uint options)
        {
            return _lowerHandle.EnableSsl(options);
        }

        /// <summary>
        /// Disable SSL
        /// </summary>
        public void DisableSsl()
        {
            _lowerHandle.DisableSsl();
        }

#if DEBUG
        /// <summary>
        /// Test handle for killing underlying connection
        /// </summary>
        public void KillConnection()
        {
            _lowerHandle.KillConnection();
        }
#endif
    }
}
