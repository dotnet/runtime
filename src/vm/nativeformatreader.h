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
            ThrowHR(COR_E_BADIMAGEFORMAT);
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
    };

    class NativeArray
    {
        NativeReader * _pReader;
        uint _baseOffset;
        uint _nElements;
        byte _entryIndexSize;

        static const uint _blockSize = 16;

    public:
        NativeArray()
            : _pReader(NULL)
        {
        }

        NativeArray(NativeReader * pReader, uint offset)
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
}
