// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// COM+99 EE to Debugger Interface Header
//



#ifndef _eedbginterface_h_
#define _eedbginterface_h_

#include "common.h"
#include "corpriv.h"
#include "hash.h"
#include "class.h"
#include "excep.h"
#include "threads.h"
#include "field.h"
#include "stackwalk.h"

#ifdef EnC_SUPPORTED
#include "encee.h"
#endif

#include "cordebug.h"
#include "../debug/inc/common.h"

class MethodDesc;
class Frame;
//
// The purpose of this object is to provide EE funcationality back to
// the debugger. This represents the entire set of EE functions used
// by the debugger.
//
// We will make this interface smaller over time to minimize the link
// between the EE and the Debugger.
//
//
typedef BOOL (*HashMapEnumCallback)(HashMap* h,
                                    void* pData,
                                    ULONG value);

typedef enum AttachAppDomainEventsEnum
{
    ONLY_SEND_APP_DOMAIN_CREATE_EVENTS,
    DONT_SEND_CLASS_EVENTS,
    ONLY_SEND_CLASS_EVENTS
} AttachAppDomainEventsEnum;

typedef VPTR(class EEDebugInterface) PTR_EEDebugInterface;

// Used for communicating EH Handler info between the LS and EE (DetermineIfOffsetsInFilterOrHandler)
struct DebugOffsetToHandlerInfo
{
    // Native offset of interest, or -1 if this entry should be ignored
    SIZE_T offset;

    // Set to true by the EE if the specified native offset is in an EH filter or handler.
    BOOL isInFilterOrHandler;
};

class EEDebugInterface
{
    VPTR_BASE_VTABLE_CLASS_AND_CTOR(EEDebugInterface);

public:

    //
    // Functions exported from the EE to the debugger.
    //

    virtual Thread* GetThread(void) = 0;

#ifndef DACCESS_COMPILE

    virtual StackWalkAction StackWalkFramesEx(Thread* pThread,
                                              PREGDISPLAY pRD,
                                              PSTACKWALKFRAMESCALLBACK pCallback,
                                              VOID* pData,
                                              unsigned int flags) = 0;

    virtual Frame *GetFrame(CrawlFrame*) = 0;

    virtual bool InitRegDisplay(Thread* pThread,
                                const PREGDISPLAY pRD,
                                const PT_CONTEXT pctx,
                                bool validContext) = 0;

    virtual BOOL IsStringObject(Object* o) = 0;

    virtual BOOL IsTypedReference(MethodTable* pMT) = 0;

    virtual WCHAR* StringObjectGetBuffer(StringObject* so) = 0;

    virtual DWORD StringObjectGetStringLength(StringObject* so) = 0;

    virtual void *GetObjectFromHandle(OBJECTHANDLE handle) = 0;

    virtual OBJECTHANDLE GetHandleFromObject(void *obj,
                                      bool fStrongNewRef,
                                      AppDomain *pAppDomain) = 0;

    virtual void DbgDestroyHandle( OBJECTHANDLE oh, bool fStrongNewRef ) = 0;

    virtual OBJECTHANDLE GetThreadException(Thread *pThread) = 0;

    virtual bool IsThreadExceptionNull(Thread *pThread) = 0;

    virtual void ClearThreadException(Thread *pThread) = 0;

    virtual bool StartSuspendForDebug(AppDomain *pAppDomain,
                                      BOOL fHoldingThreadStoreLock = FALSE) = 0;

    virtual void ResumeFromDebug(AppDomain *pAppDomain)= 0;

    virtual void MarkThreadForDebugSuspend(Thread* pRuntimeThread) = 0;

    virtual void MarkThreadForDebugStepping(Thread* pRuntimeThread,
                                            bool onOff) = 0;

    virtual void SetThreadFilterContext(Thread *thread,
                                        T_CONTEXT *context) = 0;

    virtual T_CONTEXT *GetThreadFilterContext(Thread *thread) = 0;

#ifdef FEATURE_INTEROP_DEBUGGING
    virtual VOID *GetThreadDebuggerWord() = 0;

    virtual void SetThreadDebuggerWord(VOID *dw) = 0;
#endif

    virtual BOOL IsManagedNativeCode(const BYTE *address) = 0;

#endif // #ifndef DACCESS_COMPILE

    virtual PCODE GetNativeCodeStartAddress(PCODE address) = 0;

    virtual MethodDesc *GetNativeCodeMethodDesc(const PCODE address) = 0;

#ifndef DACCESS_COMPILE

#ifndef USE_GC_INFO_DECODER
    virtual BOOL IsInPrologOrEpilog(const BYTE *address,
                                    size_t* prologSize) = 0;
#endif

    // Determine whether certain native offsets of the specified function are within
    // an exception filter or handler.
    virtual void DetermineIfOffsetsInFilterOrHandler(const BYTE *functionAddress,
                                                          DebugOffsetToHandlerInfo *pOffsetToHandlerInfo,
                                                          unsigned offsetToHandlerInfoLength) = 0;

#endif // #ifndef DACCESS_COMPILE

    virtual void GetMethodRegionInfo(const PCODE    pStart,
                                           PCODE  * pCold,
                                           size_t * hotSize,
                                           size_t * coldSize) = 0;

#if defined(FEATURE_EH_FUNCLETS)
    virtual DWORD GetFuncletStartOffsets(const BYTE *pStart, DWORD* pStartOffsets, DWORD dwLength) = 0;
    virtual StackFrame FindParentStackFrame(CrawlFrame* pCF) = 0;
#endif // FEATURE_EH_FUNCLETS

    virtual size_t GetFunctionSize(MethodDesc *pFD) = 0;

    virtual PCODE GetFunctionAddress(MethodDesc *pFD) = 0;

#ifndef DACCESS_COMPILE

#ifdef EnC_SUPPORTED

    // Apply an EnC edit
    virtual HRESULT EnCApplyChanges(EditAndContinueModule *pModule,
                                    DWORD cbMetadata,
                                    BYTE *pMetadata,
                                    DWORD cbIL,
                                    BYTE *pIL) = 0;

    // Perform an EnC remap to resume execution in the new version of a method (doesn't return)
    virtual void ResumeInUpdatedFunction(EditAndContinueModule *pModule,
                                         MethodDesc *pFD,
                                         void *debuggerFuncHandle,
                                         SIZE_T resumeIP,
                                         CONTEXT *pContext) = 0;
#endif //EnC_SUPPORTED

    //
    // New methods to support the new debugger.
    //

    virtual MethodDesc *FindLoadedMethodRefOrDef(Module* pModule,
                                                   mdMemberRef memberRef) = 0;

    virtual MethodDesc *LoadMethodDef(Module* pModule,
                                      mdMethodDef methodDef,
                                      DWORD numGenericArgs = 0,
                                      TypeHandle *pGenericArgs = NULL,
                                      TypeHandle *pOwnerType = NULL) = 0;

    // These will lookup a type, and if it's not loaded, return the null TypeHandle
    virtual TypeHandle FindLoadedClass(Module *pModule,
                                     mdTypeDef classToken) = 0;

    virtual TypeHandle FindLoadedElementType(CorElementType et) = 0;

    virtual TypeHandle FindLoadedInstantiation(Module *pModule,
                                               mdTypeDef typeDef,
                                               DWORD ntypars,
                                               TypeHandle *inst) = 0;

    virtual TypeHandle FindLoadedFnptrType(TypeHandle *inst,
                                           DWORD ntypars) = 0;

    virtual TypeHandle FindLoadedPointerOrByrefType(CorElementType et,
                                                    TypeHandle elemtype) = 0;

    virtual TypeHandle FindLoadedArrayType(CorElementType et,
                                           TypeHandle elemtype,
                                           unsigned rank) = 0;

    // These will lookup a type, and if it's not loaded, will load and run
    // the class init etc.
    virtual TypeHandle LoadClass(Module *pModule,
                               mdTypeDef classToken) = 0;

    virtual TypeHandle LoadElementType(CorElementType et) = 0;

    virtual TypeHandle LoadInstantiation(Module *pModule,
                                         mdTypeDef typeDef,
                                         DWORD ntypars,
                                         TypeHandle *inst) = 0;

    virtual TypeHandle LoadFnptrType(TypeHandle *inst,
                                     DWORD ntypars) = 0;

    virtual TypeHandle LoadPointerOrByrefType(CorElementType et,
                                              TypeHandle elemtype) = 0;

    virtual TypeHandle LoadArrayType(CorElementType et,
                                     TypeHandle elemtype,
                                     unsigned rank) = 0;

    __checkReturn
    virtual HRESULT GetMethodImplProps(Module *pModule,
                                       mdToken tk,
                                       DWORD *pRVA,
                                       DWORD *pImplFlags) = 0;

    virtual HRESULT GetParentToken(Module *pModule,
                                   mdToken tk,
                                   mdToken *pParentToken) = 0;

    virtual bool IsPreemptiveGCDisabled(void) = 0;

    virtual void DisablePreemptiveGC(void) = 0;

    virtual void EnablePreemptiveGC(void) = 0;

    virtual DWORD MethodDescIsStatic(MethodDesc *pFD) = 0;

#endif // #ifndef DACCESS_COMPILE

    virtual Module *MethodDescGetModule(MethodDesc *pFD) = 0;

#ifndef DACCESS_COMPILE

    virtual COR_ILMETHOD* MethodDescGetILHeader(MethodDesc *pFD) = 0;

    virtual ULONG MethodDescGetRVA(MethodDesc *pFD) = 0;

    virtual void MarkDebuggerAttached(void) = 0;

    virtual void MarkDebuggerUnattached(void) = 0;

    virtual bool CrawlFrameIsGcSafe(CrawlFrame *pCF) = 0;

    virtual bool SweepThreadsForDebug(bool forceSync) = 0;

   virtual void GetRuntimeOffsets(SIZE_T *pTLSIndex,
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
                                  DWORD  *pEEIsManagedExceptionStateMask) = 0;

    virtual bool IsStub(const BYTE *ip) = 0;

#endif // #ifndef DACCESS_COMPILE

    virtual bool DetectHandleILStubs(Thread *thread) = 0;

    virtual bool TraceStub(const BYTE *ip, TraceDestination *trace) = 0;

#ifndef DACCESS_COMPILE

    virtual bool FollowTrace(TraceDestination *trace) = 0;

    virtual bool TraceFrame(Thread *thread,
                            Frame *frame,
                            BOOL fromPatch,
                            TraceDestination *trace,
                            REGDISPLAY *regs) = 0;

    virtual bool TraceManager(Thread *thread,
                              StubManager *stubManager,
                              TraceDestination *trace,
                              T_CONTEXT *context,
                              BYTE **pRetAddr) = 0;

    virtual void EnableTraceCall(Thread *thread) = 0;
    virtual void DisableTraceCall(Thread *thread) = 0;

#endif // #ifndef DACCESS_COMPILE

#ifndef DACCESS_COMPILE

    virtual void DebuggerModifyingLogSwitch (int iNewLevel,
                                             const WCHAR *pLogSwitchName) = 0;

    virtual HRESULT SetIPFromSrcToDst(Thread *pThread,
                          SLOT addrStart,
                          DWORD offFrom,
                          DWORD offTo,
                          bool fCanSetIPOnly,
                          PREGDISPLAY pReg,
                          PT_CONTEXT pCtx,
                          void *pDji,
                          EHRangeTree *pEHRT) = 0;

    virtual void SetDebugState(Thread *pThread,
                               CorDebugThreadState state) = 0;

    virtual void SetAllDebugState(Thread *et,
                                  CorDebugThreadState state) = 0;

    virtual CorDebugUserState GetPartialUserState( Thread *pThread ) = 0;

#endif // #ifndef DACCESS_COMPILE

#ifdef DACCESS_COMPILE
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags) = 0;
#endif

    virtual unsigned GetSizeForCorElementType(CorElementType etyp) = 0;

#ifndef DACCESS_COMPILE
    virtual BOOL ObjIsInstanceOf(Object *pElement, TypeHandle toTypeHnd) = 0;
#endif

    virtual void ClearAllDebugInterfaceReferences(void) = 0;

#ifndef DACCESS_COMPILE
#ifdef _DEBUG
    virtual void ObjectRefFlush(Thread *pThread) = 0;
#endif
#endif

#ifndef DACCESS_COMPILE
    virtual BOOL AdjustContextForJITHelpersForDebugger(CONTEXT* context) = 0;
#endif
};

#endif // _eedbginterface_h_
