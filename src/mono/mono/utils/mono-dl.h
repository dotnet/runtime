#ifndef __MONO_UTILS_DL_H__
#define __MONO_UTILS_DL_H__

#include "mono/utils/mono-compiler.h"
#include "mono/utils/mono-dl-fallback.h"

#ifdef TARGET_WIN32
#define MONO_SOLIB_EXT ".dll"
#elif defined(__ppc__) && defined(TARGET_MACH)
#define MONO_SOLIB_EXT ".dylib"
#elif defined(TARGET_MACH) && defined(TARGET_X86) && !defined(__native_client_codegen__)
#define MONO_SOLIB_EXT ".dylib"
#elif defined(TARGET_MACH) && defined(TARGET_AMD64) && !defined(__native_client_codegen__)
#define MONO_SOLIB_EXT ".dylib"
#else
#define MONO_SOLIB_EXT ".so"
#endif

typedef struct {
	void *handle;
	int main_module;

	/* If not NULL, use the methods in MonoDlFallbackHandler instead of the LL_* methods */
	MonoDlFallbackHandler *dl_fallback;
} MonoDl;


MonoDl*     mono_dl_open       (const char *name, int flags, char **error_msg) MONO_LLVM_INTERNAL;
char*       mono_dl_symbol     (MonoDl *module, const char *name, void **symbol) MONO_LLVM_INTERNAL;
void        mono_dl_close      (MonoDl *module) MONO_LLVM_INTERNAL;

char*       mono_dl_build_path (const char *directory, const char *name, void **iter) MONO_INTERNAL;

MonoDl*     mono_dl_open_runtime_lib (const char *lib_name, int flags, char **error_msg) MONO_INTERNAL;


//Platform API for mono_dl
const char* mono_dl_get_so_prefix (void) MONO_INTERNAL;
const char** mono_dl_get_so_suffixes (void) MONO_INTERNAL;
void* mono_dl_open_file (const char *file, int flags) MONO_INTERNAL;
void mono_dl_close_handle (MonoDl *module) MONO_INTERNAL;
void* mono_dl_lookup_symbol (MonoDl *module, const char *name) MONO_INTERNAL;
int mono_dl_convert_flags (int flags) MONO_INTERNAL;
char* mono_dl_current_error_string (void) MONO_INTERNAL;
int mono_dl_get_executable_path (char *buf, int buflen) MONO_INTERNAL;

#endif /* __MONO_UTILS_DL_H__ */

