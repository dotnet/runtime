// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    internal sealed record GeneratedStubCodeContext(
        ManagedTypeInfo OriginalDefiningType,
        ContainingSyntaxContext ContainingSyntaxContext,
        SyntaxEquivalentNode<MethodDeclarationSyntax> Stub,
        SequenceEqualImmutableArray<Diagnostic> Diagnostics) : GeneratedMethodContextBase(OriginalDefiningType, Diagnostics);
}
