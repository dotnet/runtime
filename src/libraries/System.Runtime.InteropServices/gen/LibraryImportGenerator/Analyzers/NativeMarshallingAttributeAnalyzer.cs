// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using static Microsoft.Interop.Analyzers.AnalyzerDiagnostics;

namespace Microsoft.Interop.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NativeMarshallingAttributeAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Usage";

        public static readonly DiagnosticDescriptor MarshallerEntryPointTypeMustHaveCustomMarshallerAttributeWithMatchingManagedTypeRule =
            new DiagnosticDescriptor(
                Ids.InvalidNativeMarshallingAttributeUsage,
                GetResourceString(nameof(SR.InvalidNativeMarshallingAttributeUsageTitle)),
                GetResourceString(nameof(SR.EntryPointTypeMustHaveCustomMarshallerAttributeWithMatchingManagedTypeMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.EntryPointTypeMustHaveCustomMarshallerAttributeWithMatchingManagedTypeDescription)));

        public static readonly DiagnosticDescriptor MarshallerEntryPointTypeMustBeNonNullRule =
            new DiagnosticDescriptor(
                Ids.InvalidNativeMarshallingAttributeUsage,
                GetResourceString(nameof(SR.InvalidNativeMarshallingAttributeUsageTitle)),
                GetResourceString(nameof(SR.EntryPointTypeMustBeNonNullMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.EntryPointTypeMustBeNonNullDescription)));

        public static readonly DiagnosticDescriptor GenericEntryPointMarshallerTypeMustBeClosedOrMatchArityRule =
            new DiagnosticDescriptor(
                Ids.InvalidNativeMarshallingAttributeUsage,
                GetResourceString(nameof(SR.InvalidNativeMarshallingAttributeUsageTitle)),
                GetResourceString(nameof(SR.GenericEntryPointMarshallerTypeMustBeClosedOrMatchArityMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.GenericEntryPointMarshallerTypeMustBeClosedOrMatchArityDescription)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                MarshallerEntryPointTypeMustHaveCustomMarshallerAttributeWithMatchingManagedTypeRule,
                MarshallerEntryPointTypeMustBeNonNullRule,
                GenericEntryPointMarshallerTypeMustBeClosedOrMatchArityRule);

        public override void Initialize(AnalysisContext context)
        {
            // Don't analyze generated code
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(PrepareForAnalysis);
        }

        private void PrepareForAnalysis(CompilationStartAnalysisContext context)
        {
            var perCompilationAnalyzer = new PerCompilationAnalyzer(context.Compilation);

            // TODO: Change this from a SyntaxNode action to an operation attribute once attribute application is represented in the
            // IOperation tree by Roslyn.
            context.RegisterSyntaxNodeAction(perCompilationAnalyzer.AnalyzeAttribute, SyntaxKind.Attribute);
        }

        private sealed partial class PerCompilationAnalyzer
        {
            private readonly Compilation _compilation;

            public PerCompilationAnalyzer(Compilation compilation)
            {
                _compilation = compilation;
            }

            public void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
            {
                AttributeSyntax syntax = (AttributeSyntax)context.Node;
                ISymbol attributedSymbol = context.ContainingSymbol!;

                AttributeData? attr = syntax.FindAttributeData(attributedSymbol);
                if (attr?.AttributeClass?.ToDisplayString() == TypeNames.NativeMarshallingAttribute
                    && attr.AttributeConstructor is not null)
                {
                    INamedTypeSymbol? entryType = (INamedTypeSymbol?)attr.ConstructorArguments[0].Value;
                    AnalyzeManagedTypeMarshallingInfo(
                        GetSymbolType(attributedSymbol),
                        DiagnosticReporter.CreateForLocation(syntax.FindArgumentWithNameOrArity("nativeType", 0).FindTypeExpressionOrNullLocation(), context.ReportDiagnostic),
                        entryType);
                }
            }

            private void AnalyzeManagedTypeMarshallingInfo(
                ITypeSymbol managedType,
                DiagnosticReporter diagnosticFactory,
                INamedTypeSymbol? entryType)
            {
                if (entryType is null)
                {
                    diagnosticFactory.CreateAndReportDiagnostic(
                        MarshallerEntryPointTypeMustBeNonNullRule,
                        managedType.ToDisplayString());
                    return;
                }

                if (!ManualTypeMarshallingHelper.HasEntryPointMarshallerAttribute(entryType))
                {
                    diagnosticFactory.CreateAndReportDiagnostic(
                        MarshallerEntryPointTypeMustHaveCustomMarshallerAttributeWithMatchingManagedTypeRule,
                        entryType.ToDisplayString(),
                        managedType.ToDisplayString());
                    return;
                }

                bool isLinearCollectionMarshaller = ManualTypeMarshallingHelper.IsLinearCollectionEntryPoint(entryType);
                if (entryType.IsUnboundGenericType)
                {
                    if (managedType is not INamedTypeSymbol namedManagedType)
                    {
                        diagnosticFactory.CreateAndReportDiagnostic(
                            GenericEntryPointMarshallerTypeMustBeClosedOrMatchArityRule,
                            entryType.ToDisplayString(),
                            managedType.ToDisplayString());
                        return;
                    }
                    if (!ManualTypeMarshallingHelper.TryResolveEntryPointType(
                        namedManagedType,
                        entryType,
                        isLinearCollectionMarshaller,
                        (managedType, entryType) => diagnosticFactory.CreateAndReportDiagnostic(
                            GenericEntryPointMarshallerTypeMustBeClosedOrMatchArityRule,
                            entryType.ToDisplayString(),
                            managedType.ToDisplayString()),
                        out ITypeSymbol resolvedEntryType))
                    {
                        return;
                    }
                    entryType = (INamedTypeSymbol)resolvedEntryType;
                }

                if (!ManualTypeMarshallingHelper.TryGetMarshallersFromEntryTypeIgnoringElements(
                    entryType,
                    managedType,
                    _compilation,
                    (entryType, managedType) =>
                        diagnosticFactory.CreateAndReportDiagnostic(
                            GenericEntryPointMarshallerTypeMustBeClosedOrMatchArityRule,
                            entryType.ToDisplayString(),
                            managedType.ToDisplayString()), out _))
                {
                    diagnosticFactory.CreateAndReportDiagnostic(
                        MarshallerEntryPointTypeMustHaveCustomMarshallerAttributeWithMatchingManagedTypeRule,
                        entryType.ToDisplayString(),
                        managedType.ToDisplayString());
                }
            }

            private static ITypeSymbol GetSymbolType(ISymbol symbol)
            {
                return symbol switch
                {
                    IMethodSymbol method => method.ReturnType,
                    IParameterSymbol param => param.Type,
                    IFieldSymbol field => field.Type,
                    ITypeSymbol type => type,
                    _ => throw new InvalidOperationException()
                };
            }
        }
    }
}
