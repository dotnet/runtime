// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal sealed class MockMethodDescVersioningState : TypedView
{
    private const string NativeCodeVersionNodeFieldName = "NativeCodeVersionNode";
    private const string FlagsFieldName = "Flags";

    internal const byte IsDefaultVersionActiveChildFlag = 0x4;

    internal static Layout<MockMethodDescVersioningState> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("MethodDescVersioningState", architecture)
            .AddPointerField(NativeCodeVersionNodeFieldName)
            .AddField(FlagsFieldName, sizeof(byte))
            .Build<MockMethodDescVersioningState>();

    internal ulong NativeCodeVersionNode
    {
        get => ReadPointerField(NativeCodeVersionNodeFieldName);
        set => WritePointerField(NativeCodeVersionNodeFieldName, value);
    }

    internal byte Flags
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

    internal const uint IsActiveChildFlag = 0x1;

    internal static Layout<MockNativeCodeVersionNode> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("NativeCodeVersionNode", architecture)
            .AddPointerField(NextFieldName)
            .AddPointerField(MethodDescFieldName)
            .AddPointerField(NativeCodeFieldName)
            .AddUInt32Field(FlagsFieldName)
            .AddNUIntField(ILVersionIdFieldName)
            .AddPointerField(GCCoverageInfoFieldName)
            .AddUInt32Field(OptimizationTierFieldName)
            .Build<MockNativeCodeVersionNode>();

    internal ulong Next
    {
        get => ReadPointerField(NextFieldName);
        set => WritePointerField(NextFieldName, value);
    }

    internal ulong MethodDesc
    {
        get => ReadPointerField(MethodDescFieldName);
        set => WritePointerField(MethodDescFieldName, value);
    }

    internal ulong NativeCode
    {
        get => ReadPointerField(NativeCodeFieldName);
        set => WritePointerField(NativeCodeFieldName, value);
    }

    internal uint Flags
    {
        get => ReadUInt32Field(FlagsFieldName);
        set => WriteUInt32Field(FlagsFieldName, value);
    }

    internal ulong ILVersionId
    {
        get => ReadPointerField(ILVersionIdFieldName);
        set => WritePointerField(ILVersionIdFieldName, value);
    }

    internal ulong GCCoverageInfo
    {
        get => ReadPointerField(GCCoverageInfoFieldName);
        set => WritePointerField(GCCoverageInfoFieldName, value);
    }

    internal uint OptimizationTier
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

    internal static Layout<MockILCodeVersioningState> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("ILCodeVersioningState", architecture)
            .AddPointerField(FirstVersionNodeFieldName)
            .AddUInt32Field(ActiveVersionMethodDefFieldName)
            .AddPointerField(ActiveVersionModuleFieldName)
            .AddUInt32Field(ActiveVersionKindFieldName)
            .AddPointerField(ActiveVersionNodeFieldName)
            .Build<MockILCodeVersioningState>();

    internal ulong FirstVersionNode
    {
        get => ReadPointerField(FirstVersionNodeFieldName);
        set => WritePointerField(FirstVersionNodeFieldName, value);
    }

    internal uint ActiveVersionMethodDef
    {
        get => ReadUInt32Field(ActiveVersionMethodDefFieldName);
        set => WriteUInt32Field(ActiveVersionMethodDefFieldName, value);
    }

    internal ulong ActiveVersionModule
    {
        get => ReadPointerField(ActiveVersionModuleFieldName);
        set => WritePointerField(ActiveVersionModuleFieldName, value);
    }

    internal uint ActiveVersionKind
    {
        get => ReadUInt32Field(ActiveVersionKindFieldName);
        set => WriteUInt32Field(ActiveVersionKindFieldName, value);
    }

    internal ulong ActiveVersionNode
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
    private const string DeoptimizedFieldName = "Deoptimized";

    internal static Layout<MockILCodeVersionNode> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("ILCodeVersionNode", architecture)
            .AddNUIntField(VersionIdFieldName)
            .AddPointerField(NextFieldName)
            .AddUInt32Field(RejitStateFieldName)
            .AddPointerField(ILAddressFieldName)
            .AddUInt32Field(DeoptimizedFieldName)
            .Build<MockILCodeVersionNode>();

    internal ulong VersionId
    {
        get => ReadPointerField(VersionIdFieldName);
        set => WritePointerField(VersionIdFieldName, value);
    }

    internal ulong Next
    {
        get => ReadPointerField(NextFieldName);
        set => WritePointerField(NextFieldName, value);
    }

    internal uint RejitState
    {
        get => ReadUInt32Field(RejitStateFieldName);
        set => WriteUInt32Field(RejitStateFieldName, value);
    }

    internal uint Deoptimized
    {
        get => ReadUInt32Field(DeoptimizedFieldName);
        set => WriteUInt32Field(DeoptimizedFieldName, value);
    }
}

internal sealed class MockGCCoverageInfo : TypedView
{
    private const string DummyFieldName = "DummyField";
    private const string SavedCodeFieldName = "SavedCode";

    internal static Layout<MockGCCoverageInfo> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("GCCoverageInfo", architecture)
            .AddPointerField(DummyFieldName)
            .AddPointerField(SavedCodeFieldName)
            .Build<MockGCCoverageInfo>();

    internal static int GetSavedCodeOffset(MockTarget.Architecture architecture)
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

    internal MockCodeVersionsBuilder(MockTarget.Architecture arch)
        : this(new MockMemorySpace.Builder(new TargetTestHelpers(arch)), (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
    {
    }

    internal MockCodeVersionsBuilder(MockMemorySpace.Builder builder)
        : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
    {
    }

    internal MockCodeVersionsBuilder(MockTarget.Architecture arch, (ulong Start, ulong End) allocationRange)
        : this(new MockMemorySpace.Builder(new TargetTestHelpers(arch)), allocationRange)
    {
    }

    internal MockCodeVersionsBuilder(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange)
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

    internal MockMethodDescVersioningState AddMethodDescVersioningState()
        => MethodDescVersioningStateLayout.Create(
            _codeVersionsAllocator.Allocate((ulong)MethodDescVersioningStateLayout.Size, "MethodDescVersioningState"));

    internal MockNativeCodeVersionNode AddNativeCodeVersionNode()
    {
        MockNativeCodeVersionNode node = NativeCodeVersionNodeLayout.Create(
            _codeVersionsAllocator.Allocate((ulong)NativeCodeVersionNodeLayout.Size, "NativeCodeVersionNode"));
        node.OptimizationTier = 0xFFFFFFFFu;
        return node;
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Kept as instance method for symmetry with other builder methods.")]
    internal void FillNativeCodeVersionNode(
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

    internal (MockNativeCodeVersionNode First, MockNativeCodeVersionNode? Active) AddNativeCodeVersionNodesForMethod(ulong methodDesc, int count, int activeIndex, ulong activeNativeCode, ulong ilVersion, MockNativeCodeVersionNode? firstNode = null)
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

    internal MockILCodeVersioningState AddILCodeVersioningState()
        => ILCodeVersioningStateLayout.Create(
            _codeVersionsAllocator.Allocate((ulong)ILCodeVersioningStateLayout.Size, "ILCodeVersioningState"));

    internal MockILCodeVersionNode AddILCodeVersionNode(ulong versionId, uint rejitFlags, bool deoptimized = false)
    {
        MockILCodeVersionNode node = ILCodeVersionNodeLayout.Create(
            _codeVersionsAllocator.Allocate((ulong)ILCodeVersionNodeLayout.Size, "ILCodeVersionNode"));
        node.VersionId = versionId;
        node.RejitState = rejitFlags;
        node.Deoptimized = deoptimized ? 1u : 0u;
        node.Next = 0;

        return node;
    }
}
