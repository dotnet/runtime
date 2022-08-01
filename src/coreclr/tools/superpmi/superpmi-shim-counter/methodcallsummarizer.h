// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _MethodCallSummarizer
#define _MethodCallSummarizer

class MethodCallSummarizer
{
public:
    MethodCallSummarizer(WCHAR* name);
    ~MethodCallSummarizer();
    void AddCall(const char* name);
    void SaveTextFile();

private:
    char**        names;
    unsigned int* counts;
    int           numNames;
    WCHAR*        dataFileName;
};

#endif
