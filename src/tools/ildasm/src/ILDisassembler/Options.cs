// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace ILDisassembler;

/// <summary>
/// Options for controlling IL disassembly output.
/// </summary>
public sealed class Options
{
    /// <summary>
    /// Show actual bytes (in hex) as instruction comments.
    /// </summary>
    public bool ShowBytes { get; set; }

    /// <summary>
    /// Show exception handling clauses in raw form.
    /// </summary>
    public bool RawExceptionHandling { get; set; }

    /// <summary>
    /// Show metadata tokens of classes and members.
    /// </summary>
    public bool ShowTokens { get; set; }

    /// <summary>
    /// Show original source lines as comments. Accepted for compatibility; this has no effect on the output.
    /// </summary>
    public bool ShowSource { get; set; }

    /// <summary>
    /// Include references to original source lines (.line directives) from the portable PDB, when one is found:
    /// embedded, at the path in the debug directory, or next to the input file.
    /// </summary>
    public bool ShowLineNumbers { get; set; }

    /// <summary>
    /// Visibility filter (PUB, PRI, FAM, ASM, FAA, FOA, PSC).
    /// </summary>
    public string? Visibility { get; set; }

    /// <summary>
    /// Only disassemble public items.
    /// </summary>
    public bool PublicOnly { get; set; }

    /// <summary>
    /// Include all names in single quotes.
    /// </summary>
    public bool QuoteAllNames { get; set; }

    /// <summary>
    /// Suppress output of custom attributes.
    /// </summary>
    public bool NoCustomAttributes { get; set; }

    /// <summary>
    /// Output the metadata from the R2R Native manifest.
    /// </summary>
    public bool R2RNativeMetadata { get; set; }

    /// <summary>
    /// Suppress IL assembler code output.
    /// </summary>
    public bool NoIL { get; set; }

    /// <summary>
    /// Use forward class declaration.
    /// </summary>
    public bool ForwardDeclarations { get; set; }

    /// <summary>
    /// Disassemble the specified item only (class[::method[(sig)]]).
    /// </summary>
    public string? Item { get; set; }

    /// <summary>
    /// The name of the input file, shown in the output and in messages. A portable PDB for <see cref="ShowLineNumbers"/>
    /// is looked for only when it is set.
    /// </summary>
    public string? InputFileName { get; set; }

    /// <summary>
    /// Opens a portable PDB file by path, or returns null if there is none. Without it, only an embedded PDB is used.
    /// </summary>
    public Func<string, Stream?>? PdbFileStreamProvider { get; set; }

    /// <summary>
    /// Also write the error messages into the output (in addition to the error writer).
    /// </summary>
    public bool WriteErrorsToOutput { get; set; }

    /// <summary>
    /// What ends the lines of the output.
    /// </summary>
    public string NewLine { get; set; } = "\n";

    // The format of the output.
    internal IOutputFormatter Formatter { get; set; } = PlainTextFormatter.Instance;

    // How custom attribute blobs and permission set blobs are shown.
    internal ICustomAttributeDisassembler CustomAttributeDisassembler { get; set; } = ByteArrayCustomAttributeDisassembler.Instance;

    internal IPermissionSetDisassembler PermissionSetDisassembler { get; set; } = VerbalCustomAttributeDisassembler.Instance;

    // Operations over the whole image, such as the headers or statistics, that add to the disassembly.
    internal IReadOnlyList<IMetadataDisassembler> MetadataDisassemblers { get; set; } = [];
}
