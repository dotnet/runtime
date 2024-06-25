// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;

namespace System.Formats.Nrbf;

/// <summary>
/// Represents a record that contains a reference to another record that contains the actual value.
/// </summary>
/// <remarks>
/// MemberReference records are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/eef0aa32-ab03-4b6a-a506-bcdfc10583fd">[MS-NRBF] 2.5.3</see>.
/// </remarks>
internal sealed class MemberReferenceRecord : SerializationRecord
{
    private MemberReferenceRecord(SerializationRecordId reference, RecordMap recordMap)
    {
        Reference = reference;
        RecordMap = recordMap;
    }

    public override SerializationRecordType RecordType => SerializationRecordType.MemberReference;

    internal SerializationRecordId Reference { get; }

    private RecordMap RecordMap { get; }

    // MemberReferenceRecord has no Id, which makes it impossible to create a cycle
    // by creating a reference to the reference itself.
    public override SerializationRecordId Id => SerializationRecordId.NoId;

    public override TypeName TypeName => GetReferencedRecord().TypeName;

    internal override object? GetValue() => GetReferencedRecord().GetValue();

    internal static MemberReferenceRecord Decode(BinaryReader reader, RecordMap recordMap)
        => new(SerializationRecordId.Decode(reader), recordMap);

    internal SerializationRecord GetReferencedRecord() => RecordMap[Reference];
}
