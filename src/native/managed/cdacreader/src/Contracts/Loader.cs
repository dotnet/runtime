// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct ModuleHandle
{
    internal ModuleHandle(TargetPointer address)
    {
        Address = address;
    }

    internal TargetPointer Address { get; }
}

[Flags]
internal enum ModuleFlags
{
    EditAndContinue = 0x00000008,   // Edit and Continue is enabled for this module
    ReflectionEmit = 0x00000040,    // Reflection.Emit was used to create this module
}

internal record struct ModuleLookupTables(
    TargetPointer FieldDefToDesc,
    TargetPointer ManifestModuleReferences,
    TargetPointer MemberRefToDesc,
    TargetPointer MethodDefToDesc,
    TargetPointer TypeDefToMethodTable,
    TargetPointer TypeRefToMethodTable);

internal struct EcmaMetadataSchema
{
    public EcmaMetadataSchema(string metadataVersion, bool largeStringHeap, bool largeBlobHeap, bool largeGuidHeap, int[] rowCount, bool[] isSorted, bool variableSizedColumnsAre4BytesLong)
    {
        MetadataVersion = metadataVersion;
        LargeStringHeap = largeStringHeap;
        LargeBlobHeap = largeBlobHeap;
        LargeGuidHeap = largeGuidHeap;

        _rowCount = rowCount;
        _isSorted = isSorted;

        VariableSizedColumnsAreAll4BytesLong = variableSizedColumnsAre4BytesLong;
    }

    public readonly string MetadataVersion;

    public readonly bool LargeStringHeap;
    public readonly bool LargeBlobHeap;
    public readonly bool LargeGuidHeap;

    // Table data, these structures hold MetadataTable.Count entries
    private readonly int[] _rowCount;
    public readonly ReadOnlySpan<int> RowCount => _rowCount;

    private readonly bool[] _isSorted;
    public readonly ReadOnlySpan<bool> IsSorted => _isSorted;

    // In certain scenarios the size of the tables is forced to be the maximum size
    // Otherwise the size of columns should be computed based on RowSize/the various heap flags
    public readonly bool VariableSizedColumnsAreAll4BytesLong;
}

internal class TargetEcmaMetadata
{
    public TargetEcmaMetadata(EcmaMetadataSchema schema,
                        TargetSpan[] tables,
                        TargetSpan stringHeap,
                        TargetSpan userStringHeap,
                        TargetSpan blobHeap,
                        TargetSpan guidHeap)
    {
        Schema = schema;
        _tables = tables;
        StringHeap = stringHeap;
        UserStringHeap = userStringHeap;
        BlobHeap = blobHeap;
        GuidHeap = guidHeap;
    }

    public EcmaMetadataSchema Schema { get; init; }

    private TargetSpan[] _tables;
    public ReadOnlySpan<TargetSpan> Tables => _tables;
    public TargetSpan StringHeap { get; init; }
    public TargetSpan UserStringHeap { get; init; }
    public TargetSpan BlobHeap { get; init; }
    public TargetSpan GuidHeap { get; init; }
}

[Flags]
internal enum AvailableMetadataType
{
    None = 0,
    ReadOnly = 1,
    ReadWriteSavedCopy = 2,
    ReadWrite = 4
}

internal interface ILoader : IContract
{
    static string IContract.Name => nameof(Loader);
    static IContract IContract.Create(Target target, int version)
    {
        return version switch
        {
            1 => new Loader_1(target),
            _ => default(Loader),
        };
    }

    public virtual ModuleHandle GetModuleHandle(TargetPointer modulePointer) => throw new NotImplementedException();

    public virtual TargetPointer GetAssembly(ModuleHandle handle) => throw new NotImplementedException();
    public virtual ModuleFlags GetFlags(ModuleHandle handle) => throw new NotImplementedException();
    public virtual TargetPointer GetLoaderAllocator(ModuleHandle handle) => throw new NotImplementedException();
    public virtual TargetPointer GetThunkHeap(ModuleHandle handle) => throw new NotImplementedException();

    public virtual TargetPointer GetILBase(ModuleHandle handle) => throw new NotImplementedException();

    public virtual TargetPointer GetMetadataAddress(ModuleHandle handle, out ulong size) => throw new NotImplementedException();

    public virtual AvailableMetadataType GetAvailableMetadataType(ModuleHandle handle) => throw new NotImplementedException();

    public virtual TargetPointer GetReadWriteSavedMetadataAddress(ModuleHandle handle, out ulong size) => throw new NotImplementedException();

    public virtual TargetEcmaMetadata GetReadWriteMetadata(ModuleHandle handle) => throw new NotImplementedException();

    public virtual ModuleLookupTables GetLookupTables(ModuleHandle handle) => throw new NotImplementedException();

    public virtual string GetPath(ModuleHandle handle) => throw new NotImplementedException();
}

internal readonly struct Loader : ILoader
{
    // Everything throws NotImplementedException
}
