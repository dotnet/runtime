#ifndef __DEBUG_MINI_H__
#define __DEBUG_MINI_H__

#include <mono/metadata/class-internals.h>
#include <mono/metadata/mono-debug-debugger.h>

#include "mini.h"

MonoDebugMethodAddressList *
mono_debugger_insert_method_breakpoint (MonoMethod *method, guint64 idx);

int
mono_debugger_remove_method_breakpoint (guint64 index);

typedef struct _MonoDebuggerThreadInfo MonoDebuggerThreadInfo;
extern MonoDebuggerThreadInfo *mono_debugger_thread_table;

void
mono_debugger_thread_created (gsize tid, MonoJitTlsData *jit_tls);

void
mono_debugger_thread_cleanup (MonoJitTlsData *jit_tls);

/*
 * This is the old breakpoint interface.
 * It isn't used by the debugger anymore, but still when using the `--break' command
 * line argument.
 */

int             mono_debugger_insert_breakpoint_full      (MonoMethodDesc *desc);
int             mono_debugger_remove_breakpoint           (int breakpoint_id);
void            mono_debugger_breakpoint_callback         (MonoMethod *method, guint32 idx);

#endif
