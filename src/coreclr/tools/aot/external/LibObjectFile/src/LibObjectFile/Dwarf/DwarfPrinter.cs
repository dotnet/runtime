// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;
using System.Linq;

namespace LibObjectFile.Dwarf
{
    public static class DwarfPrinter
    {
        public static void Print(this DwarfAbbreviationTable abbrevTable, TextWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            writer.WriteLine("Contents of the .debug_abbrev section:");
            
            foreach (var abbreviation in abbrevTable.Abbreviations)
            {
                Print(abbreviation, writer);
            }
        }

        public static void Print(this DwarfAbbreviation abbreviation, TextWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            writer.WriteLine();

            writer.WriteLine($"  Number TAG (0x{abbreviation.Offset})");

            foreach (var item in abbreviation.Items)
            {
                writer.WriteLine($"   {item.Code}      {item.Tag}    [{(item.HasChildren ? "has children" : "no children")}]");
                var descriptors = item.Descriptors;
                for (int i = 0; i < descriptors.Length; i++)
                {
                    var descriptor = descriptors[i];
                    writer.WriteLine($"    {descriptor.Kind.ToString(),-18} {descriptor.Form}");
                }
                writer.WriteLine("    DW_AT value: 0     DW_FORM value: 0");
            }
        }

        public static void PrintRelocations(this DwarfRelocatableSection relocSection, TextWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            writer.WriteLine();
            if (relocSection.Relocations.Count == 0)
            {
                writer.WriteLine("  There are no relocations in this section.");
                return;
            }

            writer.WriteLine($"  Relocations of this section contains {(relocSection.Relocations.Count > 1 ? $"{relocSection.Relocations.Count} entries" : "1 entry")}:");
            writer.WriteLine();
            writer.WriteLine("    Offset             Target               Size   Addend");
            foreach (var reloc in relocSection.Relocations)
            {
                writer.WriteLine($"{reloc.Offset:x16}   {reloc.Target,-24} {(uint)reloc.Size,-6} {reloc.Addend:x}");
            }
        }

        public static void Print(this DwarfInfoSection debugInfo, TextWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            foreach (var unit in debugInfo.Units)
            {
                Print(unit, writer);
            }
        }

        public static void Print(this DwarfUnit unit, TextWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            writer.WriteLine("Contents of the .debug_info section:");
            writer.WriteLine();
            writer.WriteLine($"  Compilation Unit @ offset 0x{unit.Offset:x}:");
            writer.WriteLine($"   Length:        0x{unit.UnitLength:x}");
            writer.WriteLine($"   Version:       {unit.Version}");
            writer.WriteLine($"   Abbrev Offset: 0x{unit.Abbreviation?.Offset ?? 0:x}");
            writer.WriteLine($"   Pointer Size:  {(uint)unit.AddressSize}");
            if (unit.Root != null)
            {
                Print(unit.Root, writer);
            }
        }

        public static void Print(this DwarfDIE die, TextWriter writer, int level = 0)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            writer.WriteLine($" <{level}><{die.Offset:x}>: Abbrev Number: {die.Abbrev.Code} ({die.Tag})");

            foreach (var attr in die.Attributes)
            {
                string attrValue = null;
                switch (attr.ValueAsObject)
                {
                    case DwarfDIE dieRef:
                        attrValue = $"<0x{dieRef.Offset:x}>";
                        break;
                    case string str:
                        attrValue = str;
                        break;
                    case DwarfExpression expr:
                        attrValue = $"{expr.Operations.Count} OpCodes ({string.Join(", ", expr.Operations.Select(x => x.Kind))})";
                        break;
                }

                switch (attr.Kind.Value)
                {
                    case DwarfAttributeKind.Language:

                        attrValue = $"{attr.ValueAsU64} {GetLanguageKind((DwarfLanguageKind)attr.ValueAsU64)}";
                        break;
                }

                if (attrValue == null)
                {

                    var encoding = DwarfHelper.GetAttributeEncoding(attr.Kind);
                    if ((encoding & DwarfAttributeEncoding.Address) != 0)
                    {
                        attrValue = $"0x{attr.ValueAsU64:x}";
                    }
                    else
                    {
                        attrValue = $"{attr.ValueAsU64}";
                    }
                }

                writer.WriteLine($"    <{attr.Offset:x}>   {attr.Kind,-18}    : {attrValue}");
            }

            foreach (var child in die.Children)
            {
                Print(child, writer, level + 1);
            }
        }

        public static void Print(this DwarfAddressRangeTable addressRangeTable, TextWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            writer.WriteLine("Contents of the .debug_aranges section:");
            writer.WriteLine();
            writer.WriteLine($"  Length:                   {addressRangeTable.HeaderLength}");
            writer.WriteLine($"  Version:                  {addressRangeTable.Version}");
            writer.WriteLine($"  Offset into .debug_info:  0x{addressRangeTable.DebugInfoOffset:x}");
            writer.WriteLine($"  Pointer Size:             {(byte)addressRangeTable.AddressSize}");
            writer.WriteLine($"  Segment Size:             {(byte)addressRangeTable.SegmentSelectorSize}");
            writer.WriteLine();
            var addressSize = (uint)addressRangeTable.AddressSize;
            if (addressSize > 4)
            {
                writer.WriteLine("    Address            Length");
            }
            else
            {
                writer.WriteLine("    Address    Length");
            }

            var formatStyle = "x" + (addressSize * 2);
            foreach (var range in addressRangeTable.Ranges)
            {
                writer.WriteLine($"    {range.Address.ToString(formatStyle)} {range.Length.ToString(formatStyle)}");
            }
            writer.WriteLine($"    {((ulong)0).ToString(formatStyle)} {((ulong)0).ToString(formatStyle)}");
        }

        private static string GetLanguageKind(DwarfLanguageKind kind)
        {
            var rawKind = (uint) kind;
            switch (rawKind)
            {
                case DwarfNative.DW_LANG_C89: return "(ANSI C)";
                case DwarfNative.DW_LANG_C: return "(non-ANSI C)";
                case DwarfNative.DW_LANG_Ada83: return "(Ada)";
                case DwarfNative.DW_LANG_C_plus_plus: return "(C++)";
                case DwarfNative.DW_LANG_Cobol74: return "(Cobol 74)";
                case DwarfNative.DW_LANG_Cobol85: return "(Cobol 85)";
                case DwarfNative.DW_LANG_Fortran77: return "(FORTRAN 77)";
                case DwarfNative.DW_LANG_Fortran90: return "(Fortran 90)";
                case DwarfNative.DW_LANG_Pascal83: return "(ANSI Pascal)";
                case DwarfNative.DW_LANG_Modula2: return "(Modula 2)";
                // DWARF 2.1
                case DwarfNative.DW_LANG_Java: return "(Java)";
                case DwarfNative.DW_LANG_C99: return "(ANSI C99)";
                case DwarfNative.DW_LANG_Ada95: return "(ADA 95)";
                case DwarfNative.DW_LANG_Fortran95: return "(Fortran 95)";
                // DWARF 3
                case DwarfNative.DW_LANG_PLI: return "(PLI)";
                case DwarfNative.DW_LANG_ObjC: return "(Objective C)";
                case DwarfNative.DW_LANG_ObjC_plus_plus: return "(Objective C++)";
                case DwarfNative.DW_LANG_UPC: return "(Unified Parallel C)";
                case DwarfNative.DW_LANG_D: return "(D)";
                // DWARF 4
                case DwarfNative.DW_LANG_Python: return "(Python)";
                // DWARF 5
                case DwarfNative.DW_LANG_OpenCL: return "(OpenCL)";
                case DwarfNative.DW_LANG_Go: return "(Go)";
                case DwarfNative.DW_LANG_Modula3: return "(Modula 3)";
                case DwarfNative.DW_LANG_Haskel: return "(Haskell)";
                case DwarfNative.DW_LANG_C_plus_plus_03: return "(C++03)";
                case DwarfNative.DW_LANG_C_plus_plus_11: return "(C++11)";
                case DwarfNative.DW_LANG_OCaml: return "(OCaml)";
                case DwarfNative.DW_LANG_Rust: return "(Rust)";
                case DwarfNative.DW_LANG_C11: return "(C11)";
                case DwarfNative.DW_LANG_Swift: return "(Swift)";
                case DwarfNative.DW_LANG_Julia: return "(Julia)";
                case DwarfNative.DW_LANG_Dylan: return "(Dylan)";
                case DwarfNative.DW_LANG_C_plus_plus_14: return "(C++14)";
                case DwarfNative.DW_LANG_Fortran03: return "(Fortran 03)";
                case DwarfNative.DW_LANG_Fortran08: return "(Fortran 08)";
                case DwarfNative.DW_LANG_RenderScript: return "(RenderScript)";

                case DwarfNative.DW_LANG_Mips_Assembler: return "(MIPS assembler)";
                
                case DwarfNative.DW_LANG_Upc: return "(Unified Parallel C)";
                
                default:
                    if (rawKind >= DwarfNative.DW_LANG_lo_user && rawKind <= DwarfNative.DW_LANG_hi_user)
                        return $"(implementation defined: {rawKind:x})";
                    break;
            }

            return $"(Unknown: {rawKind:x})";
        }
    }
}