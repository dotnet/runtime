// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal sealed class MockLoaderHeap : TypedView
{
    private const string FirstBlockFieldName = "FirstBlock";

    public static Layout<MockLoaderHeap> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("LoaderHeap", architecture)
            .AddPointerField(FirstBlockFieldName)
            .Build<MockLoaderHeap>();

    public ulong FirstBlock
    {
        get => ReadPointerField(FirstBlockFieldName);
        set => WritePointerField(FirstBlockFieldName, value);
    }
}

internal sealed class MockLoaderHeapBlock : TypedView
{
    private const string NextFieldName = "Next";
    private const string VirtualAddressFieldName = "VirtualAddress";
    private const string VirtualSizeFieldName = "VirtualSize";

    public static Layout<MockLoaderHeapBlock> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("LoaderHeapBlock", architecture)
            .AddPointerField(NextFieldName)
            .AddPointerField(VirtualAddressFieldName)
            .AddNUIntField(VirtualSizeFieldName)
            .Build<MockLoaderHeapBlock>();

    public ulong Next
    {
        get => ReadPointerField(NextFieldName);
        set => WritePointerField(NextFieldName, value);
    }

    public ulong VirtualAddress
    {
        get => ReadPointerField(VirtualAddressFieldName);
        set => WritePointerField(VirtualAddressFieldName, value);
    }

    public ulong VirtualSize
    {
        get => ReadPointerField(VirtualSizeFieldName);
        set => WritePointerField(VirtualSizeFieldName, value);
    }
}

internal sealed class MockLoaderModule : TypedView
{
    private const string AssemblyFieldName = "Assembly";
    private const string PEAssemblyFieldName = "PEAssembly";
    private const string BaseFieldName = "Base";
    private const string FlagsFieldName = "Flags";
    private const string LoaderAllocatorFieldName = "LoaderAllocator";
    private const string DynamicMetadataFieldName = "DynamicMetadata";
    private const string SimpleNameFieldName = "SimpleName";
    private const string PathFieldName = "Path";
    private const string FileNameFieldName = "FileName";
    private const string ReadyToRunInfoFieldName = "ReadyToRunInfo";
    private const string GrowableSymbolStreamFieldName = "GrowableSymbolStream";
    private const string AvailableTypeParamsFieldName = "AvailableTypeParams";
    private const string InstMethodHashTableFieldName = "InstMethodHashTable";
    private const string FieldDefToDescMapFieldName = "FieldDefToDescMap";
    private const string ManifestModuleReferencesMapFieldName = "ManifestModuleReferencesMap";
    private const string MemberRefToDescMapFieldName = "MemberRefToDescMap";
    private const string MethodDefToDescMapFieldName = "MethodDefToDescMap";
    private const string TypeDefToMethodTableMapFieldName = "TypeDefToMethodTableMap";
    private const string TypeRefToMethodTableMapFieldName = "TypeRefToMethodTableMap";
    private const string MethodDefToILCodeVersioningStateMapFieldName = "MethodDefToILCodeVersioningStateMap";
    private const string DynamicILBlobTableFieldName = "DynamicILBlobTable";

    public static Layout<MockLoaderModule> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("Module", architecture)
            .AddPointerField(AssemblyFieldName)
            .AddPointerField(PEAssemblyFieldName)
            .AddPointerField(BaseFieldName)
            .AddUInt32Field(FlagsFieldName)
            .AddPointerField(LoaderAllocatorFieldName)
            .AddPointerField(DynamicMetadataFieldName)
            .AddPointerField(SimpleNameFieldName)
            .AddPointerField(PathFieldName)
            .AddPointerField(FileNameFieldName)
            .AddPointerField(ReadyToRunInfoFieldName)
            .AddPointerField(GrowableSymbolStreamFieldName)
            .AddPointerField(AvailableTypeParamsFieldName)
            .AddPointerField(InstMethodHashTableFieldName)
            .AddPointerField(FieldDefToDescMapFieldName)
            .AddPointerField(ManifestModuleReferencesMapFieldName)
            .AddPointerField(MemberRefToDescMapFieldName)
            .AddPointerField(MethodDefToDescMapFieldName)
            .AddPointerField(TypeDefToMethodTableMapFieldName)
            .AddPointerField(TypeRefToMethodTableMapFieldName)
            .AddPointerField(MethodDefToILCodeVersioningStateMapFieldName)
            .AddPointerField(DynamicILBlobTableFieldName)
            .Build<MockLoaderModule>();

    public ulong Assembly
    {
        get => ReadPointerField(AssemblyFieldName);
        set => WritePointerField(AssemblyFieldName, value);
    }

    public ulong PEAssembly
    {
        get => ReadPointerField(PEAssemblyFieldName);
        set => WritePointerField(PEAssemblyFieldName, value);
    }

    public ulong SimpleName
    {
        get => ReadPointerField(SimpleNameFieldName);
        set => WritePointerField(SimpleNameFieldName, value);
    }

    public ulong Path
    {
        get => ReadPointerField(PathFieldName);
        set => WritePointerField(PathFieldName, value);
    }

    public ulong FileName
    {
        get => ReadPointerField(FileNameFieldName);
        set => WritePointerField(FileNameFieldName, value);
    }

    public uint Flags
    {
        get => ReadUInt32Field(FlagsFieldName);
        set => WriteUInt32Field(FlagsFieldName, value);
    }

    public ulong ReadyToRunInfo
    {
        get => ReadPointerField(ReadyToRunInfoFieldName);
        set => WritePointerField(ReadyToRunInfoFieldName, value);
    }
}

internal sealed class MockLoaderAssembly : TypedView
{
    private const string ModuleFieldName = "Module";
    private const string IsCollectibleFieldName = "IsCollectible";
    private const string IsDynamicFieldName = "IsDynamic";
    private const string ErrorFieldName = "Error";
    private const string NotifyFlagsFieldName = "NotifyFlags";
    private const string IsLoadedFieldName = "IsLoaded";

    public static Layout<MockLoaderAssembly> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("Assembly", architecture)
            .AddPointerField(ModuleFieldName)
            .AddField(IsCollectibleFieldName, sizeof(byte))
            .AddField(IsDynamicFieldName, sizeof(byte))
            .AddPointerField(ErrorFieldName)
            .AddUInt32Field(NotifyFlagsFieldName)
            .AddField(IsLoadedFieldName, sizeof(byte))
            .Build<MockLoaderAssembly>();

    public ulong Module
    {
        get => ReadPointerField(ModuleFieldName);
        set => WritePointerField(ModuleFieldName, value);
    }
}

internal sealed class MockEEConfig : TypedView
{
    private const string ModifiableAssembliesFieldName = "ModifiableAssemblies";

    public static Layout<MockEEConfig> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("EEConfig", architecture)
            .AddUInt32Field(ModifiableAssembliesFieldName)
            .Build<MockEEConfig>();

    public uint ModifiableAssemblies
    {
        get => ReadUInt32Field(ModifiableAssembliesFieldName);
        set => WriteUInt32Field(ModifiableAssembliesFieldName, value);
    }
}

internal sealed class MockLoaderBuilder
{
    private const ulong DefaultAllocationRangeStart = 0x0001_0000;
    private const ulong DefaultAllocationRangeEnd = 0x0002_0000;

    internal MockMemorySpace.Builder Builder { get; }
    internal Layout<MockLoaderModule> ModuleLayout { get; }
    internal Layout<MockLoaderAssembly> AssemblyLayout { get; }
    internal Layout<MockEEConfig> EEConfigLayout { get; }
    internal Layout<MockLoaderHeap> LoaderHeapLayout { get; }
    internal Layout<MockLoaderHeapBlock> LoaderHeapBlockLayout { get; }

    private readonly MockMemorySpace.BumpAllocator _allocator;

    public MockLoaderBuilder(MockMemorySpace.Builder builder)
        : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
    {
    }

    public MockLoaderBuilder(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Builder = builder;
        _allocator = Builder.CreateAllocator(allocationRange.Start, allocationRange.End);

        ModuleLayout = MockLoaderModule.CreateLayout(builder.TargetTestHelpers.Arch);
        AssemblyLayout = MockLoaderAssembly.CreateLayout(builder.TargetTestHelpers.Arch);
        EEConfigLayout = MockEEConfig.CreateLayout(builder.TargetTestHelpers.Arch);
        LoaderHeapLayout = MockLoaderHeap.CreateLayout(builder.TargetTestHelpers.Arch);
        LoaderHeapBlockLayout = MockLoaderHeapBlock.CreateLayout(builder.TargetTestHelpers.Arch);
    }

    internal MockLoaderHeap AddLoaderHeap(ulong firstBlockAddress = 0)
    {
        MockLoaderHeap heap = LoaderHeapLayout.Create(_allocator.Allocate((ulong)LoaderHeapLayout.Size, "LoaderHeap"));
        heap.FirstBlock = firstBlockAddress;
        return heap;
    }

    internal MockLoaderHeapBlock AddLoaderHeapBlock(ulong virtualAddress, ulong virtualSize, ulong nextBlockAddress = 0)
    {
        MockLoaderHeapBlock block = LoaderHeapBlockLayout.Create(_allocator.Allocate((ulong)LoaderHeapBlockLayout.Size, "LoaderHeapBlock"));
        block.VirtualAddress = virtualAddress;
        block.VirtualSize = virtualSize;
        block.Next = nextBlockAddress;
        return block;
    }

    internal MockLoaderModule AddModule(
        string? path = null,
        string? fileName = null,
        string? simpleName = null,
        byte[]? simpleNameBytes = null,
        uint flags = 0)
    {
        MockLoaderModule module = ModuleLayout.Create(_allocator.Allocate((ulong)ModuleLayout.Size, "Module"));

        if (flags != 0)
        {
            module.Flags = flags;
        }

        byte[]? rawSimpleName = simpleName is not null ? Encoding.UTF8.GetBytes(simpleName) : simpleNameBytes;
        if (rawSimpleName is not null)
        {
            module.SimpleName = AddNullTerminatedUtf8(rawSimpleName, "Module simple name");
        }

        if (path is not null)
        {
            module.Path = AddUtf16String(path, $"Module path = {path}");
        }

        if (fileName is not null)
        {
            module.FileName = AddUtf16String(fileName, $"Module file name = {fileName}");
        }

        MockLoaderAssembly assembly = AssemblyLayout.Create(_allocator.Allocate((ulong)AssemblyLayout.Size, "Assembly"));
        assembly.Module = module.Address;
        module.Assembly = assembly.Address;
        return module;
    }

    internal MockEEConfig AddEEConfig(uint modifiableAssemblies)
    {
        MockEEConfig config = EEConfigLayout.Create(_allocator.Allocate((ulong)EEConfigLayout.Size, "EEConfig"));
        config.ModifiableAssemblies = modifiableAssemblies;
        return config;
    }

    private ulong AddNullTerminatedUtf8(ReadOnlySpan<byte> bytes, string name)
    {
        MockMemorySpace.HeapFragment fragment = _allocator.Allocate((ulong)bytes.Length + 1, name);
        bytes.CopyTo(fragment.Data);
        fragment.Data[^1] = 0;
        return fragment.Address;
    }

    private ulong AddUtf16String(string value, string name)
    {
        TargetTestHelpers helpers = Builder.TargetTestHelpers;
        Encoding encoding = helpers.Arch.IsLittleEndian ? Encoding.Unicode : Encoding.BigEndianUnicode;
        MockMemorySpace.HeapFragment fragment = _allocator.Allocate((ulong)encoding.GetByteCount(value) + sizeof(char), name);
        helpers.WriteUtf16String(fragment.Data, value);
        return fragment.Address;
    }
}
