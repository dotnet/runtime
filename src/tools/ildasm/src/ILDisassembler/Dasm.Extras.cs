// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ILDisassembler;

// /STATS and /CLASSLIST (DumpStatistics and PrintClassList in the native dasm.cpp).
internal sealed partial class Dasm
{
    private static string Pct(int value, int fileSize)
    {
        double d = (double)value * 100 / fileSize;
        return d.ToString("F2", CultureInfo.InvariantCulture).PadLeft(5);
    }

    private static string Left(string s, int width) => s.Length >= width ? s : s.PadRight(width);

    private void DumpStatistics()
    {
        PEHeaders h = _pe.PEHeaders;
        PEHeader pe = h.PEHeader!;
        CorHeader cor = h.CorHeader!;
        HashSet<TableIndex> seen = new();
        int metaSize = 0;
        int fileSize = Image.Length;
        PrintLine($"// File size            : {fileSize}");

        bool pe32 = pe.Magic == PEMagic.PE32;
        int lfanew = (int)U32(0x3C);
        int ntHeadersNoOpt = 4 + 20; // signature + file header
        int size = lfanew + ntHeadersNoOpt + h.CoffHeader.SizeOfOptionalHeader + h.CoffHeader.NumberOfSections * 40;
        int sizeOfHeaders = pe.SizeOfHeaders;
        PrintLine($"// PE header size       : {sizeOfHeaders} ({size} used)    ({Pct(sizeOfHeaders, fileSize)}%)");

        int miscPESize = 0;
        DirectoryEntry[] dirs =
        [
            pe.ExportTableDirectory, pe.ImportTableDirectory, pe.ResourceTableDirectory, pe.ExceptionTableDirectory,
            pe.CertificateTableDirectory, pe.BaseRelocationTableDirectory, pe.DebugTableDirectory, pe.CopyrightTableDirectory,
            pe.GlobalPointerTableDirectory, pe.ThreadLocalStorageTableDirectory, pe.LoadConfigTableDirectory, pe.BoundImportTableDirectory,
            pe.ImportAddressTableDirectory, pe.DelayImportTableDirectory, pe.CorHeaderTableDirectory, default,
        ];
        for (int i = 0; i < pe.NumberOfRvaAndSizes && i < dirs.Length; i++)
        {
            if (i != 14)
            {
                miscPESize += dirs[i].Size;
            }
        }

        PrintLine($"// {Left($"PE additional info   : {miscPESize}", 40)} ({Pct(miscPESize, fileSize)}%)");
        int numberOfSections = h.CoffHeader.NumberOfSections;
        PrintLine($"// Num.of PE sections   : {numberOfSections}");
        int corCb = (int)U32(CorHeaderFileOffset());
        PrintLine($"// {Left($"CLR header size     : {corCb}", 40)} ({Pct(corCb, fileSize)}%)");
        int cbMetaData = cor.MetadataDirectory.Size;
        PrintLine($"// {Left($"CLR meta-data size  : {cbMetaData}", 40)} ({Pct(cbMetaData, fileSize)}%)");

        int miscCOMPlusSize = cor.ResourcesDirectory.Size + cor.StrongNameSignatureDirectory.Size + cor.CodeManagerTableDirectory.Size
            + cor.VtableFixupsDirectory.Size + cor.ExportAddressTableJumpsDirectory.Size + cor.ManagedNativeHeaderDirectory.Size;
        PrintLine($"// {Left($"CLR additional info : {miscCOMPlusSize}", 40)} ({Pct(miscCOMPlusSize, fileSize)}%)");

        int methodHeaderSize = 0, methodBodySize = 0, methodBodies = 0, fatHeaders = 0, tinyHeaders = 0, fatSections = 0, smallSections = 0;
        foreach (MethodDefinitionHandle mh in _md.MethodDefinitions)
        {
            MethodDefinition md = _md.GetMethodDefinition(mh);
            int rva = md.RelativeVirtualAddress;
            int codeType = (int)md.ImplAttributes & 0x3;
            if (rva == 0 || (codeType != 0 && codeType != 2))
            {
                continue;
            }

            methodBodies++;
            ReadOnlySpan<byte> b = SpanAtRva((uint)rva);
            if (b.Length == 0)
            {
                continue;
            }

            if ((b[0] & 0x3) == 0x3)
            {
                fatHeaders++;
                int headerSize = (b[1] >> 4) * 4;
                int codeSize = (int)ReadUInt32(b, 4);
                methodHeaderSize += headerSize;
                methodBodySize += codeSize;
                if ((b[0] & 0x8) != 0)
                {
                    int sectsBegin = headerSize + codeSize;
                    int p = (sectsBegin + 3) & ~3;
                    bool more;
                    do
                    {
                        byte kind = b[p];
                        more = (kind & 0x80) != 0;
                        if ((kind & 0x40) != 0)
                        {
                            fatSections++;
                            p += b[p + 1] | b[p + 2] << 8 | b[p + 3] << 16;
                        }
                        else
                        {
                            smallSections++;
                            p += b[p + 1];
                        }

                        p = (p + 3) & ~3;
                    }
                    while (more);

                    methodHeaderSize += p - sectsBegin;
                }
            }
            else if ((b[0] & 0x3) == 0x2)
            {
                tinyHeaders++;
                methodHeaderSize += 1;
                methodBodySize += b[0] >> 2;
            }
        }

        PrintLine($"// {Left($"CLR method headers  : {methodHeaderSize}", 40)} ({Pct(methodHeaderSize, fileSize)}%)");
        PrintLine($"// {Left($"Managed code         : {methodBodySize}", 40)} ({Pct(methodBodySize, fileSize)}%)");
        int initData = pe.SizeOfInitializedData;
        PrintLine($"// {Left($"Data                 : {initData}", 40)} ({Pct(initData, fileSize)}%)");
        size = unchecked(fileSize - sizeOfHeaders - miscPESize - corCb - cbMetaData - miscCOMPlusSize - initData - methodHeaderSize - methodBodySize);
        PrintLine($"// {Left($"Unaccounted          : {size}", 40)} ({Pct(size, fileSize)}%)");

        PrintLine("");
        PrintLine($"// Num.of PE sections   : {numberOfSections}");
        foreach (SectionHeader sh in h.SectionHeaders)
        {
            PrintLine($"//   {Left(CSectionName(sh), 8)} - {sh.SizeOfRawData}");
        }

        PrintLine("");
        PrintLine($"// CLR meta-data size  : {cbMetaData}");

        void Table(TableIndex t, string? name)
        {
            seen.Add(t);
            int count = RowCount(t);
            if (count > 0)
            {
                int s = count * _md.GetTableRowSize(t);
                metaSize += s;
                PrintLine($"//   {Left(name ?? t.ToString(), 14)}- {count,4} ({s} bytes)");
            }
        }

        {
            seen.Add(TableIndex.Module);
            int count = RowCount(TableIndex.Module);
            int s = count * _md.GetTableRowSize(TableIndex.Module);
            metaSize += s;
            PrintLine($"//   {Left("Module", 14)}- {count,4} ({s} bytes)");
        }

        int typeDefs = RowCount(TableIndex.TypeDef);
        if (typeDefs - 1 > 0)
        {
            int interfaces = 0, explicitLayout = 0;
            for (int i = 1; i <= typeDefs - 1; i++)
            {
                int flags = (int)_md.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(i)).Attributes;
                if ((flags & 0x20) != 0) interfaces++;
                if ((flags & 0x10) != 0) explicitLayout++;
            }

            seen.Add(TableIndex.TypeDef);
            int s = typeDefs * _md.GetTableRowSize(TableIndex.TypeDef);
            metaSize += s;
            PrintLine($"// {Left($"  TypeDef       - {typeDefs,4} ({s} bytes)", 38)} {interfaces} interfaces, {explicitLayout} explicit layout");
        }

        seen.Add(TableIndex.TypeRef);
        if (RowCount(TableIndex.TypeRef) > 0)
        {
            int count = RowCount(TableIndex.TypeRef);
            int s = count * _md.GetTableRowSize(TableIndex.TypeRef);
            metaSize += s;
            PrintLine($"//   {Left("TypeRef", 14)}- {count,4} ({s} bytes)");
        }

        int methods = RowCount(TableIndex.MethodDef);
        if (methods > 0)
        {
            int abstractCount = 0;
            foreach (MethodDefinitionHandle mh in _md.MethodDefinitions)
            {
                if ((_md.GetMethodDefinition(mh).Attributes & MethodAttributes.Abstract) != 0)
                {
                    abstractCount++;
                }
            }

            seen.Add(TableIndex.MethodDef);
            int s = methods * _md.GetTableRowSize(TableIndex.MethodDef);
            metaSize += s;
            PrintLine($"// {Left($"  MethodDef     - {methods,4} ({s} bytes)", 38)} {abstractCount} abstract, 0 native, {methodBodies} bodies");
        }

        int fields = RowCount(TableIndex.Field);
        if (fields > 0)
        {
            int constants = 0;
            foreach (FieldDefinitionHandle fh in _md.FieldDefinitions)
            {
                FieldAttributes a = _md.GetFieldDefinition(fh).Attributes;
                if ((a & (FieldAttributes.Static | FieldAttributes.InitOnly)) == (FieldAttributes.Static | FieldAttributes.InitOnly))
                {
                    constants++;
                }
            }

            int s = fields * _md.GetTableRowSize(TableIndex.Field);
            metaSize += s;
            PrintLine($"// {Left($"  FieldDef      - {fields,4} ({s} bytes)", 38)} {constants} constant");
            seen.Add(TableIndex.Field);
        }

        Table(TableIndex.MemberRef, "MemberRef");
        Table(TableIndex.Param, "ParamDef");
        Table(TableIndex.MethodImpl, "MethodImpl");
        Table(TableIndex.Constant, "Constant");
        Table(TableIndex.CustomAttribute, "CustomAttribute");
        Table(TableIndex.FieldMarshal, "NativeType");
        Table(TableIndex.ClassLayout, "ClassLayout");
        Table(TableIndex.FieldLayout, "FieldLayout");
        Table(TableIndex.StandAloneSig, "StandAloneSig");
        Table(TableIndex.InterfaceImpl, "InterfaceImpl");
        Table(TableIndex.PropertyMap, "PropertyMap");
        Table(TableIndex.Property, "Property");
        Table(TableIndex.MethodSemantics, "MethodSemantic");
        Table(TableIndex.DeclSecurity, "Security");
        Table(TableIndex.TypeSpec, "TypeSpec");
        Table(TableIndex.ModuleRef, "ModuleRef");
        Table(TableIndex.Assembly, "Assembly");
        Table(TableIndex.AssemblyProcessor, "AssemblyProcessor");
        Table(TableIndex.AssemblyOS, "AssemblyOS");
        Table(TableIndex.AssemblyRef, "AssemblyRef");
        Table(TableIndex.AssemblyRefProcessor, "AssemblyRefProcessor");
        Table(TableIndex.AssemblyRefOS, "AssemblyRefOS");
        Table(TableIndex.File, "File");
        Table(TableIndex.ExportedType, "ExportedType");
        Table(TableIndex.ManifestResource, "ManifestResource");
        Table(TableIndex.NestedClass, "NestedClass");
        for (int i = 0; i <= (int)TableIndex.GenericParamConstraint; i++)
        {
            TableIndex t = (TableIndex)i;
            if (!seen.Contains(t))
            {
                Table(t, s_schemaTableNames[i]);
            }
        }

        void Heap(HeapIndex hi, string label)
        {
            int s = _md.GetHeapSize(hi);
            if (s > 0)
            {
                metaSize += s;
                PrintLine($"//   {label} - {s,5} bytes");
            }
        }

        Heap(HeapIndex.String, "Strings      ");
        Heap(HeapIndex.Blob, "Blobs        ");
        Heap(HeapIndex.UserString, "UserStrings  ");
        Heap(HeapIndex.Guid, "Guids        ");
        if (cbMetaData != metaSize)
        {
            PrintLine($"//   Uncategorized - {cbMetaData - metaSize,5} bytes");
        }

        if (miscCOMPlusSize != 0)
        {
            PrintLine("");
            PrintLine($"// CLR additional info : {miscCOMPlusSize}");
            if (cor.CodeManagerTableDirectory.Size != 0)
            {
                PrintLine($"//   CodeManagerTable  - {cor.CodeManagerTableDirectory.Size}");
            }

            if (cor.VtableFixupsDirectory.Size != 0)
            {
                PrintLine($"//   VTableFixups      - {cor.VtableFixupsDirectory.Size}");
            }

            if (cor.ResourcesDirectory.Size != 0)
            {
                PrintLine($"//   Resources         - {cor.ResourcesDirectory.Size}");
            }
        }

        PrintLine("");
        PrintLine($"// CLR method headers : {methodHeaderSize}");
        PrintLine($"//   Num.of method bodies  - {methodBodies}");
        PrintLine($"//   Num.of fat headers    - {fatHeaders}");
        PrintLine($"//   Num.of tiny headers   - {tinyHeaders}");
        if (fatSections != 0 || smallSections != 0)
        {
            PrintLine($"//   Num.of fat sections   - {fatSections}");
            PrintLine($"//   Num.of small sections - {smallSections}");
        }

        PrintLine("");
        PrintLine($"// Managed code : {methodBodySize}");
        if (methodBodies != 0)
        {
            PrintLine($"//   Ave method size - {methodBodySize / methodBodies}");
        }
    }

    // The metadata schema table names (g_Tables in the native metadata reader).
    private static readonly string[] s_schemaTableNames =
    [
        "Module", "TypeRef", "TypeDef", "FieldPtr", "Field", "MethodPtr", "Method", "ParamPtr", "Param", "InterfaceImpl",
        "MemberRef", "Constant", "CustomAttribute", "FieldMarshal", "DeclSecurity", "ClassLayout", "FieldLayout", "StandAloneSig",
        "EventMap", "EventPtr", "Event", "PropertyMap", "PropertyPtr", "Property", "MethodSemantics", "MethodImpl", "ModuleRef",
        "TypeSpec", "ImplMap", "FieldRVA", "ENCLog", "ENCMap", "Assembly", "AssemblyProcessor", "AssemblyOS", "AssemblyRef",
        "AssemblyRefProcessor", "AssemblyRefOS", "File", "ExportedType", "ManifestResource", "NestedClass", "GenericParam",
        "MethodSpec", "GenericParamConstraint",
    ];

    private void PrintClassList()
    {
        if (_classList.Count == 0)
        {
            PrintLine(COMMENT("// No classes defined in this module"));
            return;
        }

        PrintLine(COMMENT("// Classes defined in this module:"));
        PrintLine(COMMENT("//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"));
        foreach (int cl in _classList)
        {
            string name = MakeNameIfNone(GetNameOfTypeDef(cl).Name, cl);
            int a = (int)GetTypeDefProps(cl).Attrs;
            StringBuilder sb = new("// ");
            sb.Append((a & 0x20) != 0 ? "Interface " : "Class ");
            sb.Append(Left(name, 30)).Append(' ');
            if ((a & 0x7) == 1) sb.Append("(public) ");
            if ((a & 0x80) != 0) sb.Append("(abstract) ");
            switch (a & 0x18)
            {
                case 0x00: sb.Append("(auto) "); break;
                case 0x08: sb.Append("(sequential) "); break;
                case 0x18: sb.Append("(extended) "); break;
                case 0x10: sb.Append("(explicit) "); break;
            }

            switch (a & 0x30000)
            {
                case 0x00000: sb.Append("(ansi) "); break;
                case 0x10000: sb.Append("(unicode) "); break;
                case 0x20000: sb.Append("(autochar) "); break;
            }

            if ((a & 0x1000) != 0) sb.Append("(import) ");
            if ((a & 0x4000) != 0) sb.Append("(windowsruntime) ");
            if ((a & 0x100) != 0) sb.Append("(sealed) ");
            switch (a & 0x7)
            {
                case 2: sb.Append("(nested public) "); break;
                case 3: sb.Append("(nested private) "); break;
                case 4: sb.Append("(nested family) "); break;
                case 5: sb.Append("(nested assembly) "); break;
                case 6: sb.Append("(nested famANDassem) "); break;
                case 7: sb.Append("(nested famORassem) "); break;
            }

            PrintLine(COMMENT(sb.ToString()));
        }

        PrintLine(COMMENT("//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"));
        PrintLine("");
    }

    // DumpResourceToFile (the native dres.cpp, Windows only): saves the Win32 resources as a .res file next to the output.
    private void DumpWin32Resources()
    {
        // The native tool replaces whatever follows the last '.' of the whole path.
        int dot = _outputFileName.LastIndexOf('.');
        string resFile = (dot < 0 ? _outputFileName : _outputFileName.Substring(0, dot)) + ".res";
        uint ret = DumpResourceToFile(resFile);
        switch (ret)
        {
            case 1:
                PrintLine(COMMENT($"// WARNING: Created Win32 resource file {resFile}"));
                break;
            case 0xDFFFFFFF:
                PrintError("// ERROR: Corrupt Win32 resources");
                break;
            case 0xEFFFFFFF:
                PrintError($"// ERROR: Unable to open file {resFile}");
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

    private uint DumpResourceToFile(string fileName)
    {
        DirectoryEntry dir = _pe.PEHeaders.PEHeader!.ResourceTableDirectory;
        if (dir.RelativeVirtualAddress == 0 || dir.Size == 0)
        {
            return 0;
        }

        byte[]? res = GetBytesAtRva((uint)dir.RelativeVirtualAddress, -1);
        if (res is null)
        {
            return 0;
        }

        List<ResourceNode> nodes = new();
        try
        {
            ushort Count(int dirOffset) => (ushort)(BitConverter.ToUInt16(res, dirOffset + 12) + BitConverter.ToUInt16(res, dirOffset + 14));

            ResourceNode Leaf(uint tid, uint nid, uint lid, uint dataEntryOffset)
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

                n.DataRva = BitConverter.ToUInt32(res, (int)dataEntryOffset);
                n.DataSize = BitConverter.ToUInt32(res, (int)dataEntryOffset + 4);
                return n;
            }

            int nTypes = Count(0);
            for (int i = 0; i < nTypes; i++)
            {
                int te = 16 + i * 8;
                uint typeId = BitConverter.ToUInt32(res, te);
                uint typeOff = BitConverter.ToUInt32(res, te + 4);
                if ((typeOff & 0x80000000) == 0)
                {
                    nodes.Add(Leaf(typeId, 0, 0, typeOff));
                    continue;
                }

                int nameDir = (int)(typeOff & 0x7FFFFFFF);
                int nNames = Count(nameDir);
                for (int j = 0; j < nNames; j++)
                {
                    int ne = nameDir + 16 + j * 8;
                    uint nameId = BitConverter.ToUInt32(res, ne);
                    uint nameOff = BitConverter.ToUInt32(res, ne + 4);
                    if ((nameOff & 0x80000000) == 0)
                    {
                        nodes.Add(Leaf(typeId, nameId, 0, nameOff));
                        continue;
                    }

                    int langDir = (int)(nameOff & 0x7FFFFFFF);
                    int nLangs = Count(langDir);
                    for (int k = 0; k < nLangs; k++)
                    {
                        int le = langDir + 16 + k * 8;
                        uint langOff = BitConverter.ToUInt32(res, le + 4);
                        if ((langOff & 0x80000000) == 0)
                        {
                            nodes.Add(Leaf(typeId, nameId, BitConverter.ToUInt32(res, le), langOff));
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

        FileStream f;
        try
        {
            f = new FileStream(fileName, FileMode.Create, FileAccess.Write);
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
                byte[]? data = GetBytesAtRva(n.DataRva, (int)n.DataSize);
                if (data is null)
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

    private static string ResString(byte[] res, int offset)
    {
        int len = BitConverter.ToUInt16(res, offset);
        return Encoding.Unicode.GetString(res, offset + 2, len * 2);
    }
}
