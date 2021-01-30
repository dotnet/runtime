// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal.h"
#include "bootstrap_thunk_chunk.h"
#include "corhdr.h"

//=================================================================================
// Get thunk from the address that the thunk code provided
bootstrap_thunk *bootstrap_thunk::get_thunk_from_cookie(std::uintptr_t cookie)
{

    // Cookie is generated via the first thunk instruction:
    //  adr x16, #0
    // The pc is returned from the hardware as the pc at the start of the instruction (i.e. the thunk address).
    return (bootstrap_thunk *)cookie;
}

//=================================================================================
// Get thunk from the thunk code entry point address
bootstrap_thunk *bootstrap_thunk::get_thunk_from_entrypoint(std::uintptr_t entryAddr)
{
    // The entry point is at the start of the thunk
    return (bootstrap_thunk*)entryAddr;
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
    return (std::uintptr_t)this;
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
    std::uint32_t rgCode[] = {
        0x10000010,        // adr x16, #0
        0xF9400A11,        // ldr x17, [x16, #16]
        0xD61F0220,        // br x17
        0x00000000,        // padding for 64-bit alignment
    };
    BYTE *pCode = (BYTE*)this;
    memcpy(pCode, rgCode, sizeof(rgCode));
    pCode += sizeof(rgCode);
    *(std::uintptr_t*)pCode = pThunkInitFcn;

    m_dll = dll;
    m_slot = pSlot;
    m_token = token;
}
