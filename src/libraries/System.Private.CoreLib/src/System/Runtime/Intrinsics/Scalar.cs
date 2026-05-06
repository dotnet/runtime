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
        public static T AddSaturate(T left, T right)
        {
            // For unsigned types, addition can only overflow up
            // so we need to check if the result is less than one
            // of the inputs and return MaxValue if it is.
            //
            // For signed types, addition can overflow in either
            // direction. However, this can only occur if the signs
            // of the inputs are the same and the sign of the result
            // then differs. This simplifies to, given a = b + c:
            //    ((a ^ b) & ~(b ^ c)) < 0
            //
            // This simplification works because all negative values
            // have their most significant bit set, while all positive
            // values have it clear. Consider the below:
            //
            //  p = p + p:    (0 ^ 0) & ~(0 ^ 0)  ->  0 & ~0  ->  0 & 1  ->  0        can  overflow and didnt
            //  p = p + n:    (0 ^ 0) & ~(0 ^ 1)  ->  0 & ~1  ->  0 & 0  ->  0        cant overflow
            //  p = n + p:    (0 ^ 1) & ~(1 ^ 0)  ->  1 & ~1  ->  1 & 0  ->  0        cant overflow
            //  p = n + n:    (0 ^ 1) & ~(1 ^ 1)  ->  1 & ~0  ->  1 & 1  ->  1        can  overflow and did
            //  n = p + p:    (1 ^ 0) & ~(0 ^ 0)  ->  1 & ~0  ->  1 & 1  ->  1        can  overflow and did
            //  n = p + n:    (1 ^ 0) & ~(0 ^ 1)  ->  1 & ~1  ->  1 & 0  ->  0        cant overflow
            //  n = n + p:    (1 ^ 1) & ~(1 ^ 0)  ->  0 & ~1  ->  0 & 0  ->  0        cant overflow
            //  n = n + n:    (1 ^ 1) & ~(1 ^ 1)  ->  0 & ~0  ->  0 & 1  ->  0        can  overflow and didnt
            //               |_______| |________|    |_| |__|    |_| |_|    |_|
            //                   |          \_________|____\______|___\______|_______ produces 1 if the signs are the same, meaning overflow could occur
            //                   |                    |           |          |
            //                   \____________________\___________\__________|_______ produces 1 if the output signs differs from the first input, meaning overflow could occur
            //                                                               |
            //                                                               \_______ produces 1 if the input signs are the same and the output sign differs from that, meaning overflow did occur
            //
            // If we did overflow then a negative result needs to
            // become MaxValue, while a positive result needs to become
            // MinValue.

            if (typeof(T) == typeof(byte))
            {
                byte actualLeft = (byte)(object)left;
                byte actualRight = (byte)(object)right;

                byte result = (byte)(actualLeft + actualRight);

                if (result < actualLeft)
                {
                    result = byte.MaxValue;
                }

                return (T)(object)result;
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)(double)((double)(object)left + (double)(object)right);
            }
            else if (typeof(T) == typeof(short))
            {
                short actualLeft = (short)(object)left;
                short actualRight = (short)(object)right;

                short result = (short)(actualLeft + actualRight);

                if (((result ^ actualLeft) & ~(actualLeft ^ actualRight)) < 0)
                {
                    result = (result < 0) ? short.MaxValue : short.MinValue;
                }

                return (T)(object)result;
            }
            else if (typeof(T) == typeof(int))
            {
                int actualLeft = (int)(object)left;
                int actualRight = (int)(object)right;

                int result = (int)(actualLeft + actualRight);

                if (((result ^ actualLeft) & ~(actualLeft ^ actualRight)) < 0)
                {
                    result = (result < 0) ? int.MaxValue : int.MinValue;
                }

                return (T)(object)result;
            }
            else if (typeof(T) == typeof(long))
            {
                long actualLeft = (long)(object)left;
                long actualRight = (long)(object)right;

                long result = (long)(actualLeft + actualRight);

                if (((result ^ actualLeft) & ~(actualLeft ^ actualRight)) < 0)
                {
                    result = (result < 0) ? long.MaxValue : long.MinValue;
                }

                return (T)(object)result;
            }
            else if (typeof(T) == typeof(nint))
            {
                nint actualLeft = (nint)(object)left;
                nint actualRight = (nint)(object)right;

                nint result = (nint)(actualLeft + actualRight);

                if (((result ^ actualLeft) & ~(actualLeft ^ actualRight)) < 0)
                {
                    result = (result < 0) ? nint.MaxValue : nint.MinValue;
                }

                return (T)(object)result;
            }
            else if (typeof(T) == typeof(nuint))
            {
                nuint actualLeft = (nuint)(object)left;
                nuint actualRight = (nuint)(object)right;

                nuint result = (nuint)(actualLeft + actualRight);

                if (result < actualLeft)
                {
                    result = nuint.MaxValue;
                }

                return (T)(object)result;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                sbyte actualLeft = (sbyte)(object)left;
                sbyte actualRight = (sbyte)(object)right;

                sbyte result = (sbyte)(actualLeft + actualRight);

                if (((result ^ actualLeft) & ~(actualLeft ^ actualRight)) < 0)
                {
                    result = (result < 0) ? sbyte.MaxValue : sbyte.MinValue;
                }

                return (T)(object)result;
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)(float)((float)(object)left + (float)(object)right);
            }
            else if (typeof(T) == typeof(ushort))
            {
                ushort actualLeft = (ushort)(object)left;
                ushort actualRight = (ushort)(object)right;

                ushort result = (ushort)(actualLeft + actualRight);

                if (result < actualLeft)
                {
                    result = ushort.MaxValue;
                }

                return (T)(object)result;
            }
            else if (typeof(T) == typeof(uint))
            {
                uint actualLeft = (uint)(object)left;
                uint actualRight = (uint)(object)right;

                uint result = (uint)(actualLeft + actualRight);

                if (result < actualLeft)
                {
                    result = uint.MaxValue;
                }

                return (T)(object)result;
            }
            else if (typeof(T) == typeof(ulong))
            {
                ulong actualLeft = (ulong)(object)left;
                ulong actualRight = (ulong)(object)right;

                ulong result = (ulong)(actualLeft + actualRight);

                if (result < actualLeft)
                {
                    result = ulong.MaxValue;
                }

                return (T)(object)result;
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
        public static T CopySign(T value, T sign)
        {
            // byte, ushort, uint, and ulong should have already been handled
            // avoid Math.Abs for integers since it throws for MinValue
            if (typeof(T) == typeof(double))
            {
                return (T)(object)double.CopySign((double)(object)value, (double)(object)sign);
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)short.CopySign((short)(object)value, (short)(object)sign);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)int.CopySign((int)(object)value, (int)(object)sign);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)long.CopySign((long)(object)value, (long)(object)sign);
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)nint.CopySign((nint)(object)value, (nint)(object)sign);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)sbyte.CopySign((sbyte)(object)value, (sbyte)(object)sign);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)float.CopySign((float)(object)value, (float)(object)sign);
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
        public static T Max(T left, T right)
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)byte.Max((byte)(object)left, (byte)(object)right);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)double.Max((double)(object)left, (double)(object)right);
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)short.Max((short)(object)left, (short)(object)right);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)int.Max((int)(object)left, (int)(object)right);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)long.Max((long)(object)left, (long)(object)right);
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)nint.Max((nint)(object)left, (nint)(object)right);
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)nuint.Max((nuint)(object)left, (nuint)(object)right);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)sbyte.Max((sbyte)(object)left, (sbyte)(object)right);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)float.Max((float)(object)left, (float)(object)right);
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)ushort.Max((ushort)(object)left, (ushort)(object)right);
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)uint.Max((uint)(object)left, (uint)(object)right);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)ulong.Max((ulong)(object)left, (ulong)(object)right);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T MaxMagnitude(T left, T right)
        {
            if (typeof(T) == typeof(double))
            {
                return (T)(object)double.MaxMagnitude((double)(object)left, (double)(object)right);
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)short.MaxMagnitude((short)(object)left, (short)(object)right);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)int.MaxMagnitude((int)(object)left, (int)(object)right);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)long.MaxMagnitude((long)(object)left, (long)(object)right);
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)nint.MaxMagnitude((nint)(object)left, (nint)(object)right);
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)nuint.Max((nuint)(object)left, (nuint)(object)right);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)sbyte.MaxMagnitude((sbyte)(object)left, (sbyte)(object)right);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)float.MaxMagnitude((float)(object)left, (float)(object)right);
            }
            else
            {
                return Max(left, right);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T MaxMagnitudeNumber(T left, T right)
        {
            if (typeof(T) == typeof(double))
            {
                return (T)(object)double.MaxMagnitudeNumber((double)(object)left, (double)(object)right);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)float.MaxMagnitudeNumber((float)(object)left, (float)(object)right);
            }
            else
            {
                return MaxMagnitude(left, right);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T MaxNumber(T left, T right)
        {
            if (typeof(T) == typeof(double))
            {
                return (T)(object)double.MaxNumber((double)(object)left, (double)(object)right);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)float.MaxNumber((float)(object)left, (float)(object)right);
            }
            else
            {
                return Max(left, right);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Min(T left, T right)
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)byte.Min((byte)(object)left, (byte)(object)right);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)double.Min((double)(object)left, (double)(object)right);
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)short.Min((short)(object)left, (short)(object)right);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)int.Min((int)(object)left, (int)(object)right);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)long.Min((long)(object)left, (long)(object)right);
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)nint.Min((nint)(object)left, (nint)(object)right);
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)nuint.Min((nuint)(object)left, (nuint)(object)right);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)sbyte.Min((sbyte)(object)left, (sbyte)(object)right);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)float.Min((float)(object)left, (float)(object)right);
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)ushort.Min((ushort)(object)left, (ushort)(object)right);
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)uint.Min((uint)(object)left, (uint)(object)right);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)ulong.Min((ulong)(object)left, (ulong)(object)right);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T MinMagnitude(T left, T right)
        {
            if (typeof(T) == typeof(double))
            {
                return (T)(object)double.MinMagnitude((double)(object)left, (double)(object)right);
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)short.MinMagnitude((short)(object)left, (short)(object)right);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)int.MinMagnitude((int)(object)left, (int)(object)right);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)long.MinMagnitude((long)(object)left, (long)(object)right);
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)nint.MinMagnitude((nint)(object)left, (nint)(object)right);
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)nuint.Min((nuint)(object)left, (nuint)(object)right);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)sbyte.MinMagnitude((sbyte)(object)left, (sbyte)(object)right);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)float.MinMagnitude((float)(object)left, (float)(object)right);
            }
            else
            {
                return Min(left, right);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T MinMagnitudeNumber(T left, T right)
        {
            if (typeof(T) == typeof(double))
            {
                return (T)(object)double.MinMagnitudeNumber((double)(object)left, (double)(object)right);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)float.MinMagnitudeNumber((float)(object)left, (float)(object)right);
            }
            else
            {
                return MinMagnitude(left, right);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T MinNumber(T left, T right)
        {
            if (typeof(T) == typeof(double))
            {
                return (T)(object)double.MinNumber((double)(object)left, (double)(object)right);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)float.MinNumber((float)(object)left, (float)(object)right);
            }
            else
            {
                return Min(left, right);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T MultiplyAddEstimate(T left, T right, T addend)
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)(byte)((((byte)(object)left * (byte)(object)right)) + (byte)(object)addend);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)double.MultiplyAddEstimate((double)(object)left, (double)(object)right, (double)(object)addend);
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)(short)((((short)(object)left * (short)(object)right)) + (short)(object)addend);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)(int)((((int)(object)left * (int)(object)right)) + (int)(object)addend);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)(long)((((long)(object)left * (long)(object)right)) + (long)(object)addend);
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)(nint)((((nint)(object)left * (nint)(object)right)) + (nint)(object)addend);
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)(nuint)((((nuint)(object)left * (nuint)(object)right)) + (nuint)(object)addend);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)(sbyte)((((sbyte)(object)left * (sbyte)(object)right)) + (sbyte)(object)addend);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)float.MultiplyAddEstimate((float)(object)left, (float)(object)right, (float)(object)addend);
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)(ushort)((((ushort)(object)left * (ushort)(object)right)) + (ushort)(object)addend);
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)(uint)((((uint)(object)left * (uint)(object)right)) + (uint)(object)addend);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)(ulong)((((ulong)(object)left * (ulong)(object)right)) + (ulong)(object)addend);
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
        public static T Round(T value)
        {
            if (typeof(T) == typeof(double))
            {
                return (T)(object)Math.Round((double)(object)value);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)MathF.Round((float)(object)value);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SubtractSaturate(T left, T right)
        {
            // For unsigned types, subtraction can only overflow up
            // so we need to check if the result is greater than the
            // first input and return MinValue if it is.
            //
            // For signed types, subtraction can overflow in either
            // direction. However, this can only occur if the signs
            // of the inputs differ and the sign of the result
            // then differs from the first input. This simplifies to: ((r ^ l) & (l ^ r)) < 0
            //
            // This simplification works because all negative values
            // have their most significant bit set, while all positive
            // values have it clear. Consider the below:
            //
            //  p = p - p:    (0 ^ 0) & (0 ^ 0)  ->  0 & 0  ->  0        cant overflow
            //  p = p - n:    (0 ^ 0) & (0 ^ 1)  ->  0 & 1  ->  0        can  overflow and didn't
            //  p = n - p:    (0 ^ 1) & (1 ^ 0)  ->  1 & 1  ->  1        can  overflow and did
            //  p = n - n:    (0 ^ 1) & (1 ^ 1)  ->  1 & 0  ->  0        cant overflow
            //  n = p - p:    (1 ^ 0) & (0 ^ 0)  ->  1 & 0  ->  0        cant overflow
            //  n = p - n:    (1 ^ 0) & (0 ^ 1)  ->  1 & 1  ->  1        can  overflow and did
            //  n = n - p:    (1 ^ 1) & (1 ^ 0)  ->  0 & 1  ->  0        can  overflow and didn't
            //  n = n - n:    (1 ^ 1) & (1 ^ 1)  ->  0 & 0  ->  0        cant overflow
            //               |_______| |_______|    |_| |_|    |_|
            //                   |          \________|___\______|_______ produces 1 if the signs are the differ, meaning overflow could occur
            //                   |                   |          |
            //                   \___________________\__________|_______ produces 1 if the output signs differs from the first input, meaning overflow could occur
            //                                                  |
            //                                                  \_______ produces 1 if the input signs are the same and the output sign differs from that, meaning overflow did occur
            //
            // If we did overflow then a negative result needs to
            // become MaxValue, while a positive result needs to become
            // MinValue.

            if (typeof(T) == typeof(byte))
            {
                byte actualLeft = (byte)(object)left;
                byte actualRight = (byte)(object)right;

                byte result = (byte)(actualLeft - actualRight);

                if (result > actualLeft)
                {
                    result = byte.MinValue;
                }

                return (T)(object)result;
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)(double)((double)(object)left - (double)(object)right);
            }
            else if (typeof(T) == typeof(short))
            {
                short actualLeft = (short)(object)left;
                short actualRight = (short)(object)right;

                short result = (short)(actualLeft - actualRight);

                if (((result ^ actualLeft) & (actualLeft ^ actualRight)) < 0)
                {
                    result = (result < 0) ? short.MaxValue : short.MinValue;
                }

                return (T)(object)result;
            }
            else if (typeof(T) == typeof(int))
            {
                int actualLeft = (int)(object)left;
                int actualRight = (int)(object)right;

                int result = (int)(actualLeft - actualRight);

                if (((result ^ actualLeft) & (actualLeft ^ actualRight)) < 0)
                {
                    result = (result < 0) ? int.MaxValue : int.MinValue;
                }

                return (T)(object)result;
            }
            else if (typeof(T) == typeof(long))
            {
                long actualLeft = (long)(object)left;
                long actualRight = (long)(object)right;

                long result = (long)(actualLeft - actualRight);

                if (((result ^ actualLeft) & (actualLeft ^ actualRight)) < 0)
                {
                    result = (result < 0) ? long.MaxValue : long.MinValue;
                }

                return (T)(object)result;
            }
            else if (typeof(T) == typeof(nint))
            {
                nint actualLeft = (nint)(object)left;
                nint actualRight = (nint)(object)right;

                nint result = (nint)(actualLeft - actualRight);

                if (((result ^ actualLeft) & (actualLeft ^ actualRight)) < 0)
                {
                    result = (result < 0) ? nint.MaxValue : nint.MinValue;
                }

                return (T)(object)result;
            }
            else if (typeof(T) == typeof(nuint))
            {
                nuint actualLeft = (nuint)(object)left;
                nuint actualRight = (nuint)(object)right;

                nuint result = (nuint)(actualLeft - actualRight);

                if (result > actualLeft)
                {
                    result = nuint.MinValue;
                }

                return (T)(object)result;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                sbyte actualLeft = (sbyte)(object)left;
                sbyte actualRight = (sbyte)(object)right;

                sbyte result = (sbyte)(actualLeft - actualRight);

                if (((result ^ actualLeft) & (actualLeft ^ actualRight)) < 0)
                {
                    result = (result < 0) ? sbyte.MaxValue : sbyte.MinValue;
                }

                return (T)(object)result;
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)(float)((float)(object)left - (float)(object)right);
            }
            else if (typeof(T) == typeof(ushort))
            {
                ushort actualLeft = (ushort)(object)left;
                ushort actualRight = (ushort)(object)right;

                ushort result = (ushort)(actualLeft - actualRight);

                if (result > actualLeft)
                {
                    result = ushort.MinValue;
                }

                return (T)(object)result;
            }
            else if (typeof(T) == typeof(uint))
            {
                uint actualLeft = (uint)(object)left;
                uint actualRight = (uint)(object)right;

                uint result = (uint)(actualLeft - actualRight);

                if (result > actualLeft)
                {
                    result = uint.MinValue;
                }

                return (T)(object)result;
            }
            else if (typeof(T) == typeof(ulong))
            {
                ulong actualLeft = (ulong)(object)left;
                ulong actualRight = (ulong)(object)right;

                ulong result = (ulong)(actualLeft - actualRight);

                if (result > actualLeft)
                {
                    result = ulong.MinValue;
                }

                return (T)(object)result;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
                return default!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Truncate(T value)
        {
            if (typeof(T) == typeof(double))
            {
                return (T)(object)Math.Truncate((double)(object)value);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)MathF.Truncate((float)(object)value);
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
