// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
//
 
//
// ==--==
//*****************************************************************************
// File: IPCManagerImpl.h
//
// Defines Classes to implement InterProcess Communication Manager for a COM+
//
//*****************************************************************************

#ifndef _IPCManagerImpl_H_
#define _IPCManagerImpl_H_

#include <aclapi.h>

#include "contract.h"
#include "ipcenums.h"


// Version of the IPC Block that this lib was compiled for
const USHORT VER_IPC_BLOCK = 4;

// Versions for the legacy IPC Blocks
const USHORT VER_LEGACYPRIVATE_IPC_BLOCK = 2;
const USHORT VER_LEGACYPUBLIC_IPC_BLOCK = 3;

struct LegacyPrivateIPCControlBlock;


//-----------------------------------------------------------------------------
// Implementation for the IPCManager for COM+.
//-----------------------------------------------------------------------------
class IPCWriterImpl
{
public:

    IPCWriterImpl();
    ~IPCWriterImpl();
 
    BOOL IsLegacyPrivateBlockOpen() const;   
    BOOL IsBlockTableOpen() const;    

	HRESULT CreateWinNTDescriptor(DWORD pid, SECURITY_ATTRIBUTES **ppSA, EDescriptorType descType);

protected:

#ifndef DACCESS_COMPILE

    void CloseMemoryMappedFile(HANDLE & hMemFile, void * & pBlock);

    HRESULT CreateNewIPCBlock();
    void CreateIPCHeader();    
    void WriteEntryHelper(EIPCClient eClient, DWORD offs, DWORD size);
    
#endif

    // Cache pointers to each section
    struct PerfCounterIPCControlBlock   *m_pPerf;
    struct AppDomainEnumerationIPCBlock *m_pAppDomain;
    PCWSTR                               m_pInstancePath;

    // Info on the Block Table
    HANDLE                         m_handleBlockTable;
    HANDLE                         m_handleBoundaryDesc;
    HANDLE                         m_handlePrivateNamespace;
    PSID                           m_pSID;
    PTR_IPCControlBlockTable       m_pBlockTable;    
    PTR_IPCControlBlock            m_pBlock;    
    PTR_IPCControlBlockTable       m_pBackupBlock;

#ifndef DACCESS_COMPILE 
    
    HRESULT CreateNewLegacyPrivateIPCBlock();
    void CreateLegacyPrivateIPCHeader();
    void WriteEntryHelper(ELegacyPrivateIPCClient eClient, DWORD offs, DWORD size);
    
#endif

    // Stats on MemoryMapped file for the given pid
    HANDLE                               m_handleLegacyPrivateBlock;
    PTR_LegacyPrivateIPCControlBlock     m_ptrLegacyPrivateBlock;
    PTR_LegacyPrivateIPCControlBlock     m_pIPCBackupBlockLegacyPrivate;

    // Security attributes cached for the current process.
    SECURITY_ATTRIBUTES                 *m_cachedPrivateDescriptor;

};

//-----------------------------------------------------------------------------
// IPCReader class connects to a COM+ IPC block and reads from it
// <TODO>@todo - make global & private readers</TODO>
//-----------------------------------------------------------------------------
class IPCReaderImpl
{
public:
    IPCReaderImpl();
    ~IPCReaderImpl();

    BOOL TryOpenBlock(IPCHeaderReadHelper & readHelper, DWORD blockIndex);

    BOOL UseWow64StructsLegacy();
    BOOL Internal_CheckEntryEmptyLegacyPublic(DWORD Id);
    BYTE * Internal_GetBlockLegacyPublic(DWORD Id);
    DWORD GetNumEntriesLegacy(void * pBlock);
    IPCEntry * GetDirectoryLegacy(void * pBlock);
    DWORD GetOffsetBaseLegacy();
    DWORD GetFirstExpectedOffsetLegacy();
    USHORT GetFlagsLegacy(void * pBlock);
    DWORD GetBlockSizeLegacy(void * pBlock);

protected:

    HANDLE m_handleBlockTable;
    HANDLE m_handleBoundaryDesc;
    HANDLE m_handlePrivateNamespace;
    PSID m_pSID;
    IPCControlBlockTable * m_pBlockTable;

    BOOL    m_fIsTarget32Bit;
#ifdef _DEBUG
    BOOL    m_fInitialized;
#endif

    HANDLE  m_handleLegacyPrivateBlock;
    LegacyPrivateIPCControlBlock * m_ptrLegacyPrivateBlock;
    HANDLE  m_handleLegacyPublicBlock;

    union
    {
        LegacyPublicIPCControlBlock * m_ptrLegacyPublicBlock;
        LegacyPublicWow64IPCControlBlock  * m_ptrWow64LegacyPublicBlock;
    };
};

//-----------------------------------------------------------------------------
// Inline definitions
//-----------------------------------------------------------------------------

#include "ipcmanagerimpl.inl"

#endif // _IPCManagerImpl_H_
