// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef __WASM_CALLHELPERS_HPP__
#define __WASM_CALLHELPERS_HPP__

#define WASM_STACKFRAME_FUNCTION_INDEX_OFFSET 0

#ifdef TARGET_64BIT
#define WASM_STACKFRAME_INDIRECT_TO_FRAMEPOINTER_OFFSET 8 // The framepointer is a pointer value, and therefore should be pointer aligned
#else
#define WASM_STACKFRAME_INDIRECT_TO_FRAMEPOINTER_OFFSET 4 // The framepointer is a pointer value, and therefore should be pointer aligned
#endif

#define WASM_STACKFRAME_VIRTUALIP_OFFSET 4 // Virtual IPs are 32bit, and will remain so even on 64bit platforms, as they are an index into the R2R function table, which is limited to 4GB of entries.

// A sentinel value to indicate to the stackwalker that a stack pointer is not a framepointer, and should
// use an indirection to find the actual frame pointer
#define STACK_WALK_INDIRECT_TO_FRAMEPOINTER 0

// A sentinel value to indicate to the stack walker that this frame is NOT R2R generated managed code,
// and it should look for the next Frame in the Frame chain to make further progress.
#define TERMINATE_R2R_STACK_WALK 1

// Within the synthetic frame established by CallFuncletWith[out]Throwable, the establishing (method)
// frame pointer of the funclet being invoked is stored immediately after the TERMINATE_R2R_STACK_WALK
// marker (which lives at offset 0). This lets the stack walker recover the establishing frame for a
// handler that is lexically nested inside a funclet that the VM invoked via CallFunclet, where native
// unwinding terminates at this synthetic frame before reaching the method's own frame.
#ifdef TARGET_64BIT
#define TERMINATE_R2R_STACK_WALK_FP_OFFSET 8 // The framepointer is a pointer value, and therefore should be pointer aligned
#else
#define TERMINATE_R2R_STACK_WALK_FP_OFFSET 4 // The framepointer is a pointer value, and therefore should be pointer aligned
#endif

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
