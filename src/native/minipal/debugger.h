// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_IS_NATIVE_DEBUGGER_PRESENT_H
#define HAVE_MINIPAL_IS_NATIVE_DEBUGGER_PRESENT_H

#include <stdbool.h>
#include <minipal/types.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Check if the minipal can check for a native debugger.
 *
 * @return true if the minipal can check if a native debugger is attached, false otherwise.
 */
bool minipal_can_check_for_native_debugger(void);

/**
 * Check if a native debugger is attached to the current process.
 *
 * @return true if a debugger is attached, false otherwise.
 */
bool minipal_is_native_debugger_present(void);

/**
 * Send a string to the attached debugger. On Windows this is OutputDebugStringA. Elsewhere there is
 * no debugger channel, so the string is written to stderr when PAL_OUTPUTDEBUGSTRING is set.
 *
 * @param message The string to send.
 */
void minipal_output_debug_string(const char* message);

/**
 * UTF-16 version of minipal_output_debug_string. On Windows this is OutputDebugStringW.
 *
 * @param message The string to send.
 */
void minipal_output_debug_string_utf16(const CHAR16_T* message);

#ifdef __cplusplus
}
#endif

#endif // HAVE_MINIPAL_IS_NATIVE_DEBUGGER_PRESENT_H
