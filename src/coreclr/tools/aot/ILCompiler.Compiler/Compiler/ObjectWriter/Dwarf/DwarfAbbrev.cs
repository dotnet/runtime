// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using static ILCompiler.ObjectWriter.DwarfNative;

namespace ILCompiler.ObjectWriter
{
    internal sealed class DwarfAbbrev
    {
        private ushort[] _definition;

        public static ushort DW_FORM_size = 0xdead; // Dummy value

        public DwarfAbbrev(ushort[] definition)
        {
            _definition = definition;
        }

        public ushort Tag => _definition[0];

        public bool HasChildren => _definition[1] == DW_CHILDREN_yes;

        public int AbbreviationCode { get; set; }

        public void Write(SectionWriter writer, int targetPointerSize)
        {
            writer.WriteULEB128((ulong)AbbreviationCode);
            writer.WriteULEB128(Tag);
            writer.WriteULEB128(HasChildren ? DW_CHILDREN_yes : DW_CHILDREN_no);

            for (int i = 2; i < _definition.Length; i++)
            {
                // Attribute
                writer.WriteULEB128(_definition[i++]);
                // Form
                if (_definition[i] != DW_FORM_size)
                {
                    writer.WriteULEB128(_definition[i]);
                }
                else if (targetPointerSize == 8)
                {
                    writer.WriteULEB128(DW_FORM_data8);
                }
                else if (targetPointerSize == 4)
                {
                    writer.WriteULEB128(DW_FORM_data4);
                }
            }

            writer.Write([0, 0]);
        }

        public static DwarfAbbrev CompileUnit = new([
            DW_TAG_compile_unit, DW_CHILDREN_yes,
            DW_AT_producer, DW_FORM_strp,
            DW_AT_language, DW_FORM_data2,
            DW_AT_name, DW_FORM_strp,
            DW_AT_comp_dir, DW_FORM_strp,
            DW_AT_low_pc, DW_FORM_addr,
            DW_AT_ranges, DW_FORM_sec_offset,
            DW_AT_stmt_list, DW_FORM_sec_offset]);

        public static DwarfAbbrev BaseType = new([
            DW_TAG_base_type, DW_CHILDREN_no,
            DW_AT_name, DW_FORM_strp,
            DW_AT_encoding, DW_FORM_data1,
            DW_AT_byte_size, DW_FORM_data1]);

        public static DwarfAbbrev EnumerationType = new([
            DW_TAG_enumeration_type, DW_CHILDREN_yes,
            DW_AT_name, DW_FORM_strp,
            DW_AT_type, DW_FORM_ref4,
            DW_AT_byte_size, DW_FORM_data1]);

        public static DwarfAbbrev EnumerationTypeNoChildren = new([
            DW_TAG_enumeration_type, DW_CHILDREN_no,
            DW_AT_name, DW_FORM_strp,
            DW_AT_type, DW_FORM_ref4,
            DW_AT_byte_size, DW_FORM_data1]);

        public static DwarfAbbrev Enumerator1 = new([
            DW_TAG_enumerator, DW_CHILDREN_no,
            DW_AT_name, DW_FORM_strp,
            DW_AT_const_value, DW_FORM_data1]);

        public static DwarfAbbrev Enumerator2 = new([
            DW_TAG_enumerator, DW_CHILDREN_no,
            DW_AT_name, DW_FORM_strp,
            DW_AT_const_value, DW_FORM_data2]);

        public static DwarfAbbrev Enumerator4 = new([
            DW_TAG_enumerator, DW_CHILDREN_no,
            DW_AT_name, DW_FORM_strp,
            DW_AT_const_value, DW_FORM_data4]);

        public static DwarfAbbrev Enumerator8 = new([
            DW_TAG_enumerator, DW_CHILDREN_no,
            DW_AT_name, DW_FORM_strp,
            DW_AT_const_value, DW_FORM_data8]);

        public static DwarfAbbrev TypeDef = new([
            DW_TAG_typedef, DW_CHILDREN_no,
            DW_AT_name, DW_FORM_strp,
            DW_AT_type, DW_FORM_ref4]);

        public static DwarfAbbrev Subprogram = new([
            DW_TAG_subprogram, DW_CHILDREN_yes,
            DW_AT_specification, DW_FORM_ref4,
            DW_AT_low_pc, DW_FORM_addr,
            DW_AT_high_pc, DW_FORM_size,
            DW_AT_frame_base, DW_FORM_exprloc,
            DW_AT_object_pointer, DW_FORM_ref4]);

        public static DwarfAbbrev SubprogramNoChildren = new([
            DW_TAG_subprogram, DW_CHILDREN_no,
            DW_AT_specification, DW_FORM_ref4,
            DW_AT_low_pc, DW_FORM_addr,
            DW_AT_high_pc, DW_FORM_size,
            DW_AT_frame_base, DW_FORM_exprloc,
            DW_AT_object_pointer, DW_FORM_ref4]);

        public static DwarfAbbrev SubprogramStatic = new([
            DW_TAG_subprogram, DW_CHILDREN_yes,
            DW_AT_specification, DW_FORM_ref4,
            DW_AT_low_pc, DW_FORM_addr,
            DW_AT_high_pc, DW_FORM_size,
            DW_AT_frame_base, DW_FORM_exprloc]);

        public static DwarfAbbrev SubprogramStaticNoChildren = new([
            DW_TAG_subprogram, DW_CHILDREN_no,
            DW_AT_specification, DW_FORM_ref4,
            DW_AT_low_pc, DW_FORM_addr,
            DW_AT_high_pc, DW_FORM_size,
            DW_AT_frame_base, DW_FORM_exprloc]);

        public static DwarfAbbrev SubprogramSpec = new([
            DW_TAG_subprogram, DW_CHILDREN_yes,
            DW_AT_name, DW_FORM_strp,
            DW_AT_linkage_name, DW_FORM_strp,
            DW_AT_decl_file, DW_FORM_data1,
            DW_AT_decl_line, DW_FORM_data1,
            DW_AT_type, DW_FORM_ref4,
            DW_AT_external, DW_FORM_flag_present,
            DW_AT_declaration, DW_FORM_flag_present,
            DW_AT_object_pointer, DW_FORM_ref4]);

        public static DwarfAbbrev SubprogramStaticSpec = new([
            DW_TAG_subprogram, DW_CHILDREN_yes,
            DW_AT_name, DW_FORM_strp,
            DW_AT_linkage_name, DW_FORM_strp,
            DW_AT_decl_file, DW_FORM_data1,
            DW_AT_decl_line, DW_FORM_data1,
            DW_AT_type, DW_FORM_ref4,
            DW_AT_external, DW_FORM_flag_present,
            DW_AT_declaration, DW_FORM_flag_present]);

        public static DwarfAbbrev SubprogramStaticNoChildrenSpec = new([
            DW_TAG_subprogram, DW_CHILDREN_no,
            DW_AT_name, DW_FORM_strp,
            DW_AT_linkage_name, DW_FORM_strp,
            DW_AT_decl_file, DW_FORM_data1,
            DW_AT_decl_line, DW_FORM_data1,
            DW_AT_type, DW_FORM_ref4,
            DW_AT_external, DW_FORM_flag_present,
            DW_AT_declaration, DW_FORM_flag_present]);

        public static DwarfAbbrev Variable = new([
            DW_TAG_variable, DW_CHILDREN_no,
            DW_AT_name, DW_FORM_strp,
            DW_AT_decl_file, DW_FORM_data1,
            DW_AT_decl_line, DW_FORM_data1,
            DW_AT_type, DW_FORM_ref4,
            DW_AT_location, DW_FORM_exprloc]);

        public static DwarfAbbrev VariableLoc = new([
            DW_TAG_variable, DW_CHILDREN_no,
            DW_AT_name, DW_FORM_strp,
            DW_AT_decl_file, DW_FORM_data1,
            DW_AT_decl_line, DW_FORM_data1,
            DW_AT_type, DW_FORM_ref4,
            DW_AT_location, DW_FORM_sec_offset]);

        public static DwarfAbbrev VariableStatic = new([
            DW_TAG_variable, DW_CHILDREN_no,
            DW_AT_specification, DW_FORM_ref4,
            DW_AT_location, DW_FORM_exprloc]);

        public static DwarfAbbrev FormalParameter = new([
            DW_TAG_formal_parameter, DW_CHILDREN_no,
            DW_AT_name, DW_FORM_strp,
            DW_AT_decl_file, DW_FORM_data1,
            DW_AT_decl_line, DW_FORM_data1,
            DW_AT_type, DW_FORM_ref4,
            DW_AT_location, DW_FORM_exprloc]);

        public static DwarfAbbrev FormalParameterThis = new([
            DW_TAG_formal_parameter, DW_CHILDREN_no,
            DW_AT_name, DW_FORM_strp,
            DW_AT_decl_file, DW_FORM_data1,
            DW_AT_decl_line, DW_FORM_data1,
            DW_AT_type, DW_FORM_ref4,
            DW_AT_location, DW_FORM_exprloc,
            DW_AT_artificial, DW_FORM_flag_present]);

        public static DwarfAbbrev FormalParameterLoc = new([
            DW_TAG_formal_parameter, DW_CHILDREN_no,
            DW_AT_name, DW_FORM_strp,
            DW_AT_decl_file, DW_FORM_data1,
            DW_AT_decl_line, DW_FORM_data1,
            DW_AT_type, DW_FORM_ref4,
            DW_AT_location, DW_FORM_sec_offset]);

        public static DwarfAbbrev FormalParameterThisLoc = new([
            DW_TAG_formal_parameter, DW_CHILDREN_no,
            DW_AT_name, DW_FORM_strp,
            DW_AT_decl_file, DW_FORM_data1,
            DW_AT_decl_line, DW_FORM_data1,
            DW_AT_type, DW_FORM_ref4,
            DW_AT_location, DW_FORM_sec_offset,
            DW_AT_artificial, DW_FORM_flag_present]);

        public static DwarfAbbrev FormalParameterSpec = new([
            DW_TAG_formal_parameter, DW_CHILDREN_no,
            DW_AT_type, DW_FORM_ref4]);

        public static DwarfAbbrev FormalParameterThisSpec = new([
            DW_TAG_formal_parameter, DW_CHILDREN_no,
            DW_AT_type, DW_FORM_ref4,
            DW_AT_artificial, DW_FORM_flag_present]);

        public static DwarfAbbrev ClassType = new([
            DW_TAG_class_type, DW_CHILDREN_yes,
            DW_AT_name, DW_FORM_strp,
            DW_AT_byte_size, DW_FORM_data4]);

        public static DwarfAbbrev ClassTypeNoChildren = new([
            DW_TAG_class_type, DW_CHILDREN_no,
            DW_AT_name, DW_FORM_strp,
            DW_AT_byte_size, DW_FORM_data4]);

        public static DwarfAbbrev ClassTypeDecl = new([
            DW_TAG_class_type, DW_CHILDREN_no,
            DW_AT_name, DW_FORM_strp,
            DW_AT_declaration, DW_FORM_flag_present]);

        public static DwarfAbbrev ClassMember = new([
            DW_TAG_member, DW_CHILDREN_no,
            DW_AT_name, DW_FORM_strp,
            DW_AT_type, DW_FORM_ref4,
            DW_AT_data_member_location, DW_FORM_data4]);

        public static DwarfAbbrev ClassMemberStatic = new([
            DW_TAG_member, DW_CHILDREN_no,
            DW_AT_name, DW_FORM_strp,
            DW_AT_type, DW_FORM_ref4,
            DW_AT_external, DW_FORM_flag_present,
            DW_AT_declaration, DW_FORM_flag_present]);

        public static DwarfAbbrev PointerType = new([
            DW_TAG_pointer_type, DW_CHILDREN_no,
            DW_AT_type, DW_FORM_ref4,
            DW_AT_byte_size, DW_FORM_data1]);

        public static DwarfAbbrev ReferenceType = new([
            DW_TAG_reference_type, DW_CHILDREN_no,
            DW_AT_type, DW_FORM_ref4,
            DW_AT_byte_size, DW_FORM_data1]);

        public static DwarfAbbrev ArrayType = new([
            DW_TAG_array_type, DW_CHILDREN_yes,
            DW_AT_type, DW_FORM_ref4]);

        public static DwarfAbbrev SubrangeType = new([
            DW_TAG_subrange_type, DW_CHILDREN_no,
            DW_AT_upper_bound, DW_FORM_udata]);

        public static DwarfAbbrev ClassInheritance = new([
            DW_TAG_inheritance, DW_CHILDREN_no,
            DW_AT_type, DW_FORM_ref4,
            DW_AT_data_member_location, DW_FORM_data1]);

        public static DwarfAbbrev LexicalBlock = new([
            DW_TAG_lexical_block, DW_CHILDREN_yes,
            DW_AT_low_pc, DW_FORM_addr,
            DW_AT_high_pc, DW_FORM_size]);

        public static DwarfAbbrev TryBlock = new([
            DW_TAG_try_block, DW_CHILDREN_no,
            DW_AT_low_pc, DW_FORM_addr,
            DW_AT_high_pc, DW_FORM_size]);

        public static DwarfAbbrev CatchBlock = new([
            DW_TAG_catch_block, DW_CHILDREN_no,
            DW_AT_low_pc, DW_FORM_addr,
            DW_AT_high_pc, DW_FORM_size]);

        public static DwarfAbbrev VoidType = new([
            DW_TAG_unspecified_type, DW_CHILDREN_no,
            DW_AT_name, DW_FORM_strp]);

        public static DwarfAbbrev VoidPointerType = new([
            DW_TAG_pointer_type, DW_CHILDREN_no]);
    }
}
