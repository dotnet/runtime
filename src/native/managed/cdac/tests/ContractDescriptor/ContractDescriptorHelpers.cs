// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Tests.ContractDescriptor;

internal unsafe class ContractDescriptorHelpers
{
    public static int Size(bool is64Bit) => is64Bit ? sizeof(ContractDescriptor64) : sizeof(ContractDescriptor32);

    public static void Fill(Span<byte> dest, MockTarget.Architecture arch, int jsonDescriptorSize, uint jsonDescriptorAddr, int pointerDataCount, uint pointerDataAddr)
    {
        if (arch.Is64Bit)
        {
            ContractDescriptor64.Fill(dest, arch.IsLittleEndian, jsonDescriptorSize, jsonDescriptorAddr, pointerDataCount, pointerDataAddr);
        }
        else
        {
            ContractDescriptor32.Fill(dest, arch.IsLittleEndian, jsonDescriptorSize, jsonDescriptorAddr, pointerDataCount, pointerDataAddr);
        }
    }

    private struct ContractDescriptor32
    {
        public ulong Magic = BitConverter.ToUInt64("DNCCDAC\0"u8);
        public uint Flags = 0x2 /*32-bit*/ | 0x1;
        public uint DescriptorSize;
        public uint Descriptor;
        public uint PointerDataCount;
        public uint Pad0 = 0;
        public uint PointerData;

        public ContractDescriptor32() { }

        public static void Fill(Span<byte> dest, bool isLittleEndian, int jsonDescriptorSize, uint jsonDescriptorAddr, int pointerDataCount, uint pointerDataAddr)
        {
            ContractDescriptor32 descriptor = new()
            {
                DescriptorSize = (uint)jsonDescriptorSize,
                Descriptor = jsonDescriptorAddr,
                PointerDataCount = (uint)pointerDataCount,
                PointerData = pointerDataAddr,
            };
            if (BitConverter.IsLittleEndian != isLittleEndian)
                descriptor.ReverseEndianness();

            MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref descriptor, 1)).CopyTo(dest);
        }

        private void ReverseEndianness()
        {
            Magic = BinaryPrimitives.ReverseEndianness(Magic);
            Flags = BinaryPrimitives.ReverseEndianness(Flags);
            DescriptorSize = BinaryPrimitives.ReverseEndianness(DescriptorSize);
            Descriptor = BinaryPrimitives.ReverseEndianness(Descriptor);
            PointerDataCount = BinaryPrimitives.ReverseEndianness(PointerDataCount);
            Pad0 = BinaryPrimitives.ReverseEndianness(Pad0);
            PointerData = BinaryPrimitives.ReverseEndianness(PointerData);
        }
    }

    private struct ContractDescriptor64
    {
        public ulong Magic = BitConverter.ToUInt64("DNCCDAC\0"u8);
        public uint Flags = 0x1;
        public uint DescriptorSize;
        public ulong Descriptor;
        public uint PointerDataCount;
        public uint Pad0 = 0;
        public ulong PointerData;

        public ContractDescriptor64() { }

        public static void Fill(Span<byte> dest, bool isLittleEndian, int jsonDescriptorSize, uint jsonDescriptorAddr, int pointerDataCount, uint pointerDataAddr)
        {
            ContractDescriptor64 descriptor = new()
            {
                DescriptorSize = (uint)jsonDescriptorSize,
                Descriptor = jsonDescriptorAddr,
                PointerDataCount = (uint)pointerDataCount,
                PointerData = pointerDataAddr,
            };
            if (BitConverter.IsLittleEndian != isLittleEndian)
                descriptor.ReverseEndianness();

            MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref descriptor, 1)).CopyTo(dest);
        }

        private void ReverseEndianness()
        {
            Magic = BinaryPrimitives.ReverseEndianness(Magic);
            Flags = BinaryPrimitives.ReverseEndianness(Flags);
            DescriptorSize = BinaryPrimitives.ReverseEndianness(DescriptorSize);
            Descriptor = BinaryPrimitives.ReverseEndianness(Descriptor);
            PointerDataCount = BinaryPrimitives.ReverseEndianness(PointerDataCount);
            Pad0 = BinaryPrimitives.ReverseEndianness(Pad0);
            PointerData = BinaryPrimitives.ReverseEndianness(PointerData);
        }
    }

    #region JSON formatting
    private static string GetTypeJson(string name, Target.TypeInfo info)
    {
        string ret = string.Empty;
        List<string> fields = info.Size is null ? [] : [$"\"!\":{info.Size}"];
        fields.AddRange(info.Fields.Select(f => $"\"{f.Key}\":{(f.Value.TypeName is null ? f.Value.Offset : $"[{f.Value.Offset},\"{f.Value.TypeName}\"]")}"));
        return $"\"{name}\":{{{string.Join(',', fields)}}}";
    }

    public static string MakeTypesJson(IDictionary<DataType, Target.TypeInfo> types)
    {
        return string.Join(',', types.Select(t => GetTypeJson(t.Key.ToString(), t.Value)));
    }

    public static string MakeGlobalsJson(IEnumerable<(string Name, ulong Value, string? Type)> globals)
    {
        return MakeGlobalsJson(globals.Select(g => (g.Name, (ulong?)g.Value, (uint?)null, (string?)null, g.Type)));
    }

    public static string MakeGlobalsJson(IEnumerable<(string Name, ulong? Value, uint? IndirectIndex, string? StringValue, string? Type)> globals)
    {
        return string.Join(',', globals.Select(FormatGlobal));

        static string FormatGlobal((string Name, ulong? Value, uint? IndirectIndex, string? StringValue, string? Type) global)
        {
            if (global.Value is ulong value)
            {
                return $"\"{global.Name}\": {FormatValue(value, global.Type)}";
            }
            else if (global.IndirectIndex is uint index)
            {
                return $"\"{global.Name}\": {FormatIndirect(index, global.Type)}";
            }
            else if (global.StringValue is string stringValue)
            {
                return $"\"{global.Name}\": {FormatString(stringValue, global.Type)}";
            }
            else
            {
                throw new InvalidOperationException("Global must have a value or indirect index");
            }

        }
        static string FormatValue(ulong value, string? type)
        {
            return type is null ? $"{value}" : $"[{value},\"{type}\"]";
        }
        static string FormatIndirect(uint value, string? type)
        {
            return type is null ? $"[{value}]" : $"[[{value}],\"{type}\"]";
        }
        static string FormatString(string value, string? type)
        {
            return type is null ? $"\"{value}\"" : $"[\"{value}\",\"{type}\"]";
        }
    }

    #endregion JSON formatting
}
