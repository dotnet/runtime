/**
 * \file
 * Creation of object files or assembly files using the same interface.
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Zoltan Varga (vargaz@gmail.com)
 *   Paolo Molaro (lupus@ximian.com)
 *   Johan Lorensson (lateralusx.github@gmail.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include "config.h"
#include <sys/types.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#ifdef HAVE_STDINT_H
#include <stdint.h>
#endif
#include <fcntl.h>
#include <ctype.h>
#include <string.h>
#include <errno.h>
#include <sys/stat.h>

#include "image-writer.h"

#include "mini.h"

/*
 * The used assembler dialect
 * TARGET_ASM_APPLE == apple assembler on OSX
 * TARGET_ASM_GAS == GNU assembler
 */
#if !defined(TARGET_ASM_APPLE) && !defined(TARGET_ASM_GAS)
#if defined(TARGET_MACH)
#define TARGET_ASM_APPLE
#else
#define TARGET_ASM_GAS
#endif
#endif

/*
 * Defines for the directives used by different assemblers
 */
#if defined(TARGET_POWERPC) || defined(TARGET_MACH)
#define AS_STRING_DIRECTIVE ".asciz"
#else
#define AS_STRING_DIRECTIVE ".string"
#endif

#define AS_INT32_DIRECTIVE ".long"
#define AS_INT64_DIRECTIVE ".quad"

#if (defined(TARGET_AMD64) || defined(TARGET_POWERPC64)) && !defined(MONO_ARCH_ILP32)
#define AS_POINTER_DIRECTIVE ".quad"
#elif defined(TARGET_ARM64)

#ifdef MONO_ARCH_ILP32
#define AS_POINTER_DIRECTIVE AS_INT32_DIRECTIVE
#else
#ifdef TARGET_ASM_APPLE
#define AS_POINTER_DIRECTIVE ".quad"
#else
#define AS_POINTER_DIRECTIVE ".xword"
#endif
#endif

#else
#define AS_POINTER_DIRECTIVE ".long"
#endif

#if defined(TARGET_ASM_APPLE)
#define AS_INT16_DIRECTIVE ".short"
#elif defined(TARGET_ASM_GAS) && defined(TARGET_WIN32)
#define AS_INT16_DIRECTIVE ".word"
#elif defined(TARGET_ASM_GAS)
#define AS_INT16_DIRECTIVE ".short"
#else
#define AS_INT16_DIRECTIVE ".word"
#endif

#if defined(TARGET_ASM_APPLE)
#define AS_SKIP_DIRECTIVE ".space"
#else
#define AS_SKIP_DIRECTIVE ".skip"
#endif

#if defined(TARGET_ASM_APPLE)
#define AS_GLOBAL_PREFIX "_"
#else
#define AS_GLOBAL_PREFIX ""
#endif

#ifdef TARGET_ASM_APPLE
#define AS_TEMP_LABEL_PREFIX "L"
#else
#define AS_TEMP_LABEL_PREFIX ".L"
#endif

#define ROUND_DOWN(VALUE,SIZE)	((VALUE) & ~((SIZE) - 1))

/* emit mode */
enum {
	EMIT_NONE,
	EMIT_BYTE,
	EMIT_WORD,
	EMIT_LONG
};

struct _MonoImageWriter {
	MonoMemPool *mempool;
	char *outfile;
	const char *current_section;
	int current_subsection;
	const char *section_stack [16];
	int subsection_stack [16];
	int stack_pos;
	FILE *fp;
	/* Asm writer */
	char *tmpfname;
	int mode; /* emit mode */
	int col_count; /* bytes emitted per .byte line */
	int label_gen;
};

static G_GNUC_UNUSED int
ilog2(int value)
{
	int count = -1;
	while (value & ~0xf) count += 4, value >>= 4;
	while (value) count++, value >>= 1;
	return count;
}

/* ASM WRITER */

static void
asm_writer_emit_start (MonoImageWriter *acfg)
{
#if defined(TARGET_ASM_APPLE)
	fprintf (acfg->fp, ".subsections_via_symbols\n");
#endif
}

static int
asm_writer_emit_writeout (MonoImageWriter *acfg)
{
	fclose (acfg->fp);

	return 0;
}

static void
asm_writer_emit_unset_mode (MonoImageWriter *acfg)
{
	if (acfg->mode == EMIT_NONE)
		return;
	fprintf (acfg->fp, "\n");
	acfg->mode = EMIT_NONE;
}

static void
asm_writer_emit_section_change (MonoImageWriter *acfg, const char *section_name, int subsection_index)
{
	asm_writer_emit_unset_mode (acfg);
#if defined(TARGET_ASM_APPLE)
	if (strcmp(section_name, ".bss") == 0)
		fprintf (acfg->fp, "%s\n", ".data");
	else if (strstr (section_name, ".debug") == section_name) {
		//g_assert (subsection_index == 0);
		fprintf (acfg->fp, ".section __DWARF, __%s,regular,debug\n", section_name + 1);
	} else
		fprintf (acfg->fp, "%s\n", section_name);
#elif defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_POWERPC)
	/* ARM gas doesn't seem to like subsections of .bss */
	if (!strcmp (section_name, ".text") || !strcmp (section_name, ".data")) {
		fprintf (acfg->fp, "%s %d\n", section_name, subsection_index);
	} else {
		fprintf (acfg->fp, ".section \"%s\"\n", section_name);
		fprintf (acfg->fp, ".subsection %d\n", subsection_index);
	}
#elif defined(HOST_WIN32)
	fprintf (acfg->fp, ".section %s\n", section_name);
#else
	if (!strcmp (section_name, ".text") || !strcmp (section_name, ".data") || !strcmp (section_name, ".bss")) {
		fprintf (acfg->fp, "%s %d\n", section_name, subsection_index);
	} else {
		fprintf (acfg->fp, ".section \"%s\"\n", section_name);
		fprintf (acfg->fp, ".subsection %d\n", subsection_index);
	}
#endif
}

static
const char *get_label (const char *s)
{
#ifdef TARGET_ASM_APPLE
	if (s [0] == '.' && s [1] == 'L')
		/* apple uses "L" instead of ".L" to mark temporary labels */
		s ++;
#endif
	return s;
}

#ifdef TARGET_WIN32
#define GLOBAL_SYMBOL_DEF_SCL 2
#define LOCAL_SYMBOL_DEF_SCL 3

static gboolean
asm_writer_in_data_section (MonoImageWriter *acfg)
{
	gboolean	in_data_section = FALSE;
	const char	*data_sections [] = {".data", ".bss", ".rdata"};

	for (guchar i = 0; i < G_N_ELEMENTS (data_sections); ++i) {
		if (strcmp (acfg->current_section, data_sections [i]) == 0) {
			in_data_section = TRUE;
			break;
		}
	}

	return in_data_section;
}

static void
asm_writer_emit_symbol_type (MonoImageWriter *acfg, const char *name, gboolean func, gboolean global)
{
	asm_writer_emit_unset_mode (acfg);

	if (func) {
		fprintf (acfg->fp, "\t.def %s; .scl %d; .type 32; .endef\n", name, (global == TRUE ? GLOBAL_SYMBOL_DEF_SCL : LOCAL_SYMBOL_DEF_SCL));
	} else {
		if (!asm_writer_in_data_section (acfg))
			fprintf (acfg->fp, "\t.data\n");
	}

	return;
}

#else

static void
asm_writer_emit_symbol_type (MonoImageWriter *acfg, const char *name, gboolean func, gboolean global)
{
#if defined(TARGET_ASM_APPLE)
	asm_writer_emit_unset_mode (acfg);
#else
	const char *stype;

	if (func)
		stype = "function";
	else
		stype = "object";

	asm_writer_emit_unset_mode (acfg);
#if defined(TARGET_ARM)
	fprintf (acfg->fp, "\t.type %s,#%s\n", name, stype);
#else
	fprintf (acfg->fp, "\t.type %s,@%s\n", name, stype);
#endif
#endif
}
#endif /* TARGET_WIN32 */

static void
asm_writer_emit_global (MonoImageWriter *acfg, const char *name, gboolean func)
{
	asm_writer_emit_unset_mode (acfg);

	fprintf (acfg->fp, "\t.globl %s\n", name);

	asm_writer_emit_symbol_type (acfg, name, func, TRUE);
}

static void
asm_writer_emit_local_symbol (MonoImageWriter *acfg, const char *name, const char *end_label, gboolean func)
{
	asm_writer_emit_unset_mode (acfg);

#if !defined(TARGET_ASM_APPLE) && !defined(TARGET_WIN32)
	fprintf (acfg->fp, "\t.local %s\n", name);
#endif

	asm_writer_emit_symbol_type (acfg, name, func, FALSE);
}

static void
asm_writer_emit_symbol_size (MonoImageWriter *acfg, const char *name, const char *end_label)
{
	asm_writer_emit_unset_mode (acfg);


#if !defined(TARGET_ASM_APPLE) && !defined(TARGET_WIN32)
	fprintf (acfg->fp, "\t.size %s,%s-%s\n", name, end_label, name);
#endif
}

static void
asm_writer_emit_label (MonoImageWriter *acfg, const char *name)
{
	asm_writer_emit_unset_mode (acfg);
	fprintf (acfg->fp, "%s:\n", get_label (name));
}

static void
asm_writer_emit_string (MonoImageWriter *acfg, const char *value)
{
	asm_writer_emit_unset_mode (acfg);
	fprintf (acfg->fp, "\t%s \"%s\"\n", AS_STRING_DIRECTIVE, value);
}

static void
asm_writer_emit_line (MonoImageWriter *acfg)
{
	asm_writer_emit_unset_mode (acfg);
	fprintf (acfg->fp, "\n");
}

static void
asm_writer_emit_alignment (MonoImageWriter *acfg, int size)
{
	asm_writer_emit_unset_mode (acfg);
#if defined(TARGET_ARM)
	fprintf (acfg->fp, "\t.align %d\n", ilog2 (size));
#elif defined(__ppc__) && defined(TARGET_ASM_APPLE)
	// the mach-o assembler specifies alignments as powers of 2.
	fprintf (acfg->fp, "\t.align %d\t; ilog2\n", ilog2(size));
#elif defined(TARGET_ASM_GAS)
	fprintf (acfg->fp, "\t.balign %d\n", size);
#elif defined(TARGET_ASM_APPLE)
	fprintf (acfg->fp, "\t.align %d\n", ilog2 (size));
#else
	fprintf (acfg->fp, "\t.align %d\n", size);
#endif
}

static void
asm_writer_emit_alignment_fill (MonoImageWriter *acfg, int size, int fill)
{
	asm_writer_emit_unset_mode (acfg);
#if defined(TARGET_ASM_APPLE)
	fprintf (acfg->fp, "\t.align %d, 0x%0x\n", ilog2 (size), fill);
#else
	asm_writer_emit_alignment (acfg, size);
#endif
}

static void
asm_writer_emit_pointer_unaligned (MonoImageWriter *acfg, const char *target)
{
	asm_writer_emit_unset_mode (acfg);
	fprintf (acfg->fp, "\t%s %s\n", AS_POINTER_DIRECTIVE, target ? target : "0");
}

static void
asm_writer_emit_pointer (MonoImageWriter *acfg, const char *target)
{
	asm_writer_emit_unset_mode (acfg);
	asm_writer_emit_alignment (acfg, TARGET_SIZEOF_VOID_P);
	asm_writer_emit_pointer_unaligned (acfg, target);
}

static char *byte_to_str;

static void
asm_writer_emit_bytes (MonoImageWriter *acfg, const guint8* buf, int size)
{
	int i;
	if (acfg->mode != EMIT_BYTE) {
		acfg->mode = EMIT_BYTE;
		acfg->col_count = 0;
	}

	if (byte_to_str == NULL) {
		byte_to_str = g_new0 (char, 256 * 8);
		for (i = 0; i < 256; ++i) {
			sprintf (byte_to_str + (i * 8), ",%d", i);
		}
	}

	for (i = 0; i < size; ++i, ++acfg->col_count) {
		if ((acfg->col_count % 32) == 0)
			fprintf (acfg->fp, "\n\t.byte %d", buf [i]);
		else
			fputs (byte_to_str + (buf [i] * 8), acfg->fp);
	}
}

static void
asm_writer_emit_int16 (MonoImageWriter *acfg, int value)
{
	if (acfg->mode != EMIT_WORD) {
		acfg->mode = EMIT_WORD;
		acfg->col_count = 0;
	}
	if ((acfg->col_count++ % 8) == 0)
		fprintf (acfg->fp, "\n\t%s ", AS_INT16_DIRECTIVE);
	else
		fprintf (acfg->fp, ", ");
	fprintf (acfg->fp, "%d", value);
}

static void
asm_writer_emit_int32 (MonoImageWriter *acfg, int value)
{
	if (acfg->mode != EMIT_LONG) {
		acfg->mode = EMIT_LONG;
		acfg->col_count = 0;
	}
	if ((acfg->col_count++ % 8) == 0)
		fprintf (acfg->fp, "\n\t%s ", AS_INT32_DIRECTIVE);
	else
		fprintf (acfg->fp, ",");
	fprintf (acfg->fp, "%d", value);
}

static void
asm_writer_emit_symbol (MonoImageWriter *acfg, const char *symbol)
{
	if (acfg->mode != EMIT_LONG) {
		acfg->mode = EMIT_LONG;
		acfg->col_count = 0;
	}

	symbol = get_label (symbol);

	if ((acfg->col_count++ % 8) == 0)
		fprintf (acfg->fp, "\n\t%s ", AS_INT32_DIRECTIVE);
	else
		fprintf (acfg->fp, ",");
	fprintf (acfg->fp, "%s", symbol);
}

static void
asm_writer_emit_symbol_diff (MonoImageWriter *acfg, const char *end, const char* start, int offset)
{
#ifdef TARGET_ASM_APPLE
	//char symbol [128];
#endif

	if (acfg->mode != EMIT_LONG) {
		acfg->mode = EMIT_LONG;
		acfg->col_count = 0;
	}

	// FIXME: This doesn't seem to work on the iphone
#if 0
	//#ifdef TARGET_ASM_APPLE
	/* The apple assembler needs a separate symbol to be able to handle complex expressions */
	sprintf (symbol, "LTMP_SYM%d", acfg->label_gen);
	start = get_label (start);
	end = get_label (end);
	acfg->label_gen ++;
	if (offset > 0)
		fprintf (acfg->fp, "\n%s=%s - %s + %d", symbol, end, start, offset);
	else if (offset < 0)
		fprintf (acfg->fp, "\n%s=%s - %s %d", symbol, end, start, offset);
	else
		fprintf (acfg->fp, "\n%s=%s - %s", symbol, end, start);

	fprintf (acfg->fp, "\n\t%s ", AS_INT32_DIRECTIVE);
	fprintf (acfg->fp, "%s", symbol);
#else
	start = get_label (start);
	end = get_label (end);

	if (offset == 0 && strcmp (start, ".") != 0) {
		char symbol [128];
		sprintf (symbol, "%sDIFF_SYM%d", AS_TEMP_LABEL_PREFIX, acfg->label_gen);
		acfg->label_gen ++;
		fprintf (acfg->fp, "\n%s=%s - %s", symbol, end, start);
		fprintf (acfg->fp, "\n\t%s ", AS_INT32_DIRECTIVE);
		fprintf (acfg->fp, "%s", symbol);
		return;
	}

	if ((acfg->col_count++ % 8) == 0)
		fprintf (acfg->fp, "\n\t%s ", AS_INT32_DIRECTIVE);
	else
		fprintf (acfg->fp, ",");
	if (offset > 0)
		fprintf (acfg->fp, "%s - %s + %d", end, start, offset);
	else if (offset < 0)
		fprintf (acfg->fp, "%s - %s %d", end, start, offset);
	else
		fprintf (acfg->fp, "%s - %s", end, start);
#endif
}

static void
asm_writer_emit_zero_bytes (MonoImageWriter *acfg, int num)
{
	asm_writer_emit_unset_mode (acfg);
	fprintf (acfg->fp, "\t%s %d\n", AS_SKIP_DIRECTIVE, num);
}

/* EMIT FUNCTIONS */

void
mono_img_writer_emit_start (MonoImageWriter *acfg)
{
	asm_writer_emit_start (acfg);
}

void
mono_img_writer_emit_section_change (MonoImageWriter *acfg, const char *section_name, int subsection_index)
{
	asm_writer_emit_section_change (acfg, section_name, subsection_index);

	acfg->current_section = section_name;
	acfg->current_subsection = subsection_index;
}

void
mono_img_writer_emit_push_section (MonoImageWriter *acfg, const char *section_name, int subsection)
{
	g_assert (acfg->stack_pos < 16 - 1);
	acfg->section_stack [acfg->stack_pos] = acfg->current_section;
	acfg->subsection_stack [acfg->stack_pos] = acfg->current_subsection;
	acfg->stack_pos ++;

	mono_img_writer_emit_section_change (acfg, section_name, subsection);
}

void
mono_img_writer_emit_pop_section (MonoImageWriter *acfg)
{
	g_assert (acfg->stack_pos > 0);
	acfg->stack_pos --;
	mono_img_writer_emit_section_change (acfg, acfg->section_stack [acfg->stack_pos], acfg->subsection_stack [acfg->stack_pos]);
}

void
mono_img_writer_set_section_addr (MonoImageWriter *acfg, guint64 addr)
{
	NOT_IMPLEMENTED;
}

void
mono_img_writer_emit_global (MonoImageWriter *acfg, const char *name, gboolean func)
{
	asm_writer_emit_global (acfg, name, func);
}

void
mono_img_writer_emit_local_symbol (MonoImageWriter *acfg, const char *name, const char *end_label, gboolean func)
{
	asm_writer_emit_local_symbol (acfg, name, end_label, func);
}

void
mono_img_writer_emit_symbol_size (MonoImageWriter *acfg, const char *name, const char *end_label)
{
	asm_writer_emit_symbol_size (acfg, name, end_label);
}

void
mono_img_writer_emit_label (MonoImageWriter *acfg, const char *name)
{
	asm_writer_emit_label (acfg, name);
}

void
mono_img_writer_emit_bytes (MonoImageWriter *acfg, const guint8* buf, int size)
{
	asm_writer_emit_bytes (acfg, buf, size);
}

void
mono_img_writer_emit_string (MonoImageWriter *acfg, const char *value)
{
	asm_writer_emit_string (acfg, value);
}

void
mono_img_writer_emit_line (MonoImageWriter *acfg)
{
	asm_writer_emit_line (acfg);
}

void
mono_img_writer_emit_alignment (MonoImageWriter *acfg, int size)
{
	asm_writer_emit_alignment (acfg, size);
}

void
mono_img_writer_emit_alignment_fill (MonoImageWriter *acfg, int size, int fill)
{
	asm_writer_emit_alignment_fill (acfg, size, fill);
}

void
mono_img_writer_emit_pointer_unaligned (MonoImageWriter *acfg, const char *target)
{
	asm_writer_emit_pointer_unaligned (acfg, target);
}

void
mono_img_writer_emit_pointer (MonoImageWriter *acfg, const char *target)
{
	asm_writer_emit_pointer (acfg, target);
}

void
mono_img_writer_emit_int16 (MonoImageWriter *acfg, int value)
{
	asm_writer_emit_int16 (acfg, value);
}

void
mono_img_writer_emit_int32 (MonoImageWriter *acfg, int value)
{
	asm_writer_emit_int32 (acfg, value);
}

void
mono_img_writer_emit_symbol (MonoImageWriter *acfg, const char *symbol)
{
	asm_writer_emit_symbol (acfg, symbol);
}

void
mono_img_writer_emit_symbol_diff (MonoImageWriter *acfg, const char *end, const char* start, int offset)
{
	asm_writer_emit_symbol_diff (acfg, end, start, offset);
}

void
mono_img_writer_emit_zero_bytes (MonoImageWriter *acfg, int num)
{
	asm_writer_emit_zero_bytes (acfg, num);
}

int
mono_img_writer_emit_writeout (MonoImageWriter *acfg)
{
	return asm_writer_emit_writeout (acfg);
}

void
mono_img_writer_emit_byte (MonoImageWriter *acfg, guint8 val)
{
	mono_img_writer_emit_bytes (acfg, &val, 1);
}

/*
 * Emit a relocation entry of type RELOC_TYPE against symbol SYMBOL at the current PC.
 * Do not advance PC.
 */
void
mono_img_writer_emit_reloc (MonoImageWriter *acfg, int reloc_type, const char *symbol, int addend)
{
	g_assert_not_reached ();
}

/*
 * mono_img_writer_emit_unset_mode:
 *
 *   Flush buffered data so it is safe to write to the output file from outside this
 * module. This is a nop for the binary writer.
 */
void
mono_img_writer_emit_unset_mode (MonoImageWriter *acfg)
{
	asm_writer_emit_unset_mode (acfg);
}

/*
 * mono_img_writer_get_output:
 *
 *   Return the output buffer of a binary writer emitting to memory. The returned memory
 * is from malloc, and it is owned by the caller.
 */
guint8*
mono_img_writer_get_output (MonoImageWriter *acfg, guint32 *size)
{
	g_assert_not_reached ();
	return NULL;
}

/*
 * mono_img_writer_create:
 *
 *   Create an image writer writing to FP.
 */
MonoImageWriter*
mono_img_writer_create (FILE *fp)
{
	MonoImageWriter *w = g_new0 (MonoImageWriter, 1);

	g_assert (fp);

	w->fp = fp;
	w->mempool = mono_mempool_new ();

	return w;
}

void
mono_img_writer_destroy (MonoImageWriter *w)
{
	// FIXME: Free all the stuff
	mono_mempool_destroy (w->mempool);
	g_free (w);
}

gboolean
mono_img_writer_subsections_supported (MonoImageWriter *acfg)
{
#ifdef TARGET_ASM_APPLE
	return FALSE;
#else
	return TRUE;
#endif
}

FILE *
mono_img_writer_get_fp (MonoImageWriter *acfg)
{
	return acfg->fp;
}

const char *
mono_img_writer_get_temp_label_prefix (MonoImageWriter *acfg)
{
	return AS_TEMP_LABEL_PREFIX;
}
