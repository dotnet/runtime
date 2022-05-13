#ifndef __PINVOKE_H__
#define __PINVOKE_H__

#include <stdint.h>

typedef struct {
	const char *name;
	void *func;
} PinvokeImport;

typedef struct {
	void *func;
	void *arg;
} InterpFtnDesc;


void*
wasm_dl_lookup_pinvoke_table (const char *name);

int
wasm_dl_is_pinvoke_table (void *handle);

void*
wasm_dl_get_native_to_interp (const char *key, void *extra_arg);

void
mono_wasm_pinvoke_vararg_stub (void);


typedef void* (*MonoWasmNativeToInterpCallback) (char * cookie);

void
mono_wasm_install_interp_to_native_callback (MonoWasmNativeToInterpCallback cb);

typedef struct {
	
} InterpMethodArguments;

int 
mono_wasm_interp_method_args_get_iarg (InterpMethodArguments *margs, int i);

int64_t
mono_wasm_interp_method_args_get_larg (InterpMethodArguments *margs, int i);

void*
mono_wasm_interp_method_args_get_retval  (InterpMethodArguments *margs);

#endif
