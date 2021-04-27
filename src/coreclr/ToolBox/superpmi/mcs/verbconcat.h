// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// verbConcat.h - verb that concatenates two files
//----------------------------------------------------------
#ifndef _verbConcat
#define _verbConcat

class verbConcat
{
public:
    static int DoWork(const char* nameOfFile1, const char* nameOfFile2);
};
#endif
