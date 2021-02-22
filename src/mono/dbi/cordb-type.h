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
        return "CordbType";
    }
    ~CordbType();
    HRESULT GetType(CorElementType* ty);
    HRESULT GetClass(ICorDebugClass** ppClass);
    HRESULT
    EnumerateTypeParameters(ICorDebugTypeEnum** ppTyParEnum);
    HRESULT GetFirstTypeParameter(ICorDebugType** value);
    HRESULT GetBase(ICorDebugType** pBase);
    HRESULT GetStaticFieldValue(mdFieldDef fieldDef, ICorDebugFrame* pFrame, ICorDebugValue** ppValue);
    HRESULT GetRank(ULONG32* pnRank);
    HRESULT QueryInterface(REFIID riid, void** ppvObject);

    HRESULT GetTypeID(COR_TYPEID* id);
};

class CordbTypeEnum : public CordbBaseMono, public ICorDebugTypeEnum
{
    CordbType* m_pType;

public:
    CordbTypeEnum(Connection* conn, CordbType* type);
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
        return "CordbTypeEnum";
    }
    ~CordbTypeEnum();
    virtual HRESULT Next(ULONG celt, ICorDebugType* values[], ULONG* pceltFetched);
    HRESULT         Skip(ULONG celt);
    HRESULT         Reset(void);
    HRESULT         Clone(ICorDebugEnum** ppEnum);
    HRESULT         GetCount(ULONG* pcelt);
    HRESULT         QueryInterface(REFIID riid, void** ppvObject);
};

#endif
