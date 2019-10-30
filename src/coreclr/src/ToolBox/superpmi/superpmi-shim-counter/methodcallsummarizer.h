//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
