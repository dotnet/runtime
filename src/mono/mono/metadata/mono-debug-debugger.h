/*
 * This header is only installed for use by the debugger:
 * the structures and the API declared here are not supported.
 */

#ifndef __MONO_DEBUG_DEBUGGER_H__
#define __MONO_DEBUG_DEBUGGER_H__

#include <glib.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/debug-mono-symfile.h>
#include <mono/io-layer/io-layer.h>

typedef struct _MonoDebuggerBreakpointInfo	MonoDebuggerBreakpointInfo;
typedef struct _MonoDebuggerBuiltinTypeInfo	MonoDebuggerBuiltinTypeInfo;
typedef struct _MonoDebuggerBuiltinTypes	MonoDebuggerBuiltinTypes;
typedef struct _MonoDebuggerSymbolTable		MonoDebuggerSymbolTable;
typedef struct _MonoDebuggerSymbolFile		MonoDebuggerSymbolFile;
typedef struct _MonoDebuggerSymbolFilePriv	MonoDebuggerSymbolFilePriv;
typedef struct _MonoDebuggerRangeInfo		MonoDebuggerRangeInfo;
typedef struct _MonoDebuggerClassEntry		MonoDebuggerClassEntry;
typedef struct _MonoDebuggerClassInfo		MonoDebuggerClassInfo;
typedef struct _MonoDebuggerClassTable		MonoDebuggerClassTable;
typedef struct _MonoDebuggerIOLayer		MonoDebuggerIOLayer;

typedef enum {
	MONO_DEBUGGER_EVENT_BREAKPOINT,
	MONO_DEBUGGER_EVENT_RELOAD_SYMTABS,
	MONO_DEBUGGER_EVENT_UNHANDLED_EXCEPTION
} MonoDebuggerEvent;

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

typedef enum {
	MONO_DEBUGGER_TYPE_UNKNOWN	= 0,
	MONO_DEBUGGER_TYPE_VOID,
	MONO_DEBUGGER_TYPE_BOOLEAN,
	MONO_DEBUGGER_TYPE_CHAR,
	MONO_DEBUGGER_TYPE_I1,
	MONO_DEBUGGER_TYPE_U1,
	MONO_DEBUGGER_TYPE_I2,
	MONO_DEBUGGER_TYPE_U2,
	MONO_DEBUGGER_TYPE_I4,
	MONO_DEBUGGER_TYPE_U4,
	MONO_DEBUGGER_TYPE_I8,
	MONO_DEBUGGER_TYPE_U8,
	MONO_DEBUGGER_TYPE_R4,
	MONO_DEBUGGER_TYPE_R8,
	MONO_DEBUGGER_TYPE_I,
	MONO_DEBUGGER_TYPE_U,
	MONO_DEBUGGER_TYPE_STRING,
	MONO_DEBUGGER_TYPE_ARRAY,
	MONO_DEBUGGER_TYPE_ENUM,
	MONO_DEBUGGER_TYPE_MAX		= 100
} MonoDebuggerType;

struct _MonoDebuggerBreakpointInfo {
	guint32 index;
	MonoMethodDesc *desc;
};

struct _MonoDebuggerBuiltinTypeInfo
{
	MonoDebuggerClassEntry *centry;
	MonoClass *klass;
	guint32 type_info;
	guint32 class_info;
	guint8 *type_data;
};

struct _MonoDebuggerBuiltinTypes {
	guint32 total_size;
	guint32 type_info_size;
	MonoDebuggerBuiltinTypeInfo *object_type;
	MonoDebuggerBuiltinTypeInfo *valuetype_type;
	MonoDebuggerBuiltinTypeInfo *byte_type;
	MonoDebuggerBuiltinTypeInfo *void_type;
	MonoDebuggerBuiltinTypeInfo *boolean_type;
	MonoDebuggerBuiltinTypeInfo *sbyte_type;
	MonoDebuggerBuiltinTypeInfo *int16_type;
	MonoDebuggerBuiltinTypeInfo *uint16_type;
	MonoDebuggerBuiltinTypeInfo *int32_type;
	MonoDebuggerBuiltinTypeInfo *uint32_type;
	MonoDebuggerBuiltinTypeInfo *int_type;
	MonoDebuggerBuiltinTypeInfo *uint_type;
	MonoDebuggerBuiltinTypeInfo *int64_type;
	MonoDebuggerBuiltinTypeInfo *uint64_type;
	MonoDebuggerBuiltinTypeInfo *single_type;
	MonoDebuggerBuiltinTypeInfo *double_type;
	MonoDebuggerBuiltinTypeInfo *char_type;
	MonoDebuggerBuiltinTypeInfo *string_type;
	MonoDebuggerBuiltinTypeInfo *enum_type;
	MonoDebuggerBuiltinTypeInfo *array_type;
	MonoDebuggerBuiltinTypeInfo *exception_type;
	MonoDebuggerBuiltinTypeInfo *type_type;
};

struct _MonoDebuggerSymbolTable {
	guint64 magic;
	guint32 version;
	guint32 total_size;

	/*
	 * Corlib and builtin types.
	 */
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

	/*
	 * New in version 44.
	 */
	guint32 num_misc_tables;
	guint32 misc_table_chunk_size;
	gpointer *misc_tables;
	gpointer current_misc_table;
	guint32 misc_table_size;
	guint32 misc_table_offset;
	guint32 misc_table_start;
};

struct _MonoDebuggerSymbolFile {
	guint32 index;
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
	/* Pointer to the class table. */
	guint32 class_table_size;
	MonoDebuggerClassTable *current_class_table;
	MonoDebuggerClassTable *class_table_start;
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

struct _MonoDebuggerClassTable {
	MonoDebuggerClassTable *next;
	guint32 index, size;
	MonoDebuggerClassInfo *data;
};

struct _MonoDebuggerClassInfo {
	MonoClass *klass;
	guint32 rank;
	guint32 token;
	guint32 type_info;
};

struct _MonoDebuggerClassEntry {
	MonoDebuggerClassInfo *info;
	guint32 type_reference;
};

enum {
	MONO_DEBUGGER_MISC_ENTRY_TYPE_UNKNOWN	= 0,
	MONO_DEBUGGER_MISC_ENTRY_TYPE_WRAPPER
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
	guint32 (*GetCurrentThreadId) (void);
};

extern MonoDebuggerIOLayer mono_debugger_io_layer;

#endif

extern void (*mono_debugger_event_handler) (MonoDebuggerEvent event, gpointer data, guint32 arg);

void            mono_debugger_initialize                  (void);
void            mono_debugger_cleanup                     (void);

void            mono_debugger_lock                        (void);
void            mono_debugger_unlock                      (void);
void            mono_debugger_event                       (MonoDebuggerEvent event, gpointer data, guint32 arg);

MonoDebuggerSymbolFile   *_mono_debugger_get_symfile      (MonoImage *image);
MonoDebuggerSymbolFile   *mono_debugger_add_symbol_file   (MonoDebugHandle *handle);
void                      mono_debugger_start_add_type    (MonoDebuggerSymbolFile *symfile,
							   MonoClass *klass);
void                      mono_debugger_add_type          (MonoDebuggerSymbolFile *symfile,
							   MonoClass *klass);
MonoDebuggerBuiltinTypes *mono_debugger_add_builtin_types (MonoDebuggerSymbolFile *symfile);

void            mono_debugger_add_method                  (MonoDebuggerSymbolFile *symfile,
						           MonoDebugMethodInfo *minfo,
						           MonoDebugMethodJitInfo *jit);

void            mono_debugger_add_wrapper                 (MonoMethod *wrapper,
							   MonoDebugMethodJitInfo *jit,
							   gpointer addr);


int             mono_debugger_insert_breakpoint_full      (MonoMethodDesc *desc);
int             mono_debugger_remove_breakpoint           (int breakpoint_id);
int             mono_debugger_insert_breakpoint           (const gchar *method_name, gboolean include_namespace);
int             mono_debugger_method_has_breakpoint       (MonoMethod *method);
void            mono_debugger_breakpoint_callback         (MonoMethod *method, guint32 idx);

gpointer        mono_debugger_create_notification_function (gpointer *notification_address);

MonoObject     *mono_debugger_runtime_invoke              (MonoMethod *method, void *obj,
							   void **params, MonoObject **exc);

guint32         mono_debugger_lookup_type                 (const gchar *type_name);
gint32          mono_debugger_lookup_assembly             (const gchar *name);
gboolean        mono_debugger_unhandled_exception         (gpointer addr, MonoObject *exc);


void *
mono_vtable_get_static_field_data (MonoVTable *vt);


MonoReflectionMethod *
ves_icall_MonoDebugger_GetMethod (MonoReflectionAssembly *assembly, guint32 token);

int
ves_icall_MonoDebugger_GetMethodToken (MonoReflectionAssembly *assembly, MonoReflectionMethod *method);

MonoReflectionType *
ves_icall_MonoDebugger_GetLocalTypeFromSignature (MonoReflectionAssembly *assembly, MonoArray *signature);

MonoReflectionType *
ves_icall_MonoDebugger_GetType (MonoReflectionAssembly *assembly, guint32 token);

gchar *
mono_debugger_check_runtime_version (const char *filename);

#endif /* __MONO_DEBUG_DEBUGGER_H__ */
