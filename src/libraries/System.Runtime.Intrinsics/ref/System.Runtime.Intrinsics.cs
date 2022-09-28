// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Runtime.Intrinsics
{
    public static partial class Vector128
    {
        public static bool IsHardwareAccelerated { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<T> Abs<T>(System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Add<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> AndNot<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Byte> AsByte<T>(this System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Double> AsDouble<T>(this System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int16> AsInt16<T>(this System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int32> AsInt32<T>(this System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int64> AsInt64<T>(this System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<nint> AsNInt<T>(this System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<nuint> AsNUInt<T>(this System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.SByte> AsSByte<T>(this System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Single> AsSingle<T>(this System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt16> AsUInt16<T>(this System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt32> AsUInt32<T>(this System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt64> AsUInt64<T>(this System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Single> AsVector128(this System.Numerics.Vector2 value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Single> AsVector128(this System.Numerics.Vector3 value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Single> AsVector128(this System.Numerics.Vector4 value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> AsVector128<T>(this System.Numerics.Vector<T> value) where T : struct { throw null; }
        public static System.Numerics.Vector2 AsVector2(this System.Runtime.Intrinsics.Vector128<System.Single> value) { throw null; }
        public static System.Numerics.Vector3 AsVector3(this System.Runtime.Intrinsics.Vector128<System.Single> value) { throw null; }
        public static System.Numerics.Vector4 AsVector4(this System.Runtime.Intrinsics.Vector128<System.Single> value) { throw null; }
        public static System.Numerics.Vector<T> AsVector<T>(this System.Runtime.Intrinsics.Vector128<T> value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<TTo> As<TFrom, TTo>(this System.Runtime.Intrinsics.Vector128<TFrom> vector) where TFrom : struct where TTo : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> BitwiseAnd<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> BitwiseOr<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Double> Ceiling(System.Runtime.Intrinsics.Vector128<System.Double> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Single> Ceiling(System.Runtime.Intrinsics.Vector128<System.Single> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> ConditionalSelect<T>(System.Runtime.Intrinsics.Vector128<T> condition, System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Double> ConvertToDouble(System.Runtime.Intrinsics.Vector128<System.Int64> vector) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.Double> ConvertToDouble(System.Runtime.Intrinsics.Vector128<System.UInt64> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int32> ConvertToInt32(System.Runtime.Intrinsics.Vector128<System.Single> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int64> ConvertToInt64(System.Runtime.Intrinsics.Vector128<System.Double> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Single> ConvertToSingle(System.Runtime.Intrinsics.Vector128<System.Int32> vector) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.Single> ConvertToSingle(System.Runtime.Intrinsics.Vector128<System.UInt32> vector) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt32> ConvertToUInt32(System.Runtime.Intrinsics.Vector128<System.Single> vector) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt64> ConvertToUInt64(System.Runtime.Intrinsics.Vector128<System.Double> vector) { throw null; }
        public static void CopyTo<T>(this System.Runtime.Intrinsics.Vector128<T> vector, System.Span<T> destination) where T : struct { }
        public static void CopyTo<T>(this System.Runtime.Intrinsics.Vector128<T> vector, T[] destination) where T : struct { }
        public static void CopyTo<T>(this System.Runtime.Intrinsics.Vector128<T> vector, T[] destination, int startIndex) where T : struct { }
        public static System.Runtime.Intrinsics.Vector128<System.Byte> Create(byte value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Byte> Create(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7, byte e8, byte e9, byte e10, byte e11, byte e12, byte e13, byte e14, byte e15) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Double> Create(double value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Double> Create(double e0, double e1) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int16> Create(short value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int16> Create(short e0, short e1, short e2, short e3, short e4, short e5, short e6, short e7) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int32> Create(int value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int32> Create(int e0, int e1, int e2, int e3) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int64> Create(long value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int64> Create(long e0, long e1) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<nint> Create(nint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<nuint> Create(nuint value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Byte> Create(System.Runtime.Intrinsics.Vector64<byte> lower, System.Runtime.Intrinsics.Vector64<byte> upper) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Double> Create(System.Runtime.Intrinsics.Vector64<double> lower, System.Runtime.Intrinsics.Vector64<double> upper) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int16> Create(System.Runtime.Intrinsics.Vector64<short> lower, System.Runtime.Intrinsics.Vector64<short> upper) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int32> Create(System.Runtime.Intrinsics.Vector64<int> lower, System.Runtime.Intrinsics.Vector64<int> upper) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int64> Create(System.Runtime.Intrinsics.Vector64<long> lower, System.Runtime.Intrinsics.Vector64<long> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.SByte> Create(System.Runtime.Intrinsics.Vector64<sbyte> lower, System.Runtime.Intrinsics.Vector64<sbyte> upper) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Single> Create(System.Runtime.Intrinsics.Vector64<float> lower, System.Runtime.Intrinsics.Vector64<float> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt16> Create(System.Runtime.Intrinsics.Vector64<ushort> lower, System.Runtime.Intrinsics.Vector64<ushort> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt32> Create(System.Runtime.Intrinsics.Vector64<uint> lower, System.Runtime.Intrinsics.Vector64<uint> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt64> Create(System.Runtime.Intrinsics.Vector64<ulong> lower, System.Runtime.Intrinsics.Vector64<ulong> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.SByte> Create(sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.SByte> Create(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7, sbyte e8, sbyte e9, sbyte e10, sbyte e11, sbyte e12, sbyte e13, sbyte e14, sbyte e15) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Single> Create(float value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Single> Create(float e0, float e1, float e2, float e3) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt16> Create(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt16> Create(ushort e0, ushort e1, ushort e2, ushort e3, ushort e4, ushort e5, ushort e6, ushort e7) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt32> Create(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt32> Create(uint e0, uint e1, uint e2, uint e3) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt64> Create(ulong value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt64> Create(ulong e0, ulong e1) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Byte> CreateScalar(byte value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Double> CreateScalar(double value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int16> CreateScalar(short value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int32> CreateScalar(int value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int64> CreateScalar(long value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<nint> CreateScalar(nint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<nuint> CreateScalar(nuint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.SByte> CreateScalar(sbyte value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Single> CreateScalar(float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt16> CreateScalar(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt32> CreateScalar(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt64> CreateScalar(ulong value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Byte> CreateScalarUnsafe(byte value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Double> CreateScalarUnsafe(double value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int16> CreateScalarUnsafe(short value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int32> CreateScalarUnsafe(int value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int64> CreateScalarUnsafe(long value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<nint> CreateScalarUnsafe(nint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<nuint> CreateScalarUnsafe(nuint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.SByte> CreateScalarUnsafe(sbyte value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Single> CreateScalarUnsafe(float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt16> CreateScalarUnsafe(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt32> CreateScalarUnsafe(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt64> CreateScalarUnsafe(ulong value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Create<T>(System.ReadOnlySpan<T> values) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Create<T>(T value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Create<T>(T[] values) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Create<T>(T[] values, int index) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Divide<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static T Dot<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static bool EqualsAll<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static bool EqualsAny<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Equals<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ExtractMostSignificantBits<T>(this System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Double> Floor(System.Runtime.Intrinsics.Vector128<System.Double> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Single> Floor(System.Runtime.Intrinsics.Vector128<System.Single> vector) { throw null; }
        public static T GetElement<T>(this System.Runtime.Intrinsics.Vector128<T> vector, int index) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> GetLower<T>(this System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> GetUpper<T>(this System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        public static bool GreaterThanAll<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static bool GreaterThanAny<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static bool GreaterThanOrEqualAll<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static bool GreaterThanOrEqualAny<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> GreaterThanOrEqual<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> GreaterThan<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static bool LessThanAll<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static bool LessThanAny<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static bool LessThanOrEqualAll<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static bool LessThanOrEqualAny<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> LessThanOrEqual<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> LessThan<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static unsafe System.Runtime.Intrinsics.Vector128<T> Load<T>(T* source) where T : unmanaged { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static unsafe System.Runtime.Intrinsics.Vector128<T> LoadAligned<T>(T* source) where T : unmanaged { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static unsafe System.Runtime.Intrinsics.Vector128<T> LoadAlignedNonTemporal<T>(T* source) where T : unmanaged { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> LoadUnsafe<T>(ref T source) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<T> LoadUnsafe<T>(ref T source, nuint elementOffset) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Max<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Min<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Multiply<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Multiply<T>(System.Runtime.Intrinsics.Vector128<T> left, T right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Multiply<T>(T left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Single> Narrow(System.Runtime.Intrinsics.Vector128<System.Double> lower, System.Runtime.Intrinsics.Vector128<System.Double> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.SByte> Narrow(System.Runtime.Intrinsics.Vector128<System.Int16> lower, System.Runtime.Intrinsics.Vector128<System.Int16> upper) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int16> Narrow(System.Runtime.Intrinsics.Vector128<System.Int32> lower, System.Runtime.Intrinsics.Vector128<System.Int32> upper) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int32> Narrow(System.Runtime.Intrinsics.Vector128<System.Int64> lower, System.Runtime.Intrinsics.Vector128<System.Int64> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.Byte> Narrow(System.Runtime.Intrinsics.Vector128<System.UInt16> lower, System.Runtime.Intrinsics.Vector128<System.UInt16> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt16> Narrow(System.Runtime.Intrinsics.Vector128<System.UInt32> lower, System.Runtime.Intrinsics.Vector128<System.UInt32> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt32> Narrow(System.Runtime.Intrinsics.Vector128<System.UInt64> lower, System.Runtime.Intrinsics.Vector128<System.UInt64> upper) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Negate<T>(System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> OnesComplement<T>(System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Byte> ShiftLeft(System.Runtime.Intrinsics.Vector128<System.Byte> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int16> ShiftLeft(System.Runtime.Intrinsics.Vector128<System.Int16> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int32> ShiftLeft(System.Runtime.Intrinsics.Vector128<System.Int32> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int64> ShiftLeft(System.Runtime.Intrinsics.Vector128<System.Int64> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<nint> ShiftLeft(System.Runtime.Intrinsics.Vector128<nint> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<nuint> ShiftLeft(System.Runtime.Intrinsics.Vector128<nuint> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.SByte> ShiftLeft(System.Runtime.Intrinsics.Vector128<System.SByte> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt16> ShiftLeft(System.Runtime.Intrinsics.Vector128<System.UInt16> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt32> ShiftLeft(System.Runtime.Intrinsics.Vector128<System.UInt32> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt64> ShiftLeft(System.Runtime.Intrinsics.Vector128<System.UInt64> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int16> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector128<System.Int16> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int32> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector128<System.Int32> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int64> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector128<System.Int64> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<nint> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector128<nint> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.SByte> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector128<System.SByte> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Byte> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<System.Byte> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int16> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<System.Int16> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int32> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<System.Int32> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<System.Int64> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<System.Int64> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<nint> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<nint> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<nuint> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<nuint> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.SByte> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<System.SByte> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt16> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<System.UInt16> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt32> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<System.UInt32> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<System.UInt64> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<System.UInt64> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Shuffle(System.Runtime.Intrinsics.Vector128<byte> vector, System.Runtime.Intrinsics.Vector128<byte> indices) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<sbyte> Shuffle(System.Runtime.Intrinsics.Vector128<sbyte> vector, System.Runtime.Intrinsics.Vector128<sbyte> indices) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> Shuffle(System.Runtime.Intrinsics.Vector128<short> vector, System.Runtime.Intrinsics.Vector128<short> indices) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<ushort> Shuffle(System.Runtime.Intrinsics.Vector128<ushort> vector, System.Runtime.Intrinsics.Vector128<ushort> indices) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Shuffle(System.Runtime.Intrinsics.Vector128<int> vector, System.Runtime.Intrinsics.Vector128<int> indices) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<uint> Shuffle(System.Runtime.Intrinsics.Vector128<uint> vector, System.Runtime.Intrinsics.Vector128<uint> indices) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Shuffle(System.Runtime.Intrinsics.Vector128<float> vector, System.Runtime.Intrinsics.Vector128<int> indices) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> Shuffle(System.Runtime.Intrinsics.Vector128<long> vector, System.Runtime.Intrinsics.Vector128<long> indices) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector128<ulong> Shuffle(System.Runtime.Intrinsics.Vector128<ulong> vector, System.Runtime.Intrinsics.Vector128<ulong> indices) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Shuffle(System.Runtime.Intrinsics.Vector128<double> vector, System.Runtime.Intrinsics.Vector128<long> indices) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Sqrt<T>(System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static unsafe void Store<T>(this System.Runtime.Intrinsics.Vector128<T> source, T* destination) where T : unmanaged { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static unsafe void StoreAligned<T>(this System.Runtime.Intrinsics.Vector128<T> source, T* destination) where T : unmanaged { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static unsafe void StoreAlignedNonTemporal<T>(this System.Runtime.Intrinsics.Vector128<T> source, T* destination) where T : unmanaged { throw null; }
        public static void StoreUnsafe<T>(this System.Runtime.Intrinsics.Vector128<T> source, ref T destination) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static void StoreUnsafe<T>(this System.Runtime.Intrinsics.Vector128<T> source, ref T destination, nuint elementOffset) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Subtract<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
        public static T Sum<T>(System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        public static T ToScalar<T>(this System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> ToVector256Unsafe<T>(this System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> ToVector256<T>(this System.Runtime.Intrinsics.Vector128<T> vector) where T : struct { throw null; }
        public static bool TryCopyTo<T>(this System.Runtime.Intrinsics.Vector128<T> vector, System.Span<T> destination) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static (System.Runtime.Intrinsics.Vector128<ushort> Lower, System.Runtime.Intrinsics.Vector128<ushort> Upper) Widen(System.Runtime.Intrinsics.Vector128<System.Byte> source) { throw null; }
        public static (System.Runtime.Intrinsics.Vector128<int> Lower, System.Runtime.Intrinsics.Vector128<int> Upper) Widen(System.Runtime.Intrinsics.Vector128<System.Int16> source) { throw null; }
        public static (System.Runtime.Intrinsics.Vector128<long> Lower, System.Runtime.Intrinsics.Vector128<long> Upper) Widen(System.Runtime.Intrinsics.Vector128<System.Int32> source) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static (System.Runtime.Intrinsics.Vector128<short> Lower, System.Runtime.Intrinsics.Vector128<short> Upper) Widen(System.Runtime.Intrinsics.Vector128<System.SByte> source) { throw null; }
        public static (System.Runtime.Intrinsics.Vector128<double> Lower, System.Runtime.Intrinsics.Vector128<double> Upper) Widen(System.Runtime.Intrinsics.Vector128<System.Single> source) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static (System.Runtime.Intrinsics.Vector128<uint> Lower, System.Runtime.Intrinsics.Vector128<uint> Upper) Widen(System.Runtime.Intrinsics.Vector128<System.UInt16> source) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static (System.Runtime.Intrinsics.Vector128<ulong> Lower, System.Runtime.Intrinsics.Vector128<ulong> Upper) Widen(System.Runtime.Intrinsics.Vector128<System.UInt32> source) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> WithElement<T>(this System.Runtime.Intrinsics.Vector128<T> vector, int index, T value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> WithLower<T>(this System.Runtime.Intrinsics.Vector128<T> vector, System.Runtime.Intrinsics.Vector64<T> value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> WithUpper<T>(this System.Runtime.Intrinsics.Vector128<T> vector, System.Runtime.Intrinsics.Vector64<T> value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> Xor<T>(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) where T : struct { throw null; }
    }
    public readonly partial struct Vector128<T> : System.IEquatable<System.Runtime.Intrinsics.Vector128<T>> where T : struct
    {
        private readonly int _dummyPrimitive;
        public static System.Runtime.Intrinsics.Vector128<T> AllBitsSet { get { throw null; } }
        public static bool IsSupported { get { throw null; } }
        public static int Count { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<T> Zero { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Runtime.Intrinsics.Vector128<T> other) { throw null; }
        public override int GetHashCode() { throw null; }
		public T this[int index] { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<T> operator +(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> operator &(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> operator |(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> operator /(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) { throw null; }
        public static bool operator ==(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> operator ^(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) { throw null; }
        public static bool operator !=(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> operator *(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> operator *(System.Runtime.Intrinsics.Vector128<T> left, T right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> operator *(T left, System.Runtime.Intrinsics.Vector128<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> operator ~(System.Runtime.Intrinsics.Vector128<T> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> operator -(System.Runtime.Intrinsics.Vector128<T> left, System.Runtime.Intrinsics.Vector128<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> operator -(System.Runtime.Intrinsics.Vector128<T> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> operator +(System.Runtime.Intrinsics.Vector128<T> value) { throw null; }
        public override string ToString() { throw null; }
    }
    public static partial class Vector256
    {
        public static bool IsHardwareAccelerated { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector256<T> Abs<T>(System.Runtime.Intrinsics.Vector256<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> Add<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> AndNot<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Byte> AsByte<T>(this System.Runtime.Intrinsics.Vector256<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Double> AsDouble<T>(this System.Runtime.Intrinsics.Vector256<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int16> AsInt16<T>(this System.Runtime.Intrinsics.Vector256<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int32> AsInt32<T>(this System.Runtime.Intrinsics.Vector256<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int64> AsInt64<T>(this System.Runtime.Intrinsics.Vector256<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<nint> AsNInt<T>(this System.Runtime.Intrinsics.Vector256<T> vector) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<nuint> AsNUInt<T>(this System.Runtime.Intrinsics.Vector256<T> vector) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.SByte> AsSByte<T>(this System.Runtime.Intrinsics.Vector256<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Single> AsSingle<T>(this System.Runtime.Intrinsics.Vector256<T> vector) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt16> AsUInt16<T>(this System.Runtime.Intrinsics.Vector256<T> vector) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt32> AsUInt32<T>(this System.Runtime.Intrinsics.Vector256<T> vector) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt64> AsUInt64<T>(this System.Runtime.Intrinsics.Vector256<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> AsVector256<T>(this System.Numerics.Vector<T> value) where T : struct { throw null; }
        public static System.Numerics.Vector<T> AsVector<T>(this System.Runtime.Intrinsics.Vector256<T> value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<TTo> As<TFrom, TTo>(this System.Runtime.Intrinsics.Vector256<TFrom> vector) where TFrom : struct where TTo : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> BitwiseAnd<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> BitwiseOr<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Double> Ceiling(System.Runtime.Intrinsics.Vector256<System.Double> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Single> Ceiling(System.Runtime.Intrinsics.Vector256<System.Single> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> ConditionalSelect<T>(System.Runtime.Intrinsics.Vector256<T> condition, System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Double> ConvertToDouble(System.Runtime.Intrinsics.Vector256<System.Int64> vector) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.Double> ConvertToDouble(System.Runtime.Intrinsics.Vector256<System.UInt64> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int32> ConvertToInt32(System.Runtime.Intrinsics.Vector256<System.Single> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int64> ConvertToInt64(System.Runtime.Intrinsics.Vector256<System.Double> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Single> ConvertToSingle(System.Runtime.Intrinsics.Vector256<System.Int32> vector) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.Single> ConvertToSingle(System.Runtime.Intrinsics.Vector256<System.UInt32> vector) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt32> ConvertToUInt32(System.Runtime.Intrinsics.Vector256<System.Single> vector) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt64> ConvertToUInt64(System.Runtime.Intrinsics.Vector256<System.Double> vector) { throw null; }
        public static void CopyTo<T>(this System.Runtime.Intrinsics.Vector256<T> vector, System.Span<T> destination) where T : struct { }
        public static void CopyTo<T>(this System.Runtime.Intrinsics.Vector256<T> vector, T[] destination) where T : struct { }
        public static void CopyTo<T>(this System.Runtime.Intrinsics.Vector256<T> vector, T[] destination, int startIndex) where T : struct { }
        public static System.Runtime.Intrinsics.Vector256<System.Byte> Create(byte value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Byte> Create(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7, byte e8, byte e9, byte e10, byte e11, byte e12, byte e13, byte e14, byte e15, byte e16, byte e17, byte e18, byte e19, byte e20, byte e21, byte e22, byte e23, byte e24, byte e25, byte e26, byte e27, byte e28, byte e29, byte e30, byte e31) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Double> Create(double value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Double> Create(double e0, double e1, double e2, double e3) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int16> Create(short value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int16> Create(short e0, short e1, short e2, short e3, short e4, short e5, short e6, short e7, short e8, short e9, short e10, short e11, short e12, short e13, short e14, short e15) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int32> Create(int value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int32> Create(int e0, int e1, int e2, int e3, int e4, int e5, int e6, int e7) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int64> Create(long value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int64> Create(long e0, long e1, long e2, long e3) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<nint> Create(nint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<nuint> Create(nuint value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Byte> Create(System.Runtime.Intrinsics.Vector128<byte> lower, System.Runtime.Intrinsics.Vector128<byte> upper) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Double> Create(System.Runtime.Intrinsics.Vector128<double> lower, System.Runtime.Intrinsics.Vector128<double> upper) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int16> Create(System.Runtime.Intrinsics.Vector128<short> lower, System.Runtime.Intrinsics.Vector128<short> upper) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int32> Create(System.Runtime.Intrinsics.Vector128<int> lower, System.Runtime.Intrinsics.Vector128<int> upper) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int64> Create(System.Runtime.Intrinsics.Vector128<long> lower, System.Runtime.Intrinsics.Vector128<long> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.SByte> Create(System.Runtime.Intrinsics.Vector128<sbyte> lower, System.Runtime.Intrinsics.Vector128<sbyte> upper) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Single> Create(System.Runtime.Intrinsics.Vector128<float> lower, System.Runtime.Intrinsics.Vector128<float> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt16> Create(System.Runtime.Intrinsics.Vector128<ushort> lower, System.Runtime.Intrinsics.Vector128<ushort> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt32> Create(System.Runtime.Intrinsics.Vector128<uint> lower, System.Runtime.Intrinsics.Vector128<uint> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt64> Create(System.Runtime.Intrinsics.Vector128<ulong> lower, System.Runtime.Intrinsics.Vector128<ulong> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.SByte> Create(sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.SByte> Create(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7, sbyte e8, sbyte e9, sbyte e10, sbyte e11, sbyte e12, sbyte e13, sbyte e14, sbyte e15, sbyte e16, sbyte e17, sbyte e18, sbyte e19, sbyte e20, sbyte e21, sbyte e22, sbyte e23, sbyte e24, sbyte e25, sbyte e26, sbyte e27, sbyte e28, sbyte e29, sbyte e30, sbyte e31) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Single> Create(float value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Single> Create(float e0, float e1, float e2, float e3, float e4, float e5, float e6, float e7) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt16> Create(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt16> Create(ushort e0, ushort e1, ushort e2, ushort e3, ushort e4, ushort e5, ushort e6, ushort e7, ushort e8, ushort e9, ushort e10, ushort e11, ushort e12, ushort e13, ushort e14, ushort e15) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt32> Create(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt32> Create(uint e0, uint e1, uint e2, uint e3, uint e4, uint e5, uint e6, uint e7) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt64> Create(ulong value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt64> Create(ulong e0, ulong e1, ulong e2, ulong e3) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Byte> CreateScalar(byte value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Double> CreateScalar(double value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int16> CreateScalar(short value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int32> CreateScalar(int value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int64> CreateScalar(long value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<nint> CreateScalar(nint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<nuint> CreateScalar(nuint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.SByte> CreateScalar(sbyte value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Single> CreateScalar(float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt16> CreateScalar(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt32> CreateScalar(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt64> CreateScalar(ulong value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Byte> CreateScalarUnsafe(byte value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Double> CreateScalarUnsafe(double value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int16> CreateScalarUnsafe(short value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int32> CreateScalarUnsafe(int value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int64> CreateScalarUnsafe(long value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<nint> CreateScalarUnsafe(nint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<nuint> CreateScalarUnsafe(nuint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.SByte> CreateScalarUnsafe(sbyte value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Single> CreateScalarUnsafe(float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt16> CreateScalarUnsafe(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt32> CreateScalarUnsafe(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt64> CreateScalarUnsafe(ulong value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> Create<T>(System.ReadOnlySpan<T> values) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> Create<T>(T value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> Create<T>(T[] values) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> Create<T>(T[] values, int index) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> Divide<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static T Dot<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static bool EqualsAll<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static bool EqualsAny<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> Equals<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ExtractMostSignificantBits<T>(this System.Runtime.Intrinsics.Vector256<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Double> Floor(System.Runtime.Intrinsics.Vector256<System.Double> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Single> Floor(System.Runtime.Intrinsics.Vector256<System.Single> vector) { throw null; }
        public static T GetElement<T>(this System.Runtime.Intrinsics.Vector256<T> vector, int index) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> GetLower<T>(this System.Runtime.Intrinsics.Vector256<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> GetUpper<T>(this System.Runtime.Intrinsics.Vector256<T> vector) where T : struct { throw null; }
        public static bool GreaterThanAll<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static bool GreaterThanAny<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static bool GreaterThanOrEqualAll<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static bool GreaterThanOrEqualAny<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> GreaterThanOrEqual<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> GreaterThan<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static bool LessThanAll<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static bool LessThanAny<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static bool LessThanOrEqualAll<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static bool LessThanOrEqualAny<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> LessThanOrEqual<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> LessThan<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static unsafe System.Runtime.Intrinsics.Vector256<T> Load<T>(T* source) where T : unmanaged { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static unsafe System.Runtime.Intrinsics.Vector256<T> LoadAligned<T>(T* source) where T : unmanaged { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static unsafe System.Runtime.Intrinsics.Vector256<T> LoadAlignedNonTemporal<T>(T* source) where T : unmanaged { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> LoadUnsafe<T>(ref T source) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<T> LoadUnsafe<T>(ref T source, nuint elementOffset) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> Max<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> Min<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> Multiply<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> Multiply<T>(System.Runtime.Intrinsics.Vector256<T> left, T right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> Multiply<T>(T left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Single> Narrow(System.Runtime.Intrinsics.Vector256<System.Double> lower, System.Runtime.Intrinsics.Vector256<System.Double> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.SByte> Narrow(System.Runtime.Intrinsics.Vector256<System.Int16> lower, System.Runtime.Intrinsics.Vector256<System.Int16> upper) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int16> Narrow(System.Runtime.Intrinsics.Vector256<System.Int32> lower, System.Runtime.Intrinsics.Vector256<System.Int32> upper) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int32> Narrow(System.Runtime.Intrinsics.Vector256<System.Int64> lower, System.Runtime.Intrinsics.Vector256<System.Int64> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.Byte> Narrow(System.Runtime.Intrinsics.Vector256<System.UInt16> lower, System.Runtime.Intrinsics.Vector256<System.UInt16> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt16> Narrow(System.Runtime.Intrinsics.Vector256<System.UInt32> lower, System.Runtime.Intrinsics.Vector256<System.UInt32> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt32> Narrow(System.Runtime.Intrinsics.Vector256<System.UInt64> lower, System.Runtime.Intrinsics.Vector256<System.UInt64> upper) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> Negate<T>(System.Runtime.Intrinsics.Vector256<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> OnesComplement<T>(System.Runtime.Intrinsics.Vector256<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Byte> ShiftLeft(System.Runtime.Intrinsics.Vector256<System.Byte> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int16> ShiftLeft(System.Runtime.Intrinsics.Vector256<System.Int16> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int32> ShiftLeft(System.Runtime.Intrinsics.Vector256<System.Int32> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int64> ShiftLeft(System.Runtime.Intrinsics.Vector256<System.Int64> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<nint> ShiftLeft(System.Runtime.Intrinsics.Vector256<nint> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<nuint> ShiftLeft(System.Runtime.Intrinsics.Vector256<nuint> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.SByte> ShiftLeft(System.Runtime.Intrinsics.Vector256<System.SByte> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt16> ShiftLeft(System.Runtime.Intrinsics.Vector256<System.UInt16> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt32> ShiftLeft(System.Runtime.Intrinsics.Vector256<System.UInt32> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt64> ShiftLeft(System.Runtime.Intrinsics.Vector256<System.UInt64> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int16> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector256<System.Int16> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int32> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector256<System.Int32> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int64> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector256<System.Int64> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<nint> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector256<nint> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.SByte> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector256<System.SByte> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Byte> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<System.Byte> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int16> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<System.Int16> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int32> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<System.Int32> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<System.Int64> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<System.Int64> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<nint> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<nint> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<nuint> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<nuint> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.SByte> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<System.SByte> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt16> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<System.UInt16> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt32> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<System.UInt32> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<System.UInt64> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<System.UInt64> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> Shuffle(System.Runtime.Intrinsics.Vector256<byte> vector, System.Runtime.Intrinsics.Vector256<byte> indices) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<sbyte> Shuffle(System.Runtime.Intrinsics.Vector256<sbyte> vector, System.Runtime.Intrinsics.Vector256<sbyte> indices) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> Shuffle(System.Runtime.Intrinsics.Vector256<short> vector, System.Runtime.Intrinsics.Vector256<short> indices) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<ushort> Shuffle(System.Runtime.Intrinsics.Vector256<ushort> vector, System.Runtime.Intrinsics.Vector256<ushort> indices) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> Shuffle(System.Runtime.Intrinsics.Vector256<int> vector, System.Runtime.Intrinsics.Vector256<int> indices) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<uint> Shuffle(System.Runtime.Intrinsics.Vector256<uint> vector, System.Runtime.Intrinsics.Vector256<uint> indices) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> Shuffle(System.Runtime.Intrinsics.Vector256<float> vector, System.Runtime.Intrinsics.Vector256<int> indices) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> Shuffle(System.Runtime.Intrinsics.Vector256<long> vector, System.Runtime.Intrinsics.Vector256<long> indices) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector256<ulong> Shuffle(System.Runtime.Intrinsics.Vector256<ulong> vector, System.Runtime.Intrinsics.Vector256<ulong> indices) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> Shuffle(System.Runtime.Intrinsics.Vector256<double> vector, System.Runtime.Intrinsics.Vector256<long> indices) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> Sqrt<T>(System.Runtime.Intrinsics.Vector256<T> vector) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static unsafe void Store<T>(this System.Runtime.Intrinsics.Vector256<T> source, T* destination) where T : unmanaged { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static unsafe void StoreAligned<T>(this System.Runtime.Intrinsics.Vector256<T> source, T* destination) where T : unmanaged { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static unsafe void StoreAlignedNonTemporal<T>(this System.Runtime.Intrinsics.Vector256<T> source, T* destination) where T : unmanaged { throw null; }
        public static void StoreUnsafe<T>(this System.Runtime.Intrinsics.Vector256<T> source, ref T destination) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static void StoreUnsafe<T>(this System.Runtime.Intrinsics.Vector256<T> source, ref T destination, nuint elementOffset) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> Subtract<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
        public static T Sum<T>(System.Runtime.Intrinsics.Vector256<T> vector) where T : struct { throw null; }
        public static T ToScalar<T>(this System.Runtime.Intrinsics.Vector256<T> vector) where T : struct { throw null; }
        public static bool TryCopyTo<T>(this System.Runtime.Intrinsics.Vector256<T> vector, System.Span<T> destination) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static (System.Runtime.Intrinsics.Vector256<ushort> Lower, System.Runtime.Intrinsics.Vector256<ushort> Upper) Widen(System.Runtime.Intrinsics.Vector256<System.Byte> source) { throw null; }
        public static (System.Runtime.Intrinsics.Vector256<int> Lower, System.Runtime.Intrinsics.Vector256<int> Upper) Widen(System.Runtime.Intrinsics.Vector256<System.Int16> source) { throw null; }
        public static (System.Runtime.Intrinsics.Vector256<long> Lower, System.Runtime.Intrinsics.Vector256<long> Upper) Widen(System.Runtime.Intrinsics.Vector256<System.Int32> source) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static (System.Runtime.Intrinsics.Vector256<short> Lower, System.Runtime.Intrinsics.Vector256<short> Upper) Widen(System.Runtime.Intrinsics.Vector256<System.SByte> source) { throw null; }
        public static (System.Runtime.Intrinsics.Vector256<double> Lower, System.Runtime.Intrinsics.Vector256<double> Upper) Widen(System.Runtime.Intrinsics.Vector256<System.Single> source) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static (System.Runtime.Intrinsics.Vector256<uint> Lower, System.Runtime.Intrinsics.Vector256<uint> Upper) Widen(System.Runtime.Intrinsics.Vector256<System.UInt16> source) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static (System.Runtime.Intrinsics.Vector256<ulong> Lower, System.Runtime.Intrinsics.Vector256<ulong> Upper) Widen(System.Runtime.Intrinsics.Vector256<System.UInt32> source) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> WithElement<T>(this System.Runtime.Intrinsics.Vector256<T> vector, int index, T value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> WithLower<T>(this System.Runtime.Intrinsics.Vector256<T> vector, System.Runtime.Intrinsics.Vector128<T> value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> WithUpper<T>(this System.Runtime.Intrinsics.Vector256<T> vector, System.Runtime.Intrinsics.Vector128<T> value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> Xor<T>(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) where T : struct { throw null; }
    }
    public readonly partial struct Vector256<T> : System.IEquatable<System.Runtime.Intrinsics.Vector256<T>> where T : struct
    {
        private readonly int _dummyPrimitive;
        public static System.Runtime.Intrinsics.Vector256<T> AllBitsSet { get { throw null; } }
        public static bool IsSupported { get { throw null; } }
        public static int Count { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector256<T> Zero { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Runtime.Intrinsics.Vector256<T> other) { throw null; }
        public override int GetHashCode() { throw null; }
		public T this[int index] { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector256<T> operator +(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> operator &(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> operator |(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> operator /(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) { throw null; }
        public static bool operator ==(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> operator ^(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) { throw null; }
        public static bool operator !=(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> operator *(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> operator *(System.Runtime.Intrinsics.Vector256<T> left, T right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> operator *(T left, System.Runtime.Intrinsics.Vector256<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> operator ~(System.Runtime.Intrinsics.Vector256<T> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> operator -(System.Runtime.Intrinsics.Vector256<T> left, System.Runtime.Intrinsics.Vector256<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> operator -(System.Runtime.Intrinsics.Vector256<T> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<T> operator +(System.Runtime.Intrinsics.Vector256<T> value) { throw null; }
        public override string ToString() { throw null; }
    }
    public static partial class Vector64
    {
        public static bool IsHardwareAccelerated { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector64<T> Abs<T>(System.Runtime.Intrinsics.Vector64<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Add<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> AndNot<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Byte> AsByte<T>(this System.Runtime.Intrinsics.Vector64<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Double> AsDouble<T>(this System.Runtime.Intrinsics.Vector64<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int16> AsInt16<T>(this System.Runtime.Intrinsics.Vector64<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int32> AsInt32<T>(this System.Runtime.Intrinsics.Vector64<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int64> AsInt64<T>(this System.Runtime.Intrinsics.Vector64<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<nint> AsNInt<T>(this System.Runtime.Intrinsics.Vector64<T> vector) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<nuint> AsNUInt<T>(this System.Runtime.Intrinsics.Vector64<T> vector) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.SByte> AsSByte<T>(this System.Runtime.Intrinsics.Vector64<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Single> AsSingle<T>(this System.Runtime.Intrinsics.Vector64<T> vector) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt16> AsUInt16<T>(this System.Runtime.Intrinsics.Vector64<T> vector) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt32> AsUInt32<T>(this System.Runtime.Intrinsics.Vector64<T> vector) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt64> AsUInt64<T>(this System.Runtime.Intrinsics.Vector64<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<TTo> As<TFrom, TTo>(this System.Runtime.Intrinsics.Vector64<TFrom> vector) where TFrom : struct where TTo : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> BitwiseAnd<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> BitwiseOr<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Double> Ceiling(System.Runtime.Intrinsics.Vector64<System.Double> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Single> Ceiling(System.Runtime.Intrinsics.Vector64<System.Single> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> ConditionalSelect<T>(System.Runtime.Intrinsics.Vector64<T> condition, System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Double> ConvertToDouble(System.Runtime.Intrinsics.Vector64<System.Int64> vector) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.Double> ConvertToDouble(System.Runtime.Intrinsics.Vector64<System.UInt64> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int32> ConvertToInt32(System.Runtime.Intrinsics.Vector64<System.Single> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int64> ConvertToInt64(System.Runtime.Intrinsics.Vector64<System.Double> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Single> ConvertToSingle(System.Runtime.Intrinsics.Vector64<System.Int32> vector) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.Single> ConvertToSingle(System.Runtime.Intrinsics.Vector64<System.UInt32> vector) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt32> ConvertToUInt32(System.Runtime.Intrinsics.Vector64<System.Single> vector) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt64> ConvertToUInt64(System.Runtime.Intrinsics.Vector64<System.Double> vector) { throw null; }
        public static void CopyTo<T>(this System.Runtime.Intrinsics.Vector64<T> vector, System.Span<T> destination) where T : struct { }
        public static void CopyTo<T>(this System.Runtime.Intrinsics.Vector64<T> vector, T[] destination) where T : struct { }
        public static void CopyTo<T>(this System.Runtime.Intrinsics.Vector64<T> vector, T[] destination, int startIndex) where T : struct { }
        public static System.Runtime.Intrinsics.Vector64<System.Byte> Create(byte value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Byte> Create(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Double> Create(double value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int16> Create(short value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int16> Create(short e0, short e1, short e2, short e3) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int32> Create(int value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int32> Create(int e0, int e1) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int64> Create(long value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<nint> Create(nint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<nuint> Create(nuint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.SByte> Create(sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.SByte> Create(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Single> Create(float value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Single> Create(float e0, float e1) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt16> Create(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt16> Create(ushort e0, ushort e1, ushort e2, ushort e3) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt32> Create(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt32> Create(uint e0, uint e1) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt64> Create(ulong value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Byte> CreateScalar(byte value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Double> CreateScalar(double value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int16> CreateScalar(short value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int32> CreateScalar(int value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int64> CreateScalar(long value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<nint> CreateScalar(nint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<nuint> CreateScalar(nuint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.SByte> CreateScalar(sbyte value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Single> CreateScalar(float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt16> CreateScalar(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt32> CreateScalar(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt64> CreateScalar(ulong value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Byte> CreateScalarUnsafe(byte value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int16> CreateScalarUnsafe(short value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int32> CreateScalarUnsafe(int value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<nint> CreateScalarUnsafe(nint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<nuint> CreateScalarUnsafe(nuint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.SByte> CreateScalarUnsafe(sbyte value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Single> CreateScalarUnsafe(float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt16> CreateScalarUnsafe(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt32> CreateScalarUnsafe(uint value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Create<T>(System.ReadOnlySpan<T> values) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Create<T>(T value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Create<T>(T[] values) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Create<T>(T[] values, int index) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Divide<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static T Dot<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static bool EqualsAll<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static bool EqualsAny<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Equals<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ExtractMostSignificantBits<T>(this System.Runtime.Intrinsics.Vector64<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Double> Floor(System.Runtime.Intrinsics.Vector64<System.Double> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Single> Floor(System.Runtime.Intrinsics.Vector64<System.Single> vector) { throw null; }
        public static T GetElement<T>(this System.Runtime.Intrinsics.Vector64<T> vector, int index) where T : struct { throw null; }
        public static bool GreaterThanAll<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static bool GreaterThanAny<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static bool GreaterThanOrEqualAll<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static bool GreaterThanOrEqualAny<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> GreaterThanOrEqual<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> GreaterThan<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static bool LessThanAll<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static bool LessThanAny<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static bool LessThanOrEqualAll<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static bool LessThanOrEqualAny<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> LessThanOrEqual<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> LessThan<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static unsafe System.Runtime.Intrinsics.Vector64<T> Load<T>(T* source) where T : unmanaged { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static unsafe System.Runtime.Intrinsics.Vector64<T> LoadAligned<T>(T* source) where T : unmanaged { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static unsafe System.Runtime.Intrinsics.Vector64<T> LoadAlignedNonTemporal<T>(T* source) where T : unmanaged { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> LoadUnsafe<T>(ref T source) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<T> LoadUnsafe<T>(ref T source, nuint elementOffset) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Max<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Min<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Multiply<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Multiply<T>(System.Runtime.Intrinsics.Vector64<T> left, T right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Multiply<T>(T left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Single> Narrow(System.Runtime.Intrinsics.Vector64<System.Double> lower, System.Runtime.Intrinsics.Vector64<System.Double> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.SByte> Narrow(System.Runtime.Intrinsics.Vector64<System.Int16> lower, System.Runtime.Intrinsics.Vector64<System.Int16> upper) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int16> Narrow(System.Runtime.Intrinsics.Vector64<System.Int32> lower, System.Runtime.Intrinsics.Vector64<System.Int32> upper) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int32> Narrow(System.Runtime.Intrinsics.Vector64<System.Int64> lower, System.Runtime.Intrinsics.Vector64<System.Int64> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.Byte> Narrow(System.Runtime.Intrinsics.Vector64<System.UInt16> lower, System.Runtime.Intrinsics.Vector64<System.UInt16> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt16> Narrow(System.Runtime.Intrinsics.Vector64<System.UInt32> lower, System.Runtime.Intrinsics.Vector64<System.UInt32> upper) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt32> Narrow(System.Runtime.Intrinsics.Vector64<System.UInt64> lower, System.Runtime.Intrinsics.Vector64<System.UInt64> upper) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Negate<T>(System.Runtime.Intrinsics.Vector64<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> OnesComplement<T>(System.Runtime.Intrinsics.Vector64<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Byte> ShiftLeft(System.Runtime.Intrinsics.Vector64<System.Byte> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int16> ShiftLeft(System.Runtime.Intrinsics.Vector64<System.Int16> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int32> ShiftLeft(System.Runtime.Intrinsics.Vector64<System.Int32> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int64> ShiftLeft(System.Runtime.Intrinsics.Vector64<System.Int64> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<nint> ShiftLeft(System.Runtime.Intrinsics.Vector64<nint> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<nuint> ShiftLeft(System.Runtime.Intrinsics.Vector64<nuint> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.SByte> ShiftLeft(System.Runtime.Intrinsics.Vector64<System.SByte> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt16> ShiftLeft(System.Runtime.Intrinsics.Vector64<System.UInt16> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt32> ShiftLeft(System.Runtime.Intrinsics.Vector64<System.UInt32> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt64> ShiftLeft(System.Runtime.Intrinsics.Vector64<System.UInt64> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int16> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector64<System.Int16> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int32> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector64<System.Int32> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int64> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector64<System.Int64> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<nint> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector64<nint> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.SByte> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector64<System.SByte> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Byte> ShiftRightLogical(System.Runtime.Intrinsics.Vector64<System.Byte> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int16> ShiftRightLogical(System.Runtime.Intrinsics.Vector64<System.Int16> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int32> ShiftRightLogical(System.Runtime.Intrinsics.Vector64<System.Int32> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<System.Int64> ShiftRightLogical(System.Runtime.Intrinsics.Vector64<System.Int64> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<nint> ShiftRightLogical(System.Runtime.Intrinsics.Vector64<nint> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<nuint> ShiftRightLogical(System.Runtime.Intrinsics.Vector64<nuint> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.SByte> ShiftRightLogical(System.Runtime.Intrinsics.Vector64<System.SByte> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt16> ShiftRightLogical(System.Runtime.Intrinsics.Vector64<System.UInt16> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt32> ShiftRightLogical(System.Runtime.Intrinsics.Vector64<System.UInt32> vector, int shiftCount) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<System.UInt64> ShiftRightLogical(System.Runtime.Intrinsics.Vector64<System.UInt64> vector, int shiftCount) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> Shuffle(System.Runtime.Intrinsics.Vector64<byte> vector, System.Runtime.Intrinsics.Vector64<byte> indices) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<sbyte> Shuffle(System.Runtime.Intrinsics.Vector64<sbyte> vector, System.Runtime.Intrinsics.Vector64<sbyte> indices) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> Shuffle(System.Runtime.Intrinsics.Vector64<short> vector, System.Runtime.Intrinsics.Vector64<short> indices) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<ushort> Shuffle(System.Runtime.Intrinsics.Vector64<ushort> vector, System.Runtime.Intrinsics.Vector64<ushort> indices) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> Shuffle(System.Runtime.Intrinsics.Vector64<int> vector, System.Runtime.Intrinsics.Vector64<int> indices) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static System.Runtime.Intrinsics.Vector64<uint> Shuffle(System.Runtime.Intrinsics.Vector64<uint> vector, System.Runtime.Intrinsics.Vector64<uint> indices) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> Shuffle(System.Runtime.Intrinsics.Vector64<float> vector, System.Runtime.Intrinsics.Vector64<int> indices) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Sqrt<T>(System.Runtime.Intrinsics.Vector64<T> vector) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static unsafe void Store<T>(this System.Runtime.Intrinsics.Vector64<T> source, T* destination) where T : unmanaged { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static unsafe void StoreAligned<T>(this System.Runtime.Intrinsics.Vector64<T> source, T* destination) where T : unmanaged { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static unsafe void StoreAlignedNonTemporal<T>(this System.Runtime.Intrinsics.Vector64<T> source, T* destination) where T : unmanaged { throw null; }
        public static void StoreUnsafe<T>(this System.Runtime.Intrinsics.Vector64<T> source, ref T destination) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static void StoreUnsafe<T>(this System.Runtime.Intrinsics.Vector64<T> source, ref T destination, nuint elementOffset) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Subtract<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
        public static T Sum<T>(System.Runtime.Intrinsics.Vector64<T> vector) where T : struct { throw null; }
        public static T ToScalar<T>(this System.Runtime.Intrinsics.Vector64<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> ToVector128Unsafe<T>(this System.Runtime.Intrinsics.Vector64<T> vector) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector128<T> ToVector128<T>(this System.Runtime.Intrinsics.Vector64<T> vector) where T : struct { throw null; }
        public static bool TryCopyTo<T>(this System.Runtime.Intrinsics.Vector64<T> vector, System.Span<T> destination) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static (System.Runtime.Intrinsics.Vector64<ushort> Lower, System.Runtime.Intrinsics.Vector64<ushort> Upper) Widen(System.Runtime.Intrinsics.Vector64<System.Byte> source) { throw null; }
        public static (System.Runtime.Intrinsics.Vector64<int> Lower, System.Runtime.Intrinsics.Vector64<int> Upper) Widen(System.Runtime.Intrinsics.Vector64<System.Int16> source) { throw null; }
        public static (System.Runtime.Intrinsics.Vector64<long> Lower, System.Runtime.Intrinsics.Vector64<long> Upper) Widen(System.Runtime.Intrinsics.Vector64<System.Int32> source) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static (System.Runtime.Intrinsics.Vector64<short> Lower, System.Runtime.Intrinsics.Vector64<short> Upper) Widen(System.Runtime.Intrinsics.Vector64<System.SByte> source) { throw null; }
        public static (System.Runtime.Intrinsics.Vector64<double> Lower, System.Runtime.Intrinsics.Vector64<double> Upper) Widen(System.Runtime.Intrinsics.Vector64<System.Single> source) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static (System.Runtime.Intrinsics.Vector64<uint> Lower, System.Runtime.Intrinsics.Vector64<uint> Upper) Widen(System.Runtime.Intrinsics.Vector64<System.UInt16> source) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static (System.Runtime.Intrinsics.Vector64<ulong> Lower, System.Runtime.Intrinsics.Vector64<ulong> Upper) Widen(System.Runtime.Intrinsics.Vector64<System.UInt32> source) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> WithElement<T>(this System.Runtime.Intrinsics.Vector64<T> vector, int index, T value) where T : struct { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> Xor<T>(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) where T : struct { throw null; }
    }
    public readonly partial struct Vector64<T> : System.IEquatable<System.Runtime.Intrinsics.Vector64<T>> where T : struct
    {
        private readonly int _dummyPrimitive;
        public static System.Runtime.Intrinsics.Vector64<T> AllBitsSet { get { throw null; } }
        public static bool IsSupported { get { throw null; } }
        public static int Count { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector64<T> Zero { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Runtime.Intrinsics.Vector64<T> other) { throw null; }
        public override int GetHashCode() { throw null; }
		public T this[int index] { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector64<T> operator +(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> operator &(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> operator |(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> operator /(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) { throw null; }
        public static bool operator ==(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> operator ^(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) { throw null; }
        public static bool operator !=(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> operator *(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> operator *(System.Runtime.Intrinsics.Vector64<T> left, T right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> operator *(T left, System.Runtime.Intrinsics.Vector64<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> operator ~(System.Runtime.Intrinsics.Vector64<T> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> operator -(System.Runtime.Intrinsics.Vector64<T> left, System.Runtime.Intrinsics.Vector64<T> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> operator -(System.Runtime.Intrinsics.Vector64<T> vector) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<T> operator +(System.Runtime.Intrinsics.Vector64<T> value) { throw null; }
        public override string ToString() { throw null; }
    }
}
namespace System.Runtime.Intrinsics.Arm
{
    [System.CLSCompliantAttribute(false)]
    public abstract partial class AdvSimd : System.Runtime.Intrinsics.Arm.ArmBase
    {
        internal AdvSimd() { }
        public static new bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<ushort> Abs(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> Abs(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Abs(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Abs(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> Abs(System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> Abs(System.Runtime.Intrinsics.Vector64<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> Abs(System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> Abs(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> AbsoluteCompareGreaterThan(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> AbsoluteCompareGreaterThan(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> AbsoluteCompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> AbsoluteCompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> AbsoluteCompareLessThan(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> AbsoluteCompareLessThan(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> AbsoluteCompareLessThanOrEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> AbsoluteCompareLessThanOrEqual(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> AbsoluteDifference(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> AbsoluteDifference(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> AbsoluteDifference(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> AbsoluteDifference(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> AbsoluteDifference(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> AbsoluteDifference(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> AbsoluteDifference(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> AbsoluteDifference(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> AbsoluteDifference(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> AbsoluteDifference(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> AbsoluteDifference(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> AbsoluteDifference(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> AbsoluteDifference(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> AbsoluteDifference(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> AbsoluteDifferenceAdd(System.Runtime.Intrinsics.Vector128<byte> addend, System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> AbsoluteDifferenceAdd(System.Runtime.Intrinsics.Vector128<short> addend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> AbsoluteDifferenceAdd(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> AbsoluteDifferenceAdd(System.Runtime.Intrinsics.Vector128<sbyte> addend, System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> AbsoluteDifferenceAdd(System.Runtime.Intrinsics.Vector128<ushort> addend, System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> AbsoluteDifferenceAdd(System.Runtime.Intrinsics.Vector128<uint> addend, System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> AbsoluteDifferenceAdd(System.Runtime.Intrinsics.Vector64<byte> addend, System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> AbsoluteDifferenceAdd(System.Runtime.Intrinsics.Vector64<short> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> AbsoluteDifferenceAdd(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> AbsoluteDifferenceAdd(System.Runtime.Intrinsics.Vector64<sbyte> addend, System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> AbsoluteDifferenceAdd(System.Runtime.Intrinsics.Vector64<ushort> addend, System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> AbsoluteDifferenceAdd(System.Runtime.Intrinsics.Vector64<uint> addend, System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> AbsoluteDifferenceWideningLower(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> AbsoluteDifferenceWideningLower(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> AbsoluteDifferenceWideningLower(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> AbsoluteDifferenceWideningLower(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> AbsoluteDifferenceWideningLower(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> AbsoluteDifferenceWideningLower(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> AbsoluteDifferenceWideningLowerAndAdd(System.Runtime.Intrinsics.Vector128<short> addend, System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> AbsoluteDifferenceWideningLowerAndAdd(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> AbsoluteDifferenceWideningLowerAndAdd(System.Runtime.Intrinsics.Vector128<long> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> AbsoluteDifferenceWideningLowerAndAdd(System.Runtime.Intrinsics.Vector128<ushort> addend, System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> AbsoluteDifferenceWideningLowerAndAdd(System.Runtime.Intrinsics.Vector128<uint> addend, System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> AbsoluteDifferenceWideningLowerAndAdd(System.Runtime.Intrinsics.Vector128<ulong> addend, System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> AbsoluteDifferenceWideningUpper(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> AbsoluteDifferenceWideningUpper(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> AbsoluteDifferenceWideningUpper(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> AbsoluteDifferenceWideningUpper(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> AbsoluteDifferenceWideningUpper(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> AbsoluteDifferenceWideningUpper(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> AbsoluteDifferenceWideningUpperAndAdd(System.Runtime.Intrinsics.Vector128<short> addend, System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> AbsoluteDifferenceWideningUpperAndAdd(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> AbsoluteDifferenceWideningUpperAndAdd(System.Runtime.Intrinsics.Vector128<long> addend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> AbsoluteDifferenceWideningUpperAndAdd(System.Runtime.Intrinsics.Vector128<ushort> addend, System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> AbsoluteDifferenceWideningUpperAndAdd(System.Runtime.Intrinsics.Vector128<uint> addend, System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> AbsoluteDifferenceWideningUpperAndAdd(System.Runtime.Intrinsics.Vector128<ulong> addend, System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> AbsSaturate(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> AbsSaturate(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> AbsSaturate(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> AbsSaturate(System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> AbsSaturate(System.Runtime.Intrinsics.Vector64<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> AbsSaturate(System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> AbsScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> AbsScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Add(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> Add(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Add(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> Add(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> Add(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Add(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> Add(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> Add(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> Add(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> Add(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> Add(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> Add(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> Add(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> Add(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> Add(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> Add(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> AddHighNarrowingLower(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> AddHighNarrowingLower(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> AddHighNarrowingLower(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> AddHighNarrowingLower(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> AddHighNarrowingLower(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> AddHighNarrowingLower(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> AddHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<byte> lower, System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> AddHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<short> lower, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> AddHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<int> lower, System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> AddHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<sbyte> lower, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> AddHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<ushort> lower, System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> AddHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<uint> lower, System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> AddPairwise(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> AddPairwise(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> AddPairwise(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> AddPairwise(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> AddPairwise(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> AddPairwise(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> AddPairwise(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> AddPairwiseWidening(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> AddPairwiseWidening(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> AddPairwiseWidening(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> AddPairwiseWidening(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> AddPairwiseWidening(System.Runtime.Intrinsics.Vector128<ushort> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> AddPairwiseWidening(System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> AddPairwiseWidening(System.Runtime.Intrinsics.Vector64<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> AddPairwiseWidening(System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> AddPairwiseWidening(System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> AddPairwiseWidening(System.Runtime.Intrinsics.Vector64<ushort> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> AddPairwiseWideningAndAdd(System.Runtime.Intrinsics.Vector128<short> addend, System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> AddPairwiseWideningAndAdd(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> AddPairwiseWideningAndAdd(System.Runtime.Intrinsics.Vector128<long> addend, System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> AddPairwiseWideningAndAdd(System.Runtime.Intrinsics.Vector128<ushort> addend, System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> AddPairwiseWideningAndAdd(System.Runtime.Intrinsics.Vector128<uint> addend, System.Runtime.Intrinsics.Vector128<ushort> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> AddPairwiseWideningAndAdd(System.Runtime.Intrinsics.Vector128<ulong> addend, System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> AddPairwiseWideningAndAdd(System.Runtime.Intrinsics.Vector64<short> addend, System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> AddPairwiseWideningAndAdd(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> AddPairwiseWideningAndAdd(System.Runtime.Intrinsics.Vector64<ushort> addend, System.Runtime.Intrinsics.Vector64<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> AddPairwiseWideningAndAdd(System.Runtime.Intrinsics.Vector64<uint> addend, System.Runtime.Intrinsics.Vector64<ushort> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> AddPairwiseWideningAndAddScalar(System.Runtime.Intrinsics.Vector64<long> addend, System.Runtime.Intrinsics.Vector64<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> AddPairwiseWideningAndAddScalar(System.Runtime.Intrinsics.Vector64<ulong> addend, System.Runtime.Intrinsics.Vector64<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> AddPairwiseWideningScalar(System.Runtime.Intrinsics.Vector64<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> AddPairwiseWideningScalar(System.Runtime.Intrinsics.Vector64<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> AddRoundedHighNarrowingLower(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> AddRoundedHighNarrowingLower(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> AddRoundedHighNarrowingLower(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> AddRoundedHighNarrowingLower(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> AddRoundedHighNarrowingLower(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> AddRoundedHighNarrowingLower(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> AddRoundedHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<byte> lower, System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> AddRoundedHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<short> lower, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> AddRoundedHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<int> lower, System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> AddRoundedHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<sbyte> lower, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> AddRoundedHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<ushort> lower, System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> AddRoundedHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<uint> lower, System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> AddSaturate(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> AddSaturate(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> AddSaturate(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> AddSaturate(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> AddSaturate(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> AddSaturate(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> AddSaturate(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> AddSaturate(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> AddSaturate(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> AddSaturate(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> AddSaturate(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> AddSaturate(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> AddSaturate(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> AddSaturate(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> AddSaturateScalar(System.Runtime.Intrinsics.Vector64<long> left, System.Runtime.Intrinsics.Vector64<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> AddSaturateScalar(System.Runtime.Intrinsics.Vector64<ulong> left, System.Runtime.Intrinsics.Vector64<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> AddScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> AddScalar(System.Runtime.Intrinsics.Vector64<long> left, System.Runtime.Intrinsics.Vector64<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> AddScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> AddScalar(System.Runtime.Intrinsics.Vector64<ulong> left, System.Runtime.Intrinsics.Vector64<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> AddWideningLower(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> AddWideningLower(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> AddWideningLower(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> AddWideningLower(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> AddWideningLower(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> AddWideningLower(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> AddWideningLower(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> AddWideningLower(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> AddWideningLower(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> AddWideningLower(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> AddWideningLower(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> AddWideningLower(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> AddWideningUpper(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> AddWideningUpper(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> AddWideningUpper(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> AddWideningUpper(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> AddWideningUpper(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> AddWideningUpper(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> AddWideningUpper(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> AddWideningUpper(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> AddWideningUpper(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> AddWideningUpper(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> AddWideningUpper(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> AddWideningUpper(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> And(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> And(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> And(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> And(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> And(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> And(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> And(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> And(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> And(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> And(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> And(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> And(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> And(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> And(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> And(System.Runtime.Intrinsics.Vector64<long> left, System.Runtime.Intrinsics.Vector64<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> And(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> And(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> And(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> And(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> And(System.Runtime.Intrinsics.Vector64<ulong> left, System.Runtime.Intrinsics.Vector64<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> BitwiseClear(System.Runtime.Intrinsics.Vector128<byte> value, System.Runtime.Intrinsics.Vector128<byte> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> BitwiseClear(System.Runtime.Intrinsics.Vector128<double> value, System.Runtime.Intrinsics.Vector128<double> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> BitwiseClear(System.Runtime.Intrinsics.Vector128<short> value, System.Runtime.Intrinsics.Vector128<short> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> BitwiseClear(System.Runtime.Intrinsics.Vector128<int> value, System.Runtime.Intrinsics.Vector128<int> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> BitwiseClear(System.Runtime.Intrinsics.Vector128<long> value, System.Runtime.Intrinsics.Vector128<long> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> BitwiseClear(System.Runtime.Intrinsics.Vector128<sbyte> value, System.Runtime.Intrinsics.Vector128<sbyte> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> BitwiseClear(System.Runtime.Intrinsics.Vector128<float> value, System.Runtime.Intrinsics.Vector128<float> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> BitwiseClear(System.Runtime.Intrinsics.Vector128<ushort> value, System.Runtime.Intrinsics.Vector128<ushort> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> BitwiseClear(System.Runtime.Intrinsics.Vector128<uint> value, System.Runtime.Intrinsics.Vector128<uint> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> BitwiseClear(System.Runtime.Intrinsics.Vector128<ulong> value, System.Runtime.Intrinsics.Vector128<ulong> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> BitwiseClear(System.Runtime.Intrinsics.Vector64<byte> value, System.Runtime.Intrinsics.Vector64<byte> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> BitwiseClear(System.Runtime.Intrinsics.Vector64<double> value, System.Runtime.Intrinsics.Vector64<double> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> BitwiseClear(System.Runtime.Intrinsics.Vector64<short> value, System.Runtime.Intrinsics.Vector64<short> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> BitwiseClear(System.Runtime.Intrinsics.Vector64<int> value, System.Runtime.Intrinsics.Vector64<int> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> BitwiseClear(System.Runtime.Intrinsics.Vector64<long> value, System.Runtime.Intrinsics.Vector64<long> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> BitwiseClear(System.Runtime.Intrinsics.Vector64<sbyte> value, System.Runtime.Intrinsics.Vector64<sbyte> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> BitwiseClear(System.Runtime.Intrinsics.Vector64<float> value, System.Runtime.Intrinsics.Vector64<float> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> BitwiseClear(System.Runtime.Intrinsics.Vector64<ushort> value, System.Runtime.Intrinsics.Vector64<ushort> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> BitwiseClear(System.Runtime.Intrinsics.Vector64<uint> value, System.Runtime.Intrinsics.Vector64<uint> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> BitwiseClear(System.Runtime.Intrinsics.Vector64<ulong> value, System.Runtime.Intrinsics.Vector64<ulong> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> BitwiseSelect(System.Runtime.Intrinsics.Vector128<byte> select, System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> BitwiseSelect(System.Runtime.Intrinsics.Vector128<double> select, System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> BitwiseSelect(System.Runtime.Intrinsics.Vector128<short> select, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> BitwiseSelect(System.Runtime.Intrinsics.Vector128<int> select, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> BitwiseSelect(System.Runtime.Intrinsics.Vector128<long> select, System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> BitwiseSelect(System.Runtime.Intrinsics.Vector128<sbyte> select, System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> BitwiseSelect(System.Runtime.Intrinsics.Vector128<float> select, System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> BitwiseSelect(System.Runtime.Intrinsics.Vector128<ushort> select, System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> BitwiseSelect(System.Runtime.Intrinsics.Vector128<uint> select, System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> BitwiseSelect(System.Runtime.Intrinsics.Vector128<ulong> select, System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> BitwiseSelect(System.Runtime.Intrinsics.Vector64<byte> select, System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> BitwiseSelect(System.Runtime.Intrinsics.Vector64<double> select, System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> BitwiseSelect(System.Runtime.Intrinsics.Vector64<short> select, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> BitwiseSelect(System.Runtime.Intrinsics.Vector64<int> select, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> BitwiseSelect(System.Runtime.Intrinsics.Vector64<long> select, System.Runtime.Intrinsics.Vector64<long> left, System.Runtime.Intrinsics.Vector64<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> BitwiseSelect(System.Runtime.Intrinsics.Vector64<sbyte> select, System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> BitwiseSelect(System.Runtime.Intrinsics.Vector64<float> select, System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> BitwiseSelect(System.Runtime.Intrinsics.Vector64<ushort> select, System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> BitwiseSelect(System.Runtime.Intrinsics.Vector64<uint> select, System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> BitwiseSelect(System.Runtime.Intrinsics.Vector64<ulong> select, System.Runtime.Intrinsics.Vector64<ulong> left, System.Runtime.Intrinsics.Vector64<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Ceiling(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> Ceiling(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> CeilingScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> CeilingScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> CompareEqual(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> CompareEqual(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> CompareEqual(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> CompareEqual(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> CompareEqual(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> CompareEqual(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> CompareEqual(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> CompareEqual(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> CompareEqual(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> CompareEqual(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> CompareEqual(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> CompareEqual(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> CompareEqual(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> CompareGreaterThan(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> CompareGreaterThan(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> CompareGreaterThan(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> CompareGreaterThan(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareGreaterThan(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> CompareGreaterThan(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> CompareGreaterThan(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> CompareGreaterThan(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> CompareGreaterThan(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> CompareGreaterThan(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> CompareGreaterThan(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> CompareGreaterThan(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> CompareGreaterThan(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> CompareGreaterThan(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> CompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> CompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> CompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> CompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> CompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> CompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> CompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> CompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> CompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> CompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> CompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> CompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> CompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> CompareLessThan(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> CompareLessThan(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> CompareLessThan(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> CompareLessThan(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareLessThan(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> CompareLessThan(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> CompareLessThan(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> CompareLessThan(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> CompareLessThan(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> CompareLessThan(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> CompareLessThan(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> CompareLessThan(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> CompareLessThan(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> CompareLessThan(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> CompareLessThanOrEqual(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> CompareLessThanOrEqual(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> CompareLessThanOrEqual(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> CompareLessThanOrEqual(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareLessThanOrEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> CompareLessThanOrEqual(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> CompareLessThanOrEqual(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> CompareLessThanOrEqual(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> CompareLessThanOrEqual(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> CompareLessThanOrEqual(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> CompareLessThanOrEqual(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> CompareLessThanOrEqual(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> CompareLessThanOrEqual(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> CompareLessThanOrEqual(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> CompareTest(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> CompareTest(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> CompareTest(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> CompareTest(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareTest(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> CompareTest(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> CompareTest(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> CompareTest(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> CompareTest(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> CompareTest(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> CompareTest(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> CompareTest(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> CompareTest(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> CompareTest(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ConvertToInt32RoundAwayFromZero(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ConvertToInt32RoundAwayFromZero(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ConvertToInt32RoundAwayFromZeroScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ConvertToInt32RoundToEven(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ConvertToInt32RoundToEven(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ConvertToInt32RoundToEvenScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ConvertToInt32RoundToNegativeInfinity(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ConvertToInt32RoundToNegativeInfinity(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ConvertToInt32RoundToNegativeInfinityScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ConvertToInt32RoundToPositiveInfinity(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ConvertToInt32RoundToPositiveInfinity(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ConvertToInt32RoundToPositiveInfinityScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ConvertToInt32RoundToZero(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ConvertToInt32RoundToZero(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ConvertToInt32RoundToZeroScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> ConvertToSingle(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> ConvertToSingle(System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> ConvertToSingle(System.Runtime.Intrinsics.Vector64<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> ConvertToSingle(System.Runtime.Intrinsics.Vector64<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> ConvertToSingleScalar(System.Runtime.Intrinsics.Vector64<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> ConvertToSingleScalar(System.Runtime.Intrinsics.Vector64<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ConvertToUInt32RoundAwayFromZero(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ConvertToUInt32RoundAwayFromZero(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ConvertToUInt32RoundAwayFromZeroScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ConvertToUInt32RoundToEven(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ConvertToUInt32RoundToEven(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ConvertToUInt32RoundToEvenScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ConvertToUInt32RoundToNegativeInfinity(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ConvertToUInt32RoundToNegativeInfinity(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ConvertToUInt32RoundToNegativeInfinityScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ConvertToUInt32RoundToPositiveInfinity(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ConvertToUInt32RoundToPositiveInfinity(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ConvertToUInt32RoundToPositiveInfinityScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ConvertToUInt32RoundToZero(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ConvertToUInt32RoundToZero(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ConvertToUInt32RoundToZeroScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> DivideScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> DivideScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> DuplicateSelectedScalarToVector128(System.Runtime.Intrinsics.Vector128<byte> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> DuplicateSelectedScalarToVector128(System.Runtime.Intrinsics.Vector128<short> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> DuplicateSelectedScalarToVector128(System.Runtime.Intrinsics.Vector128<int> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> DuplicateSelectedScalarToVector128(System.Runtime.Intrinsics.Vector128<sbyte> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> DuplicateSelectedScalarToVector128(System.Runtime.Intrinsics.Vector128<float> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> DuplicateSelectedScalarToVector128(System.Runtime.Intrinsics.Vector128<ushort> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> DuplicateSelectedScalarToVector128(System.Runtime.Intrinsics.Vector128<uint> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> DuplicateSelectedScalarToVector128(System.Runtime.Intrinsics.Vector64<byte> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> DuplicateSelectedScalarToVector128(System.Runtime.Intrinsics.Vector64<short> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> DuplicateSelectedScalarToVector128(System.Runtime.Intrinsics.Vector64<int> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> DuplicateSelectedScalarToVector128(System.Runtime.Intrinsics.Vector64<sbyte> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> DuplicateSelectedScalarToVector128(System.Runtime.Intrinsics.Vector64<float> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> DuplicateSelectedScalarToVector128(System.Runtime.Intrinsics.Vector64<ushort> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> DuplicateSelectedScalarToVector128(System.Runtime.Intrinsics.Vector64<uint> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> DuplicateSelectedScalarToVector64(System.Runtime.Intrinsics.Vector128<byte> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> DuplicateSelectedScalarToVector64(System.Runtime.Intrinsics.Vector128<short> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> DuplicateSelectedScalarToVector64(System.Runtime.Intrinsics.Vector128<int> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> DuplicateSelectedScalarToVector64(System.Runtime.Intrinsics.Vector128<sbyte> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> DuplicateSelectedScalarToVector64(System.Runtime.Intrinsics.Vector128<float> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> DuplicateSelectedScalarToVector64(System.Runtime.Intrinsics.Vector128<ushort> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> DuplicateSelectedScalarToVector64(System.Runtime.Intrinsics.Vector128<uint> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> DuplicateSelectedScalarToVector64(System.Runtime.Intrinsics.Vector64<byte> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> DuplicateSelectedScalarToVector64(System.Runtime.Intrinsics.Vector64<short> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> DuplicateSelectedScalarToVector64(System.Runtime.Intrinsics.Vector64<int> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> DuplicateSelectedScalarToVector64(System.Runtime.Intrinsics.Vector64<sbyte> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> DuplicateSelectedScalarToVector64(System.Runtime.Intrinsics.Vector64<float> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> DuplicateSelectedScalarToVector64(System.Runtime.Intrinsics.Vector64<ushort> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> DuplicateSelectedScalarToVector64(System.Runtime.Intrinsics.Vector64<uint> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> DuplicateToVector128(byte value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> DuplicateToVector128(short value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> DuplicateToVector128(int value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> DuplicateToVector128(sbyte value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> DuplicateToVector128(float value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> DuplicateToVector128(ushort value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> DuplicateToVector128(uint value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> DuplicateToVector64(byte value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> DuplicateToVector64(short value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> DuplicateToVector64(int value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> DuplicateToVector64(sbyte value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> DuplicateToVector64(float value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> DuplicateToVector64(ushort value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> DuplicateToVector64(uint value) { throw null; }
        public static byte Extract(System.Runtime.Intrinsics.Vector128<byte> vector, byte index) { throw null; }
        public static double Extract(System.Runtime.Intrinsics.Vector128<double> vector, byte index) { throw null; }
        public static short Extract(System.Runtime.Intrinsics.Vector128<short> vector, byte index) { throw null; }
        public static int Extract(System.Runtime.Intrinsics.Vector128<int> vector, byte index) { throw null; }
        public static long Extract(System.Runtime.Intrinsics.Vector128<long> vector, byte index) { throw null; }
        public static sbyte Extract(System.Runtime.Intrinsics.Vector128<sbyte> vector, byte index) { throw null; }
        public static float Extract(System.Runtime.Intrinsics.Vector128<float> vector, byte index) { throw null; }
        public static ushort Extract(System.Runtime.Intrinsics.Vector128<ushort> vector, byte index) { throw null; }
        public static uint Extract(System.Runtime.Intrinsics.Vector128<uint> vector, byte index) { throw null; }
        public static ulong Extract(System.Runtime.Intrinsics.Vector128<ulong> vector, byte index) { throw null; }
        public static byte Extract(System.Runtime.Intrinsics.Vector64<byte> vector, byte index) { throw null; }
        public static short Extract(System.Runtime.Intrinsics.Vector64<short> vector, byte index) { throw null; }
        public static int Extract(System.Runtime.Intrinsics.Vector64<int> vector, byte index) { throw null; }
        public static sbyte Extract(System.Runtime.Intrinsics.Vector64<sbyte> vector, byte index) { throw null; }
        public static float Extract(System.Runtime.Intrinsics.Vector64<float> vector, byte index) { throw null; }
        public static ushort Extract(System.Runtime.Intrinsics.Vector64<ushort> vector, byte index) { throw null; }
        public static uint Extract(System.Runtime.Intrinsics.Vector64<uint> vector, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ExtractNarrowingLower(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ExtractNarrowingLower(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ExtractNarrowingLower(System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ExtractNarrowingLower(System.Runtime.Intrinsics.Vector128<ushort> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ExtractNarrowingLower(System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ExtractNarrowingLower(System.Runtime.Intrinsics.Vector128<ulong> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ExtractNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ExtractNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ExtractNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ExtractNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<ushort> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ExtractNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ExtractNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<ulong> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ExtractNarrowingSaturateUnsignedLower(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ExtractNarrowingSaturateUnsignedLower(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ExtractNarrowingSaturateUnsignedLower(System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ExtractNarrowingSaturateUnsignedUpper(System.Runtime.Intrinsics.Vector64<byte> lower, System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ExtractNarrowingSaturateUnsignedUpper(System.Runtime.Intrinsics.Vector64<ushort> lower, System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ExtractNarrowingSaturateUnsignedUpper(System.Runtime.Intrinsics.Vector64<uint> lower, System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ExtractNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<byte> lower, System.Runtime.Intrinsics.Vector128<ushort> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ExtractNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<short> lower, System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ExtractNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<int> lower, System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ExtractNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<sbyte> lower, System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ExtractNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<ushort> lower, System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ExtractNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<uint> lower, System.Runtime.Intrinsics.Vector128<ulong> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ExtractNarrowingUpper(System.Runtime.Intrinsics.Vector64<byte> lower, System.Runtime.Intrinsics.Vector128<ushort> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ExtractNarrowingUpper(System.Runtime.Intrinsics.Vector64<short> lower, System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ExtractNarrowingUpper(System.Runtime.Intrinsics.Vector64<int> lower, System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ExtractNarrowingUpper(System.Runtime.Intrinsics.Vector64<sbyte> lower, System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ExtractNarrowingUpper(System.Runtime.Intrinsics.Vector64<ushort> lower, System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ExtractNarrowingUpper(System.Runtime.Intrinsics.Vector64<uint> lower, System.Runtime.Intrinsics.Vector128<ulong> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ExtractVector128(System.Runtime.Intrinsics.Vector128<byte> upper, System.Runtime.Intrinsics.Vector128<byte> lower, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> ExtractVector128(System.Runtime.Intrinsics.Vector128<double> upper, System.Runtime.Intrinsics.Vector128<double> lower, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ExtractVector128(System.Runtime.Intrinsics.Vector128<short> upper, System.Runtime.Intrinsics.Vector128<short> lower, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ExtractVector128(System.Runtime.Intrinsics.Vector128<int> upper, System.Runtime.Intrinsics.Vector128<int> lower, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ExtractVector128(System.Runtime.Intrinsics.Vector128<long> upper, System.Runtime.Intrinsics.Vector128<long> lower, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ExtractVector128(System.Runtime.Intrinsics.Vector128<sbyte> upper, System.Runtime.Intrinsics.Vector128<sbyte> lower, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> ExtractVector128(System.Runtime.Intrinsics.Vector128<float> upper, System.Runtime.Intrinsics.Vector128<float> lower, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ExtractVector128(System.Runtime.Intrinsics.Vector128<ushort> upper, System.Runtime.Intrinsics.Vector128<ushort> lower, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ExtractVector128(System.Runtime.Intrinsics.Vector128<uint> upper, System.Runtime.Intrinsics.Vector128<uint> lower, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ExtractVector128(System.Runtime.Intrinsics.Vector128<ulong> upper, System.Runtime.Intrinsics.Vector128<ulong> lower, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ExtractVector64(System.Runtime.Intrinsics.Vector64<byte> upper, System.Runtime.Intrinsics.Vector64<byte> lower, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ExtractVector64(System.Runtime.Intrinsics.Vector64<short> upper, System.Runtime.Intrinsics.Vector64<short> lower, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ExtractVector64(System.Runtime.Intrinsics.Vector64<int> upper, System.Runtime.Intrinsics.Vector64<int> lower, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ExtractVector64(System.Runtime.Intrinsics.Vector64<sbyte> upper, System.Runtime.Intrinsics.Vector64<sbyte> lower, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> ExtractVector64(System.Runtime.Intrinsics.Vector64<float> upper, System.Runtime.Intrinsics.Vector64<float> lower, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ExtractVector64(System.Runtime.Intrinsics.Vector64<ushort> upper, System.Runtime.Intrinsics.Vector64<ushort> lower, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ExtractVector64(System.Runtime.Intrinsics.Vector64<uint> upper, System.Runtime.Intrinsics.Vector64<uint> lower, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Floor(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> Floor(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> FloorScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> FloorScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> FusedAddHalving(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> FusedAddHalving(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> FusedAddHalving(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> FusedAddHalving(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> FusedAddHalving(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> FusedAddHalving(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> FusedAddHalving(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> FusedAddHalving(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> FusedAddHalving(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> FusedAddHalving(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> FusedAddHalving(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> FusedAddHalving(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> FusedAddRoundedHalving(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> FusedAddRoundedHalving(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> FusedAddRoundedHalving(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> FusedAddRoundedHalving(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> FusedAddRoundedHalving(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> FusedAddRoundedHalving(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> FusedAddRoundedHalving(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> FusedAddRoundedHalving(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> FusedAddRoundedHalving(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> FusedAddRoundedHalving(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> FusedAddRoundedHalving(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> FusedAddRoundedHalving(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> FusedMultiplyAdd(System.Runtime.Intrinsics.Vector128<float> addend, System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> FusedMultiplyAdd(System.Runtime.Intrinsics.Vector64<float> addend, System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> FusedMultiplyAddNegatedScalar(System.Runtime.Intrinsics.Vector64<double> addend, System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> FusedMultiplyAddNegatedScalar(System.Runtime.Intrinsics.Vector64<float> addend, System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> FusedMultiplyAddScalar(System.Runtime.Intrinsics.Vector64<double> addend, System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> FusedMultiplyAddScalar(System.Runtime.Intrinsics.Vector64<float> addend, System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> FusedMultiplySubtract(System.Runtime.Intrinsics.Vector128<float> minuend, System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> FusedMultiplySubtract(System.Runtime.Intrinsics.Vector64<float> minuend, System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> FusedMultiplySubtractNegatedScalar(System.Runtime.Intrinsics.Vector64<double> minuend, System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> FusedMultiplySubtractNegatedScalar(System.Runtime.Intrinsics.Vector64<float> minuend, System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> FusedMultiplySubtractScalar(System.Runtime.Intrinsics.Vector64<double> minuend, System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> FusedMultiplySubtractScalar(System.Runtime.Intrinsics.Vector64<float> minuend, System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> FusedSubtractHalving(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> FusedSubtractHalving(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> FusedSubtractHalving(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> FusedSubtractHalving(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> FusedSubtractHalving(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> FusedSubtractHalving(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> FusedSubtractHalving(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> FusedSubtractHalving(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> FusedSubtractHalving(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> FusedSubtractHalving(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> FusedSubtractHalving(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> FusedSubtractHalving(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Insert(System.Runtime.Intrinsics.Vector128<byte> vector, byte index, byte data) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Insert(System.Runtime.Intrinsics.Vector128<double> vector, byte index, double data) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> Insert(System.Runtime.Intrinsics.Vector128<short> vector, byte index, short data) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Insert(System.Runtime.Intrinsics.Vector128<int> vector, byte index, int data) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> Insert(System.Runtime.Intrinsics.Vector128<long> vector, byte index, long data) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> Insert(System.Runtime.Intrinsics.Vector128<sbyte> vector, byte index, sbyte data) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Insert(System.Runtime.Intrinsics.Vector128<float> vector, byte index, float data) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> Insert(System.Runtime.Intrinsics.Vector128<ushort> vector, byte index, ushort data) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> Insert(System.Runtime.Intrinsics.Vector128<uint> vector, byte index, uint data) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> Insert(System.Runtime.Intrinsics.Vector128<ulong> vector, byte index, ulong data) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> Insert(System.Runtime.Intrinsics.Vector64<byte> vector, byte index, byte data) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> Insert(System.Runtime.Intrinsics.Vector64<short> vector, byte index, short data) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> Insert(System.Runtime.Intrinsics.Vector64<int> vector, byte index, int data) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> Insert(System.Runtime.Intrinsics.Vector64<sbyte> vector, byte index, sbyte data) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> Insert(System.Runtime.Intrinsics.Vector64<float> vector, byte index, float data) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> Insert(System.Runtime.Intrinsics.Vector64<ushort> vector, byte index, ushort data) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> Insert(System.Runtime.Intrinsics.Vector64<uint> vector, byte index, uint data) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> InsertScalar(System.Runtime.Intrinsics.Vector128<double> result, byte resultIndex, System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> InsertScalar(System.Runtime.Intrinsics.Vector128<long> result, byte resultIndex, System.Runtime.Intrinsics.Vector64<long> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> InsertScalar(System.Runtime.Intrinsics.Vector128<ulong> result, byte resultIndex, System.Runtime.Intrinsics.Vector64<ulong> value) { throw null; }
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
        public unsafe static System.Runtime.Intrinsics.Vector128<byte> LoadAndInsertScalar(System.Runtime.Intrinsics.Vector128<byte> value, byte index, byte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<double> LoadAndInsertScalar(System.Runtime.Intrinsics.Vector128<double> value, byte index, double* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<short> LoadAndInsertScalar(System.Runtime.Intrinsics.Vector128<short> value, byte index, short* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<int> LoadAndInsertScalar(System.Runtime.Intrinsics.Vector128<int> value, byte index, int* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<long> LoadAndInsertScalar(System.Runtime.Intrinsics.Vector128<long> value, byte index, long* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<sbyte> LoadAndInsertScalar(System.Runtime.Intrinsics.Vector128<sbyte> value, byte index, sbyte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<float> LoadAndInsertScalar(System.Runtime.Intrinsics.Vector128<float> value, byte index, float* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<ushort> LoadAndInsertScalar(System.Runtime.Intrinsics.Vector128<ushort> value, byte index, ushort* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<uint> LoadAndInsertScalar(System.Runtime.Intrinsics.Vector128<uint> value, byte index, uint* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<ulong> LoadAndInsertScalar(System.Runtime.Intrinsics.Vector128<ulong> value, byte index, ulong* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<byte> LoadAndInsertScalar(System.Runtime.Intrinsics.Vector64<byte> value, byte index, byte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<short> LoadAndInsertScalar(System.Runtime.Intrinsics.Vector64<short> value, byte index, short* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<int> LoadAndInsertScalar(System.Runtime.Intrinsics.Vector64<int> value, byte index, int* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<sbyte> LoadAndInsertScalar(System.Runtime.Intrinsics.Vector64<sbyte> value, byte index, sbyte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<float> LoadAndInsertScalar(System.Runtime.Intrinsics.Vector64<float> value, byte index, float* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<ushort> LoadAndInsertScalar(System.Runtime.Intrinsics.Vector64<ushort> value, byte index, ushort* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<uint> LoadAndInsertScalar(System.Runtime.Intrinsics.Vector64<uint> value, byte index, uint* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<byte> LoadAndReplicateToVector128(byte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<short> LoadAndReplicateToVector128(short* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<int> LoadAndReplicateToVector128(int* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<sbyte> LoadAndReplicateToVector128(sbyte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<float> LoadAndReplicateToVector128(float* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<ushort> LoadAndReplicateToVector128(ushort* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<uint> LoadAndReplicateToVector128(uint* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<byte> LoadAndReplicateToVector64(byte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<short> LoadAndReplicateToVector64(short* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<int> LoadAndReplicateToVector64(int* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<sbyte> LoadAndReplicateToVector64(sbyte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<float> LoadAndReplicateToVector64(float* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<ushort> LoadAndReplicateToVector64(ushort* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<uint> LoadAndReplicateToVector64(uint* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<byte> LoadVector128(byte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<double> LoadVector128(double* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<short> LoadVector128(short* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<int> LoadVector128(int* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<long> LoadVector128(long* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<sbyte> LoadVector128(sbyte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<float> LoadVector128(float* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<ushort> LoadVector128(ushort* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<uint> LoadVector128(uint* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<ulong> LoadVector128(ulong* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<byte> LoadVector64(byte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<double> LoadVector64(double* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<short> LoadVector64(short* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<int> LoadVector64(int* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<long> LoadVector64(long* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<sbyte> LoadVector64(sbyte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<float> LoadVector64(float* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<ushort> LoadVector64(ushort* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<uint> LoadVector64(uint* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector64<ulong> LoadVector64(ulong* address) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Max(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
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
        public static System.Runtime.Intrinsics.Vector128<float> MaxNumber(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> MaxNumber(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> MaxNumberScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> MaxNumberScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> MaxPairwise(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MaxPairwise(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MaxPairwise(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> MaxPairwise(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> MaxPairwise(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> MaxPairwise(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> MaxPairwise(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Min(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
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
        public static System.Runtime.Intrinsics.Vector128<float> MinNumber(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> MinNumber(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> MinNumberScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> MinNumberScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> MinPairwise(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MinPairwise(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MinPairwise(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> MinPairwise(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> MinPairwise(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> MinPairwise(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> MinPairwise(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Multiply(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
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
        public static System.Runtime.Intrinsics.Vector128<byte> MultiplyAdd(System.Runtime.Intrinsics.Vector128<byte> addend, System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyAdd(System.Runtime.Intrinsics.Vector128<short> addend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyAdd(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> MultiplyAdd(System.Runtime.Intrinsics.Vector128<sbyte> addend, System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> MultiplyAdd(System.Runtime.Intrinsics.Vector128<ushort> addend, System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyAdd(System.Runtime.Intrinsics.Vector128<uint> addend, System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> MultiplyAdd(System.Runtime.Intrinsics.Vector64<byte> addend, System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplyAdd(System.Runtime.Intrinsics.Vector64<short> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplyAdd(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> MultiplyAdd(System.Runtime.Intrinsics.Vector64<sbyte> addend, System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> MultiplyAdd(System.Runtime.Intrinsics.Vector64<ushort> addend, System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> MultiplyAdd(System.Runtime.Intrinsics.Vector64<uint> addend, System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyAddByScalar(System.Runtime.Intrinsics.Vector128<short> addend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyAddByScalar(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> MultiplyAddByScalar(System.Runtime.Intrinsics.Vector128<ushort> addend, System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyAddByScalar(System.Runtime.Intrinsics.Vector128<uint> addend, System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplyAddByScalar(System.Runtime.Intrinsics.Vector64<short> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplyAddByScalar(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> MultiplyAddByScalar(System.Runtime.Intrinsics.Vector64<ushort> addend, System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> MultiplyAddByScalar(System.Runtime.Intrinsics.Vector64<uint> addend, System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyAddBySelectedScalar(System.Runtime.Intrinsics.Vector128<short> addend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyAddBySelectedScalar(System.Runtime.Intrinsics.Vector128<short> addend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyAddBySelectedScalar(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyAddBySelectedScalar(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> MultiplyAddBySelectedScalar(System.Runtime.Intrinsics.Vector128<ushort> addend, System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> MultiplyAddBySelectedScalar(System.Runtime.Intrinsics.Vector128<ushort> addend, System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyAddBySelectedScalar(System.Runtime.Intrinsics.Vector128<uint> addend, System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyAddBySelectedScalar(System.Runtime.Intrinsics.Vector128<uint> addend, System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector64<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplyAddBySelectedScalar(System.Runtime.Intrinsics.Vector64<short> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplyAddBySelectedScalar(System.Runtime.Intrinsics.Vector64<short> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplyAddBySelectedScalar(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplyAddBySelectedScalar(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> MultiplyAddBySelectedScalar(System.Runtime.Intrinsics.Vector64<ushort> addend, System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> MultiplyAddBySelectedScalar(System.Runtime.Intrinsics.Vector64<ushort> addend, System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> MultiplyAddBySelectedScalar(System.Runtime.Intrinsics.Vector64<uint> addend, System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector128<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> MultiplyAddBySelectedScalar(System.Runtime.Intrinsics.Vector64<uint> addend, System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyByScalar(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyByScalar(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> MultiplyByScalar(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> MultiplyByScalar(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyByScalar(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplyByScalar(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplyByScalar(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> MultiplyByScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> MultiplyByScalar(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> MultiplyByScalar(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyBySelectedScalar(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyBySelectedScalar(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyBySelectedScalar(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyBySelectedScalar(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> MultiplyBySelectedScalar(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> MultiplyBySelectedScalar(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector64<float> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> MultiplyBySelectedScalar(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> MultiplyBySelectedScalar(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyBySelectedScalar(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyBySelectedScalar(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector64<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplyBySelectedScalar(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplyBySelectedScalar(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplyBySelectedScalar(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplyBySelectedScalar(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> MultiplyBySelectedScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector128<float> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> MultiplyBySelectedScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> MultiplyBySelectedScalar(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> MultiplyBySelectedScalar(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> MultiplyBySelectedScalar(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector128<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> MultiplyBySelectedScalar(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyBySelectedScalarWideningLower(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyBySelectedScalarWideningLower(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyBySelectedScalarWideningLower(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyBySelectedScalarWideningLower(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyBySelectedScalarWideningLower(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyBySelectedScalarWideningLower(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> MultiplyBySelectedScalarWideningLower(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector128<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> MultiplyBySelectedScalarWideningLower(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyBySelectedScalarWideningLowerAndAdd(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyBySelectedScalarWideningLowerAndAdd(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyBySelectedScalarWideningLowerAndAdd(System.Runtime.Intrinsics.Vector128<long> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyBySelectedScalarWideningLowerAndAdd(System.Runtime.Intrinsics.Vector128<long> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyBySelectedScalarWideningLowerAndAdd(System.Runtime.Intrinsics.Vector128<uint> addend, System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyBySelectedScalarWideningLowerAndAdd(System.Runtime.Intrinsics.Vector128<uint> addend, System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> MultiplyBySelectedScalarWideningLowerAndAdd(System.Runtime.Intrinsics.Vector128<ulong> addend, System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector128<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> MultiplyBySelectedScalarWideningLowerAndAdd(System.Runtime.Intrinsics.Vector128<ulong> addend, System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyBySelectedScalarWideningLowerAndSubtract(System.Runtime.Intrinsics.Vector128<int> minuend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyBySelectedScalarWideningLowerAndSubtract(System.Runtime.Intrinsics.Vector128<int> minuend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyBySelectedScalarWideningLowerAndSubtract(System.Runtime.Intrinsics.Vector128<long> minuend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyBySelectedScalarWideningLowerAndSubtract(System.Runtime.Intrinsics.Vector128<long> minuend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyBySelectedScalarWideningLowerAndSubtract(System.Runtime.Intrinsics.Vector128<uint> minuend, System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyBySelectedScalarWideningLowerAndSubtract(System.Runtime.Intrinsics.Vector128<uint> minuend, System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> MultiplyBySelectedScalarWideningLowerAndSubtract(System.Runtime.Intrinsics.Vector128<ulong> minuend, System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector128<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> MultiplyBySelectedScalarWideningLowerAndSubtract(System.Runtime.Intrinsics.Vector128<ulong> minuend, System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyBySelectedScalarWideningUpper(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyBySelectedScalarWideningUpper(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyBySelectedScalarWideningUpper(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyBySelectedScalarWideningUpper(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyBySelectedScalarWideningUpper(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyBySelectedScalarWideningUpper(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> MultiplyBySelectedScalarWideningUpper(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> MultiplyBySelectedScalarWideningUpper(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector64<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyBySelectedScalarWideningUpperAndAdd(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyBySelectedScalarWideningUpperAndAdd(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyBySelectedScalarWideningUpperAndAdd(System.Runtime.Intrinsics.Vector128<long> addend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyBySelectedScalarWideningUpperAndAdd(System.Runtime.Intrinsics.Vector128<long> addend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyBySelectedScalarWideningUpperAndAdd(System.Runtime.Intrinsics.Vector128<uint> addend, System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyBySelectedScalarWideningUpperAndAdd(System.Runtime.Intrinsics.Vector128<uint> addend, System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> MultiplyBySelectedScalarWideningUpperAndAdd(System.Runtime.Intrinsics.Vector128<ulong> addend, System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> MultiplyBySelectedScalarWideningUpperAndAdd(System.Runtime.Intrinsics.Vector128<ulong> addend, System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector64<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyBySelectedScalarWideningUpperAndSubtract(System.Runtime.Intrinsics.Vector128<int> minuend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyBySelectedScalarWideningUpperAndSubtract(System.Runtime.Intrinsics.Vector128<int> minuend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyBySelectedScalarWideningUpperAndSubtract(System.Runtime.Intrinsics.Vector128<long> minuend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyBySelectedScalarWideningUpperAndSubtract(System.Runtime.Intrinsics.Vector128<long> minuend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyBySelectedScalarWideningUpperAndSubtract(System.Runtime.Intrinsics.Vector128<uint> minuend, System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyBySelectedScalarWideningUpperAndSubtract(System.Runtime.Intrinsics.Vector128<uint> minuend, System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> MultiplyBySelectedScalarWideningUpperAndSubtract(System.Runtime.Intrinsics.Vector128<ulong> minuend, System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> MultiplyBySelectedScalarWideningUpperAndSubtract(System.Runtime.Intrinsics.Vector128<ulong> minuend, System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector64<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyDoublingByScalarSaturateHigh(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingByScalarSaturateHigh(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplyDoublingByScalarSaturateHigh(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplyDoublingByScalarSaturateHigh(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyDoublingBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyDoublingBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplyDoublingBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplyDoublingBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplyDoublingBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplyDoublingBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyDoublingSaturateHigh(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingSaturateHigh(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplyDoublingSaturateHigh(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplyDoublingSaturateHigh(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningLowerAndAddSaturate(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningLowerAndAddSaturate(System.Runtime.Intrinsics.Vector128<long> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningLowerAndSubtractSaturate(System.Runtime.Intrinsics.Vector128<int> minuend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningLowerAndSubtractSaturate(System.Runtime.Intrinsics.Vector128<long> minuend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningLowerByScalarAndAddSaturate(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningLowerByScalarAndAddSaturate(System.Runtime.Intrinsics.Vector128<long> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningLowerByScalarAndSubtractSaturate(System.Runtime.Intrinsics.Vector128<int> minuend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningLowerByScalarAndSubtractSaturate(System.Runtime.Intrinsics.Vector128<long> minuend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningLowerBySelectedScalarAndAddSaturate(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningLowerBySelectedScalarAndAddSaturate(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningLowerBySelectedScalarAndAddSaturate(System.Runtime.Intrinsics.Vector128<long> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningLowerBySelectedScalarAndAddSaturate(System.Runtime.Intrinsics.Vector128<long> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningLowerBySelectedScalarAndSubtractSaturate(System.Runtime.Intrinsics.Vector128<int> minuend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningLowerBySelectedScalarAndSubtractSaturate(System.Runtime.Intrinsics.Vector128<int> minuend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningLowerBySelectedScalarAndSubtractSaturate(System.Runtime.Intrinsics.Vector128<long> minuend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningLowerBySelectedScalarAndSubtractSaturate(System.Runtime.Intrinsics.Vector128<long> minuend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningSaturateLower(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningSaturateLower(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningSaturateLowerByScalar(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningSaturateLowerByScalar(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningSaturateLowerBySelectedScalar(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningSaturateLowerBySelectedScalar(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningSaturateLowerBySelectedScalar(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningSaturateLowerBySelectedScalar(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningSaturateUpper(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningSaturateUpper(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningSaturateUpperByScalar(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningSaturateUpperByScalar(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningSaturateUpperBySelectedScalar(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningSaturateUpperBySelectedScalar(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningSaturateUpperBySelectedScalar(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningSaturateUpperBySelectedScalar(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningUpperAndAddSaturate(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningUpperAndAddSaturate(System.Runtime.Intrinsics.Vector128<long> addend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningUpperAndSubtractSaturate(System.Runtime.Intrinsics.Vector128<int> minuend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningUpperAndSubtractSaturate(System.Runtime.Intrinsics.Vector128<long> minuend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningUpperByScalarAndAddSaturate(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningUpperByScalarAndAddSaturate(System.Runtime.Intrinsics.Vector128<long> addend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningUpperByScalarAndSubtractSaturate(System.Runtime.Intrinsics.Vector128<int> minuend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningUpperByScalarAndSubtractSaturate(System.Runtime.Intrinsics.Vector128<long> minuend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningUpperBySelectedScalarAndAddSaturate(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningUpperBySelectedScalarAndAddSaturate(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningUpperBySelectedScalarAndAddSaturate(System.Runtime.Intrinsics.Vector128<long> addend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningUpperBySelectedScalarAndAddSaturate(System.Runtime.Intrinsics.Vector128<long> addend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningUpperBySelectedScalarAndSubtractSaturate(System.Runtime.Intrinsics.Vector128<int> minuend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyDoublingWideningUpperBySelectedScalarAndSubtractSaturate(System.Runtime.Intrinsics.Vector128<int> minuend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningUpperBySelectedScalarAndSubtractSaturate(System.Runtime.Intrinsics.Vector128<long> minuend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyDoublingWideningUpperBySelectedScalarAndSubtractSaturate(System.Runtime.Intrinsics.Vector128<long> minuend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyRoundedDoublingByScalarSaturateHigh(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyRoundedDoublingByScalarSaturateHigh(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplyRoundedDoublingByScalarSaturateHigh(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplyRoundedDoublingByScalarSaturateHigh(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyRoundedDoublingBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyRoundedDoublingBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyRoundedDoublingBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyRoundedDoublingBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplyRoundedDoublingBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplyRoundedDoublingBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplyRoundedDoublingBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplyRoundedDoublingBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyRoundedDoublingSaturateHigh(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyRoundedDoublingSaturateHigh(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplyRoundedDoublingSaturateHigh(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplyRoundedDoublingSaturateHigh(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> MultiplyScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> MultiplyScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> MultiplyScalarBySelectedScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector128<float> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> MultiplyScalarBySelectedScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> MultiplySubtract(System.Runtime.Intrinsics.Vector128<byte> minuend, System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplySubtract(System.Runtime.Intrinsics.Vector128<short> minuend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplySubtract(System.Runtime.Intrinsics.Vector128<int> minuend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> MultiplySubtract(System.Runtime.Intrinsics.Vector128<sbyte> minuend, System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> MultiplySubtract(System.Runtime.Intrinsics.Vector128<ushort> minuend, System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplySubtract(System.Runtime.Intrinsics.Vector128<uint> minuend, System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> MultiplySubtract(System.Runtime.Intrinsics.Vector64<byte> minuend, System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplySubtract(System.Runtime.Intrinsics.Vector64<short> minuend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplySubtract(System.Runtime.Intrinsics.Vector64<int> minuend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> MultiplySubtract(System.Runtime.Intrinsics.Vector64<sbyte> minuend, System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> MultiplySubtract(System.Runtime.Intrinsics.Vector64<ushort> minuend, System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> MultiplySubtract(System.Runtime.Intrinsics.Vector64<uint> minuend, System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplySubtractByScalar(System.Runtime.Intrinsics.Vector128<short> minuend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplySubtractByScalar(System.Runtime.Intrinsics.Vector128<int> minuend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> MultiplySubtractByScalar(System.Runtime.Intrinsics.Vector128<ushort> minuend, System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplySubtractByScalar(System.Runtime.Intrinsics.Vector128<uint> minuend, System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplySubtractByScalar(System.Runtime.Intrinsics.Vector64<short> minuend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplySubtractByScalar(System.Runtime.Intrinsics.Vector64<int> minuend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> MultiplySubtractByScalar(System.Runtime.Intrinsics.Vector64<ushort> minuend, System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> MultiplySubtractByScalar(System.Runtime.Intrinsics.Vector64<uint> minuend, System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplySubtractBySelectedScalar(System.Runtime.Intrinsics.Vector128<short> minuend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplySubtractBySelectedScalar(System.Runtime.Intrinsics.Vector128<short> minuend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplySubtractBySelectedScalar(System.Runtime.Intrinsics.Vector128<int> minuend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplySubtractBySelectedScalar(System.Runtime.Intrinsics.Vector128<int> minuend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> MultiplySubtractBySelectedScalar(System.Runtime.Intrinsics.Vector128<ushort> minuend, System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> MultiplySubtractBySelectedScalar(System.Runtime.Intrinsics.Vector128<ushort> minuend, System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplySubtractBySelectedScalar(System.Runtime.Intrinsics.Vector128<uint> minuend, System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplySubtractBySelectedScalar(System.Runtime.Intrinsics.Vector128<uint> minuend, System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector64<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplySubtractBySelectedScalar(System.Runtime.Intrinsics.Vector64<short> minuend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplySubtractBySelectedScalar(System.Runtime.Intrinsics.Vector64<short> minuend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplySubtractBySelectedScalar(System.Runtime.Intrinsics.Vector64<int> minuend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplySubtractBySelectedScalar(System.Runtime.Intrinsics.Vector64<int> minuend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> MultiplySubtractBySelectedScalar(System.Runtime.Intrinsics.Vector64<ushort> minuend, System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> MultiplySubtractBySelectedScalar(System.Runtime.Intrinsics.Vector64<ushort> minuend, System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> MultiplySubtractBySelectedScalar(System.Runtime.Intrinsics.Vector64<uint> minuend, System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector128<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> MultiplySubtractBySelectedScalar(System.Runtime.Intrinsics.Vector64<uint> minuend, System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> MultiplyWideningLower(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyWideningLower(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyWideningLower(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyWideningLower(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyWideningLower(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> MultiplyWideningLower(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyWideningLowerAndAdd(System.Runtime.Intrinsics.Vector128<short> addend, System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyWideningLowerAndAdd(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyWideningLowerAndAdd(System.Runtime.Intrinsics.Vector128<long> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> MultiplyWideningLowerAndAdd(System.Runtime.Intrinsics.Vector128<ushort> addend, System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyWideningLowerAndAdd(System.Runtime.Intrinsics.Vector128<uint> addend, System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> MultiplyWideningLowerAndAdd(System.Runtime.Intrinsics.Vector128<ulong> addend, System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyWideningLowerAndSubtract(System.Runtime.Intrinsics.Vector128<short> minuend, System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyWideningLowerAndSubtract(System.Runtime.Intrinsics.Vector128<int> minuend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyWideningLowerAndSubtract(System.Runtime.Intrinsics.Vector128<long> minuend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> MultiplyWideningLowerAndSubtract(System.Runtime.Intrinsics.Vector128<ushort> minuend, System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyWideningLowerAndSubtract(System.Runtime.Intrinsics.Vector128<uint> minuend, System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> MultiplyWideningLowerAndSubtract(System.Runtime.Intrinsics.Vector128<ulong> minuend, System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> MultiplyWideningUpper(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyWideningUpper(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyWideningUpper(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyWideningUpper(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyWideningUpper(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> MultiplyWideningUpper(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyWideningUpperAndAdd(System.Runtime.Intrinsics.Vector128<short> addend, System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyWideningUpperAndAdd(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyWideningUpperAndAdd(System.Runtime.Intrinsics.Vector128<long> addend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> MultiplyWideningUpperAndAdd(System.Runtime.Intrinsics.Vector128<ushort> addend, System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyWideningUpperAndAdd(System.Runtime.Intrinsics.Vector128<uint> addend, System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> MultiplyWideningUpperAndAdd(System.Runtime.Intrinsics.Vector128<ulong> addend, System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyWideningUpperAndSubtract(System.Runtime.Intrinsics.Vector128<short> minuend, System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyWideningUpperAndSubtract(System.Runtime.Intrinsics.Vector128<int> minuend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MultiplyWideningUpperAndSubtract(System.Runtime.Intrinsics.Vector128<long> minuend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> MultiplyWideningUpperAndSubtract(System.Runtime.Intrinsics.Vector128<ushort> minuend, System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyWideningUpperAndSubtract(System.Runtime.Intrinsics.Vector128<uint> minuend, System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> MultiplyWideningUpperAndSubtract(System.Runtime.Intrinsics.Vector128<ulong> minuend, System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> Negate(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Negate(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> Negate(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Negate(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> Negate(System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> Negate(System.Runtime.Intrinsics.Vector64<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> Negate(System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> Negate(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> NegateSaturate(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> NegateSaturate(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> NegateSaturate(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> NegateSaturate(System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> NegateSaturate(System.Runtime.Intrinsics.Vector64<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> NegateSaturate(System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> NegateScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> NegateScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Not(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Not(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> Not(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Not(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> Not(System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> Not(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Not(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> Not(System.Runtime.Intrinsics.Vector128<ushort> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> Not(System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> Not(System.Runtime.Intrinsics.Vector128<ulong> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> Not(System.Runtime.Intrinsics.Vector64<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> Not(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> Not(System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> Not(System.Runtime.Intrinsics.Vector64<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> Not(System.Runtime.Intrinsics.Vector64<long> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> Not(System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> Not(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> Not(System.Runtime.Intrinsics.Vector64<ushort> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> Not(System.Runtime.Intrinsics.Vector64<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> Not(System.Runtime.Intrinsics.Vector64<ulong> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Or(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Or(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> Or(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Or(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> Or(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> Or(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Or(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> Or(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> Or(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> Or(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> Or(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> Or(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> Or(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> Or(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> Or(System.Runtime.Intrinsics.Vector64<long> left, System.Runtime.Intrinsics.Vector64<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> Or(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> Or(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> Or(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> Or(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> Or(System.Runtime.Intrinsics.Vector64<ulong> left, System.Runtime.Intrinsics.Vector64<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> OrNot(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> OrNot(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> OrNot(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> OrNot(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> OrNot(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> OrNot(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> OrNot(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> OrNot(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> OrNot(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> OrNot(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> OrNot(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> OrNot(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> OrNot(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> OrNot(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> OrNot(System.Runtime.Intrinsics.Vector64<long> left, System.Runtime.Intrinsics.Vector64<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> OrNot(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> OrNot(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> OrNot(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> OrNot(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> OrNot(System.Runtime.Intrinsics.Vector64<ulong> left, System.Runtime.Intrinsics.Vector64<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> PolynomialMultiply(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> PolynomialMultiply(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> PolynomialMultiply(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> PolynomialMultiply(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> PolynomialMultiplyWideningLower(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> PolynomialMultiplyWideningLower(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> PolynomialMultiplyWideningUpper(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> PolynomialMultiplyWideningUpper(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> PopCount(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> PopCount(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> PopCount(System.Runtime.Intrinsics.Vector64<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> PopCount(System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> ReciprocalEstimate(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ReciprocalEstimate(System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> ReciprocalEstimate(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ReciprocalEstimate(System.Runtime.Intrinsics.Vector64<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> ReciprocalSquareRootEstimate(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ReciprocalSquareRootEstimate(System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> ReciprocalSquareRootEstimate(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ReciprocalSquareRootEstimate(System.Runtime.Intrinsics.Vector64<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> ReciprocalSquareRootStep(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> ReciprocalSquareRootStep(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> ReciprocalStep(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> ReciprocalStep(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ReverseElement16(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ReverseElement16(System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ReverseElement16(System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ReverseElement16(System.Runtime.Intrinsics.Vector128<ulong> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ReverseElement16(System.Runtime.Intrinsics.Vector64<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ReverseElement16(System.Runtime.Intrinsics.Vector64<long> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ReverseElement16(System.Runtime.Intrinsics.Vector64<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> ReverseElement16(System.Runtime.Intrinsics.Vector64<ulong> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ReverseElement32(System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ReverseElement32(System.Runtime.Intrinsics.Vector128<ulong> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ReverseElement32(System.Runtime.Intrinsics.Vector64<long> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> ReverseElement32(System.Runtime.Intrinsics.Vector64<ulong> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ReverseElement8(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ReverseElement8(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ReverseElement8(System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ReverseElement8(System.Runtime.Intrinsics.Vector128<ushort> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ReverseElement8(System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ReverseElement8(System.Runtime.Intrinsics.Vector128<ulong> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ReverseElement8(System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ReverseElement8(System.Runtime.Intrinsics.Vector64<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ReverseElement8(System.Runtime.Intrinsics.Vector64<long> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ReverseElement8(System.Runtime.Intrinsics.Vector64<ushort> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ReverseElement8(System.Runtime.Intrinsics.Vector64<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> ReverseElement8(System.Runtime.Intrinsics.Vector64<ulong> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> RoundAwayFromZero(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> RoundAwayFromZero(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> RoundAwayFromZeroScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> RoundAwayFromZeroScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> RoundToNearest(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> RoundToNearest(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> RoundToNearestScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> RoundToNearestScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> RoundToNegativeInfinity(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> RoundToNegativeInfinity(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> RoundToNegativeInfinityScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> RoundToNegativeInfinityScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> RoundToPositiveInfinity(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> RoundToPositiveInfinity(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> RoundToPositiveInfinityScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> RoundToPositiveInfinityScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> RoundToZero(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> RoundToZero(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> RoundToZeroScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> RoundToZeroScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftArithmetic(System.Runtime.Intrinsics.Vector128<short> value, System.Runtime.Intrinsics.Vector128<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftArithmetic(System.Runtime.Intrinsics.Vector128<int> value, System.Runtime.Intrinsics.Vector128<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftArithmetic(System.Runtime.Intrinsics.Vector128<long> value, System.Runtime.Intrinsics.Vector128<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftArithmetic(System.Runtime.Intrinsics.Vector128<sbyte> value, System.Runtime.Intrinsics.Vector128<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftArithmetic(System.Runtime.Intrinsics.Vector64<short> value, System.Runtime.Intrinsics.Vector64<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftArithmetic(System.Runtime.Intrinsics.Vector64<int> value, System.Runtime.Intrinsics.Vector64<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftArithmetic(System.Runtime.Intrinsics.Vector64<sbyte> value, System.Runtime.Intrinsics.Vector64<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftArithmeticRounded(System.Runtime.Intrinsics.Vector128<short> value, System.Runtime.Intrinsics.Vector128<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftArithmeticRounded(System.Runtime.Intrinsics.Vector128<int> value, System.Runtime.Intrinsics.Vector128<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftArithmeticRounded(System.Runtime.Intrinsics.Vector128<long> value, System.Runtime.Intrinsics.Vector128<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftArithmeticRounded(System.Runtime.Intrinsics.Vector128<sbyte> value, System.Runtime.Intrinsics.Vector128<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftArithmeticRounded(System.Runtime.Intrinsics.Vector64<short> value, System.Runtime.Intrinsics.Vector64<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftArithmeticRounded(System.Runtime.Intrinsics.Vector64<int> value, System.Runtime.Intrinsics.Vector64<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftArithmeticRounded(System.Runtime.Intrinsics.Vector64<sbyte> value, System.Runtime.Intrinsics.Vector64<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftArithmeticRoundedSaturate(System.Runtime.Intrinsics.Vector128<short> value, System.Runtime.Intrinsics.Vector128<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftArithmeticRoundedSaturate(System.Runtime.Intrinsics.Vector128<int> value, System.Runtime.Intrinsics.Vector128<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftArithmeticRoundedSaturate(System.Runtime.Intrinsics.Vector128<long> value, System.Runtime.Intrinsics.Vector128<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftArithmeticRoundedSaturate(System.Runtime.Intrinsics.Vector128<sbyte> value, System.Runtime.Intrinsics.Vector128<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftArithmeticRoundedSaturate(System.Runtime.Intrinsics.Vector64<short> value, System.Runtime.Intrinsics.Vector64<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftArithmeticRoundedSaturate(System.Runtime.Intrinsics.Vector64<int> value, System.Runtime.Intrinsics.Vector64<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftArithmeticRoundedSaturate(System.Runtime.Intrinsics.Vector64<sbyte> value, System.Runtime.Intrinsics.Vector64<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ShiftArithmeticRoundedSaturateScalar(System.Runtime.Intrinsics.Vector64<long> value, System.Runtime.Intrinsics.Vector64<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ShiftArithmeticRoundedScalar(System.Runtime.Intrinsics.Vector64<long> value, System.Runtime.Intrinsics.Vector64<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftArithmeticSaturate(System.Runtime.Intrinsics.Vector128<short> value, System.Runtime.Intrinsics.Vector128<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftArithmeticSaturate(System.Runtime.Intrinsics.Vector128<int> value, System.Runtime.Intrinsics.Vector128<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftArithmeticSaturate(System.Runtime.Intrinsics.Vector128<long> value, System.Runtime.Intrinsics.Vector128<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftArithmeticSaturate(System.Runtime.Intrinsics.Vector128<sbyte> value, System.Runtime.Intrinsics.Vector128<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftArithmeticSaturate(System.Runtime.Intrinsics.Vector64<short> value, System.Runtime.Intrinsics.Vector64<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftArithmeticSaturate(System.Runtime.Intrinsics.Vector64<int> value, System.Runtime.Intrinsics.Vector64<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftArithmeticSaturate(System.Runtime.Intrinsics.Vector64<sbyte> value, System.Runtime.Intrinsics.Vector64<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ShiftArithmeticSaturateScalar(System.Runtime.Intrinsics.Vector64<long> value, System.Runtime.Intrinsics.Vector64<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ShiftArithmeticScalar(System.Runtime.Intrinsics.Vector64<long> value, System.Runtime.Intrinsics.Vector64<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ShiftLeftAndInsert(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftLeftAndInsert(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftLeftAndInsert(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftLeftAndInsert(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftLeftAndInsert(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftLeftAndInsert(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftLeftAndInsert(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftLeftAndInsert(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ShiftLeftAndInsert(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftLeftAndInsert(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftLeftAndInsert(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftLeftAndInsert(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ShiftLeftAndInsert(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ShiftLeftAndInsert(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ShiftLeftAndInsertScalar(System.Runtime.Intrinsics.Vector64<long> left, System.Runtime.Intrinsics.Vector64<long> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> ShiftLeftAndInsertScalar(System.Runtime.Intrinsics.Vector64<ulong> left, System.Runtime.Intrinsics.Vector64<ulong> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ShiftLeftLogical(System.Runtime.Intrinsics.Vector128<byte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftLeftLogical(System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftLeftLogical(System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftLeftLogical(System.Runtime.Intrinsics.Vector128<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftLeftLogical(System.Runtime.Intrinsics.Vector128<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftLeftLogical(System.Runtime.Intrinsics.Vector128<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftLeftLogical(System.Runtime.Intrinsics.Vector128<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ShiftLeftLogical(System.Runtime.Intrinsics.Vector64<byte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftLeftLogical(System.Runtime.Intrinsics.Vector64<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftLeftLogical(System.Runtime.Intrinsics.Vector64<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftLeftLogical(System.Runtime.Intrinsics.Vector64<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ShiftLeftLogical(System.Runtime.Intrinsics.Vector64<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ShiftLeftLogical(System.Runtime.Intrinsics.Vector64<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ShiftLeftLogicalSaturate(System.Runtime.Intrinsics.Vector128<byte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftLeftLogicalSaturate(System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftLeftLogicalSaturate(System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftLeftLogicalSaturate(System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftLeftLogicalSaturate(System.Runtime.Intrinsics.Vector128<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftLeftLogicalSaturate(System.Runtime.Intrinsics.Vector128<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftLeftLogicalSaturate(System.Runtime.Intrinsics.Vector128<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftLeftLogicalSaturate(System.Runtime.Intrinsics.Vector128<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ShiftLeftLogicalSaturate(System.Runtime.Intrinsics.Vector64<byte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftLeftLogicalSaturate(System.Runtime.Intrinsics.Vector64<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftLeftLogicalSaturate(System.Runtime.Intrinsics.Vector64<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftLeftLogicalSaturate(System.Runtime.Intrinsics.Vector64<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ShiftLeftLogicalSaturate(System.Runtime.Intrinsics.Vector64<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ShiftLeftLogicalSaturate(System.Runtime.Intrinsics.Vector64<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ShiftLeftLogicalSaturateScalar(System.Runtime.Intrinsics.Vector64<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> ShiftLeftLogicalSaturateScalar(System.Runtime.Intrinsics.Vector64<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftLeftLogicalSaturateUnsigned(System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftLeftLogicalSaturateUnsigned(System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftLeftLogicalSaturateUnsigned(System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ShiftLeftLogicalSaturateUnsigned(System.Runtime.Intrinsics.Vector128<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ShiftLeftLogicalSaturateUnsigned(System.Runtime.Intrinsics.Vector64<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ShiftLeftLogicalSaturateUnsigned(System.Runtime.Intrinsics.Vector64<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ShiftLeftLogicalSaturateUnsigned(System.Runtime.Intrinsics.Vector64<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> ShiftLeftLogicalSaturateUnsignedScalar(System.Runtime.Intrinsics.Vector64<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ShiftLeftLogicalScalar(System.Runtime.Intrinsics.Vector64<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> ShiftLeftLogicalScalar(System.Runtime.Intrinsics.Vector64<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftLeftLogicalWideningLower(System.Runtime.Intrinsics.Vector64<byte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftLeftLogicalWideningLower(System.Runtime.Intrinsics.Vector64<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftLeftLogicalWideningLower(System.Runtime.Intrinsics.Vector64<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftLeftLogicalWideningLower(System.Runtime.Intrinsics.Vector64<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftLeftLogicalWideningLower(System.Runtime.Intrinsics.Vector64<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftLeftLogicalWideningLower(System.Runtime.Intrinsics.Vector64<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftLeftLogicalWideningUpper(System.Runtime.Intrinsics.Vector128<byte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftLeftLogicalWideningUpper(System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftLeftLogicalWideningUpper(System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftLeftLogicalWideningUpper(System.Runtime.Intrinsics.Vector128<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftLeftLogicalWideningUpper(System.Runtime.Intrinsics.Vector128<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftLeftLogicalWideningUpper(System.Runtime.Intrinsics.Vector128<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ShiftLogical(System.Runtime.Intrinsics.Vector128<byte> value, System.Runtime.Intrinsics.Vector128<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftLogical(System.Runtime.Intrinsics.Vector128<short> value, System.Runtime.Intrinsics.Vector128<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftLogical(System.Runtime.Intrinsics.Vector128<int> value, System.Runtime.Intrinsics.Vector128<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftLogical(System.Runtime.Intrinsics.Vector128<long> value, System.Runtime.Intrinsics.Vector128<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftLogical(System.Runtime.Intrinsics.Vector128<sbyte> value, System.Runtime.Intrinsics.Vector128<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftLogical(System.Runtime.Intrinsics.Vector128<ushort> value, System.Runtime.Intrinsics.Vector128<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftLogical(System.Runtime.Intrinsics.Vector128<uint> value, System.Runtime.Intrinsics.Vector128<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftLogical(System.Runtime.Intrinsics.Vector128<ulong> value, System.Runtime.Intrinsics.Vector128<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ShiftLogical(System.Runtime.Intrinsics.Vector64<byte> value, System.Runtime.Intrinsics.Vector64<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftLogical(System.Runtime.Intrinsics.Vector64<short> value, System.Runtime.Intrinsics.Vector64<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftLogical(System.Runtime.Intrinsics.Vector64<int> value, System.Runtime.Intrinsics.Vector64<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftLogical(System.Runtime.Intrinsics.Vector64<sbyte> value, System.Runtime.Intrinsics.Vector64<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ShiftLogical(System.Runtime.Intrinsics.Vector64<ushort> value, System.Runtime.Intrinsics.Vector64<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ShiftLogical(System.Runtime.Intrinsics.Vector64<uint> value, System.Runtime.Intrinsics.Vector64<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ShiftLogicalRounded(System.Runtime.Intrinsics.Vector128<byte> value, System.Runtime.Intrinsics.Vector128<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftLogicalRounded(System.Runtime.Intrinsics.Vector128<short> value, System.Runtime.Intrinsics.Vector128<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftLogicalRounded(System.Runtime.Intrinsics.Vector128<int> value, System.Runtime.Intrinsics.Vector128<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftLogicalRounded(System.Runtime.Intrinsics.Vector128<long> value, System.Runtime.Intrinsics.Vector128<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftLogicalRounded(System.Runtime.Intrinsics.Vector128<sbyte> value, System.Runtime.Intrinsics.Vector128<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftLogicalRounded(System.Runtime.Intrinsics.Vector128<ushort> value, System.Runtime.Intrinsics.Vector128<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftLogicalRounded(System.Runtime.Intrinsics.Vector128<uint> value, System.Runtime.Intrinsics.Vector128<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftLogicalRounded(System.Runtime.Intrinsics.Vector128<ulong> value, System.Runtime.Intrinsics.Vector128<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ShiftLogicalRounded(System.Runtime.Intrinsics.Vector64<byte> value, System.Runtime.Intrinsics.Vector64<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftLogicalRounded(System.Runtime.Intrinsics.Vector64<short> value, System.Runtime.Intrinsics.Vector64<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftLogicalRounded(System.Runtime.Intrinsics.Vector64<int> value, System.Runtime.Intrinsics.Vector64<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftLogicalRounded(System.Runtime.Intrinsics.Vector64<sbyte> value, System.Runtime.Intrinsics.Vector64<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ShiftLogicalRounded(System.Runtime.Intrinsics.Vector64<ushort> value, System.Runtime.Intrinsics.Vector64<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ShiftLogicalRounded(System.Runtime.Intrinsics.Vector64<uint> value, System.Runtime.Intrinsics.Vector64<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ShiftLogicalRoundedSaturate(System.Runtime.Intrinsics.Vector128<byte> value, System.Runtime.Intrinsics.Vector128<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftLogicalRoundedSaturate(System.Runtime.Intrinsics.Vector128<short> value, System.Runtime.Intrinsics.Vector128<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftLogicalRoundedSaturate(System.Runtime.Intrinsics.Vector128<int> value, System.Runtime.Intrinsics.Vector128<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftLogicalRoundedSaturate(System.Runtime.Intrinsics.Vector128<long> value, System.Runtime.Intrinsics.Vector128<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftLogicalRoundedSaturate(System.Runtime.Intrinsics.Vector128<sbyte> value, System.Runtime.Intrinsics.Vector128<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftLogicalRoundedSaturate(System.Runtime.Intrinsics.Vector128<ushort> value, System.Runtime.Intrinsics.Vector128<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftLogicalRoundedSaturate(System.Runtime.Intrinsics.Vector128<uint> value, System.Runtime.Intrinsics.Vector128<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftLogicalRoundedSaturate(System.Runtime.Intrinsics.Vector128<ulong> value, System.Runtime.Intrinsics.Vector128<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ShiftLogicalRoundedSaturate(System.Runtime.Intrinsics.Vector64<byte> value, System.Runtime.Intrinsics.Vector64<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftLogicalRoundedSaturate(System.Runtime.Intrinsics.Vector64<short> value, System.Runtime.Intrinsics.Vector64<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftLogicalRoundedSaturate(System.Runtime.Intrinsics.Vector64<int> value, System.Runtime.Intrinsics.Vector64<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftLogicalRoundedSaturate(System.Runtime.Intrinsics.Vector64<sbyte> value, System.Runtime.Intrinsics.Vector64<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ShiftLogicalRoundedSaturate(System.Runtime.Intrinsics.Vector64<ushort> value, System.Runtime.Intrinsics.Vector64<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ShiftLogicalRoundedSaturate(System.Runtime.Intrinsics.Vector64<uint> value, System.Runtime.Intrinsics.Vector64<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ShiftLogicalRoundedSaturateScalar(System.Runtime.Intrinsics.Vector64<long> value, System.Runtime.Intrinsics.Vector64<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> ShiftLogicalRoundedSaturateScalar(System.Runtime.Intrinsics.Vector64<ulong> value, System.Runtime.Intrinsics.Vector64<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ShiftLogicalRoundedScalar(System.Runtime.Intrinsics.Vector64<long> value, System.Runtime.Intrinsics.Vector64<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> ShiftLogicalRoundedScalar(System.Runtime.Intrinsics.Vector64<ulong> value, System.Runtime.Intrinsics.Vector64<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ShiftLogicalSaturate(System.Runtime.Intrinsics.Vector128<byte> value, System.Runtime.Intrinsics.Vector128<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftLogicalSaturate(System.Runtime.Intrinsics.Vector128<short> value, System.Runtime.Intrinsics.Vector128<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftLogicalSaturate(System.Runtime.Intrinsics.Vector128<int> value, System.Runtime.Intrinsics.Vector128<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftLogicalSaturate(System.Runtime.Intrinsics.Vector128<long> value, System.Runtime.Intrinsics.Vector128<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftLogicalSaturate(System.Runtime.Intrinsics.Vector128<sbyte> value, System.Runtime.Intrinsics.Vector128<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftLogicalSaturate(System.Runtime.Intrinsics.Vector128<ushort> value, System.Runtime.Intrinsics.Vector128<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftLogicalSaturate(System.Runtime.Intrinsics.Vector128<uint> value, System.Runtime.Intrinsics.Vector128<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftLogicalSaturate(System.Runtime.Intrinsics.Vector128<ulong> value, System.Runtime.Intrinsics.Vector128<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ShiftLogicalSaturate(System.Runtime.Intrinsics.Vector64<byte> value, System.Runtime.Intrinsics.Vector64<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftLogicalSaturate(System.Runtime.Intrinsics.Vector64<short> value, System.Runtime.Intrinsics.Vector64<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftLogicalSaturate(System.Runtime.Intrinsics.Vector64<int> value, System.Runtime.Intrinsics.Vector64<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftLogicalSaturate(System.Runtime.Intrinsics.Vector64<sbyte> value, System.Runtime.Intrinsics.Vector64<sbyte> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ShiftLogicalSaturate(System.Runtime.Intrinsics.Vector64<ushort> value, System.Runtime.Intrinsics.Vector64<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ShiftLogicalSaturate(System.Runtime.Intrinsics.Vector64<uint> value, System.Runtime.Intrinsics.Vector64<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ShiftLogicalSaturateScalar(System.Runtime.Intrinsics.Vector64<long> value, System.Runtime.Intrinsics.Vector64<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> ShiftLogicalSaturateScalar(System.Runtime.Intrinsics.Vector64<ulong> value, System.Runtime.Intrinsics.Vector64<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ShiftLogicalScalar(System.Runtime.Intrinsics.Vector64<long> value, System.Runtime.Intrinsics.Vector64<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> ShiftLogicalScalar(System.Runtime.Intrinsics.Vector64<ulong> value, System.Runtime.Intrinsics.Vector64<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ShiftRightAndInsert(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftRightAndInsert(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightAndInsert(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftRightAndInsert(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftRightAndInsert(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftRightAndInsert(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftRightAndInsert(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftRightAndInsert(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ShiftRightAndInsert(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftRightAndInsert(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftRightAndInsert(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftRightAndInsert(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ShiftRightAndInsert(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ShiftRightAndInsert(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ShiftRightAndInsertScalar(System.Runtime.Intrinsics.Vector64<long> left, System.Runtime.Intrinsics.Vector64<long> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> ShiftRightAndInsertScalar(System.Runtime.Intrinsics.Vector64<ulong> left, System.Runtime.Intrinsics.Vector64<ulong> right, byte shift) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector128<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector64<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector64<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector64<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftRightArithmeticAdd(System.Runtime.Intrinsics.Vector128<short> addend, System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightArithmeticAdd(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftRightArithmeticAdd(System.Runtime.Intrinsics.Vector128<long> addend, System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftRightArithmeticAdd(System.Runtime.Intrinsics.Vector128<sbyte> addend, System.Runtime.Intrinsics.Vector128<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftRightArithmeticAdd(System.Runtime.Intrinsics.Vector64<short> addend, System.Runtime.Intrinsics.Vector64<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftRightArithmeticAdd(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftRightArithmeticAdd(System.Runtime.Intrinsics.Vector64<sbyte> addend, System.Runtime.Intrinsics.Vector64<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ShiftRightArithmeticAddScalar(System.Runtime.Intrinsics.Vector64<long> addend, System.Runtime.Intrinsics.Vector64<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftRightArithmeticNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftRightArithmeticNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftRightArithmeticNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ShiftRightArithmeticNarrowingSaturateUnsignedLower(System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ShiftRightArithmeticNarrowingSaturateUnsignedLower(System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ShiftRightArithmeticNarrowingSaturateUnsignedLower(System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ShiftRightArithmeticNarrowingSaturateUnsignedUpper(System.Runtime.Intrinsics.Vector64<byte> lower, System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftRightArithmeticNarrowingSaturateUnsignedUpper(System.Runtime.Intrinsics.Vector64<ushort> lower, System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftRightArithmeticNarrowingSaturateUnsignedUpper(System.Runtime.Intrinsics.Vector64<uint> lower, System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftRightArithmeticNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<short> lower, System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightArithmeticNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<int> lower, System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftRightArithmeticNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<sbyte> lower, System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftRightArithmeticRounded(System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightArithmeticRounded(System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftRightArithmeticRounded(System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftRightArithmeticRounded(System.Runtime.Intrinsics.Vector128<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftRightArithmeticRounded(System.Runtime.Intrinsics.Vector64<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftRightArithmeticRounded(System.Runtime.Intrinsics.Vector64<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftRightArithmeticRounded(System.Runtime.Intrinsics.Vector64<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftRightArithmeticRoundedAdd(System.Runtime.Intrinsics.Vector128<short> addend, System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightArithmeticRoundedAdd(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftRightArithmeticRoundedAdd(System.Runtime.Intrinsics.Vector128<long> addend, System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftRightArithmeticRoundedAdd(System.Runtime.Intrinsics.Vector128<sbyte> addend, System.Runtime.Intrinsics.Vector128<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftRightArithmeticRoundedAdd(System.Runtime.Intrinsics.Vector64<short> addend, System.Runtime.Intrinsics.Vector64<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftRightArithmeticRoundedAdd(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftRightArithmeticRoundedAdd(System.Runtime.Intrinsics.Vector64<sbyte> addend, System.Runtime.Intrinsics.Vector64<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ShiftRightArithmeticRoundedAddScalar(System.Runtime.Intrinsics.Vector64<long> addend, System.Runtime.Intrinsics.Vector64<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftRightArithmeticRoundedNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftRightArithmeticRoundedNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftRightArithmeticRoundedNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLower(System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedUpper(System.Runtime.Intrinsics.Vector64<byte> lower, System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedUpper(System.Runtime.Intrinsics.Vector64<ushort> lower, System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedUpper(System.Runtime.Intrinsics.Vector64<uint> lower, System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftRightArithmeticRoundedNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<short> lower, System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightArithmeticRoundedNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<int> lower, System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftRightArithmeticRoundedNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<sbyte> lower, System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ShiftRightArithmeticRoundedScalar(System.Runtime.Intrinsics.Vector64<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ShiftRightArithmeticScalar(System.Runtime.Intrinsics.Vector64<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<byte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ShiftRightLogical(System.Runtime.Intrinsics.Vector64<byte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftRightLogical(System.Runtime.Intrinsics.Vector64<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftRightLogical(System.Runtime.Intrinsics.Vector64<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftRightLogical(System.Runtime.Intrinsics.Vector64<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ShiftRightLogical(System.Runtime.Intrinsics.Vector64<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ShiftRightLogical(System.Runtime.Intrinsics.Vector64<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ShiftRightLogicalAdd(System.Runtime.Intrinsics.Vector128<byte> addend, System.Runtime.Intrinsics.Vector128<byte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftRightLogicalAdd(System.Runtime.Intrinsics.Vector128<short> addend, System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightLogicalAdd(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftRightLogicalAdd(System.Runtime.Intrinsics.Vector128<long> addend, System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftRightLogicalAdd(System.Runtime.Intrinsics.Vector128<sbyte> addend, System.Runtime.Intrinsics.Vector128<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftRightLogicalAdd(System.Runtime.Intrinsics.Vector128<ushort> addend, System.Runtime.Intrinsics.Vector128<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftRightLogicalAdd(System.Runtime.Intrinsics.Vector128<uint> addend, System.Runtime.Intrinsics.Vector128<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftRightLogicalAdd(System.Runtime.Intrinsics.Vector128<ulong> addend, System.Runtime.Intrinsics.Vector128<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ShiftRightLogicalAdd(System.Runtime.Intrinsics.Vector64<byte> addend, System.Runtime.Intrinsics.Vector64<byte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftRightLogicalAdd(System.Runtime.Intrinsics.Vector64<short> addend, System.Runtime.Intrinsics.Vector64<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftRightLogicalAdd(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftRightLogicalAdd(System.Runtime.Intrinsics.Vector64<sbyte> addend, System.Runtime.Intrinsics.Vector64<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ShiftRightLogicalAdd(System.Runtime.Intrinsics.Vector64<ushort> addend, System.Runtime.Intrinsics.Vector64<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ShiftRightLogicalAdd(System.Runtime.Intrinsics.Vector64<uint> addend, System.Runtime.Intrinsics.Vector64<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ShiftRightLogicalAddScalar(System.Runtime.Intrinsics.Vector64<long> addend, System.Runtime.Intrinsics.Vector64<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> ShiftRightLogicalAddScalar(System.Runtime.Intrinsics.Vector64<ulong> addend, System.Runtime.Intrinsics.Vector64<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftRightLogicalNarrowingLower(System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftRightLogicalNarrowingLower(System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftRightLogicalNarrowingLower(System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ShiftRightLogicalNarrowingLower(System.Runtime.Intrinsics.Vector128<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ShiftRightLogicalNarrowingLower(System.Runtime.Intrinsics.Vector128<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ShiftRightLogicalNarrowingLower(System.Runtime.Intrinsics.Vector128<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftRightLogicalNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftRightLogicalNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftRightLogicalNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ShiftRightLogicalNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ShiftRightLogicalNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ShiftRightLogicalNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ShiftRightLogicalNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<byte> lower, System.Runtime.Intrinsics.Vector128<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftRightLogicalNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<short> lower, System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightLogicalNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<int> lower, System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftRightLogicalNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<sbyte> lower, System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftRightLogicalNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<ushort> lower, System.Runtime.Intrinsics.Vector128<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftRightLogicalNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<uint> lower, System.Runtime.Intrinsics.Vector128<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ShiftRightLogicalNarrowingUpper(System.Runtime.Intrinsics.Vector64<byte> lower, System.Runtime.Intrinsics.Vector128<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftRightLogicalNarrowingUpper(System.Runtime.Intrinsics.Vector64<short> lower, System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightLogicalNarrowingUpper(System.Runtime.Intrinsics.Vector64<int> lower, System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftRightLogicalNarrowingUpper(System.Runtime.Intrinsics.Vector64<sbyte> lower, System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftRightLogicalNarrowingUpper(System.Runtime.Intrinsics.Vector64<ushort> lower, System.Runtime.Intrinsics.Vector128<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftRightLogicalNarrowingUpper(System.Runtime.Intrinsics.Vector64<uint> lower, System.Runtime.Intrinsics.Vector128<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ShiftRightLogicalRounded(System.Runtime.Intrinsics.Vector128<byte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftRightLogicalRounded(System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightLogicalRounded(System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftRightLogicalRounded(System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftRightLogicalRounded(System.Runtime.Intrinsics.Vector128<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftRightLogicalRounded(System.Runtime.Intrinsics.Vector128<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftRightLogicalRounded(System.Runtime.Intrinsics.Vector128<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftRightLogicalRounded(System.Runtime.Intrinsics.Vector128<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ShiftRightLogicalRounded(System.Runtime.Intrinsics.Vector64<byte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftRightLogicalRounded(System.Runtime.Intrinsics.Vector64<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftRightLogicalRounded(System.Runtime.Intrinsics.Vector64<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftRightLogicalRounded(System.Runtime.Intrinsics.Vector64<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ShiftRightLogicalRounded(System.Runtime.Intrinsics.Vector64<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ShiftRightLogicalRounded(System.Runtime.Intrinsics.Vector64<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ShiftRightLogicalRoundedAdd(System.Runtime.Intrinsics.Vector128<byte> addend, System.Runtime.Intrinsics.Vector128<byte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftRightLogicalRoundedAdd(System.Runtime.Intrinsics.Vector128<short> addend, System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightLogicalRoundedAdd(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftRightLogicalRoundedAdd(System.Runtime.Intrinsics.Vector128<long> addend, System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftRightLogicalRoundedAdd(System.Runtime.Intrinsics.Vector128<sbyte> addend, System.Runtime.Intrinsics.Vector128<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftRightLogicalRoundedAdd(System.Runtime.Intrinsics.Vector128<ushort> addend, System.Runtime.Intrinsics.Vector128<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftRightLogicalRoundedAdd(System.Runtime.Intrinsics.Vector128<uint> addend, System.Runtime.Intrinsics.Vector128<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftRightLogicalRoundedAdd(System.Runtime.Intrinsics.Vector128<ulong> addend, System.Runtime.Intrinsics.Vector128<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ShiftRightLogicalRoundedAdd(System.Runtime.Intrinsics.Vector64<byte> addend, System.Runtime.Intrinsics.Vector64<byte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftRightLogicalRoundedAdd(System.Runtime.Intrinsics.Vector64<short> addend, System.Runtime.Intrinsics.Vector64<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftRightLogicalRoundedAdd(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftRightLogicalRoundedAdd(System.Runtime.Intrinsics.Vector64<sbyte> addend, System.Runtime.Intrinsics.Vector64<sbyte> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ShiftRightLogicalRoundedAdd(System.Runtime.Intrinsics.Vector64<ushort> addend, System.Runtime.Intrinsics.Vector64<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ShiftRightLogicalRoundedAdd(System.Runtime.Intrinsics.Vector64<uint> addend, System.Runtime.Intrinsics.Vector64<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ShiftRightLogicalRoundedAddScalar(System.Runtime.Intrinsics.Vector64<long> addend, System.Runtime.Intrinsics.Vector64<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> ShiftRightLogicalRoundedAddScalar(System.Runtime.Intrinsics.Vector64<ulong> addend, System.Runtime.Intrinsics.Vector64<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftRightLogicalRoundedNarrowingLower(System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftRightLogicalRoundedNarrowingLower(System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftRightLogicalRoundedNarrowingLower(System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ShiftRightLogicalRoundedNarrowingLower(System.Runtime.Intrinsics.Vector128<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ShiftRightLogicalRoundedNarrowingLower(System.Runtime.Intrinsics.Vector128<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ShiftRightLogicalRoundedNarrowingLower(System.Runtime.Intrinsics.Vector128<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftRightLogicalRoundedNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> ShiftRightLogicalRoundedNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> ShiftRightLogicalRoundedNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> ShiftRightLogicalRoundedNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> ShiftRightLogicalRoundedNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> ShiftRightLogicalRoundedNarrowingSaturateLower(System.Runtime.Intrinsics.Vector128<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ShiftRightLogicalRoundedNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<byte> lower, System.Runtime.Intrinsics.Vector128<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftRightLogicalRoundedNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<short> lower, System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightLogicalRoundedNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<int> lower, System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftRightLogicalRoundedNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<sbyte> lower, System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftRightLogicalRoundedNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<ushort> lower, System.Runtime.Intrinsics.Vector128<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftRightLogicalRoundedNarrowingSaturateUpper(System.Runtime.Intrinsics.Vector64<uint> lower, System.Runtime.Intrinsics.Vector128<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ShiftRightLogicalRoundedNarrowingUpper(System.Runtime.Intrinsics.Vector64<byte> lower, System.Runtime.Intrinsics.Vector128<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftRightLogicalRoundedNarrowingUpper(System.Runtime.Intrinsics.Vector64<short> lower, System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightLogicalRoundedNarrowingUpper(System.Runtime.Intrinsics.Vector64<int> lower, System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftRightLogicalRoundedNarrowingUpper(System.Runtime.Intrinsics.Vector64<sbyte> lower, System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftRightLogicalRoundedNarrowingUpper(System.Runtime.Intrinsics.Vector64<ushort> lower, System.Runtime.Intrinsics.Vector128<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftRightLogicalRoundedNarrowingUpper(System.Runtime.Intrinsics.Vector64<uint> lower, System.Runtime.Intrinsics.Vector128<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ShiftRightLogicalRoundedScalar(System.Runtime.Intrinsics.Vector64<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> ShiftRightLogicalRoundedScalar(System.Runtime.Intrinsics.Vector64<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> ShiftRightLogicalScalar(System.Runtime.Intrinsics.Vector64<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> ShiftRightLogicalScalar(System.Runtime.Intrinsics.Vector64<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> SignExtendWideningLower(System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> SignExtendWideningLower(System.Runtime.Intrinsics.Vector64<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> SignExtendWideningLower(System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> SignExtendWideningUpper(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> SignExtendWideningUpper(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> SignExtendWideningUpper(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> SqrtScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> SqrtScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
        public unsafe static void Store(byte* address, System.Runtime.Intrinsics.Vector128<byte> source) { }
        public unsafe static void Store(byte* address, System.Runtime.Intrinsics.Vector64<byte> source) { }
        public unsafe static void Store(double* address, System.Runtime.Intrinsics.Vector128<double> source) { }
        public unsafe static void Store(double* address, System.Runtime.Intrinsics.Vector64<double> source) { }
        public unsafe static void Store(short* address, System.Runtime.Intrinsics.Vector128<short> source) { }
        public unsafe static void Store(short* address, System.Runtime.Intrinsics.Vector64<short> source) { }
        public unsafe static void Store(int* address, System.Runtime.Intrinsics.Vector128<int> source) { }
        public unsafe static void Store(int* address, System.Runtime.Intrinsics.Vector64<int> source) { }
        public unsafe static void Store(long* address, System.Runtime.Intrinsics.Vector128<long> source) { }
        public unsafe static void Store(long* address, System.Runtime.Intrinsics.Vector64<long> source) { }
        public unsafe static void Store(sbyte* address, System.Runtime.Intrinsics.Vector128<sbyte> source) { }
        public unsafe static void Store(sbyte* address, System.Runtime.Intrinsics.Vector64<sbyte> source) { }
        public unsafe static void Store(float* address, System.Runtime.Intrinsics.Vector128<float> source) { }
        public unsafe static void Store(float* address, System.Runtime.Intrinsics.Vector64<float> source) { }
        public unsafe static void Store(ushort* address, System.Runtime.Intrinsics.Vector128<ushort> source) { }
        public unsafe static void Store(ushort* address, System.Runtime.Intrinsics.Vector64<ushort> source) { }
        public unsafe static void Store(uint* address, System.Runtime.Intrinsics.Vector128<uint> source) { }
        public unsafe static void Store(uint* address, System.Runtime.Intrinsics.Vector64<uint> source) { }
        public unsafe static void Store(ulong* address, System.Runtime.Intrinsics.Vector128<ulong> source) { }
        public unsafe static void Store(ulong* address, System.Runtime.Intrinsics.Vector64<ulong> source) { }
        public unsafe static void StoreSelectedScalar(byte* address, System.Runtime.Intrinsics.Vector128<byte> value, byte index) { }
        public unsafe static void StoreSelectedScalar(byte* address, System.Runtime.Intrinsics.Vector64<byte> value, byte index) { }
        public unsafe static void StoreSelectedScalar(double* address, System.Runtime.Intrinsics.Vector128<double> value, byte index) { }
        public unsafe static void StoreSelectedScalar(short* address, System.Runtime.Intrinsics.Vector128<short> value, byte index) { }
        public unsafe static void StoreSelectedScalar(short* address, System.Runtime.Intrinsics.Vector64<short> value, byte index) { }
        public unsafe static void StoreSelectedScalar(int* address, System.Runtime.Intrinsics.Vector128<int> value, byte index) { }
        public unsafe static void StoreSelectedScalar(int* address, System.Runtime.Intrinsics.Vector64<int> value, byte index) { }
        public unsafe static void StoreSelectedScalar(long* address, System.Runtime.Intrinsics.Vector128<long> value, byte index) { }
        public unsafe static void StoreSelectedScalar(sbyte* address, System.Runtime.Intrinsics.Vector128<sbyte> value, byte index) { }
        public unsafe static void StoreSelectedScalar(sbyte* address, System.Runtime.Intrinsics.Vector64<sbyte> value, byte index) { }
        public unsafe static void StoreSelectedScalar(float* address, System.Runtime.Intrinsics.Vector128<float> value, byte index) { }
        public unsafe static void StoreSelectedScalar(float* address, System.Runtime.Intrinsics.Vector64<float> value, byte index) { }
        public unsafe static void StoreSelectedScalar(ushort* address, System.Runtime.Intrinsics.Vector128<ushort> value, byte index) { }
        public unsafe static void StoreSelectedScalar(ushort* address, System.Runtime.Intrinsics.Vector64<ushort> value, byte index) { }
        public unsafe static void StoreSelectedScalar(uint* address, System.Runtime.Intrinsics.Vector128<uint> value, byte index) { }
        public unsafe static void StoreSelectedScalar(uint* address, System.Runtime.Intrinsics.Vector64<uint> value, byte index) { }
        public unsafe static void StoreSelectedScalar(ulong* address, System.Runtime.Intrinsics.Vector128<ulong> value, byte index) { }
        public static System.Runtime.Intrinsics.Vector128<byte> Subtract(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> Subtract(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Subtract(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> Subtract(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> Subtract(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Subtract(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> Subtract(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> Subtract(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> Subtract(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> Subtract(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> Subtract(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> Subtract(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> Subtract(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> Subtract(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> Subtract(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> Subtract(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> SubtractHighNarrowingLower(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> SubtractHighNarrowingLower(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> SubtractHighNarrowingLower(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> SubtractHighNarrowingLower(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> SubtractHighNarrowingLower(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> SubtractHighNarrowingLower(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> SubtractHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<byte> lower, System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> SubtractHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<short> lower, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> SubtractHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<int> lower, System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> SubtractHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<sbyte> lower, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> SubtractHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<ushort> lower, System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> SubtractHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<uint> lower, System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> SubtractRoundedHighNarrowingLower(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> SubtractRoundedHighNarrowingLower(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> SubtractRoundedHighNarrowingLower(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> SubtractRoundedHighNarrowingLower(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> SubtractRoundedHighNarrowingLower(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> SubtractRoundedHighNarrowingLower(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> SubtractRoundedHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<byte> lower, System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> SubtractRoundedHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<short> lower, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> SubtractRoundedHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<int> lower, System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> SubtractRoundedHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<sbyte> lower, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> SubtractRoundedHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<ushort> lower, System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> SubtractRoundedHighNarrowingUpper(System.Runtime.Intrinsics.Vector64<uint> lower, System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> SubtractSaturate(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> SubtractSaturate(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> SubtractSaturate(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> SubtractSaturate(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> SubtractSaturate(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> SubtractSaturate(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> SubtractSaturate(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> SubtractSaturate(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> SubtractSaturate(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> SubtractSaturate(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> SubtractSaturate(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> SubtractSaturate(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> SubtractSaturate(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> SubtractSaturate(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> SubtractSaturateScalar(System.Runtime.Intrinsics.Vector64<long> left, System.Runtime.Intrinsics.Vector64<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> SubtractSaturateScalar(System.Runtime.Intrinsics.Vector64<ulong> left, System.Runtime.Intrinsics.Vector64<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> SubtractScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> SubtractScalar(System.Runtime.Intrinsics.Vector64<long> left, System.Runtime.Intrinsics.Vector64<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> SubtractScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> SubtractScalar(System.Runtime.Intrinsics.Vector64<ulong> left, System.Runtime.Intrinsics.Vector64<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> SubtractWideningLower(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> SubtractWideningLower(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> SubtractWideningLower(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> SubtractWideningLower(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> SubtractWideningLower(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> SubtractWideningLower(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> SubtractWideningLower(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> SubtractWideningLower(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> SubtractWideningLower(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> SubtractWideningLower(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> SubtractWideningLower(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> SubtractWideningLower(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> SubtractWideningUpper(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> SubtractWideningUpper(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> SubtractWideningUpper(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> SubtractWideningUpper(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> SubtractWideningUpper(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> SubtractWideningUpper(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> SubtractWideningUpper(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> SubtractWideningUpper(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> SubtractWideningUpper(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> SubtractWideningUpper(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> SubtractWideningUpper(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> SubtractWideningUpper(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> VectorTableLookup(System.Runtime.Intrinsics.Vector128<byte> table, System.Runtime.Intrinsics.Vector64<byte> byteIndexes) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> VectorTableLookup(System.Runtime.Intrinsics.Vector128<sbyte> table, System.Runtime.Intrinsics.Vector64<sbyte> byteIndexes) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> VectorTableLookupExtension(System.Runtime.Intrinsics.Vector64<byte> defaultValues, System.Runtime.Intrinsics.Vector128<byte> table, System.Runtime.Intrinsics.Vector64<byte> byteIndexes) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> VectorTableLookupExtension(System.Runtime.Intrinsics.Vector64<sbyte> defaultValues, System.Runtime.Intrinsics.Vector128<sbyte> table, System.Runtime.Intrinsics.Vector64<sbyte> byteIndexes) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Xor(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Xor(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> Xor(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Xor(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> Xor(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> Xor(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Xor(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> Xor(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> Xor(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> Xor(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<byte> Xor(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<double> Xor(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> Xor(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> Xor(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<long> Xor(System.Runtime.Intrinsics.Vector64<long> left, System.Runtime.Intrinsics.Vector64<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<sbyte> Xor(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<float> Xor(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ushort> Xor(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> Xor(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<ulong> Xor(System.Runtime.Intrinsics.Vector64<ulong> left, System.Runtime.Intrinsics.Vector64<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ZeroExtendWideningLower(System.Runtime.Intrinsics.Vector64<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ZeroExtendWideningLower(System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ZeroExtendWideningLower(System.Runtime.Intrinsics.Vector64<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ZeroExtendWideningLower(System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ZeroExtendWideningLower(System.Runtime.Intrinsics.Vector64<ushort> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ZeroExtendWideningLower(System.Runtime.Intrinsics.Vector64<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ZeroExtendWideningUpper(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ZeroExtendWideningUpper(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ZeroExtendWideningUpper(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ZeroExtendWideningUpper(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ZeroExtendWideningUpper(System.Runtime.Intrinsics.Vector128<ushort> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ZeroExtendWideningUpper(System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
        public new abstract partial class Arm64 : System.Runtime.Intrinsics.Arm.ArmBase.Arm64
        {
            internal Arm64() { }
            public static new bool IsSupported { get { throw null; } }
            public static System.Runtime.Intrinsics.Vector128<double> Abs(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> Abs(System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> AbsoluteCompareGreaterThan(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> AbsoluteCompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> AbsoluteCompareGreaterThanOrEqualScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> AbsoluteCompareGreaterThanOrEqualScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> AbsoluteCompareGreaterThanScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> AbsoluteCompareGreaterThanScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> AbsoluteCompareLessThan(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> AbsoluteCompareLessThanOrEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> AbsoluteCompareLessThanOrEqualScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> AbsoluteCompareLessThanOrEqualScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> AbsoluteCompareLessThanScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> AbsoluteCompareLessThanScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> AbsoluteDifference(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> AbsoluteDifferenceScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> AbsoluteDifferenceScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> AbsSaturate(System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> AbsSaturateScalar(System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> AbsSaturateScalar(System.Runtime.Intrinsics.Vector64<int> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> AbsSaturateScalar(System.Runtime.Intrinsics.Vector64<long> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> AbsSaturateScalar(System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ulong> AbsScalar(System.Runtime.Intrinsics.Vector64<long> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> Add(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> AddAcross(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> AddAcross(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> AddAcross(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> AddAcross(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> AddAcross(System.Runtime.Intrinsics.Vector128<ushort> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> AddAcross(System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> AddAcross(System.Runtime.Intrinsics.Vector64<byte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> AddAcross(System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> AddAcross(System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> AddAcross(System.Runtime.Intrinsics.Vector64<ushort> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> AddAcrossWidening(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> AddAcrossWidening(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> AddAcrossWidening(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> AddAcrossWidening(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> AddAcrossWidening(System.Runtime.Intrinsics.Vector128<ushort> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ulong> AddAcrossWidening(System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> AddAcrossWidening(System.Runtime.Intrinsics.Vector64<byte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> AddAcrossWidening(System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> AddAcrossWidening(System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> AddAcrossWidening(System.Runtime.Intrinsics.Vector64<ushort> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<byte> AddPairwise(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> AddPairwise(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<short> AddPairwise(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<int> AddPairwise(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> AddPairwise(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<sbyte> AddPairwise(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> AddPairwise(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ushort> AddPairwise(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<uint> AddPairwise(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> AddPairwise(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> AddPairwiseScalar(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> AddPairwiseScalar(System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ulong> AddPairwiseScalar(System.Runtime.Intrinsics.Vector128<ulong> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> AddPairwiseScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<byte> AddSaturate(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<short> AddSaturate(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<int> AddSaturate(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> AddSaturate(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<sbyte> AddSaturate(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ushort> AddSaturate(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<uint> AddSaturate(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> AddSaturate(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> AddSaturate(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> AddSaturate(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> AddSaturate(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> AddSaturate(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> AddSaturate(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> AddSaturate(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> AddSaturateScalar(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> AddSaturateScalar(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> AddSaturateScalar(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> AddSaturateScalar(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> AddSaturateScalar(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> AddSaturateScalar(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> AddSaturateScalar(System.Runtime.Intrinsics.Vector64<long> left, System.Runtime.Intrinsics.Vector64<ulong> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> AddSaturateScalar(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> AddSaturateScalar(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> AddSaturateScalar(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> AddSaturateScalar(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> AddSaturateScalar(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> AddSaturateScalar(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ulong> AddSaturateScalar(System.Runtime.Intrinsics.Vector64<ulong> left, System.Runtime.Intrinsics.Vector64<long> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> Ceiling(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> CompareEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> CompareEqual(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> CompareEqual(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> CompareEqualScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> CompareEqualScalar(System.Runtime.Intrinsics.Vector64<long> left, System.Runtime.Intrinsics.Vector64<long> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> CompareEqualScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ulong> CompareEqualScalar(System.Runtime.Intrinsics.Vector64<ulong> left, System.Runtime.Intrinsics.Vector64<ulong> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> CompareGreaterThan(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> CompareGreaterThan(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> CompareGreaterThan(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> CompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> CompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> CompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> CompareGreaterThanOrEqualScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> CompareGreaterThanOrEqualScalar(System.Runtime.Intrinsics.Vector64<long> left, System.Runtime.Intrinsics.Vector64<long> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> CompareGreaterThanOrEqualScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ulong> CompareGreaterThanOrEqualScalar(System.Runtime.Intrinsics.Vector64<ulong> left, System.Runtime.Intrinsics.Vector64<ulong> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> CompareGreaterThanScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> CompareGreaterThanScalar(System.Runtime.Intrinsics.Vector64<long> left, System.Runtime.Intrinsics.Vector64<long> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> CompareGreaterThanScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ulong> CompareGreaterThanScalar(System.Runtime.Intrinsics.Vector64<ulong> left, System.Runtime.Intrinsics.Vector64<ulong> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> CompareLessThan(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> CompareLessThan(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> CompareLessThan(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> CompareLessThanOrEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> CompareLessThanOrEqual(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> CompareLessThanOrEqual(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> CompareLessThanOrEqualScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> CompareLessThanOrEqualScalar(System.Runtime.Intrinsics.Vector64<long> left, System.Runtime.Intrinsics.Vector64<long> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> CompareLessThanOrEqualScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ulong> CompareLessThanOrEqualScalar(System.Runtime.Intrinsics.Vector64<ulong> left, System.Runtime.Intrinsics.Vector64<ulong> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> CompareLessThanScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> CompareLessThanScalar(System.Runtime.Intrinsics.Vector64<long> left, System.Runtime.Intrinsics.Vector64<long> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> CompareLessThanScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ulong> CompareLessThanScalar(System.Runtime.Intrinsics.Vector64<ulong> left, System.Runtime.Intrinsics.Vector64<ulong> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> CompareTest(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> CompareTest(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> CompareTest(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> CompareTestScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> CompareTestScalar(System.Runtime.Intrinsics.Vector64<long> left, System.Runtime.Intrinsics.Vector64<long> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ulong> CompareTestScalar(System.Runtime.Intrinsics.Vector64<ulong> left, System.Runtime.Intrinsics.Vector64<ulong> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> ConvertToDouble(System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> ConvertToDouble(System.Runtime.Intrinsics.Vector128<ulong> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> ConvertToDouble(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> ConvertToDoubleScalar(System.Runtime.Intrinsics.Vector64<long> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> ConvertToDoubleScalar(System.Runtime.Intrinsics.Vector64<ulong> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> ConvertToDoubleUpper(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> ConvertToInt64RoundAwayFromZero(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> ConvertToInt64RoundAwayFromZeroScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> ConvertToInt64RoundToEven(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> ConvertToInt64RoundToEvenScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> ConvertToInt64RoundToNegativeInfinity(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> ConvertToInt64RoundToNegativeInfinityScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> ConvertToInt64RoundToPositiveInfinity(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> ConvertToInt64RoundToPositiveInfinityScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> ConvertToInt64RoundToZero(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> ConvertToInt64RoundToZeroScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> ConvertToSingleLower(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> ConvertToSingleRoundToOddLower(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> ConvertToSingleRoundToOddUpper(System.Runtime.Intrinsics.Vector64<float> lower, System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> ConvertToSingleUpper(System.Runtime.Intrinsics.Vector64<float> lower, System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> ConvertToUInt64RoundAwayFromZero(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ulong> ConvertToUInt64RoundAwayFromZeroScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> ConvertToUInt64RoundToEven(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ulong> ConvertToUInt64RoundToEvenScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> ConvertToUInt64RoundToNegativeInfinity(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ulong> ConvertToUInt64RoundToNegativeInfinityScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> ConvertToUInt64RoundToPositiveInfinity(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ulong> ConvertToUInt64RoundToPositiveInfinityScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> ConvertToUInt64RoundToZero(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ulong> ConvertToUInt64RoundToZeroScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> Divide(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> Divide(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> Divide(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> DuplicateSelectedScalarToVector128(System.Runtime.Intrinsics.Vector128<double> value, byte index) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> DuplicateSelectedScalarToVector128(System.Runtime.Intrinsics.Vector128<long> value, byte index) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> DuplicateSelectedScalarToVector128(System.Runtime.Intrinsics.Vector128<ulong> value, byte index) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> DuplicateToVector128(double value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> DuplicateToVector128(long value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> DuplicateToVector128(ulong value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> ExtractNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> ExtractNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<int> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> ExtractNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<long> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> ExtractNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<ushort> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> ExtractNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<uint> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> ExtractNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<ulong> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> ExtractNarrowingSaturateUnsignedScalar(System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> ExtractNarrowingSaturateUnsignedScalar(System.Runtime.Intrinsics.Vector64<int> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> ExtractNarrowingSaturateUnsignedScalar(System.Runtime.Intrinsics.Vector64<long> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> Floor(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> FusedMultiplyAdd(System.Runtime.Intrinsics.Vector128<double> addend, System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> FusedMultiplyAddByScalar(System.Runtime.Intrinsics.Vector128<double> addend, System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> FusedMultiplyAddByScalar(System.Runtime.Intrinsics.Vector128<float> addend, System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> FusedMultiplyAddByScalar(System.Runtime.Intrinsics.Vector64<float> addend, System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> FusedMultiplyAddBySelectedScalar(System.Runtime.Intrinsics.Vector128<double> addend, System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> FusedMultiplyAddBySelectedScalar(System.Runtime.Intrinsics.Vector128<float> addend, System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> FusedMultiplyAddBySelectedScalar(System.Runtime.Intrinsics.Vector128<float> addend, System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector64<float> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> FusedMultiplyAddBySelectedScalar(System.Runtime.Intrinsics.Vector64<float> addend, System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector128<float> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> FusedMultiplyAddBySelectedScalar(System.Runtime.Intrinsics.Vector64<float> addend, System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> FusedMultiplyAddScalarBySelectedScalar(System.Runtime.Intrinsics.Vector64<double> addend, System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector128<double> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> FusedMultiplyAddScalarBySelectedScalar(System.Runtime.Intrinsics.Vector64<float> addend, System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector128<float> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> FusedMultiplyAddScalarBySelectedScalar(System.Runtime.Intrinsics.Vector64<float> addend, System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> FusedMultiplySubtract(System.Runtime.Intrinsics.Vector128<double> minuend, System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> FusedMultiplySubtractByScalar(System.Runtime.Intrinsics.Vector128<double> minuend, System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> FusedMultiplySubtractByScalar(System.Runtime.Intrinsics.Vector128<float> minuend, System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> FusedMultiplySubtractByScalar(System.Runtime.Intrinsics.Vector64<float> minuend, System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> FusedMultiplySubtractBySelectedScalar(System.Runtime.Intrinsics.Vector128<double> minuend, System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> FusedMultiplySubtractBySelectedScalar(System.Runtime.Intrinsics.Vector128<float> minuend, System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> FusedMultiplySubtractBySelectedScalar(System.Runtime.Intrinsics.Vector128<float> minuend, System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector64<float> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> FusedMultiplySubtractBySelectedScalar(System.Runtime.Intrinsics.Vector64<float> minuend, System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector128<float> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> FusedMultiplySubtractBySelectedScalar(System.Runtime.Intrinsics.Vector64<float> minuend, System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> FusedMultiplySubtractScalarBySelectedScalar(System.Runtime.Intrinsics.Vector64<double> minuend, System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector128<double> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> FusedMultiplySubtractScalarBySelectedScalar(System.Runtime.Intrinsics.Vector64<float> minuend, System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector128<float> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> FusedMultiplySubtractScalarBySelectedScalar(System.Runtime.Intrinsics.Vector64<float> minuend, System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<byte> InsertSelectedScalar(System.Runtime.Intrinsics.Vector128<byte> result, byte resultIndex, System.Runtime.Intrinsics.Vector128<byte> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<byte> InsertSelectedScalar(System.Runtime.Intrinsics.Vector128<byte> result, byte resultIndex, System.Runtime.Intrinsics.Vector64<byte> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> InsertSelectedScalar(System.Runtime.Intrinsics.Vector128<double> result, byte resultIndex, System.Runtime.Intrinsics.Vector128<double> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<short> InsertSelectedScalar(System.Runtime.Intrinsics.Vector128<short> result, byte resultIndex, System.Runtime.Intrinsics.Vector128<short> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<short> InsertSelectedScalar(System.Runtime.Intrinsics.Vector128<short> result, byte resultIndex, System.Runtime.Intrinsics.Vector64<short> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<int> InsertSelectedScalar(System.Runtime.Intrinsics.Vector128<int> result, byte resultIndex, System.Runtime.Intrinsics.Vector128<int> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<int> InsertSelectedScalar(System.Runtime.Intrinsics.Vector128<int> result, byte resultIndex, System.Runtime.Intrinsics.Vector64<int> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> InsertSelectedScalar(System.Runtime.Intrinsics.Vector128<long> result, byte resultIndex, System.Runtime.Intrinsics.Vector128<long> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<sbyte> InsertSelectedScalar(System.Runtime.Intrinsics.Vector128<sbyte> result, byte resultIndex, System.Runtime.Intrinsics.Vector128<sbyte> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<sbyte> InsertSelectedScalar(System.Runtime.Intrinsics.Vector128<sbyte> result, byte resultIndex, System.Runtime.Intrinsics.Vector64<sbyte> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> InsertSelectedScalar(System.Runtime.Intrinsics.Vector128<float> result, byte resultIndex, System.Runtime.Intrinsics.Vector128<float> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> InsertSelectedScalar(System.Runtime.Intrinsics.Vector128<float> result, byte resultIndex, System.Runtime.Intrinsics.Vector64<float> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ushort> InsertSelectedScalar(System.Runtime.Intrinsics.Vector128<ushort> result, byte resultIndex, System.Runtime.Intrinsics.Vector128<ushort> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ushort> InsertSelectedScalar(System.Runtime.Intrinsics.Vector128<ushort> result, byte resultIndex, System.Runtime.Intrinsics.Vector64<ushort> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<uint> InsertSelectedScalar(System.Runtime.Intrinsics.Vector128<uint> result, byte resultIndex, System.Runtime.Intrinsics.Vector128<uint> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<uint> InsertSelectedScalar(System.Runtime.Intrinsics.Vector128<uint> result, byte resultIndex, System.Runtime.Intrinsics.Vector64<uint> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> InsertSelectedScalar(System.Runtime.Intrinsics.Vector128<ulong> result, byte resultIndex, System.Runtime.Intrinsics.Vector128<ulong> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> InsertSelectedScalar(System.Runtime.Intrinsics.Vector64<byte> result, byte resultIndex, System.Runtime.Intrinsics.Vector128<byte> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> InsertSelectedScalar(System.Runtime.Intrinsics.Vector64<byte> result, byte resultIndex, System.Runtime.Intrinsics.Vector64<byte> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> InsertSelectedScalar(System.Runtime.Intrinsics.Vector64<short> result, byte resultIndex, System.Runtime.Intrinsics.Vector128<short> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> InsertSelectedScalar(System.Runtime.Intrinsics.Vector64<short> result, byte resultIndex, System.Runtime.Intrinsics.Vector64<short> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> InsertSelectedScalar(System.Runtime.Intrinsics.Vector64<int> result, byte resultIndex, System.Runtime.Intrinsics.Vector128<int> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> InsertSelectedScalar(System.Runtime.Intrinsics.Vector64<int> result, byte resultIndex, System.Runtime.Intrinsics.Vector64<int> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> InsertSelectedScalar(System.Runtime.Intrinsics.Vector64<sbyte> result, byte resultIndex, System.Runtime.Intrinsics.Vector128<sbyte> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> InsertSelectedScalar(System.Runtime.Intrinsics.Vector64<sbyte> result, byte resultIndex, System.Runtime.Intrinsics.Vector64<sbyte> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> InsertSelectedScalar(System.Runtime.Intrinsics.Vector64<float> result, byte resultIndex, System.Runtime.Intrinsics.Vector128<float> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> InsertSelectedScalar(System.Runtime.Intrinsics.Vector64<float> result, byte resultIndex, System.Runtime.Intrinsics.Vector64<float> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> InsertSelectedScalar(System.Runtime.Intrinsics.Vector64<ushort> result, byte resultIndex, System.Runtime.Intrinsics.Vector128<ushort> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> InsertSelectedScalar(System.Runtime.Intrinsics.Vector64<ushort> result, byte resultIndex, System.Runtime.Intrinsics.Vector64<ushort> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> InsertSelectedScalar(System.Runtime.Intrinsics.Vector64<uint> result, byte resultIndex, System.Runtime.Intrinsics.Vector128<uint> value, byte valueIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> InsertSelectedScalar(System.Runtime.Intrinsics.Vector64<uint> result, byte resultIndex, System.Runtime.Intrinsics.Vector64<uint> value, byte valueIndex) { throw null; }
            public unsafe static System.Runtime.Intrinsics.Vector128<double> LoadAndReplicateToVector128(double* address) { throw null; }
            public unsafe static System.Runtime.Intrinsics.Vector128<long> LoadAndReplicateToVector128(long* address) { throw null; }
            public unsafe static System.Runtime.Intrinsics.Vector128<ulong> LoadAndReplicateToVector128(ulong* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<int> Value1, System.Runtime.Intrinsics.Vector64<int> Value2) LoadPairScalarVector64(int* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<float> Value1, System.Runtime.Intrinsics.Vector64<float> Value2) LoadPairScalarVector64(float* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<uint> Value1, System.Runtime.Intrinsics.Vector64<uint> Value2) LoadPairScalarVector64(uint* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<int> Value1, System.Runtime.Intrinsics.Vector64<int> Value2) LoadPairScalarVector64NonTemporal(int* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<float> Value1, System.Runtime.Intrinsics.Vector64<float> Value2) LoadPairScalarVector64NonTemporal(float* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<uint> Value1, System.Runtime.Intrinsics.Vector64<uint> Value2) LoadPairScalarVector64NonTemporal(uint* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector128<byte> Value1, System.Runtime.Intrinsics.Vector128<byte> Value2) LoadPairVector128(byte* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector128<double> Value1, System.Runtime.Intrinsics.Vector128<double> Value2) LoadPairVector128(double* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector128<short> Value1, System.Runtime.Intrinsics.Vector128<short> Value2) LoadPairVector128(short* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector128<int> Value1, System.Runtime.Intrinsics.Vector128<int> Value2) LoadPairVector128(int* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector128<long> Value1, System.Runtime.Intrinsics.Vector128<long> Value2) LoadPairVector128(long* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector128<sbyte> Value1, System.Runtime.Intrinsics.Vector128<sbyte> Value2) LoadPairVector128(sbyte* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector128<float> Value1, System.Runtime.Intrinsics.Vector128<float> Value2) LoadPairVector128(float* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector128<ushort> Value1, System.Runtime.Intrinsics.Vector128<ushort> Value2) LoadPairVector128(ushort* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector128<uint> Value1, System.Runtime.Intrinsics.Vector128<uint> Value2) LoadPairVector128(uint* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector128<ulong> Value1, System.Runtime.Intrinsics.Vector128<ulong> Value2) LoadPairVector128(ulong* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector128<byte> Value1, System.Runtime.Intrinsics.Vector128<byte> Value2) LoadPairVector128NonTemporal(byte* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector128<double> Value1, System.Runtime.Intrinsics.Vector128<double> Value2) LoadPairVector128NonTemporal(double* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector128<short> Value1, System.Runtime.Intrinsics.Vector128<short> Value2) LoadPairVector128NonTemporal(short* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector128<int> Value1, System.Runtime.Intrinsics.Vector128<int> Value2) LoadPairVector128NonTemporal(int* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector128<long> Value1, System.Runtime.Intrinsics.Vector128<long> Value2) LoadPairVector128NonTemporal(long* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector128<sbyte> Value1, System.Runtime.Intrinsics.Vector128<sbyte> Value2) LoadPairVector128NonTemporal(sbyte* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector128<float> Value1, System.Runtime.Intrinsics.Vector128<float> Value2) LoadPairVector128NonTemporal(float* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector128<ushort> Value1, System.Runtime.Intrinsics.Vector128<ushort> Value2) LoadPairVector128NonTemporal(ushort* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector128<uint> Value1, System.Runtime.Intrinsics.Vector128<uint> Value2) LoadPairVector128NonTemporal(uint* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector128<ulong> Value1, System.Runtime.Intrinsics.Vector128<ulong> Value2) LoadPairVector128NonTemporal(ulong* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<byte> Value1, System.Runtime.Intrinsics.Vector64<byte> Value2) LoadPairVector64(byte* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<double> Value1, System.Runtime.Intrinsics.Vector64<double> Value2) LoadPairVector64(double* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<short> Value1, System.Runtime.Intrinsics.Vector64<short> Value2) LoadPairVector64(short* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<int> Value1, System.Runtime.Intrinsics.Vector64<int> Value2) LoadPairVector64(int* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<long> Value1, System.Runtime.Intrinsics.Vector64<long> Value2) LoadPairVector64(long* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<sbyte> Value1, System.Runtime.Intrinsics.Vector64<sbyte> Value2) LoadPairVector64(sbyte* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<float> Value1, System.Runtime.Intrinsics.Vector64<float> Value2) LoadPairVector64(float* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<ushort> Value1, System.Runtime.Intrinsics.Vector64<ushort> Value2) LoadPairVector64(ushort* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<uint> Value1, System.Runtime.Intrinsics.Vector64<uint> Value2) LoadPairVector64(uint* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<ulong> Value1, System.Runtime.Intrinsics.Vector64<ulong> Value2) LoadPairVector64(ulong* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<byte> Value1, System.Runtime.Intrinsics.Vector64<byte> Value2) LoadPairVector64NonTemporal(byte* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<double> Value1, System.Runtime.Intrinsics.Vector64<double> Value2) LoadPairVector64NonTemporal(double* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<short> Value1, System.Runtime.Intrinsics.Vector64<short> Value2) LoadPairVector64NonTemporal(short* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<int> Value1, System.Runtime.Intrinsics.Vector64<int> Value2) LoadPairVector64NonTemporal(int* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<long> Value1, System.Runtime.Intrinsics.Vector64<long> Value2) LoadPairVector64NonTemporal(long* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<sbyte> Value1, System.Runtime.Intrinsics.Vector64<sbyte> Value2) LoadPairVector64NonTemporal(sbyte* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<float> Value1, System.Runtime.Intrinsics.Vector64<float> Value2) LoadPairVector64NonTemporal(float* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<ushort> Value1, System.Runtime.Intrinsics.Vector64<ushort> Value2) LoadPairVector64NonTemporal(ushort* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<uint> Value1, System.Runtime.Intrinsics.Vector64<uint> Value2) LoadPairVector64NonTemporal(uint* address) { throw null; }
            public unsafe static (System.Runtime.Intrinsics.Vector64<ulong> Value1, System.Runtime.Intrinsics.Vector64<ulong> Value2) LoadPairVector64NonTemporal(ulong* address) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> Max(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> MaxAcross(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> MaxAcross(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MaxAcross(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> MaxAcross(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> MaxAcross(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> MaxAcross(System.Runtime.Intrinsics.Vector128<ushort> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> MaxAcross(System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> MaxAcross(System.Runtime.Intrinsics.Vector64<byte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> MaxAcross(System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> MaxAcross(System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> MaxAcross(System.Runtime.Intrinsics.Vector64<ushort> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> MaxNumber(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> MaxNumberAcross(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> MaxNumberPairwise(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> MaxNumberPairwise(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> MaxNumberPairwise(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> MaxNumberPairwiseScalar(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> MaxNumberPairwiseScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<byte> MaxPairwise(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> MaxPairwise(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<short> MaxPairwise(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<int> MaxPairwise(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<sbyte> MaxPairwise(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> MaxPairwise(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ushort> MaxPairwise(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<uint> MaxPairwise(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> MaxPairwiseScalar(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> MaxPairwiseScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> MaxScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> MaxScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> Min(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> MinAcross(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> MinAcross(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MinAcross(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> MinAcross(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> MinAcross(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> MinAcross(System.Runtime.Intrinsics.Vector128<ushort> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> MinAcross(System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> MinAcross(System.Runtime.Intrinsics.Vector64<byte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> MinAcross(System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> MinAcross(System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> MinAcross(System.Runtime.Intrinsics.Vector64<ushort> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> MinNumber(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> MinNumberAcross(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> MinNumberPairwise(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> MinNumberPairwise(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> MinNumberPairwise(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> MinNumberPairwiseScalar(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> MinNumberPairwiseScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<byte> MinPairwise(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> MinPairwise(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<short> MinPairwise(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<int> MinPairwise(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<sbyte> MinPairwise(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> MinPairwise(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ushort> MinPairwise(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<uint> MinPairwise(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> MinPairwiseScalar(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> MinPairwiseScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> MinScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> MinScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> Multiply(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> MultiplyByScalar(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> MultiplyBySelectedScalar(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> MultiplyDoublingSaturateHighScalar(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MultiplyDoublingSaturateHighScalar(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> MultiplyDoublingScalarBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> MultiplyDoublingScalarBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MultiplyDoublingScalarBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MultiplyDoublingScalarBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MultiplyDoublingWideningAndAddSaturateScalar(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> MultiplyDoublingWideningAndAddSaturateScalar(System.Runtime.Intrinsics.Vector64<long> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MultiplyDoublingWideningAndSubtractSaturateScalar(System.Runtime.Intrinsics.Vector64<int> minuend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> MultiplyDoublingWideningAndSubtractSaturateScalar(System.Runtime.Intrinsics.Vector64<long> minuend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MultiplyDoublingWideningSaturateScalar(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> MultiplyDoublingWideningSaturateScalar(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MultiplyDoublingWideningSaturateScalarBySelectedScalar(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MultiplyDoublingWideningSaturateScalarBySelectedScalar(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> MultiplyDoublingWideningSaturateScalarBySelectedScalar(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> MultiplyDoublingWideningSaturateScalarBySelectedScalar(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MultiplyDoublingWideningScalarBySelectedScalarAndAddSaturate(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MultiplyDoublingWideningScalarBySelectedScalarAndAddSaturate(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> MultiplyDoublingWideningScalarBySelectedScalarAndAddSaturate(System.Runtime.Intrinsics.Vector64<long> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> MultiplyDoublingWideningScalarBySelectedScalarAndAddSaturate(System.Runtime.Intrinsics.Vector64<long> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MultiplyDoublingWideningScalarBySelectedScalarAndSubtractSaturate(System.Runtime.Intrinsics.Vector64<int> minuend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MultiplyDoublingWideningScalarBySelectedScalarAndSubtractSaturate(System.Runtime.Intrinsics.Vector64<int> minuend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> MultiplyDoublingWideningScalarBySelectedScalarAndSubtractSaturate(System.Runtime.Intrinsics.Vector64<long> minuend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> MultiplyDoublingWideningScalarBySelectedScalarAndSubtractSaturate(System.Runtime.Intrinsics.Vector64<long> minuend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> MultiplyExtended(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> MultiplyExtended(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> MultiplyExtended(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> MultiplyExtendedByScalar(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> MultiplyExtendedBySelectedScalar(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> MultiplyExtendedBySelectedScalar(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> MultiplyExtendedBySelectedScalar(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector64<float> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> MultiplyExtendedBySelectedScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector128<float> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> MultiplyExtendedBySelectedScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> MultiplyExtendedScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> MultiplyExtendedScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> MultiplyExtendedScalarBySelectedScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector128<double> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> MultiplyExtendedScalarBySelectedScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector128<float> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> MultiplyExtendedScalarBySelectedScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> MultiplyRoundedDoublingSaturateHighScalar(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MultiplyRoundedDoublingSaturateHighScalar(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> MultiplyRoundedDoublingScalarBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> MultiplyRoundedDoublingScalarBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MultiplyRoundedDoublingScalarBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MultiplyRoundedDoublingScalarBySelectedScalarSaturateHigh(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> MultiplyScalarBySelectedScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector128<double> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> Negate(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> Negate(System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> NegateSaturate(System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> NegateSaturateScalar(System.Runtime.Intrinsics.Vector64<short> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> NegateSaturateScalar(System.Runtime.Intrinsics.Vector64<int> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> NegateSaturateScalar(System.Runtime.Intrinsics.Vector64<long> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> NegateSaturateScalar(System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<long> NegateScalar(System.Runtime.Intrinsics.Vector64<long> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> ReciprocalEstimate(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> ReciprocalEstimateScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> ReciprocalEstimateScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> ReciprocalExponentScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> ReciprocalExponentScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> ReciprocalSquareRootEstimate(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> ReciprocalSquareRootEstimateScalar(System.Runtime.Intrinsics.Vector64<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> ReciprocalSquareRootEstimateScalar(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> ReciprocalSquareRootStep(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> ReciprocalSquareRootStepScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> ReciprocalSquareRootStepScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> ReciprocalStep(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<double> ReciprocalStepScalar(System.Runtime.Intrinsics.Vector64<double> left, System.Runtime.Intrinsics.Vector64<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> ReciprocalStepScalar(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<byte> ReverseElementBits(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<sbyte> ReverseElementBits(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> ReverseElementBits(System.Runtime.Intrinsics.Vector64<byte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> ReverseElementBits(System.Runtime.Intrinsics.Vector64<sbyte> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> RoundAwayFromZero(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> RoundToNearest(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> RoundToNegativeInfinity(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> RoundToPositiveInfinity(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> RoundToZero(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> ShiftArithmeticRoundedSaturateScalar(System.Runtime.Intrinsics.Vector64<short> value, System.Runtime.Intrinsics.Vector64<short> count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> ShiftArithmeticRoundedSaturateScalar(System.Runtime.Intrinsics.Vector64<int> value, System.Runtime.Intrinsics.Vector64<int> count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftArithmeticRoundedSaturateScalar(System.Runtime.Intrinsics.Vector64<sbyte> value, System.Runtime.Intrinsics.Vector64<sbyte> count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> ShiftArithmeticSaturateScalar(System.Runtime.Intrinsics.Vector64<short> value, System.Runtime.Intrinsics.Vector64<short> count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> ShiftArithmeticSaturateScalar(System.Runtime.Intrinsics.Vector64<int> value, System.Runtime.Intrinsics.Vector64<int> count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftArithmeticSaturateScalar(System.Runtime.Intrinsics.Vector64<sbyte> value, System.Runtime.Intrinsics.Vector64<sbyte> count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> ShiftLeftLogicalSaturateScalar(System.Runtime.Intrinsics.Vector64<byte> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> ShiftLeftLogicalSaturateScalar(System.Runtime.Intrinsics.Vector64<short> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> ShiftLeftLogicalSaturateScalar(System.Runtime.Intrinsics.Vector64<int> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftLeftLogicalSaturateScalar(System.Runtime.Intrinsics.Vector64<sbyte> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> ShiftLeftLogicalSaturateScalar(System.Runtime.Intrinsics.Vector64<ushort> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> ShiftLeftLogicalSaturateScalar(System.Runtime.Intrinsics.Vector64<uint> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> ShiftLeftLogicalSaturateUnsignedScalar(System.Runtime.Intrinsics.Vector64<short> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> ShiftLeftLogicalSaturateUnsignedScalar(System.Runtime.Intrinsics.Vector64<int> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> ShiftLeftLogicalSaturateUnsignedScalar(System.Runtime.Intrinsics.Vector64<sbyte> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> ShiftLogicalRoundedSaturateScalar(System.Runtime.Intrinsics.Vector64<byte> value, System.Runtime.Intrinsics.Vector64<sbyte> count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> ShiftLogicalRoundedSaturateScalar(System.Runtime.Intrinsics.Vector64<short> value, System.Runtime.Intrinsics.Vector64<short> count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> ShiftLogicalRoundedSaturateScalar(System.Runtime.Intrinsics.Vector64<int> value, System.Runtime.Intrinsics.Vector64<int> count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftLogicalRoundedSaturateScalar(System.Runtime.Intrinsics.Vector64<sbyte> value, System.Runtime.Intrinsics.Vector64<sbyte> count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> ShiftLogicalRoundedSaturateScalar(System.Runtime.Intrinsics.Vector64<ushort> value, System.Runtime.Intrinsics.Vector64<short> count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> ShiftLogicalRoundedSaturateScalar(System.Runtime.Intrinsics.Vector64<uint> value, System.Runtime.Intrinsics.Vector64<int> count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> ShiftLogicalSaturateScalar(System.Runtime.Intrinsics.Vector64<byte> value, System.Runtime.Intrinsics.Vector64<sbyte> count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> ShiftLogicalSaturateScalar(System.Runtime.Intrinsics.Vector64<short> value, System.Runtime.Intrinsics.Vector64<short> count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> ShiftLogicalSaturateScalar(System.Runtime.Intrinsics.Vector64<int> value, System.Runtime.Intrinsics.Vector64<int> count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftLogicalSaturateScalar(System.Runtime.Intrinsics.Vector64<sbyte> value, System.Runtime.Intrinsics.Vector64<sbyte> count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> ShiftLogicalSaturateScalar(System.Runtime.Intrinsics.Vector64<ushort> value, System.Runtime.Intrinsics.Vector64<short> count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> ShiftLogicalSaturateScalar(System.Runtime.Intrinsics.Vector64<uint> value, System.Runtime.Intrinsics.Vector64<int> count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftRightArithmeticNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<short> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> ShiftRightArithmeticNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<int> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> ShiftRightArithmeticNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<long> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> ShiftRightArithmeticNarrowingSaturateUnsignedScalar(System.Runtime.Intrinsics.Vector64<short> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> ShiftRightArithmeticNarrowingSaturateUnsignedScalar(System.Runtime.Intrinsics.Vector64<int> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> ShiftRightArithmeticNarrowingSaturateUnsignedScalar(System.Runtime.Intrinsics.Vector64<long> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftRightArithmeticRoundedNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<short> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> ShiftRightArithmeticRoundedNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<int> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> ShiftRightArithmeticRoundedNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<long> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedScalar(System.Runtime.Intrinsics.Vector64<short> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedScalar(System.Runtime.Intrinsics.Vector64<int> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedScalar(System.Runtime.Intrinsics.Vector64<long> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftRightLogicalNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<short> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> ShiftRightLogicalNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<int> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> ShiftRightLogicalNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<long> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> ShiftRightLogicalNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<ushort> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> ShiftRightLogicalNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<uint> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> ShiftRightLogicalNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<ulong> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> ShiftRightLogicalRoundedNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<short> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> ShiftRightLogicalRoundedNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<int> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> ShiftRightLogicalRoundedNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<long> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> ShiftRightLogicalRoundedNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<ushort> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> ShiftRightLogicalRoundedNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<uint> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> ShiftRightLogicalRoundedNarrowingSaturateScalar(System.Runtime.Intrinsics.Vector64<ulong> value, byte count) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> Sqrt(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> Sqrt(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> Sqrt(System.Runtime.Intrinsics.Vector64<float> value) { throw null; }
            public unsafe static void StorePair(byte* address, System.Runtime.Intrinsics.Vector128<byte> value1, System.Runtime.Intrinsics.Vector128<byte> value2) { }
            public unsafe static void StorePair(byte* address, System.Runtime.Intrinsics.Vector64<byte> value1, System.Runtime.Intrinsics.Vector64<byte> value2) { }
            public unsafe static void StorePair(double* address, System.Runtime.Intrinsics.Vector128<double> value1, System.Runtime.Intrinsics.Vector128<double> value2) { }
            public unsafe static void StorePair(double* address, System.Runtime.Intrinsics.Vector64<double> value1, System.Runtime.Intrinsics.Vector64<double> value2) { }
            public unsafe static void StorePair(short* address, System.Runtime.Intrinsics.Vector128<short> value1, System.Runtime.Intrinsics.Vector128<short> value2) { }
            public unsafe static void StorePair(short* address, System.Runtime.Intrinsics.Vector64<short> value1, System.Runtime.Intrinsics.Vector64<short> value2) { }
            public unsafe static void StorePair(int* address, System.Runtime.Intrinsics.Vector128<int> value1, System.Runtime.Intrinsics.Vector128<int> value2) { }
            public unsafe static void StorePair(int* address, System.Runtime.Intrinsics.Vector64<int> value1, System.Runtime.Intrinsics.Vector64<int> value2) { }
            public unsafe static void StorePair(long* address, System.Runtime.Intrinsics.Vector128<long> value1, System.Runtime.Intrinsics.Vector128<long> value2) { }
            public unsafe static void StorePair(long* address, System.Runtime.Intrinsics.Vector64<long> value1, System.Runtime.Intrinsics.Vector64<long> value2) { }
            public unsafe static void StorePair(sbyte* address, System.Runtime.Intrinsics.Vector128<sbyte> value1, System.Runtime.Intrinsics.Vector128<sbyte> value2) { }
            public unsafe static void StorePair(sbyte* address, System.Runtime.Intrinsics.Vector64<sbyte> value1, System.Runtime.Intrinsics.Vector64<sbyte> value2) { }
            public unsafe static void StorePair(float* address, System.Runtime.Intrinsics.Vector128<float> value1, System.Runtime.Intrinsics.Vector128<float> value2) { }
            public unsafe static void StorePair(float* address, System.Runtime.Intrinsics.Vector64<float> value1, System.Runtime.Intrinsics.Vector64<float> value2) { }
            public unsafe static void StorePair(ushort* address, System.Runtime.Intrinsics.Vector128<ushort> value1, System.Runtime.Intrinsics.Vector128<ushort> value2) { }
            public unsafe static void StorePair(ushort* address, System.Runtime.Intrinsics.Vector64<ushort> value1, System.Runtime.Intrinsics.Vector64<ushort> value2) { }
            public unsafe static void StorePair(uint* address, System.Runtime.Intrinsics.Vector128<uint> value1, System.Runtime.Intrinsics.Vector128<uint> value2) { }
            public unsafe static void StorePair(uint* address, System.Runtime.Intrinsics.Vector64<uint> value1, System.Runtime.Intrinsics.Vector64<uint> value2) { }
            public unsafe static void StorePair(ulong* address, System.Runtime.Intrinsics.Vector128<ulong> value1, System.Runtime.Intrinsics.Vector128<ulong> value2) { }
            public unsafe static void StorePair(ulong* address, System.Runtime.Intrinsics.Vector64<ulong> value1, System.Runtime.Intrinsics.Vector64<ulong> value2) { }
            public unsafe static void StorePairNonTemporal(byte* address, System.Runtime.Intrinsics.Vector128<byte> value1, System.Runtime.Intrinsics.Vector128<byte> value2) { }
            public unsafe static void StorePairNonTemporal(byte* address, System.Runtime.Intrinsics.Vector64<byte> value1, System.Runtime.Intrinsics.Vector64<byte> value2) { }
            public unsafe static void StorePairNonTemporal(double* address, System.Runtime.Intrinsics.Vector128<double> value1, System.Runtime.Intrinsics.Vector128<double> value2) { }
            public unsafe static void StorePairNonTemporal(double* address, System.Runtime.Intrinsics.Vector64<double> value1, System.Runtime.Intrinsics.Vector64<double> value2) { }
            public unsafe static void StorePairNonTemporal(short* address, System.Runtime.Intrinsics.Vector128<short> value1, System.Runtime.Intrinsics.Vector128<short> value2) { }
            public unsafe static void StorePairNonTemporal(short* address, System.Runtime.Intrinsics.Vector64<short> value1, System.Runtime.Intrinsics.Vector64<short> value2) { }
            public unsafe static void StorePairNonTemporal(int* address, System.Runtime.Intrinsics.Vector128<int> value1, System.Runtime.Intrinsics.Vector128<int> value2) { }
            public unsafe static void StorePairNonTemporal(int* address, System.Runtime.Intrinsics.Vector64<int> value1, System.Runtime.Intrinsics.Vector64<int> value2) { }
            public unsafe static void StorePairNonTemporal(long* address, System.Runtime.Intrinsics.Vector128<long> value1, System.Runtime.Intrinsics.Vector128<long> value2) { }
            public unsafe static void StorePairNonTemporal(long* address, System.Runtime.Intrinsics.Vector64<long> value1, System.Runtime.Intrinsics.Vector64<long> value2) { }
            public unsafe static void StorePairNonTemporal(sbyte* address, System.Runtime.Intrinsics.Vector128<sbyte> value1, System.Runtime.Intrinsics.Vector128<sbyte> value2) { }
            public unsafe static void StorePairNonTemporal(sbyte* address, System.Runtime.Intrinsics.Vector64<sbyte> value1, System.Runtime.Intrinsics.Vector64<sbyte> value2) { }
            public unsafe static void StorePairNonTemporal(float* address, System.Runtime.Intrinsics.Vector128<float> value1, System.Runtime.Intrinsics.Vector128<float> value2) { }
            public unsafe static void StorePairNonTemporal(float* address, System.Runtime.Intrinsics.Vector64<float> value1, System.Runtime.Intrinsics.Vector64<float> value2) { }
            public unsafe static void StorePairNonTemporal(ushort* address, System.Runtime.Intrinsics.Vector128<ushort> value1, System.Runtime.Intrinsics.Vector128<ushort> value2) { }
            public unsafe static void StorePairNonTemporal(ushort* address, System.Runtime.Intrinsics.Vector64<ushort> value1, System.Runtime.Intrinsics.Vector64<ushort> value2) { }
            public unsafe static void StorePairNonTemporal(uint* address, System.Runtime.Intrinsics.Vector128<uint> value1, System.Runtime.Intrinsics.Vector128<uint> value2) { }
            public unsafe static void StorePairNonTemporal(uint* address, System.Runtime.Intrinsics.Vector64<uint> value1, System.Runtime.Intrinsics.Vector64<uint> value2) { }
            public unsafe static void StorePairNonTemporal(ulong* address, System.Runtime.Intrinsics.Vector128<ulong> value1, System.Runtime.Intrinsics.Vector128<ulong> value2) { }
            public unsafe static void StorePairNonTemporal(ulong* address, System.Runtime.Intrinsics.Vector64<ulong> value1, System.Runtime.Intrinsics.Vector64<ulong> value2) { }
            public unsafe static void StorePairScalar(int* address, System.Runtime.Intrinsics.Vector64<int> value1, System.Runtime.Intrinsics.Vector64<int> value2) { }
            public unsafe static void StorePairScalar(float* address, System.Runtime.Intrinsics.Vector64<float> value1, System.Runtime.Intrinsics.Vector64<float> value2) { }
            public unsafe static void StorePairScalar(uint* address, System.Runtime.Intrinsics.Vector64<uint> value1, System.Runtime.Intrinsics.Vector64<uint> value2) { }
            public unsafe static void StorePairScalarNonTemporal(int* address, System.Runtime.Intrinsics.Vector64<int> value1, System.Runtime.Intrinsics.Vector64<int> value2) { }
            public unsafe static void StorePairScalarNonTemporal(float* address, System.Runtime.Intrinsics.Vector64<float> value1, System.Runtime.Intrinsics.Vector64<float> value2) { }
            public unsafe static void StorePairScalarNonTemporal(uint* address, System.Runtime.Intrinsics.Vector64<uint> value1, System.Runtime.Intrinsics.Vector64<uint> value2) { }
            public static System.Runtime.Intrinsics.Vector128<double> Subtract(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> SubtractSaturateScalar(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> SubtractSaturateScalar(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> SubtractSaturateScalar(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> SubtractSaturateScalar(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> SubtractSaturateScalar(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> SubtractSaturateScalar(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<byte> TransposeEven(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> TransposeEven(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<short> TransposeEven(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<int> TransposeEven(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> TransposeEven(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<sbyte> TransposeEven(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> TransposeEven(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ushort> TransposeEven(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<uint> TransposeEven(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> TransposeEven(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> TransposeEven(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> TransposeEven(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> TransposeEven(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> TransposeEven(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> TransposeEven(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> TransposeEven(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> TransposeEven(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<byte> TransposeOdd(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> TransposeOdd(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<short> TransposeOdd(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<int> TransposeOdd(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> TransposeOdd(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<sbyte> TransposeOdd(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> TransposeOdd(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ushort> TransposeOdd(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<uint> TransposeOdd(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> TransposeOdd(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> TransposeOdd(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> TransposeOdd(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> TransposeOdd(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> TransposeOdd(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> TransposeOdd(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> TransposeOdd(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> TransposeOdd(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<byte> UnzipEven(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> UnzipEven(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<short> UnzipEven(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<int> UnzipEven(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> UnzipEven(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<sbyte> UnzipEven(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> UnzipEven(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ushort> UnzipEven(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<uint> UnzipEven(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> UnzipEven(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> UnzipEven(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> UnzipEven(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> UnzipEven(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> UnzipEven(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> UnzipEven(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> UnzipEven(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> UnzipEven(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<byte> UnzipOdd(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> UnzipOdd(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<short> UnzipOdd(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<int> UnzipOdd(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> UnzipOdd(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<sbyte> UnzipOdd(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> UnzipOdd(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ushort> UnzipOdd(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<uint> UnzipOdd(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> UnzipOdd(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> UnzipOdd(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> UnzipOdd(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> UnzipOdd(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> UnzipOdd(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> UnzipOdd(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> UnzipOdd(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> UnzipOdd(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<byte> VectorTableLookup(System.Runtime.Intrinsics.Vector128<byte> table, System.Runtime.Intrinsics.Vector128<byte> byteIndexes) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<sbyte> VectorTableLookup(System.Runtime.Intrinsics.Vector128<sbyte> table, System.Runtime.Intrinsics.Vector128<sbyte> byteIndexes) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<byte> VectorTableLookupExtension(System.Runtime.Intrinsics.Vector128<byte> defaultValues, System.Runtime.Intrinsics.Vector128<byte> table, System.Runtime.Intrinsics.Vector128<byte> byteIndexes) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<sbyte> VectorTableLookupExtension(System.Runtime.Intrinsics.Vector128<sbyte> defaultValues, System.Runtime.Intrinsics.Vector128<sbyte> table, System.Runtime.Intrinsics.Vector128<sbyte> byteIndexes) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<byte> ZipHigh(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> ZipHigh(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<short> ZipHigh(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<int> ZipHigh(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> ZipHigh(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<sbyte> ZipHigh(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> ZipHigh(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ushort> ZipHigh(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<uint> ZipHigh(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> ZipHigh(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> ZipHigh(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> ZipHigh(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> ZipHigh(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> ZipHigh(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> ZipHigh(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> ZipHigh(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> ZipHigh(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<byte> ZipLow(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<double> ZipLow(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<short> ZipLow(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<int> ZipLow(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> ZipLow(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<sbyte> ZipLow(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<float> ZipLow(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ushort> ZipLow(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<uint> ZipLow(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> ZipLow(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<byte> ZipLow(System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> ZipLow(System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> ZipLow(System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<sbyte> ZipLow(System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<float> ZipLow(System.Runtime.Intrinsics.Vector64<float> left, System.Runtime.Intrinsics.Vector64<float> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<ushort> ZipLow(System.Runtime.Intrinsics.Vector64<ushort> left, System.Runtime.Intrinsics.Vector64<ushort> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<uint> ZipLow(System.Runtime.Intrinsics.Vector64<uint> left, System.Runtime.Intrinsics.Vector64<uint> right) { throw null; }
        }
    }
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Aes : System.Runtime.Intrinsics.Arm.ArmBase
    {
        internal Aes() { }
        public static new bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<byte> Decrypt(System.Runtime.Intrinsics.Vector128<byte> value, System.Runtime.Intrinsics.Vector128<byte> roundKey) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Encrypt(System.Runtime.Intrinsics.Vector128<byte> value, System.Runtime.Intrinsics.Vector128<byte> roundKey) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> InverseMixColumns(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> MixColumns(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> PolynomialMultiplyWideningLower(System.Runtime.Intrinsics.Vector64<long> left, System.Runtime.Intrinsics.Vector64<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> PolynomialMultiplyWideningLower(System.Runtime.Intrinsics.Vector64<ulong> left, System.Runtime.Intrinsics.Vector64<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> PolynomialMultiplyWideningUpper(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> PolynomialMultiplyWideningUpper(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public new abstract partial class Arm64 : System.Runtime.Intrinsics.Arm.ArmBase.Arm64
        {
            internal Arm64() { }
            public static new bool IsSupported { get { throw null; } }
        }
    }
    [System.CLSCompliantAttribute(false)]
    public abstract partial class ArmBase
    {
        internal ArmBase() { }
        public static bool IsSupported { get { throw null; } }
        public static int LeadingZeroCount(int value) { throw null; }
        public static int LeadingZeroCount(uint value) { throw null; }
        public static int ReverseElementBits(int value) { throw null; }
        public static uint ReverseElementBits(uint value) { throw null; }
        public static void Yield() { throw null; }
        public abstract partial class Arm64
        {
            internal Arm64() { }
            public static bool IsSupported { get { throw null; } }
            public static int LeadingSignCount(int value) { throw null; }
            public static int LeadingSignCount(long value) { throw null; }
            public static int LeadingZeroCount(long value) { throw null; }
            public static int LeadingZeroCount(ulong value) { throw null; }
            public static long MultiplyHigh(long left, long right) { throw null; }
            public static ulong MultiplyHigh(ulong left, ulong right) { throw null; }
            public static long ReverseElementBits(long value) { throw null; }
            public static ulong ReverseElementBits(ulong value) { throw null; }
        }
    }
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Crc32 : System.Runtime.Intrinsics.Arm.ArmBase
    {
        internal Crc32() { }
        public static new bool IsSupported { get { throw null; } }
        public static uint ComputeCrc32(uint crc, byte data) { throw null; }
        public static uint ComputeCrc32(uint crc, ushort data) { throw null; }
        public static uint ComputeCrc32(uint crc, uint data) { throw null; }
        public static uint ComputeCrc32C(uint crc, byte data) { throw null; }
        public static uint ComputeCrc32C(uint crc, ushort data) { throw null; }
        public static uint ComputeCrc32C(uint crc, uint data) { throw null; }
        public new abstract partial class Arm64 : System.Runtime.Intrinsics.Arm.ArmBase.Arm64
        {
            internal Arm64() { }
            public static new bool IsSupported { get { throw null; } }
            public static uint ComputeCrc32(uint crc, ulong data) { throw null; }
            public static uint ComputeCrc32C(uint crc, ulong data) { throw null; }
        }
    }
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Dp : System.Runtime.Intrinsics.Arm.AdvSimd
    {
        internal Dp() { }
        public static new bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<int> DotProduct(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> DotProduct(System.Runtime.Intrinsics.Vector128<uint> addend, System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> DotProduct(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> DotProduct(System.Runtime.Intrinsics.Vector64<uint> addend, System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> DotProductBySelectedQuadruplet(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right, byte rightScaledIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> DotProductBySelectedQuadruplet(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right, byte rightScaledIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> DotProductBySelectedQuadruplet(System.Runtime.Intrinsics.Vector128<uint> addend, System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right, byte rightScaledIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> DotProductBySelectedQuadruplet(System.Runtime.Intrinsics.Vector128<uint> addend, System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector64<byte> right, byte rightScaledIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> DotProductBySelectedQuadruplet(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right, byte rightScaledIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> DotProductBySelectedQuadruplet(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<sbyte> left, System.Runtime.Intrinsics.Vector64<sbyte> right, byte rightScaledIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> DotProductBySelectedQuadruplet(System.Runtime.Intrinsics.Vector64<uint> addend, System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector128<byte> right, byte rightScaledIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<uint> DotProductBySelectedQuadruplet(System.Runtime.Intrinsics.Vector64<uint> addend, System.Runtime.Intrinsics.Vector64<byte> left, System.Runtime.Intrinsics.Vector64<byte> right, byte rightScaledIndex) { throw null; }
        public new abstract partial class Arm64 : System.Runtime.Intrinsics.Arm.AdvSimd.Arm64
        {
            internal Arm64() { }
            public static new bool IsSupported { get { throw null; } }
        }
    }
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Rdm : System.Runtime.Intrinsics.Arm.AdvSimd
    {
        internal Rdm() { }
        public static new bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyRoundedDoublingAndAddSaturateHigh(System.Runtime.Intrinsics.Vector128<short> addend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyRoundedDoublingAndAddSaturateHigh(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplyRoundedDoublingAndAddSaturateHigh(System.Runtime.Intrinsics.Vector64<short> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplyRoundedDoublingAndAddSaturateHigh(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyRoundedDoublingAndSubtractSaturateHigh(System.Runtime.Intrinsics.Vector128<short> minuend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyRoundedDoublingAndSubtractSaturateHigh(System.Runtime.Intrinsics.Vector128<int> minuend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplyRoundedDoublingAndSubtractSaturateHigh(System.Runtime.Intrinsics.Vector64<short> minuend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplyRoundedDoublingAndSubtractSaturateHigh(System.Runtime.Intrinsics.Vector64<int> minuend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh(System.Runtime.Intrinsics.Vector128<short> addend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh(System.Runtime.Intrinsics.Vector128<short> addend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh(System.Runtime.Intrinsics.Vector64<short> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh(System.Runtime.Intrinsics.Vector64<short> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh(System.Runtime.Intrinsics.Vector128<short> minuend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh(System.Runtime.Intrinsics.Vector128<short> minuend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh(System.Runtime.Intrinsics.Vector128<int> minuend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh(System.Runtime.Intrinsics.Vector128<int> minuend, System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh(System.Runtime.Intrinsics.Vector64<short> minuend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<short> MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh(System.Runtime.Intrinsics.Vector64<short> minuend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh(System.Runtime.Intrinsics.Vector64<int> minuend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
        public static System.Runtime.Intrinsics.Vector64<int> MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh(System.Runtime.Intrinsics.Vector64<int> minuend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        public new abstract partial class Arm64 : System.Runtime.Intrinsics.Arm.AdvSimd.Arm64
        {
            internal Arm64() { }
            public static new bool IsSupported { get { throw null; } }
            public static System.Runtime.Intrinsics.Vector64<short> MultiplyRoundedDoublingAndAddSaturateHighScalar(System.Runtime.Intrinsics.Vector64<short> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MultiplyRoundedDoublingAndAddSaturateHighScalar(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> MultiplyRoundedDoublingAndSubtractSaturateHighScalar(System.Runtime.Intrinsics.Vector64<short> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MultiplyRoundedDoublingAndSubtractSaturateHighScalar(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh(System.Runtime.Intrinsics.Vector64<short> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh(System.Runtime.Intrinsics.Vector64<short> addend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh(System.Runtime.Intrinsics.Vector64<int> addend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh(System.Runtime.Intrinsics.Vector64<short> minuend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<short> MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh(System.Runtime.Intrinsics.Vector64<short> minuend, System.Runtime.Intrinsics.Vector64<short> left, System.Runtime.Intrinsics.Vector64<short> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh(System.Runtime.Intrinsics.Vector64<int> minuend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte rightIndex) { throw null; }
            public static System.Runtime.Intrinsics.Vector64<int> MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh(System.Runtime.Intrinsics.Vector64<int> minuend, System.Runtime.Intrinsics.Vector64<int> left, System.Runtime.Intrinsics.Vector64<int> right, byte rightIndex) { throw null; }
        }
    }
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Sha1 : System.Runtime.Intrinsics.Arm.ArmBase
    {
        internal Sha1() { }
        public static new bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector64<uint> FixedRotate(System.Runtime.Intrinsics.Vector64<uint> hash_e) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> HashUpdateChoose(System.Runtime.Intrinsics.Vector128<uint> hash_abcd, System.Runtime.Intrinsics.Vector64<uint> hash_e, System.Runtime.Intrinsics.Vector128<uint> wk) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> HashUpdateMajority(System.Runtime.Intrinsics.Vector128<uint> hash_abcd, System.Runtime.Intrinsics.Vector64<uint> hash_e, System.Runtime.Intrinsics.Vector128<uint> wk) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> HashUpdateParity(System.Runtime.Intrinsics.Vector128<uint> hash_abcd, System.Runtime.Intrinsics.Vector64<uint> hash_e, System.Runtime.Intrinsics.Vector128<uint> wk) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ScheduleUpdate0(System.Runtime.Intrinsics.Vector128<uint> w0_3, System.Runtime.Intrinsics.Vector128<uint> w4_7, System.Runtime.Intrinsics.Vector128<uint> w8_11) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ScheduleUpdate1(System.Runtime.Intrinsics.Vector128<uint> tw0_3, System.Runtime.Intrinsics.Vector128<uint> w12_15) { throw null; }
        public new abstract partial class Arm64 : System.Runtime.Intrinsics.Arm.ArmBase.Arm64
        {
            internal Arm64() { }
            public static new bool IsSupported { get { throw null; } }
        }
    }
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Sha256 : System.Runtime.Intrinsics.Arm.ArmBase
    {
        internal Sha256() { }
        public static new bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<uint> HashUpdate1(System.Runtime.Intrinsics.Vector128<uint> hash_abcd, System.Runtime.Intrinsics.Vector128<uint> hash_efgh, System.Runtime.Intrinsics.Vector128<uint> wk) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> HashUpdate2(System.Runtime.Intrinsics.Vector128<uint> hash_efgh, System.Runtime.Intrinsics.Vector128<uint> hash_abcd, System.Runtime.Intrinsics.Vector128<uint> wk) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ScheduleUpdate0(System.Runtime.Intrinsics.Vector128<uint> w0_3, System.Runtime.Intrinsics.Vector128<uint> w4_7) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ScheduleUpdate1(System.Runtime.Intrinsics.Vector128<uint> w0_3, System.Runtime.Intrinsics.Vector128<uint> w8_11, System.Runtime.Intrinsics.Vector128<uint> w12_15) { throw null; }
        public new abstract partial class Arm64 : System.Runtime.Intrinsics.Arm.ArmBase.Arm64
        {
            internal Arm64() { }
            public static new bool IsSupported { get { throw null; } }
        }
    }
}
namespace System.Runtime.Intrinsics.X86
{
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Aes : System.Runtime.Intrinsics.X86.Sse2
    {
        internal Aes() { }
        public static new bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<byte> Decrypt(System.Runtime.Intrinsics.Vector128<byte> value, System.Runtime.Intrinsics.Vector128<byte> roundKey) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> DecryptLast(System.Runtime.Intrinsics.Vector128<byte> value, System.Runtime.Intrinsics.Vector128<byte> roundKey) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Encrypt(System.Runtime.Intrinsics.Vector128<byte> value, System.Runtime.Intrinsics.Vector128<byte> roundKey) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> EncryptLast(System.Runtime.Intrinsics.Vector128<byte> value, System.Runtime.Intrinsics.Vector128<byte> roundKey) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> InverseMixColumns(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> KeygenAssist(System.Runtime.Intrinsics.Vector128<byte> value, byte control) { throw null; }
        public new abstract partial class X64 : System.Runtime.Intrinsics.X86.Sse2.X64
        {
            internal X64() { }
            public static new bool IsSupported { get { throw null; } }
        }
    }
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Avx : System.Runtime.Intrinsics.X86.Sse42
    {
        internal Avx() { }
        public static new bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector256<double> Add(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> Add(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> AddSubtract(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> AddSubtract(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> And(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> And(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> AndNot(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> AndNot(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> Blend(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> Blend(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> BlendVariable(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right, System.Runtime.Intrinsics.Vector256<double> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> BlendVariable(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right, System.Runtime.Intrinsics.Vector256<float> mask) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<float> BroadcastScalarToVector128(float* source) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<double> BroadcastScalarToVector256(double* source) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<float> BroadcastScalarToVector256(float* source) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<double> BroadcastVector128ToVector256(double* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<float> BroadcastVector128ToVector256(float* address) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> Ceiling(System.Runtime.Intrinsics.Vector256<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> Ceiling(System.Runtime.Intrinsics.Vector256<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Compare(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right, System.Runtime.Intrinsics.X86.FloatComparisonMode mode) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Compare(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right, System.Runtime.Intrinsics.X86.FloatComparisonMode mode) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> Compare(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right, System.Runtime.Intrinsics.X86.FloatComparisonMode mode) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> Compare(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right, System.Runtime.Intrinsics.X86.FloatComparisonMode mode) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> CompareEqual(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> CompareEqual(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> CompareGreaterThan(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> CompareGreaterThan(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> CompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> CompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> CompareLessThan(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> CompareLessThan(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> CompareLessThanOrEqual(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> CompareLessThanOrEqual(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> CompareNotEqual(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> CompareNotEqual(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> CompareNotGreaterThan(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> CompareNotGreaterThan(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> CompareNotGreaterThanOrEqual(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> CompareNotGreaterThanOrEqual(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> CompareNotLessThan(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> CompareNotLessThan(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> CompareNotLessThanOrEqual(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> CompareNotLessThanOrEqual(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> CompareOrdered(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> CompareOrdered(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareScalar(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right, System.Runtime.Intrinsics.X86.FloatComparisonMode mode) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareScalar(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right, System.Runtime.Intrinsics.X86.FloatComparisonMode mode) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> CompareUnordered(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> CompareUnordered(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ConvertToVector128Int32(System.Runtime.Intrinsics.Vector256<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ConvertToVector128Int32WithTruncation(System.Runtime.Intrinsics.Vector256<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> ConvertToVector128Single(System.Runtime.Intrinsics.Vector256<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> ConvertToVector256Double(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> ConvertToVector256Double(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> ConvertToVector256Int32(System.Runtime.Intrinsics.Vector256<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> ConvertToVector256Int32WithTruncation(System.Runtime.Intrinsics.Vector256<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> ConvertToVector256Single(System.Runtime.Intrinsics.Vector256<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> Divide(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> Divide(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> DotProduct(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> DuplicateEvenIndexed(System.Runtime.Intrinsics.Vector256<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> DuplicateEvenIndexed(System.Runtime.Intrinsics.Vector256<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> DuplicateOddIndexed(System.Runtime.Intrinsics.Vector256<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ExtractVector128(System.Runtime.Intrinsics.Vector256<byte> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> ExtractVector128(System.Runtime.Intrinsics.Vector256<double> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ExtractVector128(System.Runtime.Intrinsics.Vector256<short> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ExtractVector128(System.Runtime.Intrinsics.Vector256<int> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ExtractVector128(System.Runtime.Intrinsics.Vector256<long> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ExtractVector128(System.Runtime.Intrinsics.Vector256<sbyte> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> ExtractVector128(System.Runtime.Intrinsics.Vector256<float> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ExtractVector128(System.Runtime.Intrinsics.Vector256<ushort> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ExtractVector128(System.Runtime.Intrinsics.Vector256<uint> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ExtractVector128(System.Runtime.Intrinsics.Vector256<ulong> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> Floor(System.Runtime.Intrinsics.Vector256<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> Floor(System.Runtime.Intrinsics.Vector256<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> HorizontalAdd(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> HorizontalAdd(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> HorizontalSubtract(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> HorizontalSubtract(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> InsertVector128(System.Runtime.Intrinsics.Vector256<byte> value, System.Runtime.Intrinsics.Vector128<byte> data, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> InsertVector128(System.Runtime.Intrinsics.Vector256<double> value, System.Runtime.Intrinsics.Vector128<double> data, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> InsertVector128(System.Runtime.Intrinsics.Vector256<short> value, System.Runtime.Intrinsics.Vector128<short> data, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> InsertVector128(System.Runtime.Intrinsics.Vector256<int> value, System.Runtime.Intrinsics.Vector128<int> data, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> InsertVector128(System.Runtime.Intrinsics.Vector256<long> value, System.Runtime.Intrinsics.Vector128<long> data, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> InsertVector128(System.Runtime.Intrinsics.Vector256<sbyte> value, System.Runtime.Intrinsics.Vector128<sbyte> data, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> InsertVector128(System.Runtime.Intrinsics.Vector256<float> value, System.Runtime.Intrinsics.Vector128<float> data, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> InsertVector128(System.Runtime.Intrinsics.Vector256<ushort> value, System.Runtime.Intrinsics.Vector128<ushort> data, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> InsertVector128(System.Runtime.Intrinsics.Vector256<uint> value, System.Runtime.Intrinsics.Vector128<uint> data, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> InsertVector128(System.Runtime.Intrinsics.Vector256<ulong> value, System.Runtime.Intrinsics.Vector128<ulong> data, byte index) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<byte> LoadAlignedVector256(byte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<double> LoadAlignedVector256(double* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<short> LoadAlignedVector256(short* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<int> LoadAlignedVector256(int* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<long> LoadAlignedVector256(long* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<sbyte> LoadAlignedVector256(sbyte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<float> LoadAlignedVector256(float* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<ushort> LoadAlignedVector256(ushort* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<uint> LoadAlignedVector256(uint* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<ulong> LoadAlignedVector256(ulong* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<byte> LoadDquVector256(byte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<short> LoadDquVector256(short* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<int> LoadDquVector256(int* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<long> LoadDquVector256(long* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<sbyte> LoadDquVector256(sbyte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<ushort> LoadDquVector256(ushort* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<uint> LoadDquVector256(uint* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<ulong> LoadDquVector256(ulong* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<byte> LoadVector256(byte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<double> LoadVector256(double* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<short> LoadVector256(short* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<int> LoadVector256(int* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<long> LoadVector256(long* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<sbyte> LoadVector256(sbyte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<float> LoadVector256(float* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<ushort> LoadVector256(ushort* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<uint> LoadVector256(uint* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<ulong> LoadVector256(ulong* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<double> MaskLoad(double* address, System.Runtime.Intrinsics.Vector128<double> mask) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<double> MaskLoad(double* address, System.Runtime.Intrinsics.Vector256<double> mask) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<float> MaskLoad(float* address, System.Runtime.Intrinsics.Vector128<float> mask) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<float> MaskLoad(float* address, System.Runtime.Intrinsics.Vector256<float> mask) { throw null; }
        public unsafe static void MaskStore(double* address, System.Runtime.Intrinsics.Vector128<double> mask, System.Runtime.Intrinsics.Vector128<double> source) { }
        public unsafe static void MaskStore(double* address, System.Runtime.Intrinsics.Vector256<double> mask, System.Runtime.Intrinsics.Vector256<double> source) { }
        public unsafe static void MaskStore(float* address, System.Runtime.Intrinsics.Vector128<float> mask, System.Runtime.Intrinsics.Vector128<float> source) { }
        public unsafe static void MaskStore(float* address, System.Runtime.Intrinsics.Vector256<float> mask, System.Runtime.Intrinsics.Vector256<float> source) { }
        public static System.Runtime.Intrinsics.Vector256<double> Max(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> Max(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> Min(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> Min(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static int MoveMask(System.Runtime.Intrinsics.Vector256<double> value) { throw null; }
        public static int MoveMask(System.Runtime.Intrinsics.Vector256<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> Multiply(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> Multiply(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> Or(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> Or(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Permute(System.Runtime.Intrinsics.Vector128<double> value, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Permute(System.Runtime.Intrinsics.Vector128<float> value, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> Permute(System.Runtime.Intrinsics.Vector256<double> value, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> Permute(System.Runtime.Intrinsics.Vector256<float> value, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> Permute2x128(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> Permute2x128(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> Permute2x128(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> Permute2x128(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> Permute2x128(System.Runtime.Intrinsics.Vector256<long> left, System.Runtime.Intrinsics.Vector256<long> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> Permute2x128(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> Permute2x128(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> Permute2x128(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> Permute2x128(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> Permute2x128(System.Runtime.Intrinsics.Vector256<ulong> left, System.Runtime.Intrinsics.Vector256<ulong> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> PermuteVar(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<long> control) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> PermuteVar(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<int> control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> PermuteVar(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<long> control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> PermuteVar(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<int> control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> Reciprocal(System.Runtime.Intrinsics.Vector256<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> ReciprocalSqrt(System.Runtime.Intrinsics.Vector256<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> RoundCurrentDirection(System.Runtime.Intrinsics.Vector256<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> RoundCurrentDirection(System.Runtime.Intrinsics.Vector256<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> RoundToNearestInteger(System.Runtime.Intrinsics.Vector256<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> RoundToNearestInteger(System.Runtime.Intrinsics.Vector256<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> RoundToNegativeInfinity(System.Runtime.Intrinsics.Vector256<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> RoundToNegativeInfinity(System.Runtime.Intrinsics.Vector256<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> RoundToPositiveInfinity(System.Runtime.Intrinsics.Vector256<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> RoundToPositiveInfinity(System.Runtime.Intrinsics.Vector256<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> RoundToZero(System.Runtime.Intrinsics.Vector256<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> RoundToZero(System.Runtime.Intrinsics.Vector256<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> Shuffle(System.Runtime.Intrinsics.Vector256<double> value, System.Runtime.Intrinsics.Vector256<double> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> Shuffle(System.Runtime.Intrinsics.Vector256<float> value, System.Runtime.Intrinsics.Vector256<float> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> Sqrt(System.Runtime.Intrinsics.Vector256<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> Sqrt(System.Runtime.Intrinsics.Vector256<float> value) { throw null; }
        public unsafe static void Store(byte* address, System.Runtime.Intrinsics.Vector256<byte> source) { }
        public unsafe static void Store(double* address, System.Runtime.Intrinsics.Vector256<double> source) { }
        public unsafe static void Store(short* address, System.Runtime.Intrinsics.Vector256<short> source) { }
        public unsafe static void Store(int* address, System.Runtime.Intrinsics.Vector256<int> source) { }
        public unsafe static void Store(long* address, System.Runtime.Intrinsics.Vector256<long> source) { }
        public unsafe static void Store(sbyte* address, System.Runtime.Intrinsics.Vector256<sbyte> source) { }
        public unsafe static void Store(float* address, System.Runtime.Intrinsics.Vector256<float> source) { }
        public unsafe static void Store(ushort* address, System.Runtime.Intrinsics.Vector256<ushort> source) { }
        public unsafe static void Store(uint* address, System.Runtime.Intrinsics.Vector256<uint> source) { }
        public unsafe static void Store(ulong* address, System.Runtime.Intrinsics.Vector256<ulong> source) { }
        public unsafe static void StoreAligned(byte* address, System.Runtime.Intrinsics.Vector256<byte> source) { }
        public unsafe static void StoreAligned(double* address, System.Runtime.Intrinsics.Vector256<double> source) { }
        public unsafe static void StoreAligned(short* address, System.Runtime.Intrinsics.Vector256<short> source) { }
        public unsafe static void StoreAligned(int* address, System.Runtime.Intrinsics.Vector256<int> source) { }
        public unsafe static void StoreAligned(long* address, System.Runtime.Intrinsics.Vector256<long> source) { }
        public unsafe static void StoreAligned(sbyte* address, System.Runtime.Intrinsics.Vector256<sbyte> source) { }
        public unsafe static void StoreAligned(float* address, System.Runtime.Intrinsics.Vector256<float> source) { }
        public unsafe static void StoreAligned(ushort* address, System.Runtime.Intrinsics.Vector256<ushort> source) { }
        public unsafe static void StoreAligned(uint* address, System.Runtime.Intrinsics.Vector256<uint> source) { }
        public unsafe static void StoreAligned(ulong* address, System.Runtime.Intrinsics.Vector256<ulong> source) { }
        public unsafe static void StoreAlignedNonTemporal(byte* address, System.Runtime.Intrinsics.Vector256<byte> source) { }
        public unsafe static void StoreAlignedNonTemporal(double* address, System.Runtime.Intrinsics.Vector256<double> source) { }
        public unsafe static void StoreAlignedNonTemporal(short* address, System.Runtime.Intrinsics.Vector256<short> source) { }
        public unsafe static void StoreAlignedNonTemporal(int* address, System.Runtime.Intrinsics.Vector256<int> source) { }
        public unsafe static void StoreAlignedNonTemporal(long* address, System.Runtime.Intrinsics.Vector256<long> source) { }
        public unsafe static void StoreAlignedNonTemporal(sbyte* address, System.Runtime.Intrinsics.Vector256<sbyte> source) { }
        public unsafe static void StoreAlignedNonTemporal(float* address, System.Runtime.Intrinsics.Vector256<float> source) { }
        public unsafe static void StoreAlignedNonTemporal(ushort* address, System.Runtime.Intrinsics.Vector256<ushort> source) { }
        public unsafe static void StoreAlignedNonTemporal(uint* address, System.Runtime.Intrinsics.Vector256<uint> source) { }
        public unsafe static void StoreAlignedNonTemporal(ulong* address, System.Runtime.Intrinsics.Vector256<ulong> source) { }
        public static System.Runtime.Intrinsics.Vector256<double> Subtract(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> Subtract(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static bool TestC(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static bool TestC(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static bool TestC(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right) { throw null; }
        public static bool TestC(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static bool TestC(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static bool TestC(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static bool TestC(System.Runtime.Intrinsics.Vector256<long> left, System.Runtime.Intrinsics.Vector256<long> right) { throw null; }
        public static bool TestC(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static bool TestC(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static bool TestC(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right) { throw null; }
        public static bool TestC(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> right) { throw null; }
        public static bool TestC(System.Runtime.Intrinsics.Vector256<ulong> left, System.Runtime.Intrinsics.Vector256<ulong> right) { throw null; }
        public static bool TestNotZAndNotC(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static bool TestNotZAndNotC(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static bool TestNotZAndNotC(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right) { throw null; }
        public static bool TestNotZAndNotC(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static bool TestNotZAndNotC(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static bool TestNotZAndNotC(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static bool TestNotZAndNotC(System.Runtime.Intrinsics.Vector256<long> left, System.Runtime.Intrinsics.Vector256<long> right) { throw null; }
        public static bool TestNotZAndNotC(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static bool TestNotZAndNotC(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static bool TestNotZAndNotC(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right) { throw null; }
        public static bool TestNotZAndNotC(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> right) { throw null; }
        public static bool TestNotZAndNotC(System.Runtime.Intrinsics.Vector256<ulong> left, System.Runtime.Intrinsics.Vector256<ulong> right) { throw null; }
        public static bool TestZ(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static bool TestZ(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static bool TestZ(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right) { throw null; }
        public static bool TestZ(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static bool TestZ(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static bool TestZ(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static bool TestZ(System.Runtime.Intrinsics.Vector256<long> left, System.Runtime.Intrinsics.Vector256<long> right) { throw null; }
        public static bool TestZ(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static bool TestZ(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static bool TestZ(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right) { throw null; }
        public static bool TestZ(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> right) { throw null; }
        public static bool TestZ(System.Runtime.Intrinsics.Vector256<ulong> left, System.Runtime.Intrinsics.Vector256<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> UnpackHigh(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> UnpackHigh(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> UnpackLow(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> UnpackLow(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> Xor(System.Runtime.Intrinsics.Vector256<double> left, System.Runtime.Intrinsics.Vector256<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> Xor(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<float> right) { throw null; }
        public new abstract partial class X64 : System.Runtime.Intrinsics.X86.Sse42.X64
        {
            internal X64() { }
            public static new bool IsSupported { get { throw null; } }
        }
    }
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Avx2 : System.Runtime.Intrinsics.X86.Avx
    {
        internal Avx2() { }
        public static new bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector256<ushort> Abs(System.Runtime.Intrinsics.Vector256<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> Abs(System.Runtime.Intrinsics.Vector256<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> Abs(System.Runtime.Intrinsics.Vector256<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> Add(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> Add(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> Add(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> Add(System.Runtime.Intrinsics.Vector256<long> left, System.Runtime.Intrinsics.Vector256<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> Add(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> Add(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> Add(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> Add(System.Runtime.Intrinsics.Vector256<ulong> left, System.Runtime.Intrinsics.Vector256<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> AddSaturate(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> AddSaturate(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> AddSaturate(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> AddSaturate(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> AlignRight(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right, byte mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> AlignRight(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right, byte mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> AlignRight(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right, byte mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> AlignRight(System.Runtime.Intrinsics.Vector256<long> left, System.Runtime.Intrinsics.Vector256<long> right, byte mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> AlignRight(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right, byte mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> AlignRight(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right, byte mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> AlignRight(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> right, byte mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> AlignRight(System.Runtime.Intrinsics.Vector256<ulong> left, System.Runtime.Intrinsics.Vector256<ulong> right, byte mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> And(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> And(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> And(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> And(System.Runtime.Intrinsics.Vector256<long> left, System.Runtime.Intrinsics.Vector256<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> And(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> And(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> And(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> And(System.Runtime.Intrinsics.Vector256<ulong> left, System.Runtime.Intrinsics.Vector256<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> AndNot(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> AndNot(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> AndNot(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> AndNot(System.Runtime.Intrinsics.Vector256<long> left, System.Runtime.Intrinsics.Vector256<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> AndNot(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> AndNot(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> AndNot(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> AndNot(System.Runtime.Intrinsics.Vector256<ulong> left, System.Runtime.Intrinsics.Vector256<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> Average(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> Average(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Blend(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> Blend(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> Blend(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> Blend(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> Blend(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> Blend(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> BlendVariable(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right, System.Runtime.Intrinsics.Vector256<byte> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> BlendVariable(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right, System.Runtime.Intrinsics.Vector256<short> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> BlendVariable(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right, System.Runtime.Intrinsics.Vector256<int> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> BlendVariable(System.Runtime.Intrinsics.Vector256<long> left, System.Runtime.Intrinsics.Vector256<long> right, System.Runtime.Intrinsics.Vector256<long> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> BlendVariable(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right, System.Runtime.Intrinsics.Vector256<sbyte> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> BlendVariable(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right, System.Runtime.Intrinsics.Vector256<ushort> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> BlendVariable(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> right, System.Runtime.Intrinsics.Vector256<uint> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> BlendVariable(System.Runtime.Intrinsics.Vector256<ulong> left, System.Runtime.Intrinsics.Vector256<ulong> right, System.Runtime.Intrinsics.Vector256<ulong> mask) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<byte> BroadcastScalarToVector128(byte* source) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<short> BroadcastScalarToVector128(short* source) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<int> BroadcastScalarToVector128(int* source) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<long> BroadcastScalarToVector128(long* source) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> BroadcastScalarToVector128(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> BroadcastScalarToVector128(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> BroadcastScalarToVector128(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> BroadcastScalarToVector128(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> BroadcastScalarToVector128(System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> BroadcastScalarToVector128(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> BroadcastScalarToVector128(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> BroadcastScalarToVector128(System.Runtime.Intrinsics.Vector128<ushort> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> BroadcastScalarToVector128(System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> BroadcastScalarToVector128(System.Runtime.Intrinsics.Vector128<ulong> value) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<sbyte> BroadcastScalarToVector128(sbyte* source) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<ushort> BroadcastScalarToVector128(ushort* source) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<uint> BroadcastScalarToVector128(uint* source) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<ulong> BroadcastScalarToVector128(ulong* source) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<byte> BroadcastScalarToVector256(byte* source) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<short> BroadcastScalarToVector256(short* source) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<int> BroadcastScalarToVector256(int* source) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<long> BroadcastScalarToVector256(long* source) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> BroadcastScalarToVector256(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> BroadcastScalarToVector256(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> BroadcastScalarToVector256(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> BroadcastScalarToVector256(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> BroadcastScalarToVector256(System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> BroadcastScalarToVector256(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> BroadcastScalarToVector256(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> BroadcastScalarToVector256(System.Runtime.Intrinsics.Vector128<ushort> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> BroadcastScalarToVector256(System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> BroadcastScalarToVector256(System.Runtime.Intrinsics.Vector128<ulong> value) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<sbyte> BroadcastScalarToVector256(sbyte* source) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<ushort> BroadcastScalarToVector256(ushort* source) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<uint> BroadcastScalarToVector256(uint* source) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<ulong> BroadcastScalarToVector256(ulong* source) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<byte> BroadcastVector128ToVector256(byte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<short> BroadcastVector128ToVector256(short* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<int> BroadcastVector128ToVector256(int* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<long> BroadcastVector128ToVector256(long* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<sbyte> BroadcastVector128ToVector256(sbyte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<ushort> BroadcastVector128ToVector256(ushort* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<uint> BroadcastVector128ToVector256(uint* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<ulong> BroadcastVector128ToVector256(ulong* address) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> CompareEqual(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> CompareEqual(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> CompareEqual(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> CompareEqual(System.Runtime.Intrinsics.Vector256<long> left, System.Runtime.Intrinsics.Vector256<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> CompareEqual(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> CompareEqual(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> CompareEqual(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> CompareEqual(System.Runtime.Intrinsics.Vector256<ulong> left, System.Runtime.Intrinsics.Vector256<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> CompareGreaterThan(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> CompareGreaterThan(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> CompareGreaterThan(System.Runtime.Intrinsics.Vector256<long> left, System.Runtime.Intrinsics.Vector256<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> CompareGreaterThan(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static int ConvertToInt32(System.Runtime.Intrinsics.Vector256<int> value) { throw null; }
        public static uint ConvertToUInt32(System.Runtime.Intrinsics.Vector256<uint> value) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<short> ConvertToVector256Int16(byte* address) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> ConvertToVector256Int16(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> ConvertToVector256Int16(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<short> ConvertToVector256Int16(sbyte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<int> ConvertToVector256Int32(byte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<int> ConvertToVector256Int32(short* address) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> ConvertToVector256Int32(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> ConvertToVector256Int32(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> ConvertToVector256Int32(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> ConvertToVector256Int32(System.Runtime.Intrinsics.Vector128<ushort> value) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<int> ConvertToVector256Int32(sbyte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<int> ConvertToVector256Int32(ushort* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<long> ConvertToVector256Int64(byte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<long> ConvertToVector256Int64(short* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<long> ConvertToVector256Int64(int* address) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> ConvertToVector256Int64(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> ConvertToVector256Int64(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> ConvertToVector256Int64(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> ConvertToVector256Int64(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> ConvertToVector256Int64(System.Runtime.Intrinsics.Vector128<ushort> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> ConvertToVector256Int64(System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<long> ConvertToVector256Int64(sbyte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<long> ConvertToVector256Int64(ushort* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<long> ConvertToVector256Int64(uint* address) { throw null; }
        public static new System.Runtime.Intrinsics.Vector128<byte> ExtractVector128(System.Runtime.Intrinsics.Vector256<byte> value, byte index) { throw null; }
        public static new System.Runtime.Intrinsics.Vector128<short> ExtractVector128(System.Runtime.Intrinsics.Vector256<short> value, byte index) { throw null; }
        public static new System.Runtime.Intrinsics.Vector128<int> ExtractVector128(System.Runtime.Intrinsics.Vector256<int> value, byte index) { throw null; }
        public static new System.Runtime.Intrinsics.Vector128<long> ExtractVector128(System.Runtime.Intrinsics.Vector256<long> value, byte index) { throw null; }
        public static new System.Runtime.Intrinsics.Vector128<sbyte> ExtractVector128(System.Runtime.Intrinsics.Vector256<sbyte> value, byte index) { throw null; }
        public static new System.Runtime.Intrinsics.Vector128<ushort> ExtractVector128(System.Runtime.Intrinsics.Vector256<ushort> value, byte index) { throw null; }
        public static new System.Runtime.Intrinsics.Vector128<uint> ExtractVector128(System.Runtime.Intrinsics.Vector256<uint> value, byte index) { throw null; }
        public static new System.Runtime.Intrinsics.Vector128<ulong> ExtractVector128(System.Runtime.Intrinsics.Vector256<ulong> value, byte index) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<double> GatherMaskVector128(System.Runtime.Intrinsics.Vector128<double> source, double* baseAddress, System.Runtime.Intrinsics.Vector128<int> index, System.Runtime.Intrinsics.Vector128<double> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<double> GatherMaskVector128(System.Runtime.Intrinsics.Vector128<double> source, double* baseAddress, System.Runtime.Intrinsics.Vector128<long> index, System.Runtime.Intrinsics.Vector128<double> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<int> GatherMaskVector128(System.Runtime.Intrinsics.Vector128<int> source, int* baseAddress, System.Runtime.Intrinsics.Vector128<int> index, System.Runtime.Intrinsics.Vector128<int> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<int> GatherMaskVector128(System.Runtime.Intrinsics.Vector128<int> source, int* baseAddress, System.Runtime.Intrinsics.Vector128<long> index, System.Runtime.Intrinsics.Vector128<int> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<int> GatherMaskVector128(System.Runtime.Intrinsics.Vector128<int> source, int* baseAddress, System.Runtime.Intrinsics.Vector256<long> index, System.Runtime.Intrinsics.Vector128<int> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<long> GatherMaskVector128(System.Runtime.Intrinsics.Vector128<long> source, long* baseAddress, System.Runtime.Intrinsics.Vector128<int> index, System.Runtime.Intrinsics.Vector128<long> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<long> GatherMaskVector128(System.Runtime.Intrinsics.Vector128<long> source, long* baseAddress, System.Runtime.Intrinsics.Vector128<long> index, System.Runtime.Intrinsics.Vector128<long> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<float> GatherMaskVector128(System.Runtime.Intrinsics.Vector128<float> source, float* baseAddress, System.Runtime.Intrinsics.Vector128<int> index, System.Runtime.Intrinsics.Vector128<float> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<float> GatherMaskVector128(System.Runtime.Intrinsics.Vector128<float> source, float* baseAddress, System.Runtime.Intrinsics.Vector128<long> index, System.Runtime.Intrinsics.Vector128<float> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<float> GatherMaskVector128(System.Runtime.Intrinsics.Vector128<float> source, float* baseAddress, System.Runtime.Intrinsics.Vector256<long> index, System.Runtime.Intrinsics.Vector128<float> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<uint> GatherMaskVector128(System.Runtime.Intrinsics.Vector128<uint> source, uint* baseAddress, System.Runtime.Intrinsics.Vector128<int> index, System.Runtime.Intrinsics.Vector128<uint> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<uint> GatherMaskVector128(System.Runtime.Intrinsics.Vector128<uint> source, uint* baseAddress, System.Runtime.Intrinsics.Vector128<long> index, System.Runtime.Intrinsics.Vector128<uint> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<uint> GatherMaskVector128(System.Runtime.Intrinsics.Vector128<uint> source, uint* baseAddress, System.Runtime.Intrinsics.Vector256<long> index, System.Runtime.Intrinsics.Vector128<uint> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<ulong> GatherMaskVector128(System.Runtime.Intrinsics.Vector128<ulong> source, ulong* baseAddress, System.Runtime.Intrinsics.Vector128<int> index, System.Runtime.Intrinsics.Vector128<ulong> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<ulong> GatherMaskVector128(System.Runtime.Intrinsics.Vector128<ulong> source, ulong* baseAddress, System.Runtime.Intrinsics.Vector128<long> index, System.Runtime.Intrinsics.Vector128<ulong> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<double> GatherMaskVector256(System.Runtime.Intrinsics.Vector256<double> source, double* baseAddress, System.Runtime.Intrinsics.Vector128<int> index, System.Runtime.Intrinsics.Vector256<double> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<double> GatherMaskVector256(System.Runtime.Intrinsics.Vector256<double> source, double* baseAddress, System.Runtime.Intrinsics.Vector256<long> index, System.Runtime.Intrinsics.Vector256<double> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<int> GatherMaskVector256(System.Runtime.Intrinsics.Vector256<int> source, int* baseAddress, System.Runtime.Intrinsics.Vector256<int> index, System.Runtime.Intrinsics.Vector256<int> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<long> GatherMaskVector256(System.Runtime.Intrinsics.Vector256<long> source, long* baseAddress, System.Runtime.Intrinsics.Vector128<int> index, System.Runtime.Intrinsics.Vector256<long> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<long> GatherMaskVector256(System.Runtime.Intrinsics.Vector256<long> source, long* baseAddress, System.Runtime.Intrinsics.Vector256<long> index, System.Runtime.Intrinsics.Vector256<long> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<float> GatherMaskVector256(System.Runtime.Intrinsics.Vector256<float> source, float* baseAddress, System.Runtime.Intrinsics.Vector256<int> index, System.Runtime.Intrinsics.Vector256<float> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<uint> GatherMaskVector256(System.Runtime.Intrinsics.Vector256<uint> source, uint* baseAddress, System.Runtime.Intrinsics.Vector256<int> index, System.Runtime.Intrinsics.Vector256<uint> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<ulong> GatherMaskVector256(System.Runtime.Intrinsics.Vector256<ulong> source, ulong* baseAddress, System.Runtime.Intrinsics.Vector128<int> index, System.Runtime.Intrinsics.Vector256<ulong> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<ulong> GatherMaskVector256(System.Runtime.Intrinsics.Vector256<ulong> source, ulong* baseAddress, System.Runtime.Intrinsics.Vector256<long> index, System.Runtime.Intrinsics.Vector256<ulong> mask, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<double> GatherVector128(double* baseAddress, System.Runtime.Intrinsics.Vector128<int> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<double> GatherVector128(double* baseAddress, System.Runtime.Intrinsics.Vector128<long> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<int> GatherVector128(int* baseAddress, System.Runtime.Intrinsics.Vector128<int> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<int> GatherVector128(int* baseAddress, System.Runtime.Intrinsics.Vector128<long> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<int> GatherVector128(int* baseAddress, System.Runtime.Intrinsics.Vector256<long> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<long> GatherVector128(long* baseAddress, System.Runtime.Intrinsics.Vector128<int> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<long> GatherVector128(long* baseAddress, System.Runtime.Intrinsics.Vector128<long> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<float> GatherVector128(float* baseAddress, System.Runtime.Intrinsics.Vector128<int> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<float> GatherVector128(float* baseAddress, System.Runtime.Intrinsics.Vector128<long> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<float> GatherVector128(float* baseAddress, System.Runtime.Intrinsics.Vector256<long> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<uint> GatherVector128(uint* baseAddress, System.Runtime.Intrinsics.Vector128<int> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<uint> GatherVector128(uint* baseAddress, System.Runtime.Intrinsics.Vector128<long> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<uint> GatherVector128(uint* baseAddress, System.Runtime.Intrinsics.Vector256<long> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<ulong> GatherVector128(ulong* baseAddress, System.Runtime.Intrinsics.Vector128<int> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<ulong> GatherVector128(ulong* baseAddress, System.Runtime.Intrinsics.Vector128<long> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<double> GatherVector256(double* baseAddress, System.Runtime.Intrinsics.Vector128<int> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<double> GatherVector256(double* baseAddress, System.Runtime.Intrinsics.Vector256<long> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<int> GatherVector256(int* baseAddress, System.Runtime.Intrinsics.Vector256<int> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<long> GatherVector256(long* baseAddress, System.Runtime.Intrinsics.Vector128<int> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<long> GatherVector256(long* baseAddress, System.Runtime.Intrinsics.Vector256<long> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<float> GatherVector256(float* baseAddress, System.Runtime.Intrinsics.Vector256<int> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<uint> GatherVector256(uint* baseAddress, System.Runtime.Intrinsics.Vector256<int> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<ulong> GatherVector256(ulong* baseAddress, System.Runtime.Intrinsics.Vector128<int> index, byte scale) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<ulong> GatherVector256(ulong* baseAddress, System.Runtime.Intrinsics.Vector256<long> index, byte scale) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> HorizontalAdd(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> HorizontalAdd(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> HorizontalAddSaturate(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> HorizontalSubtract(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> HorizontalSubtract(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> HorizontalSubtractSaturate(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static new System.Runtime.Intrinsics.Vector256<byte> InsertVector128(System.Runtime.Intrinsics.Vector256<byte> value, System.Runtime.Intrinsics.Vector128<byte> data, byte index) { throw null; }
        public static new System.Runtime.Intrinsics.Vector256<short> InsertVector128(System.Runtime.Intrinsics.Vector256<short> value, System.Runtime.Intrinsics.Vector128<short> data, byte index) { throw null; }
        public static new System.Runtime.Intrinsics.Vector256<int> InsertVector128(System.Runtime.Intrinsics.Vector256<int> value, System.Runtime.Intrinsics.Vector128<int> data, byte index) { throw null; }
        public static new System.Runtime.Intrinsics.Vector256<long> InsertVector128(System.Runtime.Intrinsics.Vector256<long> value, System.Runtime.Intrinsics.Vector128<long> data, byte index) { throw null; }
        public static new System.Runtime.Intrinsics.Vector256<sbyte> InsertVector128(System.Runtime.Intrinsics.Vector256<sbyte> value, System.Runtime.Intrinsics.Vector128<sbyte> data, byte index) { throw null; }
        public static new System.Runtime.Intrinsics.Vector256<ushort> InsertVector128(System.Runtime.Intrinsics.Vector256<ushort> value, System.Runtime.Intrinsics.Vector128<ushort> data, byte index) { throw null; }
        public static new System.Runtime.Intrinsics.Vector256<uint> InsertVector128(System.Runtime.Intrinsics.Vector256<uint> value, System.Runtime.Intrinsics.Vector128<uint> data, byte index) { throw null; }
        public static new System.Runtime.Intrinsics.Vector256<ulong> InsertVector128(System.Runtime.Intrinsics.Vector256<ulong> value, System.Runtime.Intrinsics.Vector128<ulong> data, byte index) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<byte> LoadAlignedVector256NonTemporal(byte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<short> LoadAlignedVector256NonTemporal(short* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<int> LoadAlignedVector256NonTemporal(int* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<long> LoadAlignedVector256NonTemporal(long* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<sbyte> LoadAlignedVector256NonTemporal(sbyte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<ushort> LoadAlignedVector256NonTemporal(ushort* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<uint> LoadAlignedVector256NonTemporal(uint* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<ulong> LoadAlignedVector256NonTemporal(ulong* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<int> MaskLoad(int* address, System.Runtime.Intrinsics.Vector128<int> mask) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<int> MaskLoad(int* address, System.Runtime.Intrinsics.Vector256<int> mask) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<long> MaskLoad(long* address, System.Runtime.Intrinsics.Vector128<long> mask) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<long> MaskLoad(long* address, System.Runtime.Intrinsics.Vector256<long> mask) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<uint> MaskLoad(uint* address, System.Runtime.Intrinsics.Vector128<uint> mask) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<uint> MaskLoad(uint* address, System.Runtime.Intrinsics.Vector256<uint> mask) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<ulong> MaskLoad(ulong* address, System.Runtime.Intrinsics.Vector128<ulong> mask) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector256<ulong> MaskLoad(ulong* address, System.Runtime.Intrinsics.Vector256<ulong> mask) { throw null; }
        public unsafe static void MaskStore(int* address, System.Runtime.Intrinsics.Vector128<int> mask, System.Runtime.Intrinsics.Vector128<int> source) { }
        public unsafe static void MaskStore(int* address, System.Runtime.Intrinsics.Vector256<int> mask, System.Runtime.Intrinsics.Vector256<int> source) { }
        public unsafe static void MaskStore(long* address, System.Runtime.Intrinsics.Vector128<long> mask, System.Runtime.Intrinsics.Vector128<long> source) { }
        public unsafe static void MaskStore(long* address, System.Runtime.Intrinsics.Vector256<long> mask, System.Runtime.Intrinsics.Vector256<long> source) { }
        public unsafe static void MaskStore(uint* address, System.Runtime.Intrinsics.Vector128<uint> mask, System.Runtime.Intrinsics.Vector128<uint> source) { }
        public unsafe static void MaskStore(uint* address, System.Runtime.Intrinsics.Vector256<uint> mask, System.Runtime.Intrinsics.Vector256<uint> source) { }
        public unsafe static void MaskStore(ulong* address, System.Runtime.Intrinsics.Vector128<ulong> mask, System.Runtime.Intrinsics.Vector128<ulong> source) { }
        public unsafe static void MaskStore(ulong* address, System.Runtime.Intrinsics.Vector256<ulong> mask, System.Runtime.Intrinsics.Vector256<ulong> source) { }
        public static System.Runtime.Intrinsics.Vector256<byte> Max(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> Max(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> Max(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> Max(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> Max(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> Max(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> Min(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> Min(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> Min(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> Min(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> Min(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> Min(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> right) { throw null; }
        public static int MoveMask(System.Runtime.Intrinsics.Vector256<byte> value) { throw null; }
        public static int MoveMask(System.Runtime.Intrinsics.Vector256<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> MultipleSumAbsoluteDifferences(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right, byte mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> Multiply(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> Multiply(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> MultiplyAddAdjacent(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> MultiplyAddAdjacent(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> MultiplyHigh(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> MultiplyHigh(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> MultiplyHighRoundScale(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> MultiplyLow(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> MultiplyLow(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> MultiplyLow(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> MultiplyLow(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> Or(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> Or(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> Or(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> Or(System.Runtime.Intrinsics.Vector256<long> left, System.Runtime.Intrinsics.Vector256<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> Or(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> Or(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> Or(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> Or(System.Runtime.Intrinsics.Vector256<ulong> left, System.Runtime.Intrinsics.Vector256<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> PackSignedSaturate(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> PackSignedSaturate(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> PackUnsignedSaturate(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> PackUnsignedSaturate(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static new System.Runtime.Intrinsics.Vector256<byte> Permute2x128(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right, byte control) { throw null; }
        public static new System.Runtime.Intrinsics.Vector256<short> Permute2x128(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right, byte control) { throw null; }
        public static new System.Runtime.Intrinsics.Vector256<int> Permute2x128(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right, byte control) { throw null; }
        public static new System.Runtime.Intrinsics.Vector256<long> Permute2x128(System.Runtime.Intrinsics.Vector256<long> left, System.Runtime.Intrinsics.Vector256<long> right, byte control) { throw null; }
        public static new System.Runtime.Intrinsics.Vector256<sbyte> Permute2x128(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right, byte control) { throw null; }
        public static new System.Runtime.Intrinsics.Vector256<ushort> Permute2x128(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right, byte control) { throw null; }
        public static new System.Runtime.Intrinsics.Vector256<uint> Permute2x128(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> right, byte control) { throw null; }
        public static new System.Runtime.Intrinsics.Vector256<ulong> Permute2x128(System.Runtime.Intrinsics.Vector256<ulong> left, System.Runtime.Intrinsics.Vector256<ulong> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> Permute4x64(System.Runtime.Intrinsics.Vector256<double> value, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> Permute4x64(System.Runtime.Intrinsics.Vector256<long> value, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> Permute4x64(System.Runtime.Intrinsics.Vector256<ulong> value, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> PermuteVar8x32(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> PermuteVar8x32(System.Runtime.Intrinsics.Vector256<float> left, System.Runtime.Intrinsics.Vector256<int> control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> PermuteVar8x32(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> ShiftLeftLogical(System.Runtime.Intrinsics.Vector256<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> ShiftLeftLogical(System.Runtime.Intrinsics.Vector256<short> value, System.Runtime.Intrinsics.Vector128<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> ShiftLeftLogical(System.Runtime.Intrinsics.Vector256<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> ShiftLeftLogical(System.Runtime.Intrinsics.Vector256<int> value, System.Runtime.Intrinsics.Vector128<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> ShiftLeftLogical(System.Runtime.Intrinsics.Vector256<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> ShiftLeftLogical(System.Runtime.Intrinsics.Vector256<long> value, System.Runtime.Intrinsics.Vector128<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> ShiftLeftLogical(System.Runtime.Intrinsics.Vector256<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> ShiftLeftLogical(System.Runtime.Intrinsics.Vector256<ushort> value, System.Runtime.Intrinsics.Vector128<ushort> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> ShiftLeftLogical(System.Runtime.Intrinsics.Vector256<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> ShiftLeftLogical(System.Runtime.Intrinsics.Vector256<uint> value, System.Runtime.Intrinsics.Vector128<uint> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> ShiftLeftLogical(System.Runtime.Intrinsics.Vector256<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> ShiftLeftLogical(System.Runtime.Intrinsics.Vector256<ulong> value, System.Runtime.Intrinsics.Vector128<ulong> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> ShiftLeftLogical128BitLane(System.Runtime.Intrinsics.Vector256<byte> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> ShiftLeftLogical128BitLane(System.Runtime.Intrinsics.Vector256<short> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> ShiftLeftLogical128BitLane(System.Runtime.Intrinsics.Vector256<int> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> ShiftLeftLogical128BitLane(System.Runtime.Intrinsics.Vector256<long> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> ShiftLeftLogical128BitLane(System.Runtime.Intrinsics.Vector256<sbyte> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> ShiftLeftLogical128BitLane(System.Runtime.Intrinsics.Vector256<ushort> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> ShiftLeftLogical128BitLane(System.Runtime.Intrinsics.Vector256<uint> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> ShiftLeftLogical128BitLane(System.Runtime.Intrinsics.Vector256<ulong> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftLeftLogicalVariable(System.Runtime.Intrinsics.Vector128<int> value, System.Runtime.Intrinsics.Vector128<uint> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftLeftLogicalVariable(System.Runtime.Intrinsics.Vector128<long> value, System.Runtime.Intrinsics.Vector128<ulong> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftLeftLogicalVariable(System.Runtime.Intrinsics.Vector128<uint> value, System.Runtime.Intrinsics.Vector128<uint> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftLeftLogicalVariable(System.Runtime.Intrinsics.Vector128<ulong> value, System.Runtime.Intrinsics.Vector128<ulong> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> ShiftLeftLogicalVariable(System.Runtime.Intrinsics.Vector256<int> value, System.Runtime.Intrinsics.Vector256<uint> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> ShiftLeftLogicalVariable(System.Runtime.Intrinsics.Vector256<long> value, System.Runtime.Intrinsics.Vector256<ulong> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> ShiftLeftLogicalVariable(System.Runtime.Intrinsics.Vector256<uint> value, System.Runtime.Intrinsics.Vector256<uint> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> ShiftLeftLogicalVariable(System.Runtime.Intrinsics.Vector256<ulong> value, System.Runtime.Intrinsics.Vector256<ulong> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector256<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector256<short> value, System.Runtime.Intrinsics.Vector128<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector256<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector256<int> value, System.Runtime.Intrinsics.Vector128<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightArithmeticVariable(System.Runtime.Intrinsics.Vector128<int> value, System.Runtime.Intrinsics.Vector128<uint> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> ShiftRightArithmeticVariable(System.Runtime.Intrinsics.Vector256<int> value, System.Runtime.Intrinsics.Vector256<uint> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<short> value, System.Runtime.Intrinsics.Vector128<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<int> value, System.Runtime.Intrinsics.Vector128<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<long> value, System.Runtime.Intrinsics.Vector128<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<ushort> value, System.Runtime.Intrinsics.Vector128<ushort> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<uint> value, System.Runtime.Intrinsics.Vector128<uint> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> ShiftRightLogical(System.Runtime.Intrinsics.Vector256<ulong> value, System.Runtime.Intrinsics.Vector128<ulong> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> ShiftRightLogical128BitLane(System.Runtime.Intrinsics.Vector256<byte> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> ShiftRightLogical128BitLane(System.Runtime.Intrinsics.Vector256<short> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> ShiftRightLogical128BitLane(System.Runtime.Intrinsics.Vector256<int> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> ShiftRightLogical128BitLane(System.Runtime.Intrinsics.Vector256<long> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> ShiftRightLogical128BitLane(System.Runtime.Intrinsics.Vector256<sbyte> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> ShiftRightLogical128BitLane(System.Runtime.Intrinsics.Vector256<ushort> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> ShiftRightLogical128BitLane(System.Runtime.Intrinsics.Vector256<uint> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> ShiftRightLogical128BitLane(System.Runtime.Intrinsics.Vector256<ulong> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightLogicalVariable(System.Runtime.Intrinsics.Vector128<int> value, System.Runtime.Intrinsics.Vector128<uint> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftRightLogicalVariable(System.Runtime.Intrinsics.Vector128<long> value, System.Runtime.Intrinsics.Vector128<ulong> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftRightLogicalVariable(System.Runtime.Intrinsics.Vector128<uint> value, System.Runtime.Intrinsics.Vector128<uint> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftRightLogicalVariable(System.Runtime.Intrinsics.Vector128<ulong> value, System.Runtime.Intrinsics.Vector128<ulong> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> ShiftRightLogicalVariable(System.Runtime.Intrinsics.Vector256<int> value, System.Runtime.Intrinsics.Vector256<uint> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> ShiftRightLogicalVariable(System.Runtime.Intrinsics.Vector256<long> value, System.Runtime.Intrinsics.Vector256<ulong> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> ShiftRightLogicalVariable(System.Runtime.Intrinsics.Vector256<uint> value, System.Runtime.Intrinsics.Vector256<uint> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> ShiftRightLogicalVariable(System.Runtime.Intrinsics.Vector256<ulong> value, System.Runtime.Intrinsics.Vector256<ulong> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> Shuffle(System.Runtime.Intrinsics.Vector256<byte> value, System.Runtime.Intrinsics.Vector256<byte> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> Shuffle(System.Runtime.Intrinsics.Vector256<int> value, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> Shuffle(System.Runtime.Intrinsics.Vector256<sbyte> value, System.Runtime.Intrinsics.Vector256<sbyte> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> Shuffle(System.Runtime.Intrinsics.Vector256<uint> value, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> ShuffleHigh(System.Runtime.Intrinsics.Vector256<short> value, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> ShuffleHigh(System.Runtime.Intrinsics.Vector256<ushort> value, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> ShuffleLow(System.Runtime.Intrinsics.Vector256<short> value, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> ShuffleLow(System.Runtime.Intrinsics.Vector256<ushort> value, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> Sign(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> Sign(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> Sign(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> Subtract(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> Subtract(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> Subtract(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> Subtract(System.Runtime.Intrinsics.Vector256<long> left, System.Runtime.Intrinsics.Vector256<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> Subtract(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> Subtract(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> Subtract(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> Subtract(System.Runtime.Intrinsics.Vector256<ulong> left, System.Runtime.Intrinsics.Vector256<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> SubtractSaturate(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> SubtractSaturate(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> SubtractSaturate(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> SubtractSaturate(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> SumAbsoluteDifferences(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> UnpackHigh(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> UnpackHigh(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> UnpackHigh(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> UnpackHigh(System.Runtime.Intrinsics.Vector256<long> left, System.Runtime.Intrinsics.Vector256<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> UnpackHigh(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> UnpackHigh(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> UnpackHigh(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> UnpackHigh(System.Runtime.Intrinsics.Vector256<ulong> left, System.Runtime.Intrinsics.Vector256<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> UnpackLow(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> UnpackLow(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> UnpackLow(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> UnpackLow(System.Runtime.Intrinsics.Vector256<long> left, System.Runtime.Intrinsics.Vector256<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> UnpackLow(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> UnpackLow(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> UnpackLow(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> UnpackLow(System.Runtime.Intrinsics.Vector256<ulong> left, System.Runtime.Intrinsics.Vector256<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<byte> Xor(System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<short> Xor(System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> Xor(System.Runtime.Intrinsics.Vector256<int> left, System.Runtime.Intrinsics.Vector256<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<long> Xor(System.Runtime.Intrinsics.Vector256<long> left, System.Runtime.Intrinsics.Vector256<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<sbyte> Xor(System.Runtime.Intrinsics.Vector256<sbyte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ushort> Xor(System.Runtime.Intrinsics.Vector256<ushort> left, System.Runtime.Intrinsics.Vector256<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<uint> Xor(System.Runtime.Intrinsics.Vector256<uint> left, System.Runtime.Intrinsics.Vector256<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<ulong> Xor(System.Runtime.Intrinsics.Vector256<ulong> left, System.Runtime.Intrinsics.Vector256<ulong> right) { throw null; }
        public new abstract partial class X64 : System.Runtime.Intrinsics.X86.Avx.X64
        {
            internal X64() { }
            public static new bool IsSupported { get { throw null; } }
        }
    }
    [System.CLSCompliantAttribute(false)]
    [System.Runtime.Versioning.RequiresPreviewFeaturesAttribute("AvxVnni is in preview.")]
    public abstract class AvxVnni : System.Runtime.Intrinsics.X86.Avx2
    {
        internal AvxVnni() { }
        public static new bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyWideningAndAdd(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyWideningAndAdd(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> MultiplyWideningAndAdd(System.Runtime.Intrinsics.Vector256<int> addend, System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> MultiplyWideningAndAdd(System.Runtime.Intrinsics.Vector256<int> addend, System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyWideningAndAddSaturate(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyWideningAndAddSaturate(System.Runtime.Intrinsics.Vector128<int> addend, System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> MultiplyWideningAndAddSaturate(System.Runtime.Intrinsics.Vector256<int> addend, System.Runtime.Intrinsics.Vector256<byte> left, System.Runtime.Intrinsics.Vector256<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<int> MultiplyWideningAndAddSaturate(System.Runtime.Intrinsics.Vector256<int> addend, System.Runtime.Intrinsics.Vector256<short> left, System.Runtime.Intrinsics.Vector256<short> right) { throw null; }
        public new abstract partial class X64 : System.Runtime.Intrinsics.X86.Avx2.X64
        {
            internal X64() { }
            public static new bool IsSupported { get { throw null; } }
        }
    }
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Bmi1 : System.Runtime.Intrinsics.X86.X86Base
    {
        internal Bmi1() { }
        public static new bool IsSupported { get { throw null; } }
        public static uint AndNot(uint left, uint right) { throw null; }
        public static uint BitFieldExtract(uint value, byte start, byte length) { throw null; }
        public static uint BitFieldExtract(uint value, ushort control) { throw null; }
        public static uint ExtractLowestSetBit(uint value) { throw null; }
        public static uint GetMaskUpToLowestSetBit(uint value) { throw null; }
        public static uint ResetLowestSetBit(uint value) { throw null; }
        public static uint TrailingZeroCount(uint value) { throw null; }
        public new abstract partial class X64 : System.Runtime.Intrinsics.X86.X86Base.X64
        {
            internal X64() { }
            public static new bool IsSupported { get { throw null; } }
            public static ulong AndNot(ulong left, ulong right) { throw null; }
            public static ulong BitFieldExtract(ulong value, byte start, byte length) { throw null; }
            public static ulong BitFieldExtract(ulong value, ushort control) { throw null; }
            public static ulong ExtractLowestSetBit(ulong value) { throw null; }
            public static ulong GetMaskUpToLowestSetBit(ulong value) { throw null; }
            public static ulong ResetLowestSetBit(ulong value) { throw null; }
            public static ulong TrailingZeroCount(ulong value) { throw null; }
        }
    }
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Bmi2 : System.Runtime.Intrinsics.X86.X86Base
    {
        internal Bmi2() { }
        public static new bool IsSupported { get { throw null; } }
        public static uint MultiplyNoFlags(uint left, uint right) { throw null; }
        public unsafe static uint MultiplyNoFlags(uint left, uint right, uint* low) { throw null; }
        public static uint ParallelBitDeposit(uint value, uint mask) { throw null; }
        public static uint ParallelBitExtract(uint value, uint mask) { throw null; }
        public static uint ZeroHighBits(uint value, uint index) { throw null; }
        public new abstract partial class X64 : System.Runtime.Intrinsics.X86.X86Base.X64
        {
            internal X64() { }
            public static new bool IsSupported { get { throw null; } }
            public static ulong MultiplyNoFlags(ulong left, ulong right) { throw null; }
            public unsafe static ulong MultiplyNoFlags(ulong left, ulong right, ulong* low) { throw null; }
            public static ulong ParallelBitDeposit(ulong value, ulong mask) { throw null; }
            public static ulong ParallelBitExtract(ulong value, ulong mask) { throw null; }
            public static ulong ZeroHighBits(ulong value, ulong index) { throw null; }
        }
    }
    public enum FloatComparisonMode : byte
    {
        OrderedEqualNonSignaling = (byte)0,
        OrderedLessThanSignaling = (byte)1,
        OrderedLessThanOrEqualSignaling = (byte)2,
        UnorderedNonSignaling = (byte)3,
        UnorderedNotEqualNonSignaling = (byte)4,
        UnorderedNotLessThanSignaling = (byte)5,
        UnorderedNotLessThanOrEqualSignaling = (byte)6,
        OrderedNonSignaling = (byte)7,
        UnorderedEqualNonSignaling = (byte)8,
        UnorderedNotGreaterThanOrEqualSignaling = (byte)9,
        UnorderedNotGreaterThanSignaling = (byte)10,
        OrderedFalseNonSignaling = (byte)11,
        OrderedNotEqualNonSignaling = (byte)12,
        OrderedGreaterThanOrEqualSignaling = (byte)13,
        OrderedGreaterThanSignaling = (byte)14,
        UnorderedTrueNonSignaling = (byte)15,
        OrderedEqualSignaling = (byte)16,
        OrderedLessThanNonSignaling = (byte)17,
        OrderedLessThanOrEqualNonSignaling = (byte)18,
        UnorderedSignaling = (byte)19,
        UnorderedNotEqualSignaling = (byte)20,
        UnorderedNotLessThanNonSignaling = (byte)21,
        UnorderedNotLessThanOrEqualNonSignaling = (byte)22,
        OrderedSignaling = (byte)23,
        UnorderedEqualSignaling = (byte)24,
        UnorderedNotGreaterThanOrEqualNonSignaling = (byte)25,
        UnorderedNotGreaterThanNonSignaling = (byte)26,
        OrderedFalseSignaling = (byte)27,
        OrderedNotEqualSignaling = (byte)28,
        OrderedGreaterThanOrEqualNonSignaling = (byte)29,
        OrderedGreaterThanNonSignaling = (byte)30,
        UnorderedTrueSignaling = (byte)31,
    }
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Fma : System.Runtime.Intrinsics.X86.Avx
    {
        internal Fma() { }
        public static new bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<double> MultiplyAdd(System.Runtime.Intrinsics.Vector128<double> a, System.Runtime.Intrinsics.Vector128<double> b, System.Runtime.Intrinsics.Vector128<double> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> MultiplyAdd(System.Runtime.Intrinsics.Vector128<float> a, System.Runtime.Intrinsics.Vector128<float> b, System.Runtime.Intrinsics.Vector128<float> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> MultiplyAdd(System.Runtime.Intrinsics.Vector256<double> a, System.Runtime.Intrinsics.Vector256<double> b, System.Runtime.Intrinsics.Vector256<double> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> MultiplyAdd(System.Runtime.Intrinsics.Vector256<float> a, System.Runtime.Intrinsics.Vector256<float> b, System.Runtime.Intrinsics.Vector256<float> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> MultiplyAddNegated(System.Runtime.Intrinsics.Vector128<double> a, System.Runtime.Intrinsics.Vector128<double> b, System.Runtime.Intrinsics.Vector128<double> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> MultiplyAddNegated(System.Runtime.Intrinsics.Vector128<float> a, System.Runtime.Intrinsics.Vector128<float> b, System.Runtime.Intrinsics.Vector128<float> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> MultiplyAddNegated(System.Runtime.Intrinsics.Vector256<double> a, System.Runtime.Intrinsics.Vector256<double> b, System.Runtime.Intrinsics.Vector256<double> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> MultiplyAddNegated(System.Runtime.Intrinsics.Vector256<float> a, System.Runtime.Intrinsics.Vector256<float> b, System.Runtime.Intrinsics.Vector256<float> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> MultiplyAddNegatedScalar(System.Runtime.Intrinsics.Vector128<double> a, System.Runtime.Intrinsics.Vector128<double> b, System.Runtime.Intrinsics.Vector128<double> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> MultiplyAddNegatedScalar(System.Runtime.Intrinsics.Vector128<float> a, System.Runtime.Intrinsics.Vector128<float> b, System.Runtime.Intrinsics.Vector128<float> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> MultiplyAddScalar(System.Runtime.Intrinsics.Vector128<double> a, System.Runtime.Intrinsics.Vector128<double> b, System.Runtime.Intrinsics.Vector128<double> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> MultiplyAddScalar(System.Runtime.Intrinsics.Vector128<float> a, System.Runtime.Intrinsics.Vector128<float> b, System.Runtime.Intrinsics.Vector128<float> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> MultiplyAddSubtract(System.Runtime.Intrinsics.Vector128<double> a, System.Runtime.Intrinsics.Vector128<double> b, System.Runtime.Intrinsics.Vector128<double> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> MultiplyAddSubtract(System.Runtime.Intrinsics.Vector128<float> a, System.Runtime.Intrinsics.Vector128<float> b, System.Runtime.Intrinsics.Vector128<float> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> MultiplyAddSubtract(System.Runtime.Intrinsics.Vector256<double> a, System.Runtime.Intrinsics.Vector256<double> b, System.Runtime.Intrinsics.Vector256<double> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> MultiplyAddSubtract(System.Runtime.Intrinsics.Vector256<float> a, System.Runtime.Intrinsics.Vector256<float> b, System.Runtime.Intrinsics.Vector256<float> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> MultiplySubtract(System.Runtime.Intrinsics.Vector128<double> a, System.Runtime.Intrinsics.Vector128<double> b, System.Runtime.Intrinsics.Vector128<double> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> MultiplySubtract(System.Runtime.Intrinsics.Vector128<float> a, System.Runtime.Intrinsics.Vector128<float> b, System.Runtime.Intrinsics.Vector128<float> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> MultiplySubtract(System.Runtime.Intrinsics.Vector256<double> a, System.Runtime.Intrinsics.Vector256<double> b, System.Runtime.Intrinsics.Vector256<double> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> MultiplySubtract(System.Runtime.Intrinsics.Vector256<float> a, System.Runtime.Intrinsics.Vector256<float> b, System.Runtime.Intrinsics.Vector256<float> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> MultiplySubtractAdd(System.Runtime.Intrinsics.Vector128<double> a, System.Runtime.Intrinsics.Vector128<double> b, System.Runtime.Intrinsics.Vector128<double> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> MultiplySubtractAdd(System.Runtime.Intrinsics.Vector128<float> a, System.Runtime.Intrinsics.Vector128<float> b, System.Runtime.Intrinsics.Vector128<float> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> MultiplySubtractAdd(System.Runtime.Intrinsics.Vector256<double> a, System.Runtime.Intrinsics.Vector256<double> b, System.Runtime.Intrinsics.Vector256<double> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> MultiplySubtractAdd(System.Runtime.Intrinsics.Vector256<float> a, System.Runtime.Intrinsics.Vector256<float> b, System.Runtime.Intrinsics.Vector256<float> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> MultiplySubtractNegated(System.Runtime.Intrinsics.Vector128<double> a, System.Runtime.Intrinsics.Vector128<double> b, System.Runtime.Intrinsics.Vector128<double> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> MultiplySubtractNegated(System.Runtime.Intrinsics.Vector128<float> a, System.Runtime.Intrinsics.Vector128<float> b, System.Runtime.Intrinsics.Vector128<float> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<double> MultiplySubtractNegated(System.Runtime.Intrinsics.Vector256<double> a, System.Runtime.Intrinsics.Vector256<double> b, System.Runtime.Intrinsics.Vector256<double> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector256<float> MultiplySubtractNegated(System.Runtime.Intrinsics.Vector256<float> a, System.Runtime.Intrinsics.Vector256<float> b, System.Runtime.Intrinsics.Vector256<float> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> MultiplySubtractNegatedScalar(System.Runtime.Intrinsics.Vector128<double> a, System.Runtime.Intrinsics.Vector128<double> b, System.Runtime.Intrinsics.Vector128<double> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> MultiplySubtractNegatedScalar(System.Runtime.Intrinsics.Vector128<float> a, System.Runtime.Intrinsics.Vector128<float> b, System.Runtime.Intrinsics.Vector128<float> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> MultiplySubtractScalar(System.Runtime.Intrinsics.Vector128<double> a, System.Runtime.Intrinsics.Vector128<double> b, System.Runtime.Intrinsics.Vector128<double> c) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> MultiplySubtractScalar(System.Runtime.Intrinsics.Vector128<float> a, System.Runtime.Intrinsics.Vector128<float> b, System.Runtime.Intrinsics.Vector128<float> c) { throw null; }
        public new abstract partial class X64 : System.Runtime.Intrinsics.X86.Avx.X64
        {
            internal X64() { }
            public static new bool IsSupported { get { throw null; } }
        }
    }
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Lzcnt : System.Runtime.Intrinsics.X86.X86Base
    {
        internal Lzcnt() { }
        public static new bool IsSupported { get { throw null; } }
        public static uint LeadingZeroCount(uint value) { throw null; }
        public new abstract partial class X64 : System.Runtime.Intrinsics.X86.X86Base.X64
        {
            internal X64() { }
            public static new bool IsSupported { get { throw null; } }
            public static ulong LeadingZeroCount(ulong value) { throw null; }
        }
    }
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Pclmulqdq : System.Runtime.Intrinsics.X86.Sse2
    {
        internal Pclmulqdq() { }
        public static new bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<long> CarrylessMultiply(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> CarrylessMultiply(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right, byte control) { throw null; }
        public new abstract partial class X64 : System.Runtime.Intrinsics.X86.Sse2.X64
        {
            internal X64() { }
            public static new bool IsSupported { get { throw null; } }
        }
    }
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Popcnt : System.Runtime.Intrinsics.X86.Sse42
    {
        internal Popcnt() { }
        public static new bool IsSupported { get { throw null; } }
        public static uint PopCount(uint value) { throw null; }
        public new abstract partial class X64 : System.Runtime.Intrinsics.X86.Sse42.X64
        {
            internal X64() { }
            public static new bool IsSupported { get { throw null; } }
            public static ulong PopCount(ulong value) { throw null; }
        }
    }
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Sse : System.Runtime.Intrinsics.X86.X86Base
    {
        internal Sse() { }
        public static new bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<float> Add(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> AddScalar(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> And(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> AndNot(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareGreaterThan(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareLessThan(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareLessThanOrEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareNotEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareNotGreaterThan(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareNotGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareNotLessThan(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareNotLessThanOrEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareOrdered(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareScalarEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareScalarGreaterThan(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareScalarGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareScalarLessThan(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareScalarLessThanOrEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareScalarNotEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareScalarNotGreaterThan(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareScalarNotGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareScalarNotLessThan(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareScalarNotLessThanOrEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareScalarOrdered(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static bool CompareScalarOrderedEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static bool CompareScalarOrderedGreaterThan(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static bool CompareScalarOrderedGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static bool CompareScalarOrderedLessThan(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static bool CompareScalarOrderedLessThanOrEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static bool CompareScalarOrderedNotEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareScalarUnordered(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static bool CompareScalarUnorderedEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static bool CompareScalarUnorderedGreaterThan(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static bool CompareScalarUnorderedGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static bool CompareScalarUnorderedLessThan(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static bool CompareScalarUnorderedLessThanOrEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static bool CompareScalarUnorderedNotEqual(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CompareUnordered(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> ConvertScalarToVector128Single(System.Runtime.Intrinsics.Vector128<float> upper, int value) { throw null; }
        public static int ConvertToInt32(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static int ConvertToInt32WithTruncation(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Divide(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> DivideScalar(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<float> LoadAlignedVector128(float* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<float> LoadHigh(System.Runtime.Intrinsics.Vector128<float> lower, float* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<float> LoadLow(System.Runtime.Intrinsics.Vector128<float> upper, float* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<float> LoadScalarVector128(float* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<float> LoadVector128(float* address) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Max(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> MaxScalar(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Min(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> MinScalar(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> MoveHighToLow(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> MoveLowToHigh(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static int MoveMask(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> MoveScalar(System.Runtime.Intrinsics.Vector128<float> upper, System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Multiply(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> MultiplyScalar(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Or(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public unsafe static void Prefetch0(void* address) { }
        public unsafe static void Prefetch1(void* address) { }
        public unsafe static void Prefetch2(void* address) { }
        public unsafe static void PrefetchNonTemporal(void* address) { }
        public static System.Runtime.Intrinsics.Vector128<float> Reciprocal(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> ReciprocalScalar(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> ReciprocalScalar(System.Runtime.Intrinsics.Vector128<float> upper, System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> ReciprocalSqrt(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> ReciprocalSqrtScalar(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> ReciprocalSqrtScalar(System.Runtime.Intrinsics.Vector128<float> upper, System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Shuffle(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Sqrt(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> SqrtScalar(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> SqrtScalar(System.Runtime.Intrinsics.Vector128<float> upper, System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public unsafe static void Store(float* address, System.Runtime.Intrinsics.Vector128<float> source) { }
        public unsafe static void StoreAligned(float* address, System.Runtime.Intrinsics.Vector128<float> source) { }
        public unsafe static void StoreAlignedNonTemporal(float* address, System.Runtime.Intrinsics.Vector128<float> source) { }
        public static void StoreFence() { }
        public unsafe static void StoreHigh(float* address, System.Runtime.Intrinsics.Vector128<float> source) { }
        public unsafe static void StoreLow(float* address, System.Runtime.Intrinsics.Vector128<float> source) { }
        public unsafe static void StoreScalar(float* address, System.Runtime.Intrinsics.Vector128<float> source) { }
        public static System.Runtime.Intrinsics.Vector128<float> Subtract(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> SubtractScalar(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> UnpackHigh(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> UnpackLow(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Xor(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public new abstract partial class X64 : System.Runtime.Intrinsics.X86.X86Base.X64
        {
            internal X64() { }
            public static new bool IsSupported { get { throw null; } }
            public static System.Runtime.Intrinsics.Vector128<float> ConvertScalarToVector128Single(System.Runtime.Intrinsics.Vector128<float> upper, long value) { throw null; }
            public static long ConvertToInt64(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
            public static long ConvertToInt64WithTruncation(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        }
    }
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Sse2 : System.Runtime.Intrinsics.X86.Sse
    {
        internal Sse2() { }
        public static new bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<byte> Add(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Add(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> Add(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Add(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> Add(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> Add(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> Add(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> Add(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> Add(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> AddSaturate(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> AddSaturate(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> AddSaturate(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> AddSaturate(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> AddScalar(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> And(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> And(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> And(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> And(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> And(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> And(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> And(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> And(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> And(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> AndNot(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> AndNot(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> AndNot(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> AndNot(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> AndNot(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> AndNot(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> AndNot(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> AndNot(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> AndNot(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Average(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> Average(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> CompareEqual(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> CompareEqual(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> CompareEqual(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> CompareEqual(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> CompareEqual(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> CompareEqual(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareGreaterThan(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> CompareGreaterThan(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> CompareGreaterThan(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> CompareGreaterThan(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareLessThan(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> CompareLessThan(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> CompareLessThan(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> CompareLessThan(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareLessThanOrEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareNotEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareNotGreaterThan(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareNotGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareNotLessThan(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareNotLessThanOrEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareOrdered(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareScalarEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareScalarGreaterThan(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareScalarGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareScalarLessThan(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareScalarLessThanOrEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareScalarNotEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareScalarNotGreaterThan(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareScalarNotGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareScalarNotLessThan(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareScalarNotLessThanOrEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareScalarOrdered(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static bool CompareScalarOrderedEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static bool CompareScalarOrderedGreaterThan(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static bool CompareScalarOrderedGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static bool CompareScalarOrderedLessThan(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static bool CompareScalarOrderedLessThanOrEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static bool CompareScalarOrderedNotEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareScalarUnordered(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static bool CompareScalarUnorderedEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static bool CompareScalarUnorderedGreaterThan(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static bool CompareScalarUnorderedGreaterThanOrEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static bool CompareScalarUnorderedLessThan(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static bool CompareScalarUnorderedLessThanOrEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static bool CompareScalarUnorderedNotEqual(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CompareUnordered(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> ConvertScalarToVector128Double(System.Runtime.Intrinsics.Vector128<double> upper, int value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> ConvertScalarToVector128Double(System.Runtime.Intrinsics.Vector128<double> upper, System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ConvertScalarToVector128Int32(int value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> ConvertScalarToVector128Single(System.Runtime.Intrinsics.Vector128<float> upper, System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ConvertScalarToVector128UInt32(uint value) { throw null; }
        public static int ConvertToInt32(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static int ConvertToInt32(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static int ConvertToInt32WithTruncation(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static uint ConvertToUInt32(System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> ConvertToVector128Double(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> ConvertToVector128Double(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ConvertToVector128Int32(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ConvertToVector128Int32(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ConvertToVector128Int32WithTruncation(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ConvertToVector128Int32WithTruncation(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> ConvertToVector128Single(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> ConvertToVector128Single(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Divide(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> DivideScalar(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static ushort Extract(System.Runtime.Intrinsics.Vector128<ushort> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> Insert(System.Runtime.Intrinsics.Vector128<short> value, short data, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> Insert(System.Runtime.Intrinsics.Vector128<ushort> value, ushort data, byte index) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<byte> LoadAlignedVector128(byte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<double> LoadAlignedVector128(double* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<short> LoadAlignedVector128(short* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<int> LoadAlignedVector128(int* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<long> LoadAlignedVector128(long* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<sbyte> LoadAlignedVector128(sbyte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<ushort> LoadAlignedVector128(ushort* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<uint> LoadAlignedVector128(uint* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<ulong> LoadAlignedVector128(ulong* address) { throw null; }
        public static void LoadFence() { }
        public unsafe static System.Runtime.Intrinsics.Vector128<double> LoadHigh(System.Runtime.Intrinsics.Vector128<double> lower, double* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<double> LoadLow(System.Runtime.Intrinsics.Vector128<double> upper, double* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<double> LoadScalarVector128(double* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<int> LoadScalarVector128(int* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<long> LoadScalarVector128(long* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<uint> LoadScalarVector128(uint* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<ulong> LoadScalarVector128(ulong* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<byte> LoadVector128(byte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<double> LoadVector128(double* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<short> LoadVector128(short* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<int> LoadVector128(int* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<long> LoadVector128(long* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<sbyte> LoadVector128(sbyte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<ushort> LoadVector128(ushort* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<uint> LoadVector128(uint* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<ulong> LoadVector128(ulong* address) { throw null; }
        public unsafe static void MaskMove(System.Runtime.Intrinsics.Vector128<byte> source, System.Runtime.Intrinsics.Vector128<byte> mask, byte* address) { }
        public unsafe static void MaskMove(System.Runtime.Intrinsics.Vector128<sbyte> source, System.Runtime.Intrinsics.Vector128<sbyte> mask, sbyte* address) { }
        public static System.Runtime.Intrinsics.Vector128<byte> Max(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Max(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> Max(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> MaxScalar(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static void MemoryFence() { }
        public static System.Runtime.Intrinsics.Vector128<byte> Min(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Min(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> Min(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> MinScalar(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static int MoveMask(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
        public static int MoveMask(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static int MoveMask(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> MoveScalar(System.Runtime.Intrinsics.Vector128<double> upper, System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> MoveScalar(System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> MoveScalar(System.Runtime.Intrinsics.Vector128<ulong> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Multiply(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> Multiply(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyAddAdjacent(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyHigh(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> MultiplyHigh(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyLow(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> MultiplyLow(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> MultiplyScalar(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Or(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Or(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> Or(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Or(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> Or(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> Or(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> Or(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> Or(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> Or(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> PackSignedSaturate(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> PackSignedSaturate(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> PackUnsignedSaturate(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftLeftLogical(System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftLeftLogical(System.Runtime.Intrinsics.Vector128<short> value, System.Runtime.Intrinsics.Vector128<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftLeftLogical(System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftLeftLogical(System.Runtime.Intrinsics.Vector128<int> value, System.Runtime.Intrinsics.Vector128<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftLeftLogical(System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftLeftLogical(System.Runtime.Intrinsics.Vector128<long> value, System.Runtime.Intrinsics.Vector128<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftLeftLogical(System.Runtime.Intrinsics.Vector128<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftLeftLogical(System.Runtime.Intrinsics.Vector128<ushort> value, System.Runtime.Intrinsics.Vector128<ushort> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftLeftLogical(System.Runtime.Intrinsics.Vector128<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftLeftLogical(System.Runtime.Intrinsics.Vector128<uint> value, System.Runtime.Intrinsics.Vector128<uint> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftLeftLogical(System.Runtime.Intrinsics.Vector128<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftLeftLogical(System.Runtime.Intrinsics.Vector128<ulong> value, System.Runtime.Intrinsics.Vector128<ulong> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ShiftLeftLogical128BitLane(System.Runtime.Intrinsics.Vector128<byte> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftLeftLogical128BitLane(System.Runtime.Intrinsics.Vector128<short> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftLeftLogical128BitLane(System.Runtime.Intrinsics.Vector128<int> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftLeftLogical128BitLane(System.Runtime.Intrinsics.Vector128<long> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftLeftLogical128BitLane(System.Runtime.Intrinsics.Vector128<sbyte> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftLeftLogical128BitLane(System.Runtime.Intrinsics.Vector128<ushort> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftLeftLogical128BitLane(System.Runtime.Intrinsics.Vector128<uint> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftLeftLogical128BitLane(System.Runtime.Intrinsics.Vector128<ulong> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector128<short> value, System.Runtime.Intrinsics.Vector128<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightArithmetic(System.Runtime.Intrinsics.Vector128<int> value, System.Runtime.Intrinsics.Vector128<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<short> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<short> value, System.Runtime.Intrinsics.Vector128<short> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<int> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<int> value, System.Runtime.Intrinsics.Vector128<int> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<long> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<long> value, System.Runtime.Intrinsics.Vector128<long> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<ushort> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<ushort> value, System.Runtime.Intrinsics.Vector128<ushort> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<uint> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<uint> value, System.Runtime.Intrinsics.Vector128<uint> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<ulong> value, byte count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftRightLogical(System.Runtime.Intrinsics.Vector128<ulong> value, System.Runtime.Intrinsics.Vector128<ulong> count) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> ShiftRightLogical128BitLane(System.Runtime.Intrinsics.Vector128<byte> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShiftRightLogical128BitLane(System.Runtime.Intrinsics.Vector128<short> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ShiftRightLogical128BitLane(System.Runtime.Intrinsics.Vector128<int> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ShiftRightLogical128BitLane(System.Runtime.Intrinsics.Vector128<long> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> ShiftRightLogical128BitLane(System.Runtime.Intrinsics.Vector128<sbyte> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShiftRightLogical128BitLane(System.Runtime.Intrinsics.Vector128<ushort> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> ShiftRightLogical128BitLane(System.Runtime.Intrinsics.Vector128<uint> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> ShiftRightLogical128BitLane(System.Runtime.Intrinsics.Vector128<ulong> value, byte numBytes) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Shuffle(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Shuffle(System.Runtime.Intrinsics.Vector128<int> value, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> Shuffle(System.Runtime.Intrinsics.Vector128<uint> value, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShuffleHigh(System.Runtime.Intrinsics.Vector128<short> value, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShuffleHigh(System.Runtime.Intrinsics.Vector128<ushort> value, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ShuffleLow(System.Runtime.Intrinsics.Vector128<short> value, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> ShuffleLow(System.Runtime.Intrinsics.Vector128<ushort> value, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Sqrt(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> SqrtScalar(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> SqrtScalar(System.Runtime.Intrinsics.Vector128<double> upper, System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public unsafe static void Store(byte* address, System.Runtime.Intrinsics.Vector128<byte> source) { }
        public unsafe static void Store(double* address, System.Runtime.Intrinsics.Vector128<double> source) { }
        public unsafe static void Store(short* address, System.Runtime.Intrinsics.Vector128<short> source) { }
        public unsafe static void Store(int* address, System.Runtime.Intrinsics.Vector128<int> source) { }
        public unsafe static void Store(long* address, System.Runtime.Intrinsics.Vector128<long> source) { }
        public unsafe static void Store(sbyte* address, System.Runtime.Intrinsics.Vector128<sbyte> source) { }
        public unsafe static void Store(ushort* address, System.Runtime.Intrinsics.Vector128<ushort> source) { }
        public unsafe static void Store(uint* address, System.Runtime.Intrinsics.Vector128<uint> source) { }
        public unsafe static void Store(ulong* address, System.Runtime.Intrinsics.Vector128<ulong> source) { }
        public unsafe static void StoreAligned(byte* address, System.Runtime.Intrinsics.Vector128<byte> source) { }
        public unsafe static void StoreAligned(double* address, System.Runtime.Intrinsics.Vector128<double> source) { }
        public unsafe static void StoreAligned(short* address, System.Runtime.Intrinsics.Vector128<short> source) { }
        public unsafe static void StoreAligned(int* address, System.Runtime.Intrinsics.Vector128<int> source) { }
        public unsafe static void StoreAligned(long* address, System.Runtime.Intrinsics.Vector128<long> source) { }
        public unsafe static void StoreAligned(sbyte* address, System.Runtime.Intrinsics.Vector128<sbyte> source) { }
        public unsafe static void StoreAligned(ushort* address, System.Runtime.Intrinsics.Vector128<ushort> source) { }
        public unsafe static void StoreAligned(uint* address, System.Runtime.Intrinsics.Vector128<uint> source) { }
        public unsafe static void StoreAligned(ulong* address, System.Runtime.Intrinsics.Vector128<ulong> source) { }
        public unsafe static void StoreAlignedNonTemporal(byte* address, System.Runtime.Intrinsics.Vector128<byte> source) { }
        public unsafe static void StoreAlignedNonTemporal(double* address, System.Runtime.Intrinsics.Vector128<double> source) { }
        public unsafe static void StoreAlignedNonTemporal(short* address, System.Runtime.Intrinsics.Vector128<short> source) { }
        public unsafe static void StoreAlignedNonTemporal(int* address, System.Runtime.Intrinsics.Vector128<int> source) { }
        public unsafe static void StoreAlignedNonTemporal(long* address, System.Runtime.Intrinsics.Vector128<long> source) { }
        public unsafe static void StoreAlignedNonTemporal(sbyte* address, System.Runtime.Intrinsics.Vector128<sbyte> source) { }
        public unsafe static void StoreAlignedNonTemporal(ushort* address, System.Runtime.Intrinsics.Vector128<ushort> source) { }
        public unsafe static void StoreAlignedNonTemporal(uint* address, System.Runtime.Intrinsics.Vector128<uint> source) { }
        public unsafe static void StoreAlignedNonTemporal(ulong* address, System.Runtime.Intrinsics.Vector128<ulong> source) { }
        public unsafe static void StoreHigh(double* address, System.Runtime.Intrinsics.Vector128<double> source) { }
        public unsafe static void StoreLow(double* address, System.Runtime.Intrinsics.Vector128<double> source) { }
        public unsafe static void StoreNonTemporal(int* address, int value) { }
        public unsafe static void StoreNonTemporal(uint* address, uint value) { }
        public unsafe static void StoreScalar(double* address, System.Runtime.Intrinsics.Vector128<double> source) { }
        public unsafe static void StoreScalar(int* address, System.Runtime.Intrinsics.Vector128<int> source) { }
        public unsafe static void StoreScalar(long* address, System.Runtime.Intrinsics.Vector128<long> source) { }
        public unsafe static void StoreScalar(uint* address, System.Runtime.Intrinsics.Vector128<uint> source) { }
        public unsafe static void StoreScalar(ulong* address, System.Runtime.Intrinsics.Vector128<ulong> source) { }
        public static System.Runtime.Intrinsics.Vector128<byte> Subtract(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Subtract(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> Subtract(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Subtract(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> Subtract(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> Subtract(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> Subtract(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> Subtract(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> Subtract(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> SubtractSaturate(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> SubtractSaturate(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> SubtractSaturate(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> SubtractSaturate(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> SubtractScalar(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> SumAbsoluteDifferences(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> UnpackHigh(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> UnpackHigh(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> UnpackHigh(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> UnpackHigh(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> UnpackHigh(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> UnpackHigh(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> UnpackHigh(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> UnpackHigh(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> UnpackHigh(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> UnpackLow(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> UnpackLow(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> UnpackLow(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> UnpackLow(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> UnpackLow(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> UnpackLow(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> UnpackLow(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> UnpackLow(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> UnpackLow(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Xor(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Xor(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> Xor(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Xor(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> Xor(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> Xor(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> Xor(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> Xor(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> Xor(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public new abstract partial class X64 : System.Runtime.Intrinsics.X86.Sse.X64
        {
            internal X64() { }
            public static new bool IsSupported { get { throw null; } }
            public static System.Runtime.Intrinsics.Vector128<double> ConvertScalarToVector128Double(System.Runtime.Intrinsics.Vector128<double> upper, long value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> ConvertScalarToVector128Int64(long value) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> ConvertScalarToVector128UInt64(ulong value) { throw null; }
            public static long ConvertToInt64(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static long ConvertToInt64(System.Runtime.Intrinsics.Vector128<long> value) { throw null; }
            public static long ConvertToInt64WithTruncation(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
            public static ulong ConvertToUInt64(System.Runtime.Intrinsics.Vector128<ulong> value) { throw null; }
            public unsafe static void StoreNonTemporal(long* address, long value) { }
            public unsafe static void StoreNonTemporal(ulong* address, ulong value) { }
        }
    }
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Sse3 : System.Runtime.Intrinsics.X86.Sse2
    {
        internal Sse3() { }
        public static new bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<double> AddSubtract(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> AddSubtract(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> HorizontalAdd(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> HorizontalAdd(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> HorizontalSubtract(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> HorizontalSubtract(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<double> LoadAndDuplicateToVector128(double* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<byte> LoadDquVector128(byte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<short> LoadDquVector128(short* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<int> LoadDquVector128(int* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<long> LoadDquVector128(long* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<sbyte> LoadDquVector128(sbyte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<ushort> LoadDquVector128(ushort* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<uint> LoadDquVector128(uint* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<ulong> LoadDquVector128(ulong* address) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> MoveAndDuplicate(System.Runtime.Intrinsics.Vector128<double> source) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> MoveHighAndDuplicate(System.Runtime.Intrinsics.Vector128<float> source) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> MoveLowAndDuplicate(System.Runtime.Intrinsics.Vector128<float> source) { throw null; }
        public new abstract partial class X64 : System.Runtime.Intrinsics.X86.Sse2.X64
        {
            internal X64() { }
            public static new bool IsSupported { get { throw null; } }
        }
    }
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Sse41 : System.Runtime.Intrinsics.X86.Ssse3
    {
        internal Sse41() { }
        public static new bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<double> Blend(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> Blend(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Blend(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> Blend(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> BlendVariable(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right, System.Runtime.Intrinsics.Vector128<byte> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> BlendVariable(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right, System.Runtime.Intrinsics.Vector128<double> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> BlendVariable(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right, System.Runtime.Intrinsics.Vector128<short> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> BlendVariable(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right, System.Runtime.Intrinsics.Vector128<int> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> BlendVariable(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right, System.Runtime.Intrinsics.Vector128<long> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> BlendVariable(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right, System.Runtime.Intrinsics.Vector128<sbyte> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> BlendVariable(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right, System.Runtime.Intrinsics.Vector128<float> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> BlendVariable(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right, System.Runtime.Intrinsics.Vector128<ushort> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> BlendVariable(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right, System.Runtime.Intrinsics.Vector128<uint> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> BlendVariable(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right, System.Runtime.Intrinsics.Vector128<ulong> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Ceiling(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Ceiling(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CeilingScalar(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> CeilingScalar(System.Runtime.Intrinsics.Vector128<double> upper, System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CeilingScalar(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> CeilingScalar(System.Runtime.Intrinsics.Vector128<float> upper, System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> CompareEqual(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> CompareEqual(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<short> ConvertToVector128Int16(byte* address) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ConvertToVector128Int16(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> ConvertToVector128Int16(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<short> ConvertToVector128Int16(sbyte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<int> ConvertToVector128Int32(byte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<int> ConvertToVector128Int32(short* address) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ConvertToVector128Int32(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ConvertToVector128Int32(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ConvertToVector128Int32(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> ConvertToVector128Int32(System.Runtime.Intrinsics.Vector128<ushort> value) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<int> ConvertToVector128Int32(sbyte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<int> ConvertToVector128Int32(ushort* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<long> ConvertToVector128Int64(byte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<long> ConvertToVector128Int64(short* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<long> ConvertToVector128Int64(int* address) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ConvertToVector128Int64(System.Runtime.Intrinsics.Vector128<byte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ConvertToVector128Int64(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ConvertToVector128Int64(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ConvertToVector128Int64(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ConvertToVector128Int64(System.Runtime.Intrinsics.Vector128<ushort> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> ConvertToVector128Int64(System.Runtime.Intrinsics.Vector128<uint> value) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<long> ConvertToVector128Int64(sbyte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<long> ConvertToVector128Int64(ushort* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<long> ConvertToVector128Int64(uint* address) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> DotProduct(System.Runtime.Intrinsics.Vector128<double> left, System.Runtime.Intrinsics.Vector128<double> right, byte control) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> DotProduct(System.Runtime.Intrinsics.Vector128<float> left, System.Runtime.Intrinsics.Vector128<float> right, byte control) { throw null; }
        public static byte Extract(System.Runtime.Intrinsics.Vector128<byte> value, byte index) { throw null; }
        public static int Extract(System.Runtime.Intrinsics.Vector128<int> value, byte index) { throw null; }
        public static float Extract(System.Runtime.Intrinsics.Vector128<float> value, byte index) { throw null; }
        public static uint Extract(System.Runtime.Intrinsics.Vector128<uint> value, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> Floor(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Floor(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> FloorScalar(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> FloorScalar(System.Runtime.Intrinsics.Vector128<double> upper, System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> FloorScalar(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> FloorScalar(System.Runtime.Intrinsics.Vector128<float> upper, System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Insert(System.Runtime.Intrinsics.Vector128<byte> value, byte data, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Insert(System.Runtime.Intrinsics.Vector128<int> value, int data, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> Insert(System.Runtime.Intrinsics.Vector128<sbyte> value, sbyte data, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> Insert(System.Runtime.Intrinsics.Vector128<float> value, System.Runtime.Intrinsics.Vector128<float> data, byte index) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> Insert(System.Runtime.Intrinsics.Vector128<uint> value, uint data, byte index) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<byte> LoadAlignedVector128NonTemporal(byte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<short> LoadAlignedVector128NonTemporal(short* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<int> LoadAlignedVector128NonTemporal(int* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<long> LoadAlignedVector128NonTemporal(long* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<sbyte> LoadAlignedVector128NonTemporal(sbyte* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<ushort> LoadAlignedVector128NonTemporal(ushort* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<uint> LoadAlignedVector128NonTemporal(uint* address) { throw null; }
        public unsafe static System.Runtime.Intrinsics.Vector128<ulong> LoadAlignedVector128NonTemporal(ulong* address) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Max(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> Max(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> Max(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> Max(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Min(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> Min(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> Min(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> Min(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> MinHorizontal(System.Runtime.Intrinsics.Vector128<ushort> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> MultipleSumAbsoluteDifferences(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right, byte mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> Multiply(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> MultiplyLow(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> MultiplyLow(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> PackUnsignedSaturate(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> RoundCurrentDirection(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> RoundCurrentDirection(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> RoundCurrentDirectionScalar(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> RoundCurrentDirectionScalar(System.Runtime.Intrinsics.Vector128<double> upper, System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> RoundCurrentDirectionScalar(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> RoundCurrentDirectionScalar(System.Runtime.Intrinsics.Vector128<float> upper, System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> RoundToNearestInteger(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> RoundToNearestInteger(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> RoundToNearestIntegerScalar(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> RoundToNearestIntegerScalar(System.Runtime.Intrinsics.Vector128<double> upper, System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> RoundToNearestIntegerScalar(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> RoundToNearestIntegerScalar(System.Runtime.Intrinsics.Vector128<float> upper, System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> RoundToNegativeInfinity(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> RoundToNegativeInfinity(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> RoundToNegativeInfinityScalar(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> RoundToNegativeInfinityScalar(System.Runtime.Intrinsics.Vector128<double> upper, System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> RoundToNegativeInfinityScalar(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> RoundToNegativeInfinityScalar(System.Runtime.Intrinsics.Vector128<float> upper, System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> RoundToPositiveInfinity(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> RoundToPositiveInfinity(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> RoundToPositiveInfinityScalar(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> RoundToPositiveInfinityScalar(System.Runtime.Intrinsics.Vector128<double> upper, System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> RoundToPositiveInfinityScalar(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> RoundToPositiveInfinityScalar(System.Runtime.Intrinsics.Vector128<float> upper, System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> RoundToZero(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> RoundToZero(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> RoundToZeroScalar(System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<double> RoundToZeroScalar(System.Runtime.Intrinsics.Vector128<double> upper, System.Runtime.Intrinsics.Vector128<double> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> RoundToZeroScalar(System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<float> RoundToZeroScalar(System.Runtime.Intrinsics.Vector128<float> upper, System.Runtime.Intrinsics.Vector128<float> value) { throw null; }
        public static bool TestC(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static bool TestC(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static bool TestC(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static bool TestC(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static bool TestC(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static bool TestC(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static bool TestC(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static bool TestC(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static bool TestNotZAndNotC(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static bool TestNotZAndNotC(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static bool TestNotZAndNotC(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static bool TestNotZAndNotC(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static bool TestNotZAndNotC(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static bool TestNotZAndNotC(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static bool TestNotZAndNotC(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static bool TestNotZAndNotC(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public static bool TestZ(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right) { throw null; }
        public static bool TestZ(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static bool TestZ(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static bool TestZ(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static bool TestZ(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static bool TestZ(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right) { throw null; }
        public static bool TestZ(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right) { throw null; }
        public static bool TestZ(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right) { throw null; }
        public new abstract partial class X64 : System.Runtime.Intrinsics.X86.Ssse3.X64
        {
            internal X64() { }
            public static new bool IsSupported { get { throw null; } }
            public static long Extract(System.Runtime.Intrinsics.Vector128<long> value, byte index) { throw null; }
            public static ulong Extract(System.Runtime.Intrinsics.Vector128<ulong> value, byte index) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<long> Insert(System.Runtime.Intrinsics.Vector128<long> value, long data, byte index) { throw null; }
            public static System.Runtime.Intrinsics.Vector128<ulong> Insert(System.Runtime.Intrinsics.Vector128<ulong> value, ulong data, byte index) { throw null; }
        }
    }
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Sse42 : System.Runtime.Intrinsics.X86.Sse41
    {
        internal Sse42() { }
        public static new bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<long> CompareGreaterThan(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right) { throw null; }
        public static uint Crc32(uint crc, byte data) { throw null; }
        public static uint Crc32(uint crc, ushort data) { throw null; }
        public static uint Crc32(uint crc, uint data) { throw null; }
        public new abstract partial class X64 : System.Runtime.Intrinsics.X86.Sse41.X64
        {
            internal X64() { }
            public static new bool IsSupported { get { throw null; } }
            public static ulong Crc32(ulong crc, ulong data) { throw null; }
        }
    }
    [System.CLSCompliantAttribute(false)]
    public abstract partial class Ssse3 : System.Runtime.Intrinsics.X86.Sse3
    {
        internal Ssse3() { }
        public static new bool IsSupported { get { throw null; } }
        public static System.Runtime.Intrinsics.Vector128<ushort> Abs(System.Runtime.Intrinsics.Vector128<short> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> Abs(System.Runtime.Intrinsics.Vector128<int> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Abs(System.Runtime.Intrinsics.Vector128<sbyte> value) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> AlignRight(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<byte> right, byte mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> AlignRight(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right, byte mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> AlignRight(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right, byte mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<long> AlignRight(System.Runtime.Intrinsics.Vector128<long> left, System.Runtime.Intrinsics.Vector128<long> right, byte mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> AlignRight(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right, byte mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ushort> AlignRight(System.Runtime.Intrinsics.Vector128<ushort> left, System.Runtime.Intrinsics.Vector128<ushort> right, byte mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<uint> AlignRight(System.Runtime.Intrinsics.Vector128<uint> left, System.Runtime.Intrinsics.Vector128<uint> right, byte mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<ulong> AlignRight(System.Runtime.Intrinsics.Vector128<ulong> left, System.Runtime.Intrinsics.Vector128<ulong> right, byte mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> HorizontalAdd(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> HorizontalAdd(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> HorizontalAddSaturate(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> HorizontalSubtract(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> HorizontalSubtract(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> HorizontalSubtractSaturate(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyAddAdjacent(System.Runtime.Intrinsics.Vector128<byte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> MultiplyHighRoundScale(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<byte> Shuffle(System.Runtime.Intrinsics.Vector128<byte> value, System.Runtime.Intrinsics.Vector128<byte> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> Shuffle(System.Runtime.Intrinsics.Vector128<sbyte> value, System.Runtime.Intrinsics.Vector128<sbyte> mask) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<short> Sign(System.Runtime.Intrinsics.Vector128<short> left, System.Runtime.Intrinsics.Vector128<short> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<int> Sign(System.Runtime.Intrinsics.Vector128<int> left, System.Runtime.Intrinsics.Vector128<int> right) { throw null; }
        public static System.Runtime.Intrinsics.Vector128<sbyte> Sign(System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right) { throw null; }
        public new abstract partial class X64 : System.Runtime.Intrinsics.X86.Sse3.X64
        {
            internal X64() { }
            public static new bool IsSupported { get { throw null; } }
        }
    }
    public abstract partial class X86Base
    {
        internal X86Base() { }
        public static bool IsSupported { get { throw null; } }
        public static (int Eax, int Ebx, int Ecx, int Edx) CpuId(int functionId, int subFunctionId) { throw null; }
        public static void Pause() { throw null; }
        public abstract partial class X64
        {
            internal X64() { }
            public static bool IsSupported { get { throw null; } }
        }
    }

    [System.CLSCompliantAttribute(false)]
    public abstract partial class X86Serialize : System.Runtime.Intrinsics.X86.X86Base
    {
        internal X86Serialize() { }
        public static new bool IsSupported { get { throw null; } }

        public static void Serialize() { throw null; }
        public new abstract partial class X64 : System.Runtime.Intrinsics.X86.X86Base.X64
        {
            internal X64() { }
            public static new bool IsSupported { get { throw null; } }
        }
    }
}
