// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//-----------------------------------------------------------------------------
// MethodStatsEmitter.h - Emits useful method stats for compiled methods for analysis
//-----------------------------------------------------------------------------
#ifndef _MethodStatsEmitter
#define _MethodStatsEmitter

#include "methodcontext.h"
#include "jitinstance.h"

class MethodStatsEmitter
{

private:
    char*  statsTypes;
    HANDLE hStatsFile;

public:
    MethodStatsEmitter(char* nameOfInput);
    ~MethodStatsEmitter();

    void Emit(int methodNumber, MethodContext* mc, ULONGLONG firstTime, ULONGLONG secondTime);
    void SetStatsTypes(char* types);
};

#endif
