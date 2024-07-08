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

        inline AssemblyName *GetAssemblyName(BOOL fAddRef = FALSE);
        inline Assembly *GetAssembly(BOOL fAddRef = FALSE);

        inline BOOL GetIsContextBound();

        inline void SetResult(Assembly *pAssembly, bool isInContext = false);
        inline void SetResult(BindResult *pBindResult);

        inline void SetNoResult();
        inline BOOL HaveResult();

        inline void Reset();

        struct AttemptResult
        {
            HRESULT HResult;
            ReleaseHolder<Assembly> AssemblyHolder;
            bool Attempted = false;

            void Set(const AttemptResult *result);

            void Reset()
            {
                AssemblyHolder = nullptr;
                Attempted = false;
            }
        };

        // Set attempt result for binding to existing context entry or platform assemblies
        void SetAttemptResult(HRESULT hr, Assembly *pAssembly, bool isInContext = false);

        const AttemptResult* GetAttempt(bool foundInContext) const;

    protected:
        bool m_isContextBound;
        ReleaseHolder<Assembly> m_pAssembly;

        AttemptResult m_inContextAttempt;
        AttemptResult m_applicationAssembliesAttempt;
    };
};

#endif
