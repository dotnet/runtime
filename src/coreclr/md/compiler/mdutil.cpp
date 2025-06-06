// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// MDUtil.cpp
//

//
// contains utility code to MD directory. This is only used for the full version.
//
//*****************************************************************************
#include "stdafx.h"
#include "metadata.h"
#include "mdutil.h"
#include "regmeta.h"
#include "disp.h"
#include "mdcommon.h"
#include "importhelper.h"
#include "sstring.h"

//*******************************************************************************
//
// Determine the blob size base of the ELEMENT_TYPE_* associated with the blob.
// This cannot be a table lookup because ELEMENT_TYPE_STRING is an unicode string.
// The size of the blob is determined by calling u16_strstr of the string + 1.
//
//*******************************************************************************
ULONG _GetSizeOfConstantBlob(
    DWORD  dwCPlusTypeFlag, // ELEMENT_TYPE_*
    void * pValue,          // BLOB value
    ULONG  cchString)       // String length in wide chars, or -1 for auto.
{
    ULONG ulSize = 0;

    switch (dwCPlusTypeFlag)
    {
    case ELEMENT_TYPE_BOOLEAN:
        ulSize = sizeof(BYTE);
        break;
    case ELEMENT_TYPE_I1:
    case ELEMENT_TYPE_U1:
        ulSize = sizeof(BYTE);
        break;
    case ELEMENT_TYPE_CHAR:
    case ELEMENT_TYPE_I2:
    case ELEMENT_TYPE_U2:
        ulSize = sizeof(SHORT);
        break;
    case ELEMENT_TYPE_I4:
    case ELEMENT_TYPE_U4:
    case ELEMENT_TYPE_R4:
        ulSize = sizeof(LONG);

        break;

    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_U8:
    case ELEMENT_TYPE_R8:
        ulSize = sizeof(DOUBLE);
        break;

    case ELEMENT_TYPE_STRING:
        if (pValue == 0)
            ulSize = 0;
        else
        if (cchString != (ULONG) -1)
            ulSize = cchString * sizeof(WCHAR);
        else
            ulSize = (ULONG)(sizeof(WCHAR) * u16_strlen((LPWSTR)pValue));
        break;

    case ELEMENT_TYPE_CLASS:
        // This was originally 'sizeof(IUnknown *)', but that varies across platforms.
        //  The only legal value is a null pointer, and on 32 bit platforms we've already
        //  stored 32 bits, so we will use just 32 bits of null.  If the type is
        //  E_T_CLASS, the caller should know that the value is always NULL anyway.
        ulSize = sizeof(ULONG);
        break;
    default:
        _ASSERTE(!"Not a valid type to specify default value!");
        break;
    }
    return ulSize;
} // _GetSizeOfConstantBlob
