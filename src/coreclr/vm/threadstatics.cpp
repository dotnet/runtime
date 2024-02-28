// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"
#include "threadstatics.h"

struct TLSArray
{
    int32_t cTLSData; // Size in bytes of offset into the TLS array which is valid
    TADDR pTLSArrayData; // Points at the Thread local array data.
};
typedef DPTR(TLSArray) PTR_TLSArray;

// Used to store access to TLS data for a single index when the TLS is accessed while the class constructor is running
struct InFlightTLSData;
typedef DPTR(InFlightTLSData) PTR_InFlightTLSData;
struct InFlightTLSData
{
#ifndef DACCESS_COMPILE
    InFlightTLSData(TLSIndex index, TADDR pTLSData) : pNext(NULL), tlsIndex(index), pTLSData(pTLSData) { }
#endif // !DACCESS_COMPILE
    PTR_InFlightTLSData pNext; // Points at the next in-flight TLS data
    TLSIndex tlsIndex; // The TLS index for the static
    TADDR pTLSData; // The TLS data for the static
};


struct ThreadLocalLoaderAllocator
{
    ThreadLocalLoaderAllocator* pNext; // Points at the next thread local loader allocator
    LoaderAllocator* pLoaderAllocator; // The loader allocator that has a TLS used in this thread
};
typedef DPTR(ThreadLocalLoaderAllocator) PTR_ThreadLocalLoaderAllocator;

struct ThreadLocalData
{
    TLSArray tlsArray; // TLS data
    Thread *pThread;
    PTR_InFlightTLSData pInFlightData; // Points at the in-flight TLS data (TLS data that exists before the class constructor finishes running)
};

// This can be used for out of thread access to TLS data. Since that isn't safe in general, we only support it for DAC.
PTR_VOID GetThreadLocalStaticBaseNoCreate(PTR_ThreadLocalData pThreadLocalData, TLSIndex index)
{
    LIMITED_METHOD_CONTRACT;
    TADDR pTLSBaseAddress = NULL;
    PTR_TLSArray pTLSArray = dac_cast<PTR_TLSArray>(dac_cast<PTR_BYTE>(pThreadLocalData) + index.GetTLSArrayOffset());

    int32_t cTLSData = pTLSArray->cTLSData;
    if (cTLSData < index.GetByteIndex())
    {
        return NULL;
    }

    TADDR pTLSArrayData = pTLSArray->pTLSArrayData;
    pTLSBaseAddress = *dac_cast<PTR_TADDR>(dac_cast<PTR_BYTE>(pTLSArrayData) + index.GetByteIndex());
    if (pTLSBaseAddress == NULL)
    {
        // Maybe it is in the InFlightData
        PTR_InFlightTLSData pInFlightData = pThreadLocalData->pInFlightData;
        while (pInFlightData != NULL)
        {
            if (pInFlightData->tlsIndex == index)
            {
                pTLSBaseAddress = pInFlightData->pTLSData;
                break;
            }
            pInFlightData = pInFlightData->pNext;
        }
    }
    return dac_cast<PTR_VOID>(pTLSBaseAddress);
}

GPTR_IMPL(TLSIndexToMethodTableMap, g_pThreadStaticTypeIndices);
 
PTR_MethodTable LookupMethodTableForThreadStatic(TLSIndex index)
{
    // TODO, if and when we get array indices, we should be pickier.
    TADDR flagsUnused;
    return g_pThreadStaticTypeIndices->GetElement(index.TLSIndexRawIndex, &flagsUnused);
}

TADDR isGCFlag = 0x1;

PTR_MethodTable LookupMethodTableAndFlagForThreadStatic(TLSIndex index, bool *pIsGCStatic)
{
    // TODO, if and when we get array indices, we should be pickier.
    TADDR flags;
    PTR_MethodTable retVal = g_pThreadStaticTypeIndices->GetElement(index.TLSIndexRawIndex, &flags);
    *pIsGCStatic = flags == isGCFlag;
    return retVal;
}

void ScanThreadStaticRoots(PTR_ThreadLocalData pThreadLocalData, promote_func* fn, ScanContext* sc)
{
    PTR_InFlightTLSData pInFlightData = pThreadLocalData->pInFlightData;
    while (pInFlightData != NULL)
    {
        fn(dac_cast<PTR_PTR_Object>(pInFlightData->pTLSData), sc, 0 /* could be GC_CALL_INTERIOR or GC_CALL_PINNED */);
        pInFlightData = pInFlightData->pNext;
    }
    PTR_BYTE pTLSArrayData = dac_cast<PTR_BYTE>(pThreadLocalData->tlsArray.pTLSArrayData);
    int32_t cTLSData = pThreadLocalData->tlsArray.cTLSData;
    for (int32_t i = 0; i < cTLSData; i += sizeof(TADDR))
    {
        TLSIndex index(i);
        bool isGCStatic;
        MethodTable *pMT = LookupMethodTableAndFlagForThreadStatic(index, &isGCStatic);
        if (pMT == NULL)
        {
            continue;
        }
        TADDR *pTLSBaseAddress = dac_cast<PTR_TADDR>(pTLSArrayData + i);
        if (pTLSBaseAddress != NULL)
        {
            fn(dac_cast<PTR_PTR_Object>(pTLSBaseAddress), sc, 0 /* could be GC_CALL_INTERIOR or GC_CALL_PINNED */);
        }
    }
}

#ifndef DACCESS_COMPILE
#ifdef _MSC_VER
__declspec(thread)  ThreadLocalData t_ThreadStatics;
#else
__thread ThreadLocalData t_ThreadStatics;
#endif // _MSC_VER

void* GetThreadLocalStaticBaseIfExistsAndInitialized(TLSIndex index)
{
    LIMITED_METHOD_CONTRACT;
    TADDR pTLSBaseAddress = NULL;
    TLSArray* pTLSArray = reinterpret_cast<TLSArray*>((uint8_t*)&t_ThreadStatics + index.GetTLSArrayOffset());

    int32_t cTLSData = pTLSArray->cTLSData;
    if (cTLSData < index.GetByteIndex())
    {
        return NULL;
    }

    TADDR pTLSArrayData = pTLSArray->pTLSArrayData;
    pTLSBaseAddress = *reinterpret_cast<TADDR*>(reinterpret_cast<uint8_t*>(pTLSArrayData) + index.GetByteIndex());
    return reinterpret_cast<void*>(pTLSBaseAddress);
}

uint32_t g_NextTLSSlot = (uint32_t)sizeof(TADDR);
CrstStatic g_TLSCrst;

void InitializeThreadStaticData()
{
    g_pThreadStaticTypeIndices = new TLSIndexToMethodTableMap();
    g_pThreadStaticTypeIndices->supportedFlags = isGCFlag;
    g_TLSCrst.Init(CrstThreadLocalStorageLock, CRST_UNSAFE_ANYMODE);
}

void InitializeCurrentThreadsStaticData(Thread* pThread)
{
    pThread->m_pThreadLocalData = &t_ThreadStatics;
    t_ThreadStatics.pThread = pThread;
}

void AllocateThreadStaticBoxes(MethodTable *pMT, PTRARRAYREF *ppRef)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pMT->HasBoxedThreadStatics());
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    FieldDesc *pField = pMT->HasGenericsStaticsInfo() ?
        pMT->GetGenericsStaticFieldDescs() : (pMT->GetApproxFieldDescListRaw() + pMT->GetNumIntroducedInstanceFields());

    // Move pField to point to the list of thread statics
    pField += pMT->GetNumStaticFields() - pMT->GetNumThreadStaticFields();

    FieldDesc *pFieldEnd = pField + pMT->GetNumThreadStaticFields();

    while (pField < pFieldEnd)
    {
        _ASSERTE(pField->IsThreadStatic());

        // We only care about thread statics which are value types
        if (pField->IsByValue())
        {
            TypeHandle  th = pField->GetFieldTypeHandleThrowing();
            MethodTable* pFieldMT = th.GetMethodTable();

            OBJECTREF obj = MethodTable::AllocateStaticBox(pFieldMT, pMT->HasFixedAddressVTStatics());
            (*ppRef)->SetAt(pField->GetOffset(), obj);
        }

        pField++;
    }
}

void FreeCurrentThreadStaticData()
{
    delete[] (uint8_t*)t_ThreadStatics.tlsArray.pTLSArrayData;

    t_ThreadStatics.tlsArray.pTLSArrayData = 0;

    while (t_ThreadStatics.pInFlightData != NULL)
    {
        InFlightTLSData* pInFlightData = t_ThreadStatics.pInFlightData;
        t_ThreadStatics.pInFlightData = pInFlightData->pNext;
        delete pInFlightData;
    }

    t_ThreadStatics.pThread = NULL;
}

void* GetThreadLocalStaticBase(TLSIndex index)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    TLSArray* pTLSArray = reinterpret_cast<TLSArray*>((uint8_t*)&t_ThreadStatics + index.GetTLSArrayOffset());

    int32_t cTLSData = pTLSArray->cTLSData;
    if (cTLSData < index.GetByteIndex())
    {
        // Grow the underlying TLS array
        CrstHolder ch(&g_TLSCrst);
        int32_t newcTLSData = index.GetByteIndex() + sizeof(TADDR) * 8; // Leave a bit of margin
        uint8_t* pNewTLSArrayData = new uint8_t[newcTLSData];
        memset(pNewTLSArrayData, 0, newcTLSData);
        if (cTLSData > 0)
            memcpy(pNewTLSArrayData, (void*)pTLSArray->pTLSArrayData, cTLSData + 1);
        uint8_t* pOldArray = (uint8_t*)pTLSArray->pTLSArrayData;
        pTLSArray->pTLSArrayData = (TADDR)pNewTLSArrayData;
        cTLSData = newcTLSData - 1;
        pTLSArray->cTLSData = cTLSData;
        delete[] pOldArray;
    }

    TADDR pTLSArrayData = pTLSArray->pTLSArrayData;
    TADDR *ppTLSBaseAddress = reinterpret_cast<TADDR*>(reinterpret_cast<uint8_t*>(pTLSArrayData) + index.GetByteIndex());
    TADDR pTLSBaseAddress = *ppTLSBaseAddress;

    if (pTLSBaseAddress == NULL)
    {
        // Maybe it is in the InFlightData
        InFlightTLSData* pInFlightData = t_ThreadStatics.pInFlightData;
        InFlightTLSData** ppOldNextPtr = &t_ThreadStatics.pInFlightData;
        while (pInFlightData != NULL)
        {
            if (pInFlightData->tlsIndex == index)
            {
                pTLSBaseAddress = pInFlightData->pTLSData;
                MethodTable *pMT = LookupMethodTableForThreadStatic(index);
                if (pMT->IsClassInited())
                {
                    *ppTLSBaseAddress = pTLSBaseAddress;
                    *ppOldNextPtr = pInFlightData->pNext;
                    delete pInFlightData;
                }
                break;
            }
            ppOldNextPtr = &pInFlightData->pNext;
            pInFlightData = pInFlightData->pNext;
        }
        if (pTLSBaseAddress == NULL)
        {
            // Now we need to actually allocate the TLS data block
            bool isGCStatic;
            MethodTable *pMT = LookupMethodTableAndFlagForThreadStatic(index, &isGCStatic);
            struct 
            {
                PTRARRAYREF ptrRef;
                OBJECTREF tlsEntry;
            } gc;
            memset(&gc, 0, sizeof(gc));
            GCPROTECT_BEGIN(gc);
            if (isGCStatic)
            {
                gc.ptrRef = AllocateObjectArray(pMT->GetClass()->GetNumHandleThreadStatics(), g_pObjectClass);
                if (pMT->HasBoxedThreadStatics())
                {
                    AllocateThreadStaticBoxes(pMT, &gc.ptrRef);
                }
                gc.tlsEntry = (OBJECTREF)gc.ptrRef;
            }
            else
            {
                gc.tlsEntry = AllocatePrimitiveArray(ELEMENT_TYPE_I1, static_cast<DWORD>(pMT->GetClass()->GetNonGCThreadStaticFieldBytes()));
            }

            {
                GCX_FORBID();
                pTLSBaseAddress = (TADDR)OBJECTREFToObject(gc.tlsEntry);
                if (pMT->IsClassInited())
                {
                    *ppTLSBaseAddress = pTLSBaseAddress;
                }
                else
                {
                    InFlightTLSData* pInFlightData = new InFlightTLSData(index, pTLSBaseAddress);
                    pInFlightData->pNext = t_ThreadStatics.pInFlightData;
                    t_ThreadStatics.pInFlightData = pInFlightData;
                }
            }
            GCPROTECT_END();
        }
    }
    _ASSERTE(pTLSBaseAddress != NULL);
    return reinterpret_cast<void*>(pTLSBaseAddress);
}

void GetTLSIndexForThreadStatic(MethodTable* pMT, bool gcStatic, TLSIndex* pIndex)
{
    WRAPPER_NO_CONTRACT;
    CrstHolder ch(&g_TLSCrst);
    if (pIndex->IsAllocated())
    {
        return;
    }

    uint32_t tlsRawIndex = g_NextTLSSlot;
    g_NextTLSSlot += (uint32_t)sizeof(TADDR);
    g_pThreadStaticTypeIndices->AddElement(g_pObjectClass->GetModule(), tlsRawIndex, pMT, (gcStatic ? isGCFlag : 0));

    // TODO Handle collectible cases
    *pIndex = TLSIndex(tlsRawIndex);
}

#if defined(TARGET_WINDOWS)
EXTERN_C uint32_t _tls_index;
/*********************************************************************/
static uint32_t ThreadLocalOffset(void* p)
{
    PTEB Teb = NtCurrentTeb();
    uint8_t** pTls = (uint8_t**)Teb->ThreadLocalStoragePointer;
    uint8_t* pOurTls = pTls[_tls_index];
    return (uint32_t)((uint8_t*)p - pOurTls);
}
#elif defined(TARGET_OSX)
extern "C" void* GetThreadVarsAddress();

static void* GetThreadVarsSectionAddressFromDesc(uint8_t* p)
{
    _ASSERT(p[0] == 0x48 && p[1] == 0x8d && p[2] == 0x3d);

    // At this point, `p` contains the instruction pointer and is pointing to the above opcodes.
    // These opcodes are patched by the dynamic linker.
    // Move beyond the opcodes that we have already checked above.
    p += 3;

    // The descriptor address is located at *p at this point.
    // (p + 4) below skips the descriptor address bytes embedded in the instruction and
    // add it to the `instruction pointer` to find out the address.
    return *(uint32_t*)p + (p + 4);
}

static void* GetThreadVarsSectionAddress()
{
#ifdef TARGET_AMD64
    // On x64, the address is related to rip, so, disassemble the function,
    // read the offset, and then relative to the IP, find the final address of
    // __thread_vars section.
    uint8_t* p = reinterpret_cast<uint8_t*>(&GetThreadVarsAddress);
    return GetThreadVarsSectionAddressFromDesc(p);
#else
    return GetThreadVarsAddress();
#endif // TARGET_AMD64
}

#else

// Linux

#ifdef TARGET_AMD64

extern "C" void* GetTlsIndexObjectDescOffset();

static void* GetThreadStaticDescriptor(uint8_t* p)
{
    if (!(p[0] == 0x66 && p[1] == 0x48 && p[2] == 0x8d && p[3] == 0x3d))
    {
        // The optimization is disabled if coreclr is not compiled in .so format.
        _ASSERTE(false && "Unexpected code sequence");
        return nullptr;
    }

    // At this point, `p` contains the instruction pointer and is pointing to the above opcodes.
    // These opcodes are patched by the dynamic linker.
    // Move beyond the opcodes that we have already checked above.
    p += 4;

    // The descriptor address is located at *p at this point. Read that and add
    // it to the instruction pointer to locate the address of `ti` that will be used
    // to pass to __tls_get_addr during execution.
    // (p + 4) below skips the descriptor address bytes embedded in the instruction and
    // add it to the `instruction pointer` to find out the address.
    return *(uint32_t*)p + (p + 4);
}

static void* GetTlsIndexObjectAddress()
{
    uint8_t* p = reinterpret_cast<uint8_t*>(&GetTlsIndexObjectDescOffset);
    return GetThreadStaticDescriptor(p);
}

#elif defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

extern "C" size_t GetThreadStaticsVariableOffset();

#endif // TARGET_ARM64 || TARGET_LOONGARCH64 || TARGET_RISCV64
#endif // TARGET_WINDOWS

void GetThreadLocalStaticBlocksInfo(CORINFO_THREAD_STATIC_BLOCKS_INFO* pInfo)
{
    STANDARD_VM_CONTRACT;
    size_t threadStaticBaseOffset = 0;

#if defined(TARGET_WINDOWS)
    pInfo->tlsIndex.addr = (void*)static_cast<uintptr_t>(_tls_index);
    pInfo->tlsIndex.accessType = IAT_VALUE;

    pInfo->offsetOfThreadLocalStoragePointer = offsetof(_TEB, ThreadLocalStoragePointer);
    threadStaticBaseOffset = ThreadLocalOffset(&t_ThreadStatics);

#elif defined(TARGET_OSX)

    pInfo->threadVarsSection = GetThreadVarsSectionAddress();

#elif defined(TARGET_AMD64)

    // For Linux/x64, get the address of tls_get_addr system method and the base address
    // of struct that we will pass to it.
    pInfo->tlsGetAddrFtnPtr = reinterpret_cast<void*>(&__tls_get_addr);
    pInfo->tlsIndexObject = GetTlsIndexObjectAddress();

#elif defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

    // For Linux arm64/loongarch64/riscv64, just get the offset of thread static variable, and during execution,
    // this offset, arm64 taken from trpid_elp0 system register gives back the thread variable address.
    // this offset, loongarch64 taken from $tp register gives back the thread variable address.
    threadStaticBaseOffset = GetThreadStaticsVariableOffset();

#else
    _ASSERTE_MSG(false, "Unsupported scenario of optimizing TLS access on Linux Arm32/x86");
#endif // TARGET_WINDOWS

    pInfo->offsetOfThreadStaticBlocks = (uint32_t)threadStaticBaseOffset;
}
#endif // !DACCESS_COMPILE

#ifdef DACCESS_COMPILE
void EnumThreadMemoryRegions(PTR_ThreadLocalData pThreadLocalData, CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    DacEnumMemoryRegion(dac_cast<TADDR>(pThreadLocalData), sizeof(ThreadLocalData), flags);
    DacEnumMemoryRegion(dac_cast<TADDR>(pThreadLocalData->tlsArray.pTLSArrayData), pThreadLocalData->tlsArray.cTLSData, flags);
    PTR_InFlightTLSData pInFlightData = pThreadLocalData->pInFlightData;
    while (pInFlightData != NULL)
    {
        DacEnumMemoryRegion(dac_cast<TADDR>(pInFlightData), sizeof(InFlightTLSData), flags);
        pInFlightData = pInFlightData->pNext;
    }
}
#endif // DACCESS_COMPILE
