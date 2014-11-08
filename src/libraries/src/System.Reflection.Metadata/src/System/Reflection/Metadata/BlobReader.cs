// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Internal;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Reflection.Metadata
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public unsafe struct BlobReader
    {
        internal const int InvalidCompressedInteger = Int32.MaxValue;

        private readonly MemoryBlock block;

        // Points right behind the last byte of the block.
        private readonly byte* endPointer;

        private byte* currentPointer;

        public unsafe BlobReader(byte* buffer, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length");
            }

            if (buffer == null && length != 0)
            {
                throw new ArgumentNullException("buffer");
            }

            // the reader performs little-endian specific operations
            if (!BitConverter.IsLittleEndian)
            {
                throw new PlatformNotSupportedException(MetadataResources.LitteEndianArchitectureRequired);
            }

            this = new BlobReader(new MemoryBlock(buffer, length));
        }

        internal BlobReader(MemoryBlock block)
        {
            Debug.Assert(BitConverter.IsLittleEndian && block.Length >= 0 && (block.Pointer != null || block.Length == 0));
            this.block = block;
            this.currentPointer = block.Pointer;
            this.endPointer = block.Pointer + block.Length;
        }

        private string GetDebuggerDisplay()
        {
            if (block.Pointer == null)
            {
                return "<null>";
            }

            int displayedBytes;
            string display = block.GetDebuggerDisplay(out displayedBytes);
            if (this.Offset < displayedBytes)
            {
                display = display.Insert(this.Offset * 3, "*");
            }
            else if (displayedBytes == block.Length)
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
                return block.Length;
            }
        }

        public int Offset
        {
            get
            {
                return (int)(this.currentPointer - block.Pointer);
            }
        }

        public int RemainingBytes
        {
            get
            {
                return (int)(this.endPointer - this.currentPointer);
            }
        }

        public void Reset()
        {
            this.currentPointer = block.Pointer;
        }

        internal bool SeekOffset(int offset)
        {
            if (unchecked((uint)offset) >= (uint)block.Length)
            {
                return false;
            }

            this.currentPointer = block.Pointer + offset;
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
                ThrowOutOfBounds();
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
                this.currentPointer += bytesToSkip;
            }
            return true;
        }

        internal MemoryBlock GetMemoryBlockAt(int offset, int length)
        {
            CheckBounds(offset, length);
            return new MemoryBlock(this.currentPointer + offset, length);
        }
        #endregion

        #region Bounds Checking

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowOutOfBounds()
        {
            throw new BadImageFormatException(MetadataResources.OutOfBoundsRead);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckBounds(int offset, int byteCount)
        {
            if (unchecked((ulong)(uint)offset + (uint)byteCount) > (ulong)(this.endPointer - this.currentPointer))
            {
                ThrowOutOfBounds();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckBounds(int byteCount)
        {
            if (unchecked((uint)byteCount) > (this.endPointer - this.currentPointer))
            {
                ThrowOutOfBounds();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte* GetCurrentPointerAndAdvance(int length)
        {
            byte* p = this.currentPointer;

            if (unchecked((uint)length) > (uint)(this.endPointer - p))
            {
                ThrowOutOfBounds();
            }

            this.currentPointer = p + length;
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte* GetCurrentPointerAndAdvance1()
        {
            byte* p = this.currentPointer;

            if (p == this.endPointer)
            {
                ThrowOutOfBounds();
            }

            this.currentPointer = p + 1;
            return p;
        }

        #endregion

        #region Read Methods

        public bool ReadBoolean()
        {
            return ReadByte() == 1;
        }

        public SByte ReadSByte()
        {
            return *(SByte*)GetCurrentPointerAndAdvance1();
        }

        public Byte ReadByte()
        {
            return *(Byte*)GetCurrentPointerAndAdvance1();
        }

        public Char ReadChar()
        {
            return *(Char*)GetCurrentPointerAndAdvance(sizeof(Char));
        }

        public Int16 ReadInt16()
        {
            return *(Int16*)GetCurrentPointerAndAdvance(sizeof(Int16));
        }

        public UInt16 ReadUInt16()
        {
            return *(UInt16*)GetCurrentPointerAndAdvance(sizeof(UInt16));
        }

        public Int32 ReadInt32()
        {
            return *(Int32*)GetCurrentPointerAndAdvance(sizeof(Int32));
        }

        public UInt32 ReadUInt32()
        {
            return *(UInt32*)GetCurrentPointerAndAdvance(sizeof(UInt32));
        }

        public Int64 ReadInt64()
        {
            return *(Int64*)GetCurrentPointerAndAdvance(sizeof(Int64));
        }

        public UInt64 ReadUInt64()
        {
            return *(UInt64*)GetCurrentPointerAndAdvance(sizeof(UInt64));
        }

        public Single ReadSingle()
        {
            return *(Single*)GetCurrentPointerAndAdvance(sizeof(Single));
        }

        public Double ReadDouble()
        {
            return *(Double*)GetCurrentPointerAndAdvance(sizeof(UInt64));
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
            string s = this.block.PeekUtf8(this.Offset, byteCount);
            this.currentPointer += byteCount;
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
            string s = this.block.PeekUtf16(this.Offset, byteCount);
            this.currentPointer += byteCount;
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
            byte[] bytes = this.block.PeekBytes(this.Offset, byteCount);
            this.currentPointer += byteCount;
            return bytes;
        }

        internal string ReadUtf8NullTerminated()
        {
            int bytesRead;
            string value = this.block.PeekUtf8NullTerminated(this.Offset, null, MetadataStringDecoder.DefaultUTF8, out bytesRead, '\0');
            this.currentPointer += bytesRead;
            return value;
        }

        private int ReadCompressedIntegerOrInvalid()
        {
            int bytesRead;
            int value = this.block.PeekCompressedInteger(this.Offset, out bytesRead);
            this.currentPointer += bytesRead;
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
        /// <exception cref="System.BadImageFormatException">The data at the current position was not a valid compressed integer.</exception>
        public int ReadCompressedInteger()
        {
            int value;
            if (!TryReadCompressedInteger(out value))
            {
                ThrowInvalidCompressedInteger();
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
            value = this.block.PeekCompressedInteger(this.Offset, out bytesRead);

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

            this.currentPointer += bytesRead;
            return true;
        }

        /// <summary>
        /// Reads a signed compressed integer value. 
        /// See Metadata Specification section II.23.2: Blobs and signatures.
        /// </summary>
        /// <returns>The value of the compressed integer that was read.</returns>
        /// <exception cref="System.BadImageFormatException">The data at the current position was not a valid compressed integer.</exception>
        public int ReadCompressedSignedInteger()
        {
            int value;
            if (!TryReadCompressedSignedInteger(out value))
            {
                ThrowInvalidCompressedInteger();
            }
            return value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidCompressedInteger()
        {
            throw new BadImageFormatException(MetadataResources.InvalidCompressedInteger);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidSerializedString()
        {
            throw new BadImageFormatException(MetadataResources.InvalidSerializedString);
        }

        /// <summary>
        /// Reads type code encoded in a serialized custom attribute value. 
        /// </summary>
        public SerializationTypeCode ReadSerializationTypeCode()
        {
            int value = ReadCompressedIntegerOrInvalid();
            if (value > Byte.MaxValue)
            {
                return SerializationTypeCode.Invalid;
            }

            return unchecked((SerializationTypeCode)value);
        }

        /// <summary>
        /// Reads type code encoded in a signature. 
        /// </summary>
        public SignatureTypeCode ReadSignatureTypeCode()
        {
            int value = ReadCompressedIntegerOrInvalid();

            switch (value)
            {
                case (int)CorElementType.ELEMENT_TYPE_CLASS:
                case (int)CorElementType.ELEMENT_TYPE_VALUETYPE:
                    return SignatureTypeCode.TypeHandle;

                default:
                    if (value > Byte.MaxValue)
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
        public string ReadSerializedString()
        {
            int length;
            if (TryReadCompressedInteger(out length))
            {
                // Removal of trailing '\0' is a departure from the spec, but required
                // for compatibility with legacy compilers.
                return ReadUTF8(length).TrimEnd('\0');
            }

            if (ReadByte() != 0xFF)
            {
                ThrowInvalidSerializedString();
            }

            return null;
        }

        /// <summary>
        /// Reads a type handle encoded in a signature as (CLASS | VALUETYPE) TypeDefOrRefOrSpecEncoded. 
        /// </summary>
        /// <returns>The handle or nil if the encoding is invalid.</returns>
        public Handle ReadTypeHandle()
        {
            uint value = (uint)ReadCompressedIntegerOrInvalid();
            uint tokenType = corEncodeTokenArray[value & 0x3];

            if (value == InvalidCompressedInteger || tokenType == 0)
            {
                return default(Handle);
            }

            return new Handle(tokenType | (value >> 2));
        }

        private static readonly uint[] corEncodeTokenArray = new uint[] { TokenTypeIds.TypeDef, TokenTypeIds.TypeRef, TokenTypeIds.TypeSpec, 0 };
        #endregion
    }
}
