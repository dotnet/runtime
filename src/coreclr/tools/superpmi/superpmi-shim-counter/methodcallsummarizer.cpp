// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "methodcallsummarizer.h"
#include "logging.h"
#include "spmiutil.h"

MethodCallSummarizer::MethodCallSummarizer(const std::string& logPath)
{
    dataFileName = GetResultFileName(logPath, GetProcessCommandLine(), ".csv");
}

// Use ordered map to make the most commonly added items are at the top of the list...
// 60% landed in the first three slots in short runs
void MethodCallSummarizer::AddCall(const char* name)
{
    // std::map initialized integer values to zero
    namesAndCounts[std::string(name)]++;
}

void MethodCallSummarizer::SaveTextFile()
{
    FILE* fp = fopen(dataFileName.c_str(), "w");
    if (fp == NULL)
    {
        LogError("Couldn't open file '%s': error %d", dataFileName.c_str(), errno);
        return;
    }

    fprintf(fp, "FunctionName,Count\n");

    for (auto& elem : namesAndCounts)
    {
        fprintf(fp, "%s,%d\n", elem.first.c_str(), elem.second);
    }

    fclose(fp);
}

MethodCallSummarizer::~MethodCallSummarizer()
{
    SaveTextFile();
}
