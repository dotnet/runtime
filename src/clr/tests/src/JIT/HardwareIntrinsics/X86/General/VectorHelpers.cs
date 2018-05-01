// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;

internal partial class IntelHardwareIntrinsicTest
{
    public static Vector128<T> Vector128Add<T>(Vector128<T> left, Vector128<T> right) where T : struct
    {
        if (typeof(T) == typeof(byte))
        {
            return Sse.StaticCast<byte, T>(Sse2.Add(Sse.StaticCast<T, byte>(left), Sse.StaticCast<T, byte>(right)));
        }
        else if (typeof(T) == typeof(sbyte))
        {
            return Sse.StaticCast<sbyte, T>(Sse2.Add(Sse.StaticCast<T, sbyte>(left), Sse.StaticCast<T, sbyte>(right)));
        }
        else if (typeof(T) == typeof(short))
        {
            return Sse.StaticCast<short, T>(Sse2.Add(Sse.StaticCast<T, short>(left), Sse.StaticCast<T, short>(right)));
        }
        else if (typeof(T) == typeof(ushort))
        {
            return Sse.StaticCast<ushort, T>(Sse2.Add(Sse.StaticCast<T, ushort>(left), Sse.StaticCast<T, ushort>(right)));
        }
        else if (typeof(T) == typeof(int))
        {
            return Sse.StaticCast<int, T>(Sse2.Add(Sse.StaticCast<T, int>(left), Sse.StaticCast<T, int>(right)));
        }
        else if (typeof(T) == typeof(uint))
        {
            return Sse.StaticCast<uint, T>(Sse2.Add(Sse.StaticCast<T, uint>(left), Sse.StaticCast<T, uint>(right)));
        }
        else if (typeof(T) == typeof(long))
        {
            return Sse.StaticCast<long, T>(Sse2.Add(Sse.StaticCast<T, long>(left), Sse.StaticCast<T, long>(right)));
        }
        else if (typeof(T) == typeof(ulong))
        {
            return Sse.StaticCast<ulong, T>(Sse2.Add(Sse.StaticCast<T, ulong>(left), Sse.StaticCast<T, ulong>(right)));
        }
        else if (typeof(T) == typeof(float))
        {
            return Sse.StaticCast<float, T>(Sse.Add(Sse.StaticCast<T, float>(left), Sse.StaticCast<T, float>(right)));
        }
        else if (typeof(T) == typeof(double))
        {
            return Sse.StaticCast<double, T>(Sse2.Add(Sse.StaticCast<T, double>(left), Sse.StaticCast<T, double>(right)));
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public static Vector256<T> Vector256Add<T>(Vector256<T> left, Vector256<T> right) where T : struct
    {
        if (typeof(T) == typeof(byte))
        {
            return Avx.StaticCast<byte, T>(Avx2.Add(Avx.StaticCast<T, byte>(left), Avx.StaticCast<T, byte>(right)));
        }
        else if (typeof(T) == typeof(sbyte))
        {
            return Avx.StaticCast<sbyte, T>(Avx2.Add(Avx.StaticCast<T, sbyte>(left), Avx.StaticCast<T, sbyte>(right)));
        }
        else if (typeof(T) == typeof(short))
        {
            return Avx.StaticCast<short, T>(Avx2.Add(Avx.StaticCast<T, short>(left), Avx.StaticCast<T, short>(right)));
        }
        else if (typeof(T) == typeof(ushort))
        {
            return Avx.StaticCast<ushort, T>(Avx2.Add(Avx.StaticCast<T, ushort>(left), Avx.StaticCast<T, ushort>(right)));
        }
        else if (typeof(T) == typeof(int))
        {
            return Avx.StaticCast<int, T>(Avx2.Add(Avx.StaticCast<T, int>(left), Avx.StaticCast<T, int>(right)));
        }
        else if (typeof(T) == typeof(uint))
        {
            return Avx.StaticCast<uint, T>(Avx2.Add(Avx.StaticCast<T, uint>(left), Avx.StaticCast<T, uint>(right)));
        }
        else if (typeof(T) == typeof(long))
        {
            return Avx.StaticCast<long, T>(Avx2.Add(Avx.StaticCast<T, long>(left), Avx.StaticCast<T, long>(right)));
        }
        else if (typeof(T) == typeof(ulong))
        {
            return Avx.StaticCast<ulong, T>(Avx2.Add(Avx.StaticCast<T, ulong>(left), Avx.StaticCast<T, ulong>(right)));
        }
        else if (typeof(T) == typeof(float))
        {
            return Avx.StaticCast<float, T>(Avx.Add(Avx.StaticCast<T, float>(left), Avx.StaticCast<T, float>(right)));
        }
        else if (typeof(T) == typeof(double))
        {
            return Avx.StaticCast<double, T>(Avx.Add(Avx.StaticCast<T, double>(left), Avx.StaticCast<T, double>(right)));
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public static Vector128<T> SetAllVector128<T>(T value) where T : struct
    {
        if (typeof(T) == typeof(byte))
        {
            return Sse.StaticCast<byte, T>(Sse2.SetAllVector128(Convert.ToByte(value)));
        }
        else if (typeof(T) == typeof(sbyte))
        {
            return Sse.StaticCast<sbyte, T>(Sse2.SetAllVector128(Convert.ToSByte(value)));
        }
        else if (typeof(T) == typeof(short))
        {
            return Sse.StaticCast<short, T>(Sse2.SetAllVector128(Convert.ToInt16(value)));
        }
        else if (typeof(T) == typeof(ushort))
        {
            return Sse.StaticCast<ushort, T>(Sse2.SetAllVector128(Convert.ToUInt16(value)));
        }
        else if (typeof(T) == typeof(int))
        {
            return Sse.StaticCast<int, T>(Sse2.SetAllVector128(Convert.ToInt32(value)));
        }
        else if (typeof(T) == typeof(uint))
        {
            return Sse.StaticCast<uint, T>(Sse2.SetAllVector128(Convert.ToUInt32(value)));
        }
        else if (typeof(T) == typeof(long))
        {
            return Sse.StaticCast<long, T>(Sse2.SetAllVector128(Convert.ToInt64(value)));
        }
        else if (typeof(T) == typeof(ulong))
        {
            return Sse.StaticCast<ulong, T>(Sse2.SetAllVector128(Convert.ToUInt64(value)));
        }
        else if (typeof(T) == typeof(float))
        {
            return Sse.StaticCast<float, T>(Sse.SetAllVector128(Convert.ToSingle(value)));
        }
        else if (typeof(T) == typeof(double))
        {
            return Sse.StaticCast<double, T>(Sse2.SetAllVector128(Convert.ToDouble(value)));
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    // These two helper functions are a workaround for the bug of Sse2.SetZeroVector128<float>
    // https://github.com/dotnet/coreclr/pull/17691
    public static Vector128<T> SetZeroVector128<T>() where T : struct
    {
        if (typeof(T) == typeof(float))
        {
            return Sse.StaticCast<float, T>(Sse.SetZeroVector128());
        }
        else
        {
            return Sse2SetZeroVector128<T>();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<T> Sse2SetZeroVector128<T>() where T : struct
    {
        if (typeof(T) == typeof(double) || typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) ||
            typeof(T) == typeof(ushort) || typeof(T) == typeof(int) || typeof(T) == typeof(uint) || typeof(T) == typeof(long) || typeof(T) == typeof(ulong))
        {
            return Sse2.SetZeroVector128<T>();
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public static bool CheckValue<T>(T value, T expectedValue) where T : struct
    {
        bool returnVal;
        if (typeof(T) == typeof(float))
        {
            returnVal = Math.Abs(((float)(object)value) - ((float)(object)expectedValue)) <= Single.Epsilon;
        }
        if (typeof(T) == typeof(double))
        {
            returnVal = Math.Abs(((double)(object)value) - ((double)(object)expectedValue)) <= Double.Epsilon;
        }
        else
        {
            returnVal = value.Equals(expectedValue);
        }
        if (returnVal == false)
        {
            if ((typeof(T) == typeof(double)) || (typeof(T) == typeof(float)))
            {
                Console.WriteLine("CheckValue failed for type " + typeof(T).ToString() + ". Expected: {0} , Got: {1}", expectedValue, value);
            }
            else
            {
                Console.WriteLine("CheckValue failed for type " + typeof(T).ToString() + ". Expected: {0} (0x{0:X}), Got: {1} (0x{1:X})", expectedValue, value);
            }
        }
        return returnVal;
    }

    public static T GetValueFromInt<T>(int value) where T : struct
    {
        if (typeof(T) == typeof(float))
        {
            float floatValue = (float)value;
            return (T)(object)floatValue;
        }
        if (typeof(T) == typeof(double))
        {
            double doubleValue = (double)value;
            return (T)(object)doubleValue;
        }
        if (typeof(T) == typeof(int))
        {
            return (T)(object)value;
        }
        if (typeof(T) == typeof(uint))
        {
            uint uintValue = (uint)value;
            return (T)(object)uintValue;
        }
        if (typeof(T) == typeof(long))
        {
            long longValue = (long)value;
            return (T)(object)longValue;
        }
        if (typeof(T) == typeof(ulong))
        {
            ulong longValue = (ulong)value;
            return (T)(object)longValue;
        }
        if (typeof(T) == typeof(ushort))
        {
            return (T)(object)(ushort)value;
        }
        if (typeof(T) == typeof(byte))
        {
            return (T)(object)(byte)value;
        }
        if (typeof(T) == typeof(short))
        {
            return (T)(object)(short)value;
        }
        if (typeof(T) == typeof(sbyte))
        {
            return (T)(object)(sbyte)value;
        }
        else
        {
            throw new ArgumentException();
        }
    }
}