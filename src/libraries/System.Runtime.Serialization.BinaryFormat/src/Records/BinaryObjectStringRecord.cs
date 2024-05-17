// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  String record.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/eb503ca5-e1f6-4271-a7ee-c4ca38d07996">
///    [MS-NRBF] 2.5.7
///   </see>
///  </para>
/// </remarks>
[DebuggerDisplay("{Value}, {ObjectId}")]
internal sealed class BinaryObjectStringRecord : PrimitiveTypeRecord<string>
{
    private BinaryObjectStringRecord(int objectId, string value) : base(value)
    {
        ObjectId = objectId;
    }

    public override RecordType RecordType => RecordType.BinaryObjectString;

    public override int ObjectId { get; }

    internal static BinaryObjectStringRecord Parse(BinaryReader reader)
        => new(reader.ReadInt32(), reader.ReadString());
}
