// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef __WASM_CALLHELPERS_HPP__
#define __WASM_CALLHELPERS_HPP__

// A sentinel value to indicate to the stack walker that this frame is NOT R2R generated managed code,
// and it should look for the next Frame in the Frame chain to make further progress.
#define TERMINATE_R2R_STACK_WALK 1

// Within the synthetic frame established by CallFuncletWith[out]Throwable, the establishing (method)
// frame pointer of the funclet being invoked is stored immediately after the TERMINATE_R2R_STACK_WALK
// marker (which lives at offset 0). This lets the stack walker recover the establishing frame for a
// handler that is lexically nested inside a funclet that the VM invoked via CallFunclet, where native
// unwinding terminates at this synthetic frame before reaching the method's own frame.
#define TERMINATE_R2R_STACK_WALK_FP_OFFSET 4

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
