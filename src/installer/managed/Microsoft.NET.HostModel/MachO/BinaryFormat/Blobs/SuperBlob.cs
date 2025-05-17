// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// See https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/SecurityTool/sharedTool/codesign.c#L61
/// </summary>
internal abstract unsafe class SuperBlob : Blob
{
    public override uint Size => (uint)(
        base.Size
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

    protected void AddBlob(Blob blob, CodeDirectorySpecialSlot slot)
    {
        _blobIndices.Add(new BlobIndex(slot, 0));
        _blobs.Add(blob);
        uint offset = (uint)(sizeof(uint) * 3 + _blobIndices.Count * sizeof(BlobIndex));
        for (int i = 0; i < _blobIndices.Count; i++)
        {
            var existingBlobIndex = _blobIndices[i];
            var existingBlob = _blobs[i];
            if (existingBlobIndex.Slot == slot)
            {
                throw new InvalidOperationException($"Blob with slot {slot} already exists.");
            }
            if (existingBlobIndex.Slot < slot)
            {
                _blobIndices.Insert(i, new BlobIndex(slot, offset));
                _blobs.Insert(i, blob);
                offset += blob.Size;
                i++;
            }
            _blobIndices[i] = new BlobIndex(existingBlobIndex.Slot, offset);
            offset += existingBlob.Size;
        }
    }

    protected SuperBlob(MemoryMappedViewAccessor accessor, long offset) : base(accessor, offset)
    {
        accessor.Read(offset + sizeof(uint) * 2, out uint count);
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

    public override void Write(Stream stream)
    {
        base.Write(stream);
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(SubBlobCount.ConvertToBigEndian());
            foreach (var blobIndex in _blobIndices)
            {
                writer.Write(((uint)blobIndex.Slot).ConvertToBigEndian());
                writer.Write(blobIndex.Offset.ConvertToBigEndian());
            }
            writer.Flush();
        }
        foreach (var blob in _blobs)
        {
            blob.Write(stream);
        }
    }
}
