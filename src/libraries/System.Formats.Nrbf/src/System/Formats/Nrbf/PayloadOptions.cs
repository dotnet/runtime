// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

namespace System.Formats.Nrbf;

/// <summary>
/// Defines configuration options for decoding NRBF payloads.
/// </summary>
public sealed class PayloadOptions
{
    /// <summary>
    /// Initializes a <see cref="PayloadOptions"/> instance with default values.
    /// </summary>
    public PayloadOptions() { }

    /// <summary>
    /// Gets or sets configuration options for parsing <see cref="TypeName"/> instances.
    /// </summary>
    public TypeNameParseOptions? TypeNameParseOptions { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether type-name truncation is undone.
    /// </summary>
    /// <value><see langword="true" /> if truncated type names should be reassembled; otherwise, <see langword="false" />. The default value is <see langword="false" />.</value>
    /// <remarks>
    /// <para>
    /// Example:
    /// TypeName: "Namespace.TypeName`1[[Namespace.GenericArgName"
    /// LibraryName: "AssemblyName]]"
    /// Is combined into "Namespace.TypeName`1[[Namespace.GenericArgName, AssemblyName]]"
    /// </para>
    /// <para>
    /// Setting this to <see langword="true" /> can render <see cref="NrbfDecoder"/> susceptible to Denial of Service
    /// attacks when parsing or handling malicious input.
    /// </para>
    /// </remarks>
    public bool UndoTruncatedTypeNames { get; set; }
}
