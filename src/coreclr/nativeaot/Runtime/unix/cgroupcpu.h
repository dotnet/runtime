// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __CGROUPCPU_H__
#define __CGROUPCPU_H__

void InitializeCpuCGroup();
bool GetCpuLimit(uint32_t* val);

#endif // __CGROUPCPU_H__
