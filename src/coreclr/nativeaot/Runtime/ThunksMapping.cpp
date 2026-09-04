// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"

#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalLimitedContext.h"
#include "CommonMacros.inl"
#include "volatile.h"
#include "Pal.h"
#include "rhassert.h"


#ifdef FEATURE_RX_THUNKS

#ifdef TARGET_AMD64
#define THUNK_SIZE  16
#elif TARGET_X86
#define THUNK_SIZE  12
#elif TARGET_ARM
#define THUNK_SIZE  8
#elif TARGET_ARM64
#define THUNK_SIZE  16
#elif TARGET_LOONGARCH64
#define THUNK_SIZE  16
#elif TARGET_RISCV64
#define THUNK_SIZE  20
#else
#define THUNK_SIZE  (2 * OS_PAGE_SIZE) // This will cause RhpGetNumThunksPerBlock to return 0
#endif

static_assert((THUNK_SIZE % 4) == 0, "Thunk stubs size not aligned correctly. This will cause runtime failures.");

// ARM32 PC-relative literal loads have a 4-KB range.
#ifdef TARGET_ARM
#define THUNKS_MAP_SIZE OS_PAGE_SIZE
#else
// 32 K or OS page
#define THUNKS_MAP_SIZE (max((size_t)0x8000, OS_PAGE_SIZE))
#endif

FCIMPL0(int, RhpGetNumThunkBlocksPerMapping)
{
    ASSERT_MSG((THUNKS_MAP_SIZE % OS_PAGE_SIZE) == 0, "Thunks map size should be in multiples of pages");

    return (int)(THUNKS_MAP_SIZE / OS_PAGE_SIZE);
}
FCIMPLEND

FCIMPL0(int, RhpGetNumThunksPerBlock)
{
    return (int)min(
        OS_PAGE_SIZE / THUNK_SIZE,                              // Number of thunks that can fit in a page
        (OS_PAGE_SIZE - POINTER_SIZE) / (POINTER_SIZE * 2)      // Number of pointer pairs, minus the jump stub cell, that can fit in a page
    );
}
FCIMPLEND

FCIMPL0(int, RhpGetThunkSize)
{
    return THUNK_SIZE;
}
FCIMPLEND

FCIMPL1(void*, RhpGetThunkDataBlockAddress, void* pThunkStubAddress)
{
    return (void*)(((uintptr_t)pThunkStubAddress & ~(OS_PAGE_SIZE - 1)) + THUNKS_MAP_SIZE);
}
FCIMPLEND

FCIMPL1(void*, RhpGetThunkStubsBlockAddress, void* pThunkDataAddress)
{
    return (void*)(((uintptr_t)pThunkDataAddress & ~(OS_PAGE_SIZE - 1)) - THUNKS_MAP_SIZE);
}
FCIMPLEND

FCIMPL0(int, RhpGetThunkBlockSize)
{
    return (int)OS_PAGE_SIZE;
}
FCIMPLEND

EXTERN_C HRESULT QCALLTYPE RhAllocateThunksMapping(void** ppThunksSection)
{
    size_t thunksMapSize = THUNKS_MAP_SIZE;

#ifdef WIN32

    void * pNewMapping = PalVirtualAlloc(thunksMapSize * 2, PAGE_READWRITE);
    if (pNewMapping == NULL)
    {
        return E_OUTOFMEMORY;
    }

    void * pThunksSection = pNewMapping;
    void * pDataSection = (uint8_t*)pNewMapping + thunksMapSize;

#else

    // Note: On secure linux systems, we can't add execute permissions to a mapped virtual memory if it was not created
    // with execute permissions in the first place. This is why we create the virtual section with RX permissions, then
    // reduce it to RW for the data section. For the stubs section we need to increase to RWX to generate the stubs
    // instructions. After this we go back to RX for the stubs section before the stubs are used and should not be
    // changed anymore.
    void * pNewMapping = PalVirtualAlloc(thunksMapSize * 2, PAGE_EXECUTE_READ);
    if (pNewMapping == NULL)
    {
        return E_OUTOFMEMORY;
    }

    void * pThunksSection = pNewMapping;
    void * pDataSection = (uint8_t*)pNewMapping + thunksMapSize;

    if (!PalVirtualProtect(pDataSection, thunksMapSize, PAGE_READWRITE) ||
        !PalVirtualProtect(pThunksSection, thunksMapSize, PAGE_EXECUTE_READWRITE))
    {
        PalVirtualFree(pNewMapping, THUNKS_MAP_SIZE * 2);
        return E_FAIL;
    }

#if defined(HOST_APPLE) && defined(HOST_ARM64)
#if defined(HOST_MACCATALYST) || defined(HOST_IOS) || defined(HOST_TVOS)
    RhFailFast(); // we don't expect to get here on these platforms
#elif defined(HOST_OSX)
    pthread_jit_write_protect_np(0);
#else
    #error "Unknown OS"
#endif
#endif
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

            // mov r10,[rip + <delta to context>]
            // jmp [rip + <delta to target>]

            *pCurrentThunkAddress++ = 0x4c;
            *pCurrentThunkAddress++ = 0x8b;
            *pCurrentThunkAddress++ = 0x15;
            *((int32_t*)pCurrentThunkAddress) =
                (int32_t)(CurrentDataAddress - (pCurrentThunkAddress + 4));
            pCurrentThunkAddress += 4;

            *pCurrentThunkAddress++ = 0xff;
            *pCurrentThunkAddress++ = 0x25;
            *((int32_t*)pCurrentThunkAddress) =
                (int32_t)(pCurrentDataAddress + POINTER_SIZE) - (pCurrentThunkAddress + 4));
            pCurrentThunkAddress += 4;

            // nops for alignment
            *pCurrentThunkAddress++ = 0x90;
            *pCurrentThunkAddress++ = 0x90;
            *pCurrentThunkAddress++ = 0x90;

#elif TARGET_X86

            // mov eax,[<context address>]
            // jmp [<target address>]

            *pCurrentThunkAddress++ = 0xa1;
            *((void **)pCurrentThunkAddress) = (void *)pCurrentDataAddress;
            pCurrentThunkAddress += 4;

            *((uint16_t*)pCurrentThunkAddress) = 0x25ff;
            pCurrentThunkAddress += 2;
            *((void **)pCurrentThunkAddress) = pCurrentDataAddress + POINTER_SIZE;
            pCurrentThunkAddress += 4;

            // nops for alignment
            *pCurrentThunkAddress++ = 0x90;

#elif TARGET_ARM

            // ldr r12,[pc + <delta to context>]
            // ldr pc,[pc + <delta to target>]

            int delta = (int)(pCurrentDataAddress - (pCurrentThunkAddress + 4));
            ASSERT((0 <= delta) && (delta <= 0xfff));
            *((uint32_t*)pCurrentThunkAddress) = 0xc000f8df | (delta << 16);
            pCurrentThunkAddress += 4;

            delta = (int)(pCurrentDataAddress + POINTER_SIZE - (pCurrentThunkAddress + 4));
            ASSERT((0 <= delta) && (delta <= 0xfff));
            *((uint32_t*)pCurrentThunkAddress) = 0xf000f8df | (delta << 16);
            pCurrentThunkAddress += 4;

#elif TARGET_ARM64

            //ldr      x10, <delta PC to target>
            //ldr      x12, <delta PC to context>
            //br       x10
            //brk      0xf000 //Stubs need to be 16 byte aligned therefore we fill with a break here

            int delta = (int)(pCurrentDataAddress + POINTER_SIZE - pCurrentThunkAddress);
            ASSERT((delta % 4) == 0);
            ASSERT((-0x100000 <= delta) && (delta < 0x100000));
            *((uint32_t*)pCurrentThunkAddress) = 0x5800000a | (((delta >> 2) & 0x7ffff) << 5);
            pCurrentThunkAddress += 4;

            delta = (int)(pCurrentDataAddress - pCurrentThunkAddress);
            ASSERT((delta % 4) == 0);
            ASSERT((-0x100000 <= delta) && (delta < 0x100000));
            *((uint32_t*)pCurrentThunkAddress) = 0x5800000c | (((delta >> 2) & 0x7ffff) << 5);
            pCurrentThunkAddress += 4;

            *((uint32_t*)pCurrentThunkAddress) = 0xd61f0140;
            pCurrentThunkAddress += 4;

            *((uint32_t*)pCurrentThunkAddress) = 0xd43e0000;
            pCurrentThunkAddress += 4;

#elif TARGET_LOONGARCH64

            //pcaddi    $t7, <delta PC to thunk data address>
            //ld.d      $t2, $t7, 0
            //ld.d      $t8, $t7, POINTER_SIZE
            //jirl      $r0, $t8, 0

            int delta = (int)(pCurrentDataAddress - pCurrentThunkAddress);
            ASSERT((-0x200000 <= delta) && (delta < 0x200000));

            *((uint32_t*)pCurrentThunkAddress) = 0x18000013 | (((delta & 0x3FFFFC) >> 2) << 5);
            pCurrentThunkAddress += 4;

            *((uint32_t*)pCurrentThunkAddress) = 0x28c0026e;
            pCurrentThunkAddress += 4;

            *((uint32_t*)pCurrentThunkAddress) = 0x28c02274;
            pCurrentThunkAddress += 4;

            *((uint32_t*)pCurrentThunkAddress) = 0x4c000280;
            pCurrentThunkAddress += 4;

#elif defined(TARGET_RISCV64)

            //auipc    t1, hi(<delta PC to thunk data address>)
            //addi     t1, t1, lo(<delta PC to thunk data address>)
            //ld       t2, 0(t1)
            //ld       t1, POINTER_SIZE(t1)
            //jalr     zero, t1, 0

            int delta = (int)(pCurrentDataAddress - pCurrentThunkAddress);
            *((uint32_t*)pCurrentThunkAddress) = 0x00000317 | ((((delta + 0x800) & 0xFFFFF000) >> 12) << 12);  // auipc t1, delta[31:12]
            pCurrentThunkAddress += 4;

            *((uint32_t*)pCurrentThunkAddress) = 0x00030313 | ((delta & 0xFFF) << 20);  // addi t1, t1, delta[11:0]
            pCurrentThunkAddress += 4;

            *((uint32_t*)pCurrentThunkAddress) = 0x00033383; // ld t2, 0(t1)
            pCurrentThunkAddress += 4;

            *((uint32_t*)pCurrentThunkAddress) = 0x00833303; // ld t1, POINTER_SIZE(t1)
            pCurrentThunkAddress += 4;

            *((uint32_t*)pCurrentThunkAddress) = 0x00030067; // jalr zero, t1, 0
            pCurrentThunkAddress += 4;

#else
            UNREFERENCED_PARAMETER(pCurrentDataAddress);
            UNREFERENCED_PARAMETER(pCurrentThunkAddress);
            PORTABILITY_ASSERT("RhAllocateThunksMapping");
#endif
        }
    }

#if defined(HOST_APPLE) && defined(HOST_ARM64)
#if defined(HOST_MACCATALYST) || defined(HOST_IOS) || defined(HOST_TVOS)
    RhFailFast(); // we don't expect to get here on these platforms
#elif defined(HOST_OSX)
    pthread_jit_write_protect_np(1);
#else
    #error "Unknown OS"
#endif
#else
    if (!PalVirtualProtect(pThunksSection, thunksMapSize, PAGE_EXECUTE_READ))
    {
        PalVirtualFree(pNewMapping, thunksMapSize * 2);
        return E_FAIL;
    }
#endif

    PalFlushInstructionCache(pThunksSection, thunksMapSize);

    *ppThunksSection = pThunksSection;
    return S_OK;
}

// FEATURE_RX_THUNKS
#elif FEATURE_FIXED_POOL_THUNKS
// This is used by the thunk code to find the stub data for the called thunk slot
extern "C" uintptr_t g_pThunkStubData;
uintptr_t g_pThunkStubData = NULL;

FCDECL0(int, RhpGetThunkBlockCount);
FCDECL0(int, RhpGetNumThunkBlocksPerMapping);
FCDECL0(int, RhpGetThunkBlockSize);
FCDECL1(void*, RhpGetThunkDataBlockAddress, void* addr);
FCDECL1(void*, RhpGetThunkStubsBlockAddress, void* addr);

EXTERN_C HRESULT QCALLTYPE RhAllocateThunksMapping(void** ppThunksSection)
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
        return E_FAIL;
    }

    if (g_pThunkStubData == NULL)
    {
        int thunkDataSize = thunkDataMappingSize * thunkDataMappingCount;

        g_pThunkStubData = (uintptr_t)VirtualAlloc(NULL, thunkDataSize, MEM_RESERVE, PAGE_READWRITE);

        if (g_pThunkStubData == NULL)
        {
            return E_OUTOFMEMORY;
        }
    }

    void* pThunkDataBlock = (int8_t*)g_pThunkStubData + nextThunkDataMapping * thunkDataMappingSize;

    if (VirtualAlloc(pThunkDataBlock, thunkDataMappingSize, MEM_COMMIT, PAGE_READWRITE) == NULL)
    {
        return E_OUTOFMEMORY;
    }

    nextThunkDataMapping++;

    void* pThunks = RhpGetThunkStubsBlockAddress(pThunkDataBlock);
    ASSERT(RhpGetThunkDataBlockAddress(pThunks) == pThunkDataBlock);

    *ppThunksSection = pThunks;
    return S_OK;
}

#else // FEATURE_FIXED_POOL_THUNKS

FCDECL0(void*, RhpGetThunksBase);
FCDECL0(int, RhpGetNumThunkBlocksPerMapping);
FCDECL0(int, RhpGetNumThunksPerBlock);
FCDECL0(int, RhpGetThunkSize);
FCDECL0(int, RhpGetThunkBlockSize);

EXTERN_C HRESULT QCALLTYPE RhAllocateThunksMapping(void** ppThunksSection)
{
    static void* pThunksTemplateAddress = NULL;

    void *pThunkMap = NULL;

    int thunkBlocksPerMapping = RhpGetNumThunkBlocksPerMapping();
    int thunkBlockSize = RhpGetThunkBlockSize();
    int templateSize = thunkBlocksPerMapping * thunkBlockSize;

#ifndef TARGET_APPLE // Apple platforms cannot use the initial template
    if (pThunksTemplateAddress == NULL)
    {
        // First, we use the thunks directly from the thunks template sections in the module until all
        // thunks in that template are used up.
        pThunksTemplateAddress = RhpGetThunksBase();
        pThunkMap = pThunksTemplateAddress;
    }
    else
#endif
    {
        // We've already used the thunks template in the module for some previous thunks, and we
        // cannot reuse it here. Now we need to create a new mapping of the thunks section in order to have
        // more thunks

        uint8_t* pModuleBase = (uint8_t*)PalGetModuleHandleFromPointer(RhpGetThunksBase());
        int templateRva = (int)((uint8_t*)RhpGetThunksBase() - pModuleBase);

        if (!PalAllocateThunksFromTemplate((HANDLE)pModuleBase, templateRva, templateSize, &pThunkMap))
            return E_OUTOFMEMORY;
    }

    if (!PalMarkThunksAsValidCallTargets(
        pThunkMap,
        RhpGetThunkSize(),
        RhpGetNumThunksPerBlock(),
        thunkBlockSize,
        thunkBlocksPerMapping))
    {
        if (pThunkMap != pThunksTemplateAddress)
            PalFreeThunksFromTemplate(pThunkMap, templateSize);

        return E_FAIL;
    }

    *ppThunksSection = pThunkMap;
    return S_OK;
}

#endif // FEATURE_RX_THUNKS
