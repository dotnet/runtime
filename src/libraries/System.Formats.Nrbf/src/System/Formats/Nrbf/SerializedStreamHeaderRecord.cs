// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Diagnostics;
using System.Formats.Nrbf.Utils;
using System.Reflection.Metadata;

namespace System.Formats.Nrbf;

/// <summary>
/// Represents the NRBF header, it must be the first record in NRBF payload.
/// </summary>
/// <remarks>
/// SerializedStreamHeader records are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/a7e578d3-400a-4249-9424-7529d10d1b3c">[MS-NRBF] 2.6.1</see>.
/// </remarks>
internal sealed class SerializedStreamHeaderRecord : SerializationRecord
{
    internal const int Size = sizeof(byte) + sizeof(int) * 4;
    internal const int MajorVersion = 1;
    internal const int MinorVersion = 0;

    internal SerializedStreamHeaderRecord(SerializationRecordId rootId) => RootId = rootId;

    public override SerializationRecordType RecordType => SerializationRecordType.SerializedStreamHeader;

    public override TypeName TypeName
    {
        get
        {
            Debug.Fail("TypeName should never be called on SerializedStreamHeaderRecord");
            return TypeName.Parse(nameof(SerializedStreamHeaderRecord).AsSpan());
        }
    }
    public override SerializationRecordId Id => SerializationRecordId.NoId;

    internal SerializationRecordId RootId { get; }

    internal static SerializedStreamHeaderRecord Decode(BinaryReader reader)
    {
        SerializationRecordId rootId = SerializationRecordId.Decode(reader);
        _ = reader.ReadInt32(); // HeaderId
        int majorVersion = reader.ReadInt32();
        int minorVersion = reader.ReadInt32();

        // Version 1.0 is the only version that was ever defined, so match it exactly.
        if (majorVersion != MajorVersion)
        {
            ThrowHelper.ThrowInvalidValue(majorVersion);
        }
        else if (minorVersion != MinorVersion)
        {
            ThrowHelper.ThrowInvalidValue(minorVersion);
        }

        return new SerializedStreamHeaderRecord(rootId);
    }
}
