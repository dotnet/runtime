// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef __WASM_CALLHELPERS_HPP__
#define __WASM_CALLHELPERS_HPP__

struct StringToWasmSigThunk
{
    const char* key;
    void*       value;
};

extern const StringToWasmSigThunk g_wasmThunks[];
extern const size_t g_wasmThunksCount;

extern const char* g_ReverseThunkMVIDs[];
extern const size_t g_ReverseThunkMVIDsCount;

struct ReverseThunkMapValue
{
    MethodDesc** Target;
    void* EntryPoint;
    mdMethodDef Token;
    size_t MVIDIndex;
    const char* FallbackSource;
};

struct ReverseThunkMapEntry
{
    ULONG key;
    ULONG fallbackKey;
    ReverseThunkMapValue value;
};

extern const ReverseThunkMapEntry g_ReverseThunks[];
extern const size_t g_ReverseThunksCount;

#endif // __WASM_CALLHELPERS_HPP__
