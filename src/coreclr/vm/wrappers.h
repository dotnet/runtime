// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef _WRAPPERS_H_
#define _WRAPPERS_H_

#include "metadata.h"
#include "interoputil.h"

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

template <typename TYPE>
inline void SafeComRelease(TYPE *value)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
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
    } CONTRACTL_END;

    SafeReleasePreemp((IUnknown*)value);
}

template<typename _TYPE>
using SafeComHolder = SpecializedWrapper<_TYPE, SafeComRelease<_TYPE>>;

// Use this holder if you're already in preemptive mode for other reasons,
// use SafeComHolder otherwise.
template<typename _TYPE>
using SafeComHolderPreemp = SpecializedWrapper<_TYPE, SafeComReleasePreemp<_TYPE>>;

//--------------------------------------------------------------------------------
// safe variant helper
void SafeVariantClear(_Inout_ VARIANT* pVar);

//-----------------------------------------------------------------------------
// VariantPtrHolder : Variant holder, Calls VariantClear on scope exit.
//
//  {
//      VariantHolder foo = pVar
//  } // Call SafeVariantClear on out of scope.
//-----------------------------------------------------------------------------

struct VariantHolderTraits final
{
    using Type = VARIANT;
    static constexpr Type Default() { return {}; }
    static void Free(Type& value)
    {
        WRAPPER_NO_CONTRACT;
        SafeVariantClear(&value);
    }
};

using VariantHolder = LifetimeHolder<VariantHolderTraits>;

struct VariantPtrHolderTraits final
{
    using Type = VARIANT*;
    static constexpr Type Default() { return NULL; }
    static void Free(Type value)
    {
        WRAPPER_NO_CONTRACT;
        if (value != NULL)
        {
            SafeVariantClear(value);
        }
    }
};

using VariantPtrHolder = LifetimeHolder<VariantPtrHolderTraits>;

#ifdef FEATURE_COMINTEROP
//-----------------------------------------------------------------------------
// SafeArrayPtrHolder : SafeArray holder, Calls SafeArrayDestroy on scope exit.
// In cooperative mode this holder should be used instead of code:SafeArrayHolder.
//
//  {
//      SafeArrayPtrHolder foo = pSafeArray
//  } // Call SafeArrayDestroy on out of scope.
//-----------------------------------------------------------------------------

struct SafeArrayPtrHolderTraits final
{
    using Type = SAFEARRAY*;
    static constexpr Type Default() { return NULL; }
    static void Free(Type value)
    {
        WRAPPER_NO_CONTRACT;

        if (value != NULL)
        {
            // SafeArrayDestroy may block and may also call back to MODE_PREEMPTIVE
            // runtime functions like e.g. code:Unknown_Release_Internal
            GCX_PREEMP();

            HRESULT hr; hr = SafeArrayDestroy(value);
            _ASSERTE(SUCCEEDED(hr));
        }
    }
};

using SafeArrayPtrHolder = LifetimeHolder<SafeArrayPtrHolderTraits>;

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
