// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __NETHOST_H__
#define __NETHOST_H__

#include <cstddef>

#if defined(_WIN32)
    #ifdef NETHOST_EXPORT
        #define NETHOST_API __declspec(dllexport)
    #else
        #define NETHOST_API __declspec(dllimport)
    #endif

    #define NETHOST_CALLTYPE __stdcall
    #ifdef _WCHAR_T_DEFINED
        using char_t = wchar_t;
    #else
        using char_t = unsigned short;
    #endif
#else
    #ifdef NETHOST_EXPORT
        #define NETHOST_API __attribute__((__visibility__("default")))
    #else
        #define NETHOST_API
    #endif

    #define NETHOST_CALLTYPE
    using char_t = char;
#endif

//
// Get the path to the hostfxr library
//
// Parameters:
//   result_buffer
//     Buffer that will be populated with the hostfxr path, including a null terminator.
//
//   buffer_size
//     Size of result_buffer in char_t units
//
//   out_buffer_required_size
//     Minimum required size in char_t units for a buffer to hold the hostfxr path
//
//   assembly_path
//     Optional. Path to the compenent's assembly. Whether or not this is specified
//     determines the behaviour for locating the hostfxr library.
//     If nullptr, hostfxr is located using the enviroment variable or global registration
//     If specified, hostfxr is located as if the assembly_path is the apphost
//
// Return value:
//   0 on success, otherwise failure
//   0x80008098 - result_buffer is too small (HostApiBufferTooSmall)
//
// Remarks:
//   The full search for the hostfxr library is done on every call. To minimize the need
//   to call this function multiple times, pass a large result_buffer (e.g. PATH_MAX).
//
extern "C" NETHOST_API int NETHOST_CALLTYPE nethost_get_hostfxr_path(
    char_t * result_buffer,
    size_t buffer_size,
    size_t * out_buffer_required_size,
    const char_t * assembly_path);

#endif // __NETHOST_H__