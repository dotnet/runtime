/**
 * \file
 */

#ifndef __MONO_MINI_LLVM_H__
#define __MONO_MINI_LLVM_H__

#include "mini.h"
#include "aot-runtime.h"

/* LLVM backend */

typedef enum {
	LLVM_MODULE_FLAG_STATIC = (1 << 0),
	LLVM_MODULE_FLAG_LLVM_ONLY = (1 << 1),
	LLVM_MODULE_FLAG_DWARF = (1 << 2),
	LLVM_MODULE_FLAG_CODEVIEW = (1 << 3),
	LLVM_MODULE_FLAG_INTERP = (1 << 4)
} LLVMModuleFlags;

/* KEEP THIS IN SYNCH WITH mini-llvm-loaded.c */
void     mono_llvm_init                     (void) MONO_LLVM_INTERNAL;
void     mono_llvm_cleanup                  (void) MONO_LLVM_INTERNAL;
void     mono_llvm_emit_method              (MonoCompile *cfg) MONO_LLVM_INTERNAL;
void     mono_llvm_emit_call                (MonoCompile *cfg, MonoCallInst *call) MONO_LLVM_INTERNAL;
void     mono_llvm_create_aot_module        (MonoAssembly *assembly, const char *global_prefix, int initial_got_size, LLVMModuleFlags flags) MONO_LLVM_INTERNAL;
void     mono_llvm_emit_aot_module          (const char *filename, const char *cu_name) MONO_LLVM_INTERNAL;
void     mono_llvm_emit_aot_file_info       (MonoAotFileInfo *info, gboolean has_jitted_code) MONO_LLVM_INTERNAL;
void     mono_llvm_emit_aot_data            (const char *symbol, guint8 *data, int data_len) MONO_LLVM_INTERNAL;
void     mono_llvm_check_method_supported   (MonoCompile *cfg) MONO_LLVM_INTERNAL;
void     mono_llvm_free_domain_info         (MonoDomain *domain) MONO_LLVM_INTERNAL;
MONO_API void mono_personality              (void);
int      mono_llvm_load                     (const char* bpath);
void     mono_llvm_create_vars (MonoCompile *cfg) MONO_LLVM_INTERNAL;
void     mono_llvm_fixup_aot_module         (void) MONO_LLVM_INTERNAL;

gboolean mini_llvm_init                     (void);
MonoCPUFeatures mono_llvm_get_cpu_features  (void);

#endif
