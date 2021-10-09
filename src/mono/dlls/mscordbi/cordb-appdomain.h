// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-APPDOMAIN.H
//

#ifndef __MONO_DEBUGGER_CORDB_APPDOMAIN_H__
#define __MONO_DEBUGGER_CORDB_APPDOMAIN_H__

#include <cordb.h>

class CordbAppDomain : public CordbBaseMono,
                       public ICorDebugAppDomain,
                       public ICorDebugAppDomain2,
                       public ICorDebugAppDomain3,
                       public ICorDebugAppDomain4
{
    CordbProcess* pProcess;

public:
    CordbAppDomain(Connection* conn, CordbProcess* ppProcess);
    ULONG STDMETHODCALLTYPE AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbAppDomain";
    }
    HRESULT STDMETHODCALLTYPE Stop(DWORD dwTimeoutIgnored);
    HRESULT STDMETHODCALLTYPE Continue(BOOL fIsOutOfBand);
    HRESULT STDMETHODCALLTYPE IsRunning(BOOL* pbRunning);
    HRESULT STDMETHODCALLTYPE HasQueuedCallbacks(ICorDebugThread* pThread, BOOL* pbQueued);
    HRESULT STDMETHODCALLTYPE EnumerateThreads(ICorDebugThreadEnum** ppThreads);
    HRESULT STDMETHODCALLTYPE SetAllThreadsDebugState(CorDebugThreadState state, ICorDebugThread* pExceptThisThread);
    HRESULT STDMETHODCALLTYPE Detach(void);
    HRESULT STDMETHODCALLTYPE Terminate(UINT exitCode);
    HRESULT STDMETHODCALLTYPE CanCommitChanges(ULONG cSnapshots, ICorDebugEditAndContinueSnapshot* pSnapshots[], ICorDebugErrorInfoEnum** pError);
    HRESULT STDMETHODCALLTYPE CommitChanges(ULONG cSnapshots, ICorDebugEditAndContinueSnapshot* pSnapshots[], ICorDebugErrorInfoEnum** pError);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppInterface);
    HRESULT STDMETHODCALLTYPE GetProcess(ICorDebugProcess** ppProcess);
    HRESULT STDMETHODCALLTYPE EnumerateAssemblies(ICorDebugAssemblyEnum** ppAssemblies);
    HRESULT STDMETHODCALLTYPE GetModuleFromMetaDataInterface(IUnknown* pIMetaData, ICorDebugModule** ppModule);
    HRESULT STDMETHODCALLTYPE EnumerateBreakpoints(ICorDebugBreakpointEnum** ppBreakpoints);
    HRESULT STDMETHODCALLTYPE EnumerateSteppers(ICorDebugStepperEnum** ppSteppers);
    HRESULT STDMETHODCALLTYPE IsAttached(BOOL* pbAttached);
    HRESULT STDMETHODCALLTYPE GetName(ULONG32 cchName, ULONG32* pcchName, WCHAR szName[]);
    HRESULT STDMETHODCALLTYPE GetObject(ICorDebugValue** ppObject);
    HRESULT STDMETHODCALLTYPE Attach(void);
    HRESULT STDMETHODCALLTYPE GetID(ULONG32* pId);
    HRESULT STDMETHODCALLTYPE GetArrayOrPointerType(CorElementType  elementType,
                                  ULONG32         nRank,
                                  ICorDebugType*  pTypeArg,
                                  ICorDebugType** ppType);
    HRESULT STDMETHODCALLTYPE GetFunctionPointerType(ULONG32 nTypeArgs, ICorDebugType* ppTypeArgs[], ICorDebugType** ppType);
    HRESULT STDMETHODCALLTYPE GetCachedWinRTTypesForIIDs(ULONG32 cReqTypes, GUID* iidsToResolve, ICorDebugTypeEnum** ppTypesEnum);
    HRESULT STDMETHODCALLTYPE GetCachedWinRTTypes(ICorDebugGuidToTypeEnum** ppGuidToTypeEnum);
    HRESULT STDMETHODCALLTYPE GetObjectForCCW(CORDB_ADDRESS ccwPointer, ICorDebugValue** ppManagedObject);
};

class CordbAppDomainEnum : public CordbBaseMono, public ICorDebugAppDomainEnum
{
    DWORD         current_pos;
    CordbProcess* pProcess;

public:
    CordbAppDomainEnum(Connection* conn, CordbProcess* ppProcess);
    ULONG STDMETHODCALLTYPE AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbAppDomainEnum";
    }
    HRESULT STDMETHODCALLTYPE Next(ULONG celt, ICorDebugAppDomain* values[], ULONG* pceltFetched);
    HRESULT STDMETHODCALLTYPE Skip(ULONG celt);
    HRESULT STDMETHODCALLTYPE Reset(void);
    HRESULT STDMETHODCALLTYPE Clone(ICorDebugEnum** ppEnum);
    HRESULT STDMETHODCALLTYPE GetCount(ULONG* pcelt);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject);
};

#endif
