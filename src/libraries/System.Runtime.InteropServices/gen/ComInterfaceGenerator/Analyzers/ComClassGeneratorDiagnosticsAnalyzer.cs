// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace Microsoft.Interop.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ComClassGeneratorDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            GeneratorDiagnostics.RequiresAllowUnsafeBlocks,
            GeneratorDiagnostics.InvalidAttributedClassMissingPartialModifier,
            GeneratorDiagnostics.ClassDoesNotImplementAnyGeneratedComInterface);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            bool unsafeCodeIsEnabled = context.Compilation.Options is CSharpCompilationOptions { AllowUnsafe: true };
            INamedTypeSymbol? generatedComClassAttributeType = context.Compilation.GetBestTypeByMetadataName(TypeNames.GeneratedComClassAttribute);
            INamedTypeSymbol? generatedComInterfaceAttributeType = context.Compilation.GetBestTypeByMetadataName(TypeNames.GeneratedComInterfaceAttribute);

            context.RegisterSymbolAction(context => AnalyzeNamedType(context, unsafeCodeIsEnabled, generatedComClassAttributeType, generatedComInterfaceAttributeType), SymbolKind.NamedType);
        });
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context, bool unsafeCodeIsEnabled, INamedTypeSymbol? generatedComClassAttributeType, INamedTypeSymbol? generatedComInterfaceAttributeType)
    {
        if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class } classToAnalyze)
        {
            return;
        }

        if (!classToAnalyze.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, generatedComClassAttributeType)))
        {
            return;
        }

        Location location = classToAnalyze.Locations.First();

        if (!unsafeCodeIsEnabled)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    GeneratorDiagnostics.RequiresAllowUnsafeBlocks,
                    location));
        }

        var declarationNode = (TypeDeclarationSyntax)location.SourceTree.GetRoot().FindNode(location.SourceSpan);

        if (!declarationNode.IsInPartialContext(out _))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    GeneratorDiagnostics.InvalidAttributedClassMissingPartialModifier,
                    location,
                    classToAnalyze));
        }

        foreach (INamedTypeSymbol iface in classToAnalyze.AllInterfaces)
        {
            if (iface.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, generatedComInterfaceAttributeType)) is { } generatedComInterfaceAttribute &&
                GeneratedComInterfaceCompilationData.GetDataFromAttribute(generatedComInterfaceAttribute).Options.HasFlag(ComInterfaceOptions.ManagedObjectWrapper))
            {
                return;
            }
        }

        // Class doesn't implement any generated COM interface. Report a warning about that
        context.ReportDiagnostic(
            Diagnostic.Create(
                GeneratorDiagnostics.ClassDoesNotImplementAnyGeneratedComInterface,
                location,
                classToAnalyze));
    }
}
