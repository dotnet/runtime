// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-CLASS.CPP
//

#include <cordb-breakpoint.h>
#include <cordb-class.h>
#include <cordb-process.h>
#include <cordb-value.h>
#include <cordb.h>

#include "cordb-assembly.h"

using namespace std;

CordbClass::CordbClass(Connection* conn, mdToken token, int module_id) : CordbBaseMono(conn)
{
    this->m_metadataToken = token;
    this->m_debuggerModuleId    = module_id;
}

HRESULT STDMETHODCALLTYPE CordbClass::GetModule(ICorDebugModule** pModule)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbClass - GetModule - IMPLEMENTED\n"));
    if (pModule)
    {
        CordbModule* module = conn->GetProcess()->GetModule(m_debuggerModuleId);
        if (module)
        {
            *pModule = static_cast<ICorDebugModule*>(module);
            (*pModule)->AddRef();
            return S_OK;
        }
    }
    return S_FALSE;
}

HRESULT STDMETHODCALLTYPE CordbClass::GetToken(mdTypeDef* pTypeDef)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbClass - GetToken - IMPLEMENTED\n"));
    *pTypeDef = m_metadataToken;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbClass::GetStaticFieldValue(mdFieldDef       fieldDef,
                                                          ICorDebugFrame*  pFrame,
                                                          ICorDebugValue** ppValue)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbClass - GetStaticFieldValue - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbClass::QueryInterface(REFIID id, void** pInterface)
{
    if (id == IID_ICorDebugClass)
    {
        *pInterface = static_cast<ICorDebugClass*>(this);
    }
    else if (id == IID_ICorDebugClass2)
    {
        *pInterface = static_cast<ICorDebugClass2*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugClass*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbClass::GetParameterizedType(CorElementType  elementType,
                                                           ULONG32         nTypeArgs,
                                                           ICorDebugType*  ppTypeArgs[],
                                                           ICorDebugType** ppType)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbClass - GetParameterizedType - NOT IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbClass::SetJMCStatus(BOOL bIsJustMyCode)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbClass - SetJMCStatus - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}
