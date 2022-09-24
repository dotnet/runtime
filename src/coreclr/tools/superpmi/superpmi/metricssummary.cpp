// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "metricssummary.h"
#include "logging.h"

void MetricsSummary::AggregateFrom(const MetricsSummary& other)
{
    SuccessfulCompiles += other.SuccessfulCompiles;
    FailingCompiles += other.FailingCompiles;
    MissingCompiles += other.MissingCompiles;
    NumContextsWithDiffs += other.NumContextsWithDiffs;
    NumCodeBytes += other.NumCodeBytes;
    NumDiffedCodeBytes += other.NumDiffedCodeBytes;
    NumExecutedInstructions += other.NumExecutedInstructions;
    NumDiffExecutedInstructions += other.NumDiffExecutedInstructions;
}

void MetricsSummaries::AggregateFrom(const MetricsSummaries& other)
{
    Overall.AggregateFrom(other.Overall);
    MinOpts.AggregateFrom(other.MinOpts);
    FullOpts.AggregateFrom(other.FullOpts);
}

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

static bool FilePrintf(HANDLE hFile, const char* fmt, ...)
{
    va_list args;
    va_start(args, fmt);

    char buffer[4096];
    int len = vsprintf_s(buffer, ARRAY_SIZE(buffer), fmt, args);
    DWORD numWritten;
    bool result =
        WriteFile(hFile, buffer, static_cast<DWORD>(len), &numWritten, nullptr) && (numWritten == static_cast<DWORD>(len));

    va_end(args);

    return result;
}

bool MetricsSummaries::SaveToFile(const char* path)
{
    FileHandleWrapper file(CreateFile(path, GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, nullptr));
    if (file.get() == INVALID_HANDLE_VALUE)
    {
        return false;
    }

    if (!FilePrintf(
        file.get(),
        "Successful compiles,Failing compiles,Missing compiles,Contexts with diffs,"
        "Code bytes,Diffed code bytes,Executed instructions,Diff executed instructions,Name\n"))
    {
        return false;
    }

    return
        WriteRow(file.get(), "Overall", Overall) &&
        WriteRow(file.get(), "MinOpts", MinOpts) &&
        WriteRow(file.get(), "FullOpts", FullOpts);
}

bool MetricsSummaries::WriteRow(HANDLE hFile, const char* name, const MetricsSummary& summary)
{
    return
        FilePrintf(
            hFile,
            "%d,%d,%d,%d,%lld,%lld,%lld,%lld,%s\n",
            summary.SuccessfulCompiles,
            summary.FailingCompiles,
            summary.MissingCompiles,
            summary.NumContextsWithDiffs,
            summary.NumCodeBytes,
            summary.NumDiffedCodeBytes,
            summary.NumExecutedInstructions,
            summary.NumDiffExecutedInstructions,
            name);
}

bool MetricsSummaries::LoadFromFile(const char* path, MetricsSummaries* metrics)
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
    std::vector<char> content(stringLen);
    DWORD numRead;
    if (!ReadFile(file.get(), content.data(), stringLen, &numRead, nullptr) || (numRead != stringLen))
    {
        return false;
    }

    std::vector<char> line;
    size_t index = 0;
    auto nextLine = [&line, &content, &index]()
    {
        size_t end = index;
        while ((end < content.size()) && (content[end] != '\r') && (content[end] != '\n'))
        {
            end++;
        }

        line.resize(end - index + 1);
        memcpy(line.data(), &content[index], end - index);
        line[end - index] = '\0';

        index = end;
        if ((index < content.size()) && (content[index] == '\r'))
            index++;
        if ((index < content.size()) && (content[index] == '\n'))
            index++;
    };

    *metrics = MetricsSummaries();
    nextLine();
    bool result = true;
    while (index < content.size())
    {
        nextLine();
        MetricsSummary summary;

        char name[32];
        int scanResult =
            sscanf_s(
                line.data(),
                "%d,%d,%d,%d,%lld,%lld,%lld,%lld,%s",
                &summary.SuccessfulCompiles,
                &summary.FailingCompiles,
                &summary.MissingCompiles,
                &summary.NumContextsWithDiffs,
                &summary.NumCodeBytes,
                &summary.NumDiffedCodeBytes,
                &summary.NumExecutedInstructions,
                &summary.NumDiffExecutedInstructions,
                name, (unsigned)sizeof(name));

        if (scanResult == 9)
        {
            MetricsSummary* tarSummary = nullptr;
            if (strcmp(name, "Overall") == 0)
                metrics->Overall = summary;
            else if (strcmp(name, "MinOpts") == 0)
                metrics->MinOpts = summary;
            else if (strcmp(name, "FullOpts") == 0)
                metrics->FullOpts = summary;
            else
                result = false;
        }
        else
        {
            result = false;
        }
    }

    return result;
}
