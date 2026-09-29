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
    move(dst, (SIZE_T)(src) + offsetof(cls, fld))

static NTSTATUS OutOfProcessFindHeader(ReadMemoryFunction fpReadMemory,PVOID pUserContext, DWORD_PTR pMapIn, DWORD_PTR addr, DWORD_PTR &codeHead)
{
    using namespace NibbleMap;
    codeHead = 0;

    DWORD       dword;
    DWORD       tmp;                              // must be a DWORD, not a DWORD_PTR
    DWORD_PTR   startPos  = ADDR2POS(addr);       // align to 32 byte buckets ( == index into the array of nibbles)
    DWORD_PTR   offset    = ADDR2OFFS(addr);      // this is the offset inside the bucket + 1
    DWORD *     pMap      = (DWORD *) pMapIn;     // make this a pointer type so our pointer math is correct w/o adding sizeof(DWORD) everywhere

    _ASSERTE(offset == (offset & NIBBLE_MASK));   // the offset must fit in a nibble

    pMap += (startPos >> LOG2_NIBBLES_PER_DWORD); // points to the proper DWORD of the map

    // #1 look up DWORD represnting current PC
    move(dword, pMap);

    // #2 if DWORD is a pointer, then we can return
    if (IsPointer(dword))
    {
        codeHead = DecodePointer(dword) - sizeof(CodeHeader);
        return STATUS_SUCCESS;
    }

    tmp = dword >> POS2SHIFTCOUNT(startPos);

    // #3 check if corresponding nibble is intialized and points to an equal or earlier address
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

    // #4 try to find preceeding nibble in the DWORD
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

    // #5.1 read previous DWORD
    // We skipped the remainder of the DWORD,
    // so we must set startPos to the highest position of
    // previous DWORD, unless we are already on the first DWORD
    if (startPos < NIBBLES_PER_DWORD)
    {
        return 0;
    }

    startPos = ((startPos >> LOG2_NIBBLES_PER_DWORD) << LOG2_NIBBLES_PER_DWORD) - 1;
    pMap--;
    move(dword, pMap);

    // If the second dword is not empty, it either has a nibble or a pointer
    if (dword)
    {
        // #5.2 either DWORD is a pointer
        if (IsPointer(dword))
        {
            codeHead = DecodePointer(dword) - sizeof(CodeHeader);
            return STATUS_SUCCESS;
        }

        // #5.4 or contains a nibble
        tmp = dword;
        while(!(tmp & NIBBLE_MASK))
        {
            tmp >>= NIBBLE_SIZE;
            startPos--;
        }
        codeHead = POSOFF2ADDR(startPos, tmp & NIBBLE_MASK) - sizeof(CodeHeader);
        return STATUS_SUCCESS;
    }

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

extern "C" NTSTATUS OutOfProcessFunctionTableCallbackEx(IN  ReadMemoryFunction    fpReadMemory,
                                                        IN  PVOID				  pUserContext,
                                                        IN  PVOID                 TableAddress,
                                                        OUT PULONG                pnEntries,
                                                        OUT PT_RUNTIME_FUNCTION*  ppFunctions)
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

    DWORD_PTR  pHp = JitMan + (DWORD_PTR)offsetof(FakeEEJitManager, m_pAllCodeHeaps);

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
