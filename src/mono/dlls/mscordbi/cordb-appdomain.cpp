// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-APPDOMAIN.CPP
//

#include <cordb-appdomain.h>
#include <cordb-process.h>
#include <cordb.h>

using namespace std;

CordbAppDomain::CordbAppDomain(Connection* conn, CordbProcess* ppProcess) : CordbBaseMono(conn)
{
    pProcess = ppProcess;
    pProcess->AddAppDomain(this);
}

HRESULT CordbAppDomain::Stop(DWORD dwTimeoutIgnored)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - Stop - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbAppDomain::Continue(BOOL fIsOutOfBand)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - Continue - IMPLEMENTED\n"));
    pProcess->Continue(fIsOutOfBand);
    return S_OK;
}

HRESULT CordbAppDomain::IsRunning(BOOL* pbRunning)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - IsRunning - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbAppDomain::HasQueuedCallbacks(ICorDebugThread* pThread, BOOL* pbQueued)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - HasQueuedCallbacks - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT
CordbAppDomain::EnumerateThreads(ICorDebugThreadEnum** ppThreads)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - EnumerateThreads - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT
CordbAppDomain::SetAllThreadsDebugState(CorDebugThreadState state, ICorDebugThread* pExceptThisThread)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - SetAllThreadsDebugState - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbAppDomain::Detach(void)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - Detach - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbAppDomain::Terminate(UINT exitCode)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - Terminate - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT
CordbAppDomain::CanCommitChanges(ULONG                             cSnapshots,
                                 ICorDebugEditAndContinueSnapshot* pSnapshots[],
                                 ICorDebugErrorInfoEnum**          pError)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - CanCommitChanges - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT
CordbAppDomain::CommitChanges(ULONG                             cSnapshots,
                              ICorDebugEditAndContinueSnapshot* pSnapshots[],
                              ICorDebugErrorInfoEnum**          pError)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - CommitChanges - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbAppDomain::QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppInterface)
{
    if (id == IID_ICorDebugAppDomain)
    {
        *ppInterface = (ICorDebugAppDomain*)this;
    }
    else if (id == IID_ICorDebugAppDomain2)
    {
        *ppInterface = (ICorDebugAppDomain2*)this;
    }
    else if (id == IID_ICorDebugAppDomain3)
    {
        *ppInterface = (ICorDebugAppDomain3*)this;
    }
    else if (id == IID_ICorDebugAppDomain4)
    {
        *ppInterface = (ICorDebugAppDomain4*)this;
    }
    else if (id == IID_ICorDebugController)
        *ppInterface = (ICorDebugController*)(ICorDebugAppDomain*)this;
    else if (id == IID_IUnknown)
        *ppInterface = (IUnknown*)(ICorDebugAppDomain*)this;
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
}

HRESULT CordbAppDomain::GetProcess(ICorDebugProcess** ppProcess)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - GetProcess - IMPLEMENTED\n"));
    pProcess->QueryInterface(IID_ICorDebugProcess, (void**)ppProcess);
    return S_OK;
}

HRESULT
CordbAppDomain::EnumerateAssemblies(ICorDebugAssemblyEnum** ppAssemblies)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - EnumerateAssemblies - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT
CordbAppDomain::GetModuleFromMetaDataInterface(IUnknown* pIMetaData, ICorDebugModule** ppModule)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - GetModuleFromMetaDataInterface - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT
CordbAppDomain::EnumerateBreakpoints(ICorDebugBreakpointEnum** ppBreakpoints)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - EnumerateBreakpoints - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbAppDomain::EnumerateSteppers(ICorDebugStepperEnum** ppSteppers)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - EnumerateSteppers - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbAppDomain::IsAttached(BOOL* pbAttached)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - IsAttached - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT
CordbAppDomain::GetName(ULONG32 cchName, ULONG32* pcchName, WCHAR szName[])
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbAppDomain - GetName - IMPLEMENTED\n"));
    if (cchName < strlen("DefaultDomain"))
    {
        *pcchName = (ULONG32)strlen("DefaultDomain") + 1;
        return S_OK;
    }
    wcscpy(szName, W("DefaultDomain"));

    return S_OK;
}

HRESULT CordbAppDomain::GetObject(ICorDebugValue** ppObject)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - GetObject - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbAppDomain::Attach(void)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - Attach - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT CordbAppDomain::GetID(ULONG32* pId)
{
    *pId = 0;
    LOG((LF_CORDB, LL_INFO1000000, "CordbAppDomain - GetID - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT CordbAppDomain::GetArrayOrPointerType(CorElementType elementType,
                                              ULONG32        nRank,

                                              ICorDebugType*  pTypeArg,
                                              ICorDebugType** ppType)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - GetArrayOrPointerType - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbAppDomain::GetFunctionPointerType(ULONG32 nTypeArgs, ICorDebugType* ppTypeArgs[], ICorDebugType** ppType)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - GetFunctionPointerType - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbAppDomain::GetCachedWinRTTypesForIIDs(ULONG32             cReqTypes,
                                                   GUID*               iidsToResolve,
                                                   ICorDebugTypeEnum** ppTypesEnum)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - GetCachedWinRTTypesForIIDs - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT CordbAppDomain::GetCachedWinRTTypes(ICorDebugGuidToTypeEnum** ppGuidToTypeEnum)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - GetCachedWinRTTypes - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT
CordbAppDomain::GetObjectForCCW(CORDB_ADDRESS ccwPointer, ICorDebugValue** ppManagedObject)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomain - GetObjectForCCW - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbAppDomainEnum::Next(ULONG celt, ICorDebugAppDomain* values[], ULONG* pceltFetched)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomainEnum - Next - NOT IMPLEMENTED\n"));
    *pceltFetched = celt;
    for (ULONG i = 0; i < celt; i++)
    {
        if (current_pos >= pProcess->m_pAddDomains->GetCount())
        {
            *pceltFetched = 0;
            return S_FALSE;
        }
        CordbAppDomain* appdomain = (CordbAppDomain*)pProcess->m_pAddDomains->Get(current_pos);
        appdomain->QueryInterface(IID_ICorDebugAppDomain, (void**)values + current_pos);
        current_pos++;
    }
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbAppDomainEnum::Skip(ULONG celt)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomainEnum - Skip - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbAppDomainEnum::Reset(void)
{
    current_pos = 0;
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomainEnum - Reset - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbAppDomainEnum::Clone(ICorDebugEnum** ppEnum)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbAppDomainEnum - Clone - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbAppDomainEnum::GetCount(ULONG* pcelt)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbAppDomainEnum - GetCount - IMPLEMENTED\n"));
    *pcelt = pProcess->m_pAddDomains->GetCount();
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbAppDomainEnum::QueryInterface(REFIID id, void** pInterface)
{
    if (id == IID_IUnknown)
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugAppDomainEnum*>(this));
    else if (id == IID_ICorDebugAppDomainEnum)
        *pInterface = static_cast<ICorDebugAppDomainEnum*>(this);
    else if (id == IID_ICorDebugEnum)
        *pInterface = static_cast<ICorDebugEnum*>(this);
    else
    {
        return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
}

CordbAppDomainEnum::CordbAppDomainEnum(Connection* conn, CordbProcess* ppProcess) : CordbBaseMono(conn)
{
    current_pos    = 0;
    this->pProcess = ppProcess;
}
