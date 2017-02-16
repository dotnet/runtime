// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
//
 
//
// ==--==
//*****************************************************************************
// File: IPCWriterImpl.cpp
//
// Implementation for COM+ memory mapped file writing
//
//*****************************************************************************

#include "stdafx.h"

#include "ipcmanagerinterface.h"
#include "ipcheader.h"
#include "ipcshared.h"
#include "ipcmanagerimpl.h"

// Declared in threads.h, but including that file seems to cause problems
DWORD GetRuntimeId();

#include <sddl.h>

#if defined(TIA64)
#define IA64MemoryBarrier()        MemoryBarrier()
#else
#define IA64MemoryBarrier()
#endif

#if defined(FEATURE_IPCMAN)

const USHORT BuildYear = VER_ASSEMBLYMAJORVERSION;
const USHORT BuildNumber = VER_ASSEMBLYBUILD;

// Import from mscorwks.obj
HINSTANCE GetModuleInst();

#if defined(_DEBUG)
static void DumpSD(PSECURITY_DESCRIPTOR sd)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HINSTANCE  hDll = WszGetModuleHandle(L"advapi32");

    // Get the pointer to the requested function
    FARPROC pProcAddr = GetProcAddress(hDll, "ConvertSecurityDescriptorToStringSecurityDescriptorW");

    // If the proc address was not found, return error
    if (pProcAddr == NULL)
    {
        LOG((LF_CORDB, LL_INFO10,
             "IPCWI::DumpSD: GetProcAddr (ConvertSecurityDescriptorToStringSecurityDescriptorW) failed.\n"));
        goto ErrorExit;
    }

    typedef BOOL WINAPI SDTOSTR(PSECURITY_DESCRIPTOR, DWORD, SECURITY_INFORMATION, LPSTR *, PULONG);

    LPSTR str = NULL;

    if (!((SDTOSTR*)pProcAddr)(sd, SDDL_REVISION_1, 0xF, &str, NULL))
    {
        LOG((LF_CORDB, LL_INFO10,
             "IPCWI::DumpSD: ConvertSecurityDescriptorToStringSecurityDescriptorW failed %d\n",
             GetLastError()));
        goto ErrorExit;
    }

    fprintf(stderr, "SD for IPC: %S\n", str);
    LOG((LF_CORDB, LL_INFO10, "IPCWI::DumpSD: SD for IPC: %s\n", str));

    (LocalFree)(str);

ErrorExit:
    return;
}
#endif // _DEBUG

//-----------------------------------------------------------------------------
// Generic init
//-----------------------------------------------------------------------------
HRESULT IPCWriterInterface::Init()
{
    LIMITED_METHOD_CONTRACT;

    // Nothing to do anymore in here...
    return S_OK;
}

//-----------------------------------------------------------------------------
// Generic publish
//-----------------------------------------------------------------------------
void IPCWriterInterface::Publish()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    IA64MemoryBarrier();

    // Set the appropriate bit to mark the LegacyPrivate IPC block as initialized
    if (m_ptrLegacyPrivateBlock != NULL)
        m_ptrLegacyPrivateBlock->m_FullIPCHeader.m_header.m_Flags |= IPC_FLAG_INITIALIZED;

    // Set the appropriate bit to mark the SxS Public IPC block as initialized
    if (m_pBlock != NULL)
        m_pBlock->m_Header.m_Flags |= IPC_FLAG_INITIALIZED;
}


#ifndef DACCESS_COMPILE

//-----------------------------------------------------------------------------
// Generic terminate
//-----------------------------------------------------------------------------
void IPCWriterInterface::Terminate()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO10, "IPCWI::Terminate: Writer: closing 0x%08x and 0x%08x\n", m_handleLegacyPrivateBlock, m_handleBlockTable));
    
    if (m_ptrLegacyPrivateBlock == m_pIPCBackupBlockLegacyPrivate)
    {
        // This is the case that we allocate a block of memory and pretending it is the map file view,
        // so we don't need to unmap the file view on m_ptrLegacyPrivateBlock
        m_ptrLegacyPrivateBlock = NULL;
    }

    IPCShared::CloseMemoryMappedFile(m_handleLegacyPrivateBlock, (void*&) m_ptrLegacyPrivateBlock);

    if (m_pBlockTable == m_pBackupBlock)
    {
        // This is the case that we allocate a block of memory and pretending it is the map file view,
        // so we don't need to unmap the file view on m_pBlock
        m_pBlockTable = NULL;
        m_pBlock = NULL;
    }
    else
    {
        BOOL fFreedChunk = TryFreeBlock();
               
        // Release our handle to the shared memory region
        IPCShared::CloseMemoryMappedFile(m_handleBlockTable, (void*&) m_pBlockTable);

        m_pBlockTable = NULL;
        m_pBlock = NULL;
    }

    // If we have a cached SA for this process, go ahead and clean it up.
    if (m_cachedPrivateDescriptor != NULL)
    {
        // DestroySecurityAttributes won't destroy our cached SA, so save the ptr to the SA and clear the cached value
        // before calling it.
        SECURITY_ATTRIBUTES *pSA = m_cachedPrivateDescriptor;
        m_cachedPrivateDescriptor = NULL;
        DestroySecurityAttributes(pSA);
    }
}

#endif

//-----------------------------------------------------------------------------
// Have ctor zero everything out
//-----------------------------------------------------------------------------
IPCWriterImpl::IPCWriterImpl()
{
    LIMITED_METHOD_CONTRACT;

    // Cache pointers to sections
    m_pPerf      = NULL;
    m_pAppDomain = NULL;
    m_pInstancePath = NULL;

    // Mem-Mapped file for LegacyPrivate Block
    m_handleLegacyPrivateBlock = NULL;
    m_ptrLegacyPrivateBlock = NULL;

    // Mem-Mapped file for SxS Public Block
    m_handleBlockTable = NULL;
    m_pBlock = NULL;
    m_pBlockTable       = NULL;
    m_handleBoundaryDesc = NULL;
    m_handlePrivateNamespace = NULL;
    m_pSID = NULL;

    // Security
    m_cachedPrivateDescriptor = NULL;

    m_pIPCBackupBlockLegacyPrivate = NULL;
    m_pBackupBlock = NULL;
}

//-----------------------------------------------------------------------------
// Assert that everything was already shutdown by a call to terminate.
// Shouldn't be anything left to do in the dtor
//-----------------------------------------------------------------------------
IPCWriterImpl::~IPCWriterImpl()
{
#ifndef DACCESS_COMPILE
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(!IsLegacyPrivateBlockOpen());
    if (m_pIPCBackupBlockLegacyPrivate)
    {
        delete [] ((BYTE *)m_pIPCBackupBlockLegacyPrivate);
    }

    _ASSERTE(!IsBlockTableOpen());
    //Note: m_handlePrivateNamespace is not NULL. This is because we do not Close the handle to PNS and instead
    //let the OS close it for us. This is because if we close the PNS, then the reader(perfmon) cannot open this PNS.
    _ASSERTE(!m_handleBoundaryDesc);
    _ASSERTE(!m_pSID);
    if (m_pBackupBlock)
    {
        delete [] ((BYTE *)m_pBackupBlock);
    }
#endif // DACCESS_COMPILE    
}

//-----------------------------------------------------------------------------
// Accessors to get each clients' blocks
//-----------------------------------------------------------------------------
struct PerfCounterIPCControlBlock * IPCWriterInterface::GetPerfBlock()
{
    LIMITED_METHOD_CONTRACT;
    return m_pPerf;
}

struct AppDomainEnumerationIPCBlock * IPCWriterInterface::GetAppDomainBlock()
{
    LIMITED_METHOD_CONTRACT;
    return m_pAppDomain;
}

//-----------------------------------------------------------------------------
// Helper to destroy the security attributes for the shared memory for a given
// process.
//-----------------------------------------------------------------------------
void IPCWriterInterface::DestroySecurityAttributes(SECURITY_ATTRIBUTES *pSA)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Don't destroy our cached SA!
    if (pSA == m_cachedPrivateDescriptor)
        return;

    IPCShared::DestroySecurityAttributes(pSA);
}

/************************************ IPC BLOCK TABLE ************************************/

#ifndef DACCESS_COMPILE

BOOL IPCWriterInterface::TryAllocBlock(DWORD numRetries)
{
    _ASSERTE(m_pBlock == NULL);

    for (DWORD i = 0; i < IPC_NUM_BLOCKS_IN_TABLE; ++i) 
    {
        m_pBlock = m_pBlockTable->GetBlock(i);

        IPCHeaderLockHolder lockHolder(m_pBlock->m_Header);
        if (lockHolder.TryGetLock(numRetries) == FALSE)
            continue;

        DWORD runtimeId = m_pBlock->m_Header.m_RuntimeId;
        if (runtimeId == 0)
        {
            // Set the runtime ID
            m_pBlock->m_Header.m_RuntimeId = GetRuntimeId();

            // Set up the IPC header while we
            // still hold the lock
            CreateIPCHeader();

            return TRUE;
        }
    }

    m_pBlock = NULL;
    return FALSE;
}

BOOL IPCWriterInterface::TryFreeBlock()
{
    _ASSERTE(m_pBlock != NULL);

    DWORD retriesLeft = 100;
    DWORD dwSwitchCount = 0;

    IPCHeaderLockHolder lockHolder(m_pBlock->m_Header);

    // Try getting the lock, and retry up to 100 times. 
    // If lock cannot be acquired, give up and return FALSE

    if (lockHolder.TryGetLock(100) == FALSE)
        return FALSE;

    // If the lock was acquired successfully, mark this
    // block as free, release the lock, and return TRUE

    m_pBlock->m_Header.m_RuntimeId = 0;
    m_pBlock = NULL;

    return TRUE;
}


//-----------------------------------------------------------------------------
// Open our SxS Public IPC block on the given pid.
//-----------------------------------------------------------------------------
HRESULT IPCWriterInterface::CreateSxSPublicBlockOnPid(DWORD pid)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Note: if our SxS Public block is open, we shouldn't be creating it again.
    _ASSERTE(!IsBlockTableOpen());

    if (IsBlockTableOpen())
    {
        // if we goto errExit, it will close the file. We don't want that.
        return HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);
    }
    

    // Note: if PID != GetCurrentProcessId(), we're expected to be opening
    // someone else's IPCBlock, so if it doesn't exist, we should assert.
    HRESULT hr = S_OK;

    SECURITY_ATTRIBUTES *pSA = NULL;

    EX_TRY
    {
        // Grab the SA
        SString szMemFileName;
        hr = GetSxSPublicSecurityAttributes(pid, &pSA);
        if (FAILED(hr))
            goto failedToGetBlock;
        
        hr = IPCShared::GenerateBlockTableName(pid, szMemFileName, m_handleBoundaryDesc, m_handlePrivateNamespace, &m_pSID, TRUE);
        if (FAILED(hr))
            goto failedToGetBlock;            

        BOOL openedExistingBlock = FALSE;

        m_handleBlockTable = NULL;

        // If unsuccessful, don't ever bail out.
        if (m_handleBlockTable != NULL)
        {
            // Get the pointer - must get it even if ERROR_ALREADY_EXISTS,
            // since the IPC block is allowed to already exist if there is
            // another runtime in the process.
            m_pBlockTable = (IPCControlBlockTable *) MapViewOfFile(m_handleBlockTable,
                                                                        FILE_MAP_ALL_ACCESS,
                                                                        0, 0, 0);
            // If the IPC Block already exists, then we need to check its size and other
            // properties. This is needed because a low privledged user may have spoofed
            // a block with the same name before the CLR started.

            if (m_pBlockTable != NULL && openedExistingBlock)
            {
                // If the BlockTable does not fit in this memory region,
                // then it is not safe to use
                //The following is a security check to ensure that incase the BlockTable was opened by a
                //malicious user we simply commit the block table to ensure that its of the required size4
                PTR_IPCControlBlockTable pBlockTable = (PTR_IPCControlBlockTable) ClrVirtualAlloc(m_pBlockTable,sizeof(IPCControlBlockTable),MEM_COMMIT, PAGE_READWRITE);
                if(pBlockTable == NULL || pBlockTable != m_pBlockTable)
                {
                    goto failedToGetBlock;
                }
            }
        }

        BOOL fGotBlock = FALSE;

        // If opening the shared memory block failed, then we need to go down an error path
        if (m_pBlockTable == NULL)
            goto failedToGetBlock;

        // Try allocating a chunk by iterating over the chunks;
        // if a chunk is locked, don't spin waiting on the lock
        fGotBlock = TryAllocBlock(0);

        // If we failed to allocate a chunk, try iterating over the chunks again,
        // but this time if a chunk is locked, spin for a while to wait on the lock
        if (!fGotBlock)
            fGotBlock = TryAllocBlock(100);

        // If we succeeded in allocating a chunk, we're done
        if (fGotBlock)
        {
            _ASSERTE(m_pBlock != NULL);
            goto done;
        }

        // If we failed to allocate a chunk, so we need to do some 
        // cleanup and set up a "backup" block. When we go into this 
        // code path, our perf counters won't work. But our code will
        // continue to run.


failedToGetBlock:

        // Release our handle to the shared memory region
        if(m_pBlockTable != NULL)
            IPCShared::CloseMemoryMappedFile(m_handleBlockTable, (void*&) m_pBlockTable);

        // Set all out SxSPublic pointers to NULL
        m_pBlockTable = NULL;
        m_pBlock = NULL;

        // Allocate a "backup" block
        DWORD arraySize = sizeof(IPCControlBlockTable);
        m_pBackupBlock = (IPCControlBlockTable *) new BYTE[arraySize];

        // Assert that allocation succeeded
        _ASSERTE(m_pBackupBlock != NULL);

        // Zero out the backup block
        ZeroMemory(m_pBackupBlock, arraySize);
        m_pBlockTable = m_pBackupBlock;

        // Since we are allocating a chunk from the backup block, there
        // should be no contention, and we should always succeed
        fGotBlock = TryAllocBlock(0);
        _ASSERTE(fGotBlock);
        _ASSERTE(m_pBlock != NULL);

done:

        ;
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
        IPCShared::CloseMemoryMappedFile(m_handleBlockTable, (void*&)m_pBlock);
    }
    DestroySecurityAttributes(pSA);
    if(!SUCCEEDED(IPCShared::FreeHandles(m_handleBoundaryDesc,m_pSID)))
    {
        hr = E_FAIL;
    }

    return hr;
}

//-----------------------------------------------------------------------------
// Return the security attributes for the shared memory for a given process.
//-----------------------------------------------------------------------------
HRESULT IPCWriterInterface::GetSxSPublicSecurityAttributes(DWORD pid, SECURITY_ATTRIBUTES **ppSA)
{
    WRAPPER_NO_CONTRACT;
    return CreateWinNTDescriptor(pid, ppSA, eDescriptor_Public);
}


//-----------------------------------------------------------------------------
// Setup a security descriptor for the named kernel objects if we're on NT.
//-----------------------------------------------------------------------------
HRESULT IPCWriterImpl::CreateWinNTDescriptor(DWORD pid, SECURITY_ATTRIBUTES **ppSA, EDescriptorType descType)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HRESULT hr = NO_ERROR;
    *ppSA = NULL;

    // If the caller wants the private descriptor for the current process
    // and a cached copy exists, return the cached copy
    if (descType == eDescriptor_Private && m_cachedPrivateDescriptor != NULL && pid == GetCurrentProcessId())
    {
        *ppSA = m_cachedPrivateDescriptor;
        return hr;
    }

    hr = IPCShared::CreateWinNTDescriptor(pid, (descType == eDescriptor_Private ? TRUE : FALSE), ppSA, Section, descType);

    // Cache the private descriptor for the current process.
    // We do not cache the public descriptor because it isn't
    // used frequently.
    if (descType == eDescriptor_Private && pid == GetCurrentProcessId())
        m_cachedPrivateDescriptor = *ppSA;

    return hr;
}

//-----------------------------------------------------------------------------
// Helper: Fill out a directory entry.
//-----------------------------------------------------------------------------
void IPCWriterImpl::WriteEntryHelper(EIPCClient eClient,
                                     DWORD offs,
                                     DWORD size)
{
    LIMITED_METHOD_CONTRACT;

    m_pBlock->m_Header.m_table[eClient].m_Offset = offs;
    m_pBlock->m_Header.m_table[eClient].m_Size = size;
}

//-----------------------------------------------------------------------------
// Initialize the header for our SxS public IPC block
//-----------------------------------------------------------------------------
void IPCWriterImpl::CreateIPCHeader()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#if defined(_TARGET_X86_)
    m_pBlock->m_Header.m_Flags = IPC_FLAG_USES_FLAGS | IPC_FLAG_X86;
#else
    m_pBlock->m_Header.m_Flags = IPC_FLAG_USES_FLAGS;
#endif

    // Stamp the IPC block with the version
    m_pBlock->m_Header.m_Version = VER_IPC_BLOCK;
    m_pBlock->m_Header.m_blockSize = SXSPUBLIC_IPC_SIZE_NO_PADDING;

    m_pBlock->m_Header.m_BuildYear = BuildYear;
    m_pBlock->m_Header.m_BuildNumber = BuildNumber;

    m_pBlock->m_Header.m_numEntries = eIPC_MAX;

    //
    // Fill out directory (offset and size of each block).
    // First fill in the used entries.
    //

    WriteEntryHelper(eIPC_PerfCounters,
                     offsetof(IPCControlBlock, m_perf),
                     sizeof(PerfCounterIPCControlBlock));

    // Cache our client pointers
    m_pPerf     = &(m_pBlock->m_perf);
}

#endif

/*********************************** LEGACY FUNCTIONS ***********************************
 *
 *  We plan to remove the LegacyPrivate block in the near future. However, the debugger 
 *  still currently relies on the LegacyPrivate block for AppDomain enumeration, and we 
 *  cannot rip out the LegacyPrivate block until the debugger is changed accordingly.
 *
 ****************************************************************************************/

#ifndef DACCESS_COMPILE


//-----------------------------------------------------------------------------
// Open our LegacyPrivate IPC block on the given pid.
//-----------------------------------------------------------------------------
HRESULT IPCWriterInterface::CreateLegacyPrivateBlockTempV4OnPid(DWORD pid, BOOL inService, HINSTANCE *phInstIPCBlockOwner)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Init the IPC block owner HINSTANCE to 0.
    *phInstIPCBlockOwner = 0;

    // Note: if our LegacyPrivate block is open, we shouldn't be creating it again.
    _ASSERTE(!IsLegacyPrivateBlockOpen());

    if (IsLegacyPrivateBlockOpen())
    {
        // if we goto errExit, it will close the file. We don't want that.
        return HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);
    }

    // Note: if PID != GetCurrentProcessId(), we're expected to be opening
    // someone else's IPCBlock, so if it doesn't exist, we should assert.
    HRESULT hr = S_OK;

    SECURITY_ATTRIBUTES *pSA = NULL;

    EX_TRY
    {
        // Grab the SA
        hr = CreateWinNTDescriptor(pid, &pSA, eDescriptor_Private);
        if (FAILED(hr))
            ThrowHR(hr);
        
        SString szMemFileName;

        IPCShared::GenerateNameLegacyTempV4(pid, szMemFileName);

        // Connect the handle
        m_handleLegacyPrivateBlock = WszCreateFileMapping(INVALID_HANDLE_VALUE,
                                                    pSA,
                                                    PAGE_READWRITE,
                                                    0,
                                                    sizeof(LegacyPrivateIPCControlBlock),
                                                    szMemFileName);

        DWORD dwFileMapErr = GetLastError();

        LOG((LF_CORDB, LL_INFO10, "IPCWI::CPBOP: CreateFileMapping of %S, handle = 0x%08x, pid = 0x%8.8x GetLastError=%d\n",
            szMemFileName.GetUnicode(), m_handleLegacyPrivateBlock, pid, GetLastError()));

        // If unsuccessful, don't ever bail out.
        if (m_handleLegacyPrivateBlock != NULL && dwFileMapErr != ERROR_ALREADY_EXISTS)
        {
            m_ptrLegacyPrivateBlock = (LegacyPrivateIPCControlBlock *) MapViewOfFile(m_handleLegacyPrivateBlock,
                                                                         FILE_MAP_ALL_ACCESS,
                                                                         0, 0, 0);
        }

        if (m_ptrLegacyPrivateBlock == NULL)
        {
            // when we go into this code path, our debugging and perf counter won't work. But
            // our managed code will continue to run.
            SIZE_T cbLen = sizeof(LegacyPrivateIPCControlBlock);
            m_pIPCBackupBlockLegacyPrivate = (LegacyPrivateIPCControlBlock *) new BYTE[cbLen];
            _ASSERTE(m_pIPCBackupBlockLegacyPrivate != NULL); // throws on OOM.

            ZeroMemory(m_pIPCBackupBlockLegacyPrivate, cbLen); // simulate that OS zeros out memory
            m_ptrLegacyPrivateBlock = m_pIPCBackupBlockLegacyPrivate;
        }

        // Hook up each sections' pointers
        CreateLegacyPrivateIPCHeader();
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
        IPCShared::CloseMemoryMappedFile(m_handleLegacyPrivateBlock, (void*&)m_ptrLegacyPrivateBlock);
    }
    DestroySecurityAttributes(pSA);

    return hr;
}

//-----------------------------------------------------------------------------
// ReDacl our LegacyPrivate block after it has been created.
//-----------------------------------------------------------------------------
HRESULT IPCWriterInterface::ReDaclLegacyPrivateBlock(PSECURITY_DESCRIPTOR pSecurityDescriptor)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (!IsLegacyPrivateBlockOpen())
    {
        // nothing to reDACL.
        return S_OK;
    }

    // note that this call will succeed only if we are the owner of this LegacyPrivate block.
    // That is this call will fail if you call from debugger RS. If this is needed in the
    // future, you can add WRITE_DAC access when we open LegacyPrivate block on the debugger RS.
    //
    if (SetKernelObjectSecurity(m_handleLegacyPrivateBlock, DACL_SECURITY_INFORMATION, pSecurityDescriptor) == 0)
    {
        // failed!
        return HRESULT_FROM_GetLastError();
    }

    return S_OK;
}

//-----------------------------------------------------------------------------
// Helper: Fill out a directory entry.
//-----------------------------------------------------------------------------
void IPCWriterImpl::WriteEntryHelper(ELegacyPrivateIPCClient eClient,
                                     DWORD offs,
                                     DWORD size)
{
    LIMITED_METHOD_CONTRACT;

    if (offs != EMPTY_ENTRY_OFFSET)
    {
        // The incoming offset is the actual data structure offset
        // but the directory is relative to the end of the full header
        // (on v1.2) so subtract that out.
        
        DWORD offsetBase = (DWORD)Internal_GetOffsetBaseLegacyPrivate(*m_ptrLegacyPrivateBlock);
        _ASSERTE(offs >= offsetBase);
        m_ptrLegacyPrivateBlock->m_FullIPCHeader.m_table[eClient].m_Offset = (offs - offsetBase);
    }
    else
    {
        m_ptrLegacyPrivateBlock->m_FullIPCHeader.m_table[eClient].m_Offset = offs;
    }
    m_ptrLegacyPrivateBlock->m_FullIPCHeader.m_table[eClient].m_Size = size;
}

//-----------------------------------------------------------------------------
// Initialize the header for our LegacyPrivate IPC block
//-----------------------------------------------------------------------------
void IPCWriterImpl::CreateLegacyPrivateIPCHeader()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Set the flags
    
#if defined(_TARGET_X86_)
    m_ptrLegacyPrivateBlock->m_FullIPCHeader.m_header.m_Flags = IPC_FLAG_USES_FLAGS | IPC_FLAG_X86;
#else
    m_ptrLegacyPrivateBlock->m_FullIPCHeader.m_header.m_Flags = IPC_FLAG_USES_FLAGS;
#endif

    // Stamp the IPC block with the version
    m_ptrLegacyPrivateBlock->m_FullIPCHeader.m_header.m_Version = VER_LEGACYPRIVATE_IPC_BLOCK;
    m_ptrLegacyPrivateBlock->m_FullIPCHeader.m_header.m_blockSize = sizeof(LegacyPrivateIPCControlBlock);

    m_ptrLegacyPrivateBlock->m_FullIPCHeader.m_header.m_hInstance = GetModuleInst();

    m_ptrLegacyPrivateBlock->m_FullIPCHeader.m_header.m_BuildYear = BuildYear;
    m_ptrLegacyPrivateBlock->m_FullIPCHeader.m_header.m_BuildNumber = BuildNumber;

    m_ptrLegacyPrivateBlock->m_FullIPCHeader.m_header.m_numEntries = eLegacyPrivateIPC_MAX;

    //
    // Fill out directory (offset and size of each block).
    // First fill in the used entries.
    //

    // Even though this first entry is obsolete, it needs to remain
    // here for binary compatibility and can't be marked empty/obsolete
    // as long as m_perf exists in the struct.
    WriteEntryHelper(eLegacyPrivateIPC_PerfCounters,
                     offsetof(LegacyPrivateIPCControlBlock, m_perf),
                     sizeof(PerfCounterIPCControlBlock));

    WriteEntryHelper(eLegacyPrivateIPC_AppDomain,
                     offsetof(LegacyPrivateIPCControlBlock, m_appdomain),
                     sizeof(AppDomainEnumerationIPCBlock));
    WriteEntryHelper(eLegacyPrivateIPC_InstancePath,
                     offsetof(LegacyPrivateIPCControlBlock, m_instancePath),
                     sizeof(m_ptrLegacyPrivateBlock->m_instancePath));

    //
    // Now explicitly mark the unused entries as empty.
    //

    WriteEntryHelper(eLegacyPrivateIPC_Obsolete_Debugger,
                     EMPTY_ENTRY_OFFSET, EMPTY_ENTRY_SIZE);
    WriteEntryHelper(eLegacyPrivateIPC_Obsolete_ClassDump,
                     EMPTY_ENTRY_OFFSET, EMPTY_ENTRY_SIZE);
    WriteEntryHelper(eLegacyPrivateIPC_Obsolete_MiniDump,
                     EMPTY_ENTRY_OFFSET, EMPTY_ENTRY_SIZE);
    WriteEntryHelper(eLegacyPrivateIPC_Obsolete_Service,
                     EMPTY_ENTRY_OFFSET, EMPTY_ENTRY_SIZE);

    // Cache our client pointers
    m_pAppDomain = &(m_ptrLegacyPrivateBlock->m_appdomain);
    m_pInstancePath = m_ptrLegacyPrivateBlock->m_instancePath;
}

PCWSTR IPCWriterInterface::GetInstancePath()
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(IsLegacyPrivateBlockOpen());
    return m_pInstancePath;
}

#endif

PTR_VOID IPCWriterInterface::GetBlockStart()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return m_ptrLegacyPrivateBlock;
}

DWORD IPCWriterInterface::GetBlockSize()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    _ASSERTE(IsLegacyPrivateBlockOpen());
    return m_ptrLegacyPrivateBlock->m_FullIPCHeader.m_header.m_blockSize;
}

PTR_VOID IPCWriterInterface::GetBlockTableStart()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return m_pBlockTable;
}

DWORD IPCWriterInterface::GetBlockTableSize()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    _ASSERTE(IsBlockTableOpen());
    return IPC_BLOCK_TABLE_SIZE;
}

#endif // FEATURE_IPCMAN
