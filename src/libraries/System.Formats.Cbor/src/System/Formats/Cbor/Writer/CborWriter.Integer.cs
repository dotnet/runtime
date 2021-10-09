// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;

namespace System.Formats.Cbor
{
    public partial class CborWriter
    {
        // Implements major type 0,1 encoding per https://tools.ietf.org/html/rfc7049#section-2.1

        /// <summary>Writes a value as a signed integer encoding (major types 0,1)</summary>
        /// <param name="value">The value to write</param>
        /// <exception cref="InvalidOperationException">Writing a new value exceeds the definite length of the parent data item.
        /// -or-
        /// The major type of the encoded value is not permitted in the parent data item.
        /// -or-
        /// The written data is not accepted under the current conformance mode.</exception>
        public void WriteInt32(int value) => WriteInt64(value);

        /// <summary>Writes the provided value as a signed integer encoding (major types 0,1)</summary>
        /// <param name="value">The value to write</param>
        /// <exception cref="InvalidOperationException">Writing a new value exceeds the definite length of the parent data item.
        /// -or-
        /// The major type of the encoded value is not permitted in the parent data item.
        /// -or-
        /// The written data is not accepted under the current conformance mode.</exception>
        public void WriteInt64(long value)
        {
            if (value < 0)
            {
                ulong unsignedRepresentation = (value == long.MinValue) ? (ulong)long.MaxValue : (ulong)(-value) - 1;
                WriteUnsignedInteger(CborMajorType.NegativeInteger, unsignedRepresentation);
            }
            else
            {
                WriteUnsignedInteger(CborMajorType.UnsignedInteger, (ulong)value);
            }

            AdvanceDataItemCounters();
        }

        /// <summary>Writes a value as an unsigned integer encoding (major type 0).</summary>
        /// <param name="value">The value to write</param>
        /// <exception cref="InvalidOperationException">Writing a new value exceeds the definite length of the parent data item.
        /// -or-
        /// The major type of the encoded value is not permitted in the parent data item.
        /// -or-
        /// The written data is not accepted under the current conformance mode.</exception>
        [CLSCompliant(false)]
        public void WriteUInt32(uint value) => WriteUInt64(value);

        /// <summary>Writes a value as an unsigned integer encoding (major type 0).</summary>
        /// <param name="value">The value to write</param>
        /// <exception cref="InvalidOperationException">Writing a new value exceeds the definite length of the parent data item.
        /// -or-
        /// The major type of the encoded value is not permitted in the parent data item.
        /// -or-
        /// The written data is not accepted under the current conformance mode.</exception>
        [CLSCompliant(false)]
        public void WriteUInt64(ulong value)
        {
            WriteUnsignedInteger(CborMajorType.UnsignedInteger, value);
            AdvanceDataItemCounters();
        }

        /// <summary>Writes the provided value as a CBOR negative integer representation (major type 1).</summary>
        /// <param name="value">An unsigned integer denoting -1 minus the integer.</param>
        /// <exception cref="InvalidOperationException">Writing a new value exceeds the definite length of the parent data item.
        /// -or-
        /// The major type of the encoded value is not permitted in the parent data item.
        /// -or-
        /// The written data is not accepted under the current conformance mode.</exception>
        /// <remarks>
        /// This method supports encoding integers between -18446744073709551616 and -1.
        /// Useful for handling values that do not fit in the <see cref="long" /> type.
        /// </remarks>
        [CLSCompliant(false)]
        public void WriteCborNegativeIntegerRepresentation(ulong value)
        {
            WriteUnsignedInteger(CborMajorType.NegativeInteger, value);
            AdvanceDataItemCounters();
        }

        private void WriteUnsignedInteger(CborMajorType type, ulong value)
        {
            if (value < (byte)CborAdditionalInfo.Additional8BitData)
            {
                EnsureWriteCapacity(1);
                WriteInitialByte(new CborInitialByte(type, (CborAdditionalInfo)value));
            }
            else if (value <= byte.MaxValue)
            {
                EnsureWriteCapacity(1 + sizeof(byte));
                WriteInitialByte(new CborInitialByte(type, CborAdditionalInfo.Additional8BitData));
                _buffer[_offset++] = (byte)value;
            }
            else if (value <= ushort.MaxValue)
            {
                EnsureWriteCapacity(1 + sizeof(ushort));
                WriteInitialByte(new CborInitialByte(type, CborAdditionalInfo.Additional16BitData));
                BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(_offset), (ushort)value);
                _offset += sizeof(ushort);
            }
            else if (value <= uint.MaxValue)
            {
                EnsureWriteCapacity(1 + sizeof(uint));
                WriteInitialByte(new CborInitialByte(type, CborAdditionalInfo.Additional32BitData));
                BinaryPrimitives.WriteUInt32BigEndian(_buffer.AsSpan(_offset), (uint)value);
                _offset += sizeof(uint);
            }
            else
            {
                EnsureWriteCapacity(1 + sizeof(ulong));
                WriteInitialByte(new CborInitialByte(type, CborAdditionalInfo.Additional64BitData));
                BinaryPrimitives.WriteUInt64BigEndian(_buffer.AsSpan(_offset), value);
                _offset += sizeof(ulong);
            }
        }

        private int GetIntegerEncodingLength(ulong value)
        {
            if (value < (byte)CborAdditionalInfo.Additional8BitData)
            {
                return 1;
            }
            else if (value <= byte.MaxValue)
            {
                return 1 + sizeof(byte);
            }
            else if (value <= ushort.MaxValue)
            {
                return 1 + sizeof(ushort);
            }
            else if (value <= uint.MaxValue)
            {
                return 1 + sizeof(uint);
            }
            else
            {
                return 1 + sizeof(ulong);
            }
        }
    }
}
