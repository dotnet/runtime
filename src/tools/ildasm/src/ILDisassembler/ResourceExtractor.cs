// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ILDisassembler;

// Saves the resources embedded in the image as files next to the output: the managed resources as the manifest lists
// them, and the Win32 resources as a .res file at the end of the output. The files are created by the caller.
internal sealed class ResourceExtractor : IMetadataDisassembler
{
    private readonly string _outputFilePath;
    private readonly Func<string, Stream> _createFile;

    /// <param name="outputFilePath">The path of the output file; the resource files are named after it and saved next to it.</param>
    /// <param name="createFile">Creates the file at a path for writing.</param>
    public ResourceExtractor(string outputFilePath, Func<string, Stream> createFile)
    {
        _outputFilePath = outputFilePath;
        _createFile = createFile;
    }

    public DisassemblyPhase Phase => DisassemblyPhase.End;

    // The PE resource directory format (IMAGE_RESOURCE_DIRECTORY, IMAGE_RESOURCE_DIRECTORY_ENTRY and
    // IMAGE_RESOURCE_DATA_ENTRY in the PE/COFF specification).
    private const int ResourceDirectorySize = 16;
    private const int NumberOfNamedEntriesOffset = 12;
    private const int NumberOfIdEntriesOffset = 14;
    private const int ResourceDirectoryEntrySize = 8;
    // In the name of an entry: the name is a string; in the offset of an entry: the entry is a subdirectory.
    private const uint ResourceEntryHighBit = 0x80000000;
    private const uint ResourceEntryOffsetMask = 0x7FFFFFFF;
    private const int DataEntrySizeOffset = 4;

    // The .res file format (RESOURCEHEADER): a type or name given by an ordinal is 0xFFFF followed by the ordinal.
    private const uint ResOrdinalMarker = 0xFFFF;
    // The size of a header whose type and name are both ordinals.
    private const uint ResHeaderSize = 32;

    private enum DumpResult
    {
        NoResources,
        Created,
        Corrupt,
        CannotCreateFile,
    }

    // The file name of the output, which a managed resource file cannot take.
    internal string OutputFileName => Path.GetFileName(_outputFilePath);

    // Saves a managed resource as the file with the given name next to the output.
    internal void SaveManagedResource(string fileName, ReadOnlySpan<byte> data)
    {
        using Stream s = _createFile(Path.Combine(Path.GetDirectoryName(_outputFilePath) ?? "", fileName));
        s.Write(data);
    }

    // Saves the Win32 resources as a .res file next to the output.
    public void Disassemble(Dasm dasm)
    {
        // Whatever follows the last '.' of the whole path is replaced.
        int dot = _outputFilePath.LastIndexOf('.');
        string resFile = (dot < 0 ? _outputFilePath : _outputFilePath.Substring(0, dot)) + ".res";
        switch (DumpResourceToFile(dasm, resFile))
        {
            case DumpResult.Created:
                dasm.PrintLine(dasm.COMMENT($"// WARNING: Created Win32 resource file {resFile}"));
                break;
            case DumpResult.Corrupt:
                dasm.PrintError("// ERROR: Corrupt Win32 resources");
                break;
            case DumpResult.CannotCreateFile:
                dasm.PrintError($"// ERROR: Unable to open file {resFile}");
                break;
        }
    }

    private sealed class ResourceNode
    {
        public uint TypeId = ResOrdinalMarker;
        public uint NameId = ResOrdinalMarker;
        public ushort LangId;
        public string? Type;
        public string? Name;
        public uint DataRva;
        public uint DataSize;
    }

    private DumpResult DumpResourceToFile(Dasm dasm, string fileName)
    {
        DirectoryEntry dir = dasm.PE.PEHeaders.PEHeader!.ResourceTableDirectory;
        if (dir.RelativeVirtualAddress == 0 || dir.Size == 0)
        {
            return DumpResult.NoResources;
        }

        if (!dasm.TryGetBytesAtRva((uint)dir.RelativeVirtualAddress, -1, out ReadOnlySpan<byte> res))
        {
            return DumpResult.NoResources;
        }

        List<ResourceNode> nodes = new();
        try
        {
            // The number of named and of id entries, which follow the directory.
            static ushort Count(ReadOnlySpan<byte> res, int dirOffset)
                => (ushort)(BinaryPrimitives.ReadUInt16LittleEndian(res.Slice(dirOffset + NumberOfNamedEntriesOffset))
                    + BinaryPrimitives.ReadUInt16LittleEndian(res.Slice(dirOffset + NumberOfIdEntriesOffset)));

            static int EntryOffset(int dirOffset, int index) => dirOffset + ResourceDirectorySize + index * ResourceDirectoryEntrySize;

            static ResourceNode Leaf(ReadOnlySpan<byte> res, uint tid, uint nid, uint lid, uint dataEntryOffset)
            {
                ResourceNode n = new() { LangId = (ushort)lid };
                if ((tid & ResourceEntryHighBit) != 0)
                {
                    n.TypeId = 0;
                    n.Type = ResString(res, (int)(tid & ResourceEntryOffsetMask));
                }
                else
                {
                    n.TypeId = ResOrdinalMarker | ((tid & 0xFFFF) << 16);
                }

                if ((nid & ResourceEntryHighBit) != 0)
                {
                    n.NameId = 0;
                    n.Name = ResString(res, (int)(nid & ResourceEntryOffsetMask));
                }
                else
                {
                    n.NameId = ResOrdinalMarker | ((nid & 0xFFFF) << 16);
                }

                n.DataRva = BinaryPrimitives.ReadUInt32LittleEndian(res.Slice((int)dataEntryOffset));
                n.DataSize = BinaryPrimitives.ReadUInt32LittleEndian(res.Slice((int)dataEntryOffset + DataEntrySizeOffset));
                return n;
            }

            int nTypes = Count(res, 0);
            for (int i = 0; i < nTypes; i++)
            {
                int te = EntryOffset(0, i);
                uint typeId = BinaryPrimitives.ReadUInt32LittleEndian(res.Slice(te));
                uint typeOff = BinaryPrimitives.ReadUInt32LittleEndian(res.Slice(te + 4));
                if ((typeOff & ResourceEntryHighBit) == 0)
                {
                    nodes.Add(Leaf(res, typeId, 0, 0, typeOff));
                    continue;
                }

                int nameDir = (int)(typeOff & ResourceEntryOffsetMask);
                int nNames = Count(res, nameDir);
                for (int j = 0; j < nNames; j++)
                {
                    int ne = EntryOffset(nameDir, j);
                    uint nameId = BinaryPrimitives.ReadUInt32LittleEndian(res.Slice(ne));
                    uint nameOff = BinaryPrimitives.ReadUInt32LittleEndian(res.Slice(ne + 4));
                    if ((nameOff & ResourceEntryHighBit) == 0)
                    {
                        nodes.Add(Leaf(res, typeId, nameId, 0, nameOff));
                        continue;
                    }

                    int langDir = (int)(nameOff & ResourceEntryOffsetMask);
                    int nLangs = Count(res, langDir);
                    for (int k = 0; k < nLangs; k++)
                    {
                        int le = EntryOffset(langDir, k);
                        uint langOff = BinaryPrimitives.ReadUInt32LittleEndian(res.Slice(le + 4));
                        if ((langOff & ResourceEntryHighBit) == 0)
                        {
                            nodes.Add(Leaf(res, typeId, nameId, BinaryPrimitives.ReadUInt32LittleEndian(res.Slice(le)), langOff));
                        }
                    }
                }
            }
        }
        catch (Exception e) when (e is ArgumentException or IndexOutOfRangeException)
        {
            return DumpResult.Corrupt;
        }

        if (nodes.Count == 0)
        {
            return DumpResult.NoResources;
        }

        Stream f;
        try
        {
            f = _createFile(fileName);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return DumpResult.CannotCreateFile;
        }

        using (f)
        using (BinaryWriter w = new(f))
        {
            // An empty leading resource: data size 0, then a header with type and name ordinal 0.
            w.Write(0u);
            w.Write(ResHeaderSize);
            w.Write(ResOrdinalMarker);
            w.Write(ResOrdinalMarker);
            w.Write(new byte[ResHeaderSize - 16]);
            foreach (ResourceNode n in nodes)
            {
                if (!dasm.TryGetBytesAtRva(n.DataRva, (int)n.DataSize, out ReadOnlySpan<byte> data))
                {
                    continue;
                }

                uint headerSize = ResHeaderSize;
                if (n.Type is not null)
                {
                    headerSize += (uint)((n.Type.Length + 1) * 2 - 4);
                }

                if (n.Name is not null)
                {
                    headerSize += (uint)((n.Name.Length + 1) * 2 - 4);
                }

                // The header size includes the padding that aligns the header to a DWORD boundary.
                headerSize = (headerSize + 3) & ~3u;
                w.Write(n.DataSize);
                w.Write(headerSize);
                uint filler = 0;
                if (n.Type is not null)
                {
                    w.Write(Encoding.Unicode.GetBytes(n.Type + "\0"));
                    filler += (uint)n.Type.Length + 1;
                }
                else
                {
                    w.Write(n.TypeId);
                }

                if (n.Name is not null)
                {
                    w.Write(Encoding.Unicode.GetBytes(n.Name + "\0"));
                    filler += (uint)n.Name.Length + 1;
                }
                else
                {
                    w.Write(n.NameId);
                }

                if ((filler & 1) != 0)
                {
                    w.Write((ushort)0);
                }

                w.Write(0u);              // data version
                w.Write((ushort)0);       // memory flags
                w.Write(n.LangId);
                w.Write(0u);              // version
                w.Write(0u);              // characteristics
                w.Write(data);
                uint pad = n.DataSize & 3;
                if (pad != 0)
                {
                    w.Write(new byte[4 - pad]);
                }
            }
        }

        return DumpResult.Created;
    }

    private static string ResString(ReadOnlySpan<byte> res, int offset)
    {
        int len = BinaryPrimitives.ReadUInt16LittleEndian(res.Slice(offset));
        return Encoding.Unicode.GetString(res.Slice(offset + 2, len * 2));
    }
}
