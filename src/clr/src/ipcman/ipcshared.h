// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
//
 
//
// ==--==
//*****************************************************************************
// File: IPCShared.h
//
// Shared LegacyPrivate utility functions for COM+ IPC operations
//
//*****************************************************************************

#ifndef _IPCSHARED_H_
#define _IPCSHARED_H_


#include "ipcenums.h"


class SString;

// This is the name of the file backed session's name on the LS (debuggee)
// Name of the LegacyPrivate (per-process) block. %d resolved to a PID
#define CorLegacyPrivateIPCBlock       L"Cor_Private_IPCBlock_%d"
#define CorLegacyPrivateIPCBlockTempV4 L"Cor_Private_IPCBlock_v4_%d"
#define CorLegacyPublicIPCBlock        L"Cor_Public_IPCBlock_%d"
#define CorSxSPublicIPCBlock           L"Cor_SxSPublic_IPCBlock_%d"
#define CorSxSBoundaryDescriptor       L"Cor_CLR_IPCBlock_%d"
#define CorSxSWriterPrivateNamespacePrefix   L"Cor_CLR_WRITER"
#define CorSxSReaderPrivateNamespacePrefix   L"Cor_CLR_READER"
#define CorSxSVistaPublicIPCBlock      L"Cor_SxSPublic_IPCBlock"

#define CorLegacyPrivateIPCBlock_RS       L"CLR_PRIVATE_RS_IPCBlock_%d"
#define CorLegacyPrivateIPCBlock_RSTempV4 L"CLR_PRIVATE_RS_IPCBlock_v4_%d"
#define CorLegacyPublicIPCBlock_RS        L"CLR_PUBLIC_IPCBlock_%d"
#define CorSxSPublicIPCBlock_RS           L"CLR_SXSPUBLIC_IPCBlock_%d"

#define CorSxSPublicInstanceName            L"%s_p%d_r%d"
#define CorSxSPublicInstanceNameWhidbey     L"%s_p%d"

// NOTE: we cannot just remove this otherwise 'FeatureCoreClr' build breaks
//       since this is not defined in old SDK header
#ifndef CREATE_BOUNDARY_DESCRIPTOR_ADD_APPCONTAINER_SID
#define CREATE_BOUNDARY_DESCRIPTOR_ADD_APPCONTAINER_SID 0x1
#endif
// ENDNOTE

enum KernelObject
{
    Section,
    Event,
    PrivateNamespace,
    TotalKernelObjects
};


class IPCShared
{
public:
// Close a handle and pointer to any memory mapped file
    static void CloseMemoryMappedFile(HANDLE & hMemFile, void * & pBlock);

// Based on the pid, write a unique name for a memory mapped file
    static void GenerateName(DWORD pid, SString & sName);
    static void GenerateNameLegacyTempV4(DWORD pid, SString & sName);
    static void GenerateLegacyPublicName(DWORD pid, SString & sName);

    static HRESULT GenerateBlockTableName(DWORD pid, 
                                          SString & sName, 
                                          HANDLE & pBoundaryDesc, 
                                          HANDLE & pPrivateNamespace, 
                                          PSID* pSID, 
                                          BOOL bCreate);
    static HRESULT IPCShared::FreeHandles(HANDLE & hDescriptor, PSID & pSID);
    static HRESULT IPCShared::FreeHandles(HANDLE & hBoundaryDescriptor, PSID & pSID, HANDLE & hPrivateNamespace);
    static HRESULT CreateWinNTDescriptor(DWORD pid, BOOL bRestrictiveACL, SECURITY_ATTRIBUTES **ppSA, KernelObject whatObject);
    static HRESULT CreateWinNTDescriptor(DWORD pid, BOOL bRestrictiveACL, SECURITY_ATTRIBUTES **ppSA, KernelObject whatObject, EDescriptorType descType);
    static void DestroySecurityAttributes(SECURITY_ATTRIBUTES *pSA);

private:
    static const int MaxNumberACEs = 5;
    static BOOL InitializeGenericIPCAcl(DWORD pid, BOOL bRestrictiveACL, PACL *ppACL, KernelObject whatObject, EDescriptorType descType);
    static DWORD GetAccessFlagsForObject(KernelObject whatObject, BOOL bRestrictiveACL);
    static HRESULT GetSidForProcess(HINSTANCE hDll,
                                    DWORD pid,
                                    PSID *ppSID,
                                    __deref_out_opt char **ppBufferToFreeByCaller);
};

#endif
