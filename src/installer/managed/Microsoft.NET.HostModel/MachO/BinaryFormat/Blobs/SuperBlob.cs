// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// See https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/SecurityTool/sharedTool/codesign.c#L61
/// This is the base class for a super blob, which is a blob containing other blobs.
/// This class handles reading and writing of all the sub-blobs.
/// The blob contains the following structure:
/// </summary>
internal abstract unsafe class SuperBlob : Blob
{
    public override uint Size => (uint)(
        base.Size
        + sizeof(uint)
        + _blobIndices.Count * sizeof(BlobIndex)
        + _blobs.Sum(b => b.Size));
    protected uint SubBlobCount => (uint)_blobIndices.Count;
    private List<BlobIndex> _blobIndices;
    private List<Blob> _blobs;

    protected IEnumerable<Blob> Blobs => _blobs;
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
        uint offset = (uint)(sizeof(uint) * 3 + _blobIndices.Count * sizeof(BlobIndex));
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
    protected void AddBlob(Blob blob, CodeDirectorySpecialSlot slot)
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
        uint offset = (uint)(sizeof(uint) * 3 + _blobIndices.Count * sizeof(BlobIndex));
        for (int i = 0; i < _blobIndices.Count; i++)
        {
            _blobIndices[i] = new BlobIndex(_blobIndices[i].Slot, offset);
            offset += _blobs[i].Size;
        }
    }

    /// <summary>
    /// Reads the SuperBlob from the <paramref name="accessor"/> at the specified <paramref name="offset"/>.
    /// </summary>
    protected SuperBlob(MemoryMappedViewAccessor accessor, long offset) : base(accessor, offset)
    {
        // accessor.Read(offset + sizeof(uint), out uint size);
        // size = size.ConvertFromBigEndian();
        accessor.Read(offset + sizeof(uint) * 2, out uint count);
        count = count.ConvertFromBigEndian();
        // throw new NotImplementedException($"SuperBlob not implemented yet. Size: {size}, Count: {count}, accessor: {accessor.Capacity}, offset: {offset}");
        _blobs = new List<Blob>((int)count);
        _blobIndices = new List<BlobIndex>((int)count);
        for (int i = 0; i < count; i++)
        {
            accessor.Read(offset + sizeof(uint) * 3 + (i * sizeof(BlobIndex)), out BlobIndex blobIndex);
            _blobIndices.Add(blobIndex);
            _blobs.Add(Blob.Read(accessor, offset + blobIndex.Offset));
        }
    }

    protected SuperBlob(BlobMagic magic) : base(magic)
    {
        _blobs = new List<Blob>();
        _blobIndices = new List<BlobIndex>();
    }

    public override void Write(MemoryMappedViewAccessor accessor, long offset)
    {
        base.Write(accessor, offset);
        accessor.Write(offset + sizeof(uint) * 2, SubBlobCount.ConvertToBigEndian());
        for (int i = 0; i < SubBlobCount; i++)
        {
            var blobIndex = _blobIndices[i];
            var blob = _blobs[i];
            accessor.Write(offset + sizeof(uint) * 3 + (i * sizeof(BlobIndex)), ref blobIndex);
            blob.Write(accessor, offset + blobIndex.Offset);
        }
    }

    public override void Write(Span<byte> buffer)
    {
        base.Write(buffer);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice((int)base.Size), SubBlobCount);
        buffer = buffer.Slice((int)base.Size + sizeof(uint));
        foreach (var blobIndex in _blobIndices)
        {
            BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)blobIndex.Slot);
            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(sizeof(uint)), blobIndex.Offset);
            buffer = buffer.Slice(sizeof(BlobIndex));
        }
        foreach (var blob in _blobs)
        {
            blob.Write(buffer);
            buffer = buffer.Slice((int)blob.Size);
        }
    }
}
