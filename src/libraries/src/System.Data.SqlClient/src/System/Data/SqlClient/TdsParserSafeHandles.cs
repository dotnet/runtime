// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;


namespace System.Data.SqlClient
{
    internal sealed partial class SNILoadHandle : SafeHandle
    {
        internal static readonly SNILoadHandle SingletonInstance = new SNILoadHandle();

        internal readonly SNINativeMethodWrapper.SqlAsyncCallbackDelegate ReadAsyncCallbackDispatcher = new SNINativeMethodWrapper.SqlAsyncCallbackDelegate(ReadDispatcher);
        internal readonly SNINativeMethodWrapper.SqlAsyncCallbackDelegate WriteAsyncCallbackDispatcher = new SNINativeMethodWrapper.SqlAsyncCallbackDelegate(WriteDispatcher);

        private readonly UInt32 _sniStatus = TdsEnums.SNI_UNINITIALIZED;
        private readonly EncryptionOptions _encryptionOption;

        private SNILoadHandle() : base(IntPtr.Zero, true)
        {
            // From security review - SafeHandle guarantees this is only called once.
            // The reason for the safehandle is guaranteed initialization and termination of SNI to
            // ensure SNI terminates and cleans up properly.
            try { }
            finally
            {
                _sniStatus = SNINativeMethodWrapper.SNIInitialize();

                UInt32 value = 0;

                // VSDevDiv 479597: If initialize fails, don't call QueryInfo.
                if (TdsEnums.SNI_SUCCESS == _sniStatus)
                {
                    // Query OS to find out whether encryption is supported.
                    SNINativeMethodWrapper.SNIQueryInfo(SNINativeMethodWrapper.QTypes.SNI_QUERY_CLIENT_ENCRYPT_POSSIBLE, ref value);
                }

                _encryptionOption = (value == 0) ? EncryptionOptions.NOT_SUP : EncryptionOptions.OFF;

                base.handle = (IntPtr)1; // Initialize to non-zero dummy variable.
            }
        }

        public override bool IsInvalid
        {
            get
            {
                return (IntPtr.Zero == base.handle);
            }
        }

        override protected bool ReleaseHandle()
        {
            if (base.handle != IntPtr.Zero)
            {
                if (TdsEnums.SNI_SUCCESS == _sniStatus)
                {
                    LocalDBAPI.ReleaseDLLHandles();
                    SNINativeMethodWrapper.SNITerminate();
                }
                base.handle = IntPtr.Zero;
            }

            return true;
        }

        public UInt32 Status
        {
            get
            {
                return _sniStatus;
            }
        }

        public EncryptionOptions Options
        {
            get
            {
                return _encryptionOption;
            }
        }

        private static void ReadDispatcher(IntPtr key, IntPtr packet, UInt32 error)
        {
            // This is the app-domain dispatcher for all async read callbacks, It 
            // simply gets the state object from the key that it is passed, and 
            // calls the state object's read callback.
            Debug.Assert(IntPtr.Zero != key, "no key passed to read callback dispatcher?");
            if (IntPtr.Zero != key)
            {
                // NOTE: we will get a null ref here if we don't get a key that
                //       contains a GCHandle to TDSParserStateObject; that is 
                //       very bad, and we want that to occur so we can catch it.
                GCHandle gcHandle = (GCHandle)key;
                TdsParserStateObject stateObj = (TdsParserStateObject)gcHandle.Target;

                if (null != stateObj)
                {
                    stateObj.ReadAsyncCallback(IntPtr.Zero, packet, error);
                }
            }
        }

        private static void WriteDispatcher(IntPtr key, IntPtr packet, UInt32 error)
        {
            // This is the app-domain dispatcher for all async write callbacks, It 
            // simply gets the state object from the key that it is passed, and 
            // calls the state object's write callback.
            Debug.Assert(IntPtr.Zero != key, "no key passed to write callback dispatcher?");
            if (IntPtr.Zero != key)
            {
                // NOTE: we will get a null ref here if we don't get a key that
                //       contains a GCHandle to TDSParserStateObject; that is 
                //       very bad, and we want that to occur so we can catch it.
                GCHandle gcHandle = (GCHandle)key;
                TdsParserStateObject stateObj = (TdsParserStateObject)gcHandle.Target;

                if (null != stateObj)
                {
                    stateObj.WriteAsyncCallback(IntPtr.Zero, packet, error);
                }
            }
        }
    }

    internal sealed class SNIHandle : SafeHandle
    {
        private readonly UInt32 _status = TdsEnums.SNI_UNINITIALIZED;
        private readonly bool _fSync = false;

        // creates a physical connection
        internal SNIHandle(
            SNINativeMethodWrapper.ConsumerInfo myInfo,
            string serverName,
            byte[] spnBuffer,
            bool ignoreSniOpenTimeout,
            int timeout,
            out byte[] instanceName,
            bool flushCache,
            bool fSync,
            bool fParallel)
            : base(IntPtr.Zero, true)
        {
            try { }
            finally
            {
                _fSync = fSync;
                instanceName = new byte[256]; // Size as specified by netlibs.
                if (ignoreSniOpenTimeout)
                {
                    timeout = Timeout.Infinite; // -1 == native SNIOPEN_TIMEOUT_VALUE / INFINITE
                }

                _status = SNINativeMethodWrapper.SNIOpenSyncEx(myInfo, serverName, ref base.handle,
                            spnBuffer, instanceName, flushCache, fSync, timeout, fParallel);
            }
        }

        // constructs SNI Handle for MARS session
        internal SNIHandle(SNINativeMethodWrapper.ConsumerInfo myInfo, SNIHandle parent) : base(IntPtr.Zero, true)
        {
            try { }
            finally
            {
                _status = SNINativeMethodWrapper.SNIOpenMarsSession(myInfo, parent, ref base.handle, parent._fSync);
            }
        }

        public override bool IsInvalid
        {
            get
            {
                return (IntPtr.Zero == base.handle);
            }
        }

        override protected bool ReleaseHandle()
        {
            // NOTE: The SafeHandle class guarantees this will be called exactly once.
            IntPtr ptr = base.handle;
            base.handle = IntPtr.Zero;
            if (IntPtr.Zero != ptr)
            {
                if (0 != SNINativeMethodWrapper.SNIClose(ptr))
                {
                    return false;   // SNIClose should never fail.
                }
            }
            return true;
        }

        internal UInt32 Status
        {
            get
            {
                return _status;
            }
        }
    }

    internal sealed class SNIPacket : SafeHandle
    {
        internal SNIPacket(SafeHandle sniHandle) : base(IntPtr.Zero, true)
        {
            SNINativeMethodWrapper.SNIPacketAllocate(sniHandle, SNINativeMethodWrapper.IOType.WRITE, ref base.handle);
            if (IntPtr.Zero == base.handle)
            {
                throw SQL.SNIPacketAllocationFailure();
            }
        }

        public override bool IsInvalid
        {
            get
            {
                return (IntPtr.Zero == base.handle);
            }
        }

        override protected bool ReleaseHandle()
        {
            // NOTE: The SafeHandle class guarantees this will be called exactly once.
            IntPtr ptr = base.handle;
            base.handle = IntPtr.Zero;
            if (IntPtr.Zero != ptr)
            {
                SNINativeMethodWrapper.SNIPacketRelease(ptr);
            }
            return true;
        }
    }

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
                SNINativeMethodWrapper.SNIPacketReset(sniHandle, SNINativeMethodWrapper.IOType.WRITE, packet, SNINativeMethodWrapper.ConsumerNumber.SNI_Consumer_SNI);
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