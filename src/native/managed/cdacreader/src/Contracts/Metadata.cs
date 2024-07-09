// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal enum MetadataTable
{
    Unused = -1,
    Module = 0x0,
    TypeRef = 0x01,
    TypeDef = 0x02,
    FieldPtr = 0x03,
    Field = 0x04,
    MethodPtr = 0x05,
    MethodDef = 0x06,
    ParamPtr = 0x07,
    Param = 0x08,
    InterfaceImpl = 0x09,
    MemberRef = 0x0a,
    Constant = 0x0b,
    CustomAttribute = 0x0c,
    FieldMarshal = 0x0d,
    DeclSecurity = 0x0e,
    ClassLayout = 0x0f,
    FieldLayout = 0x10,
    StandAloneSig = 0x11,
    EventMap = 0x12,
    EventPtr = 0x13,
    Event = 0x14,
    PropertyMap = 0x15,
    PropertyPtr = 0x16,
    Property = 0x17,
    MethodSemantics = 0x18,
    MethodImpl = 0x19,
    ModuleRef = 0x1a,
    TypeSpec = 0x1b,
    ImplMap = 0x1c,
    FieldRva = 0x1d,
    ENCLog = 0x1e,
    ENCMap = 0x1f,
    Assembly = 0x20,
    AssemblyProcessor = 0x21,
    AssemblyOS = 0x22,
    AssemblyRef = 0x23,
    AssemblyRefProcessor = 0x24,
    AssemblyRefOS = 0x25,
    File = 0x26,
    ExportedType = 0x27,
    ManifestResource = 0x28,
    NestedClass = 0x29,
    GenericParam = 0x2a,
    MethodSpec = 0x2b,
    GenericParamConstraint = 0x2c,
    MaxValue = 0x2c
}

internal struct MetadataCursor
{
    public ulong reserved1;
    public object reserved2;
}

internal enum MetadataColumnIndex
{
    Assembly_HashAlgId,
    Assembly_MajorVersion,
    Assembly_MinorVersion,
    Assembly_BuildNumber,
    Assembly_RevisionNumber,
    Assembly_Flags,
    Assembly_PublicKey,
    Assembly_Name,
    Assembly_Culture,

    GenericParam_Number,
    GenericParam_Flags,
    GenericParam_Owner,
    GenericParam_Name,
    NestedClass_NestedClass,
    NestedClass_EnclosingClass,
    TypeDef_Flags,
    TypeDef_TypeName,
    TypeDef_TypeNamespace,
    TypeDef_Extends,
    TypeDef_FieldList,
    TypeDef_MethodList,
    Count
}

internal abstract class MetadataReader
{
    public static MetadataTable TokenToTable(uint token)
    {
        byte tableIndex = (byte)(token >> 24);
        if (tableIndex > (uint)MetadataTable.GenericParamConstraint)
        {
            return MetadataTable.Unused;
        }
        else
        {
            return (MetadataTable)tableIndex;
        }
    }

    public static uint RidFromToken(uint token)
    {
        return token & 0xFFFFFF;
    }
    public static uint CreateToken(MetadataTable table, uint rid)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan<uint>(rid, 0xFFFFFF, nameof(rid));
        ArgumentOutOfRangeException.ThrowIfGreaterThan<int>((int)table, (int)MetadataTable.GenericParamConstraint, nameof(table));
        return ((uint)table << 24) | rid;
    }

    public abstract MetadataCursor GetCursor(uint token);
    public abstract bool TryGetCursor(uint token, out MetadataCursor cursor);
    public abstract bool TryGetCursorToFirstEntryInTable(MetadataTable table, out MetadataCursor cursor);
    public abstract uint GetToken(MetadataCursor c);

    // Query row's column values
    // The returned number represents the number of valid cursor(s) for indexing.
    public abstract uint GetColumnAsToken(MetadataCursor c, MetadataColumnIndex col_idx);
    public abstract MetadataCursor GetColumnAsCursor(MetadataCursor c, MetadataColumnIndex col_idx);
    // Resolve the column to a cursor and a range based on the run/list pattern in tables.
    // The run continues to the smaller of:
    //   * the last row of the target table
    //   * the next run in the target table, found by inspecting the column value of the next row in the current table.
    // See md_find_token_of_range_element() for mapping elements in the other direction.
    public abstract void GetColumnAsRange(MetadataCursor c, MetadataColumnIndex col_idx, out MetadataCursor cursor, out int count);
    public abstract uint GetColumnAsConstant(MetadataCursor c, MetadataColumnIndex col_idx);
    public abstract ReadOnlySpan<byte> GetColumnAsUtf8(MetadataCursor c, MetadataColumnIndex col_idx);
    public virtual string GetColumnAsUtf8String(MetadataCursor c, MetadataColumnIndex col_idx)
    {
        ReadOnlySpan<byte> utf8Data = GetColumnAsUtf8(c, col_idx);
        string str = string.Empty;
        if (utf8Data.Length > 0)
        {
            str = System.Text.Encoding.UTF8.GetString(utf8Data);
        }
        return str;
    }
    public abstract ReadOnlySpan<char> GetColumnAsUserstring(MetadataCursor c, MetadataColumnIndex col_idx);
    public abstract ReadOnlySpan<byte> GetColumnAsBlob(MetadataCursor c, MetadataColumnIndex col_idx);
    public abstract Guid GetColumnAsGuid(MetadataCursor c, MetadataColumnIndex col_idx);

    // Find a row or range of rows where the supplied column has the expected value.
    // These APIs assume the value to look for is the value in the table, typically record IDs (RID)
    // for tokens. An exception is made for coded indices, which are cumbersome to compute.
    // If the queried column contains a coded index value, the value will be validated and
    // transformed to its coded form for comparison.
    public abstract bool TryFindRowFromCursor(MetadataCursor begin, MetadataColumnIndex col_idx, uint value, out MetadataCursor foundCursor);
}

internal interface IMetadata : IContract
{
    static string IContract.Name => nameof(Metadata);
    static IContract IContract.Create(Target target, int version)
    {
        return version switch
        {
            _ => default(Metadata),
        };
    }

    public virtual MetadataReader GetMetadataReader(ModuleHandle module) => throw new NotImplementedException();
}

internal readonly struct Metadata : IMetadata
{
    // Everything throws NotImplementedException
}
