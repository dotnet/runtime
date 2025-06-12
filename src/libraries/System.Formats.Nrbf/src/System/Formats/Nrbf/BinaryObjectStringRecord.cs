// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;

namespace System.Formats.Nrbf;

/// <summary>
/// Represents a <see langword="string" /> record.
/// </summary>
/// <remarks>
/// BinaryObjectString records are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/eb503ca5-e1f6-4271-a7ee-c4ca38d07996">[MS-NRBF] 2.5.7</see>.
/// </remarks>
[DebuggerDisplay("{Value}, {Id}")]
internal sealed class BinaryObjectStringRecord : PrimitiveTypeRecord<string>
{
    private BinaryObjectStringRecord(SerializationRecordId id, string value) : base(value)
    {
        Id = id;
    }

    public override SerializationRecordType RecordType => SerializationRecordType.BinaryObjectString;

    /// <inheritdoc />
    public override SerializationRecordId Id { get; }

    internal static BinaryObjectStringRecord Decode(BinaryReader reader)
        => new(SerializationRecordId.Decode(reader), reader.ReadString());
}
