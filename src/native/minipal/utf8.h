// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_UTF8_H
#define HAVE_MINIPAL_UTF8_H

#include <minipal/utils.h>
#include <stdlib.h>
#include <stdbool.h>

#define MB_ERR_INVALID_CHARS 0x00000008
#define ERROR_NO_UNICODE_TRANSLATION 1113L
#define ERROR_INSUFFICIENT_BUFFER 122L
#define ERROR_INVALID_PARAMETER 87L

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

int minipal_utf8_to_utf16_preallocated(const char* lpSrcStr, int cchSrc, char16_t** lpDestStr, int cchDest, unsigned int dwFlags, bool treatAsLE);

int minipal_utf16_to_utf8_preallocated(const char16_t* lpSrcStr, int cchSrc, char** lpDestStr, int cchDest);

int minipal_utf8_to_utf16_allocate(const char* lpSrcStr, int cchSrc, char16_t** lpDestStr, unsigned int dwFlags, bool treatAsLE);

int minipal_utf16_to_utf8_allocate(const char16_t* lpSrcStr, int cchSrc, char** lpDestStr, bool treatAsLE);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* HAVE_MINIPAL_UTF8_H */
