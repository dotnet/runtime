/**
 * \file
 */

#ifndef __MONO_UTILS_DL_H__
#define __MONO_UTILS_DL_H__

#include "mono/utils/mono-compiler.h"
#include "mono/utils/mono-dl-fallback.h"
#include "mono/utils/refcount.h"

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
MonoDl*     mono_dl_open       (const char *name, int flags, char **error_msg);
MONO_EXTERN_C
char*       mono_dl_symbol     (MonoDl *module, const char *name, void **symbol);
MONO_EXTERN_C
void        mono_dl_close      (MonoDl *module);

char*       mono_dl_build_path (const char *directory, const char *name, void **iter);

MonoDl*     mono_dl_open_runtime_lib (const char *lib_name, int flags, char **error_msg);

MonoDl *
mono_dl_open_self (char **error_msg);
// This converts the MONO_DL_* enum to native flags, combines it with the other flags passed, and resolves some inconsistencies
MonoDl *
mono_dl_open_full (const char *name, int mono_flags, int native_flags, char **error_msg);


//Platform API for mono_dl
const char* mono_dl_get_so_prefix (void);
const char** mono_dl_get_so_suffixes (void);
void* mono_dl_open_file (const char *file, int flags);
void mono_dl_close_handle (MonoDl *module);
void* mono_dl_lookup_symbol (MonoDl *module, const char *name);
int mono_dl_convert_flags (int mono_flags, int native_flags);
char* mono_dl_current_error_string (void);
int mono_dl_get_executable_path (char *buf, int buflen);
const char* mono_dl_get_system_dir (void);

#endif /* __MONO_UTILS_DL_H__ */

