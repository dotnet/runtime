// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class EcmaMetadata_2(Target target) : EcmaMetadata_1(target)
{
    private const int MaxMetadataComponentSize = 100_000_000;

    protected override TargetPointer GetMetadataHandle(ModuleHandle handle)
    {
        TargetPointer peAssemblyAddress = Target.Contracts.Loader.GetPEAssembly(handle);
        Data.PEAssembly peAssembly = Target.ProcessedData.GetOrAdd<Data.PEAssembly>(peAssemblyAddress);
        TargetPointer slot = peAssembly.DNMDMetadataHandleSlot;
        return slot == TargetPointer.Null ? TargetPointer.Null : Target.ReadPointer(slot);
    }

    protected override TargetEcmaMetadata GetTargetEcmaMetadata(ModuleHandle handle)
    {
        TargetPointer contextAddress = GetMetadataHandle(handle);
        if (contextAddress == TargetPointer.Null)
            throw new InvalidOperationException("Module does not have a DNMD metadata handle.");

        Data.DNMDContext context = Target.ProcessedData.GetOrAdd<Data.DNMDContext>(contextAddress);
        if (context.Magic != Target.ReadGlobal<uint>(Constants.Globals.DNMDContextMagic))
            throw Marshal.GetExceptionForHR(CorDbgHResults.CLDB_E_FILE_CORRUPT)!;

        uint tableCount = Target.ReadGlobal<uint>(Constants.Globals.DNMDTableCount);
        if (tableCount == 0 || tableCount > MetadataTokens.TableCount || context.Tables == TargetPointer.Null)
            throw Marshal.GetExceptionForHR(CorDbgHResults.CLDB_E_FILE_CORRUPT)!;

        int[] rowCounts = new int[tableCount];
        bool[] sorted = new bool[tableCount];
        byte[][] tables = new byte[tableCount][];
        uint tableSize = Target.GetTypeInfo(DataType.DNMDTable).Size!.Value;

        for (uint i = 0; i < tableCount; i++)
        {
            TargetPointer tableAddress = context.Tables + checked((ulong)i * tableSize);
            Data.DNMDTable table = Target.ProcessedData.GetOrAdd<Data.DNMDTable>(tableAddress);
            if (table.AddingNewRow != 0)
                throw new InvalidOperationException($"DNMD table {i} has an incomplete row.");

            rowCounts[i] = checked((int)table.RowCount);
            if (table.RowCount == 0)
            {
                tables[i] = [];
                continue;
            }

            if (table.Context != contextAddress || table.TableId != i || table.RowSize == 0 || table.Sorted > 1)
            {
                throw Marshal.GetExceptionForHR(CorDbgHResults.CLDB_E_FILE_CORRUPT)!;
            }

            sorted[i] = table.Sorted != 0;
            tables[i] = ReadRegion(table.Data, $"table {i}", checked((ulong)table.RowCount * table.RowSize));
        }

        uint flags = context.Flags;
        string version = context.Version == TargetPointer.Null
            ? throw Marshal.GetExceptionForHR(CorDbgHResults.CLDB_E_FILE_CORRUPT)!
            : Target.ReadUtf8String(context.Version, strict: true);
        EcmaMetadataSchema schema = new(
            version,
            (flags & Target.ReadGlobal<uint>(Constants.Globals.DNMDLargeStringHeap)) != 0,
            (flags & Target.ReadGlobal<uint>(Constants.Globals.DNMDLargeBlobHeap)) != 0,
            (flags & Target.ReadGlobal<uint>(Constants.Globals.DNMDLargeGuidHeap)) != 0,
            rowCounts,
            sorted,
            (flags & Target.ReadGlobal<uint>(Constants.Globals.DNMDMinimalDelta)) != 0,
            useUncompressedTableStream: (flags & Target.ReadGlobal<uint>(Constants.Globals.DNMDUncompressedTables)) != 0);

        return new TargetEcmaMetadata(
            schema,
            tables,
            ReadRegion(context.StringsHeap, "strings heap"),
            ReadRegion(context.UserStringHeap, "user-string heap"),
            ReadRegion(context.BlobHeap, "blob heap"),
            ReadRegion(context.GuidHeap, "GUID heap"));
    }

    private byte[] ReadRegion(TargetPointer dataAddress, string name, ulong? expectedSize = null)
    {
        Data.DNMDData data = Target.ProcessedData.GetOrAdd<Data.DNMDData>(dataAddress);
        if (data.Size.Value > MaxMetadataComponentSize
            || (expectedSize is not null && data.Size.Value != expectedSize.Value)
            || (data.Size.Value != 0 && data.Ptr == TargetPointer.Null))
            throw new InvalidOperationException($"Invalid DNMD {name} size or address.");

        byte[] bytes = new byte[checked((int)data.Size.Value)];
        if (bytes.Length != 0)
            Target.ReadBuffer(data.Ptr, bytes);
        return bytes;
    }
}
