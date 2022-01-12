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
            "Successful compiles,Failing compiles,Missing compiles,Code bytes,Diffed code bytes\n"
            "%d,%d,%d,%lld,%lld\n",
            SuccessfulCompiles, FailingCompiles, MissingCompiles, NumCodeBytes, NumDiffedCodeBytes);
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

    std::vector<char> content(static_cast<size_t>(len.QuadPart));
    DWORD numRead;
    if (!ReadFile(file.get(), content.data(), static_cast<DWORD>(content.size()), &numRead, nullptr) || numRead != content.size())
    {
        return false;
    }
 
    int scanResult =
        sscanf_s(
            content.data(),
            "Successful compiles,Failing compiles,Missing compiles,Code bytes,Diffed code bytes\n"
            "%d,%d,%d,%lld,%lld\n",
            &metrics->SuccessfulCompiles, &metrics->FailingCompiles, &metrics->MissingCompiles, &metrics->NumCodeBytes, &metrics->NumDiffedCodeBytes);

    return scanResult == 5;
}

void MetricsSummary::AggregateFrom(const MetricsSummary& other)
{
    SuccessfulCompiles += other.SuccessfulCompiles;
    FailingCompiles += other.FailingCompiles;
    MissingCompiles += other.MissingCompiles;
    NumCodeBytes += other.NumCodeBytes;
    NumDiffedCodeBytes += other.NumDiffedCodeBytes;
}
