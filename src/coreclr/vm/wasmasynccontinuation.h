// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Wasm runtime-async continuation accessors.
//
// Wasm has no REG_ASYNC_CONTINUATION_RET register, so the continuation is passed in a shared
// i32 WebAssembly.Global owned and exported by the runtime module (see helpers.cpp) and imported
// by every R2R webcil (see libCorerun.js). R2R codegen reads/writes it directly; C++ bridge code
// (interp <-> R2R) accesses the same global through the accessors below.
//

#ifndef _WASM_ASYNC_CONTINUATION_H
#define _WASM_ASYNC_CONTINUATION_H

#ifdef TARGET_WASM

// Load the shared `asyncContinuation` global.
extern "C" int32_t RuntimeAsync_LoadAsyncContinuation();

// Store the shared `asyncContinuation` global.
extern "C" void RuntimeAsync_StoreAsyncContinuation(int32_t value);

#endif // TARGET_WASM

#endif // _WASM_ASYNC_CONTINUATION_H
