// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_UTILS_H
#define HAVE_MINIPAL_UTILS_H

#define ARRAY_SIZE(arr) (sizeof(arr)/sizeof(arr[0]))

// Number of characters in a string literal. Excludes terminating NULL.
#define STRING_LENGTH(str) (ARRAY_SIZE(str) - 1)

#ifndef __has_builtin
#define __has_builtin(x) 0
#endif

#ifndef __has_attribute
#define __has_attribute(x) 0
#endif

#ifdef __cplusplus
#  ifndef __has_cpp_attribute
#    define __has_cpp_attribute(x) 0
#  endif
#  if __has_cpp_attribute(fallthrough)
#    define FALLTHROUGH [[fallthrough]]
#  else
#    define FALLTHROUGH
#  endif
#elif __has_attribute(fallthrough)
#  define FALLTHROUGH __attribute__((fallthrough))
#else
#  define FALLTHROUGH
#endif

#if defined(_MSC_VER)
#define LIBC_CALLBACK __cdecl
#else
#define LIBC_CALLBACK
#endif

#if defined(_MSC_VER)
#  if defined(__SANITIZE_ADDRESS__)
#    define HAS_ADDRESS_SANITIZER
#    define DISABLE_ASAN __declspec(no_sanitize_address)
#  else
#    define DISABLE_ASAN
#  endif
#elif defined(__has_feature)
#  if __has_feature(address_sanitizer)
#    define HAS_ADDRESS_SANITIZER
#    define DISABLE_ASAN __attribute__((no_sanitize("address")))
#  else
#    define DISABLE_ASAN
#  endif
#else
#    define DISABLE_ASAN
#endif

#if defined(_MSC_VER)
#  ifdef SANITIZER_SHARED_RUNTIME
#    define SANITIZER_CALLBACK_CALLCONV __declspec(dllexport no_sanitize_address) __cdecl
#    define SANITIZER_INTERFACE_CALLCONV __declspec(dllimport) __cdecl
#  else
#    define SANITIZER_CALLBACK_CALLCONV __declspec(no_sanitize_address) __cdecl
#    define SANITIZER_INTERFACE_CALLCONV __cdecl
#  endif
#else
#  ifdef SANITIZER_SHARED_RUNTIME
#    define SANITIZER_CALLBACK_CALLCONV __attribute__((no_address_safety_analysis)) __attribute__((visibility("default")))
#  else
#    define SANITIZER_CALLBACK_CALLCONV __attribute__((no_address_safety_analysis))
#  endif
#    define SANITIZER_INTERFACE_CALLCONV
#endif

#if defined(HAS_ADDRESS_SANITIZER)
#  ifdef __cplusplus
   extern "C"
   {
#  endif
      void SANITIZER_INTERFACE_CALLCONV __asan_handle_no_return(void);
#  ifdef __cplusplus
   }
#  endif
#elif defined(__llvm__)
#  pragma clang diagnostic push
#  ifdef COMPILER_SUPPORTS_W_RESERVED_IDENTIFIER
#    pragma clang diagnostic ignored "-Wreserved-identifier"
#  endif
    // Stub out a dummy implmentation when asan isn't enabled.
    inline void __asan_handle_no_return(void);
    inline void __asan_handle_no_return(void){}
#  pragma clang diagnostic pop
#else
    // Use a macro for GCC since __asan_handle_no_return is always available as a built-in on GCC
    #define __asan_handle_no_return()
#endif

#endif // HAVE_MINIPAL_UTILS_H
