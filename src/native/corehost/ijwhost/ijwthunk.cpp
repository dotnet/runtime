// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "ijwhost.h"
#include "bootstrap_thunk_chunk.h"
#include "error_codes.h"
#include "trace.h"
#include "utils.h"
#include "corhdr.h"
#include <heapapi.h>
#include <new>
#include <mutex>

#ifdef _WIN64
#define COR_VTABLE_PTRSIZED     COR_VTABLE_64BIT
#define COR_VTABLE_NOT_PTRSIZED COR_VTABLE_32BIT
#else
#define COR_VTABLE_PTRSIZED     COR_VTABLE_32BIT
#define COR_VTABLE_NOT_PTRSIZED COR_VTABLE_64BIT
#endif

namespace
{
    std::mutex g_thunkChunkLock{};
    bootstrap_thunk_chunk* g_pVtableBootstrapThunkChunkList;

    // We swallow the trace messages so we don't output to a stderr of a process that we do not own unless tracing is enabled.
    void __cdecl swallow_trace(const pal::char_t* msg)
    {
        (void)msg;
    }
}

HANDLE g_heapHandle;

bool patch_vtable_entries(PEDecoder& pe)
{
    size_t numFixupRecords;
    IMAGE_COR_VTABLEFIXUP* pFixupTable = pe.GetVTableFixups(&numFixupRecords);

    if (numFixupRecords == 0)
    {
        // If we have no fixups, no need to allocate thunks.
        return true;
    }

    size_t numThunks = 0;
    for (size_t i = 0; i < numFixupRecords; ++i)
    {
        numThunks += pFixupTable[i].Count;
    }

    size_t chunkSize = sizeof(bootstrap_thunk_chunk) + sizeof(bootstrap_thunk) * numThunks;

    void* pbChunk = HeapAlloc(g_heapHandle, 0, chunkSize);

    if (pbChunk == nullptr)
    {
        return false;
    }

    bootstrap_thunk_chunk* chunk = new (pbChunk) bootstrap_thunk_chunk(numThunks, (pal::dll_t)pe.GetBase());

    {
        std::lock_guard<std::mutex> lock(g_thunkChunkLock);
        chunk->SetNext(g_pVtableBootstrapThunkChunkList);
        g_pVtableBootstrapThunkChunkList = chunk;
    }

    trace::setup();

    error_writer_scope_t writer_scope(swallow_trace);

    size_t currentThunk = 0;
    for (size_t i = 0; i < numFixupRecords; ++i)
    {
        if (pFixupTable[i].Type & COR_VTABLE_PTRSIZED)
        {
            const BYTE** pointers = (const BYTE**)pe.GetRvaData(pFixupTable[i].RVA);

#ifdef _WIN64
            DWORD oldProtect;
            if (!VirtualProtect(pointers, (sizeof(BYTE*) * pFixupTable[i].Count), PAGE_READWRITE, &oldProtect))
            {
                trace::error(_X("Failed to change the vtfixup table from RO to R/W failed.\n"));
                return false;
            }
#endif

            for (std::uint16_t method = 0; method < pFixupTable[i].Count; method++)
            {
                mdToken tok = (mdToken)(std::uintptr_t) pointers[method];
                bootstrap_thunk* pThunk = chunk->GetThunk(currentThunk++);
                pThunk->initialize((std::uintptr_t)&start_runtime_thunk_stub,
                                    (pal::dll_t)pe.GetBase(),
                                    tok,
                                    (std::uintptr_t *)&pointers[method]);
                pointers[method] = (BYTE*)pThunk->get_entrypoint();
            }

#ifdef _WIN64
            DWORD _;
            if (!VirtualProtect(pointers, (sizeof(BYTE*) * pFixupTable[i].Count), oldProtect, &_))
            {
                trace::warning(_X("Failed to change the vtfixup table from R/W back to RO failed.\n"));
            }
#endif
        }
    }

    return true;
}

extern "C" std::uintptr_t __stdcall start_runtime_and_get_target_address(std::uintptr_t cookie)
{
    trace::setup();
    error_writer_scope_t writer_scope(swallow_trace);

    bootstrap_thunk *pThunk = bootstrap_thunk::get_thunk_from_cookie(cookie);
    load_in_memory_assembly_fn loadInMemoryAssembly;
    pal::dll_t moduleHandle = pThunk->get_dll_handle();
    pal::hresult_t status = get_load_in_memory_assembly_delegate(moduleHandle, &loadInMemoryAssembly);

    if (status != StatusCode::Success)
    {
        // If we ignore the failure to patch bootstrap thunks we will come to this same
        // function again, causing an infinite loop of "Failed to start the .NET runtime" errors.
        // As we were taken here via an entry point with arbitrary signature,
        // there's no way of returning the error code so we just throw it.

        trace::error(_X("Failed to start the .NET runtime. Error code: %#x"), status);

#pragma warning (push)
#pragma warning (disable: 4297)
        throw status;
#pragma warning (pop)
    }

    pal::string_t app_path;
    if (!pal::get_module_path(moduleHandle, &app_path))
    {
#pragma warning (push)
#pragma warning (disable: 4297)
        throw StatusCode::LibHostCurExeFindFailure;
#pragma warning (pop)
    }

    loadInMemoryAssembly(moduleHandle, app_path.c_str(), nullptr);

    std::uintptr_t thunkAddress = *(pThunk->get_slot_address());

    return thunkAddress;
}

void release_bootstrap_thunks(PEDecoder& pe)
{
    std::lock_guard<std::mutex> lock(g_thunkChunkLock);
    // Clean up the VTable thunks if they exist.
    for (bootstrap_thunk_chunk **ppCurChunk = &g_pVtableBootstrapThunkChunkList;
            *ppCurChunk != NULL;
            ppCurChunk = (*ppCurChunk)->GetNextPtr())
    {
        if ((*ppCurChunk)->get_dll_handle() == (pal::dll_t) pe.GetBase())
        {
            bootstrap_thunk_chunk *pDel = *ppCurChunk;
            *ppCurChunk = (*ppCurChunk)->GetNext();
            HeapFree(g_heapHandle, 0, pDel);
            break;
        }
    }
}


bool are_thunks_installed_for_module(pal::dll_t instance)
{
    std::lock_guard<std::mutex> lock{g_thunkChunkLock};

    bootstrap_thunk_chunk* currentChunk = g_pVtableBootstrapThunkChunkList;
    while (currentChunk != nullptr)
    {
        if (currentChunk->get_dll_handle() == instance)
        {
            return true;
        }
        currentChunk = currentChunk->GetNext();
    }

    return false;
}
