#include <stdlib.h>
#include <string.h>
#include <errno.h>
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
#define ABBREV_ENUM_TYPE		11
#define ABBREV_ENUM_VALUE		12
#define ABBREV_ENUM_VALUE_UNSIGNED	13
#define ABBREV_ENUM_VALUE_SIGNED	14
#define ABBREV_CLASS_TYPE		15
#define ABBREV_CLASS_INHERITANCE	16
#define ABBREV_POINTER_TYPE		17
#define ABBREV_CLASS_METHOD		18
#define ABBREV_CLASS_METHOD_RETVAL	19
#define ABBREV_ARTIFICIAL_PARAMETER	20
#define ABBREV_SIMPLE_ARRAY		21
#define ABBREV_ARRAY			22
#define ABBREV_SUBRANGE			23

// The following constants are defined in the DWARF 2 specification
#define DW_TAG_array_type		0x01
#define DW_TAG_class_type		0x02
#define DW_TAG_enumeration_type		0x04
#define DW_TAG_formal_parameter		0x05
#define DW_TAG_member			0x0d
#define DW_TAG_pointer_type		0x0f
#define DW_TAG_compile_unit		0x11
#define DW_TAG_structure_type		0x13
#define DW_TAG_inheritance		0x1c
#define DW_TAG_subrange_type		0x21
#define DW_TAG_access_declaration	0x23
#define DW_TAG_base_type		0x24
#define DW_TAG_enumerator		0x28
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
#define DW_AT_const_value		0x1c
#define DW_AT_lower_bound		0x22
#define DW_AT_producer			0x25
#define DW_AT_start_scope		0x2c
#define DW_AT_upper_bound		0x2f
#define DW_AT_accessibility		0x32
#define DW_AT_artificial		0x34
#define DW_AT_calling_convention	0x36
#define DW_AT_count			0x37
#define DW_AT_data_member_location	0x38
#define DW_AT_encoding			0x3e
#define DW_AT_external			0x3f
#define DW_AT_type			0x49
#define DW_AT_virtuality		0x4c
#define DW_AT_vtable_elem_location	0x4d

/* Martin Baulig's extensions. */
#define DW_AT_end_scope			0x2121

#define DW_FORM_addr			0x01
#define DW_FORM_block4			0x04
#define DW_FORM_data2			0x05
#define DW_FORM_data4			0x06
#define DW_FORM_data8			0x07
#define DW_FORM_string			0x08
#define DW_FORM_data1			0x0b
#define DW_FORM_flag			0x0c
#define DW_FORM_sdata			0x0d
#define DW_FORM_udata			0x0f
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

#define DW_OP_const1u			0x08
#define DW_OP_const1s			0x09
#define DW_OP_constu			0x10
#define DW_OP_consts			0x11
#define DW_OP_plus			0x22
#define DW_OP_reg0			0x50
#define DW_OP_breg0			0x70
#define DW_OP_fbreg			0x91
#define DW_OP_piece			0x93

#define DW_CC_normal			1
#define DW_CC_program			2
#define DW_CC_nocall			3

#define DW_ACCESS_public		1
#define DW_ACCESS_protected		2
#define DW_ACCESS_private		3

#define DW_VIRTUALITY_none		0
#define DW_VIRTUALITY_virtual		1
#define DW_VIRTUALITY_pure_virtual	2

#define DW_LANG_C_plus_plus		0x04
#define DW_LANG_Java			0x0b
// This is NOT in the standard, we're using Java for the moment. */
#define DW_LANG_C_sharp			DW_LANG_C_plus_plus

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

static void
dwarf2_write_byte (FILE *f, int byte)
{
	fprintf (f, "\t.byte %d\n", byte);
}

static void
dwarf2_write_2byte (FILE *f, int word)
{
	fprintf (f, "\t.word %d\n", word);
}

static void
dwarf2_write_pair (FILE *f, int a, int b)
{
	fprintf (f, "\t.uleb128 %d, %d\n", a, b);
}

static void
dwarf2_write_long (FILE *f, unsigned long value)
{
	fprintf (f, "\t.long %lu\n", value);
}

static void
dwarf2_write_address (FILE *f, void *address)
{
	fprintf (f, "\t.long 0x%lx\n", address);
}

static void
dwarf2_write_string (FILE *f, const char *string)
{
	fprintf (f, "\t.string \"%s\"\n", string);
}

static void
dwarf2_write_sleb128 (FILE *f, long value)
{
	fprintf (f, "\t.sleb128 %ld\n", value);
}

static void
dwarf2_write_uleb128 (FILE *f, unsigned long value)
{
	fprintf (f, "\t.uleb128 %lu\n", value);
}

static void
dwarf2_write_section_start (FILE *f, const char *section)
{
	fprintf (f, "\t.section .%s\n", section);
}

static void
dwarf2_write_label (FILE *f, const char *label)
{
	fprintf (f, ".L_%s:\n", label);
}

static void
dwarf2_write_section_size (FILE *f, const char *start_label, const char *end_label)
{
	fprintf (f, "\t.long .L_%s - .L_%s\n", end_label, start_label);
}

static void
dwarf2_write_ref4 (FILE *f, const char *target_label)
{
	fprintf (f, "\t.long .L_%s\n", target_label);
}

static void
dwarf2_write_type_ref (FILE *f, unsigned long type_index)
{
	fprintf (f, "\t.long .L_TYPE_%lu - .L_debug_info_b\n", type_index);
}

static void
dwarf2_write_type_ptr_ref (FILE *f, unsigned long idx)
{
	fprintf (f, "\t.long .L_TYPE_PTR_%lu - .L_debug_info_b\n", idx);
}

static void
dwarf2_write_relative_ref (FILE *f, const gchar *name, unsigned long idx)
{
	fprintf (f, "\t.long .L_%s_%lu - .L_debug_info_b\n", name, idx);
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
dwarf2_write_dw_lns_set_file (FILE *f, unsigned value)
{
	dwarf2_write_byte (f, DW_LNS_set_file);
	dwarf2_write_uleb128 (f, value + 1);
}

static void
dwarf2_write_dw_lns_negate_stmt (FILE *f)
{
	dwarf2_write_byte (f, DW_LNS_negate_stmt);
}

#if 0 /* never used */
static void
dwarf2_write_dw_lns_set_basic_block (FILE *f)
{
	dwarf2_write_byte (f, DW_LNS_set_basic_block);
}
#endif

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
dwarf2_write_base_type (MonoDebugHandle *debug, int idx,
			int type, int size, const gchar *name)
{
	char buffer [BUFSIZ];

	sprintf (buffer, "TYPE_%d", idx);
	dwarf2_write_label (debug->f, buffer);
	// DW_TAG_basic_type
	dwarf2_write_byte (debug->f, ABBREV_BASE_TYPE);
	dwarf2_write_string (debug->f, name);
	dwarf2_write_byte (debug->f, type);
	dwarf2_write_byte (debug->f, size);
}

static void
dwarf2_write_enum_value (MonoDebugHandle *debug, MonoClass *klass, int idx)
{
	const void *ptr;
	guint32 field_index = idx + klass->field.first;
	guint32 crow;

	crow = mono_metadata_get_constant_index (klass->image, MONO_TOKEN_FIELD_DEF | (field_index + 1));
	if (!crow) {
		dwarf2_write_byte (debug->f, ABBREV_ENUM_VALUE);
		dwarf2_write_string (debug->f, klass->fields [idx].name);
		dwarf2_write_long (debug->f, 0);
		return;
	}

	crow = mono_metadata_decode_row_col (&klass->image->tables [MONO_TABLE_CONSTANT], crow-1,
					     MONO_CONSTANT_VALUE);

	ptr = 1 + mono_metadata_blob_heap (klass->image, crow);

	switch (klass->enum_basetype->type) {
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_U1:
		dwarf2_write_byte (debug->f, ABBREV_ENUM_VALUE_UNSIGNED);
		dwarf2_write_string (debug->f, klass->fields [idx].name);
		dwarf2_write_uleb128 (debug->f, *(guint8 *) ptr);
		break;
	case MONO_TYPE_I1:
		dwarf2_write_byte (debug->f, ABBREV_ENUM_VALUE_SIGNED);
		dwarf2_write_string (debug->f, klass->fields [idx].name);
		dwarf2_write_sleb128 (debug->f, *(gint8 *) ptr);
		break;
	case MONO_TYPE_CHAR:
	case MONO_TYPE_U2:
		dwarf2_write_byte (debug->f, ABBREV_ENUM_VALUE_UNSIGNED);
		dwarf2_write_string (debug->f, klass->fields [idx].name);
		dwarf2_write_uleb128 (debug->f, *(guint16 *) ptr);
		break;
	case MONO_TYPE_I2:
		dwarf2_write_byte (debug->f, ABBREV_ENUM_VALUE_SIGNED);
		dwarf2_write_string (debug->f, klass->fields [idx].name);
		dwarf2_write_sleb128 (debug->f, *(gint16 *) ptr);
		break;
	case MONO_TYPE_U4:
		dwarf2_write_byte (debug->f, ABBREV_ENUM_VALUE_UNSIGNED);
		dwarf2_write_string (debug->f, klass->fields [idx].name);
		dwarf2_write_uleb128 (debug->f, *(guint32 *) ptr);
		break;
	case MONO_TYPE_I4:
		dwarf2_write_byte (debug->f, ABBREV_ENUM_VALUE_SIGNED);
		dwarf2_write_string (debug->f, klass->fields [idx].name);
		dwarf2_write_sleb128 (debug->f, *(gint32 *) ptr);
		break;
	case MONO_TYPE_U8:
		dwarf2_write_byte (debug->f, ABBREV_ENUM_VALUE_UNSIGNED);
		dwarf2_write_string (debug->f, klass->fields [idx].name);
		dwarf2_write_uleb128 (debug->f, *(guint64 *) ptr);
		break;
	case MONO_TYPE_I8:
		dwarf2_write_byte (debug->f, ABBREV_ENUM_VALUE_SIGNED);
		dwarf2_write_string (debug->f, klass->fields [idx].name);
		dwarf2_write_sleb128 (debug->f, *(gint64 *) ptr);
		break;
	default:
		g_assert_not_reached ();
	}
}

static void
dwarf2_write_enum_type (MonoDebugHandle *debug, MonoClass *klass)
{
	int i;

	// DW_TAG_enumeration_type
	dwarf2_write_byte (debug->f, ABBREV_ENUM_TYPE);
	dwarf2_write_string (debug->f, klass->name);
	dwarf2_write_long (debug->f, klass->instance_size - sizeof (MonoObject));

	for (i = 0; i < klass->field.count; i++) {
		if (klass->fields [i].type->attrs & FIELD_ATTRIBUTE_LITERAL)
			dwarf2_write_enum_value (debug, klass, i);
	}

	dwarf2_write_byte (debug->f, 0);
	// DW_TAG_enumeration_type ends here
}

static void
dwarf2_write_class_field (MonoDebugHandle *debug, MonoClass *klass, int idx,
			  int type_index, int start_offset)
{
    MonoClass *subclass = mono_class_from_mono_type (klass->fields [idx].type);
    char start [BUFSIZ], end [BUFSIZ];
    static long label_index = 0;

    // Don't include any static fields, they aren't supported yet.
    // If a struct contains a static field which has the same type as
    // the struct itself, we'd get a recursion loop there.
    if (klass->fields [idx].type->attrs & FIELD_ATTRIBUTE_STATIC)
	    return;

    sprintf (start, "DSF1_%ld", ++label_index);
    sprintf (end, "DSF2_%ld", label_index);

    // DW_TAG_member
    dwarf2_write_byte (debug->f, ABBREV_STRUCT_MEMBER);
    dwarf2_write_string (debug->f, klass->fields [idx].name);
    if (!subclass->valuetype)
	dwarf2_write_type_ptr_ref (debug->f, type_index);
    else
	dwarf2_write_type_ref (debug->f, type_index);

    if (klass->fields [idx].type->attrs & FIELD_ATTRIBUTE_PRIVATE)
	dwarf2_write_byte (debug->f, DW_ACCESS_private);
    else if (klass->fields [idx].type->attrs & FIELD_ATTRIBUTE_FAMILY)
	dwarf2_write_byte (debug->f, DW_ACCESS_protected);
    else
	dwarf2_write_byte (debug->f, DW_ACCESS_public);

    dwarf2_write_section_size (debug->f, start, end);
    dwarf2_write_label (debug->f, start);
    dwarf2_write_byte (debug->f, DW_OP_constu);
    dwarf2_write_uleb128 (debug->f, klass->fields [idx].offset - start_offset);
    dwarf2_write_label (debug->f, end);

    dwarf2_write_long (debug->f, subclass->instance_size);
}

static void
dwarf2_write_class_method (MonoDebugHandle *debug, MonoClass *klass, MonoMethod *method)
{
	MonoType *ret_type = NULL;
	gchar **names;
	int i;

	if (method->signature->ret->type != MONO_TYPE_VOID)
		ret_type = method->signature->ret;

	// DW_TAG_subprogram
	if (ret_type)
		dwarf2_write_byte (debug->f, ABBREV_CLASS_METHOD_RETVAL);
	else
		dwarf2_write_byte (debug->f, ABBREV_CLASS_METHOD);
	dwarf2_write_string (debug->f, method->name);

	if (method->flags & METHOD_ATTRIBUTE_PUBLIC)
		dwarf2_write_byte (debug->f, DW_ACCESS_public);
	else if (method->flags & METHOD_ATTRIBUTE_PRIVATE)
		dwarf2_write_byte (debug->f, DW_ACCESS_private);
	else
		dwarf2_write_byte (debug->f, DW_ACCESS_protected);

	if (method->flags & METHOD_ATTRIBUTE_VIRTUAL)
		dwarf2_write_byte (debug->f, DW_VIRTUALITY_pure_virtual);
	else
		dwarf2_write_byte (debug->f, DW_VIRTUALITY_none);

	dwarf2_write_byte (debug->f, DW_CC_nocall);

	if (ret_type) {
		MonoClass *k = mono_class_from_mono_type (ret_type);
		int type_index = mono_debug_get_type (debug, k);
		dwarf2_write_type_ref (debug->f, type_index);
	}

	if (method->signature->hasthis) {
		int type_index = mono_debug_get_type (debug, klass);

		dwarf2_write_byte (debug->f, ABBREV_ARTIFICIAL_PARAMETER);
		dwarf2_write_string (debug->f, "this");
		dwarf2_write_type_ptr_ref (debug->f, type_index);
		dwarf2_write_byte (debug->f, 1);
	}

	names = g_new (char *, method->signature->param_count);
	mono_method_get_param_names (method, (const char **) names);

	for (i = 0; i < method->signature->param_count; i++) {
		MonoType *subtype = method->signature->params [i];
		MonoClass *subklass = mono_class_from_mono_type (subtype);
		int type_index = mono_debug_get_type (debug, subklass);

		// DW_TAG_formal_parameter
		dwarf2_write_byte (debug->f, ABBREV_FORMAL_PARAMETER);
		dwarf2_write_string (debug->f, names [i]);
		if (subklass->valuetype)
			dwarf2_write_type_ref (debug->f, type_index);
		else
			dwarf2_write_type_ptr_ref (debug->f, type_index);
	}

	g_free (names);

	dwarf2_write_byte (debug->f, 0);
	// DW_TAG_subprogram ends here
}

static void
dwarf2_write_struct_type (MonoDebugHandle *debug, MonoClass *klass)
{
	guint32 *idxs;
	int i;

	idxs = g_new0 (guint32, klass->field.last - klass->field.first + 1);
	for (i = 0; i < klass->field.count; i++) {
		MonoClass *subclass = mono_class_from_mono_type (klass->fields [i].type);
		idxs [i] = mono_debug_get_type (debug, subclass);
	}

	// DW_TAG_structure_type
	dwarf2_write_byte (debug->f, ABBREV_STRUCT_TYPE);
	dwarf2_write_string (debug->f, klass->name);
	dwarf2_write_long (debug->f, klass->instance_size - sizeof (MonoObject));

	for (i = 0; i < klass->field.count; i++)
		dwarf2_write_class_field (debug, klass, i, idxs [i], sizeof (MonoObject));

	dwarf2_write_byte (debug->f, 0);
	// DW_TAG_structure_type ends here

	g_free (idxs);
}

static void
dwarf2_write_class_type (MonoDebugHandle *debug, MonoClass *klass)
{
	guint32 *idxs;
	int i;

	idxs = g_new0 (guint32, klass->field.last - klass->field.first + 1);
	for (i = 0; i < klass->field.count; i++) {
		MonoClass *subclass = mono_class_from_mono_type (klass->fields [i].type);
		idxs [i] = mono_debug_get_type (debug, subclass);
	}

	// DW_TAG_structure_type
	dwarf2_write_byte (debug->f, ABBREV_CLASS_TYPE);
	dwarf2_write_string (debug->f, klass->name);
	dwarf2_write_long (debug->f, klass->instance_size);
	if (klass->flags & TYPE_ATTRIBUTE_PUBLIC)
		dwarf2_write_byte (debug->f, DW_ACCESS_public);
	else
		dwarf2_write_byte (debug->f, DW_ACCESS_private);

	if (klass->parent && klass->parent->byval_arg.type == MONO_TYPE_CLASS) {
		guint32 parent_index = mono_debug_get_type (debug, klass->parent);

		// DW_TAG_inheritance
		dwarf2_write_byte (debug->f, ABBREV_CLASS_INHERITANCE);
		dwarf2_write_type_ref (debug->f, parent_index);
		if (klass->parent->flags & TYPE_ATTRIBUTE_PUBLIC)
			dwarf2_write_byte (debug->f, DW_ACCESS_public);
		else
			dwarf2_write_byte (debug->f, DW_ACCESS_private);
	}

	for (i = 0; i < klass->field.count; i++)
		dwarf2_write_class_field (debug, klass, i, idxs [i], 0);

	for (i = 0; i < klass->method.count; i++) {
		if (!strcmp (klass->methods [i]->name, ".ctor"))
			continue;

		dwarf2_write_class_method (debug, klass, klass->methods [i]);
	}

	dwarf2_write_byte (debug->f, 0);
	// DW_TAG_class_type ends here

	g_free (idxs);
}

static void
dwarf2_write_array (MonoDebugHandle *debug, const gchar *name, MonoClass *element_class,
		    int rank, int idx)
{
	unsigned long uint32_index = mono_debug_get_type (debug, mono_defaults.uint32_class);
	char buffer [BUFSIZ];
	MonoArray array;

	dwarf2_write_byte (debug->f, ABBREV_STRUCT_TYPE);
	dwarf2_write_string (debug->f, name);
	dwarf2_write_long (debug->f, sizeof (MonoArray));

	// DW_TAG_structure_type
	dwarf2_write_byte (debug->f, ABBREV_STRUCT_MEMBER);
	dwarf2_write_string (debug->f, "max_length");
	dwarf2_write_type_ref (debug->f, uint32_index);
	dwarf2_write_byte (debug->f, DW_ACCESS_public);
	dwarf2_write_long (debug->f, 2);
	dwarf2_write_byte (debug->f, DW_OP_const1u);
	dwarf2_write_byte (debug->f, (guchar *) &array.max_length - (guchar *) &array);
	dwarf2_write_long (debug->f, 4);

	dwarf2_write_byte (debug->f, ABBREV_STRUCT_MEMBER);
	dwarf2_write_string (debug->f, "bounds");
	dwarf2_write_relative_ref (debug->f, "ARRAY_BOUNDS_PTR", idx);
	dwarf2_write_byte (debug->f, DW_ACCESS_public);
	dwarf2_write_long (debug->f, 2);
	dwarf2_write_byte (debug->f, DW_OP_const1u);
	dwarf2_write_byte (debug->f, (guchar *) &array.bounds - (guchar *) &array);
	dwarf2_write_long (debug->f, 4);

	dwarf2_write_byte (debug->f, ABBREV_STRUCT_MEMBER);
	dwarf2_write_string (debug->f, "vector");
	dwarf2_write_relative_ref (debug->f, "ARRAY_PTR", idx);
	dwarf2_write_byte (debug->f, DW_ACCESS_public);
	dwarf2_write_long (debug->f, 2);
	dwarf2_write_byte (debug->f, DW_OP_const1u);
	dwarf2_write_byte (debug->f, (guchar *) &array.vector - (guchar *) &array);
	dwarf2_write_long (debug->f, 4);

	dwarf2_write_byte (debug->f, 0);
	// DW_TAG_structure_type ends here

	sprintf (buffer, "ARRAY_BOUNDS_PTR_%u", idx);
	dwarf2_write_label (debug->f, buffer);

	// DW_TAG_pointer_type
	dwarf2_write_byte (debug->f, ABBREV_POINTER_TYPE);
	dwarf2_write_relative_ref (debug->f, "ARRAY_BOUNDS", idx);

	sprintf (buffer, "ARRAY_BOUNDS_%u", idx);
	dwarf2_write_label (debug->f, buffer);

	// DW_TAG_array_type
	dwarf2_write_byte (debug->f, ABBREV_ARRAY);
	dwarf2_write_string (debug->f, name);
	dwarf2_write_type_ref (debug->f, uint32_index);
	dwarf2_write_long (debug->f, rank * 2);

	// DW_TAG_subrange_type
	dwarf2_write_byte (debug->f, ABBREV_SUBRANGE);
	dwarf2_write_long (debug->f, 0);
	dwarf2_write_long (debug->f, rank-1);
	dwarf2_write_long (debug->f, rank);

	// DW_TAG_subrange_type
	dwarf2_write_byte (debug->f, ABBREV_SUBRANGE);
	dwarf2_write_long (debug->f, 0);
	dwarf2_write_long (debug->f, 1);
	dwarf2_write_long (debug->f, 2);

	dwarf2_write_byte (debug->f, 0);
	// DW_TAG_array_type ends here

	sprintf (buffer, "ARRAY_PTR_%u", idx);
	dwarf2_write_label (debug->f, buffer);

	// DW_TAG_array_type
	dwarf2_write_byte (debug->f, ABBREV_SIMPLE_ARRAY);
	dwarf2_write_string (debug->f, name);
	if (element_class->valuetype)
		dwarf2_write_type_ref (debug->f, mono_debug_get_type (debug, element_class));
	else
		dwarf2_write_type_ptr_ref (debug->f, mono_debug_get_type (debug, element_class));
}

static void
dwarf2_write_array_type (MonoDebugHandle *debug, MonoClass *klass, int idx)
{
	char buffer [BUFSIZ], *name;
	int i;

	buffer[0] = '\0';
	for (i = 0; i < klass->rank; i++)
		strcat (buffer, "[]");

	name = g_strdup_printf ("%s%s", klass->element_class->name, buffer);

	dwarf2_write_array (debug, name, klass->element_class, klass->rank, idx);

	g_free (name);
}

static void
dwarf2_write_string_type (MonoDebugHandle *debug, MonoClass *klass, int idx)
{
	unsigned long uint32_index = mono_debug_get_type (debug, mono_defaults.uint32_class);
	char buffer [BUFSIZ];
	MonoString string;

	// DW_TAG_structure_type
	dwarf2_write_byte (debug->f, ABBREV_STRUCT_TYPE);
	dwarf2_write_string (debug->f, klass->name);
	dwarf2_write_long (debug->f, sizeof (MonoString));

	dwarf2_write_byte (debug->f, ABBREV_STRUCT_MEMBER);
	dwarf2_write_string (debug->f, "length");
	dwarf2_write_type_ref (debug->f, uint32_index);
	dwarf2_write_byte (debug->f, DW_ACCESS_public);
	dwarf2_write_long (debug->f, 2);
	dwarf2_write_byte (debug->f, DW_OP_const1u);
	dwarf2_write_byte (debug->f, (guchar *) &string.length - (guchar *) &string);
	dwarf2_write_long (debug->f, 4);

	dwarf2_write_byte (debug->f, ABBREV_STRUCT_MEMBER);
	dwarf2_write_string (debug->f, "chars");
	dwarf2_write_relative_ref (debug->f, "CHARS", idx);
	dwarf2_write_byte (debug->f, DW_ACCESS_public);
	dwarf2_write_long (debug->f, 2);
	dwarf2_write_byte (debug->f, DW_OP_const1u);
	dwarf2_write_byte (debug->f, (guchar *) &string.chars - (guchar *) &string);
	dwarf2_write_long (debug->f, 4);

	dwarf2_write_byte (debug->f, 0);
	// DW_TAG_structure_type ends here

	sprintf (buffer, "CHARS_%u", idx);
	dwarf2_write_label (debug->f, buffer);

	dwarf2_write_byte (debug->f, ABBREV_SIMPLE_ARRAY);
	dwarf2_write_string (debug->f, "Char[]");
	dwarf2_write_type_ref (debug->f, mono_debug_get_type (debug, mono_defaults.char_class));
}

static void
dwarf2_write_class (MonoDebugHandle *debug, MonoClass *klass, int idx)
{
	char buffer [BUFSIZ];
	int print = 0;

	if (!strncmp (klass->name, "My", 2)) {
		g_message (G_STRLOC ": %s - %s - %x", klass->name_space, klass->name, klass->flags);
		print = 1;
		// G_BREAKPOINT ();
	}

	if (!klass->valuetype) {
		sprintf (buffer, "TYPE_PTR_%u", idx);
		dwarf2_write_label (debug->f, buffer);

		// DW_TAG_pointer_type
		dwarf2_write_byte (debug->f, ABBREV_POINTER_TYPE);
		dwarf2_write_type_ref (debug->f, idx);
	}

	sprintf (buffer, "TYPE_%u", idx);
	dwarf2_write_label (debug->f, buffer);

	if (klass->enumtype) {
		dwarf2_write_enum_type (debug, klass);
		return;
	}

	switch (klass->byval_arg.type) {
	case MONO_TYPE_VALUETYPE:
		dwarf2_write_struct_type (debug, klass);
		break;
	case MONO_TYPE_CLASS:
		dwarf2_write_class_type (debug, klass);
		break;
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:
		dwarf2_write_array_type (debug, klass, idx);
		break;
	case MONO_TYPE_STRING:
		dwarf2_write_string_type (debug, klass, idx);
		break;
	default:
#if 0
		g_message (G_STRLOC ": %s.%s - 0x%x - 0x%x", klass->name_space, klass->name,
			   klass->byval_arg.type, klass->flags);
#endif

		// DW_TAG_basic_type
		dwarf2_write_byte (debug->f, ABBREV_BASE_TYPE);
		dwarf2_write_string (debug->f, klass->name);
		dwarf2_write_byte (debug->f, DW_ATE_address);
		dwarf2_write_byte (debug->f, 0);
		break;
	}
}

static void
dwarf2_write_variable_location (MonoDebugHandle *debug, MonoDebugVarInfo *var)
{
	switch (var->index & MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS) {
	case MONO_DEBUG_VAR_ADDRESS_MODE_STACK:
		/*
		 * Variable is on the stack.
		 *
		 * If `index' is zero, use the normal frame register.  Otherwise, bits
		 * 0..4 of `index' contain the frame register.
		 *
		 */

		if (!var->index)
			/* Use the normal frame register (%ebp on the i386). */
			dwarf2_write_byte (debug->f, DW_OP_fbreg);
		else
			/* Use a custom frame register. */
			dwarf2_write_byte (debug->f, DW_OP_breg0 + (var->index & 0x001f));
		dwarf2_write_sleb128 (debug->f, var->offset);
		break;

	case MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER:
		/*
		 * Variable is in the register whose number is contained in bits 0..4
		 * of `index'.
		 *
		 */
		dwarf2_write_byte (debug->f, DW_OP_reg0 + (var->index & 0x001f));
		if (var->offset) {
			dwarf2_write_byte (debug->f, DW_OP_consts);
			dwarf2_write_sleb128 (debug->f, var->offset);
			dwarf2_write_byte (debug->f, DW_OP_plus);
		}
		break;

	case MONO_DEBUG_VAR_ADDRESS_MODE_TWO_REGISTERS:
		/*
		 * Variable is in two registers whose numbers are in bits 0..4 and 5..9 of 
		 * the `index' field.
		 */
		dwarf2_write_byte (debug->f, DW_OP_reg0 + (var->index & 0x001f));
		dwarf2_write_byte (debug->f, DW_OP_piece);
		dwarf2_write_byte (debug->f, sizeof (int));

		dwarf2_write_byte (debug->f, DW_OP_reg0 + ((var->index & 0x1f0) >> 5));
		dwarf2_write_byte (debug->f, DW_OP_piece);
		dwarf2_write_byte (debug->f, sizeof (int));

		break;

	default:
		g_assert_not_reached ();
	}
}

static void
dwarf2_write_parameter (MonoDebugHandle *debug, DebugMethodInfo *minfo, const gchar *name,
			MonoDebugVarInfo *var, MonoClass *klass)
{
	static long label_index = 0;
	int type_index = mono_debug_get_type (debug, klass);
	char start [BUFSIZ], end [BUFSIZ];

	sprintf (start, "DT1_%ld", ++label_index);
	sprintf (end, "DT2_%ld", label_index);
		
	// DW_TAG_format_parameter
	dwarf2_write_byte (debug->f, ABBREV_PARAMETER);
	dwarf2_write_string (debug->f, name);
	if (klass->valuetype)
		dwarf2_write_type_ref (debug->f, type_index);
	else
		dwarf2_write_type_ptr_ref (debug->f, type_index);
	dwarf2_write_section_size (debug->f, start, end);
	dwarf2_write_label (debug->f, start);
	dwarf2_write_variable_location (debug, var);
	dwarf2_write_label (debug->f, end);
	dwarf2_write_long (debug->f, minfo->method_info.prologue_end);
}

static void
dwarf2_write_variable (MonoDebugHandle *debug, DebugMethodInfo *minfo, const gchar *name,
		       MonoDebugVarInfo *var, MonoClass *klass)
{
	static long label_index = 0;
	int type_index = mono_debug_get_type (debug, klass);
	char start [BUFSIZ], end [BUFSIZ];

	sprintf (start, "DT3_%ld", ++label_index);
	sprintf (end, "DT4_%ld", label_index);
		
	// DW_TAG_formal_parameter
	dwarf2_write_byte (debug->f, ABBREV_LOCAL_VARIABLE);
	dwarf2_write_string (debug->f, name);
	if (klass->valuetype)
		dwarf2_write_type_ref (debug->f, type_index);
	else
		dwarf2_write_type_ptr_ref (debug->f, type_index);
	dwarf2_write_section_size (debug->f, start, end);
	dwarf2_write_label (debug->f, start);
	dwarf2_write_variable_location (debug, var);
	dwarf2_write_label (debug->f, end);
	dwarf2_write_address (debug->f, minfo->method_info.code_start + var->begin_scope);
	dwarf2_write_address (debug->f, minfo->method_info.code_start + var->end_scope);
}

static void 
write_method_lines_dwarf2 (MonoDebugHandle *debug, DebugMethodInfo *minfo)
{
	guint32 st_line = 0;
	gpointer st_address = 0;
	int i;

	if (!minfo->line_numbers)
		return;

	// Start of statement program
	dwarf2_write_dw_lns_set_file (debug->f, minfo->source_file);
	dwarf2_write_dw_lns_advance_line (debug->f, minfo->start_line - 1);
	dwarf2_write_dw_lne_set_address (debug->f, minfo->method_info.code_start);
	dwarf2_write_dw_lns_negate_stmt (debug->f);
	dwarf2_write_dw_lns_copy (debug->f);

	st_line = minfo->start_line;
	st_address = minfo->method_info.code_start;

	for (i = 1; i < minfo->line_numbers->len; i++) {
		DebugLineNumberInfo *lni = g_ptr_array_index (minfo->line_numbers, i);
		gint32 line_inc, addr_inc, opcode;
		int used_standard_opcode = 0;

		line_inc = lni->line - st_line;
		addr_inc = (char *)lni->address - (char *)st_address;

		if (addr_inc < 0) {
			dwarf2_write_dw_lne_set_address (debug->f, lni->address);
			used_standard_opcode = 1;
		} else if (addr_inc && !line_inc) {
			dwarf2_write_dw_lns_advance_pc (debug->f, addr_inc);
			used_standard_opcode = 1;
		}

		if ((line_inc < 0) || (line_inc >= line_range)) {
			dwarf2_write_dw_lns_advance_pc (debug->f, addr_inc);
			dwarf2_write_dw_lns_advance_line (debug->f, line_inc);
			used_standard_opcode = 1;
		} else if (line_inc > 0) {
			opcode = (line_inc - 1) + (line_range * addr_inc) + opcode_base;
			g_assert (opcode >= 0);

			if (opcode >= 256) {
				dwarf2_write_dw_lns_advance_pc (debug->f, addr_inc);
				dwarf2_write_dw_lns_advance_line (debug->f, line_inc);
				used_standard_opcode = 1;
			} else
				dwarf2_write_byte (debug->f, opcode);
		}

		if (used_standard_opcode)
			dwarf2_write_dw_lns_copy (debug->f);

		st_line += line_inc;
		st_address = (char *)st_address + addr_inc;
	}

	dwarf2_write_dw_lne_set_address (debug->f,
					 (char *)minfo->method_info.code_start +
					 minfo->method_info.epilogue_begin);
	dwarf2_write_dw_lns_advance_line (debug->f, minfo->last_line - st_line);
	dwarf2_write_dw_lns_copy (debug->f);

	dwarf2_write_dw_lns_copy (debug->f);
	dwarf2_write_dw_lne_end_sequence (debug->f);
}

static void
write_method_lines_func (gpointer key, gpointer value, gpointer user_data)
{
	write_method_lines_dwarf2 (user_data, value);
}

static void
write_line_numbers (MonoDebugHandle *debug)
{
	GList *tmp;

	/* State machine registers. */
	int i;
	
	// Line number information.
	dwarf2_write_section_start (debug->f, "debug_line");
	dwarf2_write_label (debug->f, "debug_line_b");
	dwarf2_write_section_size (debug->f, "DL1", "debug_line_e");
	dwarf2_write_label (debug->f, "DL1");
	dwarf2_write_2byte (debug->f, 2);
	dwarf2_write_section_size (debug->f, "DL2", "DL3");
	dwarf2_write_label (debug->f, "DL2");
	// minimum instruction length
	dwarf2_write_byte (debug->f, 1);
	// default is statement
	dwarf2_write_byte (debug->f, 1);
	// line base
	dwarf2_write_byte (debug->f, line_base);
	// line range
	dwarf2_write_byte (debug->f, line_range);
	// opcode base
	dwarf2_write_byte (debug->f, opcode_base);
	// standard opcode sizes
	for (i = 1; i < opcode_base; i++)
		dwarf2_write_byte (debug->f, standard_opcode_sizes [i]);
	// include directories
	dwarf2_write_byte (debug->f, 0);
	// file names
	for (i = 0; i < debug->source_files->len; i++) {
		gchar *source_file = g_ptr_array_index (debug->source_files, i);
		dwarf2_write_string (debug->f, source_file);
		dwarf2_write_uleb128 (debug->f, 0);
		dwarf2_write_uleb128 (debug->f, 0);
		dwarf2_write_uleb128 (debug->f, 0);
	}
	// end of list
	dwarf2_write_byte (debug->f, 0);
	dwarf2_write_label (debug->f, "DL3");

	for (tmp = debug->info; tmp; tmp = tmp->next) {
		AssemblyDebugInfo *info = (AssemblyDebugInfo*)tmp->data;

		g_hash_table_foreach (info->methods, write_method_lines_func, debug);
	}

	dwarf2_write_label (debug->f, "debug_line_e");
}

static void
write_class_dwarf2 (MonoDebugHandle *debug, MonoClass *klass, guint idx)
{
	switch (klass->byval_arg.type) {
	case MONO_TYPE_VOID:
		dwarf2_write_base_type (debug, idx, DW_ATE_unsigned, 0, "Void");
		break;
	case MONO_TYPE_BOOLEAN:
		dwarf2_write_base_type (debug, idx, DW_ATE_boolean, 1, "Boolean");
		break;
	case MONO_TYPE_CHAR:
		dwarf2_write_base_type (debug, idx, DW_ATE_unsigned_char, 2, "Char");
		break;
	case MONO_TYPE_I1:
		dwarf2_write_base_type (debug, idx, DW_ATE_signed, 1, "SByte");
		break;
	case MONO_TYPE_U1:
		dwarf2_write_base_type (debug, idx, DW_ATE_unsigned, 1, "Byte");
		break;
	case MONO_TYPE_I2:
		dwarf2_write_base_type (debug, idx, DW_ATE_signed, 2, "Int16");
		break;
	case MONO_TYPE_U2:
		dwarf2_write_base_type (debug, idx, DW_ATE_unsigned, 2, "UInt16");
		break;
	case MONO_TYPE_I4:
		dwarf2_write_base_type (debug, idx, DW_ATE_signed, 4, "Int32");
		break;
	case MONO_TYPE_U4:
		dwarf2_write_base_type (debug, idx, DW_ATE_unsigned, 4, "UInt32");
		break;
	case MONO_TYPE_I8:
		dwarf2_write_base_type (debug, idx, DW_ATE_signed, 8, "Int64");
		break;
	case MONO_TYPE_U8:
		dwarf2_write_base_type (debug, idx, DW_ATE_unsigned, 8, "UInt64");
		break;
	case MONO_TYPE_R4:
		dwarf2_write_base_type (debug, idx, DW_ATE_float, 4, "Float");
		break;
	case MONO_TYPE_R8:
		dwarf2_write_base_type (debug, idx, DW_ATE_float, 8, "Double");
		break;
	default:
		dwarf2_write_class (debug, klass, idx);
		break;
	}
}

static void
write_class (gpointer key, gpointer value, gpointer user_data)
{
	write_class_dwarf2 (user_data, key, GPOINTER_TO_INT (value));
}

static void
write_method_dwarf2 (MonoDebugHandle *debug, DebugMethodInfo *minfo)
{
	int is_external = 0, i;
	MonoType *ret_type = NULL;
	gchar **names;

	if (minfo->method_info.method->signature->ret->type != MONO_TYPE_VOID)
		ret_type = minfo->method_info.method->signature->ret;

	// DW_TAG_subprogram
	if (ret_type)
		dwarf2_write_byte (debug->f, ABBREV_SUBPROGRAM_RETVAL);
	else
		dwarf2_write_byte (debug->f, ABBREV_SUBPROGRAM);
	dwarf2_write_string (debug->f, minfo->name);
	dwarf2_write_byte (debug->f, is_external);
	dwarf2_write_address (debug->f, minfo->method_info.code_start);
	dwarf2_write_address (debug->f, (char *)minfo->method_info.code_start + minfo->method_info.code_size);
	dwarf2_write_byte (debug->f, DW_CC_nocall);
	if (ret_type) {
		MonoClass *klass = mono_class_from_mono_type (ret_type);
		int type_index = mono_debug_get_type (debug, klass);
		dwarf2_write_type_ref (debug->f, type_index);
	}

	if (minfo->method_info.method->signature->hasthis)
		dwarf2_write_parameter (debug, minfo, "this", minfo->method_info.this_var,
					minfo->method_info.method->klass);

	names = g_new (char *, minfo->method_info.method->signature->param_count);
	mono_method_get_param_names (minfo->method_info.method, (const char **) names);

	for (i = 0; i < minfo->method_info.num_params; i++) {
		MonoType *type = minfo->method_info.method->signature->params [i];
		MonoClass *klass = mono_class_from_mono_type (type);

		dwarf2_write_parameter (debug, minfo, names [i], &minfo->method_info.params [i], klass);
	}

	g_free (names);

	for (i = 0; i < minfo->method_info.num_locals; i++) {
		MonoMethodHeader *header = ((MonoMethodNormal*) minfo->method_info.method)->header;
		MonoClass *klass = mono_class_from_mono_type (header->locals [i]);
		char name [BUFSIZ];

		sprintf (name, "V_%d", i);
		dwarf2_write_variable (debug, minfo, name, &minfo->method_info.locals [i], klass);
	}

	dwarf2_write_byte (debug->f, 0);
	// DW_TAG_subprogram ends here
}

static void
write_method_func (gpointer key, gpointer value, gpointer user_data)
{
	write_method_dwarf2 (user_data, value);
}

void
mono_debug_write_dwarf2 (MonoDebugHandle *debug)
{
	GList *tmp;

	if (!(debug->f = fopen (debug->filename, "w"))) {
		g_warning ("Can't create dwarf file `%s': %s", debug->filename, g_strerror (errno)); 
		return;
	}

	// Produce assembler code which is free of comments and extra whitespaces.
	fprintf (debug->f, "#NOAPP\n");

	// DWARF 2 Abbreviation table.
	dwarf2_write_section_start (debug->f, "debug_abbrev");
	dwarf2_write_label (debug->f, "debug_abbrev");

	dwarf2_write_byte (debug->f, ABBREV_COMPILE_UNIT);
	dwarf2_write_byte (debug->f, DW_TAG_compile_unit);
	dwarf2_write_byte (debug->f, DW_CHILDREN_yes);
	dwarf2_write_pair (debug->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (debug->f, DW_AT_language, DW_FORM_data2);
	dwarf2_write_pair (debug->f, DW_AT_producer, DW_FORM_string);
	dwarf2_write_pair (debug->f, DW_AT_stmt_list, DW_FORM_ref4);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_SUBPROGRAM);
	dwarf2_write_byte (debug->f, DW_TAG_subprogram);
	dwarf2_write_byte (debug->f, DW_CHILDREN_yes);
	dwarf2_write_pair (debug->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (debug->f, DW_AT_external, DW_FORM_flag);
	dwarf2_write_pair (debug->f, DW_AT_low_pc, DW_FORM_addr);
	dwarf2_write_pair (debug->f, DW_AT_high_pc, DW_FORM_addr);
	dwarf2_write_pair (debug->f, DW_AT_calling_convention, DW_FORM_data1);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_SUBPROGRAM_RETVAL);
	dwarf2_write_byte (debug->f, DW_TAG_subprogram);
	dwarf2_write_byte (debug->f, DW_CHILDREN_yes);
	dwarf2_write_pair (debug->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (debug->f, DW_AT_external, DW_FORM_flag);
	dwarf2_write_pair (debug->f, DW_AT_low_pc, DW_FORM_addr);
	dwarf2_write_pair (debug->f, DW_AT_high_pc, DW_FORM_addr);
	dwarf2_write_pair (debug->f, DW_AT_calling_convention, DW_FORM_data1);
	dwarf2_write_pair (debug->f, DW_AT_type, DW_FORM_ref4);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_BASE_TYPE);
	dwarf2_write_byte (debug->f, DW_TAG_base_type);
	dwarf2_write_byte (debug->f, DW_CHILDREN_no);
	dwarf2_write_pair (debug->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (debug->f, DW_AT_encoding, DW_FORM_data1);
	dwarf2_write_pair (debug->f, DW_AT_byte_size, DW_FORM_data1);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_FORMAL_PARAMETER);
	dwarf2_write_byte (debug->f, DW_TAG_formal_parameter);
	dwarf2_write_byte (debug->f, DW_CHILDREN_no);
	dwarf2_write_pair (debug->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (debug->f, DW_AT_type, DW_FORM_ref4);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_ARTIFICIAL_PARAMETER);
	dwarf2_write_byte (debug->f, DW_TAG_formal_parameter);
	dwarf2_write_byte (debug->f, DW_CHILDREN_no);
	dwarf2_write_pair (debug->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (debug->f, DW_AT_type, DW_FORM_ref4);
	dwarf2_write_pair (debug->f, DW_AT_artificial, DW_FORM_data1);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_PARAMETER);
	dwarf2_write_byte (debug->f, DW_TAG_formal_parameter);
	dwarf2_write_byte (debug->f, DW_CHILDREN_no);
	dwarf2_write_pair (debug->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (debug->f, DW_AT_type, DW_FORM_ref4);
	dwarf2_write_pair (debug->f, DW_AT_location, DW_FORM_block4);
	dwarf2_write_pair (debug->f, DW_AT_start_scope, DW_FORM_data4);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_LOCAL_VARIABLE);
	dwarf2_write_byte (debug->f, DW_TAG_variable);
	dwarf2_write_byte (debug->f, DW_CHILDREN_no);
	dwarf2_write_pair (debug->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (debug->f, DW_AT_type, DW_FORM_ref4);
	dwarf2_write_pair (debug->f, DW_AT_location, DW_FORM_block4);
	dwarf2_write_pair (debug->f, DW_AT_start_scope, DW_FORM_addr);
	dwarf2_write_pair (debug->f, DW_AT_end_scope, DW_FORM_addr);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_STRUCT_TYPE);
	dwarf2_write_byte (debug->f, DW_TAG_structure_type);
	dwarf2_write_byte (debug->f, DW_CHILDREN_yes);
	dwarf2_write_pair (debug->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (debug->f, DW_AT_byte_size, DW_FORM_data4);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_STRUCT_MEMBER);
	dwarf2_write_byte (debug->f, DW_TAG_member);
	dwarf2_write_byte (debug->f, DW_CHILDREN_no);
	dwarf2_write_pair (debug->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (debug->f, DW_AT_type, DW_FORM_ref4);
	dwarf2_write_pair (debug->f, DW_AT_accessibility, DW_FORM_data1);
	dwarf2_write_pair (debug->f, DW_AT_data_member_location, DW_FORM_block4);
	dwarf2_write_pair (debug->f, DW_AT_byte_size, DW_FORM_data4);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_STRUCT_ACCESS);
	dwarf2_write_byte (debug->f, DW_TAG_access_declaration);
	dwarf2_write_byte (debug->f, DW_CHILDREN_no);
	dwarf2_write_pair (debug->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (debug->f, DW_AT_accessibility, DW_FORM_data1);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_ENUM_TYPE);
	dwarf2_write_byte (debug->f, DW_TAG_enumeration_type);
	dwarf2_write_byte (debug->f, DW_CHILDREN_yes);
	dwarf2_write_pair (debug->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (debug->f, DW_AT_byte_size, DW_FORM_data4);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_ENUM_VALUE);
	dwarf2_write_byte (debug->f, DW_TAG_enumerator);
	dwarf2_write_byte (debug->f, DW_CHILDREN_no);
	dwarf2_write_pair (debug->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (debug->f, DW_AT_const_value, DW_FORM_data4);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_ENUM_VALUE_UNSIGNED);
	dwarf2_write_byte (debug->f, DW_TAG_enumerator);
	dwarf2_write_byte (debug->f, DW_CHILDREN_no);
	dwarf2_write_pair (debug->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (debug->f, DW_AT_const_value, DW_FORM_udata);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_ENUM_VALUE_SIGNED);
	dwarf2_write_byte (debug->f, DW_TAG_enumerator);
	dwarf2_write_byte (debug->f, DW_CHILDREN_no);
	dwarf2_write_pair (debug->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (debug->f, DW_AT_const_value, DW_FORM_sdata);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_CLASS_TYPE);
	dwarf2_write_byte (debug->f, DW_TAG_class_type);
	dwarf2_write_byte (debug->f, DW_CHILDREN_yes);
	dwarf2_write_pair (debug->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (debug->f, DW_AT_byte_size, DW_FORM_data4);
	dwarf2_write_pair (debug->f, DW_AT_accessibility, DW_FORM_data1);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_CLASS_INHERITANCE);
	dwarf2_write_byte (debug->f, DW_TAG_inheritance);
	dwarf2_write_byte (debug->f, DW_CHILDREN_no);
	dwarf2_write_pair (debug->f, DW_AT_type, DW_FORM_ref4);
	dwarf2_write_pair (debug->f, DW_AT_accessibility, DW_FORM_data1);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_POINTER_TYPE);
	dwarf2_write_byte (debug->f, DW_TAG_pointer_type);
	dwarf2_write_byte (debug->f, DW_CHILDREN_no);
	dwarf2_write_pair (debug->f, DW_AT_type, DW_FORM_ref4);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_CLASS_METHOD);
	dwarf2_write_byte (debug->f, DW_TAG_subprogram);
	dwarf2_write_byte (debug->f, DW_CHILDREN_yes);
	dwarf2_write_pair (debug->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (debug->f, DW_AT_accessibility, DW_FORM_data1);
	dwarf2_write_pair (debug->f, DW_AT_virtuality, DW_FORM_data1);
	dwarf2_write_pair (debug->f, DW_AT_calling_convention, DW_FORM_data1);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_CLASS_METHOD_RETVAL);
	dwarf2_write_byte (debug->f, DW_TAG_subprogram);
	dwarf2_write_byte (debug->f, DW_CHILDREN_yes);
	dwarf2_write_pair (debug->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (debug->f, DW_AT_accessibility, DW_FORM_data1);
	dwarf2_write_pair (debug->f, DW_AT_virtuality, DW_FORM_data1);
	dwarf2_write_pair (debug->f, DW_AT_calling_convention, DW_FORM_data1);
	dwarf2_write_pair (debug->f, DW_AT_type, DW_FORM_ref4);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_SIMPLE_ARRAY);
	dwarf2_write_byte (debug->f, DW_TAG_array_type);
	dwarf2_write_byte (debug->f, DW_CHILDREN_no);
	dwarf2_write_pair (debug->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (debug->f, DW_AT_type, DW_FORM_ref4);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_ARRAY);
	dwarf2_write_byte (debug->f, DW_TAG_array_type);
	dwarf2_write_byte (debug->f, DW_CHILDREN_yes);
	dwarf2_write_pair (debug->f, DW_AT_name, DW_FORM_string);
	dwarf2_write_pair (debug->f, DW_AT_type, DW_FORM_ref4);
	dwarf2_write_pair (debug->f, DW_AT_byte_size, DW_FORM_data4);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_byte (debug->f, ABBREV_SUBRANGE);
	dwarf2_write_byte (debug->f, DW_TAG_subrange_type);
	dwarf2_write_byte (debug->f, DW_CHILDREN_no);
	dwarf2_write_pair (debug->f, DW_AT_lower_bound, DW_FORM_data4);
	dwarf2_write_pair (debug->f, DW_AT_upper_bound, DW_FORM_data4);
	dwarf2_write_pair (debug->f, DW_AT_count, DW_FORM_data4);
	dwarf2_write_pair (debug->f, 0, 0);

	dwarf2_write_label (debug->f, "debug_abbrev_e");

	// Line numbers
	write_line_numbers (debug);

	// Compile unit header
	dwarf2_write_section_start (debug->f, "debug_info");
	dwarf2_write_label (debug->f, "debug_info_b");
	dwarf2_write_section_size (debug->f, "DI1", "debug_info_e");
	dwarf2_write_label (debug->f, "DI1");
	dwarf2_write_2byte (debug->f, 2);
	dwarf2_write_ref4 (debug->f, "debug_abbrev_b");
	dwarf2_write_byte (debug->f, sizeof (gpointer));

	// DW_TAG_compile_unit
	dwarf2_write_byte (debug->f, ABBREV_COMPILE_UNIT);
	dwarf2_write_string (debug->f, debug->name);
	dwarf2_write_2byte (debug->f, DW_LANG_C_sharp);
	dwarf2_write_string (debug->f, debug->producer_name);
	dwarf2_write_ref4 (debug->f, "debug_lines_b");

	// Methods
	for (tmp = debug->info; tmp; tmp = tmp->next) {
		AssemblyDebugInfo *info = (AssemblyDebugInfo*)tmp->data;

		g_hash_table_foreach (info->methods, write_method_func, debug);
	}

	// Derived types
	g_hash_table_foreach (debug->type_hash, write_class, debug);

	dwarf2_write_byte (debug->f, 0);
	// DW_TAG_compile_unit ends here

	dwarf2_write_label (debug->f, "debug_info_e");

	fclose (debug->f);
	debug->f = NULL;

	if (!(debug->flags & MONO_DEBUG_FLAGS_DONT_ASSEMBLE)) {
		char *buf;

		/* yes, it's completely unsafe */
		buf = g_strdup_printf ("as %s -o %s", debug->filename, debug->objfile);
		system (buf);
		g_free (buf);
	}
}
