// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: IPCManagerInterface.h
//
// Interface for InterProcess Communication with a COM+ process.
//
//*****************************************************************************


#ifndef _IPCMANAGERINTERFACE_H_
#define _IPCMANAGERINTERFACE_H_

#include "../ipcman/ipcheader.h"

struct PerfCounterIPCControlBlock;
struct AppDomainEnumerationIPCBlock;

#include "../ipcman/ipcmanagerimpl.h"

// These are the right that we will give to the global section and global events used
// in communicating between debugger and debugee
//
// SECTION_ALL_ACCESS is needed for the IPC block. Unfortunately, we DACL our events and
// IPC block identically. Or this particular right does not need to bleed into here. 
//
#define CLR_IPC_GENERIC_RIGHT (GENERIC_READ | GENERIC_WRITE | GENERIC_EXECUTE | STANDARD_RIGHTS_ALL | SECTION_ALL_ACCESS)

//-----------------------------------------------------------------------------
// Writer - create a COM+ IPC Block with security attributes.
//-----------------------------------------------------------------------------
class IPCWriterInterface : public IPCWriterImpl
{
public:

    BOOL TryAllocBlock(DWORD numRetries);
    BOOL TryFreeBlock();

    //.............................................................................
    // Creation / Destruction only on implementation
    //.............................................................................
    HRESULT Init();
    void Terminate();

    //.............................................................................
    // Marks the data in the IPC block as initialized
    //.............................................................................
    void Publish();

    //.............................................................................
    // Accessor for the Perf block
    //.............................................................................
    PerfCounterIPCControlBlock * GetPerfBlock();
    
	void DestroySecurityAttributes(SECURITY_ATTRIBUTES *pSA);

#ifndef DACCESS_COMPILE

    //.............................................................................
    // Create the SxS Public IPC block. 
    //.............................................................................
    HRESULT CreateSxSPublicBlockOnPid(DWORD PID);

    //.............................................................................
    // Open the SxS Public IPC block that has alread been created.
    //.............................................................................
    HRESULT OpenSxSPublicBlockOnPid(DWORD PID);

    HRESULT GetSxSPublicSecurityAttributes(DWORD pid, SECURITY_ATTRIBUTES **ppSA);    

#endif 

    DWORD       GetBlockTableSize();
    PTR_VOID    GetBlockTableStart();

    /*********************************** LEGACY FUNCTIONS ***********************************
     *
     *  Though newer versions of the runtime do not open the LegacyPrivate block or the LegacyPublic
     *  block, we need functionality in the reader to inspect the LegacyPrivate block and LegacyPublic
     *  block so reading data from older runtimes.
     *
     ****************************************************************************************/

    //.............................................................................
    // The AppDomain block resides within the LegacyPrivate IPC block. 
    //.............................................................................
    AppDomainEnumerationIPCBlock * GetAppDomainBlock();
    
    //.............................................................................
    // Create the LegacyPrivate IPC block. If this fails because the IPC block has already been
    // created by another module then the phInstIPCBlockOwner argument will be set to the
    // HINSTANCE of the module that created the IPC block.
    // Set inService to TRUE if creating from within a service on behalf of a process.
    //.............................................................................
    HRESULT CreateLegacyPrivateBlockTempV4OnPid(DWORD PID, BOOL inService, HINSTANCE *phInstIPCBlockOwner);

    //.............................................................................
    // Open the LegacyPrivate IPC block that has alread been created.
    //.............................................................................
    HRESULT OpenLegacyPrivateBlockOnPid(DWORD PID);

    //.............................................................................
    // ReDacl our LegacyPrivate block after it has been created.
    //.............................................................................
    HRESULT ReDaclLegacyPrivateBlock(PSECURITY_DESCRIPTOR pSecurityDescriptor);

    //.............................................................................
    // Accessors - return info from header - 
    // These functions work on LegacyPrivate Block
    //.............................................................................
    DWORD       GetBlockSize();
    PTR_VOID    GetBlockStart();
    PCWSTR      GetInstancePath();
};

//-----------------------------------------------------------------------------
// IPCReader class connects to a COM+ IPC block and reads from it
// <TODO>@FUTURE - make global & private readers</TODO>
//-----------------------------------------------------------------------------
class IPCReaderInterface : public IPCReaderImpl
{
public:

    void MakeInstanceName(const WCHAR * szProcessName, DWORD pid, DWORD runtimeId, SString & sName);
    void MakeInstanceNameWhidbey(const WCHAR * szProcessName, DWORD pid, SString & sName);

    BOOL TryOpenBlock(IPCHeaderReadHelper & readHelper, DWORD blockIndex);

    //.............................................................................
    // Create & Destroy
    //.............................................................................
    ~IPCReaderInterface();

    HRESULT OpenLegacyPrivateBlockTempV4OnPid(DWORD pid);
    HRESULT OpenLegacyPrivateBlockTempV4OnPid(DWORD pid, DWORD dwDesiredAccess);

    HRESULT OpenLegacyPrivateBlockOnPid(DWORD pid);
    HRESULT OpenLegacyPrivateBlockOnPid(DWORD pid, DWORD dwDesiredAccess);
    HRESULT OpenLegacyPrivateBlockOnPidReadWrite(DWORD pid);
    HRESULT OpenLegacyPrivateBlockOnPidReadOnly(DWORD pid);
    void CloseLegacyPrivateBlock();

#ifndef DACCESS_COMPILE
    HRESULT OpenLegacyPublicBlockOnPid(DWORD pid);
    HRESULT OpenLegacyPublicBlockOnPid(DWORD pid, DWORD dwDesiredAccess);
    HRESULT OpenLegacyPublicBlockOnPidReadOnly(DWORD pid);    
    void CloseLegacyPublicBlock();

    HRESULT OpenBlockTableOnPid(DWORD pid);
    HRESULT OpenBlockTableOnPid(DWORD pid, DWORD dwDesiredAccess);
    HRESULT OpenBlockTableOnPidReadOnly(DWORD pid);    
    void CloseBlockTable();
#endif

    //.............................................................................
    // Accessors - return info from header
    // <TODO>@FUTURE - factor this into IPCWriterInterface as well.</TODO>
    //.............................................................................
    USHORT      GetBlockVersion();
    USHORT      GetLegacyPublicBlockVersion();
    HINSTANCE   GetInstance();
    USHORT      GetBuildYear();
    USHORT      GetBuildNumber();
    PVOID       GetBlockStart();
    PCWSTR      GetInstancePath();

    //........................................
    // Check the block to see if its valid
    //........................................
    BOOL IsValidLegacy(BOOL fIsLegacyPublicBlock);

#ifndef DACCESS_COMPILE
    //BOOL IsValidForSxSPublic(IPCControlBlock * pBlock);
#endif

    //.............................................................................
    // Get different sections of the IPC
    //.............................................................................
    void * GetLegacyPrivateBlock(ELegacyPrivateIPCClient eClient);
    void * GetLegacyPublicBlock(ELegacyPublicIPCClient eClient);    
#ifndef DACCESS_COMPILE
    //void * GetSxSPublicBlock(DWORD chunkIndex, EIPCClient eClient);    
#endif

    void * GetPerfBlockLegacyPrivate();
    void * GetPerfBlockLegacyPublic();    
#ifndef DACCESS_COMPILE
    //PerfCounterIPCControlBlock *    GetPerfBlockSxSPublic(DWORD chunkIndex);
#endif
    AppDomainEnumerationIPCBlock * GetAppDomainBlock();

    //.............................................................................
    // Return true if we're connected to a memory-mapped file, else false.
    //.............................................................................
    bool IsLegacyPrivateBlockOpen() const;
    bool IsLegacyPublicBlockOpen() const;
    bool IsBlockTableOpen() const;    

    HRESULT IsCompatablePlatformForDebuggerAndDebuggee(DWORD pid, BOOL * pfCompatible);
};

#endif

