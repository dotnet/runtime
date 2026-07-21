// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Wasm runtime-async continuation accessors.
//
// Wasm has no REG_ASYNC_CONTINUATION_RET register, so the continuation is passed in a shared
// i32 WebAssembly.Global (see WasmGlobalImports.cs and libCorerun.js). R2R codegen reads/writes
// it directly; C++ bridge code (interp <-> R2R) can't emit wasm global ops, so it uses the
// JS-supplied accessors below.
//

#ifndef _WASM_ASYNC_CONTINUATION_H
#define _WASM_ASYNC_CONTINUATION_H

#ifdef TARGET_WASM

// Load the shared `asyncContinuation` global (supplied by the wasm host).
extern "C" int32_t RuntimeAsync_LoadAsyncContinuation();

// Store the shared `asyncContinuation` global (supplied by the wasm host).
extern "C" void RuntimeAsync_StoreAsyncContinuation(int32_t value);

#endif // TARGET_WASM

#endif // _WASM_ASYNC_CONTINUATION_H
