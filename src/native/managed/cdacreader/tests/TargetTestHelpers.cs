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

    internal struct HeapFragment
    {
        public ulong Address;
        public byte[] Data;
        public string? Name;
    }

    /// <summary>
    ///  Helper to build a context for reading from a target.
    /// </summary>
    /// <remarks>
    /// All the spans should be stackalloc or pinned while the context is being used.
    /// </remarks>
    internal unsafe ref struct ContextBuilder
    {
        private bool _created = false;
        private byte* _descriptor = null;
        private int _descriptorLength = 0;
        private byte* _json = null;
        private int _jsonLength = 0;
        private byte* _pointerData = null;
        private int _pointerDataLength = 0;
        private List<HeapFragment> _heapFragments = new();

        public ContextBuilder()
        {

        }

        public ContextBuilder SetDescriptor(scoped ReadOnlySpan<byte> descriptor)
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            _descriptor = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(descriptor));
            _descriptorLength = descriptor.Length;
            return this;
        }

        public ContextBuilder SetJson(scoped ReadOnlySpan<byte> json)
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            _json = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(json));
            _jsonLength = json.Length;
            return this;
        }

        public ContextBuilder SetPointerData(scoped ReadOnlySpan<byte> pointerData)
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            if (pointerData.Length >= 0)
            {
                _pointerData = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(pointerData));
                _pointerDataLength = pointerData.Length;
            }
            return this;
        }

        public ContextBuilder AddHeapFragment(HeapFragment fragment)
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            _heapFragments.Add(fragment);
            return this;
        }

        public ContextBuilder AddHeapFragments(IEnumerable<HeapFragment> fragments)
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            _heapFragments.AddRange(fragments);
            return this;
        }

        public ReadContext Create()
        {
            if (_created)
                throw new InvalidOperationException("Context already created");
            GCHandle fragmentReaderHandle = default; ;
            if (_heapFragments.Count > 0)
            {
                fragmentReaderHandle = GCHandle.Alloc(new HeapFragmentReader(_heapFragments));
            }
            ReadContext context = new ReadContext
            {
                ContractDescriptor = _descriptor,
                ContractDescriptorLength = _descriptorLength,
                JsonDescriptor = _json,
                JsonDescriptorLength = _jsonLength,
                PointerData = _pointerData,
                PointerDataLength = _pointerDataLength,
                HeapFragmentReader = GCHandle.ToIntPtr(fragmentReaderHandle)
            };
            _created = true;
            return context;
        }
    }

    // Note: all the spans should be stackalloc or pinned.
    public static ReadContext CreateContext(ReadOnlySpan<byte> descriptor, ReadOnlySpan<byte> json, ReadOnlySpan<byte> pointerData = default)
    {
        ContextBuilder builder = new ContextBuilder()
        .SetJson(json)
        .SetDescriptor(descriptor)
        .SetPointerData(pointerData);
        return builder.Create();
    }

    public static bool TryCreateTarget(ReadContext* context, out Target? target)
    {
        return Target.TryCreate(ContractDescriptorAddr, &ReadFromTarget, context, out target);
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

        HeapFragmentReader? heapFragmentReader = GCHandle.FromIntPtr(readContext->HeapFragmentReader).Target as HeapFragmentReader;
        if (heapFragmentReader is not null)
        {
            return heapFragmentReader.ReadFragment(address, span);
        }

        return -1;
    }

    // Used by ReadFromTarget to return the appropriate bytes
    internal ref struct ReadContext : IDisposable
    {
        public byte* ContractDescriptor;
        public int ContractDescriptorLength;

        public byte* JsonDescriptor;
        public int JsonDescriptorLength;

        public byte* PointerData;
        public int PointerDataLength;

        public IntPtr HeapFragmentReader;

        public void Dispose()
        {
            if (HeapFragmentReader != IntPtr.Zero)
            {
                GCHandle.FromIntPtr(HeapFragmentReader).Free();
                HeapFragmentReader = IntPtr.Zero;
            }
        }
    }

    private class HeapFragmentReader
    {
        private readonly IReadOnlyList<HeapFragment> _fragments;
        public HeapFragmentReader(IReadOnlyList<HeapFragment> fragments)
        {
            _fragments = fragments;
        }

        public int ReadFragment(ulong address, Span<byte> buffer)
        {
            foreach (var fragment in _fragments)
            {
                if (address >= fragment.Address && address < fragment.Address + (ulong)fragment.Data.Length)
                {
                    int offset = (int)(address - fragment.Address);
                    int availableLength = fragment.Data.Length - offset;
                    if (availableLength >= buffer.Length)
                    {
                        fragment.Data.AsSpan(offset, buffer.Length).CopyTo(buffer);
                        return 0;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Not enough data in fragment at {fragment.Address:X} ('{fragment.Name}') to read {buffer.Length} bytes at {address:X} (only {availableLength} bytes available)");
                    }
                }
            }
            return -1;
        }
    }

    private static string GetTypeJson(string name, Target.TypeInfo info)
    {
        string ret = string.Empty;
        List<string> fields = info.Size is null ? [] : [$"\"!\":{info.Size}"];
        fields.AddRange(info.Fields.Select(f => $"\"{f.Key}\":{(f.Value.TypeName is null ? f.Value.Offset : $"[{f.Value.Offset},\"{f.Value.TypeName}\"]")}"));
        return $"\"{name}\":{{{string.Join(',', fields)}}}";
    }

    public static string MakeTypesJson(IEnumerable<(DataType Type, Target.TypeInfo Info)> types)
    {
        return string.Join(',', types.Select(t => GetTypeJson(t.Type.ToString(), t.Info)));
    }

    public static string MakeGlobalsJson(IEnumerable<(string Name, ulong Value, string? Type)> globals)
    {
        return string.Join(',', globals.Select(i => $"\"{i.Name}\": {(i.Type is null ? i.Value.ToString() : $"[{i.Value}, \"{i.Type}\"]")}"));
    }

    internal static void WritePointer(Span<byte> dest, ulong value, bool isLittleEndian, int pointerSize)
    {
        if (pointerSize == sizeof(ulong))
        {
            if (isLittleEndian)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(dest, value);
            }
            else
            {
                BinaryPrimitives.WriteUInt64BigEndian(dest, value);
            }
        }
        else if (pointerSize == sizeof(uint))
        {
            if (isLittleEndian)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(dest, (uint)value);
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(dest, (uint)value);
            }
        }
    }

    internal static int SizeOfPrimitive(DataType type, bool is64Bit)
    {
        return type switch
        {
            DataType.uint8 or DataType.int8 => sizeof(byte),
            DataType.uint16 or DataType.int16 => sizeof(ushort),
            DataType.uint32 or DataType.int32 => sizeof(uint),
            DataType.uint64 or DataType.int64 => sizeof(ulong),
            DataType.pointer or DataType.nint or DataType.nuint => is64Bit ? sizeof(ulong) : sizeof(uint),
            _ => throw new InvalidOperationException($"Not a primitive: {type}"),
        };
    }

    internal static int SizeOfTypeInfo(bool is64Bit, Target.TypeInfo info)
    {
        int size = 0;
        foreach (var (_, field) in info.Fields)
        {
            size = Math.Max(size, field.Offset + SizeOfPrimitive(field.Type, is64Bit));
        }

        return size;
    }


}
