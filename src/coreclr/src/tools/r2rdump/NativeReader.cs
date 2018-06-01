// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace R2RDump
{
    class NativeReader
    {
        /// <summary>
        /// Extracts a 64bit value from the image byte array
        /// </summary>
        /// <param name="image">PE image</param>
        /// <param name="start">Starting index of the value</param>
        /// <remarks>
        /// The <paramref name="start"/> gets incremented by the size of the value
        /// </remarks>
        public static long ReadInt64(byte[] image, ref int start)
        {
            int size = sizeof(long);
            byte[] bytes = new byte[size];
            Array.Copy(image, start, bytes, 0, size);
            start += size;
            return BitConverter.ToInt64(bytes, 0);
        }

        // <summary>
        /// Extracts a 32bit value from the image byte array
        /// </summary>
        /// <param name="image">PE image</param>
        /// <param name="start">Starting index of the value</param>
        /// <remarks>
        /// The <paramref name="start"/> gets incremented by the size of the value
        /// </remarks>
        public static int ReadInt32(byte[] image, ref int start)
        {
            int size = sizeof(int);
            byte[] bytes = new byte[size];
            Array.Copy(image, start, bytes, 0, size);
            start += size;
            return BitConverter.ToInt32(bytes, 0);
        }

        // <summary>
        /// Extracts an unsigned 32bit value from the image byte array
        /// </summary>
        /// <param name="image">PE image</param>
        /// <param name="start">Starting index of the value</param>
        /// <remarks>
        /// The <paramref name="start"/> gets incremented by the size of the value
        /// </remarks>
        public static uint ReadUInt32(byte[] image, ref int start)
        {
            int size = sizeof(int);
            byte[] bytes = new byte[size];
            Array.Copy(image, start, bytes, 0, size);
            start += size;
            return (uint)BitConverter.ToInt32(bytes, 0);
        }

        // <summary>
        /// Extracts an unsigned 16bit value from the image byte array
        /// </summary>
        /// <param name="image">PE image</param>
        /// <param name="start">Starting index of the value</param>
        /// <remarks>
        /// The <paramref name="start"/> gets incremented by the size of the value
        /// </remarks>
        public static ushort ReadUInt16(byte[] image, ref int start)
        {
            int size = sizeof(short);
            byte[] bytes = new byte[size];
            Array.Copy(image, start, bytes, 0, size);
            start += size;
            return (ushort)BitConverter.ToInt16(bytes, 0);
        }

        // <summary>
        /// Extracts byte from the image byte array
        /// </summary>
        /// <param name="image">PE image</param>
        /// <param name="start">Start index of the value</param>
        /// /// <remarks>
        /// The <paramref name="start"/> gets incremented by the size of the value
        /// </remarks>
        public static byte ReadByte(byte[] image, ref int start)
        {
            byte val = image[start];
            start += sizeof(byte);
            return val;
        }

        public static uint DecodeUnsigned(byte[] image, uint offset, ref uint pValue)
        {
            if (offset >= image.Length)
                throw new System.BadImageFormatException("offset out of bounds");

            int off = (int)offset;
            uint val = ReadByte(image, ref off);

            if ((val & 1) == 0)
            {
                pValue = (val >> 1);
                offset += 1;
            }
            else if ((val & 2) == 0)
            {
                if (offset + 1 >= image.Length)
                    throw new System.BadImageFormatException("offset out of bounds");

                pValue = (val >> 2) |
                      ((uint)ReadByte(image, ref off) << 6);
                offset += 2;
            }
            else if ((val & 4) == 0)
            {
                if (offset + 2 >= image.Length)
                    throw new System.BadImageFormatException("offset out of bounds");

                pValue = (val >> 3) |
                      ((uint)ReadByte(image, ref off) << 5) |
                      ((uint)ReadByte(image, ref off) << 13);
                offset += 3;
            }
            else if ((val & 8) == 0)
            {
                if (offset + 3 >= image.Length)
                    throw new System.BadImageFormatException("offset out of bounds");

                pValue = (val >> 4) |
                      ((uint)ReadByte(image, ref off) << 4) |
                      ((uint)ReadByte(image, ref off) << 12) |
                      ((uint)ReadByte(image, ref off) << 20);
                offset += 4;
            }
            else if ((val & 16) == 0)
            {
                pValue = ReadUInt32(image, ref off);
                offset += 5;
            }
            else
            {
                throw new System.BadImageFormatException("DecodeUnsigned");
            }

            return offset;
        }

        public static uint DecodeSigned(byte[] image, uint offset, ref int pValue)
        {
            if (offset >= image.Length)
                throw new System.BadImageFormatException("offset out of bounds");

            int off = (int)offset;
            int val = ReadByte(image, ref off);

            if ((val & 1) == 0)
            {
                pValue = (val >> 1);
                offset += 1;
            }
            else if ((val & 2) == 0)
            {
                if (offset + 1 >= image.Length)
                    throw new System.BadImageFormatException("offset out of bounds");

                pValue = (val >> 2) |
                      (ReadByte(image, ref off) << 6);
                offset += 2;
            }
            else if ((val & 4) == 0)
            {
                if (offset + 2 >= image.Length)
                    throw new System.BadImageFormatException("offset out of bounds");

                pValue = (val >> 3) |
                      (ReadByte(image, ref off) << 5) |
                      (ReadByte(image, ref off) << 13);
                offset += 3;
            }
            else if ((val & 8) == 0)
            {
                if (offset + 3 >= image.Length)
                    throw new System.BadImageFormatException("offset out of bounds");

                pValue = (val >> 4) |
                      (ReadByte(image, ref off) << 4) |
                      (ReadByte(image, ref off) << 12) |
                      (ReadByte(image, ref off) << 20);
                offset += 4;
            }
            else if ((val & 16) == 0)
            {
                pValue = ReadInt32(image, ref off);
                offset += 5;
            }
            else
            {
                throw new System.BadImageFormatException("DecodeSigned");
            }

            return offset;
        }

        public static uint ReadCompressedData(byte[] image, ref int start)
        {
            int off = start;
            uint data = ReadUInt32(image, ref off);
            if ((data & 0x80) == 0x00)
            {
                start++;
                return (byte)data;
            }
            if ((data & 0xC0) == 0x80)  // 10?? ????
            {
                data = (uint)((ReadByte(image, ref start) & 0x3f) << 8);
                data |= ReadByte(image, ref start);
            }
            else // 110? ????
            {
                data = (uint)(ReadByte(image, ref start) & 0x1f) << 24;
                data |= (uint)ReadByte(image, ref start) << 16;
                data |= (uint)ReadByte(image, ref start) << 8;
                data |= ReadByte(image, ref start);
            }
            return data;
        }
    }
}
