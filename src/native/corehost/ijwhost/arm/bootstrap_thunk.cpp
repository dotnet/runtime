// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "bootstrap_thunk.h"
#include "corhdr.h"
#include "pal.h"

//=================================================================================
// Get thunk from the address that the thunk code provided
bootstrap_thunk *bootstrap_thunk::get_thunk_from_cookie(std::uintptr_t cookie)
{

    // Cookie is generated via the first thunk instruction:
    //  mov r12, pc
    // The pc is returned from the hardware as the pc at the start of the instruction (i.e. the thunk address)
    // + 4. So we can recover the thunk address simply by subtracting 4 from the cookie.
    return (bootstrap_thunk *)(cookie - 4);
}

//=================================================================================
// Get thunk from the thunk code entry point address
bootstrap_thunk *bootstrap_thunk::get_thunk_from_entrypoint(std::uintptr_t entryAddr)
{
    // The entry point is at the start of the thunk but the code address will have the low-order bit set to
    // indicate Thumb code and we need to mask that out.
    return (bootstrap_thunk *)(entryAddr & ~1);
}

//=================================================================================
// Returns the slot address of the vtable entry for this thunk
std::uintptr_t *bootstrap_thunk::get_slot_address()
{
    return m_slot;
}

//=================================================================================
// Returns the pal::dll_t for this thunk's module
pal::dll_t bootstrap_thunk::get_dll_handle()
{
    return m_dll;
}

//=================================================================================
// Returns the token of this thunk
std::uint32_t bootstrap_thunk::get_token()
{
    return m_token;
}

//=================================================================================
std::uintptr_t bootstrap_thunk::get_entrypoint()
{
    // Set the low-order bit of the address returned to indicate to the hardware that it's Thumb code.
    return (std::uintptr_t)this | 1;
}

//=================================================================================
// Initializes the thunk to point to the bootstrap helper that will load the
// runtime and perform the real thunk initialization.
//
void bootstrap_thunk::initialize(std::uintptr_t pThunkInitFcn,
                                          pal::dll_t dll,
                                          std::uint32_t token,
                                          std::uintptr_t *pSlot)
{

    // Initialize code section of the thunk:
    WORD rgCode[] = {
        0x46fc,             // mov r12, pc
        0xf8df, 0xf004,     // ldr pc, [pc, #4]
        0x0000              // padding for 4-byte alignment of target address that follows
    };
    BYTE *pCode = (BYTE*)this;
    memcpy(pCode, rgCode, sizeof(rgCode));
    pCode += sizeof(rgCode);
    *(std::uintptr_t*)pCode = pThunkInitFcn;

    m_dll = dll;
    m_slot = pSlot;
    m_token = token;
}
