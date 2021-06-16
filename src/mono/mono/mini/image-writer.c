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
#ifndef HOST_WIN32
#include <sys/time.h>
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

#include "image-writer.h"

#ifndef HOST_WIN32
#include <mono/utils/freebsd-elf32.h>
#include <mono/utils/freebsd-elf64.h>
#endif

#include "mini.h"

#define TV_DECLARE(name) gint64 name
#define TV_GETTIME(tv) tv = mono_100ns_ticks ()
#define TV_ELAPSED(start,end) (((end) - (start)) / 10)

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
#define AS_INT16_DIRECTIVE ".hword"
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

#ifdef USE_BIN_WRITER

typedef struct _BinSymbol BinSymbol;
typedef struct _BinReloc BinReloc;
typedef struct _BinSection BinSection;

#endif

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
	gboolean use_bin_writer;
	const char *current_section;
	int current_subsection;
	const char *section_stack [16];
	int subsection_stack [16];
	int stack_pos;
	FILE *fp;
	/* Bin writer */
#ifdef USE_BIN_WRITER
	BinSymbol *symbols;
	BinSection *sections;
	BinSection *cur_section;
	BinReloc *relocations;
	GHashTable *labels;
	int num_relocs;
	guint8 *out_buf;
	int out_buf_size, out_buf_pos;
#endif
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

#ifdef USE_BIN_WRITER

typedef struct _BinLabel BinLabel;
struct _BinLabel {
	char *name;
	BinSection *section;
	int offset;
};

struct _BinReloc {
	BinReloc *next;
	char *val1;
	char *val2;
	BinSection *val2_section;
	int val2_offset;
	int offset;
	BinSection *section;
	int section_offset;
	int reloc_type;
};

struct _BinSymbol {
	BinSymbol *next;
	char *name;
	BinSection *section;
	int offset;
	gboolean is_function;
	gboolean is_global;
	char *end_label;
};

struct _BinSection {
	BinSection *next;
	BinSection *parent;
	char *name;
	int subsection;
	guint8 *data;
	int data_len;
	int cur_offset;
	int file_offset;
	int virt_offset;
	int shidx;
	guint64 addr;
	gboolean has_addr;
};

static void
bin_writer_emit_start (MonoImageWriter *acfg)
{
	acfg->labels = g_hash_table_new (g_str_hash, g_str_equal);
}

static void
bin_writer_emit_section_change (MonoImageWriter *acfg, const char *section_name, int subsection_index)
{
	BinSection *section;

	if (acfg->cur_section && acfg->cur_section->subsection == subsection_index
			&& strcmp (acfg->cur_section->name, section_name) == 0)
		return;
	for (section = acfg->sections; section; section = section->next) {
		if (section->subsection == subsection_index && strcmp (section->name, section_name) == 0) {
			acfg->cur_section = section;
			return;
		}
	}
	if (!section) {
		section = g_new0 (BinSection, 1);
		section->name = g_strdup (section_name);
		section->subsection = subsection_index;
		section->next = acfg->sections;
		acfg->sections = section;
		acfg->cur_section = section;
	}
}

static void
bin_writer_set_section_addr (MonoImageWriter *acfg, guint64 addr)
{
	acfg->cur_section->addr = addr;
	acfg->cur_section->has_addr = TRUE;
}

static void
bin_writer_emit_symbol_inner (MonoImageWriter *acfg, const char *name, const char *end_label, gboolean is_global, gboolean func)
{
	BinSymbol *symbol = g_new0 (BinSymbol, 1);
	symbol->name = g_strdup (name);
	if (end_label)
		symbol->end_label = g_strdup (end_label);
	symbol->is_function = func;
	symbol->is_global = is_global;
	symbol->section = acfg->cur_section;
	/* FIXME: we align after this call... */
	symbol->offset = symbol->section->cur_offset;
	symbol->next = acfg->symbols;
	acfg->symbols = symbol;
}

static void
bin_writer_emit_global (MonoImageWriter *acfg, const char *name, gboolean func)
{
	bin_writer_emit_symbol_inner (acfg, name, NULL, TRUE, func);
}

static void
bin_writer_emit_local_symbol (MonoImageWriter *acfg, const char *name, const char *end_label, gboolean func)
{
	bin_writer_emit_symbol_inner (acfg, name, end_label, FALSE, func);
}

static void
bin_writer_emit_label (MonoImageWriter *acfg, const char *name)
{
	BinLabel *label = g_new0 (BinLabel, 1);
	label->name = g_strdup (name);
	label->section = acfg->cur_section;
	label->offset = acfg->cur_section->cur_offset;
	g_hash_table_insert (acfg->labels, label->name, label);
}

static void
bin_writer_emit_ensure_buffer (BinSection *section, int size)
{
	int new_offset = section->cur_offset + size;
	if (new_offset >= section->data_len) {
		int new_size = section->data_len? section->data_len * 2: 256;
		guint8 *data;
		while (new_size <= new_offset)
			new_size *= 2;
		data = (guint8 *)g_malloc0 (new_size);
		memcpy (data, section->data, section->data_len);
		g_free (section->data);
		section->data = data;
		section->data_len = new_size;
	}
}

static void
bin_writer_emit_bytes (MonoImageWriter *acfg, const guint8* buf, int size)
{
	bin_writer_emit_ensure_buffer (acfg->cur_section, size);
	memcpy (acfg->cur_section->data + acfg->cur_section->cur_offset, buf, size);
	acfg->cur_section->cur_offset += size;
}

static void
bin_writer_emit_string (MonoImageWriter *acfg, const char *value)
{
	int size = strlen (value) + 1;
	bin_writer_emit_bytes (acfg, (const guint8*)value, size);
}

static void
bin_writer_emit_line (MonoImageWriter *acfg)
{
	/* Nothing to do in binary writer */
}

static void 
bin_writer_emit_alignment (MonoImageWriter *acfg, int size)
{
	int offset = acfg->cur_section->cur_offset;
	int add;
	offset += (size - 1);
	offset &= ~(size - 1);
	add = offset - acfg->cur_section->cur_offset;
	if (add) {
		bin_writer_emit_ensure_buffer (acfg->cur_section, add);
		acfg->cur_section->cur_offset += add;
	}
}

static void
bin_writer_emit_pointer_unaligned (MonoImageWriter *acfg, const char *target)
{
	BinReloc *reloc;

	if (!target) {
		acfg->cur_section->cur_offset += sizeof (gpointer);
		return;
	}

	reloc = g_new0 (BinReloc, 1);
	reloc->val1 = g_strdup (target);
	reloc->section = acfg->cur_section;
	reloc->section_offset = acfg->cur_section->cur_offset;
	reloc->next = acfg->relocations;
	acfg->relocations = reloc;
	if (strcmp (reloc->section->name, ".data") == 0) {
		acfg->num_relocs++;
		//g_print ("reloc: %s at %d\n", target, acfg->cur_section->cur_offset);
	}
	acfg->cur_section->cur_offset += sizeof (gpointer);
}

static void
bin_writer_emit_pointer (MonoImageWriter *acfg, const char *target)
{
	bin_writer_emit_alignment (acfg, sizeof (gpointer));
	bin_writer_emit_pointer_unaligned (acfg, target);
}

static void
bin_writer_emit_int16 (MonoImageWriter *acfg, int value)
{
	guint8 *data;
	bin_writer_emit_ensure_buffer (acfg->cur_section, 2);
	data = acfg->cur_section->data + acfg->cur_section->cur_offset;
	acfg->cur_section->cur_offset += 2;
	/* FIXME: little endian */
	data [0] = value;
	data [1] = value >> 8;
}

static void
bin_writer_emit_int32 (MonoImageWriter *acfg, int value)
{
	guint8 *data;
	bin_writer_emit_ensure_buffer (acfg->cur_section, 4);
	data = acfg->cur_section->data + acfg->cur_section->cur_offset;
	acfg->cur_section->cur_offset += 4;
	/* FIXME: little endian */
	data [0] = value;
	data [1] = value >> 8;
	data [2] = value >> 16;
	data [3] = value >> 24;
}

static BinReloc*
create_reloc (MonoImageWriter *acfg, const char *end, const char* start, int offset)
{
	BinReloc *reloc;
	reloc = (BinReloc *)mono_mempool_alloc0 (acfg->mempool, sizeof (BinReloc));
	reloc->val1 = mono_mempool_strdup (acfg->mempool, end);
	if (start)
	{
		if (strcmp (start, ".") == 0) {
			reloc->val2_section = acfg->cur_section;
			reloc->val2_offset = acfg->cur_section->cur_offset;
		} else {
			reloc->val2 = mono_mempool_strdup (acfg->mempool, start);
		}
	}
	reloc->offset = offset;
	reloc->section = acfg->cur_section;
	reloc->section_offset = acfg->cur_section->cur_offset;
	reloc->next = acfg->relocations;
	acfg->relocations = reloc;
	return reloc;
}

static void
bin_writer_emit_symbol (MonoImageWriter *acfg, const char *symbol)
{
	create_reloc (acfg, symbol, NULL, 0);
	acfg->cur_section->cur_offset += 4;
}

static void
bin_writer_emit_symbol_diff (MonoImageWriter *acfg, const char *end, const char* start, int offset)
{
	create_reloc (acfg, end, start, offset);
	acfg->cur_section->cur_offset += 4;
	/*if (strcmp (reloc->section->name, ".data") == 0) {
		acfg->num_relocs++;
		g_print ("reloc: %s - %s + %d at %d\n", end, start, offset, acfg->cur_section->cur_offset - 4);
	}*/
}

/* 
 * Emit a relocation entry of type RELOC_TYPE against symbol SYMBOL at the current PC.
 * Do not advance PC.
 */
static G_GNUC_UNUSED void
bin_writer_emit_reloc (MonoImageWriter *acfg, int reloc_type, const char *symbol, int addend)
{
	BinReloc *reloc = create_reloc (acfg, symbol, ".", addend);
	reloc->reloc_type = reloc_type;
}

static void
bin_writer_emit_zero_bytes (MonoImageWriter *acfg, int num)
{
	bin_writer_emit_ensure_buffer (acfg->cur_section, num);
	acfg->cur_section->cur_offset += num;
}

static void
bin_writer_fwrite (MonoImageWriter *acfg, void *val, size_t size, size_t nmemb)
{
	if (acfg->fp)
		fwrite (val, size, nmemb, acfg->fp);
	else {
		g_assert (acfg->out_buf_pos + (size * nmemb) <= acfg->out_buf_size);
		memcpy (acfg->out_buf + acfg->out_buf_pos, val, size * nmemb);
		acfg->out_buf_pos += (size * nmemb);
	}
}

static void
bin_writer_fseek (MonoImageWriter *acfg, int offset)
{
	if (acfg->fp)
		fseek (acfg->fp, offset, SEEK_SET);
	else
		acfg->out_buf_pos = offset;
}

#ifdef USE_MACH_WRITER

/*
 * This is a minimal implementation designed to support xdebug on 32 bit osx
 * FIXME: 64 bit support
 */

#include <mach-o/loader.h>

static gsize
get_label_addr (MonoImageWriter *acfg, const char *name)
{
	int offset;
	BinLabel *lab;
	BinSection *section;
	gsize value;

	lab = g_hash_table_lookup (acfg->labels, name);
	if (!lab)
		g_error ("Undefined label: '%s'.\n", name);
	section = lab->section;
	offset = lab->offset;
	if (section->parent) {
		value = section->parent->virt_offset + section->cur_offset + offset;
	} else {
		value = section->virt_offset + offset;
	}
	return value;
}


static void
resolve_reloc (MonoImageWriter *acfg, BinReloc *reloc, guint8 **out_data, gsize *out_vaddr, gsize *out_start_val, gsize *out_end_val)
{
	guint8 *data;
	gssize end_val, start_val;
	gsize vaddr;

	end_val = get_label_addr (acfg, reloc->val1);
	if (reloc->val2) {
		start_val = get_label_addr (acfg, reloc->val2);
	} else if (reloc->val2_section) {
		start_val = reloc->val2_offset;
		if (reloc->val2_section->parent)
			start_val += reloc->val2_section->parent->virt_offset + reloc->val2_section->cur_offset;
		else
			start_val += reloc->val2_section->virt_offset;
	} else {
		start_val = 0;
	}
	end_val = end_val - start_val + reloc->offset;
	if (reloc->section->parent) {
		data = reloc->section->parent->data;
		data += reloc->section->cur_offset;
		data += reloc->section_offset;
		vaddr = reloc->section->parent->virt_offset;
		vaddr += reloc->section->cur_offset;
		vaddr += reloc->section_offset;
	} else {
		data = reloc->section->data;
		data += reloc->section_offset;
		vaddr = reloc->section->virt_offset;
		vaddr += reloc->section_offset;
	}

	*out_start_val = start_val;
	*out_end_val = end_val;
	*out_data = data;
	*out_vaddr = vaddr;
}

static void
resolve_relocations (MonoImageWriter *acfg)
{
	BinReloc *reloc;
	guint8 *data;
	gsize end_val, start_val;
	gsize vaddr;

	/* Only resolve static relocations */
	for (reloc = acfg->relocations; reloc; reloc = reloc->next) {
		resolve_reloc (acfg, reloc, &data, &vaddr, &start_val, &end_val);
		data [0] = end_val;
		data [1] = end_val >> 8;
		data [2] = end_val >> 16;
		data [3] = end_val >> 24;
	}
}

static int
bin_writer_emit_writeout (MonoImageWriter *acfg)
{
	BinSection *s;
	int sindex, file_size, nsections, file_offset, vmaddr;
	struct mach_header header;
	struct segment_command segment;
	struct section *sections;

	/* Assing vm addresses to sections */
	nsections = 0;
	vmaddr = 0;
	for (s = acfg->sections; s; s = s->next) {
		s->virt_offset = vmaddr;
		vmaddr += s->cur_offset;
		nsections ++;
	}

	resolve_relocations (acfg);

	file_offset = 0;

	memset (&header, 0, sizeof (header));
	header.magic = MH_MAGIC;
	header.cputype = CPU_TYPE_X86;
	header.cpusubtype = CPU_SUBTYPE_X86_ALL;
	header.filetype = MH_OBJECT;
	header.ncmds = 0;
	header.sizeofcmds = 0;
	header.flags = 0;

	file_offset += sizeof (header);

	memset (&segment, 0, sizeof (segment));
	segment.cmd = LC_SEGMENT;
	segment.cmdsize = sizeof (segment);
	segment.maxprot = VM_PROT_READ|VM_PROT_WRITE|VM_PROT_EXECUTE;
	segment.initprot = VM_PROT_READ|VM_PROT_WRITE|VM_PROT_EXECUTE;

	file_offset += sizeof (segment);
	file_offset += nsections * sizeof (struct section);

	sections = g_new0 (struct section, nsections);
	sindex = 0;
	for (s = acfg->sections; s; s = s->next) {
		s->file_offset = file_offset;

		/* .debug_line -> __debug_line */
		sprintf (sections [sindex].sectname, "__%s", s->name + 1);
		sprintf (sections [sindex].segname, "%s", "__DWARF");
		sections [sindex].addr = s->virt_offset;
		sections [sindex].size = s->cur_offset;
		sections [sindex].offset = s->file_offset;

		file_offset += s->cur_offset;

		segment.nsects ++;
		segment.cmdsize += sizeof (struct section);

		sindex ++;
	}

	header.ncmds ++;
	header.sizeofcmds += segment.cmdsize;

	/* Emit data */
	file_size = file_offset;

	if (!acfg->fp) {
		acfg->out_buf_size = file_size;
		acfg->out_buf = g_malloc (acfg->out_buf_size);
	}

	bin_writer_fwrite (acfg, &header, sizeof (header), 1);
	bin_writer_fwrite (acfg, &segment, sizeof (segment), 1);
	bin_writer_fwrite (acfg, sections, sizeof (struct section), nsections);
	for (s = acfg->sections; s; s = s->next) {
		if (!acfg->fp)
			g_assert (acfg->out_buf_pos == s->file_offset);
		bin_writer_fwrite (acfg, s->data, s->cur_offset, 1);
	}

	if (acfg->fp)
		fclose (acfg->fp);

	return 0;
}

#endif

#ifdef USE_ELF_WRITER

enum {
	SECT_NULL,
	SECT_HASH,
	SECT_DYNSYM,
	SECT_DYNSTR,
	SECT_REL_DYN,
	SECT_RELA_DYN,
	SECT_TEXT,
	SECT_RODATA,
	SECT_DYNAMIC,
	SECT_GOT_PLT,
	SECT_DATA,
	SECT_BSS,
	SECT_DEBUG_FRAME,
	SECT_DEBUG_INFO,
	SECT_DEBUG_ABBREV,
	SECT_DEBUG_LINE,
	SECT_DEBUG_LOC,
	SECT_SHSTRTAB,
	SECT_SYMTAB,
	SECT_STRTAB,
	SECT_NUM
};

#if TARGET_SIZEOF_VOID_P == 4

typedef Elf32_Ehdr ElfHeader;
typedef Elf32_Shdr ElfSectHeader;
typedef Elf32_Phdr ElfProgHeader;
typedef Elf32_Sym ElfSymbol;
typedef Elf32_Rel ElfReloc;
typedef Elf32_Rela ElfRelocA;
typedef Elf32_Dyn ElfDynamic;

#else

typedef Elf64_Ehdr ElfHeader;
typedef Elf64_Shdr ElfSectHeader;
typedef Elf64_Phdr ElfProgHeader;
typedef Elf64_Sym ElfSymbol;
typedef Elf64_Rel ElfReloc;
typedef Elf64_Rela ElfRelocA;
typedef Elf64_Dyn ElfDynamic;

#endif

typedef struct {
	const char *name;
	int type;
	int esize;
	int flags;
	int align;
} SectInfo;

static SectInfo section_info [] = {
	{"", 0, 0, 0, 0},
	{".hash", SHT_HASH, 4, 2, TARGET_SIZEOF_VOID_P},
	{".dynsym", SHT_DYNSYM, sizeof (ElfSymbol), 2, TARGET_SIZEOF_VOID_P},
	{".dynstr", SHT_STRTAB, 0, 2, 1},
	{".rel.dyn", SHT_REL, sizeof (ElfReloc), 2, TARGET_SIZEOF_VOID_P},
	{".rela.dyn", SHT_RELA, sizeof (ElfRelocA), 2, TARGET_SIZEOF_VOID_P},
	{".text", SHT_PROGBITS, 0, 6, 4096},
	{".rodata", SHT_PROGBITS, 0, SHF_ALLOC, 4096},
	{".dynamic", SHT_DYNAMIC, sizeof (ElfDynamic), 3, TARGET_SIZEOF_VOID_P},
	{".got.plt", SHT_PROGBITS, TARGET_SIZEOF_VOID_P, 3, TARGET_SIZEOF_VOID_P},
	{".data", SHT_PROGBITS, 0, 3, 8},
	{".bss", SHT_NOBITS, 0, 3, 8},
	{".debug_frame", SHT_PROGBITS, 0, 0, 8},
	{".debug_info", SHT_PROGBITS, 0, 0, 1},
	{".debug_abbrev", SHT_PROGBITS, 0, 0, 1},
	{".debug_line", SHT_PROGBITS, 0, 0, 1},
	{".debug_loc", SHT_PROGBITS, 0, 0, 1},
	{".shstrtab", SHT_STRTAB, 0, 0, 1},
	{".symtab", SHT_SYMTAB, sizeof (ElfSymbol), 0, TARGET_SIZEOF_VOID_P},
	{".strtab", SHT_STRTAB, 0, 0, 1}
};

typedef struct {
	GString *data;
	GHashTable *hash;
} ElfStrTable;

static int
str_table_add (ElfStrTable *table, const char* value)
{
	int idx;
	if (!table->data) {
		table->data = g_string_new_len ("", 1);
		table->hash = g_hash_table_new (g_str_hash, g_str_equal);
	}
	idx = GPOINTER_TO_UINT (g_hash_table_lookup (table->hash, value));
	if (idx)
		return idx;
	idx = table->data->len;
	g_string_append (table->data, value);
	g_string_append_c (table->data, 0);
	g_hash_table_insert (table->hash, (void*)value, GUINT_TO_POINTER (idx));
	return idx;
}

static void
append_subsection (MonoImageWriter *acfg, ElfSectHeader *sheaders, BinSection *sect, BinSection *add)
{
	int offset = sect->cur_offset;
	/*offset += (sheaders [sect->shidx].sh_addralign - 1);
	offset &= ~(sheaders [sect->shidx].sh_addralign - 1);*/
	/* 
	 * FIXME: we shouldn't align subsections at all, but if we don't then the
	 * stuff inside the subsections which is aligned won't get aligned.
	 */
	if (strcmp (sect->name, ".debug_line") != 0) {
		offset += (8 - 1);
		offset &= ~(8 - 1);
	}
	bin_writer_emit_ensure_buffer (sect, offset);
	//g_print ("section %s aligned to %d from %d\n", sect->name, offset, sect->cur_offset);
	sect->cur_offset = offset;

	bin_writer_emit_ensure_buffer (sect, add->cur_offset);
	memcpy (sect->data + sect->cur_offset, add->data, add->cur_offset);
	add->parent = sect;
	sect->cur_offset += add->cur_offset;
	add->cur_offset = offset; /* it becomes the offset in the parent section */
	//g_print ("subsection %d of %s added at offset %d (align: %d)\n", add->subsection, sect->name, add->cur_offset, (int)sheaders [sect->shidx].sh_addralign);
	add->data = NULL;
	add->data_len = 0;
}

/* merge the subsections */
static int
collect_sections (MonoImageWriter *acfg, ElfSectHeader *sheaders, BinSection **out, int num)
{
	int i, j, maxs, num_sections;
	BinSection *sect;

	num_sections = 0;
	maxs = 0;
	for (sect = acfg->sections; sect; sect = sect->next) {
		if (sect->subsection == 0) {
			out [num_sections++] = sect;
			g_assert (num_sections < num);
		}
		maxs = MAX (maxs, sect->subsection);
	}
	for (i = 0; i < num_sections; i++) {
		for (j = 1; j <= maxs; ++j) {
			for (sect = acfg->sections; sect; sect = sect->next) {
				if (sect->subsection == j && strcmp (out [i]->name, sect->name) == 0) {
					append_subsection (acfg, sheaders, out [i], sect);
				}
			}
		}
	}
	return num_sections;
}

static unsigned long
elf_hash (const unsigned char *name)
{
	unsigned long h = 0, g;
	while (*name) {
		h = (h << 4) + *name++;
		if ((g = h & 0xf0000000))
			h ^= g >> 24;
		h &= ~g;
	}
	return h;
}

#define NUM_BUCKETS 17

static int*
build_hash (MonoImageWriter *acfg, int num_sections, ElfStrTable *dynstr)
{
	int *data;
	int num_symbols = 1 + num_sections + 3;
	BinSymbol *symbol;

	for (symbol = acfg->symbols; symbol; symbol = symbol->next) {
		if (!symbol->is_global)
			continue;
		num_symbols++;
		str_table_add (dynstr, symbol->name);
		/*g_print ("adding sym: %s\n", symbol->name);*/
	}
	str_table_add (dynstr, "__bss_start");
	str_table_add (dynstr, "_edata");
	str_table_add (dynstr, "_end");

	data = g_new0 (int, num_symbols + 2 + NUM_BUCKETS);
	data [0] = NUM_BUCKETS;
	data [1] = num_symbols;

	return data;
}

static gsize
get_label_addr (MonoImageWriter *acfg, const char *name)
{
	int offset;
	BinLabel *lab;
	BinSection *section;
	gsize value;

	lab = (BinLabel *)g_hash_table_lookup (acfg->labels, name);
	if (!lab)
		g_error ("Undefined label: '%s'.\n", name);
	section = lab->section;
	offset = lab->offset;
	if (section->parent) {
		value = section->parent->virt_offset + section->cur_offset + offset;
	} else {
		value = section->virt_offset + offset;
	}
	return value;
}

static ElfSymbol*
collect_syms (MonoImageWriter *acfg, int *hash, ElfStrTable *strtab, ElfSectHeader *sheaders, int *num_syms)
{
	ElfSymbol *symbols;
	BinSymbol *symbol;
	BinSection *section;
	int i;
	int *bucket;
	int *chain;
	unsigned long hashc;

	if (hash)
		symbols = g_new0 (ElfSymbol, hash [1]);
	else {
		i = 0;
		for (symbol = acfg->symbols; symbol; symbol = symbol->next)
			i ++;
		
		symbols = g_new0 (ElfSymbol, i + SECT_NUM + 10); /* FIXME */
	}

	/* the first symbol is undef, all zeroes */
	i = 1;
	if (sheaders) {
		int j;
		for (j = 1; j < SECT_NUM; ++j) {
			symbols [i].st_info = ELF32_ST_INFO (STB_LOCAL, STT_SECTION);
			symbols [i].st_shndx = j;
			symbols [i].st_value = sheaders [j].sh_addr;
			++i;
		}
	} else {
		for (section = acfg->sections; section; section = section->next) {
			if (section->parent)
				continue;
			symbols [i].st_info = ELF32_ST_INFO (STB_LOCAL, STT_SECTION);
			if (strcmp (section->name, ".text") == 0) {
				symbols [i].st_shndx = SECT_TEXT;
				section->shidx = SECT_TEXT;
				section->file_offset = 4096;
				symbols [i].st_value = section->virt_offset;
			} else if (strcmp (section->name, ".rodata") == 0) {
				symbols [i].st_shndx = SECT_RODATA;
				section->shidx = SECT_RODATA;
				section->file_offset = 4096;
				symbols [i].st_value = section->virt_offset;
			} else if (strcmp (section->name, ".data") == 0) {
				symbols [i].st_shndx = SECT_DATA;
				section->shidx = SECT_DATA;
				section->file_offset = 4096 + 28; /* FIXME */
				symbols [i].st_value = section->virt_offset;
			} else if (strcmp (section->name, ".bss") == 0) {
				symbols [i].st_shndx = SECT_BSS;
				section->shidx = SECT_BSS;
				section->file_offset = 4096 + 28 + 8; /* FIXME */
				symbols [i].st_value = section->virt_offset;
			}
			++i;
		}
	}
	for (symbol = acfg->symbols; symbol; symbol = symbol->next) {
		int offset;
		BinLabel *lab;
		if (!symbol->is_global && hash)
			continue;
		symbols [i].st_info = ELF32_ST_INFO (symbol->is_global ? STB_GLOBAL : STB_LOCAL, symbol->is_function? STT_FUNC : STT_OBJECT);
		symbols [i].st_name = str_table_add (strtab, symbol->name);
		/*g_print ("sym name %s tabled to %d\n", symbol->name, symbols [i].st_name);*/
		section = symbol->section;
		symbols [i].st_shndx = section->parent? section->parent->shidx: section->shidx;
		lab = (BinLabel *)g_hash_table_lookup (acfg->labels, symbol->name);
		offset = lab->offset;
		if (section->parent) {
			symbols [i].st_value = section->parent->virt_offset + section->cur_offset + offset;
		} else {
			symbols [i].st_value = section->virt_offset + offset;
		}

		if (symbol->end_label) {
			BinLabel *elab = (BinLabel *)g_hash_table_lookup (acfg->labels, symbol->end_label);
			g_assert (elab);
			symbols [i].st_size = elab->offset - lab->offset;
		}
		++i;
	}
	/* add special symbols */
	symbols [i].st_name = str_table_add (strtab, "__bss_start");
	symbols [i].st_shndx = 0xfff1;
	symbols [i].st_info = ELF32_ST_INFO (STB_GLOBAL, 0);
	++i;
	symbols [i].st_name = str_table_add (strtab, "_edata");
	symbols [i].st_shndx = 0xfff1;
	symbols [i].st_info = ELF32_ST_INFO (STB_GLOBAL, 0);
	++i;
	symbols [i].st_name = str_table_add (strtab, "_end");
	symbols [i].st_shndx = 0xfff1;
	symbols [i].st_info = ELF32_ST_INFO (STB_GLOBAL, 0);
	++i;

	if (num_syms)
		*num_syms = i;

	/* add to hash table */
	if (hash) {
		bucket = hash + 2;
		chain = hash + 2 + hash [0];
		for (i = 0; i < hash [1]; ++i) {
			int slot;
			/*g_print ("checking %d '%s' (sym %d)\n", symbols [i].st_name, strtab->data->str + symbols [i].st_name, i);*/
			if (!symbols [i].st_name)
				continue;
			hashc = elf_hash ((guint8*)strtab->data->str + symbols [i].st_name);
			slot = hashc % hash [0];
			/*g_print ("hashing '%s' at slot %d (sym %d)\n", strtab->data->str + symbols [i].st_name, slot, i);*/
			if (bucket [slot]) {
				chain [i] = bucket [slot];
				bucket [slot] = i;
			} else {
				bucket [slot] = i;
			}
		}
	}
	return symbols;
}

static void
reloc_symbols (MonoImageWriter *acfg, ElfSymbol *symbols, ElfSectHeader *sheaders, ElfStrTable *strtab, gboolean dynamic)
{
	BinSection *section;
	BinSymbol *symbol;
	int i;

	i = 1;
	if (dynamic) {
		for (section = acfg->sections; section; section = section->next) {
			if (section->parent)
				continue;
			symbols [i].st_value = sheaders [section->shidx].sh_addr;
			++i;
		}
	} else {
		for (i = 1; i < SECT_NUM; ++i) {
			symbols [i].st_value = sheaders [i].sh_addr;
		}
	}
	for (symbol = acfg->symbols; symbol; symbol = symbol->next) {
		int offset;
		BinLabel *lab;
		if (dynamic && !symbol->is_global)
			continue;
		section = symbol->section;
		lab = (BinLabel *)g_hash_table_lookup (acfg->labels, symbol->name);
		offset = lab->offset;
		if (section->parent) {
			symbols [i].st_value = sheaders [section->parent->shidx].sh_addr + section->cur_offset + offset;
		} else {
			symbols [i].st_value = sheaders [section->shidx].sh_addr + offset;
		}
		++i;
	}
	/* __bss_start */
	symbols [i].st_value = sheaders [SECT_BSS].sh_addr;
	++i;
	/* _edata */
	symbols [i].st_value = sheaders [SECT_DATA].sh_addr + sheaders [SECT_DATA].sh_size;
	++i;
	/* _end */
	symbols [i].st_value = sheaders [SECT_BSS].sh_addr + sheaders [SECT_BSS].sh_size;
	++i;
}

static void
resolve_reloc (MonoImageWriter *acfg, BinReloc *reloc, guint8 **out_data, gsize *out_vaddr, gsize *out_start_val, gsize *out_end_val)
{
	guint8 *data;
	gssize end_val, start_val;
	gsize vaddr;

	end_val = get_label_addr (acfg, reloc->val1);
	if (reloc->val2) {
		start_val = get_label_addr (acfg, reloc->val2);
	} else if (reloc->val2_section) {
		start_val = reloc->val2_offset;
		if (reloc->val2_section->parent)
			start_val += reloc->val2_section->parent->virt_offset + reloc->val2_section->cur_offset;
		else
			start_val += reloc->val2_section->virt_offset;
	} else {
		start_val = 0;
	}
	end_val = end_val - start_val + reloc->offset;
	if (reloc->section->parent) {
		data = reloc->section->parent->data;
		data += reloc->section->cur_offset;
		data += reloc->section_offset;
		vaddr = reloc->section->parent->virt_offset;
		vaddr += reloc->section->cur_offset;
		vaddr += reloc->section_offset;
	} else {
		data = reloc->section->data;
		data += reloc->section_offset;
		vaddr = reloc->section->virt_offset;
		vaddr += reloc->section_offset;
	}

	*out_start_val = start_val;
	*out_end_val = end_val;
	*out_data = data;
	*out_vaddr = vaddr;
}

#ifdef USE_ELF_RELA

static ElfRelocA*
resolve_relocations (MonoImageWriter *acfg)
{
	BinReloc *reloc;
	guint8 *data;
	gsize end_val, start_val;
	ElfRelocA *rr;
	int i;
	gsize vaddr;

	rr = g_new0 (ElfRelocA, acfg->num_relocs);
	i = 0;

	for (reloc = acfg->relocations; reloc; reloc = reloc->next) {
		resolve_reloc (acfg, reloc, &data, &vaddr, &start_val, &end_val);
		/* FIXME: little endian */
		data [0] = end_val;
		data [1] = end_val >> 8;
		data [2] = end_val >> 16;
		data [3] = end_val >> 24;
		// FIXME:
		if (start_val == 0 && reloc->val1 [0] != '.') {
			rr [i].r_offset = vaddr;
			rr [i].r_info = R_X86_64_RELATIVE;
			rr [i].r_addend = end_val;
			++i;
			g_assert (i <= acfg->num_relocs);
		}
	}
	return rr;
}

#else /* USE_ELF_RELA */

static void
do_reloc (MonoImageWriter *acfg, BinReloc *reloc, guint8 *data, gssize addr)
{
#ifdef TARGET_ARM
	/*
	 * We use the official ARM relocation types, but implement only the stuff actually
	 * needed by the code we generate.
	 */
	switch (reloc->reloc_type) {
	case R_ARM_CALL:
	case R_ARM_JUMP24: {
		guint32 *code = (guint32*)(gpointer)data;
		guint32 ins = *code;
		int diff = addr;

		if (reloc->reloc_type == R_ARM_CALL)
			/* bl */
			g_assert (data [3] == 0xeb);
		else
			/* b */
			g_assert (data [3] == 0xea);
		if (diff >= 0 && diff <= 33554431) {
			diff >>= 2;
			ins = (ins & 0xff000000) | diff;
			*code = ins;
		} else if (diff <= 0 && diff >= -33554432) {
			diff >>= 2;
			ins = (ins & 0xff000000) | (diff & ~0xff000000);
			*code = ins;
		} else {
			g_assert_not_reached ();
		}
		break;
	}
	case R_ARM_ALU_PC_G0_NC: {
		/* Generated by emit_plt () */
		guint8 *code = data;
		guint32 val = addr;

		g_assert (val <= 0xffffff);
		if (val & 0xff0000)
			ARM_ADD_REG_IMM (code, ARMREG_IP, ARMREG_PC, (val & 0xFF0000) >> 16, 16);
		else
			ARM_ADD_REG_IMM (code, ARMREG_IP, ARMREG_PC, 0, 0);
		ARM_ADD_REG_IMM (code, ARMREG_IP, ARMREG_IP, (val & 0xFF00) >> 8, 24);
		ARM_LDR_IMM (code, ARMREG_PC, ARMREG_IP, val & 0xFF);
		break;
	}		
	default:
		g_assert_not_reached ();
	}
#else
	g_assert_not_reached ();
#endif
}

static ElfReloc*
resolve_relocations (MonoImageWriter *acfg)
{
	BinReloc *reloc;
	guint8 *data;
	gsize end_val, start_val;
	ElfReloc *rr;
	int i;
	gsize vaddr;

	rr = g_new0 (ElfReloc, acfg->num_relocs);
	i = 0;

	for (reloc = acfg->relocations; reloc; reloc = reloc->next) {
		resolve_reloc (acfg, reloc, &data, &vaddr, &start_val, &end_val);
		/* FIXME: little endian */
		if (reloc->reloc_type) {
			/* Must be static */
			g_assert (start_val > 0);
			do_reloc (acfg, reloc, data, end_val);
		} else {
			data [0] = end_val;
			data [1] = end_val >> 8;
			data [2] = end_val >> 16;
			data [3] = end_val >> 24;
		}
		// FIXME:
		if (start_val == 0 && reloc->val1 [0] != '.') {
			rr [i].r_offset = vaddr;
			rr [i].r_info = R_386_RELATIVE;
			++i;
			g_assert (i <= acfg->num_relocs);
		}
	}
	return rr;
}

#endif /* USE_ELF_RELA */

static int normal_sections [] = { SECT_DATA, SECT_DEBUG_FRAME, SECT_DEBUG_INFO, SECT_DEBUG_ABBREV, SECT_DEBUG_LINE, SECT_DEBUG_LOC };

static int
bin_writer_emit_writeout (MonoImageWriter *acfg)
{
	ElfHeader header;
	ElfProgHeader progh [4];
	ElfSectHeader secth [SECT_NUM];
#ifdef USE_ELF_RELA
	ElfRelocA *relocs;
#else
	ElfReloc *relocs;
#endif
	ElfStrTable str_table = {NULL, NULL};
	ElfStrTable sh_str_table = {NULL, NULL};
	ElfStrTable dyn_str_table = {NULL, NULL};
	BinSection* all_sections [32];
	BinSection* sections [SECT_NUM];
	ElfSymbol *dynsym;
	ElfSymbol *symtab;
	ElfDynamic dynamic [14];
	int *hash;
	int i, num_sections, file_offset, virt_offset, size;
	int num_local_syms;

	/* Section headers */
	memset (&secth, 0, sizeof (secth));
	memset (&dynamic, 0, sizeof (dynamic));
	memset (&header, 0, sizeof (header));

	for (i = 1; i < SECT_NUM; ++i) {
		secth [i].sh_name = str_table_add (&sh_str_table, section_info [i].name);
		secth [i].sh_type = section_info [i].type;
		secth [i].sh_addralign = section_info [i].align;
		secth [i].sh_flags = section_info [i].flags;
		secth [i].sh_entsize = section_info [i].esize;
	}
	secth [SECT_DYNSYM].sh_info = TARGET_SIZEOF_VOID_P == 4 ? 4 : 2;
	secth [SECT_SYMTAB].sh_info = TARGET_SIZEOF_VOID_P == 4 ? 20 : 17;
	secth [SECT_HASH].sh_link = SECT_DYNSYM;
	secth [SECT_DYNSYM].sh_link = SECT_DYNSTR;
	secth [SECT_REL_DYN].sh_link = SECT_DYNSYM;
	secth [SECT_RELA_DYN].sh_link = SECT_DYNSYM;
	secth [SECT_DYNAMIC].sh_link = SECT_DYNSTR;
	secth [SECT_SYMTAB].sh_link = SECT_STRTAB;

	num_sections = collect_sections (acfg, secth, all_sections, 16);
	hash = build_hash (acfg, num_sections, &dyn_str_table);
#if 0
	g_print ("num_sections: %d\n", num_sections);
	g_print ("dynsym: %d, dynstr size: %d\n", hash [1], (int)dyn_str_table.data->len);
	for (i = 0; i < num_sections; ++i) {
		g_print ("section %s, size: %d, %x\n", all_sections [i]->name, all_sections [i]->cur_offset, all_sections [i]->cur_offset);
	}
#endif
	/* Associate the bin sections with the ELF sections */
	memset (sections, 0, sizeof (sections));
	for (i = 0; i < num_sections; ++i) {
		BinSection *sect = all_sections [i];
		int j;

		for (j = 0; j < SECT_NUM; ++j) {
			if (strcmp (sect->name, section_info [j].name) == 0) {
				sect->shidx = j;
				break;
			}
		}

		sections [all_sections [i]->shidx] = sect;
	}

	/* at this point we know where in the file the first segment sections go */
	dynsym = collect_syms (acfg, hash, &dyn_str_table, NULL, NULL);
	num_local_syms = hash [1];
	symtab = collect_syms (acfg, NULL, &str_table, secth, &num_local_syms);

	file_offset = virt_offset = sizeof (header) + sizeof (progh);
	secth [SECT_HASH].sh_addr = secth [SECT_HASH].sh_offset = file_offset;
	size = sizeof (int) * (2 + hash [0] + hash [1]);
	virt_offset = (file_offset += size);
	secth [SECT_HASH].sh_size = size;
	secth [SECT_DYNSYM].sh_addr = secth [SECT_DYNSYM].sh_offset = file_offset;
	size = sizeof (ElfSymbol) * hash [1];
	virt_offset = (file_offset += size);
	secth [SECT_DYNSYM].sh_size = size;
	secth [SECT_DYNSTR].sh_addr = secth [SECT_DYNSTR].sh_offset = file_offset;
	size = dyn_str_table.data->len;
	virt_offset = (file_offset += size);
	secth [SECT_DYNSTR].sh_size = size;
	file_offset += 4-1;
	file_offset &= ~(4-1);
	secth [SECT_REL_DYN].sh_addr = secth [SECT_REL_DYN].sh_offset = file_offset;
#ifndef USE_ELF_RELA
	size = sizeof (ElfReloc) * acfg->num_relocs;
#else
	size = 0;
#endif
	virt_offset = (file_offset += size);
	secth [SECT_REL_DYN].sh_size = size;
	secth [SECT_RELA_DYN].sh_addr = secth [SECT_RELA_DYN].sh_offset = file_offset;
#ifdef USE_ELF_RELA
	size = sizeof (ElfRelocA) * acfg->num_relocs;
#else
	size = 0;
#endif
	virt_offset = (file_offset += size);
	secth [SECT_RELA_DYN].sh_size = size;

	file_offset = ALIGN_TO (file_offset, secth [SECT_TEXT].sh_addralign);
	virt_offset = file_offset;
	secth [SECT_TEXT].sh_addr = secth [SECT_TEXT].sh_offset = file_offset;
	if (sections [SECT_TEXT]) {
		if (sections [SECT_TEXT]->has_addr) {
			secth [SECT_TEXT].sh_addr = sections [SECT_TEXT]->addr;
			secth [SECT_TEXT].sh_flags &= ~SHF_ALLOC;
		}
		size = sections [SECT_TEXT]->cur_offset;
		secth [SECT_TEXT].sh_size = size;
		file_offset += size;
	}

	file_offset = ALIGN_TO (file_offset, secth [SECT_RODATA].sh_addralign);
	virt_offset = file_offset;
	secth [SECT_RODATA].sh_addr = virt_offset;
	secth [SECT_RODATA].sh_offset = file_offset;
	if (sections [SECT_RODATA]) {
		size = sections [SECT_RODATA]->cur_offset;
		secth [SECT_RODATA].sh_size = size;
		file_offset += size;
		virt_offset += size;
	}

	file_offset = ALIGN_TO (file_offset, secth [SECT_DYNAMIC].sh_addralign);
	virt_offset = file_offset;

	/* .dynamic, .got.plt, .data, .bss here */
	/* Have to increase the virt offset since these go to a separate segment */
	virt_offset += PAGESIZE;
	secth [SECT_DYNAMIC].sh_addr = virt_offset;
	secth [SECT_DYNAMIC].sh_offset = file_offset;
	size = sizeof (dynamic);
	secth [SECT_DYNAMIC].sh_size = size;
	file_offset += size;
	virt_offset += size;

	file_offset = ALIGN_TO (file_offset, secth [SECT_GOT_PLT].sh_addralign);
	virt_offset = ALIGN_TO (virt_offset, secth [SECT_GOT_PLT].sh_addralign);
	secth [SECT_GOT_PLT].sh_addr = virt_offset;
	secth [SECT_GOT_PLT].sh_offset = file_offset;
	size = 3 * TARGET_SIZEOF_VOID_P;
	secth [SECT_GOT_PLT].sh_size = size;
	file_offset += size;
	virt_offset += size;

	file_offset = ALIGN_TO (file_offset, secth [SECT_DATA].sh_addralign);
	virt_offset = ALIGN_TO (virt_offset, secth [SECT_DATA].sh_addralign);
	secth [SECT_DATA].sh_addr = virt_offset;
	secth [SECT_DATA].sh_offset = file_offset;
	if (sections [SECT_DATA]) {
		size = sections [SECT_DATA]->cur_offset;
		secth [SECT_DATA].sh_size = size;
		file_offset += size;
		virt_offset += size;
	}

	file_offset = ALIGN_TO (file_offset, secth [SECT_BSS].sh_addralign);
	virt_offset = ALIGN_TO (virt_offset, secth [SECT_BSS].sh_addralign);
	secth [SECT_BSS].sh_addr = virt_offset;
	secth [SECT_BSS].sh_offset = file_offset;
	if (sections [SECT_BSS]) {
		size = sections [SECT_BSS]->cur_offset;
		secth [SECT_BSS].sh_size = size;
	}

	/* virtual doesn't matter anymore */
	file_offset = ALIGN_TO (file_offset, secth [SECT_DEBUG_FRAME].sh_addralign);
 	secth [SECT_DEBUG_FRAME].sh_offset = file_offset;
 	if (sections [SECT_DEBUG_FRAME])
 		size = sections [SECT_DEBUG_FRAME]->cur_offset;
 	else
 		size = 0;
 	secth [SECT_DEBUG_FRAME].sh_size = size;
 	file_offset += size;

 	secth [SECT_DEBUG_INFO].sh_offset = file_offset;
 	if (sections [SECT_DEBUG_INFO])
 		size = sections [SECT_DEBUG_INFO]->cur_offset;
 	else
 		size = 0;
 	secth [SECT_DEBUG_INFO].sh_size = size;
 	file_offset += size;

 	secth [SECT_DEBUG_ABBREV].sh_offset = file_offset;
 	if (sections [SECT_DEBUG_ABBREV])
 		size = sections [SECT_DEBUG_ABBREV]->cur_offset;
 	else
 		size = 0;
 	secth [SECT_DEBUG_ABBREV].sh_size = size;
 	file_offset += size;

 	secth [SECT_DEBUG_LINE].sh_offset = file_offset;
 	if (sections [SECT_DEBUG_LINE])
 		size = sections [SECT_DEBUG_LINE]->cur_offset;
 	else
 		size = 0;
 	secth [SECT_DEBUG_LINE].sh_size = size;
 	file_offset += size;

 	secth [SECT_DEBUG_LOC].sh_offset = file_offset;
 	if (sections [SECT_DEBUG_LOC])
 		size = sections [SECT_DEBUG_LOC]->cur_offset;
 	else
 		size = 0;
 	secth [SECT_DEBUG_LOC].sh_size = size;
 	file_offset += size;

	file_offset = ALIGN_TO (file_offset, secth [SECT_SHSTRTAB].sh_addralign);
	secth [SECT_SHSTRTAB].sh_offset = file_offset;
	size = sh_str_table.data->len;
	secth [SECT_SHSTRTAB].sh_size = size;
	file_offset += size;

	file_offset = ALIGN_TO (file_offset, secth [SECT_SYMTAB].sh_addralign);
	secth [SECT_SYMTAB].sh_offset = file_offset;
	size = sizeof (ElfSymbol) * num_local_syms;
	secth [SECT_SYMTAB].sh_size = size;
	file_offset += size;

	file_offset = ALIGN_TO (file_offset, secth [SECT_STRTAB].sh_addralign);
	secth [SECT_STRTAB].sh_offset = file_offset;
	size = str_table.data->len;
	secth [SECT_STRTAB].sh_size = size;
	file_offset += size;

	for (i = 1; i < SECT_NUM; ++i) {
		if (section_info [i].esize != 0)
			g_assert (secth [i].sh_size % section_info [i].esize == 0);
	}

	file_offset += 4-1;
	file_offset &= ~(4-1);

	header.e_ident [EI_MAG0] = ELFMAG0;
	header.e_ident [EI_MAG1] = ELFMAG1;
	header.e_ident [EI_MAG2] = ELFMAG2;
	header.e_ident [EI_MAG3] = ELFMAG3;
	header.e_ident [EI_CLASS] = TARGET_SIZEOF_VOID_P == 4 ? ELFCLASS32 : ELFCLASS64;
	header.e_ident [EI_DATA] = ELFDATA2LSB;
	header.e_ident [EI_VERSION] = EV_CURRENT;
	header.e_ident [EI_OSABI] = ELFOSABI_NONE;
	header.e_ident [EI_ABIVERSION] = 0;
	for (i = EI_PAD; i < EI_NIDENT; ++i)
		header.e_ident [i] = 0;

	header.e_type = ET_DYN;
#if defined(TARGET_X86)
	header.e_machine = EM_386;
#elif defined(TARGET_AMD64)
	header.e_machine = EM_X86_64;
#elif defined(TARGET_ARM)
	header.e_machine = EM_ARM;
#else
	g_assert_not_reached ();
#endif
	header.e_version = 1;

	header.e_phoff = sizeof (header);
	header.e_ehsize = sizeof (header);
	header.e_phentsize = sizeof (ElfProgHeader);
	header.e_phnum = 4;
	header.e_entry = secth [SECT_TEXT].sh_addr;
	header.e_shstrndx = SECT_SHSTRTAB;
	header.e_shentsize = sizeof (ElfSectHeader);
	header.e_shnum = SECT_NUM;
	header.e_shoff = file_offset;

	/* dynamic data */
	i = 0;
	dynamic [i].d_tag = DT_HASH;
	dynamic [i].d_un.d_val = secth [SECT_HASH].sh_offset;
	++i;
	dynamic [i].d_tag = DT_STRTAB;
	dynamic [i].d_un.d_val = secth [SECT_DYNSTR].sh_offset;
	++i;
	dynamic [i].d_tag = DT_SYMTAB;
	dynamic [i].d_un.d_val = secth [SECT_DYNSYM].sh_offset;
	++i;
	dynamic [i].d_tag = DT_STRSZ;
	dynamic [i].d_un.d_val = dyn_str_table.data->len;
	++i;
	dynamic [i].d_tag = DT_SYMENT;
	dynamic [i].d_un.d_val = sizeof (ElfSymbol);
	++i;
#ifdef USE_ELF_RELA
	dynamic [i].d_tag = DT_RELA;
	dynamic [i].d_un.d_val = secth [SECT_RELA_DYN].sh_offset;
	++i;
	dynamic [i].d_tag = DT_RELASZ;
	dynamic [i].d_un.d_val = secth [SECT_RELA_DYN].sh_size;
	++i;
	dynamic [i].d_tag = DT_RELAENT;
	dynamic [i].d_un.d_val = sizeof (ElfRelocA);
	++i;
#else
	dynamic [i].d_tag = DT_REL;
	dynamic [i].d_un.d_val = secth [SECT_REL_DYN].sh_offset;
	++i;
	dynamic [i].d_tag = DT_RELSZ;
	dynamic [i].d_un.d_val = secth [SECT_REL_DYN].sh_size;
	++i;
	dynamic [i].d_tag = DT_RELENT;
	dynamic [i].d_un.d_val = sizeof (ElfReloc);
	++i;
#endif
	dynamic [i].d_tag = DT_RELCOUNT;
	dynamic [i].d_un.d_val = acfg->num_relocs;
	++i;

	/* Program header */
	memset (&progh, 0, sizeof (progh));
	progh [0].p_type = PT_LOAD;
	progh [0].p_filesz = progh [0].p_memsz = secth [SECT_DYNAMIC].sh_offset;
	progh [0].p_align = 4096;
	progh [0].p_flags = 5;

	progh [1].p_type = PT_LOAD;
	progh [1].p_offset = secth [SECT_DYNAMIC].sh_offset;
	progh [1].p_vaddr = progh [1].p_paddr = secth [SECT_DYNAMIC].sh_addr;
	progh [1].p_filesz = secth [SECT_BSS].sh_offset  - secth [SECT_DYNAMIC].sh_offset;
	progh [1].p_memsz = secth [SECT_BSS].sh_addr + secth [SECT_BSS].sh_size - secth [SECT_DYNAMIC].sh_addr;
	progh [1].p_align = 4096;
	progh [1].p_flags = 6;

	progh [2].p_type = PT_DYNAMIC;
	progh [2].p_offset = secth [SECT_DYNAMIC].sh_offset;
	progh [2].p_vaddr = progh [2].p_paddr = secth [SECT_DYNAMIC].sh_addr;
	progh [2].p_filesz = progh [2].p_memsz = secth [SECT_DYNAMIC].sh_size;
	progh [2].p_align = TARGET_SIZEOF_VOID_P;
	progh [2].p_flags = 6;

	progh [3].p_type = PT_GNU_STACK;
	progh [3].p_offset = secth [SECT_DYNAMIC].sh_offset;
	progh [3].p_vaddr = progh [3].p_paddr = secth [SECT_DYNAMIC].sh_addr;
	progh [3].p_filesz = progh [3].p_memsz = secth [SECT_DYNAMIC].sh_size;
	progh [3].p_align = TARGET_SIZEOF_VOID_P;
	progh [3].p_flags = 6;

	/* Compute the addresses of the bin sections, so relocation can be done */
	for (i = 0; i < SECT_NUM; ++i) {
		if (sections [i]) {
			sections [i]->file_offset = secth [i].sh_offset;
			sections [i]->virt_offset = secth [i].sh_addr;
		}
	}

	reloc_symbols (acfg, dynsym, secth, &dyn_str_table, TRUE);
	reloc_symbols (acfg, symtab, secth, &str_table, FALSE);
	relocs = resolve_relocations (acfg);

	if (!acfg->fp) {
		acfg->out_buf_size = file_offset + sizeof (secth);
		acfg->out_buf = (guint8 *)g_malloc (acfg->out_buf_size);
	}

	bin_writer_fwrite (acfg, &header, sizeof (header), 1);
	bin_writer_fwrite (acfg, &progh, sizeof (progh), 1);
	bin_writer_fwrite (acfg, hash, sizeof (int) * (hash [0] + hash [1] + 2), 1);
	bin_writer_fwrite (acfg, dynsym, sizeof (ElfSymbol) * hash [1], 1);
	bin_writer_fwrite (acfg, dyn_str_table.data->str, dyn_str_table.data->len, 1);
	/* .rel.dyn */
	bin_writer_fseek (acfg, secth [SECT_REL_DYN].sh_offset);
	bin_writer_fwrite (acfg, relocs, sizeof (ElfReloc), acfg->num_relocs);

	/* .rela.dyn */
	bin_writer_fseek (acfg, secth [SECT_RELA_DYN].sh_offset);
	bin_writer_fwrite (acfg, relocs, secth [SECT_RELA_DYN].sh_size, 1);

	/* .text */
	if (sections [SECT_TEXT]) {
		bin_writer_fseek (acfg, secth [SECT_TEXT].sh_offset);
		bin_writer_fwrite (acfg, sections [SECT_TEXT]->data, sections [SECT_TEXT]->cur_offset, 1);
	}
	/* .rodata */
	if (sections [SECT_RODATA]) {
		bin_writer_fseek (acfg, secth [SECT_RODATA].sh_offset);
		bin_writer_fwrite (acfg, sections [SECT_RODATA]->data, sections [SECT_RODATA]->cur_offset, 1);
	}
	/* .dynamic */
	bin_writer_fseek (acfg, secth [SECT_DYNAMIC].sh_offset);
	bin_writer_fwrite (acfg, dynamic, sizeof (dynamic), 1);

	/* .got.plt */
	size = secth [SECT_DYNAMIC].sh_addr;
	bin_writer_fseek (acfg, secth [SECT_GOT_PLT].sh_offset);
	bin_writer_fwrite (acfg, &size, sizeof (size), 1);

	/* normal sections */
	for (i = 0; i < sizeof (normal_sections) / sizeof (normal_sections [0]); ++i) {
		int sect = normal_sections [i];

		if (sections [sect]) {
			bin_writer_fseek (acfg, secth [sect].sh_offset);
			bin_writer_fwrite (acfg, sections [sect]->data, sections [sect]->cur_offset, 1);
		}
	}

	bin_writer_fseek (acfg, secth [SECT_SHSTRTAB].sh_offset);
	bin_writer_fwrite (acfg, sh_str_table.data->str, sh_str_table.data->len, 1);
	bin_writer_fseek (acfg, secth [SECT_SYMTAB].sh_offset);
	bin_writer_fwrite (acfg, symtab, sizeof (ElfSymbol) * num_local_syms, 1);
	bin_writer_fseek (acfg, secth [SECT_STRTAB].sh_offset);
	bin_writer_fwrite (acfg, str_table.data->str, str_table.data->len, 1);
	/*g_print ("file_offset %d vs %d\n", file_offset, ftell (file));*/
	/*g_assert (file_offset >= ftell (file));*/
	bin_writer_fseek (acfg, file_offset);
	bin_writer_fwrite (acfg, &secth, sizeof (secth), 1);

	if (acfg->fp)
		fclose (acfg->fp);

	return 0;
}

#endif /* USE_ELF_WRITER */

#endif /* USE_BIN_WRITER */

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
	const char *stype;

	if (func)
		stype = "function";
	else
		stype = "object";

	asm_writer_emit_unset_mode (acfg);

#if defined(TARGET_ASM_APPLE)

#elif defined(TARGET_ARM)
	fprintf (acfg->fp, "\t.type %s,#%s\n", name, stype);
#else
	fprintf (acfg->fp, "\t.type %s,@%s\n", name, stype);
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

#ifndef USE_BIN_WRITER
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
#endif

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
#ifdef USE_BIN_WRITER
	if (acfg->use_bin_writer)
		bin_writer_emit_start (acfg);
	else
		asm_writer_emit_start (acfg);
#else
	asm_writer_emit_start (acfg);
#endif
}

void
mono_img_writer_emit_section_change (MonoImageWriter *acfg, const char *section_name, int subsection_index)
{
#ifdef USE_BIN_WRITER
	if (acfg->use_bin_writer)
		bin_writer_emit_section_change (acfg, section_name, subsection_index);
	else
		asm_writer_emit_section_change (acfg, section_name, subsection_index);
#else
	asm_writer_emit_section_change (acfg, section_name, subsection_index);
#endif

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
#ifdef USE_BIN_WRITER
	if (!acfg->use_bin_writer)
		NOT_IMPLEMENTED;
	else
		bin_writer_set_section_addr (acfg, addr);
#else
	NOT_IMPLEMENTED;
#endif
}

void
mono_img_writer_emit_global (MonoImageWriter *acfg, const char *name, gboolean func)
{
#ifdef USE_BIN_WRITER
	if (acfg->use_bin_writer)
		bin_writer_emit_global (acfg, name, func);
	else
		asm_writer_emit_global (acfg, name, func);
#else
	asm_writer_emit_global (acfg, name, func);
#endif
}

void
mono_img_writer_emit_local_symbol (MonoImageWriter *acfg, const char *name, const char *end_label, gboolean func)
{
#ifdef USE_BIN_WRITER
	if (acfg->use_bin_writer)
		bin_writer_emit_local_symbol (acfg, name, end_label, func);
	else
		asm_writer_emit_local_symbol (acfg, name, end_label, func);
#else
	asm_writer_emit_local_symbol (acfg, name, end_label, func);
#endif
}

void
mono_img_writer_emit_symbol_size (MonoImageWriter *acfg, const char *name, const char *end_label)
{
	if (!acfg->use_bin_writer)
		asm_writer_emit_symbol_size (acfg, name, end_label);
}

void
mono_img_writer_emit_label (MonoImageWriter *acfg, const char *name)
{
#ifdef USE_BIN_WRITER
	if (acfg->use_bin_writer)
		bin_writer_emit_label (acfg, name);
	else
		asm_writer_emit_label (acfg, name);
#else
	asm_writer_emit_label (acfg, name);
#endif
}

void
mono_img_writer_emit_bytes (MonoImageWriter *acfg, const guint8* buf, int size)
{
#ifdef USE_BIN_WRITER
	if (acfg->use_bin_writer)
		bin_writer_emit_bytes (acfg, buf, size);
	else
		asm_writer_emit_bytes (acfg, buf, size);
#else
	asm_writer_emit_bytes (acfg, buf, size);
#endif
}

void
mono_img_writer_emit_string (MonoImageWriter *acfg, const char *value)
{
#ifdef USE_BIN_WRITER
	if (acfg->use_bin_writer)
		bin_writer_emit_string (acfg, value);
	else
		asm_writer_emit_string (acfg, value);
#else
	asm_writer_emit_string (acfg, value);
#endif
}

void
mono_img_writer_emit_line (MonoImageWriter *acfg)
{
#ifdef USE_BIN_WRITER
	if (acfg->use_bin_writer)
		bin_writer_emit_line (acfg);
	else
		asm_writer_emit_line (acfg);
#else
		asm_writer_emit_line (acfg);
#endif
}

void
mono_img_writer_emit_alignment (MonoImageWriter *acfg, int size)
{
#ifdef USE_BIN_WRITER
	if (acfg->use_bin_writer)
		bin_writer_emit_alignment (acfg, size);
	else
		asm_writer_emit_alignment (acfg, size);
#else
	asm_writer_emit_alignment (acfg, size);
#endif
}

void
mono_img_writer_emit_alignment_fill (MonoImageWriter *acfg, int size, int fill)
{
#ifdef USE_BIN_WRITER
	if (acfg->use_bin_writer)
		bin_writer_emit_alignment (acfg, size);
	else
		asm_writer_emit_alignment (acfg, size);
#else
	asm_writer_emit_alignment_fill (acfg, size, fill);
#endif
}

void
mono_img_writer_emit_pointer_unaligned (MonoImageWriter *acfg, const char *target)
{
#ifdef USE_BIN_WRITER
	if (acfg->use_bin_writer)
		bin_writer_emit_pointer_unaligned (acfg, target);
	else
		asm_writer_emit_pointer_unaligned (acfg, target);
#else
	asm_writer_emit_pointer_unaligned (acfg, target);
#endif
}

void
mono_img_writer_emit_pointer (MonoImageWriter *acfg, const char *target)
{
#ifdef USE_BIN_WRITER
	if (acfg->use_bin_writer)
		bin_writer_emit_pointer (acfg, target);
	else
		asm_writer_emit_pointer (acfg, target);
#else
	asm_writer_emit_pointer (acfg, target);
#endif
}

void
mono_img_writer_emit_int16 (MonoImageWriter *acfg, int value)
{
#ifdef USE_BIN_WRITER
	if (acfg->use_bin_writer)
		bin_writer_emit_int16 (acfg, value);
	else
		asm_writer_emit_int16 (acfg, value);
#else
	asm_writer_emit_int16 (acfg, value);
#endif
}

void
mono_img_writer_emit_int32 (MonoImageWriter *acfg, int value)
{
#ifdef USE_BIN_WRITER
	if (acfg->use_bin_writer)
		bin_writer_emit_int32 (acfg, value);
	else
		asm_writer_emit_int32 (acfg, value);
#else
	asm_writer_emit_int32 (acfg, value);
#endif
}

void
mono_img_writer_emit_symbol (MonoImageWriter *acfg, const char *symbol)
{
#ifdef USE_BIN_WRITER
	if (acfg->use_bin_writer)
		bin_writer_emit_symbol (acfg, symbol);
	else
		asm_writer_emit_symbol (acfg, symbol);
#else
	asm_writer_emit_symbol (acfg, symbol);
#endif
}

void
mono_img_writer_emit_symbol_diff (MonoImageWriter *acfg, const char *end, const char* start, int offset)
{
#ifdef USE_BIN_WRITER
	if (acfg->use_bin_writer)
		bin_writer_emit_symbol_diff (acfg, end, start, offset);
	else
		asm_writer_emit_symbol_diff (acfg, end, start, offset);
#else
	asm_writer_emit_symbol_diff (acfg, end, start, offset);
#endif
}

void
mono_img_writer_emit_zero_bytes (MonoImageWriter *acfg, int num)
{
#ifdef USE_BIN_WRITER
	if (acfg->use_bin_writer)
		bin_writer_emit_zero_bytes (acfg, num);
	else
		asm_writer_emit_zero_bytes (acfg, num);
#else
	asm_writer_emit_zero_bytes (acfg, num);
#endif
}

int
mono_img_writer_emit_writeout (MonoImageWriter *acfg)
{
#ifdef USE_BIN_WRITER
	if (acfg->use_bin_writer)
		return bin_writer_emit_writeout (acfg);
	else
		return asm_writer_emit_writeout (acfg);
#else
		return asm_writer_emit_writeout (acfg);
#endif
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
	/* This is only supported by the bin writer */
#ifdef USE_BIN_WRITER
	if (acfg->use_bin_writer)
		bin_writer_emit_reloc (acfg, reloc_type, symbol, addend);
	else
		g_assert_not_reached ();
#else
		g_assert_not_reached ();
#endif
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
	if (!acfg->use_bin_writer)
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
#ifdef USE_BIN_WRITER
	guint8 *buf;

	g_assert (acfg->use_bin_writer);

	buf = acfg->out_buf;
	*size = acfg->out_buf_size;
	acfg->out_buf = NULL;
	return buf;
#else
	g_assert_not_reached ();
	return NULL;
#endif
}

/*
 * Return whenever the binary writer is supported on this platform.
 */
gboolean
mono_bin_writer_supported (void)
{
#ifdef USE_BIN_WRITER
	return TRUE;
#else
	return FALSE;
#endif
}

/*
 * mono_img_writer_create:
 *
 *   Create an image writer writing to FP. If USE_BIN_WRITER is TRUE, FP can be NULL,
 * in this case the image writer will write to a memory buffer obtainable by calling
 * mono_img_writer_get_output ().
 */
MonoImageWriter*
mono_img_writer_create (FILE *fp, gboolean use_bin_writer)
{
	MonoImageWriter *w = g_new0 (MonoImageWriter, 1);
	
#ifndef USE_BIN_WRITER
	g_assert (!use_bin_writer);
#endif

	if (!use_bin_writer)
		g_assert (fp);

	w->fp = fp;
	w->use_bin_writer = use_bin_writer;
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
	return acfg->use_bin_writer;
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
