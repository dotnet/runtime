// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//*****************************************************************************
// QuirksApi.h
// 

//
// definitions required for querying windows quirk DB
//*****************************************************************************

#ifndef _QUIRKSAPI_H_
#define _QUIRKSAPI_H_


#define QUIRKSAPI_DLL     W("api-ms-win-core-quirks-l1-1-0.dll")

typedef struct _CPT_QUIRK_DATA {
    DWORD Size;
    DWORD Id;
    WCHAR Name[128];
    WCHAR CommandLine[128];
} CPT_QUIRK_DATA, *PCPT_QUIRK_DATA;



typedef bool (STDMETHODCALLTYPE * PFN_CptQuirkIsEnabled3)(
    PCWSTR QuirkName,
    DWORD ComponentVersion);

typedef HRESULT (STDMETHODCALLTYPE * PFN_CptQuirkGetData2)(
    PCWSTR QuirkName,
    PCPT_QUIRK_DATA QuirkData);

#endif // _QUIRKSAPI_H_

