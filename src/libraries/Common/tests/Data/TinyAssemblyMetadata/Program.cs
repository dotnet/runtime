// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

internal static class Program
{
    private const uint TimeDateStamp = 0x5931C36A;
    private static readonly Guid s_moduleVersionId = new("06BB2468-908C-48CF-ADE9-DB6DE4614004");

    private static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: TinyAssemblyMetadata_inbuild <ilasm-output> <normalized-output>");
            return 1;
        }

        byte[] image = File.ReadAllBytes(args[0]);
        int timestampOffset;
        int mvidOffset;
        using (var stream = new MemoryStream(image, writable: false))
        using (var pe = new PEReader(stream))
        {
            if (!pe.HasMetadata)
            {
                throw new BadImageFormatException($"ILAsm output '{args[0]}' has no CLI metadata.");
            }

            MetadataReader metadata = pe.GetMetadataReader();
            ModuleDefinition module = metadata.GetModuleDefinition();
            if (!metadata.IsAssembly ||
                metadata.GetString(metadata.GetAssemblyDefinition().Name) != "TinyAssembly" ||
                metadata.GetString(module.Name) != "TinyModule.dll" ||
                module.Mvid.IsNil)
            {
                throw new BadImageFormatException($"ILAsm output '{args[0]}' is not the TinyAssembly/TinyModule.dll fixture.");
            }

            // ECMA-335 II.24.2.5: GUID handles are one-based indices into an array of 16-byte GUIDs.
            const int GuidSize = 16;
            int guidOffset = checked((MetadataTokens.GetHeapOffset(module.Mvid) - 1) * GuidSize);
            if (guidOffset < 0 || guidOffset > metadata.GetHeapSize(HeapIndex.Guid) - GuidSize)
            {
                throw new BadImageFormatException($"ILAsm output '{args[0]}' has an invalid MVID heap index.");
            }

            mvidOffset = checked(pe.PEHeaders.MetadataStartOffset + metadata.GetHeapMetadataOffset(HeapIndex.Guid) + guidOffset);
            if (mvidOffset < pe.PEHeaders.MetadataStartOffset ||
                mvidOffset > checked(pe.PEHeaders.MetadataStartOffset + pe.PEHeaders.MetadataSize - GuidSize))
            {
                throw new BadImageFormatException($"ILAsm output '{args[0]}' has an MVID outside its metadata.");
            }

            // PE/COFF file header: TimeDateStamp follows the two WORD fields.
            timestampOffset = checked(pe.PEHeaders.CoffHeaderStartOffset + 2 * sizeof(ushort));
            if (timestampOffset < 0 || timestampOffset > image.Length - sizeof(uint))
            {
                throw new BadImageFormatException($"ILAsm output '{args[0]}' has an invalid COFF header.");
            }
        }

        BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(timestampOffset, sizeof(uint)), TimeDateStamp);
        s_moduleVersionId.ToByteArray().CopyTo(image, mvidOffset);
        File.WriteAllBytes(args[1], image);

        return 0;
    }
}
