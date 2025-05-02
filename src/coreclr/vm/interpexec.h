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
        float f;
        double d;
        void *o;
        void *p;
    } data;
};

struct InterpMethodContextFrame
{
    PTR_InterpMethodContextFrame pParent;
    const int32_t *startIp; // from startIp we can obtain InterpMethod and MethodDesc
    int8_t *pStack;
    int8_t *pRetVal;
    const int32_t *ip; // This ip is updated only when execution can leave the frame
    PTR_InterpMethodContextFrame pNext;

#ifndef DACCESS_COMPILE
    void ReInit(InterpMethodContextFrame *pParent, const int32_t *startIp, int8_t *pRetVal, int8_t *pStack)
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
    int8_t *pStackStart;
    int8_t *pStackEnd;

    // This stack pointer is the highest stack memory that can be used by the current frame. This does not
    // change throughout the execution of a frame and it is essentially the upper limit of the execution
    // stack pointer. It is needed when re-entering interp, to know from which address we can start using
    // stack, and also needed for the GC to be able to scan the stack.
    int8_t *pStackPointer;

    FrameDataAllocator frameDataAllocator;

    InterpThreadContext();
    ~InterpThreadContext();
};

//
// This overloaded function serves two purposes:
//
// It could be used to call a function, where throwable, pHandler and handlerFrame is null, or
// It could be used to call a funclet, where throwable should be the exception object, ip is the bytecode offset of the handler,
//
// In the case of calling a function, it will return nullptr
// In the case of catch, it will return the bytecode address where the execution should resume, or
// In the case of filter, it will return the decision to either execute on the current handler or continue searching for another handler.
//
DWORD_PTR ExecuteInterpretedCode(TransitionBlock* pTransitionBlock, TADDR byteCodeAddr, OBJECTREF throwable, void* pHandler, InterpMethodContextFrame* handlerFrame, bool isFilter);

void* InterpExecMethod(InterpreterFrame *pInterpreterFrame, InterpMethodContextFrame *pFrame, OBJECTREF throwable, const int32_t* ip, int8_t *frame, InterpThreadContext *pThreadContext);

#endif
