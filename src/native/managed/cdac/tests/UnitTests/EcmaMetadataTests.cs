// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;
using ModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class EcmaMetadataTests
{
    private const ulong ModuleAddress = 0x1000;
    private const ulong PEAssemblyAddress = 0x2000;
    private const ulong HandleSlotAddress = 0x3000;
    private const ulong ContextAddress = 0x4000;
    private const ulong TablesAddress = 0x5000;
    private const ulong VersionAddress = 0x8000;
    private const ulong ModuleTableAddress = 0x9000;
    private const ulong TypeDefTableAddress = 0xA000;
    private const ulong StringsAddress = 0xB000;
    private const ulong GuidAddress = 0x30000;
    private const ulong BlobAddress = 0x31000;
    private const ulong UserStringAddress = 0x32000;

    private const uint ContextMagic = 0x3d71b;
    private const uint TableCount = 0x2d;
    private const uint LargeStrings = 0x1;
    private const uint MinimalDelta = 0x10000;
    private const uint UncompressedTables = 0x20000;

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetReadWriteMetadata_UsesNarrowColumnsWithoutJtd(MockTarget.Architecture arch)
    {
        (TestPlaceholderTarget target, ModuleHandle handle, _, _) = CreateTarget(arch, largeStrings: false, uncompressedTables: false);

        byte[] image = target.Contracts.EcmaMetadata.GetReadWriteMetadata(handle);
        Assert.Equal(-1, image.AsSpan().IndexOf("#JTD"u8));
        Assert.True(image.AsSpan().IndexOf("#~"u8) >= 0);
        AssertMetadataContainsSampleType(image);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetReadWriteMetadata_UsesWideStringColumnsWithoutJtd(MockTarget.Architecture arch)
    {
        (TestPlaceholderTarget target, ModuleHandle handle, _, _) = CreateTarget(arch, largeStrings: true, uncompressedTables: true);

        byte[] image = target.Contracts.EcmaMetadata.GetReadWriteMetadata(handle);
        Assert.Equal(-1, image.AsSpan().IndexOf("#JTD"u8));
        Assert.True(image.AsSpan().IndexOf("#-"u8) >= 0);
        AssertMetadataContainsSampleType(image);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetReadWriteMetadata_AddsJtdOnlyForMinimalDelta(MockTarget.Architecture arch)
    {
        (TestPlaceholderTarget target, ModuleHandle handle, _, _) = CreateTarget(arch, largeStrings: false, uncompressedTables: true, minimalDelta: true);

        byte[] image = target.Contracts.EcmaMetadata.GetReadWriteMetadata(handle);
        Assert.True(image.AsSpan().IndexOf("#JTD"u8) >= 0);
        AssertMetadataContainsSampleType(image);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetReadWriteMetadata_RechecksHandleSlotWithoutGenerationChange(MockTarget.Architecture arch)
    {
        (TestPlaceholderTarget target, ModuleHandle handle, MockMemorySpace.Builder memory, TargetTestHelpers helpers) =
            CreateTarget(arch, largeStrings: false, uncompressedTables: false);

        AssertMetadataContainsSampleType(target.Contracts.EcmaMetadata.GetReadWriteMetadata(handle));
        Assert.NotNull(target.Contracts.EcmaMetadata.GetMetadata(handle));

        helpers.WritePointer(memory.BorrowAddressRange(HandleSlotAddress, helpers.PointerSize), 0);
        Assert.Throws<InvalidOperationException>(() => target.Contracts.EcmaMetadata.GetReadWriteMetadata(handle));
        Assert.Throws<InvalidOperationException>(() => target.Contracts.EcmaMetadata.GetMetadata(handle));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetReadWriteMetadata_RejectsIncompleteDNMDRow(MockTarget.Architecture arch)
    {
        (TestPlaceholderTarget target, ModuleHandle handle, MockMemorySpace.Builder memory, _) =
            CreateTarget(arch, largeStrings: false, uncompressedTables: false);
        Target.TypeInfo tableType = target.GetTypeInfo(DataType.DNMDTable);
        int addingNewRowOffset = tableType.Fields["AddingNewRow"].Offset;
        memory.BorrowAddressRange(TablesAddress + (ulong)addingNewRowOffset, 1)[0] = 1;

        Assert.Throws<InvalidOperationException>(() => target.Contracts.EcmaMetadata.GetReadWriteMetadata(handle));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetReadWriteMetadata_RejectsMismatchedDNMDTableDataSize(MockTarget.Architecture arch)
    {
        (TestPlaceholderTarget target, ModuleHandle handle, MockMemorySpace.Builder memory, TargetTestHelpers helpers) =
            CreateTarget(arch, largeStrings: false, uncompressedTables: false);
        Target.TypeInfo tableType = target.GetTypeInfo(DataType.DNMDTable);
        Target.TypeInfo dataType = target.GetTypeInfo(DataType.DNMDData);
        ulong sizeAddress = TablesAddress + (ulong)tableType.Fields["Data"].Offset + (ulong)dataType.Fields["Size"].Offset;
        helpers.WritePointer(memory.BorrowAddressRange(sizeAddress, helpers.PointerSize), 0);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => target.Contracts.EcmaMetadata.GetReadWriteMetadata(handle));
        Assert.Equal("Invalid DNMD table 0 size or address.", exception.Message);
    }

    private static void AssertMetadataContainsSampleType(byte[] image)
    {
        using MetadataReaderProvider provider = MetadataReaderProvider.FromMetadataImage(ImmutableCollectionsMarshal.AsImmutableArray(image));
        MetadataReader reader = provider.GetMetadataReader();
        Assert.Equal("v4.0.30319", reader.MetadataVersion);
        Assert.Equal(2, reader.GetTableRowCount(TableIndex.TypeDef));
        TypeDefinition type = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
        Assert.Equal("Sample", reader.GetString(type.Name));
        Assert.Equal("Example", reader.GetString(type.Namespace));
    }

    private static (TestPlaceholderTarget Target, ModuleHandle Handle, MockMemorySpace.Builder Memory, TargetTestHelpers Helpers)
        CreateTarget(MockTarget.Architecture arch, bool largeStrings, bool uncompressedTables, bool minimalDelta = false)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder memory = new(helpers);
        TargetTestHelpers.LayoutResult moduleLayout = helpers.LayoutFields(
        [
            new("PEAssembly", DataType.pointer),
            new("DynamicMetadata", DataType.pointer),
            new("MetadataGeneration", DataType.uint32),
        ]);
        TargetTestHelpers.LayoutResult peAssemblyLayout = helpers.LayoutFields(
        [
            new("PEImage", DataType.pointer),
            new("AssemblyBinder", DataType.pointer),
            new("MDImportIsRW", DataType.int32),
            new("MDImport", DataType.pointer),
            new("DNMDMetadataHandleSlot", DataType.pointer),
        ]);
        TargetTestHelpers.LayoutResult dataLayout = helpers.LayoutFields(
        [
            new("Ptr", DataType.pointer),
            new("Size", DataType.nuint),
        ]);
        TargetTestHelpers.LayoutResult contextLayout = helpers.LayoutFields(TargetTestHelpers.FieldLayout.Packed,
        [
            new("Magic", DataType.uint32),
            new("Flags", DataType.uint32),
            new("Version", DataType.pointer),
            new("StringsHeap", DataType.DNMDData, dataLayout.Stride),
            new("GuidHeap", DataType.DNMDData, dataLayout.Stride),
            new("BlobHeap", DataType.DNMDData, dataLayout.Stride),
            new("UserStringHeap", DataType.DNMDData, dataLayout.Stride),
            new("Tables", DataType.pointer),
        ]);
        TargetTestHelpers.LayoutResult tableLayout = helpers.LayoutFields(TargetTestHelpers.FieldLayout.Packed,
        [
            new("Data", DataType.DNMDData, dataLayout.Stride),
            new("RowCount", DataType.uint32),
            new("RowSize", DataType.uint8),
            new("Sorted", DataType.uint8),
            new("AddingNewRow", DataType.uint8),
            new("TableId", DataType.uint8),
            new("Context", DataType.pointer),
        ]);

        Dictionary<DataType, Target.TypeInfo> types = new()
        {
            [DataType.Module] = new() { Size = moduleLayout.Stride, Fields = moduleLayout.Fields },
            [DataType.PEAssembly] = new() { Size = peAssemblyLayout.Stride, Fields = peAssemblyLayout.Fields },
            [DataType.DNMDContext] = new() { Size = contextLayout.Stride, Fields = contextLayout.Fields },
            [DataType.DNMDData] = new() { Size = dataLayout.Stride, Fields = dataLayout.Fields },
            [DataType.DNMDTable] = new() { Size = tableLayout.Stride, Fields = tableLayout.Fields },
        };

        byte[] module = new byte[checked((int)moduleLayout.Stride)];
        WritePointer(module, moduleLayout, "PEAssembly", PEAssemblyAddress);
        WriteUInt32(module, moduleLayout, "MetadataGeneration", 1);
        memory.AddHeapFragment(new() { Address = ModuleAddress, Data = module, Name = "Module" });

        byte[] peAssembly = new byte[checked((int)peAssemblyLayout.Stride)];
        WritePointer(peAssembly, peAssemblyLayout, "DNMDMetadataHandleSlot", HandleSlotAddress);
        helpers.Write(peAssembly.AsSpan(peAssemblyLayout.Fields["MDImportIsRW"].Offset), 1);
        memory.AddHeapFragment(new() { Address = PEAssemblyAddress, Data = peAssembly, Name = "PEAssembly" });

        byte[] slot = new byte[helpers.PointerSize];
        helpers.WritePointer(slot, ContextAddress);
        memory.AddHeapFragment(new() { Address = HandleSlotAddress, Data = slot, Name = "Handle slot" });

        List<byte> strings = [0];
        int moduleName = AddString(strings, "TestModule");
        int moduleTypeName = AddString(strings, "<Module>");
        int typeNamespace = AddString(strings, "Example");
        int typeName = AddString(strings, "Sample");
        if (largeStrings)
            strings.AddRange(new byte[ushort.MaxValue + 1 - strings.Count]);

        byte[] moduleRows = CreateModuleRows(moduleName, largeStrings);
        byte[] typeRows = CreateTypeDefRows(moduleTypeName, typeNamespace, typeName, largeStrings, minimalDelta);

        uint flags = (largeStrings ? LargeStrings : 0)
            | (uncompressedTables ? UncompressedTables : 0)
            | (minimalDelta ? MinimalDelta : 0);
        byte[] context = new byte[checked((int)contextLayout.Stride)];
        WriteUInt32(context, contextLayout, "Magic", ContextMagic);
        WriteUInt32(context, contextLayout, "Flags", flags);
        WritePointer(context, contextLayout, "Version", VersionAddress);
        WriteData(context, contextLayout, "StringsHeap", StringsAddress, (ulong)strings.Count);
        WriteData(context, contextLayout, "GuidHeap", GuidAddress, 16);
        WriteData(context, contextLayout, "BlobHeap", BlobAddress, 1);
        WriteData(context, contextLayout, "UserStringHeap", UserStringAddress, 1);
        WritePointer(context, contextLayout, "Tables", TablesAddress);
        memory.AddHeapFragment(new() { Address = ContextAddress, Data = context, Name = "DNMD context" });

        byte[] tables = new byte[checked((int)(tableLayout.Stride * TableCount))];
        WriteTable(tables.AsSpan(), tableLayout, 0, ModuleTableAddress, moduleRows, 1);
        WriteTable(tables.AsSpan(), tableLayout, 2, TypeDefTableAddress, typeRows, 2);
        memory.AddHeapFragment(new() { Address = TablesAddress, Data = tables, Name = "DNMD tables" });
        memory.AddHeapFragment(new() { Address = VersionAddress, Data = Encoding.UTF8.GetBytes("v4.0.30319\0"), Name = "Metadata version" });
        memory.AddHeapFragment(new() { Address = ModuleTableAddress, Data = moduleRows, Name = "Module rows" });
        memory.AddHeapFragment(new() { Address = TypeDefTableAddress, Data = typeRows, Name = "TypeDef rows" });
        memory.AddHeapFragment(new() { Address = StringsAddress, Data = strings.ToArray(), Name = "Strings heap" });
        memory.AddHeapFragment(new() { Address = GuidAddress, Data = new byte[16], Name = "GUID heap" });
        memory.AddHeapFragment(new() { Address = BlobAddress, Data = [0], Name = "Blob heap" });
        memory.AddHeapFragment(new() { Address = UserStringAddress, Data = [0], Name = "User string heap" });

        Mock<ILoader> loader = new();
        ModuleHandle handle = new(new TargetPointer(ModuleAddress));
        loader.Setup(l => l.GetPEAssembly(handle)).Returns(new TargetPointer(PEAssemblyAddress));
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(arch)
            .UseReader(memory.GetMemoryContext().ReadFromTarget)
            .AddTypes(types)
            .AddGlobals(
                (Constants.Globals.DNMDContextMagic, ContextMagic),
                (Constants.Globals.DNMDTableCount, TableCount),
                (Constants.Globals.DNMDLargeStringHeap, LargeStrings),
                (Constants.Globals.DNMDLargeGuidHeap, 2),
                (Constants.Globals.DNMDLargeBlobHeap, 4),
                (Constants.Globals.DNMDMinimalDelta, MinimalDelta),
                (Constants.Globals.DNMDUncompressedTables, UncompressedTables))
            .AddMockContract(loader)
            .AddContract<IEcmaMetadata>("c2")
            .Build();
        return (target, handle, memory, helpers);

        void WritePointer(byte[] destination, TargetTestHelpers.LayoutResult layout, string name, ulong value)
            => helpers.WritePointer(destination.AsSpan(layout.Fields[name].Offset), value);

        void WriteUInt32(byte[] destination, TargetTestHelpers.LayoutResult layout, string name, uint value)
            => helpers.Write(destination.AsSpan(layout.Fields[name].Offset), value);

        void WriteData(Span<byte> destination, TargetTestHelpers.LayoutResult layout, string name, ulong pointer, ulong size)
        {
            int offset = layout.Fields[name].Offset;
            helpers.WritePointer(destination[(offset + dataLayout.Fields["Ptr"].Offset)..], pointer);
            helpers.WritePointer(destination[(offset + dataLayout.Fields["Size"].Offset)..], size);
        }

        void WriteTable(Span<byte> destination, TargetTestHelpers.LayoutResult layout, int tableId, ulong dataAddress, byte[] rows, uint rowCount)
        {
            Span<byte> table = destination.Slice(checked((int)layout.Stride * tableId), checked((int)layout.Stride));
            WriteData(table, layout, "Data", dataAddress, (ulong)rows.Length);
            helpers.Write(table[layout.Fields["RowCount"].Offset..], rowCount);
            table[layout.Fields["RowSize"].Offset] = checked((byte)(rows.Length / rowCount));
            table[layout.Fields["Sorted"].Offset] = 1;
            table[layout.Fields["TableId"].Offset] = checked((byte)tableId);
            helpers.WritePointer(table[layout.Fields["Context"].Offset..], ContextAddress);
        }
    }

    private static int AddString(List<byte> strings, string value)
    {
        int offset = strings.Count;
        strings.AddRange(Encoding.UTF8.GetBytes(value));
        strings.Add(0);
        return offset;
    }

    private static byte[] CreateModuleRows(int name, bool wideStrings)
    {
        BlobBuilder builder = new();
        builder.WriteUInt16(0);
        WriteIndex(builder, name, wideStrings);
        WriteIndex(builder, 1, wide: false);
        WriteIndex(builder, 0, wide: false);
        WriteIndex(builder, 0, wide: false);
        return builder.ToArray();
    }

    private static byte[] CreateTypeDefRows(int moduleName, int typeNamespace, int typeName, bool wideStrings, bool minimalDelta)
    {
        BlobBuilder builder = new();
        WriteTypeDef(builder, 0, moduleName, 0);
        WriteTypeDef(builder, 1, typeName, typeNamespace);
        return builder.ToArray();

        void WriteTypeDef(BlobBuilder writer, uint flags, int name, int ns)
        {
            writer.WriteUInt32(flags);
            WriteIndex(writer, name, wideStrings);
            WriteIndex(writer, ns, wideStrings);
            WriteIndex(writer, 0, minimalDelta);
            WriteIndex(writer, 1, minimalDelta);
            WriteIndex(writer, 1, minimalDelta);
        }
    }

    private static void WriteIndex(BlobBuilder builder, int index, bool wide)
    {
        if (wide)
            builder.WriteUInt32(checked((uint)index));
        else
            builder.WriteUInt16(checked((ushort)index));
    }
}
