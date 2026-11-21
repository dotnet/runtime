// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ILDisassembler;

// PE and CLR header dumps (/HEADERS).
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

    private ImmutableArray<byte> Image => _image.IsDefault ? _image = _pe.GetEntireImage().GetContent() : _image;

    private ushort U16(int fileOffset) => (ushort)(Image[fileOffset] | Image[fileOffset + 1] << 8);

    private uint U32(int fileOffset) => (uint)(Image[fileOffset] | Image[fileOffset + 1] << 8 | Image[fileOffset + 2] << 16 | Image[fileOffset + 3] << 24);

    private void DumpDirectory(string name, DirectoryEntry d)
        => PrintLine($"// 0x{(uint)d.RelativeVirtualAddress:x8} [0x{(uint)d.Size:x8}] address [size] of {name}");

    private void DumpHeader()
    {
        PEHeaders h = _pe.PEHeaders;
        PrintLine("// ----- DOS Header:");
        string[] dosNames =
        [
            "// Magic:                      ", "// Bytes on last page:         ", "// Pages in file:              ",
            "// Relocations:                ", "// Size of header (paragraphs):", "// Min extra paragraphs:       ",
            "// Max extra paragraphs:       ", "// Initial (relative) SS:      ", "// Initial SP:                 ",
            "// Checksum:                   ", "// Initial IP:                 ",
        ];
        for (int i = 0; i < dosNames.Length; i++)
        {
            PrintLine($"{dosNames[i]}0x{U16(i * 2):x4}");
        }

        // e_ip is printed again for the CS line.
        PrintLine($"// Initial (relative) CS:      0x{U16(0x14):x4}");
        PrintLine($"// File addr. of reloc table:  0x{U16(0x18):x4}");
        PrintLine($"// Overlay number:             0x{U16(0x1A):x4}");
        PrintLine($"// OEM identifier:             0x{U16(0x24):x4}");
        PrintLine($"// OEM info:                   0x{U16(0x26):x4}");
        PrintLine($"// File addr. of COFF header:  0x{U16(0x3C):x4}");

        int peOffset = (int)U32(0x3C);
        PrintLine("// ----- COFF/PE Headers:");
        PrintLine($"// Signature:                  0x{U32(peOffset):x8}");
        PrintLine("// ----- COFF Header:");
        CoffHeader coff = h.CoffHeader;
        PrintLine($"// Machine:                    0x{(ushort)coff.Machine:x4}");
        PrintLine($"// Number of sections:         0x{(ushort)coff.NumberOfSections:x4}");
        PrintLine($"// Time-date stamp:            0x{(uint)coff.TimeDateStamp:x8}");
        PrintLine($"// Ptr to symbol table:        0x{(uint)coff.PointerToSymbolTable:x8}");
        PrintLine($"// Number of symbols:          0x{(uint)coff.NumberOfSymbols:x8}");
        PrintLine($"// Size of optional header:    0x{(ushort)coff.SizeOfOptionalHeader:x4}");
        PrintLine($"// Characteristics:            0x{(ushort)coff.Characteristics:x4}");

        PEHeader pe = h.PEHeader!;
        bool pe32 = pe.Magic == PEMagic.PE32;
        PrintLine(pe32 ? "// ----- PE Optional Header (32 bit):" : "// ----- PE Optional Header (64 bit):");
        PrintLine($"// Magic:                          0x{(ushort)pe.Magic:x4}");
        PrintLine($"// Major linker version:           0x{pe.MajorLinkerVersion:x2}");
        PrintLine($"// Minor linker version:           0x{pe.MinorLinkerVersion:x2}");
        PrintLine($"// Size of code:                   0x{(uint)pe.SizeOfCode:x8}");
        PrintLine($"// Size of init.data:              0x{(uint)pe.SizeOfInitializedData:x8}");
        PrintLine($"// Size of uninit.data:            0x{(uint)pe.SizeOfUninitializedData:x8}");
        PrintLine($"// Addr. of entry point:           0x{(uint)pe.AddressOfEntryPoint:x8}");
        PrintLine($"// Base of code:                   0x{(uint)pe.BaseOfCode:x8}");
        if (pe32)
        {
            PrintLine($"// Base of data:                   0x{(uint)pe.BaseOfData:x8}");
            PrintLine($"// Image base:                     0x{(uint)pe.ImageBase:x8}");
        }
        else
        {
            PrintLine($"// Image base:                     0x{pe.ImageBase:x16}");
        }

        PrintLine($"// Section alignment:              0x{(uint)pe.SectionAlignment:x8}");
        PrintLine($"// File alignment:                 0x{(uint)pe.FileAlignment:x8}");
        PrintLine($"// Major OS version:               0x{pe.MajorOperatingSystemVersion:x4}");
        PrintLine($"// Minor OS version:               0x{pe.MinorOperatingSystemVersion:x4}");
        PrintLine($"// Major image version:            0x{pe.MajorImageVersion:x4}");
        PrintLine($"// Minor image version:            0x{pe.MinorImageVersion:x4}");
        PrintLine($"// Major subsystem version:        0x{pe.MajorSubsystemVersion:x4}");
        PrintLine($"// Minor subsystem version:        0x{pe.MinorSubsystemVersion:x4}");
        PrintLine($"// Size of image:                  0x{(uint)pe.SizeOfImage:x8}");
        PrintLine($"// Size of headers:                0x{(uint)pe.SizeOfHeaders:x8}");
        PrintLine($"// Checksum:                       0x{pe.CheckSum:x8}");
        PrintLine($"// Subsystem:                      0x{(ushort)pe.Subsystem:x4}");
        PrintLine($"// DLL characteristics:            0x{(ushort)pe.DllCharacteristics:x4}");
        if (pe32)
        {
            PrintLine($"// Size of stack reserve:          0x{(uint)pe.SizeOfStackReserve:x8}");
            PrintLine($"// Size of stack commit:           0x{(uint)pe.SizeOfStackCommit:x8}");
            PrintLine($"// Size of heap reserve:           0x{(uint)pe.SizeOfHeapReserve:x8}");
            PrintLine($"// Size of heap commit:            0x{(uint)pe.SizeOfHeapCommit:x8}");
        }
        else
        {
            PrintLine($"// Size of stack reserve:          0x{pe.SizeOfStackReserve:x16}");
            PrintLine($"// Size of stack commit:           0x{pe.SizeOfStackCommit:x16}");
            PrintLine($"// Size of heap reserve:           0x{pe.SizeOfHeapReserve:x16}");
            PrintLine($"// Size of heap commit:            0x{pe.SizeOfHeapCommit:x16}");
        }

        PrintLine("// Loader flags:                   0x00000000");
        PrintLine($"// Directories:                    0x{(uint)pe.NumberOfRvaAndSizes:x8}");
        DumpDirectory("Export Directory:          ", pe.ExportTableDirectory);
        DumpDirectory("Import Directory:          ", pe.ImportTableDirectory);
        DumpDirectory("Resource Directory:        ", pe.ResourceTableDirectory);
        DumpDirectory("Exception Directory:       ", pe.ExceptionTableDirectory);
        DumpDirectory("Security Directory:        ", pe.CertificateTableDirectory);
        DumpDirectory("Base Relocation Table:     ", pe.BaseRelocationTableDirectory);
        DumpDirectory("Debug Directory:           ", pe.DebugTableDirectory);
        DumpDirectory("Architecture Specific:     ", pe.CopyrightTableDirectory);
        DumpDirectory("Global Pointer:            ", pe.GlobalPointerTableDirectory);
        DumpDirectory("TLS Directory:             ", pe.ThreadLocalStorageTableDirectory);
        DumpDirectory("Load Config Directory:     ", pe.LoadConfigTableDirectory);
        DumpDirectory("Bound Import Directory:    ", pe.BoundImportTableDirectory);
        DumpDirectory("Import Address Table:      ", pe.ImportAddressTableDirectory);
        DumpDirectory("Delay Load IAT:            ", pe.DelayImportTableDirectory);
        DumpDirectory("CLR Header:                ", pe.CorHeaderTableDirectory);
        PrintLine("");

        DumpSectionHeaders();
        DumpBaseReloc("Base Relocation Table", pe.BaseRelocationTableDirectory);
        DumpIAT("Import Address Table", pe.ImportTableDirectory);
        DumpIAT("Delay Load Import Address Table", pe.DelayImportTableDirectory);

        if (pe.AddressOfEntryPoint != 0)
        {
            DumpEntryPoint((uint)pe.AddressOfEntryPoint, pe32 ? 6 : 12);
        }

        PrintLine("");
        PrintLine("");
        CorHeader? cor = h.CorHeader;
        if (cor is null)
        {
            PrintLine("// Not a CLR image");
            return;
        }

        PrintLine("// ----- CLR Header:");
        PrintLine($"// Header size:                        0x{U32(CorHeaderFileOffset()):x8}");
        PrintLine($"// Major runtime version:              0x{cor.MajorRuntimeVersion:x4}");
        PrintLine($"// Minor runtime version:              0x{cor.MinorRuntimeVersion:x4}");
        DumpDirectory("Metadata Directory:        ", cor.MetadataDirectory);
        PrintLine($"// Flags:                              0x{(uint)cor.Flags:x8}");
        PrintLine($"// Entry point token:                  0x{(uint)cor.EntryPointTokenOrRelativeVirtualAddress:x8}");
        DumpDirectory("Resources Directory:       ", cor.ResourcesDirectory);
        DumpDirectory("Strong Name Signature:     ", cor.StrongNameSignatureDirectory);
        DumpDirectory("CodeManager Table:         ", cor.CodeManagerTableDirectory);
        DumpDirectory("VTableFixups Directory:    ", cor.VtableFixupsDirectory);
        DumpDirectory("Export Address Table:      ", cor.ExportAddressTableJumpsDirectory);
        DumpDirectory("Precompile Header:         ", cor.ManagedNativeHeaderDirectory);
        DumpMetadataHeader("Metadata Header", cor.MetadataDirectory);
    }

    private int CorHeaderFileOffset()
    {
        DirectoryEntry d = _pe.PEHeaders.PEHeader!.CorHeaderTableDirectory;
        _pe.PEHeaders.TryGetDirectoryOffset(d, out int offset);
        return offset;
    }

    private void DumpSectionHeaders()
    {
        PrintLine("");
        PrintLine("// Image sections:");
        foreach (SectionHeader sh in _pe.PEHeaders.SectionHeaders)
        {
            PrintLine($"//              {CSectionName(sh)}");
            PrintLine($"//              0x{(uint)sh.VirtualSize:x8} Virtual Size");
            PrintLine($"//              0x{(uint)sh.VirtualAddress:x8} Virtual Address");
            PrintLine($"//              0x{(uint)sh.SizeOfRawData:x8} Size of Raw Data");
            PrintLine($"//              0x{(uint)sh.PointerToRawData:x8} Pointer to Raw Data");
            PrintLine($"//              0x{(uint)sh.PointerToRelocations:x8} Pointer to Relocations");
            PrintLine($"//              0x{(uint)sh.PointerToLineNumbers:x8} Pointer to Linenumbers");
            PrintLine($"//                  0x{sh.NumberOfRelocations:x4} Number of Relocations");
            PrintLine($"//                  0x{sh.NumberOfLineNumbers:x4} Number of Linenumbers");
            uint c = (uint)sh.SectionCharacteristics;
            PrintLine($"//              0x{c:x8} Characteristics");
            (uint Flag, string Name)[] flags =
            [
                (0x00000001, "SCALE_INDEX"), (0x00000020, "CNT_CODE"), (0x00000040, "CNT_INITIALIZED_DATA"),
                (0x00000080, "CNT_UNINITIALIZED_DATA"), (0x00004000, "NO_DEFER_SPEC_EXC"), (0x01000000, "LNK_NRELOC_OVFL"),
                (0x02000000, "MEM_DISCARDABLE"), (0x04000000, "MEM_NOT_CACHED"), (0x08000000, "MEM_NOT_PAGED"),
                (0x10000000, "MEM_SHARED"), (0x20000000, "MEM_EXECUTE"), (0x40000000, "MEM_READ"), (0x80000000, "MEM_WRITE"),
            ];
            foreach ((uint flag, string name) in flags)
            {
                if ((c & flag) != 0)
                {
                    PrintLine("//                         " + name);
                }
            }

            PrintLine("");
        }
    }

    private void DumpBaseReloc(string name, DirectoryEntry dir)
    {
        PrintLine("// " + name);
        if (dir.Size == 0)
        {
            PrintLine("// No data.");
            return;
        }

        byte[]? data = GetBytesAtRva((uint)dir.RelativeVirtualAddress, dir.Size);
        if (data is null)
        {
            PrintLine("// Failed to read import data.");
            return;
        }

        int p = 0;
        while (p + 8 <= data.Length)
        {
            uint page = ReadUInt32(data, p);
            uint block = ReadUInt32(data, p + 4);
            PrintLine($"//              0x{page:x8} Page RVA");
            PrintLine($"//              0x{block:x8} Block Size");
            uint n = (block - 8) / 2;
            PrintLine($"//              0x{n:x8} Number of Entries");
            p += 8;
            for (uint i = 1; i <= n && p + 2 <= data.Length; i++, p += 2)
            {
                ushort w = (ushort)(data[p] | data[p + 1] << 8);
                PrintLine($"//              Entry {i}: Type 0x{w >> 12:x} Offset 0x{w & 0x0FFF:x8}");
            }

            if ((n & 1) != 0)
            {
                p += 2;
            }

            PrintLine("");
        }
    }

    private void DumpIAT(string name, DirectoryEntry dir)
    {
        PrintLine("// " + name);
        if (dir.Size == 0)
        {
            PrintLine("// No data.");
            return;
        }

        byte[]? desc = GetBytesAtRva((uint)dir.RelativeVirtualAddress, -1);
        if (desc is null)
        {
            PrintLine("// Failed to read import data.");
            return;
        }

        for (int d = 0; d + 20 <= desc.Length; d += 20)
        {
            uint firstThunk = ReadUInt32(desc, d + 16);
            if (firstThunk == 0)
            {
                break;
            }

            uint nameRva = ReadUInt32(desc, d + 12);
            byte[]? dllName = GetBytesAtRva(nameRva, -1);
            byte[]? thunks = GetBytesAtRva(firstThunk, -1);
            if (dllName is null || thunks is null)
            {
                PrintLine("// Failed to read import data.");
                return;
            }

            PrintLine($"//     DLL : {CString(dllName, 0)}");
            PrintLine($"//              0x{firstThunk:x8} Import Address Table");
            PrintLine($"//              0x{nameRva:x8} Import Name Table");
            PrintLine($"//              {((int)ReadUInt32(desc, d + 4)).ToString(CultureInfo.InvariantCulture),-8}   Time Date Stamp");
            PrintLine($"//              {((int)ReadUInt32(desc, d + 8)).ToString(CultureInfo.InvariantCulture),-8}   Index of First Forwarder Reference");
            PrintLine("//");
            for (int t = 0; t + 4 <= thunks.Length; t += 4)
            {
                uint id = ReadUInt32(thunks, t);
                if (id == 0)
                {
                    break;
                }

                if ((id & 0x80000000) != 0)
                {
                    PrintLine($"//              by Ordinal {id & 0x7fffffff}");
                }
                else
                {
                    byte[]? byName = GetBytesAtRva(id & 0x7fffffff, -1);
                    PrintLine(byName is not null
                        ? $"//              0x{byName[0] | byName[1] << 8:x4}  {CString(byName, 2)}"
                        : $"//          0x{id:x8}  bad RVA of IMAGE_IMPORT_BY_NAME");
                }
            }

            PrintLine("");
        }
    }

    private static string CString(byte[] b, int start)
    {
        int z = Array.IndexOf(b, (byte)0, start);
        return Encoding.UTF8.GetString(b, start, (z < 0 ? b.Length : z) - start);
    }

    private void DumpEntryPoint(uint rva, int size)
    {
        PrintLine("");
        PrintLine("// Entry point code:");
        byte[]? b = GetBytesAtRva(rva, size);
        if (b is null)
        {
            PrintLine("Bad RVA of entry point");
            return;
        }

        StringBuilder sb = new("//");
        for (int i = 0; i < size && i < b.Length; i++)
        {
            sb.Append(b[i].ToString("X2", CultureInfo.InvariantCulture)).Append(' ');
        }

        PrintLine(sb);
    }

    private void DumpMetadataHeader(string name, DirectoryEntry dir)
    {
        PrintLine("");
        PrintLine("// " + name);
        if (dir.Size == 0)
        {
            PrintLine("// No data.");
            return;
        }

        byte[]? m = GetBytesAtRva((uint)dir.RelativeVirtualAddress, -1);
        if (m is null)
        {
            PrintLine("// Failed to read import data.");
            return;
        }

        PrintLine("//    Storage Signature:");
        PrintLine($"//              0x{ReadUInt32(m, 0):x8} Signature");
        PrintLine($"//                  0x{m[4] | m[5] << 8:x4} Major Version");
        PrintLine($"//                  0x{m[6] | m[7] << 8:x4} Minor Version");
        PrintLine($"//              0x{ReadUInt32(m, 8):x8} Extra Data Offset");
        uint verLen = ReadUInt32(m, 12);
        PrintLine($"//              0x{verLen:x8} Version String Length");
        PrintLine($"//              '{CString(m.AsSpan(16, (int)Math.Min(verLen, (uint)m.Length - 16)).ToArray(), 0)}' Version String");

        int p = (int)((16 + verLen + 3) & ~3u);
        PrintLine("//    Storage Header:");
        PrintLine($"//                    0x{m[p]:x2} Flags");
        int nStr = (short)(m[p + 2] | m[p + 3] << 8);
        string bogus = "";
        if (nStr > 5)
        {
            bogus = " -- BOGUS!";
        }

        PrintLine($"//                  0x{(ushort)nStr:x4} Number of Streams{bogus}");
        if (nStr > 5)
        {
            nStr = 5;
        }

        p += 4;
        int mdStream = -1;
        uint mdSize = 0;
        for (int i = 1; i <= nStr; i++)
        {
            uint off = ReadUInt32(m, p);
            uint size = ReadUInt32(m, p + 4);
            string sname = CString(m, p + 8);
            PrintLine($"//    Stream {i}:");
            PrintLine($"//              0x{off:x8} Offset");
            PrintLine($"//              0x{size:x8} Size");
            PrintLine($"//              '{sname}' Name");
            if (sname is "#-" or "#~")
            {
                mdStream = (int)off;
                mdSize = size;
            }

            p += (8 + Encoding.UTF8.GetByteCount(sname) + 1 + 3) & ~3;
        }

        if (mdStream >= 0 && mdSize >= 24)
        {
            PrintLine("");
            PrintLine("//    Metadata Stream Header:");
            PrintLine($"//              0x{ReadUInt32(m, mdStream):x8} Reserved");
            PrintLine($"//                    0x{m[mdStream + 4]:x2} Major");
            PrintLine($"//                    0x{m[mdStream + 5]:x2} Minor");
            PrintLine($"//                    0x{m[mdStream + 6]:x2} Heaps");
            PrintLine($"//                    0x{m[mdStream + 7]:x2} Rid");
            PrintLine($"//      0x{BitConverter.ToUInt64(m, mdStream + 8):x16} MaskValid");
            PrintLine($"//      0x{BitConverter.ToUInt64(m, mdStream + 16):x16} Sorted");
        }
    }

    private void DumpHeaderDetails()
    {
        CorHeader cor = _pe.PEHeaders.CorHeader!;
        PrintLine("");
        PrintLine("// Code Manager Table:");
        if (cor.CodeManagerTableDirectory.Size == 0)
        {
            PrintLine("//  default");
        }
        else
        {
            byte[]? g = GetBytesAtRva((uint)cor.CodeManagerTableDirectory.RelativeVirtualAddress, cor.CodeManagerTableDirectory.Size);
            if (g is null)
            {
                PrintLine("// Failed to get Code Manager table.");
            }
            else
            {
                PrintLine("//   [index]         ID");
                for (int i = 0; i + 16 <= g.Length; i += 16)
                {
                    PrintLine($"//   [0x{i / 16:x8}]    {{{new Guid(g.AsSpan(i, 16)).ToString("D")}}}");
                }

                PrintLine("");
            }
        }

        PrintLine("");
        DumpVTables();
        PrintLine("");
        DumpEATTable();
        PrintLine("");
    }

    private void DumpEATTable()
    {
        CorHeader cor = _pe.PEHeaders.CorHeader!;
        PrintLine("// Export Address Table Jumps:");
        DirectoryEntry d = cor.ExportAddressTableJumpsDirectory;
        if (d.RelativeVirtualAddress == 0)
        {
            PrintLine("// No data.");
            return;
        }

        const int thunkSize = 8;
        byte[]? data = GetBytesAtRva((uint)d.RelativeVirtualAddress, d.Size);
        if (data is null || data.Length < d.Size)
        {
            PrintLine("// Failed to get export address table jumps table.");
            PrintLine("");
            return;
        }

        if (d.Size % thunkSize != 0)
        {
            PrintLine($"// Error!  size of ExportAddressTableJumps {d.Size} not a multiple of reserved size {thunkSize}");
        }

        uint bufferRva = (uint)d.RelativeVirtualAddress;
        for (int i = 0; i < d.Size / thunkSize; i++)
        {
            uint reserved = ReadUInt32(data, i * thunkSize + 4);
            PrintLine($"//   Fixup Jump Entry [{i}], at RVA 0x{bufferRva:x8}:");
            PrintLine($"//       RVA of slot:       0x{ReadUInt32(data, i * thunkSize):x8}");
            PrintLine($"//       Reserved flag:     0x{reserved:x8}");
            if (reserved != 0)
            {
                PrintLine("// Error!  reserved flags need to be 0.");
            }

            bufferRva += thunkSize;
        }

        PrintLine("");
    }
}
