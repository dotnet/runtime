// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    include/pal/utf8.h

Abstract:
    Header file for UTF-8 conversion functions.

Revision History:



--*/

#ifndef _PAL_UTF8_H_
#define _PAL_UTF8_H_

#include <pal/palinternal.h> /* for WCHAR */

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/*++
Function :
    UTF8ToUnicode

    Convert a string from UTF-8 to UTF-16 (UCS-2)
--*/
int UTF8ToUnicode(LPCSTR lpSrcStr, int cchSrc, LPWSTR lpDestStr, int cchDest, DWORD dwFlags);


/*++
Function :
    UnicodeToUTF8

    Convert a string from UTF-16 (UCS-2) to UTF-8
--*/
int UnicodeToUTF8(LPCWSTR lpSrcStr, int cchSrc, LPSTR lpDestStr, int cchDest);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* _PAL_UTF8_H_ */
