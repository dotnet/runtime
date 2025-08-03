// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// This class represents a simple blob with unstructured byte array data.
/// </summary>
internal class SimpleBlob : IBlob
{
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
    public BlobMagic Magic { get; }

    /// <inheritdoc />
    public uint Size => sizeof(uint) + sizeof(uint) + (uint)Data.Length;

    /// <summary>
    /// Gets the data stored in the blob after the 8-byte header.
    /// </summary>
    public byte[] Data { get; }

    /// <inheritdoc />
    public int Write(IMachOFileWriter file, long offset)
    {
        int bytesWritten = 0;

        file.WriteUInt32BigEndian(offset, (uint)Magic);
        bytesWritten += sizeof(uint);

        file.WriteUInt32BigEndian(offset + sizeof(uint), Size);
        bytesWritten += sizeof(uint);

        if (Data.Length > 0)
        {
            file.WriteExactly(offset + sizeof(uint) * 2, Data);
            bytesWritten += Data.Length;
        }
        Debug.Assert(bytesWritten == Size, "The number of bytes written does not match the expected size of the blob.");

        return bytesWritten;
    }

    public static SimpleBlob Read(IMachOFileReader reader, long offset)
    {
        var blobMagic = (BlobMagic)reader.ReadUInt32BigEndian(offset);
        var size = reader.ReadUInt32BigEndian(offset + sizeof(uint));

        uint dataSize = size - sizeof(uint) - sizeof(uint);
        byte[] data = new byte[dataSize];
        if (dataSize > 0)
            reader.ReadExactly(offset + sizeof(uint) * 2, data);

        return new SimpleBlob(blobMagic, data);
    }
}
