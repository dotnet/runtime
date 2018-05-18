// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace R2RDump
{
    class NativeArray
    {
        private const int _blockSize = 16;
        private uint _baseOffset;
        private uint _nElements;
        private byte _entryIndexSize;

        public NativeArray(byte[] image, uint offset)
        {
            uint val = 0;
            _baseOffset = DecodeUnsigned(image, offset, ref val);
            _nElements = (val >> 2);
            _entryIndexSize = (byte)(val & 3);
        }

        public uint GetCount()
        {
            return _nElements;
        }

        public bool TryGetAt(byte[] image, uint index, ref uint pOffset)
        {
            if (index >= _nElements)
                return false;

            uint offset = 0;
            if (_entryIndexSize == 0)
            {
                int i = (int)(_baseOffset + (index / _blockSize));
                offset = R2RReader.ReadByte(image, ref i);
            }
            else if (_entryIndexSize == 1)
            {
                int i = (int)(_baseOffset + 2 * (index / _blockSize));
                offset = R2RReader.ReadUInt16(image, ref i);
            }
            else
            {
                int i = (int)(_baseOffset + 4 * (index / _blockSize));
                offset = R2RReader.ReadUInt32(image, ref i);
            }
            offset += _baseOffset;

            for (uint bit = _blockSize >> 1; bit > 0; bit >>= 1)
            {
                uint val = 0;
                uint offset2 = DecodeUnsigned(image, offset, ref val);
                if ((index & bit) != 0)
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
            pOffset = offset;
            return true;
        }

        public uint DecodeUnsigned(byte[] image, uint offset, ref uint pValue)
        {
            if (offset >= image.Length)
                throw new System.BadImageFormatException("NativeArray offset out of bounds");

            int off = (int)offset;
            uint val = R2RReader.ReadByte(image, ref off);

            if ((val & 1) == 0)
            {
                pValue = (val >> 1);
                offset += 1;
            }
            else if ((val & 2) == 0)
            {
                if (offset + 1 >= image.Length)
                    throw new System.BadImageFormatException("NativeArray offset out of bounds");

                pValue = (val >> 2) |
                      ((uint)R2RReader.ReadByte(image, ref off) << 6);
                offset += 2;
            }
            else if ((val & 4) == 0)
            {
                if (offset + 2 >= image.Length)
                    throw new System.BadImageFormatException("NativeArray offset out of bounds");

                pValue = (val >> 3) |
                      ((uint)R2RReader.ReadByte(image, ref off) << 5) |
                      ((uint)R2RReader.ReadByte(image, ref off) << 13);
                offset += 3;
            }
            else if ((val & 8) == 0)
            {
                if (offset + 3 >= image.Length)
                    throw new System.BadImageFormatException("NativeArray offset out of bounds");

                pValue = (val >> 4) |
                      ((uint)R2RReader.ReadByte(image, ref off) << 4) |
                      ((uint)R2RReader.ReadByte(image, ref off) << 12) |
                      ((uint)R2RReader.ReadByte(image, ref off) << 20);
                offset += 4;
            }
            else if ((val & 16) == 0)
            {
                pValue = R2RReader.ReadUInt32(image, ref off);
                offset += 5;
            }
            else
            {
                throw new System.BadImageFormatException("NativeArray");
            }

            return offset;
        }
    }
}
