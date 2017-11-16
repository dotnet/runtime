// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    internal readonly struct PEBinaryReader
    {
        private readonly long _startOffset;
        private readonly long _maxOffset;
        private readonly BinaryReader _reader;

        public PEBinaryReader(Stream stream, int size)
        {
            Debug.Assert(size >= 0 && size <= (stream.Length - stream.Position));

            _startOffset = stream.Position;
            _maxOffset = _startOffset + size;
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

        public ushort ReadUInt16()
        {
            CheckBounds(sizeof(ushort));
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

        /// <summary>
        /// Reads a fixed-length byte block as a null-padded UTF8-encoded string.
        /// The padding is not included in the returned string.
        /// 
        /// Note that it is legal for UTF8 strings to contain NUL; if NUL occurs
        /// between non-NUL codepoints, it is not considered to be padding and
        /// is included in the result.
        /// </summary>
        public string ReadNullPaddedUTF8(int byteCount)
        {
            byte[] bytes = ReadBytes(byteCount);
            int nonPaddedLength = 0;
            for (int i = bytes.Length; i > 0; --i)
            {
                if (bytes[i - 1] != 0)
                {
                    nonPaddedLength = i;
                    break;
                }
            }
            return Encoding.UTF8.GetString(bytes, 0, nonPaddedLength);
        }

        private void CheckBounds(uint count)
        {
            Debug.Assert(count <= sizeof(Int64));  // Error message assumes we're trying to read constant small number of bytes.
            Debug.Assert(_reader.BaseStream.Position >= 0 && _maxOffset >= 0);

            // Add cannot overflow because the worst case is (ulong)long.MaxValue + uint.MaxValue < ulong.MaxValue.
            if ((ulong)_reader.BaseStream.Position + count > (ulong)_maxOffset)
            {
                Throw.ImageTooSmall();
            }
        }

        private void CheckBounds(long startPosition, int count)
        {
            Debug.Assert(startPosition >= 0 && _maxOffset >= 0);

            // Add cannot overflow because the worst case is (ulong)long.MaxValue + uint.MaxValue < ulong.MaxValue.
            // Negative count is handled by overflow to greater than maximum size = int.MaxValue.
            if ((ulong)startPosition + unchecked((uint)count) > (ulong)_maxOffset)
            {
                Throw.ImageTooSmallOrContainsInvalidOffsetOrCount();
            }
        }
    }
}
