// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Dwarf
{
    public abstract class DwarfDIEDeclaration : DwarfDIE
    {
        //  DW_AT_decl_column, DW_AT_decl_file, and DW_AT_decl_line
        public ulong? DeclColumn
        {
            get => GetAttributeValueOpt<ulong>(DwarfAttributeKind.DeclColumn);
            set => SetAttributeValueOpt<ulong>(DwarfAttributeKind.DeclColumn, value);
        }

        public DwarfFileName DeclFile
        {
            get => GetAttributeValue<DwarfFileName>(DwarfAttributeKind.DeclFile);
            set => SetAttributeValue<DwarfFileName>(DwarfAttributeKind.DeclFile, value);
        }

        public ulong? DeclLine
        {
            get => GetAttributeValueOpt<ulong>(DwarfAttributeKind.DeclLine);
            set => SetAttributeValueOpt<ulong>(DwarfAttributeKind.DeclLine, value);
        }
    }
}