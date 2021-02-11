// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    include/pal/numa.h

Abstract:

    Header file for the NUMA functions.



--*/

#ifndef _PAL_NUMA_H_
#define _PAL_NUMA_H_

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

BOOL
NUMASupportInitialize();

VOID
NUMASupportCleanup();

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* _PAL_CRITSECT_H_ */
