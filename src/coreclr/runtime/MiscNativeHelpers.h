// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __MISCNATIVEHELPERS_H__
#define __MISCNATIVEHELPERS_H__

#if defined(TARGET_X86) || defined(TARGET_AMD64)
extern "C" void QCALLTYPE X86Base_CpuId(int cpuInfo[4], int functionId, int subFunctionId);
#endif // defined(TARGET_X86) || defined(TARGET_AMD64)

extern "C" void QCALLTYPE Interlocked_MemoryBarrierProcessWide();

// WASM-TODO: once we have R2R stack walking working we should be able to set pTransitionBlock to &transitionBlock unconditionally

#if defined(TARGET_WASM) && !defined(TARGET_NATIVEAOT)
#define PREPARE_TRANSITION_ARG() \
TransitionBlock transitionBlock; \
transitionBlock.m_ReturnAddress = 0; \
transitionBlock.m_StackPointer = callersStackPointer; \
TransitionBlock* pTransitionBlock = (callersStackPointer == 0 || *(int*)callersStackPointer == TERMINATE_R2R_STACK_WALK) ? NULL : &transitionBlock

#define TRANSITION_ARG_PARAM pTransitionBlock
#else
#define PREPARE_TRANSITION_ARG() do { } while(0)
#define TRANSITION_ARG_PARAM nullptr
#endif

#endif // __MISCNATIVEHELPERS_H__
