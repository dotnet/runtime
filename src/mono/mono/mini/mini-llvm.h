#ifndef __MONO_MINI_LLVM_H__
#define __MONO_MINI_LLVM_H__

#include "mini.h"

/* LLVM backend */
/* KEEP THIS IN SYNCH WITH mini-llvm-loaded.c */
void     mono_llvm_init                     (void) MONO_LLVM_INTERNAL;
void     mono_llvm_cleanup                  (void) MONO_LLVM_INTERNAL;
void     mono_llvm_emit_method              (MonoCompile *cfg) MONO_LLVM_INTERNAL;
void     mono_llvm_emit_call                (MonoCompile *cfg, MonoCallInst *call) MONO_LLVM_INTERNAL;
void     mono_llvm_create_aot_module        (MonoAssembly *assembly, const char *global_prefix, gboolean emit_dwarf, gboolean static_link, gboolean llvm_only) MONO_LLVM_INTERNAL;
void     mono_llvm_emit_aot_module          (const char *filename, const char *cu_name) MONO_LLVM_INTERNAL;
void     mono_llvm_emit_aot_file_info       (MonoAotFileInfo *info, gboolean has_jitted_code) MONO_LLVM_INTERNAL;
void     mono_llvm_emit_aot_data            (const char *symbol, guint8 *data, int data_len) MONO_LLVM_INTERNAL;
void     mono_llvm_check_method_supported   (MonoCompile *cfg) MONO_LLVM_INTERNAL;
void     mono_llvm_free_domain_info         (MonoDomain *domain) MONO_LLVM_INTERNAL;
MONO_API void mono_personality              (void);
int      mono_llvm_load                     (const char* bpath);
void     mono_llvm_rethrow_exception (MonoObject *ex);
void     mono_llvm_throw_exception (MonoObject *ex);
void     mono_llvm_throw_corlib_exception (guint32 ex_token_index);
void     mono_llvm_resume_exception (void);
gint32   mono_llvm_match_exception (MonoJitInfo *jinfo, guint32 region_start, guint32 region_end);
void     mono_llvm_clear_exception (void);
MonoObject *mono_llvm_load_exception (void);
void     mono_llvm_reset_exception (void);
void     mono_llvm_raise_exception (MonoException *e);

gboolean mini_llvm_init                     (void);

#endif
