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


void            mono_debugger_initialize                    (void);

void            mono_debugger_lock                          (void);
void            mono_debugger_unlock                        (void);

gchar *
mono_debugger_check_runtime_version (const char *filename);

#endif /* __MONO_DEBUG_DEBUGGER_H__ */
