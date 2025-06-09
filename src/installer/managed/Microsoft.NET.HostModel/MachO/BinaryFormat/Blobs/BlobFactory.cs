// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// Factory for creating blob instances.
/// </summary>
internal static class BlobFactory
{
    /// <summary>
    /// Reads a blob from a file at the specified offset.
    /// </summary>
    /// <param name="reader">The memory-mapped view accessor to read from.</param>
    /// <param name="offset">The offset to start reading from.</param>
    /// <returns>The created blob.</returns>
    public static IBlob ReadBlob(IMachOFileReader reader, long offset)
    {
        var magic = (BlobMagic)reader.ReadUInt32BigEndian(offset);
        return magic switch
        {
            BlobMagic.CodeDirectory => new CodeDirectoryBlob(ReadSimpleBlob(reader, offset)),
            BlobMagic.Requirements => new RequirementsBlob(ReadSuperBlob(reader, offset)),
            BlobMagic.Entitlements => new EntitlementsBlob(ReadSimpleBlob(reader, offset)),
            BlobMagic.DerEntitlements => new DerEntitlementsBlob(ReadSimpleBlob(reader, offset)),
            BlobMagic.CmsWrapper => new CmsWrapperBlob(ReadSimpleBlob(reader, offset)),
            BlobMagic.EmbeddedSignature => new EmbeddedSignatureBlob(ReadSuperBlob(reader, offset)),
            _ => ReadSimpleBlob(reader, offset)
        };
    }

    private static SimpleBlob ReadSimpleBlob(IMachOFileReader reader, long offset)
    {
        var blobMagic = (BlobMagic)reader.ReadUInt32BigEndian(offset);
        var size = reader.ReadUInt32BigEndian(offset + sizeof(uint));

        uint dataSize = size - sizeof(uint) - sizeof(uint);
        byte[] data = new byte[dataSize];
        if (dataSize > 0)
            reader.ReadExactly(offset + sizeof(uint) * 2, data);

        return new SimpleBlob(blobMagic, data);
    }

    /// <summary>
    /// Creates a SuperBlob by reading from a memory-mapped file.
    /// </summary>
    private static SuperBlob ReadSuperBlob(IMachOFileReader reader, long offset)
    {
        BlobMagic magic = (BlobMagic)reader.ReadUInt32BigEndian(offset);
        uint size = reader.ReadUInt32BigEndian(offset + sizeof(BlobMagic));
        uint count = reader.ReadUInt32BigEndian(offset + sizeof(BlobMagic) + sizeof(uint));

        var blobs = new List<IBlob>((int)count);
        var blobIndices = new List<BlobIndex>((int)count);
        for (int i = 0; i < count; i++)
        {
            reader.Read(offset + sizeof(uint) * 3 + (i * BlobIndex.Size), out BlobIndex blobIndex);
            blobIndices.Add(blobIndex);
            blobs.Add(ReadBlob(reader, offset + blobIndex.Offset));
        }
        Debug.Assert(size == sizeof(uint) + sizeof(uint) + sizeof(uint)
                             + blobIndices.Count * BlobIndex.Size
                             + blobs.Sum(b => b.Size));

        return new SuperBlob(magic, blobIndices, blobs);
    }
}
