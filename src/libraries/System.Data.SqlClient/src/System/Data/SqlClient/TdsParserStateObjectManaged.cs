// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Data.SqlClient.SNI
{
    internal class TdsParserStateObjectManaged : TdsParserStateObject
    {
        private SNIMarsConnection _marsConnection = null;
        private SNIHandle _sessionHandle = null;              // the SNI handle we're to work on
        private SNIPacket _sniPacket = null;                // Will have to re-vamp this for MARS
        internal SNIPacket _sniAsyncAttnPacket = null;                // Packet to use to send Attn
        private readonly Dictionary<SNIPacket, SNIPacket> _pendingWritePackets = new Dictionary<SNIPacket, SNIPacket>(); // Stores write packets that have been sent to SNI, but have not yet finished writing (i.e. we are waiting for SNI's callback)

        private readonly WritePacketCache _writePacketCache = new WritePacketCache(); // Store write packets that are ready to be re-used

        public TdsParserStateObjectManaged(TdsParser parser) : base(parser) { }

        internal SspiClientContextStatus sspiClientContextStatus = new SspiClientContextStatus();

        internal TdsParserStateObjectManaged(TdsParser parser, TdsParserStateObject physicalConnection, bool async) :
            base(parser, physicalConnection, async)
        { }

        internal SNIHandle Handle => _sessionHandle;

        internal override UInt32 Status => _sessionHandle != null ? _sessionHandle.Status : TdsEnums.SNI_UNINITIALIZED;

        internal override object SessionHandle => _sessionHandle;

        protected override object EmptyReadPacket => null;

        protected override bool CheckPacket(object packet, TaskCompletionSource<object> source)
        {
            SNIPacket p = packet as SNIPacket;
            return p.IsInvalid || (!p.IsInvalid && source != null);
        }

        protected override void CreateSessionHandle(TdsParserStateObject physicalConnection, bool async)
        {
            Debug.Assert(physicalConnection is TdsParserStateObjectManaged, "Expected a stateObject of type " + this.GetType());
            TdsParserStateObjectManaged managedSNIObject = physicalConnection as TdsParserStateObjectManaged;

            _sessionHandle = managedSNIObject.CreateMarsSession(this, async);
        }

        internal SNIMarsHandle CreateMarsSession(object callbackObject, bool async)
        {
            return _marsConnection.CreateMarsSession(callbackObject, async);
        }

        protected override uint SNIPacketGetData(object packet, byte[] _inBuff, ref uint dataSize) => SNIProxy.Singleton.PacketGetData(packet as SNIPacket, _inBuff, ref dataSize);

        internal override void CreatePhysicalSNIHandle(string serverName, bool ignoreSniOpenTimeout, long timerExpire, out byte[] instanceName, ref byte[] spnBuffer, bool flushCache, bool async, bool parallel, bool isIntegratedSecurity)
        {
            _sessionHandle = SNIProxy.Singleton.CreateConnectionHandle(this, serverName, ignoreSniOpenTimeout, timerExpire, out instanceName, ref spnBuffer, flushCache, async, parallel, isIntegratedSecurity);
            if (_sessionHandle == null)
            {
                _parser.ProcessSNIError(this);
            }
            else if (async)
            {
                // Create call backs and allocate to the session handle
                SNIAsyncCallback ReceiveAsyncCallbackDispatcher = new SNIAsyncCallback(ReadAsyncCallback);
                SNIAsyncCallback SendAsyncCallbackDispatcher = new SNIAsyncCallback(WriteAsyncCallback);
                _sessionHandle.SetAsyncCallbacks(ReceiveAsyncCallbackDispatcher, SendAsyncCallbackDispatcher);
            }
        }

        internal void ReadAsyncCallback(SNIPacket packet, UInt32 error) => ReadAsyncCallback(IntPtr.Zero, packet, error);

        internal void WriteAsyncCallback(SNIPacket packet, UInt32 sniError) => WriteAsyncCallback(IntPtr.Zero, packet, sniError);

        protected override void RemovePacketFromPendingList(object packet)
        {
            // No-Op
        }

        internal override void Dispose()
        {
            SNIPacket packetHandle = _sniPacket;
            SNIHandle sessionHandle = _sessionHandle;
            SNIPacket asyncAttnPacket = _sniAsyncAttnPacket;

            _sniPacket = null;
            _sessionHandle = null;
            _sniAsyncAttnPacket = null;
            _marsConnection = null;

            DisposeCounters();

            if (null != sessionHandle || null != packetHandle)
            {
                packetHandle?.Dispose();
                asyncAttnPacket?.Dispose();
                
                if (sessionHandle != null)
                {
                    sessionHandle.Dispose();
                    DecrementPendingCallbacks(true); // Will dispose of GC handle.
                }
            }

            DisposePacketCache();
        }

        internal override void DisposePacketCache()
        {
            lock (_writePacketLockObject)
            {
                _writePacketCache.Dispose();
                // Do not set _writePacketCache to null, just in case a WriteAsyncCallback completes after this point
            }
        }

        protected override void FreeGcHandle(int remaining, bool release)
        {
            // No - op
        }

        internal override bool IsFailedHandle() => _sessionHandle.Status != TdsEnums.SNI_SUCCESS;

        internal override object ReadSyncOverAsync(int timeoutRemaining, bool isMarsOn, out uint error)
        {
            SNIHandle handle = Handle;
            if (handle == null)
            {
                throw ADP.ClosedConnectionError();
            }
            if (isMarsOn)
            {
                IncrementPendingCallbacks();
            }
            SNIPacket packet = null;
            error = SNIProxy.Singleton.ReadSyncOverAsync(handle, out packet, timeoutRemaining);
            return packet;
        }

        internal override bool IsPacketEmpty(object packet)
        {
            return packet == null;
        }

        internal override void ReleasePacket(object syncReadPacket)
        {
            ((SNIPacket)syncReadPacket).Dispose();
        }

        internal override uint CheckConnection()
        {
            SNIHandle handle = Handle;
            return handle == null ? TdsEnums.SNI_SUCCESS : SNIProxy.Singleton.CheckConnection(handle);
        }

        internal override object ReadAsync(out uint error, ref object handle)
        {
            SNIPacket packet;
            error = SNIProxy.Singleton.ReadAsync((SNIHandle)handle, out packet);
            return packet;
        }

        internal override object CreateAndSetAttentionPacket()
        {
            SNIPacket attnPacket = new SNIPacket(Handle);
            _sniAsyncAttnPacket = attnPacket;
            SetPacketData(attnPacket, SQL.AttentionHeader, TdsEnums.HEADER_LEN);
            return attnPacket;
        }

        internal override uint WritePacket(object packet, bool sync)
        {
            return SNIProxy.Singleton.WritePacket((SNIHandle)Handle, (SNIPacket)packet, sync);
        }

        internal override object AddPacketToPendingList(object packet)
        {
            // No-Op
            return packet;
        }

        internal override bool IsValidPacket(object packetPointer) => (SNIPacket)packetPointer != null && !((SNIPacket)packetPointer).IsInvalid;

        internal override object GetResetWritePacket()
        {
            if (_sniPacket != null)
            {
                _sniPacket.Reset();
            }
            else
            {
                lock (_writePacketLockObject)
                {
                    _sniPacket = _writePacketCache.Take(Handle);
                }
            }
            return _sniPacket;
        }

        internal override void ClearAllWritePackets()
        {
            if (_sniPacket != null)
            {
                _sniPacket.Dispose();
                _sniPacket = null;
            }
            lock (_writePacketLockObject)
            {
                Debug.Assert(_pendingWritePackets.Count == 0 && _asyncWriteCount == 0, "Should not clear all write packets if there are packets pending");
                _writePacketCache.Clear();
            }
        }

        internal override void SetPacketData(object packet, byte[] buffer, int bytesUsed) => SNIProxy.Singleton.PacketSetData((SNIPacket)packet, buffer, bytesUsed);
        
        internal override uint SniGetConnectionId(ref Guid clientConnectionId) => SNIProxy.Singleton.GetConnectionId(Handle, ref clientConnectionId);

        internal override uint DisabeSsl() => SNIProxy.Singleton.DisableSsl(Handle);

        internal override uint EnableMars(ref uint info)
        {
            _marsConnection = new SNIMarsConnection(Handle);
            if (_marsConnection.StartReceive() == TdsEnums.SNI_SUCCESS_IO_PENDING)
            {
                return TdsEnums.SNI_SUCCESS;
            }

            return TdsEnums.SNI_ERROR;
        }

        internal override uint EnableSsl(ref uint info)=>  SNIProxy.Singleton.EnableSsl(Handle, info);

        internal override uint SetConnectionBufferSize(ref uint unsignedPacketSize) => SNIProxy.Singleton.SetConnectionBufferSize(Handle, unsignedPacketSize);

        internal override uint GenerateSspiClientContext(byte[] receivedBuff, uint receivedLength, ref byte[] sendBuff, ref uint sendLength, byte[] _sniSpnBuffer)
        {
            SNIProxy.Singleton.GenSspiClientContext(sspiClientContextStatus, receivedBuff, ref sendBuff, _sniSpnBuffer);
            sendLength = (uint)(sendBuff != null ? sendBuff.Length : 0);
            return 0;
        }

        internal override uint WaitForSSLHandShakeToComplete() => 0;

        internal sealed class WritePacketCache : IDisposable
        {
            private bool _disposed;
            private Stack<SNIPacket> _packets;

            public WritePacketCache()
            {
                _disposed = false;
                _packets = new Stack<SNIPacket>();
            }

            public SNIPacket Take(SNIHandle sniHandle)
            {
                SNIPacket packet;
                if (_packets.Count > 0)
                {
                    // Success - reset the packet
                    packet = _packets.Pop();
                    packet.Reset();
                }
                else
                {
                    // Failed to take a packet - create a new one
                    packet = new SNIPacket(sniHandle);
                }
                return packet;
            }

            public void Add(SNIPacket packet)
            {
                if (!_disposed)
                {
                    _packets.Push(packet);
                }
                else
                {
                    // If we're disposed, then get rid of any packets added to us
                    packet.Dispose();
                }
            }

            public void Clear()
            {
                while (_packets.Count > 0)
                {
                    _packets.Pop().Dispose();
                }
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    Clear();
                }
            }
        }
    }
}
