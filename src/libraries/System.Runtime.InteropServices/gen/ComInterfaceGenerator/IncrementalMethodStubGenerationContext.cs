// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Microsoft.Interop
{
    internal sealed record IncrementalMethodStubGenerationContext(
        SignatureContext SignatureContext,
        ContainingSyntaxContext ContainingSyntaxContext,
        ContainingSyntax StubMethodSyntaxTemplate,
        MethodSignatureDiagnosticLocations DiagnosticLocation,
        SequenceEqualImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> CallingConvention,
        VirtualMethodIndexData VtableIndexData,
        MarshallingInfo ExceptionMarshallingInfo,
        MarshallingGeneratorFactoryKey<(TargetFramework TargetFramework, Version TargetFrameworkVersion)> ManagedToUnmanagedGeneratorFactory,
        MarshallingGeneratorFactoryKey<(TargetFramework TargetFramework, Version TargetFrameworkVersion)> UnmanagedToManagedGeneratorFactory,
        ManagedTypeInfo TypeKeyOwner,
        SequenceEqualImmutableArray<Diagnostic> Diagnostics,
        TypeSyntax UnwrapperSyntax);
}
