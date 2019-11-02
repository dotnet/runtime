// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// Contains helper types for assembly binding host infrastructure.

#ifndef __CLRPRIVBINDERUTIL_H__
#define __CLRPRIVBINDERUTIL_H__

#include "holder.h"
#include "internalunknownimpl.h"
#include "clrprivbinding.h"
#include "slist.h"
#ifdef FEATURE_COMINTEROP
#include "windowsstring.h"
#endif // FEATURE_COMINTEROP
#include "strongnameholders.h"

//=====================================================================================================================
#define STANDARD_BIND_CONTRACT  \
    CONTRACTL {                 \
        NOTHROW;                \
        GC_TRIGGERS;            \
        MODE_PREEMPTIVE;        \
    } CONTRACTL_END

//=====================================================================================================================
// Forward declarations
interface ICLRPrivAssembly;
typedef DPTR(ICLRPrivAssembly) PTR_ICLRPrivAssembly;
typedef DPTR(ICLRPrivBinder) PTR_ICLRPrivBinder;

//=====================================================================================================================
#define VALIDATE_CONDITION(condition, fail_op)  \
    do {                                        \
        _ASSERTE((condition));                  \
        if (!(condition))                       \
            fail_op;                            \
    } while (false)

#define VALIDATE_PTR_RET(val) VALIDATE_CONDITION((val) != nullptr, return E_POINTER)
#define VALIDATE_PTR_THROW(val) VALIDATE_CONDITION((val) != nullptr, ThrowHR(E_POINTER))
#define VALIDATE_ARG_RET(condition) VALIDATE_CONDITION(condition, return E_INVALIDARG)
#define VALIDATE_ARG_THROW(condition) VALIDATE_CONDITION(condition, ThrowHR(E_INVALIDARG))

//=====================================================================================================================
namespace CLRPrivBinderUtil
{
    //=================================================================================================================
    class CLRPrivResourcePathImpl :
        public IUnknownCommon2<ICLRPrivResource, IID_ICLRPrivResource, ICLRPrivResourcePath, IID_ICLRPrivResourcePath>
    {
    public:
        //---------------------------------------------------------------------------------------------
        CLRPrivResourcePathImpl(LPCWSTR wzPath);

        //---------------------------------------------------------------------------------------------
        LPCWSTR GetPath()
        { return m_wzPath; }

        //---------------------------------------------------------------------------------------------
        STDMETHOD(GetResourceType)(
            IID* pIID)
        {
            LIMITED_METHOD_CONTRACT;
            if (pIID == nullptr)
                return E_INVALIDARG;
            *pIID = __uuidof(ICLRPrivResourcePath);
            return S_OK;
        }

        //---------------------------------------------------------------------------------------------
        STDMETHOD(GetPath)(
            DWORD cchBuffer,
            LPDWORD pcchBuffer,
            __inout_ecount_part(cchBuffer, *pcchBuffer) LPWSTR wzBuffer);

    private:
        //---------------------------------------------------------------------------------------------
        NewArrayHolder<WCHAR> m_wzPath;
    };

    //=================================================================================================================
    // Types for WStringList (used in WinRT binders)

    typedef SListElem< PTR_WSTR > WStringListElem;
    typedef DPTR(WStringListElem) PTR_WStringListElem;
    typedef SList< WStringListElem, false /* = fHead default value */, PTR_WStringListElem > WStringList;
    typedef DPTR(WStringList)     PTR_WStringList;

    // Destroys list of strings (code:WStringList).
    void WStringList_Delete(WStringList * pList);

#ifndef DACCESS_COMPILE
    //=====================================================================================================================
    // Holder of allocated code:WStringList (helper class for WinRT binders - e.g. code:CLRPrivBinderWinRT::GetFileNameListForNamespace).
    class WStringListHolder
    {
    public:
        WStringListHolder(WStringList * pList = nullptr)
        {
            LIMITED_METHOD_CONTRACT;
            m_pList = pList;
        }
        ~WStringListHolder()
        {
            LIMITED_METHOD_CONTRACT;
            Destroy();
        }

        void InsertTail(LPCWSTR wszValue)
        {
            CONTRACTL
            {
                THROWS;
                GC_NOTRIGGER;
                MODE_ANY;
            }
            CONTRACTL_END;

            NewArrayHolder<WCHAR> wszElemValue = DuplicateStringThrowing(wszValue);
            NewHolder<WStringListElem> pElem = new WStringListElem(wszElemValue);

            if (m_pList == nullptr)
            {
                m_pList = new WStringList();
            }

            m_pList->InsertTail(pElem.Extract());
            // The string is now owned by the list
            wszElemValue.SuppressRelease();
        }

        WStringList * GetValue()
        {
            LIMITED_METHOD_CONTRACT;
            return m_pList;
        }

        WStringList * Extract()
        {
            LIMITED_METHOD_CONTRACT;

            WStringList * pList = m_pList;
            m_pList = nullptr;
            return pList;
        }

    private:
        void Destroy()
        {
            LIMITED_METHOD_CONTRACT;

            if (m_pList != nullptr)
            {
                WStringList_Delete(m_pList);
                m_pList = nullptr;
            }
        }

    private:
        WStringList * m_pList;
    };  // class WStringListHolder
#endif //!DACCESS_COMPILE

#ifdef FEATURE_COMINTEROP
    //=====================================================================================================================
    // Holder of allocated array of HSTRINGs (helper class for WinRT binders - e.g. code:CLRPrivBinderWinRT::m_rgAltPaths).
    class HSTRINGArrayHolder
    {
    public:
        HSTRINGArrayHolder()
        {
            LIMITED_METHOD_CONTRACT;

            m_cValues = 0;
            m_rgValues = nullptr;
        }
#ifndef DACCESS_COMPILE
        ~HSTRINGArrayHolder()
        {
            LIMITED_METHOD_CONTRACT;
            Destroy();
        }

        // Destroys current array and allocates a new one with cValues elements.
        void Allocate(DWORD cValues)
        {
            STANDARD_VM_CONTRACT;

            Destroy();
            _ASSERTE(m_cValues == 0);

            if (cValues > 0)
            {
                m_rgValues = new HSTRING[cValues];
                m_cValues = cValues;

                // Initialize the array values
                for (DWORD i = 0; i < cValues; i++)
                {
                    m_rgValues[i] = nullptr;
                }
            }
        }
#endif //!DACCESS_COMPILE

        HSTRING GetAt(DWORD index) const
        {
            LIMITED_METHOD_CONTRACT;
            return m_rgValues[index];
        }

        HSTRING * GetRawArray()
        {
            LIMITED_METHOD_CONTRACT;
            return m_rgValues;
        }

        DWORD GetCount()
        {
            LIMITED_METHOD_CONTRACT;
            return m_cValues;
        }

    private:
#ifndef DACCESS_COMPILE
        void Destroy()
        {
            LIMITED_METHOD_CONTRACT;

            for (DWORD i = 0; i < m_cValues; i++)
            {
                if (m_rgValues[i] != nullptr)
                {
                    WindowsDeleteString(m_rgValues[i]);
                }
            }
            m_cValues = 0;

            if (m_rgValues != nullptr)
            {
                delete [] m_rgValues;
                m_rgValues = nullptr;
            }
        }
#endif //!DACCESS_COMPILE

    private:
        DWORD     m_cValues;
        HSTRING * m_rgValues;
    };  // class HSTRINGArrayHolder

#endif // FEATURE_COMINTEROP
} // namespace CLRPrivBinderUtil

#endif // __CLRPRIVBINDERUTIL_H__
