// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "bootstrap_thunk_chunk.h"

//=================================================================================
// Ctor
bootstrap_thunk_chunk::bootstrap_thunk_chunk(size_t numThunks, pal::dll_t dll)
    : m_dll(dll), m_numThunks(numThunks), m_next(nullptr)
{
#ifdef _DEBUG
    memset(m_thunks, 0, m_numThunks * sizeof(bootstrap_thunk));
#endif
}

//=================================================================================
// Returns the bootstrap_thunk at the given index.
bootstrap_thunk *bootstrap_thunk_chunk::GetThunk(size_t idx)
{
    return (bootstrap_thunk *)((std::uintptr_t)m_thunks + (idx * sizeof(bootstrap_thunk)));
}

//=================================================================================
// Returns the pal::dll_t for this module
pal::dll_t bootstrap_thunk_chunk::get_dll_handle()
{
    return m_dll;
}

//=================================================================================
//
bootstrap_thunk_chunk *bootstrap_thunk_chunk::GetNext()
{
    return m_next;
}

//=================================================================================
//
bootstrap_thunk_chunk **bootstrap_thunk_chunk::GetNextPtr()
{
    return &m_next;
}

//=================================================================================
//
void bootstrap_thunk_chunk::SetNext(bootstrap_thunk_chunk *pNext)
{
    m_next = pNext;
}
