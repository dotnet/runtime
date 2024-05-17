// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Multiple null object record.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/f4abb5dd-aab7-4e0a-9d77-1d6c99f5779e">
///    [MS-NRBF] 2.5.5
///   </see>
///  </para>
/// </remarks>
internal sealed class ObjectNullMultipleRecord : NullsRecord
{
    private ObjectNullMultipleRecord(int count) => NullCount = count;

    public override RecordType RecordType => RecordType.ObjectNullMultiple;

    internal override int NullCount { get; }

    internal static ObjectNullMultipleRecord Parse(BinaryReader reader)
    {
        int count = reader.ReadInt32();
        if (count <= byte.MaxValue) // BinaryFormatter would have used ObjectNullMultiple256Record
        {
            throw new SerializationException($"Unexpected Null Record count: {count}");
        }

        return new(count);
    }
}
