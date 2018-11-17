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
            return Sse2.Add(left.AsByte(), right.AsByte()).As<T>();
        }
        else if (typeof(T) == typeof(sbyte))
        {
            return Sse2.Add(left.AsSByte(), right.AsSByte()).As<T>();
        }
        else if (typeof(T) == typeof(short))
        {
            return Sse2.Add(left.AsInt16(), right.AsInt16()).As<T>();
        }
        else if (typeof(T) == typeof(ushort))
        {
            return Sse2.Add(left.AsUInt16(), right.AsUInt16()).As<T>();
        }
        else if (typeof(T) == typeof(int))
        {
            return Sse2.Add(left.AsInt32(), right.AsInt32()).As<T>();
        }
        else if (typeof(T) == typeof(uint))
        {
            return Sse2.Add(left.AsUInt32(), right.AsUInt32()).As<T>();
        }
        else if (typeof(T) == typeof(long))
        {
            return Sse2.Add(left.AsInt64(), right.AsInt64()).As<T>();
        }
        else if (typeof(T) == typeof(ulong))
        {
            return Sse2.Add(left.AsUInt64(), right.AsUInt64()).As<T>();
        }
        else if (typeof(T) == typeof(float))
        {
            return Sse.Add(left.AsSingle(), right.AsSingle()).As<T>();
        }
        else if (typeof(T) == typeof(double))
        {
            return Sse2.Add(left.AsDouble(), right.AsDouble()).As<T>();
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
            return Avx2.Add(left.AsByte(), right.AsByte()).As<T>();
        }
        else if (typeof(T) == typeof(sbyte))
        {
            return Avx2.Add(left.AsSByte(), right.AsSByte()).As<T>();
        }
        else if (typeof(T) == typeof(short))
        {
            return Avx2.Add(left.AsInt16(), right.AsInt16()).As<T>();
        }
        else if (typeof(T) == typeof(ushort))
        {
            return Avx2.Add(left.AsUInt16(), right.AsUInt16()).As<T>();
        }
        else if (typeof(T) == typeof(int))
        {
            return Avx2.Add(left.AsInt32(), right.AsInt32()).As<T>();
        }
        else if (typeof(T) == typeof(uint))
        {
            return Avx2.Add(left.AsUInt32(), right.AsUInt32()).As<T>();
        }
        else if (typeof(T) == typeof(long))
        {
            return Avx2.Add(left.AsInt64(), right.AsInt64()).As<T>();
        }
        else if (typeof(T) == typeof(ulong))
        {
            return Avx2.Add(left.AsUInt64(), right.AsUInt64()).As<T>();
        }
        else if (typeof(T) == typeof(float))
        {
            return Avx.Add(left.AsSingle(), right.AsSingle()).As<T>();
        }
        else if (typeof(T) == typeof(double))
        {
            return Avx.Add(left.AsDouble(), right.AsDouble()).As<T>();
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public static Vector128<T> CreateVector128<T>(T value) where T : struct
    {
        if (typeof(T) == typeof(byte))
        {
            return Vector128.Create(Convert.ToByte(value)).As<T>();
        }
        else if (typeof(T) == typeof(sbyte))
        {
            return Vector128.Create(Convert.ToSByte(value)).As<T>();
        }
        else if (typeof(T) == typeof(short))
        {
            return Vector128.Create(Convert.ToInt16(value)).As<T>();
        }
        else if (typeof(T) == typeof(ushort))
        {
            return Vector128.Create(Convert.ToUInt16(value)).As<T>();
        }
        else if (typeof(T) == typeof(int))
        {
            return Vector128.Create(Convert.ToInt32(value)).As<T>();
        }
        else if (typeof(T) == typeof(uint))
        {
            return Vector128.Create(Convert.ToUInt32(value)).As<T>();
        }
        else if (typeof(T) == typeof(long))
        {
            return Vector128.Create(Convert.ToInt64(value)).As<T>();
        }
        else if (typeof(T) == typeof(ulong))
        {
            return Vector128.Create(Convert.ToUInt64(value)).As<T>();
        }
        else if (typeof(T) == typeof(float))
        {
            return Vector128.Create(Convert.ToSingle(value)).As<T>();
        }
        else if (typeof(T) == typeof(double))
        {
            return Vector128.Create(Convert.ToDouble(value)).As<T>();
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public static Vector256<T> CreateVector256<T>(T value) where T : struct
    {
        if (typeof(T) == typeof(byte))
        {
            return Vector256.Create(Convert.ToByte(value)).As<T>();
        }
        else if (typeof(T) == typeof(sbyte))
        {
            return Vector256.Create(Convert.ToSByte(value)).As<T>();
        }
        else if (typeof(T) == typeof(short))
        {
            return Vector256.Create(Convert.ToInt16(value)).As<T>();
        }
        else if (typeof(T) == typeof(ushort))
        {
            return Vector256.Create(Convert.ToUInt16(value)).As<T>();
        }
        else if (typeof(T) == typeof(int))
        {
            return Vector256.Create(Convert.ToInt32(value)).As<T>();
        }
        else if (typeof(T) == typeof(uint))
        {
            return Vector256.Create(Convert.ToUInt32(value)).As<T>();
        }
        else if (typeof(T) == typeof(long))
        {
            return Vector256.Create(Convert.ToInt64(value)).As<T>();
        }
        else if (typeof(T) == typeof(ulong))
        {
            return Vector256.Create(Convert.ToUInt64(value)).As<T>();
        }
        else if (typeof(T) == typeof(float))
        {
            return Vector256.Create(Convert.ToSingle(value)).As<T>();
        }
        else if (typeof(T) == typeof(double))
        {
            return Vector256.Create(Convert.ToDouble(value)).As<T>();
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
