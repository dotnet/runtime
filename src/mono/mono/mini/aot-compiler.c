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
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
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
#include <mono/metadata/gc-internal.h>
#include <mono/utils/mono-logger.h>
#include "mono/utils/mono-compiler.h"

#include "mini.h"
#include "version.h"

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
	gboolean metadata_only;
	gboolean bind_to_runtime_version;
} MonoAotOptions;

typedef struct MonoAotStats {
	int ccount, mcount, lmfcount, abscount, wrappercount, gcount, ocount, genericcount;
	int code_size, info_size, ex_info_size, got_size, class_info_size, got_info_size, got_info_offsets_size;
	int methods_without_got_slots, direct_calls, all_calls;
	int got_slots;
	int got_slot_types [MONO_PATCH_INFO_NONE];
} MonoAotStats;

/*#define USE_ELF_WRITER 1*/

#if defined(USE_ELF_WRITER)
#define USE_BIN_WRITER 1
#endif

#ifdef USE_BIN_WRITER

typedef struct _BinSymbol BinSymbol;
typedef struct _BinReloc BinReloc;
typedef struct _BinSection BinSection;

#else

/* emit mode */
enum {
	EMIT_NONE,
	EMIT_BYTE,
	EMIT_WORD,
	EMIT_LONG
};

#endif

typedef struct MonoAotCompile {
	MonoImage *image;
	MonoCompile **cfgs;
	GHashTable *patch_to_plt_offset;
	GHashTable **patch_to_plt_offset_wrapper;
	GHashTable *plt_offset_to_patch;
	GHashTable *patch_to_shared_got_offset;
	GPtrArray *shared_patches;
	GHashTable *image_hash;
	GHashTable *method_to_cfg;
	GHashTable *wrapper_to_method;
	GHashTable *token_info_hash;
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
#ifdef USE_BIN_WRITER
	BinSymbol *symbols;
	BinSection *sections;
	BinSection *cur_section;
	BinReloc *relocations;
	GHashTable *labels;
	int num_relocs;
#else
	FILE *fp;
	char *tmpfname;
	int mode; /* emit mode */
	int col_count; /* bytes emitted per .byte line */
#endif
} MonoAotCompile;

#ifdef HAVE_ARRAY_ELEM_INIT
#define MSGSTRFIELD(line) MSGSTRFIELD1(line)
#define MSGSTRFIELD1(line) str##line
static const struct msgstr_t {
#define PATCH_INFO(a,b) char MSGSTRFIELD(__LINE__) [sizeof (b)];
#include "patch-info.h"
#undef PATCH_INFO
} opstr = {
#define PATCH_INFO(a,b) b,
#include "patch-info.h"
#undef PATCH_INFO
};
static const gint16 opidx [] = {
#define PATCH_INFO(a,b) [MONO_PATCH_INFO_ ## a] = offsetof (struct msgstr_t, MSGSTRFIELD(__LINE__)),
#include "patch-info.h"
#undef PATCH_INFO
};

static const char*
get_patch_name (int info)
{
	return (const char*)&opstr + opidx [info];
}

#else
#define PATCH_INFO(a,b) b,
static const char* const
patch_types [MONO_PATCH_INFO_NUM + 1] = {
#include "patch-info.h"
	NULL
};

static const char*
get_patch_name (int info)
{
	return patch_types [info];
}

#endif

static gboolean 
is_got_patch (MonoJumpInfoType patch_type)
{
	return TRUE;
}

static G_GNUC_UNUSED int
ilog2(register int value)
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
};

struct _BinSymbol {
	BinSymbol *next;
	char *name;
	BinSection *section;
	int offset;
	gboolean is_function;
	gboolean is_global;
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
};

static void
emit_start (MonoAotCompile *acfg)
{
	acfg->labels = g_hash_table_new (g_str_hash, g_str_equal);
}

static void
emit_section_change (MonoAotCompile *acfg, const char *section_name, int subsection_index)
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
emit_global (MonoAotCompile *acfg, const char *name, gboolean func)
{
	BinSymbol *symbol = g_new0 (BinSymbol, 1);
	symbol->name = g_strdup (name);
	symbol->is_function = func;
	symbol->is_global = TRUE;
	symbol->section = acfg->cur_section;
	/* FIXME: we align after this call... */
	symbol->offset = symbol->section->cur_offset;
	symbol->next = acfg->symbols;
	acfg->symbols = symbol;
}

static void
emit_label (MonoAotCompile *acfg, const char *name)
{
	BinLabel *label = g_new0 (BinLabel, 1);
	label->name = g_strdup (name);
	label->section = acfg->cur_section;
	label->offset = acfg->cur_section->cur_offset;
	g_hash_table_insert (acfg->labels, label->name, label);
}

static void
emit_ensure_buffer (BinSection *section, int size)
{
	int new_offset = section->cur_offset + size;
	if (new_offset >= section->data_len) {
		int new_size = section->data_len? section->data_len * 2: 256;
		guint8 *data;
		while (new_size <= new_offset)
			new_size *= 2;
		data = g_malloc0 (new_size);
		memcpy (data, section->data, section->data_len);
		g_free (section->data);
		section->data = data;
		section->data_len = new_size;
	}
}

static void
emit_bytes (MonoAotCompile *acfg, const guint8* buf, int size)
{
	emit_ensure_buffer (acfg->cur_section, size);
	memcpy (acfg->cur_section->data + acfg->cur_section->cur_offset, buf, size);
	acfg->cur_section->cur_offset += size;
}

static void
emit_string (MonoAotCompile *acfg, const char *value)
{
	int size = strlen (value) + 1;
	emit_bytes (acfg, (const guint8*)value, size);
}

static void
emit_line (MonoAotCompile *acfg)
{
	/* Nothing to do in binary writer */
}

static void
emit_string_symbol (MonoAotCompile *acfg, const char *name, const char *value)
{
	emit_section_change (acfg, ".text", 1);
	emit_global (acfg, name, FALSE);
	emit_label (acfg, name);
	emit_string (acfg, value);
}

static void 
emit_alignment (MonoAotCompile *acfg, int size)
{
	int offset = acfg->cur_section->cur_offset;
	int add;
	offset += (size - 1);
	offset &= ~(size - 1);
	add = offset - acfg->cur_section->cur_offset;
	if (add) {
		emit_ensure_buffer (acfg->cur_section, add);
		acfg->cur_section->cur_offset += add;
	}
}

static void
emit_pointer (MonoAotCompile *acfg, const char *target)
{
	BinReloc *reloc;
	emit_alignment (acfg, sizeof (gpointer));
	reloc = g_new0 (BinReloc, 1);
	reloc->val1 = g_strdup (target);
	reloc->section = acfg->cur_section;
	reloc->section_offset = acfg->cur_section->cur_offset;
	reloc->next = acfg->relocations;
	acfg->relocations = reloc;
	if (strcmp (reloc->section->name, ".data") == 0) {
		acfg->num_relocs++;
		g_print ("reloc: %s at %d\n", target, acfg->cur_section->cur_offset);
	}
	acfg->cur_section->cur_offset += sizeof (gpointer);
}

static void
emit_int16 (MonoAotCompile *acfg, int value)
{
	guint8 *data;
	emit_ensure_buffer (acfg->cur_section, 2);
	data = acfg->cur_section->data + acfg->cur_section->cur_offset;
	acfg->cur_section->cur_offset += 2;
	/* FIXME: little endian */
	data [0] = value;
	data [1] = value >> 8;
}

static void
emit_int32 (MonoAotCompile *acfg, int value)
{
	guint8 *data;
	emit_ensure_buffer (acfg->cur_section, 4);
	data = acfg->cur_section->data + acfg->cur_section->cur_offset;
	acfg->cur_section->cur_offset += 4;
	/* FIXME: little endian */
	data [0] = value;
	data [1] = value >> 8;
	data [2] = value >> 16;
	data [3] = value >> 24;
}

static void
emit_symbol_diff (MonoAotCompile *acfg, const char *end, const char* start, int offset)
{
	BinReloc *reloc;
	reloc = g_new0 (BinReloc, 1);
	reloc->val1 = g_strdup (end);
	if (strcmp (start, ".") == 0) {
		reloc->val2_section = acfg->cur_section;
		reloc->val2_offset = acfg->cur_section->cur_offset;
	} else {
		reloc->val2 = g_strdup (start);
	}
	reloc->offset = offset;
	reloc->section = acfg->cur_section;
	reloc->section_offset = acfg->cur_section->cur_offset;
	reloc->next = acfg->relocations;
	acfg->relocations = reloc;
	acfg->cur_section->cur_offset += 4;
	/*if (strcmp (reloc->section->name, ".data") == 0) {
		acfg->num_relocs++;
		g_print ("reloc: %s - %s + %d at %d\n", end, start, offset, acfg->cur_section->cur_offset - 4);
	}*/
}

static void
emit_zero_bytes (MonoAotCompile *acfg, int num)
{
	emit_ensure_buffer (acfg->cur_section, num);
	acfg->cur_section->cur_offset += num;
}

#ifdef USE_ELF_WRITER
enum {
	SYM_LOCAL = 0 << 4,
	SYM_GLOBAL = 1 << 4,
	SYM_OBJECT = 1,
	SYM_FUNC = 2,
	SYM_SECTION = 3
};

enum {
	SECT_NULL,
	SECT_HASH,
	SECT_DYNSYM,
	SECT_DYNSTR,
	SECT_REL_DYN,
	SECT_TEXT,
	SECT_DYNAMIC,
	SECT_GOT_PLT,
	SECT_DATA,
	SECT_BSS,
	SECT_SHSTRTAB,
	SECT_SYMTAB,
	SECT_STRTAB,
	SECT_NUM
};

enum {
	DYN_HASH = 4,
	DYN_STRTAB = 5,
	DYN_SYMTAB = 6,
	DYN_STRSZ = 10,
	DYN_SYMENT = 11,
	DYN_REL = 17,
	DYN_RELSZ = 18,
	DYN_RELENT = 19,
	DYN_RELCOUNT = 0x6ffffffa
};

static const char* section_names [] = {
	"",
	".hash",
	".dynsym",
	".dynstr",
	".rel.dyn",
	".text",
	".dynamic",
	".got.plt",
	".data",
	".bss",
	".shstrtab",
	".symtab",
	".strtab"
};

static const guint8 section_type [] = {
	0, 5, 11, 3, 9, 1,
	6, 1, 1, 8, 3, 2, 3
};

static const guint8 section_link [] = {
	0, 2, 3, 0, 2, 0, 3, 0, 0, 0, 0, 12, 0
};

static const guint8 section_esize [] = {
	0, 4, 16, 0, 8, 0, 8, 4, 0, 0, 0, 16, 0
};

static const guint8 section_flags [] = {
	0, 2, 2, 2, 2, 6, 3, 3, 3, 3, 0, 0, 0
};

static const guint16 section_align [] = {
	0, 4, 4, 1, 4, 4096, 4, 4, 8, 8, 1, 4, 1
};

struct ElfHeader {
	guint8  e_ident [16];
	guint16 e_type;
	guint16 e_machine;
	guint32 e_version;
	gsize   e_entry;
	gsize   e_phoff;
	gsize   e_shoff;
	guint32 e_flags;
	guint16 e_ehsize;
	guint16 e_phentsize;
	guint16 e_phnum;
	guint16 e_shentsize;
	guint16 e_shnum;
	guint16 e_shstrndx;
};

struct ElfSectHeader {
	guint32 sh_name;
	guint32 sh_type;
	gsize   sh_flags;
	gsize   sh_addr;
	gsize   sh_offset;
	gsize   sh_size;
	guint32 sh_link;
	guint32 sh_info;
	gsize   sh_addralign;
	gsize   sh_entsize;
};

#if SIZEOF_VOID_P == 4

struct ElfProgHeader {
	guint32 p_type;
	guint32 p_offset;
	guint32 p_vaddr;
	guint32 p_paddr;
	guint32 p_filesz;
	guint32 p_memsz;
	guint32 p_flags;
	guint32 p_align;
};

typedef struct {
	guint32 st_name;
	guint32 st_value;
	guint32 st_size;
	guint8  st_info;
	guint8  st_other;
	guint16 st_shndx;
} ElfSymbol;

typedef struct {
	guint32 addr;
	guint32 value;
} ElfReloc;

typedef struct {
	guint32 d_tag;
	guint32 d_val;
} ElfDynamic;

#else

struct ElfProgHeader {
	guint32 p_type;
	guint32 p_flags;
	guint64 p_offset;
	guint64 p_vaddr;
	guint64 p_paddr;
	guint64 p_filesz;
	guint64 p_memsz;
	guint64 p_align;
};

typedef struct {
	guint32 st_name;
	guint8  st_info;
	guint8  st_other;
	guint16 st_shndx;
	guint64 st_value;
	guint64 st_size;
} ElfSymbol;

typedef struct {
	guint64 addr;
	guint64 value;
} ElfReloc;

typedef struct {
	guint64 addr;
	guint64 value;
	guint64 addend;
} ElfRelocA;

typedef struct {
	guint64 d_tag;
	guint64 d_val;
} ElfDynamic;

#endif

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
append_subsection (MonoAotCompile *acfg, struct ElfSectHeader *sheaders, BinSection *sect, BinSection *add)
{
	int offset = sect->cur_offset;
	/*offset += (sheaders [sect->shidx].sh_addralign - 1);
	offset &= ~(sheaders [sect->shidx].sh_addralign - 1);*/
	offset += (8 - 1);
	offset &= ~(8 - 1);
	emit_ensure_buffer (sect, offset);
	g_print ("section %s aligned to %d from %d\n", sect->name, offset, sect->cur_offset);
	sect->cur_offset = offset;

	emit_ensure_buffer (sect, add->cur_offset);
	memcpy (sect->data + sect->cur_offset, add->data, add->cur_offset);
	add->parent = sect;
	sect->cur_offset += add->cur_offset;
	add->cur_offset = offset; /* it becomes the offset in the parent section */
	g_print ("subsection %d of %s added at offset %d (align: %d)\n", add->subsection, sect->name, add->cur_offset, sheaders [sect->shidx].sh_addralign);
	add->data = NULL;
	add->data_len = 0;
}

/* merge the subsections */
static int
collect_sections (MonoAotCompile *acfg, struct ElfSectHeader *sheaders, BinSection **out, int num)
{
	int i, j, maxs, num_sections;
	BinSection *sect;

	num_sections = 0;
	maxs = 0;
	for (sect = acfg->sections; sect; sect = sect->next) {
		if (sect->subsection == 0) {
			out [num_sections++] = sect;
			g_assert (num_sections < num);
			if (strcmp (sect->name, ".text") == 0) {
				sect->shidx = SECT_TEXT;
			} else if (strcmp (sect->name, ".data") == 0) {
				sect->shidx = SECT_DATA;
			} else if (strcmp (sect->name, ".bss") == 0) {
				sect->shidx = SECT_BSS;
			}
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
build_hash (MonoAotCompile *acfg, int num_sections, ElfStrTable *dynstr)
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
get_label_addr (MonoAotCompile *acfg, const char *name)
{
	int offset;
	BinLabel *lab;
	BinSection *section;
	gsize value;

	lab = g_hash_table_lookup (acfg->labels, name);
	section = lab->section;
	offset = lab->offset;
	if (section->parent) {
		value = section->parent->file_offset + section->cur_offset + offset;
	} else {
		value = section->file_offset + offset;
	}
	return value;
}

static ElfSymbol*
collect_syms (MonoAotCompile *acfg, int *hash, ElfStrTable *strtab, struct ElfSectHeader *sheaders, int *num_syms)
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
	else
		symbols = g_new0 (ElfSymbol, *num_syms + SECT_NUM + 10); /* FIXME */

	/* the first symbol is undef, all zeroes */
	i = 1;
	if (sheaders) {
		int j;
		for (j = 1; j < SECT_NUM; ++j) {
			symbols [i].st_info = SYM_LOCAL | SYM_SECTION;
			symbols [i].st_shndx = j;
			symbols [i].st_value = sheaders [j].sh_addr;
			++i;
		}
	} else {
		for (section = acfg->sections; section; section = section->next) {
			if (section->parent)
				continue;
			symbols [i].st_info = SYM_LOCAL | SYM_SECTION;
			if (strcmp (section->name, ".text") == 0) {
				symbols [i].st_shndx = SECT_TEXT;
				section->shidx = SECT_TEXT;
				section->file_offset = 4096;
				symbols [i].st_value = section->file_offset;
			} else if (strcmp (section->name, ".data") == 0) {
				symbols [i].st_shndx = SECT_DATA;
				section->shidx = SECT_DATA;
				section->file_offset = 4096 + 28; /* FIXME */
				symbols [i].st_value = section->file_offset;
			} else if (strcmp (section->name, ".bss") == 0) {
				symbols [i].st_shndx = SECT_BSS;
				section->shidx = SECT_BSS;
				section->file_offset = 4096 + 28 + 8; /* FIXME */
				symbols [i].st_value = section->file_offset;
			}
			++i;
		}
	}
	for (symbol = acfg->symbols; symbol; symbol = symbol->next) {
		int offset;
		BinLabel *lab;
		if (!symbol->is_global)
			continue;
		symbols [i].st_info = (symbol->is_function? SYM_FUNC : SYM_OBJECT) | SYM_GLOBAL;
		symbols [i].st_name = str_table_add (strtab, symbol->name);
		/*g_print ("sym name %s tabled to %d\n", symbol->name, symbols [i].st_name);*/
		section = symbol->section;
		symbols [i].st_shndx = section->parent? section->parent->shidx: section->shidx;
		lab = g_hash_table_lookup (acfg->labels, symbol->name);
		offset = lab->offset;
		if (section->parent) {
			symbols [i].st_value = section->parent->file_offset + section->cur_offset + offset;
		} else {
			symbols [i].st_value = section->file_offset + offset;
		}
		++i;
	}
	/* add special symbols */
	symbols [i].st_name = str_table_add (strtab, "__bss_start");
	symbols [i].st_shndx = 0xfff1;
	symbols [i].st_info = SYM_GLOBAL;
	++i;
	symbols [i].st_name = str_table_add (strtab, "_edata");
	symbols [i].st_shndx = 0xfff1;
	symbols [i].st_info = SYM_GLOBAL;
	++i;
	symbols [i].st_name = str_table_add (strtab, "_end");
	symbols [i].st_shndx = 0xfff1;
	symbols [i].st_info = SYM_GLOBAL;
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
reloc_symbols (MonoAotCompile *acfg, ElfSymbol *symbols, struct ElfSectHeader *sheaders, ElfStrTable *strtab, gboolean dynamic)
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
		lab = g_hash_table_lookup (acfg->labels, symbol->name);
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

static ElfReloc*
resolve_relocations (MonoAotCompile *acfg)
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
		end_val = get_label_addr (acfg, reloc->val1);
		if (reloc->val2) {
			start_val = get_label_addr (acfg, reloc->val2);
		} else if (reloc->val2_section) {
			start_val = reloc->val2_offset;
			if (reloc->val2_section->parent)
				start_val += reloc->val2_section->parent->file_offset + reloc->val2_section->cur_offset;
			else
				start_val += reloc->val2_section->file_offset;
		} else {
			start_val = 0;
		}
		end_val = end_val - start_val + reloc->offset;
		if (reloc->section->parent) {
			data = reloc->section->parent->data;
			data += reloc->section->cur_offset;
			data += reloc->section_offset;
			vaddr = reloc->section->parent->file_offset;
			vaddr += reloc->section->cur_offset;
			vaddr += reloc->section_offset;
		} else {
			data = reloc->section->data;
			data += reloc->section_offset;
			vaddr = reloc->section->file_offset;
			vaddr += reloc->section_offset;
		}
		/* FIXME: little endian */
		data [0] = end_val;
		data [1] = end_val >> 8;
		data [2] = end_val >> 16;
		data [3] = end_val >> 24;
		if (start_val == 0) {
			rr [i].addr = vaddr;
			rr [i].value = 8; /* FIXME: 386_RELATIVE */
			++i;
			g_assert (i <= acfg->num_relocs);
		}
	}
	return rr;
}

static void
emit_writeout (MonoAotCompile *acfg)
{
	char *outfile_name, *tmp_outfile_name;
	FILE *file;
	struct ElfHeader header;
	struct ElfProgHeader progh [3];
	struct ElfSectHeader secth [SECT_NUM];
	ElfReloc *relocs;
	ElfStrTable str_table = {NULL, NULL};
	ElfStrTable sh_str_table = {NULL, NULL};
	ElfStrTable dyn_str_table = {NULL, NULL};
	BinSection* sections [6];
	BinSection *text_section = NULL, *data_section = NULL, *bss_section = NULL;
	ElfSymbol *dynsym;
	ElfSymbol *symtab;
	ElfDynamic dynamic [14];
	int *hash;
	int i, num_sections, file_offset, virt_offset, size, num_symtab;
	int num_local_syms;

	if (acfg->aot_opts.outfile)
		outfile_name = g_strdup_printf ("%s", acfg->aot_opts.outfile);
	else
		outfile_name = g_strdup_printf ("%s%s", acfg->image->name, SHARED_EXT);

	tmp_outfile_name = g_strdup_printf ("%s.tmp", outfile_name);

	unlink (tmp_outfile_name);
	file = fopen (tmp_outfile_name, "w");
	g_assert (file);

	/* Section headers */
	memset (&secth, 0, sizeof (secth));
	memset (&dynamic, 0, sizeof (dynamic));
	memset (&header, 0, sizeof (header));

	for (i = 1; i < SECT_NUM; ++i) {
		secth [i].sh_name = str_table_add (&sh_str_table, section_names [i]);
		secth [i].sh_type = section_type [i];
		secth [i].sh_link = section_link [i];
		secth [i].sh_addralign = section_align [i];
		secth [i].sh_flags = section_flags [i];
		secth [i].sh_entsize = section_esize [i];
	}
	secth [SECT_DYNSYM].sh_info = 4;
	secth [SECT_SYMTAB].sh_info = 20;

	num_sections = collect_sections (acfg, secth, sections, 6);
	hash = build_hash (acfg, num_sections, &dyn_str_table);
	num_symtab = hash [1]; /* FIXME */
	g_print ("num_sections: %d\n", num_sections);
	g_print ("dynsym: %d, dynstr size: %d\n", hash [1], dyn_str_table.data->len);
	for (i = 0; i < num_sections; ++i) {
		g_print ("section %s, size: %d, %x\n", sections [i]->name, sections [i]->cur_offset, sections [i]->cur_offset);
	}

	/* at this point we know where in the file the first segment sections go */
	dynsym = collect_syms (acfg, hash, &dyn_str_table, NULL, NULL);
	num_local_syms = hash [1];
	symtab = collect_syms (acfg, NULL, &str_table, secth, &num_local_syms);

	for (i = 0; i < num_sections; ++i) {
		if (sections [i]->shidx == SECT_TEXT) {
			text_section = sections [i];
		} else if (sections [i]->shidx == SECT_DATA) {
			data_section = sections [i];
		} else if (sections [i]->shidx == SECT_BSS) {
			bss_section = sections [i];
		}
	}

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
	size = sizeof (ElfReloc) * acfg->num_relocs;
	secth [SECT_REL_DYN].sh_size = size;
	virt_offset = (file_offset += size);
	secth [SECT_REL_DYN].sh_size = size;
	file_offset += 4096-1;
	file_offset &= ~(4096-1);
	virt_offset = file_offset;
	secth [SECT_TEXT].sh_addr = secth [SECT_TEXT].sh_offset = file_offset;
	size = text_section->cur_offset;
	secth [SECT_TEXT].sh_size = size;
	file_offset += size;
	file_offset += 4-1;
	file_offset &= ~(4-1);
	virt_offset = file_offset;
	/* .dynamic, .got.plt, .data, .bss here */
	secth [SECT_DYNAMIC].sh_addr = virt_offset;
	secth [SECT_DYNAMIC].sh_offset = file_offset;
	size = sizeof (dynamic);
	secth [SECT_DYNAMIC].sh_size = size;
	size += 4-1;
	size &= ~(4-1);
	file_offset += size;
	virt_offset += size;
	secth [SECT_GOT_PLT].sh_addr = virt_offset;
	secth [SECT_GOT_PLT].sh_offset = file_offset;
	size = 12;
	secth [SECT_GOT_PLT].sh_size = size;
	size += 8-1;
	size &= ~(8-1);
	file_offset += size;
	virt_offset += size;
	secth [SECT_DATA].sh_addr = virt_offset;
	secth [SECT_DATA].sh_offset = file_offset;
	size = data_section->cur_offset;
	secth [SECT_DATA].sh_size = size;
	size += 8-1;
	size &= ~(8-1);
	file_offset += size;
	virt_offset += size;
	secth [SECT_BSS].sh_addr = virt_offset;
	secth [SECT_BSS].sh_offset = file_offset;
	size = bss_section->cur_offset;
	secth [SECT_BSS].sh_size = size;

	/* virtual doesn't matter anymore */
	secth [SECT_SHSTRTAB].sh_offset = file_offset;
	size = sh_str_table.data->len;
	secth [SECT_SHSTRTAB].sh_size = size;
	size += 4-1;
	size &= ~(4-1);
	file_offset += size;
	secth [SECT_SYMTAB].sh_offset = file_offset;
	size = sizeof (ElfSymbol) * num_local_syms;
	secth [SECT_SYMTAB].sh_size = size;
	file_offset += size;
	secth [SECT_STRTAB].sh_offset = file_offset;
	size = str_table.data->len;
	secth [SECT_STRTAB].sh_size = size;
	file_offset += size;
	file_offset += 4-1;
	file_offset &= ~(4-1);

	text_section->file_offset = secth [SECT_TEXT].sh_offset;
	data_section->file_offset = secth [SECT_DATA].sh_offset;
	bss_section->file_offset = secth [SECT_BSS].sh_offset;

	header.e_ident [0] = 0x7f; header.e_ident [1] = 'E';
	header.e_ident [2] = 'L'; header.e_ident [3] = 'F';
	header.e_ident [4] = SIZEOF_VOID_P == 4? 1: 2;
	header.e_ident [5] = 1; /* FIXME: little endian, bigendian is 2 */
	header.e_ident [6] = 1; /* version */
	header.e_ident [7] = 0; /* FIXME: */
	header.e_ident [8] = 0; /* FIXME: */
	for (i = 9; i < 16; ++i)
		header.e_ident [i] = 0;

	header.e_type = 3; /* shared library */
	header.e_machine = 3; /* FIXME: 386 */
	header.e_version = 1; /* FIXME:  */

	header.e_phoff = sizeof (header);
	header.e_ehsize = sizeof (header);
	header.e_phentsize = sizeof (struct ElfProgHeader);
	header.e_phnum = 3;
	header.e_entry = secth [SECT_TEXT].sh_addr;
	header.e_shstrndx = 10;
	header.e_shentsize = sizeof (struct ElfSectHeader);
	header.e_shnum = SECT_NUM;
	header.e_shoff = file_offset;

	/* dynamic data */
	i = 0;
	dynamic [i].d_tag = DYN_HASH;
	dynamic [i].d_val = secth [SECT_HASH].sh_offset;
	++i;
	dynamic [i].d_tag = DYN_STRTAB;
	dynamic [i].d_val = secth [SECT_DYNSTR].sh_offset;
	++i;
	dynamic [i].d_tag = DYN_SYMTAB;
	dynamic [i].d_val = secth [SECT_DYNSYM].sh_offset;
	++i;
	dynamic [i].d_tag = DYN_STRSZ;
	dynamic [i].d_val = dyn_str_table.data->len;
	++i;
	dynamic [i].d_tag = DYN_SYMENT;
	dynamic [i].d_val = sizeof (ElfSymbol);
	++i;
	dynamic [i].d_tag = DYN_REL;
	dynamic [i].d_val = secth [SECT_REL_DYN].sh_offset;
	++i;
	dynamic [i].d_tag = DYN_RELSZ;
	dynamic [i].d_val = secth [SECT_REL_DYN].sh_size;
	++i;
	dynamic [i].d_tag = DYN_RELENT;
	dynamic [i].d_val = sizeof (ElfReloc);
	++i;
	dynamic [i].d_tag = DYN_RELCOUNT;
	dynamic [i].d_val = acfg->num_relocs;
	++i;

	/* Program header */
	memset (&progh, 0, sizeof (progh));
	progh [0].p_type = 1; /* LOAD */
	progh [0].p_filesz = progh [0].p_memsz = secth [SECT_DYNAMIC].sh_offset;
	progh [0].p_align = 4096;
	progh [0].p_flags = 5;

	progh [1].p_type = 1;
	progh [1].p_offset = secth [SECT_DYNAMIC].sh_offset;
	progh [1].p_vaddr = progh [1].p_paddr = secth [SECT_DYNAMIC].sh_addr;
	progh [1].p_filesz = secth [SECT_BSS].sh_offset  - secth [SECT_DYNAMIC].sh_offset;
	progh [1].p_memsz = secth [SECT_BSS].sh_addr + secth [SECT_BSS].sh_size - secth [SECT_DYNAMIC].sh_addr;
	progh [1].p_align = 4096;
	progh [1].p_flags = 6;

	progh [2].p_type = 2; /* DYNAMIC */
	progh [2].p_offset = secth [SECT_DYNAMIC].sh_offset;
	progh [2].p_vaddr = progh [2].p_paddr = secth [SECT_DYNAMIC].sh_addr;
	progh [2].p_filesz = progh [2].p_memsz = secth [SECT_DYNAMIC].sh_size;
	progh [2].p_align = 4;
	progh [2].p_flags = 6;

	reloc_symbols (acfg, dynsym, secth, &dyn_str_table, TRUE);
	reloc_symbols (acfg, symtab, secth, &str_table, FALSE);
	relocs = resolve_relocations (acfg);

	fwrite (&header, sizeof (header), 1, file);
	fwrite (&progh, sizeof (progh), 1, file);
	fwrite (hash, sizeof (int) * (hash [0] + hash [1] + 2), 1, file);
	fwrite (dynsym, sizeof (ElfSymbol) * hash [1], 1, file);
	fwrite (dyn_str_table.data->str, dyn_str_table.data->len, 1, file);
	/* .rel.dyn */
	fseek (file, secth [SECT_REL_DYN].sh_offset, SEEK_SET);
	fwrite (relocs, sizeof (ElfReloc), acfg->num_relocs, file);

	fseek (file, secth [SECT_TEXT].sh_offset, SEEK_SET);
	/* write .text, .data, .bss sections */
	fwrite (text_section->data, text_section->cur_offset, 1, file);

	/* .dynamic */
	fwrite (dynamic, sizeof (dynamic), 1, file);
	/* .got.plt */
	size = secth [SECT_DYNAMIC].sh_addr;
	fwrite (&size, sizeof (size), 1, file);
	fseek (file, secth [SECT_DATA].sh_offset, SEEK_SET);
	fwrite (data_section->data, data_section->cur_offset, 1, file);

	fseek (file, secth [SECT_SHSTRTAB].sh_offset, SEEK_SET);
	fwrite (sh_str_table.data->str, sh_str_table.data->len, 1, file);
	fseek (file, secth [SECT_SYMTAB].sh_offset, SEEK_SET);
	fwrite (symtab, sizeof (ElfSymbol) * num_local_syms, 1, file);
	fseek (file, secth [SECT_STRTAB].sh_offset, SEEK_SET);
	fwrite (str_table.data->str, str_table.data->len, 1, file);
	/*g_print ("file_offset %d vs %d\n", file_offset, ftell (file));*/
	/*g_assert (file_offset >= ftell (file));*/
	fseek (file, file_offset, SEEK_SET);
	fwrite (&secth, sizeof (secth), 1, file);
	fclose (file);
	rename (tmp_outfile_name, outfile_name);

	g_free (tmp_outfile_name);
	g_free (outfile_name);
}

#endif /* USE_ELF_WRITER */

#else

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
#elif defined(sparc) || defined(__arm__)
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
#if defined(sparc) || defined(__arm__)
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
#if defined(__arm__)
	fprintf (acfg->fp, "\t.align %d\n", ilog2 (size));
#elif defined(__ppc__) && defined(__MACH__)
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

static inline void
emit_int16 (MonoAotCompile *acfg, int value)
{
	if (acfg->mode != EMIT_WORD) {
		acfg->mode = EMIT_WORD;
		acfg->col_count = 0;
	}
	if ((acfg->col_count++ % 8) == 0)
#if defined(__arm__)
		/* FIXME: Use .hword on other archs as well */
		fprintf (acfg->fp, "\n\t.hword ");
#else
		fprintf (acfg->fp, "\n\t.word ");
#endif
	else
		fprintf (acfg->fp, ", ");
	fprintf (acfg->fp, "%d", value);
}

static inline void
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

#endif /* ASM_WRITER */

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

static guint32
find_typespec_for_class (MonoAotCompile *acfg, MonoClass *klass)
{
	int i;
	MonoClass *k = NULL;

	/* FIXME: Search referenced images as well */
	for (i = 0; i < acfg->image->tables [MONO_TABLE_TYPESPEC].rows; ++i) {
		k = mono_class_get_full (acfg->image, MONO_TOKEN_TYPE_SPEC | (i + 1), NULL);
		if (k == klass)
			break;
	}

	if (i < acfg->image->tables [MONO_TABLE_TYPESPEC].rows)
		return MONO_TOKEN_TYPE_SPEC | (i + 1);
	else
		return 0;
}

static void
encode_klass_ref (MonoAotCompile *acfg, MonoClass *klass, guint8 *buf, guint8 **endbuf)
{
	if (klass->generic_class) {
		guint32 token;
		g_assert (klass->type_token);

		/* Find a typespec for a class if possible */
		token = find_typespec_for_class (acfg, klass);
		if (token) {
			encode_value (token, buf, &buf);
			encode_value (get_image_index (acfg, acfg->image), buf, &buf);
		} else {
			MonoClass *gclass = klass->generic_class->container_class;
			MonoGenericInst *inst = klass->generic_class->context.class_inst;
			int i;

			/* Encode it ourselves */
			/* Marker */
			encode_value (MONO_TOKEN_TYPE_SPEC, buf, &buf);
			encode_klass_ref (acfg, gclass, buf, &buf);
			encode_value (inst->type_argc, buf, &buf);
			for (i = 0; i < inst->type_argc; ++i)
				encode_klass_ref (acfg, mono_class_from_mono_type (inst->type_argv [i]), buf, &buf);
		}
	} else if (klass->type_token) {
		g_assert (mono_metadata_token_code (klass->type_token) == MONO_TOKEN_TYPE_DEF);
		encode_value (klass->type_token - MONO_TOKEN_TYPE_DEF, buf, &buf);
		encode_value (get_image_index (acfg, klass->image), buf, &buf);
	} else {
		/* Array class */
		g_assert (klass->rank > 0);
		encode_value (MONO_TOKEN_TYPE_DEF, buf, &buf);
		encode_value (get_image_index (acfg, klass->image), buf, &buf);
		encode_value (klass->rank, buf, &buf);
		encode_klass_ref (acfg, klass->element_class, buf, &buf);
	}
	*endbuf = buf;
}

static void
encode_field_info (MonoAotCompile *cfg, MonoClassField *field, guint8 *buf, guint8 **endbuf)
{
	guint32 token = mono_get_field_token (field);

	encode_klass_ref (cfg, field->parent, buf, &buf);
	g_assert (mono_metadata_token_code (token) == MONO_TOKEN_FIELD_DEF);
	encode_value (token - MONO_TOKEN_FIELD_DEF, buf, &buf);
	*endbuf = buf;
}

#if 0
static guint32
find_methodspec_for_method (MonoAotCompile *acfg, MonoMethod *method)
{
	int i;
	MonoMethod *m = NULL;

	/* FIXME: Search referenced images as well */
	for (i = 0; i < acfg->image->tables [MONO_TABLE_METHODSPEC].rows; ++i) {
		/* Since we don't compile generic methods, the context is empty */
		m = mono_get_method_full (acfg->image, MONO_TOKEN_METHOD_SPEC | (i + 1), NULL, NULL);
		if (m == method)
			break;
	}

	g_assert (i < acfg->image->tables [MONO_TABLE_METHODSPEC].rows);

	return MONO_TOKEN_METHOD_SPEC | (i + 1);
}
#endif

static void
encode_method_ref (MonoAotCompile *acfg, MonoMethod *method, guint8 *buf, guint8 **endbuf)
{
	guint32 image_index = get_image_index (acfg, method->klass->image);
	guint32 token = method->token;
	MonoJumpInfoToken *ji;

	g_assert (image_index < 255);

	if (mono_method_signature (method)->is_inflated) {
		/* 
		 * This is a generic method, find the original token which referenced it and
		 * encode that.
		 */
		/* This doesn't work for some reason */
		/*
		image_index = get_image_index (acfg, acfg->image);
		g_assert (image_index < 255);
		token = find_methodspec_for_method (acfg, method);
		*/
		/* Obtain the token from information recorded by the JIT */
		ji = g_hash_table_lookup (acfg->token_info_hash, method);
		g_assert (ji);
		image_index = get_image_index (acfg, ji->image);
		g_assert (image_index < 255);
		token = ji->token;

		/* Marker */
		encode_value ((255 << 24), buf, &buf);
		encode_value (image_index, buf, &buf);
		encode_value (token, buf, &buf);
	} else {
		g_assert (mono_metadata_token_table (token) == MONO_TABLE_METHOD);
		encode_value ((image_index << 24) | mono_metadata_token_index (token), buf, &buf);
	}
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
	GHashTable *hash = acfg->patch_to_plt_offset;

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
			hash = acfg->patch_to_plt_offset_wrapper [patch_info->data.method->wrapper_type];
			if (!hash) {
				acfg->patch_to_plt_offset_wrapper [patch_info->data.method->wrapper_type] = g_hash_table_new (NULL, NULL);
				hash = acfg->patch_to_plt_offset_wrapper [patch_info->data.method->wrapper_type];
			}
			patch_id = patch_info->data.method;
			break;
		default:
			g_assert_not_reached ();
		}

		if (patch_id) {
			idx = GPOINTER_TO_UINT (g_hash_table_lookup (hash, patch_id));
			if (idx)
				res = idx;
			else
				g_hash_table_insert (hash, patch_id, GUINT_TO_POINTER (acfg->plt_offset));
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

static guint32
get_method_index (MonoAotCompile *acfg, MonoMethod *method)
{
	int method_index = mono_metadata_token_index (method->token);

	if (method_index == 0) {
		MonoMethod *wrapped = g_hash_table_lookup (acfg->wrapper_to_method, method);
		g_assert (wrapped);
		method_index = mono_metadata_token_index (wrapped->token);
	}

	return method_index;
}

static void
emit_method_code (MonoAotCompile *acfg, MonoCompile *cfg)
{
	MonoMethod *method;
	int i, pindex, start_index, method_index;
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

	method_index = get_method_index (acfg, method);

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
	start_index = 0;
	for (i = 0; i < cfg->code_len; i++) {
		patch_info = NULL;
		for (pindex = start_index; pindex < patches->len; ++pindex) {
			patch_info = g_ptr_array_index (patches, pindex);
			if (patch_info->ip.i >= i)
				break;
		}

#ifdef MONO_ARCH_AOT_SUPPORTED
		skip = FALSE;
		if (patch_info && (patch_info->ip.i == i) && (pindex < patches->len)) {
			start_index = pindex;

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
#elif defined(__arm__)
#ifdef USE_BIN_WRITER
					/* FIXME: Can't encode this using the current symbol writer functions */
					g_assert_not_reached ();
#else
					emit_unset_mode (acfg);
					fprintf (acfg->fp, "bl %s\n", direct_call_target);
					i += 4 - 1;
#endif
#endif
				} else {
					got_slot = get_got_offset (acfg, patch_info);

					emit_bytes (acfg, code + i, mono_arch_get_patch_offset (code + i));
#ifdef __x86_64__
					emit_symbol_diff (acfg, "got", ".", (unsigned int) ((got_slot * sizeof (gpointer)) - 4));
#elif defined(__i386__)
					emit_int32 (acfg, (unsigned int) ((got_slot * sizeof (gpointer))));
#elif defined(__arm__)
					emit_symbol_diff (acfg, "got", ".", (unsigned int) ((got_slot * sizeof (gpointer))) - 12);
#else
					g_assert_not_reached ();
#endif
					
					i += mono_arch_get_patch_offset (code + i) + 4 - 1;
				}
				skip = TRUE;
			}
			}
		}
#endif /* MONO_ARCH_AOT_SUPPORTED */

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
	case MONO_PATCH_INFO_ICALL_ADDR:
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
		encode_value (image_index, p, &p);
		encode_value (patch_info->data.token->token - MONO_TOKEN_STRING, p, &p);
		break;
	}
	case MONO_PATCH_INFO_RVA:
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
		encode_klass_ref (acfg, ex_class, p, &p);
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
			encode_klass_ref (acfg, patch_info->data.klass, p, &p);
		}
		break;
	case MONO_PATCH_INFO_CLASS_INIT:
	case MONO_PATCH_INFO_DELEGATE_TRAMPOLINE:
		encode_klass_ref (acfg, patch_info->data.klass, p, &p);
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
	case MONO_PATCH_INFO_WRAPPER:
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
		case MONO_WRAPPER_ISINST: {
			MonoClass *proxy_class = (MonoClass*)mono_marshal_method_from_wrapper (patch_info->data.method);
			encode_klass_ref (acfg, proxy_class, p, &p);
			break;
		}
		case MONO_WRAPPER_LDFLD_REMOTE:
		case MONO_WRAPPER_STFLD_REMOTE:
			break;
		case MONO_WRAPPER_ALLOC: {
			int alloc_type = mono_gc_get_managed_allocator_type (patch_info->data.method);
			g_assert (alloc_type != -1);
			encode_value (alloc_type, p, &p);
			break;
		}
		case MONO_WRAPPER_STELEMREF:
			break;
		default:
			g_assert_not_reached ();
		}
		break;
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
	guint32 last_offset, method_index;
	guint8 *p, *buf;
	guint32 first_got_offset;

	method = cfg->method;
	code = cfg->native_code;
	header = mono_method_get_header (method);

	method_index = get_method_index (acfg, method);

	/* Make the labels local */
	symbol = g_strdup_printf (".Lm_%x_p", method_index);

	/* Sort relocations */
	patches = g_ptr_array_new ();
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next)
		g_ptr_array_add (patches, patch_info);
	g_ptr_array_sort (patches, compare_patches);

	first_got_offset = acfg->method_got_offsets [method_index];

	/**********************/
	/* Encode method info */
	/**********************/

	buf_size = (patches->len < 1000) ? 40960 : 40960 + (patches->len * 64);
	p = buf = g_malloc (buf_size);

	if (mono_class_get_cctor (method->klass))
		encode_klass_ref (acfg, method->klass, p, &p);
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

		if ((patch_info->type == MONO_PATCH_INFO_METHOD) ||
			(patch_info->type == MONO_PATCH_INFO_INTERNAL_METHOD) ||
			(patch_info->type == MONO_PATCH_INFO_WRAPPER) ||
			(patch_info->type == MONO_PATCH_INFO_CLASS_INIT)) {
			/* Calls are made through the PLT */
			patch_info->type = MONO_PATCH_INFO_NONE;
			continue;
		}

		n_patches ++;
	}

	if (n_patches)
		g_assert (acfg->has_got_slots [method_index]);

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
				printf ("\t%s", get_patch_name (patch_info->type));
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
	int k, buf_size, method_index;
	guint32 debug_info_size;
	guint8 *code;
	char *symbol;
	MonoMethodHeader *header;
	guint8 *p, *buf, *debug_info;

	method = cfg->method;
	code = cfg->native_code;
	header = mono_method_get_header (method);

	method_index = get_method_index (acfg, method);

	/* Make the labels local */
	symbol = g_strdup_printf (".Le_%x_p", method_index);

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
	gboolean no_special_static, cant_encode;

	buf_size = 10240 + (klass->vtable_size * 16);
	p = buf = g_malloc (buf_size);

	g_assert (klass);

	mono_class_init (klass);

	/* 
	 * Emit all the information which is required for creating vtables so
	 * the runtime does not need to create the MonoMethod structures which
	 * take up a lot of space.
	 */

	no_special_static = !mono_class_has_special_static_fields (klass);

	/* Check whenever we have enough info to encode the vtable */
	cant_encode = FALSE;
	for (i = 0; i < klass->vtable_size; ++i) {
		MonoMethod *cm = klass->vtable [i];

		if (cm && mono_method_signature (cm)->is_inflated && !g_hash_table_lookup (acfg->token_info_hash, cm))
			cant_encode = TRUE;
	}

	if (klass->generic_container || cant_encode) {
		encode_value (-1, p, &p);
	} else {
		encode_value (klass->vtable_size, p, &p);
		encode_value ((no_special_static << 7) | (klass->has_static_refs << 6) | (klass->has_references << 5) | ((klass->blittable << 4) | (klass->nested_classes ? 1 : 0) << 3) | (klass->has_cctor << 2) | (klass->has_finalize << 1) | klass->ghcimpl, p, &p);
		if (klass->has_cctor)
			encode_method_ref (acfg, mono_class_get_cctor (klass), p, &p);
		if (klass->has_finalize)
			encode_method_ref (acfg, mono_class_get_finalizer (klass), p, &p);
 
		encode_value (klass->instance_size, p, &p);
		encode_value (mono_class_data_size (klass), p, &p);
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
 *   known. Initially the PLT entries jump to code which transfers control to the
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

#if defined(USE_BIN_WRITER) && defined(__arm__)
	/* FIXME: */
	g_assert_not_reached ();
#endif

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
#elif defined(__arm__)
	/* This is 8 bytes long, init_plt () depends on this */
	emit_unset_mode (acfg);
	fprintf (acfg->fp, "\tldr pc, [pc, #-4]\n");
	/* This is filled up during loading by the AOT loader */
	fprintf (acfg->fp, "\t.word 0\n");
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
#elif defined(__arm__)
		/* 
		 * Emit an indirect call since branch displacements are limited to 24 bits on 
		 * ARM. Put the jump table entries inline since offsets are even smaller on
		 * ARM.
		 * FIXME:
		 * - optimize OP_AOTCONST implementation
		 * - optimize the PLT entries
		 * - optimize SWITCH AOT implementation
		 * - implement IMT support
		 */
		/* This is 8 bytes long, init_plt () depends on this */
		emit_unset_mode (acfg);
		fprintf (acfg->fp, "\tldr pc, [pc, #-4]\n");
		/* This is filled up during loading by the AOT loader */
		fprintf (acfg->fp, "\t.word 0\n");
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
#elif defined(__arm__)
		/* This is 12 bytes long, init_plt () depends on this */
		emit_unset_mode (acfg);
		fprintf (acfg->fp, "\tldr ip, [pc, #0]\n");
		fprintf (acfg->fp, "\tb .Lp_0\n");
		fprintf (acfg->fp, "\t.word %d\n", plt_info_offsets [i]);
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
		} else if (str_begins_with (arg, "metadata-only")) {
			opts->metadata_only = TRUE;
		} else if (str_begins_with (arg, "bind-to-runtime-version")) {
			opts->bind_to_runtime_version = TRUE;
		} else {
			fprintf (stderr, "AOT : Unknown argument '%s'.\n", arg);
			exit (1);
		}
	}
}

static void
add_token_info_hash (gpointer key, gpointer value, gpointer user_data)
{
	MonoMethod *method = (MonoMethod*)key;
	MonoJumpInfoToken *ji = (MonoJumpInfoToken*)value;
	MonoJumpInfoToken *new_ji = g_new0 (MonoJumpInfoToken, 1);
	MonoAotCompile *acfg = user_data;

	new_ji->image = ji->image;
	new_ji->token = ji->token;
	g_hash_table_insert (acfg->token_info_hash, method, new_ji);
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

	if (acfg->aot_opts.metadata_only)
		return;

	method = mono_get_method (acfg->image, token, NULL);

	method_idx = mono_metadata_token_index (method->token);	
		
	/* fixme: maybe we can also precompile wrapper methods */
	if ((method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
		(method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
		(method->flags & METHOD_ATTRIBUTE_ABSTRACT)) {
		//printf ("Skip (impossible): %s\n", mono_method_full_name (method, TRUE));
		return;
	}

	if (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL)
		return;

#if 0
	if (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) {
		/* Compile the wrapper instead */
		MonoMethod *wrapper = mono_marshal_get_native_wrapper (method, check_for_pending_exc);
		g_hash_table_insert (acfg->wrapper_to_method, wrapper, method);
		method = wrapper;
	}
#endif

	acfg->stats.mcount++;

	if (method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED) {
		/* 
		 * FIXME: Enabling this causes virtual-sync.exe to fail, since the trampoline
		 * code can't determine that it needs to insert a sync wrapper in the AOT case.
		 */
		return;
	}

	//acfg->opts &= ~MONO_OPT_GSHARED;

	if (!(acfg->opts & MONO_OPT_GSHARED)) {
		if (method->is_generic || method->klass->generic_container) {
			acfg->stats.genericcount ++;
			return;
		}
	}

	/*
	 * Since these methods are the only ones which are compiled with
	 * AOT support, and they are not used by runtime startup/shutdown code,
	 * the runtime will not see AOT methods during AOT compilation,so it
	 * does not need to support them by creating a fake GOT etc.
	 */
	cfg = mini_method_compile (method, acfg->opts, mono_get_root_domain (), FALSE, TRUE, 0);
	if (cfg->exception_type == MONO_EXCEPTION_GENERIC_SHARING_FAILED) {
		//printf ("F: %s\n", mono_method_full_name (method, TRUE));
		acfg->stats.genericcount ++;
		return;
	}
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

	/* Collect method->token associations from the cfg */
	g_hash_table_foreach (cfg->token_info_hash, add_token_info_hash, acfg);

	skip = FALSE;
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_ABS:
			/* unable to handle this */
			//printf ("Skip (abs addr):   %s %d\n", mono_method_full_name (method, TRUE), patch_info->type);
			skip = TRUE;	
			break;
		default:
			break;
		}
	}

	if (skip) {
		acfg->stats.abscount++;
		mono_destroy_compile (cfg);
		return;
	}

	skip = FALSE;
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		if (patch_info->type == MONO_PATCH_INFO_METHOD_JUMP) {
			/* 
			 * FIXME: We can't handle this because mono_jit_compile_method_inner will try
			 * to patch the AOT code when the target of the jump is compiled.
			 */
			skip = TRUE;
			break;
		}
	}

	if (skip) {
		acfg->stats.ocount++;
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
			case MONO_WRAPPER_ALLOC:
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
			if (patch_info->data.method->is_inflated && !g_hash_table_lookup (acfg->token_info_hash, patch_info->data.method))
				/* FIXME: Can't encode these */
				skip = TRUE;
			break;
		case MONO_PATCH_INFO_VTABLE:
		case MONO_PATCH_INFO_CLASS_INIT:
		case MONO_PATCH_INFO_DELEGATE_TRAMPOLINE:
		case MONO_PATCH_INFO_CLASS:
		case MONO_PATCH_INFO_IID:
		case MONO_PATCH_INFO_ADJUSTED_IID:
			if (!patch_info->data.klass->type_token)
				if (!patch_info->data.klass->element_class->type_token && !(patch_info->data.klass->element_class->rank && patch_info->data.klass->element_class->element_class->type_token))
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
	GList *unordered;

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
			fclose (infile);
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
		fclose (infile);
	}

	/* Add missing methods */
	unordered = NULL;
	for (i = 0; i < acfg->image->tables [MONO_TABLE_METHOD].rows; ++i) {
		if (!g_list_find (acfg->method_order, GUINT_TO_POINTER (i)))
			unordered = g_list_prepend (unordered, GUINT_TO_POINTER (i));
	}
	unordered = g_list_reverse (unordered);
	if (acfg->method_order)
		g_list_last (acfg->method_order)->next = unordered;
	else
		acfg->method_order = unordered;
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
				case MONO_PATCH_INFO_RVA:
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

	/* To reduce size of generated assembly */
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

typedef struct ClassNameTableEntry {
	guint32 token, index;
	struct ClassNameTableEntry *next;
} ClassNameTableEntry;

static void
emit_class_name_table (MonoAotCompile *acfg)
{
	int i, table_size;
	guint32 token, hash;
	MonoClass *klass;
	GPtrArray *table;
	char *full_name;
	char *symbol;
	ClassNameTableEntry *entry, *new_entry;

	/*
	 * Construct a chained hash table for mapping class names to typedef tokens.
	 */
	table_size = g_spaced_primes_closest ((int)(acfg->image->tables [MONO_TABLE_TYPEDEF].rows * 1.5));
	table = g_ptr_array_sized_new (table_size);
	for (i = 0; i < table_size; ++i)
		g_ptr_array_add (table, NULL);
	for (i = 0; i < acfg->image->tables [MONO_TABLE_TYPEDEF].rows; ++i) {
		token = MONO_TOKEN_TYPE_DEF | (i + 1);
		klass = mono_class_get (acfg->image, token);
		full_name = mono_type_get_name_full (mono_class_get_type (klass), MONO_TYPE_NAME_FORMAT_FULL_NAME);
		hash = g_str_hash (full_name) % table_size;
		g_free (full_name);

		/* FIXME: Allocate from the mempool */
		new_entry = g_new0 (ClassNameTableEntry, 1);
		new_entry->token = token;

		entry = g_ptr_array_index (table, hash);
		if (entry == NULL) {
			new_entry->index = hash;
			g_ptr_array_index (table, hash) = new_entry;
		} else {
			while (entry->next)
				entry = entry->next;
			
			entry->next = new_entry;
			new_entry->index = table->len;
			g_ptr_array_add (table, new_entry);
		}
	}

	/* Emit the table */
	symbol = g_strdup_printf ("class_name_table");
	emit_section_change (acfg, ".text", 0);
	emit_global (acfg, symbol, FALSE);
	emit_alignment (acfg, 8);
	emit_label (acfg, symbol);

	/* FIXME: Optimize memory usage */
	g_assert (table_size < 65000);
	emit_int16 (acfg, table_size);
	g_assert (table->len < 65000);
	for (i = 0; i < table->len; ++i) {
		ClassNameTableEntry *entry = g_ptr_array_index (table, i);

		if (entry == NULL) {
			emit_int16 (acfg, 0);
			emit_int16 (acfg, 0);
		} else {
			emit_int16 (acfg, mono_metadata_token_index (entry->token));
			if (entry->next)
				emit_int16 (acfg, entry->next->index);
			else
				emit_int16 (acfg, 0);
		}
	}
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

	if (acfg->aot_opts.bind_to_runtime_version)
		emit_string_symbol (acfg, "mono_runtime_version", FULL_VERSION);
	else
		emit_string_symbol (acfg, "mono_runtime_version", "");
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
	acfg->patch_to_plt_offset_wrapper = g_malloc0 (sizeof (GHashTable*) * 128);
	acfg->patch_to_shared_got_offset = g_hash_table_new (mono_patch_info_hash, mono_patch_info_equal);
	acfg->shared_patches = g_ptr_array_new ();
	acfg->method_to_cfg = g_hash_table_new (NULL, NULL);
	acfg->wrapper_to_method = g_hash_table_new (NULL, NULL);
	acfg->token_info_hash = g_hash_table_new (NULL, NULL);
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
	for (i = 0; i < acfg->nmethods; ++i)
		compile_method (acfg, i);

	alloc_got_slots (acfg);

	emit_code (acfg);

	emit_info (acfg);

	emit_method_order (acfg);

	emit_class_name_table (acfg);

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
	printf ("%d methods are generic (%d%%)\n", acfg->stats.genericcount, acfg->stats.mcount ? (acfg->stats.genericcount * 100) / acfg->stats.mcount : 100);
	printf ("%d methods contain absolute addresses (%d%%)\n", acfg->stats.abscount, acfg->stats.mcount ? (acfg->stats.abscount * 100) / acfg->stats.mcount : 100);
	printf ("%d methods contain wrapper references (%d%%)\n", acfg->stats.wrappercount, acfg->stats.mcount ? (acfg->stats.wrappercount * 100) / acfg->stats.mcount : 100);
	printf ("%d methods contain lmf pointers (%d%%)\n", acfg->stats.lmfcount, acfg->stats.mcount ? (acfg->stats.lmfcount * 100) / acfg->stats.mcount : 100);
	printf ("%d methods have other problems (%d%%)\n", acfg->stats.ocount, acfg->stats.mcount ? (acfg->stats.ocount * 100) / acfg->stats.mcount : 100);
	printf ("Methods without GOT slots: %d (%d%%)\n", acfg->stats.methods_without_got_slots, acfg->stats.mcount ? (acfg->stats.methods_without_got_slots * 100) / acfg->stats.mcount : 100);
	printf ("Direct calls: %d (%d%%)\n", acfg->stats.direct_calls, acfg->stats.all_calls ? (acfg->stats.direct_calls * 100) / acfg->stats.all_calls : 100);

	printf ("GOT slot distribution:\n");
	for (i = 0; i < MONO_PATCH_INFO_NONE; ++i)
		if (acfg->stats.got_slot_types [i])
			printf ("\t%s: %d\n", get_patch_name (i), acfg->stats.got_slot_types [i]);

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
