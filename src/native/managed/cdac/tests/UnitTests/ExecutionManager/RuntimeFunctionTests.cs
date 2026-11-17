// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests.ExecutionManager;

public class RuntimeFunctionTests
{
    private static Dictionary<DataType, Target.TypeInfo> CreateContractTypes(MockRuntimeFunctionsBuilder runtimeFunctions)
        => new()
        {
            [DataType.RuntimeFunction] = TargetTestHelpers.CreateTypeInfo(runtimeFunctions.RuntimeFunctionLayout),
            [DataType.UnwindInfo] = TargetTestHelpers.CreateTypeInfo(runtimeFunctions.UnwindInfoLayout),
        };

    private static TestPlaceholderTarget CreateWasmTarget(MockMemorySpace.Builder builder)
    {
        MockTarget.Architecture arch = builder.TargetTestHelpers.Arch;
        return new TestPlaceholderTarget.Builder(arch)
            .UseReader(builder.GetMemoryContext().ReadFromTarget)
            .AddTypes(new Dictionary<DataType, Target.TypeInfo>
            {
                [DataType.RuntimeFunction] = TargetTestHelpers.CreateTypeInfo(MockWasmRuntimeFunctions.CreateRuntimeFunctionLayout(arch)),
                [DataType.WasmRuntimeFunctionGroup] = TargetTestHelpers.CreateTypeInfo(MockWasmRuntimeFunctions.CreateGroupLayout(arch)),
            })
            .AddMockContract(new Mock<Contracts.IPlatformMetadata>())
            .Build();
    }

    public static IEnumerable<object[]> StdArchFunctionLengthData()
    {
        foreach (object[] arr in new MockTarget.StdArch())
        {
            MockTarget.Architecture arch = (MockTarget.Architecture)arr[0];
            yield return new object[] { arch, true /*includeEndAddress*/, false /*unwindInfoIsFunctionLength*/};
            yield return new object[] { arch, true /*includeEndAddress*/, true /*unwindInfoIsFunctionLength*/};
            yield return new object[] { arch, false /*includeEndAddress*/, false /*unwindInfoIsFunctionLength*/};
            yield return new object[] { arch, false /*includeEndAddress*/, true /*unwindInfoIsFunctionLength*/};
        }
    }

    [Theory]
    [MemberData(nameof(StdArchFunctionLengthData))]
    public void GetFunctionLength(MockTarget.Architecture arch, bool includeEndAddress, bool unwindInfoIsFunctionLength)
    {
        MockMemorySpace.Builder builder = new(new TargetTestHelpers(arch));
        MockRuntimeFunctionsBuilder runtimeFunctions = new(builder, includeEndAddress, unwindInfoIsFunctionLength);

        uint[] entries = [0x100, 0x1f0, 0x1000, 0x2000, 0xa000];
        TargetPointer addr = runtimeFunctions.AddRuntimeFunctions(entries);

        Target target = new TestPlaceholderTarget.Builder(builder.TargetTestHelpers.Arch)
            .UseReader(builder.GetMemoryContext().ReadFromTarget)
            .AddTypes(CreateContractTypes(runtimeFunctions))
            .Build();
        RuntimeFunctionLookup lookup = RuntimeFunctionLookup.Create(target);

        for (uint i = 0; i < entries.Length; i++)
        {
            uint expectedFunctionLength = i < entries.Length - 1
                ? Math.Min(entries[i + 1] - entries[i], MockRuntimeFunctionsBuilder.DefaultFunctionLength)
                : MockRuntimeFunctionsBuilder.DefaultFunctionLength;

            TargetPointer function = lookup.GetRuntimeFunctionAddress(addr, i);
            uint functionLength = lookup.GetFunctionLength(new(0), function);
            Assert.Equal(expectedFunctionLength, functionLength);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetRuntimeFunctionIndexForAddress(MockTarget.Architecture arch)
    {
        MockMemorySpace.Builder builder = new(new TargetTestHelpers(arch));
        MockRuntimeFunctionsBuilder runtimeFunctions = new(builder);

        uint[] entries = [0x100, 0x1f0, 0x1000, 0x2000, 0xa000];
        TargetPointer addr = runtimeFunctions.AddRuntimeFunctions(entries);

        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(builder.TargetTestHelpers.Arch)
            .UseReader(builder.GetMemoryContext().ReadFromTarget)
            .AddTypes(CreateContractTypes(runtimeFunctions))
            .AddMockContract(new Mock<Contracts.IPlatformMetadata>())
            .Build();
        RuntimeFunctionLookup lookup = RuntimeFunctionLookup.Create(target);

        for (uint i = 0; i < entries.Length; i++)
        {
            TargetPointer relativeAddress = (TargetPointer)entries[i];
            bool res = lookup.TryGetRuntimeFunctionIndexForAddress(addr, (uint)entries.Length, relativeAddress, out uint index);
            Assert.True(res);
            Assert.Equal(i, index);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetRuntimeFunctionIndexForAddress_NoMatch(MockTarget.Architecture arch)
    {
        MockMemorySpace.Builder builder = new(new TargetTestHelpers(arch));
        MockRuntimeFunctionsBuilder runtimeFunctions = new(builder);

        uint[] entries = [0x100, 0x1f0];
        TargetPointer addr = runtimeFunctions.AddRuntimeFunctions(entries);

        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(builder.TargetTestHelpers.Arch)
            .UseReader(builder.GetMemoryContext().ReadFromTarget)
            .AddTypes(CreateContractTypes(runtimeFunctions))
            .AddMockContract(new Mock<Contracts.IPlatformMetadata>())
            .Build();
        RuntimeFunctionLookup lookup = RuntimeFunctionLookup.Create(target);

        TargetPointer relativeAddress = 0x0ff;
        bool res = lookup.TryGetRuntimeFunctionIndexForAddress(addr, (uint)entries.Length, relativeAddress, out _);
        Assert.False(res);
    }

    [Theory]
    [InlineData(0xffu, false, 0u)]
    [InlineData(0x100u, true, 0u)]
    [InlineData(0x10eu, true, 1u)]
    [InlineData(0x11eu, true, 1u)]
    [InlineData(0x120u, true, 32u)]
    [InlineData(0x12eu, true, 33u)]
    public void TryGetWasmRuntimeFunctionIndexForAddress_GroupedSkips(
        uint relativeAddress, bool expectedResult, uint expectedIndex)
    {
        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = false };
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        byte[] groups = new byte[256];
        Array.Fill(groups, byte.MaxValue, 32, 32);
        Array.Fill(groups, byte.MaxValue, 160, 32);
        helpers.Write(groups.AsSpan(0, sizeof(uint)), 0x100u);
        groups[32] = 0;
        groups[33] = 4;
        helpers.Write(groups.AsSpan(128, sizeof(uint)), 0x120u);
        groups[160] = 0;
        groups[161] = 4;
        builder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = 0x10000,
            Data = groups,
            Name = "Grouped runtime functions",
        });

        Target target = CreateWasmTarget(builder);
        RuntimeFunctionLookup lookup = RuntimeFunctionLookup.Create(target, isWasm: true);

        bool result = lookup.TryGetRuntimeFunctionIndexForAddress(0x10000, 64, relativeAddress, out uint index);
        Assert.Equal(expectedResult, result);
        if (result)
        {
            Assert.Equal(expectedIndex, index);
        }
    }

    [Theory]
    [InlineData(0u, 0x100u, false, 0x11234u)]
    [InlineData(1u, 0x110u, true, 0xe5678u)]
    [InlineData(2u, 0x120u, false, 0x1234abcdu)]
    [InlineData(3u, 0x130u, true, 0x12340001u)]
    public void WasmRuntimeFunction_DecodesGroupedEntries(uint index, uint beginAddress, bool isFunclet, uint unwindData)
    {
        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = false };
        MockMemorySpace.Builder builder = new(new TargetTestHelpers(arch));
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(0x10000, 0x20000);
        (ulong address, _) = MockWasmRuntimeFunctions.Add(builder, allocator,
        [
            new(0x100, 0x11234),
            new(0x110, 0xe5678, IsFunclet: true),
            new(0x120, 0x1234abcd),
            new(0x130, 0x12340001, IsFunclet: true),
        ]);
        Target target = CreateWasmTarget(builder);

        Data.WasmRuntimeFunction function =
            target.ProcessedData.GetOrAdd<Data.WasmRuntimeFunction>(address + index * MockWasmRuntimeFunctions.EntrySize);
        Assert.False(function.IsSkip);
        Assert.Equal(beginAddress, function.BeginAddress);
        Assert.Equal(isFunclet, function.IsFunclet);
        Assert.Equal(unwindData, function.UnwindData);

        Data.WasmRuntimeFunction skip =
            target.ProcessedData.GetOrAdd<Data.WasmRuntimeFunction>(address + 31 * MockWasmRuntimeFunctions.EntrySize);
        Assert.True(skip.IsSkip);
        Assert.Throws<InvalidOperationException>(() => skip.BeginAddress);
        Assert.Throws<InvalidOperationException>(() => skip.UnwindData);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(31u)]
    public void WasmRuntimeFunction_RejectsInvalidUnwindEscape(uint index)
    {
        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = false };
        MockMemorySpace.Builder builder = new(new TargetTestHelpers(arch));
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(0x10000, 0x20000);
        (ulong address, _) = MockWasmRuntimeFunctions.Add(builder, allocator, [new(0x100, 0x0010_0000)]);
        Span<byte> group = builder.BorrowAddressRange(address, MockWasmRuntimeFunctions.GroupSize);
        group[32 + 31] = 0;
        group[8 + 15] |= 0xf0;
        Target target = CreateWasmTarget(builder);

        Data.WasmRuntimeFunction function =
            target.ProcessedData.GetOrAdd<Data.WasmRuntimeFunction>(address + index * MockWasmRuntimeFunctions.EntrySize);
        Assert.Throws<BadImageFormatException>(() => function.UnwindData);
    }

    [Fact]
    public void TryGetWasmRuntimeFunctionIndexForAddress_AllSkips()
    {
        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = false };
        MockMemorySpace.Builder builder = new(new TargetTestHelpers(arch));
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(0x10000, 0x20000);
        (ulong address, uint count) = MockWasmRuntimeFunctions.Add(builder, allocator, [new(0, IsSkip: true)]);
        Target target = CreateWasmTarget(builder);

        RuntimeFunctionLookup lookup = RuntimeFunctionLookup.Create(target, isWasm: true);
        Assert.False(lookup.TryGetRuntimeFunctionIndexForAddress(address, count, 0x100, out _));
    }

    [Theory]
    [InlineData(0xffu, false, 0u)]
    [InlineData(0x100u, true, 1u)]
    [InlineData(0x11eu, true, 1u)]
    [InlineData(0x120u, true, 3u)]
    public void TryGetWasmRuntimeFunctionIndexForAddress_LeadingAndInteriorSkips(
        uint relativeAddress, bool expectedResult, uint expectedIndex)
    {
        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = false };
        MockMemorySpace.Builder builder = new(new TargetTestHelpers(arch));
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(0x10000, 0x20000);
        (ulong address, uint count) = MockWasmRuntimeFunctions.Add(builder, allocator,
        [
            new(0, IsSkip: true),
            new(0x100),
            new(0, IsSkip: true),
            new(0x120),
        ]);
        RuntimeFunctionLookup lookup = RuntimeFunctionLookup.Create(CreateWasmTarget(builder), isWasm: true);

        bool result = lookup.TryGetRuntimeFunctionIndexForAddress(address, count, relativeAddress, out uint index);
        Assert.Equal(expectedResult, result);
        if (result)
            Assert.Equal(expectedIndex, index);
    }
}
