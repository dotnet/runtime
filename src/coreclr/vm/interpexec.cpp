// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "interpexec.h"
#include <math.h>

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

    int32_t returnOffset, callArgsOffset;

MAIN_LOOP:
    while (true)
    {
        switch (*ip)
        {
            case INTOP_LDC_I4:
                LOCAL_VAR(ip[1], int32_t) = ip[2];
                ip += 3;
                break;
            case INTOP_LDC_I4_0:
                LOCAL_VAR(ip[1], int32_t) = 0;
                ip += 2;
                break;
            case INTOP_LDC_I8_0:
                LOCAL_VAR(ip[1], int64_t) = 0;
                ip += 2;
                break;
            case INTOP_RET:
                // Return stack slot sized value
                *(int64_t*)pFrame->pRetVal = LOCAL_VAR(ip[1], int64_t);
                goto EXIT_FRAME;
            case INTOP_RET_VT:
                memmove(pFrame->pRetVal, stack + ip[1], ip[2]);
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

            case INTOP_CONV_R_UN_I4:
                LOCAL_VAR(ip[1], double) = (double)LOCAL_VAR(ip[2], uint32_t);
                ip += 3;
                break;
            case INTOP_CONV_R_UN_I8:
                LOCAL_VAR(ip[1], double) = (double)LOCAL_VAR(ip[2], uint64_t);
                ip += 3;
                break;
            case INTOP_CONV_I1_I4:
                LOCAL_VAR(ip[1], int32_t) = (int8_t)LOCAL_VAR(ip[2], int32_t);
                ip += 3;
                break;
            case INTOP_CONV_I1_I8:
                LOCAL_VAR(ip[1], int32_t) = (int8_t)LOCAL_VAR(ip[2], int64_t);
                ip += 3;
                break;
            case INTOP_CONV_I1_R4:
                LOCAL_VAR(ip[1], int32_t) = (int8_t)(int32_t)LOCAL_VAR(ip[2], float);
                ip += 3;
                break;
            case INTOP_CONV_I1_R8:
                LOCAL_VAR(ip[1], int32_t) = (int8_t)(int32_t)LOCAL_VAR(ip[2], double);
                ip += 3;
                break;
            case INTOP_CONV_U1_I4:
                LOCAL_VAR(ip[1], int32_t) = (uint8_t)LOCAL_VAR(ip[2], int32_t);
                ip += 3;
                break;
            case INTOP_CONV_U1_I8:
                LOCAL_VAR(ip[1], int32_t) = (uint8_t)LOCAL_VAR(ip[2], int64_t);
                ip += 3;
                break;
            case INTOP_CONV_U1_R4:
                LOCAL_VAR(ip[1], int32_t) = (uint8_t)(uint32_t)LOCAL_VAR(ip[2], float);
                ip += 3;
                break;
            case INTOP_CONV_U1_R8:
                LOCAL_VAR(ip[1], int32_t) = (uint8_t)(uint32_t)LOCAL_VAR(ip[2], double);
                ip += 3;
                break;
            case INTOP_CONV_I2_I4:
                LOCAL_VAR(ip[1], int32_t) = (int16_t)LOCAL_VAR(ip[2], int32_t);
                ip += 3;
                break;
            case INTOP_CONV_I2_I8:
                LOCAL_VAR(ip[1], int32_t) = (int16_t)LOCAL_VAR(ip[2], int64_t);
                ip += 3;
                break;
            case INTOP_CONV_I2_R4:
                LOCAL_VAR(ip[1], int32_t) = (int16_t)(int32_t)LOCAL_VAR(ip[2], float);
                ip += 3;
                break;
            case INTOP_CONV_I2_R8:
                LOCAL_VAR(ip[1], int32_t) = (int16_t)(int32_t)LOCAL_VAR(ip[2], double);
                ip += 3;
                break;
            case INTOP_CONV_U2_I4:
                LOCAL_VAR(ip[1], int32_t) = (uint16_t)LOCAL_VAR(ip[2], int32_t);
                ip += 3;
                break;
            case INTOP_CONV_U2_I8:
                LOCAL_VAR(ip[1], int32_t) = (uint16_t)LOCAL_VAR(ip[2], int64_t);
                ip += 3;
                break;
            case INTOP_CONV_U2_R4:
                LOCAL_VAR(ip[1], int32_t) = (uint16_t)(uint32_t)LOCAL_VAR(ip[2], float);
                ip += 3;
                break;
            case INTOP_CONV_U2_R8:
                LOCAL_VAR(ip[1], int32_t) = (uint16_t)(uint32_t)LOCAL_VAR(ip[2], double);
                ip += 3;
                break;
            case INTOP_CONV_I4_R4:
                LOCAL_VAR(ip[1], int32_t) = (int32_t)LOCAL_VAR(ip[2], float);
                ip += 3;
                break;;
            case INTOP_CONV_I4_R8:
                LOCAL_VAR(ip[1], int32_t) = (int32_t)LOCAL_VAR(ip[2], double);
                ip += 3;
                break;;

            case INTOP_CONV_U4_R4:
            case INTOP_CONV_U4_R8:
                assert(0);
                break;

            case INTOP_CONV_I8_I4:
                LOCAL_VAR(ip[1], int64_t) = LOCAL_VAR(ip[2], int32_t);
                ip += 3;
                break;
            case INTOP_CONV_I8_U4:
                LOCAL_VAR(ip[1], int64_t) = (uint32_t)LOCAL_VAR(ip[2], int32_t);
                ip += 3;
                break;;
            case INTOP_CONV_I8_R4:
                LOCAL_VAR(ip[1], int64_t) = (int64_t)LOCAL_VAR(ip[2], float);
                ip += 3;
                break;
            case INTOP_CONV_I8_R8:
                LOCAL_VAR(ip[1], int64_t) = (int64_t)LOCAL_VAR(ip[2], double);
                ip += 3;
                break;
            case INTOP_CONV_R4_I4:
                LOCAL_VAR(ip[1], float) = (float)LOCAL_VAR(ip[2], int32_t);
                ip += 3;
                break;;
            case INTOP_CONV_R4_I8:
                LOCAL_VAR(ip[1], float) = (float)LOCAL_VAR(ip[2], int64_t);
                ip += 3;
                break;
            case INTOP_CONV_R4_R8:
                LOCAL_VAR(ip[1], float) = (float)LOCAL_VAR(ip[2], double);
                ip += 3;
                break;
            case INTOP_CONV_R8_I4:
                LOCAL_VAR(ip[1], double) = (double)LOCAL_VAR(ip[2], int32_t);
                ip += 3;
                break;
            case INTOP_CONV_R8_I8:
                LOCAL_VAR(ip[1], double) = (double)LOCAL_VAR(ip[2], int64_t);
                ip += 3;
                break;
            case INTOP_CONV_R8_R4:
                LOCAL_VAR(ip[1], double) = (double)LOCAL_VAR(ip[2], float);
                ip += 3;
                break;
            case INTOP_CONV_U8_R4:
            case INTOP_CONV_U8_R8:
                // TODO
                assert(0);
                break;

            case INTOP_SWITCH:
            {
                uint32_t val = LOCAL_VAR(ip[1], uint32_t);
                uint32_t n = ip[2];
                ip += 3;
                if (val < n)
                {
                    ip += val;
                    ip += *ip;
                }
                else
                {
                    ip += n;
                }
                break;
            }

            case INTOP_BR:
                ip += ip[1];
                break;

#define BR_UNOP(datatype, op)           \
    if (LOCAL_VAR(ip[1], datatype) op)  \
        ip += ip[2];                    \
    else \
        ip += 3;

            case INTOP_BRFALSE_I4:
                BR_UNOP(int32_t, == 0);
                break;
            case INTOP_BRFALSE_I8:
                BR_UNOP(int64_t, == 0);
                break;
            case INTOP_BRTRUE_I4:
                BR_UNOP(int32_t, != 0);
                break;
            case INTOP_BRTRUE_I8:
                BR_UNOP(int64_t, != 0);
                break;

#define BR_BINOP_COND(cond) \
    if (cond)               \
        ip += ip[3];        \
    else                    \
        ip += 4;

#define BR_BINOP(datatype, op) \
    BR_BINOP_COND(LOCAL_VAR(ip[1], datatype) op LOCAL_VAR(ip[2], datatype))

            case INTOP_BEQ_I4:
                BR_BINOP(int32_t, ==);
                break;
            case INTOP_BEQ_I8:
                BR_BINOP(int64_t, ==);
                break;
            case INTOP_BEQ_R4:
            {
                float f1 = LOCAL_VAR(ip[1], float);
                float f2 = LOCAL_VAR(ip[2], float);
                BR_BINOP_COND(!isunordered(f1, f2) && f1 == f2);
                break;
            }
            case INTOP_BEQ_R8:
            {
                double d1 = LOCAL_VAR(ip[1], double);
                double d2 = LOCAL_VAR(ip[2], double);
                BR_BINOP_COND(!isunordered(d1, d2) && d1 == d2);
                break;
            }
            case INTOP_BGE_I4:
                BR_BINOP(int32_t, >=);
                break;
            case INTOP_BGE_I8:
                BR_BINOP(int64_t, >=);
                break;
            case INTOP_BGE_R4:
            {
                float f1 = LOCAL_VAR(ip[1], float);
                float f2 = LOCAL_VAR(ip[2], float);
                BR_BINOP_COND(!isunordered(f1, f2) && f1 >= f2);
                break;
            }
            case INTOP_BGE_R8:
            {
                double d1 = LOCAL_VAR(ip[1], double);
                double d2 = LOCAL_VAR(ip[2], double);
                BR_BINOP_COND(!isunordered(d1, d2) && d1 >= d2);
                break;
            }
            case INTOP_BGT_I4:
                BR_BINOP(int32_t, >);
                break;
            case INTOP_BGT_I8:
                BR_BINOP(int64_t, >);
                break;
            case INTOP_BGT_R4:
            {
                float f1 = LOCAL_VAR(ip[1], float);
                float f2 = LOCAL_VAR(ip[2], float);
                BR_BINOP_COND(!isunordered(f1, f2) && f1 > f2);
                break;
            }
            case INTOP_BGT_R8:
            {
                double d1 = LOCAL_VAR(ip[1], double);
                double d2 = LOCAL_VAR(ip[2], double);
                BR_BINOP_COND(!isunordered(d1, d2) && d1 > d2);
                break;
            }
            case INTOP_BLT_I4:
                BR_BINOP(int32_t, <);
                break;
            case INTOP_BLT_I8:
                BR_BINOP(int64_t, <);
                break;
            case INTOP_BLT_R4:
            {
                float f1 = LOCAL_VAR(ip[1], float);
                float f2 = LOCAL_VAR(ip[2], float);
                BR_BINOP_COND(!isunordered(f1, f2) && f1 < f2);
                break;
            }
            case INTOP_BLT_R8:
            {
                double d1 = LOCAL_VAR(ip[1], double);
                double d2 = LOCAL_VAR(ip[2], double);
                BR_BINOP_COND(!isunordered(d1, d2) && d1 < d2);
                break;
            }
            case INTOP_BLE_I4:
                BR_BINOP(int32_t, <=);
                break;
            case INTOP_BLE_I8:
                BR_BINOP(int64_t, <=);
                break;
            case INTOP_BLE_R4:
            {
                float f1 = LOCAL_VAR(ip[1], float);
                float f2 = LOCAL_VAR(ip[2], float);
                BR_BINOP_COND(!isunordered(f1, f2) && f1 <= f2);
                break;
            }
            case INTOP_BLE_R8:
            {
                double d1 = LOCAL_VAR(ip[1], double);
                double d2 = LOCAL_VAR(ip[2], double);
                BR_BINOP_COND(!isunordered(d1, d2) && d1 <= d2);
                break;
            }
            case INTOP_BNE_UN_I4:
                BR_BINOP(uint32_t, !=);
                break;
            case INTOP_BNE_UN_I8:
                BR_BINOP(uint64_t, !=);
                break;
            case INTOP_BNE_UN_R4:
            {
                float f1 = LOCAL_VAR(ip[1], float);
                float f2 = LOCAL_VAR(ip[2], float);
                BR_BINOP_COND(isunordered(f1, f2) || f1 != f2);
                break;
            }
            case INTOP_BNE_UN_R8:
            {
                double d1 = LOCAL_VAR(ip[1], double);
                double d2 = LOCAL_VAR(ip[2], double);
                BR_BINOP_COND(isunordered(d1, d2) || d1 != d2);
                break;
            }
            case INTOP_BGE_UN_I4:
                BR_BINOP(uint32_t, >=);
                break;
            case INTOP_BGE_UN_I8:
                BR_BINOP(uint64_t, >=);
                break;
            case INTOP_BGE_UN_R4:
            {
                float f1 = LOCAL_VAR(ip[1], float);
                float f2 = LOCAL_VAR(ip[2], float);
                BR_BINOP_COND(isunordered(f1, f2) || f1 >= f2);
                break;
            }
            case INTOP_BGE_UN_R8:
            {
                double d1 = LOCAL_VAR(ip[1], double);
                double d2 = LOCAL_VAR(ip[2], double);
                BR_BINOP_COND(isunordered(d1, d2) || d1 >= d2);
                break;
            }
            case INTOP_BGT_UN_I4:
                BR_BINOP(uint32_t, >);
                break;
            case INTOP_BGT_UN_I8:
                BR_BINOP(uint64_t, >);
                break;
            case INTOP_BGT_UN_R4:
            {
                float f1 = LOCAL_VAR(ip[1], float);
                float f2 = LOCAL_VAR(ip[2], float);
                BR_BINOP_COND(isunordered(f1, f2) || f1 > f2);
                break;
            }
            case INTOP_BGT_UN_R8:
            {
                double d1 = LOCAL_VAR(ip[1], double);
                double d2 = LOCAL_VAR(ip[2], double);
                BR_BINOP_COND(isunordered(d1, d2) || d1 > d2);
                break;
            }
            case INTOP_BLE_UN_I4:
                BR_BINOP(uint32_t, <=);
                break;
            case INTOP_BLE_UN_I8:
                BR_BINOP(uint64_t, <=);
                break;
            case INTOP_BLE_UN_R4:
            {
                float f1 = LOCAL_VAR(ip[1], float);
                float f2 = LOCAL_VAR(ip[2], float);
                BR_BINOP_COND(isunordered(f1, f2) || f1 <= f2);
                break;
            }
            case INTOP_BLE_UN_R8:
            {
                double d1 = LOCAL_VAR(ip[1], double);
                double d2 = LOCAL_VAR(ip[2], double);
                BR_BINOP_COND(isunordered(d1, d2) || d1 <= d2);
                break;
            }
            case INTOP_BLT_UN_I4:
                BR_BINOP(uint32_t, <);
                break;
            case INTOP_BLT_UN_I8:
                BR_BINOP(uint64_t, <);
                break;
            case INTOP_BLT_UN_R4:
            {
                float f1 = LOCAL_VAR(ip[1], float);
                float f2 = LOCAL_VAR(ip[2], float);
                BR_BINOP_COND(isunordered(f1, f2) || f1 < f2);
                break;
            }
            case INTOP_BLT_UN_R8:
            {
                double d1 = LOCAL_VAR(ip[1], double);
                double d2 = LOCAL_VAR(ip[2], double);
                BR_BINOP_COND(isunordered(d1, d2) || d1 < d2);
                break;
            }

            case INTOP_ADD_I4:
                LOCAL_VAR(ip[1], int32_t) = LOCAL_VAR(ip[2], int32_t) + LOCAL_VAR(ip[3], int32_t);
                ip += 4;
                break;
            case INTOP_ADD_I8:
                LOCAL_VAR(ip[1], int64_t) = LOCAL_VAR(ip[2], int64_t) + LOCAL_VAR(ip[3], int64_t);
                ip += 4;
                break;
            case INTOP_ADD_R4:
                LOCAL_VAR(ip[1], float) = LOCAL_VAR(ip[2], float) + LOCAL_VAR(ip[3], float);
                ip += 4;
                break;
            case INTOP_ADD_R8:
                LOCAL_VAR(ip[1], double) = LOCAL_VAR(ip[2], double) + LOCAL_VAR(ip[3], double);
                ip += 4;
                break;

            case INTOP_SUB_I4:
                LOCAL_VAR(ip[1], int32_t) = LOCAL_VAR(ip[2], int32_t) - LOCAL_VAR(ip[3], int32_t);
                ip += 4;
                break;
            case INTOP_SUB_I8:
                LOCAL_VAR(ip[1], int64_t) = LOCAL_VAR(ip[2], int64_t) - LOCAL_VAR(ip[3], int64_t);
                ip += 4;
                break;
            case INTOP_SUB_R4:
                LOCAL_VAR(ip[1], float) = LOCAL_VAR(ip[2], float) - LOCAL_VAR(ip[3], float);
                ip += 4;
                break;
            case INTOP_SUB_R8:
                LOCAL_VAR(ip[1], double) = LOCAL_VAR(ip[2], double) - LOCAL_VAR(ip[3], double);
                ip += 4;
                break;

            case INTOP_MUL_I4:
                LOCAL_VAR(ip[1], int32_t) = LOCAL_VAR(ip[2], int32_t) * LOCAL_VAR(ip[3], int32_t);
                ip += 4;
                break;
            case INTOP_MUL_I8:
                LOCAL_VAR(ip[1], int64_t) = LOCAL_VAR(ip[2], int64_t) * LOCAL_VAR(ip[3], int64_t);
                ip += 4;
                break;
            case INTOP_MUL_R4:
                LOCAL_VAR(ip[1], float) = LOCAL_VAR(ip[2], float) * LOCAL_VAR(ip[3], float);
                ip += 4;
                break;
            case INTOP_MUL_R8:
                LOCAL_VAR(ip[1], double) = LOCAL_VAR(ip[2], double) * LOCAL_VAR(ip[3], double);
                ip += 4;
                break;

            case INTOP_SHL_I4:
                LOCAL_VAR(ip[1], int32_t) = LOCAL_VAR(ip[2], int32_t) << LOCAL_VAR(ip[3], int32_t);
                ip += 4;
                break;
            case INTOP_SHL_I8:
                LOCAL_VAR(ip[1], int64_t) = LOCAL_VAR(ip[2], int64_t) << LOCAL_VAR(ip[3], int32_t);
                ip += 4;
                break;
            case INTOP_SHR_I4:
                LOCAL_VAR(ip[1], int32_t) = LOCAL_VAR(ip[2], int32_t) >> LOCAL_VAR(ip[3], int32_t);
                ip += 4;
                break;
            case INTOP_SHR_I8:
                LOCAL_VAR(ip[1], int64_t) = LOCAL_VAR(ip[2], int64_t) >> LOCAL_VAR(ip[3], int32_t);
                ip += 4;
                break;
            case INTOP_SHR_UN_I4:
                LOCAL_VAR(ip[1], uint32_t) = LOCAL_VAR(ip[2], uint32_t) >> LOCAL_VAR(ip[3], int32_t);
                ip += 4;
                break;
            case INTOP_SHR_UN_I8:
                LOCAL_VAR(ip[1], uint64_t) = LOCAL_VAR(ip[2], uint64_t) >> LOCAL_VAR(ip[3], int32_t);
                ip += 4;
                break;

            case INTOP_NEG_I4:
                LOCAL_VAR(ip[1], int32_t) = - LOCAL_VAR(ip[2], int32_t);
                ip += 3;
                break;
            case INTOP_NEG_I8:
                LOCAL_VAR(ip[1], int64_t) = - LOCAL_VAR(ip[2], int64_t);
                ip += 3;
                break;
            case INTOP_NEG_R4:
                LOCAL_VAR(ip[1], float) = - LOCAL_VAR(ip[2], float);
                ip += 3;
                break;
            case INTOP_NEG_R8:
                LOCAL_VAR(ip[1], double) = - LOCAL_VAR(ip[2], double);
                ip += 3;
                break;
            case INTOP_NOT_I4:
                LOCAL_VAR(ip[1], int32_t) = ~ LOCAL_VAR(ip[2], int32_t);
                ip += 3;
                break;
            case INTOP_NOT_I8:
                LOCAL_VAR(ip[1], int64_t) = ~ LOCAL_VAR(ip[2], int64_t);
                ip += 3;
                break;

            case INTOP_AND_I4:
                LOCAL_VAR(ip[1], int32_t) = LOCAL_VAR(ip[2], int32_t) & LOCAL_VAR(ip[3], int32_t);
                ip += 4;
                break;
            case INTOP_AND_I8:
                LOCAL_VAR(ip[1], int64_t) = LOCAL_VAR(ip[2], int64_t) & LOCAL_VAR(ip[3], int64_t);
                ip += 4;
                break;
            case INTOP_OR_I4:
                LOCAL_VAR(ip[1], int32_t) = LOCAL_VAR(ip[2], int32_t) | LOCAL_VAR(ip[3], int32_t);
                ip += 4;
                break;
            case INTOP_OR_I8:
                LOCAL_VAR(ip[1], int64_t) = LOCAL_VAR(ip[2], int64_t) | LOCAL_VAR(ip[3], int64_t);
                ip += 4;
                break;
            case INTOP_XOR_I4:
                LOCAL_VAR(ip[1], int32_t) = LOCAL_VAR(ip[2], int32_t) ^ LOCAL_VAR(ip[3], int32_t);
                ip += 4;
                break;
            case INTOP_XOR_I8:
                LOCAL_VAR(ip[1], int64_t) = LOCAL_VAR(ip[2], int64_t) ^ LOCAL_VAR(ip[3], int64_t);
                ip += 4;
                break;

#define CMP_BINOP_FP(datatype, op, noOrderVal)      \
    do {                                            \
        datatype f1 = LOCAL_VAR(ip[2], datatype);   \
        datatype f2 = LOCAL_VAR(ip[3], datatype);   \
        if (isunordered(f1, f2))                    \
            LOCAL_VAR(ip[1], int32_t) = noOrderVal; \
        else                                        \
            LOCAL_VAR(ip[1], int32_t) = f1 op f2;   \
        ip += 4;                                    \
    } while (0)

            case INTOP_CEQ_I4:
                LOCAL_VAR(ip[1], int32_t) = LOCAL_VAR(ip[2], int32_t) == LOCAL_VAR(ip[3], int32_t);
                ip += 4;
                break;
            case INTOP_CEQ_I8:
                LOCAL_VAR(ip[1], int32_t) = LOCAL_VAR(ip[2], int64_t) == LOCAL_VAR(ip[3], int64_t);
                ip += 4;
                break;
            case INTOP_CEQ_R4:
                CMP_BINOP_FP(float, ==, 0);
                break;
            case INTOP_CEQ_R8:
                CMP_BINOP_FP(double, ==, 0);
                break;

            case INTOP_CGT_I4:
                LOCAL_VAR(ip[1], int32_t) = LOCAL_VAR(ip[2], int32_t) > LOCAL_VAR(ip[3], int32_t);
                ip += 4;
                break;
            case INTOP_CGT_I8:
                LOCAL_VAR(ip[1], int32_t) = LOCAL_VAR(ip[2], int64_t) > LOCAL_VAR(ip[3], int64_t);
                ip += 4;
                break;
            case INTOP_CGT_R4:
                CMP_BINOP_FP(float, >, 0);
                break;
            case INTOP_CGT_R8:
                CMP_BINOP_FP(double, >, 0);
                break;

            case INTOP_CGT_UN_I4:
                LOCAL_VAR(ip[1], int32_t) = LOCAL_VAR(ip[2], uint32_t) > LOCAL_VAR(ip[3], uint32_t);
                ip += 4;
                break;
            case INTOP_CGT_UN_I8:
                LOCAL_VAR(ip[1], int32_t) = LOCAL_VAR(ip[2], uint32_t) > LOCAL_VAR(ip[3], uint32_t);
                ip += 4;
                break;
            case INTOP_CGT_UN_R4:
                CMP_BINOP_FP(float, >, 1);
                break;
            case INTOP_CGT_UN_R8:
                CMP_BINOP_FP(double, >, 1);
                break;

            case INTOP_CLT_I4:
                LOCAL_VAR(ip[1], int32_t) = LOCAL_VAR(ip[2], int32_t) < LOCAL_VAR(ip[3], int32_t);
                ip += 4;
                break;
            case INTOP_CLT_I8:
                LOCAL_VAR(ip[1], int32_t) = LOCAL_VAR(ip[2], int64_t) < LOCAL_VAR(ip[3], int64_t);
                ip += 4;
                break;
            case INTOP_CLT_R4:
                CMP_BINOP_FP(float, <, 0);
                break;
            case INTOP_CLT_R8:
                CMP_BINOP_FP(double, <, 0);
                break;

            case INTOP_CLT_UN_I4:
                LOCAL_VAR(ip[1], int32_t) = LOCAL_VAR(ip[2], uint32_t) < LOCAL_VAR(ip[3], uint32_t);
                ip += 4;
                break;
            case INTOP_CLT_UN_I8:
                LOCAL_VAR(ip[1], int32_t) = LOCAL_VAR(ip[2], uint64_t) < LOCAL_VAR(ip[3], uint64_t);
                ip += 4;
                break;
            case INTOP_CLT_UN_R4:
                CMP_BINOP_FP(float, <, 1);
                break;
            case INTOP_CLT_UN_R8:
                CMP_BINOP_FP(double, <, 1);
                break;

            case INTOP_CALL:
            {
                size_t targetMethod = (size_t)pMethod->pDataItems[ip[3]];
                returnOffset = ip[1];
                callArgsOffset = ip[2];
                const int32_t *targetIp;

                if (targetMethod & INTERP_METHOD_DESC_TAG)
                {
                    // First execution of this call. Ensure target method is compiled and
                    // patch the data item slot with the actual method code.
                    MethodDesc *pMD = (MethodDesc*)(targetMethod & ~INTERP_METHOD_DESC_TAG);
                    PCODE code = pMD->GetNativeCode();
                    if (!code) {
                        GCX_PREEMP();
                        pMD->PrepareInitialCode(CallerGCMode::Coop);
                        code = pMD->GetNativeCode();
                    }
                    pMethod->pDataItems[ip[3]] = (void*)code;
                    targetIp = (const int32_t*)code;
                }
                else
                {
                    // At this stage in the implementation, we assume this is pointer to
                    // interpreter code. In the future, this should probably be tagged pointer
                    // for interpreter call or normal pointer for JIT/R2R call.
                    targetIp = (const int32_t*)targetMethod;
                }

                // Save current execution state once we return from called method
                pFrame->ip = ip + 4;

                // Allocate child frame.
                {
                    InterpMethodContextFrame *pChildFrame = pFrame->pNext;
                    if (!pChildFrame)
                    {
                        pChildFrame = (InterpMethodContextFrame*)alloca(sizeof(InterpMethodContextFrame));
                        pChildFrame->pNext = NULL;
                        pFrame->pNext = pChildFrame;
                    }
                    pChildFrame->ReInit(pFrame, targetIp, stack + returnOffset, stack + callArgsOffset);
                    pFrame = pChildFrame;
                }
                assert (((size_t)pFrame->pStack % INTERP_STACK_ALIGNMENT) == 0);

                // Set execution state for the new frame
                pMethod = *(InterpMethod**)pFrame->startIp;
                stack = pFrame->pStack;
                ip = pFrame->startIp + sizeof(InterpMethod*) / sizeof(int32_t);
                pThreadContext->pStackPointer = stack + pMethod->allocaSize;
                break;
            }
            case INTOP_FAILFAST:
                assert(0);
                break;
            default:
                assert(0);
                break;
        }
    }

EXIT_FRAME:
    if (pFrame->pParent && pFrame->pParent->ip)
    {
        // Return to the main loop after a non-recursive interpreter call
        pFrame = pFrame->pParent;
        ip = pFrame->ip;
        stack = pFrame->pStack;
        pMethod = *(InterpMethod**)pFrame->startIp;
        pFrame->ip = NULL;

        pThreadContext->pStackPointer = pFrame->pStack + pMethod->allocaSize;
        goto MAIN_LOOP;
    }

    pThreadContext->pStackPointer = pFrame->pStack;
}

#endif // FEATURE_INTERPRETER
