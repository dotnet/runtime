// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

#pragma warning disable IDE0060

namespace System.Runtime.Intrinsics.Wasm
{
    [CLSCompliant(false)]
    public abstract class PackedSimd
    {
        public static bool IsSupported { [Intrinsic] get { return false; } }

        public static Vector128<sbyte>  Splat(sbyte  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   Splat(byte   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  Splat(short  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> Splat(ushort value) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    Splat(int    value) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   Splat(uint   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   Splat(long   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  Splat(ulong  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  Splat(float  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> Splat(double value) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   Splat(nint   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  Splat(nuint  value) { throw new PlatformNotSupportedException(); }

        public static int    ExtractLane(Vector128<sbyte>  value, byte index) { throw new PlatformNotSupportedException(); }
        public static uint   ExtractLane(Vector128<byte>   value, byte index) { throw new PlatformNotSupportedException(); }
        public static int    ExtractLane(Vector128<short>  value, byte index) { throw new PlatformNotSupportedException(); }
        public static uint   ExtractLane(Vector128<ushort> value, byte index) { throw new PlatformNotSupportedException(); }
        public static int    ExtractLane(Vector128<int>    value, byte index) { throw new PlatformNotSupportedException(); }
        public static uint   ExtractLane(Vector128<uint>   value, byte index) { throw new PlatformNotSupportedException(); }
        public static long   ExtractLane(Vector128<long>   value, byte index) { throw new PlatformNotSupportedException(); }
        public static ulong  ExtractLane(Vector128<ulong>  value, byte index) { throw new PlatformNotSupportedException(); }
        public static float  ExtractLane(Vector128<float>  value, byte index) { throw new PlatformNotSupportedException(); }
        public static double ExtractLane(Vector128<double> value, byte index) { throw new PlatformNotSupportedException(); }
        public static nint   ExtractLane(Vector128<nint>   value, byte index) { throw new PlatformNotSupportedException(); }
        public static nuint  ExtractLane(Vector128<nuint>  value, byte index) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  ReplaceLane(Vector128<sbyte>  vector, byte imm, int    value) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   ReplaceLane(Vector128<byte>   vector, byte imm, uint   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  ReplaceLane(Vector128<short>  vector, byte imm, int    value) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> ReplaceLane(Vector128<ushort> vector, byte imm, uint   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    ReplaceLane(Vector128<int>    vector, byte imm, int    value) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    ReplaceLane(Vector128<uint>   vector, byte imm, uint   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   ReplaceLane(Vector128<long>   vector, byte imm, long   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  ReplaceLane(Vector128<ulong>  vector, byte imm, ulong  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  ReplaceLane(Vector128<float>  vector, byte imm, float  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> ReplaceLane(Vector128<double> vector, byte imm, double value) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   ReplaceLane(Vector128<nint>   vector, byte imm, nint   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  ReplaceLane(Vector128<nuint>  vector, byte imm, nuint  value) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte> Shuffle(Vector128<sbyte> lower, Vector128<sbyte> upper, Vector128<sbyte> indices) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>  Shuffle(Vector128<byte>  lower, Vector128<byte>  upper, Vector128<byte>  indices) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte> Swizzle(Vector128<sbyte> vector, Vector128<sbyte> indices) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>  Swizzle(Vector128<byte>  vector, Vector128<byte>  indices) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  Add(Vector128<sbyte>  left, Vector128<sbyte>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   Add(Vector128<byte>   left, Vector128<byte>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  Add(Vector128<short>  left, Vector128<short>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> Add(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    Add(Vector128<int>    left, Vector128<int>    right) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   Add(Vector128<uint>   left, Vector128<uint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   Add(Vector128<long>   left, Vector128<long>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  Add(Vector128<ulong>  left, Vector128<ulong>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   Add(Vector128<nint>   left, Vector128<nint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  Add(Vector128<nuint>  left, Vector128<nuint>  right) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  Subtract(Vector128<sbyte>  left, Vector128<sbyte>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   Subtract(Vector128<byte>   left, Vector128<byte>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  Subtract(Vector128<short>  left, Vector128<short>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> Subtract(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    Subtract(Vector128<int>    left, Vector128<int>    right) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   Subtract(Vector128<uint>   left, Vector128<uint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   Subtract(Vector128<long>   left, Vector128<long>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  Subtract(Vector128<ulong>  left, Vector128<ulong>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   Subtract(Vector128<nint>   left, Vector128<nint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  Subtract(Vector128<nuint>  left, Vector128<nuint>  right) { throw new PlatformNotSupportedException(); }

        public static Vector128<short>  Multiply(Vector128<short>  left, Vector128<short>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> Multiply(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    Multiply(Vector128<int>    left, Vector128<int>    right) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   Multiply(Vector128<uint>   left, Vector128<uint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   Multiply(Vector128<long>   left, Vector128<long>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  Multiply(Vector128<ulong>  left, Vector128<ulong>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   Multiply(Vector128<nint>   left, Vector128<nint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  Multiply(Vector128<nuint>  left, Vector128<nuint>  right) { throw new PlatformNotSupportedException(); }

        public static Vector128<int> Dot(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  Negate(Vector128<sbyte>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   Negate(Vector128<byte>   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  Negate(Vector128<short>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> Negate(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    Negate(Vector128<int>    value) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   Negate(Vector128<uint>   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   Negate(Vector128<long>   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  Negate(Vector128<ulong>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   Negate(Vector128<nint>   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  Negate(Vector128<nuint>  value) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  And(Vector128<sbyte>  left, Vector128<sbyte>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   And(Vector128<byte>   left, Vector128<byte>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  And(Vector128<short>  left, Vector128<short>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> And(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    And(Vector128<int>    left, Vector128<int>    right) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   And(Vector128<uint>   left, Vector128<uint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   And(Vector128<long>   left, Vector128<long>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  And(Vector128<ulong>  left, Vector128<ulong>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  And(Vector128<float>  left, Vector128<float>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> And(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   And(Vector128<nint>   left, Vector128<nint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  And(Vector128<nuint>  left, Vector128<nuint>  right) { throw new PlatformNotSupportedException(); }

        public static int Bitmask(Vector128<sbyte>  value) { throw new PlatformNotSupportedException(); }
        public static int Bitmask(Vector128<byte>   value) { throw new PlatformNotSupportedException(); }
        public static int Bitmask(Vector128<short>  value) { throw new PlatformNotSupportedException(); }
        public static int Bitmask(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        public static int Bitmask(Vector128<int>    value) { throw new PlatformNotSupportedException(); }
        public static int Bitmask(Vector128<uint>   value) { throw new PlatformNotSupportedException(); }
        public static int Bitmask(Vector128<long>   value) { throw new PlatformNotSupportedException(); }
        public static int Bitmask(Vector128<ulong>  value) { throw new PlatformNotSupportedException(); }
        public static int Bitmask(Vector128<nint>   value) { throw new PlatformNotSupportedException(); }
        public static int Bitmask(Vector128<nuint>  value) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  CompareEqual(Vector128<sbyte>  left, Vector128<sbyte>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   CompareEqual(Vector128<byte>   left, Vector128<byte>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  CompareEqual(Vector128<short>  left, Vector128<short>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> CompareEqual(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    CompareEqual(Vector128<int>    left, Vector128<int>    right) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   CompareEqual(Vector128<uint>   left, Vector128<uint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   CompareEqual(Vector128<long>   left, Vector128<long>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  CompareEqual(Vector128<ulong>  left, Vector128<ulong>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  CompareEqual(Vector128<float>  left, Vector128<float>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> CompareEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   CompareEqual(Vector128<nint>   left, Vector128<nint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  CompareEqual(Vector128<nuint>  left, Vector128<nuint>  right) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  CompareNotEqual(Vector128<sbyte>  left, Vector128<sbyte>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   CompareNotEqual(Vector128<byte>   left, Vector128<byte>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  CompareNotEqual(Vector128<short>  left, Vector128<short>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> CompareNotEqual(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    CompareNotEqual(Vector128<int>    left, Vector128<int>    right) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   CompareNotEqual(Vector128<uint>   left, Vector128<uint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   CompareNotEqual(Vector128<long>   left, Vector128<long>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  CompareNotEqual(Vector128<ulong>  left, Vector128<ulong>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  CompareNotEqual(Vector128<float>  left, Vector128<float>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> CompareNotEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   CompareNotEqual(Vector128<nint>   left, Vector128<nint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  CompareNotEqual(Vector128<nuint>  left, Vector128<nuint>  right) { throw new PlatformNotSupportedException(); }
    }
}
