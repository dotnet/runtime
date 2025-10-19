// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _MethodCallSummarizer
#define _MethodCallSummarizer

#include <string>
#include <map>

class MethodCallSummarizer
{
public:
    MethodCallSummarizer(const std::string& name);
    void AddCall(const char* name);
    ~MethodCallSummarizer();

private:
    std::map<std::string, int> namesAndCounts;
    std::string                dataFileName;

    void SaveTextFile();
};

#endif
