// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    internal abstract record GeneratedMethodContextBase(ManagedTypeInfo OriginalDefiningType, SequenceEqualImmutableArray<DiagnosticInfo> Diagnostics);

    internal record IncrementalMethodStubGenerationContext(
        SignatureContext SignatureContext,
        ISignatureDiagnosticLocations DiagnosticLocation,
        SequenceEqualImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> CallingConvention,
        VirtualMethodIndexData VtableIndexData,
        MarshallingInfo ExceptionMarshallingInfo,
        EnvironmentFlags EnvironmentFlags,
        ManagedTypeInfo TypeKeyOwner,
        ManagedTypeInfo DeclaringType,
        SequenceEqualImmutableArray<DiagnosticInfo> Diagnostics,
        MarshallingInfo ManagedThisMarshallingInfo) : GeneratedMethodContextBase(DeclaringType, Diagnostics);

    internal sealed record SourceAvailableIncrementalMethodStubGenerationContext(
        SignatureContext SignatureContext,
        ContainingSyntaxContext ContainingSyntaxContext,
        ContainingSyntax StubMethodSyntaxTemplate,
        ISignatureDiagnosticLocations DiagnosticLocation,
        SequenceEqualImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> CallingConvention,
        VirtualMethodIndexData VtableIndexData,
        MarshallingInfo ExceptionMarshallingInfo,
        EnvironmentFlags EnvironmentFlags,
        ManagedTypeInfo TypeKeyOwner,
        ManagedTypeInfo DeclaringType,
        SequenceEqualImmutableArray<DiagnosticInfo> Diagnostics,
        MarshallingInfo ManagedThisMarshallingInfo) : IncrementalMethodStubGenerationContext(
            SignatureContext,
            DiagnosticLocation,
            CallingConvention,
            VtableIndexData,
            ExceptionMarshallingInfo,
            EnvironmentFlags,
            TypeKeyOwner,
            DeclaringType,
            Diagnostics,
            ManagedThisMarshallingInfo);
}
