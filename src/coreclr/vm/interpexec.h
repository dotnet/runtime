// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTERPEXEC_H_
#define _INTERPEXEC_H_

#include <interpretershared.h>
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

enum class InterpreterFrameReporting
{
    Normal = 0,
#ifndef FEATURE_REUSE_INTERPRETER_STACK_FOR_NORMAL_FUNCLETS
    FuncletReportGlobals = 1,
    FuncletNoReportGlobals = 2,

    Mask = 3
#endif // FEATURE_REUSE_INTERPRETER_STACK_FOR_NORMAL_FUNCLETS
};

struct InterpMethodContextFrame
{
    PTR_InterpMethodContextFrame pParent = 0;
    PTR_InterpByteCodeStart startIp = 0; // from startIp we can obtain InterpMethod and MethodDesc
    int8_t *pStack = 0;
private:
    int8_t *pRetVal = 0; // If the low bit is set on pRetVal, then Frame represents a funclet
public:
    const int32_t *ip = 0; // This ip is updated only when execution can leave the frame
    PTR_InterpMethodContextFrame pNext = 0;

    bool IsFuncletFrame()
    {
        LIMITED_METHOD_CONTRACT;
#ifdef FEATURE_REUSE_INTERPRETER_STACK_FOR_NORMAL_FUNCLETS
        return false;
#else
        return ((size_t)pRetVal & (size_t)InterpreterFrameReporting::Mask) != (size_t)InterpreterFrameReporting::Normal;
#endif // FEATURE_REUSE_INTERPRETER_STACK_FOR_NORMAL_FUNCLETS
    }
    bool ShouldReportGlobals()
    {
        LIMITED_METHOD_CONTRACT;
#ifdef FEATURE_REUSE_INTERPRETER_STACK_FOR_NORMAL_FUNCLETS
        return true;
#else
        switch ((InterpreterFrameReporting)((size_t)pRetVal & (size_t)InterpreterFrameReporting::Mask))
        {
        case InterpreterFrameReporting::FuncletReportGlobals:
        case InterpreterFrameReporting::Normal:
            return true;
        case InterpreterFrameReporting::FuncletNoReportGlobals:
            return false;
        default:
            assert(false);
            return false;
        }
#endif // FEATURE_REUSE_INTERPRETER_STACK_FOR_NORMAL_FUNCLETS
    }

    int8_t* GetRetValAddr()
    {
        LIMITED_METHOD_CONTRACT;
#ifdef FEATURE_REUSE_INTERPRETER_STACK_FOR_NORMAL_FUNCLETS
        return pRetVal;
#else
        return (int8_t*)((size_t)pRetVal & ~(size_t)InterpreterFrameReporting::Mask);
#endif // FEATURE_REUSE_INTERPRETER_STACK_FOR_NORMAL_FUNCLETS
    }

    int8_t* GetRetValAddr_KnownNormalReporting()
    {
        LIMITED_METHOD_CONTRACT;
        assert(!IsFuncletFrame());
        return pRetVal;
    }

#ifndef DACCESS_COMPILE
    void ReInit(InterpMethodContextFrame *pParent, InterpByteCodeStart* startIp, int8_t *pRetVal, InterpreterFrameReporting reporting, int8_t *pStack)
    {
        this->pParent = pParent;
        this->startIp = startIp;
#ifdef FEATURE_REUSE_INTERPRETER_STACK_FOR_NORMAL_FUNCLETS
        this->pRetVal = pRetVal;
#else
        this->pRetVal = (int8_t*)((size_t)pRetVal | (size_t)reporting);
#endif // FEATURE_REUSE_INTERPRETER_STACK_FOR_NORMAL_FUNCLETS
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
