// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BuildDriver;

[Flags]
public enum BuildTargets
{
    None = 0,
    CoreCLR = 1 << 0,
    NullGC = 1 << 1,
    EmbeddingHost = 1 << 2,
    All = CoreCLR | NullGC | EmbeddingHost
}
