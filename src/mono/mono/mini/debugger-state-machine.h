/**
 * \file
 * Types for the debugger state machine and wire protocol
 *
 * Author:
 *   Alexander Kyte (alkyte@microsoft.com)
 *
 * (C) 2018 Microsoft, Inc.
 *
 */
#ifndef __MONO_UTILS_DEBUGGER_STATE_MACHINE__
#define __MONO_UTILS_DEBUGGER_STATE_MACHINE__

#include "debugger-agent.h"

#if 1
#define DEBUGGER_STATE_MACHINE_DEBUG(...)
#else
#define DEBUGGER_STATE_MACHINE_DEBUG MOSTLY_ASYNC_SAFE_PRINTF
#endif

#endif // __MONO_UTILS_DEBUGGER_STATE_MACHINE__ 
