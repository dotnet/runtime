#ifndef __MONO_JIT_DEBUG_H__
#define __MONO_JIT_DEBUG_H__

#include <glib.h>
#include <stdio.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/debug-mono-symfile.h>
#include <mono/metadata/loader.h>

typedef struct _MonoDebugHandle			MonoDebugHandle;
typedef struct _MonoDebuggerSymbolFileTable	MonoDebuggerSymbolFileTable;
typedef struct _MonoDebuggerBreakpointInfo	MonoDebuggerBreakpointInfo;

typedef struct _MonoDebuggerIOLayer		MonoDebuggerIOLayer;

typedef enum {
	MONO_DEBUG_FORMAT_NONE,
	MONO_DEBUG_FORMAT_STABS,
	MONO_DEBUG_FORMAT_DWARF2,
	MONO_DEBUG_FORMAT_MONO
} MonoDebugFormat;

typedef enum {
	MONO_DEBUGGER_EVENT_TYPE_ADDED,
	MONO_DEBUGGER_EVENT_METHOD_ADDED,
	MONO_DEBUGGER_EVENT_BREAKPOINT_TRAMPOLINE,
	MONO_DEBUGGER_EVENT_THREAD_CREATED
} MonoDebuggerEvent;

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

extern MonoDebugFormat mono_debug_format;

MonoDebugHandle* mono_debug_open (MonoAssembly *assembly, MonoDebugFormat format, const char **args);

void           mono_debug_cleanup (void);

void           mono_debug_add_wrapper (MonoMethod *method, MonoMethod *wrapper_method);

void           mono_debug_add_type (MonoClass *klass);

gchar *        mono_debug_source_location_from_address (MonoMethod *method, guint32 address,
							guint32 *line_number);

gint32         mono_debug_il_offset_from_address (MonoMethod *method, gint32 address);

gint32         mono_debug_address_from_il_offset (MonoMethod *method, gint32 il_offset);

int            mono_method_has_breakpoint (MonoMethod* method, gboolean use_trampoline);

int            mono_insert_breakpoint (const gchar *method_name, gboolean include_namespace);

int            mono_insert_breakpoint_full (MonoMethodDesc *desc, gboolean use_trampoline);

int            mono_remove_breakpoint (int breakpint_id);

void           mono_debugger_trampoline_breakpoint_callback (void);

void           mono_debugger_event (MonoDebuggerEvent event, gpointer data, gpointer data2);

gpointer       mono_debug_create_notification_function (gpointer *notification_address);

void           mono_debug_init (int running_in_the_mono_debugger);
void           mono_debug_lock (void);
void           mono_debug_unlock (void);
int            mono_debug_update_symbol_file_table (void);


/* DEBUGGER PUBLIC FUNCTION:
 *
 * This is a public function which is supposed to be called from within a debugger
 * each time the program stops. It's used to recreate the symbol file to tell the
 * debugger about method addresses and such things. After calling this function,
 * you must tell your debugger to reload its symbol file.
 */
void           mono_debug_make_symbols (void);

void           mono_debug_write_symbols (MonoDebugHandle* debug);

/*
 * Address of the x86 trampoline code.  This is used by the debugger to check
 * whether a method is a trampoline.
 */
extern guint8 *mono_generic_trampoline_code;

/*
 * Address of a special breakpoint code which is used by the debugger to get a breakpoint
 * after compiling a method.
 */
extern guint8 *mono_breakpoint_trampoline_code;

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

struct _MonoDebuggerSymbolFileTable {
	guint64 magic;
	guint32 version;
	guint32 total_size;
	guint32 count;
	guint32 generation;
	MonoGlobalSymbolFile *global_symfile;
	MonoSymbolFile *symfiles [MONO_ZERO_LEN_ARRAY];
};

struct _MonoDebuggerBreakpointInfo {
	guint32 index;
	gboolean use_trampoline;
	MonoMethodDesc *desc;
};

#endif /* __MONO_JIT_DEBUG_H__ */
