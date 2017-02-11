// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef _CORPOLICY_H
#define _CORPOLICY_H

#include <ole2.h> // Definitions of OLE types.

#ifdef __cplusplus
extern "C" {
#endif

#include "product_version.h"

// {D41E4F1D-A407-11d1-8BC9-00C04FA30A41}
#define COR_POLICY_PROVIDER_DOWNLOAD \
{ 0xd41e4f1d, 0xa407, 0x11d1, {0x8b, 0xc9, 0x0, 0xc0, 0x4f, 0xa3, 0xa, 0x41 } }

// {D41E4F1E-A407-11d1-8BC9-00C04FA30A41}
#define COR_POLICY_PROVIDER_CHECK \
{ 0xd41e4f1e, 0xa407, 0x11d1, {0x8b, 0xc9, 0x0, 0xc0, 0x4f, 0xa3, 0xa, 0x41 } }


// {D41E4F1F-A407-11d1-8BC9-00C04FA30A41}
#define COR_POLICY_LOCKDOWN_CHECK \
{ 0xd41e4f1f, 0xa407, 0x11d1, {0x8b, 0xc9, 0x0, 0xc0, 0x4f, 0xa3, 0xa, 0x41 } }



// This structure is returned from the winverify trust call, free up the structure
// using CoTaskMemAlloc except for COREE_POLICY_PROVIDER which uses LocalALLoc.

typedef struct _COR_TRUST {
    DWORD       cbSize;                   // Size of structure
    DWORD       flag;                     // Reserved
    BOOL        fAllActiveXPermissions;   // ActiveX explicitly asked for all (must have been signed)
    BOOL        fAllPermissions;          // Cor permissions, explicit ask for all
    DWORD       dwEncodingType;           // Encoding type
    PBYTE       pbCorPermissions;         // Encoded cor permission blob
    DWORD       cbCorPermissions;
    PBYTE       pbSigner;                 // Encoded signer.
    DWORD       cbSigner;
    LPCWSTR     pwszZone;                 // Zone index (copied from action data)
    GUID        guidZone;                 // Not used currently
    HRESULT     hVerify;                  // Authenticode policy return
} COR_TRUST, *PCOR_TRUST;

// Pass this structure into WinVerifyTrust (corpol trust provider). The result
// is returned in pbCorTrust.
typedef struct _COR_POLICY_PROVIDER {
    DWORD                 cbSize;                   // Size of policy provider
    LPVOID                pZoneManager;             // Zone interface manager
    LPCWSTR               pwszZone;                 // Zone index
    BOOL                  fNoBadUI;                 // Optional bad ui
    PCOR_TRUST            pbCorTrust;               // Returned cor information (CoTaskMemAlloc)
    DWORD                 cbCorTrust;               // Total allocated size of pCorTrust
    DWORD                 dwActionID;               // Optional ActionID ID
    DWORD                 dwUnsignedActionID;       // Optional ActionID ID
    BOOL                  VMBased;                  // Called from VM (FALSE by DEFAULT)
    DWORD                 dwZoneIndex;              // IE zone numbers
} COR_POLICY_PROVIDER, *PCOR_POLICY_PROVIDER;

//  Returned flags in COR_TRUST flag
#define COR_NOUI_DISPLAYED 0x1
#define COR_DELAYED_PERMISSIONS 0x02  // The subject was unsigned, returned
                                      // look up information in pbCorPermissions
                                      // to be passed into GetUnsignedPermissions().
                                      // If this flag is not set and pbCorPermissions
                                      // is not NULL then pbCorPermissions contains
                                      // encoded permissions

//--------------------------------------------------------------------
// For COR_POLICY_LOCKDOWN_CHECK:
// -----------------------------

// Structure to pass into WVT
typedef struct _COR_LOCKDOWN {
    DWORD                 cbSize;          // Size of policy provider
    DWORD                 flag;            // reserved
    BOOL                  fAllPublishers;  // Trust all publishers or just ones in the trusted data base
} COR_LOCKDOWN, *PCOR_LOCKDOWN;

#ifdef __cplusplus
}
#endif

#endif // _CORPOLICY_H
