// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BuildDriver;

[Flags]
public enum TestTargets
{
    None = 0,
    EmbeddingManaged = 1 << 0,
    EmbeddingNative = 1 << 1,
    Embedding = EmbeddingNative | EmbeddingManaged,
    Runtime = 1 << 2,
    Classlibs = 1 << 3,
    Pal = 1 << 4,
    CoreClr = Runtime | Classlibs | Pal,
    All = Embedding | CoreClr
}
