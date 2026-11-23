// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ILDisassembler;

// .data: the data referenced through field RVAs and vtable fixups, labeled where it is referenced and dumped at the end.
internal sealed partial class Dasm
{
    // The referenced data: its RVA and the size the reference implies (UnknownSize when not known).
    private readonly List<(uint Rva, uint Size)> _ptrTags = new();

    private static string DataLabelPrefix(SectionHeader sh) => CSectionName(sh) switch
    {
        ".tls" => "T_",
        ".text" => "I_",
        _ => "D_",
    };

    // DumpDataPtr: the label of data at rva, registering it for the .data dump at the end.
    private void DumpDataPtr(StringBuilder sb, uint ptr, uint size)
    {
        SectionHeader? sh = SectionOf(ptr);
        if (sh is not null)
        {
            // The computed size can over-estimate (auto layout); the data cannot extend past its section.
            ulong sectionEnd = (ulong)(uint)sh.Value.VirtualAddress + (uint)sh.Value.VirtualSize;
            if (size != UnknownSize && ptr + (ulong)size > sectionEnd)
            {
                size = (uint)(sectionEnd - ptr);
            }

            int j = _ptrTags.FindIndex(t => t.Rva == ptr);
            if (j < 0)
            {
                _ptrTags.Add((ptr, size));
            }
            else if (_ptrTags[j].Size < size)
            {
                _ptrTags[j] = (ptr, size);
            }

            sb.Append(DataLabelPrefix(sh.Value)).Append(ptr.ToString("X8", CultureInfo.InvariantCulture));
        }
        else
        {
            sb.Append(ERRORMSG($"0x{ptr:X8} /* WARNING: rogue pointer! (size 0x{size:X8}) */"));
        }
    }

    // ---------------------------------------------------------------- field sizes

    // The size of the data of an RVA field, computed from the layout like the runtime does for sequential and
    // explicit layout (an upper bound for auto layout, which only ever packs tighter). 0xFFFFFFFF means unknown: the data then extends to the next label.
    private const uint UnknownSize = 0xFFFFFFFF;

    private readonly Dictionary<TypeDefinitionHandle, (uint Size, uint Align)> _valueTypeLayouts = new();

    private uint SizeOfField(FieldDefinitionHandle field)
    {
        if (field.IsNil)
        {
            return UnknownSize;
        }

        byte[] sig = Blob(_md.GetFieldDefinition(field).Signature);
        try
        {
            int p = 0;
            if (sig.Length == 0 || KindOf(UncompressData(sig, ref p)) != SignatureKind.Field)
            {
                return UnknownSize;
            }

            return LayoutOfType(sig, ref p, 0).Size;
        }
        catch (IndexOutOfRangeException)
        {
            return UnknownSize;
        }
    }

    private (uint Size, uint Align) LayoutOfType(byte[] sig, ref int p, int depth)
    {
        uint ptrSize = IsPE32 ? 4u : 8u;
        while (true)
        {
            SignatureTypeCode typ = (SignatureTypeCode)sig[p++];
            switch (typ)
            {
                case SignatureTypeCode.Boolean:
                case SignatureTypeCode.SByte:
                case SignatureTypeCode.Byte:
                    return (1, 1);
                case SignatureTypeCode.Char:
                case SignatureTypeCode.Int16:
                case SignatureTypeCode.UInt16:
                    return (2, 2);
                case SignatureTypeCode.Int32:
                case SignatureTypeCode.UInt32:
                case SignatureTypeCode.Single:
                    return (4, 4);
                case SignatureTypeCode.Int64:
                case SignatureTypeCode.UInt64:
                case SignatureTypeCode.Double:
                    return (8, 8);
                case SignatureTypeCode.Object:
                case SignatureTypeCode.String:
                case ClassTypeCode:
                case SignatureTypeCode.Pointer:
                case SignatureTypeCode.ByReference:
                case SignatureTypeCode.FunctionPointer:
                case SignatureTypeCode.IntPtr:
                case SignatureTypeCode.UIntPtr:
                case SignatureTypeCode.SZArray:
                case SignatureTypeCode.Array:
                    return (ptrSize, ptrSize);
                case SignatureTypeCode.TypedReference:
                    return (ptrSize * 2, ptrSize);
                case ValueTypeTypeCode:
                {
                    int tk = UncompressToken(sig, ref p);
                    return depth <= 64 && TryGetHandle(tk, out EntityHandle h) && h.Kind == HandleKind.TypeDefinition
                        ? LayoutOfValueType((TypeDefinitionHandle)h, depth + 1)
                        : (UnknownSize, 1);
                }

                case SignatureTypeCode.OptionalModifier:
                case SignatureTypeCode.RequiredModifier:
                    UncompressToken(sig, ref p);
                    continue;
                case SignatureTypeCode.Pinned:
                    continue;
                default:
                    // void, generic parameters and instantiations, and anything unexpected.
                    return (UnknownSize, 1);
            }
        }
    }

    // A speculative layout of a value type of this module, used only to size the data of RVA fields: it depends on
    // implementation details of the runtime's sequential and explicit layout (alignment, packing, padding), and auto
    // layout is treated like sequential. The type system of the image is not closed (types of other assemblies have no
    // known layout), so the runtime's layout algorithm cannot be reused.
    private (uint Size, uint Align) LayoutOfValueType(TypeDefinitionHandle type, int depth)
    {
        if (_valueTypeLayouts.TryGetValue(type, out (uint, uint) known))
        {
            return known;
        }

        // Guards against cycles through invalid metadata.
        _valueTypeLayouts[type] = (UnknownSize, 1);
        TypeDefinition td = _md.GetTypeDefinition(type);
        bool hasLayout = HasClassLayoutRow(type);
        TypeLayout layout = td.GetLayout();
        uint pack = hasLayout && layout.PackingSize != 0 ? (uint)layout.PackingSize : 8;
        uint classSize = hasLayout ? (uint)layout.Size : 0;
        bool isExplicit = (td.Attributes & TypeAttributes.LayoutMask) == TypeAttributes.ExplicitLayout;
        uint size = 0;
        uint align = 1;
        foreach (FieldDefinitionHandle fh in td.GetFields())
        {
            FieldDefinition fd = _md.GetFieldDefinition(fh);
            if ((fd.Attributes & (FieldAttributes.Static | FieldAttributes.Literal)) != 0)
            {
                continue;
            }

            byte[] fsig = Blob(fd.Signature);
            int p = 1;
            (uint fieldSize, uint fieldAlign) = LayoutOfType(fsig, ref p, depth);
            if (fieldSize == UnknownSize)
            {
                return _valueTypeLayouts[type] = (UnknownSize, 1);
            }

            fieldAlign = Math.Min(fieldAlign, pack);
            align = Math.Max(align, fieldAlign);
            if (isExplicit)
            {
                int offset = fd.GetOffset();
                size = Math.Max(size, (offset < 0 ? 0 : (uint)offset) + fieldSize);
            }
            else
            {
                size = (size + fieldAlign - 1) / fieldAlign * fieldAlign + fieldSize;
            }
        }

        size = (size + align - 1) / align * align;
        size = Math.Max(Math.Max(size, classSize), 1);
        return _valueTypeLayouts[type] = (size, align);
    }

    // ---------------------------------------------------------------- the .data dump

    // .data sections for everything referenced via DumpDataPtr.
    private void DumpDataSections()
    {
        if (_ptrTags.Count == 0)
        {
            return;
        }

        // Sorted by RVA (the RVAs are distinct).
        (uint Rva, uint Size)[] tags = _ptrTags.OrderBy(t => t.Rva).ToArray();
        for (int j = 0; j < tags.Length; j++)
        {
            uint fromPtr = tags[j].Rva;
            SectionHeader? shn = SectionOf(fromPtr);
            if (shn is null)
            {
                PrintLine($"ERROR: One of used pointers references undefined data (D_{fromPtr:X8})");
                break;
            }

            SectionHeader sh = shn.Value;
            uint secEnd = (uint)sh.VirtualAddress + (uint)sh.VirtualSize;
            string prefix = DataLabelPrefix(sh);
            uint toPtr = j == tags.Length - 1 ? secEnd : Math.Min(tags[j + 1].Rva, secEnd);
            if (toPtr - fromPtr > tags[j].Size)
            {
                toPtr = fromPtr + tags[j].Size;
            }

            uint limPtr = Math.Min(toPtr, (uint)sh.VirtualAddress + (uint)sh.SizeOfRawData);
            while (true)
            {
                StringBuilder sb = new();
                sb.Append(Indent).Append(KEYWORD(".data")).Append(' ');
                if (prefix == "T_")
                {
                    sb.Append(KEYWORD("tls "));
                }
                else if (prefix == "I_")
                {
                    sb.Append(KEYWORD("cil "));
                }

                if (fromPtr >= limPtr)
                {
                    sb.Append(prefix).Append(fromPtr.ToString("X8", CultureInfo.InvariantCulture)).Append(" = ").Append(KEYWORD("int8")).Append('[').Append(((int)(toPtr - fromPtr)).ToString(CultureInfo.InvariantCulture)).Append(']');
                    PrintLine(sb);
                }
                else
                {
                    sb.Append(prefix).Append(fromPtr.ToString("X8", CultureInfo.InvariantCulture)).Append(" = ").Append(KEYWORD("bytearray")).Append(" (");
                    PrintLine(sb);
                    DumpHexbytes(RawSectionBytes(sh, fromPtr, limPtr - fromPtr), fromPtr, toPtr, limPtr);
                }

                if (limPtr == toPtr && j < tags.Length - 1 && toPtr < tags[j + 1].Rva)
                {
                    uint stptr = tags[j + 1].Rva;
                    uint align = 1;
                    while ((align & stptr) == 0)
                    {
                        align <<= 1;
                    }

                    align -= 1;
                    if ((toPtr & align) != 0)
                    {
                        fromPtr = toPtr;
                        toPtr = (toPtr + align) & ~align;
                        continue;
                    }
                }

                break;
            }
        }
    }

    private void DumpHexbytes(ReadOnlySpan<byte> data, uint fromPtr, uint toPtr, uint limPtr)
    {
        StringBuilder sb = new();
        sb.Append(Indent).Append("                ");
        StringBuilder sz = new();
        int k = 0;
        bool printsz = false;
        for (uint curPtr = fromPtr; curPtr < toPtr; curPtr++, k++)
        {
            if (k == 16)
            {
                if (printsz)
                {
                    sb.Append(COMMENT("  // " + sz));
                }

                PrintLine(sb);
                sb.Clear();
                sb.Append(Indent).Append("                ");
                k = 0;
                sz.Clear();
                printsz = false;
            }

            byte b = curPtr >= limPtr ? (byte)0 : data[(int)(curPtr - fromPtr)];
            sb.Append(' ').Append(b.ToString("X2", CultureInfo.InvariantCulture));
            if (IsPrint(b))
            {
                _formatter.AppendDataCommentChar(sz, (char)b);
                printsz = true;
            }
            else
            {
                sz.Append('.');
            }
        }

        sb.Append(") ");
        if (printsz)
        {
            for (int i = k; i < 16; i++)
            {
                sb.Append("   ");
            }

            sb.Append(COMMENT("// " + sz));
        }

        PrintLine(sb);
    }
}
