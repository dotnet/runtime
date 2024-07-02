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

public unsafe class TargetTests
{
    private const ulong ContractDescriptorAddr = 0xaaaaaaaa;
    private const uint JsonDescriptorAddr = 0xdddddddd;
    private const uint PointerDataAddr = 0xeeeeeeee;

    private static readonly (DataType Type, Target.TypeInfo Info)[] TestTypes =
    [
        // Size and fields
        (DataType.Thread, new(){
            Size = 56,
            Fields = {
                { "Field1", new(){ Offset = 8, Type = DataType.uint16, TypeName = DataType.uint16.ToString() }},
                { "Field2", new(){ Offset = 16, Type = DataType.GCHandle, TypeName = DataType.GCHandle.ToString() }},
                { "Field3", new(){ Offset = 32 }}
            }}),
        // Fields only
        (DataType.ThreadStore, new(){
            Fields = {
                { "Field1", new(){ Offset = 0, TypeName = "FieldType" }},
                { "Field2", new(){ Offset = 8 }}
            }}),
        // Size only
        (DataType.GCHandle, new(){
            Size = 8
        })
    ];

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void GetTypeInfo(bool isLittleEndian, bool is64Bit)
    {
        string typesJson = string.Join(',', TestTypes.Select(t => GetTypeJson(t.Type.ToString(), t.Info)));
        byte[] json = Encoding.UTF8.GetBytes($$"""
        {
            "version": 0,
            "baseline": "empty",
            "contracts": {},
            "types": { {{typesJson}} },
            "globals": {}
        }
        """);
        Span<byte> descriptor = stackalloc byte[ContractDescriptor.Size(is64Bit)];
        ContractDescriptor.Fill(descriptor, isLittleEndian, is64Bit, json.Length, 0);
        fixed (byte* jsonPtr = json)
        {
            ReadContext context = new ReadContext
            {
                ContractDescriptor = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(descriptor)),
                ContractDescriptorLength = descriptor.Length,
                JsonDescriptor = jsonPtr,
                JsonDescriptorLength = json.Length,
            };

            bool success = Target.TryCreate(ContractDescriptorAddr, &ReadFromTarget, &context, out Target? target);
            Assert.True(success);

            foreach ((DataType type, Target.TypeInfo info) in TestTypes)
            {
                {
                    // By known type
                    Target.TypeInfo actual = target.GetTypeInfo(type);
                    Assert.Equal(info.Size, actual.Size);
                    Assert.Equal(info.Fields, actual.Fields);
                }
                {
                    // By name
                    Target.TypeInfo actual = target.GetTypeInfo(type.ToString());
                    Assert.Equal(info.Size, actual.Size);
                    Assert.Equal(info.Fields, actual.Fields);
                }
            }
        }

        static string GetTypeJson(string name, Target.TypeInfo info)
        {
            string ret = string.Empty;
            List<string> fields = info.Size is null ? [] : [$"\"!\":{info.Size}"];
            fields.AddRange(info.Fields.Select(f => $"\"{f.Key}\":{(f.Value.TypeName is null ? f.Value.Offset : $"[{f.Value.Offset},\"{f.Value.TypeName}\"]")}"));
            return $"\"{name}\":{{{string.Join(',', fields)}}}";
        }
    }

    private static readonly (string Name, ulong Value, string? Type)[] TestGlobals =
    [
        ("value", (ulong)sbyte.MaxValue, null),
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

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void ReadGlobalValue(bool isLittleEndian, bool is64Bit)
    {
        string globalsJson = string.Join(',', TestGlobals.Select(i => $"\"{i.Name}\": {(i.Type is null ? i.Value.ToString() : $"[{i.Value}, \"{i.Type}\"]")}"));
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
                ContractDescriptor = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(descriptor)),
                ContractDescriptorLength = descriptor.Length,
                JsonDescriptor = jsonPtr,
                JsonDescriptorLength = json.Length,
            };

            bool success = Target.TryCreate(ContractDescriptorAddr, &ReadFromTarget, &context, out Target? target);
            Assert.True(success);

            ValidateGlobals(target, TestGlobals);
        }
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void ReadIndirectGlobalValue(bool isLittleEndian, bool is64Bit)
    {
        int pointerSize = is64Bit ? sizeof(ulong) : sizeof(uint);
        Span<byte> pointerData = stackalloc byte[TestGlobals.Length * pointerSize];
        for (int i = 0; i < TestGlobals.Length; i++)
        {
            var (_, value, _) = TestGlobals[i];
            WritePointer(pointerData.Slice(i * pointerSize), value, isLittleEndian, pointerSize);
        }

        string globalsJson = string.Join(',', TestGlobals.Select((g, i) => $"\"{g.Name}\": {(g.Type is null ? $"[{i}]" : $"[[{i}], \"{g.Type}\"]")}"));
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
        ContractDescriptor.Fill(descriptor, isLittleEndian, is64Bit, json.Length, pointerData.Length / pointerSize);
        fixed (byte* jsonPtr = json)
        {
            ReadContext context = new ReadContext
            {
                ContractDescriptor = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(descriptor)),
                ContractDescriptorLength = descriptor.Length,
                JsonDescriptor = jsonPtr,
                JsonDescriptorLength = json.Length,
                PointerData = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(pointerData)),
                PointerDataLength = pointerData.Length
            };

            bool success = Target.TryCreate(ContractDescriptorAddr, &ReadFromTarget, &context, out Target? target);
            Assert.True(success);

            // Indirect values are pointer-sized, so max 32-bits for a 32-bit target
            var expected = is64Bit
                ? TestGlobals
                : TestGlobals.Select(g => (g.Name, g.Value & 0xffffffff, g.Type)).ToArray();

            ValidateGlobals(target, expected);
        }
    }

    private static void WritePointer(Span<byte> dest, ulong value, bool isLittleEndian, int pointerSize)
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

    private static void ValidateGlobals(
        Target target,
        (string Name, ulong Value, string? Type)[] globals,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        foreach (var (name, value, type) in globals)
        {
            // Validate that each global can be read successfully based on its type
            // and that it matches the expected value
            if (type is null || type == "int8")
            {
                sbyte actual = target.ReadGlobal<sbyte>(name, out string? actualType);
                AssertEqualsWithCallerInfo(actualType, type);
                AssertEqualsWithCallerInfo((sbyte)value, actual);
            }

            if (type is null || type == "uint8")
            {
                byte actual = target.ReadGlobal<byte>(name, out string? actualType);
                AssertEqualsWithCallerInfo(actualType, type);
                AssertEqualsWithCallerInfo(value, actual);
            }

            if (type is null || type == "int16")
            {
                short actual = target.ReadGlobal<short>(name, out string? actualType);
                AssertEqualsWithCallerInfo(actualType, type);
                AssertEqualsWithCallerInfo((short)value, actual);
            }

            if (type is null || type == "uint16")
            {
                ushort actual = target.ReadGlobal<ushort>(name, out string? actualType);
                AssertEqualsWithCallerInfo(actualType, type);
                AssertEqualsWithCallerInfo(value, actual);
            }

            if (type is null || type == "int32")
            {
                int actual = target.ReadGlobal<int>(name, out string? actualType);
                AssertEqualsWithCallerInfo(actualType, type);
                AssertEqualsWithCallerInfo((int)value, actual);
            }

            if (type is null || type == "uint32")
            {
                uint actual = target.ReadGlobal<uint>(name, out string? actualType);
                AssertEqualsWithCallerInfo(actualType, type);
                AssertEqualsWithCallerInfo((uint)value, actual);
            }

            if (type is null || type == "int64")
            {
                long actual = target.ReadGlobal<long>(name, out string? actualType);
                AssertEqualsWithCallerInfo(actualType, type);
                AssertEqualsWithCallerInfo((long)value, actual);
            }

            if (type is null || type == "uint64")
            {
                ulong actual = target.ReadGlobal<ulong>(name, out string? actualType);
                AssertEqualsWithCallerInfo(actualType, type);
                AssertEqualsWithCallerInfo(value, actual);
            }

            if (type is null || type == "pointer" || type == "nint" || type == "nuint")
            {
                TargetPointer actual = target.ReadGlobalPointer(name, out string? actualType);
                AssertEqualsWithCallerInfo(actualType, type);
                AssertEqualsWithCallerInfo(value, actual.Value);
            }
        }

        void AssertEqualsWithCallerInfo<T>(T expected, T actual) 
        {
            Assert.True((expected is null && actual is null) || expected.Equals(actual), $"Expected: {expected}. Actual: {actual}. [test case: {caller} in {filePath}:{lineNumber}]");
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

        return -1;
    }

    // Used by ReadFromTarget to return the appropriate bytes
    private struct ReadContext
    {
        public byte* ContractDescriptor;
        public int ContractDescriptorLength;

        public byte* JsonDescriptor;
        public int JsonDescriptorLength;

        public byte* PointerData;
        public int PointerDataLength;
    }

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

}
