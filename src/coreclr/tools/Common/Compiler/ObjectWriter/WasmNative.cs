// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using ILCompiler.DependencyAnalysis.Wasm;

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
