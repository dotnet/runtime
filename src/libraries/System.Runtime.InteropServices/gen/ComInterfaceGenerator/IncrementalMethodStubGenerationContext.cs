// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    internal abstract record GeneratedMethodContextBase(ManagedTypeInfo OriginalDefiningType, SequenceEqualImmutableArray<DiagnosticInfo> Diagnostics);

    internal enum StubMemberKind
    {
        Method,
        PropertyGetter,
        PropertySetter,
    }

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
        MarshallingInfo ManagedThisMarshallingInfo,
        StubMemberKind MemberKind) : GeneratedMethodContextBase(DeclaringType, Diagnostics);

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
        MarshallingInfo ManagedThisMarshallingInfo,
        StubMemberKind MemberKind) : IncrementalMethodStubGenerationContext(
            SignatureContext,
            DiagnosticLocation,
            CallingConvention,
            VtableIndexData,
            ExceptionMarshallingInfo,
            EnvironmentFlags,
            TypeKeyOwner,
            DeclaringType,
            Diagnostics,
            ManagedThisMarshallingInfo,
            MemberKind)
    {
        /// <summary>
        /// The user-visible name of the member this stub targets, suitable for use as an identifier in
        /// generated source. For an ordinary method this is the method's name; for a property accessor
        /// this is the property's name (e.g. <c>Foo</c> for accessor <c>get_Foo</c>, stripping the
        /// Roslyn-internal <c>get_</c> / <c>set_</c> prefix).
        /// </summary>
        public string TemplateName
        {
            get
            {
                string templateName = StubMethodSyntaxTemplate.Identifier.Text;
                if (MemberKind is StubMemberKind.PropertyGetter or StubMemberKind.PropertySetter)
                {
                    // Roslyn always names property accessor methods "get_<PropertyName>" / "set_<PropertyName>".
                    Debug.Assert(templateName.StartsWith("get_", System.StringComparison.Ordinal)
                        || templateName.StartsWith("set_", System.StringComparison.Ordinal));
                    return templateName.Substring("get_".Length);
                }
                return templateName;
            }
        }
    }
}
