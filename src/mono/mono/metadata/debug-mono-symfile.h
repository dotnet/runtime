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
typedef struct MonoSymbolFileDynamicTable	MonoSymbolFileDynamicTable;

typedef struct MonoDebugMethodInfo		MonoDebugMethodInfo;
typedef struct MonoDebugMethodJitInfo		MonoDebugMethodJitInfo;
typedef struct MonoDebugVarInfo			MonoDebugVarInfo;
typedef struct MonoDebugLineNumberEntry		MonoDebugLineNumberEntry;
typedef struct MonoDebugRangeInfo		MonoDebugRangeInfo;
typedef struct MonoDebugTypeInfo		MonoDebugTypeInfo;

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
	guint32 local_variable_table_offset;
	guint32 local_variable_table_size;
	guint32 type_count;
	guint32 type_index_table_offset;
	guint32 type_index_table_size;
};

struct MonoSymbolFileMethodEntry {
	guint32 token;
	guint32 start_row;
	guint32 end_row;
	guint32 this_type_index;
	guint32 num_parameters;
	guint32 num_locals;
	guint32 num_line_numbers;
	guint32 type_index_table_offset;
	guint32 local_variable_table_offset;
	guint32 source_file_offset;
	guint32 line_number_table_offset;
};

struct MonoSymbolFileMethodAddress {
	guint32 size;
	const guint8 *start_address;
	const guint8 *end_address;
	const guint8 *method_start_address;
	const guint8 *method_end_address;
	guint32 variable_table_offset;
	guint32 type_table_offset;
	guint32 num_line_numbers;
	guint32 line_number_size;
	MonoDebugLineNumberEntry *line_numbers;
	guint8 data [MONO_ZERO_LEN_ARRAY];
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
	guint32 start_line;
	guint32 end_line;
	MonoSymbolFileLineNumberEntry *il_offsets;
	MonoDebugMethodJitInfo *jit;
	gpointer user_data;
};

struct MonoDebugLineNumberEntry {
	guint32 line;
	guint32 offset;
	guint32 address;
};

struct MonoDebugMethodJitInfo {
	const guint8 *code_start;
	guint32 code_size;
	guint32 prologue_end;
	guint32 epilogue_begin;
	// Array of MonoDebugLineNumberEntry
	GArray *line_numbers;
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
	guint32 size;
	guint32 begin_scope;
	guint32 end_scope;
};

struct MonoDebugRangeInfo {
	const guint8 *start_address;
	const guint8 *end_address;
	guint32 file_offset;
	gpointer dynamic_data;
	guint32 dynamic_size;
};

struct MonoDebugTypeInfo {
	MonoClass *klass;
	guint32 rank;
	guint32 token;
	gpointer type_info;
};

struct MonoSymbolFile {
	guint64 magic;
	guint32 version;
	guint64 dynamic_magic;
	guint32 dynamic_version;
	guint32 is_dynamic;
	char *image_file;
	char *symbol_file;
	/* Pointer to the mmap()ed contents of the file. */
	guint8 *raw_contents;
	guint32 raw_contents_size;
	/* Pointer to the malloced string table. */
	guint8 *string_table;
	guint32 string_table_size;
	/* Pointer to the malloced range table. */
	guint32 locked;
	guint32 generation;
	MonoDebugRangeInfo *range_table;
	guint32 range_entry_size;
	guint32 num_range_entries;
	/* Pointer to the malloced class table. */
	MonoDebugTypeInfo *type_table;
	guint32 type_entry_size;
	guint32 num_type_entries;
	/* Private. */
	MonoSymbolFilePriv *_priv;
};

#define MONO_SYMBOL_FILE_VERSION		26
#define MONO_SYMBOL_FILE_MAGIC			0x45e82623fd7fa614

#define MONO_SYMBOL_FILE_DYNAMIC_VERSION	14
#define MONO_SYMBOL_FILE_DYNAMIC_MAGIC		0x7aff65af4253d427

MonoSymbolFile *
mono_debug_open_mono_symbol_file   (MonoImage                 *image,
				    const char                *filename,
				    gboolean                   emit_warnings);

void
mono_debug_symfile_add_method      (MonoSymbolFile           *symfile,
				    MonoMethod               *method);

void
mono_debug_symfile_add_type        (MonoSymbolFile           *symfile,
				    MonoClass                *klass);

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

MonoReflectionType *
ves_icall_MonoDebugger_GetLocalTypeFromSignature (MonoReflectionAssembly *assembly,
						  MonoArray              *signature);

MonoReflectionType *
ves_icall_MonoDebugger_GetType     (MonoReflectionAssembly   *assembly,
				    guint32                   token);

#endif /* __MONO_SYMFILE_H__ */

