// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    /// Show original source lines as comments (requires PDB).
    /// </summary>
    public bool ShowSource { get; set; }

    /// <summary>
    /// Include references to original source lines.
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
    /// Output custom attribute blobs in verbal form (default: binary).
    /// </summary>
    public bool CustomAttributesVerbal { get; set; }

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
    /// Output full list of types (to preserve type ordering in round-trip).
    /// </summary>
    public bool TypeList { get; set; }

    /// <summary>
    /// Include file headers information in the output.
    /// </summary>
    public bool Headers { get; set; }

    /// <summary>
    /// Disassemble the specified item only (class[::method[(sig)]]).
    /// </summary>
    public string? Item { get; set; }

    /// <summary>
    /// Include statistics on the image.
    /// </summary>
    public bool Stats { get; set; }

    /// <summary>
    /// Include list of classes defined in the module.
    /// </summary>
    public bool ClassList { get; set; }

    /// <summary>
    /// Show MetaData (MDHEADER, HEX, CSV, UNREX, SCHEMA, RAW, HEAPS, VALIDATE).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Output in HTML format.
    /// </summary>
    public bool Html { get; set; }

    /// <summary>
    /// Output in RTF format.
    /// </summary>
    public bool Rtf { get; set; }

    /// <summary>
    /// Use UTF-8 encoding for output (default).
    /// </summary>
    public bool Utf8 { get; set; } = true;

    /// <summary>
    /// Use Unicode encoding for output.
    /// </summary>
    public bool Unicode { get; set; }
}
