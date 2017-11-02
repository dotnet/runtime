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

using System.Threading;
using System.Security;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Diagnostics;

namespace System
{
    internal sealed class SharedStatics
    {
        // this is declared static but is actually forced to be the same object 
        // for each AppDomain at AppDomain create time.
        private static SharedStatics _sharedStatics;

        // Note: Do not add any code in this ctor because it is not called 
        // when we set up _sharedStatics via AppDomain::SetupSharedStatics
        private SharedStatics()
        {
            Debug.Fail("SharedStatics..ctor() is never called.");
        }

        // This is the total amount of memory currently "reserved" via
        // all MemoryFailPoints allocated within the process.
        // Stored as a long because we need to use Interlocked.Add.
        private long _memFailPointReservedMemory;

        internal static long AddMemoryFailPointReservation(long size)
        {
            // Size can legitimately be negative - see Dispose.
            return Interlocked.Add(ref _sharedStatics._memFailPointReservedMemory, (long)size);
        }

        internal static ulong MemoryFailPointReservedMemory
        {
            get
            {
                Debug.Assert(Volatile.Read(ref _sharedStatics._memFailPointReservedMemory) >= 0, "Process-wide MemoryFailPoint reserved memory was negative!");
                return (ulong)Volatile.Read(ref _sharedStatics._memFailPointReservedMemory);
            }
        }
    }
}
