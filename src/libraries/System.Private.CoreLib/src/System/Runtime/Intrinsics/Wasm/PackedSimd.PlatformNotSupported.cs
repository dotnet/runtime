// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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

        public static int    ExtractLane(Vector128<sbyte>  value, [ConstantExpected(Max = (byte)(15))] byte index) { throw new PlatformNotSupportedException(); }
        public static uint   ExtractLane(Vector128<byte>   value, [ConstantExpected(Max = (byte)(15))] byte index) { throw new PlatformNotSupportedException(); }
        public static int    ExtractLane(Vector128<short>  value, [ConstantExpected(Max = (byte)(7))] byte index) { throw new PlatformNotSupportedException(); }
        public static uint   ExtractLane(Vector128<ushort> value, [ConstantExpected(Max = (byte)(7))] byte index) { throw new PlatformNotSupportedException(); }
        public static int    ExtractLane(Vector128<int>    value, [ConstantExpected(Max = (byte)(3))] byte index) { throw new PlatformNotSupportedException(); }
        public static uint   ExtractLane(Vector128<uint>   value, [ConstantExpected(Max = (byte)(3))] byte index) { throw new PlatformNotSupportedException(); }
        public static long   ExtractLane(Vector128<long>   value, [ConstantExpected(Max = (byte)(1))] byte index) { throw new PlatformNotSupportedException(); }
        public static ulong  ExtractLane(Vector128<ulong>  value, [ConstantExpected(Max = (byte)(1))] byte index) { throw new PlatformNotSupportedException(); }
        public static float  ExtractLane(Vector128<float>  value, [ConstantExpected(Max = (byte)(3))] byte index) { throw new PlatformNotSupportedException(); }
        public static double ExtractLane(Vector128<double> value, [ConstantExpected(Max = (byte)(1))] byte index) { throw new PlatformNotSupportedException(); }
        public static nint   ExtractLane(Vector128<nint>   value, [ConstantExpected(Max = (byte)(3))] byte index) { throw new PlatformNotSupportedException(); }
        public static nuint  ExtractLane(Vector128<nuint>  value, [ConstantExpected(Max = (byte)(3))] byte index) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  ReplaceLane(Vector128<sbyte>  vector, [ConstantExpected(Max = (byte)(15))] byte imm, int    value) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   ReplaceLane(Vector128<byte>   vector, [ConstantExpected(Max = (byte)(15))] byte imm, uint   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  ReplaceLane(Vector128<short>  vector, [ConstantExpected(Max = (byte)(7))] byte imm, int    value) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> ReplaceLane(Vector128<ushort> vector, [ConstantExpected(Max = (byte)(7))] byte imm, uint   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    ReplaceLane(Vector128<int>    vector, [ConstantExpected(Max = (byte)(3))] byte imm, int    value) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    ReplaceLane(Vector128<uint>   vector, [ConstantExpected(Max = (byte)(3))] byte imm, uint   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   ReplaceLane(Vector128<long>   vector, [ConstantExpected(Max = (byte)(1))] byte imm, long   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  ReplaceLane(Vector128<ulong>  vector, [ConstantExpected(Max = (byte)(1))] byte imm, ulong  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  ReplaceLane(Vector128<float>  vector, [ConstantExpected(Max = (byte)(3))] byte imm, float  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> ReplaceLane(Vector128<double> vector, [ConstantExpected(Max = (byte)(1))] byte imm, double value) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   ReplaceLane(Vector128<nint>   vector, [ConstantExpected(Max = (byte)(3))] byte imm, nint   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  ReplaceLane(Vector128<nuint>  vector, [ConstantExpected(Max = (byte)(3))] byte imm, nuint  value) { throw new PlatformNotSupportedException(); }

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

        // Extended integer arithmetic

        public static Vector128<short>  MultiplyWideningLower(Vector128<sbyte>  left, Vector128<sbyte>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> MultiplyWideningLower(Vector128<byte>   left, Vector128<byte>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    MultiplyWideningLower(Vector128<short>  left, Vector128<short>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   MultiplyWideningLower(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   MultiplyWideningLower(Vector128<int>    left, Vector128<int>    right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  MultiplyWideningLower(Vector128<uint>   left, Vector128<uint>   right) { throw new PlatformNotSupportedException(); }

        public static Vector128<short>  MultiplyWideningUpper(Vector128<sbyte>  left, Vector128<sbyte>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> MultiplyWideningUpper(Vector128<byte>   left, Vector128<byte>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    MultiplyWideningUpper(Vector128<short>  left, Vector128<short>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   MultiplyWideningUpper(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   MultiplyWideningUpper(Vector128<int>    left, Vector128<int>    right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  MultiplyWideningUpper(Vector128<uint>   left, Vector128<uint>   right) { throw new PlatformNotSupportedException(); }

        public static Vector128<short>  AddPairwiseWidening(Vector128<sbyte>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> AddPairwiseWidening(Vector128<byte>   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    AddPairwiseWidening(Vector128<short>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   AddPairwiseWidening(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }

        // Saturating integer arithmetic

        public static Vector128<sbyte>  AddSaturate(Vector128<sbyte>  left, Vector128<sbyte>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   AddSaturate(Vector128<byte>   left, Vector128<byte>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  AddSaturate(Vector128<short>  left, Vector128<short>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> AddSaturate(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  SubtractSaturate(Vector128<sbyte>  left, Vector128<sbyte>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   SubtractSaturate(Vector128<byte>   left, Vector128<byte>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  SubtractSaturate(Vector128<short>  left, Vector128<short>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> SubtractSaturate(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

        public static Vector128<short> MultiplyRoundedSaturateQ15(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  Min(Vector128<sbyte>  left, Vector128<sbyte>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   Min(Vector128<byte>   left, Vector128<byte>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  Min(Vector128<short>  left, Vector128<short>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> Min(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    Min(Vector128<int>    left, Vector128<int>    right) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   Min(Vector128<uint>   left, Vector128<uint>   right) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  Max(Vector128<sbyte>  left, Vector128<sbyte>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   Max(Vector128<byte>   left, Vector128<byte>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  Max(Vector128<short>  left, Vector128<short>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> Max(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    Max(Vector128<int>    left, Vector128<int>    right) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   Max(Vector128<uint>   left, Vector128<uint>   right) { throw new PlatformNotSupportedException(); }

        public static Vector128<byte>   AverageRounded(Vector128<byte>   left, Vector128<byte>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> AverageRounded(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte> Abs(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        public static Vector128<short> Abs(Vector128<short> value) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>   Abs(Vector128<int>   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>  Abs(Vector128<long>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>  Abs(Vector128<nint>  value) { throw new PlatformNotSupportedException(); }

        // Bit shifts

        public static Vector128<sbyte>  ShiftLeft(Vector128<sbyte>  value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   ShiftLeft(Vector128<byte>   value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  ShiftLeft(Vector128<short>  value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> ShiftLeft(Vector128<ushort> value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    ShiftLeft(Vector128<int>    value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   ShiftLeft(Vector128<uint>   value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   ShiftLeft(Vector128<long>   value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  ShiftLeft(Vector128<ulong>  value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   ShiftLeft(Vector128<nint>   value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  ShiftLeft(Vector128<nuint>  value, int count) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  ShiftRightArithmetic(Vector128<sbyte>  value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   ShiftRightArithmetic(Vector128<byte>   value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  ShiftRightArithmetic(Vector128<short>  value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> ShiftRightArithmetic(Vector128<ushort> value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    ShiftRightArithmetic(Vector128<int>    value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   ShiftRightArithmetic(Vector128<uint>   value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   ShiftRightArithmetic(Vector128<long>   value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  ShiftRightArithmetic(Vector128<ulong>  value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   ShiftRightArithmetic(Vector128<nint>   value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  ShiftRightArithmetic(Vector128<nuint>  value, int count) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  ShiftRightLogical(Vector128<sbyte>  value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   ShiftRightLogical(Vector128<byte>   value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  ShiftRightLogical(Vector128<short>  value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> ShiftRightLogical(Vector128<ushort> value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    ShiftRightLogical(Vector128<int>    value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   ShiftRightLogical(Vector128<uint>   value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   ShiftRightLogical(Vector128<long>   value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  ShiftRightLogical(Vector128<ulong>  value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   ShiftRightLogical(Vector128<nint>   value, int count) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  ShiftRightLogical(Vector128<nuint>  value, int count) { throw new PlatformNotSupportedException(); }

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

        public static Vector128<sbyte>  Or(Vector128<sbyte>  left, Vector128<sbyte>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   Or(Vector128<byte>   left, Vector128<byte>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  Or(Vector128<short>  left, Vector128<short>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> Or(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    Or(Vector128<int>    left, Vector128<int>    right) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   Or(Vector128<uint>   left, Vector128<uint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   Or(Vector128<long>   left, Vector128<long>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  Or(Vector128<ulong>  left, Vector128<ulong>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  Or(Vector128<float>  left, Vector128<float>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> Or(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   Or(Vector128<nint>   left, Vector128<nint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  Or(Vector128<nuint>  left, Vector128<nuint>  right) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  Xor(Vector128<sbyte>  left, Vector128<sbyte>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   Xor(Vector128<byte>   left, Vector128<byte>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  Xor(Vector128<short>  left, Vector128<short>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> Xor(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    Xor(Vector128<int>    left, Vector128<int>    right) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   Xor(Vector128<uint>   left, Vector128<uint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   Xor(Vector128<long>   left, Vector128<long>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  Xor(Vector128<ulong>  left, Vector128<ulong>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  Xor(Vector128<float>  left, Vector128<float>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> Xor(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   Xor(Vector128<nint>   left, Vector128<nint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  Xor(Vector128<nuint>  left, Vector128<nuint>  right) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  Not(Vector128<sbyte>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   Not(Vector128<byte>   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  Not(Vector128<short>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> Not(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    Not(Vector128<int>    value) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   Not(Vector128<uint>   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   Not(Vector128<long>   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  Not(Vector128<ulong>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  Not(Vector128<float>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> Not(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   Not(Vector128<nint>   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  Not(Vector128<nuint>  value) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  AndNot(Vector128<sbyte>  left, Vector128<sbyte>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   AndNot(Vector128<byte>   left, Vector128<byte>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  AndNot(Vector128<short>  left, Vector128<short>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> AndNot(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    AndNot(Vector128<int>    left, Vector128<int>    right) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   AndNot(Vector128<uint>   left, Vector128<uint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   AndNot(Vector128<long>   left, Vector128<long>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  AndNot(Vector128<ulong>  left, Vector128<ulong>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  AndNot(Vector128<float>  left, Vector128<float>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> AndNot(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   AndNot(Vector128<nint>   left, Vector128<nint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  AndNot(Vector128<nuint>  left, Vector128<nuint>  right) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  BitwiseSelect(Vector128<sbyte>  left, Vector128<sbyte>  right, Vector128<sbyte>  select) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   BitwiseSelect(Vector128<byte>   left, Vector128<byte>   right, Vector128<byte>   select) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  BitwiseSelect(Vector128<short>  left, Vector128<short>  right, Vector128<short>  select) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> BitwiseSelect(Vector128<ushort> left, Vector128<ushort> right, Vector128<ushort> select) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    BitwiseSelect(Vector128<int>    left, Vector128<int>    right, Vector128<int>    select) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   BitwiseSelect(Vector128<uint>   left, Vector128<uint>   right, Vector128<uint>   select) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   BitwiseSelect(Vector128<long>   left, Vector128<long>   right, Vector128<long>   select) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  BitwiseSelect(Vector128<ulong>  left, Vector128<ulong>  right, Vector128<ulong>  select) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  BitwiseSelect(Vector128<float>  left, Vector128<float>  right, Vector128<float>  select) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> BitwiseSelect(Vector128<double> left, Vector128<double> right, Vector128<double> select) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   BitwiseSelect(Vector128<nint>   left, Vector128<nint>   right, Vector128<nint>   select) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  BitwiseSelect(Vector128<nuint>  left, Vector128<nuint>  right, Vector128<nuint>  select) { throw new PlatformNotSupportedException(); }

        public static Vector128<byte> PopCount(Vector128<byte> value) { throw new PlatformNotSupportedException(); }

        // Boolean horizontal reductions

        public static bool AnyTrue(Vector128<sbyte>  value) { throw new PlatformNotSupportedException(); }
        public static bool AnyTrue(Vector128<byte>   value) { throw new PlatformNotSupportedException(); }
        public static bool AnyTrue(Vector128<short>  value) { throw new PlatformNotSupportedException(); }
        public static bool AnyTrue(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        public static bool AnyTrue(Vector128<int>    value) { throw new PlatformNotSupportedException(); }
        public static bool AnyTrue(Vector128<uint>   value) { throw new PlatformNotSupportedException(); }
        public static bool AnyTrue(Vector128<long>   value) { throw new PlatformNotSupportedException(); }
        public static bool AnyTrue(Vector128<ulong>  value) { throw new PlatformNotSupportedException(); }
        public static bool AnyTrue(Vector128<float>  value) { throw new PlatformNotSupportedException(); }
        public static bool AnyTrue(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        public static bool AnyTrue(Vector128<nint>   value) { throw new PlatformNotSupportedException(); }
        public static bool AnyTrue(Vector128<nuint>  value) { throw new PlatformNotSupportedException(); }

        public static bool AllTrue(Vector128<sbyte>  value) { throw new PlatformNotSupportedException(); }
        public static bool AllTrue(Vector128<byte>   value) { throw new PlatformNotSupportedException(); }
        public static bool AllTrue(Vector128<short>  value) { throw new PlatformNotSupportedException(); }
        public static bool AllTrue(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        public static bool AllTrue(Vector128<int>    value) { throw new PlatformNotSupportedException(); }
        public static bool AllTrue(Vector128<uint>   value) { throw new PlatformNotSupportedException(); }
        public static bool AllTrue(Vector128<long>   value) { throw new PlatformNotSupportedException(); }
        public static bool AllTrue(Vector128<ulong>  value) { throw new PlatformNotSupportedException(); }
        public static bool AllTrue(Vector128<nint>   value) { throw new PlatformNotSupportedException(); }
        public static bool AllTrue(Vector128<nuint>  value) { throw new PlatformNotSupportedException(); }

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

        // Comparisons

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

        public static Vector128<sbyte>  CompareLessThan(Vector128<sbyte>  left, Vector128<sbyte>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   CompareLessThan(Vector128<byte>   left, Vector128<byte>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  CompareLessThan(Vector128<short>  left, Vector128<short>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> CompareLessThan(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    CompareLessThan(Vector128<int>    left, Vector128<int>    right) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   CompareLessThan(Vector128<uint>   left, Vector128<uint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   CompareLessThan(Vector128<long>   left, Vector128<long>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  CompareLessThan(Vector128<ulong>  left, Vector128<ulong>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  CompareLessThan(Vector128<float>  left, Vector128<float>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> CompareLessThan(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   CompareLessThan(Vector128<nint>   left, Vector128<nint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  CompareLessThan(Vector128<nuint>  left, Vector128<nuint>  right) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  CompareLessThanOrEqual(Vector128<sbyte>  left, Vector128<sbyte>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   CompareLessThanOrEqual(Vector128<byte>   left, Vector128<byte>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  CompareLessThanOrEqual(Vector128<short>  left, Vector128<short>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> CompareLessThanOrEqual(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    CompareLessThanOrEqual(Vector128<int>    left, Vector128<int>    right) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   CompareLessThanOrEqual(Vector128<uint>   left, Vector128<uint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   CompareLessThanOrEqual(Vector128<long>   left, Vector128<long>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  CompareLessThanOrEqual(Vector128<ulong>  left, Vector128<ulong>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  CompareLessThanOrEqual(Vector128<float>  left, Vector128<float>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> CompareLessThanOrEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   CompareLessThanOrEqual(Vector128<nint>   left, Vector128<nint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  CompareLessThanOrEqual(Vector128<nuint>  left, Vector128<nuint>  right) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  CompareGreaterThan(Vector128<sbyte>  left, Vector128<sbyte>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   CompareGreaterThan(Vector128<byte>   left, Vector128<byte>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  CompareGreaterThan(Vector128<short>  left, Vector128<short>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> CompareGreaterThan(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    CompareGreaterThan(Vector128<int>    left, Vector128<int>    right) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   CompareGreaterThan(Vector128<uint>   left, Vector128<uint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   CompareGreaterThan(Vector128<long>   left, Vector128<long>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  CompareGreaterThan(Vector128<ulong>  left, Vector128<ulong>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  CompareGreaterThan(Vector128<float>  left, Vector128<float>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> CompareGreaterThan(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   CompareGreaterThan(Vector128<nint>   left, Vector128<nint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  CompareGreaterThan(Vector128<nuint>  left, Vector128<nuint>  right) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  CompareGreaterThanOrEqual(Vector128<sbyte>  left, Vector128<sbyte>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   CompareGreaterThanOrEqual(Vector128<byte>   left, Vector128<byte>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  CompareGreaterThanOrEqual(Vector128<short>  left, Vector128<short>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> CompareGreaterThanOrEqual(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    CompareGreaterThanOrEqual(Vector128<int>    left, Vector128<int>    right) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   CompareGreaterThanOrEqual(Vector128<uint>   left, Vector128<uint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   CompareGreaterThanOrEqual(Vector128<long>   left, Vector128<long>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  CompareGreaterThanOrEqual(Vector128<ulong>  left, Vector128<ulong>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  CompareGreaterThanOrEqual(Vector128<float>  left, Vector128<float>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> CompareGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   CompareGreaterThanOrEqual(Vector128<nint>   left, Vector128<nint>   right) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  CompareGreaterThanOrEqual(Vector128<nuint>  left, Vector128<nuint>  right) { throw new PlatformNotSupportedException(); }

        // Conversions

        internal static Vector128<sbyte>  ConvertNarrowingSignedSaturate(Vector128<short>   lower, Vector128<short> upper) { throw new PlatformNotSupportedException(); }
        internal static Vector128<short>  ConvertNarrowingSignedSaturate(Vector128<int>     lower, Vector128<int>   upper) { throw new PlatformNotSupportedException(); }

        internal static Vector128<byte>   ConvertNarrowingUnsignedSaturate(Vector128<short> lower, Vector128<short> upper) { throw new PlatformNotSupportedException(); }
        internal static Vector128<ushort> ConvertNarrowingUnsignedSaturate(Vector128<int>   lower, Vector128<int>   upper) { throw new PlatformNotSupportedException(); }
    }
}
