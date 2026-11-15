// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef __CGROUP_H__
#define __CGROUP_H__

#include <stdint.h>

void InitializeCGroup();
void CleanupCGroup();

// Can be called before InitializeCGroup.
bool GetCGroupCpuLimit(uint32_t* val);

#endif // __CGROUP_H__
