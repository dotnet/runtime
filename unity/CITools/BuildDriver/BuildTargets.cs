// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BuildDriver;

[Flags]
public enum BuildTargets
{
    None = 0,
    Runtime = 1 << 0,
    ClassLibs = 1 << 1,
    CoreCLR = Runtime | ClassLibs,
    All = CoreCLR
}
