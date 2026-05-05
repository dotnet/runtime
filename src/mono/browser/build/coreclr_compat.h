// Auto-included CoreCLR compat header for app native build.
//
// This header is pre-included via -include when compiling pinvoke-table.cpp
// and wasm_m2n_invoke.g.cpp produced by ManagedToNativeGenerator, so those
// files can be compiled outside the full CoreCLR build context (e.g. in
// Wasm.Build.Tests on Helix where src/coreclr/vm/wasm/callhelpers.hpp is not
// part of the payload).
//
// Macro definitions consumed by generator output (NOINLINE, ARRAY_SIZE, ...)
// come from src/native/minipal/utils.h, which is shipped to the WBT Helix
// payload and force-included alongside this header by
// BrowserWasmApp.CoreCLR.targets.
//
// Definitions for the symbols declared here live in libcoreclr_static.a (which
// is linked in later) or in the same generated .cpp (e.g. g_wasmThunks /
// g_ReverseThunks tables are emitted by the generator itself).

#pragma once

#include <stddef.h>
#include <stdint.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>

// CoreCLR type stubs
#ifndef _CORECLR_COMPAT_TYPES
#define _CORECLR_COMPAT_TYPES
typedef void MethodDesc;
typedef uintptr_t PCODE;
typedef uint32_t ULONG;
#define INTERP_STACK_SLOT_SIZE 8u
#endif

// CoreCLR logging stubs
#define LF_INTEROP 0
#define LL_INFO1000 0
#define LOG(x)

// CoreCLR assertion stub
#define PORTABILITY_ASSERT(msg) do { fprintf(stderr, "PORTABILITY_ASSERT: %s\n", msg); abort(); } while(0)

// Mirrors of declarations from src/coreclr/vm/wasm/callhelpers.hpp.
#define TERMINATE_R2R_STACK_WALK 1
struct StringToWasmSigThunk { const char* key; void* value; };
extern const StringToWasmSigThunk g_wasmThunks[];
extern const size_t g_wasmThunksCount;
struct ReverseThunkMapValue { MethodDesc** Target; void* EntryPoint; };
struct ReverseThunkMapEntry { ULONG hashCode; const char* Source; ReverseThunkMapValue value; };
extern const ReverseThunkMapEntry g_ReverseThunks[];
extern const size_t g_ReverseThunksCount;

// Mirrors of declarations from src/native/minipal/entrypoints.h, used by
// pinvoke-table.cpp. Marked static inline (rather than the upstream 'static')
// so cpp files that #include this compat header but don't call the helper
// don't trigger -Wunused-function.
typedef struct { const char* name; const void* method; } Entry;
#define DllImportEntry(impl) {#impl, (void*)&impl},
static inline const void* minipal_resolve_dllimport(const Entry* resolutionTable, size_t tableLength, const char* name)
{
    for (size_t i = 0; i < tableLength; i++)
    {
        if (strcmp(name, resolutionTable[i].name) == 0)
            return resolutionTable[i].method;
    }
    return NULL;
}
