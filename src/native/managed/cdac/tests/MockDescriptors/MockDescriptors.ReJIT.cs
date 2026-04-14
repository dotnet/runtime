// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal sealed class MockProfControlBlock : TypedView
{
    private const string GlobalEventMaskFieldName = "GlobalEventMask";
    private const string RejitOnAttachEnabledFieldName = "RejitOnAttachEnabled";

    public static Layout<MockProfControlBlock> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("ProfControlBlock", architecture)
            .AddUInt64Field(GlobalEventMaskFieldName)
            .AddField(RejitOnAttachEnabledFieldName, sizeof(byte))
            .Build<MockProfControlBlock>();

    public ulong GlobalEventMask
    {
        get => ReadUInt64Field(GlobalEventMaskFieldName);
        set => WriteUInt64Field(GlobalEventMaskFieldName, value);
    }

    public byte RejitOnAttachEnabled
    {
        get => ReadByteField(RejitOnAttachEnabledFieldName);
        set => WriteByteField(RejitOnAttachEnabledFieldName, value);
    }
}

internal sealed class MockReJITBuilder
{
    private const ulong DefaultAllocationRangeStart = 0x0010_1000;
    private const ulong DefaultAllocationRangeEnd = 0x00011_0000;
    private const string ProfilerControlBlockGlobalName = "ProfilerControlBlock";

    // see src/coreclr/vm/codeversion.h
    [Flags]
    public enum RejitFlags : uint
    {
        kStateRequested = 0x00000000,
        kStateActive = 0x00000002,
        kStateMask = 0x0000000F
    }

    internal MockMemorySpace.Builder Builder { get; }
    internal Layout<MockProfControlBlock> ProfControlBlockLayout { get; }
    public ulong ProfilerControlBlockGlobalAddress { get; }

    private readonly MockCodeVersionsBuilder _codeVersions;
    private readonly MockMemorySpace.BumpAllocator _rejitAllocator;

    public MockReJITBuilder(MockTarget.Architecture arch, bool rejitOnAttachEnabled = true)
        : this(new MockMemorySpace.Builder(new TargetTestHelpers(arch)), (DefaultAllocationRangeStart, DefaultAllocationRangeEnd), rejitOnAttachEnabled)
    {
    }

    public MockReJITBuilder(MockMemorySpace.Builder builder, bool rejitOnAttachEnabled = true)
        : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd), rejitOnAttachEnabled)
    {
    }

    public MockReJITBuilder(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange, bool rejitOnAttachEnabled = true)
    {
        Builder = builder;
        _rejitAllocator = Builder.CreateAllocator(allocationRange.Start, allocationRange.End);

        _codeVersions = new MockCodeVersionsBuilder(Builder);
        ProfControlBlockLayout = MockProfControlBlock.CreateLayout(builder.TargetTestHelpers.Arch);
        ProfilerControlBlockGlobalAddress = AddProfControlBlock(rejitOnAttachEnabled);
    }

    internal Layout<MockMethodDescVersioningState> MethodDescVersioningStateLayout => _codeVersions.MethodDescVersioningStateLayout;
    internal Layout<MockNativeCodeVersionNode> NativeCodeVersionNodeLayout => _codeVersions.NativeCodeVersionNodeLayout;
    internal Layout<MockILCodeVersioningState> ILCodeVersioningStateLayout => _codeVersions.ILCodeVersioningStateLayout;
    internal Layout<MockILCodeVersionNode> ILCodeVersionNodeLayout => _codeVersions.ILCodeVersionNodeLayout;
    internal Layout<MockGCCoverageInfo> GCCoverageInfoLayout => _codeVersions.GCCoverageInfoLayout;

    public MockILCodeVersionNode AddExplicitILCodeVersionNode(ulong rejitId, RejitFlags rejitFlags)
        => _codeVersions.AddILCodeVersionNode(rejitId, (uint)rejitFlags);

    private ulong AddProfControlBlock(bool rejitOnAttachEnabled)
    {
        MockMemorySpace.HeapFragment fragment = _rejitAllocator.Allocate((ulong)ProfControlBlockLayout.Size, "ProfControlBlock");
        MockProfControlBlock profControlBlock = ProfControlBlockLayout.Create(fragment);
        profControlBlock.GlobalEventMask = 0;
        profControlBlock.RejitOnAttachEnabled = rejitOnAttachEnabled ? (byte)1 : (byte)0;
        return fragment.Address;
    }
}
