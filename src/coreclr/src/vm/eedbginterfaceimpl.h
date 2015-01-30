//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


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

#ifdef FEATURE_PREJIT
#include "corcompile.h"
#endif // FEATURE_PREJIT

#include "eeconfig.h"
#include "pefile.h"

class EEDbgInterfaceImpl : public EEDebugInterface
{
    VPTR_VTABLE_CLASS(EEDbgInterfaceImpl, EEDebugInterface);

public:

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

    void SetEEThreadPtr(VOID* newPtr);

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

    VOID *GetThreadDebuggerWord(Thread *thread);

    void SetThreadDebuggerWord(Thread *thread,
                               VOID *dw);

    BOOL IsManagedNativeCode(const BYTE *address);

    MethodDesc *GetNativeCodeMethodDesc(const PCODE address) DAC_UNEXPECTED();

    BOOL IsInPrologOrEpilog(const BYTE *address,
                            size_t* prologSize);

    void DetermineIfOffsetsInFilterOrHandler(const BYTE *functionAddress,
                                                  DebugOffsetToHandlerInfo *pOffsetToHandlerInfo,
                                                  unsigned offsetToHandlerInfoLength);

    void GetMethodRegionInfo(const PCODE pStart, 
                             PCODE     * pCold, 
                             size_t *hotSize,
                             size_t *coldSize);

#if defined(WIN64EXCEPTIONS)
    DWORD GetFuncletStartOffsets(const BYTE *pStart, DWORD* pStartOffsets, DWORD dwLength);
    StackFrame FindParentStackFrame(CrawlFrame* pCF);
#endif // WIN64EXCEPTIONS

    size_t GetFunctionSize(MethodDesc *pFD) DAC_UNEXPECTED();

    const PCODE GetFunctionAddress(MethodDesc *pFD);

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
                           SIZE_T *pTLSIsSpecialIndex,
                           SIZE_T *pTLSCantStopIndex,
                           SIZE_T *pTLSIndexOfPredefs,
                           SIZE_T *pEEThreadStateOffset,
                           SIZE_T *pEEThreadStateNCOffset,
                           SIZE_T *pEEThreadPGCDisabledOffset,
                           DWORD  *pEEThreadPGCDisabledValue,
                           SIZE_T *pEEThreadDebuggerWordOffset,
                           SIZE_T *pEEThreadFrameOffset,
                           SIZE_T *pEEThreadMaxNeededSize,
                           DWORD  *pEEThreadSteppingStateMask,
                           DWORD  *pEEMaxFrameValue,
                           SIZE_T *pEEThreadDebuggerFilterContextOffset,
                           SIZE_T *pEEThreadCantStopOffset,
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

#ifdef FEATURE_PREJIT
#ifndef DACCESS_COMPILE
    virtual void SetNGENDebugFlags(BOOL fAllowOpt)
    {
        LIMITED_METHOD_CONTRACT;
        PEFile::SetNGENDebugFlags(fAllowOpt);
    }

    virtual void GetNGENDebugFlags(BOOL *fAllowOpt)
    {
        LIMITED_METHOD_CONTRACT;
        PEFile::GetNGENDebugFlags(fAllowOpt);
    }
#endif
#endif // FEATURE_PREJIT

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
};

#endif // DEBUGGING_SUPPORTED

#endif // _eedbginterfaceimpl_h_
