//===---- dwarfAbbrev.cpp ---------------------------------------*- C++ -*-===//
//
// dwarf abbreviations implementation
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
//===----------------------------------------------------------------------===//

#include "dwarfAbbrev.h"
#include "llvm/MC/MCContext.h"
#include "llvm/MC/MCObjectFileInfo.h"

namespace DwarfAbbrev {

void Dump(MCObjectStreamer *Streamer, uint16_t DwarfVersion, unsigned TargetPointerSize) {
  uint16_t DW_FORM_size;
  switch (TargetPointerSize) {
    case 1:
      DW_FORM_size = dwarf::DW_FORM_data1;
      break;
    case 2:
      DW_FORM_size = dwarf::DW_FORM_data2;
      break;
    case 4:
      DW_FORM_size = dwarf::DW_FORM_data4;
      break;
    case 8:
      DW_FORM_size = dwarf::DW_FORM_data8;
      break;
    default:
      assert(false && "Unexpected TargerPointerSize");
      return;
  }

  const uint16_t AbbrevTable[] = {
    CompileUnit,
        dwarf::DW_TAG_compile_unit, dwarf::DW_CHILDREN_yes,
        dwarf::DW_AT_producer, dwarf::DW_FORM_string,
        dwarf::DW_AT_language, dwarf::DW_FORM_data2,
        dwarf::DW_AT_stmt_list, (DwarfVersion >= 4 ? dwarf::DW_FORM_sec_offset : dwarf::DW_FORM_data4),
        0, 0,

    BaseType,
        dwarf::DW_TAG_base_type, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_name, dwarf::DW_FORM_strp,
        dwarf::DW_AT_encoding, dwarf::DW_FORM_data1,
        dwarf::DW_AT_byte_size, dwarf::DW_FORM_data1,
        0, 0,

    EnumerationType,
        dwarf::DW_TAG_enumeration_type, dwarf::DW_CHILDREN_yes,
        dwarf::DW_AT_name, dwarf::DW_FORM_strp,
        dwarf::DW_AT_type, dwarf::DW_FORM_ref4,
        dwarf::DW_AT_byte_size, dwarf::DW_FORM_data1,
        0, 0,

    Enumerator1,
        dwarf::DW_TAG_enumerator, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_name, dwarf::DW_FORM_strp,
        dwarf::DW_AT_const_value, dwarf::DW_FORM_data1,
        0, 0,

    Enumerator2,
        dwarf::DW_TAG_enumerator, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_name, dwarf::DW_FORM_strp,
        dwarf::DW_AT_const_value, dwarf::DW_FORM_data2,
        0, 0,

    Enumerator4,
        dwarf::DW_TAG_enumerator, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_name, dwarf::DW_FORM_strp,
        dwarf::DW_AT_const_value, dwarf::DW_FORM_data4,
        0, 0,

    Enumerator8,
        dwarf::DW_TAG_enumerator, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_name, dwarf::DW_FORM_strp,
        dwarf::DW_AT_const_value, dwarf::DW_FORM_data8,
        0, 0,

    TypeDef,
        dwarf::DW_TAG_typedef, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_name, dwarf::DW_FORM_strp,
        dwarf::DW_AT_type, dwarf::DW_FORM_ref4,
        0, 0,

    Subprogram,
        dwarf::DW_TAG_subprogram, dwarf::DW_CHILDREN_yes,
        dwarf::DW_AT_specification, dwarf::DW_FORM_ref4,
        dwarf::DW_AT_low_pc, dwarf::DW_FORM_addr,
        dwarf::DW_AT_high_pc, DW_FORM_size,
        dwarf::DW_AT_frame_base, dwarf::DW_FORM_exprloc,
        dwarf::DW_AT_object_pointer, dwarf::DW_FORM_ref4,
        0, 0,

    SubprogramStatic,
        dwarf::DW_TAG_subprogram, dwarf::DW_CHILDREN_yes,
        dwarf::DW_AT_specification, dwarf::DW_FORM_ref4,
        dwarf::DW_AT_low_pc, dwarf::DW_FORM_addr,
        dwarf::DW_AT_high_pc, DW_FORM_size,
        dwarf::DW_AT_frame_base, dwarf::DW_FORM_exprloc,
        0, 0,

    SubprogramSpec,
        dwarf::DW_TAG_subprogram, dwarf::DW_CHILDREN_yes,
        dwarf::DW_AT_name, dwarf::DW_FORM_strp,
        dwarf::DW_AT_linkage_name, dwarf::DW_FORM_strp,
        dwarf::DW_AT_decl_file, dwarf::DW_FORM_data1,
        dwarf::DW_AT_decl_line, dwarf::DW_FORM_data1,
        dwarf::DW_AT_type, dwarf::DW_FORM_ref4,
        dwarf::DW_AT_external, dwarf::DW_FORM_flag_present,
        dwarf::DW_AT_declaration, dwarf::DW_FORM_flag_present,
        dwarf::DW_AT_object_pointer, dwarf::DW_FORM_ref4,
        0, 0,

    SubprogramStaticSpec,
        dwarf::DW_TAG_subprogram, dwarf::DW_CHILDREN_yes,
        dwarf::DW_AT_name, dwarf::DW_FORM_strp,
        dwarf::DW_AT_linkage_name, dwarf::DW_FORM_strp,
        dwarf::DW_AT_decl_file, dwarf::DW_FORM_data1,
        dwarf::DW_AT_decl_line, dwarf::DW_FORM_data1,
        dwarf::DW_AT_type, dwarf::DW_FORM_ref4,
        dwarf::DW_AT_external, dwarf::DW_FORM_flag_present,
        dwarf::DW_AT_declaration, dwarf::DW_FORM_flag_present,
        0, 0,

    Variable,
        dwarf::DW_TAG_variable, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_name, dwarf::DW_FORM_strp,
        dwarf::DW_AT_decl_file, dwarf::DW_FORM_data1,
        dwarf::DW_AT_decl_line, dwarf::DW_FORM_data1,
        dwarf::DW_AT_type, dwarf::DW_FORM_ref4,
        dwarf::DW_AT_location, dwarf::DW_FORM_exprloc,
        0, 0,

    VariableLoc,
        dwarf::DW_TAG_variable, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_name, dwarf::DW_FORM_strp,
        dwarf::DW_AT_decl_file, dwarf::DW_FORM_data1,
        dwarf::DW_AT_decl_line, dwarf::DW_FORM_data1,
        dwarf::DW_AT_type, dwarf::DW_FORM_ref4,
        dwarf::DW_AT_location, dwarf::DW_FORM_sec_offset,
        0, 0,

    VariableStatic,
        dwarf::DW_TAG_variable, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_specification, dwarf::DW_FORM_ref4,
        dwarf::DW_AT_location, dwarf::DW_FORM_exprloc,
        0, 0,

    FormalParameter,
        dwarf::DW_TAG_formal_parameter, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_name, dwarf::DW_FORM_strp,
        dwarf::DW_AT_decl_file, dwarf::DW_FORM_data1,
        dwarf::DW_AT_decl_line, dwarf::DW_FORM_data1,
        dwarf::DW_AT_type, dwarf::DW_FORM_ref4,
        dwarf::DW_AT_location, dwarf::DW_FORM_exprloc,
        0, 0,

    FormalParameterThis,
        dwarf::DW_TAG_formal_parameter, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_name, dwarf::DW_FORM_strp,
        dwarf::DW_AT_decl_file, dwarf::DW_FORM_data1,
        dwarf::DW_AT_decl_line, dwarf::DW_FORM_data1,
        dwarf::DW_AT_type, dwarf::DW_FORM_ref4,
        dwarf::DW_AT_location, dwarf::DW_FORM_exprloc,
        dwarf::DW_AT_artificial, dwarf::DW_FORM_flag_present,
        0, 0,

    FormalParameterLoc,
        dwarf::DW_TAG_formal_parameter, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_name, dwarf::DW_FORM_strp,
        dwarf::DW_AT_decl_file, dwarf::DW_FORM_data1,
        dwarf::DW_AT_decl_line, dwarf::DW_FORM_data1,
        dwarf::DW_AT_type, dwarf::DW_FORM_ref4,
        dwarf::DW_AT_location, dwarf::DW_FORM_sec_offset,
        0, 0,

    FormalParameterThisLoc,
        dwarf::DW_TAG_formal_parameter, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_name, dwarf::DW_FORM_strp,
        dwarf::DW_AT_decl_file, dwarf::DW_FORM_data1,
        dwarf::DW_AT_decl_line, dwarf::DW_FORM_data1,
        dwarf::DW_AT_type, dwarf::DW_FORM_ref4,
        dwarf::DW_AT_location, dwarf::DW_FORM_sec_offset,
        dwarf::DW_AT_artificial, dwarf::DW_FORM_flag_present,
        0, 0,

    FormalParameterSpec,
        dwarf::DW_TAG_formal_parameter, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_type, dwarf::DW_FORM_ref4,
        0, 0,

    FormalParameterThisSpec,
        dwarf::DW_TAG_formal_parameter, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_type, dwarf::DW_FORM_ref4,
        dwarf::DW_AT_artificial, dwarf::DW_FORM_flag_present,
        0, 0,

    ClassType,
        dwarf::DW_TAG_class_type, dwarf::DW_CHILDREN_yes,
        dwarf::DW_AT_name, dwarf::DW_FORM_strp,
        dwarf::DW_AT_byte_size, dwarf::DW_FORM_data4,
        0, 0,

    ClassTypeDecl,
        dwarf::DW_TAG_class_type, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_name, dwarf::DW_FORM_strp,
        dwarf::DW_AT_declaration, dwarf::DW_FORM_flag_present,
        0, 0,

    ClassMember,
        dwarf::DW_TAG_member, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_name, dwarf::DW_FORM_strp,
        dwarf::DW_AT_type, dwarf::DW_FORM_ref4,
        dwarf::DW_AT_data_member_location, dwarf::DW_FORM_data4,
        0, 0,

    ClassMemberStatic,
        dwarf::DW_TAG_member, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_name, dwarf::DW_FORM_strp,
        dwarf::DW_AT_type, dwarf::DW_FORM_ref4,
        dwarf::DW_AT_external, dwarf::DW_FORM_flag_present,
        dwarf::DW_AT_declaration, dwarf::DW_FORM_flag_present,
        0, 0,

    PointerType,
        dwarf::DW_TAG_pointer_type, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_type, dwarf::DW_FORM_ref4,
        dwarf::DW_AT_byte_size, dwarf::DW_FORM_data1,
        0, 0,

    ReferenceType,
        dwarf::DW_TAG_reference_type, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_type, dwarf::DW_FORM_ref4,
        dwarf::DW_AT_byte_size, dwarf::DW_FORM_data1,
        0, 0,

    ArrayType,
        dwarf::DW_TAG_array_type, dwarf::DW_CHILDREN_yes,
        dwarf::DW_AT_type, dwarf::DW_FORM_ref4,
        0, 0,

    SubrangeType,
        dwarf::DW_TAG_subrange_type, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_upper_bound, dwarf::DW_FORM_udata,
        0, 0,

    ClassInheritance,
        dwarf::DW_TAG_inheritance, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_type, dwarf::DW_FORM_ref4,
        dwarf::DW_AT_data_member_location, dwarf::DW_FORM_data1,
        0, 0,

    LexicalBlock,
        dwarf::DW_TAG_lexical_block, dwarf::DW_CHILDREN_yes,
        dwarf::DW_AT_low_pc, dwarf::DW_FORM_addr,
        dwarf::DW_AT_high_pc, DW_FORM_size,
        0, 0,

    TryBlock,
        dwarf::DW_TAG_try_block, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_low_pc, dwarf::DW_FORM_addr,
        dwarf::DW_AT_high_pc, DW_FORM_size,
        0, 0,

    CatchBlock,
        dwarf::DW_TAG_catch_block, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_low_pc, dwarf::DW_FORM_addr,
        dwarf::DW_AT_high_pc, DW_FORM_size,
        0, 0,

    VoidType,
        dwarf::DW_TAG_unspecified_type, dwarf::DW_CHILDREN_no,
        dwarf::DW_AT_name, dwarf::DW_FORM_strp,
        0, 0,

    VoidPointerType,
        dwarf::DW_TAG_pointer_type, dwarf::DW_CHILDREN_no,
        0, 0,
  };

  MCContext &context = Streamer->getContext();
  Streamer->SwitchSection(context.getObjectFileInfo()->getDwarfAbbrevSection());

  for (uint16_t e : AbbrevTable) {
      Streamer->emitULEB128IntValue(e);
  }
}

}
