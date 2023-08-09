// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __NUMASUPPORT_H__
#define __NUMASUPPORT_H__

void NUMASupportInitialize();
int GetNumaNodeNumByCpu(int cpu);
long BindMemoryPolicy(void* start, unsigned long len, const unsigned long* nodemask, unsigned long maxnode);

#endif // __NUMASUPPORT_H__
