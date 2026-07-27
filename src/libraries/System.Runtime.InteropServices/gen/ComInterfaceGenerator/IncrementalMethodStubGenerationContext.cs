// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        IndexerGetter,
        IndexerSetter,
    }

    internal static class StubMemberKindExtensions
    {
        /// <summary>
        /// Returns true if <paramref name="kind"/> represents any property or indexer accessor
        /// (<see cref="StubMemberKind.PropertyGetter"/>, <see cref="StubMemberKind.PropertySetter"/>,
        /// <see cref="StubMemberKind.IndexerGetter"/>, or <see cref="StubMemberKind.IndexerSetter"/>).
        /// </summary>
        public static bool IsPropertyOrIndexerAccessor(this StubMemberKind kind)
            => kind is StubMemberKind.PropertyGetter or StubMemberKind.PropertySetter
                    or StubMemberKind.IndexerGetter or StubMemberKind.IndexerSetter;

        /// <summary>
        /// Returns true if <paramref name="kind"/> represents the setter half of either a property
        /// or an indexer.
        /// </summary>
        public static bool IsAccessorSetter(this StubMemberKind kind)
            => kind is StubMemberKind.PropertySetter or StubMemberKind.IndexerSetter;

        /// <summary>
        /// Returns true if <paramref name="kind"/> represents either accessor of an indexer
        /// (as opposed to an ordinary property).
        /// </summary>
        public static bool IsIndexerAccessor(this StubMemberKind kind)
            => kind is StubMemberKind.IndexerGetter or StubMemberKind.IndexerSetter;
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
        StubMemberKind MemberKind) : GeneratedMethodContextBase(DeclaringType, Diagnostics)
    {
        private const string GetterPrefix = "get_";
        private const string SetterPrefix = "set_";

        /// <summary>
        /// Returns true if <paramref name="name"/> matches the Roslyn naming convention for a
        /// property accessor method (<c>get_X</c> or <c>set_X</c>). Centralizes the convention so
        /// callers do not embed the prefix literals directly.
        /// </summary>
        public static bool IsPropertyAccessorName(string name)
            => name.StartsWith(GetterPrefix, StringComparison.Ordinal)
               || name.StartsWith(SetterPrefix, StringComparison.Ordinal);

        /// <summary>
        /// Strips the <c>get_</c>/<c>set_</c> prefix from <paramref name="accessorName"/> to recover
        /// the underlying property name. Caller is responsible for ensuring the input is an
        /// accessor-shaped name (validated via <see cref="IsPropertyAccessorName"/>).
        /// </summary>
        public static string GetPropertyNameFromAccessor(string accessorName)
        {
            Debug.Assert(IsPropertyAccessorName(accessorName));
            // GetterPrefix and SetterPrefix are the same length; either constant works here.
            return accessorName.Substring(GetterPrefix.Length);
        }
    }

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
                if (MemberKind.IsPropertyOrIndexerAccessor())
                {
                    return GetPropertyNameFromAccessor(templateName);
                }
                return templateName;
            }
        }
    }
}
