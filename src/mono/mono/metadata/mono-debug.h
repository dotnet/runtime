#ifndef __MONO_DEBUG_H__
#define __MONO_DEBUG_H__

#include <glib.h>
#include <mono/metadata/debug-mono-symfile.h>

typedef struct _MonoDebugHandle			MonoDebugHandle;
typedef struct _MonoDebuggerSymbolFileTable	MonoDebuggerSymbolFileTable;

typedef enum {
	MONO_DEBUG_FORMAT_NONE,
	MONO_DEBUG_FORMAT_MONO,
	MONO_DEBUG_FORMAT_DEBUGGER
} MonoDebugFormat;

struct _MonoDebugHandle {
	MonoImage *image;
	MonoSymbolFile *symfile;
	GHashTable *wrapper_info;
};

struct _MonoDebuggerSymbolFileTable {
	guint64 magic;
	guint32 version;
	guint32 total_size;
	guint32 count;
	guint32 generation;
	MonoGlobalSymbolFile *global_symfile;
	MonoSymbolFile *symfiles [MONO_ZERO_LEN_ARRAY];
};

/* This is incremented each time the symbol table is modified.
 * The debugger looks at this variable and if it has a higher value than its current
 * copy of the symbol table, it must call debugger_update_symbol_file_table().
 */
extern guint32 mono_debugger_symbol_file_table_generation;
extern guint32 mono_debugger_symbol_file_table_modified;

/* Caution: This variable may be accessed at any time from the debugger;
 *          it is very important not to modify the memory it is pointing to
 *          without previously setting this pointer back to NULL.
 */
extern MonoDebuggerSymbolFileTable *mono_debugger_symbol_file_table;
extern MonoDebugFormat mono_debug_format;

void mono_debug_init (MonoDebugFormat format);
void mono_debug_init_2 (MonoAssembly *assembly);
void mono_debug_cleanup (void);
void mono_debug_lock (void);
void mono_debug_unlock (void);
int  mono_debug_update_symbol_file_table (void);
void mono_debug_add_wrapper (MonoMethod *method, MonoMethod *wrapper_method);
void mono_debug_add_method (MonoMethod *method, MonoDebugMethodJitInfo *jit);
void mono_debug_update (void);
gchar *mono_debug_source_location_from_address (MonoMethod *method, guint32 address,
						guint32 *line_number);
gchar *mono_debug_source_location_from_il_offset (MonoMethod *method, guint32 offset,
						  guint32 *line_number);
gint32 mono_debug_il_offset_from_address (MonoMethod *method, gint32 address);
gint32 mono_debug_address_from_il_offset (MonoMethod *method, gint32 il_offset);

#endif /* __MONO_DEBUG_H__ */
