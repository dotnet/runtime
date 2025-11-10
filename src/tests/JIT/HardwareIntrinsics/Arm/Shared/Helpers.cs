// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file is auto-generated from template file Helpers.tt
// In order to make changes to this file, please update Helpers.tt
// and run the following command from Developer Command Prompt for Visual Studio
//   "%DevEnvDir%\TextTransform.exe" .\Helpers.tt

using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Reflection;

namespace JIT.HardwareIntrinsics.Arm
{
    static class Helpers
    {
        public static Vector<T> InitVector<T>(Func<int, T> f)
        {
            var count = Vector<T>.Count;
            var arr = new T[count];
            for (var i = 0; i < count; i++)
            {
                arr[i] = f(i);
            }
            return new Vector<T>(arr);
        }

        public static T[] ConvertVectorToMask<T>(T[] vector) where T : IBinaryInteger<T>
        {
            T[] result = new T[vector.Length];
            for (int i = 0; i < vector.Length; i++)
            {
                result[i] = vector[i] == T.Zero ? T.Zero : T.One;
            }
            return result;
        }

        public static T[] CreateMaskForFirstActiveElement<T>(T[] mask, T[] srcMask)
            where T : unmanaged, IBinaryInteger<T>
        {
            int count = srcMask.Length;
            T[] result = ConvertVectorToMask(srcMask);

            for (int i = 0; i < count; i++)
            {
                if (mask[i] != T.Zero)
                {
                    result[i] = T.One;
                    return result;
                }
            }

            return result;
        }

        public static int LastActiveElement<T>(T[] v) where T : INumber<T>
        {
            for (int i = v.Length - 1; i >= 0; i--)
            {
                if (v[i] != T.Zero)
                {
                    return i;
                }
            }
            return -1;
        }

        public static T[] CreateMaskForNextActiveElement<T>(T[] mask, T[] srcMask) where T : INumber<T>
        {
            int count = srcMask.Length;
            T[] result = new T[count];
            int start = LastActiveElement(srcMask) + 1;

            for (int i = start; i < count; i++)
            {
                if (mask[i] != T.Zero)
                {
                    result[i] = T.One;
                    return result;
                }
            }

            return result;
        }

        public static T CountLeadingSignBits<T>(T value)
            where T : unmanaged,
                    INumber<T>,
                    IBitwiseOperators<T, T, T>,
                    IShiftOperators<T, int, T>
        {
            T shifted = value >> 1;
            T xor = value ^ shifted;

            return CountLeadingZeroBits(xor) - T.One;
        }

        public static unsafe T CountLeadingZeroBits<T>(T value)
            where T : unmanaged, INumber<T>, IBitwiseOperators<T, T, T>
        {
            int bitSize = sizeof(T) * 8;
            int highest = HighestSetBit(value);

            return T.CreateChecked(bitSize - (highest + 1));
        }

        public static unsafe int HighestSetBit<T>(T value)
            where T : unmanaged, INumber<T>, IBitwiseOperators<T, T, T>
        {
            ulong v = Convert.ToUInt64(value);

            for (int i = sizeof(T) * 8 - 1; i >= 0; i--)
            {
                if ((v & (1UL << i)) != 0)
                {
                    return i;
                }
            }

            return -1;
        }

        public static T BitCount<T>(T value)
            where T : unmanaged, INumber<T>, IBitwiseOperators<T, T, T>
        {
            ulong bits = Convert.ToUInt64(value);
            int count = 0;

            for (int i = 0; i < sizeof(ulong) * 8; i++)
            {
                if ((bits & (1UL << i)) != 0)
                    count++;
            }

            return T.CreateChecked(count);
        }
        public static int BitCount(float op1) => BitCount(BitConverter.SingleToInt32Bits(op1));

        public static long BitCount(double op1) => BitCount(BitConverter.DoubleToInt64Bits(op1));


        public static T ReverseElementBits<T>(T value)
            where T : unmanaged,
                    INumber<T>,
                    IBitwiseOperators<T, T, T>,
                    IShiftOperators<T, int, T>,
                    IComparisonOperators<T, T, bool>
        {
            T result = T.Zero;
            T one = T.One;
            int bitSize = Unsafe.SizeOf<T>() * 8;

            for (int i = 0; i < bitSize; i++)
            {
                if ((value & (one << i)) != T.Zero)
                {
                    result |= (one << (bitSize - 1 - i));
                }
            }

            return result;
        }

        public static T And<T>(T op1, T op2)
            where T : unmanaged, IBitwiseOperators<T, T, T>
            => op1 & op2;

        public static T BitwiseClear<T>(T op1, T op2)
            where T : unmanaged, IBitwiseOperators<T, T, T>
            => op1 & ~op2;

        public static T BitwiseSelect<T>(T op1, T op2, T op3)
            where T : unmanaged,
                    INumber<T>,
                    IBitwiseOperators<T, T, T>,
                    IShiftOperators<T, int, T>,
                    IComparisonOperators<T, T, bool>
        {
            T result = T.Zero;
            T one = T.One;
            int bitSize = Unsafe.SizeOf<T>() * 8;

            for (int i = 0; i < bitSize; i++)
            {
                T bit = one << i;
                result |= ((op1 & bit) != T.Zero)
                    ? (op2 & bit)
                    : (op3 & bit);
            }

            return result;
        }

        public static T Not<T>(T op1)
            where T : unmanaged, IBitwiseOperators<T, T, T>
            => ~op1;

        public static T Or<T>(T op1, T op2)
            where T : unmanaged, IBitwiseOperators<T, T, T>
            => op1 | op2;

        public static T OrNot<T>(T op1, T op2)
            where T : unmanaged, IBitwiseOperators<T, T, T>
            => op1 | ~op2;

        public static T Xor<T>(T op1, T op2)
            where T : unmanaged, IBitwiseOperators<T, T, T>
            => op1 ^ op2;

        public static float And(float op1, float op2) => BitConverter.Int32BitsToSingle(And(BitConverter.SingleToInt32Bits(op1), BitConverter.SingleToInt32Bits(op2)));

        public static double And(double op1, double op2) => BitConverter.Int64BitsToDouble(And(BitConverter.DoubleToInt64Bits(op1), BitConverter.DoubleToInt64Bits(op2)));

        public static float BitwiseClear(float op1, float op2) => BitConverter.Int32BitsToSingle(BitwiseClear(BitConverter.SingleToInt32Bits(op1), BitConverter.SingleToInt32Bits(op2)));

        public static double BitwiseClear(double op1, double op2) => BitConverter.Int64BitsToDouble(BitwiseClear(BitConverter.DoubleToInt64Bits(op1), BitConverter.DoubleToInt64Bits(op2)));

        public static float BitwiseSelect(float op1, float op2, float op3) => BitConverter.Int32BitsToSingle(BitwiseSelect(BitConverter.SingleToInt32Bits(op1), BitConverter.SingleToInt32Bits(op2), BitConverter.SingleToInt32Bits(op3)));

        public static double BitwiseSelect(double op1, double op2, double op3) => BitConverter.Int64BitsToDouble(BitwiseSelect(BitConverter.DoubleToInt64Bits(op1), BitConverter.DoubleToInt64Bits(op2), BitConverter.DoubleToInt64Bits(op3)));

        public static float Not(float op1) => BitConverter.Int32BitsToSingle(~BitConverter.SingleToInt32Bits(op1));

        public static double Not(double op1) => BitConverter.Int64BitsToDouble(~BitConverter.DoubleToInt64Bits(op1));

        public static float Or(float op1, float op2) => BitConverter.Int32BitsToSingle(Or(BitConverter.SingleToInt32Bits(op1), BitConverter.SingleToInt32Bits(op2)));

        public static double Or(double op1, double op2) => BitConverter.Int64BitsToDouble(Or(BitConverter.DoubleToInt64Bits(op1), BitConverter.DoubleToInt64Bits(op2)));

        public static float OrNot(float op1, float op2) => BitConverter.Int32BitsToSingle(OrNot(BitConverter.SingleToInt32Bits(op1), BitConverter.SingleToInt32Bits(op2)));

        public static double OrNot(double op1, double op2) => BitConverter.Int64BitsToDouble(OrNot(BitConverter.DoubleToInt64Bits(op1), BitConverter.DoubleToInt64Bits(op2)));

        public static float Xor(float op1, float op2) => BitConverter.Int32BitsToSingle(Xor(BitConverter.SingleToInt32Bits(op1), BitConverter.SingleToInt32Bits(op2)));

        public static double Xor(double op1, double op2) => BitConverter.Int64BitsToDouble(Xor(BitConverter.DoubleToInt64Bits(op1), BitConverter.DoubleToInt64Bits(op2)));

        public static T CompareEqual<T>(T left, T right)
            where T : unmanaged, IBinaryNumber<T>
            => left == right ? T.AllBitsSet : T.Zero;

        public static T CompareGreaterThan<T>(T left, T right)
            where T : unmanaged, IBinaryNumber<T>
            => left > right ? T.AllBitsSet : T.Zero;

        public static T CompareGreaterThanOrEqual<T>(T left, T right)
            where T : unmanaged, IBinaryNumber<T>
            => left >= right ? T.AllBitsSet : T.Zero;

        public static T CompareLessThan<T>(T left, T right)
            where T : unmanaged, IBinaryNumber<T>
            => left < right ? T.AllBitsSet : T.Zero;

        public static T CompareLessThanOrEqual<T>(T left, T right)
            where T : unmanaged, IBinaryNumber<T>
            => left <= right ? T.AllBitsSet : T.Zero;

        public static T CompareTest<T>(T left, T right)
            where T : unmanaged, IBinaryNumber<T>
            => (left & right) != T.Zero ? T.AllBitsSet : T.Zero;

        public static double CompareEqual(double left, double right)
        {
            long result = 0;

            if (left == right)
            {
                result = -1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float CompareEqual(float left, float right)
        {
            int result = 0;

            if (left == right)
            {
                result = -1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double CompareGreaterThan(double left, double right)
        {
            long result = 0;

            if (left > right)
            {
                result = -1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float CompareGreaterThan(float left, float right)
        {
            int result = 0;

            if (left > right)
            {
                result = -1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double CompareGreaterThanOrEqual(double left, double right)
        {
            long result = 0;

            if (left >= right)
            {
                result = -1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float CompareGreaterThanOrEqual(float left, float right)
        {
            int result = 0;

            if (left >= right)
            {
                result = -1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double CompareLessThan(double left, double right)
        {
            long result = 0;

            if (left < right)
            {
                result = -1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float CompareLessThan(float left, float right)
        {
            int result = 0;

            if (left < right)
            {
                result = -1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double CompareLessThanOrEqual(double left, double right)
        {
            long result = 0;

            if (left <= right)
            {
                result = -1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float CompareLessThanOrEqual(float left, float right)
        {
            int result = 0;

            if (left <= right)
            {
                result = -1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double CompareTest(double left, double right)
        {
            long result = 0;

            if ((BitConverter.DoubleToInt64Bits(left) & BitConverter.DoubleToInt64Bits(right)) != 0)
            {
                result = -1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float CompareTest(float left, float right)
        {
            int result = 0;

            if ((BitConverter.SingleToInt32Bits(left) & BitConverter.SingleToInt32Bits(right)) != 0)
            {
                result = -1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double AbsoluteCompareGreaterThan(double left, double right)
        {
            long result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left > right)
            {
                result = -1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float AbsoluteCompareGreaterThan(float left, float right)
        {
            int result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left > right)
            {
                result = -1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double AbsoluteCompareGreaterThanOrEqual(double left, double right)
        {
            long result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left >= right)
            {
                result = -1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float AbsoluteCompareGreaterThanOrEqual(float left, float right)
        {
            int result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left >= right)
            {
                result = -1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double AbsoluteCompareLessThan(double left, double right)
        {
            long result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left < right)
            {
                result = -1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float AbsoluteCompareLessThan(float left, float right)
        {
            int result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left < right)
            {
                result = -1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double AbsoluteCompareLessThanOrEqual(double left, double right)
        {
            long result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left <= right)
            {
                result = -1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float AbsoluteCompareLessThanOrEqual(float left, float right)
        {
            int result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left <= right)
            {
                result = -1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }


        public static double SveAbsoluteCompareGreaterThan(double left, double right)
        {
            long result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left > right)
            {
                result = 1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float SveAbsoluteCompareGreaterThan(float left, float right)
        {
            int result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left > right)
            {
                result = 1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double SveAbsoluteCompareGreaterThanOrEqual(double left, double right)
        {
            long result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left >= right)
            {
                result = 1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float SveAbsoluteCompareGreaterThanOrEqual(float left, float right)
        {
            int result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left >= right)
            {
                result = 1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double SveAbsoluteCompareLessThan(double left, double right)
        {
            long result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left < right)
            {
                result = 1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float SveAbsoluteCompareLessThan(float left, float right)
        {
            int result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left < right)
            {
                result = 1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double SveAbsoluteCompareLessThanOrEqual(double left, double right)
        {
            long result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left <= right)
            {
                result = 1;
            }

            return BitConverter.Int64BitsToDouble(result);
        }

        public static float SveAbsoluteCompareLessThanOrEqual(float left, float right)
        {
            int result = 0;

            left = Math.Abs(left);
            right = Math.Abs(right);

            if (left <= right)
            {
                result = 1;
            }

            return BitConverter.Int32BitsToSingle(result);
        }

        public static double SveCompareEqual(double left, double right) => BitConverter.Int64BitsToDouble((left == right) ? 1 : 0);
        public static float SveCompareEqual(float left, float right) => BitConverter.Int32BitsToSingle((left == right) ? 1 : 0);
        public static T SveCompareEqual<T>(T left, T right)
            where T : INumber<T>, IEqualityOperators<T, T, bool>
            => left == right ? T.One : T.Zero;

        public static double SveCompareNotEqual(double left, double right) => BitConverter.Int64BitsToDouble((left != right) ? 1 : 0);
        public static float SveCompareNotEqual(float left, float right) => BitConverter.Int32BitsToSingle((left != right) ? 1 : 0);
        public static T SveCompareNotEqual<T>(T left, T right)
            where T : INumber<T>, IEqualityOperators<T, T, bool>
            => left != right ? T.One : T.Zero;

        public static double SveCompareGreaterThan(double left, double right) => BitConverter.Int64BitsToDouble((left > right) ? 1 : 0);
        public static float SveCompareGreaterThan(float left, float right) => BitConverter.Int32BitsToSingle((left > right) ? 1 : 0);
        public static T SveCompareGreaterThan<T>(T left, T right)
            where T : INumber<T>, IComparisonOperators<T, T, bool>
            => left > right ? T.One : T.Zero;

        public static double SveCompareGreaterThanOrEqual(double left, double right) => BitConverter.Int64BitsToDouble((left >= right) ? 1 : 0);
        public static float SveCompareGreaterThanOrEqual(float left, float right) => BitConverter.Int32BitsToSingle((left >= right) ? 1 : 0);
        public static T SveCompareGreaterThanOrEqual<T>(T left, T right)
            where T : INumber<T>, IComparisonOperators<T, T, bool>
            => left >= right ? T.One : T.Zero;

        public static double SveCompareLessThan(double left, double right) => BitConverter.Int64BitsToDouble((left < right) ? 1 : 0);
        public static float SveCompareLessThan(float left, float right) => BitConverter.Int32BitsToSingle((left < right) ? 1 : 0);
        public static T SveCompareLessThan<T>(T left, T right)
            where T : INumber<T>, IComparisonOperators<T, T, bool>
            => left < right ? T.One : T.Zero;

        public static double SveCompareLessThanOrEqual(double left, double right) => BitConverter.Int64BitsToDouble((left <= right) ? 1 : 0);
        public static float SveCompareLessThanOrEqual(float left, float right) => BitConverter.Int32BitsToSingle((left <= right) ? 1 : 0);
        public static T SveCompareLessThanOrEqual<T>(T left, T right)
            where T : INumber<T>, IComparisonOperators<T, T, bool>
            => left <= right ? T.One : T.Zero;

        public static double SveCompareUnordered(double left, double right) => BitConverter.Int64BitsToDouble((double.IsNaN(left) || double.IsNaN(right)) ? 1 : 0);
        public static float SveCompareUnordered(float left, float right) => BitConverter.Int32BitsToSingle((float.IsNaN(left) || float.IsNaN(right)) ? 1 : 0);

        public static byte Abs(sbyte value) => value < 0 ? (byte)-value : (byte)value;

        public static ushort Abs(short value) => value < 0 ? (ushort)-value : (ushort)value;

        public static uint Abs(int value) => value < 0 ? (uint)-value : (uint)value;

        public static ulong Abs(long value) => value < 0 ? (ulong)-value : (ulong)value;

        public static float Abs(float value) => Math.Abs(value);

        public static double Abs(double value) => Math.Abs(value);

        public static float Divide(float op1, float op2) => op1 / op2;

        public static double Divide(double op1, double op2) => op1 / op2;

        public static float Scale(float op1, int op2) => op1 * MathF.Pow((float)2.0, op2);

        public static double Scale(double op1, long op2) => op1 * Math.Pow(2.0, op2);

        public static float Sqrt(float value) => MathF.Sqrt(value);

        public static double Sqrt(double value) => Math.Sqrt(value);

        public static T AbsoluteDifference<T>(T op1, T op2)
            where T : unmanaged, INumber<T>, IComparisonOperators<T, T, bool>
        {
            return op1 < op2 ? op2 - op1 : op1 - op2;
        }

        public static T AbsoluteDifferenceAdd<T>(T op1, T op2, T op3)
            where T : unmanaged, INumber<T>, IComparisonOperators<T, T, bool>
        {
            return op1 + AbsoluteDifference(op2, op3);
        }

        public static ushort AbsoluteDifferenceWidening(sbyte op1, sbyte op2) => op1 < op2 ? (ushort)(op2 - op1) : (ushort)(op1 - op2);

        public static ushort AbsoluteDifferenceWideningUpper(sbyte[] op1, sbyte[] op2, int i) => AbsoluteDifferenceWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static short AbsoluteDifferenceWideningAndAdd(short op1, sbyte op2, sbyte op3) => (short)(op1 + (short)AbsoluteDifferenceWidening(op2, op3));

        public static short AbsoluteDifferenceWideningUpperAndAdd(short[] op1, sbyte[] op2, sbyte[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static short AbsoluteDifferenceWideningLowerAndAddEven(short[] op1, sbyte[] op2, sbyte[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i * 2], op3[i * 2]);

        public static short AbsoluteDifferenceWideningLowerAndAddOdd(short[] op1, sbyte[] op2, sbyte[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[(i * 2) + 1], op3[(i * 2) + 1]);

        public static short AbsoluteDifferenceWideningEven(sbyte[] op1, sbyte[] op2, int i) => (short)AbsoluteDifferenceWidening(op1[i * 2], op2[i * 2]);

        public static short AbsoluteDifferenceWideningOdd(sbyte[] op1, sbyte[] op2, int i) => (short)AbsoluteDifferenceWidening(op1[(i * 2) + 1], op2[(i * 2) + 1]);

        public static short AddAcrossWidening(sbyte[] op1) => Reduce<short, sbyte>(AddWidening, op1);

        public static long AddAcrossWideningLong(sbyte[] op1) => Reduce<long, sbyte>(AddWidening, op1);

        public static short AddPairwiseWidening(sbyte[] op1, int i) => AddWidening(op1[2 * i], op1[2 * i + 1]);

        public static short AddPairwiseWideningAndAdd(short[] op1, sbyte[] op2, int i) => (short)(op1[i] + AddWidening(op2[2 * i], op2[2 * i + 1]));

        public static sbyte AddHighNarrowingEven(short[] op1, short[] op2, int i)
        {
            if (i % 2 == 0)
            {
                return (sbyte) ((op1[i / 2] + op2[i / 2]) >> (8 * sizeof(sbyte)));
            }

            return 0;
        }

        public static sbyte AddHighNarrowingOdd(sbyte[] even, short[] op1, short[] op2, int i)
        {
            if (i % 2 == 1)
            {
                return (sbyte) ((op1[(i - 1) / 2] + op2[(i - 1) / 2]) >> (8 * sizeof(sbyte)));
            }

            return even[i];
        }

        public static unsafe U AddHighNarrowing<T, U>(T op1, T op2)
            where U : unmanaged, INumber<U>
            where T : unmanaged, INumber<T>, IShiftOperators<T, int, T>
            => HighNarrowing<T, U>(op1 + op2, round: false);

        public static sbyte AddHighNarrowingUpper(sbyte[] op1, short[] op2, short[] op3, int i) => i < op1.Length ? op1[i] : AddHighNarrowing<short, sbyte>(op2[i - op1.Length], op3[i - op1.Length]);

        public static short AddRoundedHighNarrowingUpper(sbyte[] op1, short[] op2, short[] op3, int i) => i < op1.Length ? op1[i] : AddRoundedHighNarrowing<short, sbyte>(op2[i - op1.Length], op3[i - op1.Length]);

        public static short AddWidening(sbyte op1, sbyte op2) => (short)((short)op1 + (short)op2);

        public static short AddWidening(short op1, sbyte op2) => (short)(op1 + op2);

        public static long AddWidening(long op1, sbyte op2) => (long)(op1 + (long)op2);

        public static short AddWideningUpper(sbyte[] op1, sbyte[] op2, int i) => AddWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static short AddWideningUpper(short[] op1, sbyte[] op2, int i) => AddWidening(op1[i], op2[i + op2.Length / 2]);

        public static T BooleanNot<T>(T value)
            where T : unmanaged, INumber<T>, IComparisonOperators<T, T, bool>
        {
            return value == T.Zero ? T.One : T.Zero;
        }

        public static sbyte ExtractNarrowing(short op1) => (sbyte)op1;

        public static sbyte ExtractNarrowingUpper(sbyte[] op1, short[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowing(op2[i - op1.Length]);

        public static sbyte FusedAddHalving(sbyte op1, sbyte op2) => (sbyte)((ushort)((short)op1 + (short)op2) >> 1);

        public static sbyte FusedAddRoundedHalving(sbyte op1, sbyte op2) => (sbyte)((ushort)((short)op1 + (short)op2 + 1) >> 1);

        public static sbyte FusedSubtractHalving(sbyte op1, sbyte op2) => (sbyte)((ushort)((short)op1 - (short)op2) >> 1);

        public static short MultiplyByScalarWideningUpper(sbyte[] op1, sbyte op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2);

        public static short MultiplyByScalarWideningUpperAndAdd(short[] op1, sbyte[] op2, sbyte op3, int i) => MultiplyAddWidening(op1[i], op2[i + op2.Length / 2], op3);

        public static short MultiplyByScalarWideningUpperAndSubtract(short[] op1, sbyte[] op2, sbyte op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3);

        public static short MultiplyWidening(sbyte op1, sbyte op2) => (short)((short)op1 * (short)op2);

        public static short MultiplyWideningAndSubtract(short op1, sbyte op2, sbyte op3) => (short)(op1 - MultiplyWidening(op2, op3));

        public static short MultiplyWideningUpper(sbyte[] op1, sbyte[] op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static short MultiplyWideningUpperAndAdd(short[] op1, sbyte[] op2, sbyte[] op3, int i) => MultiplyAddWidening(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static short MultiplyWideningUpperAndSubtract(short[] op1, sbyte[] op2, sbyte[] op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static N SubtractHighNarrowing<W, N>(W op1, W op2)
            where W : unmanaged, IBinaryInteger<W>
            where N : unmanaged, IBinaryInteger<N>
            => HighNarrowing<W, N>(op1 - op2, round: false);

        public static N SubtractHighNarrowingUpper<W, N>(N[] op1, W[] op2, W[] op3, int i)
            where W : unmanaged, IBinaryInteger<W>
            where N : unmanaged, IBinaryInteger<N>
        {
            if (i < op1.Length)
            {
                return op1[i];
            }

            return SubtractHighNarrowing<W, N>(op2[i - op1.Length], op3[i - op1.Length]);
        }

        public static N SubtractRoundedHighNarrowingUpper<W, N>(N[] op1, W[] op2, W[] op3, int i)
            where W : unmanaged, IBinaryInteger<W>
            where N : unmanaged, IBinaryInteger<N>
        {
            if (i < op1.Length)
            {
                return op1[i];
            }

            return SubtractRoundedHighNarrowing<W, N>(op2[i - op1.Length], op3[i - op1.Length]);
        }

        public static short SubtractWidening(sbyte op1, sbyte op2) => (short)((short)op1 - (short)op2);

        public static short SubtractWidening(short op1, sbyte op2) => (short)(op1 - op2);

        public static short SubtractWideningUpper(sbyte[] op1, sbyte[] op2, int i) => SubtractWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static short SubtractWideningUpper(short[] op1, sbyte[] op2, int i) => SubtractWidening(op1[i], op2[i + op2.Length / 2]);

        public static short ZeroExtendWidening(sbyte op1) => (short)(ushort)op1;

        public static short ZeroExtendWideningUpper(sbyte[] op1, int i) => ZeroExtendWidening(op1[i + op1.Length / 2]);

        public static uint AbsoluteDifferenceWidening(short op1, short op2) => op1 < op2 ? (uint)(op2 - op1) : (uint)(op1 - op2);

        public static uint AbsoluteDifferenceWideningUpper(short[] op1, short[] op2, int i) => AbsoluteDifferenceWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static int AbsoluteDifferenceWideningAndAdd(int op1, short op2, short op3) => (int)(op1 + (int)AbsoluteDifferenceWidening(op2, op3));

        public static int AbsoluteDifferenceWideningUpperAndAdd(int[] op1, short[] op2, short[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static int AbsoluteDifferenceWideningLowerAndAddEven(int[] op1, short[] op2, short[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i * 2], op3[i * 2]);

        public static int AbsoluteDifferenceWideningLowerAndAddOdd(int[] op1, short[] op2, short[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[(i * 2) + 1], op3[(i * 2) + 1]);

        public static int AbsoluteDifferenceWideningEven(short[] op1, short[] op2, int i) => (int)AbsoluteDifferenceWidening(op1[i * 2], op2[i * 2]);

        public static int AbsoluteDifferenceWideningOdd(short[] op1, short[] op2, int i) => (int)AbsoluteDifferenceWidening(op1[(i * 2) + 1], op2[(i * 2) + 1]);

        public static int AddAcrossWidening(short[] op1) => Reduce<int, short>(AddWidening, op1);

        public static long AddAcrossWideningLong(short[] op1) => Reduce<long, short>(AddWidening, op1);

        public static int AddPairwiseWidening(short[] op1, int i) => AddWidening(op1[2 * i], op1[2 * i + 1]);

        public static int AddPairwiseWideningAndAdd(int[] op1, short[] op2, int i) => (int)(op1[i] + AddWidening(op2[2 * i], op2[2 * i + 1]));

        public static uint AddCarryWideningEven(uint[] op1, uint[] op2, uint[] op3, int i)
        {
            uint lsb;
            ulong res;

            if ((i < 0) || (i >= op1.Length) || (i >= op2.Length) || (i >= op3.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(i), "Index i is out of range");
            }

            if (i % 2 == 0)
            {
                if (i + 1 >= op2.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(i), "Index i + 1 is out of range.");
                }

                lsb = op2[i + 1] & 1u;
                res = (ulong)op1[i] + op3[i] + lsb;
                return (uint)res;
            }
            else
            {
                if (((i - 1) < 0) || ((i - 1) >= op1.Length) || ((i - 1) >= op3.Length))
                {
                    throw new ArgumentOutOfRangeException(nameof(i), "Index i - 1 is out of range.");
                }

                lsb = op2[i] & 1u;
                res = (ulong)op1[i - 1] + op3[i - 1] + lsb;

                // Shift result to get the carry bit
                return (uint)(res >> 32);
            }
        }

        public static uint AddCarryWideningOdd(uint[] op1, uint[] op2, uint[] op3, int i)
        {
            uint lsb;
            ulong res;

            if ((i < 0) || (i >= op1.Length) || (i >= op2.Length) || (i >= op3.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(i), "Index i is out of range");
            }

            if (i % 2 == 0)
            {
                if (((i + 1) >= op1.Length) || ((i + 1) >= op2.Length))
                {
                    throw new ArgumentOutOfRangeException(nameof(i), "Index i + 1 is out of range.");
                }

                lsb = op2[i + 1] & 1u;
                res = (ulong)op1[i + 1] + op3[i] + lsb;
                return (uint)res;
            }
            else
            {
                if (((i - 1) < 0) || ((i - 1) >= op3.Length))
                {
                    throw new ArgumentOutOfRangeException(nameof(i), "Index i - 1 is out of range.");
                }

                lsb = op2[i] & 1u;
                res = (ulong)op1[i] + op3[i - 1] + lsb;

                // Shift result to get the carry bit
                return (uint)(res >> 32);
            }
        }

        public static unsafe U HighNarrowing<T, U>(T op1, bool round)
            where U : unmanaged, INumber<U>
            where T : unmanaged, INumber<T>, IShiftOperators<T, int, T>
        {
            T roundConst = T.Zero;
            int shift = 8 * Unsafe.SizeOf<U>();
            if (round)
            {
                roundConst = T.One << (shift - 1);
            }

            return U.CreateChecked((op1 + roundConst) >> shift);
        }

        public static short AddHighNarrowingEven(int[] op1, int[] op2, int i)
        {
            if (i % 2 == 0)
            {
                return (short) ((op1[i / 2] + op2[i / 2]) >> (8 * sizeof(short)));
            }

            return 0;
        }

        public static short AddHighNarrowingOdd(short[] even, int[] op1, int[] op2, int i)
        {
            if (i % 2 == 1)
            {
                return (short) ((op1[(i - 1) / 2] + op2[(i - 1) / 2]) >> (8 * sizeof(short)));
            }

            return even[i];
        }

        public static short AddHighNarrowingUpper(short[] op1, int[] op2, int[] op3, int i) => i < op1.Length ? op1[i] : AddHighNarrowing<int, short>(op2[i - op1.Length], op3[i - op1.Length]);

        public static int AddRoundedHighNarrowingUpper(short[] op1, int[] op2, int[] op3, int i) => i < op1.Length ? op1[i] : AddRoundedHighNarrowing<int, short>(op2[i - op1.Length], op3[i - op1.Length]);

        public static int AddWidening(short op1, short op2) => (int)((int)op1 + (int)op2);

        public static int AddWidening(int op1, short op2) => (int)(op1 + op2);

        public static long AddWidening(long op1, short op2) => (long)(op1 + (long)op2);

        public static int AddWideningUpper(short[] op1, short[] op2, int i) => AddWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static int AddWideningUpper(int[] op1, short[] op2, int i) => AddWidening(op1[i], op2[i + op2.Length / 2]);

        public static short ExtractNarrowing(int op1) => (short)op1;

        public static short ExtractNarrowingUpper(short[] op1, int[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowing(op2[i - op1.Length]);

        public static short FusedAddHalving(short op1, short op2) => (short)((uint)((int)op1 + (int)op2) >> 1);

        public static short FusedAddRoundedHalving(short op1, short op2) => (short)((uint)((int)op1 + (int)op2 + 1) >> 1);

        public static short FusedSubtractHalving(short op1, short op2) => (short)((uint)((int)op1 - (int)op2) >> 1);

        public static int Log2(float val)
        {
            if (float.IsNaN(val) || val <= 0)
            {
                return int.MinValue;
            }
            if (float.IsInfinity(val))
            {
                return int.MaxValue;
            }

            double log2 = Math.Log(val, 2.0);
            if (log2 >= int.MaxValue) return int.MaxValue;
            if (log2 <= int.MinValue) return int.MinValue;

            return (int)Math.Floor(log2);
        }

        public static int MultiplyByScalarWideningUpper(short[] op1, short op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2);

        public static int MultiplyByScalarWideningUpperAndAdd(int[] op1, short[] op2, short op3, int i) => MultiplyAddWidening(op1[i], op2[i + op2.Length / 2], op3);

        public static int MultiplyByScalarWideningUpperAndSubtract(int[] op1, short[] op2, short op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3);

        public static int MultiplyWidening(short op1, short op2) => (int)((int)op1 * (int)op2);

        public static int MultiplyWideningAndSubtract(int op1, short op2, short op3) => (int)(op1 - MultiplyWidening(op2, op3));

        public static int MultiplyWideningUpper(short[] op1, short[] op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static int MultiplyWideningUpperAndAdd(int[] op1, short[] op2, short[] op3, int i) => MultiplyAddWidening(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static int MultiplyWideningUpperAndSubtract(int[] op1, short[] op2, short[] op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static short SubtractHighNarrowing(int op1, int op2) => HighNarrowing<int, short>((int)(op1 - op2), round: false);

        public static int SubtractHighNarrowingUpper(short[] op1, int[] op2, int[] op3, int i) => i < op1.Length ? op1[i] : SubtractHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static short SubtractRoundedHighNarrowing(int op1, int op2) => HighNarrowing<int, short>((int)(op1 - op2), round: true);

        public static int SubtractRoundedHighNarrowingUpper(short[] op1, int[] op2, int[] op3, int i) => i < op1.Length ? op1[i] : SubtractRoundedHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static int SubtractWidening(short op1, short op2) => (int)((int)op1 - (int)op2);

        public static int SubtractWidening(int op1, short op2) => (int)(op1 - op2);

        public static int SubtractWideningUpper(short[] op1, short[] op2, int i) => SubtractWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static int SubtractWideningUpper(int[] op1, short[] op2, int i) => SubtractWidening(op1[i], op2[i + op2.Length / 2]);

        public static int ZeroExtendWidening(short op1) => (int)(uint)op1;

        public static int ZeroExtendWideningUpper(short[] op1, int i) => ZeroExtendWidening(op1[i + op1.Length / 2]);

        public static ulong AbsoluteDifferenceWidening(int op1, int op2) => op1 < op2 ? (ulong)(op2 - op1) : (ulong)(op1 - op2);

        public static ulong AbsoluteDifferenceWideningUpper(int[] op1, int[] op2, int i) => AbsoluteDifferenceWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static long AbsoluteDifferenceWideningAndAdd(long op1, int op2, int op3) => (long)(op1 + (long)AbsoluteDifferenceWidening(op2, op3));

        public static long AbsoluteDifferenceWideningUpperAndAdd(long[] op1, int[] op2, int[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static long AbsoluteDifferenceWideningLowerAndAddEven(long[] op1, int[] op2, int[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i * 2], op3[i * 2]);

        public static long AbsoluteDifferenceWideningLowerAndAddOdd(long[] op1, int[] op2, int[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[(i * 2) + 1], op3[(i * 2) + 1]);

        public static long AbsoluteDifferenceWideningEven(int[] op1, int[] op2, int i) => (long)AbsoluteDifferenceWidening(op1[i * 2], op2[i * 2]);

        public static long AbsoluteDifferenceWideningOdd(int[] op1, int[] op2, int i) => (long)AbsoluteDifferenceWidening(op1[(i * 2) + 1], op2[(i * 2) + 1]);

        public static long AddAcrossWidening(int[] op1) => Reduce<long, int>(AddWidening, op1);

        public static long AddPairwiseWidening(int[] op1, int i) => AddWidening(op1[2 * i], op1[2 * i + 1]);

        public static long AddPairwiseWideningAndAdd(long[] op1, int[] op2, int i) => (long)(op1[i] + AddWidening(op2[2 * i], op2[2 * i + 1]));

        public static ulong AddCarryWideningEven(ulong[] op1, ulong[] op2, ulong[] op3, int i)
        {
            ulong lsb;
            ulong res;

            if ((i < 0) || (i >= op1.Length) || (i >= op2.Length) || (i >= op3.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(i), "Index i is out of range");
            }

            if (i % 2 == 0)
            {
                if ((i + 1) >= op2.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(i), "Index i + 1 is out of range for op3.");
                }

                lsb = op2[i + 1] & 1UL;
                res = op1[i] + op3[i] + lsb;
                return res;
            }
            else
            {
                if (((i - 1) < 0) || ((i - 1) >= op1.Length) || ((i - 1) >= op3.Length))
                {
                    throw new ArgumentOutOfRangeException(nameof(i), "Index i - 1 is out of range.");
                }

                lsb = op2[i] & 1UL;

                // Look for an overflow in the addition to get the carry bit
                ulong sum1 = op1[i - 1] + op3[i - 1];
                bool overflow1 = sum1 < op1[i - 1];

                ulong sum2 = sum1 + lsb;
                bool overflow2 = sum2 < sum1;

                return (overflow1 || overflow2) ? 1UL : 0UL;
            }
        }

        public static ulong AddCarryWideningOdd(ulong[] op1, ulong[] op2, ulong[] op3, int i)
        {
            ulong lsb;
            ulong res;

            if ((i < 0) || (i >= op1.Length) || (i >= op2.Length) || (i >= op3.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(i), "Index i is out of range");
            }

            if (i % 2 == 0)
            {
                if (((i + 1) >= op1.Length) || ((i + 1) >= op2.Length))
                {
                    throw new ArgumentOutOfRangeException(nameof(i), "Index i + 1 is out of range.");
                }

                lsb = op2[i + 1] & 1UL;
                res = op1[i + 1] + op3[i] + lsb;
                return res;
            }
            else
            {
                if (((i - 1) < 0) || ((i - 1) >= op3.Length))
                {
                    throw new ArgumentOutOfRangeException(nameof(i), "Index i - 1 is out of range.");
                }

                lsb = op2[i] & 1UL;

                // Look for an overflow in the addition to get the carry bit
                ulong sum1 = op1[i] + op3[i - 1];
                bool overflow1 = sum1 < op1[i];

                ulong sum2 = sum1 + lsb;
                bool overflow2 = sum2 < sum1;

                return (overflow1 || overflow2) ? 1UL : 0UL;
            }
        }

        public static int AddHighNarrowingEven(long[] op1, long[] op2, int i)
        {
            if (i % 2 == 0)
            {
                return (int) ((op1[i / 2] + op2[i / 2]) >> (8 * sizeof(int)));
            }

            return 0;
        }

        public static int AddHighNarrowingOdd(int[] even, long[] op1, long[] op2, int i)
        {
            if (i % 2 == 1)
            {
                return (int) ((op1[(i - 1) / 2] + op2[(i - 1) / 2]) >> (8 * sizeof(int)));
            }

            return even[i];
        }

        public static int AddHighNarrowingUpper(int[] op1, long[] op2, long[] op3, int i) => i < op1.Length ? op1[i] : AddHighNarrowing<long, int>(op2[i - op1.Length], op3[i - op1.Length]);

        public static long AddRoundedHighNarrowingUpper(int[] op1, long[] op2, long[] op3, int i) => i < op1.Length ? op1[i] : AddRoundedHighNarrowing<long, int>(op2[i - op1.Length], op3[i - op1.Length]);

        public static long AddWidening(int op1, int op2) => (long)((long)op1 + (long)op2);

        public static long AddWidening(long op1, int op2) => (long)(op1 + op2);

        public static long AddWideningUpper(int[] op1, int[] op2, int i) => AddWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static long AddWideningUpper(long[] op1, int[] op2, int i) => AddWidening(op1[i], op2[i + op2.Length / 2]);

        public static int ExtractNarrowing(long op1) => (int)op1;

        public static int ExtractNarrowingUpper(int[] op1, long[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowing(op2[i - op1.Length]);

        public static int FusedAddHalving(int op1, int op2) => (int)((ulong)((long)op1 + (long)op2) >> 1);

        public static int FusedAddRoundedHalving(int op1, int op2) => (int)((ulong)((long)op1 + (long)op2 + 1) >> 1);

        public static int FusedSubtractHalving(int op1, int op2) => (int)((ulong)((long)op1 - (long)op2) >> 1);

        public static long Log2(double val)
        {
            if (double.IsNaN(val) || val <= 0)
            {
                return long.MinValue;
            }
            if (double.IsInfinity(val))
            {
                return long.MaxValue;
            }

            double log2 = Math.Log(val, 2.0);
            if (log2 >= long.MaxValue) return long.MaxValue;
            if (log2 <= long.MinValue) return long.MinValue;

            return (long)Math.Floor(log2);
        }

        public static long MultiplyByScalarWideningUpper(int[] op1, int op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2);

        public static long MultiplyByScalarWideningUpperAndAdd(long[] op1, int[] op2, int op3, int i) => MultiplyAddWidening(op1[i], op2[i + op2.Length / 2], op3);

        public static long MultiplyByScalarWideningUpperAndSubtract(long[] op1, int[] op2, int op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3);

        public static long MultiplyWidening(int op1, int op2) => (long)((long)op1 * (long)op2);

        public static long MultiplyWideningAndSubtract(long op1, int op2, int op3) => (long)(op1 - MultiplyWidening(op2, op3));

        public static long MultiplyWideningUpper(int[] op1, int[] op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static long MultiplyWideningUpperAndAdd(long[] op1, int[] op2, int[] op3, int i) => MultiplyAddWidening(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static long MultiplyWideningUpperAndSubtract(long[] op1, int[] op2, int[] op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static T ShiftLeft<T>(T op1, ulong op2) where T : INumber<T>
        {
            T two = T.One + T.One;
            for (ulong i = 0; (op1 != T.Zero) && (i < op2); i++)
            {
                op1 *= two;
            }

            return op1;
        }

        public static T ShiftRight<T>(T op1, ulong op2) where T : INumber<T>
        {
            T two = T.One + T.One;
            for (ulong i = 0; (op1 != T.Zero) && (i < op2); i++)
            {
                op1 /= two;
            }

            return op1;
        }

        public static T SignExtend<T>(T n, int numBits, bool zeroExtend) where T : struct, IComparable, IConvertible, IBinaryInteger<T>
        {
            // Get the underlying integer value
            long value = long.CreateTruncating(n);

            // Mask to extract the lowest numBits
            long mask = (1L << numBits) - 1;
            long lowestBits = value & mask;

            // Sign extension for signed integers
            long signBitMask = 1L << (numBits - 1);
            if (!zeroExtend && ((lowestBits & signBitMask) != 0))
            {
                // If sign bit is set, it's a negative number
                return (T)Convert.ChangeType(-((~lowestBits & mask) + 1), typeof(T));
            }
            else
            {
                // If sign bit is not set, it's a positive number
                return (T)Convert.ChangeType(lowestBits, typeof(T));
            }
        }

        public static int SubtractHighNarrowing(long op1, long op2) => HighNarrowing<long, int>((long)(op1 - op2), round: false);

        public static long SubtractHighNarrowingUpper(int[] op1, long[] op2, long[] op3, int i) => i < op1.Length ? op1[i] : SubtractHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static int SubtractRoundedHighNarrowing(long op1, long op2) => HighNarrowing<long, int>((long)(op1 - op2), round: true);

        public static long SubtractRoundedHighNarrowingUpper(int[] op1, long[] op2, long[] op3, int i) => i < op1.Length ? op1[i] : SubtractRoundedHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static long SubtractWidening(int op1, int op2) => (long)((long)op1 - (long)op2);

        public static long SubtractWidening(long op1, int op2) => (long)(op1 - op2);

        public static long SubtractWideningUpper(int[] op1, int[] op2, int i) => SubtractWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static long SubtractWideningUpper(long[] op1, int[] op2, int i) => SubtractWidening(op1[i], op2[i + op2.Length / 2]);

        public static long ZeroExtendWidening(int op1) => (long)(ulong)op1;

        public static long ZeroExtendWideningUpper(int[] op1, int i) => ZeroExtendWidening(op1[i + op1.Length / 2]);

        public static ushort AbsoluteDifferenceWidening(byte op1, byte op2) => op1 < op2 ? (ushort)(op2 - op1) : (ushort)(op1 - op2);

        public static ushort AbsoluteDifferenceWideningUpper(byte[] op1, byte[] op2, int i) => AbsoluteDifferenceWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ushort AbsoluteDifferenceWideningAndAdd(ushort op1, byte op2, byte op3) => (ushort)(op1 + (ushort)AbsoluteDifferenceWidening(op2, op3));

        public static ushort AbsoluteDifferenceWideningUpperAndAdd(ushort[] op1, byte[] op2, byte[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static ushort AbsoluteDifferenceWideningLowerAndAddEven(ushort[] op1, byte[] op2, byte[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i * 2], op3[i * 2]);

        public static ushort AbsoluteDifferenceWideningLowerAndAddOdd(ushort[] op1, byte[] op2, byte[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[(i * 2) + 1], op3[(i * 2) + 1]);

        public static ushort AbsoluteDifferenceWideningEven(byte[] op1, byte[] op2, int i) => AbsoluteDifferenceWidening(op1[i * 2], op2[i * 2]);

        public static ushort AbsoluteDifferenceWideningOdd(byte[] op1, byte[] op2, int i) => AbsoluteDifferenceWidening(op1[(i * 2) + 1], op2[(i * 2) + 1]);

        public static ushort AddAcrossWidening(byte[] op1) => Reduce<ushort, byte>(AddWidening, op1);

        public static ulong AddAcrossWideningULong(byte[] op1) => Reduce<ulong, byte>(AddWidening, op1);

        public static ushort AddPairwiseWidening(byte[] op1, int i) => AddWidening(op1[2 * i], op1[2 * i + 1]);

        public static ushort AddPairwiseWideningAndAdd(ushort[] op1, byte[] op2, int i) => (ushort)(op1[i] + AddWidening(op2[2 * i], op2[2 * i + 1]));

        public static byte AddHighNarrowingEven(ushort[] op1, ushort[] op2, int i)
        {
            if (i % 2 == 0)
            {
                return (byte) ((op1[i / 2] + op2[i / 2]) >> (8 * sizeof(byte)));
            }

            return 0;
        }

        public static byte AddHighNarrowingOdd(byte[] even, ushort[] op1, ushort[] op2, int i)
        {
            if (i % 2 == 1)
            {
                return (byte) ((op1[(i - 1) / 2] + op2[(i - 1) / 2]) >> (8 * sizeof(byte)));
            }

            return even[i];
        }

        public static byte AddHighNarrowingUpper(byte[] op1, ushort[] op2, ushort[] op3, int i) => i < op1.Length ? op1[i] : AddHighNarrowing<ushort, byte>(op2[i - op1.Length], op3[i - op1.Length]);

        public static ushort AddRoundedHighNarrowingUpper(byte[] op1, ushort[] op2, ushort[] op3, int i) => i < op1.Length ? op1[i] : AddRoundedHighNarrowing<ushort, byte>(op2[i - op1.Length], op3[i - op1.Length]);

        public static ushort AddWidening(byte op1, byte op2) => (ushort)((ushort)op1 + (ushort)op2);

        public static ushort AddWidening(ushort op1, byte op2) => (ushort)(op1 + op2);

        public static ulong AddWidening(ulong op1, byte op2) => (ulong)(op1 + (ulong)op2);

        public static ushort AddWideningUpper(byte[] op1, byte[] op2, int i) => AddWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ushort AddWideningUpper(ushort[] op1, byte[] op2, int i) => AddWidening(op1[i], op2[i + op2.Length / 2]);

        public static byte ExtractNarrowing(ushort op1) => (byte)op1;

        public static byte ExtractNarrowingUpper(byte[] op1, ushort[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowing(op2[i - op1.Length]);

        public static byte FusedAddHalving(byte op1, byte op2) => (byte)((ushort)((ushort)op1 + (ushort)op2) >> 1);

        public static byte FusedAddRoundedHalving(byte op1, byte op2) => (byte)((ushort)((ushort)op1 + (ushort)op2 + 1) >> 1);

        public static byte FusedSubtractHalving(byte op1, byte op2) => (byte)((ushort)((ushort)op1 - (ushort)op2) >> 1);

        public static ushort MultiplyByScalarWideningUpper(byte[] op1, byte op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2);

        public static ushort MultiplyByScalarWideningUpperAndAdd(ushort[] op1, byte[] op2, byte op3, int i) => MultiplyAddWidening(op1[i], op2[i + op2.Length / 2], op3);

        public static ushort MultiplyByScalarWideningUpperAndSubtract(ushort[] op1, byte[] op2, byte op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3);

        public static ushort MultiplyWidening(byte op1, byte op2) => (ushort)((ushort)op1 * (ushort)op2);

        public static ushort MultiplyWideningAndSubtract(ushort op1, byte op2, byte op3) => (ushort)(op1 - MultiplyWidening(op2, op3));

        public static ushort MultiplyWideningUpper(byte[] op1, byte[] op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ushort MultiplyWideningUpperAndAdd(ushort[] op1, byte[] op2, byte[] op3, int i) => MultiplyAddWidening(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static ushort MultiplyWideningUpperAndSubtract(ushort[] op1, byte[] op2, byte[] op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static byte SubtractHighNarrowing(ushort op1, ushort op2) => HighNarrowing<ushort, byte>((ushort)(op1 - op2), round: false);

        public static ushort SubtractHighNarrowingUpper(byte[] op1, ushort[] op2, ushort[] op3, int i) => i < op1.Length ? op1[i] : SubtractHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static byte SubtractRoundedHighNarrowing(ushort op1, ushort op2) => HighNarrowing<ushort, byte>((ushort)(op1 - op2), round: true);

        public static ushort SubtractRoundedHighNarrowingUpper(byte[] op1, ushort[] op2, ushort[] op3, int i) => i < op1.Length ? op1[i] : SubtractRoundedHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static ushort SubtractWidening(byte op1, byte op2) => (ushort)((ushort)op1 - (ushort)op2);

        public static ushort SubtractWidening(ushort op1, byte op2) => (ushort)(op1 - op2);

        public static ushort SubtractWideningUpper(byte[] op1, byte[] op2, int i) => SubtractWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ushort SubtractWideningUpper(ushort[] op1, byte[] op2, int i) => SubtractWidening(op1[i], op2[i + op2.Length / 2]);

        public static ushort ZeroExtendWidening(byte op1) => (ushort)(ushort)op1;

        public static ushort ZeroExtendWideningUpper(byte[] op1, int i) => ZeroExtendWidening(op1[i + op1.Length / 2]);

        public static uint AbsoluteDifferenceWidening(ushort op1, ushort op2) => op1 < op2 ? (uint)(op2 - op1) : (uint)(op1 - op2);

        public static uint AbsoluteDifferenceWideningUpper(ushort[] op1, ushort[] op2, int i) => AbsoluteDifferenceWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static uint AbsoluteDifferenceWideningAndAdd(uint op1, ushort op2, ushort op3) => (uint)(op1 + (uint)AbsoluteDifferenceWidening(op2, op3));

        public static uint AbsoluteDifferenceWideningUpperAndAdd(uint[] op1, ushort[] op2, ushort[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static uint AbsoluteDifferenceWideningLowerAndAddEven(uint[] op1, ushort[] op2, ushort[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i * 2], op3[i * 2]);

        public static uint AbsoluteDifferenceWideningLowerAndAddOdd(uint[] op1, ushort[] op2, ushort[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[(i * 2) + 1], op3[(i * 2) + 1]);

        public static uint AbsoluteDifferenceWideningEven(ushort[] op1, ushort[] op2, int i) => AbsoluteDifferenceWidening(op1[i * 2], op2[i * 2]);

        public static uint AbsoluteDifferenceWideningOdd(ushort[] op1, ushort[] op2, int i) => AbsoluteDifferenceWidening(op1[(i * 2) + 1], op2[(i * 2) + 1]);

        public static uint AddAcrossWidening(ushort[] op1) => Reduce<uint, ushort>(AddWidening, op1);

        public static ulong AddAcrossWideningULong(ushort[] op1) => Reduce<ulong, ushort>(AddWidening, op1);

        public static uint AddPairwiseWidening(ushort[] op1, int i) => AddWidening(op1[2 * i], op1[2 * i + 1]);

        public static uint AddPairwiseWideningAndAdd(uint[] op1, ushort[] op2, int i) => (uint)(op1[i] + AddWidening(op2[2 * i], op2[2 * i + 1]));

        public static ushort AddHighNarrowingEven(uint[] op1, uint[] op2, int i)
        {
            if (i % 2 == 0)
            {
                return (ushort) ((op1[i / 2] + op2[i / 2]) >> (8 * sizeof(ushort)));
            }

            return 0;
        }

        public static ushort AddHighNarrowingOdd(ushort[] even, uint[] op1, uint[] op2, int i)
        {
            if (i % 2 == 1)
            {
                return (ushort) ((op1[(i - 1) / 2] + op2[(i - 1) / 2]) >> (8 * sizeof(ushort)));
            }

            return even[i];
        }

        public static ushort AddHighNarrowingUpper(ushort[] op1, uint[] op2, uint[] op3, int i) => i < op1.Length ? op1[i] : AddHighNarrowing<uint, ushort>(op2[i - op1.Length], op3[i - op1.Length]);

        public static uint AddRoundedHighNarrowingUpper(ushort[] op1, uint[] op2, uint[] op3, int i) => i < op1.Length ? op1[i] : AddRoundedHighNarrowing<uint, ushort>(op2[i - op1.Length], op3[i - op1.Length]);

        public static uint AddWidening(ushort op1, ushort op2) => (uint)((uint)op1 + (uint)op2);

        public static uint AddWidening(uint op1, ushort op2) => (uint)(op1 + op2);

        public static ulong AddWidening(ulong op1, ushort op2) => (ulong)(op1 + (ulong)op2);

        public static uint AddWideningUpper(ushort[] op1, ushort[] op2, int i) => AddWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static uint AddWideningUpper(uint[] op1, ushort[] op2, int i) => AddWidening(op1[i], op2[i + op2.Length / 2]);

        public static ushort ExtractNarrowing(uint op1) => (ushort)op1;

        public static ushort ExtractNarrowingUpper(ushort[] op1, uint[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowing(op2[i - op1.Length]);

        public static ushort FusedAddHalving(ushort op1, ushort op2) => (ushort)((uint)((uint)op1 + (uint)op2) >> 1);

        public static ushort FusedAddRoundedHalving(ushort op1, ushort op2) => (ushort)((uint)((uint)op1 + (uint)op2 + 1) >> 1);

        public static ushort FusedSubtractHalving(ushort op1, ushort op2) => (ushort)((uint)((uint)op1 - (uint)op2) >> 1);

        public static uint MultiplyByScalarWideningUpper(ushort[] op1, ushort op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2);

        public static uint MultiplyByScalarWideningUpperAndAdd(uint[] op1, ushort[] op2, ushort op3, int i) => MultiplyAddWidening(op1[i], op2[i + op2.Length / 2], op3);

        public static uint MultiplyByScalarWideningUpperAndSubtract(uint[] op1, ushort[] op2, ushort op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3);

        public static uint MultiplyWidening(ushort op1, ushort op2) => (uint)((uint)op1 * (uint)op2);

        public static uint MultiplyWideningAndSubtract(uint op1, ushort op2, ushort op3) => (uint)(op1 - MultiplyWidening(op2, op3));

        public static uint MultiplyWideningUpper(ushort[] op1, ushort[] op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static uint MultiplyWideningUpperAndAdd(uint[] op1, ushort[] op2, ushort[] op3, int i) => MultiplyAddWidening(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static uint MultiplyWideningUpperAndSubtract(uint[] op1, ushort[] op2, ushort[] op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static ushort SubtractHighNarrowing(uint op1, uint op2) => HighNarrowing<uint, ushort>((uint)(op1 - op2), round: false);

        public static uint SubtractHighNarrowingUpper(ushort[] op1, uint[] op2, uint[] op3, int i) => i < op1.Length ? op1[i] : SubtractHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static ushort SubtractRoundedHighNarrowing(uint op1, uint op2) => HighNarrowing<uint, ushort>((uint)(op1 - op2), round: true);

        public static uint SubtractRoundedHighNarrowingUpper(ushort[] op1, uint[] op2, uint[] op3, int i) => i < op1.Length ? op1[i] : SubtractRoundedHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static uint SubtractWidening(ushort op1, ushort op2) => (uint)((uint)op1 - (uint)op2);

        public static uint SubtractWidening(uint op1, ushort op2) => (uint)(op1 - op2);

        public static uint SubtractWideningUpper(ushort[] op1, ushort[] op2, int i) => SubtractWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static uint SubtractWideningUpper(uint[] op1, ushort[] op2, int i) => SubtractWidening(op1[i], op2[i + op2.Length / 2]);

        public static uint ZeroExtendWidening(ushort op1) => (uint)(uint)op1;

        public static uint ZeroExtendWideningUpper(ushort[] op1, int i) => ZeroExtendWidening(op1[i + op1.Length / 2]);

        public static ulong AbsoluteDifferenceWidening(uint op1, uint op2) => op1 < op2 ? (ulong)(op2 - op1) : (ulong)(op1 - op2);

        public static ulong AbsoluteDifferenceWideningUpper(uint[] op1, uint[] op2, int i) => AbsoluteDifferenceWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ulong AbsoluteDifferenceWideningAndAdd(ulong op1, uint op2, uint op3) => (ulong)(op1 + (ulong)AbsoluteDifferenceWidening(op2, op3));

        public static ulong AbsoluteDifferenceWideningUpperAndAdd(ulong[] op1, uint[] op2, uint[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static ulong AbsoluteDifferenceWideningLowerAndAddEven(ulong[] op1, uint[] op2, uint[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[i * 2], op3[i * 2]);

        public static ulong AbsoluteDifferenceWideningLowerAndAddOdd(ulong[] op1, uint[] op2, uint[] op3, int i) => AbsoluteDifferenceWideningAndAdd(op1[i], op2[(i * 2) + 1], op3[(i * 2) + 1]);

        public static ulong AbsoluteDifferenceWideningEven(uint[] op1, uint[] op2, int i) => AbsoluteDifferenceWidening(op1[i * 2], op2[i * 2]);

        public static ulong AbsoluteDifferenceWideningOdd(uint[] op1, uint[] op2, int i) => AbsoluteDifferenceWidening(op1[(i * 2) + 1], op2[(i * 2) + 1]);

        public static ulong AddAcrossWidening(uint[] op1) => Reduce<ulong, uint>(AddWidening, op1);

        public static ulong AddPairwiseWidening(uint[] op1, int i) => AddWidening(op1[2 * i], op1[2 * i + 1]);

        public static ulong AddPairwiseWideningAndAdd(ulong[] op1, uint[] op2, int i) => (ulong)(op1[i] + AddWidening(op2[2 * i], op2[2 * i + 1]));

        public static uint AddHighNarrowingEven(ulong[] op1, ulong[] op2, int i)
        {
            if (i % 2 == 0)
            {
                return (uint) ((op1[i / 2] + op2[i / 2]) >> (8 * sizeof(uint)));
            }

            return 0;
        }

        public static uint AddHighNarrowingOdd(uint[] even, ulong[] op1, ulong[] op2, int i)
        {
            if (i % 2 == 1)
            {
                return (uint) ((op1[(i - 1) / 2] + op2[(i - 1) / 2]) >> (8 * sizeof(uint)));
            }

            return even[i];
        }

        public static uint AddHighNarrowingUpper(uint[] op1, ulong[] op2, ulong[] op3, int i) => i < op1.Length ? op1[i] : AddHighNarrowing<ulong, uint>(op2[i - op1.Length], op3[i - op1.Length]);

        public static ulong AddRoundedHighNarrowingUpper(uint[] op1, ulong[] op2, ulong[] op3, int i) => i < op1.Length ? op1[i] : AddRoundedHighNarrowing<ulong, uint>(op2[i - op1.Length], op3[i - op1.Length]);

        public static ulong AddWidening(uint op1, uint op2) => (ulong)((ulong)op1 + (ulong)op2);

        public static ulong AddWidening(ulong op1, uint op2) => (ulong)(op1 + op2);

        public static ulong AddWideningUpper(uint[] op1, uint[] op2, int i) => AddWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ulong AddWideningUpper(ulong[] op1, uint[] op2, int i) => AddWidening(op1[i], op2[i + op2.Length / 2]);

        public static uint ExtractNarrowing(ulong op1) => (uint)op1;

        public static uint ExtractNarrowingUpper(uint[] op1, ulong[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowing(op2[i - op1.Length]);

        public static uint FusedAddHalving(uint op1, uint op2) => (uint)((ulong)((ulong)op1 + (ulong)op2) >> 1);

        public static ulong FusedAddHalving(ulong op1, ulong op2)
        {
            ulong sum = op1 + op2;
            bool carry = sum < op1;
            return (sum >> 1) + (carry ? 1UL << 63 : 0);
        }
        public static long FusedAddHalving(long op1, long op2)
        {
            long sum = op1 + op2;

            if (op1 > 0 && op2 > 0 && sum < 0)
            {
                // Addition overflows into the sign bit, which simulates an
                // unsigned 64-bit addition. We need to perform a logical shift
                // to make sure the sign-bit is clear on the half value.
                return (long)((ulong)sum >>> 1);
            }
            else if (op1 < 0 && op2 < 0 && sum > 0)
            {
                // Addition of negative values overflows beyond the sign-bit into
                // the positive range. The halved value will be OK but we need to
                // reinstate the sign bit which was lost.
                return (long)((ulong)(sum >> 1) | (1UL << 63));
            }
            else
            {
                // No overflow, simply halve preserving sign-bit.
                return sum >> 1;
            }
        }

        public static long FusedSubtractHalving(long op1, long op2)
        {
            long diff = op1 - op2;

            if (op1 > 0 && op2 < 0 && diff < 0)
            {
                // Subtract of negative value overflows into the sign bit We need
                // to perform a logical shift to make sure the sign-bit is clear
                // on the half value.
                return (long)((ulong)diff >>> 1);
            }
            else if (op1 < 0 && op2 > 0 && diff > 0)
            {
                // Subtraction of positive value overflows beyond the sign-bit into
                // the positive range. The halved value will be OK but we need to
                // reinstate the sign bit which was lost.
                return (long)((ulong)(diff >> 1) | (1UL << 63));
            }
            else
            {
                // No overflow, simply halve preserving sign-bit.
                return diff >> 1;
            }
        }

        public static ulong FusedSubtractHalving(ulong op1, ulong op2)
        {
            ulong diff = op1 - op2;
            bool overflow = op1 < op2;
            return (diff >> 1) + (overflow ? 1UL << 63 : 0);
        }

        public static uint FusedAddRoundedHalving(uint op1, uint op2) => (uint)((ulong)((ulong)op1 + (ulong)op2 + 1) >> 1);

        public static uint FusedSubtractHalving(uint op1, uint op2) => (uint)((ulong)((ulong)op1 - (ulong)op2) >> 1);

        public static ulong MultiplyByScalarWideningUpper(uint[] op1, uint op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2);

        public static ulong MultiplyByScalarWideningUpperAndAdd(ulong[] op1, uint[] op2, uint op3, int i) => MultiplyAddWidening(op1[i], op2[i + op2.Length / 2], op3);

        public static ulong MultiplyByScalarWideningUpperAndSubtract(ulong[] op1, uint[] op2, uint op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3);

        public static ulong MultiplyWidening(uint op1, uint op2) => (ulong)((ulong)op1 * (ulong)op2);

        public static ulong MultiplyWideningAndSubtract(ulong op1, uint op2, uint op3) => (ulong)(op1 - MultiplyWidening(op2, op3));

        public static ulong MultiplyWideningUpper(uint[] op1, uint[] op2, int i) => MultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ulong MultiplyWideningUpperAndAdd(ulong[] op1, uint[] op2, uint[] op3, int i) => MultiplyAddWidening(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static ulong MultiplyWideningUpperAndSubtract(ulong[] op1, uint[] op2, uint[] op3, int i) => MultiplyWideningAndSubtract(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static uint SubtractHighNarrowing(ulong op1, ulong op2) => HighNarrowing<ulong, uint>((ulong)(op1 - op2), round: false);

        public static ulong SubtractHighNarrowingUpper(uint[] op1, ulong[] op2, ulong[] op3, int i) => i < op1.Length ? op1[i] : SubtractHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static uint SubtractRoundedHighNarrowing(ulong op1, ulong op2) => HighNarrowing<ulong, uint>((ulong)(op1 - op2), round: true);

        public static ulong SubtractRoundedHighNarrowingUpper(uint[] op1, ulong[] op2, ulong[] op3, int i) => i < op1.Length ? op1[i] : SubtractRoundedHighNarrowing(op2[i - op1.Length], op3[i - op1.Length]);

        public static ulong SubtractWidening(uint op1, uint op2) => (ulong)((ulong)op1 - (ulong)op2);

        public static ulong SubtractWidening(ulong op1, uint op2) => (ulong)(op1 - op2);

        public static ulong SubtractWideningUpper(uint[] op1, uint[] op2, int i) => SubtractWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ulong SubtractWideningUpper(ulong[] op1, uint[] op2, int i) => SubtractWidening(op1[i], op2[i + op2.Length / 2]);

        public static ulong ZeroExtendWidening(uint op1) => (ulong)(ulong)op1;

        public static ulong ZeroExtendWideningUpper(uint[] op1, int i) => ZeroExtendWidening(op1[i + op1.Length / 2]);

        private static ulong Reduce(Func<ulong, uint, ulong> reduceOp, uint[] op1)
        {
            ulong acc = op1[0];

            for (int i = 1; i < op1.Length; i++)
            {
                acc = reduceOp(acc, op1[i]);
            }

            return acc;
        }

        public static double AddWidening(double op1, float op2) => (double)(op1 + (double)op2);


        private static bool SignedSatQ(short val, out sbyte result)
        {
            bool saturated = false;

            if (val > sbyte.MaxValue)
            {
                result = sbyte.MaxValue;
                saturated = true;
            }
            else if (val < sbyte.MinValue)
            {
                result = sbyte.MinValue;
                saturated = true;
            }
            else
            {
                result = (sbyte)val;
            }

            return saturated;
        }

        private static bool UnsignedSatQ(short val, out byte result)
        {
            bool saturated = false;

            if (val > byte.MaxValue)
            {
                result = byte.MaxValue;
                saturated = true;
            }
            else if (val < 0)
            {
                result = 0;
                saturated = true;
            }
            else
            {
                result = (byte)val;
            }

            return saturated;
        }

        private static bool UnsignedSatQ(ushort val, out byte result)
        {
            bool saturated = false;

            if (val > byte.MaxValue)
            {
                result = byte.MaxValue;
                saturated = true;
            }
            else if (val < 0)
            {
                result = 0;
                saturated = true;
            }
            else
            {
                result = (byte)val;
            }

            return saturated;
        }

        private static bool SatQ(short val, out sbyte result, bool reinterpretAsUnsigned = false)
        {
            bool saturated;

            if (reinterpretAsUnsigned)
            {
                byte res;
                saturated = UnsignedSatQ((ushort)val, out res);
                result = (sbyte)res;
            }
            else
            {
                saturated = SignedSatQ(val, out result);
            }

            return saturated;
        }

        private static bool SatQ(ushort val, out byte result) => UnsignedSatQ(val, out result);

        public static sbyte ExtractNarrowingSaturate(short op1)
        {
            sbyte result;

            SatQ(op1, out result);

            return result;
        }

        public static sbyte ExtractNarrowingSaturateUpper(sbyte[] op1, short[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowingSaturate(op2[i - op1.Length]);

        public static byte ExtractNarrowingSaturate(ushort op1)
        {
            byte result;

            SatQ(op1, out result);

            return result;
        }

        public static byte ExtractNarrowingSaturateUpper(byte[] op1, ushort[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowingSaturate(op2[i - op1.Length]);

        public static byte ExtractNarrowingSaturateUnsigned(short op1)
        {
            byte result;

            UnsignedSatQ(op1, out result);

            return result;
        }

        public static byte ExtractNarrowingSaturateUnsignedUpper(byte[] op1, short[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowingSaturateUnsigned(op2[i - op1.Length]);

        private static (short val, bool ovf) MultiplyDoublingOvf(sbyte op1, sbyte op2, bool rounding, short op3, bool subOp)
        {
            short product = (short)((short)op1 * (short)op2);

            bool dblOvf;
            (product, dblOvf) = AddOvf(product, product);

            bool addOvf;
            short accum;

            if (subOp)
            {
                (accum, addOvf) = SubtractOvf(op3, product);
            }
            else
            {
                (accum, addOvf) = AddOvf(op3, product);
            }

            short roundConst = 0;

            if (rounding)
            {
                roundConst = (short)1 << (8 * sizeof(sbyte) - 1);
            }

            bool rndOvf;
            short result;

            (result, rndOvf) = AddOvf(accum, roundConst);

            return (result, addOvf ^ rndOvf ^ dblOvf);
        }

        private static sbyte SaturateHigh(short val, bool ovf)
        {
            if (ovf)
            {
                return val < 0 ? sbyte.MaxValue : sbyte.MinValue;
            }

            return (sbyte)UnsignedShift(val, (short)(-8 * sizeof(sbyte)));
        }

        public static sbyte MultiplyDoublingSaturateHigh(sbyte op1, sbyte op2)
        {
            var (val, ovf) = MultiplyDoublingOvf(op1, op2, rounding: false, (short)0, subOp: false);

            return SaturateHigh(val, ovf);
        }

        public static sbyte MultiplyRoundedDoublingSaturateHigh(sbyte op1, sbyte op2)
        {
            var (val, ovf) = MultiplyDoublingOvf(op1, op2, rounding: true, (short)0, subOp: false);

            return SaturateHigh(val, ovf);
        }

        public static short MultiplyDoublingWideningSaturate(sbyte op1, sbyte op2)
        {
            var (product, ovf) = MultiplyDoublingOvf(op1, op2, rounding: false, (short)0, subOp: false);

            if (ovf)
            {
                return product < 0 ? sbyte.MaxValue : sbyte.MinValue;
            }

            return product;
        }

        public static sbyte MultiplyRoundedDoublingAndAddSaturateHigh(sbyte op1, sbyte op2, sbyte op3)
        {
            short addend = UnsignedShift((short)op1, (short)(8 * sizeof(sbyte)));

            var (val, ovf) = MultiplyDoublingOvf(op2, op3, rounding: true, addend, subOp: false);

            return SaturateHigh(val, ovf);
        }

        public static sbyte MultiplyRoundedDoublingAndSubtractSaturateHigh(sbyte op1, sbyte op2, sbyte op3)
        {
            short minuend = UnsignedShift((short)op1, (short)(8 * sizeof(sbyte)));

            var (val, ovf) = MultiplyDoublingOvf(op2, op3, rounding: true, minuend, subOp: true);

            return SaturateHigh(val, ovf);
        }

        public static sbyte[] MultiplyAddRoundedDoublingSaturateHighRotateComplex(sbyte[] op1, sbyte[] op2, sbyte[] op3, byte rot)
        {
            for (int i = 0; i < op1.Length; i += 2)
            {
                int real = i;
                int img = i + 1;
                (sbyte ans1, sbyte ans2) = rot switch
                {
                    0 => (MultiplyRoundedDoublingAndAddSaturateHigh(op1[real], op2[real], op3[real]), MultiplyRoundedDoublingAndAddSaturateHigh(op1[img], op2[real], op3[img])),
                    1 => (MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[real], op2[img], op3[img]), MultiplyRoundedDoublingAndAddSaturateHigh(op1[img], op2[img], op3[real])),
                    2 => (MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[real], op2[real], op3[real]), MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[img], op2[real], op3[img])),
                    3 => (MultiplyRoundedDoublingAndAddSaturateHigh(op1[real], op2[img], op3[img]), MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[img], op2[img], op3[real])),
                    _ => (default, default)
                };

                op1[real] = ans1;
                op1[img] = ans2;
            }

            return op1;
        }

        public static short MultiplyDoublingWideningAndAddSaturate(short op1, sbyte op2, sbyte op3) => AddSaturate(op1, MultiplyDoublingWideningSaturate(op2, op3));

        public static short MultiplyDoublingWideningAndSubtractSaturate(short op1, sbyte op2, sbyte op3) => SubtractSaturate(op1, MultiplyDoublingWideningSaturate(op2, op3));

        public static short MultiplyDoublingWideningSaturateUpperByScalar(sbyte[] op1, sbyte op2, int i) => MultiplyDoublingWideningSaturate(op1[i + op1.Length / 2], op2);

        public static short MultiplyDoublingWideningUpperByScalarAndAddSaturate(short[] op1, sbyte[] op2, sbyte op3, int i) => MultiplyDoublingWideningAndAddSaturate(op1[i], op2[i + op2.Length / 2], op3);

        public static short MultiplyDoublingWideningUpperByScalarAndSubtractSaturate(short[] op1, sbyte[] op2, sbyte op3, int i) => MultiplyDoublingWideningAndSubtractSaturate(op1[i], op2[i + op2.Length / 2], op3);

        public static short MultiplyDoublingWideningSaturateUpper(sbyte[] op1, sbyte[] op2, int i) => MultiplyDoublingWideningSaturate(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static short MultiplyDoublingWideningUpperAndAddSaturate(short[] op1, sbyte[] op2, sbyte[] op3, int i) => MultiplyDoublingWideningAndAddSaturate(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static short MultiplyDoublingWideningUpperAndSubtractSaturate(short[] op1, sbyte[] op2, sbyte[] op3, int i) => MultiplyDoublingWideningAndSubtractSaturate(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static short ShiftLeftLogicalWidening(sbyte op1, byte op2) => UnsignedShift((short)op1, (short)op2);

        public static ushort ShiftLeftLogicalWidening(byte op1, byte op2) => UnsignedShift((ushort)op1, (short)op2);

        public static short ShiftLeftLogicalWideningUpper(sbyte[] op1, byte op2, int i) => ShiftLeftLogicalWidening(op1[i + op1.Length / 2], op2);

        public static ushort ShiftLeftLogicalWideningUpper(byte[] op1, byte op2, int i) => ShiftLeftLogicalWidening(op1[i + op1.Length / 2], op2);

        public static sbyte ShiftRightArithmeticRoundedNarrowingSaturate(short op1, byte op2)
        {
            sbyte result;

            SatQ(SignedShift(op1, (short)(-op2), rounding: true), out result);

            return result;
        }

        public static byte ShiftRightArithmeticRoundedNarrowingSaturateUnsigned(short op1, byte op2)
        {
            byte result;

            UnsignedSatQ(SignedShift(op1, (short)(-op2), rounding: true), out result);

            return result;
        }

        public static byte ShiftRightArithmeticRoundedNarrowingSaturateUnsignedUpper(byte[] op1, short[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (byte)ShiftRightArithmeticRoundedNarrowingSaturateUnsigned(op2[i - op1.Length], op3);

        public static sbyte ShiftRightArithmeticRoundedNarrowingSaturateUpper(sbyte[] op1, short[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (sbyte)ShiftRightArithmeticRoundedNarrowingSaturate(op2[i - op1.Length], op3);

        public static sbyte ShiftRightArithmeticNarrowingSaturate(short op1, byte op2)
        {
            sbyte result;

            SatQ(SignedShift(op1, (short)(-op2)), out result);

            return result;
        }

        public static byte ShiftRightArithmeticNarrowingSaturateUnsigned(short op1, byte op2)
        {
            byte result;

            UnsignedSatQ(SignedShift(op1, (short)(-op2)), out result);

            return result;
        }

        public static byte ShiftRightArithmeticNarrowingSaturateUnsignedUpper(byte[] op1, short[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (byte)ShiftRightArithmeticNarrowingSaturateUnsigned(op2[i - op1.Length], op3);

        public static sbyte ShiftRightArithmeticNarrowingSaturateUpper(sbyte[] op1, short[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (sbyte)ShiftRightArithmeticNarrowingSaturate(op2[i - op1.Length], op3);

        public static sbyte ShiftRightLogicalNarrowing(short op1, byte op2) => (sbyte)UnsignedShift(op1, (short)(-op2));

        public static byte ShiftRightLogicalNarrowing(ushort op1, byte op2) => (byte)UnsignedShift(op1, (short)(-op2));

        public static sbyte ShiftRightLogicalRoundedNarrowing(short op1, byte op2) => (sbyte)UnsignedShift(op1, (short)(-op2), rounding: true);

        public static byte ShiftRightLogicalRoundedNarrowing(ushort op1, byte op2) => (byte)UnsignedShift(op1, (short)(-op2), rounding: true);

        public static sbyte ShiftRightLogicalRoundedNarrowingUpper(sbyte[] op1, short[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (sbyte)ShiftRightLogicalRoundedNarrowing(op2[i - op1.Length], op3);

        public static byte ShiftRightLogicalRoundedNarrowingUpper(byte[] op1, ushort[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (byte)ShiftRightLogicalRoundedNarrowing(op2[i - op1.Length], op3);

        public static sbyte ShiftRightLogicalRoundedNarrowingSaturate(short op1, byte op2)
        {
            sbyte result;

            SatQ(UnsignedShift(op1, (short)(-op2), rounding: true), out result, reinterpretAsUnsigned: true);

            return result;
        }

        public static byte ShiftRightLogicalRoundedNarrowingSaturate(ushort op1, byte op2)
        {
            byte result;

            SatQ(UnsignedShift(op1, (short)(-op2), rounding: true), out result);

            return result;
        }

        public static sbyte ShiftRightLogicalRoundedNarrowingSaturateUpper(sbyte[] op1, short[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (sbyte)ShiftRightLogicalRoundedNarrowingSaturate(op2[i - op1.Length], op3);

        public static byte ShiftRightLogicalRoundedNarrowingSaturateUpper(byte[] op1, ushort[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (byte)ShiftRightLogicalRoundedNarrowingSaturate(op2[i - op1.Length], op3);

        public static sbyte ShiftRightLogicalNarrowingUpper(sbyte[] op1, short[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (sbyte)ShiftRightLogicalNarrowing(op2[i - op1.Length], op3);

        public static byte ShiftRightLogicalNarrowingUpper(byte[] op1, ushort[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (byte)ShiftRightLogicalNarrowing(op2[i - op1.Length], op3);

        public static sbyte ShiftRightLogicalNarrowingSaturate(short op1, byte op2)
        {
            sbyte result;

            SatQ(UnsignedShift(op1, (short)(-op2)), out result, reinterpretAsUnsigned: true);

            return result;
        }

        public static byte ShiftRightLogicalNarrowingSaturate(ushort op1, byte op2)
        {
            byte result;

            SatQ(UnsignedShift(op1, (short)(-op2)), out result);

            return result;
        }

        public static sbyte ShiftRightLogicalNarrowingSaturateUpper(sbyte[] op1, short[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (sbyte)ShiftRightLogicalNarrowingSaturate(op2[i - op1.Length], op3);

        public static byte ShiftRightLogicalNarrowingSaturateUpper(byte[] op1, ushort[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (byte)ShiftRightLogicalNarrowingSaturate(op2[i - op1.Length], op3);

        public static short SignExtendWidening(sbyte op1) => op1;

        public static short SignExtendWideningUpper(sbyte[] op1, int i) => SignExtendWidening(op1[i + op1.Length / 2]);

        private static bool SignedSatQ(int val, out short result)
        {
            bool saturated = false;

            if (val > short.MaxValue)
            {
                result = short.MaxValue;
                saturated = true;
            }
            else if (val < short.MinValue)
            {
                result = short.MinValue;
                saturated = true;
            }
            else
            {
                result = (short)val;
            }

            return saturated;
        }

        private static bool UnsignedSatQ(int val, out ushort result)
        {
            bool saturated = false;

            if (val > ushort.MaxValue)
            {
                result = ushort.MaxValue;
                saturated = true;
            }
            else if (val < 0)
            {
                result = 0;
                saturated = true;
            }
            else
            {
                result = (ushort)val;
            }

            return saturated;
        }

        private static bool UnsignedSatQ(uint val, out ushort result)
        {
            bool saturated = false;

            if (val > ushort.MaxValue)
            {
                result = ushort.MaxValue;
                saturated = true;
            }
            else if (val < 0)
            {
                result = 0;
                saturated = true;
            }
            else
            {
                result = (ushort)val;
            }

            return saturated;
        }

        private static bool SatQ(int val, out short result, bool reinterpretAsUnsigned = false)
        {
            bool saturated;

            if (reinterpretAsUnsigned)
            {
                ushort res;
                saturated = UnsignedSatQ((uint)val, out res);
                result = (short)res;
            }
            else
            {
                saturated = SignedSatQ(val, out result);
            }

            return saturated;
        }

        private static bool SatQ(uint val, out ushort result) => UnsignedSatQ(val, out result);

        public static short ExtractNarrowingSaturate(int op1)
        {
            short result;

            SatQ(op1, out result);

            return result;
        }

        public static short ExtractNarrowingSaturateUpper(short[] op1, int[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowingSaturate(op2[i - op1.Length]);

        public static ushort ExtractNarrowingSaturate(uint op1)
        {
            ushort result;

            SatQ(op1, out result);

            return result;
        }

        public static ushort ExtractNarrowingSaturateUpper(ushort[] op1, uint[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowingSaturate(op2[i - op1.Length]);

        public static ushort ExtractNarrowingSaturateUnsigned(int op1)
        {
            ushort result;

            UnsignedSatQ(op1, out result);

            return result;
        }

        public static ushort ExtractNarrowingSaturateUnsignedUpper(ushort[] op1, int[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowingSaturateUnsigned(op2[i - op1.Length]);

        private static (int val, bool ovf) MultiplyDoublingOvf(short op1, short op2, bool rounding, int op3, bool subOp)
        {
            int product = (int)((int)op1 * (int)op2);

            bool dblOvf;
            (product, dblOvf) = AddOvf(product, product);

            bool addOvf;
            int accum;

            if (subOp)
            {
                (accum, addOvf) = SubtractOvf(op3, product);
            }
            else
            {
                (accum, addOvf) = AddOvf(op3, product);
            }

            int roundConst = 0;

            if (rounding)
            {
                roundConst = (int)1 << (8 * sizeof(short) - 1);
            }

            bool rndOvf;
            int result;

            (result, rndOvf) = AddOvf(accum, roundConst);

            return (result, addOvf ^ rndOvf ^ dblOvf);
        }

        private static short SaturateHigh(int val, bool ovf)
        {
            if (ovf)
            {
                return val < 0 ? short.MaxValue : short.MinValue;
            }

            return (short)UnsignedShift(val, (int)(-8 * sizeof(short)));
        }

        public static short MultiplyDoublingSaturateHigh(short op1, short op2)
        {
            var (val, ovf) = MultiplyDoublingOvf(op1, op2, rounding: false, (int)0, subOp: false);

            return SaturateHigh(val, ovf);
        }

        public static short MultiplyRoundedDoublingSaturateHigh(short op1, short op2)
        {
            var (val, ovf) = MultiplyDoublingOvf(op1, op2, rounding: true, (int)0, subOp: false);

            return SaturateHigh(val, ovf);
        }

        public static int MultiplyDoublingWideningSaturate(short op1, short op2)
        {
            var (product, ovf) = MultiplyDoublingOvf(op1, op2, rounding: false, (int)0, subOp: false);

            if (ovf)
            {
                return product < 0 ? short.MaxValue : short.MinValue;
            }

            return product;
        }

        public static short MultiplyRoundedDoublingAndAddSaturateHigh(short op1, short op2, short op3)
        {
            int addend = UnsignedShift((int)op1, (int)(8 * sizeof(short)));

            var (val, ovf) = MultiplyDoublingOvf(op2, op3, rounding: true, addend, subOp: false);

            return SaturateHigh(val, ovf);
        }

        public static short MultiplyRoundedDoublingAndSubtractSaturateHigh(short op1, short op2, short op3)
        {
            int minuend = UnsignedShift((int)op1, (int)(8 * sizeof(short)));

            var (val, ovf) = MultiplyDoublingOvf(op2, op3, rounding: true, minuend, subOp: true);

            return SaturateHigh(val, ovf);
        }

        public static short[] MultiplyAddRoundedDoublingSaturateHighRotateComplex(short[] op1, short[] op2, short[] op3, byte rot)
        {
            for (int i = 0; i < op1.Length; i += 2)
            {
                int real = i;
                int img = i + 1;
                (short ans1, short ans2) = rot switch
                {
                    0 => (MultiplyRoundedDoublingAndAddSaturateHigh(op1[real], op2[real], op3[real]), MultiplyRoundedDoublingAndAddSaturateHigh(op1[img], op2[real], op3[img])),
                    1 => (MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[real], op2[img], op3[img]), MultiplyRoundedDoublingAndAddSaturateHigh(op1[img], op2[img], op3[real])),
                    2 => (MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[real], op2[real], op3[real]), MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[img], op2[real], op3[img])),
                    3 => (MultiplyRoundedDoublingAndAddSaturateHigh(op1[real], op2[img], op3[img]), MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[img], op2[img], op3[real])),
                    _ => (default, default)
                };

                op1[real] = ans1;
                op1[img] = ans2;
            }

            return op1;
        }

        public static short[] MultiplyAddRoundedDoublingSaturateHighRotateComplexBySelectedScalar(short[] op1, short[] op2, short[] op3, byte index, byte rot)
        {
            for (int i = 0; i < op1.Length; i += 2)
            {
                int real = i;
                int img = i + 1;
                (short op3Real, short op3Img) = (op3[index * 2], op3[(index * 2) + 1]);
                (short ans1, short ans2) = rot switch
                {
                    0 => (MultiplyRoundedDoublingAndAddSaturateHigh(op1[real], op2[real], op3Real), MultiplyRoundedDoublingAndAddSaturateHigh(op1[img], op2[real], op3Img)),
                    1 => (MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[real], op2[img], op3Img), MultiplyRoundedDoublingAndAddSaturateHigh(op1[img], op2[img], op3Real)),
                    2 => (MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[real], op2[real], op3Real), MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[img], op2[real], op3Img)),
                    3 => (MultiplyRoundedDoublingAndAddSaturateHigh(op1[real], op2[img], op3Img), MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[img], op2[img], op3Real)),
                    _ => (default, default)
                };

                op1[real] = ans1;
                op1[img] = ans2;
            }

            return op1;
        }

        public static int MultiplyDoublingWideningAndAddSaturate(int op1, short op2, short op3) => AddSaturate(op1, MultiplyDoublingWideningSaturate(op2, op3));

        public static int MultiplyDoublingWideningAndSubtractSaturate(int op1, short op2, short op3) => SubtractSaturate(op1, MultiplyDoublingWideningSaturate(op2, op3));

        public static int MultiplyDoublingWideningSaturateUpperByScalar(short[] op1, short op2, int i) => MultiplyDoublingWideningSaturate(op1[i + op1.Length / 2], op2);

        public static int MultiplyDoublingWideningUpperByScalarAndAddSaturate(int[] op1, short[] op2, short op3, int i) => MultiplyDoublingWideningAndAddSaturate(op1[i], op2[i + op2.Length / 2], op3);

        public static int MultiplyDoublingWideningUpperByScalarAndSubtractSaturate(int[] op1, short[] op2, short op3, int i) => MultiplyDoublingWideningAndSubtractSaturate(op1[i], op2[i + op2.Length / 2], op3);

        public static int MultiplyDoublingWideningSaturateUpper(short[] op1, short[] op2, int i) => MultiplyDoublingWideningSaturate(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static int MultiplyDoublingWideningUpperAndAddSaturate(int[] op1, short[] op2, short[] op3, int i) => MultiplyDoublingWideningAndAddSaturate(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static int MultiplyDoublingWideningUpperAndSubtractSaturate(int[] op1, short[] op2, short[] op3, int i) => MultiplyDoublingWideningAndSubtractSaturate(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static int ShiftLeftLogicalWidening(short op1, byte op2) => UnsignedShift((int)op1, (int)op2);

        public static uint ShiftLeftLogicalWidening(ushort op1, byte op2) => UnsignedShift((uint)op1, (int)op2);

        public static int ShiftLeftLogicalWideningUpper(short[] op1, byte op2, int i) => ShiftLeftLogicalWidening(op1[i + op1.Length / 2], op2);

        public static uint ShiftLeftLogicalWideningUpper(ushort[] op1, byte op2, int i) => ShiftLeftLogicalWidening(op1[i + op1.Length / 2], op2);

        public static short ShiftRightArithmeticRoundedNarrowingSaturate(int op1, byte op2)
        {
            short result;

            SatQ(SignedShift(op1, (int)(-op2), rounding: true), out result);

            return result;
        }

        public static ushort ShiftRightArithmeticRoundedNarrowingSaturateUnsigned(int op1, byte op2)
        {
            ushort result;

            UnsignedSatQ(SignedShift(op1, (int)(-op2), rounding: true), out result);

            return result;
        }

        public static ushort ShiftRightArithmeticRoundedNarrowingSaturateUnsignedUpper(ushort[] op1, int[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (ushort)ShiftRightArithmeticRoundedNarrowingSaturateUnsigned(op2[i - op1.Length], op3);

        public static short ShiftRightArithmeticRoundedNarrowingSaturateUpper(short[] op1, int[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (short)ShiftRightArithmeticRoundedNarrowingSaturate(op2[i - op1.Length], op3);

        public static short ShiftRightArithmeticNarrowingSaturate(int op1, byte op2)
        {
            short result;

            SatQ(SignedShift(op1, (int)(-op2)), out result);

            return result;
        }

        public static ushort ShiftRightArithmeticNarrowingSaturateUnsigned(int op1, byte op2)
        {
            ushort result;

            UnsignedSatQ(SignedShift(op1, (int)(-op2)), out result);

            return result;
        }

        public static ushort ShiftRightArithmeticNarrowingSaturateUnsignedUpper(ushort[] op1, int[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (ushort)ShiftRightArithmeticNarrowingSaturateUnsigned(op2[i - op1.Length], op3);

        public static short ShiftRightArithmeticNarrowingSaturateUpper(short[] op1, int[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (short)ShiftRightArithmeticNarrowingSaturate(op2[i - op1.Length], op3);

        public static short ShiftRightLogicalNarrowing(int op1, byte op2) => (short)UnsignedShift(op1, (int)(-op2));

        public static ushort ShiftRightLogicalNarrowing(uint op1, byte op2) => (ushort)UnsignedShift(op1, (int)(-op2));

        public static short ShiftRightLogicalRoundedNarrowing(int op1, byte op2) => (short)UnsignedShift(op1, (int)(-op2), rounding: true);

        public static ushort ShiftRightLogicalRoundedNarrowing(uint op1, byte op2) => (ushort)UnsignedShift(op1, (int)(-op2), rounding: true);

        public static short ShiftRightLogicalRoundedNarrowingUpper(short[] op1, int[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (short)ShiftRightLogicalRoundedNarrowing(op2[i - op1.Length], op3);

        public static ushort ShiftRightLogicalRoundedNarrowingUpper(ushort[] op1, uint[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (ushort)ShiftRightLogicalRoundedNarrowing(op2[i - op1.Length], op3);

        public static short ShiftRightLogicalRoundedNarrowingSaturate(int op1, byte op2)
        {
            short result;

            SatQ(UnsignedShift(op1, (int)(-op2), rounding: true), out result, reinterpretAsUnsigned: true);

            return result;
        }

        public static ushort ShiftRightLogicalRoundedNarrowingSaturate(uint op1, byte op2)
        {
            ushort result;

            SatQ(UnsignedShift(op1, (int)(-op2), rounding: true), out result);

            return result;
        }

        public static short ShiftRightLogicalRoundedNarrowingSaturateUpper(short[] op1, int[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (short)ShiftRightLogicalRoundedNarrowingSaturate(op2[i - op1.Length], op3);

        public static ushort ShiftRightLogicalRoundedNarrowingSaturateUpper(ushort[] op1, uint[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (ushort)ShiftRightLogicalRoundedNarrowingSaturate(op2[i - op1.Length], op3);

        public static short ShiftRightLogicalNarrowingUpper(short[] op1, int[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (short)ShiftRightLogicalNarrowing(op2[i - op1.Length], op3);

        public static ushort ShiftRightLogicalNarrowingUpper(ushort[] op1, uint[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (ushort)ShiftRightLogicalNarrowing(op2[i - op1.Length], op3);

        public static short ShiftRightLogicalNarrowingSaturate(int op1, byte op2)
        {
            short result;

            SatQ(UnsignedShift(op1, (int)(-op2)), out result, reinterpretAsUnsigned: true);

            return result;
        }

        public static ushort ShiftRightLogicalNarrowingSaturate(uint op1, byte op2)
        {
            ushort result;

            SatQ(UnsignedShift(op1, (int)(-op2)), out result);

            return result;
        }

        public static short ShiftRightLogicalNarrowingSaturateUpper(short[] op1, int[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (short)ShiftRightLogicalNarrowingSaturate(op2[i - op1.Length], op3);

        public static ushort ShiftRightLogicalNarrowingSaturateUpper(ushort[] op1, uint[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (ushort)ShiftRightLogicalNarrowingSaturate(op2[i - op1.Length], op3);

        public static int SignExtendWidening(short op1) => op1;

        public static int SignExtendWideningUpper(short[] op1, int i) => SignExtendWidening(op1[i + op1.Length / 2]);

        private static bool SignedSatQ(long val, out int result)
        {
            bool saturated = false;

            if (val > int.MaxValue)
            {
                result = int.MaxValue;
                saturated = true;
            }
            else if (val < int.MinValue)
            {
                result = int.MinValue;
                saturated = true;
            }
            else
            {
                result = (int)val;
            }

            return saturated;
        }

        private static bool UnsignedSatQ(long val, out uint result)
        {
            bool saturated = false;

            if (val > uint.MaxValue)
            {
                result = uint.MaxValue;
                saturated = true;
            }
            else if (val < 0)
            {
                result = 0;
                saturated = true;
            }
            else
            {
                result = (uint)val;
            }

            return saturated;
        }

        private static bool UnsignedSatQ(ulong val, out uint result)
        {
            bool saturated = false;

            if (val > uint.MaxValue)
            {
                result = uint.MaxValue;
                saturated = true;
            }
            else if (val < 0)
            {
                result = 0;
                saturated = true;
            }
            else
            {
                result = (uint)val;
            }

            return saturated;
        }

        private static bool SatQ(long val, out int result, bool reinterpretAsUnsigned = false)
        {
            bool saturated;

            if (reinterpretAsUnsigned)
            {
                uint res;
                saturated = UnsignedSatQ((ulong)val, out res);
                result = (int)res;
            }
            else
            {
                saturated = SignedSatQ(val, out result);
            }

            return saturated;
        }

        private static bool SatQ(ulong val, out uint result) => UnsignedSatQ(val, out result);

        public static int ExtractNarrowingSaturate(long op1)
        {
            int result;

            SatQ(op1, out result);

            return result;
        }

        public static int ExtractNarrowingSaturateUpper(int[] op1, long[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowingSaturate(op2[i - op1.Length]);

        public static uint ExtractNarrowingSaturate(ulong op1)
        {
            uint result;

            SatQ(op1, out result);

            return result;
        }

        public static uint ExtractNarrowingSaturateUpper(uint[] op1, ulong[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowingSaturate(op2[i - op1.Length]);

        public static uint ExtractNarrowingSaturateUnsigned(long op1)
        {
            uint result;

            UnsignedSatQ(op1, out result);

            return result;
        }

        public static uint ExtractNarrowingSaturateUnsignedUpper(uint[] op1, long[] op2, int i) => i < op1.Length ? op1[i] : ExtractNarrowingSaturateUnsigned(op2[i - op1.Length]);

        private static (long val, bool ovf) MultiplyDoublingOvf(int op1, int op2, bool rounding, long op3, bool subOp)
        {
            long product = (long)((long)op1 * (long)op2);

            bool dblOvf;
            (product, dblOvf) = AddOvf(product, product);

            bool addOvf;
            long accum;

            if (subOp)
            {
                (accum, addOvf) = SubtractOvf(op3, product);
            }
            else
            {
                (accum, addOvf) = AddOvf(op3, product);
            }

            long roundConst = 0;

            if (rounding)
            {
                roundConst = (long)1 << (8 * sizeof(int) - 1);
            }

            bool rndOvf;
            long result;

            (result, rndOvf) = AddOvf(accum, roundConst);

            return (result, addOvf ^ rndOvf ^ dblOvf);
        }

        private static int SaturateHigh(long val, bool ovf)
        {
            if (ovf)
            {
                return val < 0 ? int.MaxValue : int.MinValue;
            }

            return (int)UnsignedShift(val, (long)(-8 * sizeof(int)));
        }

        public static int MultiplyDoublingSaturateHigh(int op1, int op2)
        {
            var (val, ovf) = MultiplyDoublingOvf(op1, op2, rounding: false, (long)0, subOp: false);

            return SaturateHigh(val, ovf);
        }

        public static int MultiplyRoundedDoublingSaturateHigh(int op1, int op2)
        {
            var (val, ovf) = MultiplyDoublingOvf(op1, op2, rounding: true, (long)0, subOp: false);

            return SaturateHigh(val, ovf);
        }

        public static long MultiplyDoublingWideningSaturate(int op1, int op2)
        {
            var (product, ovf) = MultiplyDoublingOvf(op1, op2, rounding: false, (long)0, subOp: false);

            if (ovf)
            {
                return product < 0 ? int.MaxValue : int.MinValue;
            }

            return product;
        }

        public static int MultiplyRoundedDoublingAndAddSaturateHigh(int op1, int op2, int op3)
        {
            long addend = UnsignedShift((long)op1, (long)(8 * sizeof(int)));

            var (val, ovf) = MultiplyDoublingOvf(op2, op3, rounding: true, addend, subOp: false);

            return SaturateHigh(val, ovf);
        }

        public static int MultiplyRoundedDoublingAndSubtractSaturateHigh(int op1, int op2, int op3)
        {
            long minuend = UnsignedShift((long)op1, (long)(8 * sizeof(int)));

            var (val, ovf) = MultiplyDoublingOvf(op2, op3, rounding: true, minuend, subOp: true);

            return SaturateHigh(val, ovf);
        }

        public static int[] MultiplyAddRoundedDoublingSaturateHighRotateComplex(int[] op1, int[] op2, int[] op3, byte rot)
        {
            for (int i = 0; i < op1.Length; i += 2)
            {
                int real = i;
                int img = i + 1;
                (int ans1, int ans2) = rot switch
                {
                    0 => (MultiplyRoundedDoublingAndAddSaturateHigh(op1[real], op2[real], op3[real]), MultiplyRoundedDoublingAndAddSaturateHigh(op1[img], op2[real], op3[img])),
                    1 => (MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[real], op2[img], op3[img]), MultiplyRoundedDoublingAndAddSaturateHigh(op1[img], op2[img], op3[real])),
                    2 => (MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[real], op2[real], op3[real]), MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[img], op2[real], op3[img])),
                    3 => (MultiplyRoundedDoublingAndAddSaturateHigh(op1[real], op2[img], op3[img]), MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[img], op2[img], op3[real])),
                    _ => (default, default)
                };

                op1[real] = ans1;
                op1[img] = ans2;
            }

            return op1;
        }

        public static int[] MultiplyAddRoundedDoublingSaturateHighRotateComplexBySelectedScalar(int[] op1, int[] op2, int[] op3, byte index, byte rot)
        {
            for (int i = 0; i < op1.Length; i += 2)
            {
                int real = i;
                int img = i + 1;
                (int op3Real, int op3Img) = (op3[index * 2], op3[(index * 2) + 1]);
                (int ans1, int ans2) = rot switch
                {
                    0 => (MultiplyRoundedDoublingAndAddSaturateHigh(op1[real], op2[real], op3Real), MultiplyRoundedDoublingAndAddSaturateHigh(op1[img], op2[real], op3Img)),
                    1 => (MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[real], op2[img], op3Img), MultiplyRoundedDoublingAndAddSaturateHigh(op1[img], op2[img], op3Real)),
                    2 => (MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[real], op2[real], op3Real), MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[img], op2[real], op3Img)),
                    3 => (MultiplyRoundedDoublingAndAddSaturateHigh(op1[real], op2[img], op3Img), MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[img], op2[img], op3Real)),
                    _ => (default, default)
                };

                op1[real] = ans1;
                op1[img] = ans2;
            }

            return op1;
        }

        public static long MultiplyDoublingWideningAndAddSaturate(long op1, int op2, int op3) => AddSaturate(op1, MultiplyDoublingWideningSaturate(op2, op3));

        public static long MultiplyDoublingWideningAndSubtractSaturate(long op1, int op2, int op3) => SubtractSaturate(op1, MultiplyDoublingWideningSaturate(op2, op3));

        public static long MultiplyDoublingWideningSaturateUpperByScalar(int[] op1, int op2, int i) => MultiplyDoublingWideningSaturate(op1[i + op1.Length / 2], op2);

        public static long MultiplyDoublingWideningUpperByScalarAndAddSaturate(long[] op1, int[] op2, int op3, int i) => MultiplyDoublingWideningAndAddSaturate(op1[i], op2[i + op2.Length / 2], op3);

        public static long MultiplyDoublingWideningUpperByScalarAndSubtractSaturate(long[] op1, int[] op2, int op3, int i) => MultiplyDoublingWideningAndSubtractSaturate(op1[i], op2[i + op2.Length / 2], op3);

        public static long MultiplyDoublingWideningSaturateUpper(int[] op1, int[] op2, int i) => MultiplyDoublingWideningSaturate(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static long MultiplyDoublingWideningUpperAndAddSaturate(long[] op1, int[] op2, int[] op3, int i) => MultiplyDoublingWideningAndAddSaturate(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static long MultiplyDoublingWideningUpperAndSubtractSaturate(long[] op1, int[] op2, int[] op3, int i) => MultiplyDoublingWideningAndSubtractSaturate(op1[i], op2[i + op2.Length / 2], op3[i + op3.Length / 2]);

        public static long MultiplyDoublingSaturateHigh(long op1, long op2)
        {
            return MultiplyDoublingSaturate(op1, op2, rounding: false, 0, subOp: false);
        }

        public static long MultiplyRoundedDoublingSaturateHigh(long op1, long op2)
        {
            return MultiplyDoublingSaturate(op1, op2, rounding: true, 0, subOp: false);
        }

        public static long MultiplyRoundedDoublingAndAddSaturateHigh(long op1, long op2, long op3)
        {
            return MultiplyDoublingSaturate(op2, op3, rounding: true, op1, subOp: false);
        }

        public static long MultiplyRoundedDoublingAndSubtractSaturateHigh(long op1, long op2, long op3)
        {
            return MultiplyDoublingSaturate(op2, op3, rounding: true, op1, subOp: true);
        }

        private static long MultiplyDoublingSaturate(long op1, long op2, bool rounding, long op3, bool subOp)
        {
            BigInteger a = new BigInteger(op1);
            BigInteger b = new BigInteger(op2);
            BigInteger c = new BigInteger(op3);

            BigInteger result =  a * b << 1;

            int shift = 8 * sizeof(long);
            c = c << shift;
            if (subOp)
            {
                result = c - result;
            }
            else
            {
                result = c + result;
            }

            if (rounding)
            {
                result += new BigInteger(1) << (8 * sizeof(long) - 1);
            }

            result = result >> shift;

            if (result > long.MaxValue)
            {
                return long.MaxValue;
            }
            else if (result < long.MinValue)
            {
                return long.MinValue;
            }

            return long.CreateChecked(result);
        }

        public static long[] MultiplyAddRoundedDoublingSaturateHighRotateComplex(long[] op1, long[] op2, long[] op3, byte rot)
        {
            for (int i = 0; i < op1.Length; i += 2)
            {
                int real = i;
                int img = i + 1;
                (long ans1, long ans2) = rot switch
                {
                    0 => (MultiplyRoundedDoublingAndAddSaturateHigh(op1[real], op2[real], op3[real]), MultiplyRoundedDoublingAndAddSaturateHigh(op1[img], op2[real], op3[img])),
                    1 => (MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[real], op2[img], op3[img]), MultiplyRoundedDoublingAndAddSaturateHigh(op1[img], op2[img], op3[real])),
                    2 => (MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[real], op2[real], op3[real]), MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[img], op2[real], op3[img])),
                    3 => (MultiplyRoundedDoublingAndAddSaturateHigh(op1[real], op2[img], op3[img]), MultiplyRoundedDoublingAndSubtractSaturateHigh(op1[img], op2[img], op3[real])),
                    _ => (default, default)
                };

                op1[real] = ans1;
                op1[img] = ans2;
            }

            return op1;
        }

        public static long ShiftLeftLogicalWidening(int op1, byte op2) => UnsignedShift((long)op1, (long)op2);

        public static ulong ShiftLeftLogicalWidening(uint op1, byte op2) => UnsignedShift((ulong)op1, (long)op2);

        public static long ShiftLeftLogicalWideningUpper(int[] op1, byte op2, int i) => ShiftLeftLogicalWidening(op1[i + op1.Length / 2], op2);

        public static ulong ShiftLeftLogicalWideningUpper(uint[] op1, byte op2, int i) => ShiftLeftLogicalWidening(op1[i + op1.Length / 2], op2);

        public static int ShiftRightArithmeticRoundedNarrowingSaturate(long op1, byte op2)
        {
            int result;

            SatQ(SignedShift(op1, (long)(-op2), rounding: true), out result);

            return result;
        }

        public static uint ShiftRightArithmeticRoundedNarrowingSaturateUnsigned(long op1, byte op2)
        {
            uint result;

            UnsignedSatQ(SignedShift(op1, (long)(-op2), rounding: true), out result);

            return result;
        }

        public static uint ShiftRightArithmeticRoundedNarrowingSaturateUnsignedUpper(uint[] op1, long[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (uint)ShiftRightArithmeticRoundedNarrowingSaturateUnsigned(op2[i - op1.Length], op3);

        public static int ShiftRightArithmeticRoundedNarrowingSaturateUpper(int[] op1, long[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (int)ShiftRightArithmeticRoundedNarrowingSaturate(op2[i - op1.Length], op3);

        public static int ShiftRightArithmeticNarrowingSaturate(long op1, byte op2)
        {
            int result;

            SatQ(SignedShift(op1, (long)(-op2)), out result);

            return result;
        }

        public static uint ShiftRightArithmeticNarrowingSaturateUnsigned(long op1, byte op2)
        {
            uint result;

            UnsignedSatQ(SignedShift(op1, (long)(-op2)), out result);

            return result;
        }

        public static uint ShiftRightArithmeticNarrowingSaturateUnsignedUpper(uint[] op1, long[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (uint)ShiftRightArithmeticNarrowingSaturateUnsigned(op2[i - op1.Length], op3);

        public static int ShiftRightArithmeticNarrowingSaturateUpper(int[] op1, long[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (int)ShiftRightArithmeticNarrowingSaturate(op2[i - op1.Length], op3);

        public static int ShiftRightLogicalNarrowing(long op1, byte op2) => (int)UnsignedShift(op1, (long)(-op2));

        public static uint ShiftRightLogicalNarrowing(ulong op1, byte op2) => (uint)UnsignedShift(op1, (long)(-op2));

        public static int ShiftRightLogicalRoundedNarrowing(long op1, byte op2) => (int)UnsignedShift(op1, (long)(-op2), rounding: true);

        public static uint ShiftRightLogicalRoundedNarrowing(ulong op1, byte op2) => (uint)UnsignedShift(op1, (long)(-op2), rounding: true);

        public static int ShiftRightLogicalRoundedNarrowingUpper(int[] op1, long[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (int)ShiftRightLogicalRoundedNarrowing(op2[i - op1.Length], op3);

        public static uint ShiftRightLogicalRoundedNarrowingUpper(uint[] op1, ulong[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (uint)ShiftRightLogicalRoundedNarrowing(op2[i - op1.Length], op3);

        public static int ShiftRightLogicalRoundedNarrowingSaturate(long op1, byte op2)
        {
            int result;

            SatQ(UnsignedShift(op1, (long)(-op2), rounding: true), out result, reinterpretAsUnsigned: true);

            return result;
        }

        public static uint ShiftRightLogicalRoundedNarrowingSaturate(ulong op1, byte op2)
        {
            uint result;

            SatQ(UnsignedShift(op1, (long)(-op2), rounding: true), out result);

            return result;
        }

        public static int ShiftRightLogicalRoundedNarrowingSaturateUpper(int[] op1, long[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (int)ShiftRightLogicalRoundedNarrowingSaturate(op2[i - op1.Length], op3);

        public static uint ShiftRightLogicalRoundedNarrowingSaturateUpper(uint[] op1, ulong[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (uint)ShiftRightLogicalRoundedNarrowingSaturate(op2[i - op1.Length], op3);

        public static int ShiftRightLogicalNarrowingUpper(int[] op1, long[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (int)ShiftRightLogicalNarrowing(op2[i - op1.Length], op3);

        public static uint ShiftRightLogicalNarrowingUpper(uint[] op1, ulong[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (uint)ShiftRightLogicalNarrowing(op2[i - op1.Length], op3);

        public static int ShiftRightLogicalNarrowingSaturate(long op1, byte op2)
        {
            int result;

            SatQ(UnsignedShift(op1, (long)(-op2)), out result, reinterpretAsUnsigned: true);

            return result;
        }

        public static uint ShiftRightLogicalNarrowingSaturate(ulong op1, byte op2)
        {
            uint result;

            SatQ(UnsignedShift(op1, (long)(-op2)), out result);

            return result;
        }

        private static long GetShift(long shift, long size, bool shiftSat)
        {
            if (shiftSat)
            {
                // SVE shifts are saturated to element size
                shift = (int)ShiftSat(shift, size);
            }
            else
            {
                // NEON shifts are truncated to bottom byte
                shift = (sbyte)shift;
            }
            return shift;
        }

        public static long ShiftSat(long shift, long size)
        {
            if (shift > size + 1)
            {
                return size + 1;
            }
            else if (shift < -(size + 1))
            {
                return -(size + 1);
            }

            return shift;
        }

        public static int ShiftRightLogicalNarrowingSaturateUpper(int[] op1, long[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (int)ShiftRightLogicalNarrowingSaturate(op2[i - op1.Length], op3);

        public static uint ShiftRightLogicalNarrowingSaturateUpper(uint[] op1, ulong[] op2, byte op3, int i) => i < op1.Length ? op1[i] : (uint)ShiftRightLogicalNarrowingSaturate(op2[i - op1.Length], op3);

        public static long SignExtendWidening(int op1) => op1;

        public static long SignExtendWideningUpper(int[] op1, int i) => SignExtendWidening(op1[i + op1.Length / 2]);

        public static sbyte ShiftArithmetic(sbyte op1, sbyte op2) => SignedShift(op1, op2);

        public static sbyte ShiftArithmeticRounded(sbyte op1, sbyte op2) => SignedShift(op1, op2, rounding: true);

        public static sbyte ShiftArithmeticSaturate(sbyte op1, sbyte op2) => SignedShift(op1, op2, saturating: true);

        public static sbyte ShiftArithmeticRoundedSaturate(sbyte op1, sbyte op2) => SignedShift(op1, op2, rounding: true, saturating: true);

        private static sbyte SignedShift(sbyte op1, sbyte op2, bool rounding = false, bool saturating = false, bool shiftSat = false)
        {
            int shift = (int)GetShift(op2, 8, shiftSat);

            sbyte rndCns = 0;

            if (rounding)
            {
                bool ovf;

                (rndCns, ovf) = ShiftOvf((sbyte)1, -shift - 1);

                if (ovf)
                {
                    return 0;
                }
            }

            sbyte result;

            bool addOvf;

            (result, addOvf) = AddOvf(op1, rndCns);

            if (addOvf)
            {
                result = (sbyte)ShiftOvf((byte)result, shift).val;
            }
            else
            {
                bool shiftOvf;

                (result, shiftOvf) = ShiftOvf(result, shift);

                if (saturating)
                {
                    if (shiftOvf)
                    {
                        return op1 > 0 ? sbyte.MaxValue : sbyte.MinValue;
                    }
                }
            }

            return result;
        }

        public static T[] ShiftAndInsert<T>(T[] op1, T op2)
        {
            T nextValue = op2;

            for (int i = 0; i < op1.Length; i++)
            {
                (op1[i], nextValue) = (nextValue, op1[i]);
            }

            return op1;
        }

        public static sbyte ShiftLeftLogical(sbyte op1, byte op2) => UnsignedShift(op1, (sbyte)op2);

        public static byte ShiftLeftLogical(byte op1, byte op2) => UnsignedShift(op1, (sbyte)op2);

        public static sbyte ShiftLeftLogicalSaturate(sbyte op1, byte op2) => SignedShift(op1, (sbyte)op2, saturating: true);

        public static byte ShiftLeftLogicalSaturate(byte op1, byte op2) => UnsignedShift(op1, (sbyte)op2, saturating: true);

        public static byte ShiftLeftLogicalSaturateUnsigned(sbyte op1, byte op2) => (byte)UnsignedShift(op1, (sbyte)op2, saturating: true);

        public static sbyte ShiftLogical(sbyte op1, sbyte op2) => UnsignedShift(op1, op2);

        public static byte ShiftLogical(byte op1, sbyte op2) => UnsignedShift(op1, op2);

        public static byte ShiftLogicalRounded(byte op1, sbyte op2) => UnsignedShift(op1, op2, rounding: true);

        public static sbyte ShiftLogicalRounded(sbyte op1, sbyte op2) => UnsignedShift(op1, op2, rounding: true);

        public static byte ShiftLogicalRoundedSaturate(byte op1, sbyte op2) => UnsignedShift(op1, op2, rounding: true, saturating: true);

        public static sbyte ShiftLogicalRoundedSaturate(sbyte op1, sbyte op2) => UnsignedShift(op1, op2, rounding: true, saturating: true);

        public static sbyte ShiftLogicalSaturate(sbyte op1, sbyte op2) => UnsignedShift(op1, op2, saturating: true);

        public static byte ShiftLogicalSaturate(byte op1, sbyte op2) => UnsignedShift(op1, op2, saturating: true);

        public static sbyte ShiftRightArithmetic(sbyte op1, byte op2) => SignedShift(op1, (sbyte)(-op2));

        public static sbyte ShiftRightArithmeticAdd(sbyte op1, sbyte op2, byte op3) => (sbyte)(op1 + ShiftRightArithmetic(op2, op3));

        public static sbyte ShiftRightArithmeticRounded(sbyte op1, byte op2) => SignedShift(op1, (sbyte)(-op2), rounding: true);

        public static sbyte ShiftRightArithmeticRoundedAdd(sbyte op1, sbyte op2, byte op3) => (sbyte)(op1 + ShiftRightArithmeticRounded(op2, op3));

        public static sbyte ShiftRightLogical(sbyte op1, byte op2) => UnsignedShift(op1, (sbyte)(-op2));

        public static byte ShiftRightLogical(byte op1, byte op2) => UnsignedShift(op1, (sbyte)(-op2));

        public static sbyte ShiftRightLogicalAdd(sbyte op1, sbyte op2, byte op3) => (sbyte)(op1 + ShiftRightLogical(op2, op3));

        public static byte ShiftRightLogicalAdd(byte op1, byte op2, byte op3) => (byte)(op1 + ShiftRightLogical(op2, op3));

        public static sbyte ShiftRightLogicalRounded(sbyte op1, byte op2) => UnsignedShift(op1, (sbyte)(-op2), rounding: true);

        public static byte ShiftRightLogicalRounded(byte op1, byte op2) => UnsignedShift(op1, (sbyte)(-op2), rounding: true);

        public static sbyte ShiftRightLogicalRoundedAdd(sbyte op1, sbyte op2, byte op3) => (sbyte)(op1 + ShiftRightLogicalRounded(op2, op3));

        public static byte ShiftRightLogicalRoundedAdd(byte op1, byte op2, byte op3) => (byte)(op1 + ShiftRightLogicalRounded(op2, op3));

        private static byte UnsignedShift(byte op1, sbyte op2, bool rounding = false, bool saturating = false, bool shiftSat = false)
        {
            int shift = (int)GetShift(op2, 8, shiftSat);

            byte rndCns = 0;

            if (rounding)
            {
                bool ovf;

                (rndCns, ovf) = ShiftOvf((byte)1, -shift - 1);

                if (ovf)
                {
                    return 0;
                }
            }

            (byte result, bool addOvf) = AddOvf(op1, rndCns);

            bool shiftOvf;

            (result, shiftOvf) = ShiftOvf(result, shift);

            if (addOvf)
            {
                byte shiftedCarry = ShiftOvf((byte)1, 8 * sizeof(byte) + shift).val;
                result = (byte)(result | shiftedCarry);
            }

            if (saturating)
            {
                if (shiftOvf)
                {
                    result = byte.MaxValue;
                }
            }

            return result;
        }

        private static sbyte UnsignedShift(sbyte op1, sbyte op2, bool rounding = false, bool saturating = false) => (sbyte)UnsignedShift((byte)op1, op2, rounding, saturating);

        private static (sbyte val, bool ovf) AddOvf(sbyte op1, sbyte op2)
        {
            sbyte result = (sbyte)(op1 + op2);

            bool ovf = false;

            if ((op1 > 0) && (op2 > 0))
            {
                ovf = (result < 0);
            }
            else if ((op1 < 0) && (op2 < 0))
            {
                ovf = (result > 0);
            }

            return (result, ovf);
        }

        private static (sbyte val, bool ovf) AddOvf(sbyte op1, byte op2)
        {
            sbyte result = (sbyte)(op1 + (sbyte)op2);

            bool ovf = (result < op1);

            return (result, ovf);
        }

        private static (byte val, bool ovf) AddOvf(byte op1, sbyte op2)
        {
            byte result = (byte)(op1 + (byte)op2);

            bool ovf;

            if (op2 < 0)
            {
                ovf = (result > op1);
            }
            else
            {
                ovf = (result < op1);
            }

            return (result, ovf);
        }

        private static (byte val, bool ovf) AddOvf(byte op1, byte op2)
        {
            byte result = (byte)(op1 + op2);

            bool ovf = (result < op1);

            return (result, ovf);
        }

        private static (sbyte val, bool ovf) SubtractOvf(sbyte op1, sbyte op2)
        {
            sbyte result = (sbyte)(op1 - op2);

            bool ovf;

            if (op2 < 0)
            {
                ovf = (result < op1);
            }
            else
            {
                ovf = (result > op1);
            }

            return (result, ovf);
        }

        private static (byte val, bool ovf) SubtractOvf(byte op1, byte op2)
        {
            byte result = (byte)(op1 - op2);

            bool ovf = (op1 < op2);

            return (result, ovf);
        }

        public static sbyte AbsSaturate(sbyte op1) => op1 < 0 ? NegateSaturate(op1) : op1;

        public static sbyte AddSaturate(sbyte op1, sbyte op2)
        {
            int result = op1 + op2;
            if (result > sbyte.MaxValue)
            {
                return sbyte.MaxValue;
            }
            else if (result < sbyte.MinValue)
            {
                return sbyte.MinValue;
            }
            else
            {
                return (sbyte)result;
            }
        }

        public static sbyte AddSaturate(sbyte op1, byte op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? sbyte.MaxValue : result;
        }

        public static byte AddSaturate(byte op1, sbyte op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? (result < op1 ? byte.MaxValue : byte.MinValue) : result;
        }

        public static byte AddSaturate(byte op1, byte op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? byte.MaxValue : result;
        }

        public static sbyte[] AddSaturateRotateComplex(sbyte[] op1, sbyte[] op2, byte rot)
        {
            for (int i = 0; i < op1.Length; i += 2)
            {
                int real = i;
                int img = i + 1;

                if (rot == 0)
                {
                    op1[real] = SubtractSaturate(op1[real], op2[img]);
                    op1[img] = AddSaturate(op1[img], op2[real]);
                }
                else
                {
                    op1[real] = AddSaturate(op1[real], op2[img]);
                    op1[img] = SubtractSaturate(op1[img], op2[real]);
                }
            }

            return op1;
        }

        public static double AddSequentialAcross(double[] op1, double[] op2, double[] mask = null)
        {
            // If mask isn't provided, default to all true
            mask = mask ?? Enumerable.Repeat<double>(1.0, op1.Length).ToArray();
            double result = op1[0];

            for (int i = 0; i < op1.Length; i++)
            {
                if (mask[i] != 0.0)
                {
                    result += op2[i];
                }
            }

            return result;
        }

        public static float AddSequentialAcross(float[] op1, float[] op2, float[] mask = null)
        {
            // If mask isn't provided, default to all true
            mask = mask ?? Enumerable.Repeat<float>((float)1.0, op1.Length).ToArray();
            float result = op1[0];

            for (int i = 0; i < op1.Length; i++)
            {
                if (mask[i] != 0.0)
                {
                    result += op2[i];
                }
            }

            return result;
        }

        public static sbyte NegateSaturate(sbyte op1) => SubtractSaturate((sbyte)0, op1);

        public static sbyte SubtractSaturate(sbyte op1, sbyte op2)
        {
            var (result, ovf) = SubtractOvf(op1, op2);
            return ovf ? (result > 0 ? sbyte.MinValue : sbyte.MaxValue) : result;
        }

        public static byte SubtractSaturate(byte op1, byte op2)
        {
            var (result, ovf) = SubtractOvf(op1, op2);
            return ovf ? byte.MinValue : result;
        }

        public static short ShiftArithmetic(short op1, short op2) => SignedShift(op1, op2);

        public static short ShiftArithmeticRounded(short op1, short op2) => SignedShift(op1, op2, rounding: true);

        public static short ShiftArithmeticSaturate(short op1, short op2) => SignedShift(op1, op2, saturating: true);

        public static short ShiftArithmeticRoundedSaturate(short op1, short op2) => SignedShift(op1, op2, rounding: true, saturating: true);

        private static short SignedShift(short op1, short op2, bool rounding = false, bool saturating = false, bool shiftSat = false)
        {
            int shift = (int)GetShift(op2, 16, shiftSat);

            short rndCns = 0;

            if (rounding)
            {
                bool ovf;

                (rndCns, ovf) = ShiftOvf((short)1, -shift - 1);

                if (ovf)
                {
                    return 0;
                }
            }

            short result;

            bool addOvf;

            (result, addOvf) = AddOvf(op1, rndCns);

            if (addOvf)
            {
                result = (short)ShiftOvf((ushort)result, shift).val;
            }
            else
            {
                bool shiftOvf;

                (result, shiftOvf) = ShiftOvf(result, shift);

                if (saturating)
                {
                    if (shiftOvf)
                    {
                        result = op1 < 0 ? short.MinValue : short.MaxValue;
                    }
                }
            }

            return result;
        }

        public static short ShiftLeftLogical(short op1, byte op2) => UnsignedShift(op1, (short)op2);

        public static ushort ShiftLeftLogical(ushort op1, byte op2) => UnsignedShift(op1, (short)op2);

        public static short ShiftLeftLogicalSaturate(short op1, byte op2) => SignedShift(op1, (short)op2, saturating: true);

        public static ushort ShiftLeftLogicalSaturate(ushort op1, byte op2) => UnsignedShift(op1, (short)op2, saturating: true);

        public static ushort ShiftLeftLogicalSaturateUnsigned(short op1, byte op2) => (ushort)UnsignedShift(op1, (short)op2, saturating: true);

        public static short ShiftLogical(short op1, short op2) => UnsignedShift(op1, op2);

        public static ushort ShiftLogical(ushort op1, short op2) => UnsignedShift(op1, op2);

        public static ushort ShiftLogicalRounded(ushort op1, short op2) => UnsignedShift(op1, op2, rounding: true);

        public static short ShiftLogicalRounded(short op1, short op2) => UnsignedShift(op1, op2, rounding: true);

        public static ushort ShiftLogicalRoundedSaturate(ushort op1, short op2) => UnsignedShift(op1, op2, rounding: true, saturating: true);

        public static short ShiftLogicalRoundedSaturate(short op1, short op2) => UnsignedShift(op1, op2, rounding: true, saturating: true);

        public static short ShiftLogicalSaturate(short op1, short op2) => UnsignedShift(op1, op2, saturating: true);

        public static ushort ShiftLogicalSaturate(ushort op1, short op2) => UnsignedShift(op1, op2, saturating: true);

        public static short ShiftRightArithmetic(short op1, byte op2) => SignedShift(op1, (short)(-op2));

        public static short ShiftRightArithmeticAdd(short op1, short op2, byte op3) => (short)(op1 + ShiftRightArithmetic(op2, op3));

        public static short ShiftRightArithmeticRounded(short op1, byte op2) => SignedShift(op1, (short)(-op2), rounding: true);

        public static short ShiftRightArithmeticRoundedAdd(short op1, short op2, byte op3) => (short)(op1 + ShiftRightArithmeticRounded(op2, op3));

        public static short ShiftRightLogical(short op1, byte op2) => UnsignedShift(op1, (short)(-op2));

        public static ushort ShiftRightLogical(ushort op1, byte op2) => UnsignedShift(op1, (short)(-op2));

        public static short ShiftRightLogicalAdd(short op1, short op2, byte op3) => (short)(op1 + ShiftRightLogical(op2, op3));

        public static ushort ShiftRightLogicalAdd(ushort op1, ushort op2, byte op3) => (ushort)(op1 + ShiftRightLogical(op2, op3));

        public static short ShiftRightLogicalRounded(short op1, byte op2) => UnsignedShift(op1, (short)(-op2), rounding: true);

        public static ushort ShiftRightLogicalRounded(ushort op1, byte op2) => UnsignedShift(op1, (short)(-op2), rounding: true);

        public static short ShiftRightLogicalRoundedAdd(short op1, short op2, byte op3) => (short)(op1 + ShiftRightLogicalRounded(op2, op3));

        public static ushort ShiftRightLogicalRoundedAdd(ushort op1, ushort op2, byte op3) => (ushort)(op1 + ShiftRightLogicalRounded(op2, op3));

        private static ushort UnsignedShift(ushort op1, short op2, bool rounding = false, bool saturating = false, bool shiftSat = false)
        {
            int shift = (int)GetShift(op2, 16, shiftSat);

            ushort rndCns = 0;

            if (rounding)
            {
                bool ovf;

                (rndCns, ovf) = ShiftOvf((ushort)1, -shift - 1);

                if (ovf)
                {
                    return 0;
                }
            }

            (ushort result, bool addOvf) = AddOvf(op1, rndCns);

            bool shiftOvf;

            (result, shiftOvf) = ShiftOvf(result, shift);

            if (addOvf)
            {
                ushort shiftedCarry = ShiftOvf((ushort)1, 8 * sizeof(ushort) + shift).val;
                result = (ushort)(result | shiftedCarry);
            }

            if (saturating)
            {
                if (shiftOvf)
                {
                    result = ushort.MaxValue;
                }
            }

            return result;
        }

        private static short UnsignedShift(short op1, short op2, bool rounding = false, bool saturating = false) => (short)UnsignedShift((ushort)op1, op2, rounding, saturating);

        private static (short val, bool ovf) AddOvf(short op1, short op2)
        {
            short result = (short)(op1 + op2);

            bool ovf = false;

            if ((op1 > 0) && (op2 > 0))
            {
                ovf = (result < 0);
            }
            else if ((op1 < 0) && (op2 < 0))
            {
                ovf = (result > 0);
            }

            return (result, ovf);
        }

        private static (short val, bool ovf) AddOvf(short op1, ushort op2)
        {
            short result = (short)(op1 + (short)op2);

            bool ovf = (result < op1);

            return (result, ovf);
        }

        private static (ushort val, bool ovf) AddOvf(ushort op1, short op2)
        {
            ushort result = (ushort)(op1 + (ushort)op2);

            bool ovf;

            if (op2 < 0)
            {
                ovf = (result > op1);
            }
            else
            {
                ovf = (result < op1);
            }

            return (result, ovf);
        }

        private static (ushort val, bool ovf) AddOvf(ushort op1, ushort op2)
        {
            ushort result = (ushort)(op1 + op2);

            bool ovf = (result < op1);

            return (result, ovf);
        }

        private static (short val, bool ovf) SubtractOvf(short op1, short op2)
        {
            short result = (short)(op1 - op2);

            bool ovf;

            if (op2 < 0)
            {
                ovf = (result < op1);
            }
            else
            {
                ovf = (result > op1);
            }

            return (result, ovf);
        }

        private static (ushort val, bool ovf) SubtractOvf(ushort op1, ushort op2)
        {
            ushort result = (ushort)(op1 - op2);

            bool ovf = (op1 < op2);

            return (result, ovf);
        }

        public static short AbsSaturate(short op1) => op1 < 0 ? NegateSaturate(op1) : op1;

        public static short AddSaturate(short op1, short op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? (result > 0 ? short.MinValue : short.MaxValue) : result;
        }

        public static short AddSaturate(short op1, ushort op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? short.MaxValue : result;
        }

        public static ushort AddSaturate(ushort op1, short op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? (result < op1 ? ushort.MaxValue : ushort.MinValue) : result;
        }

        public static ushort AddSaturate(ushort op1, ushort op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? ushort.MaxValue : result;
        }

        public static short[] AddSaturateRotateComplex(short[] op1, short[] op2, byte rot)
        {
            for (int i = 0; i < op1.Length; i += 2)
            {
                int real = i;
                int img = i + 1;

                if (rot == 0)
                {
                    op1[real] = SubtractSaturate(op1[real], op2[img]);
                    op1[img] = AddSaturate(op1[img], op2[real]);
                }
                else
                {
                    op1[real] = AddSaturate(op1[real], op2[img]);
                    op1[img] = SubtractSaturate(op1[img], op2[real]);
                }
            }

            return op1;
        }

        public static short NegateSaturate(short op1) => SubtractSaturate((short)0, op1);

        public static short SubtractSaturate(short op1, short op2)
        {
            var (result, ovf) = SubtractOvf(op1, op2);
            return ovf ? (result > 0 ? short.MinValue : short.MaxValue) : result;
        }

        public static ushort SubtractSaturate(ushort op1, ushort op2)
        {
            var (result, ovf) = SubtractOvf(op1, op2);
            return ovf ? ushort.MinValue : result;
        }

        public static int ShiftArithmetic(int op1, int op2) => SignedShift(op1, op2);

        public static int ShiftArithmeticRounded(int op1, int op2) => SignedShift(op1, op2, rounding: true);

        public static int ShiftArithmeticSaturate(int op1, int op2) => SignedShift(op1, op2, saturating: true);

        public static int ShiftArithmeticRoundedSaturate(int op1, int op2) => SignedShift(op1, op2, rounding: true, saturating: true);

        private static int SignedShift(int op1, int op2, bool rounding = false, bool saturating = false, bool shiftSat = false)
        {
            int shift = (int)GetShift(op2, 32, shiftSat);

            int rndCns = 0;

            if (rounding)
            {
                bool ovf;

                (rndCns, ovf) = ShiftOvf((int)1, -shift - 1);

                if (ovf)
                {
                    return 0;
                }
            }

            int result;

            bool addOvf;

            (result, addOvf) = AddOvf(op1, rndCns);

            if (addOvf)
            {
                result = (int)ShiftOvf((uint)result, shift).val;
            }
            else
            {
                bool shiftOvf;

                (result, shiftOvf) = ShiftOvf(result, shift);

                if (saturating)
                {
                    if (shiftOvf)
                    {
                        result = op1 < 0 ? int.MinValue : int.MaxValue;
                    }
                }
            }

            return result;
        }

        public static int ShiftLeftLogical(int op1, byte op2) => UnsignedShift(op1, (int)op2);

        public static uint ShiftLeftLogical(uint op1, byte op2) => UnsignedShift(op1, (int)op2);

        public static int ShiftLeftLogicalSaturate(int op1, byte op2) => SignedShift(op1, (int)op2, saturating: true);

        public static uint ShiftLeftLogicalSaturate(uint op1, byte op2) => UnsignedShift(op1, (int)op2, saturating: true);

        public static uint ShiftLeftLogicalSaturateUnsigned(int op1, byte op2) => (uint)UnsignedShift(op1, (int)op2, saturating: true);

        public static int ShiftLogical(int op1, int op2) => UnsignedShift(op1, op2);

        public static uint ShiftLogical(uint op1, int op2) => UnsignedShift(op1, op2);

        public static uint ShiftLogicalRounded(uint op1, int op2) => UnsignedShift(op1, op2, rounding: true);

        public static int ShiftLogicalRounded(int op1, int op2) => UnsignedShift(op1, op2, rounding: true);

        public static uint ShiftLogicalRoundedSaturate(uint op1, int op2) => UnsignedShift(op1, op2, rounding: true, saturating: true);

        public static int ShiftLogicalRoundedSaturate(int op1, int op2) => UnsignedShift(op1, op2, rounding: true, saturating: true);

        public static int ShiftLogicalSaturate(int op1, int op2) => UnsignedShift(op1, op2, saturating: true);

        public static uint ShiftLogicalSaturate(uint op1, int op2) => UnsignedShift(op1, op2, saturating: true);

        public static int ShiftRightArithmetic(int op1, byte op2) => SignedShift(op1, (int)(-op2));

        public static int ShiftRightArithmeticAdd(int op1, int op2, byte op3) => (int)(op1 + ShiftRightArithmetic(op2, op3));

        public static int ShiftRightArithmeticRounded(int op1, byte op2) => SignedShift(op1, (int)(-op2), rounding: true);

        public static int ShiftRightArithmeticRoundedAdd(int op1, int op2, byte op3) => (int)(op1 + ShiftRightArithmeticRounded(op2, op3));

        public static int ShiftRightLogical(int op1, byte op2) => UnsignedShift(op1, (int)(-op2));

        public static uint ShiftRightLogical(uint op1, byte op2) => UnsignedShift(op1, (int)(-op2));

        public static int ShiftRightLogicalAdd(int op1, int op2, byte op3) => (int)(op1 + ShiftRightLogical(op2, op3));

        public static uint ShiftRightLogicalAdd(uint op1, uint op2, byte op3) => (uint)(op1 + ShiftRightLogical(op2, op3));

        public static int ShiftRightLogicalRounded(int op1, byte op2) => UnsignedShift(op1, (int)(-op2), rounding: true);

        public static uint ShiftRightLogicalRounded(uint op1, byte op2) => UnsignedShift(op1, (int)(-op2), rounding: true);

        public static int ShiftRightLogicalRoundedAdd(int op1, int op2, byte op3) => (int)(op1 + ShiftRightLogicalRounded(op2, op3));

        public static uint ShiftRightLogicalRoundedAdd(uint op1, uint op2, byte op3) => (uint)(op1 + ShiftRightLogicalRounded(op2, op3));

        private static uint UnsignedShift(uint op1, int op2, bool rounding = false, bool saturating = false, bool shiftSat = false)
        {
            int shift = (int)GetShift(op2, 32, shiftSat);

            uint rndCns = 0;

            if (rounding)
            {
                bool ovf;

                (rndCns, ovf) = ShiftOvf((uint)1, -shift - 1);

                if (ovf)
                {
                    return 0;
                }
            }

            (uint result, bool addOvf) = AddOvf(op1, rndCns);

            bool shiftOvf;

            (result, shiftOvf) = ShiftOvf(result, shift);

            if (addOvf)
            {
                uint shiftedCarry = ShiftOvf((uint)1, 8 * sizeof(uint) + shift).val;
                result = (uint)(result | shiftedCarry);
            }

            if (saturating)
            {
                if (shiftOvf)
                {
                    result = uint.MaxValue;
                }
            }

            return result;
        }

        private static int UnsignedShift(int op1, int op2, bool rounding = false, bool saturating = false) => (int)UnsignedShift((uint)op1, op2, rounding, saturating);

        private static (int val, bool ovf) AddOvf(int op1, int op2)
        {
            int result = (int)(op1 + op2);

            bool ovf = false;

            if ((op1 > 0) && (op2 > 0))
            {
                ovf = (result < 0);
            }
            else if ((op1 < 0) && (op2 < 0))
            {
                ovf = (result > 0);
            }

            return (result, ovf);
        }

        private static (int val, bool ovf) AddOvf(int op1, uint op2)
        {
            int result = (int)(op1 + (int)op2);

            bool ovf = (result < op1);

            return (result, ovf);
        }

        private static (uint val, bool ovf) AddOvf(uint op1, int op2)
        {
            uint result = (uint)(op1 + (uint)op2);

            bool ovf;

            if (op2 < 0)
            {
                ovf = (result > op1);
            }
            else
            {
                ovf = (result < op1);
            }

            return (result, ovf);
        }

        private static (uint val, bool ovf) AddOvf(uint op1, uint op2)
        {
            uint result = (uint)(op1 + op2);

            bool ovf = (result < op1);

            return (result, ovf);
        }

        private static (int val, bool ovf) SubtractOvf(int op1, int op2)
        {
            int result = (int)(op1 - op2);

            bool ovf;

            if (op2 < 0)
            {
                ovf = (result < op1);
            }
            else
            {
                ovf = (result > op1);
            }

            return (result, ovf);
        }

        private static (uint val, bool ovf) SubtractOvf(uint op1, uint op2)
        {
            uint result = (uint)(op1 - op2);

            bool ovf = (op1 < op2);

            return (result, ovf);
        }

        public static int AbsSaturate(int op1) => op1 < 0 ? NegateSaturate(op1) : op1;

        public static int AddSaturate(int op1, int op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? (result > 0 ? int.MinValue : int.MaxValue) : result;
        }

        public static int AddSaturate(int op1, uint op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? int.MaxValue : result;
        }

        public static uint AddSaturate(uint op1, int op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? (result < op1 ? uint.MaxValue : uint.MinValue) : result;
        }

        public static uint AddSaturate(uint op1, uint op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? uint.MaxValue : result;
        }

        public static int[] AddSaturateRotateComplex(int[] op1, int[] op2, byte rot)
        {
            for (int i = 0; i < op1.Length; i += 2)
            {
                int real = i;
                int img = i + 1;

                if (rot == 0)
                {
                    op1[real] = SubtractSaturate(op1[real], op2[img]);
                    op1[img] = AddSaturate(op1[img], op2[real]);
                }
                else
                {
                    op1[real] = AddSaturate(op1[real], op2[img]);
                    op1[img] = SubtractSaturate(op1[img], op2[real]);
                }
            }

            return op1;
        }

        public static int NegateSaturate(int op1) => SubtractSaturate((int)0, op1);

        public static int SubtractSaturate(int op1, int op2)
        {
            var (result, ovf) = SubtractOvf(op1, op2);
            return ovf ? (result > 0 ? int.MinValue : int.MaxValue) : result;
        }

        public static uint SubtractSaturate(uint op1, uint op2)
        {
            var (result, ovf) = SubtractOvf(op1, op2);
            return ovf ? uint.MinValue : result;
        }

        public static long ShiftArithmetic(long op1, long op2) => SignedShift(op1, op2);

        public static long ShiftArithmeticRounded(long op1, long op2) => SignedShift(op1, op2, rounding: true);

        public static long ShiftArithmeticSaturate(long op1, long op2) => SignedShift(op1, op2, saturating: true);

        public static long ShiftArithmeticRoundedSaturate(long op1, long op2) => SignedShift(op1, op2, rounding: true, saturating: true);

        private static long SignedShift(long op1, long op2, bool rounding = false, bool saturating = false, bool shiftSat = false)
        {
            int shift = (int)GetShift(op2, 64, shiftSat);

            long rndCns = 0;

            if (rounding)
            {
                bool ovf;

                (rndCns, ovf) = ShiftOvf((long)1, -shift - 1);

                if (ovf)
                {
                    return 0;
                }
            }

            long result;

            bool addOvf;

            (result, addOvf) = AddOvf(op1, rndCns);

            if (addOvf)
            {
                result = (long)ShiftOvf((ulong)result, shift).val;
            }
            else
            {
                bool shiftOvf;

                (result, shiftOvf) = ShiftOvf(result, shift);

                if (saturating)
                {
                    if (shiftOvf)
                    {
                        result = op1 < 0 ? long.MinValue : long.MaxValue;
                    }
                }
            }

            return result;
        }

        public static long ShiftLeftLogical(long op1, byte op2) => UnsignedShift(op1, (long)op2);

        public static ulong ShiftLeftLogical(ulong op1, byte op2) => UnsignedShift(op1, (long)op2);

        public static long ShiftLeftLogicalSaturate(long op1, byte op2) => SignedShift(op1, (long)op2, saturating: true);

        public static ulong ShiftLeftLogicalSaturate(ulong op1, byte op2) => UnsignedShift(op1, (long)op2, saturating: true);

        public static ulong ShiftLeftLogicalSaturateUnsigned(long op1, byte op2) => (ulong)UnsignedShift(op1, (long)op2, saturating: true);

        public static long ShiftLogical(long op1, long op2) => UnsignedShift(op1, op2);

        public static ulong ShiftLogical(ulong op1, long op2) => UnsignedShift(op1, op2);

        public static ulong ShiftLogicalRounded(ulong op1, long op2) => UnsignedShift(op1, op2, rounding: true);

        public static long ShiftLogicalRounded(long op1, long op2) => UnsignedShift(op1, op2, rounding: true);

        public static ulong ShiftLogicalRoundedSaturate(ulong op1, long op2) => UnsignedShift(op1, op2, rounding: true, saturating: true);

        public static long ShiftLogicalRoundedSaturate(long op1, long op2) => UnsignedShift(op1, op2, rounding: true, saturating: true);

        public static long ShiftLogicalSaturate(long op1, long op2) => UnsignedShift(op1, op2, saturating: true);

        public static ulong ShiftLogicalSaturate(ulong op1, long op2) => UnsignedShift(op1, op2, saturating: true);

        public static long ShiftRightArithmetic(long op1, byte op2) => SignedShift(op1, (long)(-op2));

        public static long ShiftRightArithmeticAdd(long op1, long op2, byte op3) => (long)(op1 + ShiftRightArithmetic(op2, op3));

        public static long ShiftRightArithmeticRounded(long op1, byte op2) => SignedShift(op1, (long)(-op2), rounding: true);

        public static long ShiftRightArithmeticRoundedAdd(long op1, long op2, byte op3) => (long)(op1 + ShiftRightArithmeticRounded(op2, op3));

        public static long ShiftRightLogical(long op1, byte op2) => UnsignedShift(op1, (long)(-op2));

        public static ulong ShiftRightLogical(ulong op1, byte op2) => UnsignedShift(op1, (long)(-op2));

        public static long ShiftRightLogicalAdd(long op1, long op2, byte op3) => (long)(op1 + ShiftRightLogical(op2, op3));

        public static ulong ShiftRightLogicalAdd(ulong op1, ulong op2, byte op3) => (ulong)(op1 + ShiftRightLogical(op2, op3));

        public static long ShiftRightLogicalRounded(long op1, byte op2) => UnsignedShift(op1, (long)(-op2), rounding: true);

        public static ulong ShiftRightLogicalRounded(ulong op1, byte op2) => UnsignedShift(op1, (long)(-op2), rounding: true);

        public static long ShiftRightLogicalRoundedAdd(long op1, long op2, byte op3) => (long)(op1 + ShiftRightLogicalRounded(op2, op3));

        public static ulong ShiftRightLogicalRoundedAdd(ulong op1, ulong op2, byte op3) => (ulong)(op1 + ShiftRightLogicalRounded(op2, op3));

        private static ulong UnsignedShift(ulong op1, long op2, bool rounding = false, bool saturating = false, bool shiftSat = false)
        {
            int shift = (int)GetShift(op2, 64, shiftSat);

            ulong rndCns = 0;

            if (rounding)
            {
                bool ovf;

                (rndCns, ovf) = ShiftOvf((ulong)1, -shift - 1);

                if (ovf)
                {
                    return 0;
                }
            }

            (ulong result, bool addOvf) = AddOvf(op1, rndCns);

            bool shiftOvf;

            (result, shiftOvf) = ShiftOvf(result, shift);

            if (addOvf)
            {
                ulong shiftedCarry = ShiftOvf((ulong)1, 8 * sizeof(ulong) + shift).val;
                result = (ulong)(result | shiftedCarry);
            }

            if (saturating)
            {
                if (shiftOvf)
                {
                    result = ulong.MaxValue;
                }
            }

            return result;
        }

        private static long UnsignedShift(long op1, long op2, bool rounding = false, bool saturating = false) => (long)UnsignedShift((ulong)op1, op2, rounding, saturating);

        private static (long val, bool ovf) AddOvf(long op1, long op2)
        {
            long result = (long)(op1 + op2);

            bool ovf = false;

            if ((op1 > 0) && (op2 > 0))
            {
                ovf = (result < 0);
            }
            else if ((op1 < 0) && (op2 < 0))
            {
                ovf = (result > 0);
            }

            return (result, ovf);
        }

        private static (long val, bool ovf) AddOvf(long op1, ulong op2)
        {
            long result = (long)(op1 + (long)op2);

            bool ovf = (result < op1);

            return (result, ovf);
        }

        private static (ulong val, bool ovf) AddOvf(ulong op1, long op2)
        {
            ulong result = (ulong)(op1 + (ulong)op2);

            bool ovf;

            if (op2 < 0)
            {
                ovf = (result > op1);
            }
            else
            {
                ovf = (result < op1);
            }

            return (result, ovf);
        }

        private static (ulong val, bool ovf) AddOvf(ulong op1, ulong op2)
        {
            ulong result = (ulong)(op1 + op2);

            bool ovf = (result < op1);

            return (result, ovf);
        }

        private static (long val, bool ovf) SubtractOvf(long op1, long op2)
        {
            long result = (long)(op1 - op2);

            bool ovf;

            if (op2 < 0)
            {
                ovf = (result < op1);
            }
            else
            {
                ovf = (result > op1);
            }

            return (result, ovf);
        }

        private static (ulong val, bool ovf) SubtractOvf(ulong op1, ulong op2)
        {
            ulong result = (ulong)(op1 - op2);

            bool ovf = (op1 < op2);

            return (result, ovf);
        }

        public static long AbsSaturate(long op1) => op1 < 0 ? NegateSaturate(op1) : op1;

        public static long AddSaturate(long op1, long op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? (result > 0 ? long.MinValue : long.MaxValue) : result;
        }

        public static long AddSaturate(long op1, ulong op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? long.MaxValue : result;
        }

        public static ulong AddSaturate(ulong op1, long op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? (result < op1 ? ulong.MaxValue : ulong.MinValue) : result;
        }

        public static ulong AddSaturate(ulong op1, ulong op2)
        {
            var (result, ovf) = AddOvf(op1, op2);
            return ovf ? ulong.MaxValue : result;
        }

        public static long[] AddSaturateRotateComplex(long[] op1, long[] op2, byte rot)
        {
            for (int i = 0; i < op1.Length; i += 2)
            {
                int real = i;
                int img = i + 1;

                if (rot == 0)
                {
                    op1[real] = SubtractSaturate(op1[real], op2[img]);
                    op1[img] = AddSaturate(op1[img], op2[real]);
                }
                else
                {
                    op1[real] = AddSaturate(op1[real], op2[img]);
                    op1[img] = SubtractSaturate(op1[img], op2[real]);
                }
            }

            return op1;
        }

        public static long NegateSaturate(long op1) => SubtractSaturate((long)0, op1);

        public static long SubtractSaturate(long op1, long op2)
        {
            var (result, ovf) = SubtractOvf(op1, op2);
            return ovf ? (result > 0 ? long.MinValue : long.MaxValue) : result;
        }

        public static ulong SubtractSaturate(ulong op1, ulong op2)
        {
            var (result, ovf) = SubtractOvf(op1, op2);
            return ovf ? ulong.MinValue : result;
        }


        private static (sbyte val, bool ovf) ShiftOvf(sbyte value, int shift)
        {
            sbyte result = value;

            bool ovf = false;
            sbyte msb = 1;
            msb = (sbyte)(msb << (8 * sizeof(sbyte) - 1));

            for (int i = 0; i < shift; i++)
            {
                ovf = ovf || ((result & msb) != 0);
                result <<= 1;
            }

            for (int i = 0; i > shift; i--)
            {
                result >>= 1;
            }

            if ((value > 0) && (result < 0))
            {
                ovf = true;
            }

            return (result, ovf);
        }


        private static (byte val, bool ovf) ShiftOvf(byte value, int shift)
        {
            byte result = value;

            bool ovf = false;
            byte msb = 1;
            msb = (byte)(msb << (8 * sizeof(byte) - 1));

            for (int i = 0; i < shift; i++)
            {
                ovf = ovf || ((result & msb) != 0);
                result <<= 1;
            }

            for (int i = 0; i > shift; i--)
            {
                result >>= 1;
            }

            if ((value > 0) && (result < 0))
            {
                ovf = true;
            }

            return (result, ovf);
        }


        private static (short val, bool ovf) ShiftOvf(short value, int shift)
        {
            short result = value;

            bool ovf = false;
            short msb = 1;
            msb = (short)(msb << (8 * sizeof(short) - 1));

            for (int i = 0; i < shift; i++)
            {
                ovf = ovf || ((result & msb) != 0);
                result <<= 1;
            }

            for (int i = 0; i > shift; i--)
            {
                result >>= 1;
            }

            if ((value > 0) && (result < 0))
            {
                ovf = true;
            }

            return (result, ovf);
        }


        private static (ushort val, bool ovf) ShiftOvf(ushort value, int shift)
        {
            ushort result = value;

            bool ovf = false;
            ushort msb = 1;
            msb = (ushort)(msb << (8 * sizeof(ushort) - 1));

            for (int i = 0; i < shift; i++)
            {
                ovf = ovf || ((result & msb) != 0);
                result <<= 1;
            }

            for (int i = 0; i > shift; i--)
            {
                result >>= 1;
            }

            if ((value > 0) && (result < 0))
            {
                ovf = true;
            }

            return (result, ovf);
        }


        private static (int val, bool ovf) ShiftOvf(int value, int shift)
        {
            int result = value;

            bool ovf = false;
            int msb = 1;
            msb = (int)(msb << (8 * sizeof(int) - 1));

            for (int i = 0; i < shift; i++)
            {
                ovf = ovf || ((result & msb) != 0);
                result <<= 1;
            }

            for (int i = 0; i > shift; i--)
            {
                result >>= 1;
            }

            if ((value > 0) && (result < 0))
            {
                ovf = true;
            }

            return (result, ovf);
        }


        private static (uint val, bool ovf) ShiftOvf(uint value, int shift)
        {
            uint result = value;

            bool ovf = false;
            uint msb = 1;
            msb = (uint)(msb << (8 * sizeof(uint) - 1));

            for (int i = 0; i < shift; i++)
            {
                ovf = ovf || ((result & msb) != 0);
                result <<= 1;
            }

            for (int i = 0; i > shift; i--)
            {
                result >>= 1;
            }

            if ((value > 0) && (result < 0))
            {
                ovf = true;
            }

            return (result, ovf);
        }


        private static (long val, bool ovf) ShiftOvf(long value, int shift)
        {
            long result = value;

            bool ovf = false;
            long msb = 1;
            msb = (long)(msb << (8 * sizeof(long) - 1));

            for (int i = 0; i < shift; i++)
            {
                ovf = ovf || ((result & msb) != 0);
                result <<= 1;
            }

            for (int i = 0; i > shift; i--)
            {
                result >>= 1;
            }

            if ((value > 0) && (result < 0))
            {
                ovf = true;
            }

            return (result, ovf);
        }


        private static (ulong val, bool ovf) ShiftOvf(ulong value, int shift)
        {
            ulong result = value;

            bool ovf = false;
            ulong msb = 1;
            msb = (ulong)(msb << (8 * sizeof(ulong) - 1));

            for (int i = 0; i < shift; i++)
            {
                ovf = ovf || ((result & msb) != 0);
                result <<= 1;
            }

            for (int i = 0; i > shift; i--)
            {
                result >>= 1;
            }

            if ((value > 0) && (result < 0))
            {
                ovf = true;
            }

            return (result, ovf);
        }

        public static float AbsoluteDifference(float op1, float op2) => MathF.Abs(op1 - op2);

        public static float ConvertToSingleEvenRoundToOdd(double[] value, int i)
        {
            if (i % 2 == 0)
            {
                double val = value[i / 2];
                float floatVal = (float)val;

                float f = (float)val;

                // If val is NaN or Inf there’s nothing else to do
                if (double.IsNaN(val) || double.IsInfinity(val))
                    return f;

                // Detect the cases where the default cast rounded away from zero
                if ((val > 0 && (double)f > val) ||
                    (val < 0 && (double)f < val))
                {
                    // Move toward zero to get truncate() behaviour.
                    int bits = BitConverter.SingleToInt32Bits(f);
                    bits += (val > 0) ? -1 : +1;
                    f = BitConverter.Int32BitsToSingle(bits);
                }

                // Round to odd, force the last bit of the mantissa to 1 if the conversion was inexact
                if (val != (double)f)
                {
                    int bits = BitConverter.SingleToInt32Bits(f);
                    bits |= 0x1;
                    f = BitConverter.Int32BitsToSingle(bits);
                }

                return f;
            }

            return 0f;
        }

        public static float FusedMultiplyAdd(float op1, float op2, float op3) => MathF.FusedMultiplyAdd(op2, op3, op1);

        public static float FusedMultiplyAddNegated(float op1, float op2, float op3) => MathF.FusedMultiplyAdd(-op2, op3, -op1);

        public static float FusedMultiplySubtract(float op1, float op2, float op3) => MathF.FusedMultiplyAdd(-op2, op3, op1);

        public static float FusedMultiplySubtractNegated(float op1, float op2, float op3) => MathF.FusedMultiplyAdd(op2, op3, -op1);

        public static float MaxNumber(float op1, float op2) => float.IsNaN(op1) ? op2 : (float.IsNaN(op2) ? op1 : MathF.Max(op1, op2));

        public static float MaxNumberPairwise(float[] op1, int i) => Pairwise(MaxNumber, op1, i);

        public static float MaxNumberPairwise(float[] op1, float[] op2, int i) => Pairwise(MaxNumber, op1, op2, i);

        public static float MaxNumberPairwiseSve(float[] op1, float[] op2, int i) => (i % 2 == 0) ? MaxNumber(op1[i], op1[i + 1]) : MaxNumber(op2[i - 1], op2[i]);

        public static float MaxPairwiseSve(float[] op1, float[] op2, int i) => (i % 2 == 0) ? Max(op1[i], op1[i + 1]) : Max(op2[i - 1], op2[i]);

        public static float MinNumber(float op1, float op2) => float.IsNaN(op1) ? op2 : (float.IsNaN(op2) ? op1 : MathF.Min(op1, op2));

        public static float MinNumberPairwise(float[] op1, int i) => Pairwise(MinNumber, op1, i);

        public static float MinNumberPairwise(float[] op1, float[] op2, int i) => Pairwise(MinNumber, op1, op2, i);

        public static float MinNumberPairwiseSve(float[] op1, float[] op2, int i) => (i % 2 == 0) ? MinNumber(op1[i], op1[i + 1]) : MinNumber(op2[i - 1], op2[i]);

        public static float MinPairwiseSve(float[] op1, float[] op2, int i) => (i % 2 == 0) ? Min(op1[i], op1[i + 1]) : Min(op2[i - 1], op2[i]);

        public static float[] MultiplyAddRotateComplex(float[] op1, float[] op2, float[] op3, byte imm)
        {
            for (int i = 0; i < op1.Length; i += 2)
            {
                int real = i;
                int img = i + 1;
                (float ans1, float ans2) = imm switch
                {
                    0 => (FusedMultiplyAdd(op1[real], op2[real], op3[real]), FusedMultiplyAdd(op1[img], op2[real], op3[img])),
                    1 => (FusedMultiplySubtract(op1[real], op2[img], op3[img]), FusedMultiplyAdd(op1[img], op2[img], op3[real])),
                    2 => (FusedMultiplySubtract(op1[real], op2[real], op3[real]), FusedMultiplySubtract(op1[img], op2[real], op3[img])),
                    3 => (FusedMultiplyAdd(op1[real], op2[img], op3[img]), FusedMultiplySubtract(op1[img], op2[img], op3[real])),
                    _ => (0.0f, 0.0f)
                };

                op1[real] = ans1;
                op1[img] = ans2;
            }

            return op1;
        }

        public static T[] MultiplyAddRotateComplex<T>(T[] op1, T[] op2, T[] op3, byte imm) where T : INumber<T>
        {
            for (int i = 0; i < op1.Length; i += 2)
            {
                int real = i;
                int img = i + 1;
                (T ans1, T ans2) = imm switch
                {
                    0 => (op1[real] + op2[real] * op3[real], op1[img] + op2[real] * op3[img]),
                    1 => (op1[real] - op2[img] * op3[img], op1[img] + op2[img] * op3[real]),
                    2 => (op1[real] - op2[real] * op3[real], op1[img] - op2[real] * op3[img]),
                    3 => (op1[real] + op2[img] * op3[img], op1[img] - op2[img] * op3[real]),
                    _ => (default, default)
                };

                op1[real] = ans1;
                op1[img] = ans2;
            }

            return op1;
        }

        public static float[] MultiplyAddRotateComplexBySelectedScalar(float[] op1, float[] op2, float[] op3, byte index, byte imm)
        {
            for (int i = 0; i < op1.Length; i += 2)
            {
                int real = i;
                int img = i + 1;
                (float op3Real, float op3Img) = (op3[index * 2], op3[(index * 2) + 1]);
                (float ans1, float ans2) = imm switch
                {
                    0 => (FusedMultiplyAdd(op1[real], op2[real], op3Real), FusedMultiplyAdd(op1[img], op2[real], op3Img)),
                    1 => (FusedMultiplySubtract(op1[real], op2[img], op3Img), FusedMultiplyAdd(op1[img], op2[img], op3Real)),
                    2 => (FusedMultiplySubtract(op1[real], op2[real], op3Real), FusedMultiplySubtract(op1[img], op2[real], op3Img)),
                    3 => (FusedMultiplyAdd(op1[real], op2[img], op3Img), FusedMultiplySubtract(op1[img], op2[img], op3Real)),
                    _ => (0.0f, 0.0f)
                };

                op1[real] = ans1;
                op1[img] = ans2;
            }

            return op1;
        }

        public static T[] MultiplyAddRotateComplexBySelectedScalar<T>(T[] op1, T[] op2, T[] op3, byte index, byte imm) where T : INumber<T>
        {
            for (int i = 0; i < op1.Length; i += 2)
            {
                int real = i;
                int img = i + 1;
                (T op3Real, T op3Img) = (op3[index * 2], op3[(index * 2) + 1]);
                (T ans1, T ans2) = imm switch
                {
                    0 => (op1[real] + op2[real] * op3Real, op1[img] + op2[real] * op3Img),
                    1 => (op1[real] - op2[img] * op3Img, op1[img] + op2[img] * op3Real),
                    2 => (op1[real] - op2[real] * op3Real, op1[img] - op2[real] * op3Img),
                    3 => (op1[real] + op2[img] * op3Img, op1[img] - op2[img] * op3Real),
                    _ => (default, default)
                };

                op1[real] = ans1;
                op1[img] = ans2;
            }

            return op1;
        }

        public static float MultiplyExtended(float op1, float op2)
        {
            bool inf1 = float.IsInfinity(op1);
            bool inf2 = float.IsInfinity(op2);

            bool zero1 = (op1 == 0);
            bool zero2 = (op2 == 0);

            if ((inf1 && zero2) || (zero1 && inf2))
            {
                return MathF.CopySign(2, (zero1 ? op2 : op1));
            }
            else
            {
                return op1 * op2;
            }
        }

        public static float TrigonometricMultiplyAddCoefficient(float op1, float op2, byte imm)
        {
            int index = (op2 < 0) ? (imm + 8) : imm;
            uint coeff = index switch
            {
                0 => 0x3f800000,
                1 => 0xbe2aaaab,
                2 => 0x3c088886,
                3 => 0xb95008b9,
                4 => 0x36369d6d,
                5 => 0x00000000,
                6 => 0x00000000,
                7 => 0x00000000,
                8 => 0x3f800000,
                9 => 0xbf000000,
                10 => 0x3d2aaaa6,
                11 => 0xbab60705,
                12 => 0x37cd37cc,
                13 => 0x00000000,
                14 => 0x00000000,
                15 => 0x00000000,
                _ => 0x00000000
            };

            return MathF.FusedMultiplyAdd(op1, Math.Abs(op2), BitConverter.UInt32BitsToSingle(coeff));
        }

        public static float TrigonometricSelectCoefficient(float op1, uint op2)
        {
            float result = ((op2 % 2) == 0) ? op1 : (float)1.0;
            bool isNegative = (op2 & 0b10) == 0b10;

            if (isNegative != (result < 0))
            {
                result *= -1;
            }

            return result;
        }

        public static float TrigonometricStartingValue(float op1, uint op2)
        {
            float result = op1 * op1;

            if (float.IsNaN(result))
            {
                return result;
            }

            if ((op2 % 2) == 1)
            {
                result *= -1;
            }

            return result;
        }

        public static float FPExponentialAccelerator(uint op1)
        {
            uint index = op1 & 0b111111;
            uint coeff = index switch
            {
                0 => 0x000000,
                1 => 0x0164d2,
                2 => 0x02cd87,
                3 => 0x043a29,
                4 => 0x05aac3,
                5 => 0x071f62,
                6 => 0x08980f,
                7 => 0x0a14d5,
                8 => 0x0b95c2,
                9 => 0x0d1adf,
                10 => 0x0ea43a,
                11 => 0x1031dc,
                12 => 0x11c3d3,
                13 => 0x135a2b,
                14 => 0x14f4f0,
                15 => 0x16942d,
                16 => 0x1837f0,
                17 => 0x19e046,
                18 => 0x1b8d3a,
                19 => 0x1d3eda,
                20 => 0x1ef532,
                21 => 0x20b051,
                22 => 0x227043,
                23 => 0x243516,
                24 => 0x25fed7,
                25 => 0x27cd94,
                26 => 0x29a15b,
                27 => 0x2b7a3a,
                28 => 0x2d583f,
                29 => 0x2f3b79,
                30 => 0x3123f6,
                31 => 0x3311c4,
                32 => 0x3504f3,
                33 => 0x36fd92,
                34 => 0x38fbaf,
                35 => 0x3aff5b,
                36 => 0x3d08a4,
                37 => 0x3f179a,
                38 => 0x412c4d,
                39 => 0x4346cd,
                40 => 0x45672a,
                41 => 0x478d75,
                42 => 0x49b9be,
                43 => 0x4bec15,
                44 => 0x4e248c,
                45 => 0x506334,
                46 => 0x52a81e,
                47 => 0x54f35b,
                48 => 0x5744fd,
                49 => 0x599d16,
                50 => 0x5bfbb8,
                51 => 0x5e60f5,
                52 => 0x60ccdf,
                53 => 0x633f89,
                54 => 0x65b907,
                55 => 0x68396a,
                56 => 0x6ac0c7,
                57 => 0x6d4f30,
                58 => 0x6fe4ba,
                59 => 0x728177,
                60 => 0x75257d,
                61 => 0x77d0df,
                62 => 0x7a83b3,
                63 => 0x7d3e0c,
                _ => 0x000000
            };

            uint result = ((op1 & 0b11111111000000) << 17) | coeff;
            return BitConverter.UInt32BitsToSingle(result);
        }

        public static float FPReciprocalStepFused(float op1, float op2) => FusedMultiplySubtract(2, op1, op2);

        public static float FPReciprocalSqrtStepFused(float op1, float op2) => FusedMultiplySubtract(3, op1, op2) / 2;

        public static double AbsoluteDifference(double op1, double op2) => Math.Abs(op1 - op2);

        public static double FusedMultiplyAdd(double op1, double op2, double op3) => Math.FusedMultiplyAdd(op2, op3, op1);

        public static double FusedMultiplyAddNegated(double op1, double op2, double op3) => Math.FusedMultiplyAdd(-op2, op3, -op1);

        public static double FusedMultiplySubtract(double op1, double op2, double op3) => Math.FusedMultiplyAdd(-op2, op3, op1);

        public static double FusedMultiplySubtractNegated(double op1, double op2, double op3) => Math.FusedMultiplyAdd(op2, op3, -op1);

        public static double MaxNumber(double op1, double op2) => double.IsNaN(op1) ? op2 : (double.IsNaN(op2) ? op1 : Math.Max(op1, op2));

        public static double MaxNumberPairwise(double[] op1, int i) => Pairwise(MaxNumber, op1, i);

        public static double MaxNumberPairwise(double[] op1, double[] op2, int i) => Pairwise(MaxNumber, op1, op2, i);

        public static double MaxPairwiseSve(double[] op1, double[] op2, int i) => (i % 2 == 0) ? Max(op1[i], op1[i + 1]) : Max(op2[i - 1], op2[i]);

        public static double MaxNumberPairwiseSve(double[] op1, double[] op2, int i) => (i % 2 == 0) ? MaxNumber(op1[i], op1[i + 1]) : MaxNumber(op2[i - 1], op2[i]);

        public static double MinNumber(double op1, double op2) => double.IsNaN(op1) ? op2 : (double.IsNaN(op2) ? op1 : Math.Min(op1, op2));

        public static double MinNumberPairwise(double[] op1, int i) => Pairwise(MinNumber, op1, i);

        public static double MinNumberPairwise(double[] op1, double[] op2, int i) => Pairwise(MinNumber, op1, op2, i);

        public static double MinNumberPairwiseSve(double[] op1, double[] op2, int i) => (i % 2 == 0) ? MinNumber(op1[i], op1[i + 1]) : MinNumber(op2[i - 1], op2[i]);

        public static double MinPairwiseSve(double[] op1, double[] op2, int i) => (i % 2 == 0) ? Min(op1[i], op1[i + 1]) : Min(op2[i - 1], op2[i]);

        public static double[] MultiplyAddRotateComplex(double[] op1, double[] op2, double[] op3, byte imm)
        {
            for (int i = 0; i < op1.Length; i += 2)
            {
                int real = i;
                int img = i + 1;
                (double ans1, double ans2) = imm switch
                {
                    0 => (FusedMultiplyAdd(op1[real], op2[real], op3[real]), FusedMultiplyAdd(op1[img], op2[real], op3[img])),
                    1 => (FusedMultiplySubtract(op1[real], op2[img], op3[img]), FusedMultiplyAdd(op1[img], op2[img], op3[real])),
                    2 => (FusedMultiplySubtract(op1[real], op2[real], op3[real]), FusedMultiplySubtract(op1[img], op2[real], op3[img])),
                    3 => (FusedMultiplyAdd(op1[real], op2[img], op3[img]), FusedMultiplySubtract(op1[img], op2[img], op3[real])),
                    _ => (0.0, 0.0)
                };

                op1[real] = ans1;
                op1[img] = ans2;
            }

            return op1;
        }

        public static double MultiplyExtended(double op1, double op2)
        {
            bool inf1 = double.IsInfinity(op1);
            bool inf2 = double.IsInfinity(op2);

            bool zero1 = (op1 == 0);
            bool zero2 = (op2 == 0);

            if ((inf1 && zero2) || (zero1 && inf2))
            {
                return Math.CopySign(2, (zero1 ? op2 : op1));
            }
            else
            {
                return op1 * op2;
            }
        }

        public static double TrigonometricMultiplyAddCoefficient(double op1, double op2, byte imm)
        {
            int index = (op2 < 0) ? (imm + 8) : imm;
            ulong coeff = index switch
            {
                0 => 0x3ff0000000000000,
                1 => 0xbfc5555555555543,
                2 => 0x3f8111111110f30c,
                3 => 0xbf2a01a019b92fc6,
                4 => 0x3ec71de351f3d22b,
                5 => 0xbe5ae5e2b60f7b91,
                6 => 0x3de5d8408868552f,
                7 => 0x0000000000000000,
                8 => 0x3ff0000000000000,
                9 => 0xbfe0000000000000,
                10 => 0x3fa5555555555536,
                11 => 0xbf56c16c16c13a0b,
                12 => 0x3efa01a019b1e8d8,
                13 => 0xbe927e4f7282f468,
                14 => 0x3e21ee96d2641b13,
                15 => 0xbda8f76380fbb401,
                _ => 0x0000000000000000
            };

            return Math.FusedMultiplyAdd(op1, Math.Abs(op2), BitConverter.UInt64BitsToDouble(coeff));
        }

        public static double TrigonometricSelectCoefficient(double op1, ulong op2)
        {
            double result = ((op2 % 2) == 0) ? op1 : 1.0;
            bool isNegative = (op2 & 0b10) == 0b10;

            if (isNegative != (result < 0))
            {
                result *= -1;
            }

            return result;
        }

        public static double TrigonometricStartingValue(double op1, ulong op2)
        {
            double result = op1 * op1;

            if (double.IsNaN(result))
            {
                return result;
            }

            if ((op2 % 2) == 1)
            {
                result *= -1;
            }

            return result;
        }

        public static double FPExponentialAccelerator(ulong op1)
        {
            ulong index = op1 & 0b111111;
            ulong coeff = index switch
            {
                0 => 0x0000000000000,
                1 => 0x02C9A3E778061,
                2 => 0x059B0D3158574,
                3 => 0x0874518759BC8,
                4 => 0x0B5586CF9890F,
                5 => 0x0E3EC32D3D1A2,
                6 => 0x11301D0125B51,
                7 => 0x1429AAEA92DE0,
                8 => 0x172B83C7D517B,
                9 => 0x1A35BEB6FCB75,
                10 => 0x1D4873168B9AA,
                11 => 0x2063B88628CD6,
                12 => 0x2387A6E756238,
                13 => 0x26B4565E27CDD,
                14 => 0x29E9DF51FDEE1,
                15 => 0x2D285A6E4030B,
                16 => 0x306FE0A31B715,
                17 => 0x33C08B26416FF,
                18 => 0x371A7373AA9CB,
                19 => 0x3A7DB34E59FF7,
                20 => 0x3DEA64C123422,
                21 => 0x4160A21F72E2A,
                22 => 0x44E086061892D,
                23 => 0x486A2B5C13CD0,
                24 => 0x4BFDAD5362A27,
                25 => 0x4F9B2769D2CA7,
                26 => 0x5342B569D4F82,
                27 => 0x56F4736B527DA,
                28 => 0x5AB07DD485429,
                29 => 0x5E76F15AD2148,
                30 => 0x6247EB03A5585,
                31 => 0x6623882552225,
                32 => 0x6A09E667F3BCD,
                33 => 0x6DFB23C651A2F,
                34 => 0x71F75E8EC5F74,
                35 => 0x75FEB564267C9,
                36 => 0x7A11473EB0187,
                37 => 0x7E2F336CF4E62,
                38 => 0x82589994CCE13,
                39 => 0x868D99B4492ED,
                40 => 0x8ACE5422AA0DB,
                41 => 0x8F1AE99157736,
                42 => 0x93737B0CDC5E5,
                43 => 0x97D829FDE4E50,
                44 => 0x9C49182A3F090,
                45 => 0xA0C667B5DE565,
                46 => 0xA5503B23E255D,
                47 => 0xA9E6B5579FDBF,
                48 => 0xAE89F995AD3AD,
                49 => 0xB33A2B84F15FB,
                50 => 0xB7F76F2FB5E47,
                51 => 0xBCC1E904BC1D2,
                52 => 0xC199BDD85529C,
                53 => 0xC67F12E57D14B,
                54 => 0xCB720DCEF9069,
                55 => 0xD072D4A07897C,
                56 => 0xD5818DCFBA487,
                57 => 0xDA9E603DB3285,
                58 => 0xDFC97337B9B5F,
                59 => 0xE502EE78B3FF6,
                60 => 0xEA4AFA2A490DA,
                61 => 0xEFA1BEE615A27,
                62 => 0xF50765B6E4540,
                63 => 0xFA7C1819E90D8,
                _ => 0x0000000000000
            };

            ulong result = ((op1 & 0b11111111111000000) << 46) | coeff;
            return BitConverter.UInt64BitsToDouble(result);
        }

        public static double FPReciprocalStepFused(double op1, double op2) => FusedMultiplySubtract(2, op1, op2);

        public static double FPReciprocalSqrtStepFused(double op1, double op2) => FusedMultiplySubtract(3, op1, op2) / 2;

        private static uint ReciprocalEstimate(uint a)
        {
            a = a * 2 + 1;

            uint b = (1 << 19) / a;
            uint r = (b + 1) / 2;

            return r;
        }

        private static uint ReciprocalSqrtEstimate(uint a)
        {
            if (a < 256)
            {
                a = a * 2 + 1;
            }
            else
            {
                a = (a >> 1) << 1;
                a = (a + 1) * 2;
            }

            uint b = 512;

            while (a * (b + 1) * (b + 1) < (1 << 28))
            {
                b = b + 1;
            }

            uint r = (b + 1) / 2;

            return r;
        }

        public static double ReciprocalEstimate(double op1) => Math.ReciprocalEstimate(op1);

        public static float ReciprocalEstimate(float op1) => MathF.ReciprocalEstimate(op1);

        public static double ReciprocalExponent(double op1)
        {
            if (double.IsNaN(op1))
            {
                return double.NaN;
            }

            ulong bits = (ulong)BitConverter.DoubleToUInt64Bits(op1);
            ulong exp = bits & 0x7FF0000000000000;

            if (exp == 0)
            {
                // Replace exponent with maximum exponent
                bits ^= exp ^ 0x7FE0000000000000;
            }
            else
            {
                // Invert the exponent
                bits ^= 0x7FF0000000000000;
            }

            // Zero the fraction
            bits &= 0xFFF0000000000000;

            return BitConverter.UInt64BitsToDouble(bits);
        }

        public static float ReciprocalExponent(float op1)
        {
            if (float.IsNaN(op1))
            {
                return float.NaN;
            }

            uint bits = BitConverter.SingleToUInt32Bits(op1);
            uint exp = bits & 0x7F800000;

            if (exp == 0)
            {
                // Replace exponent with maximum exponent
                bits ^= exp ^ 0x7F000000;
            }
            else
            {
                // Invert the exponent
                bits ^= 0x7F800000;
            }

            // Zero the fraction
            bits &= 0xFF800000;

            return BitConverter.UInt32BitsToSingle(bits);
        }

        public static double ReciprocalSqrtEstimate(double op1) => Math.ReciprocalSqrtEstimate(op1);

        public static float ReciprocalSqrtEstimate(float op1) => MathF.ReciprocalSqrtEstimate(op1);

        private static uint ExtractBits(uint val, byte msbPos, byte lsbPos)
        {
            uint andMask = 0;

            for (byte pos = lsbPos; pos <= msbPos; pos++)
            {
                andMask |= (uint)1 << pos;
            }

            return (val & andMask) >> lsbPos;
        }

        public static uint UnsignedReciprocalEstimate(uint op1)
        {
            if ((op1 & 0x8000_0000u) == 0)
                return 0xFFFF_FFFFu;

            uint idx = (op1 >> 23) & 0x1FFu;

            uint estimate = ReciprocalEstimate(idx) & 0x1FFu;

            return estimate << 23;
        }

        public static uint UnsignedReciprocalSqrtEstimate(uint op1)
        {
            if ((op1 & 0xC0000000u) == 0)
                return 0xFFFFFFFFu;

            uint estimate = ReciprocalSqrtEstimate(ExtractBits(op1, 31, 23)) & 0x1FFu;

            return estimate << 23;
        }

        public static T Add<T>(T a, T b) where T : INumber<T> => a + b;
        public static T Subtract<T>(T a, T b) where T : INumber<T> => a - b;
        public static T Multiply<T>(T a, T b) where T : INumber<T> => a * b;
        public static T MultiplyAdd<T>(T a, T b, T c) where T : INumber<T> => a + b * c;
        public static T MultiplySubtract<T>(T a, T b, T c) where T : INumber<T> => a - b * c;
        public static T Max<T>(T a, T b) where T : IComparisonOperators<T, T, bool> => a > b ? a : b;
        public static T Min<T>(T a, T b) where T : IComparisonOperators<T, T, bool> => a < b ? a : b;

        public static T AddPairwise<T>(T[] array, int i)
            where T : unmanaged, INumber<T>
            => Pairwise((a, b) => Add(a, b), array, i);

        public static T AddPairwise<T>(T[] array1, T[] array2, int i)
            where T : unmanaged, INumber<T>
            => Pairwise((a, b) => Add(a, b), array1, array2, i);

        public static T AddPairwiseSve<T>(T[] array1, T[] array2, int i)
            where T : unmanaged, INumber<T>
            => PairwiseSve((a, b) => Add(a, b), array1, array2, i);

        public static T[] AddRotateComplex<T>(T[] op1, T[] op2, byte rot) where T : INumber<T>
        {
            for (int i = 0; i < op1.Length; i += 2)
            {
                int real = i;
                int img = i + 1;

                if (rot == 0)
                {
                    op1[real] -= op2[img];
                    op1[img] += op2[real];
                }
                else
                {
                    op1[real] += op2[img];
                    op1[img] -= op2[real];
                }
            }

            return op1;
        }

        public static T MaxPairwise<T>(T[] array, int i)
            where T : unmanaged, INumber<T>, IComparisonOperators<T, T, bool>
            => Pairwise((a, b) => Max(a, b), array, i);

        public static T MaxPairwise<T>(T[] array1, T[] array2, int i)
            where T : unmanaged, INumber<T>, IComparisonOperators<T, T, bool>
            => Pairwise((a, b) => Max(a, b), array1, array2, i);

        public static T MaxPairwiseSve<T>(T[] array1, T[] array2, int i)
            where T : unmanaged, INumber<T>, IComparisonOperators<T, T, bool>
            => PairwiseSve((a, b) => Max(a, b), array1, array2, i);

        public static T MinPairwise<T>(T[] array, int i)
            where T : unmanaged, INumber<T>, IComparisonOperators<T, T, bool>
            => Pairwise((a, b) => Min(a, b), array, i);

        public static T MinPairwise<T>(T[] array1, T[] array2, int i)
            where T : unmanaged, INumber<T>, IComparisonOperators<T, T, bool>
            => Pairwise((a, b) => Min(a, b), array1, array2, i);

        public static T MinPairwiseSve<T>(T[] array1, T[] array2, int i)
            where T : unmanaged, INumber<T>, IComparisonOperators<T, T, bool>
            => PairwiseSve((a, b) => Min(a, b), array1, array2, i);

        public static T Pairwise<T>(Func<T, T, T> pairOp, T[] op1, int i)
            where T : unmanaged, INumber<T>
        {
            int idx = 2 * i;
            if (idx + 1 < op1.Length)
            {
                return pairOp(op1[idx], op1[idx + 1]);
            }
            return T.Zero;
        }

        public static T Pairwise<T>(Func<T, T, T> pairOp, T[] op1, T[] op2, int i)
            where T : unmanaged, INumber<T>
        {
            int idx = 2 * i;
            if (idx + 1 < op1.Length)
            {
                return pairOp(op1[idx], op1[idx + 1]);
            }
            else
            {
                int offset = idx - op1.Length;
                return pairOp(op2[offset], op2[offset + 1]);
            }
        }

        public static T PairwiseSve<T>(Func<T, T, T> op, T[] array1, T[] array2, int i)
            where T : unmanaged, INumber<T>
        {
            return (i % 2 == 0)
                ? op(array1[i], array1[i + 1])
                : op(array2[i - 1], array2[i]);
        }

        public static T AddAcross<T>(T[] values)
            where T : unmanaged, INumber<T>
            => Reduce((a, b) => Add(a, b), values);

        public static T AndAcross<T>(T[] values)
            where T : unmanaged, IBitwiseOperators<T, T, T>
            => Reduce((a, b) => And(a, b), values);

        public static T OrAcross<T>(T[] values)
            where T : unmanaged, IBitwiseOperators<T, T, T>
            => Reduce((a, b) => Or(a, b), values);

        public static T XorAcross<T>(T[] values)
            where T : unmanaged, IBitwiseOperators<T, T, T>
            => Reduce((a, b) => Xor(a, b), values);

        public static T MaxAcross<T>(T[] values)
            where T : unmanaged, IComparisonOperators<T, T, bool>
            => Reduce((a, b) => Max(a, b), values);

        public static T MinAcross<T>(T[] values)
            where T : unmanaged, IComparisonOperators<T, T, bool>
            => Reduce((a, b) => Min(a, b), values);

        public static T Reduce<T>(Func<T, T, T> reducer, T[] values)
            where T : unmanaged
        {
            T acc = values[0];
            for (int i = 1; i < values.Length; i++)
            {
                acc = reducer(acc, values[i]);
            }

            return acc;
        }

        public static T Reduce<T, U>(Func<T, U, T> reducer, U[] values)
            where T : unmanaged
            where U : unmanaged
        {
            T acc = (T)Convert.ChangeType(values[0], typeof(T));

            for (int i = 1; i < values.Length; i++)
            {
                acc = reducer(acc, values[i]);
            }

            return acc;
        }

        public static float AddAcrossRecursivePairwise(float[] op1) => ReduceRecursivePairwise(Add, op1);

        private static float ReduceRecursivePairwise(Func<float, float, float> reduceOp, float[] op1)
        {
            if (op1.Length == 2)
            {
                return reduceOp(op1[0], op1[1]);
            }

            if (op1.Length % 2 != 0)
            {
                return float.NaN;
            }

            float[] l = new float[op1.Length / 2];
            Array.Copy(op1, 0, l, 0, (op1.Length / 2));
            float l_reduced = ReduceRecursivePairwise(reduceOp, l);

            float[] r = new float[op1.Length / 2];
            Array.Copy(op1, (op1.Length / 2), r, 0, (op1.Length / 2));
            float r_reduced = ReduceRecursivePairwise(reduceOp, r);

            return reduceOp(l_reduced, r_reduced);
        }

        public static double AddAcrossRecursivePairwise(double[] op1) => ReduceRecursivePairwise(Add, op1);

        private static double ReduceRecursivePairwise(Func<double, double, double> reduceOp, double[] op1)
        {
            if (op1.Length == 2)
            {
                return reduceOp(op1[0], op1[1]);
            }

            if (op1.Length % 2 != 0)
            {
                return double.NaN;
            }

            double[] l = new double[op1.Length / 2];
            Array.Copy(op1, 0, l, 0, (op1.Length / 2));
            double l_reduced = ReduceRecursivePairwise(reduceOp, l);

            double[] r = new double[op1.Length / 2];
            Array.Copy(op1, (op1.Length / 2), r, 0, (op1.Length / 2));
            double r_reduced = ReduceRecursivePairwise(reduceOp, r);

            return reduceOp(l_reduced, r_reduced);
        }

        public static float MaxNumberAcross(float[] op1) => Reduce(MaxNumber, op1);

        public static float MinNumberAcross(float[] op1) => Reduce(MinNumber, op1);

        private struct poly128_t
        {
            public ulong lo;
            public ulong hi;

            public static poly128_t operator ^(poly128_t op1, poly128_t op2)
            {
                op1.lo ^= op2.lo;
                op1.hi ^= op2.hi;

                return op1;
            }

            public static poly128_t operator <<(poly128_t val, int shiftAmount)
            {
                for (int i = 0; i < shiftAmount; i++)
                {
                    val.hi <<= 1;

                    if ((val.lo & 0x8000000000000000U) != 0)
                    {
                        val.hi |= 1;
                    }

                    val.lo <<= 1;
                }

                return val;
            }

            public static implicit operator poly128_t(ulong lo)
            {
                poly128_t result = new poly128_t();
                result.lo = lo;
                return result;
            }

            public static explicit operator poly128_t(long lo)
            {
                poly128_t result = new poly128_t();
                result.lo = (ulong)lo;
                return result;
            }
        }

        private static ushort PolynomialMult(byte op1, byte op2)
        {
            ushort result = default(ushort);
            ushort extendedOp2 = (ushort)op2;

            for (int i = 0; i < 8 * sizeof(byte); i++)
            {
                if ((op1 & ((byte)1 << i)) != 0)
                {
                    result = (ushort)(result ^ (extendedOp2 << i));
                }
            }

            return result;
        }

        private static short PolynomialMult(sbyte op1, sbyte op2)
        {
            short result = default(short);
            short extendedOp2 = (short)op2;

            for (int i = 0; i < 8 * sizeof(sbyte); i++)
            {
                if ((op1 & ((sbyte)1 << i)) != 0)
                {
                    result = (short)(result ^ (extendedOp2 << i));
                }
            }

            return result;
        }

        private static ulong PolynomialMult(uint op1, uint op2)
        {
            ulong result = default(ulong);
            ulong extendedOp2 = (ulong)op2;

            for (int i = 0; i < 8 * sizeof(uint); i++)
            {
                if ((op1 & ((uint)1 << i)) != 0)
                {
                    result = (ulong)(result ^ (extendedOp2 << i));
                }
            }

            return result;
        }

        private static poly128_t PolynomialMult(ulong op1, ulong op2)
        {
            poly128_t result = default(poly128_t);
            poly128_t extendedOp2 = (poly128_t)op2;

            for (int i = 0; i < 8 * sizeof(ulong); i++)
            {
                if ((op1 & ((ulong)1 << i)) != 0)
                {
                    result = (poly128_t)(result ^ (extendedOp2 << i));
                }
            }

            return result;
        }

        private static poly128_t PolynomialMult(long op1, long op2)
        {
            poly128_t result = default(poly128_t);
            poly128_t extendedOp2 = (poly128_t)op2;

            for (int i = 0; i < 8 * sizeof(long); i++)
            {
                if ((op1 & ((long)1 << i)) != 0)
                {
                    result = (poly128_t)(result ^ (extendedOp2 << i));
                }
            }

            return result;
        }

        public static long MultiplyHigh(long op1, long op2)
        {
            ulong u0, v0, w0;
            long u1, v1, w1, w2, t;
            u0 = (ulong)op1 & 0xFFFFFFFF;
            u1 = op1 >> 32;
            v0 = (ulong)op2 & 0xFFFFFFFF;
            v1 = op2 >> 32;
            w0 = u0 * v0;
            t = u1 * (long)v0 + (long)(w0 >> 32);
            w1 = t & 0xFFFFFFFF;
            w2 = t >> 32;
            w1 = (long)u0 * v1 + w1;
            return u1 * v1 + w2 + (w1 >> 32);
        }

        public static ulong MultiplyHigh(ulong op1, ulong op2)
        {
            ulong u0, v0, w0;
            ulong u1, v1, w1, w2, t;
            u0 = (ulong)op1 & 0xFFFFFFFF;
            u1 = op1 >> 32;
            v0 = (ulong)op2 & 0xFFFFFFFF;
            v1 = op2 >> 32;
            w0 = u0 * v0;
            t = u1 * (ulong)v0 + (ulong)(w0 >> 32);
            w1 = t & 0xFFFFFFFF;
            w2 = t >> 32;
            w1 = (ulong)u0 * v1 + w1;
            return u1 * v1 + w2 + (w1 >> 32);
        }

        public static byte PolynomialMultiply(byte op1, byte op2) => (byte)PolynomialMult(op1, op2);

        public static ushort PolynomialMultiplyWidening(byte op1, byte op2) => PolynomialMult(op1, op2);

        public static ushort PolynomialMultiplyWideningUpper(byte[] op1, byte[] op2, int i) => PolynomialMultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static sbyte PolynomialMultiply(sbyte op1, sbyte op2) => (sbyte)PolynomialMult(op1, op2);

        public static short PolynomialMultiplyWidening(sbyte op1, sbyte op2) => PolynomialMult(op1, op2);

        public static short PolynomialMultiplyWideningUpper(sbyte[] op1, sbyte[] op2, int i) => PolynomialMultiplyWidening(op1[i + op1.Length / 2], op2[i + op2.Length / 2]);

        public static ulong PolynomialMultiplyWideningLo64(ulong op1, ulong op2) => PolynomialMult(op1, op2).lo;

        public static long PolynomialMultiplyWideningLo64(long op1, long op2) => (long)PolynomialMult(op1, op2).lo;

        public static ulong PolynomialMultiplyWideningHi64(ulong op1, ulong op2) => PolynomialMult(op1, op2).hi;

        public static long PolynomialMultiplyWideningHi64(long op1, long op2) => (long)PolynomialMult(op1, op2).hi;

        public static ulong PolynomialMultiplyWidening(uint op1, uint op2) => PolynomialMult(op1, op2);

        public static sbyte Concat(sbyte[] op1, sbyte[] op2, int i) => (i < op1.Length) ? op1[i] : op2[i - op1.Length];

        public static sbyte ConcatScalar(sbyte[] op1, sbyte[] op2, int i)
        {
            return i switch
            {
                0 => op1[0],
                1 => op2[0],
                _ => 0
            };
        }

        public static sbyte ExtractVector(sbyte[] op1, sbyte[] op2, int op3, int i) => Concat(op1, op2, op3 + i);

        public static sbyte Insert(sbyte[] op1, int op2, sbyte op3, int i) => (op2 != i) ? op1[i] : op3;

        public static byte Concat(byte[] op1, byte[] op2, int i) => (i < op1.Length) ? op1[i] : op2[i - op1.Length];

        public static byte ConcatScalar(byte[] op1, byte[] op2, int i)
        {
            return i switch
            {
                0 => op1[0],
                1 => op2[0],
                _ => 0
            };
        }

        public static byte ExtractVector(byte[] op1, byte[] op2, int op3, int i) => Concat(op1, op2, op3 + i);

        public static byte Insert(byte[] op1, int op2, byte op3, int i) => (op2 != i) ? op1[i] : op3;

        public static short Concat(short[] op1, short[] op2, int i) => (i < op1.Length) ? op1[i] : op2[i - op1.Length];

        public static short ConcatScalar(short[] op1, short[] op2, int i)
        {
            return i switch
            {
                0 => op1[0],
                1 => op2[0],
                _ => 0
            };
        }

        public static short ExtractVector(short[] op1, short[] op2, int op3, int i) => Concat(op1, op2, op3 + i);

        public static short Insert(short[] op1, int op2, short op3, int i) => (op2 != i) ? op1[i] : op3;

        public static ushort Concat(ushort[] op1, ushort[] op2, int i) => (i < op1.Length) ? op1[i] : op2[i - op1.Length];

        public static ushort ConcatScalar(ushort[] op1, ushort[] op2, int i)
        {
            return i switch
            {
                0 => op1[0],
                1 => op2[0],
                _ => 0
            };
        }

        public static ushort ExtractVector(ushort[] op1, ushort[] op2, int op3, int i) => Concat(op1, op2, op3 + i);

        public static ushort Insert(ushort[] op1, int op2, ushort op3, int i) => (op2 != i) ? op1[i] : op3;

        public static int Concat(int[] op1, int[] op2, int i) => (i < op1.Length) ? op1[i] : op2[i - op1.Length];

        public static int ConcatScalar(int[] op1, int[] op2, int i)
        {
            return i switch
            {
                0 => op1[0],
                1 => op2[0],
                _ => 0
            };
        }

        public static int ExtractVector(int[] op1, int[] op2, int op3, int i) => Concat(op1, op2, op3 + i);

        public static int Insert(int[] op1, int op2, int op3, int i) => (op2 != i) ? op1[i] : op3;

        public static uint Concat(uint[] op1, uint[] op2, int i) => (i < op1.Length) ? op1[i] : op2[i - op1.Length];

        public static uint ConcatScalar(uint[] op1, uint[] op2, int i)
        {
            return i switch
            {
                0 => op1[0],
                1 => op2[0],
                _ => 0
            };
        }

        public static uint ExtractVector(uint[] op1, uint[] op2, int op3, int i) => Concat(op1, op2, op3 + i);

        public static uint Insert(uint[] op1, int op2, uint op3, int i) => (op2 != i) ? op1[i] : op3;

        public static long Concat(long[] op1, long[] op2, int i) => (i < op1.Length) ? op1[i] : op2[i - op1.Length];

        public static long ConcatScalar(long[] op1, long[] op2, int i)
        {
            return i switch
            {
                0 => op1[0],
                1 => op2[0],
                _ => 0
            };
        }

        public static long ExtractVector(long[] op1, long[] op2, int op3, int i) => Concat(op1, op2, op3 + i);

        public static long Insert(long[] op1, int op2, long op3, int i) => (op2 != i) ? op1[i] : op3;

        public static ulong Concat(ulong[] op1, ulong[] op2, int i) => (i < op1.Length) ? op1[i] : op2[i - op1.Length];

        public static ulong ConcatScalar(ulong[] op1, ulong[] op2, int i)
        {
            return i switch
            {
                0 => op1[0],
                1 => op2[0],
                _ => 0
            };
        }

        public static ulong ExtractVector(ulong[] op1, ulong[] op2, int op3, int i) => Concat(op1, op2, op3 + i);

        public static ulong Insert(ulong[] op1, int op2, ulong op3, int i) => (op2 != i) ? op1[i] : op3;

        public static float Concat(float[] op1, float[] op2, int i) => (i < op1.Length) ? op1[i] : op2[i - op1.Length];

        public static float ConcatScalar(float[] op1, float[] op2, int i)
        {
            return i switch
            {
                0 => op1[0],
                1 => op2[0],
                _ => 0
            };
        }

        public static float ExtractVector(float[] op1, float[] op2, int op3, int i) => Concat(op1, op2, op3 + i);

        public static float Insert(float[] op1, int op2, float op3, int i) => (op2 != i) ? op1[i] : op3;

        public static double Concat(double[] op1, double[] op2, int i) => (i < op1.Length) ? op1[i] : op2[i - op1.Length];

        public static double ConcatScalar(double[] op1, double[] op2, int i)
        {
            return i switch
            {
                0 => op1[0],
                1 => op2[0],
                _ => 0
            };
        }

        public static double ExtractVector(double[] op1, double[] op2, int op3, int i) => Concat(op1, op2, op3 + i);

        public static double Insert(double[] op1, int op2, double op3, int i) => (op2 != i) ? op1[i] : op3;

        public static int LoadPairScalar(int[] op1, int i) => (i == 0) ? op1[0] : (i == op1.Length / 2) ? op1[1] : (int)0;

        public static uint LoadPairScalar(uint[] op1, int i) => (i == 0) ? op1[0] : (i == op1.Length / 2) ? op1[1] : (uint)0;

        public static float LoadPairScalar(float[] op1, int i) => (i == 0) ? op1[0] : (i == op1.Length / 2) ? op1[1] : (float)0;

        public static sbyte TableVectorExtension(int i, sbyte[] defaultValues, sbyte[] indices, params sbyte[][] table)
        {
            sbyte[] fullTable = table.SelectMany(x => x).ToArray();
            int index = indices[i];

            if (index < 0 || index >= fullTable.Length)
                return defaultValues[i];

            return fullTable[index];
        }

        public static sbyte TableVectorLookup(int i, sbyte[] indices, params sbyte[][] table)
        {
            sbyte[] zeros = new sbyte[indices.Length];
            Array.Fill<sbyte>(zeros, 0, 0, indices.Length);

            return TableVectorExtension(i, zeros, indices, table);
        }

        public static byte TableVectorExtension(int i, byte[] defaultValues, byte[] indices, params byte[][] table)
        {
            byte[] fullTable = table.SelectMany(x => x).ToArray();
            int index = indices[i];

            if (index < 0 || index >= fullTable.Length)
                return defaultValues[i];

            return fullTable[index];
        }

        public static byte TableVectorLookup(int i, byte[] indices, params byte[][] table)
        {
            byte[] zeros = new byte[indices.Length];
            Array.Fill<byte>(zeros, 0, 0, indices.Length);

            return TableVectorExtension(i, zeros, indices, table);
        }

        public static byte ShiftLeftAndInsert(byte left, byte right, byte shift)
        {
            byte mask = (byte)~(byte.MaxValue << shift);
            byte value = (byte)(right << shift);
            byte newval = (byte)(((byte)left & mask) | value);
            return newval;
        }

        public static byte ShiftRightAndInsert(byte left, byte right, byte shift)
        {
            byte mask = (byte)~(byte.MaxValue >> shift);
            byte value = (byte)(right >> shift);
            byte newval = (byte)(((byte)left & mask) | value);
            return newval;
        }

        public static short ShiftLeftAndInsert(short left, short right, byte shift)
        {
            ushort mask = (ushort)~(ushort.MaxValue << shift);
            ushort value = (ushort)(right << shift);
            short newval = (short)(((ushort)left & mask) | value);
            return newval;
        }

        public static short ShiftRightAndInsert(short left, short right, byte shift)
        {
            ushort mask = (ushort)~(ushort.MaxValue >> shift);
            ushort value = (ushort)(right >> shift);
            short newval = (short)(((ushort)left & mask) | value);
            return newval;
        }

        public static int ShiftLeftAndInsert(int left, int right, byte shift)
        {
            uint mask = (uint)~(uint.MaxValue << shift);
            uint value = (uint)(right << shift);
            int newval = (int)(((uint)left & mask) | value);
            return newval;
        }

        public static int ShiftRightAndInsert(int left, int right, byte shift)
        {
            uint mask = (uint)~(uint.MaxValue >> shift);
            uint value = (uint)(right >> shift);
            int newval = (int)(((uint)left & mask) | value);
            return newval;
        }

        public static long ShiftLeftAndInsert(long left, long right, byte shift)
        {
            ulong mask = (ulong)~(ulong.MaxValue << shift);
            ulong value = (ulong)(right << shift);
            long newval = (long)(((ulong)left & mask) | value);
            return newval;
        }

        public static long ShiftRightAndInsert(long left, long right, byte shift)
        {
            ulong mask = (ulong)~(ulong.MaxValue >> shift);
            ulong value = (ulong)(right >> shift);
            long newval = (long)(((ulong)left & mask) | value);
            return newval;
        }

        public static sbyte ShiftLeftAndInsert(sbyte left, sbyte right, byte shift)
        {
            byte mask = (byte)~(byte.MaxValue << shift);
            byte value = (byte)(right << shift);
            sbyte newval = (sbyte)(((byte)left & mask) | value);
            return newval;
        }

        public static sbyte ShiftRightAndInsert(sbyte left, sbyte right, byte shift)
        {
            byte mask = (byte)~(byte.MaxValue >> shift);
            byte value = (byte)(right >> shift);
            sbyte newval = (sbyte)(((byte)left & mask) | value);
            return newval;
        }

        public static ushort ShiftLeftAndInsert(ushort left, ushort right, byte shift)
        {
            ushort mask = (ushort)~(ushort.MaxValue << shift);
            ushort value = (ushort)(right << shift);
            ushort newval = (ushort)(((ushort)left & mask) | value);
            return newval;
        }

        public static ushort ShiftRightAndInsert(ushort left, ushort right, byte shift)
        {
            ushort mask = (ushort)~(ushort.MaxValue >> shift);
            ushort value = (ushort)(right >> shift);
            ushort newval = (ushort)(((ushort)left & mask) | value);
            return newval;
        }

        public static uint ShiftLeftAndInsert(uint left, uint right, byte shift)
        {
            uint mask = (uint)~(uint.MaxValue << shift);
            uint value = (uint)(right << shift);
            uint newval = (uint)(((uint)left & mask) | value);
            return newval;
        }

        public static uint ShiftRightAndInsert(uint left, uint right, byte shift)
        {
            uint mask = (uint)~(uint.MaxValue >> shift);
            uint value = (uint)(right >> shift);
            uint newval = (uint)(((uint)left & mask) | value);
            return newval;
        }

        public static ulong ShiftLeftAndInsert(ulong left, ulong right, byte shift)
        {
            ulong mask = (ulong)~(ulong.MaxValue << shift);
            ulong value = (ulong)(right << shift);
            ulong newval = (ulong)(((ulong)left & mask) | value);
            return newval;
        }

        public static ulong ShiftRightAndInsert(ulong left, ulong right, byte shift)
        {
            ulong mask = (ulong)~(ulong.MaxValue >> shift);
            ulong value = (ulong)(right >> shift);
            ulong newval = (ulong)(((ulong)left & mask) | value);
            return newval;
        }

        public static double Ceiling(double op1) => Math.Ceiling(op1);

        public static double Floor(double op1) => Math.Floor(op1);

        public static double RoundAwayFromZero(double op1) => Math.Round(op1, MidpointRounding.AwayFromZero);

        public static double RoundToNearest(double op1) => Math.Round(op1, MidpointRounding.ToEven);

        public static double RoundToNegativeInfinity(double op1) => Math.Round(op1, MidpointRounding.ToNegativeInfinity);

        public static double RoundToPositiveInfinity(double op1) => Math.Round(op1, MidpointRounding.ToPositiveInfinity);

        public static double RoundToZero(double op1) => Math.Round(op1, MidpointRounding.ToZero);

        public static float Ceiling(float op1) => MathF.Ceiling(op1);

        public static float Floor(float op1) => MathF.Floor(op1);

        public static float RoundAwayFromZero(float op1) => MathF.Round(op1, MidpointRounding.AwayFromZero);

        public static float RoundToNearest(float op1) => MathF.Round(op1, MidpointRounding.ToEven);

        public static float RoundToNegativeInfinity(float op1) => MathF.Round(op1, MidpointRounding.ToNegativeInfinity);

        public static float RoundToPositiveInfinity(float op1) => MathF.Round(op1, MidpointRounding.ToPositiveInfinity);

        public static float RoundToZero(float op1) => MathF.Round(op1, MidpointRounding.ToZero);

        private static int ConvertToInt32(double op1) => (int)Math.Clamp(op1, int.MinValue, int.MaxValue);

        public static int[] ConvertToInt32(double[] op1)
        {
            int[] result = new int[op1.Length * 2];

            for (int i = 0; i < op1.Length; i++)
            {
                int index = i * 2;
                result[index] = ConvertToInt32(op1[i]);
                if (op1[i] < 0)
                {
                    // Sign-extend next lane with all ones
                    result[index + 1] = -1;
                }
            }

            return result;
        }

        public static int[] ConvertToInt32(float[] op1) => Array.ConvertAll(op1, num => ConvertToInt32(num));

        private static long ConvertToInt64(double op1) => (long)Math.Clamp(op1, long.MinValue, long.MaxValue);

        public static long[] ConvertToInt64(double[] op1) => Array.ConvertAll(op1, num => ConvertToInt64(num));

        public static long[] ConvertToInt64(float[] op1)
        {
            long[] result = new long[op1.Length / 2];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = ConvertToInt64(op1[i * 2]);
            }

            return result;
        }

        private static uint ConvertToUInt32(double op1) => (uint)Math.Clamp(op1, uint.MinValue, uint.MaxValue);

        public static uint[] ConvertToUInt32(double[] op1)
        {
            uint[] result = new uint[op1.Length * 2];

            for (int i = 0; i < op1.Length; i++)
            {
                result[i * 2] = ConvertToUInt32(op1[i]);
            }

            return result;
        }

        public static uint[] ConvertToUInt32(float[] op1) => Array.ConvertAll(op1, num => ConvertToUInt32(num));

        private static ulong ConvertToUInt64(double op1) => (ulong)Math.Clamp(op1, ulong.MinValue, ulong.MaxValue);

        public static ulong[] ConvertToUInt64(double[] op1) => Array.ConvertAll(op1, num => ConvertToUInt64(num));

        public static ulong[] ConvertToUInt64(float[] op1)
        {
            ulong[] result = new ulong[op1.Length / 2];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = ConvertToUInt64(op1[i * 2]);
            }

            return result;
        }

        public static Int32 ConvertToInt32RoundAwayFromZero(float op1) => ConvertToInt32(RoundAwayFromZero(op1));

        public static Int32 ConvertToInt32RoundToEven(float op1) => ConvertToInt32(RoundToNearest(op1));

        public static Int32 ConvertToInt32RoundToNegativeInfinity(float op1) => ConvertToInt32(RoundToNegativeInfinity(op1));

        public static Int32 ConvertToInt32RoundToPositiveInfinity(float op1) => ConvertToInt32(RoundToPositiveInfinity(op1));

        public static Int32 ConvertToInt32RoundToZero(float op1) => ConvertToInt32(RoundToZero(op1));

        public static Int64 ConvertToInt64RoundAwayFromZero(double op1) => ConvertToInt64(RoundAwayFromZero(op1));

        public static Int64 ConvertToInt64RoundToEven(double op1) => ConvertToInt64(RoundToNearest(op1));

        public static Int64 ConvertToInt64RoundToNegativeInfinity(double op1) => ConvertToInt64(RoundToNegativeInfinity(op1));

        public static Int64 ConvertToInt64RoundToPositiveInfinity(double op1) => ConvertToInt64(RoundToPositiveInfinity(op1));

        public static Int64 ConvertToInt64RoundToZero(double op1) => ConvertToInt64(RoundToZero(op1));

        public static UInt32 ConvertToUInt32RoundAwayFromZero(float op1) => ConvertToUInt32(RoundAwayFromZero(op1));

        public static UInt32 ConvertToUInt32RoundToEven(float op1) => ConvertToUInt32(RoundToNearest(op1));

        public static UInt32 ConvertToUInt32RoundToNegativeInfinity(float op1) => ConvertToUInt32(RoundToNegativeInfinity(op1));

        public static UInt32 ConvertToUInt32RoundToPositiveInfinity(float op1) => ConvertToUInt32(RoundToPositiveInfinity(op1));

        public static UInt32 ConvertToUInt32RoundToZero(float op1) => ConvertToUInt32(RoundToZero(op1));

        public static UInt64 ConvertToUInt64RoundAwayFromZero(double op1) => ConvertToUInt64(RoundAwayFromZero(op1));

        public static UInt64 ConvertToUInt64RoundToEven(double op1) => ConvertToUInt64(RoundToNearest(op1));

        public static UInt64 ConvertToUInt64RoundToNegativeInfinity(double op1) => ConvertToUInt64(RoundToNegativeInfinity(op1));

        public static UInt64 ConvertToUInt64RoundToPositiveInfinity(double op1) => ConvertToUInt64(RoundToPositiveInfinity(op1));

        public static UInt64 ConvertToUInt64RoundToZero(double op1) => ConvertToUInt64(RoundToZero(op1));

        public static float ConvertToSingle(int op1) => op1;

        public static float ConvertToSingle(uint op1) => op1;

        public static float ConvertToSingle(double op1) => (float)op1;

        public static float[] ConvertToSingle(double[] op1)
        {
            float[] result = new float[op1.Length * 2];

            for (int i = 0; i < op1.Length; i++)
            {
                result[i * 2] = (float)op1[i];
            }

            return result;
        }

        public static float[] ConvertToSingle(int[] op1) => Array.ConvertAll(op1, num => (float)num);

        public static float[] ConvertToSingle(long[] op1)
        {
            float[] result = new float[op1.Length * 2];

            for (int i = 0; i < op1.Length; i++)
            {
                result[i * 2] = (float)op1[i];
            }

            return result;
        }

        public static float[] ConvertToSingle(uint[] op1) => Array.ConvertAll(op1, num => (float)num);

        public static float[] ConvertToSingle(ulong[] op1)
        {
            float[] result = new float[op1.Length * 2];

            for (int i = 0; i < op1.Length; i++)
            {
                result[i * 2] = (float)op1[i];
            }

            return result;
        }

        public static float ConvertToSingleUpper(float[] op1, double[] op2, int i) => i < op1.Length ? op1[i] : ConvertToSingle(op2[i - op1.Length]);

        public static double ConvertToDouble(float op1) => op1;

        public static double ConvertToDouble(long op1) => op1;

        public static double ConvertToDouble(ulong op1) => op1;

        public static double[] ConvertToDouble(float[] op1)
        {
            double[] result = new double[op1.Length / 2];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = op1[i * 2];
            }

            return result;
        }

        public static double[] ConvertToDouble(int[] op1)
        {
            double[] result = new double[op1.Length / 2];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = op1[i * 2];
            }

            return result;
        }

        public static double[] ConvertToDouble(long[] op1) => Array.ConvertAll(op1, num => (double)num);

        public static double[] ConvertToDouble(uint[] op1)
        {
            double[] result = new double[op1.Length / 2];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = op1[i * 2];
            }

            return result;
        }

        public static double[] ConvertToDouble(ulong[] op1) => Array.ConvertAll(op1, num => (double)num);

        public static double ConvertToDoubleUpper(float[] op1, int i) => ConvertToDouble(op1[i + op1.Length / 2]);

        public static short ReverseElement8(short val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(short)) / 8;
            ulong elementMask = ((1UL << 8) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (8 * i)) & elementMask;
                ulong reversedElement = element << (8 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (short)result;
        }

        public static ushort ReverseElement8(ushort val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(ushort)) / 8;
            ulong elementMask = ((1UL << 8) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (8 * i)) & elementMask;
                ulong reversedElement = element << (8 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (ushort)result;
        }

        public static int ReverseElement8(int val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(int)) / 8;
            ulong elementMask = ((1UL << 8) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (8 * i)) & elementMask;
                ulong reversedElement = element << (8 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (int)result;
        }

        public static uint ReverseElement8(uint val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(uint)) / 8;
            ulong elementMask = ((1UL << 8) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (8 * i)) & elementMask;
                ulong reversedElement = element << (8 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (uint)result;
        }

        public static long ReverseElement8(long val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(long)) / 8;
            ulong elementMask = ((1UL << 8) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (8 * i)) & elementMask;
                ulong reversedElement = element << (8 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (long)result;
        }

        public static ulong ReverseElement8(ulong val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(ulong)) / 8;
            ulong elementMask = ((1UL << 8) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (8 * i)) & elementMask;
                ulong reversedElement = element << (8 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (ulong)result;
        }

        public static int ReverseElement16(int val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(int)) / 16;
            ulong elementMask = ((1UL << 16) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (16 * i)) & elementMask;
                ulong reversedElement = element << (16 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (int)result;
        }

        public static uint ReverseElement16(uint val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(uint)) / 16;
            ulong elementMask = ((1UL << 16) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (16 * i)) & elementMask;
                ulong reversedElement = element << (16 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (uint)result;
        }

        public static long ReverseElement16(long val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(long)) / 16;
            ulong elementMask = ((1UL << 16) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (16 * i)) & elementMask;
                ulong reversedElement = element << (16 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (long)result;
        }

        public static ulong ReverseElement16(ulong val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(ulong)) / 16;
            ulong elementMask = ((1UL << 16) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (16 * i)) & elementMask;
                ulong reversedElement = element << (16 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (ulong)result;
        }

        public static long ReverseElement32(long val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(long)) / 32;
            ulong elementMask = ((1UL << 32) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (32 * i)) & elementMask;
                ulong reversedElement = element << (32 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (long)result;
        }

        public static ulong ReverseElement32(ulong val)
        {
            ulong result = 0UL;

            int numberOfElements = (8 * sizeof(ulong)) / 32;
            ulong elementMask = ((1UL << 32) - 1);

            for (int i = 0; i < numberOfElements; i++)
            {
                ulong element = ((ulong)val >> (32 * i)) & elementMask;
                ulong reversedElement = element << (32 * (numberOfElements - 1 - i));
                result |= reversedElement;
            }

            return (ulong)result;
        }

        public static uint DotProduct(uint op1, byte[] op2, int s, byte[] op3, int t)
        {
            uint result = op1;

            for (int i = 0; i < 4; i++)
            {
                result += (uint)((uint)op2[s + i] * (uint)op3[t + i]);
            }

            return result;
        }

        public static int DotProduct(int op1, sbyte[] op2, int s, sbyte[] op3, int t)
        {
            int result = op1;

            for (int i = 0; i < 4; i++)
            {
                result += (int)((int)op2[s + i] * (int)op3[t + i]);
            }

            return result;
        }

        public static ulong DotProduct(ulong op1, ushort[] op2, int s, ushort[] op3, int t)
        {
            ulong result = op1;

            for (int i = 0; i < 4; i++)
            {
                result += (ulong)((ulong)op2[s + i] * (ulong)op3[t + i]);
            }

            return result;
        }

        public static long DotProduct(long op1, short[] op2, int s, short[] op3, int t)
        {
            long result = op1;

            for (int i = 0; i < 4; i++)
            {
                result += (long)((long)op2[s + i] * (long)op3[t + i]);
            }

            return result;
        }

        public static int DotProductRotateComplex(int op1, sbyte[] op2, int s, sbyte[] op3, byte rotation)
        {
            int result = op1;

            int r1 = s;
            int i1 = s + 1;
            int r2 = s + 2;
            int i2 = s + 3;

            switch (rotation)
            {
                case 0:
                    result += ((int)op2[r1] * (int)op3[r1]) - ((int)op2[i1] * (int)op3[i1]) + ((int)op2[r2] * (int)op3[r2]) - ((int)op2[i2] * (int)op3[i2]);
                    break;
                case 1:
                    result += ((int)op2[r1] * (int)op3[i1]) + ((int)op2[i1] * (int)op3[r1]) + ((int)op2[r2] * (int)op3[i2]) + ((int)op2[i2] * (int)op3[r2]);
                    break;
                case 2:
                    result += ((int)op2[r1] * (int)op3[r1]) + ((int)op2[i1] * (int)op3[i1]) + ((int)op2[r2] * (int)op3[r2]) + ((int)op2[i2] * (int)op3[i2]);
                    break;
                case 3:
                    result += ((int)op2[r1] * (int)op3[i1]) - ((int)op2[i1] * (int)op3[r1]) + ((int)op2[r2] * (int)op3[i2]) - ((int)op2[i2] * (int)op3[r2]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rotation), "Invalid rotation value.");
            }

            return result;
        }

        public static int DotProductRotateComplexBySelectedIndex(int op1, sbyte[] op2, int s, sbyte[] op3, int immIndex, byte rotation)
        {
            int result = op1;
            int r1 = s;
            int i1 = s + 1;
            int r2 = s + 2;
            int i2 = s + 3;

            switch (rotation)
            {
                case 0:
                    result += ((int)op2[r1] * (int)op3[immIndex]) - ((int)op2[i1] * (int)op3[immIndex + 1]) + ((int)op2[r2] * (int)op3[immIndex + 2]) - ((int)op2[i2] * (int)op3[immIndex + 3]);
                    break;
                case 1:
                    result += ((int)op2[r1] * (int)op3[immIndex + 1]) + ((int)op2[i1] * (int)op3[immIndex]) + ((int)op2[r2] * (int)op3[immIndex + 3]) + ((int)op2[i2] * (int)op3[immIndex + 2]);
                    break;
                case 2:
                    result += ((int)op2[r1] * (int)op3[immIndex]) + ((int)op2[i1] * (int)op3[immIndex + 1]) + ((int)op2[r2] * (int)op3[immIndex + 2]) + ((int)op2[i2] * (int)op3[immIndex + 3]);
                    break;
                case 3:
                    result += ((int)op2[r1] * (int)op3[immIndex + 1]) - ((int)op2[i1] * (int)op3[immIndex]) + ((int)op2[r2] * (int)op3[immIndex + 3]) - ((int)op2[i2] * (int)op3[immIndex + 2]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rotation), "Invalid rotation value.");
            }

            return result;
        }


        public static long DotProductRotateComplex(long op1, short[] op2, int s, short[] op3, byte rotation)
        {
            long result = op1;

            int r1 = s;
            int i1 = s + 1;
            int r2 = s + 2;
            int i2 = s + 3;

            switch (rotation)
            {
                case 0:
                    result += ((long)op2[r1] * (long)op3[r1]) - ((long)op2[i1] * (long)op3[i1]) + ((long)op2[r2] * (long)op3[r2]) - ((long)op2[i2] * (long)op3[i2]);
                    break;
                case 1:
                    result += ((long)op2[r1] * (long)op3[i1]) + ((long)op2[i1] * (long)op3[r1]) + ((long)op2[r2] * (long)op3[i2]) + ((long)op2[i2] * (long)op3[r2]);
                    break;
                case 2:
                    result += ((long)op2[r1] * (long)op3[r1]) + ((long)op2[i1] * (long)op3[i1]) + ((long)op2[r2] * (long)op3[r2]) + ((long)op2[i2] * (long)op3[i2]);
                    break;
                case 3:
                    result += ((long)op2[r1] * (long)op3[i1]) - ((long)op2[i1] * (long)op3[r1]) + ((long)op2[r2] * (long)op3[i2]) - ((long)op2[i2] * (long)op3[r2]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rotation), "Invalid rotation value.");
            }

            return result;
        }

        public static long DotProductRotateComplexBySelectedIndex(long op1, short[] op2, int s, short[] op3, int immIndex, byte rotation)
        {
            long result = op1;
            int r1 = s;
            int i1 = s + 1;
            int r2 = s + 2;
            int i2 = s + 3;

            switch (rotation)
            {
            case 0:
                result += ((long)op2[r1] * (long)op3[immIndex]) - ((long)op2[i1] * (long)op3[immIndex + 1]) + ((long)op2[r2] * (long)op3[immIndex + 2]) - ((long)op2[i2] * (long)op3[immIndex + 3]);
                break;
            case 1:
                result += ((long)op2[r1] * (long)op3[immIndex + 1]) + ((long)op2[i1] * (long)op3[immIndex]) + ((long)op2[r2] * (long)op3[immIndex + 3]) + ((long)op2[i2] * (long)op3[immIndex + 2]);
                break;
            case 2:
                result += ((long)op2[r1] * (long)op3[immIndex]) + ((long)op2[i1] * (long)op3[immIndex + 1]) + ((long)op2[r2] * (long)op3[immIndex + 2]) + ((long)op2[i2] * (long)op3[immIndex + 3]);
                break;
            case 3:
                result += ((long)op2[r1] * (long)op3[immIndex + 1]) - ((long)op2[i1] * (long)op3[immIndex]) + ((long)op2[r2] * (long)op3[immIndex + 3]) - ((long)op2[i2] * (long)op3[immIndex + 2]);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(rotation), "Invalid rotation value.");
            }

            return result;
        }

        public static int WhileLessThanMask(int op1, int op2)
        {
            return (op1 < op2) ? 1 : 0;
        }

        public static uint WhileLessThanMask(uint op1, uint op2)
        {
            return (uint)((op1 < op2) ? 1 : 0);
        }

        public static long WhileLessThanMask(long op1, long op2)
        {
            return (op1 < op2) ? 1 : 0;
        }

        public static ulong WhileLessThanMask(ulong op1, ulong op2)
        {
            return (ulong)((op1 < op2) ? 1 : 0);
        }

        public static int WhileLessThanOrEqualMask(int op1, int op2)
        {
            return (op1 <= op2) ? 1 : 0;
        }

        public static uint WhileLessThanOrEqualMask(uint op1, uint op2)
        {
            return (uint)((op1 <= op2) ? 1 : 0);
        }

        public static long WhileLessThanOrEqualMask(long op1, long op2)
        {
            return (op1 <= op2) ? 1 : 0;
        }

        public static ulong WhileLessThanOrEqualMask(ulong op1, ulong op2)
        {
            return (ulong)((op1 <= op2) ? 1 : 0);
        }

        public static ulong MaskBothSet(byte[] op1, byte[] op2)
        {
            ulong acc = 0;
            for (var i = 0; i < op1.Length; i++)
            {
                acc += (ulong)((op1[i] == 1 && op2[i] == 1) ? 1 : 0);
            }
            return acc;
        }

        public static ulong MaskBothSet(sbyte[] op1, sbyte[] op2)
        {
            ulong acc = 0;
            for (var i = 0; i < op1.Length; i++)
            {
                acc += (ulong)((op1[i] == 1 && op2[i] == 1) ? 1 : 0);
            }
            return acc;
        }

        public static ulong MaskBothSet(short[] op1, short[] op2)
        {
            ulong acc = 0;
            for (var i = 0; i < op1.Length; i++)
            {
                acc += (ulong)((op1[i] == 1 && op2[i] == 1) ? 1 : 0);
            }
            return acc;
        }

        public static ulong MaskBothSet(ushort[] op1, ushort[] op2)
        {
            ulong acc = 0;
            for (var i = 0; i < op1.Length; i++)
            {
                acc += (ulong)((op1[i] == 1 && op2[i] == 1) ? 1 : 0);
            }
            return acc;
        }

        public static ulong MaskBothSet(int[] op1, int[] op2)
        {
            ulong acc = 0;
            for (var i = 0; i < op1.Length; i++)
            {
                acc += (ulong)((op1[i] == 1 && op2[i] == 1) ? 1 : 0);
            }
            return acc;
        }

        public static ulong MaskBothSet(uint[] op1, uint[] op2)
        {
            ulong acc = 0;
            for (var i = 0; i < op1.Length; i++)
            {
                acc += (ulong)((op1[i] == 1 && op2[i] == 1) ? 1 : 0);
            }
            return acc;
        }

        public static ulong MaskBothSet(long[] op1, long[] op2)
        {
            ulong acc = 0;
            for (var i = 0; i < op1.Length; i++)
            {
                acc += (ulong)((op1[i] == 1 && op2[i] == 1) ? 1 : 0);
            }
            return acc;
        }

        public static ulong MaskBothSet(ulong[] op1, ulong[] op2)
        {
            ulong acc = 0;
            for (var i = 0; i < op1.Length; i++)
            {
                acc += (ulong)((op1[i] == 1 && op2[i] == 1) ? 1 : 0);
            }
            return acc;
        }

        public static ulong MaskBothSet(float[] op1, float[] op2)
        {
            ulong acc = 0;
            for (var i = 0; i < op1.Length; i++)
            {
                acc += (ulong)((op1[i] == 1) && (op2[i] == 1) ? 1 : 0);
            }
            return acc;
        }

        public static ulong MaskBothSet(double[] op1, double[] op2)
        {
            ulong acc = 0;
            for (var i = 0; i < op1.Length; i++)
            {
                acc += (ulong)((op1[i] == 1) && (op2[i] == 1) ? 1 : 0);
            }
            return acc;
        }

        public static byte getMaskByte()
        {
            return (byte)(TestLibrary.Generator.GetByte() % 2);
        }

        public static sbyte getMaskSByte()
        {
            return (sbyte)(TestLibrary.Generator.GetByte() % 2);
        }

        public static short getMaskInt16()
        {
            return (short)(TestLibrary.Generator.GetUInt16() % 2);
        }

        public static ushort getMaskUInt16()
        {
            return (ushort)(TestLibrary.Generator.GetUInt16() % 2);
        }

        public static int getMaskInt32()
        {
            return (int)(TestLibrary.Generator.GetUInt32() % 2);
        }

        public static uint getMaskUInt32()
        {
            return (uint)(TestLibrary.Generator.GetUInt32() % 2);
        }

        public static long getMaskInt64()
        {
            return (long)(TestLibrary.Generator.GetUInt64() % 2);
        }

        public static ulong getMaskUInt64()
        {
            return (ulong)(TestLibrary.Generator.GetUInt64() % 2);
        }

        public static float getMaskSingle()
        {
            return (float)(TestLibrary.Generator.GetUInt32() % 2);
        }

        public static double getMaskDouble()
        {
            return (double)(TestLibrary.Generator.GetUInt64() % 2);
        }

        public static int MaskNumberOfElementsVector(int elems, SveMaskPattern pattern)
        {

            switch (pattern)
            {
                // Returns elems, as this is always a power of 2.
                case SveMaskPattern.LargestPowerOf2:
                    return elems;

                // Returns N if N elements can fit in the vector. Otherwise 0.
                case SveMaskPattern.VectorCount1:
                    return elems >= 1 ? 1 : 0;
                case SveMaskPattern.VectorCount2:
                    return elems >= 2 ? 2 : 0;
                case SveMaskPattern.VectorCount3:
                    return elems >= 3 ? 3 : 0;
                case SveMaskPattern.VectorCount4:
                    return elems >= 4 ? 4 : 0;
                case SveMaskPattern.VectorCount5:
                    return elems >= 5 ? 5 : 0;
                case SveMaskPattern.VectorCount6:
                    return elems >= 6 ? 6 : 0;
                case SveMaskPattern.VectorCount7:
                    return elems >= 7 ? 7 : 0;
                case SveMaskPattern.VectorCount8:
                    return elems >= 8 ? 8 : 0;
                case SveMaskPattern.VectorCount16:
                    return elems >= 16 ? 16 : 0;
                case SveMaskPattern.VectorCount32:
                    return elems >= 32 ? 32 : 0;
                case SveMaskPattern.VectorCount64:
                    return elems >= 64 ? 64 : 0;
                case SveMaskPattern.VectorCount128:
                    return elems >= 128 ? 128 : 0;
                case SveMaskPattern.VectorCount256:
                    return elems >= 256 ? 256 : 0;

                // Number of elems rounded down to nearest multiple of 4
                case SveMaskPattern.LargestMultipleOf4:
                    return elems - (elems % 4);

                // Number of elems rounded down to nearest multiple of 3
                case SveMaskPattern.LargestMultipleOf3:
                    return elems - (elems % 3);

                case SveMaskPattern.All:
                default:
                    return elems;
            }
        }

        public static int NumberOfElementsInVectorInt8(SveMaskPattern pattern)
        {
            return MaskNumberOfElementsVector(Unsafe.SizeOf<Vector<byte>>() / sizeof(byte), pattern);
        }

        public static int NumberOfElementsInVectorInt16(SveMaskPattern pattern)
        {
            return MaskNumberOfElementsVector(Unsafe.SizeOf<Vector<short>>() / sizeof(short), pattern);
        }

        public static int NumberOfElementsInVectorInt32(SveMaskPattern pattern)
        {
            return MaskNumberOfElementsVector(Unsafe.SizeOf<Vector<int>>() / sizeof(int), pattern);
        }

        public static int NumberOfElementsInVectorInt64(SveMaskPattern pattern)
        {
            return MaskNumberOfElementsVector(Unsafe.SizeOf<Vector<long>>() / sizeof(long), pattern);
        }

        public static int NumberOfActiveElementsInMask(sbyte[] mask)
        {
            int acc = 0;
            for (var i = 0; i < mask.Length; i++)
            {
                acc += (mask[i] == 1) ? 1 : 0;
            }
            return acc;
        }

        public static int NumberOfActiveElementsInMask(short[] mask)
        {
            int acc = 0;
            for (var i = 0; i < mask.Length; i++)
            {
                acc += (mask[i] == 1) ? 1 : 0;
            }
            return acc;
        }

        public static int NumberOfActiveElementsInMask(int[] mask)
        {
            int acc = 0;
            for (var i = 0; i < mask.Length; i++)
            {
                acc += (mask[i] == 1) ? 1 : 0;
            }
            return acc;
        }

        public static int NumberOfActiveElementsInMask(long[] mask)
        {
            int acc = 0;
            for (var i = 0; i < mask.Length; i++)
            {
                acc += (mask[i] == 1) ? 1 : 0;
            }
            return acc;
        }

        public static int NumberOfActiveElementsInMask(byte[] mask)
        {
            int acc = 0;
            for (var i = 0; i < mask.Length; i++)
            {
                acc += (mask[i] == 1) ? 1 : 0;
            }
            return acc;
        }

        public static int NumberOfActiveElementsInMask(ushort[] mask)
        {
            int acc = 0;
            for (var i = 0; i < mask.Length; i++)
            {
                acc += (mask[i] == 1) ? 1 : 0;
            }
            return acc;
        }

        public static int NumberOfActiveElementsInMask(uint[] mask)
        {
            int acc = 0;
            for (var i = 0; i < mask.Length; i++)
            {
                acc += (mask[i] == 1) ? 1 : 0;
            }
            return acc;
        }

        public static int NumberOfActiveElementsInMask(ulong[] mask)
        {
            int acc = 0;
            for (var i = 0; i < mask.Length; i++)
            {
                acc += (mask[i] == 1) ? 1 : 0;
            }
            return acc;
        }

        public static double[] Compact(double[] op1, double[] op2)
        {
            double[] result = new double[op1.Length];
            Array.Fill<double>(result, 0, 0, op1.Length);

            int i = 0;
            for (int j = 0; j < op1.Length; j++)
            {
                if (op1[j] != 0)
                {
                    result[i++] = op2[j];
                }
            }

            return result;
        }

        public static int[] Compact(int[] op1, int[] op2)
        {
            int[] result = new int[op1.Length];
            Array.Fill<int>(result, 0, 0, op1.Length);

            int i = 0;
            for (int j = 0; j < op1.Length; j++)
            {
                if (op1[j] != 0)
                {
                    result[i++] = op2[j];
                }
            }

            return result;
        }

        public static long[] Compact(long[] op1, long[] op2)
        {
            long[] result = new long[op1.Length];
            Array.Fill<long>(result, 0, 0, op1.Length);

            long i = 0;
            for (int j = 0; j < op1.Length; j++)
            {
                if (op1[j] != 0)
                {
                    result[i++] = op2[j];
                }
            }

            return result;
        }

        public static float[] Compact(float[] op1, float[] op2)
        {
            float[] result = new float[op1.Length];
            Array.Fill<float>(result, 0, 0, op1.Length);

            int i = 0;
            for (int j = 0; j < op1.Length; j++)
            {
                if (op1[j] != 0)
                {
                    result[i++] = op2[j];
                }
            }

            return result;
        }

        public static uint[] Compact(uint[] op1, uint[] op2)
        {
            uint[] result = new uint[op1.Length];
            Array.Fill<uint>(result, 0, 0, op1.Length);

            int i = 0;
            for (int j = 0; j < op1.Length; j++)
            {
                if (op1[j] != 0)
                {
                    result[i++] = op2[j];
                }
            }

            return result;
        }

        public static ulong[] Compact(ulong[] op1, ulong[] op2)
        {
            ulong[] result = new ulong[op1.Length];
            Array.Fill<ulong>(result, 0, 0, op1.Length);

            ulong i = 0;
            for (int j = 0; j < op1.Length; j++)
            {
                if (op1[j] != 0)
                {
                    result[i++] = op2[j];
                }
            }

            return result;
        }

        public static int LoadInt16FromByteArray(byte[] array, int offset)
        {
            int ret = 0;
            for (int i = 1; i >= 0; i--)
            {
                ret = (ret << 8) + (int)array[offset + i];
            }
            return ret;
        }

        public static int LoadInt16FromByteArray(byte[] array, uint offset)
        {
            int ret = 0;
            for (int i = 1; i >= 0; i--)
            {
                ret = (ret << 8) + (int)array[offset + i];
            }
            return ret;
        }
        public static int LoadInt32FromByteArray(byte[] array, int offset)
        {
            int ret = 0;
            for (int i = 3; i >= 0; i--)
            {
                ret = (ret << 8) + (int)array[offset + i];
            }
            return ret;
        }

        public static int LoadInt32FromByteArray(byte[] array, uint offset)
        {
            int ret = 0;
            for (int i = 3; i >= 0; i--)
            {
                ret = (ret << 8) + (int)array[offset + i];
            }
            return ret;
        }

        public static long LoadInt64FromByteArray(byte[] array, long offset)
        {
            long ret = 0;
            for (long i = 7; i >= 0; i--)
            {
                ret = (ret << 8) + (long)array[offset + i];
            }
            return ret;
        }

        public static long LoadInt64FromByteArray(byte[] array, ulong offset)
        {
            long ret = 0;
            for (long i = 7; i >= 0; i--)
            {
                ret = (ret << 8) + (long)array[offset + (ulong)i];
            }
            return ret;
        }

        public static uint LoadUInt16FromByteArray(byte[] array, int offset)
        {
            uint ret = 0;
            for (int i = 1; i >= 0; i--)
            {
                ret = (ret << 8) + (uint)array[offset + i];
            }
            return ret;
        }

        public static uint LoadUInt16FromByteArray(byte[] array, uint offset)
        {
            uint ret = 0;
            for (int i = 1; i >= 0; i--)
            {
                ret = (ret << 8) + (uint)array[offset + i];
            }
            return ret;
        }

        public static uint LoadUInt32FromByteArray(byte[] array, int offset)
        {
            uint ret = 0;
            for (int i = 3; i >= 0; i--)
            {
                ret = (ret << 8) + (uint)array[offset + i];
            }
            return ret;
        }

        public static uint LoadUInt32FromByteArray(byte[] array, uint offset)
        {
            uint ret = 0;
            for (int i = 3; i >= 0; i--)
            {
                ret = (ret << 8) + (uint)array[offset + i];
            }
            return ret;
        }

        public static ulong LoadUInt64FromByteArray(byte[] array, long offset)
        {
            ulong ret = 0;
            for (long i = 7; i >= 0; i--)
            {
                ret = (ret << 8) + (ulong)array[offset + i];
            }
            return ret;
        }

        public static ulong LoadUInt64FromByteArray(byte[] array, ulong offset)
        {
            ulong ret = 0;
            for (long i = 7; i >= 0; i--)
            {
                ret = (ret << 8) + (ulong)array[offset + (ulong)i];
            }
            return ret;
        }

        public static float LoadSingleFromByteArray(byte[] array, int offset)
        {
            int ret = 0;
            for (int i = 3; i >= 0; i--)
            {
                ret = (ret << 8) + (int)array[offset + i];
            }
            return BitConverter.Int32BitsToSingle(ret);
        }

        public static float LoadSingleFromByteArray(byte[] array, uint offset)
        {
            int ret = 0;
            for (int i = 3; i >= 0; i--)
            {
                ret = (ret << 8) + (int)array[offset + i];
            }
            return BitConverter.Int32BitsToSingle(ret);
        }

        public static double LoadDoubleFromByteArray(byte[] array, long offset)
        {
            long ret = 0;
            for (long i = 7; i >= 0; i--)
            {
                ret = (ret << 8) + (long)array[offset + i];
            }
            return BitConverter.Int64BitsToDouble(ret);
        }

        public static double LoadDoubleFromByteArray(byte[] array, ulong offset)
        {
            long ret = 0;
            for (long i = 7; i >= 0; i--)
            {
                ret = (ret << 8) + (long)array[offset + (ulong)i];
            }
            return BitConverter.Int64BitsToDouble(ret);
        }

        public static T Splice<T>(T[] first, T[] second, T[] maskArray, int index)
            where T : unmanaged, IEquatable<T>
        {
            int start = -1;
            int end = -1;

            for (int i = 0; i < maskArray.Length; i++)
            {
                if (!maskArray[i].Equals(default))
                {
                    if (start == -1)
                        start = i;
                    end = i;
                }
            }

            if (start == -1)
            {
                return second[index];
            }

            int rangeSize = end - start + 1;
            return (index < rangeSize)
                ? first[start + index]
                : second[index - rangeSize];
        }

        public static T LastActive<T>(T[] mask, T[] x) where T : IBinaryInteger<T>
        {
            for (var i = mask.Length - 1; i >= 0; i--)
            {
                if (mask[i] != T.Zero)
                {
                    return x[i];
                }
            }
            return T.Zero;
        }

        public static T[] CreateBreakAfterMask<T>(T[] mask, T[] op) where T : IBinaryInteger<T>
        {
            var count = mask.Length;
            var result = new T[count];
            var isBreakSet = false;
            for (var i = 0; i < count; i++)
            {
                var isElementActive = op[i] != T.Zero;
                if (mask[i] != T.Zero)
                {
                    if (isBreakSet)
                    {
                        result[i] = T.Zero;
                    }
                    else
                    {
                        result[i] = T.One;
                    }
                    isBreakSet = isBreakSet || isElementActive;
                }
                else
                {
                    result[i] = T.Zero;
                }
            }
            return result;
        }

        public static T[] CreateBreakAfterPropagateMask<T>(T[] mask, T[] op1, T[] op2) where T : IBinaryInteger<T>
        {
            var count = mask.Length;
            var result = new T[count];
            var isLastActive = LastActive(mask, op1) != T.Zero;
            for (var i = 0; i < count; i++)
            {
                if (mask[i] != T.Zero)
                {
                    if (isLastActive)
                    {
                        result[i] = T.One;
                    }
                    else
                    {
                        result[i] = T.Zero;
                    }
                    isLastActive = isLastActive && (op2[i] == T.Zero);
                }
                else
                {
                    result[i] = T.Zero;
                }
            }
            return result;
        }

        public static T[] CreateBreakBeforeMask<T>(T[] mask, T[] op) where T : IBinaryInteger<T>
        {
            var count = mask.Length;
            var result = new T[count];
            var isBreakSet = false;
            for (var i = 0; i < count; i++)
            {
                var isElementActive = op[i] != T.Zero;
                if (mask[i] != T.Zero)
                {
                    isBreakSet = isBreakSet || isElementActive;
                    if (isBreakSet)
                    {
                        result[i] = T.Zero;
                    }
                    else
                    {
                        result[i] = T.One;
                    }
                }
                else
                {
                    result[i] = T.Zero;
                }
            }
            return result;
        }

        public static T[] CreateBreakBeforePropagateMask<T>(T[] mask, T[] op1, T[] op2) where T : IBinaryInteger<T>
        {
            var count = mask.Length;
            var result = new T[count];
            var isLastActive = LastActive(mask, op1) != T.Zero;
            for (var i = 0; i < count; i++)
            {
                if (mask[i] != T.Zero)
                {
                    isLastActive = isLastActive && (op2[i] == T.Zero);
                    if (isLastActive)
                    {
                        result[i] = T.One;
                    }
                    else
                    {
                        result[i] = T.Zero;
                    }
                }
                else
                {
                    result[i] = T.Zero;
                }
            }
            return result;
        }

        private static T ConditionalSelectResult<T>(T maskResult, T result, T falseResult) where T : INumberBase<T>
        {
            return (maskResult != T.Zero) ? result : falseResult;
        }

        private static T ConditionalSelectTrueResult<T>(T maskResult, T result, T trueResult) where T : INumberBase<T>
        {
            return (maskResult != T.Zero) ? trueResult : result;
        }


        private static TElem GetLoadVectorExpectedResultByIndex<TMem, TElem>(int index, TElem[] mask, TMem[] data, TElem[] result)
            where TMem : INumberBase<TMem>
            where TElem : INumberBase<TElem>
        {
            return (mask[index] == TElem.Zero) ? TElem.Zero : TElem.CreateTruncating(data[index]);
        }

        private static TElem GetLoadVectorExpectedResultByIndex<TMem, TElem>(int index, TMem[] data, TElem[] result)
            where TMem : INumberBase<TMem>
            where TElem : INumberBase<TElem>
        {
            TElem[] mask = new TElem[result.Length];
            Array.Fill(mask, TElem.One);

            return GetLoadVectorExpectedResultByIndex(index, mask, data, result);
        }

        private static bool CheckLoadVectorBehaviorCore<TMem, TElem>(TElem[] mask, TMem[] data, TElem[] result, Func<int, TElem, TElem> map)
            where TMem : INumberBase<TMem>
            where TElem : INumberBase<TElem>
        {
            for (var i = 0; i < data.Length; i++)
            {
                TElem expectedResult = GetLoadVectorExpectedResultByIndex(i, mask, data, result);
                expectedResult = map(i, expectedResult);
                if (result[i] != expectedResult)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool CheckLoadVectorBehaviorCore<TMem, TElem>(TMem[] data, TElem[] result, Func<int, TElem, TElem> map)
            where TMem : INumberBase<TMem>
            where TElem : INumberBase<TElem>
        {
            for (var i = 0; i < data.Length; i++)
            {
                TElem expectedResult = GetLoadVectorExpectedResultByIndex(i, data, result);
                expectedResult = map(i, expectedResult);
                if (result[i] != expectedResult)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool CheckLoadVectorBehavior<TMem, TElem>(TElem[] mask, TMem[] data, TElem[] result)
            where TMem : INumberBase<TMem>, IConvertible
            where TElem : INumberBase<TElem>
        {
            return CheckLoadVectorBehaviorCore(mask, data, result, (_, loadResult) => loadResult);
        }

        public static bool CheckLoadVectorBehavior<TMem, TElem>(TMem[] data, TElem[] result)
            where TMem : INumberBase<TMem>, IConvertible
            where TElem : INumberBase<TElem>
        {
            return CheckLoadVectorBehaviorCore(data, result, (_, loadResult) => loadResult);
        }

        public static bool CheckLoadVectorBehavior<TMem, TElem>(TElem[] maskOp, TMem[] data, TElem[] result, TElem[] falseOp)
            where TMem : INumberBase<TMem>, IConvertible
            where TElem : INumberBase<TElem>
        {
            return CheckLoadVectorBehaviorCore(data, result, (i, loadResult) => ConditionalSelectResult(maskOp[i], loadResult, falseOp[i]));
        }

        private static T GetGatherVectorResultByIndex<T, ExtendedElementT, Index>(int index, T[] mask, ExtendedElementT[] data, Index[] indices)
                where T : INumberBase<T>
                where ExtendedElementT : INumberBase<ExtendedElementT>
                where Index : IBinaryInteger<Index>
        {
            return (mask[index] == T.Zero) ? T.Zero : T.CreateTruncating(data[int.CreateChecked(indices[index])]);
        }

        private static unsafe T GetGatherVectorBasesResultByIndex<T, AddressT, ExtendedElementT>(int index, T[] mask, AddressT[] data)
                where T : INumberBase<T>
                where AddressT : unmanaged, INumberBase<AddressT>
                where ExtendedElementT : unmanaged, INumberBase<ExtendedElementT>
        {
            return (mask[index] == T.Zero) ? T.Zero : T.CreateTruncating(*(ExtendedElementT*)Unsafe.BitCast<AddressT, nint>(data[index]));
        }

        private static bool GetGatherVectorResultByByteOffset<T, ExtendedElementT, Offset>(int index, T[] mask, byte[] data, Offset[] offsets, T result)
                where T : INumberBase<T>
                where ExtendedElementT : INumberBase<ExtendedElementT>
                where Offset : IBinaryInteger<Offset>
        {
            if (mask[index] == T.Zero)
            {
                return result == T.Zero;
            }

            int offset = int.CreateChecked(offsets[index]);

            if (typeof(ExtendedElementT) == typeof(Int16))
            {
                return ExtendedElementT.CreateTruncating(result) == ExtendedElementT.CreateTruncating(LoadInt16FromByteArray(data, offset));
            }
            else if (typeof(ExtendedElementT) == typeof(UInt16))
            {
                return ExtendedElementT.CreateTruncating(result) == ExtendedElementT.CreateTruncating(LoadUInt16FromByteArray(data, offset));
            }
            else if (typeof(ExtendedElementT) == typeof(int))
            {
                return ExtendedElementT.CreateTruncating(result) == ExtendedElementT.CreateTruncating(LoadInt32FromByteArray(data, offset));
            }
            else if (typeof(ExtendedElementT) == typeof(uint))
            {
                return ExtendedElementT.CreateTruncating(result) == ExtendedElementT.CreateTruncating(LoadUInt32FromByteArray(data, offset));
            }
            else if (typeof(ExtendedElementT) == typeof(long))
            {
                return ExtendedElementT.CreateTruncating(result) == ExtendedElementT.CreateTruncating(LoadInt64FromByteArray(data, offset));
            }
            else if (typeof(ExtendedElementT) == typeof(ulong))
            {
                return ExtendedElementT.CreateTruncating(result) == ExtendedElementT.CreateTruncating(LoadUInt64FromByteArray(data, offset));
            }
            else if (typeof(ExtendedElementT) == typeof(float))
            {
                return BitConverter.SingleToInt32Bits((float)(object)result) == LoadInt32FromByteArray(data, offset);
            }
            else if (typeof(ExtendedElementT) == typeof(double))
            {
                return BitConverter.DoubleToInt64Bits((double)(object)result) == LoadInt64FromByteArray(data, offset);
            }
            else
            {
                return false;
            }
        }

        private static bool CheckGatherVectorBehaviorCore<T, ExtendedElementT, Index>(T[] mask, ExtendedElementT[] data, Index[] indices, T[] result, Func<int, T, T> map)
                where T : INumberBase<T>
                where ExtendedElementT : INumberBase<ExtendedElementT>
                where Index : IBinaryInteger<Index>
        {
            for (var i = 0; i < mask.Length; i++)
            {
                T gatherResult = GetGatherVectorResultByIndex(i, mask, data, indices);
                gatherResult = map(i, gatherResult);
                if (result[i] != gatherResult)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool CheckGatherVectorBasesBehaviorCore<T, AddressT, ExtendedElementT>(T[] mask, AddressT[] data, T[] result, Func<int, T, T> map)
                where T : INumberBase<T>
                where AddressT : unmanaged, INumberBase<AddressT>
                where ExtendedElementT : unmanaged, INumberBase<ExtendedElementT>
        {
            for (var i = 0; i < mask.Length; i++)
            {
                T gatherResult = GetGatherVectorBasesResultByIndex<T, AddressT, ExtendedElementT>(i, mask, data);
                gatherResult = map(i, gatherResult);
                if (result[i] != gatherResult)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool CheckGatherVectorBehavior<T, ExtendedElementT, Index>(T[] mask, ExtendedElementT[] data, Index[] indices, T[] result)
                where T : INumberBase<T>
                where ExtendedElementT : INumberBase<ExtendedElementT>
                where Index : IBinaryInteger<Index>
        {
            return CheckGatherVectorBehaviorCore(mask, data, indices, result, (_, gatherResult) => gatherResult);
        }

        public static bool CheckGatherVectorConditionalSelectBehavior<T, ExtendedElementT, Index>(T[] cndSelMask, T[] mask, ExtendedElementT[] data, Index[] indices, T[] cndSelFalse, T[] result)
                where T : INumberBase<T>
                where ExtendedElementT : INumberBase<ExtendedElementT>
                where Index : IBinaryInteger<Index>
        {
            return CheckGatherVectorBehaviorCore(mask, data, indices, result, (i, gatherResult) => ConditionalSelectResult(cndSelMask[i], gatherResult, cndSelFalse[i]));
        }

        public static bool CheckGatherVectorConditionalSelectTrueBehavior<T, ExtendedElementT, Index>(T[] cndSelMask, T[] mask, ExtendedElementT[] data, Index[] indices, T[] cndSelTrue, T[] result)
                where T : INumberBase<T>
                where ExtendedElementT : INumberBase<ExtendedElementT>
                where Index : IBinaryInteger<Index>
        {
            return CheckGatherVectorBehaviorCore(mask, data, indices, result, (i, gatherResult) => ConditionalSelectTrueResult(cndSelMask[i], gatherResult, cndSelTrue[i]));
        }


        public static bool CheckGatherVectorBasesBehavior<T, AddressT, ExtendedElementT>(T[] mask, AddressT[] data, T[] result)
                where T : INumberBase<T>
                where AddressT : unmanaged, INumberBase<AddressT>
                where ExtendedElementT : unmanaged, INumberBase<ExtendedElementT>
        {
            return CheckGatherVectorBasesBehaviorCore<T, AddressT, ExtendedElementT>(mask, data, result, (_, gatherResult) => gatherResult);
        }

        public static bool CheckGatherVectorBasesConditionalSelectBehavior<T, AddressT, ExtendedElementT>(T[] cndSelMask, T[] mask, AddressT[] data, T[] cndSelFalse, T[] result)
                where T : INumberBase<T>
                where AddressT : unmanaged, INumberBase<AddressT>
                where ExtendedElementT : unmanaged, INumberBase<ExtendedElementT>
        {
            return CheckGatherVectorBasesBehaviorCore<T, AddressT, ExtendedElementT>(mask, data, result, (i, gatherResult) => ConditionalSelectResult(cndSelMask[i], gatherResult, cndSelFalse[i]));
        }

        public static bool CheckGatherVectorBasesConditionalSelectTrueBehavior<T, AddressT, ExtendedElementT>(T[] cndSelMask, T[] mask, AddressT[] data, T[] cndSelTrue, T[] result)
                where T : INumberBase<T>
                where AddressT : unmanaged, INumberBase<AddressT>
                where ExtendedElementT : unmanaged, INumberBase<ExtendedElementT>
        {
            return CheckGatherVectorBasesBehaviorCore<T, AddressT, ExtendedElementT>(mask, data, result, (i, gatherResult) => ConditionalSelectTrueResult(cndSelMask[i], gatherResult, cndSelTrue[i]));
        }

        private static bool CheckFirstFaultingBehaviorCore<T, TFault>(T[] result, Vector<TFault> faultResult, Func<int, bool> checkIter)
                where T : INumberBase<T>
                where TFault : INumberBase<TFault>
        {
            bool hitFault = false;

            for (var i = 0; i < result.Length; i++)
            {
                if (hitFault)
                {
                    if (faultResult[i] != TFault.Zero)
                    {
                        return false;
                    }
                }
                else
                {
                    if (faultResult[i] == TFault.Zero)
                    {
                        // There has to be a valid value for the first element, so check it.
                        if (i == 0)
                        {
                            return false;
                        }
                        hitFault = true;
                    }
                    else
                    {
                        if (!checkIter(i))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool CheckFaultResultHasAtLeastOneZero<T>(Vector<T> faultResult) where T : INumberBase<T>
        {
            for (var i = 0; i < Vector<T>.Count; i++)
            {
                if (faultResult[i] == T.Zero)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool CheckLoadVectorFirstFaultingBehavior<TMem, TElem, TFault>(TElem[] mask, TMem[] data, TElem[] result, Vector<TFault> faultResult)
                where TMem : INumberBase<TMem>, IConvertible
                where TElem : INumberBase<TElem>
                where TFault : INumberBase<TFault>
        {
            // Checking first faulting behavior requires at least one zero to ensure we are testing the behavior.
            if (!CheckFaultResultHasAtLeastOneZero(faultResult))
            {
                TestLibrary.TestFramework.LogInformation("Fault result requires at least one zero.");
                return false;
            }

            var validElementCount = data.Length;
            var hasFaulted = false;
            var expectedFaultResult =
                InitVector<TFault>(i =>
                {
                    if (hasFaulted)
                    {
                        return TFault.Zero;
                    }

                    if (mask[i] == TElem.Zero)
                    {
                        return TFault.One;
                    }

                    if (i < validElementCount)
                    {
                        return TFault.One;
                    }

                    hasFaulted = true;
                    return TFault.Zero;
                });
            if (expectedFaultResult != faultResult)
            {
                TestLibrary.TestFramework.LogInformation($"Expected fault result: {expectedFaultResult}\nActual fault result: {faultResult}");
                return false;
            }

            return CheckFirstFaultingBehaviorCore(result, faultResult, i => GetLoadVectorExpectedResultByIndex(i, mask, data, result) == result[i]);
        }

        public static bool CheckGatherVectorFirstFaultingBehavior<T, ExtendedElementT, Index, TFault>(T[] mask, ExtendedElementT[] data, Index[] indices, T[] result, Vector<TFault> faultResult)
                where T : INumberBase<T>
                where ExtendedElementT : INumberBase<ExtendedElementT>
                where Index : IBinaryInteger<Index>
                where TFault : INumberBase<TFault>
        {
            // Checking first faulting behavior requires at least one zero to ensure we are testing the behavior.
            if (!CheckFaultResultHasAtLeastOneZero(faultResult))
            {
                TestLibrary.TestFramework.LogInformation("Fault result requires at least one zero.");
                return false;
            }

            var hasFaulted = false;
            var expectedFaultResult =
                InitVector<TFault>(i =>
                {
                    if (hasFaulted)
                    {
                        return TFault.Zero;
                    }

                    if (mask[i] == T.Zero)
                    {
                        return TFault.One;
                    }

                    var index = int.CreateChecked(indices[i]);
                    if (index < 0 || index >= data.Length)
                    {
                        hasFaulted = true;
                        return TFault.Zero;
                    }
                    return TFault.One;
                });
            if (expectedFaultResult != faultResult)
            {
                TestLibrary.TestFramework.LogInformation($"Expected fault result: {expectedFaultResult}\nActual fault result: {faultResult}");
                return false;
            }

            return CheckFirstFaultingBehaviorCore(result, faultResult, i => GetGatherVectorResultByIndex(i, mask, data, indices) == result[i]);
        }

        public static bool CheckGatherVectorBasesFirstFaultingBehavior<T, AddressT, ExtendedElementT, TFault>(T[] mask, AddressT[] data, T[] result, Vector<TFault> faultResult)
                where T : INumberBase<T>
                where AddressT : unmanaged, INumberBase<AddressT>
                where ExtendedElementT : unmanaged, INumberBase<ExtendedElementT>
                where TFault : INumberBase<TFault>
        {
            // Checking first faulting behavior requires at least one zero to ensure we are testing the behavior.
            if (!CheckFaultResultHasAtLeastOneZero(faultResult))
            {
                TestLibrary.TestFramework.LogInformation("Fault result requires at least one zero.");
                return false;
            }

            var hasFaulted = false;
            var expectedFaultResult =
                InitVector<TFault>(i =>
                {
                    if (hasFaulted)
                    {
                        return TFault.Zero;
                    }

                    if (mask[i] == T.Zero)
                    {
                        return TFault.One;
                    }

                    if (data[i] == AddressT.Zero)
                    {
                        hasFaulted = true;
                        return TFault.Zero;
                    }
                    return TFault.One;
                });
            if (expectedFaultResult != faultResult)
            {
                TestLibrary.TestFramework.LogInformation($"Expected fault result: {expectedFaultResult}\nActual fault result: {faultResult}");
                return false;
            }

            return CheckFirstFaultingBehaviorCore(result, faultResult, i => GetGatherVectorBasesResultByIndex<T, AddressT, ExtendedElementT>(i, mask, data) == result[i]);
        }

        public static bool CheckGatherVectorWithByteOffsetFirstFaultingBehavior<T, ExtendedElementT, Offset, TFault>(T[] mask, byte[] data, Offset[] offsets, T[] result, Vector<TFault> faultResult)
                where T : INumberBase<T>
                where ExtendedElementT : INumberBase<ExtendedElementT>
                where Offset : IBinaryInteger<Offset>
                where TFault : INumberBase<TFault>
        {
            // Checking first faulting behavior requires at least one zero to ensure we are testing the behavior.
            if (!CheckFaultResultHasAtLeastOneZero(faultResult))
            {
                TestLibrary.TestFramework.LogInformation("Fault result requires at least one zero.");
                return false;
            }

            var elemSize = Unsafe.SizeOf<ExtendedElementT>();
            var hasFaulted = false;
            var expectedFaultResult =
                InitVector<TFault>(i =>
                {
                    if (hasFaulted)
                    {
                        return TFault.Zero;
                    }

                    if (mask[i] == T.Zero)
                    {
                        return TFault.One;
                    }

                    var offset = int.CreateChecked(offsets[i]);
                    if (offset < 0 || (offset + elemSize) > data.Length)
                    {
                        hasFaulted = true;
                        return TFault.Zero;
                    }
                    return TFault.One;
                });
            if (expectedFaultResult != faultResult)
            {
                TestLibrary.TestFramework.LogInformation($"Expected fault result: {expectedFaultResult}\nActual fault result: {faultResult}");
                return false;
            }

            return CheckFirstFaultingBehaviorCore(result, faultResult, i => GetGatherVectorResultByByteOffset<T, ExtendedElementT, Offset>(i, mask, data, offsets, result[i]));
        }

        public static T[] CreateBreakPropagateMask<T>(T[] op1, T[] op2) where T : IBinaryInteger<T>
        {
            var count = op1.Length;
            var result = new T[count];

            // embedded true mask
            var mask = new T[count];
            for (var i = 0; i < count; i++)
            {
                mask[i] = T.One;
            }

            if (LastActive(mask, op1) != T.Zero)
            {
                result = ConvertVectorToMask(op2);
            }

            return result;
        }

        public static T ConditionalExtract<T>(T[] op1, T op2, T[] op3, bool after) where T : INumber<T>
        {
            int last = LastActiveElement(op1);
            if (last < 0)
            {
                return op2;
            }

            int pos = last;
            if (after)
            {
                pos++;
                if (pos == op1.Length)
                {
                    pos = 0;
                }
            }

            return op3[pos];
        }

        public static T[] ConditionalExtract<T>(T[] op1, T[] op2, T[] op3, bool after, bool replicate)
            where T : INumber<T>
        {
            int last = LastActiveElement(op1);
            if (last < 0)
            {
                return op2;
            }

            T[] result = new T[op1.Length];
            int pos = last;
            if (after)
            {
                pos++;
                if (pos == op1.Length)
                {
                    pos = 0;
                }
            }

            if (replicate)
            {
                Array.Fill(result, op3[pos]);
            }
            else
            {
                Array.Fill(result, T.Zero);
                result[0] = op3[pos];
            }

            return result;
        }

        public static T ConditionalExtractAfterLastActiveElement<T>(T[] op1, T op2, T[] op3) where T : INumber<T>
            => ConditionalExtract(op1, op2, op3, after: true);

        public static T ConditionalExtractLastActiveElement<T>(T[] op1, T op2, T[] op3) where T : INumber<T>
            => ConditionalExtract(op1, op2, op3, after: false);

        public static T[] ConditionalExtractAfterLastActiveElement<T>(T[] op1, T[] op2, T[] op3) where T : INumber<T>
            => ConditionalExtract(op1, op2, op3, after: true, replicate: false);

        public static T[] ConditionalExtractAfterLastActiveElementAndReplicate<T>(T[] op1, T[] op2, T[] op3) where T : INumber<T>
            => ConditionalExtract(op1, op2, op3, after: true, replicate: true);

        public static T[] ConditionalExtractLastActiveElement<T>(T[] op1, T[] op2, T[] op3) where T : INumber<T>
            => ConditionalExtract(op1, op2, op3, after: false, replicate: false);

        public static T[] ConditionalExtractLastActiveElementAndReplicate<T>(T[] op1, T[] op2, T[] op3) where T : INumber<T>
            => ConditionalExtract(op1, op2, op3, after: false, replicate: true);

        public static T[] Extract<T>(T[] op1, T[] op2, bool after) where T : INumber<T>
        {
            int pos = LastActiveElement(op1);
            if (after)
            {
                pos++;
                if (pos == op1.Length)
                {
                    pos = 0;
                }
            }

            T[] result = new T[op1.Length];
            Array.Fill(result, T.Zero);
            result[0] = op2[pos];
            return result;
        }

        public static T ExtractScalar<T>(T[] op1, T[] op2, bool after) where T : INumber<T>
        {
            int pos = LastActiveElement(op1);
            if (after)
            {
                pos++;
                if (pos == op1.Length)
                {
                    pos = 0;
                }
            }

            return op2[pos];
        }

        public static T[] ExtractAfterLastActiveElement<T>(T[] op1, T[] op2) where T : INumber<T>
            => Extract(op1, op2, after: true);

        public static T ExtractAfterLastActiveElementScalar<T>(T[] op1, T[] op2) where T : INumber<T>
            => ExtractScalar(op1, op2, after: true);

        public static T[] ExtractLastActiveElement<T>(T[] op1, T[] op2) where T : INumber<T>
            => Extract(op1, op2, after: false);

        public static T ExtractLastActiveElementScalar<T>(T[] op1, T[] op2) where T : INumber<T>
            => ExtractScalar(op1, op2, after: false);

        public static T BitwiseClearXor<T>(T op1, T op2, T op3) where T : IBitwiseOperators<T, T, T>
        {
            return op1 ^ (op2 & ~op3);
        }

        public static T BitwiseSelectLeftInverted<T>(T select, T left, T right) where T : IBitwiseOperators<T, T, T>
        {
            return (~left & select) | (right & ~select);
        }

        public static T BitwiseSelectRightInverted<T>(T select, T left, T right) where T : IBitwiseOperators<T, T, T>
        {
            return (left & select) | (~right & ~select);
        }

        public static T[] InterleavingXorEvenOdd<T>(T[] odd, T[] left, T[] right) where T : IBinaryInteger<T>
        {
            for (int i = 0; i < odd.Length; i += 2)
            {
                odd[i] = left[i] ^ right[i + 1];
            }
            return odd;
        }

        public static T[] InterleavingXorOddEven<T>(T[] even, T[] left, T[] right) where T : IBinaryInteger<T>
        {
            for (int i = 0; i < even.Length; i += 2)
            {
                even[i + 1] = left[i + 1] ^ right[i];
            }
            return even;
        }

        public static T[] SubtractBorrowWideningEven<T>(T[] op1, T[] op2, T[] op3)
            where T : unmanaged, IBinaryInteger<T>
        {
            T[] result = new T[op1.Length];
            for (int i = 0; i < op1.Length; i += 2)
            {
                T a = op1[i];
                T b = ~op2[i];
                T carryIn = op3[i + 1] & T.One;
                (T sum, T carryOut) = AddWithCarry(a, b, carryIn);
                result[i] = sum;
                result[i + 1] = carryOut;
            }

            return result;
        }

        public static T[] SubtractBorrowWideningOdd<T>(T[] op1, T[] op2, T[] op3)
            where T : unmanaged, IBinaryInteger<T>
        {
            T[] result = new T[op1.Length];
            for (int i = 0; i < op1.Length; i += 2)
            {
                T a = op1[i];
                T b = ~op2[i+1];
                T carryIn = op3[i + 1] & T.One;
                (T sum, T carryOut) = AddWithCarry(a, b, carryIn);
                result[i] = sum;
                result[i + 1] = carryOut;
            }

            return result;
        }

        public static sbyte SubtractHighNarrowingEven(short[] left, short[] right, int i)
        {
            if (i % 2 == 0)
            {
                return (sbyte)((left[i / 2] - right[i / 2]) >> 8);
            }

            return 0;
        }

        public static short SubtractHighNarrowingEven(int[] left, int[] right, int i)
        {
            if (i % 2 == 0)
            {
                return (short) ((left[i / 2] - right[i / 2]) >> 16);
            }

            return 0;
        }

        public static int SubtractHighNarrowingEven(long[] left, long[] right, int i)
        {
            if (i % 2 == 0)
            {
                return (int) ((left[i / 2] - right[i / 2]) >> 32);
            }

            return 0;
        }

        public static byte SubtractHighNarrowingEven(ushort[] left, ushort[] right, int i)
        {
            if (i % 2 == 0)
            {
                return (byte)((left[i / 2] - right[i / 2]) >> 8);
            }

            return 0;
        }

        public static ushort SubtractHighNarrowingEven(uint[] left, uint[] right, int i)
        {
            if (i % 2 == 0)
            {
                return (ushort)((left[i / 2] - right[i / 2]) >> 16);
            }

            return 0;
        }

        public static uint SubtractHighNarrowingEven(ulong[] left, ulong[] right, int i)
        {
            if (i % 2 == 0)
            {
                return (uint)((left[i / 2] - right[i / 2]) >> 32);
            }

            return 0;
        }

        public static sbyte SubtractHighNarrowingOdd(sbyte[] even, short[] left, short[] right, int i)
        {
            if (i % 2 == 1)
            {
                return (sbyte) ((left[i / 2] - right[i / 2]) >> 8);
            }

            return even[i];
        }

        public static short SubtractHighNarrowingOdd(short[] even, int[] left, int[] right, int i)
        {
            if (i % 2 == 1)
            {
                return (short) ((left[i / 2] - right[i / 2]) >> 16);
            }

            return even[i];
        }

        public static int SubtractHighNarrowingOdd(int[] even, long[] left, long[] right, int i)
        {
            if (i % 2 == 1)
            {
                return (int) ((left[i / 2] - right[i / 2]) >> 32);
            }

            return even[i];
        }

        public static byte SubtractHighNarrowingOdd(byte[] even, ushort[] left, ushort[] right, int i)
        {
            if (i % 2 == 1)
            {
                return (byte)((left[i / 2] - right[i / 2]) >> 8);
            }

            return even[i];
        }

        public static ushort SubtractHighNarrowingOdd(ushort[] even, uint[] left, uint[] right, int i)
        {
            if (i % 2 == 1)
            {
                return (ushort)((left[i / 2] - right[i / 2]) >> 16);
            }

            return even[i];
        }

        public static uint SubtractHighNarrowingOdd(uint[] even, ulong[] left, ulong[] right, int i)
        {
            if (i % 2 == 1)
            {
                return (uint)((left[i / 2] - right[i / 2]) >> 32);
            }

            return even[i];
        }

        public static (T sum, T carryOut) AddWithCarry<T>(T a, T b, T carryIn)
        where T : unmanaged, IBinaryInteger<T>
        {
            T sum = a + b + carryIn;
            T one = T.One;
            T zero = T.Zero;
            T carryOut = (sum < a || (sum == a && carryIn == one)) ? one : zero;
            return (sum, carryOut);
        }

        public static T Xor<T>(params T[] ops) where T : IBitwiseOperators<T, T, T>
        {
            T result = ops[0];
            for (int i = 1; i < ops.Length; i++)
            {
                result ^= ops[i];
            }
            return result;
        }

        public static T XorRotateRight<T>(T op1, T op2, int shift) where T : IBinaryInteger<T>
        {
            return T.RotateRight(Xor(op1, op2), shift);
        }

        public static sbyte SveShiftArithmeticRounded(sbyte op1, sbyte op2) => SignedShift(op1, op2, rounding: true, shiftSat: true);

        public static sbyte SveShiftArithmeticSaturate(sbyte op1, sbyte op2) => SignedShift(op1, op2, saturating: true, shiftSat: true);

        public static sbyte SveShiftArithmeticRoundedSaturate(sbyte op1, sbyte op2) => SignedShift(op1, op2, rounding: true, saturating: true, shiftSat: true);

        public static short SveShiftArithmeticRounded(short op1, short op2) => SignedShift(op1, op2, rounding: true, shiftSat: true);

        public static short SveShiftArithmeticSaturate(short op1, short op2) => SignedShift(op1, op2, saturating: true, shiftSat: true);

        public static short SveShiftArithmeticRoundedSaturate(short op1, short op2) => SignedShift(op1, op2, rounding: true, saturating: true, shiftSat: true);

        public static int SveShiftArithmeticRounded(int op1, int op2) => SignedShift(op1, op2, rounding: true, shiftSat: true);

        public static int SveShiftArithmeticSaturate(int op1, int op2) => SignedShift(op1, op2, saturating: true, shiftSat: true);

        public static int SveShiftArithmeticRoundedSaturate(int op1, int op2) => SignedShift(op1, op2, rounding: true, saturating: true, shiftSat: true);

        public static long SveShiftArithmeticRounded(long op1, long op2) => SignedShift(op1, op2, rounding: true, shiftSat: true);

        public static long SveShiftArithmeticSaturate(long op1, long op2) => SignedShift(op1, op2, saturating: true, shiftSat: true);

        public static long SveShiftArithmeticRoundedSaturate(long op1, long op2) => SignedShift(op1, op2, rounding: true, saturating: true, shiftSat: true);

        public static byte SveShiftLeftLogicalSaturate(byte op1, sbyte op2) => UnsignedShift(op1, op2, saturating: true, shiftSat: true);

        public static ushort SveShiftLeftLogicalSaturate(ushort op1, short op2) => UnsignedShift(op1, op2, saturating: true, shiftSat: true);

        public static uint SveShiftLeftLogicalSaturate(uint op1, int op2) => UnsignedShift(op1, op2, saturating: true, shiftSat: true);

        public static ulong SveShiftLeftLogicalSaturate(ulong op1, long op2) => UnsignedShift(op1, op2, saturating: true, shiftSat: true);

        public static byte SveShiftLeftLogicalSaturateUnsigned(sbyte op1, byte op2) => UnsignedShift((byte)op1, (sbyte)op2, saturating: true, shiftSat: true);

        public static ushort SveShiftLeftLogicalSaturateUnsigned(short op1, byte op2) => UnsignedShift((ushort)op1, (sbyte)op2, saturating: true, shiftSat: true);

        public static uint SveShiftLeftLogicalSaturateUnsigned(int op1, byte op2) => UnsignedShift((uint)op1, (sbyte)op2, saturating: true, shiftSat: true);

        public static ulong SveShiftLeftLogicalSaturateUnsigned(long op1, byte op2) => UnsignedShift((ulong)op1, (sbyte)op2, saturating: true, shiftSat: true);

        public static byte SveShiftLogicalRounded(byte op1, sbyte op2) => UnsignedShift(op1, op2, rounding: true, shiftSat: true);

        public static ushort SveShiftLogicalRounded(ushort op1, short op2) => UnsignedShift(op1, op2, rounding: true, shiftSat: true);

        public static uint SveShiftLogicalRounded(uint op1, int op2) => UnsignedShift(op1, op2, rounding: true, shiftSat: true);

        public static ulong SveShiftLogicalRounded(ulong op1, long op2) => UnsignedShift(op1, op2, rounding: true, shiftSat: true);

        public static byte SveShiftLogicalRoundedSaturate(byte op1, sbyte op2) => UnsignedShift(op1, op2, rounding: true, saturating: true, shiftSat: true);

        public static ushort SveShiftLogicalRoundedSaturate(ushort op1, short op2) => UnsignedShift(op1, op2, rounding: true, saturating: true, shiftSat: true);

        public static uint SveShiftLogicalRoundedSaturate(uint op1, int op2) => UnsignedShift(op1, op2, rounding: true, saturating: true, shiftSat: true);

        public static ulong SveShiftLogicalRoundedSaturate(ulong op1, long op2) => UnsignedShift(op1, op2, rounding: true, saturating: true, shiftSat: true);

        public static int NarrowIdx(int i)
        {
            return (i - i % 2) / 2;
        }

        public static T Even<T>(T val, int idx) where T : IBinaryInteger<T>, new()
        {
            if (idx % 2 == 0)
            {
                return val;
            }
            else
            {
                return new T();
            }
        }

        public static T Odd<T>(T even, T odd, int idx) where T : IBinaryInteger<T>
        {
            if (idx % 2 != 0)
            {
                return odd;
            }
            else
            {
                return even;
            }
        }

        public static U ArithmeticShift<T, U>(T value, int count, bool rounding = false, bool saturate = false)
            where T : IBinaryInteger<T>
            where U : IBinaryInteger<U>, IMinMaxValue<U>
        {
            long v = long.CreateChecked(value);
            long shifted;
            if (count > 0)
            {
                if (rounding)
                {
                    long bias = 1L << (count - 1);
                    shifted = v >= 0 ? (v + bias) >> count
                                     : (v - bias) >> count;
                }
                else
                {
                    shifted = v >> count;
                }
            }
            else if (count < 0)
            {
                shifted = v << -count;
            }
            else
            {
                shifted = v;
            }

            if (saturate)
            {
                long min = long.CreateChecked(U.MinValue);
                long max = long.CreateChecked(U.MaxValue);
                if (shifted < min) shifted = min;
                if (shifted > max) shifted = max;
            }

            return U.CreateTruncating(shifted);
        }

        public static U LogicalShift<T, U>(T value, int count, bool rounding = false, bool saturate = false)
            where T : IBinaryInteger<T>
            where U : IBinaryInteger<U>, IMinMaxValue<U>
        {
            ulong v = ulong.CreateTruncating(value);
            ulong shifted;
            if (count > 0)
            {
                if (rounding)
                {
                    ulong bias = 1UL << (count - 1);
                    shifted = v >= 0 ? (v + bias) >>> count
                                     : (v - bias) >>> count;
                }
                else
                {
                    shifted = v >>> count;
                }
            }
            else if (count < 0)
            {
                shifted = v << -count;
            }
            else
            {
                shifted = v;
            }

            if (saturate)
            {
                ulong max = ulong.CreateTruncating(U.MaxValue);
                if (shifted > max) shifted = max;
            }

            return U.CreateTruncating(shifted);
        }

        public static U ShiftRightArithmeticNarrowingSaturateEven<T, U>(T op1, byte op2, int i)
            where T : IBinaryInteger<T>
            where U : IBinaryInteger<U>, IMinMaxValue<U>, new()
        {
            return Even<U>(ArithmeticShift<T, U>(op1, op2, saturate: true), i);
        }

        public static U ShiftRightArithmeticNarrowingSaturateOdd<T, U>(U op0, T op1, byte op2, int i)
            where T : IBinaryInteger<T>
            where U : IBinaryInteger<U>, IMinMaxValue<U>
        {
            return Odd<U>(op0, ArithmeticShift<T, U>(op1, op2, saturate: true), i);
        }

        public static U ShiftRightArithmeticNarrowingSaturateUnsignedEven<T, U>(T op1, byte op2, int i)
            where T : IBinaryInteger<T>
            where U : IBinaryInteger<U>, IMinMaxValue<U>, new()
        {
            return ShiftRightArithmeticNarrowingSaturateEven<T, U>(op1, op2, i);
        }

        public static U ShiftRightArithmeticNarrowingSaturateUnsignedOdd<T, U>(U op0, T op1, byte op2, int i)
            where T : IBinaryInteger<T>
            where U : IBinaryInteger<U>, IMinMaxValue<U>
        {
            return ShiftRightArithmeticNarrowingSaturateOdd<T, U>(op0, op1, op2, i);
        }

        public static U ShiftRightArithmeticRoundedNarrowingSaturateEven<T, U>(T val, byte shift, int i)
            where T : IBinaryInteger<T>
            where U : IBinaryInteger<U>, IMinMaxValue<U>, new()
        {
            return Even<U>(ArithmeticShift<T, U>(val, shift, rounding: true, saturate: true), i);
        }

        public static U ShiftRightArithmeticRoundedNarrowingSaturateOdd<T, U>(U even, T val, byte shift, int i)
            where T : IBinaryInteger<T>
            where U : IBinaryInteger<U>, IMinMaxValue<U>
        {
            return Odd<U>(even, ArithmeticShift<T, U>(val, shift, rounding: true, saturate: true), i);
        }

        public static U ShiftRightArithmeticRoundedNarrowingSaturateUnsignedEven<T, U>(T val, byte shift, int i)
            where T : IBinaryInteger<T>
            where U : IBinaryInteger<U>, IMinMaxValue<U>, new()
        {
            return ShiftRightArithmeticRoundedNarrowingSaturateEven<T, U>(val, shift, i);
        }

        public static U ShiftRightArithmeticRoundedNarrowingSaturateUnsignedOdd<T, U>(U even, T val, byte shift, int i)
            where T : IBinaryInteger<T>
            where U : IBinaryInteger<U>, IMinMaxValue<U>, new()
        {
            return ShiftRightArithmeticRoundedNarrowingSaturateOdd<T, U>(even, val, shift, i);
        }

        public static U ShiftRightLogicalNarrowingEven<T, U>(T val, byte shift, int i)
            where T : IBinaryInteger<T>
            where U : IBinaryInteger<U>, IMinMaxValue<U>, new()
        {
            return Even<U>(LogicalShift<T, U>(val, shift), i);
        }

        public static U ShiftRightLogicalNarrowingOdd<T, U>(U even, T val, byte shift, int i)
            where T : IBinaryInteger<T>
            where U : IBinaryInteger<U>, IMinMaxValue<U>
        {
            return Odd<U>(even, LogicalShift<T, U>(val, shift), i);
        }

        public static U ShiftRightLogicalRoundedNarrowingEven<T, U>(T val, byte shift, int i)
            where T : IBinaryInteger<T>
            where U : IBinaryInteger<U>, IMinMaxValue<U>, new()
        {
            return Even<U>(LogicalShift<T, U>(val, shift, rounding: true), i);
        }

        public static U ShiftRightLogicalRoundedNarrowingOdd<T, U>(U even, T val, byte shift, int i)
            where T : IBinaryInteger<T>
            where U : IBinaryInteger<U>, IMinMaxValue<U>
        {
            return Odd<U>(even, LogicalShift<T, U>(val, shift, rounding: true), i);
        }

        public static U ShiftRightLogicalRoundedNarrowingSaturateEven<T, U>(T val, byte shift, int i)
            where T : IBinaryInteger<T>
            where U : IBinaryInteger<U>, IMinMaxValue<U>, new()
        {
            return Even<U>(LogicalShift<T, U>(val, shift, rounding: true, saturate: true), i);
        }

        public static U ShiftRightLogicalRoundedNarrowingSaturateOdd<T, U>(U even, T val, byte shift, int i)
            where T : IBinaryInteger<T>
            where U : IBinaryInteger<U>, IMinMaxValue<U>
        {
            return Odd<U>(even, LogicalShift<T, U>(val, shift, rounding: true, saturate: true), i);
        }

        public static W MultiplyAddWidening<W, N>(W op1, N op2, N op3)
            where W : IBinaryInteger<W>
            where N : IBinaryInteger<N>
        {
            W a = W.CreateChecked(op2);
            W b = W.CreateChecked(op3);
            W product = W.CreateTruncating(a * b);
            return W.CreateTruncating(op1 + product);
        }

        public static W MultiplySubtractWidening<W, N>(W op1, N op2, N op3)
            where W : IBinaryInteger<W>
            where N : IBinaryInteger<N>
        {
            W a = W.CreateChecked(op2);
            W b = W.CreateChecked(op3);
            W product = W.CreateTruncating(a * b);
            return W.CreateTruncating(op1 - product);
        }

        public static N AddRoundedHighNarrowing<W, N>(W op1, W op2)
            where W : IBinaryInteger<W>
            where N : IBinaryInteger<N>
        {
            int halfsize = default(N).GetByteCount() * 8;
            ulong a = ulong.CreateChecked(op1);
            ulong b = ulong.CreateChecked(op2);
            ulong sum = a + b;
            ulong bias = 1UL << (halfsize - 1);
            ulong result = (sum + bias) >> halfsize;
            return N.CreateTruncating(result);
        }

        public static N AddRoundedHighNarrowingEven<W, N>(W op1, W op2, int i)
            where W : IBinaryInteger<W>
            where N : IBinaryInteger<N>, new()
        {
            return Even<N>(AddRoundedHighNarrowing<W, N>(op1, op2), i);
        }

        public static N AddRoundedHighNarrowingOdd<W, N>(N even, W op1, W op2, int i)
            where W : IBinaryInteger<W>
            where N : IBinaryInteger<N>
        {
            return Odd<N>(even, AddRoundedHighNarrowing<W, N>(op1, op2), i);
        }

        public static N SubtractRoundedHighNarrowing<W, N>(W op1, W op2)
            where W : IBinaryInteger<W>
            where N : IBinaryInteger<N>
        {
            int halfsize = default(N).GetByteCount() * 8;
            ulong a = ulong.CreateChecked(op1);
            ulong b = ulong.CreateChecked(op2);
            ulong sum = (ulong)a - (ulong)b;
            ulong bias = 1UL << (halfsize - 1);
            ulong result = (sum + bias) >> halfsize;
            return N.CreateTruncating(result);
        }

        public static N SubtractRoundedHighNarrowingEven<W, N>(W op1, W op2, int i)
            where W : IBinaryInteger<W>
            where N : IBinaryInteger<N>, new()
        {
            return Even<N>(SubtractRoundedHighNarrowing<W, N>(op1, op2), i);
        }

        public static N SubtractRoundedHighNarrowingOdd<W, N>(N even, W op1, W op2, int i)
            where W : IBinaryInteger<W>
            where N : IBinaryInteger<N>
        {
            return Odd<N>(even, SubtractRoundedHighNarrowing<W, N>(op1, op2), i);
        }

        public static long FusedAddRoundedHalving(long op1, long op2)
        {
            bool overflow = false;
            long sum = 0;
            try
            {
                sum = checked(op1 + op2 + 1);
            }
            catch (OverflowException)
            {
                overflow = true;
                sum = op1 + op2 + 1;
            }

            // See FusedAddHalving for description of cases.
            if (op1 > 0 && op2 > 0 && overflow)
            {
                return (long)((ulong)sum >>> 1);
            }
            else if (op1 < 0 && op2 < 0 && overflow)
            {
                return (long)((ulong)(sum >> 1) | (1UL << 63));
            }
            else
            {
                return sum >> 1;
            }
        }

        public static ulong FusedAddRoundedHalving(ulong op1, ulong op2)
        {
            bool overflow = false;
            ulong sum = 0;
            try
            {
                sum = checked(op1 + op2 + 1);
            }
            catch (OverflowException)
            {
                overflow = true;
                sum = op1 + op2 + 1;
            }

            sum >>>= 1;

            if (overflow)
            {
                sum |= (ulong)(1UL << 63);
            }

            return sum;
        }
    }
}
