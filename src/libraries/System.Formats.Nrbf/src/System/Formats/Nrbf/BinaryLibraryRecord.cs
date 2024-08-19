// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Nrbf.Utils;
using System.IO;
using System.Reflection.Metadata;

namespace System.Formats.Nrbf;

/// <summary>
/// Represents a library.
/// </summary>
/// <remarks>
/// BinaryLibrary records are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/7fcf30e1-4ad4-4410-8f1a-901a4a1ea832">[MS-NRBF] 2.6.2</see>.
/// </remarks>
internal sealed class BinaryLibraryRecord : SerializationRecord
{
    private BinaryLibraryRecord(SerializationRecordId libraryId, string rawLibraryName)
    {
        Id = libraryId;
        RawLibraryName = rawLibraryName;
    }

    private BinaryLibraryRecord(SerializationRecordId libraryId, AssemblyNameInfo libraryName)
    {
        Id = libraryId;
        LibraryName = libraryName;
    }

    public override SerializationRecordType RecordType => SerializationRecordType.BinaryLibrary;

    public override TypeName TypeName
    {
        get
        {
            Debug.Fail("TypeName should never be called on BinaryLibraryRecord");
            return TypeName.Parse(nameof(BinaryLibraryRecord).AsSpan());
        }
    }

    internal string? RawLibraryName { get; }

    internal AssemblyNameInfo? LibraryName { get; }

    /// <inheritdoc />
    public override SerializationRecordId Id { get; }

    internal static BinaryLibraryRecord Decode(BinaryReader reader, PayloadOptions options)
    {
        SerializationRecordId id = SerializationRecordId.Decode(reader);
        string rawName = reader.ReadString();

        if (AssemblyNameInfo.TryParse(rawName.AsSpan(), out AssemblyNameInfo? assemblyNameInfo))
        {
            return new BinaryLibraryRecord(id, assemblyNameInfo);
        }
        else if (!options.UndoTruncatedTypeNames)
        {
            ThrowHelper.ThrowInvalidAssemblyName();
        }

        return new BinaryLibraryRecord(id, rawName);
    }
}
