// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Globalization;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ILDisassembler;

// /HEADERS: the PE and CLR headers.
internal sealed class HeadersDisassembler : IMetadataDisassembler
{
    public DisassemblyPhase Phase => DisassemblyPhase.Headers;

    // Only the PE headers are shown for an image without a CLR header.
    public void Disassemble(Dasm dasm)
    {
        DumpHeader(dasm);
        if (dasm.PE.PEHeaders.CorHeader is not null)
        {
            DumpHeaderDetails(dasm);
        }
    }

    private static void DumpDirectory(Dasm dasm, string name, DirectoryEntry d)
        => dasm.PrintLine($"// 0x{(uint)d.RelativeVirtualAddress:x8} [0x{(uint)d.Size:x8}] address [size] of {name}");

    private static void DumpHeader(Dasm dasm)
    {
        PEHeaders h = dasm.PE.PEHeaders;
        dasm.PrintLine("// ----- DOS Header:");
        string[] dosNames =
        [
            "// Magic:                      ", "// Bytes on last page:         ", "// Pages in file:              ",
            "// Relocations:                ", "// Size of header (paragraphs):", "// Min extra paragraphs:       ",
            "// Max extra paragraphs:       ", "// Initial (relative) SS:      ", "// Initial SP:                 ",
            "// Checksum:                   ", "// Initial IP:                 ",
        ];
        for (int i = 0; i < dosNames.Length; i++)
        {
            dasm.PrintLine($"{dosNames[i]}0x{dasm.U16(i * 2):x4}");
        }

        // e_ip is printed again for the CS line.
        dasm.PrintLine($"// Initial (relative) CS:      0x{dasm.U16(0x14):x4}");
        dasm.PrintLine($"// File addr. of reloc table:  0x{dasm.U16(0x18):x4}");
        dasm.PrintLine($"// Overlay number:             0x{dasm.U16(0x1A):x4}");
        dasm.PrintLine($"// OEM identifier:             0x{dasm.U16(0x24):x4}");
        dasm.PrintLine($"// OEM info:                   0x{dasm.U16(0x26):x4}");
        dasm.PrintLine($"// File addr. of COFF header:  0x{dasm.U16(0x3C):x4}");

        int peOffset = (int)dasm.U32(0x3C);
        dasm.PrintLine("// ----- COFF/PE Headers:");
        dasm.PrintLine($"// Signature:                  0x{dasm.U32(peOffset):x8}");
        dasm.PrintLine("// ----- COFF Header:");
        CoffHeader coff = h.CoffHeader;
        dasm.PrintLine($"// Machine:                    0x{(ushort)coff.Machine:x4}");
        dasm.PrintLine($"// Number of sections:         0x{(ushort)coff.NumberOfSections:x4}");
        dasm.PrintLine($"// Time-date stamp:            0x{(uint)coff.TimeDateStamp:x8}");
        dasm.PrintLine($"// Ptr to symbol table:        0x{(uint)coff.PointerToSymbolTable:x8}");
        dasm.PrintLine($"// Number of symbols:          0x{(uint)coff.NumberOfSymbols:x8}");
        dasm.PrintLine($"// Size of optional header:    0x{(ushort)coff.SizeOfOptionalHeader:x4}");
        dasm.PrintLine($"// Characteristics:            0x{(ushort)coff.Characteristics:x4}");

        PEHeader pe = h.PEHeader!;
        bool pe32 = pe.Magic == PEMagic.PE32;
        dasm.PrintLine(pe32 ? "// ----- PE Optional Header (32 bit):" : "// ----- PE Optional Header (64 bit):");
        dasm.PrintLine($"// Magic:                          0x{(ushort)pe.Magic:x4}");
        dasm.PrintLine($"// Major linker version:           0x{pe.MajorLinkerVersion:x2}");
        dasm.PrintLine($"// Minor linker version:           0x{pe.MinorLinkerVersion:x2}");
        dasm.PrintLine($"// Size of code:                   0x{(uint)pe.SizeOfCode:x8}");
        dasm.PrintLine($"// Size of init.data:              0x{(uint)pe.SizeOfInitializedData:x8}");
        dasm.PrintLine($"// Size of uninit.data:            0x{(uint)pe.SizeOfUninitializedData:x8}");
        dasm.PrintLine($"// Addr. of entry point:           0x{(uint)pe.AddressOfEntryPoint:x8}");
        dasm.PrintLine($"// Base of code:                   0x{(uint)pe.BaseOfCode:x8}");
        if (pe32)
        {
            dasm.PrintLine($"// Base of data:                   0x{(uint)pe.BaseOfData:x8}");
            dasm.PrintLine($"// Image base:                     0x{(uint)pe.ImageBase:x8}");
        }
        else
        {
            dasm.PrintLine($"// Image base:                     0x{pe.ImageBase:x16}");
        }

        dasm.PrintLine($"// Section alignment:              0x{(uint)pe.SectionAlignment:x8}");
        dasm.PrintLine($"// File alignment:                 0x{(uint)pe.FileAlignment:x8}");
        dasm.PrintLine($"// Major OS version:               0x{pe.MajorOperatingSystemVersion:x4}");
        dasm.PrintLine($"// Minor OS version:               0x{pe.MinorOperatingSystemVersion:x4}");
        dasm.PrintLine($"// Major image version:            0x{pe.MajorImageVersion:x4}");
        dasm.PrintLine($"// Minor image version:            0x{pe.MinorImageVersion:x4}");
        dasm.PrintLine($"// Major subsystem version:        0x{pe.MajorSubsystemVersion:x4}");
        dasm.PrintLine($"// Minor subsystem version:        0x{pe.MinorSubsystemVersion:x4}");
        dasm.PrintLine($"// Size of image:                  0x{(uint)pe.SizeOfImage:x8}");
        dasm.PrintLine($"// Size of headers:                0x{(uint)pe.SizeOfHeaders:x8}");
        dasm.PrintLine($"// Checksum:                       0x{pe.CheckSum:x8}");
        dasm.PrintLine($"// Subsystem:                      0x{(ushort)pe.Subsystem:x4}");
        dasm.PrintLine($"// DLL characteristics:            0x{(ushort)pe.DllCharacteristics:x4}");
        if (pe32)
        {
            dasm.PrintLine($"// Size of stack reserve:          0x{(uint)pe.SizeOfStackReserve:x8}");
            dasm.PrintLine($"// Size of stack commit:           0x{(uint)pe.SizeOfStackCommit:x8}");
            dasm.PrintLine($"// Size of heap reserve:           0x{(uint)pe.SizeOfHeapReserve:x8}");
            dasm.PrintLine($"// Size of heap commit:            0x{(uint)pe.SizeOfHeapCommit:x8}");
        }
        else
        {
            dasm.PrintLine($"// Size of stack reserve:          0x{pe.SizeOfStackReserve:x16}");
            dasm.PrintLine($"// Size of stack commit:           0x{pe.SizeOfStackCommit:x16}");
            dasm.PrintLine($"// Size of heap reserve:           0x{pe.SizeOfHeapReserve:x16}");
            dasm.PrintLine($"// Size of heap commit:            0x{pe.SizeOfHeapCommit:x16}");
        }

        dasm.PrintLine("// Loader flags:                   0x00000000");
        dasm.PrintLine($"// Directories:                    0x{(uint)pe.NumberOfRvaAndSizes:x8}");
        DumpDirectory(dasm, "Export Directory:          ", pe.ExportTableDirectory);
        DumpDirectory(dasm, "Import Directory:          ", pe.ImportTableDirectory);
        DumpDirectory(dasm, "Resource Directory:        ", pe.ResourceTableDirectory);
        DumpDirectory(dasm, "Exception Directory:       ", pe.ExceptionTableDirectory);
        DumpDirectory(dasm, "Security Directory:        ", pe.CertificateTableDirectory);
        DumpDirectory(dasm, "Base Relocation Table:     ", pe.BaseRelocationTableDirectory);
        DumpDirectory(dasm, "Debug Directory:           ", pe.DebugTableDirectory);
        DumpDirectory(dasm, "Architecture Specific:     ", pe.CopyrightTableDirectory);
        DumpDirectory(dasm, "Global Pointer:            ", pe.GlobalPointerTableDirectory);
        DumpDirectory(dasm, "TLS Directory:             ", pe.ThreadLocalStorageTableDirectory);
        DumpDirectory(dasm, "Load Config Directory:     ", pe.LoadConfigTableDirectory);
        DumpDirectory(dasm, "Bound Import Directory:    ", pe.BoundImportTableDirectory);
        DumpDirectory(dasm, "Import Address Table:      ", pe.ImportAddressTableDirectory);
        DumpDirectory(dasm, "Delay Load IAT:            ", pe.DelayImportTableDirectory);
        DumpDirectory(dasm, "CLR Header:                ", pe.CorHeaderTableDirectory);
        dasm.PrintLine("");

        DumpSectionHeaders(dasm);
        DumpBaseReloc(dasm, "Base Relocation Table", pe.BaseRelocationTableDirectory);
        DumpIAT(dasm, "Import Address Table", pe.ImportTableDirectory);
        DumpIAT(dasm, "Delay Load Import Address Table", pe.DelayImportTableDirectory);

        if (pe.AddressOfEntryPoint != 0)
        {
            DumpEntryPoint(dasm, (uint)pe.AddressOfEntryPoint, pe32 ? 6 : 12);
        }

        dasm.PrintLine("");
        dasm.PrintLine("");
        CorHeader? cor = h.CorHeader;
        if (cor is null)
        {
            dasm.PrintLine("// Not a CLR image");
            return;
        }

        dasm.PrintLine("// ----- CLR Header:");
        dasm.PrintLine($"// Header size:                        0x{dasm.U32(dasm.CorHeaderFileOffset()):x8}");
        dasm.PrintLine($"// Major runtime version:              0x{cor.MajorRuntimeVersion:x4}");
        dasm.PrintLine($"// Minor runtime version:              0x{cor.MinorRuntimeVersion:x4}");
        DumpDirectory(dasm, "Metadata Directory:        ", cor.MetadataDirectory);
        dasm.PrintLine($"// Flags:                              0x{(uint)cor.Flags:x8}");
        dasm.PrintLine($"// Entry point token:                  0x{(uint)cor.EntryPointTokenOrRelativeVirtualAddress:x8}");
        DumpDirectory(dasm, "Resources Directory:       ", cor.ResourcesDirectory);
        DumpDirectory(dasm, "Strong Name Signature:     ", cor.StrongNameSignatureDirectory);
        DumpDirectory(dasm, "CodeManager Table:         ", cor.CodeManagerTableDirectory);
        DumpDirectory(dasm, "VTableFixups Directory:    ", cor.VtableFixupsDirectory);
        DumpDirectory(dasm, "Export Address Table:      ", cor.ExportAddressTableJumpsDirectory);
        DumpDirectory(dasm, "Precompile Header:         ", cor.ManagedNativeHeaderDirectory);
        DumpMetadataHeader(dasm, "Metadata Header", cor.MetadataDirectory);
    }

    private static void DumpSectionHeaders(Dasm dasm)
    {
        dasm.PrintLine("");
        dasm.PrintLine("// Image sections:");
        foreach (SectionHeader sh in dasm.PE.PEHeaders.SectionHeaders)
        {
            dasm.PrintLine($"//              {Dasm.CSectionName(sh)}");
            dasm.PrintLine($"//              0x{(uint)sh.VirtualSize:x8} Virtual Size");
            dasm.PrintLine($"//              0x{(uint)sh.VirtualAddress:x8} Virtual Address");
            dasm.PrintLine($"//              0x{(uint)sh.SizeOfRawData:x8} Size of Raw Data");
            dasm.PrintLine($"//              0x{(uint)sh.PointerToRawData:x8} Pointer to Raw Data");
            dasm.PrintLine($"//              0x{(uint)sh.PointerToRelocations:x8} Pointer to Relocations");
            dasm.PrintLine($"//              0x{(uint)sh.PointerToLineNumbers:x8} Pointer to Linenumbers");
            dasm.PrintLine($"//                  0x{sh.NumberOfRelocations:x4} Number of Relocations");
            dasm.PrintLine($"//                  0x{sh.NumberOfLineNumbers:x4} Number of Linenumbers");
            uint c = (uint)sh.SectionCharacteristics;
            dasm.PrintLine($"//              0x{c:x8} Characteristics");
            (SectionCharacteristics Flag, string Name)[] flags =
            [
                (SectionCharacteristics.TypeDSect, "SCALE_INDEX"), (SectionCharacteristics.ContainsCode, "CNT_CODE"),
                (SectionCharacteristics.ContainsInitializedData, "CNT_INITIALIZED_DATA"),
                (SectionCharacteristics.ContainsUninitializedData, "CNT_UNINITIALIZED_DATA"),
                (SectionCharacteristics.NoDeferSpecExc, "NO_DEFER_SPEC_EXC"), (SectionCharacteristics.LinkerNRelocOvfl, "LNK_NRELOC_OVFL"),
                (SectionCharacteristics.MemDiscardable, "MEM_DISCARDABLE"), (SectionCharacteristics.MemNotCached, "MEM_NOT_CACHED"),
                (SectionCharacteristics.MemNotPaged, "MEM_NOT_PAGED"), (SectionCharacteristics.MemShared, "MEM_SHARED"),
                (SectionCharacteristics.MemExecute, "MEM_EXECUTE"), (SectionCharacteristics.MemRead, "MEM_READ"),
                (SectionCharacteristics.MemWrite, "MEM_WRITE"),
            ];
            foreach ((SectionCharacteristics flag, string name) in flags)
            {
                if ((sh.SectionCharacteristics & flag) != 0)
                {
                    dasm.PrintLine("//                         " + name);
                }
            }

            dasm.PrintLine("");
        }
    }

    private static void DumpBaseReloc(Dasm dasm, string name, DirectoryEntry dir)
    {
        dasm.PrintLine("// " + name);
        if (dir.Size == 0)
        {
            dasm.PrintLine("// No data.");
            return;
        }

        if (!dasm.TryGetBytesAtRva((uint)dir.RelativeVirtualAddress, dir.Size, out ReadOnlySpan<byte> data))
        {
            dasm.PrintLine("// Failed to read import data.");
            return;
        }

        int p = 0;
        while (p + 8 <= data.Length)
        {
            uint page = Dasm.ReadUInt32(data, p);
            uint block = Dasm.ReadUInt32(data, p + 4);
            dasm.PrintLine($"//              0x{page:x8} Page RVA");
            dasm.PrintLine($"//              0x{block:x8} Block Size");
            uint n = (block - 8) / 2;
            dasm.PrintLine($"//              0x{n:x8} Number of Entries");
            p += 8;
            for (uint i = 1; i <= n && p + 2 <= data.Length; i++, p += 2)
            {
                ushort w = (ushort)(data[p] | data[p + 1] << 8);
                dasm.PrintLine($"//              Entry {i}: Type 0x{w >> 12:x} Offset 0x{w & 0x0FFF:x8}");
            }

            if ((n & 1) != 0)
            {
                p += 2;
            }

            dasm.PrintLine("");
        }
    }

    private static void DumpIAT(Dasm dasm, string name, DirectoryEntry dir)
    {
        dasm.PrintLine("// " + name);
        if (dir.Size == 0)
        {
            dasm.PrintLine("// No data.");
            return;
        }

        if (!dasm.TryGetBytesAtRva((uint)dir.RelativeVirtualAddress, -1, out ReadOnlySpan<byte> desc))
        {
            dasm.PrintLine("// Failed to read import data.");
            return;
        }

        for (int d = 0; d + 20 <= desc.Length; d += 20)
        {
            uint firstThunk = Dasm.ReadUInt32(desc, d + 16);
            if (firstThunk == 0)
            {
                break;
            }

            uint nameRva = Dasm.ReadUInt32(desc, d + 12);
            if (!dasm.TryGetBytesAtRva(nameRva, -1, out ReadOnlySpan<byte> dllName) || !dasm.TryGetBytesAtRva(firstThunk, -1, out ReadOnlySpan<byte> thunks))
            {
                dasm.PrintLine("// Failed to read import data.");
                return;
            }

            dasm.PrintLine($"//     DLL : {CString(dllName, 0)}");
            dasm.PrintLine($"//              0x{firstThunk:x8} Import Address Table");
            dasm.PrintLine($"//              0x{nameRva:x8} Import Name Table");
            dasm.PrintLine($"//              {((int)Dasm.ReadUInt32(desc, d + 4)).ToString(CultureInfo.InvariantCulture),-8}   Time Date Stamp");
            dasm.PrintLine($"//              {((int)Dasm.ReadUInt32(desc, d + 8)).ToString(CultureInfo.InvariantCulture),-8}   Index of First Forwarder Reference");
            dasm.PrintLine("//");
            for (int t = 0; t + 4 <= thunks.Length; t += 4)
            {
                uint id = Dasm.ReadUInt32(thunks, t);
                if (id == 0)
                {
                    break;
                }

                if ((id & 0x80000000) != 0)
                {
                    dasm.PrintLine($"//              by Ordinal {id & 0x7fffffff}");
                }
                else
                {
                    dasm.PrintLine(dasm.TryGetBytesAtRva(id & 0x7fffffff, -1, out ReadOnlySpan<byte> byName)
                        ? $"//              0x{byName[0] | byName[1] << 8:x4}  {CString(byName, 2)}"
                        : $"//          0x{id:x8}  bad RVA of IMAGE_IMPORT_BY_NAME");
                }
            }

            dasm.PrintLine("");
        }
    }

    private static string CString(ReadOnlySpan<byte> b, int start)
    {
        b = b.Slice(start);
        int z = b.IndexOf((byte)0);
        return Encoding.UTF8.GetString(z < 0 ? b : b.Slice(0, z));
    }

    private static void DumpEntryPoint(Dasm dasm, uint rva, int size)
    {
        dasm.PrintLine("");
        dasm.PrintLine("// Entry point code:");
        if (!dasm.TryGetBytesAtRva(rva, size, out ReadOnlySpan<byte> b))
        {
            dasm.PrintLine("Bad RVA of entry point");
            return;
        }

        StringBuilder sb = new("//");
        for (int i = 0; i < size && i < b.Length; i++)
        {
            sb.Append(b[i].ToString("X2", CultureInfo.InvariantCulture)).Append(' ');
        }

        dasm.PrintLine(sb);
    }

    private static void DumpMetadataHeader(Dasm dasm, string name, DirectoryEntry dir)
    {
        dasm.PrintLine("");
        dasm.PrintLine("// " + name);
        if (dir.Size == 0)
        {
            dasm.PrintLine("// No data.");
            return;
        }

        if (!dasm.TryGetBytesAtRva((uint)dir.RelativeVirtualAddress, -1, out ReadOnlySpan<byte> m))
        {
            dasm.PrintLine("// Failed to read import data.");
            return;
        }

        dasm.PrintLine("//    Storage Signature:");
        dasm.PrintLine($"//              0x{Dasm.ReadUInt32(m, 0):x8} Signature");
        dasm.PrintLine($"//                  0x{m[4] | m[5] << 8:x4} Major Version");
        dasm.PrintLine($"//                  0x{m[6] | m[7] << 8:x4} Minor Version");
        dasm.PrintLine($"//              0x{Dasm.ReadUInt32(m, 8):x8} Extra Data Offset");
        uint verLen = Dasm.ReadUInt32(m, 12);
        dasm.PrintLine($"//              0x{verLen:x8} Version String Length");
        dasm.PrintLine($"//              '{CString(m.Slice(16, (int)Math.Min(verLen, (uint)m.Length - 16)), 0)}' Version String");

        int p = (int)((16 + verLen + 3) & ~3u);
        dasm.PrintLine("//    Storage Header:");
        dasm.PrintLine($"//                    0x{m[p]:x2} Flags");
        int nStr = (short)(m[p + 2] | m[p + 3] << 8);
        string bogus = "";
        if (nStr > 5)
        {
            bogus = " -- BOGUS!";
        }

        dasm.PrintLine($"//                  0x{(ushort)nStr:x4} Number of Streams{bogus}");
        if (nStr > 5)
        {
            nStr = 5;
        }

        p += 4;
        int mdStream = -1;
        uint mdSize = 0;
        for (int i = 1; i <= nStr; i++)
        {
            uint off = Dasm.ReadUInt32(m, p);
            uint size = Dasm.ReadUInt32(m, p + 4);
            string sname = CString(m, p + 8);
            dasm.PrintLine($"//    Stream {i}:");
            dasm.PrintLine($"//              0x{off:x8} Offset");
            dasm.PrintLine($"//              0x{size:x8} Size");
            dasm.PrintLine($"//              '{sname}' Name");
            if (sname is "#-" or "#~")
            {
                mdStream = (int)off;
                mdSize = size;
            }

            p += (8 + Encoding.UTF8.GetByteCount(sname) + 1 + 3) & ~3;
        }

        if (mdStream >= 0 && mdSize >= 24)
        {
            dasm.PrintLine("");
            dasm.PrintLine("//    Metadata Stream Header:");
            dasm.PrintLine($"//              0x{Dasm.ReadUInt32(m, mdStream):x8} Reserved");
            dasm.PrintLine($"//                    0x{m[mdStream + 4]:x2} Major");
            dasm.PrintLine($"//                    0x{m[mdStream + 5]:x2} Minor");
            dasm.PrintLine($"//                    0x{m[mdStream + 6]:x2} Heaps");
            dasm.PrintLine($"//                    0x{m[mdStream + 7]:x2} Rid");
            dasm.PrintLine($"//      0x{BinaryPrimitives.ReadUInt64LittleEndian(m.Slice(mdStream + 8)):x16} MaskValid");
            dasm.PrintLine($"//      0x{BinaryPrimitives.ReadUInt64LittleEndian(m.Slice(mdStream + 16)):x16} Sorted");
        }
    }

    private static void DumpHeaderDetails(Dasm dasm)
    {
        CorHeader cor = dasm.PE.PEHeaders.CorHeader!;
        dasm.PrintLine("");
        dasm.PrintLine("// Code Manager Table:");
        if (cor.CodeManagerTableDirectory.Size == 0)
        {
            dasm.PrintLine("//  default");
        }
        else
        {
            if (!dasm.TryGetBytesAtRva((uint)cor.CodeManagerTableDirectory.RelativeVirtualAddress, cor.CodeManagerTableDirectory.Size, out ReadOnlySpan<byte> g))
            {
                dasm.PrintLine("// Failed to get Code Manager table.");
            }
            else
            {
                dasm.PrintLine("//   [index]         ID");
                for (int i = 0; i + 16 <= g.Length; i += 16)
                {
                    dasm.PrintLine($"//   [0x{i / 16:x8}]    {{{new Guid(g.Slice(i, 16)).ToString("D")}}}");
                }

                dasm.PrintLine("");
            }
        }

        dasm.PrintLine("");
        dasm.DumpVTables();
        dasm.PrintLine("");
        DumpEATTable(dasm);
        dasm.PrintLine("");
    }

    private static void DumpEATTable(Dasm dasm)
    {
        CorHeader cor = dasm.PE.PEHeaders.CorHeader!;
        dasm.PrintLine("// Export Address Table Jumps:");
        DirectoryEntry d = cor.ExportAddressTableJumpsDirectory;
        if (d.RelativeVirtualAddress == 0)
        {
            dasm.PrintLine("// No data.");
            return;
        }

        const int thunkSize = 8;
        if (!dasm.TryGetBytesAtRva((uint)d.RelativeVirtualAddress, d.Size, out ReadOnlySpan<byte> data) || data.Length < d.Size)
        {
            dasm.PrintLine("// Failed to get export address table jumps table.");
            dasm.PrintLine("");
            return;
        }

        if (d.Size % thunkSize != 0)
        {
            dasm.PrintLine($"// Error!  size of ExportAddressTableJumps {d.Size} not a multiple of reserved size {thunkSize}");
        }

        uint bufferRva = (uint)d.RelativeVirtualAddress;
        for (int i = 0; i < d.Size / thunkSize; i++)
        {
            uint reserved = Dasm.ReadUInt32(data, i * thunkSize + 4);
            dasm.PrintLine($"//   Fixup Jump Entry [{i}], at RVA 0x{bufferRva:x8}:");
            dasm.PrintLine($"//       RVA of slot:       0x{Dasm.ReadUInt32(data, i * thunkSize):x8}");
            dasm.PrintLine($"//       Reserved flag:     0x{reserved:x8}");
            if (reserved != 0)
            {
                dasm.PrintLine("// Error!  reserved flags need to be 0.");
            }

            bufferRva += thunkSize;
        }

        dasm.PrintLine("");
    }
}
