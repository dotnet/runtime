// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection.Internal;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public unsafe struct BlobReader
    {
        /// <summary>An array containing the '\0' character.</summary>
        private static readonly char[] s_nullCharArray = new char[1] { '\0' };

        internal const int InvalidCompressedInteger = Int32.MaxValue;

        private readonly MemoryBlock _block;

        // Points right behind the last byte of the block.
        private readonly byte* _endPointer;

        private byte* _currentPointer;

        public unsafe BlobReader(byte* buffer, int length)
            : this(MemoryBlock.CreateChecked(buffer, length))
        {

        }

        internal BlobReader(MemoryBlock block)
        {
            Debug.Assert(BitConverter.IsLittleEndian && block.Length >= 0 && (block.Pointer != null || block.Length == 0));
            _block = block;
            _currentPointer = block.Pointer;
            _endPointer = block.Pointer + block.Length;
        }

        internal string GetDebuggerDisplay()
        {
            if (_block.Pointer == null)
            {
                return "<null>";
            }

            int displayedBytes;
            string display = _block.GetDebuggerDisplay(out displayedBytes);
            if (this.Offset < displayedBytes)
            {
                display = display.Insert(this.Offset * 3, "*");
            }
            else if (displayedBytes == _block.Length)
            {
                display += "*";
            }
            else
            {
                display += "*...";
            }

            return display;
        }

        #region Offset, Skipping, Marking, Alignment, Bounds Checking

        public int Length
        {
            get
            {
                return _block.Length;
            }
        }

        public int Offset
        {
            get
            {
                return (int)(_currentPointer - _block.Pointer);
            }
        }

        public int RemainingBytes
        {
            get
            {
                return (int)(_endPointer - _currentPointer);
            }
        }

        public void Reset()
        {
            _currentPointer = _block.Pointer;
        }

        internal bool SeekOffset(int offset)
        {
            if (unchecked((uint)offset) >= (uint)_block.Length)
            {
                return false;
            }

            _currentPointer = _block.Pointer + offset;
            return true;
        }

        internal void SkipBytes(int count)
        {
            GetCurrentPointerAndAdvance(count);
        }

        internal void Align(byte alignment)
        {
            if (!TryAlign(alignment))
            {
                Throw.OutOfBounds();
            }
        }

        internal bool TryAlign(byte alignment)
        {
            int remainder = this.Offset & (alignment - 1);

            Debug.Assert((alignment & (alignment - 1)) == 0, "Alignment must be a power of two.");
            Debug.Assert(remainder >= 0 && remainder < alignment);

            if (remainder != 0)
            {
                int bytesToSkip = alignment - remainder;
                if (bytesToSkip > RemainingBytes)
                {
                    return false;
                }

                _currentPointer += bytesToSkip;
            }

            return true;
        }

        internal MemoryBlock GetMemoryBlockAt(int offset, int length)
        {
            CheckBounds(offset, length);
            return new MemoryBlock(_currentPointer + offset, length);
        }

        #endregion

        #region Bounds Checking

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckBounds(int offset, int byteCount)
        {
            if (unchecked((ulong)(uint)offset + (uint)byteCount) > (ulong)(_endPointer - _currentPointer))
            {
                Throw.OutOfBounds();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckBounds(int byteCount)
        {
            if (unchecked((uint)byteCount) > (_endPointer - _currentPointer))
            {
                Throw.OutOfBounds();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte* GetCurrentPointerAndAdvance(int length)
        {
            byte* p = _currentPointer;

            if (unchecked((uint)length) > (uint)(_endPointer - p))
            {
                Throw.OutOfBounds();
            }

            _currentPointer = p + length;
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte* GetCurrentPointerAndAdvance1()
        {
            byte* p = _currentPointer;

            if (p == _endPointer)
            {
                Throw.OutOfBounds();
            }

            _currentPointer = p + 1;
            return p;
        }

        #endregion

        #region Read Methods

        public bool ReadBoolean()
        {
            // It's not clear from the ECMA spec what exactly is the encoding of Boolean. 
            // Some metadata writers encode "true" as 0xff, others as 1. So we treat all non-zero values as "true".
            //
            // We propose to clarify and relax the current wording in the spec as follows:
            //
            // Chapter II.16.2 "Field init metadata"
            //   ... bool '(' true | false ')' Boolean value stored in a single byte, 0 represents false, any non-zero value represents true ...
            // 
            // Chapter 23.3 "Custom attributes"
            //   ... A bool is a single byte with value 0 reprseenting false and any non-zero value representing true ...
            return ReadByte() != 0;
        }

        public sbyte ReadSByte()
        {
            return *(sbyte*)GetCurrentPointerAndAdvance1();
        }

        public byte ReadByte()
        {
            return *(byte*)GetCurrentPointerAndAdvance1();
        }

        public char ReadChar()
        {
            return *(char*)GetCurrentPointerAndAdvance(sizeof(char));
        }

        public short ReadInt16()
        {
            return *(short*)GetCurrentPointerAndAdvance(sizeof(short));
        }

        public ushort ReadUInt16()
        {
            return *(ushort*)GetCurrentPointerAndAdvance(sizeof(ushort));
        }

        public int ReadInt32()
        {
            return *(int*)GetCurrentPointerAndAdvance(sizeof(int));
        }

        public uint ReadUInt32()
        {
            return *(uint*)GetCurrentPointerAndAdvance(sizeof(uint));
        }

        public long ReadInt64()
        {
            return *(long*)GetCurrentPointerAndAdvance(sizeof(long));
        }

        public ulong ReadUInt64()
        {
            return *(ulong*)GetCurrentPointerAndAdvance(sizeof(ulong));
        }

        public float ReadSingle()
        {
            return *(float*)GetCurrentPointerAndAdvance(sizeof(float));
        }

        public double ReadDouble()
        {
            return *(double*)GetCurrentPointerAndAdvance(sizeof(double));
        }

        public Guid ReadGuid()
        {
            const int size = 16;
            return *(Guid*)GetCurrentPointerAndAdvance(size);
        }

        /// <summary>
        /// Reads <see cref="decimal"/> number.
        /// </summary>
        /// <remarks>
        /// Decimal number is encoded in 13 bytes as follows:
        /// - byte 0: highest bit indicates sign (1 for negative, 0 for non-negative); the remaining 7 bits encode scale
        /// - bytes 1..12: 96-bit unsigned integer in little endian encoding.
        /// </remarks>
        /// <exception cref="BadImageFormatException">The data at the current position was not a valid <see cref="decimal"/> number.</exception>
        public decimal ReadDecimal()
        {
            byte* ptr = GetCurrentPointerAndAdvance(13);
            
            byte scale = (byte)(*ptr & 0x7f);
            if (scale > 28)
            {
                throw new BadImageFormatException(SR.ValueTooLarge);
            }

            return new decimal(
                *(int*)(ptr + 1),
                *(int*)(ptr + 5),
                *(int*)(ptr + 9),
                isNegative: (*ptr & 0x80) != 0,
                scale: scale);
        }

        public DateTime ReadDateTime()
        {
            return new DateTime(ReadInt64());
        }

        public SignatureHeader ReadSignatureHeader()
        {
            return new SignatureHeader(ReadByte());
        }

        /// <summary>
        /// Reads UTF8 encoded string starting at the current position. 
        /// </summary>
        /// <param name="byteCount">The number of bytes to read.</param>
        /// <returns>The string.</returns>
        /// <exception cref="BadImageFormatException"><paramref name="byteCount"/> bytes not available.</exception>
        public string ReadUTF8(int byteCount)
        {
            string s = _block.PeekUtf8(this.Offset, byteCount);
            _currentPointer += byteCount;
            return s;
        }

        /// <summary>
        /// Reads UTF16 (little-endian) encoded string starting at the current position. 
        /// </summary>
        /// <param name="byteCount">The number of bytes to read.</param>
        /// <returns>The string.</returns>
        /// <exception cref="BadImageFormatException"><paramref name="byteCount"/> bytes not available.</exception>
        public string ReadUTF16(int byteCount)
        {
            string s = _block.PeekUtf16(this.Offset, byteCount);
            _currentPointer += byteCount;
            return s;
        }

        /// <summary>
        /// Reads bytes starting at the current position. 
        /// </summary>
        /// <param name="byteCount">The number of bytes to read.</param>
        /// <returns>The byte array.</returns>
        /// <exception cref="BadImageFormatException"><paramref name="byteCount"/> bytes not available.</exception>
        public byte[] ReadBytes(int byteCount)
        {
            byte[] bytes = _block.PeekBytes(this.Offset, byteCount);
            _currentPointer += byteCount;
            return bytes;
        }

        internal string ReadUtf8NullTerminated()
        {
            int bytesRead;
            string value = _block.PeekUtf8NullTerminated(this.Offset, null, MetadataStringDecoder.DefaultUTF8, out bytesRead, '\0');
            _currentPointer += bytesRead;
            return value;
        }

        private int ReadCompressedIntegerOrInvalid()
        {
            int bytesRead;
            int value = _block.PeekCompressedInteger(this.Offset, out bytesRead);
            _currentPointer += bytesRead;
            return value;
        }

        /// <summary>
        /// Reads an unsigned compressed integer value. 
        /// See Metadata Specification section II.23.2: Blobs and signatures.
        /// </summary>
        /// <param name="value">The value of the compressed integer that was read.</param>
        /// <returns>true if the value was read successfully. false if the data at the current position was not a valid compressed integer.</returns>
        public bool TryReadCompressedInteger(out int value)
        {
            value = ReadCompressedIntegerOrInvalid();
            return value != InvalidCompressedInteger;
        }

        /// <summary>
        /// Reads an unsigned compressed integer value. 
        /// See Metadata Specification section II.23.2: Blobs and signatures.
        /// </summary>
        /// <returns>The value of the compressed integer that was read.</returns>
        /// <exception cref="BadImageFormatException">The data at the current position was not a valid compressed integer.</exception>
        public int ReadCompressedInteger()
        {
            int value;
            if (!TryReadCompressedInteger(out value))
            {
                Throw.InvalidCompressedInteger();
            }
            return value;
        }

        /// <summary>
        /// Reads a signed compressed integer value. 
        /// See Metadata Specification section II.23.2: Blobs and signatures.
        /// </summary>
        /// <param name="value">The value of the compressed integer that was read.</param>
        /// <returns>true if the value was read successfully. false if the data at the current position was not a valid compressed integer.</returns>
        public bool TryReadCompressedSignedInteger(out int value)
        {
            int bytesRead;
            value = _block.PeekCompressedInteger(this.Offset, out bytesRead);

            if (value == InvalidCompressedInteger)
            {
                return false;
            }

            bool signExtend = (value & 0x1) != 0;
            value >>= 1;

            if (signExtend)
            {
                switch (bytesRead)
                {
                    case 1:
                        value |= unchecked((int)0xffffffc0);
                        break;
                    case 2:
                        value |= unchecked((int)0xffffe000);
                        break;
                    default:
                        Debug.Assert(bytesRead == 4);
                        value |= unchecked((int)0xf0000000);
                        break;
                }
            }

            _currentPointer += bytesRead;
            return true;
        }

        /// <summary>
        /// Reads a signed compressed integer value. 
        /// See Metadata Specification section II.23.2: Blobs and signatures.
        /// </summary>
        /// <returns>The value of the compressed integer that was read.</returns>
        /// <exception cref="BadImageFormatException">The data at the current position was not a valid compressed integer.</exception>
        public int ReadCompressedSignedInteger()
        {
            int value;
            if (!TryReadCompressedSignedInteger(out value))
            {
                Throw.InvalidCompressedInteger();
            }
            return value;
        }

        /// <summary>
        /// Reads type code encoded in a serialized custom attribute value. 
        /// </summary>
        /// <returns><see cref="SerializationTypeCode.Invalid"/> if the encoding is invalid.</returns>
        public SerializationTypeCode ReadSerializationTypeCode()
        {
            int value = ReadCompressedIntegerOrInvalid();
            if (value > byte.MaxValue)
            {
                return SerializationTypeCode.Invalid;
            }

            return unchecked((SerializationTypeCode)value);
        }

        /// <summary>
        /// Reads type code encoded in a signature. 
        /// </summary>
        /// <returns><see cref="SignatureTypeCode.Invalid"/> if the encoding is invalid.</returns>
        public SignatureTypeCode ReadSignatureTypeCode()
        {
            int value = ReadCompressedIntegerOrInvalid();

            switch (value)
            {
                case (int)CorElementType.ELEMENT_TYPE_CLASS:
                case (int)CorElementType.ELEMENT_TYPE_VALUETYPE:
                    return SignatureTypeCode.TypeHandle;

                default:
                    if (value > byte.MaxValue)
                    {
                        return SignatureTypeCode.Invalid;
                    }

                    return unchecked((SignatureTypeCode)value);
            }
        }

        /// <summary>
        /// Reads a string encoded as a compressed integer containing its length followed by
        /// its contents in UTF8. Null strings are encoded as a single 0xFF byte.
        /// </summary>
        /// <remarks>Defined as a 'SerString' in the Ecma CLI specification.</remarks>
        /// <returns>String value or null.</returns>
        /// <exception cref="BadImageFormatException">If the encoding is invalid.</exception>
        public string ReadSerializedString()
        {
            int length;
            if (TryReadCompressedInteger(out length))
            {
                // Removal of trailing '\0' is a departure from the spec, but required
                // for compatibility with legacy compilers.
                return ReadUTF8(length).TrimEnd(s_nullCharArray);
            }

            if (ReadByte() != 0xFF)
            {
                Throw.InvalidSerializedString();
            }

            return null;
        }

        /// <summary>
        /// Reads a type handle encoded in a signature as TypeDefOrRefOrSpecEncoded (see ECMA-335 II.23.2.8).
        /// </summary>
        /// <returns>The handle or nil if the encoding is invalid.</returns>
        public EntityHandle ReadTypeHandle()
        {
            uint value = (uint)ReadCompressedIntegerOrInvalid();
            uint tokenType = s_corEncodeTokenArray[value & 0x3];

            if (value == InvalidCompressedInteger || tokenType == 0)
            {
                return default(EntityHandle);
            }

            return new EntityHandle(tokenType | (value >> 2));
        }

        private static readonly uint[] s_corEncodeTokenArray = new uint[] { TokenTypeIds.TypeDef, TokenTypeIds.TypeRef, TokenTypeIds.TypeSpec, 0 };

        /// <summary>
        /// Reads a #Blob heap handle encoded as a compressed integer.
        /// </summary>
        /// <remarks>
        /// Blobs that contain references to other blobs are used in Portable PDB format, for example <see cref="Document.Name"/>.
        /// </remarks>
        public BlobHandle ReadBlobHandle()
        {
            return BlobHandle.FromOffset(ReadCompressedInteger());
        }

        /// <summary>
        /// Reads a constant value (see ECMA-335 Partition II section 22.9) from the current position.
        /// </summary>
        /// <exception cref="BadImageFormatException">Error while reading from the blob.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="typeCode"/> is not a valid <see cref="ConstantTypeCode"/>.</exception>
        /// <returns>
        /// Boxed constant value. To avoid allocating the object use Read* methods directly.
        /// Constants of type <see cref="ConstantTypeCode.String"/> are encoded as UTF16 strings, use <see cref="ReadUTF16(int)"/> to read them.
        /// </returns>
        public object ReadConstant(ConstantTypeCode typeCode)
        {
            // Partition II section 22.9:
            //
            // Type shall be exactly one of: ELEMENT_TYPE_BOOLEAN, ELEMENT_TYPE_CHAR, ELEMENT_TYPE_I1, 
            // ELEMENT_TYPE_U1, ELEMENT_TYPE_I2, ELEMENT_TYPE_U2, ELEMENT_TYPE_I4, ELEMENT_TYPE_U4, 
            // ELEMENT_TYPE_I8, ELEMENT_TYPE_U8, ELEMENT_TYPE_R4, ELEMENT_TYPE_R8, or ELEMENT_TYPE_STRING; 
            // or ELEMENT_TYPE_CLASS with a Value of zero  (23.1.16)

            switch (typeCode)
            {
                case ConstantTypeCode.Boolean:
                    return ReadBoolean();

                case ConstantTypeCode.Char:
                    return ReadChar();

                case ConstantTypeCode.SByte:
                    return ReadSByte();

                case ConstantTypeCode.Int16:
                    return ReadInt16();

                case ConstantTypeCode.Int32:
                    return ReadInt32();

                case ConstantTypeCode.Int64:
                    return ReadInt64();

                case ConstantTypeCode.Byte:
                    return ReadByte();

                case ConstantTypeCode.UInt16:
                    return ReadUInt16();

                case ConstantTypeCode.UInt32:
                    return ReadUInt32();

                case ConstantTypeCode.UInt64:
                    return ReadUInt64();

                case ConstantTypeCode.Single:
                    return ReadSingle();

                case ConstantTypeCode.Double:
                    return ReadDouble();

                case ConstantTypeCode.String:
                    return ReadUTF16(RemainingBytes);

                case ConstantTypeCode.NullReference:
                    // Partition II section 22.9:
                    // The encoding of Type for the nullref value is ELEMENT_TYPE_CLASS with a Value of a 4-byte zero.
                    // Unlike uses of ELEMENT_TYPE_CLASS in signatures, this one is not followed by a type token.
                    if (ReadUInt32() != 0)
                    {
                        throw new BadImageFormatException(SR.InvalidConstantValue);
                    }

                    return null;

                default:
                    throw new ArgumentOutOfRangeException(nameof(typeCode));
            }
        }

        #endregion
    }
}
