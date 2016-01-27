// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: umthunkhash.h
// 

//


#ifndef UMTHUNKHASH_H_
#define UMTHUNKHASH_H_
#include "dllimportcallback.h"

#ifdef FEATURE_MIXEDMODE // IJW
//
// A hashtable for u->m thunks not represented in the fixup tables.
//
class UMThunkHash : public CClosedHashBase {
    private:
        //----------------------------------------------------
        // Hash key for CClosedHashBase
        //----------------------------------------------------
        struct UTHKey {
            LPVOID          m_pTarget;
            PCCOR_SIGNATURE m_pSig;
            DWORD           m_cSig;
        };

        //----------------------------------------------------
        // Hash entry for CClosedHashBase
        //----------------------------------------------------
        struct UTHEntry {
            UTHKey           m_key;
            ELEMENTSTATUS    m_status;
            UMEntryThunk     *m_pUMEntryThunk;
            UMThunkMarshInfo m_UMThunkMarshInfo;
        };

    public:
        UMThunkHash(Module *pModule, AppDomain *pDomain);
        ~UMThunkHash(); 
        LPVOID GetUMThunk(LPVOID pTarget, PCCOR_SIGNATURE pSig, DWORD cSig);
        // *** OVERRIDES FOR CClosedHashBase ***/

        //*****************************************************************************
        // Hash is called with a pointer to an element in the table.  You must override
        // this method and provide a hash algorithm for your element type.
        //*****************************************************************************
            virtual unsigned int Hash(             // The key value.
                void const  *pData);                 // Raw data to hash.

        //*****************************************************************************
        // Compare is used in the typical memcmp way, 0 is eqaulity, -1/1 indicate
        // direction of miscompare.  In this system everything is always equal or not.
        //*****************************************************************************
        inline unsigned int Compare(          // 0, -1, or 1.
                              void const  *pData,               // Raw key data on lookup.
                              BYTE        *pElement);           // The element to compare data against.
                              
        //*****************************************************************************
        // Return true if the element is free to be used.
        //*****************************************************************************
            virtual ELEMENTSTATUS Status(           // The status of the entry.
                BYTE        *pElement);            // The element to check.

        //*****************************************************************************
        // Sets the status of the given element.
        //*****************************************************************************
            virtual void SetStatus(
                BYTE        *pElement,              // The element to set status for.
                ELEMENTSTATUS eStatus);           // New status.

        //*****************************************************************************
        // Returns the internal key value for an element.
        //*****************************************************************************
            virtual void *GetKey(                   // The data to hash on.
                BYTE        *pElement);            // The element to return data ptr for.

protected:
        Module      *m_pModule;
        ADID         m_dwAppDomainId;
        Crst         m_crst;
};

#endif
#endif
