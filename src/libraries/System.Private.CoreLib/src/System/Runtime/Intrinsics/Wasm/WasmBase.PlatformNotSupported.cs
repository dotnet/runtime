// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.Wasm
{
    internal abstract class WasmBase
    {
        public static bool IsSupported { [Intrinsic] get { return false; } }

        public static Vector128<byte> Constant(Vector128<byte> v) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  Splat(sbyte  x) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   Splat(byte   x) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  Splat(short  x) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> Splat(ushort x) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    Splat(int    x) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   Splat(uint   x) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   Splat(long   x) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  Splat(ulong  x) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  Splat(float  x) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> Splat(double x) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   Splat(nint   x) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  Splat(nuint  x) { throw new PlatformNotSupportedException(); }

        public static int    ExtractLane(Vector128<sbyte>  a, byte imm) { throw new PlatformNotSupportedException(); }
        public static uint   ExtractLane(Vector128<byte>   a, byte imm) { throw new PlatformNotSupportedException(); }
        public static int    ExtractLane(Vector128<short>  a, byte imm) { throw new PlatformNotSupportedException(); }
        public static uint   ExtractLane(Vector128<ushort> a, byte imm) { throw new PlatformNotSupportedException(); }
        public static int    ExtractLane(Vector128<int>    a, byte imm) { throw new PlatformNotSupportedException(); }
        public static uint   ExtractLane(Vector128<uint>   a, byte imm) { throw new PlatformNotSupportedException(); }
        public static long   ExtractLane(Vector128<long>   a, byte imm) { throw new PlatformNotSupportedException(); }
        public static ulong  ExtractLane(Vector128<ulong>  a, byte imm) { throw new PlatformNotSupportedException(); }
        public static float  ExtractLane(Vector128<float>  a, byte imm) { throw new PlatformNotSupportedException(); }
        public static double ExtractLane(Vector128<double> a, byte imm) { throw new PlatformNotSupportedException(); }
        public static nint   ExtractLane(Vector128<nint>   a, byte imm) { throw new PlatformNotSupportedException(); }
        public static nuint  ExtractLane(Vector128<nuint>  a, byte imm) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte> Shuffle(Vector128<sbyte> a, Vector128<sbyte> b, Vector128<sbyte> imm) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>  Shuffle(Vector128<byte>  a, Vector128<byte>  b, Vector128<byte> imm) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte> Swizzle(Vector128<sbyte> a, Vector128<sbyte> s) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>  Swizzle(Vector128<byte>  a, Vector128<byte>  s) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  And(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   And(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  And(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> And(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    And(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   And(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   And(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  And(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  And(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> And(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   And(Vector128<nint> left, Vector128<nint> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  And(Vector128<nuint> left, Vector128<nuint> right) { throw new PlatformNotSupportedException(); }

        public static int Bitmask(Vector128<sbyte> a) { throw new PlatformNotSupportedException(); }
        public static int Bitmask(Vector128<byte> a) { throw new PlatformNotSupportedException(); }
        public static int Bitmask(Vector128<short> a) { throw new PlatformNotSupportedException(); }
        public static int Bitmask(Vector128<ushort> a) { throw new PlatformNotSupportedException(); }
        public static int Bitmask(Vector128<int> a) { throw new PlatformNotSupportedException(); }
        public static int Bitmask(Vector128<uint> a) { throw new PlatformNotSupportedException(); }
        public static int Bitmask(Vector128<long> a) { throw new PlatformNotSupportedException(); }
        public static int Bitmask(Vector128<ulong> a) { throw new PlatformNotSupportedException(); }
        public static int Bitmask(Vector128<nint> a) { throw new PlatformNotSupportedException(); }
        public static int Bitmask(Vector128<nuint> a) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  CompareEqual(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   CompareEqual(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  CompareEqual(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> CompareEqual(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    CompareEqual(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   CompareEqual(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   CompareEqual(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  CompareEqual(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  CompareEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> CompareEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   CompareEqual(Vector128<nint> left, Vector128<nint> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  CompareEqual(Vector128<nuint> left, Vector128<nuint> right) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  CompareNotEqual(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   CompareNotEqual(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  CompareNotEqual(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> CompareNotEqual(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    CompareNotEqual(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   CompareNotEqual(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   CompareNotEqual(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  CompareNotEqual(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  CompareNotEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> CompareNotEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   CompareNotEqual(Vector128<nint> left, Vector128<nint> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  CompareNotEqual(Vector128<nuint> left, Vector128<nuint> right) { throw new PlatformNotSupportedException(); }
    }
}
