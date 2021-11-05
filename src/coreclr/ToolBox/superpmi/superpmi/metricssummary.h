// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _MetricsSummary
#define _MetricsSummary

class MetricsSummary
{
public:
    // Number of methods successfully jitted.
    int SuccessfulCompiles;
    // Number of methods that failed jitting.
    int FailingCompiles;
    // Number of methods that failed jitting due to missing SPMI data.
    int MissingCompiles;
    // Number of code bytes produced by the JIT for the successful compiles.
    long long NumCodeBytes;
    // Number of code bytes that were diffed with the other compiler in diff mode.
    long long NumDiffedCodeBytes;

    MetricsSummary()
        : SuccessfulCompiles(0)
        , FailingCompiles(0)
        , MissingCompiles(0)
        , NumCodeBytes(0)
        , NumDiffedCodeBytes(0)
    {
    }

    bool SaveToFile(const char* path);
    static bool LoadFromFile(const char* path, MetricsSummary* metrics);
    void AggregateFrom(const MetricsSummary& other);
};

#endif
