// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Runtime.Intrinsics.Arm.Arm64
{
    public static partial class Aes
    {
        public static bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<byte> Decrypt(System.Runtime.Intrinsics.Vector128<byte> value, System.Runtime.Intrinsics.Vector128<byte> roundKey) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Encrypt(System.Runtime.Intrinsics.Vector128<byte> value, System.Runtime.Intrinsics.Vector128<byte> roundKey) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> InverseMixColumns(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> MixColumns(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
    }
    public static partial class Base
    {
        public static bool IsSupported { get { throw null; } }
        public static int LeadingSignCount(int value) { throw null; }
        public static int LeadingSignCount(long value) { throw null; }
        public static int LeadingZeroCount(int value) { throw null; }
        public static int LeadingZeroCount(long value) { throw null; }
        public static int LeadingZeroCount(uint value) { throw null; }
        public static int LeadingZeroCount(ulong value) { throw null; }
    }
    public static partial class Sha1
    {
        public static bool IsSupported { get { throw null; } }
        public static uint FixedRotate(uint hash_e) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> HashChoose(System.Runtime.Intrinsics.Vector128<uint> hash_abcd, uint hash_e, System.Runtime.Intrinsics.Vector128<uint> wk) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> HashMajority(System.Runtime.Intrinsics.Vector128<uint> hash_abcd, uint hash_e, System.Runtime.Intrinsics.Vector128<uint> wk) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> HashParity(System.Runtime.Intrinsics.Vector128<uint> hash_abcd, uint hash_e, System.Runtime.Intrinsics.Vector128<uint> wk) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> SchedulePart1(System.Runtime.Intrinsics.Vector128<uint> w0_3, System.Runtime.Intrinsics.Vector128<uint> w4_7, System.Runtime.Intrinsics.Vector128<uint> w8_11) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> SchedulePart2(System.Runtime.Intrinsics.Vector128<uint> tw0_3, System.Runtime.Intrinsics.Vector128<uint> w12_15) { throw null; }
    }
    public static partial class Sha256
    {
        public static bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<uint> HashLower(System.Runtime.Intrinsics.Vector128<uint> hash_abcd, System.Runtime.Intrinsics.Vector128<uint> hash_efgh, System.Runtime.Intrinsics.Vector128<uint> wk) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> HashUpper(System.Runtime.Intrinsics.Vector128<uint> hash_efgh, System.Runtime.Intrinsics.Vector128<uint> hash_abcd, System.Runtime.Intrinsics.Vector128<uint> wk) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> SchedulePart1(System.Runtime.Intrinsics.Vector128<uint> w0_3, System.Runtime.Intrinsics.Vector128<uint> w4_7) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> SchedulePart2(System.Runtime.Intrinsics.Vector128<uint> w0_3, System.Runtime.Intrinsics.Vector128<uint> w8_11, System.Runtime.Intrinsics.Vector128<uint> w12_15) { throw null; }
    }
    public static partial class Simd
    {
        public static bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<double> Abs(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> Abs(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> Abs(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> Abs(System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Abs(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Abs(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> Abs(System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> Abs(System.Runtime.Intrinsics.Vector64<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> Abs(System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> Abs(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Add<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Add<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> AndNot<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> AndNot<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> And<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> And<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> BitwiseSelect<T>(System.Runtime.Intrinsics.Vector128<T> sel, System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> BitwiseSelect<T>(System.Runtime.Intrinsics.Vector64<T> sel, System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> CompareEqualZero<T>(System.Runtime.Intrinsics.Vector128<T> value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> CompareEqualZero<T>(System.Runtime.Intrinsics.Vector64<T> value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> CompareEqual<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> CompareEqual<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> CompareGreaterThanOrEqualZero<T>(System.Runtime.Intrinsics.Vector128<T> value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> CompareGreaterThanOrEqualZero<T>(System.Runtime.Intrinsics.Vector64<T> value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> CompareGreaterThanOrEqual<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> CompareGreaterThanOrEqual<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> CompareGreaterThanZero<T>(System.Runtime.Intrinsics.Vector128<T> value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> CompareGreaterThanZero<T>(System.Runtime.Intrinsics.Vector64<T> value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> CompareGreaterThan<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> CompareGreaterThan<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> CompareLessThanOrEqualZero<T>(System.Runtime.Intrinsics.Vector128<T> value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> CompareLessThanOrEqualZero<T>(System.Runtime.Intrinsics.Vector64<T> value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> CompareLessThanZero<T>(System.Runtime.Intrinsics.Vector128<T> value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> CompareLessThanZero<T>(System.Runtime.Intrinsics.Vector64<T> value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> CompareTest<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> CompareTest<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Divide(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Divide(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> Divide(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static T Extract<T>(System.Runtime.Intrinsics.Vector128<T> vector, byte index) where T : struct { throw null; }
        public static T Extract<T>(System.Runtime.Intrinsics.Vector64<T> vector, byte index) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Insert<T>(System.Runtime.Intrinsics.Vector128<T> vector, byte index, T data) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Insert<T>(System.Runtime.Intrinsics.Vector64<T> vector, byte index, T data) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> LeadingSignCount(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> LeadingSignCount(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> LeadingSignCount(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> LeadingSignCount(System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> LeadingSignCount(System.Runtime.Intrinsics.Vector64<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> LeadingSignCount(System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> LeadingZeroCount(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> LeadingZeroCount(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> LeadingZeroCount(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> LeadingZeroCount(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> LeadingZeroCount(System.Runtime.Intrinsics.Vector128<ushort> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> LeadingZeroCount(System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> LeadingZeroCount(System.Runtime.Intrinsics.Vector64<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> LeadingZeroCount(System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> LeadingZeroCount(System.Runtime.Intrinsics.Vector64<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> LeadingZeroCount(System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> LeadingZeroCount(System.Runtime.Intrinsics.Vector64<ushort> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> LeadingZeroCount(System.Runtime.Intrinsics.Vector64<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Max(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Max(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> Max(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Max(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> Max(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Max(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> Max(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> Max(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> Max(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> Max(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> Max(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> Max(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> Max(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> Max(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> Max(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Min(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Min(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> Min(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Min(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> Min(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Min(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> Min(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> Min(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> Min(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> Min(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> Min(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> Min(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> Min(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> Min(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> Min(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Multiply(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Multiply(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> Multiply(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Multiply(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> Multiply(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Multiply(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> Multiply(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> Multiply(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> Multiply(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> Multiply(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> Multiply(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> Multiply(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> Multiply(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> Multiply(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> Multiply(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Negate(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> Negate(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Negate(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> Negate(System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> Negate(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Negate(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> Negate(System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> Negate(System.Runtime.Intrinsics.Vector64<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> Negate(System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> Negate(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Not<T>(System.Runtime.Intrinsics.Vector128<T> value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Not<T>(System.Runtime.Intrinsics.Vector64<T> value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> OrNot<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> OrNot<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Or<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Or<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> PopCount(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> PopCount(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> PopCount(System.Runtime.Intrinsics.Vector64<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> PopCount(System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> SetAllVector128<T>(T value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> SetAllVector64<T>(T value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Sqrt(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Sqrt(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> Sqrt(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Subtract<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Subtract<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Xor<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Xor<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
    }
}
