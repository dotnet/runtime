// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Metadata;

namespace System.Formats.Nrbf;

/// <summary>
/// Represents the record that marks the end of the binary format stream.
/// </summary>
/// <remarks>
/// MessageEnd records are described in <see href="https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/de6a574b-c596-4d83-9df7-63c0077acd32">[MS-NRBF] 2.6.3</see>.
/// </remarks>
internal sealed class MessageEndRecord : SerializationRecord
{
    internal static MessageEndRecord Singleton { get; } = new();

    private MessageEndRecord()
    {
    }

    public override SerializationRecordType RecordType => SerializationRecordType.MessageEnd;

    public override SerializationRecordId Id => SerializationRecordId.NoId;

    public override TypeName TypeName
    {
        get
        {
            Debug.Fail("TypeName should never be called on MessageEndRecord");
            return TypeName.Parse(nameof(MessageEndRecord).AsSpan());
        }
    }
}
