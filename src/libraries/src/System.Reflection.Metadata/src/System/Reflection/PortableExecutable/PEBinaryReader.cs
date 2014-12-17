// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Reflection.PortableExecutable
{
    /// <summary>
    /// Simple BinaryReader wrapper to:
    ///
    ///  1) throw BadImageFormat instead of EndOfStream or ArgumentOutOfRange.
    ///  2) limit reads to a subset of the base stream.
    ///
    /// Only methods that are needed to read PE headers are implemented.
    /// </summary>
    internal struct PEBinaryReader
    {
        private readonly long _startOffset;
        private readonly long _maxOffset;
        private readonly BinaryReader _reader;

        public PEBinaryReader(Stream stream, int size)
        {
            Debug.Assert(size >= 0 && size <= (stream.Length - stream.Position));

            _startOffset = stream.Position;
            _maxOffset = _startOffset + (uint)size;
            _reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        }

        public int CurrentOffset
        {
            get { return (int)(_reader.BaseStream.Position - _startOffset); }
        }

        public void Seek(int offset)
        {
            CheckBounds(_startOffset, offset);
            _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        }

        public byte[] ReadBytes(int count)
        {
            CheckBounds(_reader.BaseStream.Position, count);
            return _reader.ReadBytes(count);
        }

        public Byte ReadByte()
        {
            CheckBounds(sizeof(Byte));
            return _reader.ReadByte();
        }

        public Int16 ReadInt16()
        {
            CheckBounds(sizeof(Int16));
            return _reader.ReadInt16();
        }

        public UInt16 ReadUInt16()
        {
            CheckBounds(sizeof(UInt16));
            return _reader.ReadUInt16();
        }

        public Int32 ReadInt32()
        {
            CheckBounds(sizeof(Int32));
            return _reader.ReadInt32();
        }

        public UInt32 ReadUInt32()
        {
            CheckBounds(sizeof(UInt32));
            return _reader.ReadUInt32();
        }

        public ulong ReadUInt64()
        {
            CheckBounds(sizeof(UInt64));
            return _reader.ReadUInt64();
        }

        public string ReadUTF8(int byteCount)
        {
            byte[] bytes = ReadBytes(byteCount);
            return Encoding.UTF8.GetString(bytes, 0, byteCount);
        }

        /// <summary>
        /// Resolve image size as either the given user-specified size or distance from current position to end-of-stream.
        /// Also performs the relevant argument validation and publicly visible caller has same argument names.
        /// </summary>
        /// <exception cref="ArgumentException">size is null and distance from current position to end-of-stream can't fit in Int32.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Size is negative or extends past the end-of-stream from current position.</exception>
        public static int GetAndValidateSize(Stream peStream, int? size)
        {
            long maxSize = peStream.Length - peStream.Position;

            if (size.HasValue)
            {
                if (unchecked((uint)size.Value) > maxSize)
                {
                    throw new ArgumentOutOfRangeException("size");
                }

                return size.Value;
            }
            else
            {
                if (maxSize > int.MaxValue)
                {
                    throw new ArgumentException(MetadataResources.StreamTooLarge, "peStream");
                }

                return (int)maxSize;
            }
        }

        private void CheckBounds(uint count)
        {
            Debug.Assert(count <= sizeof(Int64));  // Error message assumes we're trying to read constant small number of bytes.
            Debug.Assert(_reader.BaseStream.Position >= 0 && _maxOffset >= 0);

            // Add cannot overflow because the worst case is (ulong)long.MaxValue + uint.MaxValue < ulong.MaxValue.
            if ((ulong)_reader.BaseStream.Position + count > (ulong)_maxOffset)
            {
                ThrowImageTooSmall();
            }
        }

        private void CheckBounds(long startPosition, int count)
        {
            Debug.Assert(startPosition >= 0 && _maxOffset >= 0);

            // Add cannot overflow because the worst case is (ulong)long.MaxValue + uint.MaxValue < ulong.MaxValue.
            // Negative count is handled by overflow to greater than maximum size = int.MaxValue.
            if ((ulong)startPosition + unchecked((uint)count) > (ulong)_maxOffset)
            {
                ThrowImageTooSmallOrContainsInvalidOffsetOrCount();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowImageTooSmall()
        {
            throw new BadImageFormatException(MetadataResources.ImageTooSmall);
        }

        // TODO: move throw helpers together. 
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowImageTooSmallOrContainsInvalidOffsetOrCount()
        {
            throw new BadImageFormatException(MetadataResources.ImageTooSmallOrContainsInvalidOffsetOrCount);
        }
    }
}
