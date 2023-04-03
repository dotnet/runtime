// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BuildDriver;

[Flags]
public enum TestTargets
{
    None = 0,
    Embedding = 1 << 0,
    CoreClr = 1 << 1,
    All = Embedding | CoreClr
}
