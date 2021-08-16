// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// ===========================================================================
// File: coguid.cpp
//
// misc guid functions for PALRT
// ===========================================================================

#include "common.h"

STDAPI_(int) StringFromGUID2(REFGUID rguid, LPOLESTR lptsz, int cchMax)
{
    if (cchMax < 39)
        return 0;

    return swprintf_s(lptsz, cchMax, W("{%08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x}"),
            rguid.Data1, rguid.Data2, rguid.Data3,
            rguid.Data4[0], rguid.Data4[1],
            rguid.Data4[2], rguid.Data4[3],
            rguid.Data4[4], rguid.Data4[5],
            rguid.Data4[6], rguid.Data4[7]) + 1;
}

static BOOL wUUIDFromString(LPCWSTR lpsz, GUID * pguid);
static BOOL wGUIDFromString(LPCWSTR lpsz, GUID * pguid);

static BOOL HexStringToDword(LPCWSTR FAR& lpsz, DWORD FAR& Value,
        int cDigits, WCHAR chDelim);

//+-------------------------------------------------------------------------
//
//  Function:   IIDFromString
//
//  Synopsis:   converts string {...} form int guid
//
//  Arguments:  [lpsz] - ptr to buffer for results
//              [lpclsid] - the guid to convert
//
//  Returns:    NOERROR
//              CO_E_CLASSSTRING
//
//--------------------------------------------------------------------------
STDAPI IIDFromString(LPWSTR lpsz, CLSID * lpclsid)
{
    if (lpsz == NULL)
    {
        *lpclsid = CLSID_NULL;
        return NOERROR;
    }

    if (*lpsz == 0)
    {
        return(CO_E_CLASSSTRING);
    }

    return wGUIDFromString(lpsz,lpclsid)
        ? NOERROR : CO_E_CLASSSTRING;
}

//+-------------------------------------------------------------------------
//
//  Function:   wGUIDFromString    (internal)
//
//  Synopsis:   Parse GUID such as {00000000-0000-0000-0000-000000000000}
//
//  Arguments:  [lpsz]  - the guid string to convert
//              [pguid] - guid to return
//
//  Returns:    TRUE if successful
//
//--------------------------------------------------------------------------
static BOOL wGUIDFromString(LPCWSTR lpsz, GUID * pguid)
{
    if (*lpsz++ != '{' )
        return FALSE;

    if (wUUIDFromString(lpsz, pguid) != TRUE)
        return FALSE;

    lpsz +=36;

    if (*lpsz++ != '}' )
        return FALSE;

    if (*lpsz != '\0')
        return FALSE;

    return TRUE;
}

//+-------------------------------------------------------------------------
//
//  Function:   wUUIDFromString    (internal)
//
//  Synopsis:   Parse UUID such as 00000000-0000-0000-0000-000000000000
//
//  Arguments:  [lpsz]  - Supplies the UUID string to convert
//              [pguid] - Returns the GUID.
//
//  Returns:    TRUE if successful
//
//--------------------------------------------------------------------------
static BOOL wUUIDFromString(LPCWSTR lpsz, GUID * pguid)
{
    DWORD dw;

    if (!HexStringToDword(lpsz, pguid->Data1, sizeof(DWORD)*2, '-'))
        return FALSE;

    if (!HexStringToDword(lpsz, dw, sizeof(WORD)*2, '-'))
        return FALSE;
    pguid->Data2 = (WORD)dw;

    if (!HexStringToDword(lpsz, dw, sizeof(WORD)*2, '-'))
        return FALSE;
    pguid->Data3 = (WORD)dw;

    if (!HexStringToDword(lpsz, dw, sizeof(BYTE)*2, 0))
        return FALSE;
    pguid->Data4[0] = (BYTE)dw;

    if (!HexStringToDword(lpsz, dw, sizeof(BYTE)*2, '-'))
        return FALSE;
    pguid->Data4[1] = (BYTE)dw;

    if (!HexStringToDword(lpsz, dw, sizeof(BYTE)*2, 0))
        return FALSE;
    pguid->Data4[2] = (BYTE)dw;

    if (!HexStringToDword(lpsz, dw, sizeof(BYTE)*2, 0))
        return FALSE;
    pguid->Data4[3] = (BYTE)dw;

    if (!HexStringToDword(lpsz, dw, sizeof(BYTE)*2, 0))
        return FALSE;
    pguid->Data4[4] = (BYTE)dw;

    if (!HexStringToDword(lpsz, dw, sizeof(BYTE)*2, 0))
        return FALSE;
    pguid->Data4[5] = (BYTE)dw;

    if (!HexStringToDword(lpsz, dw, sizeof(BYTE)*2, 0))
        return FALSE;
    pguid->Data4[6] = (BYTE)dw;

    if (!HexStringToDword(lpsz, dw, sizeof(BYTE)*2, 0))
        return FALSE;
    pguid->Data4[7] = (BYTE)dw;

    return TRUE;
}

//+-------------------------------------------------------------------------
//
//  Function:   HexStringToDword   (private)
//
//  Synopsis:   scan lpsz for a number of hex digits (at most 8); update lpsz
//              return value in Value; check for chDelim;
//
//  Arguments:  [lpsz]    - the hex string to convert
//              [Value]   - the returned value
//              [cDigits] - count of digits
//
//  Returns:    TRUE for success
//
//--------------------------------------------------------------------------
static BOOL HexStringToDword(LPCWSTR FAR& lpsz, DWORD FAR& Value,
        int cDigits, WCHAR chDelim)
{
    int Count;

    Value = 0;
    for (Count = 0; Count < cDigits; Count++, lpsz++)
    {
        if (*lpsz >= '0' && *lpsz <= '9')
            Value = (Value << 4) + *lpsz - '0';
        else if (*lpsz >= 'A' && *lpsz <= 'F')
            Value = (Value << 4) + *lpsz - 'A' + 10;
        else if (*lpsz >= 'a' && *lpsz <= 'f')
            Value = (Value << 4) + *lpsz - 'a' + 10;
        else
            return(FALSE);
    }

    if (chDelim != 0)
         return *lpsz++ == chDelim;
    else
         return TRUE;
}
