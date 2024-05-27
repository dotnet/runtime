// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  The <see cref="MemberReferenceRecord"/> record contains a reference to another record that contains the actual value.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/eef0aa32-ab03-4b6a-a506-bcdfc10583fd">
///    [MS-NRBF] 2.5.3
///   </see>
///  </para>
/// </remarks>
internal sealed class MemberReferenceRecord : SerializationRecord
{
    // This type has no ObjectId, so it's impossible to create a reference to a reference
    // and get into issues with cycles or unbounded recursion.
    private MemberReferenceRecord(int reference, RecordMap recordMap)
    {
        Reference = reference;
        RecordMap = recordMap;
    }

    public override RecordType RecordType => RecordType.MemberReference;

    internal int Reference { get; }

    private RecordMap RecordMap { get; }

    internal override object? GetValue() => GetReferencedRecord().GetValue();

    public override bool IsTypeNameMatching(Type type) => RecordMap[Reference].IsTypeNameMatching(type);

    internal static MemberReferenceRecord Parse(BinaryReader reader, RecordMap recordMap)
        => new(reader.ReadInt32(), recordMap);

    internal SerializationRecord GetReferencedRecord() => RecordMap[Reference];
}
