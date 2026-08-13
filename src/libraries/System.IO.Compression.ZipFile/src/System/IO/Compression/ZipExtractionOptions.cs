// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.IO.Compression;

/// <summary>
/// Options for extracting entries from a zip archive.
/// </summary>
public sealed class ZipExtractionOptions
{
    /// <summary>
    /// Gets or sets the password used to decrypt encrypted entries in the archive.
    /// </summary>
    public ReadOnlyMemory<char> Password { get; set; }

    /// <summary>
    /// Gets or sets the encoding to use when reading entry names and comments.
    /// </summary>
    public Encoding? EntryNameEncoding { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to overwrite existing files during extraction.
    /// </summary>
    public bool OverwriteFiles { get; set; }
}
