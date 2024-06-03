// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
/// Represents a <see langword="string" /> record.
/// </summary>
/// <remarks>
/// BinaryObjectString records are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/eb503ca5-e1f6-4271-a7ee-c4ca38d07996">[MS-NRBF] 2.5.7</see>.
/// </remarks>
[DebuggerDisplay("{Value}, {ObjectId}")]
internal sealed class BinaryObjectStringRecord : PrimitiveTypeRecord<string>
{
    private BinaryObjectStringRecord(int objectId, string value) : base(value)
    {
        ObjectId = objectId;
    }

    public override RecordType RecordType => RecordType.BinaryObjectString;

    /// <inheritdoc />
    public override int ObjectId { get; }

    internal static BinaryObjectStringRecord Decode(BinaryReader reader)
        => new(reader.ReadInt32(), reader.ReadString());
}
