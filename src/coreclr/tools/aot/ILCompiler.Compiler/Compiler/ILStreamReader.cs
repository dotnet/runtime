// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;
using Internal.IL;

namespace Internal.Compiler
{
    /// <summary>
    /// IL Opcode reader in external reader style where the reading is done by trying to read
    /// various opcodes, and the reader can indicate success or failure of reading a particular opcode
    ///
    /// Used by logic which is designed to encode information in il structure, but not used
    /// to support general compilation of IL.
    /// </summary>
    public struct ILStreamReader
    {
        private ILReader _reader;
        private readonly MethodIL _methodIL;

        public ILStreamReader(MethodIL methodIL)
        {
            _methodIL = methodIL;
            _reader = new ILReader(methodIL.GetILBytes());
        }

        public bool HasNextInstruction
        {
            get
            {
                return _reader.HasNext;
            }
        }

        public bool TryReadLdtoken(out int token)
        {
            if (_reader.PeekILOpcode() != ILOpcode.ldtoken)
            {
                token = 0;
                return false;
            }

            _reader.ReadILOpcode();
            token = _reader.ReadILToken();
            return true;
        }

        public int ReadLdtoken()
        {
            int result;
            if (!TryReadLdtoken(out result))
                throw new BadImageFormatException();

            return result;
        }

        public bool TryReadLdtokenAsTypeSystemEntity(out TypeSystemEntity entity)
        {
            int token;
            bool tokenResolved;
            try
            {
                tokenResolved = TryReadLdtoken(out token);
                entity = tokenResolved ? (TypeSystemEntity)_methodIL.GetObject(token) : null;
            }
            catch (TypeSystemException)
            {
                tokenResolved = false;
                entity = null;
            }

            return tokenResolved;
        }

        public TypeSystemEntity ReadLdtokenAsTypeSystemEntity()
        {
            TypeSystemEntity result;
            if (!TryReadLdtokenAsTypeSystemEntity(out result))
                throw new BadImageFormatException();

            return result;
        }

        public bool TryReadLdcI4(out int value)
        {
            ILOpcode opcode = _reader.PeekILOpcode();

            if (opcode == ILOpcode.ldc_i4) // ldc.i4
            {
                _reader.ReadILOpcode();
                value = unchecked((int)_reader.ReadILUInt32());
                return true;
            }

            if ((opcode >= ILOpcode.ldc_i4_m1) && (opcode <= ILOpcode.ldc_i4_8)) // ldc.m1 to ldc.i4.8
            {
                _reader.ReadILOpcode();
                value = -1 + ((int)opcode) - 0x15;
                return true;
            }

            if (opcode == ILOpcode.ldc_i4_s) // ldc.i4.s
            {
                _reader.ReadILOpcode();

                value = (int)unchecked((sbyte)_reader.ReadILByte());
                return true;
            }
            value = 0;
            return false;
        }

        public int ReadLdcI4()
        {
            int result;
            if (!TryReadLdcI4(out result))
                throw new BadImageFormatException();

            return result;
        }

        public bool TryReadRet()
        {
            ILOpcode opcode = _reader.PeekILOpcode();
            if (opcode == ILOpcode.ret)
            {
                _reader.ReadILOpcode();
                return true;
            }
            return false;
        }

        public void ReadRet()
        {
            if (!TryReadRet())
                throw new BadImageFormatException();
        }

        public bool TryReadPop()
        {
            ILOpcode opcode = _reader.PeekILOpcode();
            if (opcode == ILOpcode.pop)
            {
                _reader.ReadILOpcode();
                return true;
            }
            return false;
        }

        public void ReadPop()
        {
            if (!TryReadPop())
                throw new BadImageFormatException();
        }

        public bool TryReadLdstr(out string ldstrString)
        {
            if (_reader.PeekILOpcode() != ILOpcode.ldstr)
            {
                ldstrString = null;
                return false;
            }

            _reader.ReadILOpcode();
            int token = _reader.ReadILToken();
            ldstrString = (string)_methodIL.GetObject(token);
            return true;
        }

        public string ReadLdstr()
        {
            string result;
            if (!TryReadLdstr(out result))
                throw new BadImageFormatException();

            return result;
        }
    }
}
