// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    pal/printfcpp.hpp

Abstract:
    Declarations for suspension safe memory allocation functions



--*/

#ifndef _PRINTFCPP_HPP
#define _PRINTFCPP_HPP

#ifdef __cplusplus
#include "pal/threadinfo.hpp"
#endif

#include <stdarg.h>

#ifdef __cplusplus
typedef char16_t wchar_16; // __wchar_16_cpp (which is defined in palinternal.h) needs to be redefined to wchar_16.

extern "C"
{
    int
    __cdecl
    PAL_vfprintf(
        PAL_FILE *stream,
        const char *format,
        va_list ap);

    int
    __cdecl
    PAL_vfwprintf(
        PAL_FILE *stream,
        const wchar_16 *format,
        va_list ap);
}

namespace CorUnix
{
    int
    InternalVfprintf(
        CPalThread *pthrCurrent,
        PAL_FILE *stream,
        const char *format,
        va_list ap);

}
#else // __cplusplus

    int
    __cdecl
    PAL_vfprintf(
        PAL_FILE *stream,
        const char *format,
        va_list ap);

    int
    __cdecl
    PAL_vfwprintf(
        PAL_FILE *stream,
        const wchar_16 *format,
        va_list ap);

#endif // __cplusplus

#endif // _PRINTFCPP_HPP

