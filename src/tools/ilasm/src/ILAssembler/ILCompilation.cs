// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace ILAssembler;

public class ILCompilation
{
    public ILCompilation(SourceText source)
    {
    }

    public ImmutableArray<Diagnostic> Diagnostics { get; }

    public ImmutableArray<byte> Emit()
    {
        return ImmutableArray<byte>.Empty;
    }
}
