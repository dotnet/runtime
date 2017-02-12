// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _METADATATRACKER_H_
#define _METADATATRACKER_H_

#if defined(FEATURE_PREJIT) && defined(FEATURE_WINDOWSPHONE)

#define METADATATRACKER_DATA 1
#if !defined(DACCESS_COMPILE)
#define METADATATRACKER_ENABLED 1
#endif

#endif

#if METADATATRACKER_ENABLED

#define METADATATRACKER_ONLY(s) (s)

#include "winbase.h"
#include "winwrap.h"
#include "holder.h"
#include "contract.h"
#include <limits.h>
#include <wchar.h>
#include <stdio.h>
#include "stdmacros.h"

#include "metamodelpub.h"

#define NUM_MD_SECTIONS (TBL_COUNT + MDPoolCount)

#define STRING_POOL     (TBL_COUNT + MDPoolStrings)
#define GUID_POOL       (TBL_COUNT + MDPoolGuids)
#define BLOB_POOL       (TBL_COUNT + MDPoolBlobs)
#define USERSTRING_POOL (TBL_COUNT + MDPoolUSBlobs)

class MetaDataTracker
{
    LPWSTR          m_ModuleName;
    BYTE           *m_MetadataBase;
    SIZE_T          m_MetadataSize;
    MetaDataTracker *m_next;

    BYTE           *m_mdSections[NUM_MD_SECTIONS];
    SIZE_T          m_mdSectionSize[NUM_MD_SECTIONS];
    SIZE_T          m_mdSectionRowSize[NUM_MD_SECTIONS];
    BOOL            m_bActivated;

    static BOOL     s_bEnabled; 

    static MetaDataTracker *m_MDTrackers;

public:
    // callback into IBCLogger.cpp. Done this crummy way because we can't include IBCLogger.h here nor link
    // to IBCLogger.cpp
    static void    (*s_IBCLogMetaDataAccess)(const void *addr);
    static void    (*s_IBCLogMetaDataSearch)(const void *result);

    MetaDataTracker(BYTE *baseAddress, DWORD mdSize, LPCWSTR modName)
    {
        CONTRACTL
        {
            CONSTRUCTOR_CHECK;
            THROWS;
            GC_NOTRIGGER;
            INJECT_FAULT(ThrowOutOfMemory());
            SO_INTOLERANT;
        }
        CONTRACTL_END;

        m_ModuleName = NULL;

        DWORD len = (DWORD)wcslen(modName);
        _ASSERTE(len + 1 != 0);      // Prevent Overflow
        m_ModuleName = new wchar_t[len + 1];
        NewArrayHolder<wchar_t> moduleNameHolder(m_ModuleName);
        wcscpy_s((wchar_t *)m_ModuleName, len + 1, (wchar_t *)modName);

        m_MetadataBase = baseAddress;
        m_MetadataSize = mdSize; 

        m_next = m_MDTrackers;
        m_MDTrackers = this;

        memset (m_mdSections, 0, NUM_MD_SECTIONS*sizeof(BYTE*));
        memset (m_mdSectionSize, 0, NUM_MD_SECTIONS*sizeof(SIZE_T));

        moduleNameHolder.SuppressRelease();
    }

    ~MetaDataTracker()
    {
        CONTRACTL
        {
            DESTRUCTOR_CHECK;
            NOTHROW;
            GC_NOTRIGGER;
            FORBID_FAULT;
            SO_INTOLERANT;
        }
        CONTRACTL_END;

        // Surely if we are dying, we are being deactivated as well
        Deactivate();

        if (m_ModuleName)
            delete m_ModuleName;

        // Remove this tracker from the global list of trackers

        MetaDataTracker *mdMod = m_MDTrackers;

        _ASSERTE (mdMod && "Trying to delete metadata tracker where none exist");

        // If ours is the first tracker
        if (mdMod == this)
        {
            m_MDTrackers = mdMod->m_next;
            mdMod->m_next = NULL;
        }
        else
        {
            // Now traverse thru the list and maintain the prev ptr.
            MetaDataTracker *mdModPrev = mdMod;
            mdMod = mdMod->m_next;
            while(mdMod)
            {
                if (mdMod == this)
                {
                    mdModPrev->m_next = mdMod->m_next;
                    mdMod->m_next = NULL;
                    break;
                }
                mdModPrev = mdMod;
                mdMod = mdMod->m_next;
            }
        }
    }

    static void Enable()
    {   LIMITED_METHOD_CONTRACT;
        s_bEnabled = TRUE;
    }

    static void Disable()
    {   LIMITED_METHOD_CONTRACT;
        s_bEnabled = FALSE;
    }

    static BOOL Enabled()
    {   LIMITED_METHOD_CONTRACT;
        return s_bEnabled;
    }

    static void NoteSection(DWORD secNum, void *address, size_t size, size_t rowSize)
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_GC_NOTRIGGER;
        STATIC_CONTRACT_SO_NOT_MAINLINE;

        if (!Enabled())
            return;
        
        MetaDataTracker *mdMod = m_MDTrackers;
        while( mdMod)
        {
            if (mdMod->NoteSectionInModule(secNum, address, size, rowSize))
                return;

            mdMod = mdMod->m_next;
        }
    }

    // With logging disabled this quickly returns the address that was passed in
    // this allows us to inline a smaller amount of code at callsites.
    __forceinline static void* NoteAccess(void *address)
    {
        WRAPPER_NO_CONTRACT;

        if (!Enabled())
            return address;

        return NoteAccessWorker(address);
    }

    __declspec(noinline) static void* NoteAccessWorker(void *address)
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_GC_NOTRIGGER;
        STATIC_CONTRACT_SO_NOT_MAINLINE;

        if (s_IBCLogMetaDataAccess != NULL)
            s_IBCLogMetaDataAccess(address);

        return address;
    }

    // See the comment above CMiniMdRW::GetHotMetadataTokensSearchAware
    __forceinline static void NoteSearch(void *result)
    {
        WRAPPER_NO_CONTRACT;

        if (!Enabled())
            return;

        NoteSearchWorker(result);
    }

    __declspec(noinline) static void NoteSearchWorker(void *result)
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_GC_NOTRIGGER;
        STATIC_CONTRACT_SO_NOT_MAINLINE;

        if (s_IBCLogMetaDataSearch != NULL && result != NULL)
            s_IBCLogMetaDataSearch(result);
    }

    static MetaDataTracker * FindTracker(BYTE *_MDBaseAddress)
    {
        LIMITED_METHOD_CONTRACT;

        if (!Enabled())
            return NULL;
        
        MetaDataTracker *mdMod = m_MDTrackers;
        while( mdMod)
        {
            if (mdMod->m_MetadataBase == _MDBaseAddress)
                return mdMod;

            mdMod = mdMod->m_next;
        }

        return NULL;
    }

    void Activate()
    {
        LIMITED_METHOD_CONTRACT;

        m_bActivated = TRUE;
    }

    void Deactivate()
    {
        LIMITED_METHOD_CONTRACT;

        m_bActivated = FALSE;
    }

    BOOL IsActivated()
    {
        LIMITED_METHOD_CONTRACT;

        return m_bActivated;
    }

    static MetaDataTracker *GetOrCreateMetaDataTracker (BYTE *baseAddress, DWORD mdSize, LPCWSTR modName)
    {
        CONTRACT(MetaDataTracker *)
        {
            THROWS;
            GC_NOTRIGGER;
            INJECT_FAULT(ThrowOutOfMemory());
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
            SO_INTOLERANT;
        }
        CONTRACT_END;

        MetaDataTracker *pTracker = NULL;

        if (MetaDataTracker::Enabled())
        {
            pTracker = MetaDataTracker::FindTracker(baseAddress);
            if (!pTracker)
            {
                FAULT_NOT_FATAL();  // It's ok - an OOM here is nonfatal
                pTracker = new MetaDataTracker(baseAddress, mdSize, modName);
            }
            pTracker->Activate();
        }

        RETURN pTracker;
    }

    // Map a metadata address to a token for the purposes of the IBCLogger
    static mdToken MapAddrToToken(const void *addr)
    {
        WRAPPER_NO_CONTRACT;

        mdToken token = 0;
        for (MetaDataTracker *mdMod = m_MDTrackers; mdMod; mdMod = mdMod->m_next)
        {
            token = mdMod->MapAddrToTokenInModule(addr);
            if (token != 0)
                break;
        }
        return token;
    }


private:
    
    // ***************************************************************************
    // Helper functions
    // ***************************************************************************

    BOOL NoteSectionInModule(DWORD secNum, void *address, size_t size, size_t rowSize)
    {
        WRAPPER_NO_CONTRACT;

        PREFAST_ASSUME(secNum < NUM_MD_SECTIONS);

        if (address < m_MetadataBase || address >= (m_MetadataBase + m_MetadataSize))
            return FALSE;

        // This address range belongs to us but the tracker is not activated. 
        if (!IsActivated())
        {
            // _ASSERTE (!"Metadata Tracker not active but trying to access metadata");
            return TRUE;
        }

        m_mdSections[secNum] = (BYTE *)address;
        m_mdSectionSize[secNum] = size;
        m_mdSectionRowSize[secNum] = rowSize;

        return TRUE;
    }

    // Map a metadata address to a fake token for the purposes of the IBCLogger
    mdToken MapAddrToTokenInModule(const void *addr)
    {
        LIMITED_METHOD_CONTRACT;

        if (!IsActivated())
            return 0;

        BYTE *address = (BYTE *)addr;

        if (address < m_MetadataBase || address >= (m_MetadataBase + m_MetadataSize))
            return 0;

        for (DWORD secNum = 0; secNum < NUM_MD_SECTIONS; secNum++)
        {
            if ((address >= m_mdSections[secNum]) && (address < m_mdSections[secNum] + m_mdSectionSize[secNum]))
            {
                DWORD rid = (DWORD)((address - m_mdSections[secNum])/m_mdSectionRowSize[secNum]);
                if (secNum < TBL_COUNT)
                    rid++;
                return TokenFromRid(rid, (secNum<<24));
            }
        }
        return 0;
    }
};

#else // METADATATRACKER_ENABLED

#define METADATATRACKER_ONLY(s)

#endif // METADATATRACKER_ENABLED

#endif // _METADATATRACKER_H_
