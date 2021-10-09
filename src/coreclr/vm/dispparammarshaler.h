// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: DispParamMarshaler.h
//

//
// Definition of dispatch parameter marshalers.
//


#ifndef _DISPPARAMMARSHALER_H
#define _DISPPARAMMARSHALER_H

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP


#include "vars.hpp"
#include "mlinfo.h"

class DispParamMarshaler
{
public:
    DispParamMarshaler()
    {
        LIMITED_METHOD_CONTRACT;
    }

    virtual ~DispParamMarshaler()
    {
        LIMITED_METHOD_CONTRACT;
    }

    virtual BOOL RequiresManagedCleanup();
    virtual void MarshalNativeToManaged(VARIANT *pSrcVar, OBJECTREF *pDestObj);
    virtual void MarshalManagedToNative(OBJECTREF *pSrcObj, VARIANT *pDestVar);
    virtual void MarshalManagedToNativeRef(OBJECTREF *pSrcObj, VARIANT *pRefVar);
    virtual void CleanUpManaged(OBJECTREF *pObj);
};



class DispParamCurrencyMarshaler : public DispParamMarshaler
{
public:
    DispParamCurrencyMarshaler()
    {
        WRAPPER_NO_CONTRACT;
    }

    virtual ~DispParamCurrencyMarshaler()
    {
        WRAPPER_NO_CONTRACT;
    }

    virtual void MarshalManagedToNative(OBJECTREF *pSrcObj, VARIANT *pDestVar);
};


class DispParamOleColorMarshaler : public DispParamMarshaler
{
public:
    DispParamOleColorMarshaler()
    {
        WRAPPER_NO_CONTRACT;
    }

    virtual ~DispParamOleColorMarshaler()
    {
        WRAPPER_NO_CONTRACT;
    }

    virtual void MarshalNativeToManaged(VARIANT *pSrcVar, OBJECTREF *pDestObj);
    virtual void MarshalManagedToNative(OBJECTREF *pSrcObj, VARIANT *pDestVar);
    virtual void MarshalManagedToNativeRef(OBJECTREF *pSrcObj, VARIANT *pRefVar);
};


class DispParamErrorMarshaler : public DispParamMarshaler
{
public:
    DispParamErrorMarshaler()
    {
        WRAPPER_NO_CONTRACT;
    }

    virtual ~DispParamErrorMarshaler()
    {
        WRAPPER_NO_CONTRACT;
    }

    virtual void MarshalManagedToNative(OBJECTREF *pSrcObj, VARIANT *pDestVar);
};



class DispParamInterfaceMarshaler : public DispParamMarshaler
{
public:
    DispParamInterfaceMarshaler(BOOL bDispatch, MethodTable* pIntfMT, MethodTable *pClassMT, BOOL bClassIsHint) :
    m_pIntfMT(pIntfMT),
    m_pClassMT(pClassMT),
    m_bDispatch(bDispatch),
    m_bClassIsHint(bClassIsHint)
    {
        WRAPPER_NO_CONTRACT;
    }

    virtual ~DispParamInterfaceMarshaler()
    {
        WRAPPER_NO_CONTRACT;
    }

    virtual void MarshalNativeToManaged(VARIANT *pSrcVar, OBJECTREF *pDestObj);
    virtual void MarshalManagedToNative(OBJECTREF *pSrcObj, VARIANT *pDestVar);

private:
    // if return type is an interface, then the method table of the interface is cached here.
    // we need to cache this and use it when we call GetCOMIPFromObjectRef
    MethodTable*            m_pIntfMT;
    MethodTable*            m_pClassMT;
    BOOL                    m_bDispatch;
    BOOL                    m_bClassIsHint;
};

class DispParamArrayMarshaler : public DispParamMarshaler
{
public:
    DispParamArrayMarshaler(VARTYPE ElementVT, MethodTable *pElementMT) :
    m_ElementVT(ElementVT),
    m_pElementMT(pElementMT)
    {
        WRAPPER_NO_CONTRACT;
    }

    virtual ~DispParamArrayMarshaler()
    {
        WRAPPER_NO_CONTRACT;
    }

    virtual void MarshalNativeToManaged(VARIANT *pSrcVar, OBJECTREF *pDestObj);
    virtual void MarshalManagedToNative(OBJECTREF *pSrcObj, VARIANT *pDestVar);
    virtual void MarshalManagedToNativeRef(OBJECTREF *pSrcObj, VARIANT *pDestVar);

private:
    VARTYPE                 m_ElementVT;
    MethodTable*            m_pElementMT;
};


class DispParamRecordMarshaler : public DispParamMarshaler
{
public:
    DispParamRecordMarshaler(MethodTable *pRecordMT) :
    m_pRecordMT(pRecordMT)
    {
        WRAPPER_NO_CONTRACT;
    }

    virtual ~DispParamRecordMarshaler()
    {
        WRAPPER_NO_CONTRACT;
    }

    virtual void MarshalNativeToManaged(VARIANT *pSrcVar, OBJECTREF *pDestObj);
    virtual void MarshalManagedToNative(OBJECTREF *pSrcObj, VARIANT *pDestVar);

private:
    MethodTable*            m_pRecordMT;
};

class DispParamDelegateMarshaler : public DispParamMarshaler
{
public:
    DispParamDelegateMarshaler(MethodTable *pDelegateMT) :
	m_pDelegateMT(pDelegateMT)
    {
        WRAPPER_NO_CONTRACT;
    }

    virtual ~DispParamDelegateMarshaler()
    {
        WRAPPER_NO_CONTRACT;
    }

    virtual void MarshalNativeToManaged(VARIANT *pSrcVar, OBJECTREF *pDestObj);
    virtual void MarshalManagedToNative(OBJECTREF *pSrcObj, VARIANT *pDestVar);

private:
	MethodTable*			m_pDelegateMT;
};


class DispParamCustomMarshaler : public DispParamMarshaler
{
public:
    DispParamCustomMarshaler(CustomMarshalerHelper *pCMHelper, VARTYPE vt) :
    m_pCMHelper(pCMHelper),
    m_vt(vt)
    {
        WRAPPER_NO_CONTRACT;
    }

    virtual ~DispParamCustomMarshaler()
    {
        WRAPPER_NO_CONTRACT;
    }

    virtual BOOL RequiresManagedCleanup();
    virtual void MarshalNativeToManaged(VARIANT *pSrcVar, OBJECTREF *pDestObj);
    virtual void MarshalManagedToNative(OBJECTREF *pSrcObj, VARIANT *pDestVar);
    virtual void MarshalManagedToNativeRef(OBJECTREF *pSrcObj, VARIANT *pRefVar);
    virtual void CleanUpManaged(OBJECTREF *pObj);

private:
    CustomMarshalerHelper*  m_pCMHelper;
    VARTYPE                 m_vt;
};

#endif // _DISPPARAMMARSHALER_H
