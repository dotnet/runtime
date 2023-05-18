// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    internal sealed record SkippedStubContext(ManagedTypeInfo OriginalDefiningType) : GeneratedMethodContextBase(OriginalDefiningType, new(ImmutableArray<Diagnostic>.Empty));
}
