// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

#if !defined(_TARGET_X86_)
#define USE_GC_INFO_DECODER
#endif

#if (defined(_TARGET_X86_) && !defined(FEATURE_PAL)) || defined(_TARGET_AMD64_)
#define HAS_QUICKUNWIND
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
    OBJECTREF*      pObject,        // address of obect-reference we are reporting
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
    AbortingCall    =   0x0004, // The current call will never return
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
#ifndef WIN64EXCEPTIONS
virtual void FixContext(ContextType     ctxType,
                        EHContext      *ctx,
                        EECodeInfo     *pCodeInfo,
                        DWORD           dwRelOffset,
                        DWORD           nestingLevel,
                        OBJECTREF       thrownObject,
                        CodeManState   *pState,
                        size_t       ** ppShadowSP,             // OUT
                        size_t       ** ppEndRegion) = 0;       // OUT
#endif // !WIN64EXCEPTIONS
#endif // #ifndef DACCESS_COMPILE

#ifdef _TARGET_X86_
/*
    Gets the ambient stack pointer value at the given nesting level within
    the method.
*/
virtual TADDR GetAmbientSP(PREGDISPLAY     pContext,
                           EECodeInfo     *pCodeInfo,
                           DWORD           dwRelOffset,
                           DWORD           nestingLevel,
                           CodeManState   *pState) = 0;
#endif // _TARGET_X86_

/*
    Get the number of bytes used for stack parameters.
    This is currently only used on x86.
*/
virtual ULONG32 GetStackParameterSize(EECodeInfo* pCodeInfo) = 0;

#ifndef CROSSGEN_COMPILE
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
                              CodeManState   *pState,
                              StackwalkCacheUnwindInfo  *pUnwindInfo) = 0;
#endif // CROSSGEN_COMPILE

/*
    Is the function currently at a "GC safe point" ?
    Can call EnumGcRefs() successfully
*/
virtual bool IsGcSafe(EECodeInfo     *pCodeInfo,
                      DWORD           dwRelOffset) = 0;

#if defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
virtual bool HasTailCalls(EECodeInfo *pCodeInfo) = 0;
#endif // _TARGET_ARM_ || _TARGET_ARM64_

#if defined(_TARGET_AMD64_) && defined(_DEBUG)
/*
    Locates the end of the last interruptible region in the given code range.
    Returns 0 if the entire range is uninterruptible.  Returns the end point
    if the entire range is interruptible.
*/
virtual unsigned FindEndOfLastInterruptibleRegion(unsigned curOffset,
                                                  unsigned endOffset,
                                                  GCInfoToken gcInfoToken) = 0;
#endif // _TARGET_AMD64_ && _DEBUG

#ifndef CROSSGEN_COMPILE
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
#endif // !CROSSGEN_COMPILE

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
/*
    Return the address of the local security object reference
    (if available).
*/
virtual OBJECTREF* GetAddrOfSecurityObject(CrawlFrame *pCF) = 0;
#endif // !DACCESS_COMPILE && !CROSSGEN_COMPILE

#ifndef CROSSGEN_COMPILE
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
#endif // !CROSSGEN_COMPILE

#ifndef CROSSGEN_COMPILE
/*
    Returns the extra argument passed to to shared generic code if it is still alive.
    Returns NULL in all other cases.
*/
virtual PTR_VOID GetParamTypeArg(PREGDISPLAY     pContext,
                                 EECodeInfo *    pCodeInfo) = 0;
#endif // !CROSSGEN_COMPILE

// Returns the type of the context parameter (this, methodtable, methoddesc, or none)
virtual GenericParamContextType GetParamContextType(PREGDISPLAY     pContext,
                                                    EECodeInfo *    pCodeInfo) = 0;

#ifndef CROSSGEN_COMPILE
/*
    Returns the offset of the GuardStack cookie if it exists.
    Returns NULL if there is no cookie.
*/
virtual void * GetGSCookieAddr(PREGDISPLAY     pContext,
                               EECodeInfo    * pCodeInfo,
                               CodeManState  * pState) = 0;
#endif

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
Returns the ReturnKind of a given function as reported in the GC info.
*/

virtual ReturnKind GetReturnKind(GCInfoToken gcInfotoken) = 0;

#ifndef USE_GC_INFO_DECODER
/*
  Returns the size of the frame (barring localloc)
*/
virtual unsigned int GetFrameSize(GCInfoToken gcInfoToken) = 0;
#endif // USE_GC_INFO_DECODER

#ifndef DACCESS_COMPILE

/* Debugger API */

#ifndef WIN64EXCEPTIONS
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
#endif // WIN64EXCEPTIONS

#ifdef EnC_SUPPORTED

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

#endif // EnC_SUPPORTED

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
#ifndef WIN64EXCEPTIONS
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
#endif // !WIN64EXCEPTIONS
#endif // #ifndef DACCESS_COMPILE

#ifdef _TARGET_X86_
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
#endif // _TARGET_X86_

/*
    Get the number of bytes used for stack parameters.
    This is currently only used on x86.
*/
virtual 
ULONG32 GetStackParameterSize(EECodeInfo* pCodeInfo);

#ifndef CROSSGEN_COMPILE
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
                CodeManState   *pState,
                StackwalkCacheUnwindInfo  *pUnwindInfo);
#endif // CROSSGEN_COMPILE

#ifdef HAS_QUICKUNWIND
enum QuickUnwindFlag
{
    UnwindCurrentStackFrame,
    EnsureCallerStackFrameIsValid
};

/*
  *  Light unwind the current stack frame, using provided cache entry.
  *  only pPC and Esp of pContext are updated. And pEbp if necessary.
  */

static
void QuickUnwindStackFrame(
             PREGDISPLAY pRD,
             StackwalkCacheEntry *pCacheEntry,
             QuickUnwindFlag flag);
#endif // HAS_QUICKUNWIND

/*
    Is the function currently at a "GC safe point" ?
    Can call EnumGcRefs() successfully
*/
virtual
bool IsGcSafe(  EECodeInfo     *pCodeInfo,
                DWORD           dwRelOffset);

#if defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
virtual
bool HasTailCalls(EECodeInfo *pCodeInfo);
#endif // _TARGET_ARM_ || _TARGET_ARM64_

#if defined(_TARGET_AMD64_) && defined(_DEBUG)
/*
    Locates the end of the last interruptible region in the given code range.
    Returns 0 if the entire range is uninterruptible.  Returns the end point
    if the entire range is interruptible.
*/
virtual
unsigned FindEndOfLastInterruptibleRegion(unsigned curOffset,
                                          unsigned endOffset,
                                          GCInfoToken gcInfoToken);
#endif // _TARGET_AMD64_ && _DEBUG

#ifndef CROSSGEN_COMPILE
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
#endif // !CROSSGEN_COMPILE

#ifdef FEATURE_CONSERVATIVE_GC
// Temporary conservative collection, for testing purposes, until we have
// accurate gc info from the JIT.
bool EnumGcRefsConservative(PREGDISPLAY     pRD,
                            EECodeInfo     *pCodeInfo,
                            unsigned        flags,
                            GCEnumCallback  pCallBack,
                            LPVOID          hCallBack);
#endif // FEATURE_CONSERVATIVE_GC

#ifdef _TARGET_X86_
/*
   Return the address of the local security object reference
   using data that was previously cached before in UnwindStackFrame
   using StackwalkCacheUnwindInfo
*/
static OBJECTREF* GetAddrOfSecurityObjectFromCachedInfo(
        PREGDISPLAY pRD,
        StackwalkCacheUnwindInfo * stackwalkCacheUnwindInfo);
#endif // _TARGET_X86_

#if !defined(DACCESS_COMPILE) && !defined(CROSSGEN_COMPILE)
virtual
OBJECTREF* GetAddrOfSecurityObject(CrawlFrame *pCF) DAC_UNEXPECTED();
#endif // !DACCESS_COMPILE && !CROSSGEN_COMPILE

#ifndef CROSSGEN_COMPILE
virtual
OBJECTREF GetInstance(
                PREGDISPLAY     pContext,
                EECodeInfo *    pCodeInfo);
#endif // !CROSSGEN_COMPILE

#ifndef CROSSGEN_COMPILE
/*
    Returns the extra argument passed to to shared generic code if it is still alive.
    Returns NULL in all other cases.
*/
virtual
PTR_VOID GetParamTypeArg(PREGDISPLAY     pContext,
                         EECodeInfo *    pCodeInfo);
#endif // !CROSSGEN_COMPILE

// Returns the type of the context parameter (this, methodtable, methoddesc, or none)
virtual GenericParamContextType GetParamContextType(PREGDISPLAY     pContext,
                                                    EECodeInfo *    pCodeInfo);

#if defined(WIN64EXCEPTIONS) && defined(USE_GC_INFO_DECODER) && !defined(CROSSGEN_COMPILE)
/*
    Returns the generics token.  This is used by GetInstance() and GetParamTypeArg() on WIN64.
*/
static 
PTR_VOID GetExactGenericsToken(PREGDISPLAY     pContext,
                               EECodeInfo *    pCodeInfo);

static 
PTR_VOID GetExactGenericsToken(SIZE_T          baseStackSlot,
                               EECodeInfo *    pCodeInfo);


#endif // WIN64EXCEPTIONS && USE_GC_INFO_DECODER && !CROSSGEN_COMPILE

#ifndef CROSSGEN_COMPILE
/*
    Returns the offset of the GuardStack cookie if it exists.
    Returns NULL if there is no cookie.
*/
virtual
void * GetGSCookieAddr(PREGDISPLAY     pContext,
                       EECodeInfo    * pCodeInfo,
                       CodeManState  * pState);
#endif


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
Returns the ReturnKind of a given function.
*/
virtual ReturnKind GetReturnKind(GCInfoToken gcInfotoken);

#ifndef USE_GC_INFO_DECODER
/*
  Returns the size of the frame (barring localloc)
*/
virtual
unsigned int GetFrameSize(GCInfoToken gcInfoToken);
#endif // USE_GC_INFO_DECODER

#ifndef DACCESS_COMPILE

#ifndef WIN64EXCEPTIONS
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
#endif // WIN64EXCEPTIONS

#ifdef EnC_SUPPORTED
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
#endif // EnC_SUPPORTED

#endif // #ifndef DACCESS_COMPILE

#ifdef WIN64EXCEPTIONS
    static void EnsureCallerContextIsValid( PREGDISPLAY pRD, StackwalkCacheEntry* pCacheEntry, EECodeInfo * pCodeInfo = NULL );
    static size_t GetCallerSp( PREGDISPLAY  pRD );
#ifdef _TARGET_X86_
    static size_t GetResumeSp( PCONTEXT  pContext );
#endif // _TARGET_X86_
#endif // WIN64EXCEPTIONS

#ifdef DACCESS_COMPILE
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

};

#ifdef _TARGET_X86_
bool UnwindStackFrame(PREGDISPLAY     pContext,
                      EECodeInfo     *pCodeInfo,
                      unsigned        flags,
                      CodeManState   *pState,
                      StackwalkCacheUnwindInfo  *pUnwindInfo);
#endif

/*****************************************************************************
 <TODO>ToDo: Do we want to include JIT/IL/target.h? </TODO>
 */

enum regNum
{
        REGI_EAX, REGI_ECX, REGI_EDX, REGI_EBX,
        REGI_ESP, REGI_EBP, REGI_ESI, REGI_EDI,
        REGI_COUNT,
        REGI_NA = REGI_COUNT
};

/*****************************************************************************
 Register masks
 */

enum RegMask
{
    RM_EAX = 0x01,
    RM_ECX = 0x02,
    RM_EDX = 0x04,
    RM_EBX = 0x08,
    RM_ESP = 0x10,
    RM_EBP = 0x20,
    RM_ESI = 0x40,
    RM_EDI = 0x80,

    RM_NONE = 0x00,
    RM_ALL = (RM_EAX|RM_ECX|RM_EDX|RM_EBX|RM_ESP|RM_EBP|RM_ESI|RM_EDI),
    RM_CALLEE_SAVED = (RM_EBP|RM_EBX|RM_ESI|RM_EDI),
    RM_CALLEE_TRASHED = (RM_ALL & ~RM_CALLEE_SAVED),
};

/*****************************************************************************
 *
 *  Helper to extract basic info from a method info block.
 */

struct hdrInfo
{
    unsigned int        methodSize;     // native code bytes
    unsigned int        argSize;        // in bytes
    unsigned int        stackSize;      // including callee saved registers
    unsigned int        rawStkSize;     // excluding callee saved registers
    ReturnKind          returnKind;     // The ReturnKind for this method.

    unsigned int        prologSize;

    // Size of the epilogs in the method.
    // For methods which use CEE_JMP, some epilogs may end with a "ret" instruction
    // and some may end with a "jmp". The epilogSize reported should be for the 
    // epilog with the smallest size.
    unsigned int        epilogSize;

    unsigned char       epilogCnt;
    bool                epilogEnd;      // is the epilog at the end of the method
    
    bool                ebpFrame;       // locals and arguments addressed relative to EBP
    bool                doubleAlign;    // is the stack double-aligned? locals addressed relative to ESP, and arguments relative to EBP
    bool                interruptible;  // intr. at all times (excluding prolog/epilog), not just call sites

    bool                securityCheck;  // has a slot for security object
    bool                handlers;       // has callable handlers
    bool                localloc;       // uses localloc
    bool                editNcontinue;  // has been compiled in EnC mode
    bool                varargs;        // is this a varargs routine
    bool                profCallbacks;  // does the method have Enter-Leave callbacks
    bool                genericsContext;// has a reported generic context paramter
    bool                genericsContextIsMethodDesc;// reported generic context parameter is methoddesc
    bool                isSpeculativeStackWalk; // is the stackwalk seeded by an untrusted source (e.g., sampling profiler)?

    // These always includes EBP for EBP-frames and double-aligned-frames
    RegMask             savedRegMask:8; // which callee-saved regs are saved on stack

    // Count of the callee-saved registers, excluding the frame pointer.
    // This does not include EBP for EBP-frames and double-aligned-frames.
    unsigned int        savedRegsCountExclFP;

    unsigned int        untrackedCnt;
    unsigned int        varPtrTableSize;
    unsigned int        argTabOffset;   // INVALID_ARGTAB_OFFSET if argtab must be reached by stepping through ptr tables
    unsigned int        gsCookieOffset; // INVALID_GS_COOKIE_OFFSET if there is no GuardStack cookie

    unsigned int        syncStartOffset; // start/end code offset of the protected region in synchronized methods.
    unsigned int        syncEndOffset;   // INVALID_SYNC_OFFSET if there not synchronized method
    unsigned int        syncEpilogStart; // The start of the epilog. Synchronized methods are guaranteed to have no more than one epilog.
    unsigned int        revPInvokeOffset; // INVALID_REV_PINVOKE_OFFSET if there is no Reverse PInvoke frame

    enum { NOT_IN_PROLOG = -1, NOT_IN_EPILOG = -1 };
    
    int                 prologOffs;     // NOT_IN_PROLOG if not in prolog
    int                 epilogOffs;     // NOT_IN_EPILOG if not in epilog. It is never 0

    //
    // Results passed back from scanArgRegTable
    //
    regNum              thisPtrResult;  // register holding "this"
    RegMask             regMaskResult;  // registers currently holding GC ptrs
    RegMask            iregMaskResult;  // iptr qualifier for regMaskResult
    unsigned            argHnumResult;
    PTR_CBYTE            argTabResult;  // Table of encoded offsets of pending ptr args
    unsigned              argTabBytes;  // Number of bytes in argTabResult[]

    // These next two are now large structs (i.e 132 bytes each)

    ptrArgTP            argMaskResult;  // pending arguments mask
    ptrArgTP           iargMaskResult;  // iptr qualifier for argMaskResult
};

/*****************************************************************************
  How the stackwalkers buffer will be interpreted
*/

struct CodeManStateBuf
{
    DWORD       hdrInfoSize;
    hdrInfo     hdrInfoBody;
};
//*****************************************************************************
#endif // _EETWAIN_H
//*****************************************************************************
