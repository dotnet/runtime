// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.IO.Compression;

/// <summary>
/// Options for creating a zip archive from a directory.
/// </summary>
public sealed class ZipFileCreationOptions
{
    /// <summary>
    /// Gets or sets the password used to encrypt entries in the archive.
    /// </summary>
    public ReadOnlyMemory<char> Password { get; set; }

    /// <summary>
    /// Gets or sets the encryption method to use when creating encrypted entries.
    /// </summary>
    public ZipEncryptionMethod EncryptionMethod { get; set; }

    /// <summary>
    /// Gets or sets the compression level to use when creating entries.
    /// </summary>
    public CompressionLevel CompressionLevel { get; set; }

    /// <summary>
    /// Gets or sets the encoding to use for entry names.
    /// </summary>
    public Encoding? EntryNameEncoding { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include the base directory name as a prefix in the entry names.
    /// </summary>
    public bool IncludeBaseDirectory { get; set; }
}
