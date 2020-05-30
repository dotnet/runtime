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
} // namespace CLRPrivBinderUtil

#endif // __CLRPRIVBINDERUTIL_H__
