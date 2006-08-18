/*
 * aot.c: mono Ahead of Time compiler
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include "config.h"
#include <sys/types.h>
#include <unistd.h>
#include <fcntl.h>
#include <string.h>
#ifndef PLATFORM_WIN32
#include <sys/mman.h>
#else
#include <winsock2.h>
#include <windows.h>
#endif

#include <errno.h>
#include <sys/stat.h>
#include <limits.h>    /* for PAGESIZE */
#ifndef PAGESIZE
#define PAGESIZE 4096
#endif

#include <mono/metadata/tabledefs.h>
#include <mono/metadata/class.h>
#include <mono/metadata/object.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/marshal.h>
#include <mono/utils/mono-logger.h>
#include "mono/utils/mono-compiler.h"

#include "mini.h"

#ifndef DISABLE_AOT

#ifdef PLATFORM_WIN32
#define SHARED_EXT ".dll"
#elif defined(__ppc__) && defined(__MACH__)
#define SHARED_EXT ".dylib"
#else
#define SHARED_EXT ".so"
#endif

#if defined(sparc) || defined(__ppc__)
#define AS_STRING_DIRECTIVE ".asciz"
#else
/* GNU as */
#define AS_STRING_DIRECTIVE ".string"
#endif

#define ALIGN_PTR_TO(ptr,align) (gpointer)((((gssize)(ptr)) + (align - 1)) & (~(align - 1)))
#define ROUND_DOWN(VALUE,SIZE)	((VALUE) & ~((SIZE) - 1))

typedef struct MonoAotOptions {
	char *outfile;
	gboolean save_temps;
	gboolean write_symbols;
} MonoAotOptions;

typedef struct MonoAotStats {
	int ccount, mcount, lmfcount, abscount, wrappercount, ocount;
	int code_size, info_size, ex_info_size, got_size, class_info_size, got_info_size, got_info_offsets_size;
	int methods_without_got_slots, direct_calls, all_calls;
	int got_slots;
	int got_slot_types [MONO_PATCH_INFO_NONE];
} MonoAotStats;

/* emit mode */
enum {
	EMIT_NONE,
	EMIT_BYTE,
	EMIT_LONG
};

typedef struct MonoAotCompile {
	MonoImage *image;
	MonoCompile **cfgs;
	GHashTable *patch_to_plt_offset;
	GHashTable *plt_offset_to_patch;
	GHashTable *patch_to_shared_got_offset;
	GPtrArray *shared_patches;
	GHashTable *image_hash;
	GHashTable *method_to_cfg;
	GPtrArray *image_table;
	GList *method_order;
	guint32 got_offset, plt_offset;
	guint32 *method_got_offsets;
	gboolean *has_got_slots;
	MonoAotOptions aot_opts;
	guint32 nmethods;
	guint32 opts;
	MonoMemPool *mempool;
	MonoAotStats stats;
	FILE *fp;
	char *tmpfname;
	int mode; /* emit mode */
	int col_count; /* bytes emitted per .byte line */
} MonoAotCompile;

/* Keep in synch with MonoJumpInfoType */
static const char* patch_types [] = {
	"bb",
	"abs",
	"label",
	"method",
	"method_jump",
	"method_rel",
	"methodconst",
	"internal_method",
	"switch",
	"exc",
	"exc_name",
	"class",
	"image",
	"field",
	"vtable",
	"class_init",
	"sflda",
	"ldstr",
	"ldtoken",
	"type_from_handle",
	"r4",
	"r8",
	"ip",
	"iid",
	"adjusted_iid",
	"bb_ovf",
	"exc_ovf",
	"wrapper",
	"got_offset",
	"declsec",
	"none"
};

static gboolean 
is_got_patch (MonoJumpInfoType patch_type)
{
#ifdef __x86_64__
	return TRUE;
#elif defined(__i386__)
	return TRUE;
#else
	return FALSE;
#endif
}

#if defined(__ppc__) && defined(__MACH__)
static int
ilog2(register int value)
{
	int count = -1;
	while (value & ~0xf) count += 4, value >>= 4;
	while (value) count++, value >>= 1;
	return count;
}
#endif

static void
emit_start (MonoAotCompile *acfg)
{
	int i = g_file_open_tmp ("mono_aot_XXXXXX", &acfg->tmpfname, NULL);
	acfg->fp = fdopen (i, "w+");
	g_assert (acfg->fp);
}

static void
emit_unset_mode (MonoAotCompile *acfg)
{
	if (acfg->mode == EMIT_NONE)
		return;
	fprintf (acfg->fp, "\n");
	acfg->mode = EMIT_NONE;
}

static void
emit_section_change (MonoAotCompile *acfg, const char *section_name, int subsection_index)
{
	emit_unset_mode (acfg);
#if defined(PLATFORM_WIN32)
	fprintf (acfg->fp, ".section %s\n", section_name);
#elif defined(sparc)
	/* For solaris as, GNU as should accept the same */
	fprintf (acfg->fp, ".section \"%s\"\n", section_name);
#elif defined(__ppc__) && defined(__MACH__)
	/* This needs to be made more precise on mach. */
	fprintf (acfg->fp, "%s\n", subsection_index == 0 ? ".text" : ".data");
#else
	fprintf (acfg->fp, "%s %d\n", section_name, subsection_index);
#endif
}

static void
emit_symbol_type (MonoAotCompile *acfg, const char *name, gboolean func)
{
	const char *stype;

	if (func)
		stype = "function";
	else
		stype = "object";

	emit_unset_mode (acfg);
#if defined(sparc)
	fprintf (acfg->fp, "\t.type %s,#%s\n", name, stype);
#elif defined(PLATFORM_WIN32)

#elif !(defined(__ppc__) && defined(__MACH__))
	fprintf (acfg->fp, "\t.type %s,@%s\n", name, stype);
#elif defined(__x86_64__) || defined(__i386__)
	fprintf (acfg->fp, "\t.type %s,@%s\n", name, stype);
#endif
}

static void
emit_global (MonoAotCompile *acfg, const char *name, gboolean func)
{
	emit_unset_mode (acfg);
#if  (defined(__ppc__) && defined(__MACH__)) || defined(PLATFORM_WIN32)
    // mach-o always uses a '_' prefix.
	fprintf (acfg->fp, "\t.globl _%s\n", name);
#else
	fprintf (acfg->fp, "\t.globl %s\n", name);
#endif

	emit_symbol_type (acfg, name, func);
}

static void
emit_label (MonoAotCompile *acfg, const char *name)
{
	emit_unset_mode (acfg);
#if (defined(__ppc__) && defined(__MACH__)) || defined(PLATFORM_WIN32)
    // mach-o always uses a '_' prefix.
	fprintf (acfg->fp, "_%s:\n", name);
#else
	fprintf (acfg->fp, "%s:\n", name);
#endif

#if defined(PLATFORM_WIN32)
	/* Emit a normal label too */
	fprintf (acfg->fp, "%s:\n", name);
#endif
}

static void
emit_string (MonoAotCompile *acfg, const char *value)
{
	emit_unset_mode (acfg);
	fprintf (acfg->fp, "\t%s \"%s\"\n", AS_STRING_DIRECTIVE, value);
}

static void
emit_line (MonoAotCompile *acfg)
{
	emit_unset_mode (acfg);
	fprintf (acfg->fp, "\n");
}

static void
emit_string_symbol (MonoAotCompile *acfg, const char *name, const char *value)
{
	emit_unset_mode (acfg);
	emit_section_change (acfg, ".text", 1);
	emit_global (acfg, name, FALSE);
	emit_label (acfg, name);
	emit_string (acfg, value);
}

static void 
emit_alignment (MonoAotCompile *acfg, int size)
{
	emit_unset_mode (acfg);
#if defined(__ppc__) && defined(__MACH__)
	// the mach-o assembler specifies alignments as powers of 2.
	fprintf (acfg->fp, "\t.align %d\t; ilog2\n", ilog2(size));
#elif defined(__powerpc__)
	/* ignore on linux/ppc */
#else
	fprintf (acfg->fp, "\t.align %d\n", size);
#endif
}

static void
emit_pointer (MonoAotCompile *acfg, const char *target)
{
	emit_unset_mode (acfg);
	emit_alignment (acfg, sizeof (gpointer));
#if defined(__x86_64__)
	fprintf (acfg->fp, "\t.quad %s\n", target);
#elif defined(sparc) && SIZEOF_VOID_P == 8
	fprintf (acfg->fp, "\t.xword %s\n", target);
#else
	fprintf (acfg->fp, "\t.long %s\n", target);
#endif
}

static void
emit_bytes (MonoAotCompile *acfg, const guint8* buf, int size)
{
	int i;
	if (acfg->mode != EMIT_BYTE) {
		acfg->mode = EMIT_BYTE;
		acfg->col_count = 0;
	}
	for (i = 0; i < size; ++i, ++acfg->col_count) {
		if ((acfg->col_count % 32) == 0)
			fprintf (acfg->fp, "\n\t.byte ");
		else
			fprintf (acfg->fp, ", ");
		fprintf (acfg->fp, "0x%x", buf [i]);
	}
}

static void
emit_int32 (MonoAotCompile *acfg, int value)
{
	if (acfg->mode != EMIT_LONG) {
		acfg->mode = EMIT_LONG;
		acfg->col_count = 0;
	}
	if ((acfg->col_count++ % 8) == 0)
		fprintf (acfg->fp, "\n\t.long ");
	else
		fprintf (acfg->fp, ", ");
	fprintf (acfg->fp, "%d", value);
}

static void
emit_symbol_diff (MonoAotCompile *acfg, const char *end, const char* start, int offset)
{
	if (acfg->mode != EMIT_LONG) {
		acfg->mode = EMIT_LONG;
		acfg->col_count = 0;
	}
	if ((acfg->col_count++ % 8) == 0)
		fprintf (acfg->fp, "\n\t.long ");
	else
		fprintf (acfg->fp, ", ");
	if (offset)
		fprintf (acfg->fp, "%s - %s %c %d", end, start, offset < 0? ' ': '+', offset);
	else
		fprintf (acfg->fp, "%s - %s", end, start);
}

static void
emit_zero_bytes (MonoAotCompile *acfg, int num)
{
	emit_unset_mode (acfg);
	fprintf (acfg->fp, "\t.skip %d\n", num);
}

static void
emit_writeout (MonoAotCompile *acfg)
{
	char *command, *objfile;
	char *outfile_name, *tmp_outfile_name;

	fclose (acfg->fp);

#if defined(__x86_64__)
#define AS_OPTIONS "--64"
#elif defined(sparc) && SIZEOF_VOID_P == 8
#define AS_OPTIONS "-xarch=v9"
#else
#define AS_OPTIONS ""
#endif
	command = g_strdup_printf ("as %s %s -o %s.o", AS_OPTIONS, acfg->tmpfname, acfg->tmpfname);
	printf ("Executing the native assembler: %s\n", command);
	if (system (command) != 0) {
		g_free (command);
		return;
	}

	g_free (command);

	if (acfg->aot_opts.outfile)
		outfile_name = g_strdup_printf ("%s", acfg->aot_opts.outfile);
	else
		outfile_name = g_strdup_printf ("%s%s", acfg->image->name, SHARED_EXT);

	tmp_outfile_name = g_strdup_printf ("%s.tmp", outfile_name);

#if defined(sparc)
	command = g_strdup_printf ("ld -shared -G -o %s %s.o", outfile_name, acfg->tmpfname);
#elif defined(__ppc__) && defined(__MACH__)
	command = g_strdup_printf ("gcc -dynamiclib -o %s %s.o", outfile_name, acfg->tmpfname);
#elif defined(PLATFORM_WIN32)
	command = g_strdup_printf ("gcc -shared --dll -mno-cygwin -o %s %s.o", outfile_name, acfg->tmpfname);
#else
	command = g_strdup_printf ("ld -shared -o %s %s.o", outfile_name, acfg->tmpfname);
#endif
	printf ("Executing the native linker: %s\n", command);
	if (system (command) != 0) {
		g_free (tmp_outfile_name);
		g_free (outfile_name);
		g_free (command);
		return;
	}

	g_free (command);
	objfile = g_strdup_printf ("%s.o", acfg->tmpfname);
	unlink (objfile);
	g_free (objfile);
	/*com = g_strdup_printf ("strip --strip-unneeded %s%s", acfg->image->name, SHARED_EXT);
	printf ("Stripping the binary: %s\n", com);
	system (com);
	g_free (com);*/

	rename (tmp_outfile_name, outfile_name);

	g_free (tmp_outfile_name);
	g_free (outfile_name);

	if (acfg->aot_opts.save_temps)
		printf ("Retained input file.\n");
	else
		unlink (acfg->tmpfname);

}

static void
emit_byte (MonoAotCompile *acfg, guint8 val)
{
	emit_bytes (acfg, &val, 1);
}

static guint32
mono_get_field_token (MonoClassField *field) 
{
	MonoClass *klass = field->parent;
	int i;

	for (i = 0; i < klass->field.count; ++i) {
		if (field == &klass->fields [i])
			return MONO_TOKEN_FIELD_DEF | (klass->field.first + 1 + i);
	}

	g_assert_not_reached ();
	return 0;
}

static inline void
encode_value (gint32 value, guint8 *buf, guint8 **endbuf)
{
	guint8 *p = buf;

	//printf ("ENCODE: %d 0x%x.\n", value, value);

	/* 
	 * Same encoding as the one used in the metadata, extended to handle values
	 * greater than 0x1fffffff.
	 */
	if ((value >= 0) && (value <= 127))
		*p++ = value;
	else if ((value >= 0) && (value <= 16383)) {
		p [0] = 0x80 | (value >> 8);
		p [1] = value & 0xff;
		p += 2;
	} else if ((value >= 0) && (value <= 0x1fffffff)) {
		p [0] = (value >> 24) | 0xc0;
		p [1] = (value >> 16) & 0xff;
		p [2] = (value >> 8) & 0xff;
		p [3] = value & 0xff;
		p += 4;
	}
	else {
		p [0] = 0xff;
		p [1] = (value >> 24) & 0xff;
		p [2] = (value >> 16) & 0xff;
		p [3] = (value >> 8) & 0xff;
		p [4] = value & 0xff;
		p += 5;
	}
	if (endbuf)
		*endbuf = p;
}

static guint32
get_image_index (MonoAotCompile *cfg, MonoImage *image)
{
	guint32 index;

	index = GPOINTER_TO_UINT (g_hash_table_lookup (cfg->image_hash, image));
	if (index)
		return index - 1;
	else {
		index = g_hash_table_size (cfg->image_hash);
		g_hash_table_insert (cfg->image_hash, image, GUINT_TO_POINTER (index + 1));
		g_ptr_array_add (cfg->image_table, image);
		return index;
	}
}

static void
encode_klass_info (MonoAotCompile *cfg, MonoClass *klass, guint8 *buf, guint8 **endbuf)
{
	if (!klass->type_token) {
		/* Array class */
		g_assert (klass->rank > 0);
		g_assert (klass->element_class->type_token);
		encode_value (MONO_TOKEN_TYPE_DEF, buf, &buf);
		encode_value (get_image_index (cfg, klass->image), buf, &buf);
		g_assert (mono_metadata_token_code (klass->element_class->type_token) == MONO_TOKEN_TYPE_DEF);
		encode_value (klass->element_class->type_token - MONO_TOKEN_TYPE_DEF, buf, &buf);
		encode_value (klass->rank, buf, &buf);
	}
	else {
		g_assert (mono_metadata_token_code (klass->type_token) == MONO_TOKEN_TYPE_DEF);
		encode_value (klass->type_token - MONO_TOKEN_TYPE_DEF, buf, &buf);
		encode_value (get_image_index (cfg, klass->image), buf, &buf);
	}
	*endbuf = buf;
}

static void
encode_field_info (MonoAotCompile *cfg, MonoClassField *field, guint8 *buf, guint8 **endbuf)
{
	guint32 token = mono_get_field_token (field);

	encode_klass_info (cfg, field->parent, buf, &buf);
	g_assert (mono_metadata_token_code (token) == MONO_TOKEN_FIELD_DEF);
	encode_value (token - MONO_TOKEN_FIELD_DEF, buf, &buf);
	*endbuf = buf;
}

static void
encode_method_ref (MonoAotCompile *acfg, MonoMethod *method, guint8 *buf, guint8 **endbuf)
{
	guint32 image_index = get_image_index (acfg, method->klass->image);
	guint32 token = method->token;
	g_assert (image_index < 256);
	g_assert (mono_metadata_token_table (token) == MONO_TABLE_METHOD);

	encode_value ((image_index << 24) + (mono_metadata_token_index (token)), buf, &buf);
	*endbuf = buf;
}

static gint
compare_patches (gconstpointer a, gconstpointer b)
{
	int i, j;

	i = (*(MonoJumpInfo**)a)->ip.i;
	j = (*(MonoJumpInfo**)b)->ip.i;

	if (i < j)
		return -1;
	else
		if (i > j)
			return 1;
	else
		return 0;
}

static int
get_plt_index (MonoAotCompile *acfg, MonoJumpInfo *patch_info)
{
	int res = -1;
	int idx;

	switch (patch_info->type) {
	case MONO_PATCH_INFO_METHOD:
	case MONO_PATCH_INFO_WRAPPER:
	case MONO_PATCH_INFO_INTERNAL_METHOD:
	case MONO_PATCH_INFO_CLASS_INIT: {
		MonoJumpInfo *new_ji = mono_patch_info_dup_mp (acfg->mempool, patch_info);
		gpointer patch_id = NULL;

		/* First check for an existing patch */
		switch (patch_info->type) {
		case MONO_PATCH_INFO_METHOD:
			patch_id = patch_info->data.method;
			break;
		case MONO_PATCH_INFO_INTERNAL_METHOD:
			patch_id = (gpointer)patch_info->data.name;
			break;
		case MONO_PATCH_INFO_CLASS_INIT:
			patch_id = patch_info->data.klass;
			break;
		case MONO_PATCH_INFO_WRAPPER:
			/* A bit ugly, but works */
			g_assert (patch_info->data.method->wrapper_type < sizeof (MonoMethod));
			patch_id = (gpointer)(((guint8*)patch_info->data.method) + patch_info->data.method->wrapper_type);
			break;
		default:
			g_assert_not_reached ();
		}

		if (patch_id) {
			idx = GPOINTER_TO_UINT (g_hash_table_lookup (acfg->patch_to_plt_offset, patch_id));
			if (idx)
				res = idx;
			else
				g_hash_table_insert (acfg->patch_to_plt_offset, patch_id, GUINT_TO_POINTER (acfg->plt_offset));
		}

		if (res == -1) {
			res = acfg->plt_offset;
			g_hash_table_insert (acfg->plt_offset_to_patch, GUINT_TO_POINTER (acfg->plt_offset), new_ji);
			acfg->plt_offset ++;
		}

		/* Nullify the patch */
		patch_info->type = MONO_PATCH_INFO_NONE;

		return res;
	}
	default:
		return -1;
	}
}

/**
 * get_got_offset:
 *
 *   Returns the offset of the GOT slot where the runtime object resulting from resolving
 * JI could be found if it exists, otherwise allocates a new one.
 */
static guint32
get_got_offset (MonoAotCompile *acfg, MonoJumpInfo *ji)
{
	guint32 got_offset;

	got_offset = GPOINTER_TO_UINT (g_hash_table_lookup (acfg->patch_to_shared_got_offset, ji));
	if (got_offset)
		return got_offset - 1;

	got_offset = acfg->got_offset;
	acfg->got_offset ++;

	acfg->stats.got_slots ++;
	acfg->stats.got_slot_types [ji->type] ++;

	return got_offset;
}

static guint32
get_shared_got_offset (MonoAotCompile *acfg, MonoJumpInfo *ji)
{
	MonoJumpInfo *copy;
	guint32 got_offset;

	if (!g_hash_table_lookup (acfg->patch_to_shared_got_offset, ji)) {
		got_offset = get_got_offset (acfg, ji);
		copy = mono_patch_info_dup_mp (acfg->mempool, ji);
		g_hash_table_insert (acfg->patch_to_shared_got_offset, copy, GUINT_TO_POINTER (got_offset + 1));
		g_ptr_array_add (acfg->shared_patches, copy);
	}

	return get_got_offset (acfg, ji);
}

static void
emit_method_code (MonoAotCompile *acfg, MonoCompile *cfg)
{
	MonoMethod *method;
	int i, pindex, method_index;
	guint8 *code;
	char *symbol;
	int func_alignment = 16;
	GPtrArray *patches;
	MonoJumpInfo *patch_info;
	MonoMethodHeader *header;
	gboolean skip;
	guint32 got_slot;

	method = cfg->method;
	code = cfg->native_code;
	header = mono_method_get_header (method);

	method_index = mono_metadata_token_index (method->token);

	/* Make the labels local */
	symbol = g_strdup_printf (".Lm_%x", method_index);

	emit_alignment (acfg, func_alignment);
	emit_label (acfg, symbol);
	if (acfg->aot_opts.write_symbols)
		emit_global (acfg, symbol, TRUE);

	if (cfg->verbose_level > 0)
		g_print ("Method %s emitted as %s\n", mono_method_full_name (method, TRUE), symbol);

	acfg->stats.code_size += cfg->code_len;

	/* Collect and sort relocations */
	patches = g_ptr_array_new ();
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next)
		g_ptr_array_add (patches, patch_info);
	g_ptr_array_sort (patches, compare_patches);

	acfg->method_got_offsets [method_index] = acfg->got_offset;
	for (i = 0; i < cfg->code_len; i++) {
		patch_info = NULL;
		for (pindex = 0; pindex < patches->len; ++pindex) {
			patch_info = g_ptr_array_index (patches, pindex);
			if (patch_info->ip.i == i)
				break;
		}

#ifdef MONO_ARCH_HAVE_PIC_AOT

		skip = FALSE;
		if (patch_info && (pindex < patches->len)) {
			switch (patch_info->type) {
			case MONO_PATCH_INFO_LABEL:
			case MONO_PATCH_INFO_BB:
			case MONO_PATCH_INFO_NONE:
				break;
			case MONO_PATCH_INFO_GOT_OFFSET: {
				guint32 offset = mono_arch_get_patch_offset (code + i);
				emit_bytes (acfg, code + i, offset);
				emit_symbol_diff (acfg, "got", ".", offset);

				i += offset + 4 - 1;
				skip = TRUE;
				break;
			}
			default: {
				int plt_index;
				char *direct_call_target;

				if (!is_got_patch (patch_info->type))
					break;

				/*
				 * If this patch is a call, try emitting a direct call instead of
				 * through a PLT entry. This is possible if the called method is in
				 * the same assembly and requires no initialization.
				 */
				direct_call_target = NULL;
				if ((patch_info->type == MONO_PATCH_INFO_METHOD) && (patch_info->data.method->klass->image == cfg->method->klass->image)) {
					MonoCompile *callee_cfg = g_hash_table_lookup (acfg->method_to_cfg, patch_info->data.method);
					if (callee_cfg) {
						guint32 callee_idx = mono_metadata_token_index (callee_cfg->method->token);
						if (!acfg->has_got_slots [callee_idx] && (callee_cfg->method->klass->flags & TYPE_ATTRIBUTE_BEFORE_FIELD_INIT)) {
							//printf ("DIRECT: %s %s\n", mono_method_full_name (cfg->method, TRUE), mono_method_full_name (callee_cfg->method, TRUE));
							direct_call_target = g_strdup_printf (".Lm_%x", mono_metadata_token_index (callee_cfg->method->token));
							patch_info->type = MONO_PATCH_INFO_NONE;
							acfg->stats.direct_calls ++;
						}
					}

					acfg->stats.all_calls ++;
				}

				if (!direct_call_target) {
					plt_index = get_plt_index (acfg, patch_info);
					if (plt_index != -1) {
						/* This patch has a PLT entry, so we must emit a call to the PLT entry */
						direct_call_target = g_strdup_printf (".Lp_%d", plt_index);
					}
				}

				if (direct_call_target) {
#if defined(__i386__) || defined(__x86_64__)
					g_assert (code [i] == 0xe8);
					/* Need to make sure this is exactly 5 bytes long */
					emit_byte (acfg, '\xe8');
					emit_symbol_diff (acfg, direct_call_target, ".", -4);
					i += 4;
#else
					g_assert_not_reached ();
#endif
				} else {
					got_slot = get_got_offset (acfg, patch_info);

					emit_bytes (acfg, code + i, mono_arch_get_patch_offset (code + i));
#ifdef __x86_64__
					emit_symbol_diff (acfg, "got", ".", (unsigned int) ((got_slot * sizeof (gpointer)) - 4));
#elif defined(__i386__)
					emit_int32 (acfg, (unsigned int) ((got_slot * sizeof (gpointer))));
#endif
					
					i += mono_arch_get_patch_offset (code + i) + 4 - 1;
				}
				skip = TRUE;
			}
			}
		}
#endif /* MONO_ARCH_HAVE_PIC_AOT */

		if (!skip)
			emit_bytes (acfg, code + i, 1);
	}
	emit_line (acfg);
}

/**
 * encode_patch:
 *
 *  Encode PATCH_INFO into its disk representation. If SHARED is true, encode some types
 * of patches by allocating a GOT entry for them, and encode the GOT offset instead.
 */
static void
encode_patch (MonoAotCompile *acfg, MonoJumpInfo *patch_info, guint8 *buf, guint8 **endbuf, gboolean shared)
{
	guint8 *p = buf;

	switch (patch_info->type) {
	case MONO_PATCH_INFO_NONE:
		break;
	case MONO_PATCH_INFO_IMAGE:
		encode_value (get_image_index (acfg, patch_info->data.image), p, &p);
		break;
	case MONO_PATCH_INFO_METHOD_REL:
		encode_value ((gint)patch_info->data.offset, p, &p);
		break;
	case MONO_PATCH_INFO_SWITCH: {
		gpointer *table = (gpointer *)patch_info->data.table->table;
		int k;

		encode_value (patch_info->data.table->table_size, p, &p);
		for (k = 0; k < patch_info->data.table->table_size; k++)
			encode_value ((int)(gssize)table [k], p, &p);
		break;
	}
	case MONO_PATCH_INFO_METHODCONST:
	case MONO_PATCH_INFO_METHOD:
	case MONO_PATCH_INFO_METHOD_JUMP:
		encode_method_ref (acfg, patch_info->data.method, p, &p);
		break;
	case MONO_PATCH_INFO_INTERNAL_METHOD: {
		guint32 len = strlen (patch_info->data.name);

		encode_value (len, p, &p);

		memcpy (p, patch_info->data.name, len);
		p += len;
		*p++ = '\0';
		break;
	}
	case MONO_PATCH_INFO_LDSTR: {
		guint32 image_index = get_image_index (acfg, patch_info->data.token->image);
		guint32 token = patch_info->data.token->token;
		g_assert (mono_metadata_token_code (token) == MONO_TOKEN_STRING);
		/* 
		 * An optimization would be to emit shared code for ldstr 
		 * statements followed by a throw.
		 */
		encode_value (image_index, p, &p);
		encode_value (patch_info->data.token->token - MONO_TOKEN_STRING, p, &p);
		break;
	}
	case MONO_PATCH_INFO_DECLSEC:
	case MONO_PATCH_INFO_LDTOKEN:
	case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
		if (shared) {
			guint32 offset = get_got_offset (acfg, patch_info);
			encode_value (offset, p, &p);
		} else {
			encode_value (get_image_index (acfg, patch_info->data.token->image), p, &p);
			encode_value (patch_info->data.token->token, p, &p);
		}
		break;
	case MONO_PATCH_INFO_EXC_NAME: {
		MonoClass *ex_class;

		ex_class =
			mono_class_from_name (mono_defaults.exception_class->image,
								  "System", patch_info->data.target);
		g_assert (ex_class);
		encode_klass_info (acfg, ex_class, p, &p);
		break;
	}
	case MONO_PATCH_INFO_R4:
		encode_value (*((guint32 *)patch_info->data.target), p, &p);
		break;
	case MONO_PATCH_INFO_R8:
		encode_value (*((guint32 *)patch_info->data.target), p, &p);
		encode_value (*(((guint32 *)patch_info->data.target) + 1), p, &p);
		break;
	case MONO_PATCH_INFO_VTABLE:
	case MONO_PATCH_INFO_CLASS:
	case MONO_PATCH_INFO_IID:
	case MONO_PATCH_INFO_ADJUSTED_IID:
		if (shared) {
			guint32 offset = get_got_offset (acfg, patch_info);
			encode_value (offset, p, &p);
		} else {
			encode_klass_info (acfg, patch_info->data.klass, p, &p);
		}
		break;
	case MONO_PATCH_INFO_CLASS_INIT:
		encode_klass_info (acfg, patch_info->data.klass, p, &p);
		break;
	case MONO_PATCH_INFO_FIELD:
	case MONO_PATCH_INFO_SFLDA:
		if (shared) {
			guint32 offset = get_got_offset (acfg, patch_info);
			encode_value (offset, p, &p);
		} else {
			encode_field_info (acfg, patch_info->data.field, p, &p);
		}
		break;
	case MONO_PATCH_INFO_WRAPPER: {
		encode_value (patch_info->data.method->wrapper_type, p, &p);

		switch (patch_info->data.method->wrapper_type) {
		case MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK: {
			MonoMethod *m;
			guint32 image_index;
			guint32 token;

			m = mono_marshal_method_from_wrapper (patch_info->data.method);
			image_index = get_image_index (acfg, m->klass->image);
			token = m->token;
			g_assert (image_index < 256);
			g_assert (mono_metadata_token_table (token) == MONO_TABLE_METHOD);

			encode_value ((image_index << 24) + (mono_metadata_token_index (token)), p, &p);
			break;
		}
		case MONO_WRAPPER_PROXY_ISINST:
		case MONO_WRAPPER_LDFLD:
		case MONO_WRAPPER_LDFLDA:
		case MONO_WRAPPER_STFLD:
		case MONO_WRAPPER_LDFLD_REMOTE:
		case MONO_WRAPPER_STFLD_REMOTE:
		case MONO_WRAPPER_ISINST: {
			MonoClass *proxy_class = (MonoClass*)mono_marshal_method_from_wrapper (patch_info->data.method);
			encode_klass_info (acfg, proxy_class, p, &p);
			break;
		}
		case MONO_WRAPPER_STELEMREF:
			break;
		default:
			g_assert_not_reached ();
		}
		break;
	}
	default:
		g_warning ("unable to handle jump info %d", patch_info->type);
		g_assert_not_reached ();
	}

	*endbuf = p;
}

static void
emit_method_info (MonoAotCompile *acfg, MonoCompile *cfg)
{
	MonoMethod *method;
	GList *l;
	int j, pindex, buf_size, n_patches;
	guint8 *code;
	char *symbol;
	GPtrArray *patches;
	MonoJumpInfo *patch_info;
	MonoMethodHeader *header;
	guint32 last_offset, method_idx;
	guint8 *p, *buf;
	guint32 first_got_offset;

	method = cfg->method;
	code = cfg->native_code;
	header = mono_method_get_header (method);

	method_idx = mono_metadata_token_index (method->token);

	/* Make the labels local */
	symbol = g_strdup_printf (".Lm_%x_p", method_idx);

	/* Sort relocations */
	patches = g_ptr_array_new ();
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next)
		g_ptr_array_add (patches, patch_info);
	g_ptr_array_sort (patches, compare_patches);

	first_got_offset = acfg->method_got_offsets [mono_metadata_token_index (cfg->method->token)];

	/**********************/
	/* Encode method info */
	/**********************/

	buf_size = (patches->len < 1000) ? 40960 : 40960 + (patches->len * 64);
	p = buf = g_malloc (buf_size);

	if (mono_class_get_cctor (method->klass))
		encode_klass_info (acfg, method->klass, p, &p);
	else
		/* Not needed when loading the method */
		encode_value (0, p, &p);

	/* String table */
	if (cfg->opt & MONO_OPT_SHARED) {
		encode_value (g_list_length (cfg->ldstr_list), p, &p);
		for (l = cfg->ldstr_list; l; l = l->next) {
			encode_value ((long)l->data, p, &p);
		}
	}
	else
		/* Used only in shared mode */
		g_assert (!cfg->ldstr_list);

	n_patches = 0;
	for (pindex = 0; pindex < patches->len; ++pindex) {
		patch_info = g_ptr_array_index (patches, pindex);
		
		if ((patch_info->type == MONO_PATCH_INFO_LABEL) ||
			(patch_info->type == MONO_PATCH_INFO_BB) ||
			(patch_info->type == MONO_PATCH_INFO_GOT_OFFSET) ||
			(patch_info->type == MONO_PATCH_INFO_NONE)) {
			patch_info->type = MONO_PATCH_INFO_NONE;
			/* Nothing to do */
			continue;
		}

		if ((patch_info->type == MONO_PATCH_INFO_IMAGE) && (patch_info->data.image == acfg->image)) {
			/* Stored in a GOT slot initialized at module load time */
			patch_info->type = MONO_PATCH_INFO_NONE;
			continue;
		}

		n_patches ++;
	}

	if (n_patches)
		g_assert (acfg->has_got_slots [method_idx]);

	encode_value (n_patches, p, &p);

	if (n_patches)
		encode_value (first_got_offset, p, &p);

	/* First encode the type+position table */
	last_offset = 0;
	j = 0;
	for (pindex = 0; pindex < patches->len; ++pindex) {
		guint32 offset;
		patch_info = g_ptr_array_index (patches, pindex);
		
		if (patch_info->type == MONO_PATCH_INFO_NONE)
			/* Nothing to do */
			continue;

		j ++;
		//printf ("T: %d O: %d.\n", patch_info->type, patch_info->ip.i);
		offset = patch_info->ip.i - last_offset;
		last_offset = patch_info->ip.i;

		/* Only the type is needed */
		*p = patch_info->type;
		p++;
	}

	/*
	if (n_patches) {
		printf ("%s:\n", mono_method_full_name (cfg->method, TRUE));
		for (pindex = 0; pindex < patches->len; ++pindex) {
			patch_info = g_ptr_array_index (patches, pindex);
			if (patch_info->type != MONO_PATCH_INFO_NONE) {
				printf ("\t%s", patch_types [patch_info->type]);
				if (patch_info->type == MONO_PATCH_INFO_VTABLE)
					printf (": %s\n", patch_info->data.klass->name);
				else
					printf ("\n");
			}
		}
	}
	*/

	/* Then encode the other info */
	for (pindex = 0; pindex < patches->len; ++pindex) {
		patch_info = g_ptr_array_index (patches, pindex);

		encode_patch (acfg, patch_info, p, &p, TRUE);
	}

	acfg->stats.info_size += p - buf;

	/* Emit method info */

	emit_label (acfg, symbol);

	g_assert (p - buf < buf_size);
	emit_bytes (acfg, buf, p - buf);
	g_free (buf);

	g_free (symbol);
}

static void
emit_exception_debug_info (MonoAotCompile *acfg, MonoCompile *cfg)
{
	MonoMethod *method;
	int k, buf_size;
	guint32 debug_info_size;
	guint8 *code;
	char *symbol;
	MonoMethodHeader *header;
	guint8 *p, *buf, *debug_info;

	method = cfg->method;
	code = cfg->native_code;
	header = mono_method_get_header (method);

	/* Make the labels local */
	symbol = g_strdup_printf (".Le_%x_p", mono_metadata_token_index (method->token));

	buf_size = header->num_clauses * 256 + 128;
	p = buf = g_malloc (buf_size);

	encode_value (cfg->code_len, p, &p);
	encode_value (cfg->used_int_regs, p, &p);

	/* Exception table */
	if (header->num_clauses) {
		MonoJitInfo *jinfo = cfg->jit_info;

		for (k = 0; k < header->num_clauses; ++k) {
			MonoJitExceptionInfo *ei = &jinfo->clauses [k];

			encode_value (ei->exvar_offset, p, &p);

			if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER)
				encode_value ((gint)((guint8*)ei->data.filter - code), p, &p);

			encode_value ((gint)((guint8*)ei->try_start - code), p, &p);
			encode_value ((gint)((guint8*)ei->try_end - code), p, &p);
			encode_value ((gint)((guint8*)ei->handler_start - code), p, &p);
		}
	}

	mono_debug_serialize_debug_info (cfg, &debug_info, &debug_info_size);

	encode_value (debug_info_size, p, &p);
	if (debug_info_size) {
		memcpy (p, debug_info, debug_info_size);
		p += debug_info_size;
		g_free (debug_info);
	}

	acfg->stats.ex_info_size += p - buf;

	/* Emit info */

	emit_label (acfg, symbol);

	g_assert (p - buf < buf_size);
	emit_bytes (acfg, buf, p - buf);
	g_free (buf);

	g_free (symbol);
}

static void
emit_klass_info (MonoAotCompile *acfg, guint32 token)
{
	MonoClass *klass = mono_class_get (acfg->image, token);
	guint8 *p, *buf;
	int i, buf_size;
	char *label;
	gboolean no_special_static;

	buf_size = 10240;
	p = buf = g_malloc (buf_size);

	g_assert (klass);

	mono_class_init (klass);

	/* 
	 * Emit all the information which is required for creating vtables so
	 * the runtime does not need to create the MonoMethod structures which
	 * take up a lot of space.
	 */

	no_special_static = !mono_class_has_special_static_fields (klass);

	if (1) {
		encode_value (klass->vtable_size, p, &p);
		encode_value ((no_special_static << 7) | (klass->has_static_refs << 6) | (klass->has_references << 5) | ((klass->blittable << 4) | (klass->nested_classes ? 1 : 0) << 3) | (klass->has_cctor << 2) | (klass->has_finalize << 1) | klass->ghcimpl, p, &p);
		if (klass->has_cctor)
			encode_method_ref (acfg, mono_class_get_cctor (klass), p, &p);
		if (klass->has_finalize)
			encode_method_ref (acfg, mono_class_get_finalizer (klass), p, &p);
 
		encode_value (klass->instance_size, p, &p);
		encode_value (klass->class_size, p, &p);
		encode_value (klass->packing_size, p, &p);
		encode_value (klass->min_align, p, &p);

		for (i = 0; i < klass->vtable_size; ++i) {
			MonoMethod *cm = klass->vtable [i];

			if (cm)
				encode_method_ref (acfg, cm, p, &p);
			else
				encode_value (0, p, &p);
		}
	}

	acfg->stats.class_info_size += p - buf;

	/* Emit the info */
	label = g_strdup_printf (".LK_I_%x", token - MONO_TOKEN_TYPE_DEF - 1);
	emit_label (acfg, label);

	g_assert (p - buf < buf_size);
	emit_bytes (acfg, buf, p - buf);
	g_free (buf);
}

/*
 * Calls made from AOTed code are routed through a table of jumps similar to the
 * ELF PLT (Program Linkage Table). The differences are the following:
 * - the ELF PLT entries make an indirect jump though the GOT so they expect the
 *   GOT pointer to be in EBX. We want to avoid this, so our table contains direct
 *   jumps. This means the jumps need to be patched when the address of the callee is
 *   known. Initially the PLT entries jump to code which transfer control to the
 *   AOT runtime through the first PLT entry.
 */
static void
emit_plt (MonoAotCompile *acfg)
{
	char *symbol;
	int i, buf_size;
	guint8 *p, *buf;
	guint32 *plt_info_offsets;

	/*
	 * Encode info need to resolve PLT entries.
	 */
	buf_size = acfg->plt_offset * 128;
	p = buf = g_malloc (buf_size);

	plt_info_offsets = g_new0 (guint32, acfg->plt_offset);

	for (i = 1; i < acfg->plt_offset; ++i) {
		MonoJumpInfo *patch_info = g_hash_table_lookup (acfg->plt_offset_to_patch, GUINT_TO_POINTER (i));

		plt_info_offsets [i] = p - buf;
		encode_value (patch_info->type, p, &p);
		encode_patch (acfg, patch_info, p, &p, FALSE);
	}

	emit_line (acfg);
	symbol = g_strdup_printf ("plt");

	/* This section will be made read-write by the AOT loader */
	emit_section_change (acfg, ".text", 0);
	emit_global (acfg, symbol, TRUE);
	emit_alignment (acfg, PAGESIZE);
	emit_label (acfg, symbol);

	/* 
	 * The first plt entry is used to transfer code to the AOT loader. 
	 */
	emit_label (acfg, ".Lp_0");
#if defined(__i386__)
	/* It is filled up during loading by the AOT loader. */
	emit_zero_bytes (acfg, 16);
#elif defined(__x86_64__)
	/* This should be exactly 16 bytes long */
	/* jmpq *<offset>(%rip) */
	emit_byte (acfg, '\xff');
	emit_byte (acfg, '\x25');
	emit_symbol_diff (acfg, "plt_jump_table", ".", -4);
	emit_zero_bytes (acfg, 10);
#else
	g_assert_not_reached ();
#endif

	for (i = 1; i < acfg->plt_offset; ++i) {
		char *label;

		label = g_strdup_printf (".Lp_%d", i);
		emit_label (acfg, label);
		g_free (label);
#if defined(__i386__)
		/* Need to make sure this is 5 bytes long */
		emit_byte (acfg, '\xe9');
		label = g_strdup_printf (".Lpd_%d", i);
		emit_symbol_diff (acfg, label, ".", -4);
		g_free (label);
#elif defined(__x86_64__)
		/*
		 * We can't emit jumps because they are 32 bits only so they can't be patched.
		 * So we emit a jump table instead whose entries are patched by the AOT loader to
		 * point to .Lpd entries. ELF stores these in the GOT too, but we don't, since
		 * methods with GOT entries can't be called directly.
		 * We also emit the default PLT code here since the PLT code will not be patched.
		 * An x86_64 plt entry is 16 bytes long, init_plt () depends on this.
		 */
		/* jmpq *<offset>(%rip) */
		emit_byte (acfg, '\xff');
		emit_byte (acfg, '\x25');
		emit_symbol_diff (acfg, "plt_jump_table", ".", (i * sizeof (gpointer)) -4);
		/* mov <plt info offset>, %eax */
		emit_byte (acfg, '\xb8');
		emit_int32 (acfg, plt_info_offsets [i]);
		/* jmp .Lp_0 */
		emit_byte (acfg, '\xe9');
		emit_symbol_diff (acfg, ".Lp_0", ".", -4);
#else
		g_assert_not_reached ();
#endif
	}

	symbol = g_strdup_printf ("plt_end");
	emit_global (acfg, symbol, TRUE);
	emit_label (acfg, symbol);

	/* 
	 * Emit the default targets for the PLT entries separately since these will not
	 * be modified at runtime.
	 */
	for (i = 1; i < acfg->plt_offset; ++i) {
		char *label;

		label = g_strdup_printf (".Lpd_%d", i);
		emit_label (acfg, label);
		g_free (label);

		/* Put the offset into the register expected by mono_aot_plt_trampoline */
#if defined(__i386__)
		/* movl $const, %eax */
		emit_byte (acfg, '\xb8');
		emit_int32 (acfg, plt_info_offsets [i]);
		/* jmp .Lp_0 */
		emit_byte (acfg, '\xe9');
		emit_symbol_diff (acfg, ".Lp_0", ".", -4);
#elif defined(__x86_64__)
		/* Emitted along with the PLT entries since they will not be patched */
#else
		g_assert_not_reached ();
#endif
	}

	/* Emit PLT info */
	symbol = g_strdup_printf ("plt_info");
	emit_global (acfg, symbol, FALSE);
	emit_label (acfg, symbol);

	g_assert (p - buf < buf_size);
	emit_bytes (acfg, buf, p - buf);
	g_free (buf);

	symbol = g_strdup_printf ("plt_jump_table_addr");
	emit_section_change (acfg, ".data", 0);
	emit_global (acfg, symbol, FALSE);
	emit_alignment (acfg, 8);
	emit_label (acfg, symbol);
	emit_pointer (acfg, "plt_jump_table");

	symbol = g_strdup_printf ("plt_jump_table_size");
	emit_section_change (acfg, ".data", 0);
	emit_global (acfg, symbol, FALSE);
	emit_alignment (acfg, 8);
	emit_label (acfg, symbol);
	emit_symbol_diff (acfg, "plt_jump_table_end", "plt_jump_table", 0);

	/* Don't make this a global so accesses don't need relocations */
	symbol = g_strdup_printf ("plt_jump_table");
	emit_section_change (acfg, ".bss", 0);
	emit_label (acfg, symbol);

#ifdef __x86_64__
	emit_zero_bytes (acfg, (int)(acfg->plt_offset * sizeof (gpointer)));
#endif	

	symbol = g_strdup_printf ("plt_jump_table_end");
	emit_label (acfg, symbol);
}

static gboolean
str_begins_with (const char *str1, const char *str2)
{
	int len = strlen (str2);
	return strncmp (str1, str2, len) == 0;
}

static void
mono_aot_parse_options (const char *aot_options, MonoAotOptions *opts)
{
	gchar **args, **ptr;

	memset (opts, 0, sizeof (*opts));

	args = g_strsplit (aot_options ? aot_options : "", ",", -1);
	for (ptr = args; ptr && *ptr; ptr ++) {
		const char *arg = *ptr;

		if (str_begins_with (arg, "outfile=")) {
			opts->outfile = g_strdup (arg + strlen ("outfile="));
		} else if (str_begins_with (arg, "save-temps")) {
			opts->save_temps = TRUE;
		} else if (str_begins_with (arg, "keep-temps")) {
			opts->save_temps = TRUE;
		} else if (str_begins_with (arg, "write-symbols")) {
			opts->write_symbols = TRUE;
		} else {
			fprintf (stderr, "AOT : Unknown argument '%s'.\n", arg);
			exit (1);
		}
	}
}

static void
compile_method (MonoAotCompile *acfg, int index)
{
	MonoCompile *cfg;
	MonoMethod *method;
	MonoJumpInfo *patch_info;
	gboolean skip;
	guint32 token = MONO_TOKEN_METHOD_DEF | (index + 1);
	guint32 method_idx;

	method = mono_get_method (acfg->image, token, NULL);

	method_idx = mono_metadata_token_index (method->token);	
		
	/* fixme: maybe we can also precompile wrapper methods */
	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
		(method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
		(method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
		(method->flags & METHOD_ATTRIBUTE_ABSTRACT)) {
		//printf ("Skip (impossible): %s\n", mono_method_full_name (method, TRUE));
		return;
	}

	acfg->stats.mcount++;

	/* fixme: we need to patch the IP for the LMF in that case */
	if (method->save_lmf) {
		//printf ("Skip (needs lmf):  %s\n", mono_method_full_name (method, TRUE));
		acfg->stats.lmfcount++;
		return;
	}

	/*
	 * Since these methods are the only ones which are compiled with
	 * AOT support, and they are not used by runtime startup/shutdown code,
	 * the runtime will not see AOT methods during AOT compilation,so it
	 * does not need to support them by creating a fake GOT etc.
	 */
	cfg = mini_method_compile (method, acfg->opts, mono_get_root_domain (), FALSE, TRUE, 0);
	if (cfg->exception_type != MONO_EXCEPTION_NONE) {
		/* Let the exception happen at runtime */
		return;
	}

	if (cfg->disable_aot) {
		//printf ("Skip (other): %s\n", mono_method_full_name (method, TRUE));
		acfg->stats.ocount++;
		mono_destroy_compile (cfg);
		return;
	}

	skip = FALSE;
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		if (patch_info->type == MONO_PATCH_INFO_ABS) {
			/* unable to handle this */
			//printf ("Skip (abs addr):   %s %d\n", mono_method_full_name (method, TRUE), patch_info->type);
			skip = TRUE;	
			break;
		}
	}

	if (skip) {
		acfg->stats.abscount++;
		mono_destroy_compile (cfg);
		return;
	}

	/* some wrappers are very common */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		if (patch_info->type == MONO_PATCH_INFO_METHODCONST) {
			switch (patch_info->data.method->wrapper_type) {
			case MONO_WRAPPER_PROXY_ISINST:
				patch_info->type = MONO_PATCH_INFO_WRAPPER;
			}
		}

		if (patch_info->type == MONO_PATCH_INFO_METHOD) {
			switch (patch_info->data.method->wrapper_type) {
			case MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK:
			case MONO_WRAPPER_STFLD:
			case MONO_WRAPPER_LDFLD:
			case MONO_WRAPPER_LDFLDA:
			case MONO_WRAPPER_LDFLD_REMOTE:
			case MONO_WRAPPER_STFLD_REMOTE:
			case MONO_WRAPPER_STELEMREF:
			case MONO_WRAPPER_ISINST:
			case MONO_WRAPPER_PROXY_ISINST:
				patch_info->type = MONO_PATCH_INFO_WRAPPER;
				break;
			}
		}
	}

	skip = FALSE;
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_METHOD:
		case MONO_PATCH_INFO_METHODCONST:
			if (patch_info->data.method->wrapper_type) {
				/* unable to handle this */
				//printf ("Skip (wrapper call):   %s %d -> %s\n", mono_method_full_name (method, TRUE), patch_info->type, mono_method_full_name (patch_info->data.method, TRUE));
				skip = TRUE;
				break;
			}
			if (!patch_info->data.method->token)
				/*
				 * The method is part of a constructed type like Int[,].Set (). It doesn't
				 * have a token, and we can't make one, since the parent type is part of
				 * assembly which contains the element type, and not the assembly which
				 * referenced this type.
				 */
				skip = TRUE;
			break;
		case MONO_PATCH_INFO_VTABLE:
		case MONO_PATCH_INFO_CLASS_INIT:
		case MONO_PATCH_INFO_CLASS:
		case MONO_PATCH_INFO_IID:
		case MONO_PATCH_INFO_ADJUSTED_IID:
			if (!patch_info->data.klass->type_token)
				if (!patch_info->data.klass->element_class->type_token)
					skip = TRUE;
			break;
		default:
			break;
		}
	}

	if (skip) {
		acfg->stats.wrappercount++;
		mono_destroy_compile (cfg);
		return;
	}

	/* Determine whenever the method has GOT slots */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_LABEL:
		case MONO_PATCH_INFO_BB:
		case MONO_PATCH_INFO_GOT_OFFSET:
		case MONO_PATCH_INFO_NONE:
		case MONO_PATCH_INFO_METHOD:
		case MONO_PATCH_INFO_INTERNAL_METHOD:
		case MONO_PATCH_INFO_WRAPPER:
			break;
		case MONO_PATCH_INFO_IMAGE:
			if (patch_info->data.image == acfg->image)
				/* Stored in GOT slot 0 */
				break;
			/* Fall through */
		default:
			acfg->has_got_slots [method_idx] = TRUE;
			break;
		}
	}

	if (!acfg->has_got_slots [method_idx])
		acfg->stats.methods_without_got_slots ++;

	/* Make a copy of the patch info which is in the mempool */
	{
		MonoJumpInfo *patches = NULL, *patches_end = NULL;

		for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
			MonoJumpInfo *new_patch_info = mono_patch_info_dup_mp (acfg->mempool, patch_info);

			if (!patches)
				patches = new_patch_info;
			else
				patches_end->next = new_patch_info;
			patches_end = new_patch_info;
		}
		cfg->patch_info = patches;
	}

	/* Free some fields used by cfg to conserve memory */
	mono_mempool_destroy (cfg->mempool);
	cfg->mempool = NULL;
	g_free (cfg->varinfo);
	cfg->varinfo = NULL;
	g_free (cfg->vars);
	cfg->vars = NULL;
	if (cfg->rs) {
		mono_regstate_free (cfg->rs);
		cfg->rs = NULL;
	}

	//printf ("Compile:           %s\n", mono_method_full_name (method, TRUE));

	acfg->cfgs [index] = cfg;

	g_hash_table_insert (acfg->method_to_cfg, cfg->method, cfg);

	acfg->stats.ccount++;
}

static void
load_profile_files (MonoAotCompile *acfg)
{
	FILE *infile;
	char *tmp;
	int file_index, res, method_index, i;
	char ver [256];
	guint32 token;

	file_index = 0;
	while (TRUE) {
		tmp = g_strdup_printf ("%s/.mono/aot-profile-data/%s-%s-%d", g_get_home_dir (), acfg->image->assembly_name, acfg->image->guid, file_index);

		if (!g_file_test (tmp, G_FILE_TEST_IS_REGULAR))
			break;

		infile = fopen (tmp, "r");
		g_assert (infile);

		printf ("Using profile data file '%s'\n", tmp);

		file_index ++;

		res = fscanf (infile, "%32s\n", ver);
		if ((res != 1) || strcmp (ver, "#VER:1") != 0) {
			printf ("Profile file has wrong version or invalid.\n");
			continue;
		}

		while (TRUE) {
			res = fscanf (infile, "%d\n", &token);
			if (res < 1)
				break;

			method_index = mono_metadata_token_index (token) - 1;

			if (!g_list_find (acfg->method_order, GUINT_TO_POINTER (method_index)))
				acfg->method_order = g_list_append (acfg->method_order, GUINT_TO_POINTER (method_index));
		}
	}

	/* Add missing methods */
	for (i = 0; i < acfg->image->tables [MONO_TABLE_METHOD].rows; ++i) {
		if (!g_list_find (acfg->method_order, GUINT_TO_POINTER (i)))
			acfg->method_order = g_list_append (acfg->method_order, GUINT_TO_POINTER (i));
	}		
}

/**
 * alloc_got_slots:
 *
 *  Collect all patches which have shared GOT entries and alloc entries for them. The
 * rest will get entries allocated during emit_code ().
 */
static void
alloc_got_slots (MonoAotCompile *acfg)
{
	int i;
	GList *l;
	MonoJumpInfo *ji;

	/* Slot 0 is reserved for the address of the current assembly */
	ji = mono_mempool_alloc0 (acfg->mempool, sizeof (MonoAotCompile));
	ji->type = MONO_PATCH_INFO_IMAGE;
	ji->data.image = acfg->image;

	get_shared_got_offset (acfg, ji);

	for (l = acfg->method_order; l != NULL; l = l->next) {
		i = GPOINTER_TO_UINT (l->data);

		if (acfg->cfgs [i]) {
			MonoCompile *cfg = acfg->cfgs [i];

			for (ji = cfg->patch_info; ji; ji = ji->next) {
				switch (ji->type) {
				case MONO_PATCH_INFO_VTABLE:
				case MONO_PATCH_INFO_CLASS:
				case MONO_PATCH_INFO_IID:
				case MONO_PATCH_INFO_ADJUSTED_IID:
				case MONO_PATCH_INFO_FIELD:
				case MONO_PATCH_INFO_SFLDA:
				case MONO_PATCH_INFO_DECLSEC:
				case MONO_PATCH_INFO_LDTOKEN:
				case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
					get_shared_got_offset (acfg, ji);
					break;
				default:
					break;
				}
			}
		}
	}
}

static void
emit_code (MonoAotCompile *acfg)
{
	int i;
	char *symbol;
	GList *l;

	symbol = g_strdup_printf ("methods");
	emit_section_change (acfg, ".text", 0);
	emit_global (acfg, symbol, TRUE);
	emit_alignment (acfg, 8);
	emit_label (acfg, symbol);

	for (l = acfg->method_order; l != NULL; l = l->next) {
		i = GPOINTER_TO_UINT (l->data);

		if (acfg->cfgs [i])
			emit_method_code (acfg, acfg->cfgs [i]);
	}

	symbol = g_strdup_printf ("methods_end");
	emit_section_change (acfg, ".text", 0);
	emit_global (acfg, symbol, FALSE);
	emit_alignment (acfg, 8);
	emit_label (acfg, symbol);

	symbol = g_strdup_printf ("method_offsets");
	emit_section_change (acfg, ".text", 1);
	emit_global (acfg, symbol, FALSE);
	emit_alignment (acfg, 8);
	emit_label (acfg, symbol);

	for (i = 0; i < acfg->image->tables [MONO_TABLE_METHOD].rows; ++i) {
		if (acfg->cfgs [i]) {
			symbol = g_strdup_printf (".Lm_%x", i + 1);
			emit_symbol_diff (acfg, symbol, "methods", 0);
		} else {
			emit_int32 (acfg, 0xffffffff);
		}
	}
	emit_line (acfg);
}

static void
emit_info (MonoAotCompile *acfg)
{
	int i;
	char *symbol;
	GList *l;

	/* Emit method info */
	symbol = g_strdup_printf ("method_info");
	emit_section_change (acfg, ".text", 1);
	emit_global (acfg, symbol, FALSE);
	emit_alignment (acfg, 8);
	emit_label (acfg, symbol);

	/* To reduce size of generated assembly code */
	symbol = g_strdup_printf ("mi");
	emit_label (acfg, symbol);

	for (l = acfg->method_order; l != NULL; l = l->next) {
		i = GPOINTER_TO_UINT (l->data);

		if (acfg->cfgs [i])
			emit_method_info (acfg, acfg->cfgs [i]);
	}

	symbol = g_strdup_printf ("method_info_offsets");
	emit_section_change (acfg, ".text", 1);
	emit_global (acfg, symbol, FALSE);
	emit_alignment (acfg, 8);
	emit_label (acfg, symbol);

	for (i = 0; i < acfg->image->tables [MONO_TABLE_METHOD].rows; ++i) {
		if (acfg->cfgs [i]) {
			symbol = g_strdup_printf (".Lm_%x_p", i + 1);
			emit_symbol_diff (acfg, symbol, "mi", 0);
		} else {
			emit_int32 (acfg, 0);
		}
	}
	emit_line (acfg);
}

static void
emit_method_order (MonoAotCompile *acfg)
{
	int i, index, len;
	char *symbol;
	GList *l;

	symbol = g_strdup_printf ("method_order");
	emit_section_change (acfg, ".text", 1);
	emit_global (acfg, symbol, FALSE);
	emit_alignment (acfg, 8);
	emit_label (acfg, symbol);

	/* First emit an index table */
	index = 0;
	len = 0;
	for (l = acfg->method_order; l != NULL; l = l->next) {
		i = GPOINTER_TO_UINT (l->data);

		if (acfg->cfgs [i]) {
			if ((index % 1024) == 0) {
				emit_int32 (acfg, i);
			}

			index ++;
		}

		len ++;
	}
	emit_int32 (acfg, 0xffffff);

	/* Then emit the whole method order */
	for (l = acfg->method_order; l != NULL; l = l->next) {
		i = GPOINTER_TO_UINT (l->data);

		if (acfg->cfgs [i]) {
			emit_int32 (acfg, i);
		}
	}	
	emit_line (acfg);

	symbol = g_strdup_printf ("method_order_end");
	emit_section_change (acfg, ".text", 1);
	emit_global (acfg, symbol, FALSE);
	emit_label (acfg, symbol);
}

static void
emit_exception_info (MonoAotCompile *acfg)
{
	int i;
	char *symbol;

	symbol = g_strdup_printf ("ex_info");
	emit_section_change (acfg, ".text", 1);
	emit_global (acfg, symbol, FALSE);
	emit_alignment (acfg, 8);
	emit_label (acfg, symbol);

	/* To reduce size of generate assembly */
	symbol = g_strdup_printf ("ex");
	emit_label (acfg, symbol);

	for (i = 0; i < acfg->image->tables [MONO_TABLE_METHOD].rows; ++i) {
		if (acfg->cfgs [i])
			emit_exception_debug_info (acfg, acfg->cfgs [i]);
	}

	symbol = g_strdup_printf ("ex_info_offsets");
	emit_section_change (acfg, ".text", 1);
	emit_global (acfg, symbol, FALSE);
	emit_alignment (acfg, 8);
	emit_label (acfg, symbol);

	for (i = 0; i < acfg->image->tables [MONO_TABLE_METHOD].rows; ++i) {
		if (acfg->cfgs [i]) {
			symbol = g_strdup_printf (".Le_%x_p", i + 1);
			emit_symbol_diff (acfg, symbol, "ex", 0);
		} else {
			emit_int32 (acfg, 0);
		}
	}
	emit_line (acfg);
}

static void
emit_class_info (MonoAotCompile *acfg)
{
	int i;
	char *symbol;

	symbol = g_strdup_printf ("class_info");
	emit_section_change (acfg, ".text", 1);
	emit_global (acfg, symbol, FALSE);
	emit_alignment (acfg, 8);
	emit_label (acfg, symbol);

	for (i = 0; i < acfg->image->tables [MONO_TABLE_TYPEDEF].rows; ++i)
		emit_klass_info (acfg, MONO_TOKEN_TYPE_DEF | (i + 1));

	symbol = g_strdup_printf ("class_info_offsets");
	emit_section_change (acfg, ".text", 1);
	emit_global (acfg, symbol, FALSE);
	emit_alignment (acfg, 8);
	emit_label (acfg, symbol);

	for (i = 0; i < acfg->image->tables [MONO_TABLE_TYPEDEF].rows; ++i) {
		symbol = g_strdup_printf (".LK_I_%x", i);
		emit_symbol_diff (acfg, symbol, "class_info", 0);
	}
	emit_line (acfg);
}

static void
emit_image_table (MonoAotCompile *acfg)
{
	int i;
	char *symbol;

	/*
	 * The image table is small but referenced in a lot of places.
	 * So we emit it at once, and reference its elements by an index.
	 */

	symbol = g_strdup_printf ("mono_image_table");
	emit_section_change (acfg, ".text", 1);
	emit_global (acfg, symbol, FALSE);
	emit_alignment (acfg, 8);
	emit_label (acfg, symbol);
	emit_int32 (acfg, acfg->image_table->len);
	for (i = 0; i < acfg->image_table->len; i++) {
		MonoImage *image = (MonoImage*)g_ptr_array_index (acfg->image_table, i);
		MonoAssemblyName *aname = &image->assembly->aname;

		/* FIXME: Support multi-module assemblies */
		g_assert (image->assembly->image == image);

		emit_string (acfg, image->assembly_name);
		emit_string (acfg, image->guid);
		emit_string (acfg, aname->culture ? aname->culture : "");
		emit_string (acfg, (const char*)aname->public_key_token);

		emit_alignment (acfg, 8);
		emit_int32 (acfg, aname->flags);
		emit_int32 (acfg, aname->major);
		emit_int32 (acfg, aname->minor);
		emit_int32 (acfg, aname->build);
		emit_int32 (acfg, aname->revision);
	}
}

static void
emit_got_info (MonoAotCompile *acfg)
{
	char *symbol;
	int i, buf_size;
	guint8 *p, *buf;
	guint32 *got_info_offsets;

	/**
	 * FIXME: 
	 * - optimize offsets table.
	 * - reduce number of exported symbols.
	 * - emit info for a klass only once.
	 * - determine when a method uses a GOT slot which is guaranteed to be already 
	 *   initialized.
	 * - clean up and document the code.
	 * - use String.Empty in class libs.
	 */

	/* Encode info required to decode shared GOT entries */
	buf_size = acfg->shared_patches->len * 64;
	p = buf = mono_mempool_alloc (acfg->mempool, buf_size);
	got_info_offsets = mono_mempool_alloc (acfg->mempool, acfg->shared_patches->len * sizeof (guint32));
	for (i = 0; i < acfg->shared_patches->len; ++i) {
		MonoJumpInfo *ji = g_ptr_array_index (acfg->shared_patches, i);

		/* No need to encode the patch type */
		got_info_offsets [i] = p - buf;
		encode_patch (acfg, ji, p, &p, FALSE);
	}

	g_assert (p - buf <= buf_size);

	acfg->stats.got_info_size = p - buf;

	/* Emit got_info table */
	symbol = g_strdup_printf ("got_info");
	emit_section_change (acfg, ".text", 1);
	emit_global (acfg, symbol, FALSE);
	emit_alignment (acfg, 8);
	emit_label (acfg, symbol);

	emit_bytes (acfg, buf, p - buf);

	/* Emit got_info_offsets table */
	symbol = g_strdup_printf ("got_info_offsets");
	emit_section_change (acfg, ".text", 1);
	emit_global (acfg, symbol, FALSE);
	emit_alignment (acfg, 8);
	emit_label (acfg, symbol);

	for (i = 0; i < acfg->shared_patches->len; ++i)
		emit_int32 (acfg, got_info_offsets [i]);

	acfg->stats.got_info_offsets_size = acfg->shared_patches->len * 4;
}

static void
emit_got (MonoAotCompile *acfg)
{
	char *symbol;

	/* Don't make GOT global so accesses to it don't need relocations */
	symbol = g_strdup_printf ("got");
	emit_section_change (acfg, ".bss", 1);
	emit_alignment (acfg, 8);
	emit_label (acfg, symbol);
	if (acfg->got_offset > 0)
		emit_zero_bytes (acfg, (int)(acfg->got_offset * sizeof (gpointer)));

	symbol = g_strdup_printf ("got_addr");
	emit_section_change (acfg, ".data", 1);
	emit_global (acfg, symbol, FALSE);
	emit_alignment (acfg, 8);
	emit_label (acfg, symbol);
	emit_pointer (acfg, "got");

	symbol = g_strdup_printf ("got_size");
	emit_section_change (acfg, ".data", 1);
	emit_global (acfg, symbol, FALSE);
	emit_alignment (acfg, 8);
	emit_label (acfg, symbol);
	emit_int32 (acfg, (int)(acfg->got_offset * sizeof (gpointer)));
}

static void
emit_globals (MonoAotCompile *acfg)
{
	char *opts_str;

	emit_string_symbol (acfg, "mono_assembly_guid" , acfg->image->guid);

	emit_string_symbol (acfg, "mono_aot_version", MONO_AOT_FILE_VERSION);

	opts_str = g_strdup_printf ("%d", acfg->opts);
	emit_string_symbol (acfg, "mono_aot_opt_flags", opts_str);
	g_free (opts_str);
}

int
mono_compile_assembly (MonoAssembly *ass, guint32 opts, const char *aot_options)
{
	MonoImage *image = ass->image;
	char *symbol;
	int i;
	MonoAotCompile *acfg;
	MonoCompile **cfgs;

	printf ("Mono Ahead of Time compiler - compiling assembly %s\n", image->name);

	acfg = g_new0 (MonoAotCompile, 1);
	acfg->plt_offset_to_patch = g_hash_table_new (NULL, NULL);
	acfg->patch_to_plt_offset = g_hash_table_new (NULL, NULL);
	acfg->patch_to_shared_got_offset = g_hash_table_new (mono_patch_info_hash, mono_patch_info_equal);
	acfg->shared_patches = g_ptr_array_new ();
	acfg->method_to_cfg = g_hash_table_new (NULL, NULL);
	acfg->image_hash = g_hash_table_new (NULL, NULL);
	acfg->image_table = g_ptr_array_new ();
	acfg->image = image;
	acfg->opts = opts;
	acfg->mempool = mono_mempool_new ();

	mono_aot_parse_options (aot_options, &acfg->aot_opts);

	load_profile_files (acfg);

	emit_start (acfg);

	cfgs = g_new0 (MonoCompile*, image->tables [MONO_TABLE_METHOD].rows + 32);
	acfg->cfgs = cfgs;
	acfg->nmethods = image->tables [MONO_TABLE_METHOD].rows;
	acfg->method_got_offsets = g_new0 (guint32, image->tables [MONO_TABLE_METHOD].rows + 32);
	acfg->has_got_slots = g_new0 (gboolean, image->tables [MONO_TABLE_METHOD].rows + 32);

	/* PLT offset 0 is reserved for the PLT trampoline */
	acfg->plt_offset = 1;

	/* Compile methods */
	for (i = 0; i < image->tables [MONO_TABLE_METHOD].rows; ++i)
		compile_method (acfg, i);

	alloc_got_slots (acfg);

	emit_code (acfg);

	emit_info (acfg);

	emit_method_order (acfg);

	emit_got_info (acfg);

	emit_exception_info (acfg);

	emit_class_info (acfg);

	emit_plt (acfg);

	emit_image_table (acfg);

	emit_got (acfg);

	emit_globals (acfg);

	symbol = g_strdup_printf ("mem_end");
	emit_section_change (acfg, ".text", 1);
	emit_global (acfg, symbol, FALSE);
	emit_alignment (acfg, 8);
	emit_label (acfg, symbol);

	printf ("Code: %d Info: %d Ex Info: %d Class Info: %d PLT: %d GOT Info: %d GOT Info Offsets: %d GOT: %d\n", acfg->stats.code_size, acfg->stats.info_size, acfg->stats.ex_info_size, acfg->stats.class_info_size, acfg->plt_offset, acfg->stats.got_info_size, acfg->stats.got_info_offsets_size, (int)(acfg->got_offset * sizeof (gpointer)));

	emit_writeout (acfg);

	printf ("Compiled %d out of %d methods (%d%%)\n", acfg->stats.ccount, acfg->stats.mcount, acfg->stats.mcount ? (acfg->stats.ccount * 100) / acfg->stats.mcount : 100);
	printf ("%d methods contain absolute addresses (%d%%)\n", acfg->stats.abscount, acfg->stats.mcount ? (acfg->stats.abscount * 100) / acfg->stats.mcount : 100);
	printf ("%d methods contain wrapper references (%d%%)\n", acfg->stats.wrappercount, acfg->stats.mcount ? (acfg->stats.wrappercount * 100) / acfg->stats.mcount : 100);
	printf ("%d methods contain lmf pointers (%d%%)\n", acfg->stats.lmfcount, acfg->stats.mcount ? (acfg->stats.lmfcount * 100) / acfg->stats.mcount : 100);
	printf ("%d methods have other problems (%d%%)\n", acfg->stats.ocount, acfg->stats.mcount ? (acfg->stats.ocount * 100) / acfg->stats.mcount : 100);
	printf ("Methods without GOT slots: %d (%d%%)\n", acfg->stats.methods_without_got_slots, acfg->stats.mcount ? (acfg->stats.methods_without_got_slots * 100) / acfg->stats.mcount : 100);
	printf ("Direct calls: %d (%d%%)\n", acfg->stats.direct_calls, acfg->stats.all_calls ? (acfg->stats.direct_calls * 100) / acfg->stats.all_calls : 100);

	printf ("GOT slot distribution:\n");
	for (i = 0; i < MONO_PATCH_INFO_NONE; ++i)
		if (acfg->stats.got_slot_types [i])
			printf ("\t%s: %d\n", patch_types [i], acfg->stats.got_slot_types [i]);

	return 0;
}

#else

/* AOT disabled */

int
mono_compile_assembly (MonoAssembly *ass, guint32 opts, const char *aot_options)
{
	return 0;
}

#endif
