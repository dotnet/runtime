// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;

namespace ILCompiler.ObjectWriter
{
    public enum WasmSectionType
    {
        Custom = 0,
        Type = 1,
        Import = 2,
        Function = 3,
        Table = 4,
        Memory = 5,
        Global = 6,
        Export = 7,
        Start = 8,
        Element = 9,
        Code = 10,
        Data = 11,
        DataCount = 12,
        Tag = 13,
    }

    public static class DummyValues
    {
        // Wasm function signature for (func (params i32) (result i32))
        public static WasmFuncType CreateWasmFunc_i32_i32()
        {
            return new WasmFuncType(
                paramTypes: new([WasmValueType.I32]),
                returnTypes: new([WasmValueType.I32])
            );
        }
    }

    public enum WasmValueType
    {
        I32 = 0x7F,
        I64 = 0x7E,
        F32 = 0x7D,
        F64 = 0x7C
    }

    public static class WasmValueTypeExtensions
    {
        public static string ToTypeString(this WasmValueType valueType)
        {
            return valueType switch
            {
                WasmValueType.I32 => "i32",
                WasmValueType.I64 => "i64",
                WasmValueType.F32 => "f32",
                WasmValueType.F64 => "f64",
                _ => "unknown",
            };
        }
    }

    public struct WasmResultType : IEquatable<WasmResultType>
    {
        private readonly WasmValueType[] _types;
        public readonly Span<WasmValueType> Types => _types;

        public WasmResultType(WasmValueType[] types)
        {
            _types = types;
        }

        public bool Equals(WasmResultType other) => Enumerable.SequenceEqual(_types, other._types);
        public override bool Equals(object obj)
        {
            return obj is WasmResultType other && Equals(other);
        }

        public override int GetHashCode()
        {
            if (_types == null || _types.Length == 0)
                return 0;

            int code = _types[0].GetHashCode();
            for (int i = 1; i < _types.Length; i++)
            {
                code = HashCode.Combine(code, _types[i].GetHashCode());
            }

            return code;
        }

        public int EncodeSize()
        {
            uint sizeLength = DwarfHelper.SizeOfULEB128((ulong)_types.Length);
            return (int)(sizeLength + (uint)_types.Length);
        }

        public int Encode(Span<byte> buffer)
        {
            DwarfHelper.WriteULEB128(buffer, (ulong)_types.Length);
            uint sizeLength = DwarfHelper.SizeOfULEB128((ulong)_types.Length);
            for (int i = 0; i < _types.Length; i++)
            {
                buffer[(int)sizeLength + i] = (byte)_types[i];
            }
            return (int)(sizeLength + (uint)_types.Length);
        }
    }

    public static class WasmResultTypeExtensions
    {
        public static string ToTypeListString(this WasmResultType result)
        {
            var types = result.Types.ToArray();

            if (types == null || types.Length == 0)
                return string.Empty;

            return string.Join(" ", types.Select(t => WasmValueTypeExtensions.ToTypeString(t)));
        }
    }

    public struct WasmFuncType : IEquatable<WasmFuncType>
    {
        private readonly WasmResultType _params;
        private readonly WasmResultType _returns;

        public WasmFuncType(WasmResultType paramTypes, WasmResultType returnTypes)
        {
            _params = paramTypes;
            _returns = returnTypes;
        }

        public readonly byte[] Encode()
        {
            int totalSize = 1+_params.EncodeSize() + _returns.EncodeSize();
            byte[] buffer = new byte[totalSize];
            buffer[0] = 0x60; // function type indicator

            var span = buffer.AsSpan();

            int paramSize = _params.Encode(span.Slice(1));
            int returnSize = _returns.Encode(span.Slice(1+paramSize));
            Debug.Assert(totalSize == 1 + paramSize + returnSize);
            return buffer;
        }

        public bool Equals(WasmFuncType other)
        {
           return _params.Equals(other._params) && _returns.Equals(other._returns);   
        }

        public override bool Equals(object obj)
        {
            return obj is WasmFuncType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_params.GetHashCode(), _returns.GetHashCode());
        }

        public override string ToString()
        {
            string paramList = _params.ToTypeListString();
            string returnList = _returns.ToTypeListString();

            if (string.IsNullOrEmpty(returnList))
                return $"(func (param {paramList}))";
            return $"(func (param {paramList}) (result {returnList}))";
        }
    }

}
