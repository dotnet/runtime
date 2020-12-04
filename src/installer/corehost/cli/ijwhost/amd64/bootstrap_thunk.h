// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef IJW_BOOTSTRAP_THUNK_H
#define IJW_BOOTSTRAP_THUNK_H

#if !defined(TARGET_AMD64)
#error "This file should only be included on amd64 builds."
#endif

#include "pal.h"
#include "corhdr.h"

extern "C" void start_runtime_thunk_stub();

#include <pshpack1.h>
class bootstrap_thunk
{
private:

    BYTE                    m_mov_r10[2];
    BYTE                    m_val_r10[8];
    BYTE                    m_mov_r11[2];
    BYTE                    m_val_r11[8];
    BYTE                    m_jmp_r11[3];
    BYTE                    m_padding[1];
    // Data for the thunk
    std::uint32_t           m_token;
    pal::dll_t              m_dll;
    std::uintptr_t          *m_slot;
public:
    // Get thunk from the return address that the call instruction would have pushed
    static bootstrap_thunk *get_thunk_from_cookie(std::uintptr_t cookie);

    // Get thunk from the return address that the call instruction would have pushed
    static bootstrap_thunk *get_thunk_from_entrypoint(std::uintptr_t entryAddr);

    // Initializes the thunk to point to pThunkInitFcn that will load the
    // runtime and perform the real thunk initialization.
    void initialize(std::uintptr_t pThunkInitFcn,
                    pal::dll_t dll,
                    std::uint32_t token,
                    std::uintptr_t *pSlot);

    // Returns the slot address of the vtable entry for this thunk
    std::uintptr_t *get_slot_address();

    // Returns the pal::dll_t for this thunk's module
    pal::dll_t get_dll_handle();

    // Returns the token of this thunk
    std::uint32_t get_token();

    std::uintptr_t get_entrypoint();
};
#include <poppack.h>

#endif
