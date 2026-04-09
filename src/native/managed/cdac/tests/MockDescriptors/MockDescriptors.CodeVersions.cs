// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal sealed class MockMethodDescVersioningState : TypedView
{
    private const string NativeCodeVersionNodeFieldName = "NativeCodeVersionNode";
    private const string FlagsFieldName = "Flags";

    public const byte IsDefaultVersionActiveChildFlag = 0x4;

    public static Layout<MockMethodDescVersioningState> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("MethodDescVersioningState", architecture)
            .AddPointerField(NativeCodeVersionNodeFieldName)
            .AddField(FlagsFieldName, sizeof(byte))
            .Build<MockMethodDescVersioningState>();

    public ulong NativeCodeVersionNode
    {
        get => ReadPointerField(NativeCodeVersionNodeFieldName);
        set => WritePointerField(NativeCodeVersionNodeFieldName, value);
    }

    public byte Flags
    {
        get => ReadByteField(FlagsFieldName);
        set => WriteByteField(FlagsFieldName, value);
    }
}

internal sealed class MockNativeCodeVersionNode : TypedView
{
    private const string NextFieldName = "Next";
    private const string MethodDescFieldName = "MethodDesc";
    private const string NativeCodeFieldName = "NativeCode";
    private const string FlagsFieldName = "Flags";
    private const string ILVersionIdFieldName = "ILVersionId";
    private const string GCCoverageInfoFieldName = "GCCoverageInfo";
    private const string OptimizationTierFieldName = "OptimizationTier";

    public const uint IsActiveChildFlag = 0x1;

    public static Layout<MockNativeCodeVersionNode> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("NativeCodeVersionNode", architecture)
            .AddPointerField(NextFieldName)
            .AddPointerField(MethodDescFieldName)
            .AddPointerField(NativeCodeFieldName)
            .AddUInt32Field(FlagsFieldName)
            .AddNUIntField(ILVersionIdFieldName)
            .AddPointerField(GCCoverageInfoFieldName)
            .AddUInt32Field(OptimizationTierFieldName)
            .Build<MockNativeCodeVersionNode>();

    public ulong Next
    {
        get => ReadPointerField(NextFieldName);
        set => WritePointerField(NextFieldName, value);
    }

    public ulong MethodDesc
    {
        get => ReadPointerField(MethodDescFieldName);
        set => WritePointerField(MethodDescFieldName, value);
    }

    public ulong NativeCode
    {
        get => ReadPointerField(NativeCodeFieldName);
        set => WritePointerField(NativeCodeFieldName, value);
    }

    public uint Flags
    {
        get => ReadUInt32Field(FlagsFieldName);
        set => WriteUInt32Field(FlagsFieldName, value);
    }

    public ulong ILVersionId
    {
        get => ReadPointerField(ILVersionIdFieldName);
        set => WritePointerField(ILVersionIdFieldName, value);
    }

    public ulong GCCoverageInfo
    {
        get => ReadPointerField(GCCoverageInfoFieldName);
        set => WritePointerField(GCCoverageInfoFieldName, value);
    }

    public uint OptimizationTier
    {
        get => ReadUInt32Field(OptimizationTierFieldName);
        set => WriteUInt32Field(OptimizationTierFieldName, value);
    }
}

internal sealed class MockILCodeVersioningState : TypedView
{
    private const string FirstVersionNodeFieldName = "FirstVersionNode";
    private const string ActiveVersionMethodDefFieldName = "ActiveVersionMethodDef";
    private const string ActiveVersionModuleFieldName = "ActiveVersionModule";
    private const string ActiveVersionKindFieldName = "ActiveVersionKind";
    private const string ActiveVersionNodeFieldName = "ActiveVersionNode";

    public static Layout<MockILCodeVersioningState> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("ILCodeVersioningState", architecture)
            .AddPointerField(FirstVersionNodeFieldName)
            .AddUInt32Field(ActiveVersionMethodDefFieldName)
            .AddPointerField(ActiveVersionModuleFieldName)
            .AddUInt32Field(ActiveVersionKindFieldName)
            .AddPointerField(ActiveVersionNodeFieldName)
            .Build<MockILCodeVersioningState>();

    public ulong FirstVersionNode
    {
        get => ReadPointerField(FirstVersionNodeFieldName);
        set => WritePointerField(FirstVersionNodeFieldName, value);
    }

    public uint ActiveVersionMethodDef
    {
        get => ReadUInt32Field(ActiveVersionMethodDefFieldName);
        set => WriteUInt32Field(ActiveVersionMethodDefFieldName, value);
    }

    public ulong ActiveVersionModule
    {
        get => ReadPointerField(ActiveVersionModuleFieldName);
        set => WritePointerField(ActiveVersionModuleFieldName, value);
    }

    public uint ActiveVersionKind
    {
        get => ReadUInt32Field(ActiveVersionKindFieldName);
        set => WriteUInt32Field(ActiveVersionKindFieldName, value);
    }

    public ulong ActiveVersionNode
    {
        get => ReadPointerField(ActiveVersionNodeFieldName);
        set => WritePointerField(ActiveVersionNodeFieldName, value);
    }
}

internal sealed class MockILCodeVersionNode : TypedView
{
    private const string VersionIdFieldName = "VersionId";
    private const string NextFieldName = "Next";
    private const string RejitStateFieldName = "RejitState";
    private const string ILAddressFieldName = "ILAddress";

    public static Layout<MockILCodeVersionNode> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("ILCodeVersionNode", architecture)
            .AddNUIntField(VersionIdFieldName)
            .AddPointerField(NextFieldName)
            .AddUInt32Field(RejitStateFieldName)
            .AddPointerField(ILAddressFieldName)
            .Build<MockILCodeVersionNode>();

    public ulong VersionId
    {
        get => ReadPointerField(VersionIdFieldName);
        set => WritePointerField(VersionIdFieldName, value);
    }

    public ulong Next
    {
        get => ReadPointerField(NextFieldName);
        set => WritePointerField(NextFieldName, value);
    }

    public uint RejitState
    {
        get => ReadUInt32Field(RejitStateFieldName);
        set => WriteUInt32Field(RejitStateFieldName, value);
    }
}

internal sealed class MockGCCoverageInfo : TypedView
{
    private const string DummyFieldName = "DummyField";
    private const string SavedCodeFieldName = "SavedCode";

    public static Layout<MockGCCoverageInfo> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("GCCoverageInfo", architecture)
            .AddPointerField(DummyFieldName)
            .AddPointerField(SavedCodeFieldName)
            .Build<MockGCCoverageInfo>();

    public static int GetSavedCodeOffset(MockTarget.Architecture architecture)
        => CreateLayout(architecture).GetField(SavedCodeFieldName).Offset;
}

internal sealed class MockCodeVersionsBuilder
{
    private const ulong DefaultAllocationRangeStart = 0x000f_c000;
    private const ulong DefaultAllocationRangeEnd = 0x00010_0000;

    internal MockMemorySpace.Builder Builder { get; }
    internal Layout<MockMethodDescVersioningState> MethodDescVersioningStateLayout { get; }
    internal Layout<MockNativeCodeVersionNode> NativeCodeVersionNodeLayout { get; }
    internal Layout<MockILCodeVersioningState> ILCodeVersioningStateLayout { get; }
    internal Layout<MockILCodeVersionNode> ILCodeVersionNodeLayout { get; }
    internal Layout<MockGCCoverageInfo> GCCoverageInfoLayout { get; }

    private readonly MockMemorySpace.BumpAllocator _codeVersionsAllocator;

    public MockCodeVersionsBuilder(MockTarget.Architecture arch)
        : this(new MockMemorySpace.Builder(new TargetTestHelpers(arch)), (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
    {
    }

    public MockCodeVersionsBuilder(MockMemorySpace.Builder builder)
        : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
    {
    }

    public MockCodeVersionsBuilder(MockTarget.Architecture arch, (ulong Start, ulong End) allocationRange)
        : this(new MockMemorySpace.Builder(new TargetTestHelpers(arch)), allocationRange)
    {
    }

    public MockCodeVersionsBuilder(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Builder = builder;
        _codeVersionsAllocator = Builder.CreateAllocator(allocationRange.Start, allocationRange.End);

        MockTarget.Architecture architecture = Builder.TargetTestHelpers.Arch;
        MethodDescVersioningStateLayout = MockMethodDescVersioningState.CreateLayout(architecture);
        NativeCodeVersionNodeLayout = MockNativeCodeVersionNode.CreateLayout(architecture);
        ILCodeVersioningStateLayout = MockILCodeVersioningState.CreateLayout(architecture);
        ILCodeVersionNodeLayout = MockILCodeVersionNode.CreateLayout(architecture);
        GCCoverageInfoLayout = MockGCCoverageInfo.CreateLayout(architecture);
    }

    public MockMethodDescVersioningState AddMethodDescVersioningState()
        => MethodDescVersioningStateLayout.Create(
            AllocateAndAdd((ulong)MethodDescVersioningStateLayout.Size, "MethodDescVersioningState"));

    public MockNativeCodeVersionNode AddNativeCodeVersionNode()
    {
        MockNativeCodeVersionNode node = NativeCodeVersionNodeLayout.Create(
            AllocateAndAdd((ulong)NativeCodeVersionNodeLayout.Size, "NativeCodeVersionNode"));
        node.OptimizationTier = 0xFFFFFFFFu;
        return node;
    }

    public void FillNativeCodeVersionNode(
        MockNativeCodeVersionNode dest,
        ulong methodDesc,
        ulong nativeCode,
        ulong next,
        bool isActive,
        ulong ilVersionId,
        ulong gcCoverageInfo = 0,
        uint? optimizationTier = null)
    {
        ArgumentNullException.ThrowIfNull(dest);

        dest.Next = next;
        dest.MethodDesc = methodDesc;
        dest.NativeCode = nativeCode;
        dest.Flags = isActive ? MockNativeCodeVersionNode.IsActiveChildFlag : 0;
        dest.ILVersionId = ilVersionId;
        dest.GCCoverageInfo = gcCoverageInfo;
        dest.OptimizationTier = optimizationTier ?? 0xFFFFFFFFu;
    }

    public (MockNativeCodeVersionNode First, MockNativeCodeVersionNode? Active) AddNativeCodeVersionNodesForMethod(ulong methodDesc, int count, int activeIndex, ulong activeNativeCode, ulong ilVersion, MockNativeCodeVersionNode? firstNode = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        MockNativeCodeVersionNode? activeVersionNode = null;
        ulong next = firstNode?.Address ?? 0;
        MockNativeCodeVersionNode? currentFirstNode = firstNode;
        for (int i = count - 1; i >= 0; i--)
        {
            MockNativeCodeVersionNode node = AddNativeCodeVersionNode();
            bool isActive = i == activeIndex;
            ulong nativeCode = isActive ? activeNativeCode : 0;
            node.Next = next;
            node.MethodDesc = methodDesc;
            node.NativeCode = nativeCode;
            node.Flags = isActive ? MockNativeCodeVersionNode.IsActiveChildFlag : 0;
            node.ILVersionId = ilVersion;
            node.GCCoverageInfo = 0;
            node.OptimizationTier = 0xFFFFFFFFu;
            next = node.Address;
            currentFirstNode = node;
            if (isActive)
            {
                activeVersionNode = node;
            }
        }

        return (currentFirstNode!, activeVersionNode);
    }

    public MockILCodeVersioningState AddILCodeVersioningState()
        => ILCodeVersioningStateLayout.Create(
            AllocateAndAdd((ulong)ILCodeVersioningStateLayout.Size, "ILCodeVersioningState"));

    public MockILCodeVersionNode AddILCodeVersionNode(ulong versionId, uint rejitFlags)
    {
        MockILCodeVersionNode node = ILCodeVersionNodeLayout.Create(
            AllocateAndAdd((ulong)ILCodeVersionNodeLayout.Size, "ILCodeVersionNode"));
        node.VersionId = versionId;
        node.RejitState = rejitFlags;
        node.Next = 0;

        return node;
    }

    private MockMemorySpace.HeapFragment AllocateAndAdd(ulong size, string name)
    {
        MockMemorySpace.HeapFragment fragment = _codeVersionsAllocator.Allocate(size, name);
        Builder.AddHeapFragment(fragment);
        return fragment;
    }
}
