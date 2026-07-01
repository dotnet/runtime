// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _ASMDumper
#define _ASMDumper

#include "methodcontext.h"
#include "compileresult.h"
#include "spmiutil.h"

class ASMDumper
{
public:
    static void DumpToFile(FILE* fp, MethodContext* mc, CompileResult* cr);
};

#endif
