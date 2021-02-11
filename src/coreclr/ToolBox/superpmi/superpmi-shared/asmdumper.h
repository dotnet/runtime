//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef _ASMDumper
#define _ASMDumper

#include "methodcontext.h"
#include "compileresult.h"

class ASMDumper
{
public:
    static void DumpToFile(HANDLE hFile, MethodContext* mc, CompileResult* cr);
};

#endif