#ifndef __MONO_DEBUG_DEBUGGER_H__
#define __MONO_DEBUG_DEBUGGER_H__

#include <glib.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/debug-mono-symfile.h>
#include <mono/io-layer/io-layer.h>

typedef struct _MonoDebuggerBreakpointInfo	MonoDebuggerBreakpointInfo;
typedef struct _MonoDebuggerBuiltinTypes	MonoDebuggerBuiltinTypes;
typedef struct _MonoDebuggerSymbolTable		MonoDebuggerSymbolTable;
typedef struct _MonoDebuggerSymbolFile		MonoDebuggerSymbolFile;
typedef struct _MonoDebuggerSymbolFilePriv	MonoDebuggerSymbolFilePriv;
typedef struct _MonoDebuggerRangeInfo		MonoDebuggerRangeInfo;
typedef struct _MonoDebuggerClassInfo		MonoDebuggerClassInfo;
typedef struct _MonoDebuggerIOLayer		MonoDebuggerIOLayer;

typedef enum {
	MONO_DEBUGGER_EVENT_TYPE_ADDED,
	MONO_DEBUGGER_EVENT_METHOD_ADDED,
	MONO_DEBUGGER_EVENT_BREAKPOINT
} MonoDebuggerEvent;

typedef enum {
	MONO_DEBUGGER_TYPE_KIND_FUNDAMENTAL = 1,
	MONO_DEBUGGER_TYPE_KIND_STRING,
	MONO_DEBUGGER_TYPE_KIND_SZARRAY,
	MONO_DEBUGGER_TYPE_KIND_ARRAY,
	MONO_DEBUGGER_TYPE_KIND_POINTER,
	MONO_DEBUGGER_TYPE_KIND_ENUM,
	MONO_DEBUGGER_TYPE_KIND_OBJECT,
	MONO_DEBUGGER_TYPE_KIND_STRUCT,
	MONO_DEBUGGER_TYPE_KIND_CLASS,
	MONO_DEBUGGER_TYPE_KIND_CLASS_INFO
} MonoDebuggerTypeKind;

struct _MonoDebuggerBreakpointInfo {
	guint32 index;
	MonoMethodDesc *desc;
};

struct _MonoDebuggerBuiltinTypes {
	guint32 total_size;
	MonoDebuggerClassInfo *object_class;
	MonoDebuggerClassInfo *byte_class;
	MonoDebuggerClassInfo *void_class;
	MonoDebuggerClassInfo *boolean_class;
	MonoDebuggerClassInfo *sbyte_class;
	MonoDebuggerClassInfo *int16_class;
	MonoDebuggerClassInfo *uint16_class;
	MonoDebuggerClassInfo *int32_class;
	MonoDebuggerClassInfo *uint32_class;
	MonoDebuggerClassInfo *int_class;
	MonoDebuggerClassInfo *uint_class;
	MonoDebuggerClassInfo *int64_class;
	MonoDebuggerClassInfo *uint64_class;
	MonoDebuggerClassInfo *single_class;
	MonoDebuggerClassInfo *double_class;
	MonoDebuggerClassInfo *char_class;
	MonoDebuggerClassInfo *string_class;
	MonoDebuggerClassInfo *enum_class;
	MonoDebuggerClassInfo *array_class;
};

struct _MonoDebuggerSymbolTable {
	guint64 magic;
	guint32 version;
	guint32 total_size;

	/*
	 * Corlib and builtin types.
	 */
	MonoDomain *domain;
	MonoDebuggerSymbolFile *corlib;
	MonoDebuggerBuiltinTypes *builtin_types;

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
	MonoImage *image;
	const char *image_file;
	guint32 class_entry_size;
	/* Pointer to the malloced range table. */
	guint32 locked;
	guint32 generation;
	MonoDebuggerRangeInfo *range_table;
	guint32 range_entry_size;
	guint32 num_range_entries;
	/* Pointer to the malloced class table. */
	MonoDebuggerClassInfo *class_table;
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

extern void (*mono_debugger_event_handler) (MonoDebuggerEvent event, gpointer data, guint32 arg);

void            mono_debugger_initialize                  (MonoDomain *domain);
void            mono_debugger_cleanup                     (void);

void            mono_debugger_lock                        (void);
void            mono_debugger_unlock                      (void);
void            mono_debugger_event                       (MonoDebuggerEvent event, gpointer data, guint32 arg);

MonoDebuggerSymbolFile   *mono_debugger_add_symbol_file   (MonoDebugHandle *handle);
void                      mono_debugger_add_type          (MonoDebuggerSymbolFile *symfile, MonoClass *klass);
MonoDebuggerBuiltinTypes *mono_debugger_add_builtin_types (MonoDebuggerSymbolFile *symfile);

void            mono_debugger_add_method                  (MonoDebuggerSymbolFile *symfile,
						           MonoDebugMethodInfo *minfo,
						           MonoDebugMethodJitInfo *jit);

int             mono_debugger_insert_breakpoint_full      (MonoMethodDesc *desc);
int             mono_debugger_remove_breakpoint           (int breakpoint_id);
int             mono_debugger_insert_breakpoint           (const gchar *method_name, gboolean include_namespace);
int             mono_debugger_method_has_breakpoint       (MonoMethod *method);
void            mono_debugger_breakpoint_callback         (MonoMethod *method, guint32 idx);

gpointer        mono_debugger_create_notification_function (gpointer *notification_address);

MonoObject     *mono_debugger_runtime_invoke              (MonoMethod *method, void *obj,
							   void **params, MonoObject **exc);

MonoReflectionMethod *
ves_icall_MonoDebugger_GetMethod (MonoReflectionAssembly *assembly, guint32 token);

int
ves_icall_MonoDebugger_GetMethodToken (MonoReflectionAssembly *assembly, MonoReflectionMethod *method);

MonoReflectionType *
ves_icall_MonoDebugger_GetLocalTypeFromSignature (MonoReflectionAssembly *assembly, MonoArray *signature);

MonoReflectionType *
ves_icall_MonoDebugger_GetType (MonoReflectionAssembly *assembly, guint32 token);

#endif /* __MONO_DEBUG_DEBUGGER_H__ */
