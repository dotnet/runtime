// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace System.Text.RegularExpressions.Generator
{
    public partial class RegexGenerator
    {
        /// <summary>
        /// Provides diagnostics for invalid usage of the <see cref="GeneratedRegexAttribute"/>.
        /// </summary>
        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        public sealed class Analyzer : DiagnosticAnalyzer
        {
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
                DiagnosticDescriptors.InvalidGeneratedRegexAttribute,
                DiagnosticDescriptors.MultipleGeneratedRegexAttributes,
                DiagnosticDescriptors.InvalidRegexArguments,
                DiagnosticDescriptors.RegexMemberMustHaveValidSignature,
                DiagnosticDescriptors.LimitedSourceGeneration,
            ];

            public override void Initialize(AnalysisContext context)
            {
                context.EnableConcurrentExecution();
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

                context.RegisterCompilationStartAction(context =>
                {
                    var generatedRegexAttributeSymbol = context.Compilation.GetTypeByMetadataName(GeneratedRegexAttributeName);
                    if (generatedRegexAttributeSymbol is null)
                    {
                        return;
                    }
                    context.RegisterSymbolAction(context =>
                    {
                        if (context.Symbol is
                            IMethodSymbol { PartialDefinitionPart: not null } or IPropertySymbol { PartialDefinitionPart: not null })
                        {
                            // The analyzer will also see the source-generated parts, so we must take care to emit diagnostics only for
                            // the partial definition that the user wrote.
                            return;
                        }
                        var attributes = context.Symbol.GetAttributes().Where(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, generatedRegexAttributeSymbol)).ToImmutableArray();
                        if (attributes.Length == 0)
                        {
                            return;
                        }
                        if (context.Symbol.DeclaringSyntaxReferences.Length == 0)
                        {
                            return;
                        }
                        var onReportDiagnostic = context.ReportDiagnostic;
                        var targetNode = context.Symbol.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken);
                        var regexPatternAndSyntax = ParseGeneratedRegexAttribute(targetNode, context.Symbol, context.Compilation, attributes, onReportDiagnostic, context.CancellationToken);
                        if (regexPatternAndSyntax is null)
                        {
                            return;
                        }
                        _ = GetRegexMethod(regexPatternAndSyntax, targetNode, onReportDiagnostic);
                    }, SymbolKind.Method, SymbolKind.Property);
                });
            }
        }
    }
}
