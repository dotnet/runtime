/*
 * This header is only installed for use by the debugger:
 * the structures and the API declared here are not supported.
 */

#ifndef __MONO_DEBUG_H__
#define __MONO_DEBUG_H__

#include <glib.h>
#include <mono/metadata/image.h>
#include <mono/metadata/appdomain.h>

typedef struct _MonoSymbolTable			MonoSymbolTable;

typedef struct _MonoSymbolFile			MonoSymbolFile;
typedef struct _MonoSymbolFilePriv		MonoSymbolFilePriv;

typedef struct _MonoDebugHandle			MonoDebugHandle;
typedef struct _MonoDebugHandlePriv		MonoDebugHandlePriv;

typedef struct _MonoDebugLineNumberEntry	MonoDebugLineNumberEntry;

typedef struct _MonoDebugVarInfo		MonoDebugVarInfo;
typedef struct _MonoDebugMethodJitInfo		MonoDebugMethodJitInfo;
typedef struct _MonoDebugMethodAddress		MonoDebugMethodAddress;
typedef struct _MonoDebugClassEntry		MonoDebugClassEntry;

typedef struct _MonoDebuggerMetadataInfo	MonoDebuggerMetadataInfo;

typedef enum {
	MONO_DEBUG_FORMAT_NONE,
	MONO_DEBUG_FORMAT_MONO,
	MONO_DEBUG_FORMAT_DEBUGGER
} MonoDebugFormat;

typedef enum {
	MONO_DEBUGGER_TYPE_KIND_UNKNOWN = 1,
	MONO_DEBUGGER_TYPE_KIND_FUNDAMENTAL,
	MONO_DEBUGGER_TYPE_KIND_STRING,
	MONO_DEBUGGER_TYPE_KIND_SZARRAY,
	MONO_DEBUGGER_TYPE_KIND_ARRAY,
	MONO_DEBUGGER_TYPE_KIND_POINTER,
	MONO_DEBUGGER_TYPE_KIND_ENUM,
	MONO_DEBUGGER_TYPE_KIND_OBJECT,
	MONO_DEBUGGER_TYPE_KIND_STRUCT,
	MONO_DEBUGGER_TYPE_KIND_CLASS,
	MONO_DEBUGGER_TYPE_KIND_CLASS_INFO,
	MONO_DEBUGGER_TYPE_KIND_REFERENCE
} MonoDebuggerTypeKind;

struct _MonoSymbolTable {
	guint64 magic;
	guint32 version;
	guint32 total_size;

	/*
	 * Corlib and metadata info.
	 */
	MonoDebugHandle *corlib;
	MonoDebuggerMetadataInfo *metadata_info;

	/*
	 * The symbol files.
	 */
	guint32 num_symbol_files;
	MonoDebugHandle **symbol_files;

	/*
	 * Data table.
	 * This is intentionally not a GPtrArray to make it more easy to
	 * read for the debugger.  The `data_tables' field contains
	 * `num_data_tables' pointers to continuous memory areas of
	 * `data_table_chunk_size' bytes each.
	 *
	 * The data table is basically a big continuous blob, but we need
	 * to split it up into pieces because we don't know the total size
	 * in advance and using g_realloc() doesn't work because that may
	 * reallocate the block to a different address.
	 */
	guint32 num_data_tables;
	guint32 data_table_chunk_size;
	gpointer *data_tables;
	/*
	 * Current data table.
	 * The `current_data_table' points to a blob of `data_table_chunk_size'
	 * bytes.
	 */
	gpointer current_data_table;
	/*
	 * This is the total size of the data table, including all the tables
	 * in the `data_tables' vector.
	 */
	guint32 data_table_size;
	/*
	 * These are global offsets - the `current_data_table' starts at global
	 * offset `data_table_start' and we've already allocated stuff in it
	 * until offset `data_table_offset'.
	 */
	guint32 data_table_offset;
	guint32 data_table_start;
};

typedef enum {
	MONO_DEBUG_DATA_ITEM_UNKNOWN		= 0,
	MONO_DEBUG_DATA_ITEM_METHOD,
	MONO_DEBUG_DATA_ITEM_CLASS
} MonoDebugDataItemType;

struct _MonoDebugHandle {
	guint32 index;
	const char *image_file;
	MonoImage *image;
	MonoSymbolFile *symfile;
	MonoDebugHandlePriv *_priv;
};

struct _MonoDebugMethodJitInfo {
	const guint8 *code_start;
	guint32 code_size;
	guint32 prologue_end;
	guint32 epilogue_begin;
	const guint8 *wrapper_addr;
	guint32 num_line_numbers;
	MonoDebugLineNumberEntry *line_numbers;
	guint32 num_params;
	MonoDebugVarInfo *this_var;
	MonoDebugVarInfo *params;
	guint32 num_locals;
	MonoDebugVarInfo *locals;
};

struct _MonoDebugMethodAddress {
	guint32 size;
	guint32 symfile_id;
	guint32 domain_id;
	guint32 method_id;
	const guint8 *code_start;
	guint32 code_size;
	const guint8 *wrapper_addr;
	guint8 data [MONO_ZERO_LEN_ARRAY];
};

struct _MonoDebugClassEntry {
	guint32 size;
	guint32 symfile_id;
	guint8 data [MONO_ZERO_LEN_ARRAY];
};

/*
 * These bits of the MonoDebugLocalInfo's "index" field are flags specifying
 * where the variable is actually stored.
 *
 * See relocate_variable() in debug-symfile.c for more info.
 */
#define MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS		0xf0000000

/* The variable is in register "index". */
#define MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER		0

/* The variable is at offset "offset" from register "index". */
#define MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET		0x10000000

/* The variable is in the two registers "offset" and "index". */
#define MONO_DEBUG_VAR_ADDRESS_MODE_TWO_REGISTERS	0x20000000

struct _MonoDebugVarInfo {
	guint32 index;
	guint32 offset;
	guint32 size;
	guint32 begin_scope;
	guint32 end_scope;
};

#define MONO_DEBUGGER_VERSION				48
#define MONO_DEBUGGER_MAGIC				0x7aff65af4253d427ULL

extern MonoSymbolTable *mono_symbol_table;
extern MonoDebugFormat mono_debug_format;
extern GHashTable *mono_debug_handles;

void mono_debug_init (MonoDebugFormat format);
void mono_debug_init_1 (MonoDomain *domain);
void mono_debug_init_2 (MonoAssembly *assembly);
void mono_debug_cleanup (void);

MonoDebugMethodAddress *mono_debug_add_method (MonoMethod *method, MonoDebugMethodJitInfo *jit,
					       MonoDomain *domain);
MonoDebugMethodJitInfo *mono_debug_read_method (MonoDebugMethodAddress *address);
gchar *mono_debug_source_location_from_address (MonoMethod *method, guint32 address,
						guint32 *line_number, MonoDomain *domain);
gchar *mono_debug_source_location_from_il_offset (MonoMethod *method, guint32 offset,
						  guint32 *line_number);
gint32 mono_debug_il_offset_from_address (MonoMethod *method, gint32 address, MonoDomain *domain);
gint32 mono_debug_address_from_il_offset (MonoMethod *method, gint32 il_offset, MonoDomain *domain);

#endif /* __MONO_DEBUG_H__ */
