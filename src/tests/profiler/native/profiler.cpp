// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "profiler.h"

#include <thread>

using std::thread;

Profiler *Profiler::Instance = nullptr;

std::atomic<bool> ShutdownGuard::s_preventHooks(false);
std::atomic<int> ShutdownGuard::s_hooksInProgress(0);

ProfilerCallback Profiler::s_callback;
ManualEvent Profiler::s_callbackSet;

Profiler::Profiler() : refCount(0), pCorProfilerInfo(nullptr)
{
    Profiler::Instance = this;
}

Profiler::~Profiler()
{
    if (this->pCorProfilerInfo != nullptr)
    {
        this->pCorProfilerInfo->Release();
        this->pCorProfilerInfo = nullptr;
    }
}

HRESULT STDMETHODCALLTYPE Profiler::Initialize(IUnknown *pICorProfilerInfoUnk)
{
    ShutdownGuard::Initialize();

    printf("Profiler.dll!Profiler::Initialize\n");
    fflush(stdout);

    HRESULT queryInterfaceResult = pICorProfilerInfoUnk->QueryInterface(__uuidof(ICorProfilerInfo11), reinterpret_cast<void **>(&this->pCorProfilerInfo));
    if (FAILED(queryInterfaceResult))
    {
        printf("Profiler.dll!Profiler::Initialize failed to QI for ICorProfilerInfo.\n");
        pICorProfilerInfoUnk = NULL;
    }
    
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::Shutdown()
{
    printf("Profiler.dll!Profiler::Shutdown\n");
    fflush(stdout);

    // Wait for any in progress profiler callbacks to finish.
    ShutdownGuard::WaitForInProgressHooks();

    if (this->pCorProfilerInfo != nullptr)
    {
        this->pCorProfilerInfo->Release();
        this->pCorProfilerInfo = nullptr;
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::AppDomainCreationStarted(AppDomainID appDomainId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::AppDomainCreationFinished(AppDomainID appDomainId, HRESULT hrStatus)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::AppDomainShutdownStarted(AppDomainID appDomainId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::AppDomainShutdownFinished(AppDomainID appDomainId, HRESULT hrStatus)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::AssemblyLoadStarted(AssemblyID assemblyId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::AssemblyLoadFinished(AssemblyID assemblyId, HRESULT hrStatus)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::AssemblyUnloadStarted(AssemblyID assemblyId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::AssemblyUnloadFinished(AssemblyID assemblyId, HRESULT hrStatus)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ModuleLoadStarted(ModuleID moduleId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ModuleUnloadStarted(ModuleID moduleId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ModuleUnloadFinished(ModuleID moduleId, HRESULT hrStatus)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ModuleAttachedToAssembly(ModuleID moduleId, AssemblyID AssemblyId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ClassLoadStarted(ClassID classId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ClassLoadFinished(ClassID classId, HRESULT hrStatus)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ClassUnloadStarted(ClassID classId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ClassUnloadFinished(ClassID classId, HRESULT hrStatus)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::FunctionUnloadStarted(FunctionID functionId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::JITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::JITCachedFunctionSearchStarted(FunctionID functionId, BOOL *pbUseCachedFunction)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::JITCachedFunctionSearchFinished(FunctionID functionId, COR_PRF_JIT_CACHE result)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::JITFunctionPitched(FunctionID functionId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::JITInlining(FunctionID callerId, FunctionID calleeId, BOOL *pfShouldInline)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ThreadCreated(ThreadID threadId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ThreadDestroyed(ThreadID threadId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::RemotingClientInvocationStarted()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::RemotingClientSendingMessage(GUID *pCookie, BOOL fIsAsync)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::RemotingClientReceivingReply(GUID *pCookie, BOOL fIsAsync)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::RemotingClientInvocationFinished()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::RemotingServerReceivingMessage(GUID *pCookie, BOOL fIsAsync)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::RemotingServerInvocationStarted()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::RemotingServerInvocationReturned()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::RemotingServerSendingReply(GUID *pCookie, BOOL fIsAsync)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::UnmanagedToManagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ManagedToUnmanagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON suspendReason)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::RuntimeSuspendFinished()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::RuntimeSuspendAborted()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::RuntimeResumeStarted()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::RuntimeResumeFinished()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::RuntimeThreadSuspended(ThreadID threadId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::RuntimeThreadResumed(ThreadID threadId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::MovedReferences(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], ULONG cObjectIDRangeLength[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ObjectAllocated(ObjectID objectId, ClassID classId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ObjectsAllocatedByClass(ULONG cClassCount, ClassID classIds[], ULONG cObjects[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ObjectReferences(ObjectID objectId, ClassID classId, ULONG cObjectRefs, ObjectID objectRefIds[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::RootReferences(ULONG cRootRefs, ObjectID rootRefIds[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ExceptionThrown(ObjectID thrownObjectId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ExceptionSearchFunctionEnter(FunctionID functionId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ExceptionSearchFunctionLeave()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ExceptionSearchFilterEnter(FunctionID functionId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ExceptionSearchFilterLeave()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ExceptionSearchCatcherFound(FunctionID functionId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ExceptionOSHandlerEnter(UINT_PTR __unused)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ExceptionOSHandlerLeave(UINT_PTR __unused)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ExceptionUnwindFunctionEnter(FunctionID functionId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ExceptionUnwindFunctionLeave()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ExceptionUnwindFinallyEnter(FunctionID functionId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ExceptionUnwindFinallyLeave()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ExceptionCatcherEnter(FunctionID functionId, ObjectID objectId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ExceptionCatcherLeave()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::COMClassicVTableCreated(ClassID wrappedClassId, REFGUID implementedIID, void *pVTable, ULONG cSlots)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::COMClassicVTableDestroyed(ClassID wrappedClassId, REFGUID implementedIID, void *pVTable)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ExceptionCLRCatcherFound()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ExceptionCLRCatcherExecute()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::SurvivingReferences(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], ULONG cObjectIDRangeLength[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::GarbageCollectionFinished()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::FinalizeableObjectQueued(DWORD finalizerFlags, ObjectID objectID)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::RootReferences2(ULONG cRootRefs, ObjectID rootRefIds[], COR_PRF_GC_ROOT_KIND rootKinds[], COR_PRF_GC_ROOT_FLAGS rootFlags[], UINT_PTR rootIds[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::HandleCreated(GCHandleID handleId, ObjectID initialObjectId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::HandleDestroyed(GCHandleID handleId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::InitializeForAttach(IUnknown *pCorProfilerInfoUnk, void *pvClientData, UINT cbClientData)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ProfilerAttachComplete()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ProfilerDetachSucceeded()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ReJITCompilationStarted(FunctionID functionId, ReJITID rejitId, BOOL fIsSafeToBlock)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::GetReJITParameters(ModuleID moduleId, mdMethodDef methodId, ICorProfilerFunctionControl *pFunctionControl)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ReJITCompilationFinished(FunctionID functionId, ReJITID rejitId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ReJITError(ModuleID moduleId, mdMethodDef methodId, FunctionID functionId, HRESULT hrStatus)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::MovedReferences2(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], SIZE_T cObjectIDRangeLength[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::SurvivingReferences2(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], SIZE_T cObjectIDRangeLength[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ConditionalWeakTableElementReferences(ULONG cRootRefs, ObjectID keyRefIds[], ObjectID valueRefIds[], GCHandleID rootIds[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::GetAssemblyReferences(const WCHAR *wszAssemblyPath, ICorProfilerAssemblyReferenceProvider *pAsmRefProvider)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::ModuleInMemorySymbolsUpdated(ModuleID moduleId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::DynamicMethodJITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock, LPCBYTE ilHeader, ULONG cbILHeader)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::DynamicMethodJITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::DynamicMethodUnloaded(FunctionID functionId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::EventPipeEventDelivered(
    EVENTPIPE_PROVIDER provider,
    DWORD eventId,
    DWORD eventVersion,
    ULONG cbMetadataBlob,
    LPCBYTE metadataBlob,
    ULONG cbEventData,
    LPCBYTE eventData,
    LPCGUID pActivityId,
    LPCGUID pRelatedActivityId,
    ThreadID eventThread,
    ULONG numStackFrames,
    UINT_PTR stackFrames[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::EventPipeProviderCreated(EVENTPIPE_PROVIDER provider)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::LoadAsNotificationOnly(BOOL *pbNotificationOnly)
{
    *pbNotificationOnly = FALSE;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::QueryInterface(REFIID riid, void **ppvObject)
{
    if (riid == __uuidof(ICorProfilerCallback11) ||
        riid == __uuidof(ICorProfilerCallback10) ||
        riid == __uuidof(ICorProfilerCallback9) ||
        riid == __uuidof(ICorProfilerCallback8) ||
        riid == __uuidof(ICorProfilerCallback7) ||
        riid == __uuidof(ICorProfilerCallback6) ||
        riid == __uuidof(ICorProfilerCallback5) ||
        riid == __uuidof(ICorProfilerCallback4) ||
        riid == __uuidof(ICorProfilerCallback3) ||
        riid == __uuidof(ICorProfilerCallback2) ||
        riid == __uuidof(ICorProfilerCallback)  ||
        riid == IID_IUnknown)
    {
        *ppvObject = this;
        this->AddRef();
        return S_OK;
    }

    *ppvObject = nullptr;
    return E_NOINTERFACE;
}

ULONG STDMETHODCALLTYPE Profiler::AddRef(void)
{
    return std::atomic_fetch_add(&this->refCount, 1) + 1;
}

ULONG STDMETHODCALLTYPE Profiler::Release(void)
{
    int count = std::atomic_fetch_sub(&this->refCount, 1) - 1;

    if (count <= 0)
    {
        delete this;
    }

    return count;
}

String Profiler::GetFunctionIDName(FunctionID funcId)
{
    // If the FunctionID is 0, we could be dealing with a native function.
    if (funcId == 0)
    {
        return WCHAR("Unknown_Native_Function");
    }

    String name;

    ClassID classId = NULL;
    ModuleID moduleId = NULL;
    mdToken token = NULL;
    ULONG32 nTypeArgs = NULL;
    ClassID typeArgs[SHORT_LENGTH];
    COR_PRF_FRAME_INFO frameInfo = NULL;

    HRESULT hr = S_OK;
    hr = pCorProfilerInfo->GetFunctionInfo2(funcId,
                                            frameInfo,
                                            &classId,
                                            &moduleId,
                                            &token,
                                            SHORT_LENGTH,
                                            &nTypeArgs,
                                            typeArgs);
    if (FAILED(hr))
    {
        printf("FAIL: GetFunctionInfo2 call failed with hr=0x%x\n", hr);
        return WCHAR("FuncNameLookupFailed");
    }

    COMPtrHolder<IMetaDataImport> pIMDImport;
    hr = pCorProfilerInfo->GetModuleMetaData(moduleId,
                                             ofRead,
                                             IID_IMetaDataImport,
                                             (IUnknown **)&pIMDImport);
    if (FAILED(hr))
    {
        printf("FAIL: GetModuleMetaData call failed with hr=0x%x\n", hr);
        return WCHAR("FuncNameLookupFailed");
    }

    WCHAR funcName[STRING_LENGTH];
    hr = pIMDImport->GetMethodProps(token,
                                    NULL,
                                    funcName,
                                    STRING_LENGTH,
                                    0,
                                    0,
                                    NULL,
                                    NULL,
                                    NULL,
                                    NULL);
    if (FAILED(hr))
    {
        printf("FAIL: GetMethodProps call failed with hr=0x%x", hr);
        return WCHAR("FuncNameLookupFailed");
    }

    name += funcName;

    // Fill in the type parameters of the generic method
    if (nTypeArgs > 0)
        name += WCHAR("<");

    for(ULONG32 i = 0; i < nTypeArgs; i++)
    {
        name += GetClassIDName(typeArgs[i]);

        if ((i + 1) != nTypeArgs)
            name += WCHAR(", ");
    }

    if (nTypeArgs > 0)
        name += WCHAR(">");

    return name;
}

String Profiler::GetClassIDName(ClassID classId)
{
    ModuleID modId;
    mdTypeDef classToken;
    ClassID parentClassID;
    ULONG32 nTypeArgs;
    ClassID typeArgs[SHORT_LENGTH];
    HRESULT hr = S_OK;

    if (classId == NULL)
    {
        printf("FAIL: Null ClassID passed in\n");
        return WCHAR("");
    }

    hr = pCorProfilerInfo->GetClassIDInfo2(classId,
                                           &modId,
                                           &classToken,
                                           &parentClassID,
                                           SHORT_LENGTH,
                                           &nTypeArgs,
                                           typeArgs);
    if (CORPROF_E_CLASSID_IS_ARRAY == hr)
    {
        // We have a ClassID of an array.
        return WCHAR("ArrayClass");
    }
    else if (CORPROF_E_CLASSID_IS_COMPOSITE == hr)
    {
        // We have a composite class
        return WCHAR("CompositeClass");
    }
    else if (CORPROF_E_DATAINCOMPLETE == hr)
    {
        // type-loading is not yet complete. Cannot do anything about it.
        return WCHAR("DataIncomplete");
    }
    else if (FAILED(hr))
    {
        printf("FAIL: GetClassIDInfo returned 0x%x for ClassID %x\n", hr, (unsigned int)classId);
        return WCHAR("GetClassIDNameFailed");
    }

    COMPtrHolder<IMetaDataImport> pMDImport;
    hr = pCorProfilerInfo->GetModuleMetaData(modId,
                                             (ofRead | ofWrite),
                                             IID_IMetaDataImport,
                                             (IUnknown **)&pMDImport );
    if (FAILED(hr))
    {
        printf("FAIL: GetModuleMetaData call failed with hr=0x%x\n", hr);
        return WCHAR("ClassIDLookupFailed");
    }

    WCHAR wName[LONG_LENGTH];
    DWORD dwTypeDefFlags = 0;
    hr = pMDImport->GetTypeDefProps(classToken,
                                         wName,
                                         LONG_LENGTH,
                                         NULL,
                                         &dwTypeDefFlags,
                                         NULL);
    if (FAILED(hr))
    {
        printf("FAIL: GetModuleMetaData call failed with hr=0x%x\n", hr);
        return WCHAR("ClassIDLookupFailed");
    }

    String name = wName;
    if (nTypeArgs > 0)
        name += WCHAR("<");

    for(ULONG32 i = 0; i < nTypeArgs; i++)
    {

        String typeArgClassName;
        typeArgClassName.Clear();
        name += GetClassIDName(typeArgs[i]);

        if ((i + 1) != nTypeArgs)
            name += WCHAR(", ");
    }

    if (nTypeArgs > 0)
        name += WCHAR(">");

    return name;
}

String Profiler::GetModuleIDName(ModuleID modId)
{
    WCHAR moduleName[STRING_LENGTH];
    ULONG nameLength = 0;
    AssemblyID assemID;

    if (modId == NULL)
    {
        printf("FAIL: Null ModuleID\n");
        return WCHAR("NullModuleIDPassedIn");
    }

    HRESULT hr = pCorProfilerInfo->GetModuleInfo(modId,
                                                 NULL,
                                                 STRING_LENGTH,
                                                 &nameLength,
                                                 moduleName,
                                                 &assemID);
    if (FAILED(hr))
    {
        printf("FAIL: GetModuleInfo call failed with hr=0x%x\n", hr);
        return WCHAR("ModuleIDLookupFailed");
    }

    return moduleName;
}

void Profiler::SetCallback(ProfilerCallback cb)
{
    assert(cb != NULL);
    s_callback = cb;
    s_callbackSet.Signal();
}

void Profiler::NotifyManagedCodeViaCallback(ICorProfilerInfo14  *pCorProfilerInfo)
{
    s_callbackSet.Wait();

    thread callbackThread([&]()
    {
        // The destructor will be called from the profiler detach thread, which causes
        // some crst order asserts if we call back in to managed code. Spin up
        // a new thread to avoid that.
        pCorProfilerInfo->InitializeCurrentThread();
        s_callback(); 
    });

    callbackThread.join();
}

extern "C" EXPORT void STDMETHODCALLTYPE PassCallbackToProfiler(ProfilerCallback callback)
{
    Profiler::SetCallback(callback);
}

#ifndef WIN32
// pal_assert expects DebugBreak and GetCurrentProcessID
void DebugBreak()
{

}

DWORD GetCurrentProcessId()
{
    return -1;
}
#endif // WIN32
