// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef WindowsRuntime_h
#define WindowsRuntime_h

#include <roapi.h>
#include <windowsstring.h>
#include "holder.h"

#ifdef FEATURE_LEAVE_RUNTIME_HOLDER
    #define HR_LEAVE_RUNTIME_HOLDER(X)      \
        GCX_PREEMP();                       \
        LeaveRuntimeHolderNoThrow lrh(X);   \
        if (FAILED(lrh.GetHR()))            \
        {                                   \
            return lrh.GetHR();             \
        }
#else
    #define HR_LEAVE_RUNTIME_HOLDER(X) (void *)0;
#endif

#ifndef IID_INS_ARGS
    #define IID_INS_ARGS(ppType) __uuidof(**(ppType)), IID_INS_ARGS_Helper(ppType)
#endif

HRESULT StringCchLength(
    __in  LPCWSTR wz,
    __out UINT32  *pcch);

#ifndef CROSSGEN_COMPILE
namespace clr
{
    namespace winrt
    {
        using ABI::Windows::Foundation::GetActivationFactory;

        template <typename ItfT> inline
        HRESULT GetActivationFactory(
            __in WinRtStringRef const & wzActivatableClassId,
            __deref_out ItfT** ppItf)
        {
            LIMITED_METHOD_CONTRACT;
            HR_LEAVE_RUNTIME_HOLDER(::RoGetActivationFactory);
            return GetActivationFactory(wzActivatableClassId.Get(), ppItf);
        }

        template <typename ItfT>
        HRESULT GetActivationFactory(
            __in WinRtStringRef const & wzActivatableClassId,
            __in typename ReleaseHolder<ItfT>& hItf)
        {
            LIMITED_METHOD_CONTRACT;
            HR_LEAVE_RUNTIME_HOLDER(::RoGetActivationFactory);
            return GetActivationFactory(wzActivatableClassId.Get(), (ItfT**)&hItf);
        }
    } // namespace winrt
} // namespace clr
#endif //CROSSGEN_COMPILE
#undef HR_LEAVE_RUNTIME_HOLDER

#endif // WindowsRuntime_h


