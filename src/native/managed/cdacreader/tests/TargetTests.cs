// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public unsafe class TargetTests
{
    private struct ReadContext
    {
        public byte IsLittleEndian;
        public int PointerSize;

        public byte* ContractDescriptor;
        public int ContractDescriptorLength;

        public byte* JsonDescriptor;
        public int JsonDescriptorLength;

        public byte* PointerData;
        public int PointerDataLength;
    }

    private const ulong ContractDescriptorAddr = 0xaaaaaaaa;
    private const uint JsonDescriptorAddr = 0xdddddddd;
    private const uint PointerDataAddr = 0xeeeeeeee;

    private static class ContractDescriptor
    {
        public static int Size(bool is64Bit) => is64Bit ? sizeof(ContractDescriptor64) : sizeof(ContractDescriptor32);

        public static void Fill(Span<byte> dest, bool isLittleEndian, bool is64Bit, int jsonDescriptorSize, int pointerDataCount)
        {
            if (is64Bit)
            {
                ContractDescriptor64.Fill(dest, isLittleEndian, jsonDescriptorSize, pointerDataCount);
            }
            else
            {
                ContractDescriptor32.Fill(dest, isLittleEndian, jsonDescriptorSize, pointerDataCount);
            }
        }

        private struct ContractDescriptor32
        {
            public ulong Magic = BitConverter.ToUInt64("DNCCDAC\0"u8);
            public uint Flags = 0x2 /*32-bit*/ | 0x1;
            public uint DescriptorSize;
            public uint Descriptor = JsonDescriptorAddr;
            public uint PointerDataCount;
            public uint Pad0 = 0;
            public uint PointerData = PointerDataAddr;

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
            public ulong Descriptor = JsonDescriptorAddr;
            public uint PointerDataCount;
            public uint Pad0 = 0;
            public ulong PointerData = PointerDataAddr;

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

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void ReadGlobalValue(bool isLittleEndian, bool is64Bit)
    {
        (string Name, ulong Value, string? Type)[] globals =
        [
            ("value", 0xff, null),
            ("int8Value", 0x12, "int8"),
            ("uint8Value", 0x12, "uint8"),
            ("int16Value", 0x1234, "int16"),
            ("uint16Value", 0x1234, "uint16"),
            ("int32Value", 0x12345678, "int32"),
            ("uint32Value", 0x12345678, "uint32"),
            ("int64Value", 0x123456789abcdef0, "int64"),
            ("uint64Value", 0x123456789abcdef0, "uint64"),
            ("nintValue", 0xabcdef0, "nint"),
            ("nuintValue", 0xabcdef0, "nuint"),
            ("pointerValue", 0xabcdef0, "pointer"),
        ];
        string globalsJson = string.Join(',', globals.Select(i => $"\"{i.Name}\": {(i.Type is null ? i.Value.ToString() : $"[{i.Value}, \"{i.Type}\"]")}"));
        byte[] json = Encoding.UTF8.GetBytes($$"""
        {
            "version": 0,
            "baseline": "empty",
            "contracts": {},
            "types": {},
            "globals": { {{globalsJson}} }
        }
        """);
        Span<byte> descriptor = stackalloc byte[ContractDescriptor.Size(is64Bit)];
        ContractDescriptor.Fill(descriptor, isLittleEndian, is64Bit, json.Length, 0);
        fixed (byte* jsonPtr = json)
        {
            ReadContext context = new ReadContext
            {
                IsLittleEndian = (byte)(isLittleEndian ? 1 : 0),
                PointerSize = is64Bit ? sizeof(ulong) : sizeof(uint),
                ContractDescriptor = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(descriptor)),
                ContractDescriptorLength = descriptor.Length,
                JsonDescriptor = jsonPtr,
                JsonDescriptorLength = json.Length,
            };

            bool success = Target.TryCreate(ContractDescriptorAddr, &ReadFromTarget, &context, out Target? target);
            Assert.True(success);

            foreach (var (name, value, type) in globals)
            {
                {
                    success = target.TryReadGlobalUInt8(name, out byte actual);
                    Assert.Equal(type is null || type == "uint8", success);
                    if (success)
                        Assert.Equal(value, actual);
                }
                {
                    success = target.TryReadGlobalPointer(name, out TargetPointer actual);
                    Assert.Equal(type is null || type == "pointer" || type == "nint" || type == "nuint", success);
                    if (success)
                        Assert.Equal(value, actual.Value);
                }
            }
        }
    }

    [UnmanagedCallersOnly]
    private static int ReadFromTarget(ulong address, byte* buffer, uint length, void* context)
    {
        ReadContext* readContext = (ReadContext*)context;

        bool isLittleEndian = readContext->IsLittleEndian != 0;
        int pointerSize = readContext->PointerSize;

        var span = new Span<byte>(buffer, (int)length);

        if (address >= ContractDescriptorAddr
            && address <= ContractDescriptorAddr + (ulong)readContext->ContractDescriptorLength - length)
        {
            ulong offset = address - ContractDescriptorAddr;
            new ReadOnlySpan<byte>(readContext->ContractDescriptor + offset, (int)length).CopyTo(span);
            return 0;
        }

        if (address == JsonDescriptorAddr)
        {
            new ReadOnlySpan<byte>(readContext->JsonDescriptor, readContext->JsonDescriptorLength).CopyTo(span);
            return 0;
        }

        if (address >= PointerDataAddr && address <= PointerDataAddr + (ulong)readContext->PointerDataLength - length)
        {
            ulong offset = address - PointerDataAddr;
            new ReadOnlySpan<byte>(readContext->PointerData + offset, (int)length).CopyTo(span);
            return 0;
        }

        return -1;
    }
}
