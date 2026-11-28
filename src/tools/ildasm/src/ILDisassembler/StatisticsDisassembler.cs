// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace ILDisassembler;

// /STATS: statistics on the sizes of the parts of the image.
internal sealed class StatisticsDisassembler : IMetadataDisassembler
{
    public DisassemblyPhase Phase => DisassemblyPhase.Summary;

    public void Disassemble(Dasm dasm) => DumpStatistics(dasm);

    private static string Pct(int value, int fileSize)
    {
        double d = (double)value * 100 / fileSize;
        return d.ToString("F2", CultureInfo.InvariantCulture).PadLeft(5);
    }

    private static string Left(string s, int width) => s.Length >= width ? s : s.PadRight(width);

    private static void DumpStatistics(Dasm dasm)
    {
        PEHeaders h = dasm.PE.PEHeaders;
        PEHeader pe = h.PEHeader!;
        CorHeader cor = h.CorHeader!;
        HashSet<TableIndex> seen = new();
        int metaSize = 0;
        int fileSize = dasm.Image.Length;
        dasm.PrintLine($"// File size            : {fileSize}");

        bool pe32 = pe.Magic == PEMagic.PE32;
        int lfanew = (int)dasm.U32(0x3C);
        int ntHeadersNoOpt = 4 + 20; // signature + file header
        int size = lfanew + ntHeadersNoOpt + h.CoffHeader.SizeOfOptionalHeader + h.CoffHeader.NumberOfSections * 40;
        int sizeOfHeaders = pe.SizeOfHeaders;
        dasm.PrintLine($"// PE header size       : {sizeOfHeaders} ({size} used)    ({Pct(sizeOfHeaders, fileSize)}%)");

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

        dasm.PrintLine($"// {Left($"PE additional info   : {miscPESize}", 40)} ({Pct(miscPESize, fileSize)}%)");
        int numberOfSections = h.CoffHeader.NumberOfSections;
        dasm.PrintLine($"// Num.of PE sections   : {numberOfSections}");
        int corCb = (int)dasm.U32(dasm.CorHeaderFileOffset());
        dasm.PrintLine($"// {Left($"CLR header size     : {corCb}", 40)} ({Pct(corCb, fileSize)}%)");
        int cbMetaData = cor.MetadataDirectory.Size;
        dasm.PrintLine($"// {Left($"CLR meta-data size  : {cbMetaData}", 40)} ({Pct(cbMetaData, fileSize)}%)");

        int miscCOMPlusSize = cor.ResourcesDirectory.Size + cor.StrongNameSignatureDirectory.Size + cor.CodeManagerTableDirectory.Size
            + cor.VtableFixupsDirectory.Size + cor.ExportAddressTableJumpsDirectory.Size + cor.ManagedNativeHeaderDirectory.Size;
        dasm.PrintLine($"// {Left($"CLR additional info : {miscCOMPlusSize}", 40)} ({Pct(miscCOMPlusSize, fileSize)}%)");

        int methodHeaderSize = 0, methodBodySize = 0, methodBodies = 0, fatHeaders = 0, tinyHeaders = 0, fatSections = 0, smallSections = 0;
        foreach (MethodDefinitionHandle mh in dasm.Metadata.MethodDefinitions)
        {
            MethodDefinition md = dasm.Metadata.GetMethodDefinition(mh);
            int rva = md.RelativeVirtualAddress;
            MethodImplAttributes codeType = md.ImplAttributes & MethodImplAttributes.CodeTypeMask;
            if (rva == 0 || codeType is not (MethodImplAttributes.IL or MethodImplAttributes.OPTIL))
            {
                continue;
            }

            methodBodies++;
            ReadOnlySpan<byte> b = dasm.SpanAtRva((uint)rva);
            if (b.Length == 0)
            {
                continue;
            }

            // The headers and all the extra sections, which MethodBodyBlock does not describe (it reads only an EH
            // section, and rejects malformed headers).
            if ((b[0] & Dasm.MethodHeaderFormatMask) == Dasm.FatFormat)
            {
                fatHeaders++;
                int headerSize = (b[1] >> 4) * 4;
                int codeSize = (int)Dasm.ReadUInt32(b, 4);
                methodHeaderSize += headerSize;
                methodBodySize += codeSize;
                if ((b[0] & Dasm.FatFormatMoreSects) != 0)
                {
                    int sectsBegin = headerSize + codeSize;
                    int p = (sectsBegin + 3) & ~3;
                    bool more;
                    do
                    {
                        byte kind = b[p];
                        more = (kind & Dasm.SectionMoreSects) != 0;
                        if ((kind & Dasm.SectionFatFormat) != 0)
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
            else if ((b[0] & Dasm.MethodHeaderFormatMask) == Dasm.TinyFormat)
            {
                tinyHeaders++;
                methodHeaderSize += 1;
                methodBodySize += b[0] >> 2;
            }
        }

        dasm.PrintLine($"// {Left($"CLR method headers  : {methodHeaderSize}", 40)} ({Pct(methodHeaderSize, fileSize)}%)");
        dasm.PrintLine($"// {Left($"Managed code         : {methodBodySize}", 40)} ({Pct(methodBodySize, fileSize)}%)");
        int initData = pe.SizeOfInitializedData;
        dasm.PrintLine($"// {Left($"Data                 : {initData}", 40)} ({Pct(initData, fileSize)}%)");
        size = unchecked(fileSize - sizeOfHeaders - miscPESize - corCb - cbMetaData - miscCOMPlusSize - initData - methodHeaderSize - methodBodySize);
        dasm.PrintLine($"// {Left($"Unaccounted          : {size}", 40)} ({Pct(size, fileSize)}%)");

        dasm.PrintLine("");
        dasm.PrintLine($"// Num.of PE sections   : {numberOfSections}");
        foreach (SectionHeader sh in h.SectionHeaders)
        {
            dasm.PrintLine($"//   {Left(Dasm.CSectionName(sh), 8)} - {sh.SizeOfRawData}");
        }

        dasm.PrintLine("");
        dasm.PrintLine($"// CLR meta-data size  : {cbMetaData}");

        void Table(TableIndex t, string? name)
        {
            seen.Add(t);
            int count = dasm.RowCount(t);
            if (count > 0)
            {
                int s = count * dasm.Metadata.GetTableRowSize(t);
                metaSize += s;
                dasm.PrintLine($"//   {Left(name ?? t.ToString(), 14)}- {count,4} ({s} bytes)");
            }
        }

        {
            seen.Add(TableIndex.Module);
            int count = dasm.RowCount(TableIndex.Module);
            int s = count * dasm.Metadata.GetTableRowSize(TableIndex.Module);
            metaSize += s;
            dasm.PrintLine($"//   {Left("Module", 14)}- {count,4} ({s} bytes)");
        }

        int typeDefs = dasm.RowCount(TableIndex.TypeDef);
        if (typeDefs - 1 > 0)
        {
            int interfaces = 0, explicitLayout = 0;
            int i = 0;
            foreach (TypeDefinitionHandle td in dasm.Metadata.TypeDefinitions)
            {
                // COMPAT: all but the last row are counted.
                if (++i == typeDefs)
                {
                    break;
                }

                TypeAttributes flags = dasm.Metadata.GetTypeDefinition(td).Attributes;
                if ((flags & TypeAttributes.Interface) != 0) interfaces++;
                if ((flags & TypeAttributes.ExplicitLayout) != 0) explicitLayout++;
            }

            seen.Add(TableIndex.TypeDef);
            int s = typeDefs * dasm.Metadata.GetTableRowSize(TableIndex.TypeDef);
            metaSize += s;
            dasm.PrintLine($"// {Left($"  TypeDef       - {typeDefs,4} ({s} bytes)", 38)} {interfaces} interfaces, {explicitLayout} explicit layout");
        }

        seen.Add(TableIndex.TypeRef);
        if (dasm.RowCount(TableIndex.TypeRef) > 0)
        {
            int count = dasm.RowCount(TableIndex.TypeRef);
            int s = count * dasm.Metadata.GetTableRowSize(TableIndex.TypeRef);
            metaSize += s;
            dasm.PrintLine($"//   {Left("TypeRef", 14)}- {count,4} ({s} bytes)");
        }

        int methods = dasm.RowCount(TableIndex.MethodDef);
        if (methods > 0)
        {
            int abstractCount = 0;
            foreach (MethodDefinitionHandle mh in dasm.Metadata.MethodDefinitions)
            {
                if ((dasm.Metadata.GetMethodDefinition(mh).Attributes & MethodAttributes.Abstract) != 0)
                {
                    abstractCount++;
                }
            }

            seen.Add(TableIndex.MethodDef);
            int s = methods * dasm.Metadata.GetTableRowSize(TableIndex.MethodDef);
            metaSize += s;
            dasm.PrintLine($"// {Left($"  MethodDef     - {methods,4} ({s} bytes)", 38)} {abstractCount} abstract, 0 native, {methodBodies} bodies");
        }

        int fields = dasm.RowCount(TableIndex.Field);
        if (fields > 0)
        {
            int constants = 0;
            foreach (FieldDefinitionHandle fh in dasm.Metadata.FieldDefinitions)
            {
                FieldAttributes a = dasm.Metadata.GetFieldDefinition(fh).Attributes;
                if ((a & (FieldAttributes.Static | FieldAttributes.InitOnly)) == (FieldAttributes.Static | FieldAttributes.InitOnly))
                {
                    constants++;
                }
            }

            int s = fields * dasm.Metadata.GetTableRowSize(TableIndex.Field);
            metaSize += s;
            dasm.PrintLine($"// {Left($"  FieldDef      - {fields,4} ({s} bytes)", 38)} {constants} constant");
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
            int s = dasm.Metadata.GetHeapSize(hi);
            if (s > 0)
            {
                metaSize += s;
                dasm.PrintLine($"//   {label} - {s,5} bytes");
            }
        }

        Heap(HeapIndex.String, "Strings      ");
        Heap(HeapIndex.Blob, "Blobs        ");
        Heap(HeapIndex.UserString, "UserStrings  ");
        Heap(HeapIndex.Guid, "Guids        ");
        if (cbMetaData != metaSize)
        {
            dasm.PrintLine($"//   Uncategorized - {cbMetaData - metaSize,5} bytes");
        }

        if (miscCOMPlusSize != 0)
        {
            dasm.PrintLine("");
            dasm.PrintLine($"// CLR additional info : {miscCOMPlusSize}");
            if (cor.CodeManagerTableDirectory.Size != 0)
            {
                dasm.PrintLine($"//   CodeManagerTable  - {cor.CodeManagerTableDirectory.Size}");
            }

            if (cor.VtableFixupsDirectory.Size != 0)
            {
                dasm.PrintLine($"//   VTableFixups      - {cor.VtableFixupsDirectory.Size}");
            }

            if (cor.ResourcesDirectory.Size != 0)
            {
                dasm.PrintLine($"//   Resources         - {cor.ResourcesDirectory.Size}");
            }
        }

        dasm.PrintLine("");
        dasm.PrintLine($"// CLR method headers : {methodHeaderSize}");
        dasm.PrintLine($"//   Num.of method bodies  - {methodBodies}");
        dasm.PrintLine($"//   Num.of fat headers    - {fatHeaders}");
        dasm.PrintLine($"//   Num.of tiny headers   - {tinyHeaders}");
        if (fatSections != 0 || smallSections != 0)
        {
            dasm.PrintLine($"//   Num.of fat sections   - {fatSections}");
            dasm.PrintLine($"//   Num.of small sections - {smallSections}");
        }

        dasm.PrintLine("");
        dasm.PrintLine($"// Managed code : {methodBodySize}");
        if (methodBodies != 0)
        {
            dasm.PrintLine($"//   Ave method size - {methodBodySize / methodBodies}");
        }
    }

    // The metadata schema table names.
    private static readonly string[] s_schemaTableNames =
    [
        "Module", "TypeRef", "TypeDef", "FieldPtr", "Field", "MethodPtr", "Method", "ParamPtr", "Param", "InterfaceImpl",
        "MemberRef", "Constant", "CustomAttribute", "FieldMarshal", "DeclSecurity", "ClassLayout", "FieldLayout", "StandAloneSig",
        "EventMap", "EventPtr", "Event", "PropertyMap", "PropertyPtr", "Property", "MethodSemantics", "MethodImpl", "ModuleRef",
        "TypeSpec", "ImplMap", "FieldRVA", "ENCLog", "ENCMap", "Assembly", "AssemblyProcessor", "AssemblyOS", "AssemblyRef",
        "AssemblyRefProcessor", "AssemblyRefOS", "File", "ExportedType", "ManifestResource", "NestedClass", "GenericParam",
        "MethodSpec", "GenericParamConstraint",
    ];
}
