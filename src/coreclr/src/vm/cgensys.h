// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// CGENSYS.H -
//
// Generic header for choosing system-dependent helpers
//



#ifndef __cgensys_h__
#define __cgensys_h__

class MethodDesc;
class Stub;
class Thread;
class CrawlFrame;
struct EE_ILEXCEPTION_CLAUSE;
struct TransitionBlock;
struct VASigCookie;
struct CORCOMPILE_EXTERNAL_METHOD_THUNK;
class ComPlusCallMethodDesc;

#include <cgencpu.h>


#ifdef EnC_SUPPORTED
void ResumeAtJit(PT_CONTEXT pContext, LPVOID oldFP);
#endif

#if defined(_TARGET_X86_)
void ResumeAtJitEH   (CrawlFrame* pCf, BYTE* startPC, EE_ILEXCEPTION_CLAUSE *EHClausePtr, DWORD nestingLevel, Thread *pThread, BOOL unwindStack);
int  CallJitEHFilter (CrawlFrame* pCf, BYTE* startPC, EE_ILEXCEPTION_CLAUSE *EHClausePtr, DWORD nestingLevel, OBJECTREF thrownObj);
void CallJitEHFinally(CrawlFrame* pCf, BYTE* startPC, EE_ILEXCEPTION_CLAUSE *EHClausePtr, DWORD nestingLevel);
#endif // _TARGET_X86_

//These are in util.cpp
extern size_t GetLogicalProcessorCacheSizeFromOS();
extern size_t GetIntelDeterministicCacheEnum();
extern size_t GetIntelDescriptorValuesCache();
extern DWORD GetLogicalCpuCountFromOS();
extern DWORD GetLogicalCpuCountFallback();


// Try to determine the largest last-level cache size of the machine - return 0 if unknown or no L2/L3 cache
size_t GetCacheSizePerLogicalCpu(BOOL bTrueSize = TRUE);


#ifdef FEATURE_COMINTEROP
extern "C" UINT32 STDCALL CLRToCOMWorker(TransitionBlock * pTransitionBlock, ComPlusCallMethodDesc * pMD);
extern "C" void GenericComPlusCallStub(void);

extern "C" void GenericComCallStub(void);
#endif // FEATURE_COMINTEROP

// Non-CPU-specific helper functions called by the CPU-dependent code
extern "C" PCODE STDCALL PreStubWorker(TransitionBlock * pTransitionBlock, MethodDesc * pMD);

extern "C" void STDCALL VarargPInvokeStubWorker(TransitionBlock * pTransitionBlock, VASigCookie * pVASigCookie, MethodDesc * pMD);
extern "C" void STDCALL VarargPInvokeStub(void);
extern "C" void STDCALL VarargPInvokeStub_RetBuffArg(void);

extern "C" void STDCALL GenericPInvokeCalliStubWorker(TransitionBlock * pTransitionBlock, VASigCookie * pVASigCookie, PCODE pUnmanagedTarget);
extern "C" void STDCALL GenericPInvokeCalliHelper(void);

extern "C" PCODE STDCALL ExternalMethodFixupWorker(TransitionBlock * pTransitionBlock, TADDR pIndirection, DWORD sectionIndex, Module * pModule);
extern "C" void STDCALL ExternalMethodFixupStub(void);
extern "C" void STDCALL ExternalMethodFixupPatchLabel(void);

extern "C" void STDCALL VirtualMethodFixupStub(void);
extern "C" void STDCALL VirtualMethodFixupPatchLabel(void);

extern "C" void STDCALL TransparentProxyStub(void);
extern "C" void STDCALL TransparentProxyStub_CrossContext();
extern "C" void STDCALL TransparentProxyStubPatchLabel(void);

#ifdef FEATURE_READYTORUN
extern "C" void STDCALL DelayLoad_MethodCall();

extern "C" void STDCALL DelayLoad_Helper();
extern "C" void STDCALL DelayLoad_Helper_Obj();
extern "C" void STDCALL DelayLoad_Helper_ObjObj();
#endif

// Returns information about the CPU processor.
// Note that this information may be the least-common-denominator in the
// case of a multi-proc machine.

#ifdef _TARGET_X86_
void GetSpecificCpuInfo(CORINFO_CPU * cpuInfo);
#else
inline void GetSpecificCpuInfo(CORINFO_CPU * cpuInfo)
{
    LIMITED_METHOD_CONTRACT;
    cpuInfo->dwCPUType = 0;
    cpuInfo->dwFeatures = 0;
    cpuInfo->dwExtendedFeatures = 0;
}

#endif // !_TARGET_X86_

#if (defined(_TARGET_X86_) || defined(_TARGET_AMD64_)) && !defined(CROSSGEN_COMPILE)
extern "C" DWORD __stdcall getcpuid(DWORD arg, unsigned char result[16]);
extern "C" DWORD __stdcall getextcpuid(DWORD arg1, DWORD arg2, unsigned char result[16]);
extern "C" DWORD __stdcall xmmYmmStateSupport();
#endif

inline bool TargetHasAVXSupport()
{
#if (defined(_TARGET_X86_) || defined(_TARGET_AMD64_)) && !defined(CROSSGEN_COMPILE)
    unsigned char buffer[16];
    // All x86/AMD64 targets support cpuid.
    (void) getcpuid(1, buffer);
    // getcpuid executes cpuid with eax set to its first argument, and ecx cleared.
    // It returns the resulting eax, ebx, ecx and edx (in that order) in buffer[].
    // The AVX feature is ECX bit 28.
    return ((buffer[11] & 0x10) != 0);
#endif // (defined(_TARGET_X86_) || defined(_TARGET_AMD64_)) && !defined(CROSSGEN_COMPILE)
    return false;
}

#ifdef FEATURE_PREJIT
// Can code compiled for "minReqdCpuType" be used on "actualCpuType"
inline BOOL IsCompatibleCpuInfo(const CORINFO_CPU * actualCpuInfo,
                                const CORINFO_CPU * minReqdCpuInfo)
{
    LIMITED_METHOD_CONTRACT;
    return ((minReqdCpuInfo->dwFeatures & actualCpuInfo->dwFeatures) ==
             minReqdCpuInfo->dwFeatures);
}
#endif // FEATURE_PREJIT


#ifndef DACCESS_COMPILE
// Given an address in a slot, figure out if the prestub will be called
BOOL DoesSlotCallPrestub(PCODE pCode);
#endif

#ifdef DACCESS_COMPILE

// Used by dac/strike to make sense of non-jit/non-jit-helper call targets
// generated by the runtime.
BOOL GetAnyThunkTarget (T_CONTEXT *pctx, TADDR *pTarget, TADDR *pTargetMethodDesc);

#endif // DACCESS_COMPILE



//
// ResetProcessorStateHolder saves/restores processor state around calls to
// mscorlib during exception handling.
//
class ResetProcessorStateHolder
{
#if defined(_TARGET_AMD64_)
    ULONG m_mxcsr;
#endif

public:

    ResetProcessorStateHolder ()
    {
#if defined(_TARGET_AMD64_)
        m_mxcsr = _mm_getcsr();
        _mm_setcsr(0x1f80);
#endif // _TARGET_AMD64_
    }

    ~ResetProcessorStateHolder ()
    {
#if defined(_TARGET_AMD64_)
        _mm_setcsr(m_mxcsr);
#endif // _TARGET_AMD64_
    }
};


#endif // !__cgensys_h__
