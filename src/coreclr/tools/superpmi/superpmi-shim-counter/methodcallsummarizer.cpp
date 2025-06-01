// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "methodcallsummarizer.h"
#include "logging.h"
#include "spmiutil.h"
#include <dn-stdio.h>

MethodCallSummarizer::MethodCallSummarizer(WCHAR* logPath)
{
    numNames = 0;
    names    = nullptr;
    counts   = nullptr;

    const WCHAR* fileName  = GetCommandLineW();
    const WCHAR* extension = W(".csv");

    dataFileName = GetResultFileName(logPath, fileName, extension);
}

MethodCallSummarizer::~MethodCallSummarizer()
{
    delete [] dataFileName;
    delete [] counts;
    for (int i = 0; i < numNames; i++)
    {
        delete [] names[i];
    }
    delete [] names;
}

// lots of ways will be faster.. this happens to be decently simple and good enough for the task at hand and nicely
// sorts the output. in this approach the most commonly added items are at the top of the list... 60% landed in the
// first
// three slots in short runs
void MethodCallSummarizer::AddCall(const char* name)
{
    // if we can find it already in our list, increment the count
    for (int i = 0; i < numNames; i++)
    {
        if (strcmp(name, names[i]) == 0)
        {
            counts[i]++;
            for (i = 1; i < numNames; i++)
                if (counts[i] > counts[i - 1])
                {
                    unsigned int tempui = counts[i - 1];
                    counts[i - 1]       = counts[i];
                    counts[i]           = tempui;
                    char* tempc         = names[i - 1];
                    names[i - 1]        = names[i];
                    names[i]            = tempc;
                }
            return;
        }
    }

    // else we didn't find it, so add it
    char**        tnames  = names;
    unsigned int* tcounts = counts;

    names = new char*[numNames + 1];
    if (tnames != nullptr)
    {
        memcpy(names, tnames, numNames * sizeof(char*));
        delete [] tnames;
    }

    size_t tlen     = strlen(name);
    names[numNames] = new char[tlen + 1];
    memcpy(names[numNames], name, tlen + 1);

    counts = new unsigned int[numNames + 1];
    if (tcounts != nullptr)
    {
        memcpy(counts, tcounts, numNames * sizeof(unsigned int));
        delete [] tcounts;
    }
    counts[numNames] = 1;

    numNames++;
}

void MethodCallSummarizer::SaveTextFile()
{
    FILE* fp;
    if (fopen_u16(&fp, dataFileName, W("w")) != 0)
    {
        LogError("Couldn't open file '%ws': error %d", dataFileName, errno);
        return;
    }

    fprintf_s(fp, "FunctionName,Count\n");

    for (int i = 0; i < numNames; i++)
    {
        fprintf_s(fp, "%s,%u\n", names[i], counts[i]);
    }
    fclose(fp);
}
