#ifndef __PINVOKE_H__
#define __PINVOKE_H__

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

#endif
