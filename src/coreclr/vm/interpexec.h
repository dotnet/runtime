// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTERPEXEC_H_
#define _INTERPEXEC_H_

#include "../interpreter/interpretershared.h"
#include "interpframeallocator.h"

#define INTERP_STACK_SIZE 1024*1024
#define INTERP_STACK_FRAGMENT_SIZE 4096

struct StackVal
{
    union
    {
        int32_t i;
        int64_t l;
        size_t s;
        float f;
        double d;
        void *o;
        void *p;
    } data;
};

typedef DPTR(struct InterpMethodContextFrame) PTR_InterpMethodContextFrame;
class InterpreterFrame;

struct InterpMethodContextFrame
{
    PTR_InterpMethodContextFrame pParent;
    PTR_InterpByteCodeStart startIp; // from startIp we can obtain InterpMethod and MethodDesc
    int8_t *pStack;
    int8_t *pRetVal;
    const int32_t *ip; // This ip is updated only when execution can leave the frame
    PTR_InterpMethodContextFrame pNext;

#ifndef DACCESS_COMPILE
    void ReInit(InterpMethodContextFrame *pParent, InterpByteCodeStart* startIp, int8_t *pRetVal, int8_t *pStack)
    {
        this->pParent = pParent;
        this->startIp = startIp;
        this->pRetVal = pRetVal;
        this->pStack = pStack;
        this->ip = NULL;
    }
#endif // DACCESS_COMPILE
};

struct InterpThreadContext
{
    PTR_INT8 pStackStart;
    PTR_INT8 pStackEnd;

    // This stack pointer is the highest stack memory that can be used by the current frame. This does not
    // change throughout the execution of a frame and it is essentially the upper limit of the execution
    // stack pointer. It is needed when re-entering interp, to know from which address we can start using
    // stack, and also needed for the GC to be able to scan the stack.
    PTR_INT8 pStackPointer;

    FrameDataAllocator frameDataAllocator;

    InterpThreadContext();
    ~InterpThreadContext();
};

struct ExceptionClauseArgs
{
    // Address of the exception clause IR code
    const int32_t *ip;
    // Frame in which context the exception clause is executed
    InterpMethodContextFrame *pFrame;
    // Set to true if the exception clause is a filter
    bool isFilter;
    // The exception object passed to the filter or catch clause
    OBJECTREF throwable;
};

void InterpExecMethod(InterpreterFrame *pInterpreterFrame, InterpMethodContextFrame *pFrame, InterpThreadContext *pThreadContext, ExceptionClauseArgs *pExceptionClauseArgs = NULL);

CallStubHeader *CreateNativeToInterpreterCallStub(InterpMethod* pInterpMethod);

#endif
