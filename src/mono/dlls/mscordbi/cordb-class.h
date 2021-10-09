// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-CLASS.H
//

#ifndef __MONO_DEBUGGER_CORDB_CLASS_H__
#define __MONO_DEBUGGER_CORDB_CLASS_H__

#include <cordb.h>

class CordbClass : public CordbBaseMono, public ICorDebugClass, public ICorDebugClass2
{
    mdToken m_metadataToken;
    int     m_debuggerModuleId;
    int     m_debuggerId;
public:
    CordbClass(Connection* conn, mdToken token, int module_id);
    int GetDebuggerId();
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
        return "CordbClass";
    }
    HRESULT STDMETHODCALLTYPE GetModule(ICorDebugModule** pModule);
    HRESULT STDMETHODCALLTYPE GetToken(mdTypeDef* pTypeDef);
    HRESULT STDMETHODCALLTYPE GetStaticFieldValue(mdFieldDef fieldDef, ICorDebugFrame* pFrame, ICorDebugValue** ppValue);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject);

    HRESULT STDMETHODCALLTYPE GetParameterizedType(CorElementType  elementType,
                                 ULONG32         nTypeArgs,
                                 ICorDebugType*  ppTypeArgs[],
                                 ICorDebugType** ppType);
    HRESULT STDMETHODCALLTYPE SetJMCStatus(BOOL bIsJustMyCode);
};

#endif
