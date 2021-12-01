// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    include/pal/cgroup.h

Abstract:

    Header file for the CGroup related functions.



--*/

#ifndef _PAL_CGROUP_H_
#define _PAL_CGROUP_H_

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

void InitializeCGroup();
void CleanupCGroup();

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* _PAL_CGROUP_H_ */

