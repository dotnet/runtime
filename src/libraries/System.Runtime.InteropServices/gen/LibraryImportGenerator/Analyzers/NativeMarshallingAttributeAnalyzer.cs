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
using Microsoft.CodeAnalysis.Operations;

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

            context.RegisterOperationAction(perCompilationAnalyzer.AnalyzeAttribute, OperationKind.Attribute);
        }

        private sealed partial class PerCompilationAnalyzer
        {
            private readonly Compilation _compilation;

            public PerCompilationAnalyzer(Compilation compilation)
            {
                _compilation = compilation;
            }

            public void AnalyzeAttribute(OperationAnalysisContext context)
            {
                IAttributeOperation attr = (IAttributeOperation)context.Operation;
                if (attr.Operation is IObjectCreationOperation attrCreation
                    && attrCreation.Type.ToDisplayString() == TypeNames.NativeMarshallingAttribute)
                {
                    IArgumentOperation marshallerEntryPointTypeArgument = attrCreation.GetArgumentByOrdinal(0);
                    if (marshallerEntryPointTypeArgument.Value.IsNullLiteralOperation())
                    {
                        DiagnosticReporter diagnosticFactory = DiagnosticReporter.CreateForLocation(marshallerEntryPointTypeArgument.Value.Syntax.GetLocation(), context.ReportDiagnostic);
                        diagnosticFactory.CreateAndReportDiagnostic(
                            MarshallerEntryPointTypeMustBeNonNullRule,
                            GetSymbolType(context.ContainingSymbol!).ToDisplayString());
                    }
                    if (marshallerEntryPointTypeArgument.Value is ITypeOfOperation typeOfOp)
                    {
                        AnalyzeManagedTypeMarshallingInfo(
                            GetSymbolType(context.ContainingSymbol!),
                            DiagnosticReporter.CreateForLocation(((TypeOfExpressionSyntax)typeOfOp.Syntax).Type.GetLocation(), context.ReportDiagnostic),
                            (INamedTypeSymbol?)typeOfOp.TypeOperand);
                    }
                }
            }

            private void AnalyzeManagedTypeMarshallingInfo(
                ITypeSymbol managedType,
                DiagnosticReporter diagnosticFactory,
                INamedTypeSymbol? entryType)
            {
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
