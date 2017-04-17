//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//----------------------------------------------------------
// verbILDump.h - verb that attempts to dump the raw IL for a MC
//----------------------------------------------------------
#ifndef _verbILDump
#define _verbILDump

#include "methodcontext.h"

class verbILDump
{
public:
    static int DoWork(const char* nameOfInput1, int indexCount, const int* indexes);
};

void DumpPrimToConsoleBare(MethodContext* mc, CorInfoType prim, DWORDLONG classHandle);
void DumpSigToConsoleBare(MethodContext* mc, CORINFO_SIG_INFO* pSig);
char* DumpAttributeToConsoleBare(DWORD attribute);

#endif
