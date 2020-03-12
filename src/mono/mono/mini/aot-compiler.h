/**
 * \file
 */

#ifndef __MONO_AOT_COMPILER_H__
#define __MONO_AOT_COMPILER_H__

#include "mini.h"

int mono_compile_assembly (MonoAssembly *ass, guint32 opts, const char *aot_options, gpointer **aot_state);
int mono_compile_deferred_assemblies (guint32 opts, const char *aot_options, gpointer **aot_state);
void* mono_aot_readonly_field_override (MonoClassField *field);
gboolean mono_aot_direct_icalls_enabled_for_method (MonoCompile *cfg, MonoMethod *method);
gboolean mono_aot_is_shared_got_offset (int offset);

guint32  mono_aot_get_got_offset            (MonoJumpInfo *ji);
char*    mono_aot_get_method_name           (MonoCompile *cfg);
char*    mono_aot_get_mangled_method_name   (MonoMethod *method);
gboolean mono_aot_is_direct_callable        (MonoJumpInfo *patch_info);
gboolean mono_aot_is_externally_callable    (MonoMethod *cmethod);
void     mono_aot_mark_unused_llvm_plt_entry(MonoJumpInfo *patch_info);
char*    mono_aot_get_plt_symbol            (MonoJumpInfoType type, gconstpointer data);
char*    mono_aot_get_direct_call_symbol    (MonoJumpInfoType type, gconstpointer data);
int      mono_aot_get_method_index          (MonoMethod *method);
MonoJumpInfo* mono_aot_patch_info_dup       (MonoJumpInfo* ji);
gboolean mono_aot_can_specialize (MonoMethod *method);
gboolean mono_aot_can_enter_interp (MonoMethod *method);

#endif
