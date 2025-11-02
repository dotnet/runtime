// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests.ContractDescriptor;

public unsafe partial class TargetTests
{
    private static readonly Dictionary<DataType, Target.TypeInfo> TestTypes = new()
    {
        // Size and fields
        [DataType.Thread] = new()
        {
            Size = 56,
            Fields = new Dictionary<string, Target.FieldInfo> {
                { "Field1", new(){ Offset = 8, Type = DataType.uint16, TypeName = DataType.uint16.ToString() }},
                { "Field2", new(){ Offset = 16, Type = DataType.GCHandle, TypeName = DataType.GCHandle.ToString() }},
                { "Field3", new(){ Offset = 32 }}
            }
        },
        // Fields only
        [DataType.ThreadStore] = new()
        {
            Fields = new Dictionary<string, Target.FieldInfo> {
                { "Field1", new(){ Offset = 0, TypeName = "FieldType" }},
                { "Field2", new(){ Offset = 8 }}
            }
        },
        // Size only
        [DataType.GCHandle] = new()
        {
            Size = 8
        }
    };

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetTypeInfo(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder.SetTypes(TestTypes)
            .SetGlobals(Array.Empty<(string, ulong, string?)>())
            .SetContracts(Array.Empty<string>());

        bool success = builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target);
        Assert.True(success);

        ValidateTypes(target, TestTypes);
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
    [ClassData(typeof(MockTarget.StdArch))]
    public void ReadGlobalValue(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder.SetTypes(new Dictionary<DataType, Target.TypeInfo>())
            .SetGlobals(TestGlobals)
            .SetContracts([]);

        bool success = builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target);
        Assert.True(success);

        ValidateGlobals(target, TestGlobals);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ReadIndirectGlobalValue(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder.SetTypes(new Dictionary<DataType, Target.TypeInfo>())
            .SetContracts([])
            .SetGlobals(TestGlobals.Select(MakeGlobalToIndirect).ToArray(),
                        TestGlobals.Select((g) => g.Value).ToArray());

        bool success = builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target);
        Assert.True(success);

        // Indirect values are pointer-sized, so max 32-bits for a 32-bit target
        var expected = arch.Is64Bit
            ? TestGlobals
            : TestGlobals.Select(g => (g.Name, g.Value & 0xffffffff, g.Type)).ToArray();

        ValidateGlobals(target, expected);

        static (string Name, ulong? Value, uint? IndirectIndex, string? StringValue, string? Type) MakeGlobalToIndirect((string Name, ulong Value, string? Type) global, int index)
        {
            return (global.Name, null, (uint?)index, null, global.Type);
        }
    }

    private static readonly (string Name, string Value, ulong? NumericValue)[] GlobalStringyValues =
    [
        ("value", "testString", null),
        ("emptyString", "", null),
        ("specialChars", "string with spaces & special chars ✓", null),
        ("longString", new string('a', 1024), null),
        ("hexString", "0x1234", 0x1234),
        ("decimalString", "123456", 123456),
        ("negativeString", "-1234", unchecked((ulong)-1234)),
        ("negativeHexString", "-0x1234", unchecked((ulong)-0x1234)),
    ];

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ReadGlobalStringyValues(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder.SetTypes(new Dictionary<DataType, Target.TypeInfo>())
            .SetContracts([])
            .SetGlobals(GlobalStringyValues.Select(MakeGlobalsToStrings).ToArray());

        bool success = builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target);
        Assert.True(success);

        ValidateGlobalStrings(target, GlobalStringyValues);

        static (string Name, ulong? Value, string? StringValue, string? Type) MakeGlobalsToStrings((string Name, string Value, ulong? NumericValue) global)
        {
            return (global.Name, null, global.Value, "string");
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ReadUtf8String(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);

        string expected = "UTF-8 string ✓";
        ulong addr = 0x1000;

        MockMemorySpace.HeapFragment fragment = new() { Address = addr, Data = new byte[Encoding.UTF8.GetByteCount(expected) + 1] };
        Encoding.UTF8.GetBytes(expected).AsSpan().CopyTo(fragment.Data);
        fragment.Data[^1] = 0;
        builder.AddHeapFragment(fragment);

        bool success = builder.TryCreateTarget(new(builder), out ContractDescriptorTarget? target);
        Assert.True(success);

        string actual = target.ReadUtf8String(addr);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void WriteValue(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        uint expected = 0xdeadbeef;
        ulong addr = 0x1000;

        MockMemorySpace.HeapFragment fragment = new() { Address = addr, Data = new byte[4] };
        builder.AddHeapFragment(fragment);

        bool success = builder.TryCreateTarget(new(builder), out ContractDescriptorTarget? target);
        Assert.True(success);
        target.Write<uint>(addr, expected);
        Assert.Equal(expected, target.Read<uint>(addr));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void WriteBuffer(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        byte[] expected = new byte[] { 0xde, 0xad, 0xbe, 0xef };
        ulong addr = 0x1000;

        MockMemorySpace.HeapFragment fragment = new() { Address = addr, Data = new byte[4] };
        builder.AddHeapFragment(fragment);

        bool success = builder.TryCreateTarget(new(builder), out ContractDescriptorTarget? target);
        Assert.True(success);
        target.WriteBuffer(addr, expected);
        Span<byte> data = stackalloc byte[4];
        target.ReadBuffer(addr, data);
        Assert.Equal(expected, data);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ReadUtf16String(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);

        string expected = "UTF-16 string ✓";
        ulong addr = 0x1000;

        Encoding encoding = arch.IsLittleEndian ? Encoding.Unicode : Encoding.BigEndianUnicode;
        MockMemorySpace.HeapFragment fragment = new() { Address = addr, Data = new byte[encoding.GetByteCount(expected) + sizeof(char)] };
        targetTestHelpers.WriteUtf16String(fragment.Data, expected);
        builder.AddHeapFragment(fragment);

        bool success = builder.TryCreateTarget(new(builder), out ContractDescriptorTarget? target);
        Assert.True(success);

        string actual = target.ReadUtf16String(addr);
        Assert.Equal(expected, actual);
    }

    private static void ValidateGlobals(
        ContractDescriptorTarget target,
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

    private static void ValidateGlobalStrings(
        ContractDescriptorTarget target,
        (string Name, string Value, ulong? NumericValue)[] globals,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        foreach (var (name, value, numericValue) in globals)
        {
            string actualString;

            Assert.True(target.TryReadGlobalString(name, out actualString));
            AssertEqualsWithCallerInfo(value, actualString);

            actualString = target.ReadGlobalString(name);
            AssertEqualsWithCallerInfo(value, actualString);

            if (numericValue != null)
            {
                ulong? actualNumericValue;

                Assert.True(target.TryReadGlobal(name, out actualNumericValue));
                AssertEqualsWithCallerInfo(numericValue.Value, actualNumericValue.Value);

                actualNumericValue = target.ReadGlobal<ulong>(name);
                AssertEqualsWithCallerInfo(numericValue.Value, actualNumericValue.Value);

                TargetPointer? actualPointer;

                Assert.True(target.TryReadGlobalPointer(name, out actualPointer));
                AssertEqualsWithCallerInfo(numericValue.Value, actualPointer.Value.Value);

                actualPointer = target.ReadGlobalPointer(name);
                AssertEqualsWithCallerInfo(numericValue.Value, actualPointer.Value.Value);
            }
            else
            {
                // if there is no numeric value, assert that reading as numeric fails
                Assert.False(target.TryReadGlobal(name, out ulong? _));
                Assert.ThrowsAny<Exception>(() => target.ReadGlobal<ulong>(name));
                Assert.False(target.TryReadGlobalPointer(name, out TargetPointer? _));
                Assert.ThrowsAny<Exception>(() => target.ReadGlobalPointer(name));
            }
        }

        void AssertEqualsWithCallerInfo<T>(T expected, T actual)
        {
            Assert.True((expected is null && actual is null) || expected.Equals(actual), $"Expected: {expected}. Actual: {actual}. [test case: {caller} in {filePath}:{lineNumber}]");
        }
    }

    private static void ValidateTypes(
        ContractDescriptorTarget target,
        Dictionary<DataType, Target.TypeInfo> types,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        foreach ((DataType type, Target.TypeInfo info) in types)
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
}
