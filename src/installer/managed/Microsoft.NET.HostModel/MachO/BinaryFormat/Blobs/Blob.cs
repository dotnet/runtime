// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.IO.MemoryMappedFiles;
using Microsoft.NET.HostModel.MachO;

/// <summary>
/// A base class for blobs in a Mach-O file.
/// Derived classes should implement specific blob types. If the blob has additional data or fields, the derived class
/// should override the Size property to include the size of their data, and implement the Write method to write their data.
/// This class implements the basic functionality of reading and writing the magic number and size of the blob.
/// All blobs are expected to be in big-endian format.
/// </summary>
internal abstract class Blob
{
    /// <summary>
    /// The magic number for this blob to identify the type of blob.
    /// </summary>
    public BlobMagic Magic { get; }

    /// <summary>
    /// The size of the entire blob.
    /// Derived blobs that contain additional data should override this property to include the size of their data.
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

    /// <summary>
    /// Reads the Magic number from the <paramref name="accessor"/> at the specified <paramref name="offset"/>.
    /// Does not read or set the size of the blob.
    /// </summary>
    protected Blob(MemoryMappedViewAccessor accessor, long offset)
    {
        accessor.Read(offset, out uint magic);
        Magic = (BlobMagic)magic.ConvertFromBigEndian();
    }

    /// <summary>
    /// Writes the blob to the <paramref name="accessor"/> at the specified <paramref name="offset"/>.
    /// </summary>
    /// <param name="accessor">The <see cref="MemoryMappedViewAccessor"/> to write the blob to.</param>
    /// <param name="offset">The base offset of the start of the blob. The Magic number should go here.</param>
    public virtual unsafe void Write(MemoryMappedViewAccessor accessor, long offset)
    {
        accessor.Write(offset, ((uint)Magic).ConvertToBigEndian());
        accessor.Write(offset + sizeof(uint), Size.ConvertToBigEndian());
    }

    /// <summary>
    /// Writes the blob at the beginning of the <paramref name="buffer"/>.
    /// </summary>
    public virtual void Write(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)Magic);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(sizeof(uint)), Size);
    }

    /// <summary>
    /// Returns the byte array representation of the blob.
    /// </summary>
    public virtual byte[] GetBytes()
    {
        var bytes = new byte[Size];
        Write(bytes);
        return bytes;
    }
}
