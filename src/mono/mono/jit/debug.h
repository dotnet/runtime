#ifndef __MONO_JIT_DEBUG_H__
#define __MONO_JIT_DEBUG_H__

#include <glib.h>
#include <stdio.h>
#include <mono/metadata/loader.h>
#include <mono/jit/jit.h>

typedef struct _MonoDebugHandle MonoDebugHandle;

typedef enum {
	MONO_DEBUG_FORMAT_STABS,
	MONO_DEBUG_FORMAT_DWARF2,
	MONO_DEBUG_FORMAT_DWARF2_PLUS
} MonoDebugFormat;

extern MonoDebugHandle *mono_debug_handle;
extern GList *mono_debug_methods;

MonoDebugHandle* mono_debug_open_file (const char *filename, MonoDebugFormat format);

void           mono_debug_close (MonoDebugHandle* debug);

void           mono_debug_add_method (MonoDebugHandle* debug, MonoFlowGraph *cfg);

void           mono_debug_add_type (MonoDebugHandle* debug, MonoClass *klass);

/* DEBUGGER PUBLIC FUNCTION:
 *
 * This is a public function which is supposed to be called from within a debugger
 * each time the program stops. It's used to recreate the symbol file to tell the
 * debugger about method addresses and such things. After calling this function,
 * you must tell your debugger to reload its symbol file.
 */
void           mono_debug_make_symbols (void);

#endif /* __MONO_JIT_DEBUG_H__ */
