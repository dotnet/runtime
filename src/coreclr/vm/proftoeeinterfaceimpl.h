// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// FILE: ProfToEEInterfaceImpl.h
//
// Declaration of class that implements the ICorProfilerInfo* interfaces, which allow the
// Profiler to communicate with the EE.  This allows the Profiler DLL to get
// access to private EE data structures and other things that should never be exported
// outside of the EE.
//

//

// ======================================================================================


#ifndef __PROFTOEEINTERFACEIMPL_H__
#define __PROFTOEEINTERFACEIMPL_H__

#ifdef PROFILING_SUPPORTED

#include "eeprofinterfaces.h"
#include "vars.hpp"
#include "threads.h"
#include "codeman.h"
#include "cor.h"
#include "callingconvention.h"


#include "profilinghelper.h"
#include "profilinghelper.inl"


class ProfilerFunctionEnum;

//
// Helper routines.
//
extern MethodDesc *FunctionIdToMethodDesc(FunctionID functionID);
extern ClassID TypeHandleToClassID(TypeHandle th);


//
// Function declarations for those functions that are platform specific.
//
extern UINT_PTR ProfileGetIPFromPlatformSpecificHandle(void * handle);

extern void ProfileSetFunctionIDInPlatformSpecificHandle(void * pPlatformSpecificHandle, FunctionID functionID);

//
// The following class is implemented differently on each platform, using
// the PlatformSpecificHandle to initialize an ArgIterator.
//
class ProfileArgIterator
{
private:
    void        *m_handle;
    ArgIterator  m_argIterator;
#if defined(UNIX_AMD64_ABI) || defined(TARGET_ARM64)
    UINT64       m_bufferPos;

#if defined(UNIX_AMD64_ABI)
    // On certain architectures we can pass args in non-sequential registers,
    // this function will copy the struct so it is laid out as it would be in memory
    // so it can be passed to the profiler
    LPVOID CopyStructFromRegisters();
#endif

#if defined(TARGET_ARM64)
    // On Arm64 the fields an HFA or an HVA argument will need to be copied
    // to a temporary buffer in memory, so they are laid out contiguously in memory.
    LPVOID CopyStructFromFPRegs(int idxFPReg, int cntFPRegs, int hfaFieldSize);
#endif

#endif // UNIX_AMD64_ABI || TARGET_ARM64

public:
    ProfileArgIterator(MetaSig * pMetaSig, void* platformSpecificHandle);

    ~ProfileArgIterator();

    //
    // Returns number of arguments returned by GetNextArgAddr
    //
    UINT GetNumArgs()
    {
        LIMITED_METHOD_CONTRACT;
        return m_argIterator.NumFixedArgs();
    }

    //
    // After initialization, this method is called repeatedly until it
    // returns NULL to get the address of each arg.
    //
    // Note: this address could be anywhere on the stack.
    //
    LPVOID GetNextArgAddr();

    //
    // Returns argument size
    //
    UINT GetArgSize()
    {
        LIMITED_METHOD_CONTRACT;
        return m_argIterator.GetArgSize();
    }

    //
    // Called after initialization, any number of times, to retrieve any
    // hidden argument, so that resolution for Generics can be done.
    //
    LPVOID GetHiddenArgValue(void);

    //
    // Called after initialization, any number of times, to retrieve the
    // value of 'this'.
    //
    LPVOID GetThis(void);

    //
    // Called after initialization, any number of times, to retrieve the
    // address of the return buffer, if there is one.  NULL indicates no
    // return buffer.
    //
    LPVOID GetReturnBufferAddr(void);
};

//---------------------------------------------------------------------------------------
// This helper class wraps a loader heap which can be used to allocate
// memory for IL after the current module.

class ModuleILHeap : public IMethodMalloc
{
public:
    // IUnknown
    virtual ULONG STDMETHODCALLTYPE AddRef();
    virtual ULONG STDMETHODCALLTYPE Release();
    virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void ** pp);

    // IMethodMalloc
    virtual void *STDMETHODCALLTYPE Alloc(ULONG cb);

    static ModuleILHeap s_Heap;
};

typedef struct _PROFILER_STACK_WALK_DATA PROFILER_STACK_WALK_DATA;

//---------------------------------------------------------------------------------------
// One of these is allocated per EE instance.   A pointer is cached to this
// from the profiler implementation.  The profiler will call back on the v-table
// to get at EE internals as required.

class ProfToEEInterfaceImpl : public ICorProfilerInfo12
{
private:
    ProfilerInfo *m_pProfilerInfo;

public:

    // Internal Housekeeping

    static void MethodTableCallback(void* context, void* methodTable);
    static void ObjectRefCallback(void* context, void* objectRefUNSAFE);

    ProfToEEInterfaceImpl();
    virtual ~ProfToEEInterfaceImpl();
    HRESULT Init();

    void SetProfilerInfo(ProfilerInfo *pProfilerInfo)
    {
        LIMITED_METHOD_CONTRACT;
        m_pProfilerInfo = pProfilerInfo;
    }

    // IUnknown
    ULONG STDMETHODCALLTYPE AddRef();
    ULONG STDMETHODCALLTYPE Release();
    COM_METHOD QueryInterface(REFIID id, void ** pInterface);

    // ICorProfilerInfo2

    COM_METHOD GetEventMask(DWORD * pdwEvents);
    COM_METHOD SetEventMask(DWORD dwEventMask);

    COM_METHOD GetHandleFromThread(ThreadID threadId, HANDLE * phThread);

    COM_METHOD GetObjectSize(ObjectID objectId, ULONG * pcSize);

    COM_METHOD GetObjectSize2(ObjectID objectId, SIZE_T * pcSize);

    COM_METHOD IsArrayClass(
        /* [in] */  ClassID classId,
        /* [out] */ CorElementType * pBaseElemType,
        /* [out] */ ClassID * pBaseClassId,
        /* [out] */ ULONG   * pcRank);

    COM_METHOD GetThreadInfo(ThreadID threadId, DWORD * pdwWin32ThreadId);

    COM_METHOD GetCurrentThreadID(ThreadID * pThreadId);

    COM_METHOD GetFunctionFromIP(LPCBYTE ip, FunctionID * pFunctionId);

    COM_METHOD GetTokenAndMetaDataFromFunction(
        FunctionID functionId,
        REFIID riid,
        IUnknown ** ppOut,
        mdToken * pToken);

    COM_METHOD GetCodeInfo(FunctionID functionId, LPCBYTE * pStart, ULONG * pcSize);

    COM_METHOD GetModuleInfo(
        ModuleID     moduleId,
        LPCBYTE *    ppBaseLoadAddress,
        ULONG        cchName,
        ULONG *      pcchName,
        _Out_writes_to_opt_(cchName, *pcchName) WCHAR szName[],
        AssemblyID * pAssemblyId);

    COM_METHOD GetModuleMetaData(
        ModuleID    moduleId,
        DWORD       dwOpenFlags,
        REFIID      riid,
        IUnknown ** ppOut);

    COM_METHOD GetILFunctionBody(
        ModuleID    moduleId,
        mdMethodDef methodid,
        LPCBYTE *   ppMethodHeader,
        ULONG *     pcbMethodSize);

    COM_METHOD GetILFunctionBodyAllocator(
        ModuleID moduleId,
        IMethodMalloc ** ppMalloc);

    COM_METHOD SetILFunctionBody(
        ModuleID    moduleId,
        mdMethodDef methodid,
        LPCBYTE     pbNewILMethodHeader);

    COM_METHOD SetILInstrumentedCodeMap(
        FunctionID functionId,
        BOOL fStartJit,
        ULONG cILMapEntries,
        COR_IL_MAP rgILMapEntries[]);

    COM_METHOD ForceGC();

    COM_METHOD GetClassIDInfo(
        ClassID classId,
        ModuleID * pModuleId,
        mdTypeDef * pTypeDefToken);

    COM_METHOD GetFunctionInfo(
        FunctionID functionId,
        ClassID * pClassId,
        ModuleID * pModuleId,
        mdToken * pToken);

    COM_METHOD GetClassFromObject(
        ObjectID objectId,
        ClassID * pClassId);

    COM_METHOD GetClassFromToken(
        ModuleID moduleId,
        mdTypeDef typeDef,
        ClassID * pClassId);

    COM_METHOD GetFunctionFromToken(
        ModuleID moduleId,
        mdToken typeDef,
        FunctionID * pFunctionId);

    COM_METHOD GetAppDomainInfo(
        AppDomainID appDomainId,
        ULONG       cchName,
        ULONG *     pcchName,
        _Out_writes_to_opt_(cchName, *pcchName) WCHAR szName[],
        ProcessID * pProcessId);

    COM_METHOD GetAssemblyInfo(
        AssemblyID    assemblyId,
        ULONG         cchName,
        ULONG *       pcchName,
        _Out_writes_to_opt_(cchName, *pcchName) WCHAR szName[],
        AppDomainID * pAppDomainId,
        ModuleID    * pModuleId);

    COM_METHOD SetEnterLeaveFunctionHooks(
        FunctionEnter * pFuncEnter,
        FunctionLeave * pFuncLeave,
        FunctionTailcall * pFuncTailcall);

    COM_METHOD SetEnterLeaveFunctionHooks2(
        FunctionEnter2 * pFuncEnter,
        FunctionLeave2 * pFuncLeave,
        FunctionTailcall2 * pFuncTailcall);

    COM_METHOD SetFunctionIDMapper(
        FunctionIDMapper * pFunc);

    COM_METHOD GetThreadContext(
        ThreadID threadId,
        ContextID * pContextId);

    COM_METHOD GetILToNativeMapping(
                /* [in] */  FunctionID functionId,
                /* [in] */  ULONG32 cMap,
                /* [out] */ ULONG32 * pcMap,
                /* [out, size_is(cMap), length_is(*pcMap)] */
                    COR_DEBUG_IL_TO_NATIVE_MAP map[]);

    COM_METHOD GetFunctionInfo2(
        /* in  */ FunctionID funcId,
        /* in  */ COR_PRF_FRAME_INFO frameInfo,
        /* out */ ClassID * pClassId,
        /* out */ ModuleID * pModuleId,
        /* out */ mdToken * pToken,
        /* in  */ ULONG32 cTypeArgs,
        /* out */ ULONG32 * pcTypeArgs,
        /* out */ ClassID typeArgs[]);

    COM_METHOD GetStringLayout(
         /* out */ ULONG * pBufferLengthOffset,
         /* out */ ULONG * pStringLengthOffset,
         /* out */ ULONG * pBufferOffset);

    COM_METHOD GetClassLayout(
         /* in    */ ClassID classID,
         /* in.out*/ COR_FIELD_OFFSET rFieldOffset[],
         /* in    */ ULONG cFieldOffset,
         /* out   */ ULONG * pcFieldOffset,
         /* out   */ ULONG * pulClassSize);

    COM_METHOD DoStackSnapshot(
        ThreadID thread,
        StackSnapshotCallback *callback,
        ULONG32 infoFlags,
        void * clientData,
        BYTE * pctx,
        ULONG32 contextSize);

    COM_METHOD GetCodeInfo2(FunctionID functionId,
                            ULONG32    cCodeInfos,
                            ULONG32 *  pcCodeInfos,
                            COR_PRF_CODE_INFO codeInfos[]);

    COM_METHOD GetArrayObjectInfo(ObjectID objectId,
                                  ULONG32 cDimensionSizes,
                                  ULONG32 pDimensionSizes[],
                                  int pDimensionLowerBounds[],
                                  BYTE ** ppData);

    COM_METHOD GetBoxClassLayout(ClassID classId,
                                 ULONG32 * pBufferOffset);

    COM_METHOD GetClassIDInfo2(ClassID classId,
                               ModuleID * pModuleId,
                               mdTypeDef * pTypeDefToken,
                               ClassID * pParentClassId,
                               ULONG32 cNumTypeArgs,
                               ULONG32 * pcNumTypeArgs,
                               ClassID typeArgs[]);

    COM_METHOD GetThreadAppDomain(ThreadID threadId,
                                  AppDomainID * pAppDomainId);

    COM_METHOD GetRVAStaticAddress(ClassID classId,
                                   mdFieldDef fieldToken,
                                   void ** ppAddress);

    COM_METHOD GetAppDomainStaticAddress(ClassID classId,
                                         mdFieldDef fieldToken,
                                         AppDomainID appDomainId,
                                         void ** ppAddress);

    COM_METHOD GetThreadStaticAddress(ClassID classId,
                                      mdFieldDef fieldToken,
                                      ThreadID threadId,
                                      void ** ppAddress);

    COM_METHOD GetContextStaticAddress(ClassID classId,
                                       mdFieldDef fieldToken,
                                       ContextID contextId,
                                       void ** ppAddress);

    COM_METHOD GetStaticFieldInfo(ClassID classId,
                                  mdFieldDef fieldToken,
                                  COR_PRF_STATIC_TYPE * pFieldInfo);

    COM_METHOD GetClassFromTokenAndTypeArgs(ModuleID moduleID,
                                            mdTypeDef typeDef,
                                            ULONG32 cTypeArgs,
                                            ClassID typeArgs[],
                                            ClassID* pClassID);

    COM_METHOD EnumModuleFrozenObjects(ModuleID moduleID,
                                       ICorProfilerObjectEnum** ppEnum);



    COM_METHOD GetFunctionFromTokenAndTypeArgs(ModuleID moduleID,
                                               mdMethodDef funcDef,
                                               ClassID classId,
                                               ULONG32 cTypeArgs,
                                               ClassID typeArgs[],
                                               FunctionID* pFunctionID);

    COM_METHOD GetGenerationBounds(ULONG cObjectRanges,
                                   ULONG * pcObjectRanges,
                                   COR_PRF_GC_GENERATION_RANGE ranges[]);

    COM_METHOD GetObjectGeneration(ObjectID objectId,
                                   COR_PRF_GC_GENERATION_RANGE *range);

    COM_METHOD GetNotifiedExceptionClauseInfo(COR_PRF_EX_CLAUSE_INFO * pinfo);

    COM_METHOD SetFunctionReJIT(FunctionID);
    COM_METHOD GetInprocInspectionInterface(IUnknown **);
    COM_METHOD GetInprocInspectionIThisThread(IUnknown **);
    COM_METHOD BeginInprocDebugging(BOOL,DWORD *);
    COM_METHOD EndInprocDebugging(DWORD);

    // ICorProfilerInfo3

    COM_METHOD EnumJITedFunctions(ICorProfilerFunctionEnum** ppEnum);
    COM_METHOD EnumModules(ICorProfilerModuleEnum ** ppEnum);

    COM_METHOD RequestProfilerDetach(
        /* in  */ DWORD dwExpectedCompletionMilliseconds);

    COM_METHOD SetFunctionIDMapper2(
        FunctionIDMapper2 * pFunc,                           // in
        void * clientData);                                  // in

    COM_METHOD SetEnterLeaveFunctionHooks3(
        FunctionEnter3 * pFuncEnter3,                               // in
        FunctionLeave3 * pFuncLeave3,                               // in
        FunctionTailcall3 * pFuncTailcall3);                        // in

    COM_METHOD SetEnterLeaveFunctionHooks3WithInfo(
        FunctionEnter3WithInfo * pFuncEnter3WithInfo,               // in
        FunctionLeave3WithInfo * pFuncLeave3WithInfo,               // in
        FunctionTailcall3WithInfo * pFuncTailcall3WithInfo);        // in

    COM_METHOD GetFunctionEnter3Info(
                FunctionID functionId,                              // in
                COR_PRF_ELT_INFO eltInfo,                           // in
                COR_PRF_FRAME_INFO * pFrameInfo,                    // out
                ULONG * pcbArgumentInfo,                            // in, out
                COR_PRF_FUNCTION_ARGUMENT_INFO * pArgumentInfo);    // out

    COM_METHOD GetFunctionLeave3Info(
                FunctionID functionId,                              // in
                COR_PRF_ELT_INFO eltInfo,                           // in
                COR_PRF_FRAME_INFO * pFrameInfo,                    // out
                COR_PRF_FUNCTION_ARGUMENT_RANGE * pRetvalRange);    // out

    COM_METHOD GetFunctionTailcall3Info(
                FunctionID functionId,                              // in
                COR_PRF_ELT_INFO pFrameInfo,                        // in
                COR_PRF_FRAME_INFO * pFunc);                        // out

    COM_METHOD GetStringLayout2(
         /* out */ ULONG * pStringLengthOffset,
         /* out */ ULONG * pBufferOffset);

    COM_METHOD GetRuntimeInformation(USHORT *               pClrInstanceId,      // out
                                     COR_PRF_RUNTIME_TYPE * pRuntimeType,        // out
                                     USHORT *               pMajorVersion,       // out
                                     USHORT *               pMinorVersion,       // out
                                     USHORT *               pBuildNumber,        // out
                                     USHORT *               pQFEVersion,         // out
                                     ULONG                  cchVersionString,    // in
                                     ULONG  *               pcchVersionString,   // out
                                     _Out_writes_to_opt_(cchVersionString, *pcchVersionString) WCHAR szVersionString[]);  // out

    COM_METHOD GetThreadStaticAddress2(ClassID classId,             // in
                                       mdFieldDef fieldToken,       // in
                                       AppDomainID appDomainId,     // in
                                       ThreadID threadId,           // in
                                       void ** ppAddress);          // out

    COM_METHOD GetAppDomainsContainingModule(ModuleID moduleId,            // in
                                             ULONG32 cAppDomainIds,        // in
                                             ULONG32 *pcAppDomainIds,      // out
                                             AppDomainID appDomainIds[]);  // out

    COM_METHOD GetModuleInfo2(
        ModuleID     moduleId,
        LPCBYTE *    ppBaseLoadAddress,
        ULONG        cchName,
        ULONG *      pcchName,
        _Out_writes_to_opt_(cchName, *pcchName) WCHAR szName[],
        AssemblyID * pAssemblyId,
        DWORD *      pdwModuleFlags);

    // end ICorProfilerInfo3

    // ICorProfilerInfo4

    COM_METHOD EnumThreads(
        /* out */ ICorProfilerThreadEnum ** ppEnum);

    COM_METHOD InitializeCurrentThread();

    // end ICorProfilerInfo4

    COM_METHOD RequestReJIT(ULONG       cFunctions,   // in
                            ModuleID    moduleIds[],  // in
                            mdMethodDef methodIds[]);  // in

    COM_METHOD RequestRevert(ULONG       cFunctions,  // in
                             ModuleID    moduleIds[], // in
                             mdMethodDef methodIds[], // in
                             HRESULT     status[]);   // out

    COM_METHOD GetCodeInfo3(FunctionID         functionID,   // in
                            ReJITID            reJitId,      // in
                            ULONG32            cCodeInfos,   // in
                            ULONG32 *          pcCodeInfos,  // out
                            COR_PRF_CODE_INFO  codeInfos[]); // out

    COM_METHOD GetFunctionFromIP2(LPCBYTE      ip,          // in
                                  FunctionID * pFunctionId, // out
                                  ReJITID *    pReJitId);   // out

    COM_METHOD GetReJITIDs(FunctionID          functionId,  // in
                           ULONG               cReJitIds,   // in
                           ULONG *             pcReJitIds,  // out
                           ReJITID             reJitIds[]); // out

    COM_METHOD GetILToNativeMapping2(
                   FunctionID                  functionId,  // in
                   ReJITID                     reJitId,     // in
                   ULONG32                     cMap,        // in
                   ULONG32 *                   pcMap,       // out
                   COR_DEBUG_IL_TO_NATIVE_MAP  map[]);      // out

    COM_METHOD EnumJITedFunctions2(ICorProfilerFunctionEnum** ppEnum);

	// end ICorProfilerInfo4


    // begin ICorProfilerInfo5

    COM_METHOD SetEventMask2(
            DWORD dwEventsLow,
            DWORD dwEventsHigh);

    COM_METHOD GetEventMask2(DWORD *pdwEventsLow, DWORD *pdwEventsHigh);

    // end ICorProfilerInfo5

    // begin ICorProfilerInfo6

    COM_METHOD EnumNgenModuleMethodsInliningThisMethod(
        ModuleID    inlinersModuleId,
        ModuleID    inlineeModuleId,
        mdMethodDef inlineeMethodId,
        BOOL       *incompleteData,
        ICorProfilerMethodEnum** ppEnum);


    // end ICorProfilerInfo6

    // begin ICorProfilerInfo7

    COM_METHOD ApplyMetaData(
        ModuleID    moduleId);

 COM_METHOD GetInMemorySymbolsLength(
        ModuleID moduleId,
        DWORD* pCountSymbolBytes);

    COM_METHOD ReadInMemorySymbols(
        ModuleID moduleId,
        DWORD symbolsReadOffset,
        BYTE* pSymbolBytes,
        DWORD countSymbolBytes,
        DWORD* pCountSymbolBytesRead);

    // end ICorProfilerInfo7

    // begin ICorProfilerInfo8

    COM_METHOD IsFunctionDynamic(
        FunctionID functionId,
        BOOL *isDynamic);

    COM_METHOD GetFunctionFromIP3(
        LPCBYTE      ip,          // in
        FunctionID * pFunctionId, // out
        ReJITID *    pReJitId);   // out

    COM_METHOD GetDynamicFunctionInfo(
        FunctionID functionId,
        ModuleID* moduleId,
        PCCOR_SIGNATURE* ppvSig,
        ULONG* pbSig,
        ULONG cchName,
        ULONG *pcchName,
        WCHAR wszName[]);

    // end ICorProfilerInfo8

    // begin ICorProfilerInfo9

    COM_METHOD GetNativeCodeStartAddresses(
        FunctionID functionID,
        ReJITID reJitId,
        ULONG32 cCodeStartAddresses,
        ULONG32 *pcCodeStartAddresses,
        UINT_PTR codeStartAddresses[]);

    COM_METHOD GetILToNativeMapping3(
        UINT_PTR pNativeCodeStartAddress,
        ULONG32 cMap,
        ULONG32 *pcMap,
        COR_DEBUG_IL_TO_NATIVE_MAP map[]);

    COM_METHOD GetCodeInfo4(
        UINT_PTR pNativeCodeStartAddress,
        ULONG32 cCodeInfos,
        ULONG32* pcCodeInfos,
        COR_PRF_CODE_INFO codeInfos[]);

    // end ICorProfilerInfo9

    // beging ICorProfilerInfo10

    COM_METHOD EnumerateObjectReferences(ObjectID objectId, ObjectReferenceCallback callback, void* clientData);

    COM_METHOD IsFrozenObject(ObjectID objectId, BOOL *pbFrozen);

    COM_METHOD GetLOHObjectSizeThreshold(DWORD *pThreshold);

    COM_METHOD RequestReJITWithInliners(
        DWORD       dwRejitFlags,
        ULONG       cFunctions,
        ModuleID    moduleIds[],
        mdMethodDef methodIds[]);

    COM_METHOD SuspendRuntime();

    COM_METHOD ResumeRuntime();

    // end ICorProfilerInfo10

    // begin ICorProfilerInfo11

    COM_METHOD GetEnvironmentVariable(
        const WCHAR *szName,
        ULONG       cchValue,
        ULONG       *pcchValue,
        _Out_writes_to_opt_(cchValue, *pcchValue) WCHAR szValue[]);

    COM_METHOD SetEnvironmentVariable(
        const WCHAR *szName,
        const WCHAR *szValue);

    // end ICorProfilerInfo11

    // begin ICorProfilerInfo12

    COM_METHOD EventPipeStartSession(
        UINT32 cProviderConfigs,
        COR_PRF_EVENTPIPE_PROVIDER_CONFIG pProviderConfigs[],
        BOOL requestRundown,
        EVENTPIPE_SESSION* pSession);

    COM_METHOD EventPipeAddProviderToSession(
        EVENTPIPE_SESSION session,
        COR_PRF_EVENTPIPE_PROVIDER_CONFIG providerConfig);

    COM_METHOD EventPipeStopSession(
        EVENTPIPE_SESSION session);

    COM_METHOD EventPipeCreateProvider(
        const WCHAR *providerName,
        EVENTPIPE_PROVIDER *pProvider);

    COM_METHOD EventPipeGetProviderInfo(
                EVENTPIPE_PROVIDER provider,
                ULONG      cchName,
                ULONG      *pcchName,
                WCHAR      providerName[]);

    COM_METHOD EventPipeDefineEvent(
        EVENTPIPE_PROVIDER provider,
        const WCHAR *eventName,
        UINT32 eventID,
        UINT64 keywords,
        UINT32 eventVersion,
        UINT32 level,
        UINT8 opcode,
        BOOL needStack,
        UINT32 cParamDescs,
        COR_PRF_EVENTPIPE_PARAM_DESC pParamDescs[],
        EVENTPIPE_EVENT *pEvent);

    COM_METHOD EventPipeWriteEvent(
        EVENTPIPE_EVENT event,
        UINT32 cData,
        COR_PRF_EVENT_DATA data[],
        LPCGUID pActivityId,
        LPCGUID pRelatedActivityId);

    // end ICorProfilerInfo12

protected:

    // Internal Helper Functions
    static void EventPipeCallbackHelper(EventPipeProvider *provider,
                                        DWORD eventId,
                                        DWORD eventVersion,
                                        ULONG cbMetadataBlob,
                                        LPCBYTE metadataBlob,
                                        ULONG cbEventData,
                                        LPCBYTE eventData,
                                        LPCGUID pActivityId,
                                        LPCGUID pRelatedActivityId,
                                        Thread *pEventThread,
                                        ULONG numStackFrames,
                                        UINT_PTR stackFrames[],
                                        void *additionalData);

    HRESULT GetCodeInfoHelper(FunctionID functionId,
                               ReJITID  reJitId,
                               ULONG32  cCodeInfos,
                               ULONG32 * pcCodeInfos,
                               COR_PRF_CODE_INFO codeInfos[]);

    HRESULT GetStringLayoutHelper(ULONG * pBufferLengthOffset,
                                  ULONG * pStringLengthOffset,
                                  ULONG * pBufferOffset);

    HRESULT GetArrayObjectInfoHelper(Object * pObj,
                                     ULONG32 cDimensionSizes,
                                     _Out_writes_(cDimensionSizes) ULONG32 pDimensionSizes[],
                                     _Out_writes_(cDimensionSizes) int pDimensionLowerBounds[],
                                     BYTE ** ppData);

    DWORD GetModuleFlags(Module * pModule);

    HRESULT DoStackSnapshotHelper(Thread * pThreadToSnapshot,
                                  PROFILER_STACK_WALK_DATA * pData,
                                  unsigned flags,
                                  LPCONTEXT pctxSeed);

    HRESULT ProfilerStackWalkFramesWrapper(Thread * pThreadToSnapshot, PROFILER_STACK_WALK_DATA * pData, unsigned flags);

    HRESULT EnumJITedFunctionsHelper(ProfilerFunctionEnum ** ppEnum, IJitManager ** ppJitMgr);

    HRESULT SetupThreadForReJIT();

#ifdef TARGET_X86
    HRESULT ProfilerEbpWalker(Thread * pThreadToSnapshot, LPCONTEXT pctxSeed, StackSnapshotCallback * callback, void * clientData);
#endif //TARGET_X86
};

#endif // PROFILING_SUPPORTED

#endif // __PROFTOEEINTERFACEIMPL_H__


