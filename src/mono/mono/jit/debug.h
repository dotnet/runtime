#ifndef __MONO_JIT_DEBUG_H__
#define __MONO_JIT_DEBUG_H__

#include <glib.h>
#include <stdio.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/debug-mono-symfile.h>
#include <mono/metadata/loader.h>
#include <mono/jit/jit.h>

typedef struct _MonoDebugHandle			MonoDebugHandle;
typedef struct _MonoDebuggerInfo		MonoDebuggerInfo;
typedef struct _MonoDebuggerSymbolFileTable	MonoDebuggerSymbolFileTable;
typedef struct _MonoDebuggerBreakpointInfo	MonoDebuggerBreakpointInfo;

typedef enum {
	MONO_DEBUG_FORMAT_NONE,
	MONO_DEBUG_FORMAT_STABS,
	MONO_DEBUG_FORMAT_DWARF2,
	MONO_DEBUG_FORMAT_MONO
} MonoDebugFormat;

extern MonoDebugFormat mono_debug_format;

/*
 * This variable is intended to be set in a debugger.
 *
 * If it's non-zero, arch_compile_method() will insert a breakpoint next time
 * it compiles a method.
 *
 * If it's positive, it acts as a counter which is decremented each time it's
 * used. Set it to a negative value to make arch_compile_method() insert a
 * breakpoint for each method.
 *
 * To use this, you should create a GDB macro like this:
 *
 *    define enter
 *      set mono_debug_insert_breakpoint = 1
 *      continue
 *      set *mono_debug_last_breakpoint_address = 0x90
 *      reload-symbol-files
 *      frame
 *    end
 *
 *    define reload-symbol-files
 *      call mono_debug_make_symbols ()
 *      add-symbol-file Test-debug.o
 *      add-symbol-file /tmp/corlib.o
 *    end
 *
 */
extern int mono_debug_insert_breakpoint;

MonoDebugHandle* mono_debug_open (const char *name, MonoDebugFormat format, const char **args);

void           mono_debug_cleanup (void);

void           mono_debug_add_image (MonoDebugHandle* debug, MonoImage *image);

MonoDebugHandle* mono_debug_handle_from_class (MonoClass *klass);

void           mono_debug_add_method (MonoFlowGraph *cfg);

void           mono_debug_add_type (MonoClass *klass);

gchar *        mono_debug_source_location_from_address (MonoMethod *method, guint32 address,
							guint32 *line_number);

gint32         mono_debug_il_offset_from_address (MonoMethod *method, gint32 address);

gint32         mono_debug_address_from_il_offset (MonoMethod *method, gint32 il_offset);

int            mono_method_has_breakpoint (MonoMethod* method, gboolean use_trampoline);

int            mono_insert_breakpoint (const gchar *method_name, gboolean include_namespace);

int            mono_insert_breakpoint_full (MonoMethodDesc *desc, gboolean use_trampoline);

int            mono_remove_breakpoint (int breakpint_id);

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

/*
 * There's a global data symbol called `MONO_DEBUGGER__debugger_info' which
 * contains pointers to global variables and functions which must be accessed
 * by the debugger.
 */
struct _MonoDebuggerInfo {
	guint64 magic;
	guint32 version;
	guint32 total_size;
	guint8 **generic_trampoline_code;
	guint8 **breakpoint_trampoline_code;
	guint32 *symbol_file_generation;
	MonoDebuggerSymbolFileTable **symbol_file_table;
	int (*update_symbol_file_table) (void);
	gpointer (*compile_method) (MonoMethod *method);
	guint64 (*insert_breakpoint) (guint64 method_argument, const gchar *string_argument);
	guint64 (*remove_breakpoint) (guint64 breakpoint);
};

struct _MonoDebuggerSymbolFileTable {
	guint64 magic;
	guint32 version;
	guint32 total_size;
	guint32 count;
	guint32 generation;
	MonoSymbolFile *symfiles [MONO_ZERO_LEN_ARRAY];
};

struct _MonoDebuggerBreakpointInfo {
	guint32 index;
	gboolean use_trampoline;
	MonoMethodDesc *desc;
};

#endif /* __MONO_JIT_DEBUG_H__ */
