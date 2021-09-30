// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*
 *
 * COM+99 EE to Debugger Interface Implementation
 *
 */
#ifndef _eedbginterfaceimpl_h_
#define _eedbginterfaceimpl_h_

#ifdef DEBUGGING_SUPPORTED

#include "common.h"
#include "corpriv.h"
#include "hash.h"
#include "class.h"
#include "excep.h"
#include "field.h"
#include "eetwain.h"
#include "jitinterface.h"
#include "stubmgr.h"

#include "eedbginterface.h"
#include "debugdebugger.h"

#include "eeconfig.h"
#include "pefile.h"

class EEDbgInterfaceImpl : public EEDebugInterface
{
    VPTR_VTABLE_CLASS_AND_CTOR(EEDbgInterfaceImpl, EEDebugInterface);

public:

    virtual ~EEDbgInterfaceImpl() {}

#ifndef DACCESS_COMPILE

    //
    // Setup and global data used by this interface.
    //
    static FORCEINLINE void Init(void)
    {
        g_pEEDbgInterfaceImpl = new EEDbgInterfaceImpl(); // new throws on failure
    }

    //
    // Cleanup any global data used by this interface.
    //
    static void Terminate(void);

#endif // #ifndef DACCESS_COMPILE

    Thread* GetThread(void);

    StackWalkAction StackWalkFramesEx(Thread* pThread,
                                             PREGDISPLAY pRD,
                                             PSTACKWALKFRAMESCALLBACK pCallback,
                                             VOID* pData,
                                      unsigned int flags);

    Frame *GetFrame(CrawlFrame *pCF);

    bool InitRegDisplay(Thread* pThread,
                        const PREGDISPLAY pRD,
            const PT_CONTEXT pctx,
                        bool validContext);

    BOOL IsStringObject(Object* o);

    BOOL IsTypedReference(MethodTable* pMT);

    WCHAR* StringObjectGetBuffer(StringObject* so);

    DWORD StringObjectGetStringLength(StringObject* so);

    void* GetObjectFromHandle(OBJECTHANDLE handle);

    OBJECTHANDLE GetHandleFromObject(void *obj,
                              bool fStrongNewRef,
                              AppDomain *pAppDomain);

    void DbgDestroyHandle(OBJECTHANDLE oh,
                          bool fStrongNewRef);

    OBJECTHANDLE GetThreadException(Thread *pThread);

    bool IsThreadExceptionNull(Thread *pThread);

    void ClearThreadException(Thread *pThread);

    bool StartSuspendForDebug(AppDomain *pAppDomain,
                              BOOL fHoldingThreadStoreLock);

    bool SweepThreadsForDebug(bool forceSync);

    void ResumeFromDebug(AppDomain *pAppDomain);

    void MarkThreadForDebugSuspend(Thread* pRuntimeThread);

    void MarkThreadForDebugStepping(Thread* pRuntimeThread,
                                    bool onOff);

    void SetThreadFilterContext(Thread *thread,
                                T_CONTEXT *context);

    T_CONTEXT *GetThreadFilterContext(Thread *thread);

#ifdef FEATURE_INTEROP_DEBUGGING
    VOID *GetThreadDebuggerWord();

    VOID SetThreadDebuggerWord(VOID *dw);
#endif

    BOOL IsManagedNativeCode(const BYTE *address);

    PCODE GetNativeCodeStartAddress(PCODE address) DAC_UNEXPECTED();

    MethodDesc *GetNativeCodeMethodDesc(const PCODE address) DAC_UNEXPECTED();

#ifndef USE_GC_INFO_DECODER
    BOOL IsInPrologOrEpilog(const BYTE *address,
                            size_t* prologSize);
#endif

    void DetermineIfOffsetsInFilterOrHandler(const BYTE *functionAddress,
                                                  DebugOffsetToHandlerInfo *pOffsetToHandlerInfo,
                                                  unsigned offsetToHandlerInfoLength);

    void GetMethodRegionInfo(const PCODE pStart,
                             PCODE     * pCold,
                             size_t *hotSize,
                             size_t *coldSize);

#if defined(FEATURE_EH_FUNCLETS)
    DWORD GetFuncletStartOffsets(const BYTE *pStart, DWORD* pStartOffsets, DWORD dwLength);
    StackFrame FindParentStackFrame(CrawlFrame* pCF);
#endif // FEATURE_EH_FUNCLETS

    size_t GetFunctionSize(MethodDesc *pFD) DAC_UNEXPECTED();

    PCODE GetFunctionAddress(MethodDesc *pFD);

    void DisablePreemptiveGC(void);

    void EnablePreemptiveGC(void);

    bool IsPreemptiveGCDisabled(void);

    DWORD MethodDescIsStatic(MethodDesc *pFD);

    Module *MethodDescGetModule(MethodDesc *pFD);

    COR_ILMETHOD* MethodDescGetILHeader(MethodDesc *pFD);

    ULONG MethodDescGetRVA(MethodDesc *pFD);

    MethodDesc *FindLoadedMethodRefOrDef(Module* pModule,
                                          mdToken memberRef);

    MethodDesc *LoadMethodDef(Module* pModule,
                              mdMethodDef methodDef,
                              DWORD numGenericArgs = 0,
                              TypeHandle *pGenericArgs = NULL,
                              TypeHandle *pOwnerTypeRes = NULL);

    TypeHandle FindLoadedClass(Module *pModule,
                             mdTypeDef classToken);

    TypeHandle FindLoadedInstantiation(Module *pModule,
                                       mdTypeDef typeDef,
                                       DWORD numGenericArgs,
                                       TypeHandle *pGenericArgs);

    TypeHandle FindLoadedFnptrType(TypeHandle *inst,
                                   DWORD ntypars);

    TypeHandle FindLoadedPointerOrByrefType(CorElementType et,
                                            TypeHandle elemtype);

    TypeHandle FindLoadedArrayType(CorElementType et,
                                   TypeHandle elemtype,
                                   unsigned rank);

    TypeHandle FindLoadedElementType(CorElementType et);

    TypeHandle LoadClass(Module *pModule,
                       mdTypeDef classToken);

    TypeHandle LoadInstantiation(Module *pModule,
                                 mdTypeDef typeDef,
                                 DWORD numGenericArgs,
                                 TypeHandle *pGenericArgs);

    TypeHandle LoadArrayType(CorElementType et,
                             TypeHandle elemtype,
                             unsigned rank);

    TypeHandle LoadPointerOrByrefType(CorElementType et,
                                      TypeHandle elemtype);

    TypeHandle LoadFnptrType(TypeHandle *inst,
                             DWORD ntypars);

    TypeHandle LoadElementType(CorElementType et);

    __checkReturn
    HRESULT GetMethodImplProps(Module *pModule,
                               mdToken tk,
                               DWORD *pRVA,
                               DWORD *pImplFlags);

    HRESULT GetParentToken(Module *pModule,
                           mdToken tk,
                           mdToken *pParentToken);

    void MarkDebuggerAttached(void);

    void MarkDebuggerUnattached(void);

#ifdef EnC_SUPPORTED

    // Apply an EnC edit to the specified module
    // This function should never return.
    HRESULT EnCApplyChanges(EditAndContinueModule *pModule,
                            DWORD cbMetadata,
                            BYTE *pMetadata,
                            DWORD cbIL,
                            BYTE *pIL);

    // Remap execution to the latest version of an edited method
    void ResumeInUpdatedFunction(EditAndContinueModule *pModule,
                                 MethodDesc *pFD,
                                 void *debuggerFuncHandle,
                                 SIZE_T resumeIP,
                                 T_CONTEXT *pContext);
  #endif // EnC_SUPPORTED

    bool CrawlFrameIsGcSafe(CrawlFrame *pCF);

    bool IsStub(const BYTE *ip);

    bool DetectHandleILStubs(Thread *thread);

    bool TraceStub(const BYTE *ip,
                   TraceDestination *trace);

    bool FollowTrace(TraceDestination *trace);

    bool TraceFrame(Thread *thread,
                    Frame *frame,
                    BOOL fromPatch,
                    TraceDestination *trace,
                    REGDISPLAY *regs);

    bool TraceManager(Thread *thread,
                      StubManager *stubManager,
                      TraceDestination *trace,
                      T_CONTEXT *context,
                      BYTE **pRetAddr);

    void EnableTraceCall(Thread *thread);

    void DisableTraceCall(Thread *thread);

    void GetRuntimeOffsets(SIZE_T *pTLSIndex,
                           SIZE_T *pTLSEEThreadOffset,
                           SIZE_T *pTLSIsSpecialOffset,
                           SIZE_T *pTLSCantStopOffset,
                           SIZE_T *pEEThreadStateOffset,
                           SIZE_T *pEEThreadStateNCOffset,
                           SIZE_T *pEEThreadPGCDisabledOffset,
                           DWORD  *pEEThreadPGCDisabledValue,
                           SIZE_T *pEEThreadFrameOffset,
                           SIZE_T *pEEThreadMaxNeededSize,
                           DWORD  *pEEThreadSteppingStateMask,
                           DWORD  *pEEMaxFrameValue,
                           SIZE_T *pEEThreadDebuggerFilterContextOffset,
                           SIZE_T *pEEFrameNextOffset,
                           DWORD  *pEEIsManagedExceptionStateMask);

    void DebuggerModifyingLogSwitch (int iNewLevel,
                                     const WCHAR *pLogSwitchName);

    HRESULT SetIPFromSrcToDst(Thread *pThread,
                              SLOT addrStart,
                              DWORD offFrom,
                              DWORD offTo,
                              bool fCanSetIPOnly,
                              PREGDISPLAY pReg,
                              PT_CONTEXT pCtx,
                              void *pDji,
                              EHRangeTree *pEHRT);

    void SetDebugState(Thread *pThread,
                       CorDebugThreadState state);

    void SetAllDebugState(Thread *et,
                          CorDebugThreadState state);

    // This is pretty much copied from VM\COMSynchronizable's
    // INT32 __stdcall ThreadNative::GetThreadState, so propogate changes
    // to both functions
    CorDebugUserState GetPartialUserState( Thread *pThread );

#ifdef DACCESS_COMPILE
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    virtual unsigned GetSizeForCorElementType(CorElementType etyp);

#ifndef DACCESS_COMPILE
    virtual BOOL ObjIsInstanceOf(Object *pElement, TypeHandle toTypeHnd);
#endif

    virtual void ClearAllDebugInterfaceReferences(void);

#ifndef DACCESS_COMPILE
#ifdef _DEBUG
    virtual void ObjectRefFlush(Thread *pThread);
#endif
#endif

#ifndef DACCESS_COMPILE
    virtual BOOL AdjustContextForJITHelpersForDebugger(CONTEXT* context);
#endif
};

#endif // DEBUGGING_SUPPORTED

#endif // _eedbginterfaceimpl_h_
