// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    include/pal/map.h

Abstract:

    Header file for file mapping functions.



--*/

#ifndef _PAL_MAP_H_
#define _PAL_MAP_H_

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/*++
Function :
    MAPGetRegionInfo

    Parameters:
    lpAddress: pointer to the starting memory location, not necessary
               to be rounded to the page location

    lpBuffer: if this function finds information about the specified address,
              the information is stored in this struct

    Note: This function is to be used in virtual.c

    Returns TRUE if this function finds information about the specified address
--*/

BOOL MAPGetRegionInfo(LPVOID lpAddress, PMEMORY_BASIC_INFORMATION lpBuffer);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* _PAL_MAP_H_ */

