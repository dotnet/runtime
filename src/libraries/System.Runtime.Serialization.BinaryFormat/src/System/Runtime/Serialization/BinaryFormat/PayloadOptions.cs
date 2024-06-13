// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

namespace System.Runtime.Serialization.BinaryFormat;

#if SYSTEM_RUNTIME_SERIALIZATION_BINARYFORMAT
public
#else
internal
#endif
sealed class PayloadOptions
{
    public PayloadOptions() { }

    public TypeNameParseOptions? TypeNameParseOptions { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether type name truncation is undone.
    /// </summary>
    /// <value><see langword="true" /> if truncated type names should be reassembled; otherwise, <see langword="false" />.</value>
    /// <remarks>
    /// Example:
    /// TypeName: "Namespace.TypeName`1[[Namespace.GenericArgName"
    /// LibraryName: "AssemblyName]]"
    /// Is combined into "Namespace.TypeName`1[[Namespace.GenericArgName, AssemblyName]]"
    /// </remarks>
    public bool UndoTruncatedTypeNames { get; set; }
}
