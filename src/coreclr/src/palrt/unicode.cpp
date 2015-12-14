//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

#include "common.h" 

// This is a simplified implementation of IsTextUnicode.
// https://github.com/dotnet/coreclr/issues/2307
BOOL IsTextUnicode(CONST VOID* lpv, int iSize, LPINT lpiResult)
{
    *lpiResult = 0;

    if (iSize < 2) return FALSE;

    BYTE * p = (BYTE *)lpv;

    // Check for Unicode BOM
    if ((*p == 0xFF) && (*(p+1) == 0xFE))
    {
        *lpiResult |= IS_TEXT_UNICODE_SIGNATURE;
        return TRUE;
    }

    return FALSE;
}

