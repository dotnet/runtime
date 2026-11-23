// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ILDisassembler;
// The R2R manifest metadata (/R2RNATIVEMETADATA) and raw access to the image.
internal sealed partial class Dasm
{
    private PEMemoryBlock _r2rBlock;

    private MetadataReader GetR2RManifestReader(PEReader pe)
    {
        DirectoryEntry nativeHeader = pe.PEHeaders.CorHeader?.ManagedNativeHeaderDirectory ?? default;
        const string error = "/r2rnativemetadata only works on non-composite R2R images.";
        if (nativeHeader.Size < 16)
        {
            throw new InvalidOperationException(error);
        }

        PEMemoryBlock hdr = pe.GetSectionData(nativeHeader.RelativeVirtualAddress);
        BlobReader r = hdr.GetReader();
        if (r.Length < 16 || r.ReadUInt32() != 0x00525452) // 'RTR'
        {
            throw new InvalidOperationException(error);
        }

        r.ReadUInt16(); // major
        r.ReadUInt16(); // minor
        r.ReadUInt32(); // flags
        int sections = r.ReadInt32();
        for (int i = 0; i < sections; i++)
        {
            int type = r.ReadInt32();
            int rva = r.ReadInt32();
            int size = r.ReadInt32();
            if (type == 112) // ReadyToRunSectionType.ManifestMetadata
            {
                _r2rBlock = pe.GetSectionData(rva);
                if (size <= 0 || size > _r2rBlock.Length)
                {
                    throw new InvalidOperationException("This R2R file has an invalid R2R manifest metadata size");
                }

                unsafe
                {
                    return new MetadataReader(_r2rBlock.Pointer, size);
                }
            }
        }

        throw new InvalidOperationException("This R2R file does not have an R2R manifest metadata");
    }

    private ImmutableArray<byte> _image;

    internal ImmutableArray<byte> Image => _image.IsDefault ? _image = _pe.GetEntireImage().GetContent() : _image;

    internal ushort U16(int fileOffset) => (ushort)(Image[fileOffset] | Image[fileOffset + 1] << 8);

    internal uint U32(int fileOffset) => (uint)(Image[fileOffset] | Image[fileOffset + 1] << 8 | Image[fileOffset + 2] << 16 | Image[fileOffset + 3] << 24);

    // The file offset of the CLR header.
    internal int CorHeaderFileOffset()
    {
        DirectoryEntry d = _pe.PEHeaders.PEHeader!.CorHeaderTableDirectory;
        _pe.PEHeaders.TryGetDirectoryOffset(d, out int offset);
        return offset;
    }
}
