// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace ILCompiler.Reflection.ReadyToRun
{
    /// <summary>
    /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/nativeformatreader.h">NativeFormat::NativeArray</a>
    /// </summary>
    public class NativeArray
    {
        // TODO (refactoring) - all these Native* class should be private
        private const int _blockSize = 16;
        private uint _baseOffset;
        private uint _nElements;
        private byte _entryIndexSize;
        private byte[] _image;

        public NativeArray(byte[] image, uint offset)
        {
            uint val = 0;
            _baseOffset = NativeReader.DecodeUnsigned(image, offset, ref val);
            _nElements = (val >> 2);
            _entryIndexSize = (byte)(val & 3);
            _image = image;
        }

        public uint GetCount()
        {
            return _nElements;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"NativeArray Size: {_nElements}");
            sb.AppendLine($"EntryIndexSize: {_entryIndexSize}");
            for (uint i = 0; i < _nElements; i++)
            {
                int val = 0;
                if (TryGetAt(_image, i, ref val))
                {
                    sb.AppendLine($"{i}: {val}");
                }
            }

            return sb.ToString();
        }

        public bool TryGetAt(byte[] image, uint index, ref int pOffset)
        {
            if (index >= _nElements)
                return false;

            uint offset = 0;
            if (_entryIndexSize == 0)
            {
                int i = (int)(_baseOffset + (index / _blockSize));
                offset = NativeReader.ReadByte(image, ref i);
            }
            else if (_entryIndexSize == 1)
            {
                int i = (int)(_baseOffset + 2 * (index / _blockSize));
                offset = NativeReader.ReadUInt16(image, ref i);
            }
            else
            {
                int i = (int)(_baseOffset + 4 * (index / _blockSize));
                offset = NativeReader.ReadUInt32(image, ref i);
            }
            offset += _baseOffset;

            for (uint bit = _blockSize >> 1; bit > 0; bit >>= 1)
            {
                uint val = 0;
                uint offset2 = NativeReader.DecodeUnsigned(image, offset, ref val);
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
            pOffset = (int)offset;
            return true;
        }
    }
}
