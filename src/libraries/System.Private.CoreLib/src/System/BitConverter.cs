// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Internal.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// Converts base data types to an array of bytes, and an array of bytes to base data types.
    /// </summary>
    public static class BitConverter
    {
        // This field indicates the "endianess" of the architecture.
        // The value is set to true if the architecture is
        // little endian; false if it is big endian.
#if BIGENDIAN
        [Intrinsic]
        public static readonly bool IsLittleEndian /* = false */;
#else
        [Intrinsic]
        public static readonly bool IsLittleEndian = true;
#endif

        /// <summary>
        /// Returns the specified Boolean value as a byte array.
        /// </summary>
        /// <param name="value">A Boolean value.</param>
        /// <returns>A byte array with length 1.</returns>
        public static byte[] GetBytes(bool value)
        {
            byte[] r = new byte[1];
            r[0] = (value ? (byte)1 : (byte)0);
            return r;
        }

        /// <summary>
        /// Converts a Boolean into a span of bytes.
        /// </summary>
        /// <param name="destination">When this method returns, the bytes representing the converted Boolean.</param>
        /// <param name="value">The Boolean to convert.</param>
        /// <returns><see langword="true"/> if the conversion was successful; <see langword="false"/> otherwise.</returns>
        public static bool TryWriteBytes(Span<byte> destination, bool value)
        {
            if (destination.Length < sizeof(byte))
                return false;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value ? (byte)1 : (byte)0);
            return true;
        }

        /// <summary>
        /// Returns the specified Unicode character value as a byte array.
        /// </summary>
        /// <param name="value">A Char value.</param>
        /// <returns>An array of bytes with length 2.</returns>
        public static byte[] GetBytes(char value)
        {
            byte[] bytes = new byte[sizeof(char)];
            Unsafe.As<byte, char>(ref bytes[0]) = value;
            return bytes;
        }

        /// <summary>
        /// Converts a character into a span of bytes.
        /// </summary>
        /// <param name="destination">When this method returns, the bytes representing the converted character.</param>
        /// <param name="value">The character to convert.</param>
        /// <returns><see langword="true"/> if the conversion was successful; <see langword="false"/> otherwise.</returns>
        public static bool TryWriteBytes(Span<byte> destination, char value)
        {
            if (destination.Length < sizeof(char))
                return false;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
            return true;
        }

        /// <summary>
        /// Returns the specified 16-bit signed integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 2.</returns>
        public static byte[] GetBytes(short value)
        {
            byte[] bytes = new byte[sizeof(short)];
            Unsafe.As<byte, short>(ref bytes[0]) = value;
            return bytes;
        }

        /// <summary>
        /// Converts a 16-bit signed integer into a span of bytes.
        /// </summary>
        /// <param name="destination">When this method returns, the bytes representing the converted 16-bit signed integer.</param>
        /// <param name="value">The 16-bit signed integer to convert.</param>
        /// <returns><see langword="true"/> if the conversion was successful; <see langword="false"/> otherwise.</returns>
        public static bool TryWriteBytes(Span<byte> destination, short value)
        {
            if (destination.Length < sizeof(short))
                return false;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
            return true;
        }

        /// <summary>
        /// Returns the specified 32-bit signed integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 4.</returns>
        public static byte[] GetBytes(int value)
        {
            byte[] bytes = new byte[sizeof(int)];
            Unsafe.As<byte, int>(ref bytes[0]) = value;
            return bytes;
        }

        /// <summary>
        /// Converts a 32-bit signed integer into a span of bytes.
        /// </summary>
        /// <param name="destination">When this method returns, the bytes representing the converted 32-bit signed integer.</param>
        /// <param name="value">The 32-bit signed integer to convert.</param>
        /// <returns><see langword="true"/> if the conversion was successful; <see langword="false"/> otherwise.</returns>
        public static bool TryWriteBytes(Span<byte> destination, int value)
        {
            if (destination.Length < sizeof(int))
                return false;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
            return true;
        }

        /// <summary>
        /// Returns the specified 64-bit signed integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 8.</returns>
        public static byte[] GetBytes(long value)
        {
            byte[] bytes = new byte[sizeof(long)];
            Unsafe.As<byte, long>(ref bytes[0]) = value;
            return bytes;
        }

        /// <summary>
        /// Converts a 64-bit signed integer into a span of bytes.
        /// </summary>
        /// <param name="destination">When this method returns, the bytes representing the converted 64-bit signed integer.</param>
        /// <param name="value">The 64-bit signed integer to convert.</param>
        /// <returns><see langword="true"/> if the conversion was successful; <see langword="false"/> otherwise.</returns>
        public static bool TryWriteBytes(Span<byte> destination, long value)
        {
            if (destination.Length < sizeof(long))
                return false;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
            return true;
        }

        /// <summary>
        /// Returns the specified 16-bit unsigned integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 2.</returns>
        [CLSCompliant(false)]
        public static byte[] GetBytes(ushort value)
        {
            byte[] bytes = new byte[sizeof(ushort)];
            Unsafe.As<byte, ushort>(ref bytes[0]) = value;
            return bytes;
        }

        /// <summary>
        /// Converts a 16-bit unsigned integer into a span of bytes.
        /// </summary>
        /// <param name="destination">When this method returns, the bytes representing the converted 16-bit unsigned integer.</param>
        /// <param name="value">The 16-bit unsigned integer to convert.</param>
        /// <returns><see langword="true"/> if the conversion was successful; <see langword="false"/> otherwise.</returns>
        [CLSCompliant(false)]
        public static bool TryWriteBytes(Span<byte> destination, ushort value)
        {
            if (destination.Length < sizeof(ushort))
                return false;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
            return true;
        }

        /// <summary>
        /// Returns the specified 32-bit unsigned integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 4.</returns>
        [CLSCompliant(false)]
        public static byte[] GetBytes(uint value)
        {
            byte[] bytes = new byte[sizeof(uint)];
            Unsafe.As<byte, uint>(ref bytes[0]) = value;
            return bytes;
        }

        /// <summary>
        /// Converts a 32-bit unsigned integer into a span of bytes.
        /// </summary>
        /// <param name="destination">When this method returns, the bytes representing the converted 32-bit unsigned integer.</param>
        /// <param name="value">The 32-bit unsigned integer to convert.</param>
        /// <returns><see langword="true"/> if the conversion was successful; <see langword="false"/> otherwise.</returns>
        [CLSCompliant(false)]
        public static bool TryWriteBytes(Span<byte> destination, uint value)
        {
            if (destination.Length < sizeof(uint))
                return false;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
            return true;
        }

        /// <summary>
        /// Returns the specified 64-bit signed integer value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 8.</returns>
        [CLSCompliant(false)]
        public static byte[] GetBytes(ulong value)
        {
            byte[] bytes = new byte[sizeof(ulong)];
            Unsafe.As<byte, ulong>(ref bytes[0]) = value;
            return bytes;
        }

        /// <summary>
        /// Converts a 64-bit unsigned integer into a span of bytes.
        /// </summary>
        /// <param name="destination">When this method returns, the bytes representing the converted 64-bit unsigned integer.</param>
        /// <param name="value">The 64-bit unsigned integer to convert.</param>
        /// <returns><see langword="true"/> if the conversion was successful; <see langword="false"/> otherwise.</returns>
        [CLSCompliant(false)]
        public static bool TryWriteBytes(Span<byte> destination, ulong value)
        {
            if (destination.Length < sizeof(ulong))
                return false;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
            return true;
        }

        /// <summary>
        /// Returns the specified half-precision floating point value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 2.</returns>
        public static unsafe byte[] GetBytes(Half value)
        {
            byte[] bytes = new byte[sizeof(Half)];
            Unsafe.As<byte, Half>(ref bytes[0]) = value;
            return bytes;
        }

        /// <summary>
        /// Converts a half-precision floating-point value into a span of bytes.
        /// </summary>
        /// <param name="destination">When this method returns, the bytes representing the converted half-precision floating-point value.</param>
        /// <param name="value">The half-precision floating-point value to convert.</param>
        /// <returns><see langword="true"/> if the conversion was successful; <see langword="false"/> otherwise.</returns>
        public static unsafe bool TryWriteBytes(Span<byte> destination, Half value)
        {
            if (destination.Length < sizeof(Half))
                return false;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
            return true;
        }

        /// <summary>
        /// Returns the specified single-precision floating point value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 4.</returns>
        public static byte[] GetBytes(float value)
        {
            byte[] bytes = new byte[sizeof(float)];
            Unsafe.As<byte, float>(ref bytes[0]) = value;
            return bytes;
        }

        /// <summary>
        /// Converts a single-precision floating-point value into a span of bytes.
        /// </summary>
        /// <param name="destination">When this method returns, the bytes representing the converted single-precision floating-point value.</param>
        /// <param name="value">The single-precision floating-point value to convert.</param>
        /// <returns><see langword="true"/> if the conversion was successful; <see langword="false"/> otherwise.</returns>
        public static bool TryWriteBytes(Span<byte> destination, float value)
        {
            if (destination.Length < sizeof(float))
                return false;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
            return true;
        }

        /// <summary>
        /// Returns the specified double-precision floating point value as an array of bytes.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns>An array of bytes with length 8.</returns>
        public static byte[] GetBytes(double value)
        {
            byte[] bytes = new byte[sizeof(double)];
            Unsafe.As<byte, double>(ref bytes[0]) = value;
            return bytes;
        }

        /// <summary>
        /// Converts a double-precision floating-point value into a span of bytes.
        /// </summary>
        /// <param name="destination">When this method returns, the bytes representing the converted double-precision floating-point value.</param>
        /// <param name="value">The double-precision floating-point value to convert.</param>
        /// <returns><see langword="true"/> if the conversion was successful; <see langword="false"/> otherwise.</returns>
        public static bool TryWriteBytes(Span<byte> destination, double value)
        {
            if (destination.Length < sizeof(double))
                return false;

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
            return true;
        }

        // Converts an array of bytes into a char.
        public static char ToChar(byte[] value, int startIndex) => unchecked((char)ToInt16(value, startIndex));

        // Converts a Span into a char
        public static char ToChar(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(char))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
            return Unsafe.ReadUnaligned<char>(ref MemoryMarshal.GetReference(value));
        }

        // Converts an array of bytes into a short.
        public static short ToInt16(byte[] value, int startIndex)
        {
            if (value == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            if (unchecked((uint)startIndex) >= unchecked((uint)value.Length))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
            if (startIndex > value.Length - sizeof(short))
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall, ExceptionArgument.value);

            return Unsafe.ReadUnaligned<short>(ref value[startIndex]);
        }

        // Converts a Span into a short
        public static short ToInt16(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(short))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
            return Unsafe.ReadUnaligned<short>(ref MemoryMarshal.GetReference(value));
        }

        // Converts an array of bytes into an int.
        public static int ToInt32(byte[] value, int startIndex)
        {
            if (value == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            if (unchecked((uint)startIndex) >= unchecked((uint)value.Length))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
            if (startIndex > value.Length - sizeof(int))
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall, ExceptionArgument.value);

            return Unsafe.ReadUnaligned<int>(ref value[startIndex]);
        }

        // Converts a Span into an int
        public static int ToInt32(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(int))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
            return Unsafe.ReadUnaligned<int>(ref MemoryMarshal.GetReference(value));
        }

        // Converts an array of bytes into a long.
        public static long ToInt64(byte[] value, int startIndex)
        {
            if (value == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            if (unchecked((uint)startIndex) >= unchecked((uint)value.Length))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
            if (startIndex > value.Length - sizeof(long))
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall, ExceptionArgument.value);

            return Unsafe.ReadUnaligned<long>(ref value[startIndex]);
        }

        // Converts a Span into a long
        public static long ToInt64(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(long))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
            return Unsafe.ReadUnaligned<long>(ref MemoryMarshal.GetReference(value));
        }

        // Converts an array of bytes into an ushort.
        //
        [CLSCompliant(false)]
        public static ushort ToUInt16(byte[] value, int startIndex) => unchecked((ushort)ToInt16(value, startIndex));

        // Converts a Span into a ushort
        [CLSCompliant(false)]
        public static ushort ToUInt16(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(ushort))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
            return Unsafe.ReadUnaligned<ushort>(ref MemoryMarshal.GetReference(value));
        }

        // Converts an array of bytes into an uint.
        //
        [CLSCompliant(false)]
        public static uint ToUInt32(byte[] value, int startIndex) => unchecked((uint)ToInt32(value, startIndex));

        // Convert a Span into a uint
        [CLSCompliant(false)]
        public static uint ToUInt32(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(uint))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
            return Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(value));
        }

        // Converts an array of bytes into an unsigned long.
        //
        [CLSCompliant(false)]
        public static ulong ToUInt64(byte[] value, int startIndex) => unchecked((ulong)ToInt64(value, startIndex));

        // Converts a Span into an unsigned long
        [CLSCompliant(false)]
        public static ulong ToUInt64(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(ulong))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
            return Unsafe.ReadUnaligned<ulong>(ref MemoryMarshal.GetReference(value));
        }

        // Converts an array of bytes into a half.
        public static Half ToHalf(byte[] value, int startIndex) => Int16BitsToHalf(ToInt16(value, startIndex));

        // Converts a Span into a half
        public static unsafe Half ToHalf(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(Half))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
            return Unsafe.ReadUnaligned<Half>(ref MemoryMarshal.GetReference(value));
        }

        // Converts an array of bytes into a float.
        public static float ToSingle(byte[] value, int startIndex) => Int32BitsToSingle(ToInt32(value, startIndex));

        // Converts a Span into a float
        public static float ToSingle(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(float))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
            return Unsafe.ReadUnaligned<float>(ref MemoryMarshal.GetReference(value));
        }

        // Converts an array of bytes into a double.
        public static double ToDouble(byte[] value, int startIndex) => Int64BitsToDouble(ToInt64(value, startIndex));

        // Converts a Span into a double
        public static double ToDouble(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(double))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
            return Unsafe.ReadUnaligned<double>(ref MemoryMarshal.GetReference(value));
        }

        // Converts an array of bytes into a String.
        public static string ToString(byte[] value, int startIndex, int length)
        {
            if (value == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            if (startIndex < 0 || startIndex >= value.Length && startIndex > 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_GenericPositive);
            if (startIndex > value.Length - length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall, ExceptionArgument.value);

            if (length == 0)
            {
                return string.Empty;
            }

            if (length > (int.MaxValue / 3))
            {
                // (int.MaxValue / 3) == 715,827,882 Bytes == 699 MB
                throw new ArgumentOutOfRangeException(nameof(length), SR.Format(SR.ArgumentOutOfRange_LengthTooLarge, int.MaxValue / 3));
            }

            return string.Create(length * 3 - 1, (value, startIndex, length), static (dst, state) =>
            {
                var src = new ReadOnlySpan<byte>(state.value, state.startIndex, state.length);

                int i = 0;
                int j = 0;

                byte b = src[i++];
                dst[j++] = HexConverter.ToCharUpper(b >> 4);
                dst[j++] = HexConverter.ToCharUpper(b);

                while (i < src.Length)
                {
                    b = src[i++];
                    dst[j++] = '-';
                    dst[j++] = HexConverter.ToCharUpper(b >> 4);
                    dst[j++] = HexConverter.ToCharUpper(b);
                }
            });
        }

        // Converts an array of bytes into a String.
        public static string ToString(byte[] value)
        {
            if (value == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            return ToString(value, 0, value.Length);
        }

        // Converts an array of bytes into a String.
        public static string ToString(byte[] value, int startIndex)
        {
            if (value == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            return ToString(value, startIndex, value.Length - startIndex);
        }

        /*==================================ToBoolean===================================
        **Action:  Convert an array of bytes to a boolean value.  We treat this array
        **         as if the first 4 bytes were an Int4 an operate on this value.
        **Returns: True if the Int4 value of the first 4 bytes is non-zero.
        **Arguments: value -- The byte array
        **           startIndex -- The position within the array.
        **Exceptions: See ToInt4.
        ==============================================================================*/
        // Converts an array of bytes into a boolean.
        public static bool ToBoolean(byte[] value, int startIndex)
        {
            if (value == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            if (startIndex < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
            if (startIndex > value.Length - 1)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index); // differs from other overloads, which throw base ArgumentException

            return value[startIndex] != 0;
        }

        public static bool ToBoolean(ReadOnlySpan<byte> value)
        {
            if (value.Length < sizeof(byte))
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
            return Unsafe.ReadUnaligned<byte>(ref MemoryMarshal.GetReference(value)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe long DoubleToInt64Bits(double value)
        {
            // Workaround for https://github.com/dotnet/runtime/issues/11413
            if (Sse2.X64.IsSupported)
            {
                Vector128<long> vec = Vector128.CreateScalarUnsafe(value).AsInt64();
                return Sse2.X64.ConvertToInt64(vec);
            }

            return *((long*)&value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe double Int64BitsToDouble(long value)
        {
            // Workaround for https://github.com/dotnet/runtime/issues/11413
            if (Sse2.X64.IsSupported)
            {
                Vector128<double> vec = Vector128.CreateScalarUnsafe(value).AsDouble();
                return vec.ToScalar();
            }

            return *((double*)&value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int SingleToInt32Bits(float value)
        {
            // Workaround for https://github.com/dotnet/runtime/issues/11413
            if (Sse2.IsSupported)
            {
                Vector128<int> vec = Vector128.CreateScalarUnsafe(value).AsInt32();
                return Sse2.ConvertToInt32(vec);
            }

            return *((int*)&value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float Int32BitsToSingle(int value)
        {
            // Workaround for https://github.com/dotnet/runtime/issues/11413
            if (Sse2.IsSupported)
            {
                Vector128<float> vec = Vector128.CreateScalarUnsafe(value).AsSingle();
                return vec.ToScalar();
            }

            return *((float*)&value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe short HalfToInt16Bits(Half value)
        {
            return *((short*)&value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Half Int16BitsToHalf(short value)
        {
            return *(Half*)&value;
        }
    }
}
