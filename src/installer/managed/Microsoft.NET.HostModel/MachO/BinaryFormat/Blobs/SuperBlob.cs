// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// See https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/SecurityTool/sharedTool/codesign.c#L61
/// This is the base class for a super blob, which is a blob containing other blobs.
/// This class handles reading and writing of all the sub-blobs.
/// The blob contains the following structure:
/// </summary>
internal class SuperBlob : IBlob
{
    /// <inheritdoc />
    public BlobMagic Magic { get; }

    /// <inheritdoc />
    public uint Size => (uint)(
        sizeof(uint) + sizeof(uint) // magic + size
        + sizeof(uint) // sub blob count
        + _blobIndices.Count * BlobIndex.Size
        + _blobs.Sum(b => b.Size));

    /// <summary>
    /// Gets the number of sub-blobs in this super blob.
    /// </summary>
    protected uint SubBlobCount => (uint)_blobIndices.Count;

    private List<BlobIndex> _blobIndices;
    private List<IBlob> _blobs;

    protected IEnumerable<IBlob> Blobs => _blobs;
    protected IEnumerable<BlobIndex> BlobIndices => _blobIndices;

    protected void RemoveBlob(CodeDirectorySpecialSlot slot)
    {
        int index = _blobIndices.FindIndex(b => b.Slot == slot);
        if (index < 0)
        {
            return;
        }
        _blobIndices.RemoveAt(index);
        _blobs.RemoveAt(index);
        uint offset = (uint)(sizeof(uint) * 3 + _blobIndices.Count * BlobIndex.Size);
        for (int i = index + 1; i < _blobIndices.Count; i++)
        {
            _blobIndices[i] = new BlobIndex(_blobIndices[i].Slot, offset);
            offset += _blobs[i].Size;
        }
    }

    /// <summary>
    /// Inserts a blob into the SuperBlob such that the order of the blobs is maintained.
    /// Will throw an exception if a blob with the same slot already exists.
    /// </summary>
    /// <param name="blob">The blob to insert.</param>
    /// <param name="slot">The slot to insert the blob into. The Offset property does not need to be set.</param>
    protected void AddBlob(IBlob blob, CodeDirectorySpecialSlot slot)
    {
        // Find the insertion point for the new blob based on slot order
        int insertionIndex = 0;
        while (insertionIndex < _blobIndices.Count && _blobIndices[insertionIndex].Slot < slot)
        {
            insertionIndex++;
        }

        // Check if a blob with the same slot already exists
        if (insertionIndex < _blobIndices.Count && _blobIndices[insertionIndex].Slot == slot)
        {
            throw new InvalidOperationException($"Blob with slot {slot} already exists.");
        }

        // Insert the new blob at the correct position
        _blobIndices.Insert(insertionIndex, new BlobIndex(slot, 0)); // Temporary offset
        _blobs.Insert(insertionIndex, blob);

        // Recalculate all offsets
        uint offset = (uint)(sizeof(uint) * 3 + _blobIndices.Count * BlobIndex.Size);
        for (int i = 0; i < _blobIndices.Count; i++)
        {
            _blobIndices[i] = new BlobIndex(_blobIndices[i].Slot, offset);
            offset += _blobs[i].Size;
        }
    }

    public SuperBlob(BlobMagic magic, List<BlobIndex> blobIndices, List<IBlob> blobs)
    {
        if (blobIndices.Count != blobs.Count)
        {
            throw new ArgumentException("Blob indices and blobs count must match.");
        }
        Magic = magic;
        _blobs = blobs;
        _blobIndices = blobIndices;
    }

    protected SuperBlob(SuperBlob other)
    {
        Magic = other.Magic;
        _blobs = other._blobs;
        _blobIndices = other._blobIndices;
    }

    protected SuperBlob(BlobMagic magic) : this(magic, [], [])
    {
    }

    public int Write(IMachOFileWriter file, long offset)
    {
        // Write magic and size
        file.WriteUInt32BigEndian(offset, (uint)Magic);
        file.WriteUInt32BigEndian(offset + sizeof(uint), Size);

        // Write sub blob count
        uint count = SubBlobCount;
        file.WriteUInt32BigEndian(offset + sizeof(uint) * 2, count);

        // Write blob indices
        for (int i = 0; i < SubBlobCount; i++)
        {
            var blobIndex = _blobIndices[i];
            file.Write(offset + sizeof(uint) * 3 + (i * BlobIndex.Size), ref blobIndex);
        }

        // Write blobs
        long currentOffset = offset + sizeof(uint) * 3 + (SubBlobCount * BlobIndex.Size);
        for (int i = 0; i < SubBlobCount; i++)
        {
            currentOffset += _blobs[i].Write(file, currentOffset);
        }

        return (int)Size;
    }
}
