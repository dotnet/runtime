// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
/// Represents a library.
/// </summary>
/// <remarks>
/// BinaryLibrary records are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/7fcf30e1-4ad4-4410-8f1a-901a4a1ea832">[MS-NRBF] 2.6.2</see>.
/// </remarks>
internal sealed class BinaryLibraryRecord : SerializationRecord
{
    private BinaryLibraryRecord(int libraryId, string libraryName)
    {
        ObjectId = libraryId;
        LibraryName = libraryName;
    }

    public override RecordType RecordType => RecordType.BinaryLibrary;

    internal string LibraryName { get; }

    /// <inheritdoc />
    public override int ObjectId { get; }

    internal static BinaryLibraryRecord Decode(BinaryReader reader)
        => new(reader.ReadInt32(), reader.ReadString());
}
