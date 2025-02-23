// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTERPEXEC_H_
#define _INTERPEXEC_H_

#include "../interpreter/interpretershared.h"

#define INTERP_STACK_SIZE 1024*1024

struct StackVal
{
    union
    {
        int32_t i;
        int64_t l;
        float f;
        double d;
        void *o;
        void *p;
    } data;
};

struct InterpMethodContextFrame
{
    InterpMethodContextFrame *pParent;
    int32_t *startIp; // from start_ip we can obtain InterpMethod and MethodDesc
    int8_t *pStack;
    int8_t *pRetVal;
    int32_t *ip;
};

struct InterpThreadContext
{
    int8_t *pStackStart;
    int8_t *pStackEnd;

    // This stack pointer is the highest stack memory that can be used by the current frame. This does not
    // change throughout the execution of a frame and it is essentially the upper limit of the execution
    // stack pointer. It is needed when re-entering interp, to know from which address we can start using
    // stack, and also needed for the GC to be able to scan the stack.
    int8_t *pStackPointer;
};

InterpThreadContext* InterpGetThreadContext();
void InterpExecMethod(InterpMethodContextFrame *pFrame, InterpThreadContext *pThreadContext);

#endif
