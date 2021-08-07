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

#include "assemblyentry.hpp"
#include "assembly.hpp"

namespace BINDER_SPACE
{
    class ContextEntry : public AssemblyEntry
    {
    public:
        typedef enum
        {
            RESULT_FLAG_NONE             = 0x00,
            //RESULT_FLAG_IS_DYNAMIC_BIND  = 0x01,
            RESULT_FLAG_IS_IN_TPA        = 0x02,
            //RESULT_FLAG_FROM_MANIFEST    = 0x04,
            RESULT_FLAG_CONTEXT_BOUND    = 0x08,
            RESULT_FLAG_FIRST_REQUEST    = 0x10,
        } ResultFlags;

        ContextEntry() : AssemblyEntry()
        {
            m_dwResultFlags = RESULT_FLAG_NONE;
            m_pAssembly = NULL;
        }

        ~ContextEntry()
        {
            SAFE_RELEASE(m_pAssembly);
        }

        BOOL GetIsInTPA()
        {
            return ((m_dwResultFlags & RESULT_FLAG_IS_IN_TPA) != 0);
        }

        void SetIsInTPA(BOOL fIsInTPA)
        {
            if (fIsInTPA)
            {
                m_dwResultFlags |= RESULT_FLAG_IS_IN_TPA;
            }
            else
            {
                m_dwResultFlags &= ~RESULT_FLAG_IS_IN_TPA;
            }
        }

        BOOL GetIsFirstRequest()
        {
            return ((m_dwResultFlags & RESULT_FLAG_FIRST_REQUEST) != 0);
        }

        void SetIsFirstRequest(BOOL fIsFirstRequest)
        {
            if (fIsFirstRequest)
            {
                m_dwResultFlags |= RESULT_FLAG_FIRST_REQUEST;
            }
            else
            {
                m_dwResultFlags &= ~RESULT_FLAG_FIRST_REQUEST;
            }
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

    protected:
        DWORD m_dwResultFlags;
        Assembly *m_pAssembly;
    };
};

#endif
