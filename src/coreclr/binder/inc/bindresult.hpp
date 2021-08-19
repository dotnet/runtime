// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
        inline Assembly *GetAssembly(BOOL fAddRef = FALSE);

        inline BOOL GetIsInTPA();
        inline void SetIsInTPA(BOOL fIsInTPA);
        inline BOOL GetIsContextBound();
        inline void SetIsContextBound(BOOL fIsContextBound);
        inline BOOL GetIsFirstRequest();
        inline void SetIsFirstRequest(BOOL fIsFirstRequest);

        inline void SetResult(ContextEntry *pContextEntry, BOOL fIsContextBound = TRUE);
        inline void SetResult(Assembly *pAssembly);
        inline void SetResult(BindResult *pBindResult);

        inline void SetNoResult();
        inline BOOL HaveResult();

        inline void Reset();

        struct AttemptResult
        {
            HRESULT HResult;
            ReleaseHolder<Assembly> Assembly;
            bool Attempted = false;

            void Set(const AttemptResult *result);

            void Reset()
            {
                Assembly = nullptr;
                Attempted = false;
            }
        };

        // Set attempt result for binding to existing context entry
        void SetAttemptResult(HRESULT hr, ContextEntry *pContextEntry);

        // Set attempt result for binding to platform assemblies
        void SetAttemptResult(HRESULT hr, Assembly *pAssembly);

        const AttemptResult* GetAttempt(bool foundInContext) const;

    protected:
        DWORD m_dwResultFlags;
        AssemblyName *m_pAssemblyName;
        ReleaseHolder<Assembly> m_pAssembly;

        AttemptResult m_inContextAttempt;
        AttemptResult m_applicationAssembliesAttempt;
    };
};

#endif
