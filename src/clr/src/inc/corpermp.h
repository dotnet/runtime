//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// File: CorPermP.H
//
// Defines the Private routines defined in the secuirty libraries. These routines
// are mainly for the security dll and the runtime.
//
//*****************************************************************************

#ifndef _CORPERMP_H_
#define _CORPERMP_H_

#include "utilcode.h"
#include "corperme.h"

#ifdef __cplusplus
extern "C" {
#endif

//==========================================================================
// Encoding and Decoding PermissionSets

//==========================================================================
// Initialization routines for registering installable OIDS for capi20
// Currently there is no C/C++ support for OID parsing. It is only supported
// by using the permission objects within the runtime
// 
// Parameter: 
//      dllName         The name of the module (eg. mscorsec.dll)
// Returns:
//      S_OK            This routines only returns S_OK currently
//==========================================================================
HRESULT WINAPI CorPermRegisterServer(LPCWSTR dllName);
HRESULT WINAPI CorPermUnregisterServer();

//==========================================================================
// Removes the capi entries for installable OID's. Is not currently supported
// so does nothing
//
// Returns:
//      S_OK            This routines only returns S_OK currently
//==========================================================================
HRESULT WINAPI CorFactoryRegister(HINSTANCE hInst);

HRESULT WINAPI CorFactoryUnregister();

HRESULT WINAPI CorFactoryCanUnloadNow();

#ifdef __cplusplus
}
#endif
    

#include "corperm.h"
#endif
