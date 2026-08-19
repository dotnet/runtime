// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Auto-included CoreCLR compat header for out-of-tree native builds.
//
// This header is pre-included via -include when compiling the callhelpers-*.cpp
// sources produced by ManagedToNativeGenerator. It provides
// only the prerequisite types/macros that the real CoreCLR headers
// (<callhelpers.hpp>, <minipal/entrypoints.h>) assume are already in scope from
// the in-tree CoreCLR PCH (vm/common.h). The generated .cpp files still
// #include the real headers directly, which are shipped alongside this header
// in the corerun test link kit and made discoverable via -I flags from the
// generated corerun-compile.rsp.
//
// Specifically:
//   * MethodDesc/PCODE/ULONG -- referenced by callhelpers.hpp without forward
//     decls (the in-tree build gets them via vm/common.h).
//   * INTERP_STACK_SLOT_SIZE -- defined in interpretershared.h in-tree; the
//     interp-to-managed file uses it but does not include that header, so the
//     kit passes the runtime's own value on the command line.
//   * LF_INTEROP/LL_INFO1000/LOG/PORTABILITY_ASSERT -- CoreCLR logging
//     primitives used by callhelpers-pinvoke.cpp.
//
// Definitions for symbols declared by <callhelpers.hpp> (g_wasmThunks,
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
// The generated tables only ever handle MethodDesc through a pointer, so an
// incomplete type is enough. It has to stay a class, as it is in the runtime,
// or the two declarations would disagree.
class MethodDesc;
typedef uintptr_t PCODE;
typedef uint32_t ULONG;
#endif

// INTERP_STACK_SLOT_SIZE is supplied on the command line from corerun-compile.rsp,
// where testkit.cmake reads it out of src/coreclr/interpreter/inc/interpretershared.h.
// Restating the value here would let it drift from the runtime's, and the generated
// interp-to-managed thunks compute stack offsets with it.
#ifndef INTERP_STACK_SLOT_SIZE
#error "INTERP_STACK_SLOT_SIZE must be supplied by the corerun test link kit (see corerun-compile.rsp)."
#endif

// CoreCLR logging stubs.
#define LF_INTEROP 0
#define LL_INFO1000 0
#define LOG(x)

// CoreCLR assertion stub.
#define PORTABILITY_ASSERT(msg) do { fprintf(stderr, "PORTABILITY_ASSERT: %s\n", msg); abort(); } while(0)
