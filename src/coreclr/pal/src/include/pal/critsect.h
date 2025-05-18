// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    include/pal/critsect.h

Abstract:

    Header file for the critical sections functions.



--*/

#ifndef _PAL_CRITSECT_H_
#define _PAL_CRITSECT_H_

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

VOID InternalInitializeCriticalSection(CRITICAL_SECTION *pcs);
VOID InternalDeleteCriticalSection(CRITICAL_SECTION *pcs);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* _PAL_CRITSECT_H_ */

