#include <stdlib.h>
#include <string.h>
#include <mono/metadata/class.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/jit/codegen.h>
#include <mono/jit/debug.h>

#include "debug-private.h"

#define ABBREV_COMPILE_UNIT		1
#define ABBREV_SUBPROGRAM		2
#define ABBREV_SUBPROGRAM_RETVAL	3
#define ABBREV_BASE_TYPE		4
#define ABBREV_FORMAL_PARAMETER		5
#define ABBREV_PARAMETER		6
#define ABBREV_LOCAL_VARIABLE		7
#define ABBREV_STRUCT_TYPE		8
#define ABBREV_STRUCT_MEMBER		9
#define ABBREV_STRUCT_ACCESS		10

// The following constants are defined in the DWARF 2 specification
#define DW_TAG_formal_parameter		0x05
#define DW_TAG_member			0x0d
#define DW_TAG_compile_unit		0x11
#define DW_TAG_structure_type		0x13
#define DW_TAG_access_declaration	0x23
#define DW_TAG_base_type		0x24
#define DW_TAG_subprogram		0x2e
#define DW_TAG_variable			0x34

#define DW_CHILDREN_no			0
#define DW_CHILDREN_yes			1

#define DW_AT_location			0x02
#define DW_AT_name			0x03
#define DW_AT_byte_size			0x0b
#define DW_AT_stmt_list			0x10
#define DW_AT_low_pc			0x11
#define DW_AT_high_pc			0x12
#define DW_AT_language			0x13
#define DW_AT_producer			0x25
#define DW_AT_start_scope		0x2c
#define DW_AT_accessibility		0x32
#define DW_AT_calling_convention	0x36
#define DW_AT_data_member_location	0x38
#define DW_AT_encoding			0x3e
#define DW_AT_external			0x3f
#define DW_AT_type			0x49

#define DW_FORM_addr			0x01
#define DW_FORM_block4			0x04
#define DW_FORM_data2			0x05
#define DW_FORM_data4			0x06
#define DW_FORM_data8			0x07
#define DW_FORM_string			0x08
#define DW_FORM_data1			0x0b
#define DW_FORM_flag			0x0c
#define DW_FORM_ref4			0x13

#define DW_ATE_void			0x00
#define DW_ATE_address			0x01
#define DW_ATE_boolean			0x02
#define DW_ATE_complex_float		0x03
#define DW_ATE_float			0x04
#define DW_ATE_signed			0x05
#define DW_ATE_signed_char		0x06
#define DW_ATE_unsigned			0x07
#define DW_ATE_unsigned_char		0x08

#define DW_OP_constu			0x10
#define DW_OP_consts			0x11
#define DW_OP_fbreg			0x91

#define DW_CC_normal			1
#define DW_CC_program			2
#define DW_CC_nocall			3

#define DW_ACCESS_public		1
#define DW_ACCESS_protected		2
#define DW_ACCESS_private		3

#define DW_LANG_C_plus_plus		0x04
#define DW_LANG_Java			0x0b
// This is NOT in the standard, we're using Java for the moment. */
#define DW_LANG_C_sharp			DW_LANG_Java

#define DW_LNS_extended_op		0
#define DW_LNS_copy			1
#define DW_LNS_advance_pc		2
#define DW_LNS_advance_line		3
#define DW_LNS_set_file			4
#define DW_LNS_set_column		5
#define DW_LNS_negate_stmt		6
#define DW_LNS_set_basic_block		7
#define DW_LNS_const_add_pc		8
#define DW_LNS_fixed_advance_pc		9

#define DW_LNE_end_sequence		1
#define DW_LNE_set_address		2
#define DW_LNE_define_file		3


static const int line_base = 1, line_range = 8, opcode_base = 10;
static const int standard_opcode_sizes [10] = {
    0, 0, 1, 1, 1, 1, 0, 0, 0, 0
};

void
mono_debug_open_assembly_dwarf2 (AssemblyDebugInfo *info)
{
}

void
mono_debug_close_assembly_dwarf2 (AssemblyDebugInfo *info)
{
}

static void
dwarf2_write_byte (FILE *f, int byte)
{
	fprintf (f, "\t.byte\t\t%d\n", byte);
}

static void
dwarf2_write_2byte (FILE *f, int word)
{
	fprintf (f, "\t.2byte\t\t%d\n", word);
}

static void
dwarf2_write_pair (FILE *f, int a, int b)
{
	fprintf (f, "\t.byte\t\t%d, %d\n", a, b);
}

static void
dwarf2_write_long (FILE *f, unsigned long value)
{
	fprintf (f, "\t.long\t\t%lu\n", value);
}

static void
dwarf2_write_address (FILE *f, void *address)
{
	fprintf (f, "\t.long\t\t%p\n", address);
}

static void
dwarf2_write_string (FILE *f, const char *string)
{
	fprintf (f, "\t.string\t\t\"%s\"\n", string);
}

static void
dwarf2_write_sleb128 (FILE *f, int value)
{
	fprintf (f, "\t.sleb128\t%d\n", value);
}

static void
dwarf2_write_uleb128 (FILE *f, int value)
{
	fprintf (f, "\t.uleb128\t%d\n", value);
}

static void
dwarf2_write_section_start (FILE *f, const char *section)
{
	fprintf (f, "\t.section\t.%s\n", section);
}

static void
dwarf2_write_section_end (FILE *f)
{
	fprintf (f, "\t.previous\n\n");
}

static void
dwarf2_write_label (FILE *f, const char *label)
{
	fprintf (f, ".L_%s:\n", label);
}

static void
dwarf2_write_section_size (FILE *f, const char *start_label, const char *end_label)
{
	fprintf (f, "\t.long\t\t.L_%s - .L_%s\n", end_label, start_label);
}

static void
dwarf2_write_ref4 (FILE *f, const char *target_label)
{
	fprintf (f, "\t.long\t\t.L_%s\n", target_label);
}

static void
dwarf2_write_type_ref (FILE *f, int type_index)
{
	fprintf (f, "\t.long\t\t.L_TYPE_%u - .L_debug_info_b\n", type_index);
}

static void
dwarf2_write_dw_lns_copy (FILE *f)
{
	dwarf2_write_byte (f, DW_LNS_copy);
}

static void
dwarf2_write_dw_lns_advance_pc (FILE *f, unsigned value)
{
	dwarf2_write_byte (f, DW_LNS_advance_pc);
	dwarf2_write_uleb128 (f, value);
}

static void
dwarf2_write_dw_lns_advance_line (FILE *f, int value)
{
	dwarf2_write_byte (f, DW_LNS_advance_line);
	dwarf2_write_sleb128 (f, value);
}

static void
dwarf2_write_dw_lns_negate_stmt (FILE *f)
{
	dwarf2_write_byte (f, DW_LNS_negate_stmt);
}

static void
dwarf2_write_dw_lns_set_basic_block (FILE *f)
{
	dwarf2_write_byte (f, DW_LNS_set_basic_block);
}

static void
dwarf2_write_dw_lne_end_sequence (FILE *f)
{
	dwarf2_write_byte (f, 0);
	dwarf2_write_byte (f, 1);
	dwarf2_write_byte (f, DW_LNE_end_sequence);
}

static void
dwarf2_write_dw_lne_set_address (FILE *f, void *address)
{
	dwarf2_write_byte (f, 0);
	dwarf2_write_byte (f, sizeof (address) + 1);
	dwarf2_write_byte (f, DW_LNE_set_address);
	dwarf2_write_address (f, address);
}

static void
dwarf2_write_base_type (AssemblyDebugInfo *info, int index,
			int type, int size, gchar *name)
{
	char buffer [BUFSIZ];

	sprintf (buffer, "TYPE_%d", index);
	dwarf2_write_label (info->f, buffer);
	// DW_TAG_basic_type
	dwarf2_write_byte (info->f, ABBREV_BASE_TYPE);
	dwarf2_write_string (info->f, name);
	dwarf2_write_byte (info->f, type);
	dwarf2_write_byte (info->f, size);
}

static void
dwarf2_write_class (AssemblyDebugInfo *info, MonoClass *klass, int index)
{
	char buffer [BUFSIZ];
	int print = 0;

	if (!strncmp (klass->name, "My", 2)) {
		g_message (G_STRLOC ": %s - %s - %x", klass->name_space, klass->name, klass->flags);
		print = 1;
		// G_BREAKPOINT ();
	}

	sprintf (buffer, "TYPE_%u", index);
	dwarf2_write_label (info->f, buffer);

	if (!klass->enumtype && klass->byval_arg.type == MONO_TYPE_VALUETYPE) {
		guint32 *idxs;
		int i;

		idxs = g_new0 (guint32, klass->field.last - klass->field.first + 1);
		for (i = 0; i < klass->field.count; i++) {
			MonoClass *subclass = mono_class_from_mono_type (klass->fields [i].type);
			idxs [i] = mono_debug_get_type (info, subclass);
		}

		// DW_TAG_structure_type
		dwarf2_write_byte (info->f, ABBREV_STRUCT_TYPE);
		dwarf2_write_string (info->f, klass->name);
		dwarf2_write_long (info->f, klass->instance_size);

		for (i = 0; i < klass->field.count; i++) {
			MonoClass *subclass = mono_class_from_mono_type (klass->fields [i].type);
			char start [BUFSIZ], end [BUFSIZ];
			static long label_index = 0;

			if (print)
				g_message (G_STRLOC ": %s - %x - %d - %d", klass->fields [i].name,
					   klass->fields [i].type->attrs, klass->fields [i].offset,
					   subclass->instance_size - sizeof (MonoObject));

			sprintf (start, "DSF1_%ld", ++label_index);
			sprintf (end, "DSF2_%ld", label_index);

			// DW_TAG_member
			dwarf2_write_byte (info->f, ABBREV_STRUCT_MEMBER);
			dwarf2_write_string (info->f, klass->fields [i].name);
			dwarf2_write_type_ref (info->f, idxs [i]);

			dwarf2_write_section_size (info->f, start, end);
			dwarf2_write_label (info->f, start);
			dwarf2_write_byte (info->f, DW_OP_constu);
			dwarf2_write_uleb128 (info->f, klass->fields [i].offset - sizeof (MonoObject));
			dwarf2_write_label (info->f, end);

			dwarf2_write_long (info->f, subclass->instance_size - sizeof (MonoObject));

			// DW_TAG_access_declaration
			dwarf2_write_byte (info->f, ABBREV_STRUCT_ACCESS);
			dwarf2_write_string (info->f, klass->fields [i].name);
			if (klass->fields [i].type->attrs & FIELD_ATTRIBUTE_PRIVATE)
				dwarf2_write_byte (info->f, DW_ACCESS_private);
			else if (klass->fields [i].type->attrs & FIELD_ATTRIBUTE_FAMILY)
				dwarf2_write_byte (info->f, DW_ACCESS_private);
			else
				dwarf2_write_byte (info->f, DW_ACCESS_public);
		}

		dwarf2_write_byte (info->f, 0);
		// DW_TAG_structure_type ends here

		g_free (idxs);
	} else {
		// DW_TAG_basic_type
		dwarf2_write_byte (info->f, ABBREV_BASE_TYPE);
		dwarf2_write_string (info->f, "<unknown>");
		dwarf2_write_byte (info->f, DW_ATE_address);
		dwarf2_write_byte (info->f, sizeof (gpointer));
	}
}

static void
dwarf2_write_parameter (AssemblyDebugInfo *info, DebugMethodInfo *minfo, const gchar *name,
			int stack_offset, MonoClass *klass)
{
	static long label_index = 0;
	int type_index = mono_debug_get_type (info, klass);
	char start [BUFSIZ], end [BUFSIZ];

	sprintf (start, "DT1_%ld", ++label_index);
	sprintf (end, "DT2_%ld", label_index);
		
	// DW_TAG_format_parameter
	dwarf2_write_byte (info->f, ABBREV_PARAMETER);
	dwarf2_write_string (info->f, name);
	dwarf2_write_type_ref (info->f, type_index);
	dwarf2_write_section_size (info->f, start, end);
	dwarf2_write_label (info->f, start);
	dwarf2_write_byte (info->f, DW_OP_fbreg);
	dwarf2_write_sleb128 (info->f, stack_offset);
	dwarf2_write_label (info->f, end);
	dwarf2_write_long (info->f, minfo->frame_start_offset);
}

static void
dwarf2_write_variable (AssemblyDebugInfo *info, DebugMethodInfo *minfo, const gchar *name,
		       int stack_offset, MonoClass *klass)
{
	static long label_index = 0;
	int type_index = mono_debug_get_type (info, klass);
	char start [BUFSIZ], end [BUFSIZ];

	sprintf (start, "DT3_%ld", ++label_index);
	sprintf (end, "DT4_%ld", label_index);
		
	// DW_TAG_format_parameter
	dwarf2_write_byte (info->f, ABBREV_LOCAL_VARIABLE);
	dwarf2_write_string (info->f, name);
	dwarf2_write_type_ref (info->f, type_index);
	dwarf2_write_section_size (info->f, start, end);
	dwarf2_write_label (info->f, start);
	dwarf2_write_byte (info->f, DW_OP_fbreg);
	dwarf2_write_sleb128 (info->f, stack_offset);
	dwarf2_write_label (info->f, end);
	dwarf2_write_long (info->f, minfo->frame_start_offset);
}

static void 
write_method_lines_dwarf2 (AssemblyDebugInfo *info, DebugMethodInfo *minfo)
{
	guint32 st_line = 0;
	gpointer st_address = 0;
	int i;

	// Start of statement program
	dwarf2_write_dw_lns_advance_line (info->f, minfo->start_line - 1);
	dwarf2_write_dw_lne_set_address (info->f, minfo->code_start);
	dwarf2_write_dw_lns_negate_stmt (info->f);
	dwarf2_write_dw_lns_copy (info->f);

	st_line = minfo->start_line;
	st_address = minfo->code_start;

	for (i = 1; i < minfo->line_numbers->len; i++) {
		DebugLineNumberInfo *lni = g_ptr_array_index (minfo->line_numbers, i);
		gint32 line_inc, addr_inc, opcode;
		int used_standard_opcode = 0;

		line_inc = lni->line - st_line;
		addr_inc = lni->address - st_address;

		if (addr_inc < 0) {
			dwarf2_write_dw_lne_set_address (info->f, lni->address);
			used_standard_opcode = 1;
		} else if (addr_inc && !line_inc) {
			dwarf2_write_dw_lns_advance_pc (info->f, addr_inc);
			used_standard_opcode = 1;
		}

		if ((line_inc < 0) || (line_inc >= line_range)) {
			dwarf2_write_dw_lns_advance_pc (info->f, addr_inc);
			dwarf2_write_dw_lns_advance_line (info->f, line_inc);
			used_standard_opcode = 1;
		} else if (line_inc > 0) {
			opcode = (line_inc - 1) + (line_range * addr_inc) + opcode_base;
			g_assert (opcode >= 0);

			if (opcode >= 256) {
				dwarf2_write_dw_lns_advance_pc (info->f, addr_inc);
				dwarf2_write_dw_lns_advance_line (info->f, line_inc);
				used_standard_opcode = 1;
			} else
				dwarf2_write_byte (info->f, opcode);
		}

		if (used_standard_opcode)
			dwarf2_write_dw_lns_copy (info->f);

		st_line += line_inc;
		st_address += addr_inc;
	}

	dwarf2_write_dw_lne_set_address (info->f, minfo->code_start + minfo->code_size);
	dwarf2_write_dw_lns_copy (info->f);
	dwarf2_write_dw_lne_end_sequence (info->f);
}

static void
write_method_lines_func (gpointer key, gpointer value, gpointer user_data)
{
	write_method_lines_dwarf2 (user_data, value);
}

static void
write_line_numbers (AssemblyDebugInfo *info)
{
	/* State machine registers. */
	const char *source_file;
	int i;
	
	source_file = (gchar *) g_ptr_array_index (info->source_files, 0);

	// Line number information.
	dwarf2_write_section_start (info->f, "debug_line");
	dwarf2_write_label (info->f, "debug_line_b");
	dwarf2_write_section_size (info->f, "DL1", "debug_line_e");
	dwarf2_write_label (info->f, "DL1");
	dwarf2_write_2byte (info->f, 2);
	dwarf2_write_section_size (info->f, "DL2", "DL3");
	dwarf2_write_label (info->f, "DL2");
	// minimum instruction length
	dwarf2_write_byte (info->f, 1);
	// default is statement
	dwarf2_write_byte (info->f, 1);
	// line base
	dwarf2_write_byte (info->f, line_base);
	// line range
	dwarf2_write_byte (info->f, line_range);
	// opcode base
	dwarf2_write_byte (info->f, opcode_base);
	// standard opcode sizes
	for (i = 1; i < opcode_base; i++)
		dwarf2_write_byte (info->f, standard_opcode_sizes [i]);
	// include directories
	dwarf2_write_byte (info->f, 0);
	// file names
	{
		// File 0
		dwarf2_write_string (info->f, source_file);
		dwarf2_write_uleb128 (info->f, 0);
		dwarf2_write_uleb128 (info->f, 0);
		dwarf2_write_uleb128 (info->f, 0);
		// end of list
		dwarf2_write_byte (info->f, 0);
	}
	dwarf2_write_label (info->f, "DL3");

	g_hash_table_foreach (info->methods, write_method_lines_func, info);

	dwarf2_write_label (info->f, "debug_line_e");
	dwarf2_write_section_end (info->f);
}

static void
write_class_dwarf2 (AssemblyDebugInfo *info, MonoClass *klass, guint index)
{
	switch (klass->byval_arg.type) {
	case MONO_TYPE_BOOLEAN:
		dwarf2_write_base_type (info, index, DW_ATE_boolean, 1, "Boolean");
		break;
	case MONO_TYPE_CHAR:
		dwarf2_write_base_type (info, index, DW_ATE_unsigned_char, 2, "Char");
		break;
	case MONO_TYPE_I1:
		dwarf2_write_base_type (info, index, DW_ATE_signed, 1, "SByte");
		break;
	case MONO_TYPE_U1:
		dwarf2_write_base_type (info, index, DW_ATE_unsigned, 1, "Byte");
		break;
	case MONO_TYPE_I2:
		dwarf2_write_base_type (info, index, DW_ATE_signed, 2, "Int16");
		break;
	case MONO_TYPE_U2:
		dwarf2_write_base_type (info, index, DW_ATE_unsigned, 2, "UInt16");
		break;
	case MONO_TYPE_I4:
		dwarf2_write_base_type (info, index, DW_ATE_signed, 4, "Int32");
		break;
	case MONO_TYPE_U4:
		dwarf2_write_base_type (info, index, DW_ATE_unsigned, 4, "UInt32");
		break;
	case MONO_TYPE_I8:
		dwarf2_write_base_type (info, index, DW_ATE_signed, 8, "Int64");
		break;
	case MONO_TYPE_U8:
		dwarf2_write_base_type (info, index, DW_ATE_unsigned, 8, "UInt64");
		break;
	case MONO_TYPE_R4:
		dwarf2_write_base_type (info, index, DW_ATE_float, 4, "Float");
		break;
	case MONO_TYPE_R8:
		dwarf2_write_base_type (info, index, DW_ATE_float, 8, "Double");
		break;
	default:
		dwarf2_write_class (info, klass, index);
		break;
	}
}

static void
write_class (gpointer key, gpointer value, gpointer user_data)
{
	write_class_dwarf2 (user_data, key, GPOINTER_TO_INT (value));
}

static void
write_method_dwarf2 (AssemblyDebugInfo *info, DebugMethodInfo *minfo)
{
	int is_external = 0, i;
	MonoType *ret_type = NULL;
	gchar **names;

	if (minfo->method->signature->ret->type != MONO_TYPE_VOID)
		ret_type = minfo->method->signature->ret;

	// DW_TAG_subprogram
	if (ret_type)
		dwarf2_write_byte (info->f, ABBREV_SUBPROGRAM_RETVAL);
	else
		dwarf2_write_byte (info->f, ABBREV_SUBPROGRAM);
	dwarf2_write_string (info->f, minfo->name);
	dwarf2_write_byte (info->f, is_external);
	dwarf2_write_address (info->f, minfo->code_start);
	dwarf2_write_address (info->f, minfo->code_start + minfo->code_size);
	dwarf2_write_byte (info->f, DW_CC_nocall);
	if (ret_type) {
		MonoClass *klass = mono_class_from_mono_type (ret_type);
		int type_index = mono_debug_get_type (info, klass);
		dwarf2_write_type_ref (info->f, type_index);
	}

	if (minfo->method->signature->hasthis)
		dwarf2_write_parameter (info, minfo, "this", 8, minfo->method->klass);

	names = g_new (char *, minfo->method->signature->param_count);
	mono_method_get_param_names (minfo->method, (const char **) names);

	for (i = 0; i < minfo->method->signature->param_count; i++) {
		MonoType *type = minfo->method->signature->params [i];
		MonoClass *klass = mono_class_from_mono_type (type);

		dwarf2_write_parameter (info, minfo, names [i], minfo->params [i].offset, klass);
	}

	g_free (names);

	for (i = 0; i < minfo->num_locals; i++) {
		MonoMethodHeader *header = ((MonoMethodNormal*) minfo->method)->header;
		MonoClass *klass = mono_class_from_mono_type (header->locals [i]);
		char name [BUFSIZ];

		sprintf (name, "V_%d", i);
		dwarf2_write_variable (info, minfo, name, minfo->locals [i].offset, klass);
	}

	dwarf2_write_byte (info->f, 0);
	// DW_TAG_subprogram ends here
}

static void
write_method_func (gpointer key, gpointer value, gpointer user_data)
{
	write_method_dwarf2 (user_data, value);
}

void
mono_debug_write_assembly_dwarf2 (AssemblyDebugInfo *info)
{
	gchar *source_file = g_ptr_array_index (info->source_files, 0);

	// DWARF 2 Abbreviation table.
	dwarf2_write_section_start (info->f, "debug_abbrev");
	dwarf2_write_label (info->f, "debug_abbrev");

	dwarf2_write_byte (info->f, ABBREV_COMPILE_UNIT);
	dwarf2_write_byte (info->f, DW_TAG_compile_unit);
	dwarf2_write_byte (info->f, DW_CHILDREN_yes);
	dwarf2_write_pair (info->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (info->f, DW_AT_language, DW_FORM_data2);
	dwarf2_write_pair (info->f, DW_AT_producer, DW_FORM_string);
	dwarf2_write_pair (info->f, DW_AT_stmt_list, DW_FORM_ref4);
	dwarf2_write_pair (info->f, 0, 0);

	dwarf2_write_byte (info->f, ABBREV_SUBPROGRAM);
	dwarf2_write_byte (info->f, DW_TAG_subprogram);
	dwarf2_write_byte (info->f, DW_CHILDREN_yes);
	dwarf2_write_pair (info->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (info->f, DW_AT_external, DW_FORM_flag);
	dwarf2_write_pair (info->f, DW_AT_low_pc, DW_FORM_addr);
	dwarf2_write_pair (info->f, DW_AT_high_pc, DW_FORM_addr);
	dwarf2_write_pair (info->f, DW_AT_calling_convention, DW_FORM_data1);
	dwarf2_write_pair (info->f, 0, 0);

	dwarf2_write_byte (info->f, ABBREV_SUBPROGRAM_RETVAL);
	dwarf2_write_byte (info->f, DW_TAG_subprogram);
	dwarf2_write_byte (info->f, DW_CHILDREN_yes);
	dwarf2_write_pair (info->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (info->f, DW_AT_external, DW_FORM_flag);
	dwarf2_write_pair (info->f, DW_AT_low_pc, DW_FORM_addr);
	dwarf2_write_pair (info->f, DW_AT_high_pc, DW_FORM_addr);
	dwarf2_write_pair (info->f, DW_AT_calling_convention, DW_FORM_data1);
	dwarf2_write_pair (info->f, DW_AT_type, DW_FORM_ref4);
	dwarf2_write_pair (info->f, 0, 0);

	dwarf2_write_byte (info->f, ABBREV_BASE_TYPE);
	dwarf2_write_byte (info->f, DW_TAG_base_type);
	dwarf2_write_byte (info->f, DW_CHILDREN_no);
	dwarf2_write_pair (info->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (info->f, DW_AT_encoding, DW_FORM_data1);
	dwarf2_write_pair (info->f, DW_AT_byte_size, DW_FORM_data1);
	dwarf2_write_pair (info->f, 0, 0);

	dwarf2_write_byte (info->f, ABBREV_FORMAL_PARAMETER);
	dwarf2_write_byte (info->f, DW_TAG_formal_parameter);
	dwarf2_write_byte (info->f, DW_CHILDREN_no);
	dwarf2_write_pair (info->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (info->f, DW_AT_type, DW_FORM_ref4);
	dwarf2_write_pair (info->f, 0, 0);

	dwarf2_write_byte (info->f, ABBREV_PARAMETER);
	dwarf2_write_byte (info->f, DW_TAG_formal_parameter);
	dwarf2_write_byte (info->f, DW_CHILDREN_no);
	dwarf2_write_pair (info->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (info->f, DW_AT_type, DW_FORM_ref4);
	dwarf2_write_pair (info->f, DW_AT_location, DW_FORM_block4);
	dwarf2_write_pair (info->f, DW_AT_start_scope, DW_FORM_data4);
	dwarf2_write_pair (info->f, 0, 0);

	dwarf2_write_byte (info->f, ABBREV_LOCAL_VARIABLE);
	dwarf2_write_byte (info->f, DW_TAG_variable);
	dwarf2_write_byte (info->f, DW_CHILDREN_no);
	dwarf2_write_pair (info->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (info->f, DW_AT_type, DW_FORM_ref4);
	dwarf2_write_pair (info->f, DW_AT_location, DW_FORM_block4);
	dwarf2_write_pair (info->f, DW_AT_start_scope, DW_FORM_data4);
	dwarf2_write_pair (info->f, 0, 0);

	dwarf2_write_byte (info->f, ABBREV_STRUCT_TYPE);
	dwarf2_write_byte (info->f, DW_TAG_structure_type);
	dwarf2_write_byte (info->f, DW_CHILDREN_yes);
	dwarf2_write_pair (info->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (info->f, DW_AT_byte_size, DW_FORM_data4);
	dwarf2_write_pair (info->f, 0, 0);

	dwarf2_write_byte (info->f, ABBREV_STRUCT_MEMBER);
	dwarf2_write_byte (info->f, DW_TAG_member);
	dwarf2_write_byte (info->f, DW_CHILDREN_no);
	dwarf2_write_pair (info->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (info->f, DW_AT_type, DW_FORM_ref4);
	dwarf2_write_pair (info->f, DW_AT_data_member_location, DW_FORM_block4);
	dwarf2_write_pair (info->f, DW_AT_byte_size, DW_FORM_data4);
	dwarf2_write_pair (info->f, 0, 0);

	dwarf2_write_byte (info->f, ABBREV_STRUCT_ACCESS);
	dwarf2_write_byte (info->f, DW_TAG_access_declaration);
	dwarf2_write_byte (info->f, DW_CHILDREN_no);
	dwarf2_write_pair (info->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (info->f, DW_AT_accessibility, DW_FORM_data1);
	dwarf2_write_pair (info->f, 0, 0);

	dwarf2_write_label (info->f, "debug_abbrev_e");
	dwarf2_write_section_end (info->f);

	// Line numbers
	write_line_numbers (info);

	// Compile unit header
	dwarf2_write_section_start (info->f, "debug_info");
	dwarf2_write_label (info->f, "debug_info_b");
	dwarf2_write_section_size (info->f, "DI1", "debug_info_e");
	dwarf2_write_label (info->f, "DI1");
	dwarf2_write_2byte (info->f, 2);
	dwarf2_write_ref4 (info->f, "debug_abbrev_b");
	dwarf2_write_byte (info->f, sizeof (gpointer));

	// DW_TAG_compile_unit
	dwarf2_write_byte (info->f, ABBREV_COMPILE_UNIT);
	dwarf2_write_string (info->f, source_file);
	dwarf2_write_2byte (info->f, DW_LANG_C_sharp);
	dwarf2_write_string (info->f, info->producer_name);
	dwarf2_write_ref4 (info->f, "debug_lines_b");

	// Methods
	g_hash_table_foreach (info->methods, write_method_func, info);

	// Derived types
	g_hash_table_foreach (info->type_hash, write_class, info);

	dwarf2_write_byte (info->f, 0);
	// DW_TAG_compile_unit ends here

	dwarf2_write_label (info->f, "debug_info_e");

	dwarf2_write_section_end (info->f);
}
