// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CALLSTUBGENERATOR_H
#define CALLSTUBGENERATOR_H

#include "callingconvention.h"

class MethodDesc;

struct CallStubHeader
{
    int NumRoutines;
    int TotalStackSize;
    void (*Invoke)(PCODE*, int8_t*, int8_t*, int);
    PCODE Routines[0];

    void SetTarget(PCODE target)
    {
        Routines[NumRoutines - 1] = target;
    }
};

class CallStubGenerator
{
    static const int NO_RANGE = -1;

    int m_r1;
    int m_r2;
    int m_x1;
    int m_x2;
    int m_s1;
    int m_s2;
    int m_routineIndex;
    int m_totalStackSize;
    CallStubHeader *m_pHeader;

    void ProcessArgument(ArgIterator& argIt, ArgLocDesc& argLocDesc);
public:
    CallStubHeader *GenerateCallStub(MethodDesc *pMD);
    static void FreeCallStub(CallStubHeader *pHeader);
};

#endif // CALLSTUBGENERATOR_H
