// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;

namespace System.Runtime.Serialization
{
    internal class BitFlagsGenerator
    {
        private int _bitCount;
        private CodeGenerator _ilg;
        private LocalBuilder[] _locals;

        public BitFlagsGenerator(int bitCount, CodeGenerator ilg, string localName)
        {
            _ilg = ilg;
            _bitCount = bitCount;
            int localCount = (bitCount + 7) / 8;
            _locals = new LocalBuilder[localCount];
            for (int i = 0; i < _locals.Length; i++)
            {
                _locals[i] = ilg.DeclareLocal(typeof(byte), localName + i, (byte)0);
            }
        }

        public static bool IsBitSet(byte[] bytes, int bitIndex)
        {
            int byteIndex = GetByteIndex(bitIndex);
            byte bitValue = GetBitValue(bitIndex);
            return (bytes[byteIndex] & bitValue) == bitValue;
        }

        public static void SetBit(byte[] bytes, int bitIndex)
        {
            int byteIndex = GetByteIndex(bitIndex);
            byte bitValue = GetBitValue(bitIndex);
            bytes[byteIndex] |= bitValue;
        }

        public int GetBitCount()
        {
            return _bitCount;
        }

        public LocalBuilder GetLocal(int i)
        {
            return _locals[i];
        }

        public int GetLocalCount()
        {
            return _locals.Length;
        }

        public void Load(int bitIndex)
        {
            LocalBuilder local = _locals[GetByteIndex(bitIndex)];
            byte bitValue = GetBitValue(bitIndex);
            _ilg.Load(local);
            _ilg.Load(bitValue);
            _ilg.And();
            _ilg.Load(bitValue);
            _ilg.Ceq();
        }

        public void LoadArray()
        {
            LocalBuilder localArray = _ilg.DeclareLocal(Globals.TypeOfByteArray, "localArray");
            _ilg.NewArray(typeof(byte), _locals.Length);
            _ilg.Store(localArray);
            for (int i = 0; i < _locals.Length; i++)
            {
                _ilg.StoreArrayElement(localArray, i, _locals[i]);
            }
            _ilg.Load(localArray);
        }

        public void Store(int bitIndex, bool value)
        {
            LocalBuilder local = _locals[GetByteIndex(bitIndex)];
            byte bitValue = GetBitValue(bitIndex);
            if (value)
            {
                _ilg.Load(local);
                _ilg.Load(bitValue);
                _ilg.Or();
                _ilg.Stloc(local);
            }
            else
            {
                _ilg.Load(local);
                _ilg.Load(bitValue);
                _ilg.Not();
                _ilg.And();
                _ilg.Stloc(local);
            }
        }

        private static byte GetBitValue(int bitIndex)
        {
            return (byte)(1 << (bitIndex & 7));
        }

        private static int GetByteIndex(int bitIndex)
        {
            return bitIndex >> 3;
        }
    }
}
