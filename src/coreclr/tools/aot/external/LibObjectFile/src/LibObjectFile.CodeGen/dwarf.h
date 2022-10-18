/*
  Copyright (C) 2000-2006 Silicon Graphics, Inc.  All Rights Reserved.
  Portions Copyright 2002-2010 Sun Microsystems, Inc. All rights reserved.
  Portions Copyright 2007-2017 David Anderson. All rights reserved.

  This program is free software; you can redistribute it and/or modify it
  under the terms of version 2.1 of the GNU Lesser General Public License
  as published by the Free Software Foundation.

  This program is distributed in the hope that it would be useful, but
  WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

  Further, this software is distributed without any warranty that it is
  free of the rightful claim of any third person regarding infringement
  or the like.  Any license provided herein, whether implied or
  otherwise, applies only to this software file.  Patent licenses, if
  any, provided herein do not apply to combinations of this program with
  other software, or any other product whatsoever.

  You should have received a copy of the GNU Lesser General Public
  License along with this program; if not, write the Free Software
  Foundation, Inc., 51 Franklin Street - Fifth Floor, Boston MA 02110-1301,
  USA.

*/


#ifndef __DWARF_H
#define __DWARF_H
#ifdef __cplusplus
extern "C" {
#endif

/*
    dwarf.h   DWARF  debugging information values
    $Revision: 1.41 $    $Date: 2006/04/17 00:09:56 $

    The comment "DWARF3" appears where there are
    new entries from DWARF3 as of 2004, "DWARF3f"
    where there are new entries as of the November 2005
    public review document and other comments apply
    where extension entries appear.

    Extensions part of DWARF4 are marked DWARF4.

    A few extension names have omitted the 'vendor id'
    (See chapter 7, "Vendor Extensibility"). Please
    always use a 'vendor id' string in extension names.

    Vendors should use a vendor string in names and
    whereever possible avoid duplicating values used by
    other vendor extensions

    The DWARF1 comments indicate values unused in
    DWARF2 and later but used or reserved in DWARF1.
*/


#define DW_TAG_array_type               0x01
#define DW_TAG_class_type               0x02
#define DW_TAG_entry_point              0x03
#define DW_TAG_enumeration_type         0x04
#define DW_TAG_formal_parameter         0x05
/*  TAG_global_subroutine               0x06 DWARF1 only */
/*  TAG_global_variable                 0x07 DWARF1 only */
#define DW_TAG_imported_declaration     0x08
/*  reserved by DWARF1                  0x09 DWARF1 only */
#define DW_TAG_label                    0x0a
#define DW_TAG_lexical_block            0x0b
/*  TAG_local_variable                  0x0c DWARF1 only. */
#define DW_TAG_member                   0x0d
/*  reserved by DWARF1                  0x0e DWARF1 only */
#define DW_TAG_pointer_type             0x0f
#define DW_TAG_reference_type           0x10
#define DW_TAG_compile_unit             0x11
#define DW_TAG_string_type              0x12
#define DW_TAG_structure_type           0x13
/* TAG_subroutine                       0x14 DWARF1 only */
#define DW_TAG_subroutine_type          0x15
#define DW_TAG_typedef                  0x16
#define DW_TAG_union_type               0x17
#define DW_TAG_unspecified_parameters   0x18
#define DW_TAG_variant                  0x19
#define DW_TAG_common_block             0x1a
#define DW_TAG_common_inclusion         0x1b
#define DW_TAG_inheritance              0x1c
#define DW_TAG_inlined_subroutine       0x1d
#define DW_TAG_module                   0x1e
#define DW_TAG_ptr_to_member_type       0x1f
#define DW_TAG_set_type                 0x20
#define DW_TAG_subrange_type            0x21
#define DW_TAG_with_stmt                0x22
#define DW_TAG_access_declaration       0x23
#define DW_TAG_base_type                0x24
#define DW_TAG_catch_block              0x25
#define DW_TAG_const_type               0x26
#define DW_TAG_constant                 0x27
#define DW_TAG_enumerator               0x28
#define DW_TAG_file_type                0x29
#define DW_TAG_friend                   0x2a
#define DW_TAG_namelist                 0x2b
        /*  Early releases of this header had the following
            misspelled with a trailing 's' */
#define DW_TAG_namelist_item            0x2c /* DWARF3/2 spelling */
#define DW_TAG_namelist_items           0x2c /* SGI misspelling/typo */
#define DW_TAG_packed_type              0x2d
#define DW_TAG_subprogram               0x2e
        /*  The DWARF2 document had two spellings of the following
            two TAGs, DWARF3 specifies the longer spelling. */
#define DW_TAG_template_type_parameter  0x2f /* DWARF3/2 spelling*/
#define DW_TAG_template_type_param      0x2f /* DWARF2   spelling*/
#define DW_TAG_template_value_parameter 0x30 /* DWARF3/2 spelling*/
#define DW_TAG_template_value_param     0x30 /* DWARF2   spelling*/
#define DW_TAG_thrown_type              0x31
#define DW_TAG_try_block                0x32
#define DW_TAG_variant_part             0x33
#define DW_TAG_variable                 0x34
#define DW_TAG_volatile_type            0x35
#define DW_TAG_dwarf_procedure          0x36  /* DWARF3 */
#define DW_TAG_restrict_type            0x37  /* DWARF3 */
#define DW_TAG_interface_type           0x38  /* DWARF3 */
#define DW_TAG_namespace                0x39  /* DWARF3 */
#define DW_TAG_imported_module          0x3a  /* DWARF3 */
#define DW_TAG_unspecified_type         0x3b  /* DWARF3 */
#define DW_TAG_partial_unit             0x3c  /* DWARF3 */
#define DW_TAG_imported_unit            0x3d  /* DWARF3 */
        /*  Do not use DW_TAG_mutable_type */
#define DW_TAG_mutable_type 0x3e /* Withdrawn from DWARF3 by DWARF3f. */
#define DW_TAG_condition                0x3f  /* DWARF3f */
#define DW_TAG_shared_type              0x40  /* DWARF3f */
#define DW_TAG_type_unit                0x41  /* DWARF4 */
#define DW_TAG_rvalue_reference_type    0x42  /* DWARF4 */
#define DW_TAG_template_alias           0x43  /* DWARF4 */
#define DW_TAG_coarray_type             0x44  /* DWARF5 */
#define DW_TAG_generic_subrange         0x45  /* DWARF5 */
#define DW_TAG_dynamic_type             0x46  /* DWARF5 */
#define DW_TAG_atomic_type              0x47  /* DWARF5 */
#define DW_TAG_call_site                0x48  /* DWARF5 */
#define DW_TAG_call_site_parameter      0x49  /* DWARF5 */
#define DW_TAG_skeleton_unit            0x4a  /* DWARF5 */
#define DW_TAG_immutable_type           0x4b  /* DWARF5 */
#define DW_TAG_lo_user                  0x4080

#define DW_TAG_MIPS_loop                0x4081

/* HP extensions: ftp://ftp.hp.com/pub/lang/tools/WDB/wdb-4.0.tar.gz  */
#define DW_TAG_HP_array_descriptor      0x4090 /* HP */

/* GNU extensions.  The first 3 missing the GNU_. */
#define DW_TAG_format_label             0x4101 /* GNU. Fortran. */
#define DW_TAG_function_template        0x4102 /* GNU. For C++ */
#define DW_TAG_class_template           0x4103 /* GNU. For C++ */
#define DW_TAG_GNU_BINCL                0x4104 /* GNU */
#define DW_TAG_GNU_EINCL                0x4105 /* GNU */

/* GNU extension. http://gcc.gnu.org/wiki/TemplateParmsDwarf */
#define DW_TAG_GNU_template_template_parameter  0x4106 /* GNU */
#define DW_TAG_GNU_template_template_param      0x4106 /* GNU */
#define DW_TAG_GNU_template_parameter_pack      0x4107 /* GNU */
#define DW_TAG_GNU_formal_parameter_pack        0x4108 /* GNU */

#define DW_TAG_GNU_call_site                    0x4109 /* GNU */
#define DW_TAG_GNU_call_site_parameter          0x410a /* GNU */

/* ALTIUM extensions */
    /* DSP-C/Starcore __circ qualifier */
#define DW_TAG_ALTIUM_circ_type         0x5101 /* ALTIUM */
    /* Starcore __mwa_circ qualifier */
#define DW_TAG_ALTIUM_mwa_circ_type     0x5102 /* ALTIUM */
    /* Starcore __rev_carry qualifier */
#define DW_TAG_ALTIUM_rev_carry_type    0x5103 /* ALTIUM */
    /* M16 __rom qualifier */
#define DW_TAG_ALTIUM_rom               0x5111 /* ALTIUM */

/* The following 3 are extensions to support UPC */
#define DW_TAG_upc_shared_type          0x8765 /* UPC */
#define DW_TAG_upc_strict_type          0x8766 /* UPC */
#define DW_TAG_upc_relaxed_type         0x8767 /* UPC */

/* PGI (STMicroelectronics) extensions. */
#define DW_TAG_PGI_kanji_type           0xa000 /* PGI */
#define DW_TAG_PGI_interface_block      0xa020 /* PGI */
/* The following are SUN extensions */
#define DW_TAG_SUN_function_template    0x4201 /* SUN */
#define DW_TAG_SUN_class_template       0x4202 /* SUN */
#define DW_TAG_SUN_struct_template      0x4203 /* SUN */
#define DW_TAG_SUN_union_template       0x4204 /* SUN */
#define DW_TAG_SUN_indirect_inheritance 0x4205 /* SUN */
#define DW_TAG_SUN_codeflags            0x4206 /* SUN */
#define DW_TAG_SUN_memop_info           0x4207 /* SUN */
#define DW_TAG_SUN_omp_child_func       0x4208 /* SUN */
#define DW_TAG_SUN_rtti_descriptor      0x4209 /* SUN */
#define DW_TAG_SUN_dtor_info            0x420a /* SUN */
#define DW_TAG_SUN_dtor                 0x420b /* SUN */
#define DW_TAG_SUN_f90_interface        0x420c /* SUN */
#define DW_TAG_SUN_fortran_vax_structure 0x420d /* SUN */
#define DW_TAG_SUN_hi                   0x42ff /* SUN */

#define DW_TAG_hi_user                  0xffff

/*  The following two are non-standard. Use DW_CHILDREN_yes
    and DW_CHILDREN_no instead.  These could
    probably be deleted, but someone might be using them,
    so they remain.  */
#define DW_children_no                  0
#define DW_children_yes                 1

#define DW_FORM_addr                    0x01
/* FORM_REF                             0x02 DWARF1 only */
#define DW_FORM_block2                  0x03
#define DW_FORM_block4                  0x04
#define DW_FORM_data2                   0x05
#define DW_FORM_data4                   0x06
#define DW_FORM_data8                   0x07
#define DW_FORM_string                  0x08
#define DW_FORM_block                   0x09
#define DW_FORM_block1                  0x0a
#define DW_FORM_data1                   0x0b
#define DW_FORM_flag                    0x0c
#define DW_FORM_sdata                   0x0d
#define DW_FORM_strp                    0x0e
#define DW_FORM_udata                   0x0f
#define DW_FORM_ref_addr                0x10
#define DW_FORM_ref1                    0x11
#define DW_FORM_ref2                    0x12
#define DW_FORM_ref4                    0x13
#define DW_FORM_ref8                    0x14
#define DW_FORM_ref_udata               0x15
#define DW_FORM_indirect                0x16
#define DW_FORM_sec_offset              0x17 /* DWARF4 */
#define DW_FORM_exprloc                 0x18 /* DWARF4 */
#define DW_FORM_flag_present            0x19 /* DWARF4 */
#define DW_FORM_strx                    0x1a /* DWARF5 */
#define DW_FORM_addrx                   0x1b /* DWARF5 */
#define DW_FORM_ref_sup4                0x1c /* DWARF5 */
#define DW_FORM_strp_sup                0x1d /* DWARF5 */
#define DW_FORM_data16                  0x1e /* DWARF5 */
#define DW_FORM_line_strp               0x1f /* DWARF5 */
#define DW_FORM_ref_sig8                0x20 /* DWARF4 */
#define DW_FORM_implicit_const          0x21 /* DWARF5 */
#define DW_FORM_loclistx                0x22 /* DWARF5 */
#define DW_FORM_rnglistx                0x23 /* DWARF5 */
#define DW_FORM_ref_sup8                0x24 /* DWARF5 */
#define DW_FORM_strx1                   0x25 /* DWARF5 */
#define DW_FORM_strx2                   0x26 /* DWARF5 */
#define DW_FORM_strx3                   0x27 /* DWARF5 */
#define DW_FORM_strx4                   0x28 /* DWARF5 */
#define DW_FORM_addrx1                  0x29 /* DWARF5 */
#define DW_FORM_addrx2                  0x2a /* DWARF5 */
#define DW_FORM_addrx3                  0x2b /* DWARF5 */
#define DW_FORM_addrx4                  0x2c /* DWARF5 */

#define DW_FORM_GNU_addr_index          0x1f01 /* GNU extension in debug_info.dwo.*/
#define DW_FORM_GNU_str_index           0x1f02 /* GNU extension, somewhat like DW_FORM_strp */
#define DW_FORM_GNU_ref_alt             0x1f20 /* GNU extension. Offset in .debug_info. */
#define DW_FORM_GNU_strp_alt            0x1f21 /* GNU extension. Offset in .debug_str of another object file. */

#define DW_AT_sibling                           0x01
#define DW_AT_location                          0x02
#define DW_AT_name                              0x03
/* reserved DWARF1                              0x04, DWARF1 only */
/* AT_fund_type                                 0x05, DWARF1 only */
/* AT_mod_fund_type                             0x06, DWARF1 only */
/* AT_user_def_type                             0x07, DWARF1 only */
/* AT_mod_u_d_type                              0x08, DWARF1 only */
#define DW_AT_ordering                          0x09
#define DW_AT_subscr_data                       0x0a
#define DW_AT_byte_size                         0x0b
#define DW_AT_bit_offset                        0x0c
#define DW_AT_bit_size                          0x0d
/* reserved DWARF1                              0x0d, DWARF1 only */
#define DW_AT_element_list                      0x0f
#define DW_AT_stmt_list                         0x10
#define DW_AT_low_pc                            0x11
#define DW_AT_high_pc                           0x12
#define DW_AT_language                          0x13
#define DW_AT_member                            0x14
#define DW_AT_discr                             0x15
#define DW_AT_discr_value                       0x16
#define DW_AT_visibility                        0x17
#define DW_AT_import                            0x18
#define DW_AT_string_length                     0x19
#define DW_AT_common_reference                  0x1a
#define DW_AT_comp_dir                          0x1b
#define DW_AT_const_value                       0x1c
#define DW_AT_containing_type                   0x1d
#define DW_AT_default_value                     0x1e
/*  reserved                                    0x1f */
#define DW_AT_inline                            0x20
#define DW_AT_is_optional                       0x21
#define DW_AT_lower_bound                       0x22
/*  reserved                                    0x23 */
/*  reserved                                    0x24 */
#define DW_AT_producer                          0x25
/*  reserved                                    0x26 */
#define DW_AT_prototyped                        0x27
/*  reserved                                    0x28 */
/*  reserved                                    0x29 */
#define DW_AT_return_addr                       0x2a
/*  reserved                                    0x2b */
#define DW_AT_start_scope                       0x2c
/*  reserved                                    0x2d */
#define DW_AT_bit_stride                        0x2e /* DWARF3 name */
#define DW_AT_stride_size                       0x2e /* DWARF2 name */
#define DW_AT_upper_bound                       0x2f
/* AT_virtual                                   0x30, DWARF1 only */
#define DW_AT_abstract_origin                   0x31
#define DW_AT_accessibility                     0x32
#define DW_AT_address_class                     0x33
#define DW_AT_artificial                        0x34
#define DW_AT_base_types                        0x35
#define DW_AT_calling_convention                0x36
#define DW_AT_count                             0x37
#define DW_AT_data_member_location              0x38
#define DW_AT_decl_column                       0x39
#define DW_AT_decl_file                         0x3a
#define DW_AT_decl_line                         0x3b
#define DW_AT_declaration                       0x3c
#define DW_AT_discr_list                        0x3d /* DWARF2 */
#define DW_AT_encoding                          0x3e
#define DW_AT_external                          0x3f
#define DW_AT_frame_base                        0x40
#define DW_AT_friend                            0x41
#define DW_AT_identifier_case                   0x42
#define DW_AT_macro_info                        0x43 /* DWARF{234} not DWARF5 */
#define DW_AT_namelist_item                     0x44
#define DW_AT_priority                          0x45
#define DW_AT_segment                           0x46
#define DW_AT_specification                     0x47
#define DW_AT_static_link                       0x48
#define DW_AT_type                              0x49
#define DW_AT_use_location                      0x4a
#define DW_AT_variable_parameter                0x4b
#define DW_AT_virtuality                        0x4c
#define DW_AT_vtable_elem_location              0x4d
#define DW_AT_allocated                         0x4e /* DWARF3 */
#define DW_AT_associated                        0x4f /* DWARF3 */
#define DW_AT_data_location                     0x50 /* DWARF3 */
#define DW_AT_byte_stride                       0x51 /* DWARF3f */
#define DW_AT_stride                            0x51 /* DWARF3 (do not use) */
#define DW_AT_entry_pc                          0x52 /* DWARF3 */
#define DW_AT_use_UTF8                          0x53 /* DWARF3 */
#define DW_AT_extension                         0x54 /* DWARF3 */
#define DW_AT_ranges                            0x55 /* DWARF3 */
#define DW_AT_trampoline                        0x56 /* DWARF3 */
#define DW_AT_call_column                       0x57 /* DWARF3 */
#define DW_AT_call_file                         0x58 /* DWARF3 */
#define DW_AT_call_line                         0x59 /* DWARF3 */
#define DW_AT_description                       0x5a /* DWARF3 */
#define DW_AT_binary_scale                      0x5b /* DWARF3f */
#define DW_AT_decimal_scale                     0x5c /* DWARF3f */
#define DW_AT_small                             0x5d /* DWARF3f */
#define DW_AT_decimal_sign                      0x5e /* DWARF3f */
#define DW_AT_digit_count                       0x5f /* DWARF3f */
#define DW_AT_picture_string                    0x60 /* DWARF3f */
#define DW_AT_mutable                           0x61 /* DWARF3f */
#define DW_AT_threads_scaled                    0x62 /* DWARF3f */
#define DW_AT_explicit                          0x63 /* DWARF3f */
#define DW_AT_object_pointer                    0x64 /* DWARF3f */
#define DW_AT_endianity                         0x65 /* DWARF3f */
#define DW_AT_elemental                         0x66 /* DWARF3f */
#define DW_AT_pure                              0x67 /* DWARF3f */
#define DW_AT_recursive                         0x68 /* DWARF3f */
#define DW_AT_signature                         0x69 /* DWARF4 */
#define DW_AT_main_subprogram                   0x6a /* DWARF4 */
#define DW_AT_data_bit_offset                   0x6b /* DWARF4 */
#define DW_AT_const_expr                        0x6c /* DWARF4 */
#define DW_AT_enum_class                        0x6d /* DWARF4 */
#define DW_AT_linkage_name                      0x6e /* DWARF4 */
#define DW_AT_string_length_bit_size            0x6f /* DWARF5 */
#define DW_AT_string_length_byte_size           0x70 /* DWARF5 */
#define DW_AT_rank                              0x71 /* DWARF5 */
#define DW_AT_str_offsets_base                  0x72 /* DWARF5 */
#define DW_AT_addr_base                         0x73 /* DWARF5 */
  /* Use DW_AT_rnglists_base, DW_AT_ranges_base is obsolete as */
  /* it was only used in some DWARF5 drafts, not the final DWARF5. */
#define DW_AT_rnglists_base                     0x74 /* DWARF5 */
  /*  DW_AT_dwo_id, an experiment in some DWARF4+. Not DWARF5! */
#define DW_AT_dwo_id                            0x75 /* DWARF4!*/
#define DW_AT_dwo_name                          0x76 /* DWARF5 */
#define DW_AT_reference                         0x77 /* DWARF5 */
#define DW_AT_rvalue_reference                  0x78 /* DWARF5 */
#define DW_AT_macros                            0x79 /* DWARF5 */
#define DW_AT_call_all_calls                    0x7a /* DWARF5 */
#define DW_AT_call_all_source_calls             0x7b /* DWARF5 */
#define DW_AT_call_all_tail_calls               0x7c /* DWARF5 */
#define DW_AT_call_return_pc                    0x7d /* DWARF5 */
#define DW_AT_call_value                        0x7e /* DWARF5 */
#define DW_AT_call_origin                       0x7f /* DWARF5 */
#define DW_AT_call_parameter                    0x80 /* DWARF5 */
#define DW_AT_call_pc                           0x81 /* DWARF5 */
#define DW_AT_call_tail_call                    0x82 /* DWARF5 */
#define DW_AT_call_target                       0x83 /* DWARF5 */
#define DW_AT_call_target_clobbered             0x84 /* DWARF5 */
#define DW_AT_call_data_location                0x85 /* DWARF5 */
#define DW_AT_call_data_value                   0x86 /* DWARF5 */
#define DW_AT_noreturn                          0x87 /* DWARF5 */
#define DW_AT_alignment                         0x88 /* DWARF5 */
#define DW_AT_export_symbols                    0x89 /* DWARF5 */
#define DW_AT_deleted                           0x8a /* DWARF5 */
#define DW_AT_defaulted                         0x8b /* DWARF5 */
#define DW_AT_loclists_base                     0x8c /* DWARF5 */

/* In extensions, we attempt to include the vendor extension
   in the name even when the vendor leaves it out. */

/* HP extensions. */
#define DW_AT_HP_block_index                    0x2000  /* HP */

/* Follows extension so dwarfdump prints the most-likely-useful name. */
#define DW_AT_lo_user                           0x2000

#define DW_AT_MIPS_fde                          0x2001 /* MIPS/SGI */
#define DW_AT_MIPS_loop_begin                   0x2002 /* MIPS/SGI */
#define DW_AT_MIPS_tail_loop_begin              0x2003 /* MIPS/SGI */
#define DW_AT_MIPS_epilog_begin                 0x2004 /* MIPS/SGI */
#define DW_AT_MIPS_loop_unroll_factor           0x2005 /* MIPS/SGI */
#define DW_AT_MIPS_software_pipeline_depth      0x2006 /* MIPS/SGI */
#define DW_AT_MIPS_linkage_name                 0x2007 /* MIPS/SGI, GNU, and others.*/
#define DW_AT_MIPS_stride                       0x2008 /* MIPS/SGI */
#define DW_AT_MIPS_abstract_name                0x2009 /* MIPS/SGI */
#define DW_AT_MIPS_clone_origin                 0x200a /* MIPS/SGI */
#define DW_AT_MIPS_has_inlines                  0x200b /* MIPS/SGI */
#define DW_AT_MIPS_stride_byte                  0x200c /* MIPS/SGI */
#define DW_AT_MIPS_stride_elem                  0x200d /* MIPS/SGI */
#define DW_AT_MIPS_ptr_dopetype                 0x200e /* MIPS/SGI */
#define DW_AT_MIPS_allocatable_dopetype         0x200f /* MIPS/SGI */
#define DW_AT_MIPS_assumed_shape_dopetype       0x2010 /* MIPS/SGI */
#define DW_AT_MIPS_assumed_size                 0x2011 /* MIPS/SGI */

/* HP extensions. */
#define DW_AT_HP_unmodifiable                   0x2001 /* conflict: MIPS */
#define DW_AT_HP_actuals_stmt_list              0x2010 /* conflict: MIPS */
#define DW_AT_HP_proc_per_section               0x2011 /* conflict: MIPS */
#define DW_AT_HP_raw_data_ptr                   0x2012 /* HP */
#define DW_AT_HP_pass_by_reference              0x2013 /* HP */
#define DW_AT_HP_opt_level                      0x2014 /* HP */
#define DW_AT_HP_prof_version_id                0x2015 /* HP */
#define DW_AT_HP_opt_flags                      0x2016 /* HP */
#define DW_AT_HP_cold_region_low_pc             0x2017 /* HP */
#define DW_AT_HP_cold_region_high_pc            0x2018 /* HP */
#define DW_AT_HP_all_variables_modifiable       0x2019 /* HP */
#define DW_AT_HP_linkage_name                   0x201a /* HP */
#define DW_AT_HP_prof_flags                     0x201b /* HP */

#define DW_AT_CPQ_discontig_ranges              0x2001 /* COMPAQ/HP */
#define DW_AT_CPQ_semantic_events               0x2002 /* COMPAQ/HP */
#define DW_AT_CPQ_split_lifetimes_var           0x2003 /* COMPAQ/HP */
#define DW_AT_CPQ_split_lifetimes_rtn           0x2004 /* COMPAQ/HP */
#define DW_AT_CPQ_prologue_length               0x2005 /* COMPAQ/HP */

#define DW_AT_INTEL_other_endian                0x2026 /* Intel, 1 if byte swapped. */

/* GNU extensions. */
#define DW_AT_sf_names                          0x2101 /* GNU */
#define DW_AT_src_info                          0x2102 /* GNU */
#define DW_AT_mac_info                          0x2103 /* GNU */
#define DW_AT_src_coords                        0x2104 /* GNU */
#define DW_AT_body_begin                        0x2105 /* GNU */
#define DW_AT_body_end                          0x2106 /* GNU */
#define DW_AT_GNU_vector                        0x2107 /* GNU */

/*  Thread safety, see http://gcc.gnu.org/wiki/ThreadSafetyAnnotation .  */
/*  The values here are from gcc-4.6.2 include/dwarf2.h.  The
    values are not given on the web page at all, nor on web pages
    it refers to. */
#define DW_AT_GNU_guarded_by                    0x2108 /* GNU */
#define DW_AT_GNU_pt_guarded_by                 0x2109 /* GNU */
#define DW_AT_GNU_guarded                       0x210a /* GNU */
#define DW_AT_GNU_pt_guarded                    0x210b /* GNU */
#define DW_AT_GNU_locks_excluded                0x210c /* GNU */
#define DW_AT_GNU_exclusive_locks_required      0x210d /* GNU */
#define DW_AT_GNU_shared_locks_required         0x210e /* GNU */

/* See http://gcc.gnu.org/wiki/DwarfSeparateTypeInfo */
#define DW_AT_GNU_odr_signature                 0x210f /* GNU */

/*  See  See http://gcc.gnu.org/wiki/TemplateParmsDwarf */
/*  The value here is from gcc-4.6.2 include/dwarf2.h.  The value is
    not consistent with the web page as of December 2011. */
#define DW_AT_GNU_template_name                 0x2110 /* GNU */
/*  The GNU call site extension.
    See http://www.dwarfstd.org/ShowIssue.php?issue=100909.2&type=open .  */
#define DW_AT_GNU_call_site_value               0x2111 /* GNU */
#define DW_AT_GNU_call_site_data_value          0x2112 /* GNU */
#define DW_AT_GNU_call_site_target              0x2113 /* GNU */
#define DW_AT_GNU_call_site_target_clobbered    0x2114 /* GNU */
#define DW_AT_GNU_tail_call                     0x2115 /* GNU */
#define DW_AT_GNU_all_tail_call_sites           0x2116 /* GNU */
#define DW_AT_GNU_all_call_sites                0x2117 /* GNU */
#define DW_AT_GNU_all_source_call_sites         0x2118 /* GNU */
/*  Section offset to .debug_macro section. */
#define DW_AT_GNU_macros                        0x2119 /* GNU */
/* The GNU DebugFission project: http://gcc.gnu.org/wiki/DebugFission */
#define DW_AT_GNU_dwo_name                      0x2130 /* GNU */
#define DW_AT_GNU_dwo_id                        0x2131 /* GNU */

#define DW_AT_GNU_ranges_base                   0x2132 /* GNU */
#define DW_AT_GNU_addr_base                     0x2133 /* GNU */
#define DW_AT_GNU_pubnames                      0x2134 /* GNU */
#define DW_AT_GNU_pubtypes                      0x2135 /* GNU */

/* To distinguish distinct basic blocks in a single source line. */
#define DW_AT_GNU_discriminator                 0x2136 /* GNU */
/*  Ada GNAT gcc attributes. constant integer forms. */
#define DW_AT_GNU_numerator                     0x2303 /* GNU */
#define DW_AT_GNU_denominator                   0x2304 /* GNU */
#define DW_AT_GNU_bias                          0x2305 /* GNU */



/* ALTIUM extension: ALTIUM Compliant location lists (flag) */
#define DW_AT_ALTIUM_loclist    0x2300          /* ALTIUM  */

/* Sun extensions */
#define DW_AT_SUN_template                      0x2201 /* SUN */
#define DW_AT_VMS_rtnbeg_pd_address             0x2201 /* VMS */
#define DW_AT_SUN_alignment                     0x2202 /* SUN */
#define DW_AT_SUN_vtable                        0x2203 /* SUN */
#define DW_AT_SUN_count_guarantee               0x2204 /* SUN */
#define DW_AT_SUN_command_line                  0x2205 /* SUN */
#define DW_AT_SUN_vbase                         0x2206 /* SUN */
#define DW_AT_SUN_compile_options               0x2207 /* SUN */
#define DW_AT_SUN_language                      0x2208 /* SUN */
#define DW_AT_SUN_browser_file                  0x2209 /* SUN */
#define DW_AT_SUN_vtable_abi                    0x2210 /* SUN */
#define DW_AT_SUN_func_offsets                  0x2211 /* SUN */
#define DW_AT_SUN_cf_kind                       0x2212 /* SUN */
#define DW_AT_SUN_vtable_index                  0x2213 /* SUN */
#define DW_AT_SUN_omp_tpriv_addr                0x2214 /* SUN */
#define DW_AT_SUN_omp_child_func                0x2215 /* SUN */
#define DW_AT_SUN_func_offset                   0x2216 /* SUN */
#define DW_AT_SUN_memop_type_ref                0x2217 /* SUN */
#define DW_AT_SUN_profile_id                    0x2218 /* SUN */
#define DW_AT_SUN_memop_signature               0x2219 /* SUN */
#define DW_AT_SUN_obj_dir                       0x2220 /* SUN */
#define DW_AT_SUN_obj_file                      0x2221 /* SUN */
#define DW_AT_SUN_original_name                 0x2222 /* SUN */
#define DW_AT_SUN_hwcprof_signature             0x2223 /* SUN */
#define DW_AT_SUN_amd64_parmdump                0x2224 /* SUN */
#define DW_AT_SUN_part_link_name                0x2225 /* SUN */
#define DW_AT_SUN_link_name                     0x2226 /* SUN */
#define DW_AT_SUN_pass_with_const               0x2227 /* SUN */
#define DW_AT_SUN_return_with_const             0x2228 /* SUN */
#define DW_AT_SUN_import_by_name                0x2229 /* SUN */
#define DW_AT_SUN_f90_pointer                   0x222a /* SUN */
#define DW_AT_SUN_pass_by_ref                   0x222b /* SUN */
#define DW_AT_SUN_f90_allocatable               0x222c /* SUN */
#define DW_AT_SUN_f90_assumed_shape_array       0x222d /* SUN */
#define DW_AT_SUN_c_vla                         0x222e /* SUN */
#define DW_AT_SUN_return_value_ptr              0x2230 /* SUN */
#define DW_AT_SUN_dtor_start                    0x2231 /* SUN */
#define DW_AT_SUN_dtor_length                   0x2232 /* SUN */
#define DW_AT_SUN_dtor_state_initial            0x2233 /* SUN */
#define DW_AT_SUN_dtor_state_final              0x2234 /* SUN */
#define DW_AT_SUN_dtor_state_deltas             0x2235 /* SUN */
#define DW_AT_SUN_import_by_lname               0x2236 /* SUN */
#define DW_AT_SUN_f90_use_only                  0x2237 /* SUN */
#define DW_AT_SUN_namelist_spec                 0x2238 /* SUN */
#define DW_AT_SUN_is_omp_child_func             0x2239 /* SUN */
#define DW_AT_SUN_fortran_main_alias            0x223a /* SUN */
#define DW_AT_SUN_fortran_based                 0x223b /* SUN */

/*   See http://gcc.gnu.org/wiki/DW_AT_GNAT_descriptive_type .  */
#define DW_AT_use_GNAT_descriptive_type         0x2301 /* GNAT */
#define DW_AT_GNAT_descriptive_type             0x2302 /* GNAT */

/* UPC extension */
#define DW_AT_upc_threads_scaled                0x3210 /* UPC */

/* PGI (STMicroelectronics) extensions. */
#define DW_AT_PGI_lbase                         0x3a00 /* PGI. Block, constant, reference. This attribute is an ASTPLAB extension used to describe the array local base.  */
#define DW_AT_PGI_soffset                       0x3a01  /* PGI. Block, constant, reference. ASTPLAB adds this attribute to describe the section offset, or the offset to the first element in the dimension. */
#define DW_AT_PGI_lstride                       0x3a02  /* PGI. Block, constant, reference. ASTPLAB adds this attribute to describe the linear stride or the distance between elements in the dimension. */

/* There are two groups of Apple extensions here, it is
   unclear what exactly is correct.  */
#define DW_AT_APPLE_optimized                   0x3fe1 /* Apple */
#define DW_AT_APPLE_flags                       0x3fe2 /* Apple */
#define DW_AT_APPLE_isa                         0x3fe3 /* Apple */
#define DW_AT_APPLE_block                       0x3fe4 /* Apple */
#define DW_AT_APPLE_major_runtime_vers          0x3fe5 /* Apple */
#define DW_AT_APPLE_runtime_class               0x3fe6 /* Apple */
#define DW_AT_APPLE_omit_frame_ptr              0x3fe7 /* Apple */

/* Apple Extensions for closures  */
#define DW_AT_APPLE_closure                     0x3fe4 /* Apple */

#define DW_AT_hi_user                           0x3fff

/* OP values 0x01,0x02,0x04,0x05,0x07 are DWARF1 only */
#define DW_OP_addr                      0x03
#define DW_OP_deref                     0x06
#define DW_OP_const1u                   0x08
#define DW_OP_const1s                   0x09
#define DW_OP_const2u                   0x0a
#define DW_OP_const2s                   0x0b
#define DW_OP_const4u                   0x0c
#define DW_OP_const4s                   0x0d
#define DW_OP_const8u                   0x0e
#define DW_OP_const8s                   0x0f
#define DW_OP_constu                    0x10
#define DW_OP_consts                    0x11
#define DW_OP_dup                       0x12
#define DW_OP_drop                      0x13
#define DW_OP_over                      0x14
#define DW_OP_pick                      0x15
#define DW_OP_swap                      0x16
#define DW_OP_rot                       0x17
#define DW_OP_xderef                    0x18
#define DW_OP_abs                       0x19
#define DW_OP_and                       0x1a
#define DW_OP_div                       0x1b
#define DW_OP_minus                     0x1c
#define DW_OP_mod                       0x1d
#define DW_OP_mul                       0x1e
#define DW_OP_neg                       0x1f
#define DW_OP_not                       0x20
#define DW_OP_or                        0x21
#define DW_OP_plus                      0x22
#define DW_OP_plus_uconst               0x23
#define DW_OP_shl                       0x24
#define DW_OP_shr                       0x25
#define DW_OP_shra                      0x26
#define DW_OP_xor                       0x27
#define DW_OP_bra                       0x28
#define DW_OP_eq                        0x29
#define DW_OP_ge                        0x2a
#define DW_OP_gt                        0x2b
#define DW_OP_le                        0x2c
#define DW_OP_lt                        0x2d
#define DW_OP_ne                        0x2e
#define DW_OP_skip                      0x2f
#define DW_OP_lit0                      0x30
#define DW_OP_lit1                      0x31
#define DW_OP_lit2                      0x32
#define DW_OP_lit3                      0x33
#define DW_OP_lit4                      0x34
#define DW_OP_lit5                      0x35
#define DW_OP_lit6                      0x36
#define DW_OP_lit7                      0x37
#define DW_OP_lit8                      0x38
#define DW_OP_lit9                      0x39
#define DW_OP_lit10                     0x3a
#define DW_OP_lit11                     0x3b
#define DW_OP_lit12                     0x3c
#define DW_OP_lit13                     0x3d
#define DW_OP_lit14                     0x3e
#define DW_OP_lit15                     0x3f
#define DW_OP_lit16                     0x40
#define DW_OP_lit17                     0x41
#define DW_OP_lit18                     0x42
#define DW_OP_lit19                     0x43
#define DW_OP_lit20                     0x44
#define DW_OP_lit21                     0x45
#define DW_OP_lit22                     0x46
#define DW_OP_lit23                     0x47
#define DW_OP_lit24                     0x48
#define DW_OP_lit25                     0x49
#define DW_OP_lit26                     0x4a
#define DW_OP_lit27                     0x4b
#define DW_OP_lit28                     0x4c
#define DW_OP_lit29                     0x4d
#define DW_OP_lit30                     0x4e
#define DW_OP_lit31                     0x4f
#define DW_OP_reg0                      0x50
#define DW_OP_reg1                      0x51
#define DW_OP_reg2                      0x52
#define DW_OP_reg3                      0x53
#define DW_OP_reg4                      0x54
#define DW_OP_reg5                      0x55
#define DW_OP_reg6                      0x56
#define DW_OP_reg7                      0x57
#define DW_OP_reg8                      0x58
#define DW_OP_reg9                      0x59
#define DW_OP_reg10                     0x5a
#define DW_OP_reg11                     0x5b
#define DW_OP_reg12                     0x5c
#define DW_OP_reg13                     0x5d
#define DW_OP_reg14                     0x5e
#define DW_OP_reg15                     0x5f
#define DW_OP_reg16                     0x60
#define DW_OP_reg17                     0x61
#define DW_OP_reg18                     0x62
#define DW_OP_reg19                     0x63
#define DW_OP_reg20                     0x64
#define DW_OP_reg21                     0x65
#define DW_OP_reg22                     0x66
#define DW_OP_reg23                     0x67
#define DW_OP_reg24                     0x68
#define DW_OP_reg25                     0x69
#define DW_OP_reg26                     0x6a
#define DW_OP_reg27                     0x6b
#define DW_OP_reg28                     0x6c
#define DW_OP_reg29                     0x6d
#define DW_OP_reg30                     0x6e
#define DW_OP_reg31                     0x6f
#define DW_OP_breg0                     0x70
#define DW_OP_breg1                     0x71
#define DW_OP_breg2                     0x72
#define DW_OP_breg3                     0x73
#define DW_OP_breg4                     0x74
#define DW_OP_breg5                     0x75
#define DW_OP_breg6                     0x76
#define DW_OP_breg7                     0x77
#define DW_OP_breg8                     0x78
#define DW_OP_breg9                     0x79
#define DW_OP_breg10                    0x7a
#define DW_OP_breg11                    0x7b
#define DW_OP_breg12                    0x7c
#define DW_OP_breg13                    0x7d
#define DW_OP_breg14                    0x7e
#define DW_OP_breg15                    0x7f
#define DW_OP_breg16                    0x80
#define DW_OP_breg17                    0x81
#define DW_OP_breg18                    0x82
#define DW_OP_breg19                    0x83
#define DW_OP_breg20                    0x84
#define DW_OP_breg21                    0x85
#define DW_OP_breg22                    0x86
#define DW_OP_breg23                    0x87
#define DW_OP_breg24                    0x88
#define DW_OP_breg25                    0x89
#define DW_OP_breg26                    0x8a
#define DW_OP_breg27                    0x8b
#define DW_OP_breg28                    0x8c
#define DW_OP_breg29                    0x8d
#define DW_OP_breg30                    0x8e
#define DW_OP_breg31                    0x8f
#define DW_OP_regx                      0x90
#define DW_OP_fbreg                     0x91
#define DW_OP_bregx                     0x92
#define DW_OP_piece                     0x93
#define DW_OP_deref_size                0x94
#define DW_OP_xderef_size               0x95
#define DW_OP_nop                       0x96
#define DW_OP_push_object_address       0x97 /* DWARF3 */
#define DW_OP_call2                     0x98 /* DWARF3 */
#define DW_OP_call4                     0x99 /* DWARF3 */
#define DW_OP_call_ref                  0x9a /* DWARF3 */
#define DW_OP_form_tls_address          0x9b /* DWARF3f */
#define DW_OP_call_frame_cfa            0x9c /* DWARF3f */
#define DW_OP_bit_piece                 0x9d /* DWARF3f */
#define DW_OP_implicit_value            0x9e /* DWARF4 */
#define DW_OP_stack_value               0x9f /* DWARF4 */
#define DW_OP_implicit_pointer          0xa0 /* DWARF5 */
#define DW_OP_addrx                     0xa1 /* DWARF5 */
#define DW_OP_constx                    0xa2 /* DWARF5 */
#define DW_OP_entry_value               0xa3 /* DWARF5 */
#define DW_OP_const_type                0xa4 /* DWARF5 */
#define DW_OP_regval_type               0xa5 /* DWARF5 */
#define DW_OP_deref_type                0xa6 /* DWARF5 */
#define DW_OP_xderef_type               0xa7 /* DWARF5 */
#define DW_OP_convert                   0xa8 /* DWARF5 */
#define DW_OP_reinterpret               0xa9 /* DWARF5 */


    /* GNU extensions. */
#define DW_OP_GNU_push_tls_address      0xe0 /* GNU */

/* Follows extension so dwarfdump prints the most-likely-useful name. */
#define DW_OP_lo_user                   0xe0


#define DW_OP_GNU_uninit                0xf0 /* GNU */
#define DW_OP_GNU_encoded_addr          0xf1 /* GNU */
#define DW_OP_GNU_implicit_pointer      0xf2 /* GNU */
#define DW_OP_GNU_entry_value           0xf3 /* GNU */
#define DW_OP_GNU_const_type            0xf4 /* GNU */
#define DW_OP_GNU_regval_type           0xf5 /* GNU */
#define DW_OP_GNU_deref_type            0xf6 /* GNU */
#define DW_OP_GNU_convert               0xf7 /* GNU */
#define DW_OP_GNU_reinterpret           0xf9 /* GNU */
#define DW_OP_GNU_parameter_ref         0xfa /* GNU */
#define DW_OP_GNU_addr_index            0xfb /* GNU DebugFission */
#define DW_OP_GNU_const_index           0xfc /* GNU DebugFission */

    /* HP extensions. */
#define DW_OP_HP_unknown                0xe0 /* HP conflict: GNU */
#define DW_OP_HP_is_value               0xe1 /* HP */
#define DW_OP_HP_fltconst4              0xe2 /* HP */
#define DW_OP_HP_fltconst8              0xe3 /* HP */
#define DW_OP_HP_mod_range              0xe4 /* HP */
#define DW_OP_HP_unmod_range            0xe5 /* HP */
#define DW_OP_HP_tls                    0xe6 /* HP */

#define DW_OP_INTEL_bit_piece           0xe8 /* Intel: made obsolete by DW_OP_bit_piece above. */

   /* Apple extension. */
#define DW_OP_APPLE_uninit              0xf0 /* Apple */
#define DW_OP_PGI_omp_thread_num        0xf8 /* PGI (STMicroelectronics) */

#define DW_OP_hi_user                   0xff

#define DW_ATE_address                  0x01
#define DW_ATE_boolean                  0x02
#define DW_ATE_complex_float            0x03
#define DW_ATE_float                    0x04
#define DW_ATE_signed                   0x05
#define DW_ATE_signed_char              0x06
#define DW_ATE_unsigned                 0x07
#define DW_ATE_unsigned_char            0x08
#define DW_ATE_imaginary_float          0x09  /* DWARF3 */
#define DW_ATE_packed_decimal           0x0a  /* DWARF3f */
#define DW_ATE_numeric_string           0x0b  /* DWARF3f */
#define DW_ATE_edited                   0x0c  /* DWARF3f */
#define DW_ATE_signed_fixed             0x0d  /* DWARF3f */
#define DW_ATE_unsigned_fixed           0x0e  /* DWARF3f */
#define DW_ATE_decimal_float            0x0f  /* DWARF3f */
#define DW_ATE_UTF                      0x10  /* DWARF4 */
#define DW_ATE_UCS                      0x11  /* DWARF5 */
#define DW_ATE_ASCII                    0x12  /* DWARF5 */


/* ALTIUM extensions. x80, x81 */
#define DW_ATE_ALTIUM_fract           0x80 /* ALTIUM __fract type */

/* Follows extension so dwarfdump prints the most-likely-useful name. */
#define DW_ATE_lo_user                  0x80

/* Shown here to help dwarfdump build script. */
#define DW_ATE_ALTIUM_accum           0x81 /* ALTIUM __accum type */

/* HP Floating point extensions. */
#define DW_ATE_HP_float80             0x80 /* (80 bit). HP */


#define DW_ATE_HP_complex_float80     0x81 /* Complex (80 bit). HP  */
#define DW_ATE_HP_float128            0x82 /* (128 bit). HP */
#define DW_ATE_HP_complex_float128    0x83 /* Complex (128 bit). HP */
#define DW_ATE_HP_floathpintel        0x84 /* (82 bit IA64). HP */
#define DW_ATE_HP_imaginary_float80   0x85 /* HP */
#define DW_ATE_HP_imaginary_float128  0x86 /* HP */

/* Sun extensions */
#define DW_ATE_SUN_interval_float       0x91
#define DW_ATE_SUN_imaginary_float      0x92 /* Obsolete: See DW_ATE_imaginary_float */

#define DW_ATE_hi_user                  0xff


/*   DWARF5  Defaulted Member Encodings. */
#define DW_DEFAULTED_no                 0x0      /* DWARF5 */
#define DW_DEFAULTED_in_class           0x1      /* DWARF5 */
#define DW_DEFAULTED_out_of_class       0x2      /* DWARF5 */


#define DW_IDX_compile_unit             0x1      /* DWARF5 */
#define DW_IDX_type_unit                0x2      /* DWARF5 */
#define DW_IDX_die_offset               0x3      /* DWARF5 */
#define DW_IDX_parent                   0x4      /* DWARF5 */
#define DW_IDX_type_hash                0x5      /* DWARF5 */
#define DW_IDX_lo_user                  0x2000   /* DWARF5 */
#define DW_IDX_hi_user                  0x0fff   /* DWARF5 */

/* These with not-quite-the-same-names were used in DWARF4
   and never official and should not be used by anyone. */
#define DW_LLEX_end_of_list_entry        0x0      /* DWARF4 experimental */
#define DW_LLEX_base_address_selection_entry 0x01 /* DWARF4 experimental */
#define DW_LLEX_start_end_entry          0x02     /* DWARF4 experimental */
#define DW_LLEX_start_length_entry       0x03     /* DWARF4 experimental */
#define DW_LLEX_offset_pair_entry        0x04     /* DWARF4 experimental */

/* DWARF5 Location List Entries in Split Objects */
#define DW_LLE_end_of_list              0x0      /* DWARF5 */
#define DW_LLE_base_addressx            0x01     /* DWARF5 */
#define DW_LLE_startx_endx              0x02     /* DWARF5 */
#define DW_LLE_startx_length            0x03     /* DWARF5 */
#define DW_LLE_offset_pair              0x04     /* DWARF5 */
#define DW_LLE_default_location         0x05     /* DWARF5 */
#define DW_LLE_base_address             0x06     /* DWARF5 */
#define DW_LLE_start_end                0x07     /* DWARF5 */
#define DW_LLE_start_length             0x08     /* DWARF5 */

/* DWARF5 Range List Entries */
#define DW_RLE_end_of_list              0x00     /* DWARF5 */
#define DW_RLE_base_addressx            0x01     /* DWARF5 */
#define DW_RLE_startx_endx              0x02     /* DWARF5 */
#define DW_RLE_startx_length            0x03     /* DWARF5 */
#define DW_RLE_offset_pair              0x04     /* DWARF5 */
#define DW_RLE_base_address             0x05     /* DWARF5 */
#define DW_RLE_start_end                0x06     /* DWARF5 */
#define DW_RLE_start_length             0x07     /* DWARF5 */

/* DWARF5 Unit header unit type encodings */
#define DW_UT_compile                   0x01  /* DWARF5 */
#define DW_UT_type                      0x02  /* DWARF5 */
#define DW_UT_partial                   0x03  /* DWARF5 */
#define DW_UT_skeleton                  0x04  /* DWARF5 */
#define DW_UT_split_compile             0x05  /* DWARF5 */
#define DW_UT_split_type                0x06  /* DWARF5 */
#define DW_UT_lo_user                   0x80  /* DWARF5 */
#define DW_UT_hi_user                   0xff  /* DWARF5 */


/*  DWARF5 DebugFission object section id values
    for  .dwp object section offsets hash table.
    0 is reserved, not used.
    2 is actually reserved, not used in DWARF5.
    But 2 may be seen in some DWARF4 objects.
*/
#define DW_SECT_INFO        1  /* .debug_info.dwo        DWARF5 */
#define DW_SECT_TYPES       2  /* .debug_types.dwo   pre-DWARF5 */
#define DW_SECT_ABBREV      3  /* .debug_abbrev.dwo      DWARF5 */
#define DW_SECT_LINE        4  /* .debug_line.dwo        DWARF5 */
#define DW_SECT_LOCLISTS    5  /* .debug_loclists.dwo    DWARF5 */
#define DW_SECT_STR_OFFSETS 6  /* .debug_str_offsets.dwo DWARF5 */
#define DW_SECT_MACRO       7  /* .debug_macro.dwo       DWARF5 */
#define DW_SECT_RNGLISTS    8  /* .debug_rnglists.dwo    DWARF5 */


/* Decimal Sign codes. */
#define DW_DS_unsigned                  0x01 /* DWARF3f */
#define DW_DS_leading_overpunch         0x02 /* DWARF3f */
#define DW_DS_trailing_overpunch        0x03 /* DWARF3f */
#define DW_DS_leading_separate          0x04 /* DWARF3f */
#define DW_DS_trailing_separate         0x05 /* DWARF3f */

/* Endian code name. */
#define DW_END_default                  0x00 /* DWARF3f */
#define DW_END_big                      0x01 /* DWARF3f */
#define DW_END_little                   0x02 /* DWARF3f */

#define DW_END_lo_user                  0x40 /* DWARF3f */
#define DW_END_hi_user                  0xff /* DWARF3f */

/*  For use with DW_TAG_SUN_codeflags
    If DW_TAG_SUN_codeflags is accepted as a dwarf standard, then
    standard dwarf ATCF entries start at 0x01 */
#define DW_ATCF_lo_user                 0x40 /* SUN */
#define DW_ATCF_SUN_mop_bitfield        0x41 /* SUN */
#define DW_ATCF_SUN_mop_spill           0x42 /* SUN */
#define DW_ATCF_SUN_mop_scopy           0x43 /* SUN */
#define DW_ATCF_SUN_func_start          0x44 /* SUN */
#define DW_ATCF_SUN_end_ctors           0x45 /* SUN */
#define DW_ATCF_SUN_branch_target       0x46 /* SUN */
#define DW_ATCF_SUN_mop_stack_probe     0x47 /* SUN */
#define DW_ATCF_SUN_func_epilog         0x48 /* SUN */
#define DW_ATCF_hi_user                 0xff /* SUN */

/* Accessibility code name. */
#define DW_ACCESS_public                0x01
#define DW_ACCESS_protected             0x02
#define DW_ACCESS_private               0x03

/* Visibility code name. */
#define DW_VIS_local                    0x01
#define DW_VIS_exported                 0x02
#define DW_VIS_qualified                0x03

/* Virtuality code name. */
#define DW_VIRTUALITY_none              0x00
#define DW_VIRTUALITY_virtual           0x01
#define DW_VIRTUALITY_pure_virtual      0x02

#define DW_LANG_C89                     0x0001
#define DW_LANG_C                       0x0002
#define DW_LANG_Ada83                   0x0003
#define DW_LANG_C_plus_plus             0x0004
#define DW_LANG_Cobol74                 0x0005
#define DW_LANG_Cobol85                 0x0006
#define DW_LANG_Fortran77               0x0007
#define DW_LANG_Fortran90               0x0008
#define DW_LANG_Pascal83                0x0009
#define DW_LANG_Modula2                 0x000a
#define DW_LANG_Java                    0x000b /* DWARF3 */
#define DW_LANG_C99                     0x000c /* DWARF3 */
#define DW_LANG_Ada95                   0x000d /* DWARF3 */
#define DW_LANG_Fortran95               0x000e /* DWARF3 */
#define DW_LANG_PLI                     0x000f /* DWARF3 */
#define DW_LANG_ObjC                    0x0010 /* DWARF3f */
#define DW_LANG_ObjC_plus_plus          0x0011 /* DWARF3f */
#define DW_LANG_UPC                     0x0012 /* DWARF3f */
#define DW_LANG_D                       0x0013 /* DWARF3f */
#define DW_LANG_Python                  0x0014 /* DWARF4 */
/* The following 2 are not yet formally approved October 2010, but
   it seems extremely likely they will be approved as the committee
   chair agrees these should be ok and no one on the committee
   has objected. */
#define DW_LANG_OpenCL                  0x0015 /* DWARF5 */
#define DW_LANG_Go                      0x0016 /* DWARF5 */
#define DW_LANG_Modula3                 0x0017 /* DWARF5 */
#define DW_LANG_Haskel                  0x0018 /* DWARF5 */
#define DW_LANG_C_plus_plus_03          0x0019 /* DWARF5 */
#define DW_LANG_C_plus_plus_11          0x001a /* DWARF5 */
#define DW_LANG_OCaml                   0x001b /* DWARF5 */
#define DW_LANG_Rust                    0x001c /* DWARF5 */
#define DW_LANG_C11                     0x001d /* DWARF5 */
#define DW_LANG_Swift                   0x001e /* DWARF5 */
#define DW_LANG_Julia                   0x001f /* DWARF5 */
#define DW_LANG_Dylan                   0x0020 /* DWARF5 */
#define DW_LANG_C_plus_plus_14          0x0021 /* DWARF5 */
#define DW_LANG_Fortran03               0x0022 /* DWARF5 */
#define DW_LANG_Fortran08               0x0023 /* DWARF5 */
#define DW_LANG_RenderScript            0x0024 /* DWARF5 */
#define DW_LANG_BLISS                   0x0025 /* DWARF5 */
#define DW_LANG_lo_user                 0x8000
#define DW_LANG_Mips_Assembler          0x8001 /* MIPS   */
#define DW_LANG_Upc                     0x8765 /* UPC, use
                                        DW_LANG_UPC instead. */
/* ALTIUM extension */
#define DW_LANG_ALTIUM_Assembler        0x9101  /* ALTIUM */

/* Sun extensions */
#define DW_LANG_SUN_Assembler           0x9001 /* SUN */

#define DW_LANG_hi_user                 0xffff

/* Identifier case name. */
#define DW_ID_case_sensitive            0x00
#define DW_ID_up_case                   0x01
#define DW_ID_down_case                 0x02
#define DW_ID_case_insensitive          0x03

/* Calling Convention Name. */
#define DW_CC_normal                    0x01
#define DW_CC_program                   0x02
#define DW_CC_nocall                    0x03
#define DW_CC_pass_by_reference         0x04 /* DWARF5 */
#define DW_CC_pass_by_value             0x05 /* DWARF5 */
#define DW_CC_lo_user                   0x40

#define DW_CC_GNU_renesas_sh            0x40 /* GNU */
#define DW_CC_GNU_borland_fastcall_i386 0x41 /* GNU */



/* ALTIUM extensions. */
/* Function is an interrupt handler, return address on system stack. */
#define DW_CC_ALTIUM_interrupt          0x65  /* ALTIUM*/

/* Near function model, return address on system stack. */
#define DW_CC_ALTIUM_near_system_stack  0x66  /*ALTIUM */

/* Near function model, return address on user stack. */
#define DW_CC_ALTIUM_near_user_stack    0x67  /* ALTIUM */

/* Huge function model, return address on user stack.  */
#define DW_CC_ALTIUM_huge_user_stack    0x68  /* ALTIUM */


#define DW_CC_hi_user                   0xff

/* Inline Code Name. */
#define DW_INL_not_inlined              0x00
#define DW_INL_inlined                  0x01
#define DW_INL_declared_not_inlined     0x02
#define DW_INL_declared_inlined         0x03

/* Ordering Name. */
#define DW_ORD_row_major                0x00
#define DW_ORD_col_major                0x01

/* Discriminant Descriptor Name. */
#define DW_DSC_label                    0x00
#define DW_DSC_range                    0x01

/*  Line number header entry format encodings. DWARF5 */
#define DW_LNCT_path                    0x1 /* DWARF5 */
#define DW_LNCT_directory_index         0x2 /* DWARF5 */
#define DW_LNCT_timestamp               0x3 /* DWARF5 */
#define DW_LNCT_size                    0x4 /* DWARF5 */
#define DW_LNCT_MD5                     0x5 /* DWARF5 */
/* Experimental two-level line tables. Non standard */
#define DW_LNCT_GNU_subprogram_name     0x6
#define DW_LNCT_GNU_decl_file           0x7
#define DW_LNCT_GNU_decl_line           0x8
#define DW_LNCT_lo_user                 0x2000 /* DWARF5 */
#define DW_LNCT_hi_user                 0x3fff /* DWARF5 */

/* Line number standard opcode name. */
#define DW_LNS_copy                     0x01
#define DW_LNS_advance_pc               0x02
#define DW_LNS_advance_line             0x03
#define DW_LNS_set_file                 0x04
#define DW_LNS_set_column               0x05
#define DW_LNS_negate_stmt              0x06
#define DW_LNS_set_basic_block          0x07
#define DW_LNS_const_add_pc             0x08
#define DW_LNS_fixed_advance_pc         0x09
#define DW_LNS_set_prologue_end         0x0a /* DWARF3 */
#define DW_LNS_set_epilogue_begin       0x0b /* DWARF3 */
#define DW_LNS_set_isa                  0x0c /* DWARF3 */

/*  Experimental two-level line tables. NOT STD DWARF5 */
/*  Not saying GNU or anything. There are no
    DW_LNS_lo_user or DW_LNS_hi_user values though.
    DW_LNS_set_address_from_logical and
    DW_LNS_set_subprogram being both 0xd
    to avoid using up more space in the special opcode table.
    EXPERIMENTAL DW_LNS follow.
*/
#define DW_LNS_set_address_from_logical 0x0d /* Actuals table only */
#define DW_LNS_set_subprogram           0x0d /* Logicals table only */
#define DW_LNS_inlined_call             0x0e /* Logicals table only */
#define DW_LNS_pop_context              0x0f /* Logicals table only */

/* Line number extended opcode name. */
#define DW_LNE_end_sequence             0x01
#define DW_LNE_set_address              0x02
#define DW_LNE_define_file              0x03  /* DWARF4 and earlier only */
#define DW_LNE_set_discriminator        0x04  /* DWARF4 */

/* HP extensions. */
#define DW_LNE_HP_negate_is_UV_update       0x11 /* 17 HP */
#define DW_LNE_HP_push_context              0x12 /* 18 HP */
#define DW_LNE_HP_pop_context               0x13 /* 19 HP */
#define DW_LNE_HP_set_file_line_column      0x14 /* 20 HP */
#define DW_LNE_HP_set_routine_name          0x15 /* 21 HP */
#define DW_LNE_HP_set_sequence              0x16 /* 22 HP */
#define DW_LNE_HP_negate_post_semantics     0x17 /* 23 HP */
#define DW_LNE_HP_negate_function_exit      0x18 /* 24 HP */
#define DW_LNE_HP_negate_front_end_logical  0x19 /* 25 HP */
#define DW_LNE_HP_define_proc               0x20 /* 32 HP */

#define DW_LNE_HP_source_file_correlation   0x80 /* HP */
#define DW_LNE_lo_user                  0x80 /* DWARF3 */
#define DW_LNE_hi_user                  0xff /* DWARF3 */

/* These are known values for DW_LNS_set_isa. */
/* These identifiers are not defined by any DWARFn standard. */
#define DW_ISA_UNKNOWN   0
/* The following two are ARM specific. */
#define DW_ISA_ARM_thumb 1 /* ARM ISA */
#define DW_ISA_ARM_arm   2 /* ARM ISA */


/* Macro information, DWARF5 */
#define DW_MACRO_define                  0x01 /* DWARF5 */
#define DW_MACRO_undef                   0x02 /* DWARF5 */
#define DW_MACRO_start_file              0x03 /* DWARF5 */
#define DW_MACRO_end_file                0x04 /* DWARF5 */
#define DW_MACRO_define_strp             0x05 /* DWARF5 */
#define DW_MACRO_undef_strp              0x06 /* DWARF5 */
#define DW_MACRO_import                  0x07 /* DWARF5 */
#define DW_MACRO_define_sup              0x08 /* DWARF5 */
#define DW_MACRO_undef_sup               0x09 /* DWARF5 */
#define DW_MACRO_import_sup              0x0a /* DWARF5 */
#define DW_MACRO_define_strx             0x0b /* DWARF5 */
#define DW_MACRO_undef_strx              0x0c /* DWARF5 */
#define DW_MACRO_lo_user                 0xe0
#define DW_MACRO_hi_user                 0xff

/* Macro information, DWARF2-DWARF4. */
#define DW_MACINFO_define               0x01
#define DW_MACINFO_undef                0x02
#define DW_MACINFO_start_file           0x03
#define DW_MACINFO_end_file             0x04
#define DW_MACINFO_vendor_ext           0xff

/* CFA operator compaction (a space saving measure, see
   the DWARF standard) means DW_CFA_extended and DW_CFA_nop
   have the same value here.  */
#define DW_CFA_advance_loc        0x40
#define DW_CFA_offset             0x80
#define DW_CFA_restore            0xc0
#define DW_CFA_extended           0

#define DW_CFA_nop              0x00
#define DW_CFA_set_loc          0x01
#define DW_CFA_advance_loc1     0x02
#define DW_CFA_advance_loc2     0x03
#define DW_CFA_advance_loc4     0x04
#define DW_CFA_offset_extended  0x05
#define DW_CFA_restore_extended 0x06
#define DW_CFA_undefined        0x07
#define DW_CFA_same_value       0x08
#define DW_CFA_register         0x09
#define DW_CFA_remember_state   0x0a
#define DW_CFA_restore_state    0x0b
#define DW_CFA_def_cfa          0x0c
#define DW_CFA_def_cfa_register 0x0d
#define DW_CFA_def_cfa_offset   0x0e
#define DW_CFA_def_cfa_expression 0x0f     /* DWARF3 */
#define DW_CFA_expression       0x10       /* DWARF3 */
#define DW_CFA_offset_extended_sf 0x11     /* DWARF3 */
#define DW_CFA_def_cfa_sf       0x12       /* DWARF3 */
#define DW_CFA_def_cfa_offset_sf 0x13      /* DWARF3 */
#define DW_CFA_val_offset        0x14      /* DWARF3f */
#define DW_CFA_val_offset_sf     0x15      /* DWARF3f */
#define DW_CFA_val_expression    0x16      /* DWARF3f */
#define DW_CFA_lo_user           0x1c
#define DW_CFA_low_user          0x1c  /* Incorrect spelling, do not use. */

/* SGI/MIPS extension. */
#define DW_CFA_MIPS_advance_loc8 0x1d   /* MIPS */

/* GNU extensions. */
#define DW_CFA_GNU_window_save   0x2d  /* GNU */
#define DW_CFA_GNU_args_size     0x2e /* GNU  */
#define DW_CFA_GNU_negative_offset_extended  0x2f /* GNU */

/* Metaware if HC is augmentation, apparently meaning High C
   and the op has a single uleb operand.
   See http://sourceforge.net/p/elftoolchain/tickets/397/  */
#define DW_CFA_METAWARE_info     0x34

#define DW_CFA_high_user         0x3f

/* GNU exception header encoding.  See the Generic
   Elf Specification of the Linux Standard Base (LSB).
   http://refspecs.freestandards.org/LSB_3.0.0/LSB-Core-generic/LSB-Core-generic/dwarfext.html
   The upper 4 bits indicate how the value is to be applied.
   The lower 4 bits indicate the format of the data.
   These identifiers are not defined by any DWARFn standard.
*/
#define DW_EH_PE_absptr   0x00  /* GNU */
#define DW_EH_PE_uleb128  0x01  /* GNU */
#define DW_EH_PE_udata2   0x02  /* GNU */
#define DW_EH_PE_udata4   0x03  /* GNU */
#define DW_EH_PE_udata8   0x04  /* GNU */
#define DW_EH_PE_sleb128  0x09  /* GNU */
#define DW_EH_PE_sdata2   0x0A  /* GNU */
#define DW_EH_PE_sdata4   0x0B  /* GNU */
#define DW_EH_PE_sdata8   0x0C  /* GNU */

#define DW_EH_PE_pcrel    0x10  /* GNU */
#define DW_EH_PE_textrel  0x20  /* GNU */
#define DW_EH_PE_datarel  0x30  /* GNU */
#define DW_EH_PE_funcrel  0x40  /* GNU */
#define DW_EH_PE_aligned  0x50  /* GNU */

#define DW_EH_PE_omit     0xff  /* GNU.  Means no value present. */


/* Mapping from machine registers and pseudo-regs into the
   .debug_frame table.  DW_FRAME entries are machine specific.
   These describe MIPS/SGI R3000, R4K, R4400 and all later
   MIPS/SGI IRIX machines.  They describe a mapping from
   hardware register number to the number used in the table
   to identify that register.

   The CFA (Canonical Frame Address) described in DWARF is
   called the Virtual Frame Pointer on MIPS/SGI machines.

   The DW_FRAME* names here are MIPS/SGI specific.
   Libdwarf interfaces defined in 2008 make the
   frame definitions here (and the fixed table sizes
   they imply) obsolete.  They are left here for compatibility.
*/
/* Default column used for CFA in the libdwarf reader client.
   Assumes reg 0 never appears as
   a register in DWARF information. Usable for MIPS,
   but never a good idea, really.    */
/*  These identifiers are not defined by any DWARFn standard. */
#define DW_FRAME_CFA_COL 0

#define DW_FRAME_REG1   1  /* integer reg 1 */
#define DW_FRAME_REG2   2  /* integer reg 2 */
#define DW_FRAME_REG3   3  /* integer reg 3 */
#define DW_FRAME_REG4   4  /* integer reg 4 */
#define DW_FRAME_REG5   5  /* integer reg 5 */
#define DW_FRAME_REG6   6  /* integer reg 6 */
#define DW_FRAME_REG7   7  /* integer reg 7 */
#define DW_FRAME_REG8   8  /* integer reg 8 */
#define DW_FRAME_REG9   9  /* integer reg 9 */
#define DW_FRAME_REG10  10 /* integer reg 10 */
#define DW_FRAME_REG11  11 /* integer reg 11 */
#define DW_FRAME_REG12  12 /* integer reg 12 */
#define DW_FRAME_REG13  13 /* integer reg 13 */
#define DW_FRAME_REG14  14 /* integer reg 14 */
#define DW_FRAME_REG15  15 /* integer reg 15 */
#define DW_FRAME_REG16  16 /* integer reg 16 */
#define DW_FRAME_REG17  17 /* integer reg 17 */
#define DW_FRAME_REG18  18 /* integer reg 18 */
#define DW_FRAME_REG19  19 /* integer reg 19 */
#define DW_FRAME_REG20  20 /* integer reg 20 */
#define DW_FRAME_REG21  21 /* integer reg 21 */
#define DW_FRAME_REG22  22 /* integer reg 22 */
#define DW_FRAME_REG23  23 /* integer reg 23 */
#define DW_FRAME_REG24  24 /* integer reg 24 */
#define DW_FRAME_REG25  25 /* integer reg 25 */
#define DW_FRAME_REG26  26 /* integer reg 26 */
#define DW_FRAME_REG27  27 /* integer reg 27 */
#define DW_FRAME_REG28  28 /* integer reg 28 */
#define DW_FRAME_REG29  29 /* integer reg 29 */
#define DW_FRAME_REG30  30 /* integer reg 30 */
#define DW_FRAME_REG31  31 /* integer reg 31, aka ra */

        /* MIPS1, 2 have only some of these 64-bit registers.
        ** MIPS1  save/restore takes 2 instructions per 64-bit reg, and
        ** in that case, the register is considered stored after the second
        ** swc1.
        */
#define DW_FRAME_FREG0  32 /* 64-bit floating point reg 0 */
#define DW_FRAME_FREG1  33 /* 64-bit floating point reg 1 */
#define DW_FRAME_FREG2  34 /* 64-bit floating point reg 2 */
#define DW_FRAME_FREG3  35 /* 64-bit floating point reg 3 */
#define DW_FRAME_FREG4  36 /* 64-bit floating point reg 4 */
#define DW_FRAME_FREG5  37 /* 64-bit floating point reg 5 */
#define DW_FRAME_FREG6  38 /* 64-bit floating point reg 6 */
#define DW_FRAME_FREG7  39 /* 64-bit floating point reg 7 */
#define DW_FRAME_FREG8  40 /* 64-bit floating point reg 8 */
#define DW_FRAME_FREG9  41 /* 64-bit floating point reg 9 */
#define DW_FRAME_FREG10 42 /* 64-bit floating point reg 10 */
#define DW_FRAME_FREG11 43 /* 64-bit floating point reg 11 */
#define DW_FRAME_FREG12 44 /* 64-bit floating point reg 12 */
#define DW_FRAME_FREG13 45 /* 64-bit floating point reg 13 */
#define DW_FRAME_FREG14 46 /* 64-bit floating point reg 14 */
#define DW_FRAME_FREG15 47 /* 64-bit floating point reg 15 */
#define DW_FRAME_FREG16 48 /* 64-bit floating point reg 16 */
#define DW_FRAME_FREG17 49 /* 64-bit floating point reg 17 */
#define DW_FRAME_FREG18 50 /* 64-bit floating point reg 18 */
#define DW_FRAME_FREG19 51 /* 64-bit floating point reg 19 */
#define DW_FRAME_FREG20 52 /* 64-bit floating point reg 20 */
#define DW_FRAME_FREG21 53 /* 64-bit floating point reg 21 */
#define DW_FRAME_FREG22 54 /* 64-bit floating point reg 22 */
#define DW_FRAME_FREG23 55 /* 64-bit floating point reg 23 */
#define DW_FRAME_FREG24 56 /* 64-bit floating point reg 24 */
#define DW_FRAME_FREG25 57 /* 64-bit floating point reg 25 */
#define DW_FRAME_FREG26 58 /* 64-bit floating point reg 26 */
#define DW_FRAME_FREG27 59 /* 64-bit floating point reg 27 */
#define DW_FRAME_FREG28 60 /* 64-bit floating point reg 28 */
#define DW_FRAME_FREG29 61 /* 64-bit floating point reg 29 */
#define DW_FRAME_FREG30 62 /* 64-bit floating point reg 30 */
#define DW_FRAME_FREG31 63 /* 64-bit floating point reg 31 */

#define DW_FRAME_FREG32 64 /* 64-bit floating point reg 32 */
#define DW_FRAME_FREG33 65 /* 64-bit floating point reg 33 */
#define DW_FRAME_FREG34 66 /* 64-bit floating point reg 34 */
#define DW_FRAME_FREG35 67 /* 64-bit floating point reg 35 */
#define DW_FRAME_FREG36 68 /* 64-bit floating point reg 36 */
#define DW_FRAME_FREG37 69 /* 64-bit floating point reg 37 */
#define DW_FRAME_FREG38 70 /* 64-bit floating point reg 38 */
#define DW_FRAME_FREG39 71 /* 64-bit floating point reg 39 */
#define DW_FRAME_FREG40 72 /* 64-bit floating point reg 40 */
#define DW_FRAME_FREG41 73 /* 64-bit floating point reg 41 */
#define DW_FRAME_FREG42 74 /* 64-bit floating point reg 42 */
#define DW_FRAME_FREG43 75 /* 64-bit floating point reg 43 */
#define DW_FRAME_FREG44 76 /* 64-bit floating point reg 44 */
#define DW_FRAME_FREG45 77 /* 64-bit floating point reg 45 */
#define DW_FRAME_FREG46 78 /* 64-bit floating point reg 46 */
#define DW_FRAME_FREG47 79 /* 64-bit floating point reg 47 */
#define DW_FRAME_FREG48 80 /* 64-bit floating point reg 48 */
#define DW_FRAME_FREG49 81 /* 64-bit floating point reg 49 */
#define DW_FRAME_FREG50 82 /* 64-bit floating point reg 50 */
#define DW_FRAME_FREG51 83 /* 64-bit floating point reg 51 */
#define DW_FRAME_FREG52 84 /* 64-bit floating point reg 52 */
#define DW_FRAME_FREG53 85 /* 64-bit floating point reg 53 */
#define DW_FRAME_FREG54 86 /* 64-bit floating point reg 54 */
#define DW_FRAME_FREG55 87 /* 64-bit floating point reg 55 */
#define DW_FRAME_FREG56 88 /* 64-bit floating point reg 56 */
#define DW_FRAME_FREG57 89 /* 64-bit floating point reg 57 */
#define DW_FRAME_FREG58 90 /* 64-bit floating point reg 58 */
#define DW_FRAME_FREG59 91 /* 64-bit floating point reg 59 */
#define DW_FRAME_FREG60 92 /* 64-bit floating point reg 60 */
#define DW_FRAME_FREG61 93 /* 64-bit floating point reg 61 */
#define DW_FRAME_FREG62 94 /* 64-bit floating point reg 62 */
#define DW_FRAME_FREG63 95 /* 64-bit floating point reg 63 */
#define DW_FRAME_FREG64 96 /* 64-bit floating point reg 64 */
#define DW_FRAME_FREG65 97 /* 64-bit floating point reg 65 */
#define DW_FRAME_FREG66 98 /* 64-bit floating point reg 66 */
#define DW_FRAME_FREG67 99 /* 64-bit floating point reg 67 */
#define DW_FRAME_FREG68 100 /* 64-bit floating point reg 68 */
#define DW_FRAME_FREG69 101 /* 64-bit floating point reg 69 */
#define DW_FRAME_FREG70 102 /* 64-bit floating point reg 70 */
#define DW_FRAME_FREG71 103 /* 64-bit floating point reg 71 */
#define DW_FRAME_FREG72 104 /* 64-bit floating point reg 72 */
#define DW_FRAME_FREG73 105 /* 64-bit floating point reg 73 */
#define DW_FRAME_FREG74 106 /* 64-bit floating point reg 74 */
#define DW_FRAME_FREG75 107 /* 64-bit floating point reg 75 */
#define DW_FRAME_FREG76 108 /* 64-bit floating point reg 76 */

/*  ***IMPORTANT NOTE, TARGET DEPENDENCY ****
    The following 4 #defines are dependent on
    the target cpu(s) that you apply libdwarf to.
    Ensure that DW_FRAME_UNDEFINED_VAL  and DW_FRAME_SAME_VAL
    do not conflict with the range [0-DW_FRAME_STATIC_LINK].
    The value 63 works for MIPS cpus at least up to the R16000.

    For a cpu with more than 63 real registers
    DW_FRAME_HIGHEST_NORMAL_REGISTER
    must be increased for things to work properly!
    Also ensure that DW_FRAME_UNDEFINED_VAL DW_FRAME_SAME_VAL
    are not in the range [0-DW_FRAME_STATIC_LINK]

    Having DW_FRAME_HIGHEST_NORMAL_REGISTER be higher than
    is strictly needed is safe.

*/

#ifndef DW_FRAME_HIGHEST_NORMAL_REGISTER
#define DW_FRAME_HIGHEST_NORMAL_REGISTER 188
#endif
/* This is the number of columns in the Frame Table.
   This constant should
   be kept in sync with DW_REG_TABLE_SIZE defined in libdwarf.h
   It must also be large enough to be beyond the highest
   compiler-defined-register (meaning DW_FRAME_RA_COL DW_FRAME_STATIC_LINK
   in the MIPS/IRIX case */
#ifndef DW_FRAME_LAST_REG_NUM
#define DW_FRAME_LAST_REG_NUM   (DW_FRAME_HIGHEST_NORMAL_REGISTER + 3)
#endif


/* Column recording ra (return address from a function call).
   This is common to many architectures, but as a 'simple register'
   is not necessarily adequate for all architectures.
   For MIPS/IRIX this register number is actually recorded on disk
   in the .debug_frame section.
   */
#define DW_FRAME_RA_COL  (DW_FRAME_HIGHEST_NORMAL_REGISTER + 1)

/* Column recording static link applicable to up-level
   addressing, as in IRIX mp code, pascal, etc.
   This is common to many architectures but
   is not necessarily adequate for all architectures.
   For MIPS/IRIX this register number is actually recorded on disk
   in the .debug_frame section.
*/
#define DW_FRAME_STATIC_LINK (DW_FRAME_HIGHEST_NORMAL_REGISTER + 2)



/*
  DW_FRAME_UNDEFINED_VAL and  DW_FRAME_SAME_VAL  are
  never on disk, just generated by libdwarf. See libdwarf.h
  for their values.
*/



#define DW_CHILDREN_no               0x00
#define DW_CHILDREN_yes              0x01

#define DW_ADDR_none            0

#ifdef __cplusplus
}
#endif
#endif /* __DWARF_H */
