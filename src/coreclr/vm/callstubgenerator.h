// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CALLSTUBGENERATOR_H
#define CALLSTUBGENERATOR_H

#include "callingconvention.h"

class MethodDesc;
class AllocMemTracker;

// This is a header for a call stub that translates arguments from the interpreter stack to the CPU registers and native
// stack, invokes the target method, and translates the return value back to the interpreter stack.
struct CallStubHeader
{
    typedef void (*InvokeFunctionPtr)(PCODE *routines, int8_t*pArgs, int8_t*pRet, int totalStackSize);

    // Number of routines in the Routines array. The last one is the target method to call.
    int NumRoutines;
    // Total stack size used for the arguments.
    int TotalStackSize;
    // This is a pointer to a helper function that invokes the target method. There are several
    // versions of this function, depending on the return type of the target method.
    InvokeFunctionPtr Invoke;
    // This is an array of routines that translate the arguments from the interpreter stack to the CPU registers and native stack.
    PCODE Routines[0];

    CallStubHeader(int numRoutines, PCODE *pRoutines, int totalStackSize, InvokeFunctionPtr pInvokeFunction)
    {
        LIMITED_METHOD_CONTRACT;

        NumRoutines = numRoutines;
        TotalStackSize = totalStackSize;
        Invoke = pInvokeFunction;

        memcpy(Routines, pRoutines, NumRoutines * sizeof(PCODE));
    }

    // Set the address of the target method to call.
    void SetTarget(PCODE target)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(target != 0);
        Routines[NumRoutines - 1] = target;
    }
};

// This class generates the call stub for a given method. It uses the calling convention of the target CPU to determine
// how to translate the arguments from the interpreter stack to the CPU registers and native stack.
class CallStubGenerator
{
    // When the m_r1, m_x1 or m_s1 are set to NoRange, it means that there is no active range of registers or stack arguments.
    static const int NoRange = -1;

    // Current sequential range of general purpose registers used to pass arguments.
    int m_r1;
    int m_r2;
    // Current sequential range of floating point registers used to pass arguments.
    int m_x1;
    int m_x2;
    // Current sequential range of offsets of stack arguments used to pass arguments.
    int m_s1;
    int m_s2;
    // The index of the next routine to store in the Routines array.
    int m_routineIndex;
    // The total stack size used for the arguments.
    int m_totalStackSize;

    // Process the argument described by argLocDesc. This function is called for each argument in the method signature.
    void ProcessArgument(ArgIterator& argIt, ArgLocDesc& argLocDesc, PCODE *pRoutines);
public:
    // Generate the call stub for the given method.
    CallStubHeader *GenerateCallStub(MethodDesc *pMD, AllocMemTracker *pamTracker);
};

#endif // CALLSTUBGENERATOR_H
