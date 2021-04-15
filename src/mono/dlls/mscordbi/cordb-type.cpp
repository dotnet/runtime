// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-TYPE.CPP
//

#include <cordb-breakpoint.h>
#include <cordb-class.h>
#include <cordb-type.h>
#include <cordb.h>

using namespace std;

CordbType::CordbType(CorElementType type, Connection* conn, CordbClass* klass, CordbType* typeParameter)
    : CordbBaseMono(conn)
{
    if (type == ELEMENT_TYPE_CLASS && klass == NULL)
        assert(0);
    this->m_pClass         = klass;
    this->m_type           = type;
    this->m_pTypeParameter = typeParameter;
    m_pTypeEnum            = NULL;
    if (typeParameter)
        typeParameter->InternalAddRef();
    if (klass)
        klass->InternalAddRef();
}

CordbType::~CordbType()
{
    if (m_pClass)
        m_pClass->InternalRelease();
    if (m_pTypeParameter)
        m_pTypeParameter->InternalRelease();
    if (m_pTypeEnum)
        m_pTypeEnum->InternalRelease();
}

HRESULT STDMETHODCALLTYPE CordbType::GetType(CorElementType* ty)
{
    *ty = m_type;
    LOG((LF_CORDB, LL_INFO1000000, "CordbType - GetType - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbType::GetClass(ICorDebugClass** ppClass)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbType - GetClass - IMPLEMENTED\n"));
    if (!m_pClass)
    {
        LOG((LF_CORDB, LL_INFO100000, "CordbType - GetClass - NO CLASS\n"));
        return S_OK;
    }
    m_pClass->QueryInterface(IID_ICorDebugClass, (void**)ppClass);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbType::EnumerateTypeParameters(ICorDebugTypeEnum** ppTyParEnum)
{
    if (m_pTypeEnum == NULL)
    {
        m_pTypeEnum = new CordbTypeEnum(conn, m_pTypeParameter);
        m_pTypeEnum->InternalAddRef();
    }
    m_pTypeEnum->QueryInterface(IID_ICorDebugTypeEnum, (void**)ppTyParEnum);

    LOG((LF_CORDB, LL_INFO1000000, "CordbType - EnumerateTypeParameters - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbType::GetFirstTypeParameter(ICorDebugType** value)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbType - GetFirstTypeParameter - IMPLEMENTED\n"));
    m_pTypeParameter->QueryInterface(IID_ICorDebugType, (void**)value);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbType::GetBase(ICorDebugType** pBase)
{
    LOG((LF_CORDB, LL_INFO1000000, "CordbType - GetBase - IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbType::GetStaticFieldValue(mdFieldDef       fieldDef,
                                                         ICorDebugFrame*  pFrame,
                                                         ICorDebugValue** ppValue)
{
    if (m_pClass) {
        return m_pClass->GetStaticFieldValue(fieldDef, pFrame, ppValue);
    }
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbType::GetRank(ULONG32* pnRank)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbType - GetRank - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbType::QueryInterface(REFIID id, void** pInterface)
{
    if (id == IID_ICorDebugType)
        *pInterface = static_cast<ICorDebugType*>(this);
    else if (id == IID_ICorDebugType2)
        *pInterface = static_cast<ICorDebugType2*>(this);
    else if (id == IID_IUnknown)
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugType*>(this));
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbType::GetTypeID(COR_TYPEID* id)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbType - GetTypeID - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

CordbTypeEnum::CordbTypeEnum(Connection* conn, CordbType* type) : CordbBaseMono(conn)
{
    this->m_pType = type;
    if (type)
        type->InternalAddRef();
}

CordbTypeEnum::~CordbTypeEnum()
{
    if (m_pType)
        m_pType->InternalRelease();
}

HRESULT STDMETHODCALLTYPE CordbTypeEnum::Next(ULONG celt, ICorDebugType* values[], ULONG* pceltFetched)
{
    if (m_pType != NULL) {
        m_pType->QueryInterface(IID_ICorDebugType, (void**)&values[0]);
        *pceltFetched = celt;
    }
    LOG((LF_CORDB, LL_INFO1000000, "CordbTypeEnum - Next - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbTypeEnum::Skip(ULONG celt)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbTypeEnum - Skip - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbTypeEnum::Reset(void)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbTypeEnum - Reset - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbTypeEnum::Clone(ICorDebugEnum** ppEnum)
{
    LOG((LF_CORDB, LL_INFO100000, "CordbTypeEnum - Clone - NOT IMPLEMENTED\n"));
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE CordbTypeEnum::GetCount(ULONG* pcelt)
{
    if (m_pType != NULL)
        *pcelt = 1;
    else
        *pcelt = 0;
    LOG((LF_CORDB, LL_INFO1000000, "CordbTypeEnum - GetCount - IMPLEMENTED\n"));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CordbTypeEnum::QueryInterface(REFIID id, void** pInterface)
{
    if (id == IID_ICorDebugEnum)
        *pInterface = static_cast<ICorDebugEnum*>(this);
    else if (id == IID_ICorDebugTypeEnum)
        *pInterface = static_cast<ICorDebugTypeEnum*>(this);
    else if (id == IID_IUnknown)
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugTypeEnum*>(this));
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
}
