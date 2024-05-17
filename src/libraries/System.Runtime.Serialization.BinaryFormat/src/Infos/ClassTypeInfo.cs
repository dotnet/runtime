// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Identifies a class by it's name and library id.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/844b24dd-9f82-426e-9b98-05334307a239">
///    [MS-NRBF] 2.1.1.8
///   </see>
///  </para>
/// </remarks>
[DebuggerDisplay("{TypeName}")]
internal sealed class ClassTypeInfo
{
    internal ClassTypeInfo(TypeName typeName, int libraryId)
    {
        TypeName = typeName;
        LibraryId = libraryId;
    }

    internal TypeName TypeName { get; }

    internal int LibraryId { get; }

    internal static ClassTypeInfo Parse(BinaryReader reader, PayloadOptions options)
        => new(reader.ReadTypeName(options), reader.ReadInt32());
}
