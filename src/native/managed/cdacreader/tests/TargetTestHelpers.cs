// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;
internal unsafe class TargetTestHelpers
{
    private const ulong ContractDescriptorAddr = 0xaaaaaaaa;
    private const uint JsonDescriptorAddr = 0xdddddddd;
    private const uint PointerDataAddr = 0xeeeeeeee;

    // Note: all the spans should be stackalloc or pinned.
    public static ReadContext CreateContext(ReadOnlySpan<byte> descriptor, ReadOnlySpan<byte> json, ReadOnlySpan<byte> pointerData = default)
    {
        return new ReadContext
        {
            ContractDescriptor = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(descriptor)),
            ContractDescriptorLength = descriptor.Length,
            JsonDescriptor = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(json)),
            JsonDescriptorLength = json.Length,
            PointerData = pointerData.Length > 0 ? (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(pointerData)) : (byte*)null,
            PointerDataLength = pointerData.Length
        };
    }

    public static bool TryCreateTarget(ReadContext* context, out Target? target)
    {
        return Target.TryCreate(ContractDescriptorAddr, &ReadFromTarget, context, out target);
    }

    // FIXME: make somethign more usable
    public static void AddHeapCallback(ref ReadContext context, delegate*<ulong, byte*, uint, void*, int> callback, void* callbackContext)
    {
        context.HeapCallback = callback;
        context.HeapCallbackContext = callbackContext;
    }

    internal static class ContractDescriptor
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

    [UnmanagedCallersOnly]
    private static int ReadFromTarget(ulong address, byte* buffer, uint length, void* context)
    {
        ReadContext* readContext = (ReadContext*)context;
        var span = new Span<byte>(buffer, (int)length);

        // Populate the span with the requested portion of the contract descriptor
        if (address >= ContractDescriptorAddr && address <= ContractDescriptorAddr + (ulong)readContext->ContractDescriptorLength - length)
        {
            ulong offset = address - ContractDescriptorAddr;
            new ReadOnlySpan<byte>(readContext->ContractDescriptor + offset, (int)length).CopyTo(span);
            return 0;
        }

        // Populate the span with the JSON descriptor - this assumes the product will read it all at once.
        if (address == JsonDescriptorAddr)
        {
            new ReadOnlySpan<byte>(readContext->JsonDescriptor, readContext->JsonDescriptorLength).CopyTo(span);
            return 0;
        }

        // Populate the span with the requested portion of the pointer data
        if (address >= PointerDataAddr && address <= PointerDataAddr + (ulong)readContext->PointerDataLength - length)
        {
            ulong offset = address - PointerDataAddr;
            new ReadOnlySpan<byte>(readContext->PointerData + offset, (int)length).CopyTo(span);
            return 0;
        }

        if (readContext->HeapCallback != null)
        {
            return readContext->HeapCallback(address, buffer, length, readContext->HeapCallbackContext);
        }

        return -1;
    }

    // Used by ReadFromTarget to return the appropriate bytes
    internal ref struct ReadContext
    {
        public byte* ContractDescriptor;
        public int ContractDescriptorLength;

        public byte* JsonDescriptor;
        public int JsonDescriptorLength;

        public byte* PointerData;
        public int PointerDataLength;

        public delegate*<ulong, byte*, uint, void*, int> HeapCallback;

        public void* HeapCallbackContext;
    }

}
