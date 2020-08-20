// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(TARGET_WINDOWS) && defined(TARGET_ARM64)

// Workaround Windows ARM64 CRT bug

double fma_workaround(double x, double y, double z)
{
    return fma(x, y, z);
}
#define fma fma_workaround

float fmaf_workaround(float x, float y, float z)
{
    return fmaf(x, y, z);
}
#define fmaf fmaf_workaround

#endif
