// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Microsoft.NET.HostModel.AppHost;
using Microsoft.NET.HostModel.MachO;

internal abstract class Blob
{
    /// <summary>
    /// The magic number for this blob to identify the type of blob.
    /// </summary>
    public BlobMagic Magic { get; }
    /// <summary>
    /// The size of the entire blob. Derived blobs that contain additional data should override this property
    /// to include the size of their data.
    /// </summary>
    public virtual uint Size => sizeof(uint) + sizeof(uint);

    public static Blob Read(MemoryMappedViewAccessor accessor, long offset)
    {
        accessor.Read(offset, out uint magic);
        return (BlobMagic)magic.ConvertFromBigEndian() switch
        {
            BlobMagic.CodeDirectory => new CodeDirectoryBlob(accessor, offset),
            BlobMagic.Requirements => new RequirementsBlob(accessor, offset),
            BlobMagic.Entitlements => new EntitlementsBlob(accessor, offset),
            BlobMagic.DerEntitlements => new DerEntitlementsBlob(accessor, offset),
            BlobMagic.CmsWrapper => new CmsWrapperBlob(accessor, offset),
            BlobMagic.EmbeddedSignature => new EmbeddedSignatureBlob(accessor, offset),
            _ => new SimpleBlob(accessor, offset)
        };
    }

    protected Blob(BlobMagic magic)
    {
        Magic = magic;
    }

    protected Blob(MemoryMappedViewAccessor accessor, long offset)
    {
        accessor.Read(offset, out uint magic);
        if (this.Magic != (BlobMagic)magic.ConvertFromBigEndian())
        {
            throw new InvalidDataException($"Invalid magic number for {this.GetType().Name} Blob: {magic}");
        }
    }

    public virtual void Write(MemoryMappedViewAccessor accessor, long offset)
    {
        accessor.Write(offset, ((uint)Magic).ConvertToBigEndian());
        accessor.Write(offset + sizeof(uint), Size.ConvertToBigEndian());
    }

    public virtual void Write(Stream stream)
    {
        using var writer = new StreamWriter(stream);
        writer.Write(((uint)Magic).ConvertToBigEndian());
        writer.Write(Size.ConvertToBigEndian());
    }

    public virtual byte[] GetBytes()
    {
        using var stream = new MemoryStream();
        Write(stream);
        return stream.ToArray();
    }
}
