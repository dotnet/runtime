#ifndef __PINVOKE_H__
#define __PINVOKE_H__

typedef struct {
	const char *name;
	void *func;
} PinvokeImport;

void*
wasm_dl_lookup_pinvoke_table (const char *name);

int
wasm_dl_is_pinvoke_table (void *handle);

#endif
