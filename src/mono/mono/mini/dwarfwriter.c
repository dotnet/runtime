/**
 * \file
 * Creation of DWARF debug information
 *
 * Author:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2008-2009 Novell, Inc.
 */

#include "config.h"
#include <mono/utils/mono-compiler.h>

#if !defined(DISABLE_AOT) && !defined(DISABLE_JIT)
#include "dwarfwriter.h"

#include <sys/types.h>
#include <ctype.h>
#include <string.h>
#ifdef HAVE_STDINT_H
#include <stdint.h>
#endif

#include <mono/metadata/mono-endian.h>
#include <mono/metadata/debug-internals.h>
#include <mono/metadata/abi-details.h>

#ifndef HOST_WIN32
#include <mono/utils/freebsd-elf32.h>
#include <mono/utils/freebsd-elf64.h>
#endif

#include <mono/utils/freebsd-dwarf.h>

#define DW_AT_MIPS_linkage_name 0x2007
#define DW_LNE_set_prologue_end 0x0a

typedef struct {
	MonoMethod *method;
	char *start_symbol, *end_symbol;
	guint8 *code;
	guint32 code_size;
} MethodLineNumberInfo;

struct _MonoDwarfWriter
{
	MonoImageWriter *w;
	GHashTable *class_to_die, *class_to_vtype_die, *class_to_pointer_die;
	GHashTable *class_to_reference_die;
	int fde_index, tdie_index, line_number_file_index, line_number_dir_index;
	GHashTable *file_to_index, *index_to_file, *dir_to_index;
	FILE *il_file;
	int il_file_line_index, loclist_index;
	GSList *cie_program;
	FILE *fp;
	const char *temp_prefix;
	gboolean emit_line;
	GSList *line_info;
	int cur_file_index;
};

static void
emit_line_number_info (MonoDwarfWriter *w, MonoMethod *method,
					   char *start_symbol, char *end_symbol,
					   guint8 *code, guint32 code_size,
					   MonoDebugMethodJitInfo *debug_info);

/*
 * mono_dwarf_writer_create:
 *
 *   Create a DWARF writer object. WRITER is the underlying image writer this
 * writer will emit to. IL_FILE is the file where IL code will be dumped to for
 * methods which have no line number info. It can be NULL.
 */
MonoDwarfWriter*
mono_dwarf_writer_create (MonoImageWriter *writer, FILE *il_file, int il_file_start_line, gboolean emit_line_numbers)
{
	MonoDwarfWriter *w = g_new0 (MonoDwarfWriter, 1);

	w->w = writer;
	w->il_file = il_file;
	w->il_file_line_index = il_file_start_line;

	w->emit_line = emit_line_numbers;

	w->fp = mono_img_writer_get_fp (w->w);
	w->temp_prefix = mono_img_writer_get_temp_label_prefix (w->w);

	w->class_to_die = g_hash_table_new (NULL, NULL);
	w->class_to_vtype_die = g_hash_table_new (NULL, NULL);
	w->class_to_pointer_die = g_hash_table_new (NULL, NULL);
	w->class_to_reference_die = g_hash_table_new (NULL, NULL);
	w->cur_file_index = -1;

	return w;
}

void
mono_dwarf_writer_destroy (MonoDwarfWriter *w)
{
	g_free (w);
}

int
mono_dwarf_writer_get_il_file_line_index (MonoDwarfWriter *w)
{
	return w->il_file_line_index;
}

/* Wrappers around the image writer functions */

static void
emit_section_change (MonoDwarfWriter *w, const char *section_name, int subsection_index)
{
	mono_img_writer_emit_section_change (w->w, section_name, subsection_index);
}

static void
emit_push_section (MonoDwarfWriter *w, const char *section_name, int subsection)
{
	mono_img_writer_emit_push_section (w->w, section_name, subsection);
}

static void
emit_pop_section (MonoDwarfWriter *w)
{
	mono_img_writer_emit_pop_section (w->w);
}

static void
emit_label (MonoDwarfWriter *w, const char *name)
{
	mono_img_writer_emit_label (w->w, name);
}

static void
emit_bytes (MonoDwarfWriter *w, const guint8* buf, int size)
{
	mono_img_writer_emit_bytes (w->w, buf, size);
}

static void
emit_string (MonoDwarfWriter *w, const char *value)
{
	mono_img_writer_emit_string (w->w, value);
}

static void
emit_line (MonoDwarfWriter *w)
{
	mono_img_writer_emit_line (w->w);
}

static void
emit_alignment (MonoDwarfWriter *w, int size)
{
	mono_img_writer_emit_alignment (w->w, size);
}

static void
emit_pointer_unaligned (MonoDwarfWriter *w, const char *target)
{
	mono_img_writer_emit_pointer_unaligned (w->w, target);
}

static void
emit_pointer (MonoDwarfWriter *w, const char *target)
{
	mono_img_writer_emit_pointer (w->w, target);
}

static void
emit_int16 (MonoDwarfWriter *w, int value)
{
	mono_img_writer_emit_int16 (w->w, value);
}

static void
emit_int32 (MonoDwarfWriter *w, int value)
{
	mono_img_writer_emit_int32 (w->w, value);
}

static void
emit_symbol (MonoDwarfWriter *w, const char *symbol)
{
	mono_img_writer_emit_symbol (w->w, symbol);
}

static void
emit_symbol_diff (MonoDwarfWriter *w, const char *end, const char* start, int offset)
{
	mono_img_writer_emit_symbol_diff (w->w, end, start, offset);
}

static void
emit_byte (MonoDwarfWriter *w, guint8 val)
{
	mono_img_writer_emit_byte (w->w, val);
}

static void
emit_escaped_string (MonoDwarfWriter *w, char *value)
{
	size_t len = (int)strlen (value);
	for (int i = 0; i < len; ++i) {
		char c = value [i];
		if (!(isalnum (c))) {
			switch (c) {
			case '_':
			case '-':
			case ':':
			case '.':
			case ',':
			case '/':
			case '<':
			case '>':
			case '`':
			case '(':
			case ')':
			case '[':
			case ']':
				break;
			default:
				value [i] = '_';
				break;
			}
		}
	}
	mono_img_writer_emit_string (w->w, value);
}

static G_GNUC_UNUSED void
emit_uleb128 (MonoDwarfWriter *w, guint32 value)
{
	do {
		guint8 b = value & 0x7f;
		value >>= 7;
		if (value != 0) /* more bytes to come */
			b |= 0x80;
		emit_byte (w, b);
	} while (value);
}

static G_GNUC_UNUSED void
emit_sleb128 (MonoDwarfWriter *w, gint64 value)
{
	gboolean more = 1;
	gboolean negative = (value < 0);
	guint32 size = 64;
	guint8 byte;

	while (more) {
		byte = value & 0x7f;
		value >>= 7;
		/* the following is unnecessary if the
		 * implementation of >>= uses an arithmetic rather
		 * than logical shift for a signed left operand
		 */
		if (negative)
			/* sign extend */
			value |= - ((gint64)1 <<(size - 7));
		/* sign bit of byte is second high order bit (0x40) */
		if ((value == 0 && !(byte & 0x40)) ||
			(value == -1 && (byte & 0x40)))
			more = 0;
		else
			byte |= 0x80;
		emit_byte (w, byte);
	}
}

static G_GNUC_UNUSED void
encode_uleb128 (guint32 value, guint8 *buf, guint8 **endbuf)
{
	guint8 *p = buf;

	do {
		guint8 b = value & 0x7f;
		value >>= 7;
		if (value != 0) /* more bytes to come */
			b |= 0x80;
		*p ++ = b;
	} while (value);

	*endbuf = p;
}

static G_GNUC_UNUSED void
encode_sleb128 (gint32 value, guint8 *buf, guint8 **endbuf)
{
	gboolean more = 1;
	gboolean negative = (value < 0);
	guint32 size = 32;
	guint8 byte;
	guint8 *p = buf;

	while (more) {
		byte = value & 0x7f;
		value >>= 7;
		/* the following is unnecessary if the
		 * implementation of >>= uses an arithmetic rather
		 * than logical shift for a signed left operand
		 */
		if (negative)
			/* sign extend */
			value |= - (1 <<(size - 7));
		/* sign bit of byte is second high order bit (0x40) */
		if ((value == 0 && !(byte & 0x40)) ||
			(value == -1 && (byte & 0x40)))
			more = 0;
		else
			byte |= 0x80;
		*p ++= byte;
	}

	*endbuf = p;
}

static void
emit_dwarf_abbrev (MonoDwarfWriter *w, int code, int tag, gboolean has_child,
				   int *attrs, int attrs_len)
{
	int i;

	emit_uleb128 (w, code);
	emit_uleb128 (w, tag);
	emit_byte (w, !!has_child);

	for (i = 0; i < attrs_len; i++)
		emit_uleb128 (w, attrs [i]);
	emit_uleb128 (w, 0);
	emit_uleb128 (w, 0);
}

static void
emit_cie (MonoDwarfWriter *w)
{
	emit_section_change (w, ".debug_frame", 0);

	emit_alignment (w, 8);

	/* Emit a CIE */
	emit_symbol_diff (w, ".Lcie0_end", ".Lcie0_start", 0); /* length */
	emit_label (w, ".Lcie0_start");
	emit_int32 (w, 0xffffffff); /* CIE id */
	emit_byte (w, 3); /* version */
	emit_string (w, ""); /* augmention */
	emit_sleb128 (w, 1); /* code alignment factor */
	emit_sleb128 (w, mono_unwind_get_dwarf_data_align ()); /* data alignment factor */
	emit_uleb128 (w, mono_unwind_get_dwarf_pc_reg ());

	w->cie_program = w->cie_program;
	if (w->cie_program) {
		guint32 uw_info_len;
		guint8 *uw_info = mono_unwind_ops_encode (w->cie_program, &uw_info_len);
		emit_bytes (w, uw_info, uw_info_len);
		g_free (uw_info);
	}

	emit_alignment (w, sizeof (target_mgreg_t));
	emit_label (w, ".Lcie0_end");
}

static void
emit_pointer_value (MonoDwarfWriter *w, gpointer ptr)
{
	gssize val = (gssize)ptr;
	emit_bytes (w, (guint8*)&val, sizeof (target_mgreg_t));
}

static void
emit_fde (MonoDwarfWriter *w, int fde_index, char *start_symbol, char *end_symbol,
		  guint8 *code, guint32 code_size, GSList *unwind_ops, gboolean use_cie)
{
	char symbol1 [128];
	char symbol2 [128];
	GSList *l;
	guint8 *uw_info;
	guint32 uw_info_len;

	emit_section_change (w, ".debug_frame", 0);

	sprintf (symbol1, ".Lfde%d_start", fde_index);
	sprintf (symbol2, ".Lfde%d_end", fde_index);
	emit_symbol_diff (w, symbol2, symbol1, 0); /* length */
	emit_label (w, symbol1);
	emit_int32 (w, 0); /* CIE_pointer */
	if (start_symbol) {
		emit_pointer (w, start_symbol); /* initial_location */
		if (end_symbol)
			emit_symbol_diff (w, end_symbol, start_symbol, 0); /* address_range */
		else {
			g_assert (code_size);
			emit_int32 (w, code_size);
		}
	} else {
		emit_pointer_value (w, code);
		emit_int32 (w, code_size);
	}
#if TARGET_SIZEOF_VOID_P == 8
	/* Upper 32 bits of code size */
	emit_int32 (w, 0);
#endif

	l = unwind_ops;
	if (w->cie_program) {
		// FIXME: Check that the ops really begin with the CIE program */
		int i;

		for (i = 0; i < g_slist_length (w->cie_program); ++i)
			if (l)
				l = l->next;
	}

	/* Convert the list of MonoUnwindOps to the format used by DWARF */
	uw_info = mono_unwind_ops_encode_full (l, &uw_info_len, FALSE);
	emit_bytes (w, uw_info, uw_info_len);
	g_free (uw_info);

	emit_alignment (w, sizeof (target_mgreg_t));
	emit_label (w, symbol2);
}

/* Abbrevations */
#define ABBREV_COMPILE_UNIT 1
#define ABBREV_SUBPROGRAM 2
#define ABBREV_PARAM 3
#define ABBREV_BASE_TYPE 4
#define ABBREV_STRUCT_TYPE 5
#define ABBREV_DATA_MEMBER 6
#define ABBREV_TYPEDEF 7
#define ABBREV_ENUM_TYPE 8
#define ABBREV_ENUMERATOR 9
#define ABBREV_NAMESPACE 10
#define ABBREV_VARIABLE 11
#define ABBREV_VARIABLE_LOCLIST 12
#define ABBREV_POINTER_TYPE 13
#define ABBREV_REFERENCE_TYPE 14
#define ABBREV_PARAM_LOCLIST 15
#define ABBREV_INHERITANCE 16
#define ABBREV_STRUCT_TYPE_NOCHILDREN 17
#define ABBREV_TRAMP_SUBPROGRAM 18

static int compile_unit_attr [] = {
	DW_AT_producer     ,DW_FORM_string,
    DW_AT_name         ,DW_FORM_string,
    DW_AT_comp_dir     ,DW_FORM_string,
	DW_AT_language     ,DW_FORM_data1,
    DW_AT_low_pc       ,DW_FORM_addr,
    DW_AT_high_pc      ,DW_FORM_addr,
	DW_AT_stmt_list    ,DW_FORM_data4
};

static int subprogram_attr [] = {
	DW_AT_name         , DW_FORM_string,
	DW_AT_MIPS_linkage_name, DW_FORM_string,
	DW_AT_decl_file    , DW_FORM_udata,
	DW_AT_decl_line    , DW_FORM_udata,
#ifndef TARGET_IOS
	DW_AT_description  , DW_FORM_string,
#endif
    DW_AT_low_pc       , DW_FORM_addr,
    DW_AT_high_pc      , DW_FORM_addr,
	DW_AT_frame_base   , DW_FORM_block1
};

static int tramp_subprogram_attr [] = {
	DW_AT_name         , DW_FORM_string,
    DW_AT_low_pc       , DW_FORM_addr,
    DW_AT_high_pc      , DW_FORM_addr,
};

static int param_attr [] = {
	DW_AT_name,     DW_FORM_string,
	DW_AT_type,     DW_FORM_ref4,
	DW_AT_location, DW_FORM_block1
};

static int param_loclist_attr [] = {
	DW_AT_name,     DW_FORM_string,
	DW_AT_type,     DW_FORM_ref4,
	DW_AT_location, DW_FORM_data4
};

static int base_type_attr [] = {
	DW_AT_byte_size,   DW_FORM_data1,
	DW_AT_encoding,    DW_FORM_data1,
	DW_AT_name,        DW_FORM_string
};

static int struct_type_attr [] = {
	DW_AT_name,        DW_FORM_string,
	DW_AT_byte_size,   DW_FORM_udata,
};

static int data_member_attr [] = {
	DW_AT_name,        DW_FORM_string,
	DW_AT_type,        DW_FORM_ref4,
	DW_AT_data_member_location, DW_FORM_block1
};

static int typedef_attr [] = {
	DW_AT_name,        DW_FORM_string,
	DW_AT_type,        DW_FORM_ref4
};

static int pointer_type_attr [] = {
	DW_AT_type,        DW_FORM_ref4,
};

static int reference_type_attr [] = {
	DW_AT_type,        DW_FORM_ref4,
};

static int enum_type_attr [] = {
	DW_AT_name,        DW_FORM_string,
	DW_AT_byte_size,   DW_FORM_udata,
	DW_AT_type,        DW_FORM_ref4,
};

static int enumerator_attr [] = {
	DW_AT_name,        DW_FORM_string,
	DW_AT_const_value, DW_FORM_sdata,
};

static int namespace_attr [] = {
	DW_AT_name,        DW_FORM_string,
};

static int variable_attr [] = {
	DW_AT_name,     DW_FORM_string,
	DW_AT_type,     DW_FORM_ref4,
	DW_AT_location, DW_FORM_block1
};

static int variable_loclist_attr [] = {
	DW_AT_name,     DW_FORM_string,
	DW_AT_type,     DW_FORM_ref4,
	DW_AT_location, DW_FORM_data4
};

static int inheritance_attr [] = {
	DW_AT_type,        DW_FORM_ref4,
	DW_AT_data_member_location, DW_FORM_block1
};

typedef struct DwarfBasicType {
	const char *die_name, *name;
	int type;
	int size;
	int encoding;
} DwarfBasicType;

static DwarfBasicType basic_types [] = {
	{ ".LDIE_I1", "sbyte", MONO_TYPE_I1, 1, DW_ATE_signed },
	{ ".LDIE_U1", "byte", MONO_TYPE_U1, 1, DW_ATE_unsigned },
	{ ".LDIE_I2", "short", MONO_TYPE_I2, 2, DW_ATE_signed },
	{ ".LDIE_U2", "ushort", MONO_TYPE_U2, 2, DW_ATE_unsigned },
	{ ".LDIE_I4", "int", MONO_TYPE_I4, 4, DW_ATE_signed },
	{ ".LDIE_U4", "uint", MONO_TYPE_U4, 4, DW_ATE_unsigned },
	{ ".LDIE_I8", "long", MONO_TYPE_I8, 8, DW_ATE_signed },
	{ ".LDIE_U8", "ulong", MONO_TYPE_U8, 8, DW_ATE_unsigned },
	{ ".LDIE_I", "intptr", MONO_TYPE_I, TARGET_SIZEOF_VOID_P, DW_ATE_signed },
	{ ".LDIE_U", "uintptr", MONO_TYPE_U, TARGET_SIZEOF_VOID_P, DW_ATE_unsigned },
	{ ".LDIE_R4", "float", MONO_TYPE_R4, 4, DW_ATE_float },
	{ ".LDIE_R8", "double", MONO_TYPE_R8, 8, DW_ATE_float },
	{ ".LDIE_BOOLEAN", "boolean", MONO_TYPE_BOOLEAN, 1, DW_ATE_boolean },
	{ ".LDIE_CHAR", "char", MONO_TYPE_CHAR, 2, DW_ATE_unsigned_char },
	{ ".LDIE_STRING", "string", MONO_TYPE_STRING, sizeof (target_mgreg_t), DW_ATE_address },
	{ ".LDIE_OBJECT", "object", MONO_TYPE_OBJECT, sizeof (target_mgreg_t), DW_ATE_address },
	{ ".LDIE_SZARRAY", "object", MONO_TYPE_SZARRAY, sizeof (target_mgreg_t), DW_ATE_address },
};

/* Constants for encoding line number special opcodes */
#define OPCODE_BASE 13
#define LINE_BASE -5
#define LINE_RANGE 14

static int
get_line_number_file_name (MonoDwarfWriter *w, const char *name)
{
	int index;

	g_assert (w->file_to_index);
	index = GPOINTER_TO_UINT (g_hash_table_lookup (w->file_to_index, name));
	g_assert (index > 0);
	return index - 1;
}

static int
add_line_number_file_name (MonoDwarfWriter *w, const char *name,
						   gint64 last_mod_time, gint64 file_size)
{
	int index;
	char *copy;

	if (!w->file_to_index) {
		w->file_to_index = g_hash_table_new (g_str_hash, g_str_equal);
		w->index_to_file = g_hash_table_new (NULL, NULL);
	}

	index = GPOINTER_TO_UINT (g_hash_table_lookup (w->file_to_index, name));
	if (index > 0)
		return index - 1;
	index = w->line_number_file_index;
	w->line_number_file_index ++;
	copy = g_strdup (name);
	g_hash_table_insert (w->file_to_index, copy, GUINT_TO_POINTER (index + 1));
	g_hash_table_insert (w->index_to_file, GUINT_TO_POINTER (index + 1), copy);

	return index;
}

char *
mono_dwarf_escape_path (const char *name)
{
	if (strchr (name, '\\')) {
		char *s;
		size_t len;
		int i, j;

		len = strlen (name);
		s = (char *)g_malloc0 ((len + 1) * 2);
		j = 0;
		for (i = 0; i < len; ++i) {
			if (name [i] == '\\') {
				s [j ++] = '\\';
				s [j ++] = '\\';
			} else {
				s [j ++] = name [i];
			}
		}
		return s;
	}
	return g_strdup (name);
}

static void
emit_all_line_number_info (MonoDwarfWriter *w)
{
	int i;
	GHashTable *dir_to_index, *index_to_dir;
	GSList *l;
	GSList *info_list;

	add_line_number_file_name (w, "<unknown>", 0, 0);

	/* Collect files */
	info_list = g_slist_reverse (w->line_info);
	for (l = info_list; l; l = l->next) {
		MethodLineNumberInfo *info = (MethodLineNumberInfo *)l->data;
		MonoDebugMethodInfo *minfo;
		GPtrArray *source_file_list;

		// FIXME: Free stuff
		minfo = mono_debug_lookup_method (info->method);
		if (!minfo)
			continue;

		mono_debug_get_seq_points (minfo, NULL, &source_file_list, NULL, NULL, NULL);
		for (i = 0; i < source_file_list->len; ++i) {
			MonoDebugSourceInfo *sinfo = (MonoDebugSourceInfo *)g_ptr_array_index (source_file_list, i);
			add_line_number_file_name (w, sinfo->source_file, 0, 0);
		}
	}

	/* Preprocess files */
	dir_to_index = g_hash_table_new (g_str_hash, g_str_equal);
	index_to_dir = g_hash_table_new (NULL, NULL);
	for (i = 0; i < w->line_number_file_index; ++i) {
		char *name = (char *)g_hash_table_lookup (w->index_to_file, GUINT_TO_POINTER (i + 1));
		char *copy;
		int dir_index = 0;

		if (g_path_is_absolute (name)) {
			char *dir = g_path_get_dirname (name);

			dir_index = GPOINTER_TO_UINT (g_hash_table_lookup (dir_to_index, dir));
			if (dir_index == 0) {
				dir_index = w->line_number_dir_index;
				w->line_number_dir_index ++;
				copy = g_strdup (dir);
				g_hash_table_insert (dir_to_index, copy, GUINT_TO_POINTER (dir_index + 1));
				g_hash_table_insert (index_to_dir, GUINT_TO_POINTER (dir_index + 1), copy);
			} else {
				dir_index --;
			}

			g_free (dir);
		}
	}

	/* Line number info header */
	emit_section_change (w, ".debug_line", 0);
	emit_label (w, ".Ldebug_line_section_start");
	emit_label (w, ".Ldebug_line_start");
	emit_symbol_diff (w, ".Ldebug_line_end", ".", -4); /* length */
	emit_int16 (w, 0x2); /* version */
	emit_symbol_diff (w, ".Ldebug_line_header_end", ".", -4); /* header_length */
	emit_byte (w, 1); /* minimum_instruction_length */
	emit_byte (w, 1); /* default_is_stmt */
	emit_byte (w, LINE_BASE); /* line_base */
	emit_byte (w, LINE_RANGE); /* line_range */
	emit_byte (w, OPCODE_BASE); /* opcode_base */
	emit_byte (w, 0); /* standard_opcode_lengths */
	emit_byte (w, 1);
	emit_byte (w, 1);
	emit_byte (w, 1);
	emit_byte (w, 1);
	emit_byte (w, 0);
	emit_byte (w, 0);
	emit_byte (w, 0);
	emit_byte (w, 1);
	emit_byte (w, 0);
	emit_byte (w, 0);
	emit_byte (w, 1);

	/* Includes */
	emit_section_change (w, ".debug_line", 0);
	for (i = 0; i < w->line_number_dir_index; ++i) {
		char *dir = (char *)g_hash_table_lookup (index_to_dir, GUINT_TO_POINTER (i + 1));

		emit_string (w, mono_dwarf_escape_path (dir));
	}
	/* End of Includes */
	emit_byte (w, 0);

	/* Files */
	for (i = 0; i < w->line_number_file_index; ++i) {
		char *name = (char *)g_hash_table_lookup (w->index_to_file, GUINT_TO_POINTER (i + 1));
		char *basename = NULL, *dir;
		int dir_index = 0;

		if (g_path_is_absolute (name)) {
			dir = g_path_get_dirname (name);

			dir_index = GPOINTER_TO_UINT (g_hash_table_lookup (dir_to_index, dir));
			basename = g_path_get_basename (name);
		}

		if (basename)
			emit_string (w, basename);
		else
			emit_string (w, mono_dwarf_escape_path (name));
		emit_uleb128 (w, dir_index);
		emit_byte (w, 0);
		emit_byte (w, 0);
	}

	/* End of Files */
	emit_byte (w, 0);

	emit_label (w, ".Ldebug_line_header_end");

	/* Emit line number table */
	for (l = info_list; l; l = l->next) {
		MethodLineNumberInfo *info = (MethodLineNumberInfo *)l->data;
		MonoDebugMethodJitInfo *dmji;

		dmji = mono_debug_find_method (info->method, NULL);
		if (!dmji)
			continue;
		emit_line_number_info (w, info->method, info->start_symbol, info->end_symbol, info->code, info->code_size, dmji);
		mono_debug_free_method_jit_info (dmji);
	}
	g_slist_free (info_list);

	emit_byte (w, 0);
	emit_byte (w, 1);
	emit_byte (w, DW_LNE_end_sequence);

	emit_label (w, ".Ldebug_line_end");
}

/*
 * Some assemblers like apple's do not support subsections, so we can't place
 * .Ldebug_info_end at the end of the section using subsections. Instead, we
 * define it every time something gets added to the .debug_info section.
 * The apple assember seems to use the last definition.
 */
static void
emit_debug_info_end (MonoDwarfWriter *w)
{
	/* This doesn't seem to work/required with recent iphone sdk versions */
#if 0
	if (!mono_img_writer_subsections_supported (w->w))
		fprintf (w->fp, "\n.set %sdebug_info_end,.\n", w->temp_prefix);
#endif
}

void
mono_dwarf_writer_emit_base_info (MonoDwarfWriter *w, const char *cu_name, GSList *base_unwind_program)
{
	char *s, *build_info;
	int i;

	if (!w->emit_line) {
		emit_section_change (w, ".debug_line", 0);
		emit_label (w, ".Ldebug_line_section_start");
		emit_label (w, ".Ldebug_line_start");
	}

	w->cie_program = base_unwind_program;

	emit_section_change (w, ".debug_abbrev", 0);
	emit_label (w, ".Ldebug_abbrev_start");
	emit_dwarf_abbrev (w, ABBREV_COMPILE_UNIT, DW_TAG_compile_unit, TRUE,
					   compile_unit_attr, G_N_ELEMENTS (compile_unit_attr));
	emit_dwarf_abbrev (w, ABBREV_SUBPROGRAM, DW_TAG_subprogram, TRUE,
					   subprogram_attr, G_N_ELEMENTS (subprogram_attr));
	emit_dwarf_abbrev (w, ABBREV_PARAM, DW_TAG_formal_parameter, FALSE,
					   param_attr, G_N_ELEMENTS (param_attr));
	emit_dwarf_abbrev (w, ABBREV_PARAM_LOCLIST, DW_TAG_formal_parameter, FALSE,
					   param_loclist_attr, G_N_ELEMENTS (param_loclist_attr));
	emit_dwarf_abbrev (w, ABBREV_BASE_TYPE, DW_TAG_base_type, FALSE,
					   base_type_attr, G_N_ELEMENTS (base_type_attr));
	emit_dwarf_abbrev (w, ABBREV_STRUCT_TYPE, DW_TAG_class_type, TRUE,
					   struct_type_attr, G_N_ELEMENTS (struct_type_attr));
	emit_dwarf_abbrev (w, ABBREV_STRUCT_TYPE_NOCHILDREN, DW_TAG_class_type, FALSE,
					   struct_type_attr, G_N_ELEMENTS (struct_type_attr));
	emit_dwarf_abbrev (w, ABBREV_DATA_MEMBER, DW_TAG_member, FALSE,
					   data_member_attr, G_N_ELEMENTS (data_member_attr));
	emit_dwarf_abbrev (w, ABBREV_TYPEDEF, DW_TAG_typedef, FALSE,
					   typedef_attr, G_N_ELEMENTS (typedef_attr));
	emit_dwarf_abbrev (w, ABBREV_ENUM_TYPE, DW_TAG_enumeration_type, TRUE,
					   enum_type_attr, G_N_ELEMENTS (enum_type_attr));
	emit_dwarf_abbrev (w, ABBREV_ENUMERATOR, DW_TAG_enumerator, FALSE,
					   enumerator_attr, G_N_ELEMENTS (enumerator_attr));
	emit_dwarf_abbrev (w, ABBREV_NAMESPACE, DW_TAG_namespace, TRUE,
					   namespace_attr, G_N_ELEMENTS (namespace_attr));
	emit_dwarf_abbrev (w, ABBREV_VARIABLE, DW_TAG_variable, FALSE,
					   variable_attr, G_N_ELEMENTS (variable_attr));
	emit_dwarf_abbrev (w, ABBREV_VARIABLE_LOCLIST, DW_TAG_variable, FALSE,
					   variable_loclist_attr, G_N_ELEMENTS (variable_loclist_attr));
	emit_dwarf_abbrev (w, ABBREV_POINTER_TYPE, DW_TAG_pointer_type, FALSE,
					   pointer_type_attr, G_N_ELEMENTS (pointer_type_attr));
	emit_dwarf_abbrev (w, ABBREV_REFERENCE_TYPE, DW_TAG_reference_type, FALSE,
					   reference_type_attr, G_N_ELEMENTS (reference_type_attr));
	emit_dwarf_abbrev (w, ABBREV_INHERITANCE, DW_TAG_inheritance, FALSE,
					   inheritance_attr, G_N_ELEMENTS (inheritance_attr));
	emit_dwarf_abbrev (w, ABBREV_TRAMP_SUBPROGRAM, DW_TAG_subprogram, FALSE,
					   tramp_subprogram_attr, G_N_ELEMENTS (tramp_subprogram_attr));
	emit_byte (w, 0);

	emit_section_change (w, ".debug_info", 0);
	emit_label (w, ".Ldebug_info_start");
	emit_symbol_diff (w, ".Ldebug_info_end", ".Ldebug_info_begin", 0); /* length */
	emit_label (w, ".Ldebug_info_begin");
	emit_int16 (w, 0x2); /* DWARF version 2 */
#if !defined(TARGET_MACH)
	emit_symbol (w, ".Ldebug_abbrev_start"); /* .debug_abbrev offset */
#else
	emit_int32 (w, 0); /* .debug_abbrev offset */
#endif
	emit_byte (w, sizeof (target_mgreg_t)); /* address size */

	/* Compilation unit */
	emit_uleb128 (w, ABBREV_COMPILE_UNIT);
	build_info = mono_get_runtime_build_info ();
	s = g_strdup_printf ("Mono AOT Compiler %s", build_info);
	emit_string (w, s);
	g_free (build_info);
	g_free (s);
	emit_string (w, cu_name);
	emit_string (w, "");
	emit_byte (w, DW_LANG_C);
	emit_pointer_value (w, 0);
	emit_pointer_value (w, 0);
	/* offset into .debug_line section */
	emit_symbol_diff (w, ".Ldebug_line_start", ".Ldebug_line_section_start", 0);

	/* Base types */
	for (i = 0; i < G_N_ELEMENTS (basic_types); ++i) {
		emit_label (w, basic_types [i].die_name);
		emit_uleb128 (w, ABBREV_BASE_TYPE);
		emit_byte (w, GINT_TO_UINT8 (basic_types [i].size));
		emit_byte (w, GINT_TO_UINT8 (basic_types [i].encoding));
		emit_string (w, basic_types [i].name);
	}

	emit_debug_info_end (w);

	/* debug_loc section */
	emit_section_change (w, ".debug_loc", 0);
	emit_label (w, ".Ldebug_loc_start");

	emit_cie (w);
}

/*
 * mono_dwarf_writer_close:
 *
 *   Finalize the emitted debugging info.
 */
void
mono_dwarf_writer_close (MonoDwarfWriter *w)
{
	emit_section_change (w, ".debug_info", 0);
	emit_byte (w, 0); /* close COMPILE_UNIT */
	emit_label (w, ".Ldebug_info_end");

	if (w->emit_line)
		emit_all_line_number_info (w);
}

static void emit_type (MonoDwarfWriter *w, MonoType *t);
static const char* get_type_die (MonoDwarfWriter *w, MonoType *t);

static const char*
get_class_die (MonoDwarfWriter *w, MonoClass *klass, gboolean vtype)
{
	GHashTable *cache;

	if (vtype)
		cache = w->class_to_vtype_die;
	else
		cache = w->class_to_die;

	return (const char *)g_hash_table_lookup (cache, klass);
}

/* Returns the local symbol pointing to the emitted debug info */
static char*
emit_class_dwarf_info (MonoDwarfWriter *w, MonoClass *klass, gboolean vtype)
{
	char *die, *pointer_die, *reference_die;
	char *full_name;
	gpointer iter;
	MonoClassField *field;
	const char *fdie;
	int k;
	gboolean emit_namespace = FALSE, has_children;
	GHashTable *cache;

	if (vtype)
		cache = w->class_to_vtype_die;
	else
		cache = w->class_to_die;

	die = (char *)g_hash_table_lookup (cache, klass);
	if (die)
		return die;

	if (!((m_class_get_byval_arg (klass)->type == MONO_TYPE_CLASS) || (m_class_get_byval_arg (klass)->type == MONO_TYPE_OBJECT) || m_class_get_byval_arg (klass)->type == MONO_TYPE_GENERICINST || m_class_is_enumtype (klass) || (m_class_get_byval_arg (klass)->type == MONO_TYPE_VALUETYPE && vtype) ||
		(m_class_get_byval_arg (klass)->type >= MONO_TYPE_BOOLEAN && m_class_get_byval_arg (klass)->type <= MONO_TYPE_R8 && !vtype)))
		return NULL;

	/*
	 * FIXME: gdb can't handle namespaces in languages it doesn't know about.
	 */
	/*
	if (klass->name_space && klass->name_space [0] != '\0')
		emit_namespace = TRUE;
	*/
	if (emit_namespace) {
		emit_uleb128 (w, ABBREV_NAMESPACE);
		emit_string (w, m_class_get_name_space (klass));
	}

	full_name = g_strdup_printf ("%s%s%s", m_class_get_name_space (klass), m_class_get_name_space (klass) ? "." : "", m_class_get_name (klass));
	/*
	 * gdb doesn't support namespaces for non-C++ dwarf objects, so use _
	 * to separate components.
	 */
	for (char *p = full_name; *p; p ++)
		if (*p == '.')
			*p = '_';

	die = g_strdup_printf (".LTDIE_%d", w->tdie_index);
	pointer_die = g_strdup_printf (".LTDIE_%d_POINTER", w->tdie_index);
	reference_die = g_strdup_printf (".LTDIE_%d_REFERENCE", w->tdie_index);
	w->tdie_index ++;

	g_hash_table_insert (w->class_to_pointer_die, klass, pointer_die);
	g_hash_table_insert (w->class_to_reference_die, klass, reference_die);
	g_hash_table_insert (cache, klass, die);

	if (m_class_is_enumtype (klass)) {
		int size = mono_class_value_size (mono_class_from_mono_type_internal (mono_class_enum_basetype_internal (klass)), NULL);

		emit_label (w, die);

		emit_uleb128 (w, ABBREV_ENUM_TYPE);
		emit_string (w, full_name);
		emit_uleb128 (w, size);
		for (k = 0; k < G_N_ELEMENTS (basic_types); ++k)
			if (basic_types [k].type == mono_class_enum_basetype_internal (klass)->type)
				break;
		g_assert (k < G_N_ELEMENTS (basic_types));
		emit_symbol_diff (w, basic_types [k].die_name, ".Ldebug_info_start", 0);

		/* Emit enum values */
		iter = NULL;
		while ((field = mono_class_get_fields_internal (klass, &iter))) {
			const char *p;
			MonoTypeEnum def_type;

			if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC))
				continue;
			if (mono_field_is_deleted (field))
				continue;

			emit_uleb128 (w, ABBREV_ENUMERATOR);
			emit_string (w, mono_field_get_name (field));

			p = mono_class_get_field_default_value (field, &def_type);
			/* len = */ mono_metadata_decode_blob_size (p, &p);
			switch (mono_class_enum_basetype_internal (klass)->type) {
			case MONO_TYPE_U1:
			case MONO_TYPE_I1:
			case MONO_TYPE_BOOLEAN:
				emit_sleb128 (w, *p);
				break;
			case MONO_TYPE_U2:
			case MONO_TYPE_I2:
			case MONO_TYPE_CHAR:
				emit_sleb128 (w, read16 (p));
				break;
			case MONO_TYPE_U4:
			case MONO_TYPE_I4:
				emit_sleb128 (w, read32 (p));
				break;
			case MONO_TYPE_U8:
			case MONO_TYPE_I8:
				emit_sleb128 (w, read64 (p));
				break;
			case MONO_TYPE_I:
			case MONO_TYPE_U:
#if TARGET_SIZEOF_VOID_P == 8
				emit_sleb128 (w, read64 (p));
#else
				emit_sleb128 (w, read32 (p));
#endif
				break;
			default:
				g_assert_not_reached ();
			}
		}

		has_children = TRUE;
	} else {
		guint8 buf [128];
		guint8 *p;
		char *parent_die;

		if (m_class_get_parent (klass))
			parent_die = emit_class_dwarf_info (w, m_class_get_parent (klass), FALSE);
		else
			parent_die = NULL;

		/* Emit field types */
		iter = NULL;
		while ((field = mono_class_get_fields_internal (klass, &iter))) {
			if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
				continue;

			emit_type (w, field->type);
		}

		iter = NULL;
		has_children = parent_die || mono_class_get_fields_internal (klass, &iter);

		emit_label (w, die);

		emit_uleb128 (w, has_children ? ABBREV_STRUCT_TYPE : ABBREV_STRUCT_TYPE_NOCHILDREN);
		emit_string (w, full_name);
		emit_uleb128 (w, m_class_get_instance_size (klass));

		if (parent_die) {
			emit_uleb128 (w, ABBREV_INHERITANCE);
			emit_symbol_diff (w, parent_die, ".Ldebug_info_start", 0);

			p = buf;
			*p ++= DW_OP_plus_uconst;
			encode_uleb128 (0, p, &p);
			emit_byte (w, GPTRDIFF_TO_UINT8 (p - buf));
			emit_bytes (w, buf, GPTRDIFF_TO_INT (p - buf));
		}

		/* Emit fields */
		iter = NULL;
		while ((field = mono_class_get_fields_internal (klass, &iter))) {
			if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
				continue;

			fdie = get_type_die (w, field->type);
			if (fdie) {
				emit_uleb128 (w, ABBREV_DATA_MEMBER);
				emit_string (w, field->name);
				emit_symbol_diff (w, fdie, ".Ldebug_info_start", 0);
				/* location */
				p = buf;
				*p ++= DW_OP_plus_uconst;
				if (m_class_is_valuetype (klass) && vtype)
					encode_uleb128 (m_field_get_offset (field) - MONO_ABI_SIZEOF (MonoObject), p, &p);
				else
					encode_uleb128 (m_field_get_offset (field), p, &p);

				emit_byte (w, GPTRDIFF_TO_UINT8 (p - buf));
				emit_bytes (w, buf, GPTRDIFF_TO_INT (p - buf));
			}
		}
	}

	/* Type end */
	if (has_children)
		emit_uleb128 (w, 0x0);

	/* Add a typedef, so we can reference the type without a 'struct' in gdb */
	emit_uleb128 (w, ABBREV_TYPEDEF);
	emit_string (w, full_name);
	emit_symbol_diff (w, die, ".Ldebug_info_start", 0);

	/* Add a pointer type */
	emit_label (w, pointer_die);

	emit_uleb128 (w, ABBREV_POINTER_TYPE);
	emit_symbol_diff (w, die, ".Ldebug_info_start", 0);

	/* Add a reference type */
	emit_label (w, reference_die);

	emit_uleb128 (w, ABBREV_REFERENCE_TYPE);
	emit_symbol_diff (w, die, ".Ldebug_info_start", 0);

	g_free (full_name);

	if (emit_namespace) {
		/* Namespace end */
		emit_uleb128 (w, 0x0);
	}

	return die;
}

static gboolean base_types_emitted [64];

static const char*
get_type_die (MonoDwarfWriter *w, MonoType *t)
{
	MonoClass *klass = mono_class_from_mono_type_internal (t);
	int j;
	const char *tdie;

	if (m_type_is_byref (t)) {
		if (t->type == MONO_TYPE_VALUETYPE) {
			tdie = (const char *)g_hash_table_lookup (w->class_to_pointer_die, klass);
		}
		else {
			tdie = get_class_die (w, klass, FALSE);
			/* Should return a pointer type to a reference */
		}
		// FIXME:
		t = mono_get_int_type ();
	}
	for (j = 0; j < G_N_ELEMENTS (basic_types); ++j)
		if (basic_types [j].type == t->type)
			break;
	if (j < G_N_ELEMENTS (basic_types)) {
		tdie = basic_types [j].die_name;
	} else {
		switch (t->type) {
		case MONO_TYPE_CLASS:
			tdie = (const char *)g_hash_table_lookup (w->class_to_reference_die, klass);
			//tdie = ".LDIE_OBJECT";
			break;
		case MONO_TYPE_ARRAY:
			tdie = ".LDIE_OBJECT";
			break;
		case MONO_TYPE_VALUETYPE:
			if (m_class_is_enumtype (klass))
				tdie = get_class_die (w, klass, FALSE);
			else
				tdie = ".LDIE_I4";
			break;
		case MONO_TYPE_GENERICINST:
			if (!MONO_TYPE_ISSTRUCT (t)) {
				tdie = (const char *)g_hash_table_lookup (w->class_to_reference_die, klass);
			} else {
				tdie = ".LDIE_I4";
			}
			break;
		case MONO_TYPE_PTR:
			tdie = ".LDIE_I";
			break;
		default:
			tdie = ".LDIE_I4";
			break;
		}
	}

	g_assert (tdie);

	return tdie;
}

static void
emit_type (MonoDwarfWriter *w, MonoType *t)
{
	MonoClass *klass = mono_class_from_mono_type_internal (t);
	int j;
	const char *tdie;

	if (m_type_is_byref (t)) {
		if (t->type == MONO_TYPE_VALUETYPE) {
			tdie = emit_class_dwarf_info (w, klass, TRUE);
			if (tdie)
				return;
		}
		else {
			emit_class_dwarf_info (w, klass, FALSE);
		}
		// FIXME:
		t = mono_get_int_type ();
	}
	for (j = 0; j < G_N_ELEMENTS (basic_types); ++j)
		if (basic_types [j].type == t->type)
			break;
	if (j < G_N_ELEMENTS (basic_types)) {
		/* Emit a boxed version of base types */
		if (j < 64 && !base_types_emitted [j]) {
			emit_class_dwarf_info (w, klass, FALSE);
			base_types_emitted [j] = TRUE;
		}
	} else {
		switch (t->type) {
		case MONO_TYPE_CLASS:
			emit_class_dwarf_info (w, klass, FALSE);
			break;
		case MONO_TYPE_ARRAY:
			break;
		case MONO_TYPE_VALUETYPE:
			if (m_class_is_enumtype (klass))
				emit_class_dwarf_info (w, klass, FALSE);
			break;
		case MONO_TYPE_GENERICINST:
			if (!MONO_TYPE_ISSTRUCT (t))
				emit_class_dwarf_info (w, klass, FALSE);
			break;
		case MONO_TYPE_PTR:
			break;
		default:
			break;
		}
	}
}

static void
emit_var_type (MonoDwarfWriter *w, MonoType *t)
{
	const char *tdie;

	tdie = get_type_die (w, t);

	emit_symbol_diff (w, tdie, ".Ldebug_info_start", 0);
}

static void
encode_var_location (MonoDwarfWriter *w, MonoInst *ins, guint8 *p, guint8 **endp)
{
	/* location */
	/* FIXME: This needs a location list, since the args can go from reg->stack */
	if (!ins || ins->flags & MONO_INST_IS_DEAD) {
		/* gdb treats this as optimized out */
	} else if (ins->opcode == OP_REGVAR) {
		*p = DW_OP_reg0 + GINT_TO_UINT8 (mono_hw_reg_to_dwarf_reg (ins->dreg));
		p ++;
	} else if (ins->opcode == OP_REGOFFSET) {
		*p ++= DW_OP_breg0 + GINT_TO_UINT8 (mono_hw_reg_to_dwarf_reg (ins->inst_basereg));
		encode_sleb128 (GTMREG_TO_INT32 (ins->inst_offset), p, &p);
	} else {
		// FIXME:
		*p ++ = DW_OP_reg0;
	}

	*endp = p;
}

static void
emit_loclist (MonoDwarfWriter *w, MonoInst *ins,
			  guint8 *loclist_begin_addr, guint8 *loclist_end_addr,
			  guint8 *expr, guint32 expr_len)
{
	char label [128];

	emit_push_section (w, ".debug_loc", 0);
	sprintf (label, ".Lloclist_%d", w->loclist_index ++ );
	emit_label (w, label);

	emit_pointer_value (w, loclist_begin_addr);
	emit_pointer_value (w, loclist_end_addr);
	emit_byte (w, GUINT32_TO_UINT8 (expr_len % 256));
	emit_byte (w, GUINT32_TO_UINT8 (expr_len / 256));
	emit_bytes (w, expr, expr_len);

	emit_pointer_value (w, NULL);
	emit_pointer_value (w, NULL);

	emit_pop_section (w);
	emit_symbol_diff (w, label, ".Ldebug_loc_start", 0);
}

/*
 * MonoDisHelper->tokener doesn't take an IP argument, and we can't add one since
 * it is a public header.
 */
static const guint8 *token_handler_ip;

static char*
token_handler (MonoDisHelper *dh, MonoMethod *method, guint32 token)
{
	ERROR_DECL (error);
	char *res, *desc;
	MonoMethod *cmethod;
	MonoClass *klass;
	MonoClassField *field;
	gpointer data = NULL;

	if (method->wrapper_type)
		data = mono_method_get_wrapper_data (method, token);

	switch (*token_handler_ip) {
	case CEE_ISINST:
	case CEE_CASTCLASS:
	case CEE_LDELEMA:
		if (method->wrapper_type) {
			klass = (MonoClass *)data;
		} else {
			klass = mono_class_get_checked (m_class_get_image (method->klass), token, error);
			g_assert (is_ok (error)); /* FIXME error handling */
		}
		res = g_strdup_printf ("<%s>", m_class_get_name (klass));
		break;
	case CEE_NEWOBJ:
	case CEE_CALL:
	case CEE_CALLVIRT:
		if (method->wrapper_type) {
			cmethod = (MonoMethod *)data;
		} else {
			cmethod = mono_get_method_checked (m_class_get_image (method->klass), token, NULL, NULL, error);
			if (!cmethod)
				g_error ("Could not load method due to %s", mono_error_get_message (error)); /* FIXME don't swallow the error */
			mono_error_assert_ok (error);
		}
		desc = mono_method_full_name (cmethod, TRUE);
		res = g_strdup_printf ("<%s>", desc);
		g_free (desc);
		break;
	case CEE_CALLI:
		if (method->wrapper_type) {
			desc = mono_signature_get_desc ((MonoMethodSignature *)data, FALSE);
			res = g_strdup_printf ("<%s>", desc);
			g_free (desc);
		} else {
			res = g_strdup_printf ("<0x%08x>", token);
		}
		break;
	case CEE_LDFLD:
	case CEE_LDSFLD:
	case CEE_STFLD:
	case CEE_STSFLD:
		if (method->wrapper_type) {
			field = (MonoClassField *)data;
		} else {
			field = mono_field_from_token_checked (m_class_get_image (method->klass), token, &klass, NULL,  error);
			g_assert (is_ok (error)); /* FIXME error handling */
		}
		desc = mono_field_full_name (field);
		res = g_strdup_printf ("<%s>", desc);
		g_free (desc);
		break;
	default:
		res = g_strdup_printf ("<0x%08x>", token);
		break;
	}

	return res;
}

/*
 * disasm_ins:
 *
 *   Produce a disassembled form of the IL instruction at IP. This is an extension
 * of mono_disasm_code_one () which can disasm tokens, handle wrapper methods, and
 * CEE_MONO_ opcodes.
 */
static char*
disasm_ins (MonoMethod *method, const guchar *ip, const guint8 **endip)
{
	ERROR_DECL (error);
	char *dis;
	MonoDisHelper dh;
	MonoMethodHeader *header = mono_method_get_header_checked (method, error);
	mono_error_assert_ok (error); /* FIXME don't swallow the error */

	memset (&dh, 0, sizeof (dh));
	dh.newline = "";
	dh.label_format = "IL_%04x: ";
	dh.label_target = "IL_%04x";
	dh.tokener = token_handler;

	token_handler_ip = ip;
	if (*ip == MONO_CUSTOM_PREFIX) {
		guint32 token;
		gpointer data;

		switch (ip [1]) {
		case CEE_MONO_ICALL: {
			MonoJitICallInfo const * const info = mono_find_jit_icall_info ((MonoJitICallId)read32 (ip + 2));
			dis = g_strdup_printf ("IL_%04x: mono_icall <%s>", (int)(ip - header->code), info->name);
			ip += 6;
			break;
		}
		case CEE_MONO_CLASSCONST: {
			token = read32 (ip + 2);
			data = mono_method_get_wrapper_data (method, token);

			dis = g_strdup_printf ("IL_%04x: mono_classconst <%s>", (int)(ip - header->code), m_class_get_name ((MonoClass*)data));
			ip += 6;
			break;
		}
		default:
			dis = mono_disasm_code_one (&dh, method, ip, &ip);
		}
	} else {
		dis = mono_disasm_code_one (&dh, method, ip, &ip);
	}
	token_handler_ip = NULL;

	*endip = ip;
	mono_metadata_free_mh (header);
	return dis;
}

static gint32
il_offset_from_address (MonoMethod *method, MonoDebugMethodJitInfo *jit,
						guint32 native_offset)
{
	int i;

	if (!jit->line_numbers)
		return -1;

	for (i = jit->num_line_numbers - 1; i >= 0; i--) {
		MonoDebugLineNumberEntry lne = jit->line_numbers [i];

		if (lne.native_offset <= native_offset)
			return lne.il_offset;
	}

	return -1;
}

static int max_special_addr_diff = 0;

static void
emit_advance_op (MonoDwarfWriter *w, int line_diff, int addr_diff)
{
	gint64 opcode = 0;

	/* Use a special opcode if possible */
	if (line_diff - LINE_BASE >= 0 && line_diff - LINE_BASE < LINE_RANGE) {
		if (max_special_addr_diff == 0)
			max_special_addr_diff = (255 - OPCODE_BASE) / LINE_RANGE;

		if (addr_diff > max_special_addr_diff && (addr_diff < 2 * max_special_addr_diff)) {
			emit_byte (w, DW_LNS_const_add_pc);
			addr_diff -= max_special_addr_diff;
		}

		opcode = (line_diff - LINE_BASE) + (LINE_RANGE * addr_diff) + OPCODE_BASE;
		if (opcode > 255)
			opcode = 0;
	}

	if (opcode != 0) {
		emit_byte (w, GINT64_TO_UINT8 (opcode));
	} else {
		//printf ("large: %d %d %d\n", line_diff, addr_diff, max_special_addr_diff);
		emit_byte (w, DW_LNS_advance_line);
		emit_sleb128 (w, line_diff);
		emit_byte (w, DW_LNS_advance_pc);
		emit_sleb128 (w, addr_diff);
		emit_byte (w, DW_LNS_copy);
	}
}

static gint
compare_lne (MonoDebugLineNumberEntry *a, MonoDebugLineNumberEntry *b)
{
	if (a->native_offset == b->native_offset)
		return a->il_offset - b->il_offset;
	else
		return a->native_offset - b->native_offset;
}

static void
emit_line_number_info (MonoDwarfWriter *w, MonoMethod *method,
					   char *start_symbol, char *end_symbol,
					   guint8 *code, guint32 code_size,
					   MonoDebugMethodJitInfo *debug_info)
{
	ERROR_DECL (error);
	guint32 prev_line = 0;
	guint32 prev_native_offset = 0;
	int i, file_index, il_offset, prev_il_offset;
	gboolean first = TRUE;
	MonoDebugSourceLocation *loc;
	char *prev_file_name = NULL;
	MonoMethodHeader *header = mono_method_get_header_checked (method, error);
	MonoDebugMethodInfo *minfo;
	MonoDebugLineNumberEntry *ln_array;
	int *native_to_il_offset = NULL;

	mono_error_assert_ok (error); /* FIXME don't swallow the error */

	if (!w->emit_line) {
		mono_metadata_free_mh (header);
		return;
	}

	minfo = mono_debug_lookup_method (method);

	/* Compute the native->IL offset mapping */

	g_assert (code_size);

	ln_array = g_new0 (MonoDebugLineNumberEntry, debug_info->num_line_numbers);
	memcpy (ln_array, debug_info->line_numbers, debug_info->num_line_numbers * sizeof (MonoDebugLineNumberEntry));

	mono_qsort (ln_array, debug_info->num_line_numbers, sizeof (MonoDebugLineNumberEntry), (int (*)(const void *, const void *))compare_lne);

	native_to_il_offset = g_new0 (int, code_size + 1);

	for (i = 0; i < debug_info->num_line_numbers; ++i) {
		int j;
		MonoDebugLineNumberEntry *lne = &ln_array [i];

		if (i == 0) {
			for (j = 0; j < lne->native_offset; ++j)
				native_to_il_offset [j] = -1;
		}

		if (i < debug_info->num_line_numbers - 1) {
			MonoDebugLineNumberEntry *lne_next = &ln_array [i + 1];

			for (j = lne->native_offset; j < lne_next->native_offset; ++j)
				native_to_il_offset [j] = lne->il_offset;
		} else {
			for (j = lne->native_offset; j < code_size; ++j)
				native_to_il_offset [j] = lne->il_offset;
		}
	}
	g_free (ln_array);

	prev_line = 1;
	prev_il_offset = -1;

	w->cur_file_index = -1;
	for (i = 0; i < code_size; ++i) {
		int line_diff, addr_diff;

		if (!minfo)
			continue;

		if (!debug_info->line_numbers)
			continue;

		if (native_to_il_offset)
			il_offset = native_to_il_offset [i];
		else
			il_offset = il_offset_from_address (method, debug_info, i);
		/*
		il_offset = il_offset_from_address (method, debug_info, i);

		g_assert (il_offset == native_to_il_offset [i]);
		*/

		il_offset = native_to_il_offset [i];
		if (il_offset < 0)
			continue;

		if (il_offset == prev_il_offset)
			continue;

		prev_il_offset = il_offset;

		loc = mono_debug_method_lookup_location (minfo, il_offset);
		if (!loc)
			continue;
		if (!loc->source_file) {
			mono_debug_free_source_location (loc);
			continue;
		}

		line_diff = (gint32)loc->row - (gint32)prev_line;
		addr_diff = i - prev_native_offset;

		if (first) {
			emit_section_change (w, ".debug_line", 0);

			emit_byte (w, 0);
			emit_byte (w, sizeof (target_mgreg_t) + 1);
			emit_byte (w, DW_LNE_set_address);
			if (start_symbol)
				emit_pointer_unaligned (w, start_symbol);
			else
				emit_pointer_value (w, code);
			first = FALSE;
		}

		if (loc->row != prev_line) {
			if (!prev_file_name || strcmp (loc->source_file, prev_file_name) != 0) {
				/* Add an entry to the file table */
				/* FIXME: Avoid duplicates */
				file_index = get_line_number_file_name (w, loc->source_file) + 1;
				g_free (prev_file_name);
				prev_file_name = g_strdup (loc->source_file);

				if (w->cur_file_index != file_index) {
					emit_byte (w, DW_LNS_set_file);
					emit_uleb128 (w, file_index);
					emit_byte (w, DW_LNS_copy);
					w->cur_file_index = file_index;
				}
			}
		}

		if (loc->row != prev_line) {
			if (prev_native_offset == 0)
				emit_byte (w, DW_LNE_set_prologue_end);

			//printf ("X: %p(+0x%x) %d %s:%d(+%d)\n", code + i, addr_diff, loc->il_offset, loc->source_file, loc->row, line_diff);
			emit_advance_op (w, line_diff, addr_diff);

			prev_line = loc->row;
			prev_native_offset = i;
		}

		mono_debug_free_source_location (loc);
		first = FALSE;
	}

	g_free (native_to_il_offset);
	g_free (prev_file_name);

	if (!first) {
		emit_byte (w, DW_LNS_advance_pc);
		emit_sleb128 (w, code_size - prev_native_offset);
		emit_byte (w, DW_LNS_copy);

		emit_byte (w, 0);
		emit_byte (w, 1);
		emit_byte (w, DW_LNE_end_sequence);
	} else if (!start_symbol) {
		/* No debug info, XDEBUG mode */
		char *name, *dis;
		const guint8 *ip = header->code;
		int *il_to_line;

		/*
		 * Emit the IL code into a temporary file and emit line number info
		 * referencing that file.
		 */

		name = mono_method_full_name (method, TRUE);
		fprintf (w->il_file, "// %s\n", name);
		w->il_file_line_index ++;
		g_free (name);

		il_to_line = g_new0 (int, header->code_size);

		emit_section_change (w, ".debug_line", 0);
		emit_byte (w, 0);
		emit_byte (w, sizeof (target_mgreg_t) + 1);
		emit_byte (w, DW_LNE_set_address);
		emit_pointer_value (w, code);

		// FIXME: Optimize this
		while (ip < header->code + header->code_size) {
			/* Emit IL */
			w->il_file_line_index ++;

			dis = disasm_ins (method, ip, &ip);
			fprintf (w->il_file, "%s\n", dis);
			g_free (dis);

			il_to_line [ip - header->code] = w->il_file_line_index;
		}

		/* Emit line number info */
		prev_line = 1;
		prev_native_offset = 0;
		for (i = 0; i < debug_info->num_line_numbers; ++i) {
			MonoDebugLineNumberEntry *lne = &debug_info->line_numbers [i];
			int line;

			if (lne->il_offset >= header->code_size)
				continue;
			line = il_to_line [lne->il_offset];
			if (!line) {
				/*
				 * This seems to happen randomly, it looks like il_offset points
				 * into the middle of an instruction.
				 */
				continue;
				/*
				printf ("%s\n", mono_method_full_name (method, TRUE));
				printf ("%d %d\n", lne->il_offset, header->code_size);
				g_assert (line);
				*/
			}

			if (line - prev_line != 0) {
				emit_advance_op (w, line - prev_line, (gint32)lne->native_offset - prev_native_offset);

				prev_line = line;
				prev_native_offset = lne->native_offset;
			}
		}

		emit_byte (w, DW_LNS_advance_pc);
		emit_sleb128 (w, code_size - prev_native_offset);
		emit_byte (w, DW_LNS_copy);

		emit_byte (w, 0);
		emit_byte (w, 1);
		emit_byte (w, DW_LNE_end_sequence);

		fflush (w->il_file);
		g_free (il_to_line);
	}
	mono_metadata_free_mh (header);
}

static MonoMethodVar*
find_vmv (MonoCompile *cfg, MonoInst *ins)
{
	int j;

	if (cfg->varinfo) {
		for (j = 0; j < cfg->num_varinfo; ++j) {
			if (cfg->varinfo [j] == ins)
				break;
		}

		if (j < cfg->num_varinfo) {
			return MONO_VARINFO (cfg, j);
		}
	}

	return NULL;
}

void
mono_dwarf_writer_emit_method (MonoDwarfWriter *w, MonoCompile *cfg, MonoMethod *method, char *start_symbol, char *end_symbol, char *linkage_name,
							   guint8 *code, guint32 code_size, MonoInst **args, MonoInst **locals, GSList *unwind_info, MonoDebugMethodJitInfo *debug_info)
{
	ERROR_DECL (error);
	char *name;
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	char **names;
	MonoDebugLocalsInfo *locals_info;
	MonoDebugMethodInfo *minfo;
	MonoDebugSourceLocation *loc = NULL;
	int i;
	guint8 buf [128];
	guint8 *p;

	emit_section_change (w, ".debug_info", 0);

	sig = mono_method_signature_internal (method);
	header = mono_method_get_header_checked (method, error);
	mono_error_assert_ok (error); /* FIXME don't swallow the error */

	/* Parameter types */
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		MonoType *t;

		if (i == 0 && sig->hasthis) {
			if (m_class_is_valuetype (method->klass))
				t = m_class_get_this_arg (method->klass);
			else
				t = m_class_get_byval_arg (method->klass);
		} else {
			t = sig->params [i - sig->hasthis];
		}

		emit_type (w, t);
	}
	//emit_type (w, mono_get_int32_type ());

	/* Local types */
	for (i = 0; i < header->num_locals; ++i) {
		emit_type (w, header->locals [i]);
	}

	minfo = mono_debug_lookup_method (method);
	if (minfo)
		loc = mono_debug_method_lookup_location (minfo, 0);

	/* Subprogram */
	names = g_new0 (char *, sig->param_count);
	mono_method_get_param_names (method, (const char **) names);

	emit_uleb128 (w, ABBREV_SUBPROGRAM);
	/* DW_AT_name */
	name = mono_method_full_name (method, FALSE);
	emit_escaped_string (w, name);
	/* DW_AT_MIPS_linkage_name */
	if (linkage_name)
		emit_string (w, linkage_name);
	else
		emit_string (w, "");
	/* DW_AT_decl_file/DW_AT_decl_line */
	if (loc) {
		int file_index = add_line_number_file_name (w, loc->source_file, 0, 0);
		emit_uleb128 (w, file_index + 1);
		emit_uleb128 (w, loc->row);

		mono_debug_free_source_location (loc);
		loc = NULL;
	} else {
		emit_uleb128 (w, 0);
		emit_uleb128 (w, 0);
	}
#ifndef TARGET_IOS
	emit_string (w, name);
#endif
	g_free (name);
	if (start_symbol) {
		emit_pointer_unaligned (w, start_symbol);
		emit_pointer_unaligned (w, end_symbol);
	} else {
		emit_pointer_value (w, code);
		emit_pointer_value (w, code + code_size);
	}
	/* frame_base */
	emit_byte (w, 2);
	emit_byte (w, DW_OP_breg6);
	emit_byte (w, 16);

	/* Parameters */
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		MonoInst *arg = args ? args [i] : NULL;
		MonoType *t;
		const char *pname;
		char pname_buf [128];
		MonoMethodVar *vmv = NULL;
		gboolean need_loclist = FALSE;

		vmv = find_vmv (cfg, arg);
		if (code && vmv && (vmv->live_range_start || vmv->live_range_end))
			need_loclist = TRUE;

		if (i == 0 && sig->hasthis) {
			if (m_class_is_valuetype (method->klass))
				t = m_class_get_this_arg (method->klass);
			else
				t = m_class_get_byval_arg (method->klass);
			pname = "this";
		} else {
			t = sig->params [i - sig->hasthis];
			pname = names [i - sig->hasthis];
		}

		emit_uleb128 (w, need_loclist ? ABBREV_PARAM_LOCLIST : ABBREV_PARAM);
		/* name */
		if (pname[0] == '\0') {
			sprintf (pname_buf, "param%d", i - sig->hasthis);
			pname = pname_buf;
		}
		emit_string (w, pname);
		/* type */
		if (!arg || arg->flags & MONO_INST_IS_DEAD)
			emit_var_type (w, mono_get_int32_type ());
		else
			emit_var_type (w, t);

		p = buf;
		encode_var_location (w, arg, p, &p);
		if (need_loclist) {
			vmv->live_range_start = 0;
			if (vmv->live_range_end == 0)
				/* FIXME: Uses made in calls are not recorded */
				vmv->live_range_end = code_size;
			emit_loclist (w, arg, code + vmv->live_range_start, code + vmv->live_range_end, buf, GPTRDIFF_TO_UINT32 (p - buf));
		} else {
			emit_byte (w, GPTRDIFF_TO_UINT8 (p - buf));
			emit_bytes (w, buf, GPTRDIFF_TO_INT (p - buf));
		}
	}
	g_free (names);

	/* Locals */
	locals_info = mono_debug_lookup_locals (method);

	for (i = 0; i < header->num_locals; ++i) {
		MonoInst *ins = locals [i];
		char name_buf [128];
		int j;
		MonoMethodVar *vmv = NULL;
		gboolean need_loclist = FALSE;
		char *lname;

		/* ins->dreg no longer contains the original vreg */
		vmv = find_vmv (cfg, ins);
		if (code && vmv) {
			if (vmv->live_range_start) {
				/* This variable has a precise live range */
				need_loclist = TRUE;
			}
		}

		emit_uleb128 (w, need_loclist ? ABBREV_VARIABLE_LOCLIST : ABBREV_VARIABLE);
		/* name */
		lname = NULL;
		if (locals_info) {
			for (j = 0; j < locals_info->num_locals; ++j)
				if (locals_info->locals [j].index == i)
					break;
			if (j < locals_info->num_locals)
				lname = locals_info->locals [j].name;
		}
		if (lname) {
			emit_string (w, lname);
		} else {
			sprintf (name_buf, "V_%d", i);
			emit_string (w, name_buf);
		}
		/* type */
		if (!ins || ins->flags & MONO_INST_IS_DEAD)
			emit_var_type (w, mono_get_int32_type ());
		else
			emit_var_type (w, header->locals [i]);

		p = buf;
		encode_var_location (w, ins, p, &p);

		if (need_loclist) {
			if (vmv->live_range_end == 0)
				/* FIXME: Uses made in calls are not recorded */
				vmv->live_range_end = code_size;
			emit_loclist (w, ins, code + vmv->live_range_start, code + vmv->live_range_end, buf, GPTRDIFF_TO_UINT32 (p - buf));
		} else {
			emit_byte (w, GPTRDIFF_TO_UINT8 (p - buf));
			emit_bytes (w, buf, GPTRDIFF_TO_INT (p - buf));
		}
	}

	if (locals_info)
		mono_debug_free_locals (locals_info);

	/* Subprogram end */
	emit_uleb128 (w, 0x0);

	emit_line (w);

	emit_debug_info_end (w);

	/* Emit unwind info */
	if (unwind_info) {
		emit_fde (w, w->fde_index, start_symbol, end_symbol, code, code_size, unwind_info, TRUE);
		w->fde_index ++;
	}

	/* Save the information needed to emit the line number info later at once */
	/* != could happen when using --regression */
	if (debug_info && (debug_info->code_start == code)) {
		MethodLineNumberInfo *info;

		info = g_new0 (MethodLineNumberInfo, 1);
		info->method = method;
		info->start_symbol = g_strdup (start_symbol);
		info->end_symbol = g_strdup (end_symbol);
		info->code = code;
		info->code_size = code_size;
		w->line_info = g_slist_prepend (w->line_info, info);
	}

	emit_line (w);
	mono_metadata_free_mh (header);
}

void
mono_dwarf_writer_emit_trampoline (MonoDwarfWriter *w, const char *tramp_name, char *start_symbol, char *end_symbol, guint8 *code, guint32 code_size, GSList *unwind_info)
{
	emit_section_change (w, ".debug_info", 0);

	/* Subprogram */
	emit_uleb128 (w, ABBREV_TRAMP_SUBPROGRAM);
	emit_string (w, tramp_name);
	emit_pointer_value (w, code);
	emit_pointer_value (w, code + code_size);

	/* Subprogram end */
	emit_uleb128 (w, 0x0);

	emit_debug_info_end (w);

	/* Emit unwind info */
	emit_fde (w, w->fde_index, start_symbol, end_symbol, code, code_size, unwind_info, FALSE);
	w->fde_index ++;
}

#else /* !defined(DISABLE_AOT) && !defined(DISABLE_JIT) */

MONO_EMPTY_SOURCE_FILE (dwarfwriter);

#endif /* End of: !defined(DISABLE_AOT) && !defined(DISABLE_JIT) */
