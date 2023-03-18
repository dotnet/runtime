// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BuildDriver;

[Flags]
public enum TestTargets
{
    None = 1 << 0,
    Embedding = 1 << 1,
    CoreClr = 1 << 2,
    All = ~0
}
