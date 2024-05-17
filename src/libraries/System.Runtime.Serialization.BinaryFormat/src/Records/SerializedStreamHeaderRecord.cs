// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Binary format header.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/a7e578d3-400a-4249-9424-7529d10d1b3c">
///    [MS-NRBF] 2.6.1
///   </see>
///  </para>
/// </remarks>
internal sealed class SerializedStreamHeaderRecord : SerializationRecord
{
    internal const int Size = sizeof(int) * 4;

    internal SerializedStreamHeaderRecord(int rootId, int headerId, int majorVersion, int minorVersion)
    {
        RootId = rootId;
        HeaderId = headerId;
        MajorVersion = majorVersion;
        MinorVersion = minorVersion;
    }

    public override RecordType RecordType => RecordType.SerializedStreamHeader;

    internal int RootId { get; }

    internal int HeaderId { get; }

    internal int MajorVersion { get; }

    internal int MinorVersion { get; }

    internal static SerializedStreamHeaderRecord Parse(BinaryReader reader)
    {
        int rootId = reader.ReadInt32();
        int headerId = reader.ReadInt32();
        int majorVersion = reader.ReadInt32();
        int minorVersion = reader.ReadInt32();

        if (majorVersion != 1 || minorVersion != 0)
        {
            throw new SerializationException();
        }

        return new(rootId, headerId, majorVersion, minorVersion);
    }
}
