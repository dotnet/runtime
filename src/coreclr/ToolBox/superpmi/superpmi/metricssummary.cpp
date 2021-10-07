// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "metricssummary.h"
#include "logging.h"

#include <fstream>
#include <inttypes.h>

bool MetricsSummary::SaveToFile(const char* path)
{
    FILE* file = fopen(path, "w");
    if (file == nullptr)
    {
        return false;
    }

    fprintf(file, "Successful compiles,Failing compiles,Code bytes\n");
    fprintf(file, "%d,%d,%" PRId64 "\n", SuccessfulCompiles, FailingCompiles, NumCodeBytes);
    fclose(file);
    return true;
}

bool MetricsSummary::LoadFromFile(const char* path, MetricsSummary* result)
{
    FILE* file = fopen(path, "r");
    if (file == nullptr)
    {
        return false;
    }

    fscanf(file, "Successful compiles,Failing compiles,Code bytes\n");
    if (fscanf(file, "%d,%d,%" SCNd64 "\n", &result->SuccessfulCompiles, &result->FailingCompiles, &result->NumCodeBytes) != 3)
    {
        return false;
    }

    fclose(file);
    return true;
}

void MetricsSummary::AggregateFrom(const MetricsSummary& other)
{
    SuccessfulCompiles += other.SuccessfulCompiles;
    FailingCompiles += other.FailingCompiles;
    NumCodeBytes += other.NumCodeBytes;
}
