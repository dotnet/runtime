#ifndef __MONO_JIT_DEBUG_H__
#define __MONO_JIT_DEBUG_H__

#include <glib.h>
#include <stdio.h>
#include <mono/metadata/loader.h>
#include <mono/jit/jit.h>

typedef struct _MonoDebugHandle MonoDebugHandle;

extern MonoDebugHandle *mono_debug_handle;
extern GList *mono_debug_methods;

MonoDebugHandle* mono_debug_open_file (char *filename);

void           mono_debug_close (MonoDebugHandle* debug);

void           mono_debug_add_method (MonoDebugHandle* debug, MonoFlowGraph *cfg);

void           mono_debug_add_type (MonoDebugHandle* debug, MonoClass *klass);

void           mono_debug_make_symbols (MonoDebugHandle* debug);

#endif /* __MONO_JIT_DEBUG_H__ */
