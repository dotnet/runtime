#ifndef __MONO_JIT_DEBUG_H__
#define __MONO_JIT_DEBUG_H__

#include <glib.h>
#include <stdio.h>
#include <mono/metadata/debug-symfile.h>
#include <mono/metadata/loader.h>
#include <mono/jit/jit.h>

typedef struct _MonoDebugHandle MonoDebugHandle;

typedef enum {
	MONO_DEBUG_FORMAT_NONE,
	MONO_DEBUG_FORMAT_STABS,
	MONO_DEBUG_FORMAT_DWARF2,
	MONO_DEBUG_FORMAT_DWARF2_PLUS
} MonoDebugFormat;

extern MonoDebugFormat mono_debug_format;
extern GList *mono_debug_methods;

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

/*
 * This is set the the core address of the last inserted breakpoint. You can
 * use this in GDB to unset the breakpoint.
 */

extern gchar *mono_debug_last_breakpoint_address;

MonoDebugHandle* mono_debug_open (MonoAssembly *assembly, MonoDebugFormat format,
				  const char **args);

void           mono_debug_cleanup (void);

void           mono_debug_add_image (MonoDebugHandle* debug, MonoImage *image);

MonoDebugHandle* mono_debug_handle_from_class (MonoClass *klass);

void           mono_debug_add_method (MonoFlowGraph *cfg);

void           mono_debug_add_type (MonoClass *klass);

gchar *        mono_debug_source_location_from_address (MonoMethod *method, guint32 address);

gint32         mono_debug_il_offset_from_address (MonoMethod *method, guint32 address);

gint32         mono_debug_address_from_il_offset (MonoMethod *method, guint32 il_offset);

/* DEBUGGER PUBLIC FUNCTION:
 *
 * This is a public function which is supposed to be called from within a debugger
 * each time the program stops. It's used to recreate the symbol file to tell the
 * debugger about method addresses and such things. After calling this function,
 * you must tell your debugger to reload its symbol file.
 */
void           mono_debug_make_symbols (void);

void           mono_debug_write_symbols (MonoDebugHandle* debug);

#endif /* __MONO_JIT_DEBUG_H__ */
