// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_IS_NATIVE_DEBUGGER_PRESENT_H
#define HAVE_MINIPAL_IS_NATIVE_DEBUGGER_PRESENT_H

#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Check if a native debugger is attached to the current process.
 *
 * @return true if a debugger is attached, false otherwise.
 */
bool minipal_is_native_debugger_present(void);

#ifdef __cplusplus
}
#endif

#endif // HAVE_MINIPAL_IS_NATIVE_DEBUGGER_PRESENT_H
