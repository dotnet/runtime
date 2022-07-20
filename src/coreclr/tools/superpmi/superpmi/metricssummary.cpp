// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "metricssummary.h"
#include "logging.h"

struct HandleCloser
{
    void operator()(HANDLE hFile)
    {
        CloseHandle(hFile);
    }
};

struct FileHandleWrapper
{
    FileHandleWrapper(HANDLE hFile)
        : hFile(hFile)
    {
    }

    ~FileHandleWrapper()
    {
        CloseHandle(hFile);
    }

    HANDLE get() { return hFile; }

private:
    HANDLE hFile;
};

bool MetricsSummary::SaveToFile(const char* path)
{
    FileHandleWrapper file(CreateFile(path, GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr));
    if (file.get() == INVALID_HANDLE_VALUE)
    {
        return false;
    }

    char buffer[4096];
    int len =
        sprintf_s(
            buffer, sizeof(buffer),
            "Successful compiles,Failing compiles,Missing compiles,Code bytes,Diffed code bytes,Executed instructions,Diff executed instructions\n"
            "%d,%d,%d,%lld,%lld,%lld,%lld\n",
            SuccessfulCompiles,
            FailingCompiles,
            MissingCompiles,
            NumCodeBytes,
            NumDiffedCodeBytes,
            NumExecutedInstructions,
            NumDiffExecutedInstructions);
    DWORD numWritten;
    if (!WriteFile(file.get(), buffer, static_cast<DWORD>(len), &numWritten, nullptr) || numWritten != static_cast<DWORD>(len))
    {
        return false;
    }

    return true;
}

bool MetricsSummary::LoadFromFile(const char* path, MetricsSummary* metrics)
{
    FileHandleWrapper file(CreateFile(path, GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr));
    if (file.get() == INVALID_HANDLE_VALUE)
    {
        return false;
    }

    LARGE_INTEGER len;
    if (!GetFileSizeEx(file.get(), &len))
    {
        return false;
    }

    DWORD stringLen = static_cast<DWORD>(len.QuadPart);
    std::vector<char> content(stringLen + 1);
    DWORD numRead;
    if (!ReadFile(file.get(), content.data(), stringLen, &numRead, nullptr) || numRead != stringLen)
    {
        return false;
    }

    content[stringLen] = '\0';
 
    int scanResult =
        sscanf_s(
            content.data(),
            "Successful compiles,Failing compiles,Missing compiles,Code bytes,Diffed code bytes,Executed instructions,Diff executed instructions\n"
            "%d,%d,%d,%lld,%lld,%lld,%lld\n",
            &metrics->SuccessfulCompiles,
            &metrics->FailingCompiles,
            &metrics->MissingCompiles,
            &metrics->NumCodeBytes,
            &metrics->NumDiffedCodeBytes,
            &metrics->NumExecutedInstructions,
            &metrics->NumDiffExecutedInstructions);

    return scanResult == 7;
}

void MetricsSummary::AggregateFrom(const MetricsSummary& other)
{
    SuccessfulCompiles += other.SuccessfulCompiles;
    FailingCompiles += other.FailingCompiles;
    MissingCompiles += other.MissingCompiles;
    NumCodeBytes += other.NumCodeBytes;
    NumDiffedCodeBytes += other.NumDiffedCodeBytes;
    NumExecutedInstructions += other.NumExecutedInstructions;
    NumDiffExecutedInstructions += other.NumDiffExecutedInstructions;
}
