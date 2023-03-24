// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "metricssummary.h"
#include "logging.h"
#include "fileio.h"

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

bool MetricsSummaries::SaveToFile(const char* path)
{
    FileWriter file;
    if (!FileWriter::CreateNew(path, &file))
    {
        return false;
    }

    if (!file.Printf(
        "Successful compiles,Failing compiles,Missing compiles,Contexts with diffs,"
        "Code bytes,Diffed code bytes,Executed instructions,Diff executed instructions,Name\n"))
    {
        return false;
    }

    return
        WriteRow(file, "Overall", Overall) &&
        WriteRow(file, "MinOpts", MinOpts) &&
        WriteRow(file, "FullOpts", FullOpts);
}

bool MetricsSummaries::WriteRow(FileWriter& fw, const char* name, const MetricsSummary& summary)
{
    return
        fw.Printf(
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
    FileLineReader reader;
    if (!FileLineReader::Open(path, &reader))
    {
        return false;
    }

    if (!reader.AdvanceLine())
    {
        return false;
    }

    *metrics = MetricsSummaries();
    bool result = true;
    while (reader.AdvanceLine())
    {
        MetricsSummary summary;

        char name[32];
        int scanResult =
            sscanf_s(
                reader.GetCurrentLine(),
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
