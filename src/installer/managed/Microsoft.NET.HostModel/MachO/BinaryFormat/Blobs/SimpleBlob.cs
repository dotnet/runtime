// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// This class represents a simple blob with unstructured byte array data.
/// </summary>
internal class SimpleBlob : IBlob
{
    /// <inheritdoc />
    public BlobMagic Magic { get; }

    /// <inheritdoc />
    public uint Size => sizeof(uint) + sizeof(uint) + (uint)Data.Length;

    /// <summary>
    /// Gets the data stored in the blob after the 8-byte header.
    /// </summary>
    public byte[] Data { get; }

    public SimpleBlob(BlobMagic magic, byte[] data)
    {
        Magic = magic;
        Data = data;
    }

    protected SimpleBlob(SimpleBlob blob)
        : this(blob.Magic, blob.Data)
    {
    }

    /// <inheritdoc />
    public int Write(IMachOFileWriter file, long offset)
    {
        int bytesWritten = 0;

        file.WriteUInt32BigEndian(offset, (uint)Magic);
        bytesWritten += sizeof(uint);

        file.WriteUInt32BigEndian(offset + sizeof(uint), Size);
        bytesWritten += sizeof(uint);

        file.WriteExactly(offset + sizeof(uint) * 2, Data);
        bytesWritten += Data.Length;

        return bytesWritten;
    }
}
