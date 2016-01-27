// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
            RESULT_FLAG_IS_DYNAMIC_BIND  = 0x01,
            RESULT_FLAG_IS_IN_GAC        = 0x02,
            //RESULT_FLAG_FROM_MANIFEST    = 0x04,
            RESULT_FLAG_CONTEXT_BOUND    = 0x08,
            RESULT_FLAG_FIRST_REQUEST    = 0x10,
            RESULT_FLAG_IS_SHARABLE      = 0x20
        } ResultFlags;

        ContextEntry() : AssemblyEntry()
        {
            m_dwResultFlags = RESULT_FLAG_NONE;
            m_pIUnknownAssembly = NULL;
        }

        ~ContextEntry()
        {
            SAFE_RELEASE(m_pIUnknownAssembly);
        }
        
        BOOL GetIsDynamicBind()
        {
            return ((m_dwResultFlags & RESULT_FLAG_IS_DYNAMIC_BIND) != 0);
        }

        void SetIsDynamicBind(BOOL fIsDynamicBind)
        {
            if (fIsDynamicBind)
            {
                m_dwResultFlags |= RESULT_FLAG_IS_DYNAMIC_BIND;
            }
            else
            {
                m_dwResultFlags &= ~RESULT_FLAG_IS_DYNAMIC_BIND;
            }
        }

        BOOL GetIsInGAC()
        {
            return ((m_dwResultFlags & RESULT_FLAG_IS_IN_GAC) != 0);
        }

        void SetIsInGAC(BOOL fIsInGAC)
        {
            if (fIsInGAC)
            {
                m_dwResultFlags |= RESULT_FLAG_IS_IN_GAC;
            }
            else
            {
                m_dwResultFlags &= ~RESULT_FLAG_IS_IN_GAC;
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

        BOOL GetIsSharable()
        {
            return ((m_dwResultFlags & RESULT_FLAG_IS_SHARABLE) != 0);
        }

        void SetIsSharable(BOOL fIsSharable)
        {
            if (fIsSharable)
            {
                m_dwResultFlags |= RESULT_FLAG_IS_SHARABLE;
            }
            else
            {
                m_dwResultFlags &= ~RESULT_FLAG_IS_SHARABLE;
            }
        }

        IUnknown *GetAssembly(BOOL fAddRef = FALSE)
        {
            IUnknown *pIUnknownAssembly = m_pIUnknownAssembly;

            if (fAddRef && (pIUnknownAssembly != NULL))
            {
                pIUnknownAssembly->AddRef();
            }

            return pIUnknownAssembly;
        }

        void SetAssembly(IUnknown *pIUnknownAssembly)
        {
            SAFE_RELEASE(m_pIUnknownAssembly);

            if (pIUnknownAssembly != NULL)
            {
                pIUnknownAssembly->AddRef();
            }

            m_pIUnknownAssembly = pIUnknownAssembly;
        }

    protected:
        DWORD m_dwResultFlags;
        IUnknown *m_pIUnknownAssembly;
    };
};

#endif
