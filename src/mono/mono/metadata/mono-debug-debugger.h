#ifndef __MONO_DEBUG_DEBUGGER_H__
#define __MONO_DEBUG_DEBUGGER_H__

#include <glib.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/debug-mono-symfile.h>

typedef struct _MonoDebuggerBreakpointInfo	MonoDebuggerBreakpointInfo;
typedef struct _MonoDebuggerSymbolTable		MonoDebuggerSymbolTable;
typedef struct _MonoDebuggerSymbolFile		MonoDebuggerSymbolFile;
typedef struct _MonoDebuggerSymbolFilePriv	MonoDebuggerSymbolFilePriv;
typedef struct _MonoDebuggerRangeInfo		MonoDebuggerRangeInfo;
typedef struct _MonoDebuggerClassInfo		MonoDebuggerClassInfo;
typedef struct _MonoDebuggerIOLayer		MonoDebuggerIOLayer;

typedef enum {
	MONO_DEBUGGER_EVENT_TYPE_ADDED,
	MONO_DEBUGGER_EVENT_METHOD_ADDED,
	MONO_DEBUGGER_EVENT_BREAKPOINT_TRAMPOLINE
} MonoDebuggerEvent;

struct _MonoDebuggerBreakpointInfo {
	guint32 index;
	gboolean use_trampoline;
	MonoMethodDesc *desc;
};

struct _MonoDebuggerSymbolTable {
	guint64 magic;
	guint32 version;
	guint32 total_size;

	/*
	 * The symbol files.
	 */
	guint32 num_symbol_files;
	MonoDebuggerSymbolFile **symbol_files;

	/*
	 * Type table.
	 * This is intentionally not a GPtrArray to make it more easy to
	 * read for the debugger.  The `type_tables' field contains
	 * `num_type_tables' pointers to continuous memory areas of
	 * `type_table_chunk_size' bytes each.
	 *
	 * The type table is basically a big continuous blob, but we need
	 * to split it up into pieces because we don't know the total size
	 * in advance and using g_realloc() doesn't work because that may
	 * reallocate the block to a different address.
	 */
	guint32 num_type_tables;
	guint32 type_table_chunk_size;
	gpointer *type_tables;
	/*
	 * Current type table.
	 * The `current_type_table' points to a blob of `type_table_chunk_size'
	 * bytes.
	 */
	gpointer current_type_table;
	/*
	 * This is the total size of the type table, including all the tables
	 * in the `type_tables' vector.
	 */
	guint32 type_table_size;
	/*
	 * These are global offsets - the `current_type_table' starts at global
	 * offset `type_table_start' and we've already allocated stuff in it
	 * until offset `type_table_offset'.
	 */
	guint32 type_table_offset;
	guint32 type_table_start;
};

struct _MonoDebuggerSymbolFile {
	MonoSymbolFile *symfile;
	const char *image_file;
	/* Pointer to the malloced range table. */
	guint32 locked;
	guint32 generation;
	MonoDebuggerRangeInfo *range_table;
	guint32 range_entry_size;
	guint32 num_range_entries;
	/* Pointer to the malloced class table. */
	MonoDebuggerClassInfo *class_table;
	guint32 class_entry_size;
	guint32 num_class_entries;
	/* Private. */
	MonoDebuggerSymbolFilePriv *_priv;
};

struct _MonoDebuggerRangeInfo {
	const guint8 *start_address;
	const guint8 *end_address;
	guint32 index;
	gpointer dynamic_data;
	guint32 dynamic_size;
};

struct _MonoDebuggerClassInfo {
	MonoClass *klass;
	guint32 rank;
	guint32 token;
	guint32 type_info;
};

extern MonoDebuggerSymbolTable *mono_debugger_symbol_table;

/*
 * Address of the x86 trampoline code.  This is used by the debugger to check
 * whether a method is a trampoline.
 */
extern guint8 *mono_generic_trampoline_code;

/*
 * Address of a special breakpoint trampoline code for the debugger.
 */
extern guint8 *mono_breakpoint_trampoline_code;

#ifndef PLATFORM_WIN32

/*
 * Functions we export to the debugger.
 */
struct _MonoDebuggerIOLayer
{
	void (*InitializeCriticalSection) (WapiCriticalSection *section);
	void (*DeleteCriticalSection) (WapiCriticalSection *section);
	gboolean (*TryEnterCriticalSection) (WapiCriticalSection *section);
	void (*EnterCriticalSection) (WapiCriticalSection *section);
	void (*LeaveCriticalSection) (WapiCriticalSection *section);

	guint32 (*WaitForSingleObject) (gpointer handle, guint32 timeout);
	guint32 (*SignalObjectAndWait) (gpointer signal_handle, gpointer wait,
					guint32 timeout, gboolean alertable);
	guint32 (*WaitForMultipleObjects) (guint32 numobjects, gpointer *handles,
					   gboolean waitall, guint32 timeout);

	gpointer (*CreateSemaphore) (WapiSecurityAttributes *security,
				     gint32 initial, gint32 max,
				     const guchar *name);
	gboolean (*ReleaseSemaphore) (gpointer handle, gint32 count, gint32 *prevcount);

	gpointer (*CreateThread) (WapiSecurityAttributes *security,
				  guint32 stacksize, WapiThreadStart start,
				  gpointer param, guint32 create, guint32 *tid);
};

extern MonoDebuggerIOLayer mono_debugger_io_layer;

#endif

extern void (*mono_debugger_event_handler) (MonoDebuggerEvent event, gpointer data, gpointer data2);

void            mono_debugger_initialize              (void);
void            mono_debugger_cleanup                 (void);

void            mono_debugger_lock                    (void);
void            mono_debugger_unlock                  (void);
void            mono_debugger_event                   (MonoDebuggerEvent event, gpointer data, gpointer data2);

MonoDebuggerSymbolFile *mono_debugger_add_symbol_file (MonoSymbolFile *symfile);
void            mono_debugger_add_type                (MonoDebuggerSymbolFile *symfile, MonoClass *klass);
void            mono_debugger_add_method              (MonoDebuggerSymbolFile *symfile, MonoMethod *method);

int             mono_debugger_insert_breakpoint_full  (MonoMethodDesc *desc, gboolean use_trampoline);
int             mono_debugger_remove_breakpoint       (int breakpoint_id);
int             mono_debugger_insert_breakpoint       (const gchar *method_name, gboolean include_namespace);
int             mono_debugger_method_has_breakpoint   (MonoMethod* method, gboolean use_trampoline);
void            mono_debugger_trampoline_breakpoint_callback (void);

gpointer        mono_debugger_create_notification_function (gpointer *notification_address);

MonoReflectionMethod *
ves_icall_MonoDebugger_GetMethod (MonoReflectionAssembly *assembly, guint32 token);

int
ves_icall_MonoDebugger_GetMethodToken (MonoReflectionAssembly *assembly, MonoReflectionMethod *method);

MonoReflectionType *
ves_icall_MonoDebugger_GetLocalTypeFromSignature (MonoReflectionAssembly *assembly, MonoArray *signature);

MonoReflectionType *
ves_icall_MonoDebugger_GetType (MonoReflectionAssembly *assembly, guint32 token);

#endif /* __MONO_DEBUG_DEBUGGER_H__ */
