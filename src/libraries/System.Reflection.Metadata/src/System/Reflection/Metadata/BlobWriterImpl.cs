// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;

namespace System.Reflection.Metadata
{
    internal static class BlobWriterImpl
    {
        internal const int SingleByteCompressedIntegerMaxValue = 0x7f;
        internal const int TwoByteCompressedIntegerMaxValue = 0x3fff;
        internal const int MaxCompressedIntegerValue = 0x1fffffff;
        internal const int MinSignedCompressedIntegerValue = unchecked((int)0xF0000000);
        internal const int MaxSignedCompressedIntegerValue = 0x0FFFFFFF;

        internal static int GetCompressedIntegerSize(int value)
        {
            Debug.Assert(value <= MaxCompressedIntegerValue);

            if (value <= SingleByteCompressedIntegerMaxValue)
            {
                return 1;
            }

            if (value <= TwoByteCompressedIntegerMaxValue)
            {
                return 2;
            }

            return 4;
        }

        internal static void WriteCompressedInteger(ref BlobWriter writer, uint value)
        {
            unchecked
            {
                if (value <= SingleByteCompressedIntegerMaxValue)
                {
                    writer.WriteByte((byte)value);
                }
                else if (value <= TwoByteCompressedIntegerMaxValue)
                {
                    writer.WriteUInt16BE((ushort)(0x8000 | value));
                }
                else if (value <= MaxCompressedIntegerValue)
                {
                    writer.WriteUInt32BE(0xc0000000 | value);
                }
                else
                {
                    Throw.ValueArgumentOutOfRange();
                }
            }
        }

        internal static void WriteCompressedInteger(BlobBuilder writer, uint value)
        {
            unchecked
            {
                if (value <= SingleByteCompressedIntegerMaxValue)
                {
                    writer.WriteByte((byte)value);
                }
                else if (value <= TwoByteCompressedIntegerMaxValue)
                {
                    writer.WriteUInt16BE((ushort)(0x8000 | value));
                }
                else if (value <= MaxCompressedIntegerValue)
                {
                    writer.WriteUInt32BE(0xc0000000 | value);
                }
                else
                {
                    Throw.ValueArgumentOutOfRange();
                }
            }
        }

        internal static void WriteCompressedSignedInteger(ref BlobWriter writer, int value)
        {
            unchecked
            {
                const int b6 = (1 << 6) - 1;
                const int b13 = (1 << 13) - 1;
                const int b28 = (1 << 28) - 1;

                // 0xffffffff for negative value
                // 0x00000000 for non-negative
                int signMask = value >> 31;

                if ((value & ~b6) == (signMask & ~b6))
                {
                    int n = ((value & b6) << 1) | (signMask & 1);
                    writer.WriteByte((byte)n);
                }
                else if ((value & ~b13) == (signMask & ~b13))
                {
                    int n = ((value & b13) << 1) | (signMask & 1);
                    writer.WriteUInt16BE((ushort)(0x8000 | n));
                }
                else if ((value & ~b28) == (signMask & ~b28))
                {
                    int n = ((value & b28) << 1) | (signMask & 1);
                    writer.WriteUInt32BE(0xc0000000 | (uint)n);
                }
                else
                {
                    Throw.ValueArgumentOutOfRange();
                }
            }
        }

        internal static void WriteCompressedSignedInteger(BlobBuilder writer, int value)
        {
            unchecked
            {
                const int b6 = (1 << 6) - 1;
                const int b13 = (1 << 13) - 1;
                const int b28 = (1 << 28) - 1;

                // 0xffffffff for negative value
                // 0x00000000 for non-negative
                int signMask = value >> 31;

                if ((value & ~b6) == (signMask & ~b6))
                {
                    int n = ((value & b6) << 1) | (signMask & 1);
                    writer.WriteByte((byte)n);
                }
                else if ((value & ~b13) == (signMask & ~b13))
                {
                    int n = ((value & b13) << 1) | (signMask & 1);
                    writer.WriteUInt16BE((ushort)(0x8000 | n));
                }
                else if ((value & ~b28) == (signMask & ~b28))
                {
                    int n = ((value & b28) << 1) | (signMask & 1);
                    writer.WriteUInt32BE(0xc0000000 | (uint)n);
                }
                else
                {
                    Throw.ValueArgumentOutOfRange();
                }
            }
        }

        /// <summary>
        /// Writes a scalar (non-string) constant to a span.
        /// </summary>
        /// <param name="bytes">The span where the content will be encoded.</param>
        /// <param name="value">The constant value.</param>
        /// <returns>The number of bytes that was written.</returns>
        internal static int WriteScalarConstant(Span<byte> bytes, object? value)
        {
            if (value == null)
            {
                // The encoding of Type for the nullref value for FieldInit is ELEMENT_TYPE_CLASS with a Value of a 32-bit.
                BinaryPrimitives.WriteUInt32LittleEndian(bytes, 0);
                return sizeof(uint);
            }

            var type = value.GetType();
            if (type.IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }

            if (type == typeof(bool))
            {
                bytes[0] = (byte)((bool)value ? 1 : 0);
                return sizeof(bool);
            }
            else if (type == typeof(int))
            {
                BinaryPrimitives.WriteInt32LittleEndian(bytes, (int)value);
                return sizeof(int);
            }
            else if (type == typeof(byte))
            {
                bytes[0] = (byte)value;
                return sizeof(byte);
            }
            else if (type == typeof(char))
            {
                BinaryPrimitives.WriteUInt16LittleEndian(bytes, (char)value);
                return sizeof(char);
            }
            else if (type == typeof(double))
            {
#if NET
                BinaryPrimitives.WriteDoubleLittleEndian(bytes, (double)value);
#else
                double v = (double)value;
                unsafe
                {
                    BinaryPrimitives.WriteUInt64LittleEndian(bytes, *(ulong*)(&v));
                }
#endif
                return sizeof(double);
            }
            else if (type == typeof(short))
            {
                BinaryPrimitives.WriteInt16LittleEndian(bytes, (short)value);
                return sizeof(short);
            }
            else if (type == typeof(long))
            {
                BinaryPrimitives.WriteInt64LittleEndian(bytes, (long)value);
                return sizeof(long);
            }
            else if (type == typeof(sbyte))
            {
                bytes[0] = (byte)(sbyte)value;
                return sizeof(sbyte);
            }
            else if (type == typeof(float))
            {
#if NET
                BinaryPrimitives.WriteSingleLittleEndian(bytes, (float)value);
#else
                float v = (float)value;
                unsafe
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(bytes, *(uint*)(&v));
                }
#endif
                return sizeof(float);
            }
            else if (type == typeof(ushort))
            {
                BinaryPrimitives.WriteUInt16LittleEndian(bytes, (ushort)value);
                return sizeof(ushort);
            }
            else if (type == typeof(uint))
            {
                BinaryPrimitives.WriteUInt32LittleEndian(bytes, (uint)value);
                return sizeof(uint);
            }
            else if (type == typeof(ulong))
            {
                BinaryPrimitives.WriteUInt64LittleEndian(bytes, (ulong)value);
                return sizeof(ulong);
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.InvalidConstantValueOfType, type));
            }
        }

        internal static void WriteConstant(ref BlobWriter writer, object? value)
        {
            if (value is string s)
            {
                writer.WriteUTF16(s);
                return;
            }

            Span<byte> bytes = stackalloc byte[sizeof(ulong)];
            int written = WriteScalarConstant(bytes, value);
            writer.WriteBytes(bytes.Slice(0, written));
        }

        internal static void WriteConstant(BlobBuilder writer, object? value)
        {
            if (value is string s)
            {
                writer.WriteUTF16(s);
                return;
            }

            Span<byte> bytes = stackalloc byte[sizeof(ulong)];
            int written = WriteScalarConstant(bytes, value);
            writer.WriteBytes(bytes.Slice(0, written));
        }
    }
}
