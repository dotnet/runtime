// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
/// Multiple null object record (less than 256).
/// </summary>
/// <remarks>
/// ObjectNullMultiple256 records are described in <see href="https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/24ae84a0-781f-45bf-a1ac-49f6a65af5dd">[MS-NRBF] 2.5.6</see>.
/// </remarks>
internal sealed class ObjectNullMultiple256Record : NullsRecord
{
    private ObjectNullMultiple256Record(byte count) => NullCount = count;

    public override RecordType RecordType => RecordType.ObjectNullMultiple256;

    internal override int NullCount { get; }

    internal static ObjectNullMultiple256Record Parse(BinaryReader reader)
        => new(reader.ReadByte());
}
