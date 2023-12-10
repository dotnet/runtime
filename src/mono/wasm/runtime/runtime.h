
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
void* mono_wasm_get_native_to_interp (MonoMethod *method, void *extra_arg);
void mono_wasm_init_icall_table (void);

#endif
