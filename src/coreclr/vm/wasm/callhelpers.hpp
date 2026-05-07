// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef __WASM_CALLHELPERS_HPP__
#define __WASM_CALLHELPERS_HPP__

// A sentinel value to indicate to the stack walker that this frame is NOT R2R generated managed code,
// and it should look for the next Frame in the Frame chain to make further progress.
#define TERMINATE_R2R_STACK_WALK 1

struct StringToWasmSigThunk
{
    const char* key;
    void*       value;
};

extern const StringToWasmSigThunk g_wasmThunks[];
extern const size_t g_wasmThunksCount;

struct ReverseThunkMapValue
{
    MethodDesc** Target;
    void* EntryPoint;
};

struct ReverseThunkMapEntry
{
    ULONG hashCode;
    const char* Source;
    ReverseThunkMapValue value;
};

extern const ReverseThunkMapEntry g_ReverseThunks[];
extern const size_t g_ReverseThunksCount;

#endif // __WASM_CALLHELPERS_HPP__
