/**
 * \file
 */

#ifndef __MONO_XDEBUG_LLDB_H__
#define __MONO_XDEBUG_LLDB_H__

#include "config.h"
#include "mini.h"

void mono_lldb_init (const char *options);

void mono_lldb_save_method_info (MonoCompile *cfg);

void mono_lldb_save_trampoline_info (MonoTrampInfo *info);

void mono_lldb_remove_method (MonoDomain *domain, MonoMethod *method, MonoJitDynamicMethodInfo *info);

void mono_lldb_save_specific_trampoline_info (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, gpointer code, guint32 code_len);

#endif
