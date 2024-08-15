// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Formats.Nrbf.Utils;

namespace System.Formats.Nrbf;

/// <summary>
/// Identifies a class by it's name and library id.
/// </summary>
/// <remarks>
/// ClassTypeInfo structures are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/844b24dd-9f82-426e-9b98-05334307a239">[MS-NRBF] 2.1.1.8</see>.
/// </remarks>
[DebuggerDisplay("{TypeName}")]
internal sealed class ClassTypeInfo
{
    internal ClassTypeInfo(TypeName typeName) => TypeName = typeName;

    internal TypeName TypeName { get; }

    internal static ClassTypeInfo Decode(BinaryReader reader, PayloadOptions options, RecordMap recordMap)
    {
        string rawName = reader.ReadString();
        SerializationRecordId libraryId = SerializationRecordId.Decode(reader);

        BinaryLibraryRecord library = (BinaryLibraryRecord)recordMap[libraryId];

        return new ClassTypeInfo(rawName.ParseNonSystemClassRecordTypeName(library, options));
    }
}
