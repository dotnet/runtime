// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal.h"
#include "corhdr.h"
#include "bootstrap_thunk_chunk.h"

//=================================================================================
// Get thunk from the return address that the call instruction would have pushed
bootstrap_thunk *bootstrap_thunk::get_thunk_from_cookie(std::uintptr_t cookie)
{
    return (bootstrap_thunk *)(cookie - (offsetof(bootstrap_thunk, m_code) + sizeof(bootstrap_thunk::m_code)));
}

//=================================================================================
//
bootstrap_thunk *bootstrap_thunk::get_thunk_from_entrypoint(std::uintptr_t entryAddr)
{
    return (bootstrap_thunk *)(entryAddr - offsetof(bootstrap_thunk, m_code));
}

//=================================================================================
// Returns the slot address of the vtable entry for this thunk
std::uintptr_t *bootstrap_thunk::get_slot_address()
{
    return (std::uintptr_t *)((std::uintptr_t)m_slot & ~1);
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
    return (std::uintptr_t)this + offsetof(bootstrap_thunk, m_code);
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
    
    m_token = token;

    // Now set up the thunk code
    std::uintptr_t pFrom;
    std::uintptr_t pTo;
    
    // This is the call to the thunk bootstrapper function
    pFrom = (std::uintptr_t)&m_code.m_thunkFcn + sizeof(m_code.m_thunkFcn);
    pTo = pThunkInitFcn;
    m_code.m_call            = 0xe8; // 0xe8 is the hex encoding of the x86 call instruction opcode
    m_code.m_thunkFcn        = (UINT32)(pTo - pFrom);

    // Fill out the rest of the info
    m_dll = dll;
    m_slot = pSlot;
}

