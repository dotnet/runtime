// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Formats.Nrbf.Utils;

namespace System.Formats.Nrbf;

/// <summary>
/// Represents multiple <see langword="null" />.
/// </summary>
/// <remarks>
/// ObjectNullMultiple records are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/f4abb5dd-aab7-4e0a-9d77-1d6c99f5779e">[MS-NRBF] 2.5.5</see>.
/// </remarks>
internal sealed class ObjectNullMultipleRecord : NullsRecord
{
    private ObjectNullMultipleRecord(int count) => NullCount = count;

    public override SerializationRecordType RecordType => SerializationRecordType.ObjectNullMultiple;

    internal override int NullCount { get; }

    internal static ObjectNullMultipleRecord Decode(BinaryReader reader)
    {
        // 2.5.5 ObjectNullMultiple

        // NullCount (4 bytes): An INT32 value ... The value MUST be a positive integer.
        int count = reader.ReadInt32();
        if (count <= 0)
        {
            ThrowHelper.ThrowInvalidValue(count);
        }

        return new ObjectNullMultipleRecord(count);
    }
}
