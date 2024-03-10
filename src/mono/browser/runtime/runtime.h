
#ifndef __MONO_WASM_RUNTIME_H__
#define __MONO_WASM_RUNTIME_H__

#include <mono/metadata/appdomain.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/class.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/image.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/object.h>
#include <mono/metadata/debug-helpers.h>

extern int mono_wasm_enable_gc;

MonoDomain *mono_wasm_load_runtime_common (int debug_level, MonoLogCallback log_callback, const char *interp_opts);
MonoAssembly *mono_wasm_assembly_load (const char *name);
MonoClass *mono_wasm_assembly_find_class (MonoAssembly *assembly, const char *namespace, const char *name);

#endif
