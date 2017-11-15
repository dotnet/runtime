/**
 * \file
 */

#ifndef __MONO_AOT_COMPILER_H__
#define __MONO_AOT_COMPILER_H__

#include "mini.h"

int mono_compile_assembly (MonoAssembly *ass, guint32 opts, const char *aot_options, gpointer **aot_state);
int mono_compile_deferred_assemblies (guint32 opts, const char *aot_options, gpointer **aot_state);
void* mono_aot_readonly_field_override (MonoClassField *field);
gboolean mono_aot_is_shared_got_offset (int offset) MONO_LLVM_INTERNAL;

guint32  mono_aot_get_got_offset            (MonoJumpInfo *ji) MONO_LLVM_INTERNAL;
char*    mono_aot_get_method_name           (MonoCompile *cfg) MONO_LLVM_INTERNAL;
char*    mono_aot_get_mangled_method_name   (MonoMethod *method) MONO_LLVM_INTERNAL;
gboolean mono_aot_is_linkonce_method        (MonoMethod *method) MONO_LLVM_INTERNAL;
gboolean mono_aot_is_direct_callable        (MonoJumpInfo *patch_info) MONO_LLVM_INTERNAL;
void     mono_aot_mark_unused_llvm_plt_entry(MonoJumpInfo *patch_info) MONO_LLVM_INTERNAL;
char*    mono_aot_get_plt_symbol            (MonoJumpInfoType type, gconstpointer data) MONO_LLVM_INTERNAL;
char*    mono_aot_get_direct_call_symbol    (MonoJumpInfoType type, gconstpointer data) MONO_LLVM_INTERNAL;
int      mono_aot_get_method_index          (MonoMethod *method) MONO_LLVM_INTERNAL;
MonoJumpInfo* mono_aot_patch_info_dup       (MonoJumpInfo* ji) MONO_LLVM_INTERNAL;

#endif




