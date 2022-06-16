// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"

#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalRedhawkCommon.h"
#include "CommonMacros.inl"
#include "volatile.h"
#include "PalRedhawk.h"
#include "rhassert.h"


#ifdef FEATURE_RX_THUNKS

#ifdef TARGET_AMD64
#define THUNK_SIZE  20
#elif TARGET_X86
#define THUNK_SIZE  12
#elif TARGET_ARM
#define THUNK_SIZE  20
#elif TARGET_ARM64
#define THUNK_SIZE  16
#else
#define THUNK_SIZE  (2 * OS_PAGE_SIZE) // This will cause RhpGetNumThunksPerBlock to return 0
#endif

static_assert((THUNK_SIZE % 4) == 0, "Thunk stubs size not aligned correctly. This will cause runtime failures.");

#define THUNKS_MAP_SIZE 0x8000     // 32 K

#ifdef TARGET_ARM
//*****************************************************************************
//  Encode a 16-bit immediate mov/movt in ARM Thumb2 Instruction (format T2_N)
//*****************************************************************************
void EncodeThumb2Mov16(uint16_t * pCode, uint16_t value, uint8_t rDestination, bool topWord)
{
    pCode[0] = ((topWord ? 0xf2c0 : 0xf240) |
        ((value >> 12) & 0x000f) |
        ((value >> 1) & 0x0400));
    pCode[1] = (((value << 4) & 0x7000) |
        (value & 0x00ff) |
        (rDestination << 8));
}

//*****************************************************************************
//  Encode a 32-bit immediate mov in ARM Thumb2 Instruction (format T2_N)
//*****************************************************************************
void EncodeThumb2Mov32(uint16_t * pCode, uint32_t value, uint8_t rDestination)
{
    EncodeThumb2Mov16(pCode, (uint16_t)(value & 0x0000ffff), rDestination, false);
    EncodeThumb2Mov16(pCode + 2, (uint16_t)(value >> 16), rDestination, true);
}
#endif

COOP_PINVOKE_HELPER(int, RhpGetNumThunkBlocksPerMapping, ())
{
    static_assert((THUNKS_MAP_SIZE % OS_PAGE_SIZE) == 0, "Thunks map size should be in multiples of pages");

    return THUNKS_MAP_SIZE / OS_PAGE_SIZE;
}

COOP_PINVOKE_HELPER(int, RhpGetNumThunksPerBlock, ())
{
    return min(
        OS_PAGE_SIZE / THUNK_SIZE,                              // Number of thunks that can fit in a page
        (OS_PAGE_SIZE - POINTER_SIZE) / (POINTER_SIZE * 2)      // Number of pointer pairs, minus the jump stub cell, that can fit in a page
    );
}

COOP_PINVOKE_HELPER(int, RhpGetThunkSize, ())
{
    return THUNK_SIZE;
}

COOP_PINVOKE_HELPER(void*, RhpGetThunkDataBlockAddress, (void* pThunkStubAddress))
{
    return (void*)(((uintptr_t)pThunkStubAddress & ~(OS_PAGE_SIZE - 1)) + THUNKS_MAP_SIZE);
}

COOP_PINVOKE_HELPER(void*, RhpGetThunkStubsBlockAddress, (void* pThunkDataAddress))
{
    return (void*)(((uintptr_t)pThunkDataAddress & ~(OS_PAGE_SIZE - 1)) - THUNKS_MAP_SIZE);
}

COOP_PINVOKE_HELPER(int, RhpGetThunkBlockSize, ())
{
    return OS_PAGE_SIZE;
}

EXTERN_C NATIVEAOT_API void* __cdecl RhAllocateThunksMapping()
{
#ifdef WIN32

    void * pNewMapping = PalVirtualAlloc(NULL, THUNKS_MAP_SIZE * 2, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
    if (pNewMapping == NULL)
        return NULL;

    void * pThunksSection = pNewMapping;
    void * pDataSection = (uint8_t*)pNewMapping + THUNKS_MAP_SIZE;

#else

    // Note: On secure linux systems, we can't add execute permissions to a mapped virtual memory if it was not created
    // with execute permissions in the first place. This is why we create the virtual section with RX permissions, then
    // reduce it to RW for the data section. For the stubs section we need to increase to RWX to generate the stubs
    // instructions. After this we go back to RX for the stubs section before the stubs are used and should not be
    // changed anymore.
    void * pNewMapping = PalVirtualAlloc(NULL, THUNKS_MAP_SIZE * 2, MEM_RESERVE | MEM_COMMIT, PAGE_EXECUTE_READ);
    if (pNewMapping == NULL)
        return NULL;

    void * pThunksSection = pNewMapping;
    void * pDataSection = (uint8_t*)pNewMapping + THUNKS_MAP_SIZE;

    if (!PalVirtualProtect(pDataSection, THUNKS_MAP_SIZE, PAGE_READWRITE) ||
        !PalVirtualProtect(pThunksSection, THUNKS_MAP_SIZE, PAGE_EXECUTE_READWRITE))
    {
        PalVirtualFree(pNewMapping, 0, MEM_RELEASE);
        return NULL;
    }

#endif

    int numBlocksPerMap = RhpGetNumThunkBlocksPerMapping();
    int numThunksPerBlock = RhpGetNumThunksPerBlock();

    for (int m = 0; m < numBlocksPerMap; m++)
    {
        uint8_t* pDataBlockAddress = (uint8_t*)pDataSection + m * OS_PAGE_SIZE;
        uint8_t* pThunkBlockAddress = (uint8_t*)pThunksSection + m * OS_PAGE_SIZE;

        for (int i = 0; i < numThunksPerBlock; i++)
        {
            uint8_t* pCurrentThunkAddress = pThunkBlockAddress + THUNK_SIZE * i;
            uint8_t* pCurrentDataAddress = pDataBlockAddress + i * POINTER_SIZE * 2;

#ifdef TARGET_AMD64

            // mov r10,<thunk data address>
            // jmp [r10 + <delta to get to last qword in data page]

            *((uint16_t*)pCurrentThunkAddress) = 0xba49;
            pCurrentThunkAddress += 2;
            *((void **)pCurrentThunkAddress) = (void *)pCurrentDataAddress;
            pCurrentThunkAddress += 8;

            *((uint32_t*)pCurrentThunkAddress) = 0x00a2ff41;
            pCurrentThunkAddress += 3;
            *((uint32_t*)pCurrentThunkAddress) = OS_PAGE_SIZE - POINTER_SIZE - (i * POINTER_SIZE * 2);
            pCurrentThunkAddress += 4;

            // nops for alignment
            *pCurrentThunkAddress++ = 0x90;
            *pCurrentThunkAddress++ = 0x90;
            *pCurrentThunkAddress++ = 0x90;

#elif TARGET_X86

            // mov eax,<thunk data address>
            // jmp [eax + <delta to get to last dword in data page]

            *pCurrentThunkAddress++ = 0xb8;
            *((void **)pCurrentThunkAddress) = (void *)pCurrentDataAddress;
            pCurrentThunkAddress += 4;

            *((uint16_t*)pCurrentThunkAddress) = 0xa0ff;
            pCurrentThunkAddress += 2;
            *((uint32_t*)pCurrentThunkAddress) = OS_PAGE_SIZE - POINTER_SIZE - (i * POINTER_SIZE * 2);
            pCurrentThunkAddress += 4;

            // nops for alignment
            *pCurrentThunkAddress++ = 0x90;

#elif TARGET_ARM

            // mov r12,<thunk data address>
            // str r12,[sp,#-4]
            // ldr r12,[r12, <delta to get to last dword in data page]
            // bx r12

            EncodeThumb2Mov32((uint16_t*)pCurrentThunkAddress, (uint32_t)pCurrentDataAddress, 12);
            pCurrentThunkAddress += 8;

            *((uint32_t*)pCurrentThunkAddress) = 0xcc04f84d;
            pCurrentThunkAddress += 4;

            *((uint32_t*)pCurrentThunkAddress) = 0xc000f8dc | ((OS_PAGE_SIZE - POINTER_SIZE - (i * POINTER_SIZE * 2)) << 16);
            pCurrentThunkAddress += 4;

            *((uint16_t*)pCurrentThunkAddress) = 0x4760;
            pCurrentThunkAddress += 2;

            // nops for alignment
            *((uint16_t*)pCurrentThunkAddress) = 0xbf00;
            pCurrentThunkAddress += 2;

#elif TARGET_ARM64

            //adr      xip0, <delta PC to thunk data address>
            //ldr      xip1, [xip0, <delta to get to last qword in data page>]
            //br       xip1
            //brk      0xf000 //Stubs need to be 16 byte aligned therefore we fill with a break here

            int delta = (int)(pCurrentDataAddress - pCurrentThunkAddress);
            *((uint32_t*)pCurrentThunkAddress) = 0x10000010 | (((delta & 0x03) << 29) | (((delta & 0x1FFFFC) >> 2) << 5));
            pCurrentThunkAddress += 4;

            *((uint32_t*)pCurrentThunkAddress) = 0xF9400211 | (((OS_PAGE_SIZE - POINTER_SIZE - (i * POINTER_SIZE * 2)) / 8) << 10);
            pCurrentThunkAddress += 4;

            *((uint32_t*)pCurrentThunkAddress) = 0xD61F0220;
            pCurrentThunkAddress += 4;

            *((uint32_t*)pCurrentThunkAddress) = 0xD43E0000;
            pCurrentThunkAddress += 4;
#else
            UNREFERENCED_PARAMETER(pCurrentDataAddress);
            UNREFERENCED_PARAMETER(pCurrentThunkAddress);
            PORTABILITY_ASSERT("RhAllocateThunksMapping");
#endif
        }
    }

    if (!PalVirtualProtect(pThunksSection, THUNKS_MAP_SIZE, PAGE_EXECUTE_READ))
    {
        PalVirtualFree(pNewMapping, 0, MEM_RELEASE);
        return NULL;
    }

    return pThunksSection;
}

// FEATURE_RX_THUNKS
#elif FEATURE_FIXED_POOL_THUNKS
// This is used by the thunk code to find the stub data for the called thunk slot
extern "C" uintptr_t g_pThunkStubData;
uintptr_t g_pThunkStubData = NULL;

COOP_PINVOKE_HELPER(int, RhpGetThunkBlockCount, ());
COOP_PINVOKE_HELPER(int, RhpGetNumThunkBlocksPerMapping, ());
COOP_PINVOKE_HELPER(int, RhpGetThunkBlockSize, ());
COOP_PINVOKE_HELPER(void*, RhpGetThunkDataBlockAddress, (void* addr));
COOP_PINVOKE_HELPER(void*, RhpGetThunkStubsBlockAddress, (void* addr));

EXTERN_C NATIVEAOT_API void* __cdecl RhAllocateThunksMapping()
{
    static int nextThunkDataMapping = 0;

    int thunkBlocksPerMapping = RhpGetNumThunkBlocksPerMapping();
    int thunkBlockSize = RhpGetThunkBlockSize();
    int blockCount = RhpGetThunkBlockCount();

    ASSERT(blockCount % thunkBlocksPerMapping == 0)

    int thunkDataMappingSize = thunkBlocksPerMapping * thunkBlockSize;
    int thunkDataMappingCount = blockCount / thunkBlocksPerMapping;

    if (nextThunkDataMapping == thunkDataMappingCount)
    {
        return NULL;
    }

    if (g_pThunkStubData == NULL)
    {
        int thunkDataSize = thunkDataMappingSize * thunkDataMappingCount;

        g_pThunkStubData = (uintptr_t)PalVirtualAlloc(NULL, thunkDataSize, MEM_RESERVE, PAGE_READWRITE);

        if (g_pThunkStubData == NULL)
        {
            return NULL;
        }
    }

    void* pThunkDataBlock = (int8_t*)g_pThunkStubData + nextThunkDataMapping * thunkDataMappingSize;

    if (PalVirtualAlloc(pThunkDataBlock, thunkDataMappingSize, MEM_COMMIT, PAGE_READWRITE) == NULL)
    {
        return NULL;
    }

    nextThunkDataMapping++;

    void* pThunks = RhpGetThunkStubsBlockAddress(pThunkDataBlock);
    ASSERT(RhpGetThunkDataBlockAddress(pThunks) == pThunkDataBlock);

    return pThunks;
}

#else // FEATURE_FIXED_POOL_THUNKS

COOP_PINVOKE_HELPER(void*, RhpGetThunksBase, ());
COOP_PINVOKE_HELPER(int, RhpGetNumThunkBlocksPerMapping, ());
COOP_PINVOKE_HELPER(int, RhpGetNumThunksPerBlock, ());
COOP_PINVOKE_HELPER(int, RhpGetThunkSize, ());
COOP_PINVOKE_HELPER(int, RhpGetThunkBlockSize, ());

EXTERN_C NATIVEAOT_API void* __cdecl RhAllocateThunksMapping()
{
    static void* pThunksTemplateAddress = NULL;

    void *pThunkMap = NULL;

    int thunkBlocksPerMapping = RhpGetNumThunkBlocksPerMapping();
    int thunkBlockSize = RhpGetThunkBlockSize();
    int templateSize = thunkBlocksPerMapping * thunkBlockSize;

    if (pThunksTemplateAddress == NULL)
    {
        // First, we use the thunks directly from the thunks template sections in the module until all
        // thunks in that template are used up.
        pThunksTemplateAddress = RhpGetThunksBase();
        pThunkMap = pThunksTemplateAddress;
    }
    else
    {
        // We've already used the thunks template in the module for some previous thunks, and we
        // cannot reuse it here. Now we need to create a new mapping of the thunks section in order to have
        // more thunks

        uint8_t* pModuleBase = (uint8_t*)PalGetModuleHandleFromPointer(pThunksTemplateAddress);
        int templateRva = (int)((uint8_t*)RhpGetThunksBase() - pModuleBase);

        if (!PalAllocateThunksFromTemplate((HANDLE)pModuleBase, templateRva, templateSize, &pThunkMap))
            return NULL;
    }

    if (!PalMarkThunksAsValidCallTargets(
        pThunkMap,
        RhpGetThunkSize(),
        RhpGetNumThunksPerBlock(),
        thunkBlockSize,
        thunkBlocksPerMapping))
    {
        if (pThunkMap != pThunksTemplateAddress)
            PalFreeThunksFromTemplate(pThunkMap);

        return NULL;
    }

    return pThunkMap;
}

#endif // FEATURE_RX_THUNKS
