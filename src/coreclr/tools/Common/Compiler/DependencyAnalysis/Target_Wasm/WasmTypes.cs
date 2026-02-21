// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using ILCompiler.DependencyAnalysis;
using ILCompiler.ObjectWriter;
using Internal.JitInterface;

namespace ILCompiler.DependencyAnalysis.Wasm
{
    // For now, we only encode Wasm numeric value types.
    // These are encoded as a single byte. However,
    // not all value types can be encoded this way.
    // For example, reference types (see https://webassembly.github.io/spec/core/binary/types.html#reference-types)
    // require a more complex encoding.
    public enum WasmValueType : byte
    {
        I32  = 0x7F,
        I64  = 0x7E,
        F32  = 0x7D,
        F64  = 0x7C,
        V128 = 0x7B
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
                WasmValueType.V128 => "v128",
                _ => "unknown",
            };
        }

        public static WasmValueType FromCorInfoType(CorInfoWasmType ty)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan((int)ty, byte.MaxValue);
            if (Enum.IsDefined(typeof(WasmValueType), (byte)ty))
            {
                return (WasmValueType)ty;
            }
            else
            {
                throw new InvalidOperationException("Unsupported CorInfoWasmType: " + ty);
            }
        }
    }

#nullable enable
    public readonly struct WasmResultType : IEquatable<WasmResultType>, IComparable<WasmResultType>
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

        public int CompareTo(WasmResultType other) => MemoryExtensions.SequenceCompareTo(Types, other.Types);

        public void AppendMangledName(Internal.Text.Utf8StringBuilder sb, bool isReturn = false)
        {
            if (isReturn && _types.Length == 0)
            {
                sb.Append("v");
                return;
            }

            foreach (var type in _types)
            {
                sb.Append(type switch
                {
                    WasmValueType.V128 => 'V',
                    WasmValueType.F64 => 'd',
                    WasmValueType.F32 => 'f',
                    WasmValueType.I64 => 'j',
                    WasmValueType.I32 => 'i',
                    _ => throw new NotImplementedException($"Unknown WasmValueType: {type}"),
                });
            }
        }
    }

    public static class WasmResultTypeExtensions
    {
        public static string ToTypeListString(this WasmResultType result)
        {
            return string.Join(" ", result.Types.ToArray().Select(t => t.ToTypeString()));
        }
    }

    public struct WasmFuncType : IEquatable<WasmFuncType>, IComparable<WasmFuncType>
    {
        private readonly WasmResultType _params;
        private readonly WasmResultType _returns;

        public int SignatureLength => _params.Types.Length + _returns.Types.Length;

        public WasmFuncType(WasmResultType paramTypes, WasmResultType returnTypes)
        {
            _params = paramTypes;
            _returns = returnTypes;
        }

        public static WasmFuncType FromCorInfoSignature(CorInfoWasmType[] types)
        {
            WasmResultType rs;
            if (types.Length == 0)
            {
                throw new ArgumentException("Signature must have at least one type for the return value");
            }

            // The first type is the return type
            rs = types[0] switch
            {
                // "void" is actually encoded as an empty type list in Wasm
                CorInfoWasmType.CORINFO_WASM_TYPE_VOID => new WasmResultType(Array.Empty<WasmValueType>()),
                _ => new WasmResultType([WasmValueTypeExtensions.FromCorInfoType(types[0])])
            };

            // The rest are parameter types
            WasmResultType ps;
            if (types.Length > 1)
            {
                WasmValueType[] paramTypes = new WasmValueType[types.Length - 1];
                int idx = 0;
                foreach (CorInfoWasmType paramType in types.AsSpan().Slice(1))
                {
                    paramTypes[idx++] = WasmValueTypeExtensions.FromCorInfoType(paramType);
                }
                ps = new WasmResultType(paramTypes);
            }
            else
            {
                ps = new WasmResultType(Array.Empty<WasmValueType>());
            }

            return new WasmFuncType(ps, rs);
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

            if (string.IsNullOrEmpty(returnList) && string.IsNullOrEmpty(paramList))
            {
                return "(func)";
            }
            else if (string.IsNullOrEmpty(returnList))
            {
                return $"(func (param {paramList}))";
            }
            else if (string.IsNullOrEmpty(paramList))
            {
                return $"(func (result {returnList}))";
            }

            return $"(func (param {paramList}) (result {returnList}))";
        }

        public int CompareTo(WasmFuncType other)
        {
            int paramComparison = _params.CompareTo(other._params);
            if (paramComparison != 0)
                return paramComparison;
            return _returns.CompareTo(other._returns);
        }

        public void AppendMangledName(Internal.Text.Utf8StringBuilder sb)
        {
            sb.Append("__wasmtype_"u8);
            _returns.AppendMangledName(sb, isReturn: true);
            _params.AppendMangledName(sb);
        }

        public Internal.Text.Utf8String GetMangledName()
        {
            Internal.Text.Utf8StringBuilder sb = new();
            AppendMangledName(sb);
            return sb.ToUtf8String();
        }
    }
}
