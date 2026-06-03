// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal class MockMethodDesc : TypedView
{
    private const string ChunkIndexFieldName = "ChunkIndex";
    private const string SlotFieldName = "Slot";
    private const string FlagsFieldName = "Flags";
    private const string Flags3AndTokenRemainderFieldName = "Flags3AndTokenRemainder";
    private const string EntryPointFlagsFieldName = "EntryPointFlags";
    private const string CodeDataFieldName = "CodeData";
    private const string GCCoverageInfoFieldName = "GCCoverageInfo";

    internal static Layout<MockMethodDesc> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("MethodDesc", architecture)
            .AddByteField(ChunkIndexFieldName)
            .AddUInt16Field(SlotFieldName)
            .AddUInt16Field(FlagsFieldName)
            .AddUInt16Field(Flags3AndTokenRemainderFieldName)
            .AddByteField(EntryPointFlagsFieldName)
            .AddPointerField(CodeDataFieldName)
            .AddPointerField(GCCoverageInfoFieldName)
            .Build<MockMethodDesc>();

    internal byte ChunkIndex
    {
        get => ReadByteField(ChunkIndexFieldName);
        set => WriteByteField(ChunkIndexFieldName, value);
    }

    internal ushort Slot
    {
        get => ReadUInt16Field(SlotFieldName);
        set => WriteUInt16Field(SlotFieldName, value);
    }

    internal ushort Flags
    {
        get => ReadUInt16Field(FlagsFieldName);
        set => WriteUInt16Field(FlagsFieldName, value);
    }

    internal ushort Flags3AndTokenRemainder
    {
        get => ReadUInt16Field(Flags3AndTokenRemainderFieldName);
        set => WriteUInt16Field(Flags3AndTokenRemainderFieldName, value);
    }

    internal byte EntryPointFlags
    {
        get => ReadByteField(EntryPointFlagsFieldName);
        set => WriteByteField(EntryPointFlagsFieldName, value);
    }

    internal ulong CodeData
    {
        get => ReadPointerField(CodeDataFieldName);
        set => WritePointerField(CodeDataFieldName, value);
    }

    internal ulong GCCoverageInfo
    {
        get => ReadPointerField(GCCoverageInfoFieldName);
        set => WritePointerField(GCCoverageInfoFieldName, value);
    }

}

internal sealed class MockMethodDescChunk : TypedView
{
    private const string MethodTableFieldName = "MethodTable";
    private const string NextFieldName = "Next";
    private const string SizeFieldName = "Size";
    private const string CountFieldName = "Count";
    private const string FlagsAndTokenRangeFieldName = "FlagsAndTokenRange";

    internal static Layout<MockMethodDescChunk> CreateLayout(MockTarget.Architecture architecture)
    {
        LayoutBuilder builder = new("MethodDescChunk", architecture);
        int pointerSize = architecture.Is64Bit ? sizeof(ulong) : sizeof(uint);

        builder.AddField(MethodTableFieldName, 0, pointerSize);
        builder.AddField(NextFieldName, pointerSize, pointerSize);
        builder.AddField(SizeFieldName, pointerSize * 2, sizeof(byte));
        builder.AddField(CountFieldName, pointerSize * 2 + sizeof(byte), sizeof(byte));
        builder.AddField(FlagsAndTokenRangeFieldName, pointerSize * 2 + sizeof(ushort), sizeof(ushort));
        builder.Size = pointerSize == sizeof(ulong) ? 24 : 12;
        return builder.Build<MockMethodDescChunk>();
    }

    internal ulong MethodTable
    {
        get => ReadPointerField(MethodTableFieldName);
        set => WritePointerField(MethodTableFieldName, value);
    }

    internal ulong Next
    {
        get => ReadPointerField(NextFieldName);
        set => WritePointerField(NextFieldName, value);
    }

    internal byte Size
    {
        get => ReadByteField(SizeFieldName);
        set => WriteByteField(SizeFieldName, value);
    }

    internal byte Count
    {
        get => ReadByteField(CountFieldName);
        set => WriteByteField(CountFieldName, value);
    }

    internal ushort FlagsAndTokenRange
    {
        get => ReadUInt16Field(FlagsAndTokenRangeFieldName);
        set => WriteUInt16Field(FlagsAndTokenRangeFieldName, value);
    }

    internal T GetMethodDescAtChunkIndex<T>(int chunkIndex, Layout<T> layout)
        where T : MockMethodDesc, new()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(chunkIndex);
        ArgumentNullException.ThrowIfNull(layout);

        int methodDescAlignment = Architecture.Is64Bit ? sizeof(ulong) : sizeof(uint);
        int methodDescOffset = checked(Layout.Size + chunkIndex * methodDescAlignment);
        ulong methodDescAddress = Address + (ulong)methodDescOffset;

        return layout.Create(Memory.Slice(methodDescOffset, layout.Size), methodDescAddress);
    }
}

internal sealed class MockInstantiatedMethodDesc : MockMethodDesc
{
    private const string PerInstInfoFieldName = "PerInstInfo";
    private const string NumGenericArgsFieldName = "NumGenericArgs";
    private const string Flags2FieldName = "Flags2";

    internal static Layout<MockInstantiatedMethodDesc> CreateLayout(Layout<MockMethodDesc> baseLayout)
        => new SequentialLayoutBuilder("InstantiatedMethodDesc", baseLayout.Architecture, baseLayout)
            .AddPointerField(PerInstInfoFieldName)
            .AddUInt16Field(NumGenericArgsFieldName)
            .AddUInt16Field(Flags2FieldName)
            .Build<MockInstantiatedMethodDesc>();

    internal ulong PerInstInfo
    {
        get => ReadPointerField(PerInstInfoFieldName);
        set => WritePointerField(PerInstInfoFieldName, value);
    }

    internal ushort NumGenericArgs
    {
        get => ReadUInt16Field(NumGenericArgsFieldName);
        set => WriteUInt16Field(NumGenericArgsFieldName, value);
    }

    internal ushort Flags2
    {
        get => ReadUInt16Field(Flags2FieldName);
        set => WriteUInt16Field(Flags2FieldName, value);
    }

}

internal sealed class MockPerInstInfo : TypedView
{
    internal static Layout<MockPerInstInfo> CreateLayout(MockTarget.Architecture architecture)
        => new("PerInstInfo", architecture, size: 0, []);

    internal ulong this[int index]
    {
        get => ReadPointer(GetPointerSlotSpan(index));
        set => WritePointer(GetPointerSlotSpan(index), value);
    }

    private Span<byte> GetPointerSlotSpan(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        int pointerSize = Architecture.Is64Bit ? sizeof(ulong) : sizeof(uint);
        return Memory.Span.Slice(index * pointerSize, pointerSize);
    }
}

internal class MockStoredSigMethodDesc : MockMethodDesc
{
    private const string SigFieldName = "Sig";
    private const string CSigFieldName = "cSig";
    private const string ExtendedFlagsFieldName = "ExtendedFlags";

    internal static Layout<MockStoredSigMethodDesc> CreateLayout(Layout<MockMethodDesc> baseLayout)
        => new SequentialLayoutBuilder("StoredSigMethodDesc", baseLayout.Architecture, baseLayout)
            .AddPointerField(SigFieldName)
            .AddUInt32Field(CSigFieldName)
            .AddUInt32Field(ExtendedFlagsFieldName)
            .Build<MockStoredSigMethodDesc>();

    internal ulong Sig
    {
        get => ReadPointerField(SigFieldName);
        set => WritePointerField(SigFieldName, value);
    }

    internal uint CSig
    {
        get => ReadUInt32Field(CSigFieldName);
        set => WriteUInt32Field(CSigFieldName, value);
    }

    internal uint ExtendedFlags
    {
        get => ReadUInt32Field(ExtendedFlagsFieldName);
        set => WriteUInt32Field(ExtendedFlagsFieldName, value);
    }
}

internal sealed class MockDynamicMethodDesc : MockStoredSigMethodDesc
{
    private const string MethodNameFieldName = "MethodName";

    internal static Layout<MockDynamicMethodDesc> CreateLayout(Layout<MockStoredSigMethodDesc> baseLayout)
        => new SequentialLayoutBuilder("DynamicMethodDesc", baseLayout.Architecture, baseLayout)
            .AddPointerField(MethodNameFieldName)
            .Build<MockDynamicMethodDesc>();

    internal ulong MethodName
    {
        get => ReadPointerField(MethodNameFieldName);
        set => WritePointerField(MethodNameFieldName, value);
    }
}

internal static partial class MockDescriptors
{
    internal sealed class MockMethodDescriptorsBuilder
    {
        internal const byte TokenRemainderBitCount = 12; /* see METHOD_TOKEN_REMAINDER_BIT_COUNT*/
        private const ulong DefaultAllocationRangeStart = 0x2000_2000;
        private const ulong DefaultAllocationRangeEnd = 0x2000_3000;

        internal RuntimeTypeSystem RTSBuilder { get; }
        internal MockLoaderBuilder LoaderBuilder { get; }
        internal Layout<MockMethodDesc> MethodDescLayout { get; }
        internal Layout<MockMethodDescChunk> MethodDescChunkLayout { get; }
        internal Layout<MockInstantiatedMethodDesc> InstantiatedMethodDescLayout { get; }
        internal Layout<MockPerInstInfo> PerInstInfoLayout { get; }
        internal Layout<MockStoredSigMethodDesc> StoredSigMethodDescLayout { get; }
        internal Layout<MockDynamicMethodDesc> DynamicMethodDescLayout { get; }

        internal ulong MethodDescTokenRemainderBitCount { get; } = TokenRemainderBitCount;
        internal uint NonVtableSlotSize => (uint)TargetTestHelpers.PointerSize;
        internal uint MethodImplSize => (uint)(TargetTestHelpers.PointerSize * 2);
        internal uint NativeCodeSlotSize => (uint)TargetTestHelpers.PointerSize;
        internal uint AsyncMethodDataSize => (uint)(TargetTestHelpers.PointerSize * 2);
        internal uint ArrayMethodDescSize => (uint)StoredSigMethodDescLayout.Size;
        internal uint FCallMethodDescSize => (uint)(MethodDescLayout.Size + TargetTestHelpers.PointerSize);
        internal uint PInvokeMethodDescSize => (uint)(MethodDescLayout.Size + TargetTestHelpers.PointerSize);
        internal uint EEImplMethodDescSize => (uint)StoredSigMethodDescLayout.Size;
        internal uint CLRToCOMCallMethodDescSize => (uint)(MethodDescLayout.Size + TargetTestHelpers.PointerSize);

        private readonly MockMemorySpace.BumpAllocator _allocator;

        internal TargetTestHelpers TargetTestHelpers => RTSBuilder.Builder.TargetTestHelpers;
        internal MockMemorySpace.Builder Builder => RTSBuilder.Builder;
        internal uint MethodDescAlignment => RuntimeTypeSystem.GetMethodDescAlignment(TargetTestHelpers);

        internal MockMethodDescriptorsBuilder(RuntimeTypeSystem rtsBuilder, MockLoaderBuilder loaderBuilder)
            : this(rtsBuilder, loaderBuilder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
        {
        }

        internal MockMethodDescriptorsBuilder(RuntimeTypeSystem rtsBuilder, MockLoaderBuilder loaderBuilder, (ulong Start, ulong End) allocationRange)
        {
            RTSBuilder = rtsBuilder;
            LoaderBuilder = loaderBuilder;
            _allocator = Builder.CreateAllocator(allocationRange.Start, allocationRange.End);

            MethodDescLayout = MockMethodDesc.CreateLayout(TargetTestHelpers.Arch);
            MethodDescChunkLayout = MockMethodDescChunk.CreateLayout(TargetTestHelpers.Arch);
            InstantiatedMethodDescLayout = MockInstantiatedMethodDesc.CreateLayout(MethodDescLayout);
            PerInstInfoLayout = MockPerInstInfo.CreateLayout(TargetTestHelpers.Arch);
            StoredSigMethodDescLayout = MockStoredSigMethodDesc.CreateLayout(MethodDescLayout);
            DynamicMethodDescLayout = MockDynamicMethodDesc.CreateLayout(StoredSigMethodDescLayout);
        }

        internal MockMethodDescChunk AddMethodDescChunk(string name, byte size)
        {
            uint totalAllocSize = (uint)MethodDescChunkLayout.Size;
            totalAllocSize += (uint)(size * MethodDescAlignment);

            MockMemorySpace.HeapFragment fragment = _allocator.Allocate(totalAllocSize, $"MethodDescChunk {name}");

            MockMethodDescChunk chunk = MethodDescChunkLayout.Create(fragment.Data.AsMemory(), fragment.Address);
            return chunk;
        }

        internal MockPerInstInfo AddPerInstInfo(ulong[] typeArgs)
        {
            ArgumentNullException.ThrowIfNull(typeArgs);

            MockMemorySpace.HeapFragment fragment = _allocator.Allocate((ulong)(typeArgs.Length * TargetTestHelpers.PointerSize), "PerInstInfo");

            MockPerInstInfo perInstInfo = PerInstInfoLayout.Create(fragment.Data.AsMemory(), fragment.Address);
            for (int i = 0; i < typeArgs.Length; i++)
            {
                perInstInfo[i] = typeArgs[i];
            }

            return perInstInfo;
        }

    }
}
