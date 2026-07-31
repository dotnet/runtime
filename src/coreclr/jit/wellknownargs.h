// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// clang-format off

#ifndef WELL_KNOWN_ARG
#error Define WELL_KNOWN_ARG before including this file.
#endif

// List of well-known arguments that the JIT recognizes. The columns are:
//
// - name:         The name of the WellKnownArg enum member.
// - shortName:    A short human-readable descriptor used when dumping args
// - isILArg:      True if this is an argument that can be treated as
//                 user-defined (i.e. it appears in the IL/signature).
// - addedByMorph: True if this is an argument that is added late by morph (in
//                 `AddFinalArgsAndDetermineABIInfo`).

//             name,                        shortName,        isILArg, addedByMorph
WELL_KNOWN_ARG(ThisPointer,                 "this",           true,    false)
WELL_KNOWN_ARG(VarArgsCookie,               "va cookie",      false,   false)
WELL_KNOWN_ARG(InstParam,                   "gctx",           false,   false)
WELL_KNOWN_ARG(AsyncContinuation,           "async",          false,   false)
WELL_KNOWN_ARG(RetBuffer,                   "retbuf",         false,   false)
WELL_KNOWN_ARG(PInvokeFrame,                "pinv frame",     false,   false)
WELL_KNOWN_ARG(ShiftLow,                    "shift low",      false,   false)
WELL_KNOWN_ARG(ShiftHigh,                   "shift high",     false,   false)
WELL_KNOWN_ARG(VirtualStubCell,             "vsd cell",       false,   true)
WELL_KNOWN_ARG(PInvokeCookie,               "pinv cookie",    false,   true)
WELL_KNOWN_ARG(PInvokeTarget,               "pinv tgt",       false,   true)
WELL_KNOWN_ARG(R2RIndirectionCell,          "r2r cell",       false,   true)
WELL_KNOWN_ARG(ValidateIndirectCallTarget,  "cfg tgt",        false,   false)
WELL_KNOWN_ARG(DispatchIndirectCallTarget,  "cfg tgt",        false,   false)
WELL_KNOWN_ARG(SwiftError,                  "swift error",    true,    false)
WELL_KNOWN_ARG(SwiftSelf,                   "swift self",     true,    false)
WELL_KNOWN_ARG(X86TailCallSpecialArg,       "tail call",      false,   false)
WELL_KNOWN_ARG(StackArrayLocal,             "&lcl arr",       false,   false)
WELL_KNOWN_ARG(RuntimeMethodHandle,         "meth hnd",       false,   false)
WELL_KNOWN_ARG(AsyncExecutionContext,       "exec ctx",       false,   false)
WELL_KNOWN_ARG(AsyncSynchronizationContext, "sync ctx",       false,   false)
WELL_KNOWN_ARG(WasmShadowStackPointer,      "wasm sp",        false,   false)
WELL_KNOWN_ARG(WasmPortableEntryPoint,      "wasm pep",       false,   false)

#undef WELL_KNOWN_ARG

// clang-format on
