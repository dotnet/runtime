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

        internal override uint Status => _sessionHandle != null ? _sessionHandle.Status : TdsEnums.SNI_UNINITIALIZED;

        internal override SessionHandle SessionHandle => SessionHandle.FromManagedSession(_sessionHandle);

        protected override bool CheckPacket(PacketHandle packet, TaskCompletionSource<object> source)
        {
            SNIPacket p = packet.ManagedPacket;
            return p.IsInvalid || source != null;
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

        protected override uint SNIPacketGetData(PacketHandle packet, byte[] _inBuff, ref uint dataSize) => SNIProxy.Singleton.PacketGetData(packet.ManagedPacket, _inBuff, ref dataSize);

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

        internal void ReadAsyncCallback(SNIPacket packet, uint error) => ReadAsyncCallback(IntPtr.Zero, PacketHandle.FromManagedPacket(packet), error);

        internal void WriteAsyncCallback(SNIPacket packet, uint sniError) => WriteAsyncCallback(IntPtr.Zero, PacketHandle.FromManagedPacket(packet), sniError);

        protected override void RemovePacketFromPendingList(PacketHandle packet)
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

        internal override PacketHandle ReadSyncOverAsync(int timeoutRemaining, out uint error)
        {
            SNIHandle handle = Handle;
            if (handle == null)
            {
                throw ADP.ClosedConnectionError();
            }
            SNIPacket packet = null;
            error = SNIProxy.Singleton.ReadSyncOverAsync(handle, out packet, timeoutRemaining);
            return PacketHandle.FromManagedPacket(packet);
        }

        protected override PacketHandle EmptyReadPacket => PacketHandle.FromManagedPacket(null);

        internal override bool IsPacketEmpty(PacketHandle packet)
        {
            return packet.ManagedPacket == null;
        }

        internal override void ReleasePacket(PacketHandle syncReadPacket)
        {
            syncReadPacket.ManagedPacket?.Dispose();
        }

        internal override uint CheckConnection()
        {
            SNIHandle handle = Handle;
            return handle == null ? TdsEnums.SNI_SUCCESS : SNIProxy.Singleton.CheckConnection(handle);
        }

        internal override PacketHandle ReadAsync(SessionHandle handle, out uint error)
        {
            SNIPacket packet;
            error = SNIProxy.Singleton.ReadAsync(handle.ManagedHandle, out packet);
            return PacketHandle.FromManagedPacket(packet);
        }

        internal override PacketHandle CreateAndSetAttentionPacket()
        {
            if (_sniAsyncAttnPacket == null)
            {
                SNIPacket attnPacket = new SNIPacket();
                SetPacketData(PacketHandle.FromManagedPacket(attnPacket), SQL.AttentionHeader, TdsEnums.HEADER_LEN);
                _sniAsyncAttnPacket = attnPacket;
            }
            return PacketHandle.FromManagedPacket(_sniAsyncAttnPacket);
        }

        internal override uint WritePacket(PacketHandle packet, bool sync)
        {
            return SNIProxy.Singleton.WritePacket(Handle, packet.ManagedPacket, sync);
        }

        internal override PacketHandle AddPacketToPendingList(PacketHandle packet)
        {
            // No-Op
            return packet;
        }

        internal override bool IsValidPacket(PacketHandle packet)
        {
            Debug.Assert(packet.Type == PacketHandle.ManagedPacketType, "unexpected packet type when requiring ManagedPacket");
            return (
                packet.Type == PacketHandle.ManagedPacketType &&
                packet.ManagedPacket != null &&
                !packet.ManagedPacket.IsInvalid
            );
        }

        internal override PacketHandle GetResetWritePacket()
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
            return PacketHandle.FromManagedPacket(_sniPacket);
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

        internal override void SetPacketData(PacketHandle packet, byte[] buffer, int bytesUsed) => SNIProxy.Singleton.PacketSetData(packet.ManagedPacket, buffer, bytesUsed);

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
                    packet = new SNIPacket();
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
