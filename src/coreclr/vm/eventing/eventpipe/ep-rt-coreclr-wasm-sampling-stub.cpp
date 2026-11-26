// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#ifdef PERFTRACING_DISABLE_THREADS

extern "C" bool SamplingProfiler_ShouldSample()
{
    LIMITED_METHOD_CONTRACT;
    return false;
}

extern "C" void SamplingProfiler_CollectSample()
{
    LIMITED_METHOD_CONTRACT;
}

extern "C" void SamplingProfiler_OnSamplepoint()
{
    LIMITED_METHOD_CONTRACT;
}

#endif // PERFTRACING_DISABLE_THREADS
