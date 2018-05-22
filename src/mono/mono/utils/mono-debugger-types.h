/**
 * \file
 * Types for the debugger 
 *
 * Author:
 *   Alexander Kyte (alkyte@microsoft.com)
 *
 * (C) 2018 Microsoft, Inc.
 *
 */

#ifndef __MONO_UTILS_DEBUGGER_TYPES__
#define __MONO_UTILS_DEBUGGER_TYPES__

#include <mono/utils/mono-stack-unwinding.h>
#include <mono/metadata/mono-debug.h>
#include <mono/mini/mini.h>
#include <mono/mini/ee.h>
#include <mono/metadata/threads-types.h>
#include <mono/mini/debugger-engine.h>

#ifndef DISABLE_SDB

void
mono_debugger_start_single_stepping (void);

void
mono_debugger_stop_single_stepping (void);

void
mono_debugger_free_frames (StackFrame **frames, int nframes);

// Only call this function with the loader
// lock held
MonoGHashTable *
mono_debugger_get_thread_states (void);

gboolean
mono_debugger_is_disconnected (void);

gint32
mono_debugger_get_suspend_count (void);


#endif // DISABLE_SDB


#endif
