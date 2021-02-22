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
    ULONG AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbAppDomain";
    }
    HRESULT Stop(DWORD dwTimeoutIgnored);
    HRESULT Continue(BOOL fIsOutOfBand);
    HRESULT IsRunning(BOOL* pbRunning);
    HRESULT HasQueuedCallbacks(ICorDebugThread* pThread, BOOL* pbQueued);
    HRESULT
    EnumerateThreads(ICorDebugThreadEnum** ppThreads);
    HRESULT
    SetAllThreadsDebugState(CorDebugThreadState state, ICorDebugThread* pExceptThisThread);
    HRESULT Detach(void);
    HRESULT Terminate(UINT exitCode);
    HRESULT
    CanCommitChanges(ULONG cSnapshots, ICorDebugEditAndContinueSnapshot* pSnapshots[], ICorDebugErrorInfoEnum** pError);
    HRESULT
    CommitChanges(ULONG cSnapshots, ICorDebugEditAndContinueSnapshot* pSnapshots[], ICorDebugErrorInfoEnum** pError);
    HRESULT
    QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppInterface);
    HRESULT
    GetProcess(ICorDebugProcess** ppProcess);
    HRESULT
    EnumerateAssemblies(ICorDebugAssemblyEnum** ppAssemblies);
    HRESULT GetModuleFromMetaDataInterface(IUnknown* pIMetaData, ICorDebugModule** ppModule);
    HRESULT
    EnumerateBreakpoints(ICorDebugBreakpointEnum** ppBreakpoints);
    HRESULT
    EnumerateSteppers(ICorDebugStepperEnum** ppSteppers);
    HRESULT IsAttached(BOOL* pbAttached);
    HRESULT
    GetName(ULONG32 cchName, ULONG32* pcchName, WCHAR szName[]);
    HRESULT GetObject(ICorDebugValue** ppObject);
    HRESULT Attach(void);
    HRESULT GetID(ULONG32* pId);
    HRESULT GetArrayOrPointerType(CorElementType  elementType,
                                  ULONG32         nRank,
                                  ICorDebugType*  pTypeArg,
                                  ICorDebugType** ppType);
    HRESULT
    GetFunctionPointerType(ULONG32 nTypeArgs, ICorDebugType* ppTypeArgs[], ICorDebugType** ppType);
    HRESULT
    GetCachedWinRTTypesForIIDs(ULONG32 cReqTypes, GUID* iidsToResolve, ICorDebugTypeEnum** ppTypesEnum);
    HRESULT
    GetCachedWinRTTypes(ICorDebugGuidToTypeEnum** ppGuidToTypeEnum);
    HRESULT
    GetObjectForCCW(CORDB_ADDRESS ccwPointer, ICorDebugValue** ppManagedObject);
};

class CordbAppDomainEnum : public CordbBaseMono, public ICorDebugAppDomainEnum
{
    DWORD         current_pos;
    CordbProcess* pProcess;

public:
    CordbAppDomainEnum(Connection* conn, CordbProcess* ppProcess);
    ULONG AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbAppDomainEnum";
    }
    HRESULT Next(ULONG celt, ICorDebugAppDomain* values[], ULONG* pceltFetched);
    HRESULT Skip(ULONG celt);
    HRESULT Reset(void);
    HRESULT Clone(ICorDebugEnum** ppEnum);
    HRESULT GetCount(ULONG* pcelt);
    HRESULT QueryInterface(REFIID riid, void** ppvObject);
};

#endif
