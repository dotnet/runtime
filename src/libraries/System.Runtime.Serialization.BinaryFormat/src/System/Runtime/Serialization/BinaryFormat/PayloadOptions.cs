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
    /// This flag allows the users to undo truncated type names.
    /// It's useful for reading resources were generated with invalid generic type names.
    /// </summary>
    /// <remarks>
    /// Example:
    /// TypeName: "Namespace.TypeName`1[[Namespace.GenericArgName"
    /// LibraryName: "AssemblyName]]"
    /// Is combined into "Namespace.TypeName`1[[Namespace.GenericArgName, AssemblyName]]"
    /// </remarks>
    public bool UndoTruncatedTypeNames { get; set; }
}
