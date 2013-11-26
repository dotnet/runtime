/*
 * This is a private header file.
 * The API in here is undocumented and may only be used by the JIT to
 * communicate with the debugger.
 */

#ifndef __MONO_DEBUG_DEBUGGER_H__
#define __MONO_DEBUG_DEBUGGER_H__

#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/debug-mono-symfile.h>
#include <mono/utils/mono-compiler.h>


void            mono_debugger_initialize                    (void) MONO_INTERNAL;

void            mono_debugger_lock                          (void) MONO_INTERNAL;
void            mono_debugger_unlock                        (void) MONO_INTERNAL;

gchar *
mono_debugger_check_runtime_version (const char *filename) MONO_INTERNAL;

MonoDebugMethodAddressList *
mono_debugger_insert_method_breakpoint (MonoMethod *method, guint64 idx) MONO_INTERNAL;

int
mono_debugger_remove_method_breakpoint (guint64 index) MONO_INTERNAL;

#endif /* __MONO_DEBUG_DEBUGGER_H__ */
