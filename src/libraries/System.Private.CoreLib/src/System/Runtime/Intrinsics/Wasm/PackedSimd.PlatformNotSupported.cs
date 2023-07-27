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

        public static int    ExtractScalar(Vector128<sbyte>  value, [ConstantExpected(Max = (byte)(15))] byte index) { throw new PlatformNotSupportedException(); }
        public static uint   ExtractScalar(Vector128<byte>   value, [ConstantExpected(Max = (byte)(15))] byte index) { throw new PlatformNotSupportedException(); }
        public static int    ExtractScalar(Vector128<short>  value, [ConstantExpected(Max = (byte)(7))] byte index) { throw new PlatformNotSupportedException(); }
        public static uint   ExtractScalar(Vector128<ushort> value, [ConstantExpected(Max = (byte)(7))] byte index) { throw new PlatformNotSupportedException(); }
        public static int    ExtractScalar(Vector128<int>    value, [ConstantExpected(Max = (byte)(3))] byte index) { throw new PlatformNotSupportedException(); }
        public static uint   ExtractScalar(Vector128<uint>   value, [ConstantExpected(Max = (byte)(3))] byte index) { throw new PlatformNotSupportedException(); }
        public static long   ExtractScalar(Vector128<long>   value, [ConstantExpected(Max = (byte)(1))] byte index) { throw new PlatformNotSupportedException(); }
        public static ulong  ExtractScalar(Vector128<ulong>  value, [ConstantExpected(Max = (byte)(1))] byte index) { throw new PlatformNotSupportedException(); }
        public static float  ExtractScalar(Vector128<float>  value, [ConstantExpected(Max = (byte)(3))] byte index) { throw new PlatformNotSupportedException(); }
        public static double ExtractScalar(Vector128<double> value, [ConstantExpected(Max = (byte)(1))] byte index) { throw new PlatformNotSupportedException(); }
        public static nint   ExtractScalar(Vector128<nint>   value, [ConstantExpected(Max = (byte)(3))] byte index) { throw new PlatformNotSupportedException(); }
        public static nuint  ExtractScalar(Vector128<nuint>  value, [ConstantExpected(Max = (byte)(3))] byte index) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  ReplaceScalar(Vector128<sbyte>  vector, [ConstantExpected(Max = (byte)(15))] byte imm, int    value) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   ReplaceScalar(Vector128<byte>   vector, [ConstantExpected(Max = (byte)(15))] byte imm, uint   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  ReplaceScalar(Vector128<short>  vector, [ConstantExpected(Max = (byte)(7))] byte imm, int    value) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> ReplaceScalar(Vector128<ushort> vector, [ConstantExpected(Max = (byte)(7))] byte imm, uint   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    ReplaceScalar(Vector128<int>    vector, [ConstantExpected(Max = (byte)(3))] byte imm, int    value) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    ReplaceScalar(Vector128<uint>   vector, [ConstantExpected(Max = (byte)(3))] byte imm, uint   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   ReplaceScalar(Vector128<long>   vector, [ConstantExpected(Max = (byte)(1))] byte imm, long   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  ReplaceScalar(Vector128<ulong>  vector, [ConstantExpected(Max = (byte)(1))] byte imm, ulong  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  ReplaceScalar(Vector128<float>  vector, [ConstantExpected(Max = (byte)(3))] byte imm, float  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> ReplaceScalar(Vector128<double> vector, [ConstantExpected(Max = (byte)(1))] byte imm, double value) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   ReplaceScalar(Vector128<nint>   vector, [ConstantExpected(Max = (byte)(3))] byte imm, nint   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  ReplaceScalar(Vector128<nuint>  vector, [ConstantExpected(Max = (byte)(3))] byte imm, nuint  value) { throw new PlatformNotSupportedException(); }

        internal static Vector128<sbyte> Shuffle(Vector128<sbyte> lower, Vector128<sbyte> upper, Vector128<sbyte> indices) { throw new PlatformNotSupportedException(); }
        internal static Vector128<byte>  Shuffle(Vector128<byte>  lower, Vector128<byte>  upper, Vector128<byte>  indices) { throw new PlatformNotSupportedException(); }

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

        // Load

        public static unsafe Vector128<sbyte>  LoadVector128(sbyte*  address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<byte>   LoadVector128(byte*   address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<short>  LoadVector128(short*  address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<ushort> LoadVector128(ushort* address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<int>    LoadVector128(int*    address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<uint>   LoadVector128(uint*   address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<long>   LoadVector128(long*   address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<ulong>  LoadVector128(ulong*  address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<float>  LoadVector128(float*  address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<double> LoadVector128(double* address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<nint>   LoadVector128(nint*   address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<nuint>  LoadVector128(nuint*  address) { throw new PlatformNotSupportedException(); }

        public static unsafe Vector128<int>    LoadScalarVector128(int*    address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<uint>   LoadScalarVector128(uint*   address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<long>   LoadScalarVector128(long*   address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<ulong>  LoadScalarVector128(ulong*  address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<float>  LoadScalarVector128(float*  address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<double> LoadScalarVector128(double* address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<nint>   LoadScalarVector128(nint*   address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<nuint>  LoadScalarVector128(nuint*  address) { throw new PlatformNotSupportedException(); }

        public static unsafe Vector128<sbyte>  LoadScalarAndSplatVector128(sbyte*  address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<byte>   LoadScalarAndSplatVector128(byte*   address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<short>  LoadScalarAndSplatVector128(short*  address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<ushort> LoadScalarAndSplatVector128(ushort* address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<int>    LoadScalarAndSplatVector128(int*    address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<uint>   LoadScalarAndSplatVector128(uint*   address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<long>   LoadScalarAndSplatVector128(long*   address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<ulong>  LoadScalarAndSplatVector128(ulong*  address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<float>  LoadScalarAndSplatVector128(float*  address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<double> LoadScalarAndSplatVector128(double* address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<nint>   LoadScalarAndSplatVector128(nint*   address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<nuint>  LoadScalarAndSplatVector128(nuint*  address) { throw new PlatformNotSupportedException(); }

        public static unsafe Vector128<sbyte>  LoadScalarAndInsert(sbyte*  address, Vector128<sbyte>  vector, [ConstantExpected(Max = (byte)(15))] byte index) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<byte>   LoadScalarAndInsert(byte*   address, Vector128<byte>   vector, [ConstantExpected(Max = (byte)(15))] byte index) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<short>  LoadScalarAndInsert(short*  address, Vector128<short>  vector, [ConstantExpected(Max = (byte)(7))] byte index) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<ushort> LoadScalarAndInsert(ushort* address, Vector128<ushort> vector, [ConstantExpected(Max = (byte)(7))] byte index) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<int>    LoadScalarAndInsert(int*    address, Vector128<int>    vector, [ConstantExpected(Max = (byte)(3))] byte index) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<uint>   LoadScalarAndInsert(uint*   address, Vector128<uint>   vector, [ConstantExpected(Max = (byte)(3))] byte index) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<long>   LoadScalarAndInsert(long*   address, Vector128<long>   vector, [ConstantExpected(Max = (byte)(1))] byte index) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<ulong>  LoadScalarAndInsert(ulong*  address, Vector128<ulong>  vector, [ConstantExpected(Max = (byte)(1))] byte index) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<float>  LoadScalarAndInsert(float*  address, Vector128<float>  vector, [ConstantExpected(Max = (byte)(3))] byte index) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<double> LoadScalarAndInsert(double* address, Vector128<double> vector, [ConstantExpected(Max = (byte)(1))] byte index) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<nint>   LoadScalarAndInsert(nint*   address, Vector128<nint>   vector, [ConstantExpected(Max = (byte)(3))] byte index) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<nuint>  LoadScalarAndInsert(nuint*  address, Vector128<nuint>  vector, [ConstantExpected(Max = (byte)(3))] byte index) { throw new PlatformNotSupportedException(); }

        public static unsafe Vector128<short>  LoadWideningVector128(sbyte*  address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<ushort> LoadWideningVector128(byte*   address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<int>    LoadWideningVector128(short*  address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<uint>   LoadWideningVector128(ushort* address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<long>   LoadWideningVector128(int*    address) { throw new PlatformNotSupportedException(); }
        public static unsafe Vector128<ulong>  LoadWideningVector128(uint*   address) { throw new PlatformNotSupportedException(); }

        // Store

        public static unsafe void Store(sbyte*  address, Vector128<sbyte>  source) { throw new PlatformNotSupportedException(); }
        public static unsafe void Store(byte*   address, Vector128<byte>   source) { throw new PlatformNotSupportedException(); }
        public static unsafe void Store(short*  address, Vector128<short>  source) { throw new PlatformNotSupportedException(); }
        public static unsafe void Store(ushort* address, Vector128<ushort> source) { throw new PlatformNotSupportedException(); }
        public static unsafe void Store(int*    address, Vector128<int>    source) { throw new PlatformNotSupportedException(); }
        public static unsafe void Store(uint*   address, Vector128<uint>   source) { throw new PlatformNotSupportedException(); }
        public static unsafe void Store(long*   address, Vector128<long>   source) { throw new PlatformNotSupportedException(); }
        public static unsafe void Store(ulong*  address, Vector128<ulong>  source) { throw new PlatformNotSupportedException(); }
        public static unsafe void Store(float*  address, Vector128<float>  source) { throw new PlatformNotSupportedException(); }
        public static unsafe void Store(double* address, Vector128<double> source) { throw new PlatformNotSupportedException(); }
        public static unsafe void Store(nint*   address, Vector128<nint>   source) { throw new PlatformNotSupportedException(); }
        public static unsafe void Store(nuint*  address, Vector128<nuint>  source) { throw new PlatformNotSupportedException(); }

        public static unsafe void StoreSelectedScalar(sbyte*  address, Vector128<sbyte>  source, [ConstantExpected(Max = (byte)(15))] byte index) { throw new PlatformNotSupportedException(); } // takes ImmLaneIdx16
        public static unsafe void StoreSelectedScalar(byte*   address, Vector128<byte>   source, [ConstantExpected(Max = (byte)(15))] byte index) { throw new PlatformNotSupportedException(); } // takes ImmLaneIdx16
        public static unsafe void StoreSelectedScalar(short*  address, Vector128<short>  source, [ConstantExpected(Max = (byte)(7))] byte index) { throw new PlatformNotSupportedException(); } // takes ImmLaneIdx8
        public static unsafe void StoreSelectedScalar(ushort* address, Vector128<ushort> source, [ConstantExpected(Max = (byte)(7))] byte index) { throw new PlatformNotSupportedException(); } // takes ImmLaneIdx8
        public static unsafe void StoreSelectedScalar(int*    address, Vector128<int>    source, [ConstantExpected(Max = (byte)(3))] byte index) { throw new PlatformNotSupportedException(); } // takes ImmLaneIdx4
        public static unsafe void StoreSelectedScalar(uint*   address, Vector128<uint>   source, [ConstantExpected(Max = (byte)(3))] byte index) { throw new PlatformNotSupportedException(); } // takes ImmLaneIdx4
        public static unsafe void StoreSelectedScalar(long*   address, Vector128<long>   source, [ConstantExpected(Max = (byte)(1))] byte index) { throw new PlatformNotSupportedException(); } // takes ImmLaneIdx2
        public static unsafe void StoreSelectedScalar(ulong*  address, Vector128<ulong>  source, [ConstantExpected(Max = (byte)(1))] byte index) { throw new PlatformNotSupportedException(); } // takes ImmLaneIdx2
        public static unsafe void StoreSelectedScalar(float*  address, Vector128<float>  source, [ConstantExpected(Max = (byte)(3))] byte index) { throw new PlatformNotSupportedException(); } // takes ImmLaneIdx4
        public static unsafe void StoreSelectedScalar(double* address, Vector128<double> source, [ConstantExpected(Max = (byte)(1))] byte index) { throw new PlatformNotSupportedException(); } // takes ImmLaneIdx2
        public static unsafe void StoreSelectedScalar(nint*   address, Vector128<nint>   source, [ConstantExpected(Max = (byte)(3))] byte index) { throw new PlatformNotSupportedException(); }
        public static unsafe void StoreSelectedScalar(nuint*  address, Vector128<nuint>  source, [ConstantExpected(Max = (byte)(3))] byte index) { throw new PlatformNotSupportedException(); }

        // Floating-point sign bit operations

        public static Vector128<float>  Negate(Vector128<float>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> Negate(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        public static Vector128<float>  Abs(Vector128<float>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> Abs(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        // Floating-point min and max

        public static Vector128<float>  Min(Vector128<float>  left, Vector128<float>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> Min(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        public static Vector128<float>  Max(Vector128<float>  left, Vector128<float>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> Max(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        public static Vector128<float>  PseudoMin(Vector128<float>  left, Vector128<float>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> PseudoMin(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        public static Vector128<float>  PseudoMax(Vector128<float>  left, Vector128<float>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> PseudoMax(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        // Floating-point arithmetic

        public static Vector128<float>  Add(Vector128<float>  left, Vector128<float>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> Add(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        public static Vector128<float>  Subtract(Vector128<float>  left, Vector128<float>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> Subtract(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        public static Vector128<float>  Divide(Vector128<float>  left, Vector128<float>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> Divide(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        public static Vector128<float>  Multiply(Vector128<float>  left, Vector128<float>  right) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> Multiply(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        public static Vector128<float>  Sqrt(Vector128<float>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> Sqrt(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        public static Vector128<float>  Ceiling(Vector128<float>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> Ceiling(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        public static Vector128<float>  Floor(Vector128<float>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> Floor(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        public static Vector128<float>  Truncate(Vector128<float>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> Truncate(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        public static Vector128<float>  RoundToNearest(Vector128<float>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> RoundToNearest(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        // Conversions

        public static Vector128<float> ConvertToSingle(Vector128<int>    value) { throw new PlatformNotSupportedException(); }
        public static Vector128<float> ConvertToSingle(Vector128<uint>   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<float> ConvertToSingle(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        public static Vector128<double> ConvertToDoubleLower(Vector128<int>   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> ConvertToDoubleLower(Vector128<uint>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> ConvertToDoubleLower(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        public static Vector128<int>  ConvertToInt32Saturate(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint> ConvertToUInt32Saturate(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        public static Vector128<int>  ConvertToInt32Saturate(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint> ConvertToUInt32Saturate(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  ConvertNarrowingSaturateSigned(Vector128<short>   lower, Vector128<short> upper) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  ConvertNarrowingSaturateSigned(Vector128<int>     lower, Vector128<int>   upper) { throw new PlatformNotSupportedException(); }

        public static Vector128<byte>   ConvertNarrowingSaturateUnsigned(Vector128<short> lower, Vector128<short> upper) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> ConvertNarrowingSaturateUnsigned(Vector128<int>   lower, Vector128<int>   upper) { throw new PlatformNotSupportedException(); }

        public static Vector128<short>  SignExtendWideningLower(Vector128<sbyte>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> SignExtendWideningLower(Vector128<byte>   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    SignExtendWideningLower(Vector128<short>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   SignExtendWideningLower(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   SignExtendWideningLower(Vector128<int>    value) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  SignExtendWideningLower(Vector128<uint>   value) { throw new PlatformNotSupportedException(); }

        public static Vector128<short>  SignExtendWideningUpper(Vector128<sbyte>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> SignExtendWideningUpper(Vector128<byte>   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    SignExtendWideningUpper(Vector128<short>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   SignExtendWideningUpper(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   SignExtendWideningUpper(Vector128<int>    value) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  SignExtendWideningUpper(Vector128<uint>   value) { throw new PlatformNotSupportedException(); }

        public static Vector128<short>  ZeroExtendWideningLower(Vector128<sbyte>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> ZeroExtendWideningLower(Vector128<byte>   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    ZeroExtendWideningLower(Vector128<short>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   ZeroExtendWideningLower(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   ZeroExtendWideningLower(Vector128<int>    value) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  ZeroExtendWideningLower(Vector128<uint>   value) { throw new PlatformNotSupportedException(); }

        public static Vector128<short>  ZeroExtendWideningUpper(Vector128<sbyte>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> ZeroExtendWideningUpper(Vector128<byte>   value) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    ZeroExtendWideningUpper(Vector128<short>  value) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   ZeroExtendWideningUpper(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   ZeroExtendWideningUpper(Vector128<int>    value) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  ZeroExtendWideningUpper(Vector128<uint>   value) { throw new PlatformNotSupportedException(); }
    }
}
