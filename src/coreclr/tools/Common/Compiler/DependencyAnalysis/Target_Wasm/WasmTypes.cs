// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file holds the wasm type model that describes a signature. It is deliberately free of any
// dependency outside the type system so that it can be linked into tools that only need to compute
// signatures (see ILCompiler.Wasm.Lowering). Binary encoding, name mangling, and the JIT interface
// conversions live in WasmTypes.Encoding.cs.

using System;
using System.Diagnostics;
using System.Linq;

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

    public static partial class WasmValueTypeExtensions
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
    }

#nullable enable
    public readonly partial struct WasmResultType : IEquatable<WasmResultType>, IComparable<WasmResultType>
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
            if (_types.Length == 0)
                return 0;

            int code = _types[0].GetHashCode();
            for (int i = 1; i < _types.Length; i++)
            {
                code = HashCode.Combine(code, _types[i].GetHashCode());
            }

            return code;
        }

        public int CompareTo(WasmResultType other) => MemoryExtensions.SequenceCompareTo(Types, other.Types);
    }

    public static class WasmResultTypeExtensions
    {
        public static string ToTypeListString(this WasmResultType result)
        {
            return string.Join(" ", result.Types.ToArray().Select(t => t.ToTypeString()));
        }
    }

    public readonly struct WasmSignature : IEquatable<WasmSignature>, IComparable<WasmSignature>
    {
        public WasmFuncType FuncType { get; }
        public string SignatureString { get; }

        public WasmSignature(WasmFuncType funcType, string signatureString)
        {
            FuncType = funcType;
            SignatureString = signatureString;
        }

        public bool Equals(WasmSignature other)
        {
            bool result = SignatureString.Equals(other.SignatureString, StringComparison.Ordinal);
            Debug.Assert(!result || FuncType.Equals(other.FuncType),
                "WasmSignature strings match but FuncTypes differ");

            return result;
        }

        public override bool Equals(object? obj) => obj is WasmSignature other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(SignatureString);

        public int CompareTo(WasmSignature other)
        {
            int result = string.Compare(SignatureString, other.SignatureString, StringComparison.Ordinal);
            Debug.Assert(result != 0 || FuncType.Equals(other.FuncType),
                "WasmSignature strings match but FuncTypes differ");
            return result;
        }

        public static bool operator ==(WasmSignature left, WasmSignature right) => left.Equals(right);
        public static bool operator !=(WasmSignature left, WasmSignature right) => !left.Equals(right);
    }

    public partial struct WasmFuncType : IEquatable<WasmFuncType>, IComparable<WasmFuncType>
    {
        private readonly WasmResultType _params;
        private readonly WasmResultType _returns;

        public int SignatureLength => _params.Types.Length + _returns.Types.Length;

        public WasmResultType Params => _params;
        public WasmResultType Returns => _returns;

        public WasmFuncType(WasmResultType paramTypes, WasmResultType returnTypes)
        {
            _params = paramTypes;
            _returns = returnTypes;
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
    }
}
