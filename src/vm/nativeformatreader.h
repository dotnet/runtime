// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ---------------------------------------------------------------------------
// NativeFormatReader
//
// Utilities to read native data from images
// ---------------------------------------------------------------------------

#pragma once

// To reduce differences between C# and C++ versions
#define byte uint8_t
#define uint uint32_t

#define UInt16 uint16_t
#define UInt32 uint32_t
#define UInt64 uint64_t

namespace NativeFormat
{
    class NativeReader;
    typedef DPTR(NativeReader) PTR_NativeReader;

    class NativeReader
    {
        PTR_BYTE _base;
        uint _size;

    public:
        NativeReader()
        {
            _base = NULL;
            _size = 0;
        }

        NativeReader(PTR_BYTE base_, uint size)
        {
            _base = base_;
            _size = size;
        }

        void ThrowBadImageFormatException()
        {
            _ASSERTE(false);

#ifndef DACCESS_COMPILE
            // Failfast instead of throwing, to avoid violating NOTHROW contracts of callers
            EEPOLICY_HANDLE_FATAL_ERROR(COR_E_BADIMAGEFORMAT);
#endif
        }

        uint EnsureOffsetInRange(uint offset, uint lookAhead)
        {
            if ((int)offset < 0 || offset + lookAhead >= _size)
                ThrowBadImageFormatException();
            return offset;
        }

        byte ReadUInt8(uint offset)
        {
            if (offset >= _size)
                ThrowBadImageFormatException();
            return *(_base + offset); // Assumes little endian and unaligned access
        }

        UInt16 ReadUInt16(uint offset)
        {
            if ((int)offset < 0 || offset + 1 >= _size)
                ThrowBadImageFormatException();
            return *dac_cast<PTR_USHORT>(_base + offset); // Assumes little endian and unaligned access
        }

        UInt32 ReadUInt32(uint offset)
        {
            if ((int)offset < 0 || offset + 3 >= _size)
                ThrowBadImageFormatException();
            return *dac_cast<PTR_UINT32>(_base + offset); // Assumes little endian and unaligned access
        }

        uint DecodeUnsigned(uint offset, uint * pValue)
        {
            if (offset >= _size)
                ThrowBadImageFormatException();

            uint val = *(_base + offset);
            if ((val & 1) == 0)
            {
                *pValue = (val >> 1);
                offset += 1;
            }
            else
            if ((val & 2) == 0)
            {
                if (offset + 1 >= _size)
                    ThrowBadImageFormatException();
                *pValue = (val >> 2) | 
                      (((uint)*(_base + offset + 1)) << 6);
                offset += 2;
            }
            else
            if ((val & 4) == 0)
            {
                if (offset + 2 >= _size)
                    ThrowBadImageFormatException();
                *pValue = (val >> 3) |
                      (((uint)*(_base + offset + 1)) << 5) |
                      (((uint)*(_base + offset + 2)) << 13);
                offset += 3;
            }
            else
            if ((val & 8) == 0)
            {
                if (offset + 3 >= _size)
                    ThrowBadImageFormatException();
                *pValue = (val >> 4) |
                      (((uint)*(_base + offset + 1)) << 4) |
                      (((uint)*(_base + offset + 2)) << 12) |
                      (((uint)*(_base + offset + 3)) << 20);
                offset += 4;
            }
            else
            if ((val & 16) == 0)
            {
                *pValue = ReadUInt32(offset + 1);
                offset += 5;
            }
            else
            {
                ThrowBadImageFormatException();
            }

            return offset;
        }

        int DecodeSigned(uint offset, int * pValue)
        {
            if (offset >= _size)
                ThrowBadImageFormatException();

            int val = *(_base + offset);
            if ((val & 1) == 0)
            {
                *pValue = val >> 1;
                offset += 1;
            }
            else if ((val & 2) == 0)
            {
                if (offset + 1 >= _size)
                    ThrowBadImageFormatException();
                *pValue = (val >> 2) |
                    (((int)*(_base + offset + 1)) << 6);
                offset += 2;
            }
            else if ((val & 4) == 0)
            {
                if (offset + 2 >= _size)
                    ThrowBadImageFormatException();
                *pValue = (val >> 3) |
                    (((int)*(_base + offset + 1)) << 5) |
                    (((int)*(_base + offset + 2)) << 13);
                offset += 3;
            }
            else if ((val & 8) == 0)
            {
                if (offset + 3 >= _size)
                    ThrowBadImageFormatException();
                *pValue = (val >> 4) |
                    (((int)*(_base + offset + 1)) << 4) |
                    (((int)*(_base + offset + 2)) << 12) |
                    (((int)*(_base + offset + 3)) << 20);
                offset += 4;
            }
            else if ((val & 16) == 0)
            {
                *pValue = (int)ReadUInt32(offset + 1);
                offset += 5;
            }
            else
            {
                ThrowBadImageFormatException();
            }

            return offset;
        }

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable : 4702) // Disable unreachable code warning
#endif
        uint SkipInteger(uint offset)
        {
            EnsureOffsetInRange(offset, 0);

            PTR_BYTE data = (_base + offset);
            if ((*data & 1) == 0)
            {
                return offset + 1;
            }
            else if ((*data & 2) == 0)
            {
                return offset + 2;
            }
            else if ((*data & 4) == 0)
            {
                return offset + 3;
            }
            else if ((*data & 8) == 0)
            {
                return offset + 4;
            }
            else if ((*data & 16) == 0)
            {
                return offset + 5;
            }
            else if ((*data & 32) == 0)
            {
                return offset + 9;
            }
            else
            {
                ThrowBadImageFormatException();
                return offset;
            }
        }

#ifndef DACCESS_COMPILE
        const BYTE* GetBlob(uint offset)
        {
            EnsureOffsetInRange(offset, 0);
            return _base + offset;
        }
#endif
#ifdef _MSC_VER
#pragma warning(pop)
#endif
    };

    class NativeParser
    {
        PTR_NativeReader _pReader;
        uint _offset;

    public:
        NativeParser()
            : _pReader(PTR_NULL), _offset(0)
        {
        }
        
        NativeParser(PTR_NativeReader pReader, uint offset)
        {
            _pReader = pReader;
            _offset = offset;
        }

        NativeReader * GetNativeReader() { return _pReader; }

        uint GetOffset() { return _offset; }
        void SetOffset(uint value) { _offset = value; }

        void ThrowBadImageFormatException()
        {
            _pReader->ThrowBadImageFormatException();
        }

        byte GetUInt8()
        {
            byte val = _pReader->ReadUInt8(_offset);
            _offset += 1;
            return val;
        }

        uint GetUnsigned()
        {
            uint value;
            _offset = _pReader->DecodeUnsigned(_offset, &value);
            return value;
        }

        int GetSigned()
        {
            int value;
            _offset = _pReader->DecodeSigned(_offset, &value);
            return value;
        }

        uint GetRelativeOffset()
        {
            uint pos = _offset;

            int delta;
            _offset = _pReader->DecodeSigned(_offset, &delta);

            return pos + (uint)delta;
        }

#ifndef DACCESS_COMPILE
        const BYTE * GetBlob()
        {
            return _pReader->GetBlob(_offset);
        }
#endif

        void SkipInteger()
        {
            _offset = _pReader->SkipInteger(_offset);
        }

        NativeParser GetParserFromRelativeOffset()
        {
            return NativeParser(_pReader, GetRelativeOffset());
        }
    };
    
    class NativeArray
    {
        PTR_NativeReader _pReader;
        uint _baseOffset;
        uint _nElements;
        byte _entryIndexSize;

        static const uint _blockSize = 16;

    public:
        NativeArray()
            : _pReader(PTR_NULL), _nElements(0)
        {
        }

        NativeArray(PTR_NativeReader pReader, uint offset)
            : _pReader(pReader)
        {
            uint val;
            _baseOffset = pReader->DecodeUnsigned(offset, &val);

            _nElements = (val >> 2);
            _entryIndexSize = (val & 3);
        }

        uint GetCount()
        {
            return _nElements;
        }

        bool TryGetAt(uint index, uint * pOffset)
        {
            if (index >= _nElements)
                return false;

            uint offset;
            if (_entryIndexSize == 0)
            {
                offset = _pReader->ReadUInt8(_baseOffset + (index / _blockSize));
            }
            else if (_entryIndexSize == 1)
            {
                offset = _pReader->ReadUInt16(_baseOffset + 2 * (index / _blockSize));
            }
            else
            {
                offset = _pReader->ReadUInt32(_baseOffset + 4 * (index / _blockSize));
            }
            offset += _baseOffset;

            for (uint bit = _blockSize >> 1; bit > 0; bit >>= 1)
            {
                uint val;
                uint offset2 = _pReader->DecodeUnsigned(offset, &val);
                if (index & bit)
                {
                    if ((val & 2) != 0)
                    {
                        offset = offset + (val >> 2);
                        continue;
                    }
                }
                else
                {
                    if ((val & 1) != 0)
                    {
                        offset = offset2;
                        continue;
                    }
                }

                // Not found
                if ((val & 3) == 0)
                {
                    // Matching special leaf node?
                    if ((val >> 2) == (index & (_blockSize - 1)))
                    {
                        offset = offset2;
                        break;
                    }
                }
                return false;
            }

            *pOffset = offset;
            return true;
        }
    };

    class NativeHashtable
    {
        PTR_NativeReader _pReader;
        uint _baseOffset;
        uint _bucketMask;
        byte _entryIndexSize;

        NativeParser GetParserForBucket(uint bucket, uint * pEndOffset)
        {
            uint start, end;

            if (_entryIndexSize == 0)
            {
                uint bucketOffset = _baseOffset + bucket;
                start = _pReader->ReadUInt8(bucketOffset);
                end = _pReader->ReadUInt8(bucketOffset + 1);
            }
            else if (_entryIndexSize == 1)
            {
                uint bucketOffset = _baseOffset + 2 * bucket;
                start = _pReader->ReadUInt16(bucketOffset);
                end = _pReader->ReadUInt16(bucketOffset + 2);
            }
            else
            {
                uint bucketOffset = _baseOffset + 4 * bucket;
                start = _pReader->ReadUInt32(bucketOffset);
                end = _pReader->ReadUInt32(bucketOffset + 4);
            }

            *pEndOffset = end + _baseOffset;
            return NativeParser(_pReader, _baseOffset + start);
        }

    public:
        NativeHashtable() : _pReader(PTR_NULL), _baseOffset(0), _bucketMask(0), _entryIndexSize(0)
        {
        }
        
        NativeHashtable(NativeParser& parser)
        {
            uint header = parser.GetUInt8();

            _pReader = dac_cast<PTR_NativeReader>(parser.GetNativeReader());
            _baseOffset = parser.GetOffset();

            int numberOfBucketsShift = (int)(header >> 2);
            if (numberOfBucketsShift > 31)
                _pReader->ThrowBadImageFormatException();
            _bucketMask = (uint)((1 << numberOfBucketsShift) - 1);

            byte entryIndexSize = (byte)(header & 3);
            if (entryIndexSize > 2)
                _pReader->ThrowBadImageFormatException();
            _entryIndexSize = entryIndexSize;
        }

        bool IsNull() { return _pReader == NULL; }

        //
        // The enumerator does not conform to the regular C# enumerator pattern to avoid paying 
        // its performance penalty (allocation, multiple calls per iteration)
        //
        class Enumerator
        {
            NativeParser _parser;
            uint _endOffset;
            byte _lowHashcode;

        public:
            Enumerator(NativeParser parser, uint endOffset, byte lowHashcode)
            {
                _parser = parser;
                _endOffset = endOffset;
                _lowHashcode = lowHashcode;
            }

            bool GetNext(NativeParser& entryParser)
            {
                while (_parser.GetOffset() < _endOffset)
                {
                    byte lowHashcode = _parser.GetUInt8();

                    if (lowHashcode == _lowHashcode)
                    {
                        entryParser = _parser.GetParserFromRelativeOffset();
                        return true;
                    }

                    // The entries are sorted by hashcode within the bucket. It allows us to terminate the lookup prematurely.
                    if (lowHashcode > _lowHashcode)
                    {
                        _endOffset = _parser.GetOffset(); // Ensure that extra call to GetNext returns null parser again
                        break;
                    }

                    _parser.SkipInteger();
                }

                return false;
            }
        };

        // The recommended code pattern to perform lookup is: 
        //
        //  NativeHashtable::Enumerator lookup = hashtable.Lookup(dwHashCode);
        //  NativeParser entryParser;
        //  while (lookup.GetNext(entryParser))
        //  {
        //      ... read entry using entryParser ...
        //  }
        //
        Enumerator Lookup(int hashcode)
        {
            uint endOffset;
            uint bucket = ((uint)hashcode >> 8) & _bucketMask;
            NativeParser parser = GetParserForBucket(bucket, &endOffset);

            return Enumerator(parser, endOffset, (byte)hashcode);
        }
    };
}
