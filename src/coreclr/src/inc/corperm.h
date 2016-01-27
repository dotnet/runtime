// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: CorPerm.H
//
// Defines the public routines defined in the security libraries. All these
// routines are defined within CorPerm.lib.
//
//*****************************************************************************

#ifndef _CORPERM_H_
#define _CORPERM_H_

#include <ole2.h> // Definitions of OLE types.

#ifndef FEATURE_PAL
#include <wintrust.h>
#endif

#include <specstrings.h>
#include "corhdr.h"
#include "corpolicy.h"

#ifdef __cplusplus
extern "C" {
#endif


//--------------------------------------------------------------------------
// Global security settings
// ------------------------
// 

// Needs to be in sync with URLZONE
typedef enum {
    LocalMachine = 0, /* URLZONE_LOCAL_MACHINE */     // 0, My Computer
    Intranet     = 1, /* URLZONE_INTRANET */          // 1, The Intranet
    Trusted      = 2, /* URLZONE_TRUSTED */           // 2, Trusted Zone
    Internet     = 3, /* URLZONE_INTERNET */          // 3, The Internet
    Untrusted    = 4, /* URLZONE_UNTRUSTED */         // 4, Untrusted Zone
    NumZones     = 5,
    NoZone       = -1
} SecZone;

// Managed URL action flags (see urlmon.idl)
#define URLACTION_MANAGED_MIN                           0x00002000
#define URLACTION_MANAGED_SIGNED                        0x00002001
#define URLACTION_MANAGED_UNSIGNED                      0x00002004
#define URLACTION_MANAGED_MANIFEST_PERMISSIONS          0x00002007
#define URLACTION_MANAGED_MAX                           0x000020FF

// Global disable flags. These are set for every zone.
#define CORSETTING_EXECUTION_PERMISSION_CHECK_DISABLED  0x00000100

// Trust Levels 
#define URLPOLICY_COR_NOTHING                   0x00000000
#define URLPOLICY_COR_TIME                      0x00010000
#define URLPOLICY_COR_EQUIPMENT                 0x00020000
#define URLPOLICY_COR_EVERYTHING                0x00030000
#define URLPOLICY_COR_CUSTOM                    0x00800000

// Manifest permission settings - note that URLPOLICY_DISABLED is also a valid value
#define URLPOLICY_COR_HIGH_SAFETY               0x00010000
#define URLPOLICY_COR_LOW_SAFETY                0x00030000

#define KEY_COM_SECURITY_POLICY L"\\Security\\Policy" 
#define KEY_COM_SECURITY_ZONEOVERRIDE L"TreatCustomZonesAsInternetZone"
#define HKEY_POLICY_ROOT        HKEY_LOCAL_MACHINE

#ifndef FEATURE_PAL

//--------------------------------------------------------------------
// GetPublisher
// ------------
// Returns signature information (Encoded signature and permissions)
// NOTE: This does perform any policy checks on the certificates. All
// that can be determined is the File was signed and the bits are OK.
//
// Free information with CoTaskMemFree (just the pointer not the contents)
//

#define COR_UNSIGNED_NO         0x0
#define COR_UNSIGNED_YES        0x1
#define COR_UNSIGNED_ALWAYS     0x2

extern HRESULT DisplayUnsignedRequestDialog(HWND hParent,       // Parents hwnd
                                            PCRYPT_PROVIDER_DATA pData, 
                                            LPCWSTR pURL,       // Url associated with code
                                            LPCWSTR pZONE,      // Zone associated with code
                                            DWORD* pdwState);   // Return COR_UNSIGNED_YES or COR_UNSIGNED_NO

// For dwFlag values
#define COR_NOUI               0x01
#define COR_NOPOLICY           0x02
#define COR_DISPLAYGRANTED     0x04    // Intersect the requested permissions with the policy to 
                                       // to display the granted set

HRESULT STDMETHODCALLTYPE
GetPublisher(__in __in_z IN LPWSTR        pwsFileName,      // File name, this is required even with the handle
             IN HANDLE        hFile,            // Optional file name
             IN  DWORD        dwFlags,          // COR_NOUI or COR_NOPOLICY
             OUT PCOR_TRUST  *pInfo,            // Returns a PCOR_TRUST (Use CoTaskMemFree)
             OUT DWORD       *dwInfo);          // Size of pInfo.

#endif  // !FEATURE_PAL

interface IMetaDataAssemblyImport;

// Structure used to describe an individual security permission.
class CORSEC_ATTRIBUTE
{
public:
    DWORD           dwIndex;                    // Unique permission index used for error tracking
    CHAR*           pName;   // Fully qualified permission class name
    mdMemberRef     tkCtor;                     // Custom attribute constructor
    mdTypeRef       tkTypeRef;                  // Custom attribute class ref
    mdAssemblyRef   tkAssemblyRef;              // Custom attribute class assembly
    BYTE            *pbValues;                  // Serialized field/property initializers
    SIZE_T          cbValues;                   // Byte count for above
    WORD            wValues;                    // Count of values in above

    CORSEC_ATTRIBUTE()
    {
        pbValues = NULL;
        pName = NULL;
    }

    ~CORSEC_ATTRIBUTE()
    {
        delete [] pbValues;
        delete [] pName;
    }
};

// Context structure that tracks the creation of a security permission set from
// individual permission requests.
class CORSEC_ATTRSET
{
public:
    mdToken         tkObj;                      // Parent object
    DWORD           dwAction;                   // Security action type (CorDeclSecurity)
    DWORD           dwAttrCount;              // Number of attributes in set
    CORSEC_ATTRIBUTE     *pAttrs;              // Pointer to array of attributes
    DWORD           dwAllocated;                // Number of elements in above array
#ifdef __cplusplus
    IMetaDataAssemblyImport *pImport;           // Current meta data scope
    IUnknown        *pAppDomain;                // AppDomain in which managed security code will be run. 

#else
    void            *pImport;
    void            *pAppDomain;
#endif

    CORSEC_ATTRSET()
    {
        pAttrs = NULL;
    }

    ~CORSEC_ATTRSET()
    {
        delete [] pAttrs;
    }
};

// Reads permission requests (if any) from the manifest of an assembly.
HRESULT STDMETHODCALLTYPE
GetPermissionRequests(LPCWSTR   pwszFileName,
                      BYTE    **ppbMinimal,
                      DWORD    *pcbMinimal,
                      BYTE    **ppbOptional,
                      DWORD    *pcbOptional,
                      BYTE    **ppbRefused,
                      DWORD    *pcbRefused);

// Translate a set of security custom attributes into a serialized permission set blob.
HRESULT STDMETHODCALLTYPE
TranslateSecurityAttributes(CORSEC_ATTRSET    *pPset,
                            BYTE          **ppbOutput,
                            DWORD          *pcbOutput,
                            BYTE          **ppbNonCasOutput,
                            DWORD          *pcbNonCasOutput,
                            DWORD          *pdwErrorIndex);

class CMiniMdRW;
struct IMDInternalImport;

HRESULT STDMETHODCALLTYPE
GroupSecurityAttributesByAction(CORSEC_ATTRSET /*OUT*/rPermSets[],
                                      COR_SECATTR rSecAttrs[],
                                      ULONG cSecAttrs,
                                      mdToken tkObj,
                                      ULONG *pulErrorAttr,
                                      CMiniMdRW* pMiniMd,
                                      IMDInternalImport* pInternalImport);

// if pBuffer is NULL, this just sets *pCount to the number of bytes required
// if pBuffer is not NULL, it serializes pAttrSet into pBuffer
HRESULT AttributeSetToBlob(CORSEC_ATTRSET* pAttrSet, BYTE* pBuffer, SIZE_T* pCount, IMetaDataAssemblyImport *pImport, DWORD dwAction);

#ifdef __cplusplus
}
#endif

#endif
