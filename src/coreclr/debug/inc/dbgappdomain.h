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
                        0, FALSE, DUPLICATE_SAME_ACCESS);
    }

    void CloseInRemoteProcess(HANDLE hProcess)
    {
        HANDLE hHandle = m_hLocal;
        m_hLocal = NULL;

        HANDLE hTmp;
        if (DuplicateHandle(hProcess, hHandle, GetCurrentProcess(), &hTmp,
                0, FALSE, DUPLICATE_SAME_ACCESS | DUPLICATE_CLOSE_SOURCE))
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

#endif //DbgAppDomain_H



