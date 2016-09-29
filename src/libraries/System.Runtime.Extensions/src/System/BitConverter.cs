// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Security;

namespace System
{
    // The BitConverter class contains methods for
    // converting an array of bytes to one of the base data 
    // types, as well as for converting a base data type to an
    // array of bytes.
    public static class BitConverter
    {
        public static readonly bool IsLittleEndian = GetIsLittleEndian();

        private static unsafe bool GetIsLittleEndian()
        {
            int i = 1;
            return *((byte*)&i) == 1;
        }

        // Converts a Boolean into an array of bytes with length one.
        public static byte[] GetBytes(bool value)
        {
            Contract.Ensures(Contract.Result<byte[]>() != null);
            Contract.Ensures(Contract.Result<byte[]>().Length == 1);

            byte[] r = new byte[1];
            r[0] = (value ? (byte)1 : (byte)0);
            return r;
        }

        // Converts a char into an array of bytes with length two.
        public static byte[] GetBytes(char value)
        {
            Contract.Ensures(Contract.Result<byte[]>() != null);
            Contract.Ensures(Contract.Result<byte[]>().Length == 2);

            return GetBytes((short)value);
        }

        // Converts a short into an array of bytes with length
        // two.
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static byte[] GetBytes(short value)
        {
            Contract.Ensures(Contract.Result<byte[]>() != null);
            Contract.Ensures(Contract.Result<byte[]>().Length == 2);

            byte[] bytes = new byte[2];
            fixed (byte* b = bytes)
                *((short*)b) = value;
            return bytes;
        }

        // Converts an int into an array of bytes with length 
        // four.
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static byte[] GetBytes(int value)
        {
            Contract.Ensures(Contract.Result<byte[]>() != null);
            Contract.Ensures(Contract.Result<byte[]>().Length == 4);

            byte[] bytes = new byte[4];
            fixed (byte* b = bytes)
                *((int*)b) = value;
            return bytes;
        }

        // Converts a long into an array of bytes with length 
        // eight.
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static byte[] GetBytes(long value)
        {
            Contract.Ensures(Contract.Result<byte[]>() != null);
            Contract.Ensures(Contract.Result<byte[]>().Length == 8);

            byte[] bytes = new byte[8];
            fixed (byte* b = bytes)
                *((long*)b) = value;
            return bytes;
        }

        // Converts an ushort into an array of bytes with
        // length two.
        [CLSCompliant(false)]
        public static byte[] GetBytes(ushort value)
        {
            Contract.Ensures(Contract.Result<byte[]>() != null);
            Contract.Ensures(Contract.Result<byte[]>().Length == 2);

            return GetBytes((short)value);
        }

        // Converts an uint into an array of bytes with
        // length four.
        [CLSCompliant(false)]
        public static byte[] GetBytes(uint value)
        {
            Contract.Ensures(Contract.Result<byte[]>() != null);
            Contract.Ensures(Contract.Result<byte[]>().Length == 4);

            return GetBytes((int)value);
        }

        // Converts an unsigned long into an array of bytes with
        // length eight.
        [CLSCompliant(false)]
        public static byte[] GetBytes(ulong value)
        {
            Contract.Ensures(Contract.Result<byte[]>() != null);
            Contract.Ensures(Contract.Result<byte[]>().Length == 8);

            return GetBytes((long)value);
        }

        // Converts a float into an array of bytes with length 
        // four.
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static byte[] GetBytes(float value)
        {
            Contract.Ensures(Contract.Result<byte[]>() != null);
            Contract.Ensures(Contract.Result<byte[]>().Length == 4);

            return GetBytes(*(int*)&value);
        }

        // Converts a double into an array of bytes with length 
        // eight.
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static byte[] GetBytes(double value)
        {
            Contract.Ensures(Contract.Result<byte[]>() != null);
            Contract.Ensures(Contract.Result<byte[]>().Length == 8);

            return GetBytes(*(long*)&value);
        }

        // Converts an array of bytes into a char.  
        public static char ToChar(byte[] value, int startIndex)
        {
            if (value == null)
                ThrowValueArgumentNull();
            if ((uint)startIndex >= value.Length)
                ThrowStartIndexArgumentOutOfRange();
            if (startIndex > value.Length - 2)
                ThrowValueArgumentTooSmall();
            Contract.EndContractBlock();

            return (char)ToInt16(value, startIndex);
        }

        // Converts an array of bytes into a short.  
        [System.Security.SecuritySafeCritical]  // auto-generated
        public static unsafe short ToInt16(byte[] value, int startIndex)
        {
            if (value == null)
                ThrowValueArgumentNull();
            if ((uint)startIndex >= value.Length)
                ThrowStartIndexArgumentOutOfRange();
            if (startIndex > value.Length - 2)
                ThrowValueArgumentTooSmall();
            Contract.EndContractBlock();

            fixed (byte* pbyte = &value[startIndex])
            {
                if (startIndex % 2 == 0)
                {
                    // data is aligned 
                    return *((short*)pbyte);
                }
                else if (IsLittleEndian)
                {
                    return (short)((*pbyte) | (*(pbyte + 1) << 8));
                }
                else
                {
                    return (short)((*pbyte << 8) | (*(pbyte + 1)));
                }
            }
        }

        // Converts an array of bytes into an int.  
        [System.Security.SecuritySafeCritical]  // auto-generated
        public static unsafe int ToInt32(byte[] value, int startIndex)
        {
            if (value == null)
                ThrowValueArgumentNull();
            if ((uint)startIndex >= value.Length)
                ThrowStartIndexArgumentOutOfRange();
            if (startIndex > value.Length - 4)
                ThrowValueArgumentTooSmall();
            Contract.EndContractBlock();

            fixed (byte* pbyte = &value[startIndex])
            {
                if (startIndex % 4 == 0)
                {
                    // data is aligned 
                    return *((int*)pbyte);
                }
                else if (IsLittleEndian)
                {
                    return (*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
                }
                else
                {
                    return (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                }
            }
        }

        // Converts an array of bytes into a long.  
        [System.Security.SecuritySafeCritical]  // auto-generated
        public static unsafe long ToInt64(byte[] value, int startIndex)
        {
            if (value == null)
                ThrowValueArgumentNull();
            if ((uint)startIndex >= value.Length)
                ThrowStartIndexArgumentOutOfRange();
            if (startIndex > value.Length - 8)
                ThrowValueArgumentTooSmall();
            Contract.EndContractBlock();

            fixed (byte* pbyte = &value[startIndex])
            {
                if (startIndex % 8 == 0)
                { 
                    // data is aligned 
                    return *((long*)pbyte);
                }
                else if (IsLittleEndian)
                {
                    int i1 = (*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
                    int i2 = (*(pbyte + 4)) | (*(pbyte + 5) << 8) | (*(pbyte + 6) << 16) | (*(pbyte + 7) << 24);
                    return (uint)i1 | ((long)i2 << 32);
                }
                else
                {
                    int i1 = (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                    int i2 = (*(pbyte + 4) << 24) | (*(pbyte + 5) << 16) | (*(pbyte + 6) << 8) | (*(pbyte + 7));
                    return (uint)i2 | ((long)i1 << 32);
                }
            }
        }


        // Converts an array of bytes into an ushort.
        // 
        [CLSCompliant(false)]
        public static ushort ToUInt16(byte[] value, int startIndex)
        {
            if (value == null)
                ThrowValueArgumentNull();
            if ((uint)startIndex >= value.Length)
                ThrowStartIndexArgumentOutOfRange();
            if (startIndex > value.Length - 2)
                ThrowValueArgumentTooSmall();
            Contract.EndContractBlock();

            return (ushort)ToInt16(value, startIndex);
        }

        // Converts an array of bytes into an uint.
        // 
        [CLSCompliant(false)]
        public static uint ToUInt32(byte[] value, int startIndex)
        {
            if (value == null)
                ThrowValueArgumentNull();
            if ((uint)startIndex >= value.Length)
                ThrowStartIndexArgumentOutOfRange();
            if (startIndex > value.Length - 4)
                ThrowValueArgumentTooSmall();
            Contract.EndContractBlock();

            return (uint)ToInt32(value, startIndex);
        }

        // Converts an array of bytes into an unsigned long.
        // 
        [CLSCompliant(false)]
        public static ulong ToUInt64(byte[] value, int startIndex)
        {
            if (value == null)
                ThrowValueArgumentNull();
            if ((uint)startIndex >= value.Length)
                ThrowStartIndexArgumentOutOfRange();
            if (startIndex > value.Length - 8)
                ThrowValueArgumentTooSmall();
            Contract.EndContractBlock();

            return (ulong)ToInt64(value, startIndex);
        }

        // Converts an array of bytes into a float.  
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static float ToSingle(byte[] value, int startIndex)
        {
            if (value == null)
                ThrowValueArgumentNull();
            if ((uint)startIndex >= value.Length)
                ThrowStartIndexArgumentOutOfRange();
            if (startIndex > value.Length - 4)
                ThrowValueArgumentTooSmall();
            Contract.EndContractBlock();

            int val = ToInt32(value, startIndex);
            return *(float*)&val;
        }

        // Converts an array of bytes into a double.  
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static double ToDouble(byte[] value, int startIndex)
        {
            if (value == null)
                ThrowValueArgumentNull();
            if ((uint)startIndex >= value.Length)
                ThrowStartIndexArgumentOutOfRange();
            if (startIndex > value.Length - 8)
                ThrowValueArgumentTooSmall();
            Contract.EndContractBlock();

            long val = ToInt64(value, startIndex);
            return *(double*)&val;
        }

        private static char GetHexValue(int i)
        {
            Debug.Assert(i >= 0 && i < 16, "i is out of range.");
            if (i < 10)
            {
                return (char)(i + '0');
            }

            return (char)(i - 10 + 'A');
        }

        // Converts an array of bytes into a String.  
        public static string ToString(byte[] value, int startIndex, int length)
        {
            if (value == null)
                ThrowValueArgumentNull();
            if (startIndex < 0 || startIndex >= value.Length && startIndex > 0)
                ThrowStartIndexArgumentOutOfRange();
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_GenericPositive);
            if (startIndex > value.Length - length)
                ThrowValueArgumentTooSmall();
            Contract.EndContractBlock();

            if (length == 0)
            {
                return string.Empty;
            }

            if (length > (int.MaxValue / 3))
            {
                // (Int32.MaxValue / 3) == 715,827,882 Bytes == 699 MB
                throw new ArgumentOutOfRangeException(nameof(length), SR.Format(SR.ArgumentOutOfRange_LengthTooLarge, (int.MaxValue / 3)));
            }

            int chArrayLength = length * 3;
            const int StackLimit = 512; // arbitrary limit to switch from stack to heap allocation
            unsafe
            {
                if (chArrayLength < StackLimit)
                {
                    char* chArrayPtr = stackalloc char[chArrayLength];
                    return ToString(value, startIndex, length, chArrayPtr, chArrayLength);
                }
                else
                {
                    fixed (char* chArrayPtr = new char[chArrayLength])
                        return ToString(value, startIndex, length, chArrayPtr, chArrayLength);
                }
            }
        }

        private static unsafe string ToString(byte[] value, int startIndex, int length, char* chArray, int chArrayLength)
        {
            Debug.Assert(length > 0);
            Debug.Assert(chArrayLength == length * 3);

            char* p = chArray;
            int endIndex = startIndex + length;
            for (int i = startIndex; i < endIndex; i++)
            {
                byte b = value[i];
                *p++ = GetHexValue(b >> 4);
                *p++ = GetHexValue(b & 0xF);
                *p++ = '-';
            }

            // We don't need the last '-' character
            return new string(chArray, 0, chArrayLength - 1);
        }

        // Converts an array of bytes into a String.  
        public static string ToString(byte[] value)
        {
            if (value == null)
                ThrowValueArgumentNull();
            Contract.Ensures(Contract.Result<string>() != null);
            Contract.EndContractBlock();
            return ToString(value, 0, value.Length);
        }

        // Converts an array of bytes into a String.  
        public static string ToString(byte[] value, int startIndex)
        {
            if (value == null)
                ThrowValueArgumentNull();
            Contract.Ensures(Contract.Result<string>() != null);
            Contract.EndContractBlock();
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
                ThrowValueArgumentNull();
            if (startIndex < 0)
                ThrowStartIndexArgumentOutOfRange();
            if (startIndex > value.Length - 1)
                ThrowStartIndexArgumentOutOfRange(); // differs from other overloads, which throw base ArgumentException
            Contract.EndContractBlock();

            return value[startIndex] != 0;
        }

        [SecuritySafeCritical]
        public static unsafe long DoubleToInt64Bits(double value)
        {
            return *((long*)&value);
        }

        [SecuritySafeCritical]
        public static unsafe double Int64BitsToDouble(long value)
        {
            return *((double*)&value);
        }

        [SecuritySafeCritical]
        public static unsafe int SingleToInt32Bits(float value)
        {
            return *((int*)&value);
        }

        [SecuritySafeCritical]
        public static unsafe float Int32BitsToSingle(int value)
        {
            return *((float*)&value);
        }

        private static void ThrowValueArgumentNull()
        {
            throw new ArgumentNullException("value");
        }

        private static void ThrowStartIndexArgumentOutOfRange()
        {
            throw new ArgumentOutOfRangeException("startIndex", SR.ArgumentOutOfRange_Index);
        }

        private static void ThrowValueArgumentTooSmall()
        {
            throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall, "value");
        }
    }
}
