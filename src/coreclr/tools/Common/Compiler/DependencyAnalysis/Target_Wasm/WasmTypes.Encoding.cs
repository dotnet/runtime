// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Binary encoding, name mangling, and JIT interface conversions for the wasm type model.
// These are split out of WasmTypes.cs because they pull in the object writer, the name mangler,
// and the JIT interface, none of which a tool that only computes signatures can reference.

using System;
using System.Diagnostics;

using ILCompiler.ObjectWriter;
using Internal.JitInterface;

namespace ILCompiler.DependencyAnalysis.Wasm
{
    public static partial class WasmValueTypeExtensions
    {
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

    public readonly partial struct WasmResultType
    {
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

    public partial struct WasmFuncType
    {
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

        public void AppendMangledName(NameMangler nameMangler, Internal.Text.Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__wasmtype_"u8);
            _returns.AppendMangledName(sb, isReturn: true);
            _params.AppendMangledName(sb);
        }

        public Internal.Text.Utf8String GetMangledName(NameMangler mangler)
        {
            Internal.Text.Utf8StringBuilder sb = new();
            AppendMangledName(mangler, sb);
            return sb.ToUtf8String();
        }
    }
}
