// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

// We disable the below warnings since all boxing/unboxing happens
// under a value type based type check and they are impossible to hit

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type
#pragma warning disable CS8605 // Unboxing a possibly null value

namespace System.Runtime.Intrinsics
{
    internal static class Scalar<T>
    {
        public static T AllBitsSet
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (typeof(T) == typeof(byte))
                {
                    return (T)(object)byte.MaxValue;
                }
                else if (typeof(T) == typeof(double))
                {
                    return (T)(object)BitConverter.Int64BitsToDouble(-1);
                }
                else if (typeof(T) == typeof(short))
                {
                    return (T)(object)(short)-1;
                }
                else if (typeof(T) == typeof(int))
                {
                    return (T)(object)-1;
                }
                else if (typeof(T) == typeof(long))
                {
                    return (T)(object)(long)-1;
                }
                else if (typeof(T) == typeof(nint))
                {
                    return (T)(object)(nint)(-1);
                }
                else if (typeof(T) == typeof(nuint))
                {
                    return (T)(object)nuint.MaxValue;
                }
                else if (typeof(T) == typeof(sbyte))
                {
                    return (T)(object)(sbyte)-1;
                }
                else if (typeof(T) == typeof(float))
                {
                    return (T)(object)BitConverter.Int32BitsToSingle(-1);
                }
                else if (typeof(T) == typeof(ushort))
                {
                    return (T)(object)ushort.MaxValue;
                }
                else if (typeof(T) == typeof(uint))
                {
                    return (T)(object)uint.MaxValue;
                }
                else if (typeof(T) == typeof(ulong))
                {
                    return (T)(object)ulong.MaxValue;
                }
                else
                {
                    ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                    return default!;
                }
            }
        }

        public static T One
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (typeof(T) == typeof(byte))
                {
                    return (T)(object)(byte)1;
                }
                else if (typeof(T) == typeof(double))
                {
                    return (T)(object)(double)1;
                }
                else if (typeof(T) == typeof(short))
                {
                    return (T)(object)(short)1;
                }
                else if (typeof(T) == typeof(int))
                {
                    return (T)(object)1;
                }
                else if (typeof(T) == typeof(long))
                {
                    return (T)(object)(long)1;
                }
                else if (typeof(T) == typeof(nint))
                {
                    return (T)(object)(nint)1;
                }
                else if (typeof(T) == typeof(nuint))
                {
                    return (T)(object)(nuint)1;
                }
                else if (typeof(T) == typeof(sbyte))
                {
                    return (T)(object)(sbyte)1;
                }
                else if (typeof(T) == typeof(float))
                {
                    return (T)(object)(float)1;
                }
                else if (typeof(T) == typeof(ushort))
                {
                    return (T)(object)(ushort)1;
                }
                else if (typeof(T) == typeof(uint))
                {
                    return (T)(object)(uint)1;
                }
                else if (typeof(T) == typeof(ulong))
                {
                    return (T)(object)(ulong)1;
                }
                else
                {
                    ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                    return default!;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Abs(T value)
        {
            // byte, ushort, uint, and ulong should have already been handled
            // avoid Math.Abs for integers since it throws for MinValue
            if (typeof(T) == typeof(double))
            {
                return (T)(object)Math.Abs((double)(object)value);
            }
            else if (typeof(T) == typeof(short))
            {
                short v = (short)(object)value;
                if (v < 0)
                {
                    v = (short)-v;
                }
                return (T)(object)v;
            }
            else if (typeof(T) == typeof(int))
            {
                int v = (int)(object)value;
                if (v < 0)
                {
                    v = -v;
                }
                return (T)(object)v;
            }
            else if (typeof(T) == typeof(long))
            {
                long v = (long)(object)value;
                if (v < 0)
                {
                    v = -v;
                }
                return (T)(object)v;
            }
            else if (typeof(T) == typeof(nint))
            {
                nint v = (nint)(object)value;
                if (v < 0)
                {
                    v = -v;
                }
                return (T)(object)v;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                sbyte v = (sbyte)(object)value;
                if (v < 0)
                {
                    v = (sbyte)-v;
                }
                return (T)(object)v;
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)Math.Abs((float)(object)value);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Add(T left, T right)
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)(byte)((byte)(object)left + (byte)(object)right);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)(double)((double)(object)left + (double)(object)right);
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)(short)((short)(object)left + (short)(object)right);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)((int)(object)left + (int)(object)right);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)((long)(object)left + (long)(object)right);
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)((nint)(object)left + (nint)(object)right);
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)((nuint)(object)left + (nuint)(object)right);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)(sbyte)((sbyte)(object)left + (sbyte)(object)right);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)(float)((float)(object)left + (float)(object)right);
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)(ushort)((ushort)(object)left + (ushort)(object)right);
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)((uint)(object)left + (uint)(object)right);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)((ulong)(object)left + (ulong)(object)right);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Ceiling(T value)
        {
            if (typeof(T) == typeof(double))
            {
                return (T)(object)Math.Ceiling((double)(object)value);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)MathF.Ceiling((float)(object)value);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Convert(int value)
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)(byte)value;
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)(double)value;
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)(short)value;
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)value;
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)(long)value;
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)(nint)value;
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)(nuint)value;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)(sbyte)value;
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)(float)value;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)(ushort)value;
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)(uint)value;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)(ulong)value;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Divide(T left, T right)
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)(byte)((byte)(object)left / (byte)(object)right);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)(double)((double)(object)left / (double)(object)right);
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)(short)((short)(object)left / (short)(object)right);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)((int)(object)left / (int)(object)right);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)((long)(object)left / (long)(object)right);
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)((nint)(object)left / (nint)(object)right);
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)((nuint)(object)left / (nuint)(object)right);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)(sbyte)((sbyte)(object)left / (sbyte)(object)right);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)(float)((float)(object)left / (float)(object)right);
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)(ushort)((ushort)(object)left / (ushort)(object)right);
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)((uint)(object)left / (uint)(object)right);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)((ulong)(object)left / (ulong)(object)right);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(T left, T right)
        {
            if (typeof(T) == typeof(byte))
            {
                return (byte)(object)left == (byte)(object)right;
            }
            else if (typeof(T) == typeof(double))
            {
                return (double)(object)left == (double)(object)right;
            }
            else if (typeof(T) == typeof(short))
            {
                return (short)(object)left == (short)(object)right;
            }
            else if (typeof(T) == typeof(int))
            {
                return (int)(object)left == (int)(object)right;
            }
            else if (typeof(T) == typeof(long))
            {
                return (long)(object)left == (long)(object)right;
            }
            else if (typeof(T) == typeof(nint))
            {
                return (nint)(object)left == (nint)(object)right;
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (nuint)(object)left == (nuint)(object)right;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (sbyte)(object)left == (sbyte)(object)right;
            }
            else if (typeof(T) == typeof(float))
            {
                return (float)(object)left == (float)(object)right;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (ushort)(object)left == (ushort)(object)right;
            }
            else if (typeof(T) == typeof(uint))
            {
                return (uint)(object)left == (uint)(object)right;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (ulong)(object)left == (ulong)(object)right;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ExtractMostSignificantBit(T value)
        {
            if (typeof(T) == typeof(byte))
            {
                uint bits = (byte)(object)value;
                return bits >> 7;
            }
            else if (typeof(T) == typeof(double))
            {
                ulong bits = BitConverter.DoubleToUInt64Bits((double)(object)value);
                return (uint)(bits >> 63);
            }
            else if (typeof(T) == typeof(short))
            {
                uint bits = (ushort)(short)(object)value;
                return bits >> 15;
            }
            else if (typeof(T) == typeof(int))
            {
                uint bits = (uint)(int)(object)value;
                return bits >> 31;
            }
            else if (typeof(T) == typeof(long))
            {
                ulong bits = (ulong)(long)(object)value;
                return (uint)(bits >> 63);
            }
            else if (typeof(T) == typeof(nint))
            {
#if TARGET_64BIT
                ulong bits = (ulong)(nint)(object)value;
                return (uint)(bits >> 63);
#else
                uint bits = (uint)(nint)(object)value;
                return bits >> 31;
#endif
            }
            else if (typeof(T) == typeof(nuint))
            {
#if TARGET_64BIT
                ulong bits = (ulong)(nuint)(object)value;
                return (uint)(bits >> 63);
#else
                uint bits = (uint)(nuint)(object)value;
                return bits >> 31;
#endif
            }
            else if (typeof(T) == typeof(sbyte))
            {
                uint bits = (byte)(sbyte)(object)value;
                return bits >> 7;
            }
            else if (typeof(T) == typeof(float))
            {
                uint bits = BitConverter.SingleToUInt32Bits((float)(object)value);
                return bits >> 31;
            }
            else if (typeof(T) == typeof(ushort))
            {
                uint bits = (ushort)(object)value;
                return bits >> 15;
            }
            else if (typeof(T) == typeof(uint))
            {
                uint bits = (uint)(object)value;
                return bits >> 31;
            }
            else if (typeof(T) == typeof(ulong))
            {
                ulong bits = (ulong)(object)value;
                return (uint)(bits >> 63);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Floor(T value)
        {
            if (typeof(T) == typeof(double))
            {
                return (T)(object)Math.Floor((double)(object)value);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)MathF.Floor((float)(object)value);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThan(T left, T right)
        {
            if (typeof(T) == typeof(byte))
            {
                return (byte)(object)left > (byte)(object)right;
            }
            else if (typeof(T) == typeof(double))
            {
                return (double)(object)left > (double)(object)right;
            }
            else if (typeof(T) == typeof(short))
            {
                return (short)(object)left > (short)(object)right;
            }
            else if (typeof(T) == typeof(int))
            {
                return (int)(object)left > (int)(object)right;
            }
            else if (typeof(T) == typeof(long))
            {
                return (long)(object)left > (long)(object)right;
            }
            else if (typeof(T) == typeof(nint))
            {
                return (nint)(object)left > (nint)(object)right;
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (nuint)(object)left > (nuint)(object)right;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (sbyte)(object)left > (sbyte)(object)right;
            }
            else if (typeof(T) == typeof(float))
            {
                return (float)(object)left > (float)(object)right;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (ushort)(object)left > (ushort)(object)right;
            }
            else if (typeof(T) == typeof(uint))
            {
                return (uint)(object)left > (uint)(object)right;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (ulong)(object)left > (ulong)(object)right;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterThanOrEqual(T left, T right)
        {
            if (typeof(T) == typeof(byte))
            {
                return (byte)(object)left >= (byte)(object)right;
            }
            else if (typeof(T) == typeof(double))
            {
                return (double)(object)left >= (double)(object)right;
            }
            else if (typeof(T) == typeof(short))
            {
                return (short)(object)left >= (short)(object)right;
            }
            else if (typeof(T) == typeof(int))
            {
                return (int)(object)left >= (int)(object)right;
            }
            else if (typeof(T) == typeof(long))
            {
                return (long)(object)left >= (long)(object)right;
            }
            else if (typeof(T) == typeof(nint))
            {
                return (nint)(object)left >= (nint)(object)right;
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (nuint)(object)left >= (nuint)(object)right;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (sbyte)(object)left >= (sbyte)(object)right;
            }
            else if (typeof(T) == typeof(float))
            {
                return (float)(object)left >= (float)(object)right;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (ushort)(object)left >= (ushort)(object)right;
            }
            else if (typeof(T) == typeof(uint))
            {
                return (uint)(object)left >= (uint)(object)right;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (ulong)(object)left >= (ulong)(object)right;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThan(T left, T right)
        {
            if (typeof(T) == typeof(byte))
            {
                return (byte)(object)left < (byte)(object)right;
            }
            else if (typeof(T) == typeof(double))
            {
                return (double)(object)left < (double)(object)right;
            }
            else if (typeof(T) == typeof(short))
            {
                return (short)(object)left < (short)(object)right;
            }
            else if (typeof(T) == typeof(int))
            {
                return (int)(object)left < (int)(object)right;
            }
            else if (typeof(T) == typeof(long))
            {
                return (long)(object)left < (long)(object)right;
            }
            else if (typeof(T) == typeof(nint))
            {
                return (nint)(object)left < (nint)(object)right;
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (nuint)(object)left < (nuint)(object)right;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (sbyte)(object)left < (sbyte)(object)right;
            }
            else if (typeof(T) == typeof(float))
            {
                return (float)(object)left < (float)(object)right;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (ushort)(object)left < (ushort)(object)right;
            }
            else if (typeof(T) == typeof(uint))
            {
                return (uint)(object)left < (uint)(object)right;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (ulong)(object)left < (ulong)(object)right;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThanOrEqual(T left, T right)
        {
            if (typeof(T) == typeof(byte))
            {
                return (byte)(object)left <= (byte)(object)right;
            }
            else if (typeof(T) == typeof(double))
            {
                return (double)(object)left <= (double)(object)right;
            }
            else if (typeof(T) == typeof(short))
            {
                return (short)(object)left <= (short)(object)right;
            }
            else if (typeof(T) == typeof(int))
            {
                return (int)(object)left <= (int)(object)right;
            }
            else if (typeof(T) == typeof(long))
            {
                return (long)(object)left <= (long)(object)right;
            }
            else if (typeof(T) == typeof(nint))
            {
                return (nint)(object)left <= (nint)(object)right;
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (nuint)(object)left <= (nuint)(object)right;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (sbyte)(object)left <= (sbyte)(object)right;
            }
            else if (typeof(T) == typeof(float))
            {
                return (float)(object)left <= (float)(object)right;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (ushort)(object)left <= (ushort)(object)right;
            }
            else if (typeof(T) == typeof(uint))
            {
                return (uint)(object)left <= (uint)(object)right;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (ulong)(object)left <= (ulong)(object)right;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Multiply(T left, T right)
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)(byte)((byte)(object)left * (byte)(object)right);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)(double)((double)(object)left * (double)(object)right);
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)(short)((short)(object)left * (short)(object)right);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)((int)(object)left * (int)(object)right);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)((long)(object)left * (long)(object)right);
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)((nint)(object)left * (nint)(object)right);
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)((nuint)(object)left * (nuint)(object)right);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)(sbyte)((sbyte)(object)left * (sbyte)(object)right);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)(float)((float)(object)left * (float)(object)right);
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)(ushort)((ushort)(object)left * (ushort)(object)right);
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)((uint)(object)left * (uint)(object)right);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)((ulong)(object)left * (ulong)(object)right);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }

        public static bool ObjectEquals(T left, T right)
        {
            if (typeof(T) == typeof(byte))
            {
                return ((byte)(object)left).Equals((byte)(object)right);
            }
            else if (typeof(T) == typeof(double))
            {
                return ((double)(object)left).Equals((double)(object)right);
            }
            else if (typeof(T) == typeof(short))
            {
                return ((short)(object)left).Equals((short)(object)right);
            }
            else if (typeof(T) == typeof(int))
            {
                return ((int)(object)left).Equals((int)(object)right);
            }
            else if (typeof(T) == typeof(long))
            {
                return ((long)(object)left).Equals((long)(object)right);
            }
            else if (typeof(T) == typeof(nint))
            {
                return ((nint)(object)left).Equals((nint)(object)right);
            }
            else if (typeof(T) == typeof(nuint))
            {
                return ((nuint)(object)left).Equals((nuint)(object)right);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return ((sbyte)(object)left).Equals((sbyte)(object)right);
            }
            else if (typeof(T) == typeof(float))
            {
                return ((float)(object)left).Equals((float)(object)right);
            }
            else if (typeof(T) == typeof(ushort))
            {
                return ((ushort)(object)left).Equals((ushort)(object)right);
            }
            else if (typeof(T) == typeof(uint))
            {
                return ((uint)(object)left).Equals((uint)(object)right);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return ((ulong)(object)left).Equals((ulong)(object)right);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ShiftLeft(T value, int shiftCount)
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)(byte)((byte)(object)value << (shiftCount & 7));
            }
            else if (typeof(T) == typeof(double))
            {
                long bits = BitConverter.DoubleToInt64Bits((double)(object)value);
                double result = BitConverter.Int64BitsToDouble(bits << shiftCount);
                return (T)(object)(double)result;
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)(short)((short)(object)value << (shiftCount & 15));
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)(int)((int)(object)value << shiftCount);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)(long)((long)(object)value << shiftCount);
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)(nint)((nint)(object)value << shiftCount);
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)(nuint)((nuint)(object)value << shiftCount);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)(sbyte)((sbyte)(object)value << (shiftCount & 7));
            }
            else if (typeof(T) == typeof(float))
            {
                int bits = BitConverter.SingleToInt32Bits((float)(object)value);
                float result = BitConverter.Int32BitsToSingle(bits << shiftCount);
                return (T)(object)(float)result;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)(ushort)((ushort)(object)value << (shiftCount & 15));
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)(uint)((uint)(object)value << shiftCount);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)(ulong)((ulong)(object)value << shiftCount);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ShiftRightArithmetic(T value, int shiftCount)
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)(byte)((byte)(object)value >> (shiftCount & 7));
            }
            else if (typeof(T) == typeof(double))
            {
                long bits = BitConverter.DoubleToInt64Bits((double)(object)value);
                double result = BitConverter.Int64BitsToDouble(bits >> shiftCount);
                return (T)(object)(double)result;
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)(short)((short)(object)value >> (shiftCount & 15));
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)(int)((int)(object)value >> shiftCount);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)(long)((long)(object)value >> shiftCount);
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)(nint)((nint)(object)value >> shiftCount);
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)(nuint)((nuint)(object)value >> shiftCount);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)(sbyte)((sbyte)(object)value >> (shiftCount & 7));
            }
            else if (typeof(T) == typeof(float))
            {
                int bits = BitConverter.SingleToInt32Bits((float)(object)value);
                float result = BitConverter.Int32BitsToSingle(bits >> shiftCount);
                return (T)(object)(float)result;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)(ushort)((ushort)(object)value >> (shiftCount & 15));
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)(uint)((uint)(object)value >> shiftCount);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)(ulong)((ulong)(object)value >> shiftCount);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ShiftRightLogical(T value, int shiftCount)
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)(byte)((byte)(object)value >>> (shiftCount & 7));
            }
            else if (typeof(T) == typeof(double))
            {
                long bits = BitConverter.DoubleToInt64Bits((double)(object)value);
                double result = BitConverter.Int64BitsToDouble(bits >>> shiftCount);
                return (T)(object)(double)result;
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)(short)((ushort)(short)(object)value >>> (shiftCount & 15));
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)(int)((uint)(int)(object)value >>> shiftCount);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)(long)((ulong)(long)(object)value >>> shiftCount);
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)(nint)((nuint)(nint)(object)value >>> shiftCount);
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)(nuint)((nuint)(object)value >>> shiftCount);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)(sbyte)((byte)(sbyte)(object)value >>> (shiftCount & 7));
            }
            else if (typeof(T) == typeof(float))
            {
                int bits = BitConverter.SingleToInt32Bits((float)(object)value);
                float result = BitConverter.Int32BitsToSingle(bits >>> shiftCount);
                return (T)(object)(float)result;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)(ushort)((ushort)(object)value >>> (shiftCount & 15));
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)(uint)((uint)(object)value >>> shiftCount);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)(ulong)((ulong)(object)value >>> shiftCount);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Sqrt(T value)
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)(byte)MathF.Sqrt((byte)(object)value);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)(double)Math.Sqrt((double)(object)value);
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)(short)MathF.Sqrt((short)(object)value);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)(int)Math.Sqrt((int)(object)value);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)(long)Math.Sqrt((long)(object)value);
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)(nint)Math.Sqrt((nint)(object)value);
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)(nuint)Math.Sqrt((nuint)(object)value);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)(sbyte)MathF.Sqrt((sbyte)(object)value);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)(float)MathF.Sqrt((float)(object)value);
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)(ushort)MathF.Sqrt((ushort)(object)value);
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)(uint)Math.Sqrt((uint)(object)value);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)(ulong)Math.Sqrt((ulong)(object)value);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Subtract(T left, T right)
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)(byte)((byte)(object)left - (byte)(object)right);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)(double)((double)(object)left - (double)(object)right);
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)(short)((short)(object)left - (short)(object)right);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)((int)(object)left - (int)(object)right);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)((long)(object)left - (long)(object)right);
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)((nint)(object)left - (nint)(object)right);
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)((nuint)(object)left - (nuint)(object)right);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)(sbyte)((sbyte)(object)left - (sbyte)(object)right);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)(float)((float)(object)left - (float)(object)right);
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)(ushort)((ushort)(object)left - (ushort)(object)right);
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)((uint)(object)left - (uint)(object)right);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)((ulong)(object)left - (ulong)(object)right);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }
    }
}

#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type
#pragma warning restore CS8605 // Unboxing a possibly null value
