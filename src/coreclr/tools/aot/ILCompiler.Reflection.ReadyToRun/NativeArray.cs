// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;

namespace ILCompiler.Reflection.ReadyToRun
{
    /// <summary>
    /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/nativeformatreader.h">NativeFormat::NativeArray</a>
    /// </summary>
    public class NativeArray
    {
        private const int _blockSize = 16;

        private NativeReader _reader;
        private uint _baseOffset;
        private uint _nElements;
        private byte _entryIndexSize;

        public NativeArray(NativeReader reader, uint offset)
        {
            _reader = reader;

            uint val = 0;
            _baseOffset = _reader.DecodeUnsigned(offset, ref val);
            _nElements = (val >> 2);
            _entryIndexSize = (byte)(val & 3);
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
                if (TryGetAt(i, ref val))
                {
                    sb.AppendLine($"{i}: {val}");
                }
            }

            return sb.ToString();
        }

        public bool TryGetAt(uint index, ref int pOffset)
        {
            if (index >= _nElements)
                return false;

            uint offset;
            if (_entryIndexSize == 0)
            {
                int i = (int)(_baseOffset + (index / _blockSize));
                offset = _reader.ReadByte(ref i);
            }
            else if (_entryIndexSize == 1)
            {
                int i = (int)(_baseOffset + 2 * (index / _blockSize));
                offset = _reader.ReadUInt16(ref i);
            }
            else
            {
                int i = (int)(_baseOffset + 4 * (index / _blockSize));
                offset = _reader.ReadUInt32(ref i);
            }
            offset += _baseOffset;

            for (uint bit = _blockSize >> 1; bit > 0; bit >>= 1)
            {
                uint val = 0;
                uint offset2 = _reader.DecodeUnsigned(offset, ref val);
                if ((index & bit) != 0)
                {
                    if ((val & 2) != 0)
                    {
                        offset += val >> 2;
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
