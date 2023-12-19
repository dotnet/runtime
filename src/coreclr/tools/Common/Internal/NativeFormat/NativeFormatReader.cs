// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ---------------------------------------------------------------------------
// Native Format Reader
//
// Utilities to read native data from images, that are written by the NativeFormatWriter engine
// ---------------------------------------------------------------------------

using System;
using System.Diagnostics;

namespace Internal.NativeFormat
{
    internal unsafe partial struct NativePrimitiveDecoder
    {
        public static void ThrowBadImageFormatException()
        {
            Debug.Assert(false);
            throw new BadImageFormatException();
        }

        public static uint DecodeUnsigned(ref byte* stream, byte* streamEnd)
        {
            if (stream >= streamEnd)
                ThrowBadImageFormatException();

            uint value = 0;

            uint val = *stream;
            if ((val & 1) == 0)
            {
                value = (val >> 1);
                stream += 1;
            }
            else if ((val & 2) == 0)
            {
                if (stream + 1 >= streamEnd)
                    ThrowBadImageFormatException();
                value = (val >> 2) |
                      (((uint)*(stream + 1)) << 6);
                stream += 2;
            }
            else if ((val & 4) == 0)
            {
                if (stream + 2 >= streamEnd)
                    ThrowBadImageFormatException();
                value = (val >> 3) |
                      (((uint)*(stream + 1)) << 5) |
                      (((uint)*(stream + 2)) << 13);
                stream += 3;
            }
            else if ((val & 8) == 0)
            {
                if (stream + 3 >= streamEnd)
                    ThrowBadImageFormatException();
                value = (val >> 4) |
                      (((uint)*(stream + 1)) << 4) |
                      (((uint)*(stream + 2)) << 12) |
                      (((uint)*(stream + 3)) << 20);
                stream += 4;
            }
            else if ((val & 16) == 0)
            {
                stream += 1;
                value = ReadUInt32(ref stream);
            }
            else
            {
                ThrowBadImageFormatException();
                return 0;
            }

            return value;
        }

        public static int DecodeSigned(ref byte* stream, byte* streamEnd)
        {
            if (stream >= streamEnd)
                ThrowBadImageFormatException();

            int value = 0;

            int val = *(stream);
            if ((val & 1) == 0)
            {
                value = ((sbyte)val) >> 1;
                stream += 1;
            }
            else if ((val & 2) == 0)
            {
                if (stream + 1 >= streamEnd)
                    ThrowBadImageFormatException();
                value = (val >> 2) |
                      (((int)*(sbyte*)(stream + 1)) << 6);
                stream += 2;
            }
            else if ((val & 4) == 0)
            {
                if (stream + 2 >= streamEnd)
                    ThrowBadImageFormatException();
                value = (val >> 3) |
                      (((int)*(stream + 1)) << 5) |
                      (((int)*(sbyte*)(stream + 2)) << 13);
                stream += 3;
            }
            else if ((val & 8) == 0)
            {
                if (stream + 3 >= streamEnd)
                    ThrowBadImageFormatException();
                value = (val >> 4) |
                      (((int)*(stream + 1)) << 4) |
                      (((int)*(stream + 2)) << 12) |
                      (((int)*(sbyte*)(stream + 3)) << 20);
                stream += 4;
            }
            else if ((val & 16) == 0)
            {
                stream += 1;
                value = (int)ReadUInt32(ref stream);
            }
            else
            {
                ThrowBadImageFormatException();
                return 0;
            }

            return value;
        }

        public static ulong DecodeUnsignedLong(ref byte* stream, byte* streamEnd)
        {
            if (stream >= streamEnd)
                ThrowBadImageFormatException();

            ulong value = 0;

            byte val = *stream;
            if ((val & 31) != 31)
            {
                value = DecodeUnsigned(ref stream, streamEnd);
            }
            else if ((val & 32) == 0)
            {
                stream += 1;
                value = ReadUInt64(ref stream);
            }
            else
            {
                ThrowBadImageFormatException();
                return 0;
            }

            return value;
        }

        public static long DecodeSignedLong(ref byte* stream, byte* streamEnd)
        {
            if (stream >= streamEnd)
                ThrowBadImageFormatException();

            long value = 0;

            byte val = *stream;
            if ((val & 31) != 31)
            {
                value = DecodeSigned(ref stream, streamEnd);
            }
            else if ((val & 32) == 0)
            {
                stream += 1;
                value = (long)ReadUInt64(ref stream);
            }
            else
            {
                ThrowBadImageFormatException();
                return 0;
            }

            return value;
        }

        public static void SkipInteger(ref byte* stream)
        {
            byte val = *stream;
            if ((val & 1) == 0)
            {
                stream += 1;
            }
            else if ((val & 2) == 0)
            {
                stream += 2;
            }
            else if ((val & 4) == 0)
            {
                stream += 3;
            }
            else if ((val & 8) == 0)
            {
                stream += 4;
            }
            else if ((val & 16) == 0)
            {
                stream += 5;
            }
            else if ((val & 32) == 0)
            {
                stream += 9;
            }
            else
            {
                ThrowBadImageFormatException();
            }
        }
    }

    internal sealed unsafe partial class NativeReader
    {
        private readonly byte* _base;
        private readonly uint _size;

        public NativeReader(byte* base_, uint size)
        {
            // Limit the maximum blob size to prevent buffer overruns triggered by boundary integer overflows
            if (size >= uint.MaxValue / 4)
                ThrowBadImageFormatException();

            Debug.Assert(base_ <= base_ + size);

            _base = base_;
            _size = size;
        }

        public uint Size
        {
            get
            {
                return _size;
            }
        }

        public uint AddressToOffset(IntPtr address)
        {
            Debug.Assert((byte*)address >= _base);
            Debug.Assert((byte*)address <= _base + _size);
            return (uint)((byte*)address - _base);
        }

        public IntPtr OffsetToAddress(uint offset)
        {
            Debug.Assert(offset < _size);

            return new IntPtr(_base + offset);
        }

        public static void ThrowBadImageFormatException()
        {
            Debug.Assert(false);
            throw new BadImageFormatException();
        }

        private uint EnsureOffsetInRange(uint offset, uint lookAhead)
        {
            if ((int)offset < 0 || offset + lookAhead >= _size)
                ThrowBadImageFormatException();
            return offset;
        }

        public byte ReadUInt8(uint offset)
        {
            EnsureOffsetInRange(offset, 0);
            byte* data = _base + offset;
            return NativePrimitiveDecoder.ReadUInt8(ref data);
        }

        public ushort ReadUInt16(uint offset)
        {
            EnsureOffsetInRange(offset, 1);
            byte* data = _base + offset;
            return NativePrimitiveDecoder.ReadUInt16(ref data);
        }

        public uint ReadUInt32(uint offset)
        {
            EnsureOffsetInRange(offset, 3);
            byte* data = _base + offset;
            return NativePrimitiveDecoder.ReadUInt32(ref data);
        }

        public ulong ReadUInt64(uint offset)
        {
            EnsureOffsetInRange(offset, 7);
            byte* data = _base + offset;
            return NativePrimitiveDecoder.ReadUInt64(ref data);
        }

        public unsafe float ReadFloat(uint offset)
        {
            EnsureOffsetInRange(offset, 3);
            byte* data = _base + offset;
            return NativePrimitiveDecoder.ReadFloat(ref data);
        }

        public double ReadDouble(uint offset)
        {
            EnsureOffsetInRange(offset, 7);
            byte* data = _base + offset;
            return NativePrimitiveDecoder.ReadDouble(ref data);
        }

        public uint DecodeUnsigned(uint offset, out uint value)
        {
            EnsureOffsetInRange(offset, 0);

            byte* data = _base + offset;
            value = NativePrimitiveDecoder.DecodeUnsigned(ref data, _base + _size);
            return (uint)(data - _base);
        }

        public uint DecodeSigned(uint offset, out int value)
        {
            EnsureOffsetInRange(offset, 0);

            byte* data = _base + offset;
            value = NativePrimitiveDecoder.DecodeSigned(ref data, _base + _size);
            return (uint)(data - _base);
        }

        public uint DecodeUnsignedLong(uint offset, out ulong value)
        {
            EnsureOffsetInRange(offset, 0);

            byte* data = _base + offset;
            value = NativePrimitiveDecoder.DecodeUnsignedLong(ref data, _base + _size);
            return (uint)(data - _base);
        }

        public uint DecodeSignedLong(uint offset, out long value)
        {
            EnsureOffsetInRange(offset, 0);

            byte* data = _base + offset;
            value = NativePrimitiveDecoder.DecodeSignedLong(ref data, _base + _size);
            return (uint)(data - _base);
        }

        public uint SkipInteger(uint offset)
        {
            EnsureOffsetInRange(offset, 0);

            byte* data = _base + offset;
            NativePrimitiveDecoder.SkipInteger(ref data);
            return (uint)(data - _base);
        }
    }

    internal partial struct NativeParser
    {
        private readonly NativeReader _reader;
        private uint _offset;

        public NativeParser(NativeReader reader, uint offset)
        {
            _reader = reader;
            _offset = offset;
        }

        public bool IsNull
        {
            get
            {
                return _reader == null;
            }
        }

        public NativeReader Reader
        {
            get
            {
                return _reader;
            }
        }

        public uint Offset
        {
            get
            {
                return _offset;
            }
            set
            {
                Debug.Assert(value < _reader.Size);
                _offset = value;
            }
        }

        public static void ThrowBadImageFormatException()
        {
            NativeReader.ThrowBadImageFormatException();
        }

        public byte GetUInt8()
        {
            byte val = _reader.ReadUInt8(_offset);
            _offset += 1;
            return val;
        }

        public uint GetUnsigned()
        {
            uint value;
            _offset = _reader.DecodeUnsigned(_offset, out value);
            return value;
        }

        public ulong GetUnsignedLong()
        {
            ulong value;
            _offset = _reader.DecodeUnsignedLong(_offset, out value);
            return value;
        }

        public int GetSigned()
        {
            int value;
            _offset = _reader.DecodeSigned(_offset, out value);
            return value;
        }

        public uint GetRelativeOffset()
        {
            uint pos = _offset;

            int delta;
            _offset = _reader.DecodeSigned(_offset, out delta);

            return pos + (uint)delta;
        }

        public void SkipInteger()
        {
            _offset = _reader.SkipInteger(_offset);
        }

        public NativeParser GetParserFromRelativeOffset()
        {
            return new NativeParser(_reader, GetRelativeOffset());
        }

        public uint GetSequenceCount()
        {
            return GetUnsigned();
        }
    }

    internal struct NativeHashtable
    {
        private NativeReader _reader;
        private uint _baseOffset;
        private uint _bucketMask;
        private byte _entryIndexSize;

        public NativeHashtable(NativeParser parser)
        {
            uint header = parser.GetUInt8();

            _reader = parser.Reader;
            _baseOffset = parser.Offset;

            int numberOfBucketsShift = (int)(header >> 2);
            if (numberOfBucketsShift > 31)
                NativeReader.ThrowBadImageFormatException();
            _bucketMask = (uint)((1 << numberOfBucketsShift) - 1);

            byte entryIndexSize = (byte)(header & 3);
            if (entryIndexSize > 2)
                NativeReader.ThrowBadImageFormatException();
            _entryIndexSize = entryIndexSize;
        }

        public bool IsNull { get { return _reader == null; } }

        //
        // The enumerator does not conform to the regular C# enumerator pattern to avoid paying
        // its performance penalty (allocation, multiple calls per iteration)
        //
        public struct Enumerator
        {
            private NativeParser _parser;
            private uint _endOffset;
            private byte _lowHashcode;

            internal Enumerator(NativeParser parser, uint endOffset, byte lowHashcode)
            {
                _parser = parser;
                _endOffset = endOffset;
                _lowHashcode = lowHashcode;
            }

            public NativeParser GetNext()
            {
                while (_parser.Offset < _endOffset)
                {
                    byte lowHashcode = _parser.GetUInt8();

                    if (lowHashcode == _lowHashcode)
                    {
                        return _parser.GetParserFromRelativeOffset();
                    }

                    // The entries are sorted by hashcode within the bucket. It allows us to terminate the lookup prematurely.
                    if (lowHashcode > _lowHashcode)
                    {
                        _endOffset = _parser.Offset; // Ensure that extra call to GetNext returns null parser again
                        break;
                    }

                    _parser.SkipInteger();
                }

                return default;
            }
        }

        public struct AllEntriesEnumerator
        {
            private NativeHashtable _table;
            private NativeParser _parser;
            private uint _currentBucket;
            private uint _endOffset;

            internal AllEntriesEnumerator(NativeHashtable table)
            {
                _table = table;
                _currentBucket = 0;
                _parser = _table.GetParserForBucket(_currentBucket, out _endOffset);
            }

            public NativeParser GetNext()
            {
                for (; ; )
                {
                    while (_parser.Offset < _endOffset)
                    {
                        _parser.GetUInt8();
                        return _parser.GetParserFromRelativeOffset();
                    }

                    if (_currentBucket >= _table._bucketMask)
                        return default;

                    _currentBucket++;
                    _parser = _table.GetParserForBucket(_currentBucket, out _endOffset);
                }
            }
        }

        private NativeParser GetParserForBucket(uint bucket, out uint endOffset)
        {
            uint start, end;

            if (_entryIndexSize == 0)
            {
                uint bucketOffset = _baseOffset + bucket;
                start = _reader.ReadUInt8(bucketOffset);
                end = _reader.ReadUInt8(bucketOffset + 1);
            }
            else if (_entryIndexSize == 1)
            {
                uint bucketOffset = _baseOffset + 2 * bucket;
                start = _reader.ReadUInt16(bucketOffset);
                end = _reader.ReadUInt16(bucketOffset + 2);
            }
            else
            {
                uint bucketOffset = _baseOffset + 4 * bucket;
                start = _reader.ReadUInt32(bucketOffset);
                end = _reader.ReadUInt32(bucketOffset + 4);
            }

            endOffset = end + _baseOffset;
            return new NativeParser(_reader, _baseOffset + start);
        }

        // The recommended code pattern to perform lookup is:
        //
        //  var lookup = t.Lookup(TypeHashingAlgorithms.ComputeGenericInstanceHashCode(genericTypeDefinitionHandle, genericTypeArgumentHandles));
        //  NativeParser typeParser;
        //  while (!(typeParser = lookup.GetNext()).IsNull)
        //  {
        //      typeParser.GetTypeSignatureKind(out index);
        //      ... create RuntimeTypeHandle from the external reference RVAs at [index]
        //      ... compare if RuntimeTypeHandle is an instance of pair (genericTypeDefinitionHandle, genericTypeArgumentHandles)
        //  }
        //
        public Enumerator Lookup(int hashcode)
        {
            uint endOffset;
            uint bucket = ((uint)hashcode >> 8) & _bucketMask;
            NativeParser parser = GetParserForBucket(bucket, out endOffset);

            return new Enumerator(parser, endOffset, (byte)hashcode);
        }

        public AllEntriesEnumerator EnumerateAllEntries()
        {
            return new AllEntriesEnumerator(this);
        }
    }
}
