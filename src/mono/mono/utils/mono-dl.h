/**
 * \file
 */

#ifndef __MONO_UTILS_DL_H__
#define __MONO_UTILS_DL_H__

#include "mono/utils/mono-compiler.h"
#include "mono/utils/mono-dl-fallback.h"
#include "mono/utils/refcount.h"
#include "mono/utils/mono-error.h"

#ifdef TARGET_WIN32
#define MONO_SOLIB_EXT ".dll"
#elif defined(TARGET_MACH)
#define MONO_SOLIB_EXT ".dylib"
#else
#define MONO_SOLIB_EXT ".so"
#endif

typedef struct {
	// Only used in native-library.c, native library lock should be held when modifying
	// Incremented on NativeLibrary.Load calls, decremented on NativeLibrary.Free
	MonoRefCount ref;
	void *handle;
	int main_module;
	char *full_name;
	/* If not NULL, use the methods in MonoDlFallbackHandler instead of the LL_* methods */
	MonoDlFallbackHandler *dl_fallback;
} MonoDl;

MONO_EXTERN_C
MonoDl*     mono_dl_open       (const char *name, int flags, MonoError *error);
MONO_EXTERN_C
void*       mono_dl_symbol     (MonoDl *module, const char *name, MonoError *error);
MONO_EXTERN_C
void        mono_dl_close      (MonoDl *module, MonoError *error);

char*       mono_dl_build_path (const char *directory, const char *name, void **iter);
char*       mono_dl_build_platform_path (const char *directory, const char *name, void **iter);

MonoDl *
mono_dl_open_self (MonoError *error);
// This converts the MONO_DL_* enum to native flags, combines it with the other flags passed, and resolves some inconsistencies
MonoDl *
mono_dl_open_full (const char *name, int mono_flags, int native_flags, MonoError *error);


//Platform API for mono_dl
const char* mono_dl_get_so_prefix (void);
const char** mono_dl_get_so_suffixes (void);
void* mono_dl_open_file (const char *file, int flags, MonoError *error);
void mono_dl_close_handle (MonoDl *module, MonoError *error);
void* mono_dl_lookup_symbol (MonoDl *module, const char *name);
int mono_dl_convert_flags (int mono_flags, int native_flags);
char* mono_dl_current_error_string (void);
const char* mono_dl_get_system_dir (void);

#if defined (HOST_WASM)
int mono_wasm_add_bundled_file (const char *name, const unsigned char *data, unsigned int size);
const unsigned char* mono_wasm_get_bundled_file (const char *name, int* out_length);
#endif /* HOST_WASM */

#endif /* __MONO_UTILS_DL_H__ */

