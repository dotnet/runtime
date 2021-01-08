//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
