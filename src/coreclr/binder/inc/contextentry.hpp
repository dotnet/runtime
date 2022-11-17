// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// ContextEntry.hpp
//


//
// Defines the ContextEntry class
//
// ============================================================

#ifndef __BINDER__CONTEXT_ENTRY_HPP__
#define __BINDER__CONTEXT_ENTRY_HPP__

#include "assembly.hpp"

namespace BINDER_SPACE
{
    class ContextEntry final
    {
    public:
        ContextEntry()
            : m_pAssembly { NULL }
        { }

        ~ContextEntry()
        {
            SAFE_RELEASE(m_pAssembly);
        }

        Assembly *GetAssembly(BOOL fAddRef = FALSE)
        {
            Assembly *pAssembly = m_pAssembly;

            if (fAddRef && (pAssembly != NULL))
            {
                pAssembly->AddRef();
            }

            return pAssembly;
        }

        void SetAssembly(Assembly *pAssembly)
        {
            SAFE_RELEASE(m_pAssembly);

            if (pAssembly != NULL)
            {
                pAssembly->AddRef();
            }

            m_pAssembly = pAssembly;
        }

        AssemblyName *GetAssemblyName(BOOL fAddRef = FALSE)
        {
            Assembly *pAssembly = m_pAssembly;
            if (pAssembly == NULL)
                return NULL;

            return pAssembly->GetAssemblyName(fAddRef);
        }

    private:
        Assembly *m_pAssembly;
    };
};

#endif
