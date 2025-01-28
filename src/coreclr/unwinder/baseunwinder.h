// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#ifndef __unwinder_h__
#define __unwinder_h__

#ifdef FEATURE_CDAC_UNWINDER
using ReadCallback = int (*)(uint64_t addr, void* pBuffer, int bufferSize);
using GetAllocatedBuffer = int (*)(int bufferSize, void** ppBuffer);
using GetStackWalkInfo = void (*)(uint64_t controlPC, UINT_PTR* pModuleBase, UINT_PTR* pFuncEntry);
#endif // FEATURE_CDAC_UNWINDER

//---------------------------------------------------------------------------------------
//
// OOPStackUnwinder is the abstract base class for unwinding stack frames.  Each of the two 64-bit platforms
// has its own derived class.  Although the name of this class and its derived classes have changed, they
// are actually borrowed from dbghelp.dll.  (StackWalk64() is built on top of these classes.)  We have ripped
// out everything we don't need such as symbol lookup and various state, and keep just enough code to support
// VirtualUnwind().  The managed debugging infrastructure can't call RtlVirtualUnwind() because it doesn't
// work from out-of-processr
//
// Notes:
//    To see what we have changed in the borrowed source, you can diff the original version and our version.
//    For example, on X64, you can diff clr\src\Debug\daccess\amd64\dbs_stack_x64.cpp (the original) and
//    clr\src\Debug\daccess\amd64\unwinder_amd64.cpp.
//

class OOPStackUnwinder
{
protected:

    // Given a control PC, return the base of the module it is in.  For jitted managed code, this is the
    // start of the code heap.
    HRESULT GetModuleBase(uint64_t address,
                                 _Out_ PDWORD64 pdwBase);

    // Given a control PC, return the function entry of the functoin it is in.
    HRESULT GetFunctionEntry(                       DWORD64 address,
                                    _Out_writes_(cbBuffer) PVOID   pBuffer,
                                                           DWORD   cbBuffer);

#ifdef FEATURE_CDAC_UNWINDER
protected:

    OOPStackUnwinder(ReadCallback readCallback,
                     GetAllocatedBuffer getAllocatedBuffer,
                     GetStackWalkInfo getStackWalkInfo)
        : readCallback(readCallback),
          getAllocatedBuffer(getAllocatedBuffer),
          getStackWalkInfo(getStackWalkInfo)
    { }

    ReadCallback readCallback;
    GetAllocatedBuffer getAllocatedBuffer;
    GetStackWalkInfo getStackWalkInfo;

#endif // FEATURE_CDAC_UWNINDER
};

#endif // __unwinder_h__
