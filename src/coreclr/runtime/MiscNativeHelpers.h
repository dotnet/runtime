// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __MISCNATIVEHELPERS_H__
#define __MISCNATIVEHELPERS_H__

// Ensure QCALLTYPE is defined
#ifndef QCALLTYPE
#define QCALLTYPE
#endif

#if defined(TARGET_X86) || defined(TARGET_AMD64)
extern "C" void QCALLTYPE X86Base_CpuId(int cpuInfo[4], int functionId, int subFunctionId);
#endif // defined(TARGET_X86) || defined(TARGET_AMD64)

extern "C" void QCALLTYPE Interlocked_MemoryBarrierProcessWide();
extern "C" void QCALLTYPE Buffer_Clear(void *dst, size_t length);
extern "C" void QCALLTYPE Buffer_MemMove(void *dst, void *src, size_t length);

#endif // __MISCNATIVEHELPERS_H__
