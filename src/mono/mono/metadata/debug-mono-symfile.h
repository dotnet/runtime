#ifndef __MONO_DEBUG_MONO_SYMFILE_H__
#define __MONO_DEBUG_MONO_SYMFILE_H__

#include <glib.h>
#include <mono/metadata/class.h>
#include <mono/metadata/reflection.h>

typedef struct MonoSymbolFile			MonoSymbolFile;
typedef struct MonoSymbolFilePriv		MonoSymbolFilePriv;
typedef struct MonoSymbolFileOffsetTable	MonoSymbolFileOffsetTable;
typedef struct MonoSymbolFileLineNumberEntry	MonoSymbolFileLineNumberEntry;
typedef struct MonoSymbolFileMethodEntry	MonoSymbolFileMethodEntry;
typedef struct MonoSymbolFileMethodAddress	MonoSymbolFileMethodAddress;

typedef struct MonoDebugMethodInfo		MonoDebugMethodInfo;
typedef struct MonoDebugMethodJitInfo		MonoDebugMethodJitInfo;
typedef struct MonoDebugVarInfo			MonoDebugVarInfo;
typedef struct MonoDebugRangeInfo		MonoDebugRangeInfo;

/* Keep in sync with OffsetTable in mcs/class/Mono.CSharp.Debugger/MonoSymbolTable.cs */
struct MonoSymbolFileOffsetTable {
	guint32 total_file_size;
	guint32 source_table_offset;
	guint32 source_table_size;
	guint32 method_count;
	guint32 method_table_offset;
	guint32 method_table_size;
	guint32 line_number_table_offset;
	guint32 line_number_table_size;
	guint32 address_table_size;
};

struct MonoSymbolFileMethodEntry {
	guint32 token;
	guint32 start_row;
	guint32 end_row;
	guint32 num_line_numbers;
	guint32 source_file_offset;
	guint32 line_number_table_offset;
	guint32 address_table_offset;
	guint32 address_table_size;
};

struct MonoSymbolFileMethodAddress {
	guint32 is_valid;
	guint64 start_address;
	guint64 end_address;
	guint32 line_addresses [MONO_ZERO_LEN_ARRAY];
};

struct MonoSymbolFileLineNumberEntry {
	guint32 row;
	guint32 offset;
};

struct MonoDebugMethodInfo {
	MonoMethod *method;
	MonoSymbolFile *symfile;
	guint32 file_offset;
	guint32 num_il_offsets;
	MonoSymbolFileLineNumberEntry *il_offsets;
	MonoDebugMethodJitInfo *jit;
	gpointer user_data;
};

struct MonoDebugMethodJitInfo {
	const guint8 *code_start;
	guint32 code_size;
	guint32 prologue_end;
	guint32 epilogue_begin;
	guint32 *il_addresses;
	guint32 num_params;
	MonoDebugVarInfo *this_var;
	MonoDebugVarInfo *params;
	guint32 num_locals;
	MonoDebugVarInfo *locals;
};

/*
 * These bits of the MonoDebugLocalInfo's "index" field are flags specifying
 * where the variable is actually stored.
 *
 * See relocate_variable() in debug-symfile.c for more info.
 */
#define MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS		0xf0000000

/* If "index" is zero, the variable is at stack offset "offset". */
#define MONO_DEBUG_VAR_ADDRESS_MODE_STACK		0

/* The variable is in the register whose number is contained in bits 0..4 of the
 * "index" field plus an offset of "offset" (which can be zero).
 */
#define MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER		0x10000000

/* The variables in in the two registers whose numbers are contained in bits 0..4
 * and 5..9 of the "index" field plus an offset of "offset" (which can be zero).
 */
#define MONO_DEBUG_VAR_ADDRESS_MODE_TWO_REGISTERS	0x20000000

struct MonoDebugVarInfo {
	guint32 index;
	guint32 offset;
	guint32 begin_scope;
	guint32 end_scope;
};

struct MonoDebugRangeInfo {
	guint64 start_address;
	guint64 end_address;
	guint32 file_offset;
};

struct MonoSymbolFile {
	guint64 magic;
	guint32 version;
	guint32 is_dynamic;
	char *image_file;
	/* Pointer to the mmap()ed contents of the file. */
	guint8 *raw_contents;
	guint32 raw_contents_size;
	/* Pointer to the malloced address table. */
	char *address_table;
	guint32 address_table_size;
	/* Pointer to the malloced range table. */
	MonoDebugRangeInfo *range_table;
	guint32 range_table_size;
	/* Pointer to the malloced string table. */
	guint8 *string_table;
	guint32 string_table_size;
	/* Private. */
	MonoSymbolFilePriv *_priv;
};

#define MONO_SYMBOL_FILE_VERSION		21
#define MONO_SYMBOL_FILE_MAGIC			0x45e82623fd7fa614

MonoSymbolFile *
mono_debug_open_mono_symbol_file   (MonoImage                 *image,
				    const char                *filename,
				    gboolean                   emit_warnings);

void
mono_debug_update_mono_symbol_file (MonoSymbolFile           *symbol_file);

void
mono_debug_close_mono_symbol_file  (MonoSymbolFile           *symfile);

MonoSymbolFile *
mono_debug_create_mono_symbol_file (MonoImage                *image);

gchar *
mono_debug_find_source_location    (MonoSymbolFile           *symfile,
				    MonoMethod               *method,
				    guint32                   offset,
				    guint32                  *line_number);

MonoDebugMethodInfo *
mono_debug_find_method             (MonoSymbolFile           *symfile,
				    MonoMethod               *method);

MonoReflectionMethod *
ves_icall_MonoDebugger_GetMethod   (MonoReflectionAssembly   *assembly,
				    guint32                   token);

#endif /* __MONO_SYMFILE_H__ */

