// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Microsoft.Interop
{
    internal abstract record GeneratedMethodContextBase(ManagedTypeInfo OriginalDefiningType, SequenceEqualImmutableArray<DiagnosticInfo> Diagnostics);

    internal sealed record IncrementalMethodStubGenerationContext(
        SignatureContext SignatureContext,
        ContainingSyntaxContext ContainingSyntaxContext,
        ContainingSyntax StubMethodSyntaxTemplate,
        MethodSignatureDiagnosticLocations DiagnosticLocation,
        SequenceEqualImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> CallingConvention,
        VirtualMethodIndexData VtableIndexData,
        MarshallingInfo ExceptionMarshallingInfo,
        EnvironmentFlags EnvironmentFlags,
        ManagedTypeInfo TypeKeyOwner,
        ManagedTypeInfo DeclaringType,
        SequenceEqualImmutableArray<DiagnosticInfo> Diagnostics,
        MarshallingInfo ManagedThisMarshallingInfo) : GeneratedMethodContextBase(DeclaringType, Diagnostics);
}
