// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef DbgAppDomain_H
#define DbgAppDomain_H

// Forward declaration
class AppDomain;


// AppDomainInfo contains information about an AppDomain
// All pointers are for the left side, and we do not own any of the memory
struct AppDomainInfo
{
    DWORD       m_id = 0; // UNUSED, only present to maintain the shape of this structure
    int         m_iNameLengthInBytes;
    LPCWSTR     m_szAppDomainName;
    AppDomain  *m_pAppDomain; // only used by LS

    // NOTE: These functions are just helpers and must not add a VTable
    // to this struct (since we need to read this out-of-proc)

    // Provide a clean definition of an empty entry
    inline bool IsEmpty() const
    {
        return m_szAppDomainName == NULL;
    }

#ifndef RIGHT_SIDE_COMPILE
    // Mark this entry as empty.
    inline void FreeEntry()
    {
        m_szAppDomainName = NULL;
    }

    // Set the string name and length.
    // If szName is null, it is adjusted to a global constant.
    // This also causes the entry to be considered valid
    inline void SetName(LPCWSTR szName)
    {
        if (szName != NULL)
            m_szAppDomainName = szName;
        else
            m_szAppDomainName = W("<NoName>");

        m_iNameLengthInBytes = (int) (u16_strlen(m_szAppDomainName) + 1) * sizeof(WCHAR);
    }
#endif
};

// Enforce the AppDomain IPC block binary layout doesn't change between versions.
// Only an issue for x86 since that's the only platform w/ multiple versions.
#if defined(TARGET_X86)
static_assert_no_msg(offsetof(AppDomainInfo, m_id) == 0x0);
static_assert_no_msg(offsetof(AppDomainInfo, m_iNameLengthInBytes) == 0x4);
static_assert_no_msg(offsetof(AppDomainInfo, m_szAppDomainName) == 0x8);
static_assert_no_msg(offsetof(AppDomainInfo, m_pAppDomain) == 0xc);
#endif



// The RemoteHANDLE encapsulates the PAL specific handling of handles to avoid PAL specific ifdefs
// everywhere else in the code.
// There are two common initialization patterns:
//
// 1. Publishing of local handle for other processes, the value of the wrapper is a local handle
//    in *this* process at the end:
//    - In this process, call SetLocal(hHandle) to initialize the handle.
//    - In the other processes, call DuplicateToLocalProcess to get a local copy of the handle.
//
// 2. Injecting of local handle into other process, the value of the wrapper is a local handle
//    in the *other* process at the end:
//    - In this process, call DuplicateToRemoteProcess(hProcess, hHandle) to initialize the handle.
//    - In the other process, call ImportToOtherProcess() to finish the initialization of the wrapper
//      with a local copy of the handle.
//
// Once initialized, the wrapper can be used the same way as a regular HANDLE in the process
// it was initialized for. There is casting operator HANDLE to achieve that.

struct RemoteHANDLE {
    HANDLE              m_hLocal;

    operator HANDLE& ()
    {
        return m_hLocal;
    }

    void Close()
    {
        HANDLE hHandle = m_hLocal;
        if (hHandle != NULL) {
            m_hLocal = NULL;
            CloseHandle(hHandle);
        }
    }

    // Sets the local value of the handle. DuplicateToLocalProcess can be used later
    // by the remote process to acquire the remote handle.
    BOOL SetLocal(HANDLE hHandle)
    {
        m_hLocal = hHandle;
        return TRUE;
    }

    // Duplicates the current handle value to remote process. ImportToLocalProcess
    // should be called in the remote process before the handle is used in the remote process.
    // NOTE: right now this is used for duplicating the debugger's process handle into the LS so
    //       that the LS can know when the RS has exited; thus we are only specifying SYNCHRONIZE
    //       access to mitigate any security concerns.
    BOOL DuplicateToRemoteProcess(HANDLE hProcess, HANDLE hHandle)
    {
        return DuplicateHandle(GetCurrentProcess(), hHandle, hProcess, &m_hLocal,
                        SYNCHRONIZE, FALSE, 0);
    }

    // Duplicates the current handle value to local process. To be used in combination with SetLocal.
    BOOL DuplicateToLocalProcess(HANDLE hProcess, HANDLE* pHandle)
    {
        return DuplicateHandle(hProcess, m_hLocal, GetCurrentProcess(), pHandle,
                        NULL, FALSE, DUPLICATE_SAME_ACCESS);
    }

    void CloseInRemoteProcess(HANDLE hProcess)
    {
        HANDLE hHandle = m_hLocal;
        m_hLocal = NULL;

        HANDLE hTmp;
        if (DuplicateHandle(hProcess, hHandle, GetCurrentProcess(), &hTmp,
                NULL, FALSE, DUPLICATE_SAME_ACCESS | DUPLICATE_CLOSE_SOURCE))
        {
            CloseHandle(hTmp);
        }
    }

    // Imports the handle to local process. To be used in combination with DuplicateToRemoteProcess.
    HANDLE ImportToLocalProcess()
    {
        return m_hLocal;
    }
};


// AppDomain publishing server support:
// Information about all appdomains in the process will be maintained
// in the shared memory block for use by the debugger, etc.
// This structure defines the layout of the information that will
// be maintained.
struct AppDomainEnumerationIPCBlock
{
    // !!! The binary format of this layout must remain the same across versions so that
    // !!! a V2.0 publisher can inspect a v1.0 app.

    // lock for serialization while manipulating AppDomain list.
    RemoteHANDLE        m_hMutex;

    // Number of slots in AppDomainListElement array
    int                 m_iTotalSlots;
    int                 m_iNumOfUsedSlots;
    int                 m_iLastFreedSlot;
    int                 m_iSizeInBytes; // Size of AppDomainInfo in bytes

    // We can use psapi!GetModuleFileNameEx to get the module name.
    // This provides an alternative.
    int                 m_iProcessNameLengthInBytes;
    WCHAR              *m_szProcessName;

    AppDomainInfo      *m_rgListOfAppDomains;
    BOOL                m_fLockInvalid;


#ifndef RIGHT_SIDE_COMPILE
    /*************************************************************************
     * Locks the list
     *************************************************************************/
    BOOL Lock()
    {
        DWORD dwRes = WaitForSingleObject(m_hMutex, 3000);
        if (dwRes == WAIT_TIMEOUT)
        {
            // Nobody should get stuck holding this lock.
            // If we timeout on the wait, then either:
            // - it's a really bad race and somebody got preempted for a long time
            // - perhaps somebody's doing a DOS attack and holding onto the mutex.
            m_fLockInvalid = TRUE;
        }


        // The only time this can happen is if we're in shutdown and a thread
        // that held this lock is killed.  If this happens, assume that this
        // IPC block is in an invalid state and return FALSE to indicate
        // that people shouldn't do anything with the block anymore.
        if (dwRes == WAIT_ABANDONED)
        {
            m_fLockInvalid = TRUE;
        }

        if (m_fLockInvalid)
        {
            Unlock();
        }

        return (dwRes == WAIT_OBJECT_0 && !m_fLockInvalid);
    }

    /*************************************************************************
     * Unlocks the list
     *************************************************************************/
    void Unlock()
    {
        // Lock may or may not be valid at this point. Thus Release may fail,
        // but we'll just ignore that.
        ReleaseMutex(m_hMutex);
    }

    /*************************************************************************
     * Gets a free AppDomainInfo entry, and will allocate room if there are
     * no free slots left.
     *************************************************************************/
    AppDomainInfo *GetFreeEntry()
    {
        // first check to see if there is space available. If not, then realloc.
        if (m_iNumOfUsedSlots == m_iTotalSlots)
        {
            // need to realloc
            AppDomainInfo *pTemp =
                new (nothrow) AppDomainInfo [m_iTotalSlots*2];

            if (pTemp == NULL)
            {
                return (NULL);
            }

            memcpy (pTemp, m_rgListOfAppDomains, m_iSizeInBytes);

            delete [] m_rgListOfAppDomains;

            // Initialize the increased portion of the realloced memory
            int iNewSlotSize = m_iTotalSlots * 2;

            for (int iIndex = m_iTotalSlots; iIndex < iNewSlotSize; iIndex++)
                pTemp[iIndex].FreeEntry();

            m_rgListOfAppDomains = pTemp;
            m_iTotalSlots = iNewSlotSize;
            m_iSizeInBytes *= 2;
        }

        // Walk the list looking for an empty slot. Start from the last
        // one which was freed.
        {
            int i = m_iLastFreedSlot;

            do
            {
                // Pointer to the entry being examined
                AppDomainInfo *pADInfo = &(m_rgListOfAppDomains[i]);

                // is the slot available?
                if (pADInfo->IsEmpty())
                    return (pADInfo);

                i = (i + 1) % m_iTotalSlots;

            } while (i != m_iLastFreedSlot);
        }

        _ASSERTE(!"ADInfo::GetFreeEntry: should never get here.");
        return (NULL);
    }

    /*************************************************************************
     * Returns an AppDomainInfo slot to the free list.
     *************************************************************************/
    void FreeEntry(AppDomainInfo *pADInfo)
    {
        _ASSERTE(pADInfo >= m_rgListOfAppDomains &&
                 pADInfo < m_rgListOfAppDomains + m_iSizeInBytes);
        _ASSERTE(((size_t)pADInfo - (size_t)m_rgListOfAppDomains) %
            sizeof(AppDomainInfo) == 0);

        // Mark this slot as free
        pADInfo->FreeEntry();

#ifdef _DEBUG
        *pADInfo = {};
#endif

        // decrement the used slot count
        m_iNumOfUsedSlots--;

        // Save the last freed slot.
        m_iLastFreedSlot = (int)((size_t)pADInfo - (size_t)m_rgListOfAppDomains) /
            sizeof(AppDomainInfo);
    }

    /*************************************************************************
     * Finds an AppDomainInfo entry corresponding to the AppDomain pointer.
     * Returns NULL if no such entry exists.
     *************************************************************************/
    AppDomainInfo *FindEntry(AppDomain *pAD)
    {
        // Walk the list looking for a matching entry
        for (int i = 0; i < m_iTotalSlots; i++)
        {
            AppDomainInfo *pADInfo = &(m_rgListOfAppDomains[i]);

            if (!pADInfo->IsEmpty() &&
                pADInfo->m_pAppDomain == pAD)
                return pADInfo;
        }

        return (NULL);
    }

    /*************************************************************************
     * Returns the first AppDomainInfo entry in the list.  Returns NULL if
     * no such entry exists.
     *************************************************************************/
    AppDomainInfo *FindFirst()
    {
        // Walk the list looking for a non-empty entry
        for (int i = 0; i < m_iTotalSlots; i++)
        {
            AppDomainInfo *pADInfo = &(m_rgListOfAppDomains[i]);

            if (!pADInfo->IsEmpty())
                return pADInfo;
        }

        return (NULL);
    }

    /*************************************************************************
     * Returns the next AppDomainInfo entry after pADInfo.  Returns NULL if
     * pADInfo was the last in the list.
     *************************************************************************/
    AppDomainInfo *FindNext(AppDomainInfo *pADInfo)
    {
        _ASSERTE(pADInfo >= m_rgListOfAppDomains &&
                 pADInfo < m_rgListOfAppDomains + m_iSizeInBytes);
        _ASSERTE(((size_t)pADInfo - (size_t)m_rgListOfAppDomains) %
            sizeof(AppDomainInfo) == 0);

        // Walk the list looking for the next non-empty entry
        for (int i = (int)((size_t)pADInfo - (size_t)m_rgListOfAppDomains)
                                                / sizeof(AppDomainInfo) + 1;
             i < m_iTotalSlots;
             i++)
        {
            AppDomainInfo *pADInfoTemp = &(m_rgListOfAppDomains[i]);

            if (!pADInfoTemp->IsEmpty())
                return pADInfoTemp;
        }

        return (NULL);
    }
#endif // RIGHT_SIDE_COMPILE
};

// Enforce the AppDomain IPC block binary layout doesn't change between versions.
// Only an issue for x86 since that's the only platform w/ multiple versions.
#if defined(TARGET_X86)
static_assert_no_msg(offsetof(AppDomainEnumerationIPCBlock, m_hMutex) == 0x0);
static_assert_no_msg(offsetof(AppDomainEnumerationIPCBlock, m_iTotalSlots) == 0x4);
static_assert_no_msg(offsetof(AppDomainEnumerationIPCBlock, m_iNumOfUsedSlots) == 0x8);
static_assert_no_msg(offsetof(AppDomainEnumerationIPCBlock, m_iLastFreedSlot) == 0xc);
static_assert_no_msg(offsetof(AppDomainEnumerationIPCBlock, m_iSizeInBytes) == 0x10);
static_assert_no_msg(offsetof(AppDomainEnumerationIPCBlock, m_iProcessNameLengthInBytes) == 0x14);
static_assert_no_msg(offsetof(AppDomainEnumerationIPCBlock, m_szProcessName) == 0x18);
static_assert_no_msg(offsetof(AppDomainEnumerationIPCBlock, m_rgListOfAppDomains) == 0x1c);
static_assert_no_msg(offsetof(AppDomainEnumerationIPCBlock, m_fLockInvalid) == 0x20);
#endif

#endif //DbgAppDomain_H



