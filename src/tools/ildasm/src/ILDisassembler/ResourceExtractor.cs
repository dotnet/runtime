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
        uint ret = DumpResourceToFile(dasm, resFile);
        switch (ret)
        {
            case 1:
                dasm.PrintLine(dasm.COMMENT($"// WARNING: Created Win32 resource file {resFile}"));
                break;
            case 0xDFFFFFFF:
                dasm.PrintError("// ERROR: Corrupt Win32 resources");
                break;
            case 0xEFFFFFFF:
                dasm.PrintError($"// ERROR: Unable to open file {resFile}");
                break;
        }
    }

    private sealed class ResourceNode
    {
        public uint TypeId = 0xFFFF;
        public uint NameId = 0xFFFF;
        public ushort LangId;
        public string? Type;
        public string? Name;
        public uint DataRva;
        public uint DataSize;
    }

    private uint DumpResourceToFile(Dasm dasm, string fileName)
    {
        DirectoryEntry dir = dasm.PE.PEHeaders.PEHeader!.ResourceTableDirectory;
        if (dir.RelativeVirtualAddress == 0 || dir.Size == 0)
        {
            return 0;
        }

        if (!dasm.TryGetBytesAtRva((uint)dir.RelativeVirtualAddress, -1, out ReadOnlySpan<byte> res))
        {
            return 0;
        }

        List<ResourceNode> nodes = new();
        try
        {
            // IMAGE_RESOURCE_DIRECTORY: the number of named and of id entries, followed by the entries.
            static ushort Count(ReadOnlySpan<byte> res, int dirOffset)
                => (ushort)(BinaryPrimitives.ReadUInt16LittleEndian(res.Slice(dirOffset + 12)) + BinaryPrimitives.ReadUInt16LittleEndian(res.Slice(dirOffset + 14)));

            static ResourceNode Leaf(ReadOnlySpan<byte> res, uint tid, uint nid, uint lid, uint dataEntryOffset)
            {
                ResourceNode n = new() { LangId = (ushort)lid };
                if ((tid & 0x80000000) != 0)
                {
                    n.TypeId = 0;
                    n.Type = ResString(res, (int)(tid & 0x7FFFFFFF));
                }
                else
                {
                    n.TypeId = 0xFFFF | ((tid & 0xFFFF) << 16);
                }

                if ((nid & 0x80000000) != 0)
                {
                    n.NameId = 0;
                    n.Name = ResString(res, (int)(nid & 0x7FFFFFFF));
                }
                else
                {
                    n.NameId = 0xFFFF | ((nid & 0xFFFF) << 16);
                }

                n.DataRva = BinaryPrimitives.ReadUInt32LittleEndian(res.Slice((int)dataEntryOffset));
                n.DataSize = BinaryPrimitives.ReadUInt32LittleEndian(res.Slice((int)dataEntryOffset + 4));
                return n;
            }

            int nTypes = Count(res, 0);
            for (int i = 0; i < nTypes; i++)
            {
                int te = 16 + i * 8;
                uint typeId = BinaryPrimitives.ReadUInt32LittleEndian(res.Slice(te));
                uint typeOff = BinaryPrimitives.ReadUInt32LittleEndian(res.Slice(te + 4));
                if ((typeOff & 0x80000000) == 0)
                {
                    nodes.Add(Leaf(res, typeId, 0, 0, typeOff));
                    continue;
                }

                int nameDir = (int)(typeOff & 0x7FFFFFFF);
                int nNames = Count(res, nameDir);
                for (int j = 0; j < nNames; j++)
                {
                    int ne = nameDir + 16 + j * 8;
                    uint nameId = BinaryPrimitives.ReadUInt32LittleEndian(res.Slice(ne));
                    uint nameOff = BinaryPrimitives.ReadUInt32LittleEndian(res.Slice(ne + 4));
                    if ((nameOff & 0x80000000) == 0)
                    {
                        nodes.Add(Leaf(res, typeId, nameId, 0, nameOff));
                        continue;
                    }

                    int langDir = (int)(nameOff & 0x7FFFFFFF);
                    int nLangs = Count(res, langDir);
                    for (int k = 0; k < nLangs; k++)
                    {
                        int le = langDir + 16 + k * 8;
                        uint langOff = BinaryPrimitives.ReadUInt32LittleEndian(res.Slice(le + 4));
                        if ((langOff & 0x80000000) == 0)
                        {
                            nodes.Add(Leaf(res, typeId, nameId, BinaryPrimitives.ReadUInt32LittleEndian(res.Slice(le)), langOff));
                        }
                    }
                }
            }
        }
        catch (Exception e) when (e is ArgumentException or IndexOutOfRangeException)
        {
            return 0xDFFFFFFF;
        }

        if (nodes.Count == 0)
        {
            return 0;
        }

        Stream f;
        try
        {
            f = _createFile(fileName);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return 0xEFFFFFFF;
        }

        using (f)
        using (BinaryWriter w = new(f))
        {
            // Dummy leading header.
            w.Write(0u);
            w.Write(32u);
            w.Write(0xFFFFu);
            w.Write(0xFFFFu);
            w.Write(new byte[16]);
            foreach (ResourceNode n in nodes)
            {
                if (!dasm.TryGetBytesAtRva(n.DataRva, (int)n.DataSize, out ReadOnlySpan<byte> data))
                {
                    continue;
                }

                uint headerSize = 32;
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

        return 1;
    }

    private static string ResString(ReadOnlySpan<byte> res, int offset)
    {
        int len = BinaryPrimitives.ReadUInt16LittleEndian(res.Slice(offset));
        return Encoding.Unicode.GetString(res.Slice(offset + 2, len * 2));
    }
}
