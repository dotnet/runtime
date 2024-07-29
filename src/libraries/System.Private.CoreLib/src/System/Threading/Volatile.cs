// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    /// <summary>Methods for accessing memory with volatile semantics.</summary>
    public static unsafe class Volatile
    {
        // The runtime may replace these implementations with more efficient ones in some cases.
        // In coreclr, for example, see importercalls.cpp.

        #region Boolean
        private struct VolatileBoolean { public volatile bool Value; }

        [Intrinsic]
        [NonVersionable]
        public static bool Read(ref readonly bool location) =>
            Unsafe.As<bool, VolatileBoolean>(ref Unsafe.AsRef(in location)).Value;

        [Intrinsic]
        [NonVersionable]
        public static void Write(ref bool location, bool value) =>
            Unsafe.As<bool, VolatileBoolean>(ref location).Value = value;
        #endregion

        #region Byte
        private struct VolatileByte { public volatile byte Value; }

        [Intrinsic]
        [NonVersionable]
        public static byte Read(ref readonly byte location) =>
            Unsafe.As<byte, VolatileByte>(ref Unsafe.AsRef(in location)).Value;

        [Intrinsic]
        [NonVersionable]
        public static void Write(ref byte location, byte value) =>
            Unsafe.As<byte, VolatileByte>(ref location).Value = value;
        #endregion

        #region Double
        [Intrinsic]
        [NonVersionable]
        public static double Read(ref readonly double location)
        {
            long result = Read(ref Unsafe.As<double, long>(ref Unsafe.AsRef(in location)));
            return BitConverter.Int64BitsToDouble(result);
        }

        [Intrinsic]
        [NonVersionable]
        public static void Write(ref double location, double value) =>
            Write(ref Unsafe.As<double, long>(ref location), BitConverter.DoubleToInt64Bits(value));
        #endregion

        #region Int16
        private struct VolatileInt16 { public volatile short Value; }

        [Intrinsic]
        [NonVersionable]
        public static short Read(ref readonly short location) =>
            Unsafe.As<short, VolatileInt16>(ref Unsafe.AsRef(in location)).Value;

        [Intrinsic]
        [NonVersionable]
        public static void Write(ref short location, short value) =>
            Unsafe.As<short, VolatileInt16>(ref location).Value = value;
        #endregion

        #region Int32
        private struct VolatileInt32 { public volatile int Value; }

        [Intrinsic]
        [NonVersionable]
        public static int Read(ref readonly int location) =>
            Unsafe.As<int, VolatileInt32>(ref Unsafe.AsRef(in location)).Value;

        [Intrinsic]
        [NonVersionable]
        public static void Write(ref int location, int value) =>
            Unsafe.As<int, VolatileInt32>(ref location).Value = value;
        #endregion

        #region Int64
        [Intrinsic]
        [NonVersionable]
        public static long Read(ref readonly long location) =>
#if TARGET_64BIT
            (long)Unsafe.As<long, VolatileIntPtr>(ref Unsafe.AsRef(in location)).Value;
#else
            // On 32-bit machines, we use Interlocked, since an ordinary volatile read would not be atomic.
            Interlocked.CompareExchange(ref Unsafe.AsRef(in location), 0, 0);
#endif

        [Intrinsic]
        [NonVersionable]
        public static void Write(ref long location, long value) =>
#if TARGET_64BIT
            Unsafe.As<long, VolatileIntPtr>(ref location).Value = (nint)value;
#else
            // On 32-bit, we use Interlocked, since an ordinary volatile write would not be atomic.
            Interlocked.Exchange(ref location, value);
#endif
        #endregion

        #region IntPtr
        private struct VolatileIntPtr { public volatile IntPtr Value; }

        [Intrinsic]
        [NonVersionable]
        public static IntPtr Read(ref readonly IntPtr location) =>
            Unsafe.As<IntPtr, VolatileIntPtr>(ref Unsafe.AsRef(in location)).Value;

        [Intrinsic]
        [NonVersionable]
        public static void Write(ref IntPtr location, IntPtr value) =>
            Unsafe.As<IntPtr, VolatileIntPtr>(ref location).Value = value;
        #endregion

        #region SByte
        private struct VolatileSByte { public volatile sbyte Value; }

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static sbyte Read(ref readonly sbyte location) =>
            Unsafe.As<sbyte, VolatileSByte>(ref Unsafe.AsRef(in location)).Value;

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static void Write(ref sbyte location, sbyte value) =>
            Unsafe.As<sbyte, VolatileSByte>(ref location).Value = value;
        #endregion

        #region Single
        private struct VolatileSingle { public volatile float Value; }

        [Intrinsic]
        [NonVersionable]
        public static float Read(ref readonly float location) =>
            Unsafe.As<float, VolatileSingle>(ref Unsafe.AsRef(in location)).Value;

        [Intrinsic]
        [NonVersionable]
        public static void Write(ref float location, float value) =>
            Unsafe.As<float, VolatileSingle>(ref location).Value = value;
        #endregion

        #region UInt16
        private struct VolatileUInt16 { public volatile ushort Value; }

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static ushort Read(ref readonly ushort location) =>
            Unsafe.As<ushort, VolatileUInt16>(ref Unsafe.AsRef(in location)).Value;

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static void Write(ref ushort location, ushort value) =>
            Unsafe.As<ushort, VolatileUInt16>(ref location).Value = value;
        #endregion

        #region UInt32
        private struct VolatileUInt32 { public volatile uint Value; }

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static uint Read(ref readonly uint location) =>
            Unsafe.As<uint, VolatileUInt32>(ref Unsafe.AsRef(in location)).Value;

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static void Write(ref uint location, uint value) =>
            Unsafe.As<uint, VolatileUInt32>(ref location).Value = value;
        #endregion

        #region UInt64
        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static ulong Read(ref readonly ulong location) =>
            (ulong)Read(ref Unsafe.As<ulong, long>(ref Unsafe.AsRef(in location)));

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static void Write(ref ulong location, ulong value) =>
            Write(ref Unsafe.As<ulong, long>(ref location), (long)value);
        #endregion

        #region UIntPtr
        private struct VolatileUIntPtr { public volatile UIntPtr Value; }

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static UIntPtr Read(ref readonly UIntPtr location) =>
            Unsafe.As<UIntPtr, VolatileUIntPtr>(ref Unsafe.AsRef(in location)).Value;

        [CLSCompliant(false)]
        [Intrinsic]
        [NonVersionable]
        public static void Write(ref UIntPtr location, UIntPtr value) =>
            Unsafe.As<UIntPtr, VolatileUIntPtr>(ref location).Value = value;
        #endregion

        #region T
        private struct VolatileObject { public volatile object? Value; }

        [Intrinsic]
        [NonVersionable]
        [return: NotNullIfNotNull(nameof(location))]
        public static T Read<T>([NotNullIfNotNull(nameof(location))] ref readonly T location) where T : class? =>
            Unsafe.As<T>(Unsafe.As<T, VolatileObject>(ref Unsafe.AsRef(in location)).Value);

        [Intrinsic]
        [NonVersionable]
        public static void Write<T>([NotNullIfNotNull(nameof(value))] ref T location, T value) where T : class? =>
            Unsafe.As<T, VolatileObject>(ref location).Value = value;
        #endregion
    }
}
