// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __NETHOST_H__
#define __NETHOST_H__

#include <stddef.h>

#ifdef _WIN32
    #ifdef NETHOST_EXPORT
        #define NETHOST_API __declspec(dllexport)
    #else
        // Consuming the nethost as a static library
        // Shouldn't export attempt to dllimport.
        #ifdef NETHOST_USE_AS_STATIC
            #define NETHOST_API
        #else
            #define NETHOST_API __declspec(dllimport)
        #endif
    #endif

    #define NETHOST_CALLTYPE __stdcall
    #ifdef _WCHAR_T_DEFINED
        typedef wchar_t char_t;
    #else
        typedef unsigned short char_t;
    #endif
#else
    #ifdef NETHOST_EXPORT
        #define NETHOST_API __attribute__((__visibility__("default")))
    #else
        #define NETHOST_API
    #endif

    #define NETHOST_CALLTYPE
    typedef char char_t;
#endif

#ifdef __cplusplus
extern "C" {
#endif

// Parameters for get_hostfxr_path
//
// Fields:
//   size
//     Size of the struct. This is used for versioning.
//
//   assembly_path
//     Path to the component's assembly.
//     If specified, hostfxr is located as if the assembly_path is the apphost
//
//   dotnet_root
//     Path to directory containing the dotnet executable.
//     If specified, hostfxr is located as if an application is started using
//     'dotnet app.dll', which means it will be searched for under the dotnet_root
//     path and the assembly_path is ignored.
//
struct get_hostfxr_parameters {
    size_t size;
    const char_t *assembly_path;
    const char_t *dotnet_root;
};

//
// Get the path to the hostfxr library
//
// Parameters:
//   buffer
//     Buffer that will be populated with the hostfxr path, including a null terminator.
//
//   buffer_size
//     [in] Size of buffer in char_t units.
//     [out] Size of buffer used in char_t units. If the input value is too small
//           or buffer is nullptr, this is populated with the minimum required size
//           in char_t units for a buffer to hold the hostfxr path
//
//   get_hostfxr_parameters
//     Optional. Parameters that modify the behaviour for locating the hostfxr library.
//     If nullptr, hostfxr is located using the environment variable or global registration
//
// Return value:
//   0 on success, otherwise failure
//   0x80008098 - buffer is too small (HostApiBufferTooSmall)
//
// Remarks:
//   The full search for the hostfxr library is done on every call. To minimize the need
//   to call this function multiple times, pass a large buffer (e.g. PATH_MAX).
//
NETHOST_API int NETHOST_CALLTYPE get_hostfxr_path(
    char_t * buffer,
    size_t * buffer_size,
    const struct get_hostfxr_parameters *parameters);

#ifdef __cplusplus
} // extern "C"
#endif

#endif // __NETHOST_H__
