// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#ifndef __unwinder_h__
#define __unwinder_h__

#ifdef FEATURE_CDAC_UNWINDER
using ReadFromTarget = LONG (*)(ULONG64 addr, PVOID pBuffer, LONG bufferSize, PVOID callbackContext);
using GetAllocatedBuffer = LONG (*)(LONG bufferSize, PVOID* ppBuffer, PVOID callbackContext);
using GetStackWalkInfo = VOID (*)(ULONG64 controlPC, UINT_PTR* pUnwindInfoBase, UINT_PTR* pFuncEntry, PVOID callbackContext);
using UnwinderFail = VOID (*)();

class CDACCallbacks
{
public:
    CDACCallbacks(ReadFromTarget readFromTarget,
        GetAllocatedBuffer getAllocatedBuffer,
        GetStackWalkInfo getStackWalkInfo,
        UnwinderFail unwinderFail,
        void* callbackContext)
        : readFromTarget(readFromTarget),
          getAllocatedBuffer(getAllocatedBuffer),
          getStackWalkInfo(getStackWalkInfo),
          unwinderFail(unwinderFail),
          callbackContext(callbackContext)
    { }

    ReadFromTarget readFromTarget;
    GetAllocatedBuffer getAllocatedBuffer;
    GetStackWalkInfo getStackWalkInfo;
    UnwinderFail unwinderFail;
    void* callbackContext;
};

// thread_local used to access cDAC callbacks outside of unwinder.
extern thread_local CDACCallbacks* t_pCallbacks;
#endif // FEATURE_CDAC_UNWINDER

// Report failure in the unwinder if the condition is FALSE
#if defined(FEATURE_CDAC_UNWINDER)
#define UNWINDER_ASSERT(Condition) if (!(Condition)) t_pCallbacks->unwinderFail()
#elif defined(DACCESS_COMPILE)
#define UNWINDER_ASSERT(Condition) if (!(Condition)) DacError(CORDBG_E_TARGET_INCONSISTENT)
#else // !DACCESS_COMPILE AND !FEATURE_CDAC_UNWINDER
#define UNWINDER_ASSERT _ASSERTE
#endif

//---------------------------------------------------------------------------------------
//
// OOPStackUnwinder is the abstract base class for unwinding stack frames.  Each of the two 64-bit platforms
// has its own derived class.  Although the name of this class and its derived classes have changed, they
// are actually borrowed from dbghelp.dll.  (StackWalk64() is built on top of these classes.)  We have ripped
// out everything we don't need such as symbol lookup and various state, and keep just enough code to support
// VirtualUnwind().  The managed debugging infrastructure can't call RtlVirtualUnwind() because it doesn't
// work from out-of-processor
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
    static HRESULT GetModuleBase(      DWORD64  address,
                                 _Out_ PDWORD64 pdwBase);

    // Given a control PC, return the function entry of the functoin it is in.
    static HRESULT GetFunctionEntry(                       DWORD64 address,
                                    _Out_writes_(cbBuffer) PVOID   pBuffer,
                                                           DWORD   cbBuffer);
};

#endif // __unwinder_h__
