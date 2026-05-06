// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "bootstrap_thunk.h"
#include "corhdr.h"

namespace
{
    // 49 BA 78 56 34 12 78 56 34 12 mov         r10,1234567812345678h
    // 49 BB 34 12 34 12 34 12 34 12 mov         r11,1234123412341234h
    // 41 FF E3                      jmp         r11
    BYTE mov_r10_instruction[2] = {0x49, 0xBA};
    BYTE mov_r11_instruction[2] = {0x49, 0xBB};
    BYTE jmp_r11_instruction[3] = {0x41, 0xFF, 0xE3};
}

//=================================================================================
// Get thunk from the return address that the call instruction would have pushed
bootstrap_thunk *bootstrap_thunk::get_thunk_from_cookie(std::uintptr_t cookie)
{
    return (bootstrap_thunk*)cookie;
}

//=================================================================================
//
bootstrap_thunk *bootstrap_thunk::get_thunk_from_entrypoint(std::uintptr_t entryAddr)
{
    return (bootstrap_thunk *)
        ((std::uintptr_t)entryAddr - offsetof(bootstrap_thunk, m_mov_r10));
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
    return (std::uintptr_t)&m_mov_r10[0];
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
    // Initialize the jump thunk.
    memcpy(&m_mov_r10[0], &mov_r10_instruction[0], sizeof(mov_r10_instruction));
    (*((void **)&m_val_r10[0])) = (void *)this;
    memcpy(&m_mov_r11[0], &mov_r11_instruction[0], sizeof(mov_r11_instruction));
    (*((void **)&m_val_r11[0])) = (void *)pThunkInitFcn;
    memcpy(&m_jmp_r11[0], &jmp_r11_instruction[0], sizeof(jmp_r11_instruction));

    // Fill out the rest of the info
    m_token = token;
    m_dll = dll;
    m_slot = pSlot;
}
