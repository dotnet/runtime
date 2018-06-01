// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace R2RDump
{
    struct NativeParser
    {
        /// <summary>
        /// The current index of the image byte array
        /// </summary>
        public uint Offset { get; set; }

        byte[] _image;

        public NativeParser(byte[] image, uint offset)
        {
            Offset = offset;
            _image = image;
        }

        public bool IsNull()
        {
            return _image == null;
        }

        public uint GetRelativeOffset()
        {
            uint pos = Offset;

            int delta = 0;
            Offset = NativeReader.DecodeSigned(_image, Offset, ref delta);

            return pos + (uint)delta;
        }

        public NativeParser GetParserFromRelativeOffset()
        {
            return new NativeParser(_image, GetRelativeOffset());
        }

        public byte GetByte()
        {
            int off = (int)Offset;
            byte val = NativeReader.ReadByte(_image, ref off);
            Offset += 1;
            return val;
        }

        public uint GetCompressedData()
        {
            int off = (int)Offset;
            uint val = NativeReader.ReadCompressedData(_image, ref off);
            Offset += 1;
            return val;
        }

        public uint GetUnsigned()
        {
            uint value = 0;
            Offset = NativeReader.DecodeUnsigned(_image, Offset, ref value);
            return value;
        }

        public int GetSigned()
        {
            int value = 0;
            Offset = NativeReader.DecodeSigned(_image, Offset, ref value);
            return value;
        }
    }

    struct NativeHashtable
    {
        private byte[] _image;
        private uint _baseOffset;
        private uint _bucketMask;
        private byte _entryIndexSize;

        public NativeHashtable(byte[] image, NativeParser parser)
        {
            uint header = parser.GetByte();
            _baseOffset = parser.Offset;
            _image = image;

            int numberOfBucketsShift = (int)(header >> 2);
            if (numberOfBucketsShift > 31)
                throw new System.BadImageFormatException();
            _bucketMask = (uint)((1 << numberOfBucketsShift) - 1);

            byte entryIndexSize = (byte)(header & 3);
            if (entryIndexSize > 2)
                throw new System.BadImageFormatException();
            _entryIndexSize = entryIndexSize;
        }

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
                        byte lowHashcode = _parser.GetByte();
                        return _parser.GetParserFromRelativeOffset();
                    }

                    if (_currentBucket >= _table._bucketMask)
                        return new NativeParser();

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
                int bucketOffset = (int)(_baseOffset + bucket);
                start = NativeReader.ReadByte(_image, ref bucketOffset);
                end = NativeReader.ReadByte(_image, ref bucketOffset);
            }
            else if (_entryIndexSize == 1)
            {
                int bucketOffset = (int)(_baseOffset + 2 * bucket);
                start = NativeReader.ReadUInt16(_image, ref bucketOffset);
                end = NativeReader.ReadUInt16(_image, ref bucketOffset);
            }
            else
            {
                int bucketOffset = (int)(_baseOffset + 4 * bucket);
                start = NativeReader.ReadUInt32(_image, ref bucketOffset);
                end = NativeReader.ReadUInt32(_image, ref bucketOffset);
            }

            endOffset = end + _baseOffset;
            return new NativeParser(_image, _baseOffset + start);
        }

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
