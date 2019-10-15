// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /*_PAL_MODULENAME_H_*/
