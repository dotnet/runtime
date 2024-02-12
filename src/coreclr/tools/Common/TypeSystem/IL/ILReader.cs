// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL
{
    internal ref struct ILReader
    {
        private int _currentOffset;
        private readonly ReadOnlySpan<byte> _ilBytes;

        public readonly int Offset => _currentOffset;

        public readonly int Size => _ilBytes.Length;

        public readonly bool HasNext => _currentOffset < _ilBytes.Length;

        public ILReader(ReadOnlySpan<byte> ilBytes, int currentOffset = 0)
        {
            _ilBytes = ilBytes;
            _currentOffset = currentOffset;
        }

        //
        // IL stream reading
        //

        public byte ReadILByte()
        {
            if (_currentOffset + 1 > _ilBytes.Length)
                ThrowHelper.ThrowInvalidProgramException();

            return _ilBytes[_currentOffset++];
        }

        public ushort ReadILUInt16()
        {
            if (!BinaryPrimitives.TryReadUInt16LittleEndian(_ilBytes.Slice(_currentOffset), out ushort value))
                ThrowHelper.ThrowInvalidProgramException();

            _currentOffset += sizeof(ushort);
            return value;
        }

        public uint ReadILUInt32()
        {
            if (!BinaryPrimitives.TryReadUInt32LittleEndian(_ilBytes.Slice(_currentOffset), out uint value))
                ThrowHelper.ThrowInvalidProgramException();

            _currentOffset += sizeof(uint);
            return value;
        }

        public int ReadILToken()
        {
            return (int)ReadILUInt32();
        }

        public ulong ReadILUInt64()
        {
            if (!BinaryPrimitives.TryReadUInt64LittleEndian(_ilBytes.Slice(_currentOffset), out ulong value))
                ThrowHelper.ThrowInvalidProgramException();

            _currentOffset += sizeof(ulong);
            return value;
        }

        public float ReadILFloat()
        {
            if (!BinaryPrimitives.TryReadSingleLittleEndian(_ilBytes.Slice(_currentOffset), out float value))
                ThrowHelper.ThrowInvalidProgramException();

            _currentOffset += sizeof(float);
            return value;
        }

        public unsafe double ReadILDouble()
        {
            if (!BinaryPrimitives.TryReadDoubleLittleEndian(_ilBytes.Slice(_currentOffset), out double value))
                ThrowHelper.ThrowInvalidProgramException();

            _currentOffset += sizeof(double);
            return value;
        }

        public ILOpcode ReadILOpcode()
        {
            ILOpcode opcode = (ILOpcode)ReadILByte();
            if (opcode == ILOpcode.prefix1)
            {
                opcode = (ILOpcode)(0x100 + ReadILByte());
            }

            return opcode;
        }

        public ILOpcode PeekILOpcode()
        {
            ILOpcode opcode = (ILOpcode)_ilBytes[_currentOffset];
            if (opcode == ILOpcode.prefix1)
            {
                if (_currentOffset + 2 > _ilBytes.Length)
                    ThrowHelper.ThrowInvalidProgramException();
                opcode = (ILOpcode)(0x100 + _ilBytes[_currentOffset + 1]);
            }

            return opcode;
        }

        public void Skip(ILOpcode opcode)
        {
            if (!opcode.IsValid())
                ThrowHelper.ThrowInvalidProgramException();

            if (opcode != ILOpcode.switch_)
            {
                int opcodeSize = (byte)opcode != (int)opcode ? 2 : 1;
                _currentOffset += opcode.GetSize() - opcodeSize;
            }
            else
            {
                // "switch" opcode is special
                uint count = ReadILUInt32();
                _currentOffset += checked((int)(count * 4));
            }
        }

        public void Seek(int offset)
        {
            _currentOffset = offset;
        }

        public int ReadBranchDestination(ILOpcode currentOpcode)
        {
            if ((currentOpcode >= ILOpcode.br_s && currentOpcode <= ILOpcode.blt_un_s)
                || currentOpcode == ILOpcode.leave_s)
            {
                return (sbyte)ReadILByte() + Offset;
            }
            else
            {
                Debug.Assert((currentOpcode >= ILOpcode.br && currentOpcode <= ILOpcode.blt_un)
                    || currentOpcode == ILOpcode.leave);
                return (int)ReadILUInt32() + Offset;
            }
        }
    }
}
