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
typedef struct _MonoDebugDataTable		MonoDebugDataTable;

typedef struct _MonoSymbolFile			MonoSymbolFile;

typedef struct _MonoDebugHandle			MonoDebugHandle;

typedef struct _MonoDebugLineNumberEntry	MonoDebugLineNumberEntry;

typedef struct _MonoDebugVarInfo		MonoDebugVarInfo;
typedef struct _MonoDebugMethodJitInfo		MonoDebugMethodJitInfo;
typedef struct _MonoDebugMethodAddress		MonoDebugMethodAddress;
typedef struct _MonoDebugMethodAddressList	MonoDebugMethodAddressList;
typedef struct _MonoDebugClassEntry		MonoDebugClassEntry;

typedef struct _MonoDebugMethodInfo		MonoDebugMethodInfo;
typedef struct _MonoDebugSourceLocation		MonoDebugSourceLocation;

typedef struct _MonoDebugList			MonoDebugList;

typedef enum {
	MONO_DEBUG_FORMAT_NONE,
	MONO_DEBUG_FORMAT_MONO,
	MONO_DEBUG_FORMAT_DEBUGGER
} MonoDebugFormat;

/*
 * NOTE:
 * We intentionally do not use GList here since the debugger needs to know about
 * the layout of the fields.
*/
struct _MonoDebugList {
	MonoDebugList *next;
	gconstpointer data;
};

struct _MonoSymbolTable {
	guint64 magic;
	guint32 version;
	guint32 total_size;

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
	guint32 index;
	char *image_file;
	MonoImage *image;
	MonoDebugDataTable *type_table;
	MonoSymbolFile *symfile;
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

struct _MonoDebugMethodAddressList {
	guint32 size;
	guint32 count;
	guint8 data [MONO_ZERO_LEN_ARRAY];
};

struct _MonoDebugSourceLocation {
	gchar *source_file;
	guint32 row, column;
	guint32 il_offset;
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
	MonoType *type;
};

#define MONO_DEBUGGER_VERSION				70
#define MONO_DEBUGGER_MAGIC				0x7aff65af4253d427ULL

extern MonoSymbolTable *mono_symbol_table;
extern MonoDebugFormat mono_debug_format;
extern GHashTable *mono_debug_handles;
extern gint32 mono_debug_debugger_version;
extern gint32 _mono_debug_using_mono_debugger;

void mono_debug_list_add (MonoDebugList **list, gconstpointer data);
void mono_debug_list_remove (MonoDebugList **list, gconstpointer data);

void mono_debug_init (MonoDebugFormat format);
void mono_debug_open_image_from_memory (MonoImage *image, const guint8 *raw_contents, int size);
void mono_debug_cleanup (void);

void mono_debug_close_image (MonoImage *image);

void mono_debug_domain_unload (MonoDomain *domain);
void mono_debug_domain_create (MonoDomain *domain);

gboolean mono_debug_using_mono_debugger (void);

MonoDebugMethodAddress *
mono_debug_add_method (MonoMethod *method, MonoDebugMethodJitInfo *jit, MonoDomain *domain);

MonoDebugMethodInfo *
mono_debug_lookup_method (MonoMethod *method);

MonoDebugMethodAddressList *
mono_debug_lookup_method_addresses (MonoMethod *method);

MonoDebugMethodJitInfo*
mono_debug_find_method (MonoMethod *method, MonoDomain *domain);

void
mono_debug_add_delegate_trampoline (gpointer code, int size);

/*
 * Line number support.
 */

MonoDebugSourceLocation *
mono_debug_lookup_source_location (MonoMethod *method, guint32 address, MonoDomain *domain);

void
mono_debug_free_source_location (MonoDebugSourceLocation *location);

gchar *
mono_debug_print_stack_frame (MonoMethod *method, guint32 native_offset, MonoDomain *domain);

/*
 * Mono Debugger support functions
 *
 * These methods are used by the JIT while running inside the Mono Debugger.
 */

int             mono_debugger_method_has_breakpoint       (MonoMethod *method);
int             mono_debugger_insert_breakpoint           (const gchar *method_name, gboolean include_namespace);
gboolean        mono_debugger_unhandled_exception         (gpointer addr, gpointer stack, MonoObject *exc);
void            mono_debugger_handle_exception            (gpointer addr, gpointer stack, MonoObject *exc);
gboolean        mono_debugger_throw_exception             (gpointer addr, gpointer stack, MonoObject *exc);

#endif /* __MONO_DEBUG_H__ */
