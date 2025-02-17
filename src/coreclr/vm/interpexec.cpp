// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "interpexec.h"

#ifdef FEATURE_INTERPRETER

thread_local InterpThreadContext *t_pThreadContext = NULL;

InterpThreadContext* InterpGetThreadContext()
{
    InterpThreadContext *threadContext = t_pThreadContext;

    if (!threadContext)
    {
        threadContext = new InterpThreadContext;
        // FIXME VirtualAlloc/mmap with INTERP_STACK_ALIGNMENT alignment
        threadContext->pStackStart = threadContext->pStackPointer = (int8_t*)malloc(INTERP_STACK_SIZE);
        threadContext->pStackEnd = threadContext->pStackStart + INTERP_STACK_SIZE;

        t_pThreadContext = threadContext;
        return threadContext;
    }
    else
    {
        return threadContext;
    }
}

#define LOCAL_VAR_ADDR(offset,type) ((type*)(stack + (offset)))
#define LOCAL_VAR(offset,type) (*LOCAL_VAR_ADDR(offset, type))

void InterpExecMethod(InterpMethodContextFrame *pFrame, InterpThreadContext *pThreadContext)
{
    const int32_t *ip;
    int8_t *stack;

    InterpMethod *pMethod = *(InterpMethod**)pFrame->startIp;
    pThreadContext->pStackPointer = pFrame->pStack + pMethod->allocaSize;
    ip = pFrame->startIp + sizeof(InterpMethod*) / sizeof(int32_t);
    stack = pFrame->pStack;

    while (true)
    {
        switch (*ip)
        {
            case INTOP_LDC_I4:
                LOCAL_VAR(ip[1], int32_t) = ip[2];
                ip += 3;
                break;
            case INTOP_RET:
                // Return stack slot sized value
                *(int64_t*)pFrame->pRetVal = LOCAL_VAR(ip[1], int64_t);
                goto EXIT_FRAME;
            case INTOP_RET_VOID:
                goto EXIT_FRAME;

#define MOV(argtype1,argtype2) \
    LOCAL_VAR(ip [1], argtype1) = LOCAL_VAR(ip [2], argtype2); \
    ip += 3;
            // When loading from a local, we might need to sign / zero extend to 4 bytes
            // which is our minimum "register" size in interp. They are only needed when
            // the address of the local is taken and we should try to optimize them out
            // because the local can't be propagated.
            case INTOP_MOV_I4_I1: MOV(int32_t, int8_t); break;
            case INTOP_MOV_I4_U1: MOV(int32_t, uint8_t); break;
            case INTOP_MOV_I4_I2: MOV(int32_t, int16_t); break;
            case INTOP_MOV_I4_U2: MOV(int32_t, uint16_t); break;
            // Normal moves between vars
            case INTOP_MOV_4: MOV(int32_t, int32_t); break;
            case INTOP_MOV_8: MOV(int64_t, int64_t); break;

            case INTOP_MOV_VT:
                memmove(stack + ip[1], stack + ip[2], ip[3]);
                ip += 4;
                break;

            default:
                assert(0);
                break;
        }
    }

EXIT_FRAME:
    pThreadContext->pStackPointer = pFrame->pStack;
}

#endif // FEATURE_INTERPRETER
