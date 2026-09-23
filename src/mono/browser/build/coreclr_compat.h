// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Auto-included CoreCLR compat header for app native build.
//
// This header is pre-included via -include when compiling the callhelpers-*.cpp
// produced by crossgen2's portable call-helpers generator. It provides
// only the prerequisite types/macros that the real CoreCLR headers
// (<callhelpers.hpp>, <minipal/entrypoints.h>) assume are already in scope from
// the in-tree CoreCLR PCH (vm/common.h). The generated .cpp files still
// #include the real headers directly, which are shipped to the WBT Helix
// payload by sendtohelix-browser.targets and made discoverable via -I flags
// from BrowserWasmApp.CoreCLR.targets.
//
// Specifically:
//   * MethodDesc/PCODE/ULONG -- referenced by callhelpers.hpp without forward
//     decls (the in-tree build gets them via vm/common.h).
//   * INTERP_STACK_SLOT_SIZE -- defined in interpretershared.h in-tree; the
//     interp-to-managed file uses it but does not include that header.
//   * LF_INTEROP/LL_INFO1000/LOG/PORTABILITY_ASSERT -- CoreCLR logging
//     primitives used by callhelpers-pinvoke.cpp.
//   * VolatileLoad/VolatileStore -- declared in inc/volatile.h in-tree; the
//     reverse-thunk file uses them to publish its cached R2R entrypoint but
//     does not include that header. wasm is single-threaded, so the in-tree
//     memory-barrier machinery collapses to a plain volatile access here.
//
// Definitions for symbols declared by <callhelpers.hpp> (g_portableCallHelperThunks,
// g_ReverseThunks, ...) live in libcoreclr_static.a or in the same generated
// .cpp (the generator emits the table bodies).

#pragma once

#include <stddef.h>
#include <stdint.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>

// CoreCLR type prereqs for <callhelpers.hpp>.
#ifndef _CORECLR_COMPAT_TYPES
#define _CORECLR_COMPAT_TYPES
typedef void MethodDesc;
typedef uintptr_t PCODE;
typedef uint32_t ULONG;
#define INTERP_STACK_SLOT_SIZE 8u
#endif

// CoreCLR volatile access helpers (inc/volatile.h). wasm is single-threaded,
// so these reduce to a plain volatile load/store with no memory barrier.
#ifdef __cplusplus
template<typename T>
inline T VolatileLoad(T const * pt)
{
    return *(T volatile const *)pt;
}

template<typename T>
inline void VolatileStore(T* pt, T val)
{
    *(T volatile *)pt = val;
}
#endif // __cplusplus

// CoreCLR logging stubs.
#define LF_INTEROP 0
#define LL_INFO1000 0
#define LOG(x)

// CoreCLR assertion stub.
#define PORTABILITY_ASSERT(msg) do { fprintf(stderr, "PORTABILITY_ASSERT: %s\n", msg); abort(); } while(0)
