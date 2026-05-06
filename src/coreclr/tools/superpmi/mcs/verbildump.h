// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
