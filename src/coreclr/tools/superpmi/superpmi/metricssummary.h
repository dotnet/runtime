// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _MetricsSummary
#define _MetricsSummary

class MetricsSummary
{
public:
    // Number of methods successfully jitted.
    int SuccessfulCompiles = 0;
    // Number of methods that failed jitting.
    int FailingCompiles = 0;
    // Number of methods that failed jitting due to missing SPMI data.
    int MissingCompiles = 0;
    // Number of code bytes produced by the JIT for the successful compiles.
    long long NumCodeBytes = 0;
    // Number of code bytes that were diffed with the other compiler in diff mode.
    long long NumDiffedCodeBytes = 0;
    // Number of executed instructions in successful compiles.
    // Requires a dynamic instrumentor to be enabled.
    long long NumExecutedInstructions = 0;
    // Number of executed instructions inside contexts that were successfully diffed.
    long long NumDiffExecutedInstructions = 0;

    bool SaveToFile(const char* path);
    static bool LoadFromFile(const char* path, MetricsSummary* metrics);
    void AggregateFrom(const MetricsSummary& other);
};

#endif
