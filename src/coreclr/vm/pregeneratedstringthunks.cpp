// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include "common.h"
#include "pregeneratedstringthunks.h"
#include "stringthunkhash.h"

#ifndef DACCESS_COMPILE

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

void ProcessInjectStringThunksFixup(TADDR moduleBase, PCCOR_SIGNATURE pBlob)
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
                    void* codeAddr = (void*)(moduleBase + (TADDR)rva);
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
            return;
        }

        // CAS failed, another thread updated the table. Delete our new table and retry.
        delete newTable;
    }
}

#endif // !DACCESS_COMPILE
