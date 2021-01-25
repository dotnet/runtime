//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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