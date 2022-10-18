// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace LibObjectFile.Dwarf
{
    public sealed class DwarfAbbreviationItem : DwarfObject<DwarfAbbreviation>
    {
        internal DwarfAbbreviationItem()
        {
        }

        internal DwarfAbbreviationItem(ulong code, DwarfTagEx tag, bool hasChildren, DwarfAttributeDescriptors descriptors)
        {
            Code = code;
            Tag = tag;
            HasChildren = hasChildren;
            Descriptors = descriptors;
        }
        
        public ulong Code { get; internal set; }

        public DwarfTagEx Tag { get; private set; }

        public bool HasChildren { get; private set; }

        public DwarfAttributeDescriptors Descriptors { get; private set; }

        protected override void UpdateLayout(DwarfLayoutContext layoutContext)
        {
            var endOffset = Offset;

            // Code
            endOffset += DwarfHelper.SizeOfULEB128(Code);

            // Tag
            endOffset += DwarfHelper.SizeOfULEB128((uint)Tag.Value);

            // HasChildren
            endOffset += 1;

            var descriptors = Descriptors;
            for (int i = 0; i < descriptors.Length; i++)
            {
                var descriptor = descriptors[i];
                endOffset += DwarfHelper.SizeOfULEB128((uint)descriptor.Kind.Value);
                endOffset += DwarfHelper.SizeOfULEB128((uint)descriptor.Form.Value);
            }

            // Null Kind and Form
            endOffset += DwarfHelper.SizeOfULEB128(0) * 2;

            Size = endOffset - Offset;
        }

        protected override void Read(DwarfReader reader)
        {
            var itemTag = new DwarfTagEx(reader.ReadULEB128AsU32());
            Tag = itemTag;
            var hasChildrenRaw = reader.ReadU8();
            bool hasChildren = false;
            if (hasChildrenRaw == DwarfNative.DW_CHILDREN_yes)
            {
                hasChildren = true;
            }
            else if (hasChildrenRaw != DwarfNative.DW_CHILDREN_no)
            {
                reader.Diagnostics.Error(DiagnosticId.DWARF_ERR_InvalidData, $"Invalid children {hasChildrenRaw}. Must be either {DwarfNative.DW_CHILDREN_yes} or {DwarfNative.DW_CHILDREN_no}");
                return;
            }

            HasChildren = hasChildren;

            List<DwarfAttributeDescriptor> descriptors = null;

            while (true)
            {
                var attributeName = new DwarfAttributeKindEx(reader.ReadULEB128AsU32());
                var attributeForm = new DwarfAttributeFormEx(reader.ReadULEB128AsU32());

                if (attributeForm.Value == 0 && attributeForm.Value == 0)
                {
                    break;
                }

                if (descriptors == null) descriptors = new List<DwarfAttributeDescriptor>(1);
                descriptors.Add(new DwarfAttributeDescriptor(attributeName, attributeForm));
            }

            Descriptors = descriptors != null ? new DwarfAttributeDescriptors(descriptors.ToArray()) : new DwarfAttributeDescriptors();

            Size = reader.Offset - Offset;
        }

        protected override void Write(DwarfWriter writer)
        {
            var startOffset = writer.Offset;
            Debug.Assert(startOffset == Offset);

            // Code
            writer.WriteULEB128(Code);

            // Tag
            writer.WriteULEB128((uint)Tag.Value);

            // HasChildren
            writer.WriteU8(HasChildren ? DwarfNative.DW_CHILDREN_yes : DwarfNative.DW_CHILDREN_no);

            var descriptors = Descriptors;
            for (int i = 0; i < descriptors.Length; i++)
            {
                var descriptor = descriptors[i];
                writer.WriteULEB128((uint)descriptor.Kind.Value);
                writer.WriteULEB128((uint)descriptor.Form.Value);
            }
            writer.WriteULEB128(0);
            writer.WriteULEB128(0);

            Debug.Assert(writer.Offset - startOffset == Size);
        }
    }
}