// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// Represents a blob in a Mach-O file.
/// </summary>
internal interface IBlob
{
    /// <summary>
    /// The magic number for this blob to identify the type of blob.
    /// </summary>
    BlobMagic Magic { get; }

    /// <summary>
    /// The size of the entire blob.
    /// </summary>
    uint Size { get; }

    /// <summary>
    /// Writes the blob to the specified writer.
    /// </summary>
    /// <param name="writer">The IMachOFileWriter to which the blob will be written.</param>
    /// <param name="offset">The offset at which to write the blob.</param>
    /// <returns>The number of bytes written.</returns>
    int Write(IMachOFileWriter writer, long offset);
}
