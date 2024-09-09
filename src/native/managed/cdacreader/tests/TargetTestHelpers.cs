// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;
internal unsafe class TargetTestHelpers
{
    public MockTarget.Architecture Arch { get; init; }

    public TargetTestHelpers(MockTarget.Architecture arch)
    {
        Arch = arch;
    }

    public int PointerSize => Arch.Is64Bit ? sizeof(ulong) : sizeof(uint);
    public int ContractDescriptorSize => ContractDescriptor.Size(Arch.Is64Bit);


    #region Contract and data descriptor creation

    public void ContractDescriptorFill(Span<byte> dest, int jsonDescriptorSize, int pointerDataCount)
    {
        ContractDescriptor.Fill(dest, Arch, jsonDescriptorSize, pointerDataCount);
    }

    internal static class ContractDescriptor
    {
        public static int Size(bool is64Bit) => is64Bit ? sizeof(ContractDescriptor64) : sizeof(ContractDescriptor32);

        public static void Fill(Span<byte> dest, MockTarget.Architecture arch, int jsonDescriptorSize, int pointerDataCount)
        {
            if (arch.Is64Bit)
            {
                ContractDescriptor64.Fill(dest, arch.IsLittleEndian, jsonDescriptorSize, pointerDataCount);
            }
            else
            {
                ContractDescriptor32.Fill(dest, arch.IsLittleEndian, jsonDescriptorSize, pointerDataCount);
            }
        }

        private struct ContractDescriptor32
        {
            public ulong Magic = BitConverter.ToUInt64("DNCCDAC\0"u8);
            public uint Flags = 0x2 /*32-bit*/ | 0x1;
            public uint DescriptorSize;
            public uint Descriptor = MockMemorySpace.JsonDescriptorAddr;
            public uint PointerDataCount;
            public uint Pad0 = 0;
            public uint PointerData = MockMemorySpace.ContractPointerDataAddr;

            public ContractDescriptor32() { }

            public static void Fill(Span<byte> dest, bool isLittleEndian, int jsonDescriptorSize, int pointerDataCount)
            {
                ContractDescriptor32 descriptor = new()
                {
                    DescriptorSize = (uint)jsonDescriptorSize,
                    PointerDataCount = (uint)pointerDataCount,
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
            public ulong Descriptor = MockMemorySpace.JsonDescriptorAddr;
            public uint PointerDataCount;
            public uint Pad0 = 0;
            public ulong PointerData = MockMemorySpace.ContractPointerDataAddr;

            public ContractDescriptor64() { }

            public static void Fill(Span<byte> dest, bool isLittleEndian, int jsonDescriptorSize, int pointerDataCount)
            {
                ContractDescriptor64 descriptor = new()
                {
                    DescriptorSize = (uint)jsonDescriptorSize,
                    PointerDataCount = (uint)pointerDataCount,
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
    }

    #endregion Contract and data descriptor creation

    #region Data descriptor json formatting
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
        return string.Join(',', globals.Select(i => $"\"{i.Name}\": {(i.Type is null ? i.Value.ToString() : $"[{i.Value}, \"{i.Type}\"]")}"));
    }

    #endregion Data descriptor json formatting




    #region Mock memory initialization

    internal uint ObjHeaderSize => (uint)(Arch.Is64Bit ? 2 * sizeof(uint) /*alignpad + syncblock*/: sizeof(uint) /* syncblock */);
    internal uint ObjectSize => (uint)PointerSize /* methtab */;

    internal uint ObjectBaseSize => ObjHeaderSize + ObjectSize;

    internal uint ArrayBaseSize => Arch.Is64Bit ? ObjectSize + sizeof(uint) /* numComponents */ + sizeof(uint) /* pad*/ : ObjectSize + sizeof(uint) /* numComponents */;

    internal uint ArrayBaseBaseSize => ObjHeaderSize + ArrayBaseSize;

    internal uint StringBaseSize => ObjectBaseSize + sizeof(uint) /* length */ + sizeof(char) /* nul terminator */;

    internal void Write(Span<byte> dest, byte b) => dest[0] = b;
    internal void Write(Span<byte> dest, ushort u)
    {
        if (Arch.IsLittleEndian)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(dest, u);
        }
        else
        {
            BinaryPrimitives.WriteUInt16BigEndian(dest, u);
        }
    }

    internal void Write(Span<byte> dest, uint u)
    {
        if (Arch.IsLittleEndian)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(dest, u);
        }
        else
        {
            BinaryPrimitives.WriteUInt32BigEndian(dest, u);
        }
    }

    internal void Write(Span<byte> dest, ulong u)
    {
        if (Arch.IsLittleEndian)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(dest, u);
        }
        else
        {
            BinaryPrimitives.WriteUInt64BigEndian(dest, u);
        }
    }


    internal void WritePointer(Span<byte> dest, ulong value)
    {
        if (Arch.Is64Bit)
        {
            if (Arch.IsLittleEndian)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(dest, value);
            }
            else
            {
                BinaryPrimitives.WriteUInt64BigEndian(dest, value);
            }
        }
        else
        {
            if (Arch.IsLittleEndian)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(dest, (uint)value);
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(dest, (uint)value);
            }
        }
    }

    internal int SizeOfPrimitive(DataType type)
    {
        return type switch
        {
            DataType.uint8 or DataType.int8 => sizeof(byte),
            DataType.uint16 or DataType.int16 => sizeof(ushort),
            DataType.uint32 or DataType.int32 => sizeof(uint),
            DataType.uint64 or DataType.int64 => sizeof(ulong),
            DataType.pointer or DataType.nint or DataType.nuint => PointerSize,
            _ => throw new InvalidOperationException($"Not a primitive: {type}"),
        };
    }

    internal int SizeOfTypeInfo(Target.TypeInfo info)
    {
        int size = 0;
        foreach (var (_, field) in info.Fields)
        {
            size = Math.Max(size, field.Offset + SizeOfPrimitive(field.Type));
        }

        return size;
    }

    #endregion Mock memory initialization

}
