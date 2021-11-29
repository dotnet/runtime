// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _MetricsSummary
#define _MetricsSummary

class MetricsSummary
{
public:
    int SuccessfulCompiles;
    int FailingCompiles;
    long long NumCodeBytes;

    MetricsSummary()
        : SuccessfulCompiles(0)
        , FailingCompiles(0)
        , NumCodeBytes(0)
    {
    }

    bool SaveToFile(const char* path);
    static bool LoadFromFile(const char* path, MetricsSummary* metrics);
    void AggregateFrom(const MetricsSummary& other);
};

#endif
