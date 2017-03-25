// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Runtime.ConstrainedExecution;
using System.Runtime.CompilerServices;
using System.Runtime;
using System.Security;

namespace System.Threading
{
    //
    // Methods for accessing memory with volatile semantics.  These are preferred over Thread.VolatileRead
    // and Thread.VolatileWrite, as these are implemented more efficiently.
    //
    // (We cannot change the implementations of Thread.VolatileRead/VolatileWrite without breaking code
    // that relies on their overly-strong ordering guarantees.)
    //
    // The actual implementations of these methods are typically supplied by the VM at JIT-time, because C# does
    // not allow us to express a volatile read/write from/to a byref arg.
    // See getILIntrinsicImplementationForVolatile() in jitinterface.cpp.
    //
    public static class Volatile
    {
        [System.Runtime.Versioning.NonVersionable]
        public static bool Read(ref bool location)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            var value = location;
            Interlocked.MemoryBarrier();
            return value;
        }

        [CLSCompliant(false)]
        [System.Runtime.Versioning.NonVersionable]
        public static sbyte Read(ref sbyte location)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            var value = location;
            Interlocked.MemoryBarrier();
            return value;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static byte Read(ref byte location)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            var value = location;
            Interlocked.MemoryBarrier();
            return value;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static short Read(ref short location)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            var value = location;
            Interlocked.MemoryBarrier();
            return value;
        }

        [CLSCompliant(false)]
        [System.Runtime.Versioning.NonVersionable]
        public static ushort Read(ref ushort location)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            var value = location;
            Interlocked.MemoryBarrier();
            return value;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static int Read(ref int location)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            var value = location;
            Interlocked.MemoryBarrier();
            return value;
        }

        [CLSCompliant(false)]
        [System.Runtime.Versioning.NonVersionable]
        public static uint Read(ref uint location)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            var value = location;
            Interlocked.MemoryBarrier();
            return value;
        }

#if BIT64
        [System.Runtime.Versioning.NonVersionable]
        public static long Read(ref long location)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            var value = location;
            Interlocked.MemoryBarrier();
            return value;
        }

        [CLSCompliant(false)]
        [System.Runtime.Versioning.NonVersionable]
        public static ulong Read(ref ulong location)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            var value = location;
            Interlocked.MemoryBarrier();
            return value;
        }
#else
        public static long Read(ref long location)
        {
            //
            // On 32-bit machines, we use this implementation, since an ordinary volatile read
            // would not be atomic.
            //
            // On 64-bit machines, the VM will replace this with a more efficient implementation.
            //
            return Interlocked.CompareExchange(ref location, 0, 0);
        }

        [CLSCompliant(false)]
        public static ulong Read(ref ulong location)
        {
            unsafe
            {
                //
                // There is no overload of Interlocked.Exchange that accepts a ulong.  So we have
                // to do some pointer tricks to pass our arguments to the overload that takes a long.
                //
                fixed (ulong* pLocation = &location)
                {
                    return (ulong)Interlocked.CompareExchange(ref *(long*)pLocation, 0, 0);
                }
            }
        }
#endif

        [System.Runtime.Versioning.NonVersionable]
        public static IntPtr Read(ref IntPtr location)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            var value = location;
            Interlocked.MemoryBarrier();
            return value;
        }

        [CLSCompliant(false)]
        [System.Runtime.Versioning.NonVersionable]
        public static UIntPtr Read(ref UIntPtr location)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            var value = location;
            Interlocked.MemoryBarrier();
            return value;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static float Read(ref float location)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            var value = location;
            Interlocked.MemoryBarrier();
            return value;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static double Read(ref double location)
        {
            //
            // On 32-bit machines, we use this implementation, since an ordinary volatile read
            // would not be atomic.
            //
            // On 64-bit machines, the VM will replace this with a more efficient implementation.
            //
            return Interlocked.CompareExchange(ref location, 0, 0);
        }

        [System.Runtime.Versioning.NonVersionable]
        public static T Read<T>(ref T location) where T : class
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            var value = location;
            Interlocked.MemoryBarrier();
            return value;
        }




        [System.Runtime.Versioning.NonVersionable]
        public static void Write(ref bool location, bool value)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            Interlocked.MemoryBarrier();
            location = value;
        }

        [CLSCompliant(false)]
        [System.Runtime.Versioning.NonVersionable]
        public static void Write(ref sbyte location, sbyte value)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            Interlocked.MemoryBarrier();
            location = value;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static void Write(ref byte location, byte value)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            Interlocked.MemoryBarrier();
            location = value;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static void Write(ref short location, short value)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            Interlocked.MemoryBarrier();
            location = value;
        }

        [CLSCompliant(false)]
        [System.Runtime.Versioning.NonVersionable]
        public static void Write(ref ushort location, ushort value)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            Interlocked.MemoryBarrier();
            location = value;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static void Write(ref int location, int value)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            Interlocked.MemoryBarrier();
            location = value;
        }

        [CLSCompliant(false)]
        [System.Runtime.Versioning.NonVersionable]
        public static void Write(ref uint location, uint value)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            Interlocked.MemoryBarrier();
            location = value;
        }

#if BIT64
        [System.Runtime.Versioning.NonVersionable]
        public static void Write(ref long location, long value)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            Interlocked.MemoryBarrier();
            location = value;
        }

        [CLSCompliant(false)]
        [System.Runtime.Versioning.NonVersionable]
        public static void Write(ref ulong location, ulong value)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            Interlocked.MemoryBarrier();
            location = value;
        }
#else
        public static void Write(ref long location, long value)
        {
            //
            // On 32-bit machines, we use this implementation, since an ordinary volatile write 
            // would not be atomic.
            //
            // On 64-bit machines, the VM will replace this with a more efficient implementation.
            //
            Interlocked.Exchange(ref location, value);
        }

        [CLSCompliant(false)]
        public static void Write(ref ulong location, ulong value)
        {
            //
            // On 32-bit machines, we use this implementation, since an ordinary volatile write 
            // would not be atomic.
            //
            // On 64-bit machines, the VM will replace this with a more efficient implementation.
            //
            unsafe
            {
                //
                // There is no overload of Interlocked.Exchange that accepts a ulong.  So we have
                // to do some pointer tricks to pass our arguments to the overload that takes a long.
                //
                fixed (ulong* pLocation = &location)
                {
                    Interlocked.Exchange(ref *(long*)pLocation, (long)value);
                }
            }
        }
#endif

        [System.Runtime.Versioning.NonVersionable]
        public static void Write(ref IntPtr location, IntPtr value)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            Interlocked.MemoryBarrier();
            location = value;
        }

        [CLSCompliant(false)]
        [System.Runtime.Versioning.NonVersionable]
        public static void Write(ref UIntPtr location, UIntPtr value)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            Interlocked.MemoryBarrier();
            location = value;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static void Write(ref float location, float value)
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            Interlocked.MemoryBarrier();
            location = value;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static void Write(ref double location, double value)
        {
            //
            // On 32-bit machines, we use this implementation, since an ordinary volatile write 
            // would not be atomic.
            //
            // On 64-bit machines, the VM will replace this with a more efficient implementation.
            //
            Interlocked.Exchange(ref location, value);
        }

        [System.Runtime.Versioning.NonVersionable]
        public static void Write<T>(ref T location, T value) where T : class
        {
            // 
            // The VM will replace this with a more efficient implementation.
            //
            Interlocked.MemoryBarrier();
            location = value;
        }
    }
}
