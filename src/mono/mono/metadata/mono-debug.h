/**
 * \file
 * This header is only installed for use by the debugger:
 * the structures and the API declared here are not supported.
 */

#ifndef __MONO_DEBUG_H__
#define __MONO_DEBUG_H__

#include <mono/utils/mono-publib.h>
#include <mono/metadata/image.h>
#include <mono/metadata/appdomain.h>

MONO_BEGIN_DECLS

typedef struct _MonoSymbolTable			MonoSymbolTable;
typedef struct _MonoDebugDataTable		MonoDebugDataTable;

typedef struct _MonoSymbolFile			MonoSymbolFile;
typedef struct _MonoPPDBFile			MonoPPDBFile;

typedef struct _MonoDebugHandle			MonoDebugHandle;

typedef struct _MonoDebugLineNumberEntry	MonoDebugLineNumberEntry;

typedef struct _MonoDebugVarInfo		MonoDebugVarInfo;
typedef struct _MonoDebugMethodJitInfo		MonoDebugMethodJitInfo;
typedef struct _MonoDebugMethodAddress		MonoDebugMethodAddress;
typedef struct _MonoDebugMethodAddressList	MonoDebugMethodAddressList;
typedef struct _MonoDebugClassEntry		MonoDebugClassEntry;

typedef struct _MonoDebugMethodInfo		MonoDebugMethodInfo;
typedef struct _MonoDebugLocalsInfo		MonoDebugLocalsInfo;
typedef struct _MonoDebugMethodAsyncInfo	MonoDebugMethodAsyncInfo;
typedef struct _MonoDebugSourceLocation		MonoDebugSourceLocation;

typedef struct _MonoDebugList			MonoDebugList;

typedef enum {
	MONO_DEBUG_FORMAT_NONE,
	MONO_DEBUG_FORMAT_MONO,
	/* Deprecated, the mdb debugger is not longer supported. */
	MONO_DEBUG_FORMAT_DEBUGGER
} MonoDebugFormat;

/*
 * NOTE:
 * We intentionally do not use GList here since the debugger needs to know about
 * the layout of the fields.
*/
struct _MonoDebugList {
	MonoDebugList *next;
	const void* data;
};

struct _MonoSymbolTable {
	uint64_t magic;
	uint32_t version;
	uint32_t total_size;

	/*
	 * Corlib and metadata info.
	 */
	MonoDebugHandle *corlib;
	MonoDebugDataTable *global_data_table;
	MonoDebugList *data_tables;

	/*
	 * The symbol files.
	 */
	MonoDebugList *symbol_files;
};

struct _MonoDebugHandle {
	uint32_t index;
	char *image_file;
	MonoImage *image;
	MonoDebugDataTable *type_table;
	MonoSymbolFile *symfile;
	MonoPPDBFile *ppdb;
};

struct _MonoDebugMethodJitInfo {
	const mono_byte *code_start;
	uint32_t code_size;
	uint32_t prologue_end;
	uint32_t epilogue_begin;
	const mono_byte *wrapper_addr;
	uint32_t num_line_numbers;
	MonoDebugLineNumberEntry *line_numbers;
	uint32_t has_var_info;
	uint32_t num_params;
	MonoDebugVarInfo *this_var;
	MonoDebugVarInfo *params;
	uint32_t num_locals;
	MonoDebugVarInfo *locals;
	MonoDebugVarInfo *gsharedvt_info_var;
	MonoDebugVarInfo *gsharedvt_locals_var;
};

struct _MonoDebugMethodAddressList {
	uint32_t size;
	uint32_t count;
	mono_byte data [MONO_ZERO_LEN_ARRAY];
};

struct _MonoDebugSourceLocation {
	char *source_file;
	uint32_t row, column;
	uint32_t il_offset;
};

MONO_API mono_bool mono_debug_enabled (void);

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

/* The variable is dead. */
#define MONO_DEBUG_VAR_ADDRESS_MODE_DEAD		0x30000000

/* Same as REGOFFSET, but do an indirection */
#define MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET_INDIR		0x40000000

/* gsharedvt local */
#define MONO_DEBUG_VAR_ADDRESS_MODE_GSHAREDVT_LOCAL		0x50000000

/* variable is a vt address */
#define MONO_DEBUG_VAR_ADDRESS_MODE_VTADDR		0x60000000

struct _MonoDebugVarInfo {
	uint32_t index;
	uint32_t offset;
	uint32_t size;
	uint32_t begin_scope;
	uint32_t end_scope;
	MonoType *type;
};

#define MONO_DEBUGGER_MAJOR_VERSION			81
#define MONO_DEBUGGER_MINOR_VERSION			6
#define MONO_DEBUGGER_MAGIC				0x7aff65af4253d427ULL

MONO_API void mono_debug_init (MonoDebugFormat format);
MONO_API void mono_debug_open_image_from_memory (MonoImage *image, const mono_byte *raw_contents, int size);
MONO_API MONO_RT_EXTERNAL_ONLY void mono_debug_cleanup (void);

MONO_API void mono_debug_close_image (MonoImage *image);

MONO_API void mono_debug_domain_unload (MonoDomain *domain);
MONO_API void mono_debug_domain_create (MonoDomain *domain);

MONO_API MonoDebugMethodAddress *
mono_debug_add_method (MonoMethod *method, MonoDebugMethodJitInfo *jit, MonoDomain *domain);

MONO_API void
mono_debug_remove_method (MonoMethod *method, MonoDomain *domain);

MONO_API MonoDebugMethodInfo *
mono_debug_lookup_method (MonoMethod *method);

MONO_API MonoDebugMethodAddressList *
mono_debug_lookup_method_addresses (MonoMethod *method);

MONO_API MonoDebugMethodJitInfo*
mono_debug_find_method (MonoMethod *method, MonoDomain *domain);

MONO_API MonoDebugHandle *
mono_debug_get_handle (MonoImage *image);

MONO_API void
mono_debug_free_method_jit_info (MonoDebugMethodJitInfo *jit);


MONO_API void
mono_debug_add_delegate_trampoline (void* code, int size);

MONO_API MonoDebugLocalsInfo*
mono_debug_lookup_locals (MonoMethod *method, mono_bool ignore_pdb);

MONO_API MonoDebugMethodAsyncInfo*
mono_debug_lookup_method_async_debug_info (MonoMethod *method);

MONO_API
MonoDebugSourceLocation *
mono_debug_method_lookup_location (MonoDebugMethodInfo *minfo, int il_offset);

/*
 * Line number support.
 */

MONO_API MonoDebugSourceLocation *
mono_debug_lookup_source_location (MonoMethod *method, uint32_t address, MonoDomain *domain);

MONO_API int32_t
mono_debug_il_offset_from_address (MonoMethod *method, MonoDomain *domain, uint32_t native_offset);

MONO_API void
mono_debug_free_source_location (MonoDebugSourceLocation *location);

MONO_API char *
mono_debug_print_stack_frame (MonoMethod *method, uint32_t native_offset, MonoDomain *domain);

/*
 * Mono Debugger support functions
 *
 * These methods are used by the JIT while running inside the Mono Debugger.
 */

MONO_API int             mono_debugger_method_has_breakpoint       (MonoMethod *method);
MONO_API int             mono_debugger_insert_breakpoint           (const char *method_name, mono_bool include_namespace);

MONO_API void mono_set_is_debugger_attached (mono_bool attached);
MONO_API mono_bool mono_is_debugger_attached (void);

MONO_END_DECLS

#endif /* __MONO_DEBUG_H__ */
