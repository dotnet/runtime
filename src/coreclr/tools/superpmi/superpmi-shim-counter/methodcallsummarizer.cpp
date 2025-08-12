// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "methodcallsummarizer.h"
#include "logging.h"
#include "spmiutil.h"
#include <fstream>

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
    try
    {
        std::ofstream outFile(dataFileName);
        outFile << "FunctionName,Count" << std::endl;

        for (auto& elem : namesAndCounts)
        {
            outFile << elem.first << "," << elem.second << std::endl;
        }
    }
    catch (std::exception& ex)
    {
        LogError("Couldn't write file '%s': %s", dataFileName.c_str(), ex.what());
    }
}

MethodCallSummarizer::~MethodCallSummarizer()
{
    SaveTextFile();
}
