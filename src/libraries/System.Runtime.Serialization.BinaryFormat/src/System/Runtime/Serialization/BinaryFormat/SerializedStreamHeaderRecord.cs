// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Serialization.BinaryFormat.Utils;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
/// NRBF header, it must be the first record in NRBF payload.
/// </summary>
/// <remarks>
/// SerializedStreamHeader records are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/a7e578d3-400a-4249-9424-7529d10d1b3c">[MS-NRBF] 2.6.1</see>.
/// </remarks>
internal sealed class SerializedStreamHeaderRecord : SerializationRecord
{
    internal const int Size = sizeof(RecordType) + sizeof(int) * 4;
    internal const int MajorVersion = 1;
    internal const int MinorVersion = 0;

    internal SerializedStreamHeaderRecord(int rootId, int headerId)
    {
        RootId = rootId;
        HeaderId = headerId;
    }

    public override RecordType RecordType => RecordType.SerializedStreamHeader;

    internal int RootId { get; }

    internal int HeaderId { get; }

    internal static SerializedStreamHeaderRecord Parse(BinaryReader reader)
    {
        int rootId = reader.ReadInt32();
        int headerId = reader.ReadInt32();
        int majorVersion = reader.ReadInt32();
        int minorVersion = reader.ReadInt32();

        if (majorVersion != MajorVersion)
        {
            ThrowHelper.ThrowInvalidValue(majorVersion);
        }
        else if (minorVersion != MinorVersion)
        {
            ThrowHelper.ThrowInvalidValue(minorVersion);
        }

        return new(rootId, headerId);
    }
}
