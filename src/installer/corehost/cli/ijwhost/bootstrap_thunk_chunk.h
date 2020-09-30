// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef BOOTSTRAP_THUNK_CHUNK_H
#define BOOTSTRAP_THUNK_CHUNK_H

#include "pal.h"
#include "bootstrap_thunk.h"

#include <pshpack1.h>
class bootstrap_thunk_chunk
{
private:
    pal::dll_t                  m_dll;
    size_t                     m_numThunks;
    bootstrap_thunk_chunk *m_next;
    bootstrap_thunk       m_thunks[0];

public:
    // Ctor
    bootstrap_thunk_chunk(size_t numThunks, pal::dll_t dll);

    // Returns the bootstrap_thunk at the given index.
    bootstrap_thunk *GetThunk(size_t idx);

    // Returns the pal::dll_t for this module
    pal::dll_t get_dll_handle();

    // Linked list of thunk chunks (one per loaded module)
    bootstrap_thunk_chunk *GetNext();
    bootstrap_thunk_chunk **GetNextPtr();
    void SetNext(bootstrap_thunk_chunk *pNext);
};
#include <poppack.h>

#endif // BOOTSTRAP_THUNK_CHUNK_H
