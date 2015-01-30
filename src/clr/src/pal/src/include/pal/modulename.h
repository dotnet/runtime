//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    include/pal/modulename.h

Abstract:
    Header file for functions to get the name of a module

Revision History:



--*/

#ifndef _PAL_MODULENAME_H_
#define _PAL_MODULENAME_H_

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

const char *PAL_dladdr(LPVOID ProcAddress);
#if defined(_AIX)
int GetLibRotorNameViaLoadQuery(LPSTR pszBuf);
#endif

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /*_PAL_MODULENAME_H_*/
