// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================

//
// File: DebugSupport.cpp
//
// Support routines for debugging the CLR
// ===========================================================================

#include "stdafx.h"

#ifndef TARGET_UNIX
#ifndef TARGET_X86

//
//
// @TODO: This is old code that should be easy to implement on top of the existing DAC support.
//        This code was originally written prior to DAC.
//
//

#include <winwrap.h>
#include <windows.h>
#include <winnt.h>
#include <clrnt.h>
#include <stddef.h> // offsetof
#include "nibblemapmacros.h"
#include "stdmacros.h"

#include "fntableaccess.h"

#define move(dst, src)  \
{ \
   if (!fpReadMemory(pUserContext, (LPCVOID)(src), &(dst), sizeof(dst), NULL))  \
   { \
      _ASSERTE(!"MSCORDBG ERROR: ReadProcessMemory failed!!"); \
      return STATUS_UNSUCCESSFUL; \
   } \
}

#define move_field(dst, src, cls, fld) \
    move(dst, (SIZE_T)(src) + FIELD_OFFSET(cls, fld))

static NTSTATUS OutOfProcessFindHeader(ReadMemoryFunction fpReadMemory,PVOID pUserContext, DWORD_PTR pMapIn, DWORD_PTR addr, DWORD_PTR &codeHead)
{
    codeHead = 0;

    DWORD       tmp;                              // must be a DWORD, not a DWORD_PTR
    DWORD_PTR   startPos  = ADDR2POS(addr);       // align to  128 byte buckets ( == index into the array of nibbles)
    DWORD_PTR   offset    = ADDR2OFFS(addr);      // this is the offset inside the bucket + 1
    DWORD *     pMap      = (DWORD *) pMapIn;     // make this a pointer type so our pointer math is correct w/o adding sizeof(DWORD) everywhere

    _ASSERTE(offset == (offset & NIBBLE_MASK));   // the offset must fit in a nibble

    pMap += (startPos >> LOG2_NIBBLES_PER_DWORD); // points to the proper DWORD of the map

    //
    // get DWORD and shift down our nibble
    //
    move(tmp, pMap);
    tmp = tmp >> POS2SHIFTCOUNT(startPos);

    // don't allow equality in the next check (tmp & NIBBLE_MASK == offset)
    // there are code blocks that terminate with a call instruction
    // (like call throwobject), i.e. their return address is
    // right behind the code block. If the memory manager allocates
    // heap blocks w/o gaps, we could find the next header in such
    // cases. Therefore we exclude the first DWORD of the header
    // from our search, but since we call this function for code
    // anyway (which starts at the end of the header) this is not
    // a problem.
    if ((tmp & NIBBLE_MASK) && ((tmp & NIBBLE_MASK) < offset) )
    {
        codeHead = POSOFF2ADDR(startPos, tmp & NIBBLE_MASK) - sizeof(CodeHeader);
        return STATUS_SUCCESS;
    }

    // is there a header in the remainder of the DWORD ?
    tmp = tmp >> NIBBLE_SIZE;

    if (tmp)
    {
        startPos--;
        while (!(tmp & NIBBLE_MASK))
        {
            tmp = tmp >> NIBBLE_SIZE;
            startPos--;
        }

        codeHead = POSOFF2ADDR(startPos, tmp & NIBBLE_MASK) - sizeof(CodeHeader);
        return STATUS_SUCCESS;
    }

    // we skipped the remainder of the DWORD,
    // so we must set startPos to the highest position of
    // previous DWORD

    startPos = ((startPos >> LOG2_NIBBLES_PER_DWORD) << LOG2_NIBBLES_PER_DWORD) - 1;

    if ((INT_PTR)startPos < 0)
    {
        return STATUS_SUCCESS;
    }

    // skip "headerless" DWORDS

    pMap--;
    move(tmp, pMap);
    while (!tmp)
    {
        startPos -= NIBBLES_PER_DWORD;
        if ((INT_PTR)startPos < 0)
        {
            return STATUS_SUCCESS;
        }
        pMap--;
        move (tmp, pMap);
    }


    while (!(tmp & NIBBLE_MASK))
    {
        tmp = tmp >> NIBBLE_SIZE;
        startPos--;
    }

    codeHead = POSOFF2ADDR(startPos, tmp & NIBBLE_MASK) - sizeof(CodeHeader);
    return STATUS_SUCCESS;
}

#define CODE_HEADER FakeRealCodeHeader
#define ResolveCodeHeader(pHeader)                          \
    if (pHeader)                                            \
    {                                                       \
        DWORD_PTR tmp = pHeader;                            \
        tmp += offsetof (FakeCodeHeader, pRealCodeHeader);  \
        move (tmp, tmp);                                    \
        pHeader = tmp;                                      \
    }

static NTSTATUS OutOfProcessFunctionTableCallback_JIT(IN  ReadMemoryFunction    fpReadMemory,
                                                      IN  PVOID                 pUserContext,
                                                      IN  PVOID                 TableAddress,
                                                      OUT PULONG                pnEntries,
                                                      OUT PT_RUNTIME_FUNCTION*    ppFunctions)
{
    if (NULL == pnEntries)      { return STATUS_INVALID_PARAMETER_3; }
    if (NULL == ppFunctions)    { return STATUS_INVALID_PARAMETER_4; }

    DYNAMIC_FUNCTION_TABLE * pTable = (DYNAMIC_FUNCTION_TABLE *) TableAddress;

    PVOID pvContext;
    move(pvContext, &pTable->Context);

    DWORD_PTR  JitMan      = (((DWORD_PTR)pvContext) & ~3);

    DWORD_PTR  MinAddress  = (DWORD_PTR) &(pTable->MinimumAddress);
    move(MinAddress, MinAddress);

    *ppFunctions = 0;
    *pnEntries   = 0;

    DWORD_PTR  pHp = JitMan + (DWORD_PTR)offsetof(FakeEEJitManager, m_pCodeHeap);

    move(pHp, pHp);

    while (pHp)
    {
        FakeHeapList Hp;

        move(Hp, pHp);

        if (Hp.GetModuleBase() == MinAddress)
        {
            DWORD_PTR          pThisHeader;
            DWORD_PTR          hdrOffset;
            DWORD_PTR          hdrOffsetInitial;
            DWORD              nEntries;
            DWORD              index;
            PT_RUNTIME_FUNCTION  pFunctions;
            LONG64             lSmallestOffset;

            //
            // walk the header map and count functions with unwind info
            //
            nEntries  = 0;
            hdrOffset = Hp.endAddress - Hp.mapBase;
            lSmallestOffset = (LONG64)(Hp.startAddress - Hp.mapBase);

            // Save the initial offset at which we start our enumeration (from the end to the beginning).
            // The target process could be running when this function is called.  New methods could be
            // added after we have started our enumeration, but their code headers would be added after
            // this initial offset.  Methods could also be deleted, but the memory would still be there.
            // It just wouldn't be marked as the beginning of a method, and we would collect fewer entries
            // than we have anticipated.
            hdrOffsetInitial = hdrOffset;

            _ASSERTE(((LONG64)hdrOffset) >= lSmallestOffset);
            OutOfProcessFindHeader(fpReadMemory, pUserContext, Hp.pHdrMap, hdrOffset, hdrOffset);

            while (((LONG64)hdrOffset) >= lSmallestOffset)  // MUST BE A SIGNED COMPARISON
            {
                pThisHeader = Hp.mapBase + hdrOffset;
                ResolveCodeHeader(pThisHeader);

                if (pThisHeader > FAKE_STUB_CODE_BLOCK_LAST)
                {
                    DWORD nUnwindInfos;
                    move_field(nUnwindInfos, pThisHeader, CODE_HEADER, nUnwindInfos);

                    nEntries += nUnwindInfos;
                }

                _ASSERTE(((LONG64)hdrOffset) >= lSmallestOffset);
                OutOfProcessFindHeader(fpReadMemory, pUserContext, Hp.pHdrMap, hdrOffset, hdrOffset);
            }

            S_SIZE_T blockSize = S_SIZE_T(nEntries) * S_SIZE_T(sizeof(T_RUNTIME_FUNCTION));
            if (blockSize.IsOverflow())
                return STATUS_UNSUCCESSFUL;

            pFunctions = (PT_RUNTIME_FUNCTION)HeapAlloc(GetProcessHeap(), 0, blockSize.Value());
            if (pFunctions == NULL)
                return STATUS_NO_MEMORY;

            //
            // walk the header map and copy the function tables
            //

            index     = 0;
            hdrOffset = hdrOffsetInitial;

            _ASSERTE(((LONG64)hdrOffset) >= lSmallestOffset);
            OutOfProcessFindHeader(fpReadMemory, pUserContext, Hp.pHdrMap, hdrOffset, hdrOffset);

            while (((LONG64)hdrOffset) >= lSmallestOffset)  // MUST BE A SIGNED COMPARISON
            {
                pThisHeader = Hp.mapBase + hdrOffset;
                ResolveCodeHeader(pThisHeader);

                if (pThisHeader > FAKE_STUB_CODE_BLOCK_LAST)
                {
                    DWORD nUnwindInfos;
                    move_field(nUnwindInfos, pThisHeader, CODE_HEADER, nUnwindInfos);

                    if ((index + nUnwindInfos) > nEntries)
                    {
                        break;
                    }
                    for (DWORD iUnwindInfo = 0; iUnwindInfo < nUnwindInfos; iUnwindInfo++)
                    {
                        move(pFunctions[index], pThisHeader + offsetof(CODE_HEADER, unwindInfos[iUnwindInfo]));
                        index++;
                    }
                }

                _ASSERTE(((LONG64)hdrOffset) >= lSmallestOffset);
                OutOfProcessFindHeader(fpReadMemory, pUserContext, Hp.pHdrMap, hdrOffset, hdrOffset);
            }

            *ppFunctions = pFunctions;
            *pnEntries = index;
            break;
        }

        pHp = (DWORD_PTR)Hp.hpNext;
    }

    return STATUS_SUCCESS;
}


#ifdef DEBUGSUPPORT_STUBS_HAVE_UNWIND_INFO

static NTSTATUS OutOfProcessFunctionTableCallback_Stub(IN  ReadMemoryFunction    fpReadMemory,
                                                       IN  PVOID                 pUserContext,
                                                       IN  PVOID                 TableAddress,
                                                       OUT PULONG                pnEntries,
                                                       OUT PT_RUNTIME_FUNCTION*    ppFunctions)
{
    if (NULL == pnEntries)      { return STATUS_INVALID_PARAMETER_3; }
    if (NULL == ppFunctions)    { return STATUS_INVALID_PARAMETER_4; }

    *ppFunctions = 0;
    *pnEntries   = 0;

    PVOID pvContext;
    move_field(pvContext, TableAddress, DYNAMIC_FUNCTION_TABLE, Context);

    SIZE_T pStubHeapSegment = ((SIZE_T)pvContext & ~3);

    FakeStubUnwindInfoHeapSegment stubHeapSegment;
    move(stubHeapSegment, pStubHeapSegment);

    UINT nEntries = 0;
    UINT nEntriesAllocated = 0;
    PT_RUNTIME_FUNCTION rgFunctions = NULL;

    for (int pass = 1; pass <= 2; pass++)
    {
        // Use the same initial header for both passes.  The process may still be running,
        // and so new entries could be added at the beginning of the list.  Using the initial header
        // makes sure new entries are not picked up in the second pass.  Entries could also be deleted,
        // and there is a small time window here where we could read invalid memory.  This just means
        // that ReadProcessMemory() may fail.  As long as we don't crash the host process (e.g. WER)
        // we are fine.
        SIZE_T pHeader = (SIZE_T)stubHeapSegment.pUnwindHeaderList;

        while (pHeader)
        {
            FakeStubUnwindInfoHeader unwindInfoHeader;
            move(unwindInfoHeader, pHeader);
#if defined(TARGET_AMD64)
            // Consistency checks to detect corrupted process state
            if (unwindInfoHeader.FunctionEntry.BeginAddress > unwindInfoHeader.FunctionEntry.EndAddress ||
                unwindInfoHeader.FunctionEntry.EndAddress > stubHeapSegment.cbSegment)
            {
                _ASSERTE(1 == pass);
                return STATUS_UNSUCCESSFUL;
            }

            if ((SIZE_T)stubHeapSegment.pbBaseAddress + unwindInfoHeader.FunctionEntry.UnwindData !=
                    pHeader + FIELD_OFFSET(FakeStubUnwindInfoHeader, UnwindInfo))
            {
                _ASSERTE(1 == pass);
                return STATUS_UNSUCCESSFUL;
            }
#elif defined(TARGET_ARM)

            // Skip checking the corrupted process stateon ARM

#elif defined(TARGET_ARM64)
            // Compute the function length
            ULONG64 functionLength = 0;
            ULONG64 unwindData = unwindInfoHeader.FunctionEntry.UnwindData;
            if (( unwindData & 3) != 0) {
                // the unwindData contains the function length, retrieve it directly from unwindData
                functionLength = (unwindInfoHeader.FunctionEntry.UnwindData >> 2) & 0x7ff;
            } else {
                // the unwindData is an RVA to the .xdata record which contains the function length
                DWORD xdataHeader=0;
                if ((SIZE_T)stubHeapSegment.pbBaseAddress + unwindData != pHeader + FIELD_OFFSET(FakeStubUnwindInfoHeader, UnwindInfo))
                {
                    _ASSERTE(1 == pass);
                    return STATUS_UNSUCCESSFUL;
                }
                move(xdataHeader, stubHeapSegment.pbBaseAddress + unwindData);
                functionLength = (xdataHeader & 0x3ffff) << 2;
            }
            if (unwindInfoHeader.FunctionEntry.BeginAddress + functionLength > stubHeapSegment.cbSegment)
            {
                _ASSERTE(1 == pass);
                return STATUS_UNSUCCESSFUL;
            }
#else
            PORTABILITY_ASSERT("OutOfProcessFunctionTableCallback_Stub");
#endif
            if (nEntriesAllocated)
            {
                if (nEntries >= nEntriesAllocated)
                    break;
                rgFunctions[nEntries] = unwindInfoHeader.FunctionEntry;
            }
            nEntries++;

            pHeader = (SIZE_T)unwindInfoHeader.pNext;
        }

        if (1 == pass)
        {
            if (!nEntries)
                break;

            _ASSERTE(!nEntriesAllocated);
            nEntriesAllocated = nEntries;

            S_SIZE_T blockSize = S_SIZE_T(nEntries) * S_SIZE_T(sizeof(T_RUNTIME_FUNCTION));
            if (blockSize.IsOverflow())
                return STATUS_UNSUCCESSFUL;

            rgFunctions = (PT_RUNTIME_FUNCTION)HeapAlloc(GetProcessHeap(), 0, blockSize.Value());
            if (rgFunctions == NULL)
                return STATUS_NO_MEMORY;
            nEntries = 0;
        }
        else
        {
            _ASSERTE(nEntriesAllocated >= nEntries);
        }
    }

    *ppFunctions = rgFunctions;
    *pnEntries   = nEntries;        // return the final count

    return STATUS_SUCCESS;
}

#endif // DEBUGSUPPORT_STUBS_HAVE_UNWIND_INFO


BOOL ReadMemory(PVOID pUserContext, LPCVOID lpBaseAddress, PVOID lpBuffer, SIZE_T nSize, SIZE_T* lpNumberOfBytesRead)
{
    HANDLE hProcess = (HANDLE)pUserContext;
    return ReadProcessMemory(hProcess, lpBaseAddress, lpBuffer, nSize, lpNumberOfBytesRead);
}

extern "C" NTSTATUS OutOfProcessFunctionTableCallback(IN  HANDLE                hProcess,
                                                      IN  PVOID                 TableAddress,
                                                      OUT PULONG                pnEntries,
                                                      OUT PT_RUNTIME_FUNCTION*    ppFunctions)
{
    return OutOfProcessFunctionTableCallbackEx(&ReadMemory, hProcess, TableAddress, pnEntries, ppFunctions);
}

extern "C" NTSTATUS OutOfProcessFunctionTableCallbackEx(IN  ReadMemoryFunction    fpReadMemory,
                                                        IN  PVOID				  pUserContext,
                                                        IN  PVOID                 TableAddress,
                                                        OUT PULONG                pnEntries,
                                                        OUT PT_RUNTIME_FUNCTION*    ppFunctions)
{
    if (NULL == pnEntries)      { return STATUS_INVALID_PARAMETER_3; }
    if (NULL == ppFunctions)    { return STATUS_INVALID_PARAMETER_4; }

    DYNAMIC_FUNCTION_TABLE * pTable = (DYNAMIC_FUNCTION_TABLE *) TableAddress;
    PVOID pvContext;

    move(pvContext, &pTable->Context);

    FakeEEDynamicFunctionTableType type = (FakeEEDynamicFunctionTableType)((SIZE_T)pvContext & 3);

    switch (type)
    {
    case FAKEDYNFNTABLE_JIT:
        return OutOfProcessFunctionTableCallback_JIT(
                fpReadMemory,
                pUserContext,
                TableAddress,
                pnEntries,
                ppFunctions);

#ifdef DEBUGSUPPORT_STUBS_HAVE_UNWIND_INFO
    case FAKEDYNFNTABLE_STUB:
        return OutOfProcessFunctionTableCallback_Stub(
                fpReadMemory,
                pUserContext,
                TableAddress,
                pnEntries,
                ppFunctions);
#endif // DEBUGSUPPORT_STUBS_HAVE_UNWIND_INFO
    default:
        break;
    }

    return STATUS_UNSUCCESSFUL;
}

#else

extern "C" NTSTATUS OutOfProcessFunctionTableCallback()
{
    return STATUS_UNSUCCESSFUL;
}

extern "C" NTSTATUS OutOfProcessFunctionTableCallbackEx()
{
    return STATUS_UNSUCCESSFUL;
}

#endif // !TARGET_X86
#endif // !TARGET_UNIX
