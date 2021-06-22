/**
 * \file
 */

#ifndef __MONO_DEBUGGER_AGENT_COMPONENT_H__
#define __MONO_DEBUGGER_AGENT_COMPONENT_H__

#include <mono/mini/mini.h>
#include "debugger.h"
#include <mono/utils/mono-stack-unwinding.h>

typedef struct _DebuggerTlsData DebuggerTlsData;

void
debugger_agent_add_function_pointers (MonoComponentDebugger* fn_table);

#endif
