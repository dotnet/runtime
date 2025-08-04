// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    /// <summary>Methods for accessing memory with volatile semantics.</summary>
    public static class Volatile
    {
        // The runtime may replace these implementations with more efficient ones in some cases.
        // In coreclr, for example, see importercalls.cpp.

        [Intrinsic]
        [NonVersionable]
        public static bool Read(ref readonly bool location)
        {
            bool value = location;
            ReadBarrier();
            return value;
        }

        [Intrinsic]
        [NonVersionable]
        public static void Write(ref bool location, bool value)
        {
            WriteBarrier();
            location = value;
        }

        [Intrinsic]
        [NonVersionable]
        public static byte Read(ref readonly byte location)
        {
            byte value = location;
            ReadBarrier();
            return value;
        }

        [Intrinsic]
        [NonVersionable]
        public static void Write(ref byte location, byte value)
        {
            WriteBarrier();
            location = value;
        }

        [Intrinsic]
        [NonVersionable]
        public static double Read(ref readonly double location) =>
            // Delegate to long overload to ensure atomicity on 32-bit platforms.
            BitConverter.Int64BitsToDouble(Read(ref Unsafe.As<double, long>(ref Unsafe.AsRef(in location))));

        [Intrinsic]
        [NonVersionable]
        public static void Write(ref double location, double value) =>
            // Delegate to long overload to ensure atomicity on 32-bit platforms.
            Write(ref Unsafe.As<double, long>(ref location), BitConverter.DoubleToInt64Bits(value));

        [Intrinsic]
        [NonVersionable]
        public static short Read(ref readonly short location)
        {
            short value = location;
            ReadBarrier();
            return value;
        }

        [Intrinsic]
        [NonVersionable]
        public static void Write(ref short location, short value)
        {
            WriteBarrier();
            location = value;
        }

        [Intrinsic]
        [NonVersionable]
        public static int Read(ref readonly int location)
        {
            int value = location;
            ReadBarrier();
            return value;
        }

        [Intrinsic]
        [NonVersionable]
        public static void Write(ref int location, int value)
        {
            WriteBarrier();
            location = value;
        }

        [Intrinsic]
        [NonVersionable]
        public static long Read(ref readonly long location)
        {
#if TARGET_64BIT
            long value = location;
            ReadBarrier();
            return value;
#else
            // On 32-bit, we use Interlocked, since an ordinary volatile read would not be atomic.
            return Interlocked.CompareExchange(ref Unsafe.AsRef(in location), 0, 0);
#endif
        }

        [Intrinsic]
        [NonVersionable]
        public static void Write(ref long location, long value)
        {
#if TARGET_64BIT
            WriteBarrier();
            location = value;
#else
            // On 32-bit, we use Interlocked, since an ordinary volatile write would not be atomic.
            Interlocked.Exchange(ref location, value);
#endif
        }

        [Intrinsic]
        [NonVersionable]
        public static IntPtr Read(ref readonly IntPtr location)
        {
            IntPtr value = location;
            ReadBarrier();
            return value;
        }

        [Intrinsic]
        [NonVersionable]
        public static void Write(ref IntPtr location, IntPtr value)
        {
            WriteBarrier();
            location = value;
        }

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static sbyte Read(ref readonly sbyte location)
        {
            sbyte value = location;
            ReadBarrier();
            return value;
        }

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static void Write(ref sbyte location, sbyte value)
        {
            WriteBarrier();
            location = value;
        }

        [Intrinsic]
        [NonVersionable]
        public static float Read(ref readonly float location)
        {
            float value = location;
            ReadBarrier();
            return value;
        }

        [Intrinsic]
        [NonVersionable]
        public static void Write(ref float location, float value)
        {
            WriteBarrier();
            location = value;
        }

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static ushort Read(ref readonly ushort location)
        {
            ushort value = location;
            ReadBarrier();
            return value;
        }

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static void Write(ref ushort location, ushort value)
        {
            WriteBarrier();
            location = value;
        }

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static uint Read(ref readonly uint location)
        {
            uint value = location;
            ReadBarrier();
            return value;
        }

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static void Write(ref uint location, uint value)
        {
            WriteBarrier();
            location = value;
        }

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static ulong Read(ref readonly ulong location) =>
            // Delegate to long overload to ensure atomicity on 32-bit platforms.
            (ulong)Read(ref Unsafe.As<ulong, long>(ref Unsafe.AsRef(in location)));

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static void Write(ref ulong location, ulong value) =>
            // Delegate to long overload to ensure atomicity on 32-bit platforms.
            Write(ref Unsafe.As<ulong, long>(ref location), (long)value);

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static UIntPtr Read(ref readonly UIntPtr location)
        {
            UIntPtr value = location;
            ReadBarrier();
            return value;
        }

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static void Write(ref UIntPtr location, UIntPtr value)
        {
            WriteBarrier();
            location = value;
        }

        [Intrinsic]
        [NonVersionable]
        [return: NotNullIfNotNull(nameof(location))]
        public static T Read<T>([NotNullIfNotNull(nameof(location))] ref readonly T location) where T : class?
        {
            T value = location;
            ReadBarrier();
            return value;
        }

        [Intrinsic]
        [NonVersionable]
        public static void Write<T>([NotNullIfNotNull(nameof(value))] ref T location, T value) where T : class?
        {
            WriteBarrier();
            location = value;
        }

        /// <summary>
        /// Synchronizes memory access as follows:
        /// The processor that executes the current thread cannot reorder instructions in such a way that memory reads before
        /// the call to <see cref="ReadBarrier"/> execute after memory accesses that follow the call to <see cref="ReadBarrier"/>.
        /// </summary>
        [Intrinsic]
        public static void ReadBarrier() => ReadBarrier();

        /// <summary>
        /// Synchronizes memory access as follows:
        /// The processor that executes the current thread cannot reorder instructions in such a way that memory writes after
        /// the call to <see cref="WriteBarrier"/> execute before memory accesses that precede the call to <see cref="WriteBarrier"/>.
        /// </summary>
        [Intrinsic]
        public static void WriteBarrier() => WriteBarrier();
    }
}
