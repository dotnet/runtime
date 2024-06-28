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

    internal bool IsDefault => _id == default;
}
