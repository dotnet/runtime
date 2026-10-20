// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef __WASM_CALLHELPERS_HPP__
#define __WASM_CALLHELPERS_HPP__

#define WASM_STACKFRAME_FUNCTION_INDEX_OFFSET 0

// Aligned PEPs, tagged establishing frames, and odd indices occupy disjoint namespaces.
#define WASM_FRAME_IDENTITY_TAG_MASK 3
#define WASM_FRAME_IDENTITY_ESTABLISHING_FRAME_TAG 2

#ifdef TARGET_64BIT
#define WASM_STACKFRAME_INDIRECT_TO_FRAMEPOINTER_OFFSET 8 // The framepointer is a pointer value, and therefore should be pointer aligned
#else
#define WASM_STACKFRAME_INDIRECT_TO_FRAMEPOINTER_OFFSET 4 // The framepointer is a pointer value, and therefore should be pointer aligned
#endif

#define WASM_STACKFRAME_VIRTUALIP_OFFSET TARGET_POINTER_SIZE // The local virtual IP is 32-bit, in the second pointer-sized slot.

// A sentinel value to indicate to the stackwalker that a stack pointer is not a framepointer, and should
// use an indirection to find the actual frame pointer
#define STACK_WALK_INDIRECT_TO_FRAMEPOINTER 0

// A sentinel value to indicate to the stack walker that this frame is NOT R2R generated managed code,
// and it should look for the next Frame in the Frame chain to make further progress.
#define TERMINATE_R2R_STACK_WALK 1

struct StringToPortableSigThunk
{
    const char* key;
    void*       value;
};

extern const StringToPortableSigThunk g_portableCallHelperThunks[];
extern const size_t g_portableCallHelperThunksCount;

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
