// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
//
// EETwain.h
//
// This file has the definition of ICodeManager and EECodeManager.
//
// ICorJitCompiler compiles the IL of a method to native code, and stores
// auxilliary data called as GCInfo (via ICorJitInfo::allocGCInfo()).
// The data is used by the EE to manage the method's garbage collection,
// exception handling, stack-walking etc.
// This data can be parsed by an ICodeManager corresponding to that
// ICorJitCompiler.
//
// EECodeManager is an implementation of ICodeManager for a default format
// of GCInfo. Various ICorJitCompiler's are free to share this format so that
// they do not need to provide their own implementation of ICodeManager
// (though they are permitted to, if they want).
//
//*****************************************************************************

#ifndef _EETWAIN_H
#define _EETWAIN_H
//*****************************************************************************

#include <daccess.h>
#include "regdisp.h"
#include "corjit.h"     // For NativeVarInfo
#include "stackwalktypes.h"
#include "bitvector.h"
#include "gcinfotypes.h"

#if !defined(TARGET_X86)
#define USE_GC_INFO_DECODER
#endif

#ifdef TARGET_AMD64
#define HAS_LIGHTUNWIND
#endif

#define CHECK_APP_DOMAIN    0

#define NO_OVERRIDE_OFFSET (DWORD)-1

struct EHContext;

#ifdef DACCESS_COMPILE
typedef struct _DAC_SLOT_LOCATION
{
    int reg;
    int regOffset;
    bool targetPtr;

    _DAC_SLOT_LOCATION(int _reg, int _regOffset, bool _targetPtr)
        : reg(_reg), regOffset(_regOffset), targetPtr(_targetPtr)
    {
    }
} DacSlotLocation;
#endif

typedef void (*GCEnumCallback)(
    LPVOID          hCallback,      // callback data
    OBJECTREF*      pObject,        // address of object-reference we are reporting
    uint32_t        flags           // is this a pinned and/or interior pointer
    DAC_ARG(DacSlotLocation loc)    // where the reference came from
);

/******************************************************************************
  The stackwalker maintains some state on behalf of ICodeManager.
*/

const int CODEMAN_STATE_SIZE = 512;

struct CodeManState
{
    DWORD       dwIsSet; // Is set to 0 by the stackwalk as appropriate
    BYTE        stateBuf[CODEMAN_STATE_SIZE];
};

/******************************************************************************
   These flags are used by some functions, although not all combinations might
   make sense for all functions.
*/

enum ICodeManagerFlags
{
    ActiveStackFrame =  0x0001, // this is the currently active function
    ExecutionAborted =  0x0002, // execution of this function has been aborted
                                    // (i.e. it will not continue execution at the
                                    // current location)
    UpdateAllRegs   =   0x0008, // update full register set
    CodeAltered     =   0x0010, // code of that function might be altered
                                    // (e.g. by debugger), need to call EE
                                    // for original code
    SpeculativeStackwalk
                    =   0x0020, // we're in the middle of a stackwalk seeded
                                    // by an untrusted source (e.g., sampling profiler)

    ParentOfFuncletStackFrame
                    =   0x0040, // A funclet for this frame was previously reported
    NoReportUntracked
                    =   0x0080, // EnumGCRefs/EnumerateLiveSlots should *not* include
                                // any untracked slots

    LightUnwind     =   0x0100, // Unwind just enough to get return addresses
    ReportFPBasedSlotsOnly
                    =   0x0200, // EnumGCRefs/EnumerateLiveSlots should only include
                                // slots that are based on the frame pointer
};

//*****************************************************************************
//
// EECodeInfo is used by ICodeManager to get information about the
// method whose GCInfo is being processed.
// It is useful so that some information which is available elsewhere does
// not need to be cached in the GCInfo.
//

class EECodeInfo;

enum GenericParamContextType
{
    GENERIC_PARAM_CONTEXT_NONE = 0,
    GENERIC_PARAM_CONTEXT_THIS = 1,
    GENERIC_PARAM_CONTEXT_METHODDESC = 2,
    GENERIC_PARAM_CONTEXT_METHODTABLE = 3
};

//*****************************************************************************
//
// ICodeManager is the abstract class that all CodeManagers
// must inherit from.  This will probably need to move into
// cor.h and become a real com interface.
//
//*****************************************************************************

class ICodeManager
{
    VPTR_BASE_VTABLE_CLASS_AND_CTOR(ICodeManager)

public:

/*
    Last chance for the runtime support to do fixups in the context
    before execution continues inside a filter, catch handler, or fault/finally
*/

enum ContextType
{
    FILTER_CONTEXT,
    CATCH_CONTEXT,
    FINALLY_CONTEXT
};

/* Type of funclet corresponding to a shadow stack-pointer */

enum
{
    SHADOW_SP_IN_FILTER = 0x1,
    SHADOW_SP_FILTER_DONE = 0x2,
    SHADOW_SP_BITS = 0x3
};

#ifndef DACCESS_COMPILE
#ifndef FEATURE_EH_FUNCLETS
virtual void FixContext(ContextType     ctxType,
                        EHContext      *ctx,
                        EECodeInfo     *pCodeInfo,
                        DWORD           dwRelOffset,
                        DWORD           nestingLevel,
                        OBJECTREF       thrownObject,
                        CodeManState   *pState,
                        size_t       ** ppShadowSP,             // OUT
                        size_t       ** ppEndRegion) = 0;       // OUT
#endif // !FEATURE_EH_FUNCLETS
#endif // #ifndef DACCESS_COMPILE

#ifdef TARGET_X86
/*
    Gets the ambient stack pointer value at the given nesting level within
    the method.
*/
virtual TADDR GetAmbientSP(PREGDISPLAY     pContext,
                           EECodeInfo     *pCodeInfo,
                           DWORD           dwRelOffset,
                           DWORD           nestingLevel,
                           CodeManState   *pState) = 0;
#endif // TARGET_X86

/*
    Get the number of bytes used for stack parameters.
    This is currently only used on x86.
*/
virtual ULONG32 GetStackParameterSize(EECodeInfo* pCodeInfo) = 0;

/*
    Unwind the current stack frame, i.e. update the virtual register
    set in pContext. This will be similar to the state after the function
    returns back to caller (IP points to after the call, Frame and Stack
    pointer has been reset, callee-saved registers restored
    (if UpdateAllRegs), callee-UNsaved registers are trashed)
    Returns success of operation.
*/
virtual bool UnwindStackFrame(PREGDISPLAY     pContext,
                              EECodeInfo     *pCodeInfo,
                              unsigned        flags,
                              CodeManState   *pState) = 0;

/*
    Is the function currently at a "GC safe point" ?
    Can call EnumGcRefs() successfully
*/
virtual bool IsGcSafe(EECodeInfo     *pCodeInfo,
                      DWORD           dwRelOffset) = 0;

#if defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
virtual bool HasTailCalls(EECodeInfo *pCodeInfo) = 0;
#endif // TARGET_ARM || TARGET_ARM64 || TARGET_LOONGARCH64 || TARGET_RISCV64

#if defined(TARGET_AMD64) && defined(_DEBUG)
/*
    Locates the end of the last interruptible region in the given code range.
    Returns 0 if the entire range is uninterruptible.  Returns the end point
    if the entire range is interruptible.
*/
virtual unsigned FindEndOfLastInterruptibleRegion(unsigned curOffset,
                                                  unsigned endOffset,
                                                  GCInfoToken gcInfoToken) = 0;
#endif // TARGET_AMD64 && _DEBUG

/*
    Enumerate all live object references in that function using
    the virtual register set. Same reference location cannot be enumerated
    multiple times (but all differenct references pointing to the same
    object have to be individually enumerated).
    Returns success of operation.
*/
virtual bool EnumGcRefs(PREGDISPLAY     pContext,
                        EECodeInfo     *pCodeInfo,
                        unsigned        flags,
                        GCEnumCallback  pCallback,
                        LPVOID          hCallBack,
                        DWORD           relOffsetOverride = NO_OVERRIDE_OFFSET) = 0;

/*
    For a non-static method, "this" pointer is passed in as argument 0.
    However, if there is a "ldarga 0" or "starg 0" in the IL,
    JIT will create a copy of arg0 and redirect all "ldarg(a) 0" and "starg 0" to this copy.
    (See Compiler::lvaArg0Var for more details.)

    The following method returns the original "this" argument, i.e. the one that is passed in,
    if it is a non-static method AND the object is still alive.
    Returns NULL in all other cases.
*/
virtual OBJECTREF GetInstance(PREGDISPLAY     pContext,
                              EECodeInfo*     pCodeInfo) = 0;

/*
    Returns the extra argument passed to shared generic code if it is still alive.
    Returns NULL in all other cases.
*/
virtual PTR_VOID GetParamTypeArg(PREGDISPLAY     pContext,
                                 EECodeInfo *    pCodeInfo) = 0;

// Returns the type of the context parameter (this, methodtable, methoddesc, or none)
virtual GenericParamContextType GetParamContextType(PREGDISPLAY     pContext,
                                                    EECodeInfo *    pCodeInfo) = 0;

/*
    Returns the offset of the GuardStack cookie if it exists.
    Returns NULL if there is no cookie.
*/
virtual void * GetGSCookieAddr(PREGDISPLAY     pContext,
                               EECodeInfo    * pCodeInfo,
                               CodeManState  * pState) = 0;

#ifndef USE_GC_INFO_DECODER
/*
  Returns true if the given IP is in the given method's prolog or an epilog.
*/
virtual bool IsInPrologOrEpilog(DWORD  relPCOffset,
                                GCInfoToken gcInfoToken,
                                size_t* prologSize) = 0;

/*
  Returns true if the given IP is in the synchronized region of the method (valid for synchronized methods only)
*/
virtual bool IsInSynchronizedRegion(
                DWORD       relOffset,
                GCInfoToken gcInfoToken,
                unsigned    flags) = 0;
#endif // !USE_GC_INFO_DECODER

/*
  Returns the size of a given function as reported in the GC info (does
  not take procedure splitting into account).  For the actual size of
  the hot region call IJitManager::JitTokenToMethodHotSize.
*/
virtual size_t GetFunctionSize(GCInfoToken gcInfoToken) = 0;

/*
*  Get information necessary for return address hijacking of the method represented by the gcInfoToken.
*  If it can be hijacked, it sets the returnKind output parameter to the kind of the return value and
*  returns true.
*  If hijacking is not possible for some reason, it return false.
*/
virtual bool GetReturnAddressHijackInfo(GCInfoToken gcInfoToken, ReturnKind * returnKind) = 0;

#ifndef USE_GC_INFO_DECODER
/*
  Returns the size of the frame (barring localloc)
*/
virtual unsigned int GetFrameSize(GCInfoToken gcInfoToken) = 0;
#endif // USE_GC_INFO_DECODER

#ifndef DACCESS_COMPILE

/* Debugger API */

#ifndef FEATURE_EH_FUNCLETS
virtual const BYTE*     GetFinallyReturnAddr(PREGDISPLAY pReg)=0;

virtual BOOL            IsInFilter(GCInfoToken gcInfoToken,
                                   unsigned offset,
                                   PCONTEXT pCtx,
                                   DWORD curNestLevel) = 0;

virtual BOOL            LeaveFinally(GCInfoToken gcInfoToken,
                                     unsigned offset,
                                     PCONTEXT pCtx) = 0;

virtual void            LeaveCatch(GCInfoToken gcInfoToken,
                                   unsigned offset,
                                   PCONTEXT pCtx)=0;
#endif // FEATURE_EH_FUNCLETS

#ifdef FEATURE_REMAP_FUNCTION

/*
    Last chance for the runtime support to do fixups in the context
    before execution continues inside an EnC updated function.
*/

virtual HRESULT FixContextForEnC(PCONTEXT        pCtx,
                                    EECodeInfo    * pOldCodeInfo,
               const ICorDebugInfo::NativeVarInfo * oldMethodVars,
                                    SIZE_T          oldMethodVarsCount,
                                    EECodeInfo    * pNewCodeInfo,
               const ICorDebugInfo::NativeVarInfo * newMethodVars,
                                    SIZE_T          newMethodVarsCount) = 0;

#endif // FEATURE_REMAP_FUNCTION

#endif // #ifndef DACCESS_COMPILE


#ifdef DACCESS_COMPILE
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags) = 0;
#endif
};

//*****************************************************************************
//
// EECodeManager is the EE's implementation of the ICodeManager which
// supports the default format of GCInfo.
//
//*****************************************************************************

struct hdrInfo;

class EECodeManager : public ICodeManager {

    VPTR_VTABLE_CLASS_AND_CTOR(EECodeManager, ICodeManager)

public:


#ifndef DACCESS_COMPILE
#ifndef FEATURE_EH_FUNCLETS
/*
    Last chance for the runtime support to do fixups in the context
    before execution continues inside a filter, catch handler, or finally
*/
virtual
void FixContext(ContextType     ctxType,
                EHContext      *ctx,
                EECodeInfo     *pCodeInfo,
                DWORD           dwRelOffset,
                DWORD           nestingLevel,
                OBJECTREF       thrownObject,
                CodeManState   *pState,
                size_t       ** ppShadowSP,             // OUT
                size_t       ** ppEndRegion);           // OUT
#endif // !FEATURE_EH_FUNCLETS
#endif // #ifndef DACCESS_COMPILE

#ifdef TARGET_X86
/*
    Gets the ambient stack pointer value at the given nesting level within
    the method.
*/
virtual
TADDR GetAmbientSP(PREGDISPLAY     pContext,
                   EECodeInfo     *pCodeInfo,
                   DWORD           dwRelOffset,
                   DWORD           nestingLevel,
                   CodeManState   *pState);
#endif // TARGET_X86

/*
    Get the number of bytes used for stack parameters.
    This is currently only used on x86.
*/
virtual
ULONG32 GetStackParameterSize(EECodeInfo* pCodeInfo);

/*
    Unwind the current stack frame, i.e. update the virtual register
    set in pContext. This will be similar to the state after the function
    returns back to caller (IP points to after the call, Frame and Stack
    pointer has been reset, callee-saved registers restored
    (if UpdateAllRegs), callee-UNsaved registers are trashed)
    Returns success of operation.
*/
virtual
bool UnwindStackFrame(
                PREGDISPLAY     pContext,
                EECodeInfo     *pCodeInfo,
                unsigned        flags,
                CodeManState   *pState);

#ifdef HAS_LIGHTUNWIND
enum LightUnwindFlag
{
    UnwindCurrentStackFrame,
    EnsureCallerStackFrameIsValid
};

/*
  *  Light unwind the current stack frame, using provided cache entry.
  *  only pPC and Esp of pContext are updated. And pEbp if necessary.
  */

static
void LightUnwindStackFrame(
             PREGDISPLAY pRD,
             EECodeInfo     *pCodeInfo,
             LightUnwindFlag flag);
#endif // HAS_LIGHTUNWIND

/*
    Is the function currently at a "GC safe point" ?
    Can call EnumGcRefs() successfully
*/
virtual
bool IsGcSafe(  EECodeInfo     *pCodeInfo,
                DWORD           dwRelOffset);

static
bool InterruptibleSafePointsEnabled();

#if defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
virtual
bool HasTailCalls(EECodeInfo *pCodeInfo);
#endif // TARGET_ARM || TARGET_ARM64 || TARGET_LOONGARCH64 || defined(TARGET_RISCV64)

#if defined(TARGET_AMD64) && defined(_DEBUG)
/*
    Locates the end of the last interruptible region in the given code range.
    Returns 0 if the entire range is uninterruptible.  Returns the end point
    if the entire range is interruptible.
*/
virtual
unsigned FindEndOfLastInterruptibleRegion(unsigned curOffset,
                                          unsigned endOffset,
                                          GCInfoToken gcInfoToken);
#endif // TARGET_AMD64 && _DEBUG

/*
    Enumerate all live object references in that function using
    the virtual register set. Same reference location cannot be enumerated
    multiple times (but all differenct references pointing to the same
    object have to be individually enumerated).
    Returns success of operation.
*/
virtual
bool EnumGcRefs(PREGDISPLAY     pContext,
                EECodeInfo     *pCodeInfo,
                unsigned        flags,
                GCEnumCallback  pCallback,
                LPVOID          hCallBack,
                DWORD           relOffsetOverride = NO_OVERRIDE_OFFSET);

#ifdef FEATURE_CONSERVATIVE_GC
// Temporary conservative collection, for testing purposes, until we have
// accurate gc info from the JIT.
bool EnumGcRefsConservative(PREGDISPLAY     pRD,
                            EECodeInfo     *pCodeInfo,
                            unsigned        flags,
                            GCEnumCallback  pCallBack,
                            LPVOID          hCallBack);
#endif // FEATURE_CONSERVATIVE_GC

virtual
OBJECTREF GetInstance(
                PREGDISPLAY     pContext,
                EECodeInfo *    pCodeInfo);

/*
    Returns the extra argument passed to shared generic code if it is still alive.
    Returns NULL in all other cases.
*/
virtual
PTR_VOID GetParamTypeArg(PREGDISPLAY     pContext,
                         EECodeInfo *    pCodeInfo);

// Returns the type of the context parameter (this, methodtable, methoddesc, or none)
virtual GenericParamContextType GetParamContextType(PREGDISPLAY     pContext,
                                                    EECodeInfo *    pCodeInfo);

#if defined(FEATURE_EH_FUNCLETS) && defined(USE_GC_INFO_DECODER)
/*
    Returns the generics token.  This is used by GetInstance() and GetParamTypeArg() on WIN64.
*/
static
PTR_VOID GetExactGenericsToken(PREGDISPLAY     pContext,
                               EECodeInfo *    pCodeInfo);

static
PTR_VOID GetExactGenericsToken(SIZE_T          baseStackSlot,
                               EECodeInfo *    pCodeInfo);


#endif // FEATURE_EH_FUNCLETS && USE_GC_INFO_DECODER

/*
    Returns the offset of the GuardStack cookie if it exists.
    Returns NULL if there is no cookie.
*/
virtual
void * GetGSCookieAddr(PREGDISPLAY     pContext,
                       EECodeInfo    * pCodeInfo,
                       CodeManState  * pState);


#ifndef USE_GC_INFO_DECODER
/*
  Returns true if the given IP is in the given method's prolog or an epilog.
*/
virtual
bool IsInPrologOrEpilog(
                DWORD       relOffset,
                GCInfoToken gcInfoToken,
                size_t*     prologSize);

/*
  Returns true if the given IP is in the synchronized region of the method (valid for synchronized functions only)
*/
virtual
bool IsInSynchronizedRegion(
                DWORD       relOffset,
                GCInfoToken gcInfoToken,
                unsigned    flags);
#endif // !USE_GC_INFO_DECODER

/*
  Returns the size of a given function.
*/
virtual
size_t GetFunctionSize(GCInfoToken gcInfoToken);

/*
*  Get information necessary for return address hijacking of the method represented by the gcInfoToken.
*  If it can be hijacked, it sets the returnKind output parameter to the kind of the return value and
*  returns true.
*  If hijacking is not possible for some reason, it return false.
*/
virtual bool GetReturnAddressHijackInfo(GCInfoToken gcInfoToken, ReturnKind * returnKind);

#ifndef USE_GC_INFO_DECODER
/*
  Returns the size of the frame (barring localloc)
*/
virtual
unsigned int GetFrameSize(GCInfoToken gcInfoToken);
#endif // USE_GC_INFO_DECODER

#ifndef DACCESS_COMPILE

#ifndef FEATURE_EH_FUNCLETS
virtual const BYTE* GetFinallyReturnAddr(PREGDISPLAY pReg);
virtual BOOL IsInFilter(GCInfoToken gcInfoToken,
                        unsigned offset,
                        PCONTEXT pCtx,
                          DWORD curNestLevel);
virtual BOOL LeaveFinally(GCInfoToken gcInfoToken,
                          unsigned offset,
                          PCONTEXT pCtx);
virtual void LeaveCatch(GCInfoToken gcInfoToken,
                         unsigned offset,
                         PCONTEXT pCtx);
#endif // FEATURE_EH_FUNCLETS

#ifdef FEATURE_REMAP_FUNCTION
/*
    Last chance for the runtime support to do fixups in the context
    before execution continues inside an EnC updated function.
*/
virtual
HRESULT FixContextForEnC(PCONTEXT        pCtx,
                            EECodeInfo    * pOldCodeInfo,
       const ICorDebugInfo::NativeVarInfo * oldMethodVars,
                            SIZE_T          oldMethodVarsCount,
                            EECodeInfo    * pNewCodeInfo,
       const ICorDebugInfo::NativeVarInfo * newMethodVars,
                            SIZE_T          newMethodVarsCount);
#endif // FEATURE_REMAP_FUNCTION

#endif // #ifndef DACCESS_COMPILE

#ifdef FEATURE_EH_FUNCLETS
    static void EnsureCallerContextIsValid( PREGDISPLAY pRD, EECodeInfo * pCodeInfo = NULL, unsigned flags = 0);
    static size_t GetCallerSp( PREGDISPLAY  pRD );
#ifdef TARGET_X86
    static size_t GetResumeSp( PCONTEXT  pContext );
#endif // TARGET_X86
#endif // FEATURE_EH_FUNCLETS

#ifdef DACCESS_COMPILE
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

};

#ifdef TARGET_X86
#include "gc_unwind_x86.h"

/*****************************************************************************
  How the stackwalkers buffer will be interpreted
*/

struct CodeManStateBuf
{
    DWORD       hdrInfoSize;
    hdrInfo     hdrInfoBody;
};

#endif

//*****************************************************************************
#endif // _EETWAIN_H
//*****************************************************************************
