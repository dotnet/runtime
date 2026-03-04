// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace ILCompiler.ObjectWriter
{
    public interface IWasmEncodable
    {
        int EncodeSize();
        int Encode(Span<byte> buffer);
    }

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

    public static class PlaceholderValues
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

    // For now, we only encode Wasm numeric value types.
    // These are encoded as a single byte. However,
    // not all value types can be encoded this way.
    // For example, reference types (see https://webassembly.github.io/spec/core/binary/types.html#reference-types)
    // require a more complex encoding.
    public enum WasmValueType : byte
    {
        I32 = 0x7F,
        I64 = 0x7E,
        F32 = 0x7D,
        F64 = 0x7C
    }

    public enum WasmMutabilityType : byte
    {
        Const = 0x00,
        Mut = 0x01
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

#nullable enable
    public readonly struct WasmResultType : IEquatable<WasmResultType>
    {
        private readonly WasmValueType[] _types;
        public ReadOnlySpan<WasmValueType> Types => _types;

        /// <summary>
        /// Initializes a new instance of the WasmResultType class with the specified value types.
        /// </summary>
        /// <param name="types">An array of WasmValueType elements representing the types included in the result. If null, an empty array is
        /// used.</param>
        public WasmResultType(WasmValueType[]? types)
        {
            _types = types ?? Array.Empty<WasmValueType>();
        }

        public bool Equals(WasmResultType other) => Types.SequenceEqual(other.Types);
        public override bool Equals(object? obj)
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
            int sizeLength = DwarfHelper.WriteULEB128(buffer, (ulong)_types.Length);
            Span<byte> rest = buffer.Slice(sizeLength);
            for (int i = 0; i < _types.Length; i++)
            {
                rest[i] = (byte)_types[i];
            }
            return (int)(sizeLength + (uint)_types.Length);
        }
    }

    public static class WasmResultTypeExtensions
    {
        public static string ToTypeListString(this WasmResultType result)
        {
            return string.Join(" ", result.Types.ToArray().Select(t => t.ToTypeString()));
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

        public readonly int EncodeSize()
        {
            return 1 + _params.EncodeSize() + _returns.EncodeSize();
        }

        public readonly int Encode(Span<byte> buffer)
        {
            int totalSize = EncodeSize();
            buffer[0] = 0x60; // function type indicator

            int paramSize = _params.Encode(buffer.Slice(1));
            int returnSize = _returns.Encode(buffer.Slice(1 + paramSize));
            Debug.Assert(totalSize == 1 + paramSize + returnSize);

            return totalSize;
        }

        public bool Equals(WasmFuncType other)
        {
            return _params.Equals(other._params) && _returns.Equals(other._returns);
        }

        public override bool Equals(object? obj)
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

    public abstract class WasmImportType : IWasmEncodable
    {
        public readonly WasmExternalKind Kind;
        public abstract int Encode(Span<byte> buffer);
        public abstract int EncodeSize();
        public WasmImportType(WasmExternalKind kind)
        {
            Kind = kind;
        }
    }

    public enum WasmExternalKind : byte
    {
        Function = 0x00,
        Table = 0x01,
        Memory = 0x02,
        Global = 0x03,
        Tag = 0x04,
        Count = 0x05 // Not actually part of the spec; used for counting kinds
    }

    public class WasmGlobalImportType : WasmImportType
    {
        WasmValueType _valueType;
        WasmMutabilityType _mutability;

        public WasmGlobalImportType(WasmValueType valueType, WasmMutabilityType mutability) : base (WasmExternalKind.Global)
        {
            _valueType = valueType;
            _mutability = mutability;
        }

        public override int Encode(Span<byte> buffer)
        {
            buffer[0] = (byte)_valueType;
            buffer[1] = (byte)_mutability;
            return 2;
        }

        public override int EncodeSize() => 2;
    }

    public enum WasmLimitType : byte
    {
        HasMin = 0x00,
        HasMinAndMax = 0x01
    }
  
    public class WasmMemoryImportType : WasmImportType
    {
        WasmLimitType _limitType;
        uint _min;
        uint? _max;

        public WasmMemoryImportType(WasmLimitType limitType, uint min, uint? max = null) : base(WasmExternalKind.Memory)
        {
            if (limitType == WasmLimitType.HasMinAndMax && !max.HasValue)
            {
                throw new ArgumentException("Max must be provided when LimitType is HasMinAndMax");
            }

            _limitType = limitType;
            _min = min;
            _max = max;
        }

        public override int Encode(Span<byte> buffer)
        {
            int pos = 0;
            buffer[pos++] = (byte)_limitType;
            pos += DwarfHelper.WriteULEB128(buffer.Slice(pos), _min);
            if (_limitType == WasmLimitType.HasMinAndMax)
            {
                pos += DwarfHelper.WriteULEB128(buffer.Slice(pos), _max!.Value);
            }
            return pos;
        }

        public override int EncodeSize()
        {
            uint size = 1 + DwarfHelper.SizeOfULEB128(_min);
            if (_limitType == WasmLimitType.HasMinAndMax)
            {
                size += DwarfHelper.SizeOfULEB128(_max!.Value);
            }
            return (int)size;
        }
    }

    public class WasmImport : IWasmEncodable
    {
        public readonly string Module;
        public readonly string Name;
        public  WasmExternalKind Kind => Import.Kind;
        public readonly int? Index;
        public readonly WasmImportType Import;

        public WasmImport(string module, string name, WasmImportType import, int? index = null)
        {
            Module = module;
            Name = name;
            Import = import;
            Index = index;
        }

        public int Encode(Span<byte> buffer) => Import.Encode(buffer);
        public int EncodeSize() => Import.EncodeSize();
    }
}
