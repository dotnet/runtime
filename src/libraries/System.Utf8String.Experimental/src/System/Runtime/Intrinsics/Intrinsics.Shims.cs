// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Intrinsics
{
    internal static class Vector64
    {
        public static Vector64<ulong> Create(ulong value) => throw new PlatformNotSupportedException();
        public static Vector64<uint> CreateScalar(uint value) => throw new PlatformNotSupportedException();
        public static Vector64<byte> AsByte<T>(this Vector64<T> vector) where T : struct => throw new PlatformNotSupportedException();
        public static Vector64<uint> AsUInt32<T>(this Vector64<T> vector) where T : struct => throw new PlatformNotSupportedException();
        public static Vector64<T> GetLower<T>(this Vector128<T> vector) where T : struct => throw new PlatformNotSupportedException();
        public static Vector64<ulong> AsUInt64<T>(this Vector64<T> vector) where T : struct => throw new PlatformNotSupportedException();
    }
    internal readonly struct Vector64<T>
        where T : struct
    {
    }

    internal static class Vector128
    {
        public static Vector128<long> Create(long value) => throw new PlatformNotSupportedException();
        public static Vector128<short> Create(short value) => throw new PlatformNotSupportedException();
        public static Vector128<ulong> Create(ulong value) => throw new PlatformNotSupportedException();
        public static Vector128<ushort> Create(ushort value) => throw new PlatformNotSupportedException();
        public static Vector128<byte> Create(byte value) => throw new PlatformNotSupportedException();
        public static Vector128<uint> Create(uint value) => throw new PlatformNotSupportedException();
        public static Vector128<ulong> CreateScalarUnsafe(ulong value) => throw new PlatformNotSupportedException();
        public static Vector128<byte> AsByte<T>(this Vector128<T> vector) where T : struct => throw new PlatformNotSupportedException();
        public static Vector128<short> AsInt16<T>(this Vector128<T> vector) where T : struct => throw new PlatformNotSupportedException();
        public static Vector128<sbyte> AsSByte<T>(this Vector128<T> vector) where T : struct => throw new PlatformNotSupportedException();
        public static Vector128<ushort> AsUInt16<T>(this Vector128<T> vector) where T : struct => throw new PlatformNotSupportedException();
        public static Vector128<uint> AsUInt32<T>(this Vector128<T> vector) where T : struct => throw new PlatformNotSupportedException();
        public static Vector128<ulong> AsUInt64<T>(this Vector128<T> vector) where T : struct => throw new PlatformNotSupportedException();
        public static T GetElement<T>(this Vector128<T> vector, int index) where T : struct => throw new PlatformNotSupportedException();
        public static T ToScalar<T>(this Vector64<T> vector) where T : struct => throw new PlatformNotSupportedException();
        public static unsafe Vector128<ulong> CreateScalar(ulong value) => throw new PlatformNotSupportedException();
        public static T ToScalar<T>(this Vector128<T> vector) where T : struct => throw new PlatformNotSupportedException();
    }
    internal readonly struct Vector128<T>
        where T : struct
    {
        public static Vector128<T> Zero => throw new PlatformNotSupportedException();
        public static int Count => throw new PlatformNotSupportedException();
    }
}

namespace System.Runtime.Intrinsics.X86
{
    internal static class X86Base
    {
        internal static class X64
        {
            public const bool IsSupported = false;
            internal static ulong BitScanForward(ulong value) => throw new PlatformNotSupportedException();
            internal static ulong BitScanReverse(ulong value) => throw new PlatformNotSupportedException();
        }
        public const bool IsSupported = false;
        internal static uint BitScanForward(uint value) => throw new PlatformNotSupportedException();
        internal static uint BitScanReverse(uint value) => throw new PlatformNotSupportedException();
    }
    internal abstract class Bmi1
    {
        public abstract class X64
        {
            public const bool IsSupported = false;
            public static ulong TrailingZeroCount(ulong value) => throw new PlatformNotSupportedException();
        }
        public const bool IsSupported = false;
        public static uint TrailingZeroCount(uint value) => throw new PlatformNotSupportedException();
    }
    internal abstract class Lzcnt
    {
        public abstract class X64
        {
            public const bool IsSupported = false;
            public static ulong LeadingZeroCount(ulong value) => throw new PlatformNotSupportedException();
        }
        public const bool IsSupported = false;
        public static uint LeadingZeroCount(uint value) => throw new PlatformNotSupportedException();
    }
    internal abstract class Popcnt
    {
        public abstract class X64
        {
            public const bool IsSupported = false;
            public static ulong PopCount(ulong value) => throw new PlatformNotSupportedException();
        }
        public const bool IsSupported = false;
        public static uint PopCount(uint value) => throw new PlatformNotSupportedException();
    }

    internal abstract class Sse2
    {
        public abstract class X64
        {
            public const bool IsSupported = false;
            public static Vector128<ulong> ConvertScalarToVector128UInt64(ulong value) => throw new PlatformNotSupportedException();
            public static ulong ConvertToUInt64(Vector128<ulong> value) => throw new PlatformNotSupportedException();
        }
        public const bool IsSupported = false;
        public static Vector128<ushort> Add(Vector128<ushort> left, Vector128<ushort> right) => throw new PlatformNotSupportedException();
        public static Vector128<ushort> AddSaturate(Vector128<ushort> left, Vector128<ushort> right) => throw new PlatformNotSupportedException();
        public static Vector128<ushort> AndNot(Vector128<ushort> left, Vector128<ushort> right) => throw new PlatformNotSupportedException();
        public static Vector128<short> CompareGreaterThan(Vector128<short> left, Vector128<short> right) => throw new PlatformNotSupportedException();
        public static Vector128<short> CompareLessThan(Vector128<short> left, Vector128<short> right) => throw new PlatformNotSupportedException();
        public static Vector128<uint> ConvertScalarToVector128UInt32(uint value) => throw new PlatformNotSupportedException();
        public static uint ConvertToUInt32(Vector128<uint> value) => throw new PlatformNotSupportedException();
        public static unsafe Vector128<byte> LoadAlignedVector128(byte* address) => throw new PlatformNotSupportedException();
        public static unsafe Vector128<ushort> LoadAlignedVector128(ushort* address) => throw new PlatformNotSupportedException();
        public static unsafe Vector128<byte> LoadVector128(byte* address) => throw new PlatformNotSupportedException();
        public static unsafe Vector128<short> LoadVector128(short* address) => throw new PlatformNotSupportedException();
        public static unsafe Vector128<ushort> LoadVector128(ushort* address) => throw new PlatformNotSupportedException();
        public static int MoveMask(Vector128<byte> value) => throw new PlatformNotSupportedException();
        public static Vector128<short> Or(Vector128<short> left, Vector128<short> right) => throw new PlatformNotSupportedException();
        public static Vector128<ushort> Or(Vector128<ushort> left, Vector128<ushort> right) => throw new PlatformNotSupportedException();
        public static Vector128<byte> PackUnsignedSaturate(Vector128<short> left, Vector128<short> right) => throw new PlatformNotSupportedException();
        public static Vector128<ushort> ShiftRightLogical(Vector128<ushort> value, byte count) => throw new PlatformNotSupportedException();
        public static unsafe void Store(byte* address, Vector128<byte> source) => throw new PlatformNotSupportedException();
        public static unsafe void StoreAligned(byte* address, Vector128<byte> source) => throw new PlatformNotSupportedException();
        public static unsafe void StoreScalar(ulong* address, Vector128<ulong> source) => throw new PlatformNotSupportedException();
        public static Vector128<ushort> Subtract(Vector128<ushort> left, Vector128<ushort> right) => throw new PlatformNotSupportedException();
        public static Vector128<byte> UnpackHigh(Vector128<byte> left, Vector128<byte> right) => throw new PlatformNotSupportedException();
        public static Vector128<byte> UnpackLow(Vector128<byte> left, Vector128<byte> right) => throw new PlatformNotSupportedException();
    }

    internal abstract class Sse41
    {
        public abstract class X64
        {
            public const bool IsSupported = false;
        }
        public const bool IsSupported = false;
        public static Vector128<ushort> Min(Vector128<ushort> left, Vector128<ushort> right) => throw new PlatformNotSupportedException();
        public static bool TestZ(Vector128<short> left, Vector128<short> right) => throw new PlatformNotSupportedException();
        public static bool TestZ(Vector128<ushort> left, Vector128<ushort> right) => throw new PlatformNotSupportedException();
    }
}

namespace System.Runtime.CompilerServices
{
    // Calls to methods or references to fields marked with this attribute may be replaced at
    // some call sites with jit intrinsic expansions.
    // Types marked with this attribute may be specially treated by the runtime/compiler.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Field, Inherited = false)]
    internal sealed class IntrinsicAttribute : Attribute
    {
    }
}
