// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <pal_error_common.h>

/**
 * Converts the given raw numeric value obtained via errno ->
 * GetLastWin32Error() to a standard numeric value defined by enum
 * Error above. If the value is not recognized, returns
 * Error_ENONSTANDARD.
 */
PALEXPORT int32_t SystemNative_ConvertErrorPlatformToPal(int32_t platformErrno);

/**
 * Converts the given PAL Error value to a platform-specific errno
 * value. This is to be used when we want to synthesize a given error
 * and obtain the appropriate error message via StrErrorR.
 */
PALEXPORT int32_t SystemNative_ConvertErrorPalToPlatform(int32_t error);

/**
 * Obtains the system error message for the given raw numeric value
 * obtained by errno/ Marhsal.GetLastWin32Error().
 *
 * By design, this does not take a PAL errno, but a raw system errno,
 * so that:
 *
 *  1. We don't waste cycles converting back and forth (generally, if
 *     we have a PAL errno, we had a platform errno just a few
 *     instructions ago.)
 *
 *  2. We don't lose the ability to get the system error message for
 *     non-standard, platform-specific errors.
 *
 * Note that buffer may or may not be used and the error message is
 * passed back via the return value.
 *
 * If the buffer was too small to fit the full message, null is
 * returned and the buffer is filled with as much of the message
 * as possible and null-terminated.
 */
PALEXPORT const char* SystemNative_StrErrorR(int32_t platformErrno, char* buffer, int32_t bufferSize);

/**
 * Gets the current errno value
 */
PALEXPORT int32_t SystemNative_GetErrNo(void);

/**
 * Sets the errno value
 */
PALEXPORT void SystemNative_SetErrNo(int32_t errorCode);
