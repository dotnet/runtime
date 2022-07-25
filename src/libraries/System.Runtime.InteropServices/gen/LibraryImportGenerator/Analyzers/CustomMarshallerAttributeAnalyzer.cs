// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using static Microsoft.Interop.Analyzers.AnalyzerDiagnostics;

namespace Microsoft.Interop.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CustomMarshallerAttributeAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Usage";

        public static class MissingMemberNames
        {
            public const string Key = nameof(MissingMemberNames);
            public const char Delimiter = ' ';
        }

        public static readonly DiagnosticDescriptor MarshallerTypeMustSpecifyManagedTypeRule =
            new DiagnosticDescriptor(
                Ids.InvalidCustomMarshallerAttributeUsage,
                GetResourceString(nameof(SR.InvalidCustomMarshallerAttributeUsageTitle)),
                GetResourceString(nameof(SR.MarshallerTypeMustSpecifyManagedTypeMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.MarshallerTypeMustSpecifyManagedTypeDescription)));

        public static readonly DiagnosticDescriptor TypeMustBeUnmanagedOrStrictlyBlittableRule =
            new DiagnosticDescriptor(
                Ids.InvalidNativeType,
                GetResourceString(nameof(SR.InvalidMarshallerTypeTitle)),
                GetResourceString(nameof(SR.TypeMustBeUnmanagedOrStrictlyBlittableMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeMustBeUnmanagedOrStrictlyBlittableDescription)));

        public static readonly DiagnosticDescriptor GetPinnableReferenceReturnTypeBlittableRule =
            new DiagnosticDescriptor(
                Ids.InvalidSignaturesInMarshallerShape,
                GetResourceString(nameof(SR.InvalidSignaturesInMarshallerShapeTitle)),
                GetResourceString(nameof(SR.GetPinnableReferenceReturnTypeBlittableMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.GetPinnableReferenceReturnTypeBlittableDescription)));

        public static readonly DiagnosticDescriptor TypeMustHaveExplicitCastFromVoidPointerRule =
            new DiagnosticDescriptor(
                Ids.InvalidNativeType,
                GetResourceString(nameof(SR.InvalidMarshallerTypeTitle)),
                GetResourceString(nameof(SR.TypeMustHaveExplicitCastFromVoidPointerMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeMustHaveExplicitCastFromVoidPointerDescription)));

        public static readonly DiagnosticDescriptor ValueInRequiresOneParameterConstructorRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                GetResourceString(nameof(SR.CustomMarshallerTypeMustHaveRequiredShapeTitle)),
                GetResourceString(nameof(SR.ValueInRequiresOneParameterConstructorMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ValueInRequiresOneParameterConstructorDescription)));

        public static readonly DiagnosticDescriptor LinearCollectionInRequiresTwoParameterConstructorRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                GetResourceString(nameof(SR.CustomMarshallerTypeMustHaveRequiredShapeTitle)),
                GetResourceString(nameof(SR.LinearCollectionInRequiresTwoParameterConstructorMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.LinearCollectionInRequiresTwoParameterConstructorDescription)));

        public static readonly DiagnosticDescriptor ValueInCallerAllocatedBufferRequiresSpanConstructorRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                GetResourceString(nameof(SR.CustomMarshallerTypeMustHaveRequiredShapeTitle)),
                GetResourceString(nameof(SR.ValueInCallerAllocatedBufferRequiresSpanConstructorMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ValueInCallerAllocatedBufferRequiresSpanConstructorDescription)));

        public static readonly DiagnosticDescriptor LinearCollectionInCallerAllocatedBufferRequiresSpanConstructorRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                GetResourceString(nameof(SR.CustomMarshallerTypeMustHaveRequiredShapeTitle)),
                GetResourceString(nameof(SR.LinearCollectionInCallerAllocatedBufferRequiresSpanConstructorMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.LinearCollectionInCallerAllocatedBufferRequiresSpanConstructorDescription)));

        public static readonly DiagnosticDescriptor OutRequiresToManagedRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                GetResourceString(nameof(SR.CustomMarshallerTypeMustHaveRequiredShapeTitle)),
                GetResourceString(nameof(SR.OutRequiresToManagedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.OutRequiresToManagedDescription)));

        public static readonly DiagnosticDescriptor LinearCollectionInRequiresCollectionMethodsRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                GetResourceString(nameof(SR.CustomMarshallerTypeMustHaveRequiredShapeTitle)),
                GetResourceString(nameof(SR.LinearCollectionInRequiresCollectionMethodsMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.LinearCollectionInRequiresCollectionMethodsDescription)));

        public static readonly DiagnosticDescriptor LinearCollectionOutRequiresCollectionMethodsRule =
            new DiagnosticDescriptor(
                Ids.CustomMarshallerTypeMustHaveRequiredShape,
                GetResourceString(nameof(SR.CustomMarshallerTypeMustHaveRequiredShapeTitle)),
                GetResourceString(nameof(SR.LinearCollectionOutRequiresCollectionMethodsMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.LinearCollectionOutRequiresCollectionMethodsDescription)));

        public static readonly DiagnosticDescriptor CallerAllocConstructorMustHaveBufferSizeRule =
            new DiagnosticDescriptor(
                Ids.CallerAllocConstructorMustHaveBufferSize,
                GetResourceString(nameof(SR.CallerAllocConstructorMustHaveBufferSizeTitle)),
                GetResourceString(nameof(SR.CallerAllocConstructorMustHaveBufferSizeMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.CallerAllocConstructorMustHaveBufferSizeDescription)));

        public static readonly DiagnosticDescriptor MarshallerTypeMustBeClosedOrMatchArityRule =
            new DiagnosticDescriptor(
                Ids.InvalidCustomMarshallerAttributeUsage,
                GetResourceString(nameof(SR.InvalidMarshallerTypeTitle)),
                GetResourceString(nameof(SR.MarshallerTypeMustBeClosedOrMatchArityMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.MarshallerTypeMustBeClosedOrMatchArityDescription)));

        public static readonly DiagnosticDescriptor MarshallerTypeMustBeNonNullRule =
            new DiagnosticDescriptor(
                Ids.InvalidCustomMarshallerAttributeUsage,
                GetResourceString(nameof(SR.InvalidMarshallerTypeTitle)),
                GetResourceString(nameof(SR.MarshallerTypeMustBeNonNullMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.MarshallerTypeMustBeNonNullDescription)));

        public static readonly DiagnosticDescriptor ToFromUnmanagedTypesMustMatchRule =
            new DiagnosticDescriptor(
                Ids.InvalidSignaturesInMarshallerShape,
                GetResourceString(nameof(SR.InvalidSignaturesInMarshallerShapeTitle)),
                GetResourceString(nameof(SR.ToFromUnmanagedTypesMustMatchMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ToFromUnmanagedTypesMustMatchDescription)));

        public static readonly DiagnosticDescriptor LinearCollectionElementTypesMustMatchRule =
            new DiagnosticDescriptor(
                Ids.InvalidSignaturesInMarshallerShape,
                GetResourceString(nameof(SR.InvalidSignaturesInMarshallerShapeTitle)),
                GetResourceString(nameof(SR.LinearCollectionElementTypesMustMatchMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.LinearCollectionElementTypesMustMatchDescription)));

        public static readonly DiagnosticDescriptor ManagedTypeMustBeClosedOrMatchArityRule =
            new DiagnosticDescriptor(
                Ids.InvalidCustomMarshallerAttributeUsage,
                GetResourceString(nameof(SR.InvalidManagedTypeTitle)),
                GetResourceString(nameof(SR.ManagedTypeMustBeClosedOrMatchArityMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ManagedTypeMustBeClosedOrMatchArityDescription)));

        public static readonly DiagnosticDescriptor ManagedTypeMustBeNonNullRule =
            new DiagnosticDescriptor(
                Ids.InvalidCustomMarshallerAttributeUsage,
                GetResourceString(nameof(SR.InvalidManagedTypeTitle)),
                GetResourceString(nameof(SR.ManagedTypeMustBeNonNullMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ManagedTypeMustBeNonNullDescription)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                MarshallerTypeMustSpecifyManagedTypeRule,
                TypeMustBeUnmanagedOrStrictlyBlittableRule,
                GetPinnableReferenceReturnTypeBlittableRule,
                TypeMustHaveExplicitCastFromVoidPointerRule,
                ValueInRequiresOneParameterConstructorRule,
                LinearCollectionInRequiresTwoParameterConstructorRule,
                OutRequiresToManagedRule,
                LinearCollectionInRequiresCollectionMethodsRule,
                LinearCollectionOutRequiresCollectionMethodsRule,
                CallerAllocConstructorMustHaveBufferSizeRule,
                MarshallerTypeMustBeClosedOrMatchArityRule,
                ToFromUnmanagedTypesMustMatchRule,
                LinearCollectionElementTypesMustMatchRule,
                ManagedTypeMustBeClosedOrMatchArityRule,
                ManagedTypeMustBeNonNullRule);

        public override void Initialize(AnalysisContext context)
        {
            // Don't analyze generated code
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(PrepareForAnalysis);
        }

        private void PrepareForAnalysis(CompilationStartAnalysisContext context)
        {
            if (context.Compilation.GetBestTypeByMetadataName(TypeNames.CustomMarshallerAttribute) is not null)
            {
                var perCompilationAnalyzer = new PerCompilationAnalyzer(context.Compilation);

                // TODO: Change this from a SyntaxNode action to an operation attribute once attribute application is represented in the
                // IOperation tree by Roslyn.
                context.RegisterSyntaxNodeAction(perCompilationAnalyzer.AnalyzeAttribute, SyntaxKind.Attribute);
            }
        }

        private sealed partial class PerCompilationAnalyzer
        {
            private readonly Compilation _compilation;
            private readonly INamedTypeSymbol _spanOfT;
            private readonly INamedTypeSymbol _readOnlySpanOfT;

            public PerCompilationAnalyzer(Compilation compilation)
            {
                _compilation = compilation;
                _spanOfT = compilation.GetBestTypeByMetadataName(TypeNames.System_Span_Metadata);
                _readOnlySpanOfT = compilation.GetBestTypeByMetadataName(TypeNames.System_ReadOnlySpan_Metadata);
            }

            public void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
            {
                AttributeSyntax syntax = (AttributeSyntax)context.Node;
                ISymbol attributedSymbol = context.ContainingSymbol!;

                AttributeData? attr = syntax.FindAttributeData(attributedSymbol);
                if (attr?.AttributeClass?.ToDisplayString() == TypeNames.CustomMarshallerAttribute
                    && attr.AttributeConstructor is not null)
                {
                    DiagnosticReporter managedTypeReporter = DiagnosticReporter.CreateForLocation(syntax.FindArgumentWithNameOrArity("managedType", 0).FindTypeExpressionOrNullLocation(), context.ReportDiagnostic);
                    INamedTypeSymbol entryType = (INamedTypeSymbol)attributedSymbol;

                    INamedTypeSymbol? managedTypeInAttribute = (INamedTypeSymbol?)attr.ConstructorArguments[0].Value;
                    if (managedTypeInAttribute is null)
                    {
                        managedTypeReporter.CreateAndReportDiagnostic(ManagedTypeMustBeNonNullRule, entryType.ToDisplayString());
                    }

                    if (!ManualTypeMarshallingHelper.TryResolveManagedType(
                        entryType,
                        managedTypeInAttribute,
                        ManualTypeMarshallingHelper.IsLinearCollectionEntryPoint(entryType),
                        (entryType, managedType) => managedTypeReporter.CreateAndReportDiagnostic(ManagedTypeMustBeClosedOrMatchArityRule, managedType, entryType), out ITypeSymbol managedType))
                    {
                        return;
                    }
                    DiagnosticReporter marshallerTypeReporter = DiagnosticReporter.CreateForLocation(syntax.FindArgumentWithNameOrArity("marshallerType", 2).FindTypeExpressionOrNullLocation(), context.ReportDiagnostic);
                    ITypeSymbol? marshallerTypeInAttribute = (ITypeSymbol?)attr.ConstructorArguments[2].Value;
                    if (marshallerTypeInAttribute is null)
                    {
                        marshallerTypeReporter.CreateAndReportDiagnostic(MarshallerTypeMustBeNonNullRule);
                    }
                    if (!ManualTypeMarshallingHelper.TryResolveMarshallerType(
                        entryType,
                        marshallerTypeInAttribute,
                        (entryType, marshallerType) => marshallerTypeReporter.CreateAndReportDiagnostic(MarshallerTypeMustBeClosedOrMatchArityRule, marshallerType, entryType),
                        out ITypeSymbol marshallerType))
                    {
                        return;
                    }

                    AnalyzeMarshallerType(
                        marshallerTypeReporter,
                        (INamedTypeSymbol)managedType,
                        (MarshalMode)attr.ConstructorArguments[1].Value,
                        (INamedTypeSymbol?)marshallerType,
                        ManualTypeMarshallingHelper.IsLinearCollectionEntryPoint(entryType));
                }
            }

#pragma warning disable CA1822 // Mark members as static
            private void AnalyzeMarshallerType(DiagnosticReporter diagnosticFactory, INamedTypeSymbol? managedType, MarshalMode mode, INamedTypeSymbol? marshallerType, bool isLinearCollectionMarshaller)
#pragma warning restore CA1822 // Mark members as static
            {
                // TODO: Implement for the V2 shapes
            }
        }
    }
}
