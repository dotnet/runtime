// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// BindResult.hpp
//


//
// Defines the BindResult class
//
// ============================================================

#ifndef __BINDER__BIND_RESULT_HPP__
#define __BINDER__BIND_RESULT_HPP__

#include "bindertypes.hpp"
 
namespace BINDER_SPACE
{
    class BindResult
    {
    public:
        inline BindResult();
        inline ~BindResult();

        inline AssemblyName *GetAssemblyName(BOOL fAddRef = FALSE);
        inline IUnknown *GetAssembly(BOOL fAddRef = FALSE);
        inline Assembly *GetAsAssembly(BOOL fAddRef = FALSE);

        inline AssemblyName *GetRetargetedAssemblyName();
        inline void SetRetargetedAssemblyName(AssemblyName *pRetargetedAssemblyName);

        inline BOOL GetIsDynamicBind();
        inline void SetIsDynamicBind(BOOL fIsDynamicBind);
        inline BOOL GetIsInGAC();
        inline void SetIsInGAC(BOOL fIsInGAC);
        inline BOOL GetIsContextBound();
        inline void SetIsContextBound(BOOL fIsContextBound);
        inline BOOL GetIsFirstRequest();
        inline void SetIsFirstRequest(BOOL fIsFirstRequest);
        inline BOOL GetIsSharable();
        inline void SetIsSharable(BOOL fIsSharable);

        inline void SetResult(ContextEntry *pContextEntry, BOOL fIsContextBound = TRUE);
        inline void SetResult(Assembly *pAssembly);
        inline void SetResult(BindResult *pBindResult);

        inline void SetNoResult();
        inline BOOL HaveResult();

        inline IUnknown *ExtractAssembly();
        inline void Reset();

    protected:
        DWORD m_dwResultFlags;
        AssemblyName *m_pAssemblyName;
        AssemblyName *m_pRetargetedAssemblyName;
        ReleaseHolder<IUnknown> m_pIUnknownAssembly;
    };
};

#endif
