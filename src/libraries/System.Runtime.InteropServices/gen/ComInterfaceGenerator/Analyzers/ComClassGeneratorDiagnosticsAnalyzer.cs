// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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

            // We use this type only to report warning diagnostic. We also don't report a warning if there is at least one error.
            // Given that with unsafe code disabled we will get an error on each declaration, we can skip
            // unnecessary work of getting this symbol here
            INamedTypeSymbol? generatedComInterfaceAttributeType = unsafeCodeIsEnabled
                ? context.Compilation.GetBestTypeByMetadataName(TypeNames.GeneratedComInterfaceAttribute)
                : null;

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

        foreach (Diagnostic diagnostic in GetDiagnosticsForAnnotatedClass(classToAnalyze, unsafeCodeIsEnabled, generatedComInterfaceAttributeType))
        {
            context.ReportDiagnostic(diagnostic);
        }
    }

    public static IEnumerable<Diagnostic> GetDiagnosticsForAnnotatedClass(INamedTypeSymbol annotatedClass, bool unsafeCodeIsEnabled, INamedTypeSymbol? generatedComInterfaceAttributeType)
    {
        Location location = annotatedClass.Locations.First();
        bool hasErrors = false;

        if (!unsafeCodeIsEnabled)
        {
            yield return Diagnostic.Create(GeneratorDiagnostics.RequiresAllowUnsafeBlocks, location);
            hasErrors = true;
        }

        var declarationNode = (TypeDeclarationSyntax)location.SourceTree.GetRoot().FindNode(location.SourceSpan);

        if (!declarationNode.IsInPartialContext(out _))
        {
            yield return Diagnostic.Create(
                GeneratorDiagnostics.InvalidAttributedClassMissingPartialModifier,
                location,
                annotatedClass);
            hasErrors = true;
        }

        if (hasErrors)
        {
            // If we already reported at least one error avoid stacking a warning on top of it
            yield break;
        }

        foreach (INamedTypeSymbol iface in annotatedClass.AllInterfaces)
        {
            if (iface.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, generatedComInterfaceAttributeType)) is { } generatedComInterfaceAttribute &&
                GeneratedComInterfaceCompilationData.GetDataFromAttribute(generatedComInterfaceAttribute).Options.HasFlag(ComInterfaceOptions.ManagedObjectWrapper))
            {
                yield break;
            }
        }

        // Class doesn't implement any generated COM interface. Report a warning about that
        yield return Diagnostic.Create(
            GeneratorDiagnostics.ClassDoesNotImplementAnyGeneratedComInterface,
            location,
            annotatedClass);
    }
}
