// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-TYPE.H
//

#ifndef __MONO_DEBUGGER_CORDB_TYPE_H__
#define __MONO_DEBUGGER_CORDB_TYPE_H__

#include <cordb.h>

class CordbType : public CordbBaseMono, public ICorDebugType, public ICorDebugType2
{
    CorElementType m_type;
    CordbClass*    m_pClass;
    CordbType*     m_pTypeParameter;
    CordbTypeEnum* m_pTypeEnum;

public:
    CordbType(CorElementType type, Connection* conn, CordbClass* klass = NULL, CordbType* typeParameter = NULL);
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
        return "CordbType";
    }
    ~CordbType();
    HRESULT STDMETHODCALLTYPE GetType(CorElementType* ty);
    HRESULT STDMETHODCALLTYPE GetClass(ICorDebugClass** ppClass);
    HRESULT STDMETHODCALLTYPE EnumerateTypeParameters(ICorDebugTypeEnum** ppTyParEnum);
    HRESULT STDMETHODCALLTYPE GetFirstTypeParameter(ICorDebugType** value);
    HRESULT STDMETHODCALLTYPE GetBase(ICorDebugType** pBase);
    HRESULT STDMETHODCALLTYPE GetStaticFieldValue(mdFieldDef fieldDef, ICorDebugFrame* pFrame, ICorDebugValue** ppValue);
    HRESULT STDMETHODCALLTYPE GetRank(ULONG32* pnRank);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject);

    HRESULT STDMETHODCALLTYPE GetTypeID(COR_TYPEID* id);
};

class CordbTypeEnum : public CordbBaseMono, public ICorDebugTypeEnum
{
    CordbType* m_pType;

public:
    CordbTypeEnum(Connection* conn, CordbType* type);
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
        return "CordbTypeEnum";
    }
    ~CordbTypeEnum();
    virtual HRESULT STDMETHODCALLTYPE Next(ULONG celt, ICorDebugType* values[], ULONG* pceltFetched);
    HRESULT STDMETHODCALLTYPE         Skip(ULONG celt);
    HRESULT STDMETHODCALLTYPE         Reset(void);
    HRESULT STDMETHODCALLTYPE         Clone(ICorDebugEnum** ppEnum);
    HRESULT STDMETHODCALLTYPE         GetCount(ULONG* pcelt);
    HRESULT STDMETHODCALLTYPE         QueryInterface(REFIID riid, void** ppvObject);
};

#endif
