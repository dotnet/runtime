// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
//
 
//
// ==--==
//*****************************************************************************
// File: IPCReaderImpl.cpp
//
// Read a COM+ memory mapped file
//
//*****************************************************************************

#include "stdafx.h"

#include "ipcmanagerinterface.h"
#include "ipcheader.h"
#include "ipcshared.h"
#include <safewrap.h>

#include <securitywrapper.h>

#if defined(FEATURE_IPCMAN)
//-----------------------------------------------------------------------------
// Ctor sets members
//-----------------------------------------------------------------------------
IPCReaderImpl::IPCReaderImpl()
{
    LIMITED_METHOD_CONTRACT;
#ifdef _DEBUG
    m_fInitialized = FALSE;
#endif
    m_fIsTarget32Bit = FALSE;
    m_handleLegacyPrivateBlock = NULL;
    m_ptrLegacyPrivateBlock = NULL;
    m_handleLegacyPublicBlock = NULL;
    m_ptrLegacyPublicBlock = NULL;
    m_handleBlockTable = NULL;
    m_pBlockTable = NULL;
    m_handleBoundaryDesc = NULL;
    m_handlePrivateNamespace = NULL;
    m_pSID = NULL;
}

//-----------------------------------------------------------------------------
// dtor
//-----------------------------------------------------------------------------
IPCReaderImpl::~IPCReaderImpl()
{
    LIMITED_METHOD_CONTRACT;

    LOG((LF_CORDB, LL_INFO10, "IPCRI::IPCReaderImpl::~IPCReaderImpl 0x%08x (LegacyPrivate)\n", m_handleLegacyPrivateBlock));
    LOG((LF_CORDB, LL_INFO10, "IPCRI::IPCReaderImpl::~IPCReaderImpl 0x%08x (LegacyPublic)\n", m_handleLegacyPublicBlock));
    LOG((LF_CORDB, LL_INFO10, "IPCRI::IPCReaderImpl::~IPCReaderImpl 0x%08x (SxSPublic)\n", m_handleBlockTable));

    _ASSERTE(m_handleLegacyPrivateBlock == NULL);
    _ASSERTE(m_ptrLegacyPrivateBlock == NULL);

    _ASSERTE(m_handleLegacyPublicBlock == NULL);
    _ASSERTE(m_ptrLegacyPublicBlock == NULL);

    _ASSERTE(m_handleBlockTable == NULL);
    _ASSERTE(m_pBlockTable == NULL);
    _ASSERTE(m_handleBoundaryDesc == NULL);
    _ASSERTE(m_pSID == NULL);
    _ASSERTE(m_handlePrivateNamespace == NULL);
    

}

//-----------------------------------------------------------------------------
// dtor
//-----------------------------------------------------------------------------
IPCReaderInterface::~IPCReaderInterface()
{
    LIMITED_METHOD_CONTRACT;

    LOG((LF_CORDB, LL_INFO10, "IPCRI::IPCReaderInterface::~IPCReaderInterface 0x%08x (BlockTable)\n", m_handleBlockTable));
    LOG((LF_CORDB, LL_INFO10, "IPCRI::IPCReaderInterface::~IPCReaderInterface 0x%08x (LegacyPrivate)\n", m_handleLegacyPrivateBlock));
    LOG((LF_CORDB, LL_INFO10, "IPCRI::IPCReaderInterface::~IPCReaderInterface 0x%08x (LegacyPublic)\n", m_handleLegacyPublicBlock));
    
    if (m_handleLegacyPrivateBlock)
    {
        CloseLegacyPrivateBlock();
    }
    _ASSERTE(m_handleLegacyPrivateBlock == NULL);
    _ASSERTE(m_ptrLegacyPrivateBlock == NULL);

#ifndef DACCESS_COMPILE
    if (m_handleLegacyPublicBlock)
    {
        CloseLegacyPublicBlock();
    }
    _ASSERTE(m_handleLegacyPublicBlock == NULL);
    _ASSERTE(m_ptrLegacyPublicBlock == NULL);

    if (m_handleBlockTable)
    {
        CloseBlockTable();
    }
    _ASSERTE(m_handleBlockTable == NULL);
    _ASSERTE(m_pBlockTable == NULL);
#endif
}

//-----------------------------------------------------------------------------
// Close whatever block we opened
//-----------------------------------------------------------------------------
void IPCReaderInterface::CloseLegacyPrivateBlock()
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_CORDB, LL_INFO10, "IPCRI::CloseLegacyPrivateBlock 0x%08x\n", m_handleLegacyPrivateBlock));
    
    IPCShared::CloseMemoryMappedFile(
        m_handleLegacyPrivateBlock,
        (void * &) m_ptrLegacyPrivateBlock
    );
    _ASSERTE(m_handleLegacyPrivateBlock == NULL);
    _ASSERTE(m_ptrLegacyPrivateBlock == NULL);
}

#ifndef DACCESS_COMPILE

//-----------------------------------------------------------------------------
// Close whatever block we opened
//-----------------------------------------------------------------------------
void IPCReaderInterface::CloseLegacyPublicBlock()
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_CORDB, LL_INFO10, "IPCRI::CloseLegacyPublicBlock 0x%08x\n", m_handleLegacyPublicBlock));
    
    IPCShared::CloseMemoryMappedFile(
        m_handleLegacyPublicBlock,
        (void * &) m_ptrLegacyPublicBlock
    );
    _ASSERTE(m_handleLegacyPublicBlock == NULL);
    _ASSERTE(m_ptrLegacyPublicBlock == NULL);
}

//-----------------------------------------------------------------------------
// Close whatever block we opened
//-----------------------------------------------------------------------------
void IPCReaderInterface::CloseBlockTable()
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_CORDB, LL_INFO10, "IPCRI::CloseBlockTable 0x%08x\n", m_handleBlockTable));
    
    IPCShared::CloseMemoryMappedFile(
        m_handleBlockTable,
        (void * &) m_pBlockTable
    );
    _ASSERTE(m_handleBlockTable == NULL);
    _ASSERTE(m_pBlockTable == NULL);
}

#endif

//-----------------------------------------------------------------------------
// Check to see if Debugger and Debuggee are on compatible platform
// Currently, we only consider incompatible if one process is on WOW64 box and the other is not.
// If so, return false. 
// For all other cases, return true for now. 
//-----------------------------------------------------------------------------
HRESULT IPCReaderInterface::IsCompatablePlatformForDebuggerAndDebuggee(
    DWORD  pid,
    BOOL * pfCompatible)
{
    if (pfCompatible == NULL)
        return E_INVALIDARG;

    // assume compatible unless otherwise
    *pfCompatible = TRUE;

    // assume that the target has the same bitness as 
    // this process unless otherwise
#ifdef _TARGET_X86_
    m_fIsTarget32Bit = TRUE;
#else
    m_fIsTarget32Bit = FALSE;
#endif
#ifdef _DEBUG
    m_fInitialized = TRUE;
#endif

    BOOL fThisProcessIsWow64 = FALSE;
    BOOL fSuccess = FALSE;            
    HANDLE hThisProcess = GetCurrentProcess();
    fSuccess = IsWow64Process(hThisProcess, &fThisProcessIsWow64);
    CloseHandle(hThisProcess);
    hThisProcess = NULL;

    if (!fSuccess)
        return HRESULT_FROM_GetLastError();
    
    HANDLE hTargetProcess = OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, pid);
    if (hTargetProcess == NULL)
        return HRESULT_FROM_GetLastError();

    BOOL fTargetProcessIsWow64 = FALSE;
    fSuccess = IsWow64Process(hTargetProcess, &fTargetProcessIsWow64);
    CloseHandle(hTargetProcess);
    hTargetProcess = NULL;

    if (!fSuccess)
        return HRESULT_FROM_GetLastError();

    // We don't want to expose the IPC block if one process is x86 and
    // the other is ia64 or amd64
    if (fTargetProcessIsWow64 != fThisProcessIsWow64)
    {
        *pfCompatible = FALSE;
        m_fIsTarget32Bit = !m_fIsTarget32Bit;
    }

    return S_OK;
}


void IPCReaderInterface::MakeInstanceName(const WCHAR * szProcessName, DWORD pid, DWORD runtimeId, SString & sName)
{
    WRAPPER_NO_CONTRACT;

    const WCHAR * szFormat = CorSxSPublicInstanceName;

    sName.Printf(szFormat, szProcessName, pid, runtimeId);
}


void IPCReaderInterface::MakeInstanceNameWhidbey(const WCHAR * szProcessName, DWORD pid, SString & sName)
{
    WRAPPER_NO_CONTRACT;

    const WCHAR * szFormat = CorSxSPublicInstanceNameWhidbey;

    sName.Printf(szFormat, szProcessName, pid);
}


//-----------------------------------------------------------------------------
// Open our LegacyPrivate block
//-----------------------------------------------------------------------------
HRESULT IPCReaderInterface::OpenLegacyPrivateBlockOnPid(DWORD pid, DWORD dwDesiredAccess)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#if 0
// Note, PID != GetCurrentProcessId(), b/c we're expected to be opening
// someone else's IPCBlock, not our own. If this isn't the case, just remove
// this assert

// exception: if we're enumerating provesses, we'll hit our own
//  _ASSERTE(pid != GetCurrentProcessId());
#endif

    // Note: if our LegacyPrivate block is open, we shouldn't be attaching to a new one.
    _ASSERTE(!IsLegacyPrivateBlockOpen());
    if (IsLegacyPrivateBlockOpen())
    {
        // if we goto errExit, it will close the file. We don't want that.
        return HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);
    }

    HRESULT hr = S_OK;

    EX_TRY
    {
        // We should not be trying to open the IPC block of an x86 process from
        // within an ia64 process, or vice versa. This can only happen on
        // Server2003 and later.

        BOOL fCompatible = FALSE;
        hr = IsCompatablePlatformForDebuggerAndDebuggee(pid, &fCompatible);
        if (FAILED(hr))
        {
            goto end;
        }
        if (fCompatible == FALSE)
        {
            hr = CORDBG_E_UNCOMPATIBLE_PLATFORMS;
            goto end;
        }

        // In order to verify handle's owner, we need READ_CONTROL.
        dwDesiredAccess |= READ_CONTROL;

        {
            SString szMemFileName;
            IPCShared::GenerateName(pid, szMemFileName);
            m_handleLegacyPrivateBlock = WszOpenFileMapping(dwDesiredAccess,
                                                      FALSE,
                                                      szMemFileName);
            if (m_handleLegacyPrivateBlock == NULL)
            {
                hr = HRESULT_FROM_GetLastError();
            }

            LOG((LF_CORDB, LL_INFO10, "IPCRI::OPBOP: CreateFileMapping of %S, handle = 0x%08x, pid = 0x%8.8x GetLastError=%d\n",
                szMemFileName.GetUnicode(), m_handleLegacyPrivateBlock, pid, GetLastError()));
            if (m_handleLegacyPrivateBlock == NULL)
            {
                goto end;
            }
        }

        // Verify that the owner of the handle is the same as the user of that pid.
        // This protects us against a 3rd user pre-creating the IPC block underneath us
        // and tricking us into attaching to that.
        // Even if a 3rd-party is able to spoof the IPC block, they themselves won't
        // have access to it and so the IPC block will remain all zeros.
        // That radically limits potential attacks that a 3rd-party could do.
        //        
        if (IsHandleSpoofed(m_handleLegacyPrivateBlock, pid))
        {
            hr = E_ACCESSDENIED;
            goto end;
        } 

        m_ptrLegacyPrivateBlock = (LegacyPrivateIPCControlBlock*) MapViewOfFile(
            m_handleLegacyPrivateBlock,
            dwDesiredAccess,
            0, 0, 0);

        if (m_ptrLegacyPrivateBlock== NULL)
        {
            hr = HRESULT_FROM_GetLastError();
            goto end;
        }

        // Check if LegacyPrivate block is valid; if it is not valid,
        // report the block as "not compatible"
        if (!IsValidLegacy(FALSE))
            hr = CORDBG_E_UNCOMPATIBLE_PLATFORMS;
        
        end:;
    }
    EX_CATCH
    {
        Exception *e = GET_EXCEPTION();
        hr = e->GetHR();
        if (hr == S_OK) 
        {
            hr = E_FAIL;
        }
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (!SUCCEEDED(hr))
    {
        CloseLegacyPrivateBlock();
    }

    return hr;
}

//-----------------------------------------------------------------------------
// Open our LegacyPrivate block
//-----------------------------------------------------------------------------
HRESULT IPCReaderInterface::OpenLegacyPrivateBlockTempV4OnPid(DWORD pid, DWORD dwDesiredAccess)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#if 0
// Note, PID != GetCurrentProcessId(), b/c we're expected to be opening
// someone else's IPCBlock, not our own. If this isn't the case, just remove
// this assert

// exception: if we're enumerating provesses, we'll hit our own
//  _ASSERTE(pid != GetCurrentProcessId());
#endif

    // Note: if our LegacyPrivate block is open, we shouldn't be attaching to a new one.
    _ASSERTE(!IsLegacyPrivateBlockOpen());
    if (IsLegacyPrivateBlockOpen())
    {
        // if we goto errExit, it will close the file. We don't want that.
        return HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);
    }

    HRESULT hr = S_OK;

    EX_TRY
    {
        // We should not be trying to open the IPC block of an x86 process from
        // within an ia64 process, or vice versa. This can only happen on
        // Server2003 and later.

        BOOL fCompatible = FALSE;
        hr = IsCompatablePlatformForDebuggerAndDebuggee(pid, &fCompatible);
        if (FAILED(hr))
        {
            goto end;
        }
        if (fCompatible == FALSE)
        {
            hr = CORDBG_E_UNCOMPATIBLE_PLATFORMS;
            goto end;
        }

        // In order to verify handle's owner, we need READ_CONTROL.
        dwDesiredAccess |= READ_CONTROL;

        {
            SString szMemFileName;
            IPCShared::GenerateNameLegacyTempV4(pid, szMemFileName);
            m_handleLegacyPrivateBlock = WszOpenFileMapping(dwDesiredAccess,
                                                      FALSE,
                                                      szMemFileName);
            if (m_handleLegacyPrivateBlock == NULL)
            {
                hr = HRESULT_FROM_GetLastError();
            }

            LOG((LF_CORDB, LL_INFO10, "IPCRI::OLPBTV4OP: CreateFileMapping of %S, handle = 0x%08x, pid = 0x%8.8x GetLastError=%d\n",
                szMemFileName.GetUnicode(), m_handleLegacyPrivateBlock, pid, GetLastError()));
            if (m_handleLegacyPrivateBlock == NULL)
            {
                goto end;
            }
        }

        // Verify that the owner of the handle is the same as the user of that pid.
        // This protects us against a 3rd user pre-creating the IPC block underneath us
        // and tricking us into attaching to that.
        // Even if a 3rd-party is able to spoof the IPC block, they themselves won't
        // have access to it and so the IPC block will remain all zeros.
        // That radically limits potential attacks that a 3rd-party could do.
        //        
        if (IsHandleSpoofed(m_handleLegacyPrivateBlock, pid))
        {
            hr = E_ACCESSDENIED;
            goto end;
        } 

        m_ptrLegacyPrivateBlock = (LegacyPrivateIPCControlBlock*) MapViewOfFile(
            m_handleLegacyPrivateBlock,
            dwDesiredAccess,
            0, 0, 0);

        if (m_ptrLegacyPrivateBlock== NULL)
        {
            hr = HRESULT_FROM_GetLastError();
            goto end;
        }

        // Check if LegacyPrivate block is valid; if it is not valid,
        // report the block as "not compatible"
        if (!IsValidLegacy(FALSE))
            hr = CORDBG_E_UNCOMPATIBLE_PLATFORMS;
        
        end:;
    }
    EX_CATCH
    {
        Exception *e = GET_EXCEPTION();
        hr = e->GetHR();
        if (hr == S_OK) 
        {
            hr = E_FAIL;
        }
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (!SUCCEEDED(hr))
    {
        CloseLegacyPrivateBlock();
    }

    return hr;
}

#ifndef DACCESS_COMPILE

//-----------------------------------------------------------------------------
// Open our LegacyPublic block
//-----------------------------------------------------------------------------
HRESULT IPCReaderInterface::OpenLegacyPublicBlockOnPid(DWORD pid, DWORD dwDesiredAccess)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Note: if our LegacyPublic block is open, we shouldn't be attaching to a new one.
    _ASSERTE(!IsLegacyPublicBlockOpen());
    if (IsLegacyPublicBlockOpen())
    {
        // if we goto errExit, it will close the file. We don't want that.
        return HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);
    }

    HRESULT hr = S_OK;
    
    EX_TRY
    {
        {
            SString szMemFileName;
            IPCShared::GenerateLegacyPublicName(pid, szMemFileName);
            m_handleLegacyPublicBlock = WszOpenFileMapping(dwDesiredAccess,
                                                    FALSE,
                                                    szMemFileName);
            if (m_handleLegacyPublicBlock == NULL)
            {
                hr = HRESULT_FROM_GetLastError();
            }

            LOG((LF_CORDB, LL_INFO10, "IPCRI::OPBOP: CreateFileMapping of %S, handle = 0x%08x, pid = 0x%8.8x GetLastError=%d\n",
                szMemFileName.GetUnicode(), m_handleLegacyPublicBlock, pid, GetLastError()));
            if (m_handleLegacyPublicBlock == NULL)
            {
                goto end;
            }
        }

        m_ptrLegacyPublicBlock = (LegacyPublicIPCControlBlock*) MapViewOfFile(
            m_handleLegacyPublicBlock,
            dwDesiredAccess,
            0, 0, 0);
        if (m_ptrLegacyPublicBlock == NULL)
        {
            hr = HRESULT_FROM_GetLastError();
            goto end;
        }      

        // Check if the target is valid and compatible
        if (!IsValidLegacy(TRUE))
        {
            hr = CORDBG_E_UNCOMPATIBLE_PLATFORMS;
            goto end;
        }

        end:;
    }
    EX_CATCH
    {
        Exception *e = GET_EXCEPTION();
        hr = e->GetHR();
        if (hr == S_OK) 
        {
            hr = E_FAIL;
        }
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (!SUCCEEDED(hr))
    {
        if (m_handleLegacyPrivateBlock != NULL)
            CloseLegacyPrivateBlock();

        if (m_handleLegacyPublicBlock != NULL)
            CloseLegacyPublicBlock();
    }

    return hr;
}





//-----------------------------------------------------------------------------
// Open our IPCBlockTable
//-----------------------------------------------------------------------------
HRESULT IPCReaderInterface::OpenBlockTableOnPid(DWORD pid, DWORD dwDesiredAccess)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Note: if our IPCBlockTable is open, we shouldn't be attaching to a new one.
    _ASSERTE(!IsBlockTableOpen());
    if (IsBlockTableOpen())
    {
        // if we goto errExit, it will close the file. We don't want that.
        return HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);
    }

    HRESULT hr = S_OK;
    
    EX_TRY
    {
        {
            SString szMemFileName;

            hr = IPCShared::GenerateBlockTableName(pid, szMemFileName, m_handleBoundaryDesc,m_handlePrivateNamespace, &m_pSID, FALSE);  
            if (FAILED(hr))
            {
                goto end;
            }

            m_handleBlockTable = WszOpenFileMapping(dwDesiredAccess,
                                                    FALSE,
                                                    szMemFileName);
            if (m_handleBlockTable == NULL)
            {
                hr = HRESULT_FROM_GetLastError();
            }

            LOG((LF_CORDB, LL_INFO10, "IPCRI::OPBOP: CreateFileMapping of %S, handle = 0x%08x, pid = 0x%8.8x GetLastError=%d\n",
                szMemFileName.GetUnicode(), m_handleBlockTable, pid, GetLastError()));
            if (m_handleBlockTable == NULL)
            {
                goto end;
            }
        }

        m_pBlockTable = (IPCControlBlockTable*) MapViewOfFile(
            m_handleBlockTable,
            dwDesiredAccess,
            0, 0, 0);

        if (m_pBlockTable == NULL)
        {
            hr = HRESULT_FROM_GetLastError();
            goto end;
        }
#if defined(_TARGET_X86_)
        //get the flags of the first available block.
        BOOL isInitialized = FALSE;
        for (DWORD i = 0; i < IPC_NUM_BLOCKS_IN_TABLE; ++i) {
            USHORT flags = m_pBlockTable->GetBlock(i)->m_Header.m_Flags;
            if ((flags & IPC_FLAG_INITIALIZED) == 0) {
               continue;
            }
            m_fIsTarget32Bit = ((flags & IPC_FLAG_X86) != 0);
            isInitialized = TRUE;
            // If this process is 32 bit and the target is 64 bit,
            // then the target is incompatible
            if (!m_fIsTarget32Bit){
                hr = E_FAIL;
                goto end;
            }
            break;
        }
        if (!isInitialized) {
            //none of the blocks are initialized
            hr = E_FAIL;
            goto end;
        }
#endif // defined(_TARGET_X86_)

        end:;
    }
    EX_CATCH
    {
        Exception *e = GET_EXCEPTION();
        hr = e->GetHR();
        if (hr == S_OK) 
        {
            hr = E_FAIL;
        }
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (!SUCCEEDED(hr))
    {
        if (m_handleBlockTable != NULL)
            CloseBlockTable();
    }
    if(!SUCCEEDED(IPCShared::FreeHandles(m_handleBoundaryDesc,m_pSID,m_handlePrivateNamespace)))
    {
        hr = E_FAIL;
    }
    return hr;
}

HRESULT IPCReaderInterface::OpenBlockTableOnPid(DWORD pid)
{
    WRAPPER_NO_CONTRACT;

    return (OpenBlockTableOnPid(pid, FILE_MAP_ALL_ACCESS));
}

HRESULT IPCReaderInterface::OpenBlockTableOnPidReadOnly(DWORD pid)
{
    WRAPPER_NO_CONTRACT;

    return (OpenBlockTableOnPid(pid, FILE_MAP_READ));
}

#endif


//-----------------------------------------------------------------------------
// Open our LegacyPrivate block for all access
//-----------------------------------------------------------------------------
HRESULT IPCReaderInterface::OpenLegacyPrivateBlockOnPid(DWORD pid)
{
    WRAPPER_NO_CONTRACT;

    return (OpenLegacyPrivateBlockOnPid(pid, FILE_MAP_ALL_ACCESS));
}

//-----------------------------------------------------------------------------
// Open our LegacyPrivate block for all access
//-----------------------------------------------------------------------------
HRESULT IPCReaderInterface::OpenLegacyPrivateBlockTempV4OnPid(DWORD pid)
{
    WRAPPER_NO_CONTRACT;

    return (OpenLegacyPrivateBlockTempV4OnPid(pid, FILE_MAP_ALL_ACCESS));
}

#ifndef DACCESS_COMPILE

//-----------------------------------------------------------------------------
// Open our LegacyPublic block for all access
//-----------------------------------------------------------------------------
HRESULT IPCReaderInterface::OpenLegacyPublicBlockOnPid(DWORD pid)
{
    WRAPPER_NO_CONTRACT;

    return (OpenLegacyPublicBlockOnPid(pid, FILE_MAP_ALL_ACCESS));
}

#endif

//-----------------------------------------------------------------------------
// Open our LegacyPrivate block for read/write access
//-----------------------------------------------------------------------------
HRESULT IPCReaderInterface::OpenLegacyPrivateBlockOnPidReadWrite(DWORD pid)
{
    WRAPPER_NO_CONTRACT;

    return (OpenLegacyPrivateBlockOnPid(pid, FILE_MAP_READ | FILE_MAP_WRITE));
}

//-----------------------------------------------------------------------------
// Open our LegacyPrivate block for read only access
//-----------------------------------------------------------------------------
HRESULT IPCReaderInterface::OpenLegacyPrivateBlockOnPidReadOnly(DWORD pid)
{
    WRAPPER_NO_CONTRACT;

    return (OpenLegacyPrivateBlockOnPid(pid, FILE_MAP_READ));
}

#ifndef DACCESS_COMPILE

//-----------------------------------------------------------------------------
// Open our LegacyPublic block for read only access
//-----------------------------------------------------------------------------
HRESULT IPCReaderInterface::OpenLegacyPublicBlockOnPidReadOnly(DWORD pid)
{
    WRAPPER_NO_CONTRACT;

    return (OpenLegacyPublicBlockOnPid(pid, FILE_MAP_READ));
}

#endif

//-----------------------------------------------------------------------------
// Get a client's LegacyPrivate block based on enum
// This is a robust function.
// It will return NULL if:
//  * the IPC block is closed (also ASSERT),
//  * the eClient is out of range (From version mismatch)
//  * the request block is removed (probably version mismatch)
// Else it will return a pointer to the requested block
//-----------------------------------------------------------------------------
void * IPCReaderInterface::GetLegacyPrivateBlock(ELegacyPrivateIPCClient eClient)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(IsLegacyPrivateBlockOpen());

    // This block doesn't exist if we're closed or out of the table's range
    if (!IsLegacyPrivateBlockOpen() || (DWORD) eClient >= m_ptrLegacyPrivateBlock->m_FullIPCHeader.m_header.m_numEntries)
    {
        return NULL;
    }

    if (Internal_CheckEntryEmptyLegacyPrivate(*m_ptrLegacyPrivateBlock,eClient))
    {
        return NULL;
    }

    return Internal_GetBlockLegacyPrivate(*m_ptrLegacyPrivateBlock,eClient);
}

#ifndef DACCESS_COMPILE

//-----------------------------------------------------------------------------
// Get a client's LegacyPublic block based on enum
// This is a robust function.
// It will return NULL if:
//  * the IPC block is closed (also ASSERT),
//  * the eClient is out of range (From version mismatch)
//  * the requested block is removed (probably version mismatch)
// Else it will return a pointer to the requested block
//-----------------------------------------------------------------------------
void * IPCReaderInterface::GetLegacyPublicBlock(ELegacyPublicIPCClient eClient)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(IsLegacyPublicBlockOpen());

    // This block doesn't exist if we're closed or out of the table's range        
    if (!IsLegacyPublicBlockOpen())
        return NULL;

    DWORD dwNumEntries = GetNumEntriesLegacy(m_ptrLegacyPublicBlock);

    if ((DWORD) eClient >= dwNumEntries)
        return NULL;

    if (Internal_CheckEntryEmptyLegacyPublic(eClient))
        return NULL;

    return Internal_GetBlockLegacyPublic(eClient);
}

#endif

//-----------------------------------------------------------------------------
// Is our LegacyPrivate block open?
//-----------------------------------------------------------------------------
bool IPCReaderInterface::IsLegacyPrivateBlockOpen() const
{
    LIMITED_METHOD_CONTRACT;

    return m_ptrLegacyPrivateBlock != NULL;
}

#ifndef DACCESS_COMPILE

bool IPCReaderInterface::IsLegacyPublicBlockOpen() const
{
    LIMITED_METHOD_CONTRACT;

    return m_ptrLegacyPublicBlock != NULL;
}

bool IPCReaderInterface::IsBlockTableOpen() const
{
    LIMITED_METHOD_CONTRACT;

    return m_pBlockTable != NULL;
}

void * IPCReaderInterface::GetPerfBlockLegacyPublic()
{
    WRAPPER_NO_CONTRACT;

    return (PerfCounterIPCControlBlock*) GetLegacyPublicBlock(eLegacyPublicIPC_PerfCounters);
}

#endif

void * IPCReaderInterface::GetPerfBlockLegacyPrivate()
{
    WRAPPER_NO_CONTRACT;

    return (PerfCounterIPCControlBlock*) GetLegacyPrivateBlock(eLegacyPrivateIPC_PerfCounters);
}

AppDomainEnumerationIPCBlock * IPCReaderInterface::GetAppDomainBlock()
{
    WRAPPER_NO_CONTRACT;

    return (AppDomainEnumerationIPCBlock*) GetLegacyPrivateBlock(eLegacyPrivateIPC_AppDomain);
}

//-----------------------------------------------------------------------------
// Check if the block is valid. Current checks include:
// * Check Flags
// * Check Directory structure
// * Check Bitness (LegacyPublic block only)
//-----------------------------------------------------------------------------
BOOL IPCReaderInterface::IsValidLegacy(BOOL fIsLegacyPublicBlock)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Initialize the pBlock pointer to point to the specified block;
    // specified block must be open

    void * pBlock = (fIsLegacyPublicBlock ? (void*)m_ptrLegacyPublicBlock : (void*)m_ptrLegacyPrivateBlock);
    _ASSERTE(pBlock != NULL);

    // Check if block size has been initialized
    DWORD dwBlockSize = GetBlockSizeLegacy(pBlock);
    if (dwBlockSize == 0) 
        return FALSE;

    // If this IPC block uses the flags field and the initialized flag isn't set, 
    // then the block has not been initialized
    USHORT flags = GetFlagsLegacy(pBlock);
    BOOL fUsesFlags = (flags & IPC_FLAG_USES_FLAGS);
    if (fUsesFlags && (flags & IPC_FLAG_INITIALIZED) == 0)
        return FALSE;

    // If this is the LegacyPublic block, then we need to check bitness; if it 
    // turns out that the bitness is incompatible, return FALSE
    if (fIsLegacyPublicBlock)
    {
        // If this IPC block uses the flags field, then use the flags to
        // determine the bitness of the target block
        if (fUsesFlags)
        {
            m_fIsTarget32Bit = ((flags & IPC_FLAG_X86) != 0);
        }

        // Otherwise, this IPC block does not use the flags field
        else
        {
            // Use block size to determine the bitness of the target block
            m_fIsTarget32Bit = (dwBlockSize == LEGACYPUBLIC_IPC_BLOCK_SIZE_32);

            // If block size is not equal known values from used by older 
            // versions of the CLR, then assume this block is not compatible
            _ASSERTE(m_fIsTarget32Bit || dwBlockSize == LEGACYPUBLIC_IPC_BLOCK_SIZE_64);
        }

#if defined(_DEBUG)
        m_fInitialized = TRUE;
#endif //_DEBUG

#if defined(_TARGET_X86_)
        // If this process is 32 bit and the target is 64 bit,
        // then the target is incompatible
        if (!m_fIsTarget32Bit)
            return FALSE;   
#endif //_TARGET_X86_
    }

    // If this IPC block uses the flags field and this is not a 
    // debug build, then no further checks are necessary.
#if !defined(_DEBUG)
    if (fUsesFlags)
        return TRUE;
#endif //_DEBUG
  
    // Make sure numEntries has been initialized
    DWORD dwNumEntries = GetNumEntriesLegacy(pBlock);   
    if (dwNumEntries == 0)
    {
        // This assert will fail only if the IPC block uses flags and 
        // 'm_numEntries' has not been initialized
        _ASSERTE(!fUsesFlags && "m_numEntries is not initialized");
        return FALSE;
    }
    
    // Make sure that block size is not too small
    SIZE_T cbOffsetBase = (SIZE_T)GetOffsetBaseLegacy() + dwNumEntries * sizeof(IPCEntry);
    if (dwBlockSize < cbOffsetBase)
    {
        _ASSERTE(!"m_blockSize is too small or m_numEntries is too big (1)");
        return FALSE;
    }
    
    // Check to make sure that the expected offset for the end of
    // m_table does not go past the end of the block
    SIZE_T offsetExpected = GetFirstExpectedOffsetLegacy(); 
    SIZE_T offsetLast = dwBlockSize - cbOffsetBase;
    if (offsetExpected > offsetLast)
    {
        _ASSERTE(!"m_blockSize is too small or m_numEntries is too big (2)");
        return FALSE;
    }
    
    // Check each entries offset and size to make sure they are correct
    IPCEntry * table = GetDirectoryLegacy(pBlock);
    for(DWORD i = 0; i < dwNumEntries; ++i)
    {
        SIZE_T entryOffset = table[i].m_Offset;
        SIZE_T entrySize = table[i].m_Size;

        if (entryOffset == EMPTY_ENTRY_OFFSET)
        {
            // Verify that this entry has size of EMPTY_ENTRY_SIZE
            if (entrySize != EMPTY_ENTRY_SIZE)
            {
                _ASSERTE(!"Empty entry has size that does not equal EMPTY_ENTRY_SIZE");
                return FALSE;
            }
        }
        else
        {
            // Verify that this entry has non-zero size
            if (entrySize == 0)
            {
                // This assert will fail only if the IPC block uses flags and 
                // 'm_Size' has not been initialized
                _ASSERTE(!fUsesFlags && "m_Size is not initialized");
                return FALSE;
            }

            // Verify that the actual offset equals the expected offset
            if (entryOffset != offsetExpected)
            {
                if (entryOffset == 0)
                {
                    // This assert will only fail if the IPC block uses flags and
                    // 'm_Offset' has not been initialized
                    _ASSERTE(!fUsesFlags && "m_Offset is not initialized");
                }
                else
                {
                    // This assert will fail if 'm_Offset' has been initialized
                    // but does not equal the expected value
                    _ASSERTE(!"Actual offset does not equal to expected offset");
                }

                return FALSE;        
            }

            // Compute the next expected offset
            offsetExpected += entrySize;
        } 
    }

    // Verify that the end of the last entry is equal to the 
    // end of the IPC block
    if (offsetExpected != offsetLast)
    {
        _ASSERTE(!"End of last entry does not equal end of IPC block");
        return FALSE;        
    }

    return TRUE;
}

BOOL IPCReaderInterface::TryOpenBlock(IPCHeaderReadHelper & readHelper, DWORD blockIndex)
{
    _ASSERTE(blockIndex < IPC_NUM_BLOCKS_IN_TABLE);
    readHelper.CloseHeader();
    return readHelper.TryOpenHeader(&m_pBlockTable->m_blocks[blockIndex].m_Header);
}

USHORT IPCReaderInterface::GetBlockVersion()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(IsLegacyPrivateBlockOpen());
    return m_ptrLegacyPrivateBlock->m_FullIPCHeader.m_header.m_Version;
}

#ifndef DACCESS_COMPILE

USHORT IPCReaderInterface::GetLegacyPublicBlockVersion()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(IsLegacyPublicBlockOpen());
    return m_ptrLegacyPublicBlock->m_FullIPCHeaderLegacyPublic.m_header.m_Version;
}

#endif

HINSTANCE IPCReaderInterface::GetInstance()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(IsLegacyPrivateBlockOpen());
    return m_ptrLegacyPrivateBlock->m_FullIPCHeader.m_header.m_hInstance;
}

USHORT IPCReaderInterface::GetBuildYear()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(IsLegacyPrivateBlockOpen());
    return m_ptrLegacyPrivateBlock->m_FullIPCHeader.m_header.m_BuildYear;
}

USHORT IPCReaderInterface::GetBuildNumber()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(IsLegacyPrivateBlockOpen());
    return m_ptrLegacyPrivateBlock->m_FullIPCHeader.m_header.m_BuildNumber;
}

PVOID IPCReaderInterface::GetBlockStart()
{
    LIMITED_METHOD_CONTRACT;

    return (PVOID) m_ptrLegacyPrivateBlock;
}

PCWSTR IPCReaderInterface::GetInstancePath()
{
    WRAPPER_NO_CONTRACT;

    return (PCWSTR) GetLegacyPrivateBlock(eLegacyPrivateIPC_InstancePath);
}

#endif // FEATURE_IPCMAN
