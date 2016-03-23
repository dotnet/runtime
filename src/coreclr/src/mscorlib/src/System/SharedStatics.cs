// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Container for statics that are shared across AppDomains.
**
**
=============================================================================*/

namespace System {

    using System.Threading;
    using System.Runtime.Remoting;
    using System.Security;
    using System.Security.Util;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Diagnostics.Contracts;
#if FEATURE_CAS_POLICY
    using StringMaker = System.Security.Util.Tokenizer.StringMaker;
#endif // FEATURE_CAS_POLICY

    internal sealed class SharedStatics
    {
        // this is declared static but is actually forced to be the same object 
        // for each AppDomain at AppDomain create time.
        private static SharedStatics _sharedStatics;
        
        // Note: Do not add any code in this ctor because it is not called 
        // when we set up _sharedStatics via AppDomain::SetupSharedStatics
        private SharedStatics()
        {
            BCLDebug.Assert(false, "SharedStatics..ctor() is never called.");
        }

        private volatile String _Remoting_Identity_IDGuid;
        public static String Remoting_Identity_IDGuid 
        { 
            [System.Security.SecuritySafeCritical]  // auto-generated
            get 
            {
                if (_sharedStatics._Remoting_Identity_IDGuid == null)
                {
                    bool tookLock = false;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try {
                        Monitor.Enter(_sharedStatics, ref tookLock);

                        if (_sharedStatics._Remoting_Identity_IDGuid == null)
                        {
                            _sharedStatics._Remoting_Identity_IDGuid = Guid.NewGuid().ToString().Replace('-', '_');
                        }
                    }
                    finally {
                        if (tookLock)
                            Monitor.Exit(_sharedStatics);
                    }
                }

                Contract.Assert(_sharedStatics._Remoting_Identity_IDGuid != null,
                                "_sharedStatics._Remoting_Identity_IDGuid != null");
                return _sharedStatics._Remoting_Identity_IDGuid;
            } 
        }

#if FEATURE_CAS_POLICY
        private StringMaker _maker;
        [System.Security.SecuritySafeCritical]  // auto-generated
        static public StringMaker GetSharedStringMaker()
        {
            StringMaker maker = null;
            
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                Monitor.Enter(_sharedStatics, ref tookLock);

                if (_sharedStatics._maker != null)
                {
                    maker = _sharedStatics._maker;
                    _sharedStatics._maker = null;
                }
            }
            finally {
                if (tookLock)
                    Monitor.Exit(_sharedStatics);
            }
            
            if (maker == null)
            {
                maker = new StringMaker();
            }
            
            return maker;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        static public void ReleaseSharedStringMaker(ref StringMaker maker)
        {
            // save this stringmaker so someone else can use it
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                Monitor.Enter(_sharedStatics, ref tookLock);

                _sharedStatics._maker = maker;
                maker = null;
            }
            finally {
                if (tookLock)
                    Monitor.Exit(_sharedStatics);
            }
        }
#endif // FEATURE_CAS_POLICY

        // Note this may not need to be process-wide.
        private int _Remoting_Identity_IDSeqNum;
        internal static int Remoting_Identity_GetNextSeqNum()
        {
            return Interlocked.Increment(ref _sharedStatics._Remoting_Identity_IDSeqNum);
        }


        // This is the total amount of memory currently "reserved" via
        // all MemoryFailPoints allocated within the process.
        // Stored as a long because we need to use Interlocked.Add.
        private long _memFailPointReservedMemory;

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static long AddMemoryFailPointReservation(long size)
        {
            // Size can legitimately be negative - see Dispose.
            return Interlocked.Add(ref _sharedStatics._memFailPointReservedMemory, (long) size);
        }

        internal static ulong MemoryFailPointReservedMemory {
            get { 
                Contract.Assert(Volatile.Read(ref _sharedStatics._memFailPointReservedMemory) >= 0, "Process-wide MemoryFailPoint reserved memory was negative!");
                return (ulong) Volatile.Read(ref _sharedStatics._memFailPointReservedMemory);
            }
        }
    }
}
