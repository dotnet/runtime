// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include "common.h"
#include "pregeneratedstringthunks.h"

#ifndef DACCESS_COMPILE

#ifdef TARGET_WASM

#include "stringthunkhash.h"
#include "loaderallocator.hpp"
#include "wasm/helpers.hpp"
#include "precode_portable.hpp"

static StringToThunkHash* s_pPregeneratedStringThunks = nullptr;

void InitializePregeneratedStringThunkHash()
{
    WRAPPER_NO_CONTRACT;

    StringToThunkHash* newTable = new StringToThunkHash();
    s_pPregeneratedStringThunks = newTable;
}

PCODE LookupPregeneratedThunkByString(const char* str)
{
    WRAPPER_NO_CONTRACT;

    StringToThunkHash* table = VolatileLoadWithoutBarrier(&s_pPregeneratedStringThunks);
    if (table == nullptr)
        return NULL;

    void* thunk;
    if (table->Lookup(str, &thunk))
        return (PCODE)(size_t)thunk;

    return NULL;
}

void ProcessInjectStringThunksFixup(ReadyToRunInfo * pR2RInfo, PCCOR_SIGNATURE pBlob)
{
    STANDARD_VM_CONTRACT;

    // Retry loop: if another thread races us to update the global hash, we retry.
    while (true)
    {
        StringToThunkHash* existingTable = VolatileLoadWithoutBarrier(&s_pPregeneratedStringThunks);
        _ASSERTE(existingTable != nullptr);

        // First pass: check if there are any new strings not already in the table.
        bool hasNewEntries = false;
        {
            PCCOR_SIGNATURE pCurrent = pBlob;
            while (true)
            {
                const char* str = (const char*)pCurrent;
                if (*str == '\0')
                    break;

                // Advance past the null-terminated string
                pCurrent += strlen(str) + 1;
                // Skip the 4-byte RVA/table index
                pCurrent += 4;

                void* unused;
                if (!existingTable->Lookup(str, &unused))
                {
                    hasNewEntries = true;
                    break;
                }
            }
        }

        if (!hasNewEntries)
            return;

#ifdef TARGET_WASM
        WebcilDecoder decoder;
        decoder.Init(dac_cast<void*>(pR2RInfo->GetImage()->GetBase()), pR2RInfo->GetImage()->GetVirtualSize());
        TADDR rvaBase = decoder.GetTableBaseOffset();
#else
        TADDR rvaBase = dac_cast<TADDR>(pR2RInfo->GetImage()->GetBase());
#endif

        // Build a new table with all existing entries plus new ones.
        StringToThunkHash* newTable = new StringToThunkHash();

        // Copy existing entries
        for (auto iter = existingTable->Begin(); iter != existingTable->End(); ++iter)
        {
            newTable->Add((*iter).Key(), (*iter).Value());
        }

        // Add new entries (existing entries take precedence)
        {
            PCCOR_SIGNATURE pCurrent = pBlob;
            while (true)
            {
                const char* str = (const char*)pCurrent;
                if (*str == '\0')
                    break;

                // Advance past the null-terminated string
                pCurrent += strlen(str) + 1;

                // Read the 4-byte RVA (or WASM table index)
                DWORD rva = GET_UNALIGNED_VAL32(pCurrent);
                pCurrent += 4;

                void* unused;
                if (!newTable->Lookup(str, &unused))
                {
                    void* codeAddr = (void*)(rvaBase + (TADDR)rva);
                    newTable->Add(str, codeAddr);
                }
            }
        }

        // Try to swap in the new table. If CAS fails, another thread updated first - retry.
        StringToThunkHash* previous = InterlockedCompareExchangeT(&s_pPregeneratedStringThunks, newTable, existingTable);
        if (previous == existingTable)
        {
            // Success. The old table is intentionally leaked - it may still be in use
            // by concurrent readers via VolatileLoadWithoutBarrier.
#ifdef FEATURE_PORTABLE_ENTRYPOINTS
            ResolvePendingPortableEntryPointThunksGlobal();
#endif // FEATURE_PORTABLE_ENTRYPOINTS
            return;
        }

        // CAS failed, another thread updated the table. Delete our new table and retry.
        delete newTable;
    }
}

// =====================================================================
// Pending portable entrypoint thunk resolution
// =====================================================================
#ifdef FEATURE_PORTABLE_ENTRYPOINTS

// s_pendingThunkResolutionLock protects BOTH the global LA list AND the
// per-LoaderAllocator m_pendingPortableEntryPointThunks arrays. This avoids
// any lock-ordering issues and keeps LAs alive during the scan (Destroy
// takes the same lock to unregister).

static CrstStatic s_pendingThunkResolutionLock;
static SArray<LoaderAllocator*> s_pendingThunkLoaderAllocators;

void InitializePendingThunkResolutionLock()
{
    WRAPPER_NO_CONTRACT;
    s_pendingThunkResolutionLock.Init(CrstLeafLock, CRST_DEFAULT);
}

void AddPendingPortableEntryPointThunkUnderLock(LoaderAllocator* pLoaderAllocator, MethodDesc* pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    CrstHolder holder(&s_pendingThunkResolutionLock);

    pLoaderAllocator->m_pendingPortableEntryPointThunks.Append(pMD);

    if (pMD->IsDynamicMethod())
    {
        pMD->AsDynamicMethodDesc()->InterlockedSetFlags(DynamicMethodDesc::FlagPendingThunkResolution);
    }

    if (!pLoaderAllocator->m_registeredForPendingThunkResolution)
    {
        s_pendingThunkLoaderAllocators.Append(pLoaderAllocator);
        pLoaderAllocator->m_registeredForPendingThunkResolution = true;
    }
}

void UnregisterLoaderAllocatorForPendingThunkResolution(LoaderAllocator* pLoaderAllocator)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    CrstHolder holder(&s_pendingThunkResolutionLock);

    if (!pLoaderAllocator->m_registeredForPendingThunkResolution)
        return;

    // Remove from the global array by finding and replacing with the last element.
    COUNT_T count = s_pendingThunkLoaderAllocators.GetCount();
    for (COUNT_T i = 0; i < count; i++)
    {
        if (s_pendingThunkLoaderAllocators[i] == pLoaderAllocator)
        {
            if (i < count - 1)
            {
                s_pendingThunkLoaderAllocators[i] = s_pendingThunkLoaderAllocators[count - 1];
            }
            s_pendingThunkLoaderAllocators.SetCount(count - 1);
            break;
        }
    }

    pLoaderAllocator->m_registeredForPendingThunkResolution = false;
    pLoaderAllocator->m_pendingPortableEntryPointThunks.Clear();
}

void ResolvePendingPortableEntryPointThunksGlobal()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    CrstHolder holder(&s_pendingThunkResolutionLock);

    COUNT_T laCount = s_pendingThunkLoaderAllocators.GetCount();
    for (COUNT_T laIdx = 0; laIdx < laCount; laIdx++)
    {
        LoaderAllocator* pLA = s_pendingThunkLoaderAllocators[laIdx];
        SArray<MethodDesc*>& pending = pLA->m_pendingPortableEntryPointThunks;
        COUNT_T count = pending.GetCount();
        COUNT_T nullCount = 0;

        for (COUNT_T i = 0; i < count; i++)
        {
            MethodDesc* pMD = pending[i];
            if (pMD == nullptr)
            {
                nullCount++;
                continue;
            }

            void* thunk = GetPortableEntryPointToInterpreterThunk(pMD);
            if (thunk != nullptr)
            {
                PCODE portableEntry = pMD->GetPortableEntryPointIfExists();
                if (portableEntry != (PCODE)NULL)
                {
                    PortableEntryPoint* pep = PortableEntryPoint::ToPortableEntryPoint(portableEntry);
                    pep->TrySetInterpreterThunk(thunk);
                }
                pending[i] = nullptr;
                nullCount++;

                if (pMD->IsDynamicMethod())
                {
                    pMD->AsDynamicMethodDesc()->InterlockedClearFlags(DynamicMethodDesc::FlagPendingThunkResolution);
                }
            }
        }

        // Compact: move non-null entries to the front, then truncate.
        if (nullCount > 0 && nullCount < count)
        {
            COUNT_T dest = 0;
            for (COUNT_T src = 0; src < count; src++)
            {
                if (pending[src] != nullptr)
                {
                    pending[dest] = pending[src];
                    dest++;
                }
            }
            pending.SetCount(dest);
        }
        else if (nullCount == count)
        {
            pending.Clear();
        }
    }
}

#endif // FEATURE_PORTABLE_ENTRYPOINTS

#else // !TARGET_WASM

void InitializePregeneratedStringThunkHash()
{
    LIMITED_METHOD_CONTRACT;
}

void ProcessInjectStringThunksFixup(ReadyToRunInfo * pR2RInfo, PCCOR_SIGNATURE pBlob)
{
    LIMITED_METHOD_CONTRACT;
}

#endif // TARGET_WASM

#endif // !DACCESS_COMPILE
