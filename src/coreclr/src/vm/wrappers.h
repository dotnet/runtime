//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#ifndef _WRAPPERS_H_
#define _WRAPPERS_H_

#include "metadata.h"
#include "interoputil.h"
#ifdef FEATURE_COMINTEROP
#include "windowsstring.h"
#endif

class MDEnumHolder
{
public:
    inline MDEnumHolder(IMDInternalImport* IMDII)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(IMDII));
        }
        CONTRACTL_END;

        m_IMDII = IMDII;
        
    }

    inline ~MDEnumHolder()
    {
        WRAPPER_NO_CONTRACT;
        m_IMDII->EnumClose(&m_HEnum);
    }

    inline operator HENUMInternal()
    {
        LIMITED_METHOD_CONTRACT;
        return m_HEnum;   
    }

    inline HENUMInternal* operator&()
    {
        LIMITED_METHOD_CONTRACT;
        return static_cast<HENUMInternal*>(&m_HEnum);
    }

private:
    MDEnumHolder() {LIMITED_METHOD_CONTRACT;} // Must use parameterized constructor

    HENUMInternal       m_HEnum;
    IMDInternalImport*  m_IMDII;
};


//--------------------------------------------------------------------------------
// safe variant helper
void SafeVariantClear(_Inout_ VARIANT* pVar);

class VariantHolder
{
public:
    inline VariantHolder()
    {        
        LIMITED_METHOD_CONTRACT;
        memset(&m_var, 0, sizeof(VARIANT));
    }

    inline ~VariantHolder()
    {
        WRAPPER_NO_CONTRACT;
        SafeVariantClear(&m_var);
    }

    inline VARIANT* operator&()
    {
        LIMITED_METHOD_CONTRACT;
        return static_cast<VARIANT*>(&m_var);
    }

private:
    VARIANT  m_var;
};


template <typename TYPE> 
inline void SafeComRelease(TYPE *value)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SO_TOLERANT;
    } CONTRACTL_END;

    SafeRelease((IUnknown*)value);
}
template <typename TYPE> 
inline void SafeComReleasePreemp(TYPE *value)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        SO_TOLERANT;
    } CONTRACTL_END;

    SafeReleasePreemp((IUnknown*)value);
}

NEW_WRAPPER_TEMPLATE1(SafeComHolder, SafeComRelease<_TYPE>);

// Use this holder if you're already in preemptive mode for other reasons, 
// use SafeComHolder otherwise.
NEW_WRAPPER_TEMPLATE1(SafeComHolderPreemp, SafeComReleasePreemp<_TYPE>);



#ifdef FEATURE_COMINTEROP
#ifdef CROSSGEN_COMPILE
    namespace clr
    {
        namespace winrt
        {
            template <typename ItfT> inline
            HRESULT GetActivationFactory(
                __in WinRtStringRef const & wzActivatableClassId,
                __deref_out ItfT** ppItf)
            {
                LIMITED_METHOD_CONTRACT;
                return GetActivationFactory(wzActivatableClassId, ppItf);
            }

            template <typename ItfT> inline
            HRESULT GetActivationFactory(
                __in WinRtStringRef const & wzActivatableClassId,
                __out typename SafeComHolderPreemp<ItfT>* pItf)
            {
                STATIC_CONTRACT_WRAPPER;

                if (pItf == nullptr)
                    return E_INVALIDARG;

                return GetActivationFactory(wzActivatableClassId, (ItfT**)&(*pItf));
            }
        }
    }
#endif //CROSSGEN_COMPILE
#endif //FEATURE_COMINTEROP

//-----------------------------------------------------------------------------
// NewPreempHolder : New'ed memory holder, deletes in preemp mode.
//
//  {
//      NewPreempHolder<Foo> foo = new Foo ();
//  } // delete foo on out of scope in preemp mode.
//-----------------------------------------------------------------------------

template <typename TYPE> 
void DeletePreemp(TYPE *value)
{
    WRAPPER_NO_CONTRACT;

    GCX_PREEMP();
    delete value;
}

NEW_WRAPPER_TEMPLATE1(NewPreempHolder, DeletePreemp<_TYPE>);


//-----------------------------------------------------------------------------
// VariantPtrHolder : Variant holder, Calls VariantClear on scope exit.
//
//  {
//      VariantHolder foo = pVar
//  } // Call SafeVariantClear on out of scope.
//-----------------------------------------------------------------------------

FORCEINLINE void VariantPtrRelease(VARIANT* value)
{
    WRAPPER_NO_CONTRACT;

    if (value)
    {
        SafeVariantClear(value);
    }
}

class VariantPtrHolder : public Wrapper<VARIANT*, VariantPtrDoNothing, VariantPtrRelease, NULL>
{
public:
    VariantPtrHolder(VARIANT* p = NULL)
        : Wrapper<VARIANT*, VariantPtrDoNothing, VariantPtrRelease, NULL>(p)
    {
        LIMITED_METHOD_CONTRACT;
    }
    
    FORCEINLINE void operator=(VARIANT* p)
    {
        WRAPPER_NO_CONTRACT;

        Wrapper<VARIANT*, VariantPtrDoNothing, VariantPtrRelease, NULL>::operator=(p);
    }
};

//-----------------------------------------------------------------------------
// SafeArrayPtrHolder : SafeArray holder, Calls SafeArrayDestroy on scope exit.
// In cooperative mode this holder should be used instead of code:SafeArrayHolder.
//
//  {
//      SafeArrayPtrHolder foo = pSafeArray
//  } // Call SafeArrayDestroy on out of scope.
//-----------------------------------------------------------------------------

FORCEINLINE void SafeArrayPtrRelease(SAFEARRAY* value)
{
    WRAPPER_NO_CONTRACT;

    if (value)
    {
        // SafeArrayDestroy may block and may also call back to MODE_PREEMPTIVE
        // runtime functions like e.g. code:Unknown_Release_Internal
        GCX_PREEMP();

        HRESULT hr; hr = SafeArrayDestroy(value);
        _ASSERTE(SUCCEEDED(hr));
    }
}

class SafeArrayPtrHolder : public Wrapper<SAFEARRAY*, SafeArrayDoNothing, SafeArrayPtrRelease, NULL>
{
public:
    SafeArrayPtrHolder(SAFEARRAY* p = NULL)
        : Wrapper<SAFEARRAY*, SafeArrayDoNothing, SafeArrayPtrRelease, NULL>(p)
    {
        LIMITED_METHOD_CONTRACT;
    }

    FORCEINLINE void operator=(SAFEARRAY* p)
    {
        WRAPPER_NO_CONTRACT;

        Wrapper<SAFEARRAY*, SafeArrayDoNothing, SafeArrayPtrRelease, NULL>::operator=(p);
    }
};

//-----------------------------------------------------------------------------
// ZeroHolder : Sets value to zero on context exit.
//
//  {
//      ZeroHolder foo = &data;
//  } // set data to zero on context exit
//-----------------------------------------------------------------------------

FORCEINLINE void ZeroRelease(VOID* value)
{
    LIMITED_METHOD_CONTRACT;
    if (value)
    {
        (*(size_t*)value) = 0;
    }
}

class ZeroHolder : public Wrapper<VOID*, ZeroDoNothing, ZeroRelease, NULL>
{
public:
    ZeroHolder(VOID* p = NULL)
        : Wrapper<VOID*, ZeroDoNothing, ZeroRelease, NULL>(p)
    {
        LIMITED_METHOD_CONTRACT;
    }
    
    FORCEINLINE void operator=(VOID* p)
    {
        WRAPPER_NO_CONTRACT;

        Wrapper<VOID*, ZeroDoNothing, ZeroRelease, NULL>::operator=(p);
    }
};

#ifdef FEATURE_COMINTEROP
class TYPEATTRHolder
{
public:
    TYPEATTRHolder(ITypeInfo* pTypeInfo)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(CheckPointer(pTypeInfo, NULL_OK));
        }
        CONTRACTL_END;
        
        m_pTypeInfo = pTypeInfo;
        m_TYPEATTR = NULL;
    }

    ~TYPEATTRHolder()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_ANY;
            PRECONDITION(m_TYPEATTR ? CheckPointer(m_pTypeInfo) : CheckPointer(m_pTypeInfo, NULL_OK));
        }
        CONTRACTL_END;

        if (m_TYPEATTR)
        {
            GCX_PREEMP();
            m_pTypeInfo->ReleaseTypeAttr(m_TYPEATTR);
        }
    }

    inline void operator=(TYPEATTR* value)
    {
        LIMITED_METHOD_CONTRACT;
        m_TYPEATTR = value;
    }

    inline TYPEATTR** operator&()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_TYPEATTR;
    }

    inline TYPEATTR* operator->()
    {
        LIMITED_METHOD_CONTRACT;
        return m_TYPEATTR;
    }

private:
    TYPEATTRHolder ()
    {
        LIMITED_METHOD_CONTRACT;
    }

    ITypeInfo*      m_pTypeInfo;
    TYPEATTR*       m_TYPEATTR;
};
#endif // FEATURE_COMINTEROP

#endif // _WRAPPERS_H_
