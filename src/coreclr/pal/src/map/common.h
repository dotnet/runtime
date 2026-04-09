// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    include/pal/common.h

Abstract:
    Header file for common helper functions in the map module.



--*/

#ifndef __COMMON_H_
#define __COMMON_H_

#ifdef __cplusplus
extern "C"
{
#endif

/*****
 *
 * W32toUnixAccessControl( DWORD ) - Maps Win32 to Unix memory access controls .
 *
 */
INT W32toUnixAccessControl( IN DWORD flProtect );

#ifdef __cplusplus
}
#endif

#endif /* __COMMON_H_ */




