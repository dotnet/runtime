// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Nrbf.Utils;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Formats.Nrbf;

/// <summary>
/// The ID of <see cref="SerializationRecord" />.
/// </summary>
/// <remarks>
/// It can be used the detect cycles in decoded records.
/// </remarks>
[DebuggerDisplay("{_id}")]
public readonly struct SerializationRecordId : IEquatable<SerializationRecordId>
{
#pragma warning disable CS0649 // the default value is used on purpose
    internal static readonly SerializationRecordId NoId;
#pragma warning restore CS0649

    internal readonly int _id;

    private SerializationRecordId(int id) => _id = id;

    internal static SerializationRecordId Decode(BinaryReader reader)
    {
        int id = reader.ReadInt32();

        // Many object ids are required to be positive. See:
        // - https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/8fac763f-e46d-43a1-b360-80eb83d2c5fb
        // - https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/eb503ca5-e1f6-4271-a7ee-c4ca38d07996
        // - https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/7fcf30e1-4ad4-4410-8f1a-901a4a1ea832 (for library id)
        //
        // Exception: https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/0a192be0-58a1-41d0-8a54-9c91db0ab7bf may be negative
        // The problem is that input generated with FormatterTypeStyle.XsdString ends up generating negative Ids anyway.
        // That information is not reflected in payload in anyway, so we just always allow for negative Ids.

        if (id == 0)
        {
            ThrowHelper.ThrowInvalidValue(id);
        }

        return new SerializationRecordId(id);
    }

    /// <inheritdoc />
    public bool Equals(SerializationRecordId other) => _id == other._id;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is SerializationRecordId other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(_id);
}
