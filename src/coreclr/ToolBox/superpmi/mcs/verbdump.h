// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// verbDump.h - verb that Dumps a MC file
//----------------------------------------------------------
#ifndef _verbDump
#define _verbDump

class verbDump
{
public:
    static int DoWork(const char* nameofInput, int indexCount, const int* indexes, bool simple);
};
#endif
