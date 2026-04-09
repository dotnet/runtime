// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal sealed class MockRuntimeFunction : TypedView
{
    private const string BeginAddressFieldName = "BeginAddress";
    private const string UnwindDataFieldName = "UnwindData";
    private const string EndAddressFieldName = "EndAddress";

    public static Layout<MockRuntimeFunction> CreateLayout(MockTarget.Architecture architecture, bool includeEndAddress)
    {
        SequentialLayoutBuilder builder = new SequentialLayoutBuilder("RuntimeFunction", architecture)
            .AddUInt32Field(BeginAddressFieldName)
            .AddUInt32Field(UnwindDataFieldName);

        if (includeEndAddress)
        {
            builder.AddUInt32Field(EndAddressFieldName);
        }

        return builder.Build<MockRuntimeFunction>();
    }

    public uint BeginAddress
    {
        get => ReadUInt32Field(BeginAddressFieldName);
        set => WriteUInt32Field(BeginAddressFieldName, value);
    }

    public uint UnwindData
    {
        get => ReadUInt32Field(UnwindDataFieldName);
        set => WriteUInt32Field(UnwindDataFieldName, value);
    }

    public uint EndAddress
    {
        get => ReadUInt32Field(EndAddressFieldName);
        set => WriteUInt32Field(EndAddressFieldName, value);
    }
}

internal sealed class MockUnwindInfo : TypedView
{
    private const string FunctionLengthFieldName = "FunctionLength";
    private const string HeaderFieldName = "Header";

    public static Layout<MockUnwindInfo> CreateLayout(MockTarget.Architecture architecture, bool isFunctionLength)
    {
        SequentialLayoutBuilder builder = new SequentialLayoutBuilder("UnwindInfo", architecture);
        builder.AddUInt32Field(isFunctionLength ? FunctionLengthFieldName : HeaderFieldName);
        return builder.Build<MockUnwindInfo>();
    }

    public uint FunctionLength
    {
        get => ReadUInt32Field(FunctionLengthFieldName);
        set => WriteUInt32Field(FunctionLengthFieldName, value);
    }

    public uint Header
    {
        get => ReadUInt32Field(HeaderFieldName);
        set => WriteUInt32Field(HeaderFieldName, value);
    }
}

internal sealed class MockRuntimeFunctionsBuilder
{
    private const ulong DefaultAllocationRangeStart = 0x0004_0000;
    private const ulong DefaultAllocationRangeEnd = 0x0005_0000;

    internal const uint DefaultFunctionLength = 0x100;

    internal MockMemorySpace.Builder Builder { get; }
    internal Layout<MockRuntimeFunction> RuntimeFunctionLayout { get; }
    internal Layout<MockUnwindInfo> UnwindInfoLayout { get; }
    private readonly MockMemorySpace.BumpAllocator _allocator;

    public MockRuntimeFunctionsBuilder(MockMemorySpace.Builder builder, bool includeEndAddress = true, bool unwindInfoIsFunctionLength = false)
        : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd), includeEndAddress, unwindInfoIsFunctionLength)
    {
    }

    public MockRuntimeFunctionsBuilder(
        MockMemorySpace.Builder builder,
        (ulong Start, ulong End) allocationRange,
        bool includeEndAddress,
        bool unwindInfoIsFunctionLength)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Builder = builder;
        _allocator = Builder.CreateAllocator(allocationRange.Start, allocationRange.End);
        RuntimeFunctionLayout = MockRuntimeFunction.CreateLayout(builder.TargetTestHelpers.Arch, includeEndAddress);
        UnwindInfoLayout = MockUnwindInfo.CreateLayout(builder.TargetTestHelpers.Arch, unwindInfoIsFunctionLength);
    }

    public ulong AddRuntimeFunctions(uint[] runtimeFunctions)
    {
        ArgumentNullException.ThrowIfNull(runtimeFunctions);

        uint numRuntimeFunctions = checked((uint)runtimeFunctions.Length);
        uint runtimeFunctionSize = checked((uint)RuntimeFunctionLayout.Size);
        MockMemorySpace.HeapFragment runtimeFunctionsFragment = AllocateAndAdd(
            checked((numRuntimeFunctions + 1) * runtimeFunctionSize),
            $"RuntimeFunctions[{numRuntimeFunctions}]");

        for (uint i = 0; i < numRuntimeFunctions; i++)
        {
            ulong functionAddress = runtimeFunctionsFragment.Address + i * runtimeFunctionSize;
            MockRuntimeFunction runtimeFunction = RuntimeFunctionLayout.Create(
                runtimeFunctionsFragment.Data.AsMemory(checked((int)(i * runtimeFunctionSize)), RuntimeFunctionLayout.Size),
                functionAddress);

            uint functionLength = i < numRuntimeFunctions - 1
                ? Math.Min(runtimeFunctions[i + 1] - runtimeFunctions[i], DefaultFunctionLength)
                : DefaultFunctionLength;

            runtimeFunction.BeginAddress = runtimeFunctions[i];
            if (HasEndAddress())
            {
                runtimeFunction.EndAddress = runtimeFunctions[i] + functionLength;
            }

            MockUnwindInfo unwindInfo = UnwindInfoLayout.Create(
                AllocateAndAdd((ulong)UnwindInfoLayout.Size, $"UnwindInfo for RuntimeFunction {runtimeFunctions[i]}"));

            if (HasFunctionLength())
            {
                unwindInfo.FunctionLength = functionLength;
            }
            else
            {
                // First 18 bits of the header are function length / (pointer size / 2)
                uint headerBits = (uint)(functionLength / (Builder.TargetTestHelpers.PointerSize / 2));
                if (headerBits > (1 << 18) - 1)
                {
                    throw new InvalidOperationException("Function length is too long.");
                }

                unwindInfo.Header = headerBits;
            }

            runtimeFunction.UnwindData = checked((uint)unwindInfo.Address);
        }

        MockRuntimeFunction sentinel = RuntimeFunctionLayout.Create(
            runtimeFunctionsFragment.Data.AsMemory(checked((int)(numRuntimeFunctions * runtimeFunctionSize)), RuntimeFunctionLayout.Size),
            runtimeFunctionsFragment.Address + numRuntimeFunctions * runtimeFunctionSize);
        sentinel.BeginAddress = uint.MaxValue;

        return runtimeFunctionsFragment.Address;
    }

    private bool HasEndAddress()
        => Array.Exists(RuntimeFunctionLayout.Fields, static field => field.Name == "EndAddress");

    private bool HasFunctionLength()
        => Array.Exists(UnwindInfoLayout.Fields, static field => field.Name == "FunctionLength");

    private MockMemorySpace.HeapFragment AllocateAndAdd(ulong size, string name)
    {
        MockMemorySpace.HeapFragment fragment = _allocator.Allocate(size, name);
        Builder.AddHeapFragment(fragment);
        return fragment;
    }
}
