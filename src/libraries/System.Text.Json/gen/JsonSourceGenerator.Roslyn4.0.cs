// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
#if !ROSLYN4_4_OR_GREATER
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
#endif

namespace System.Text.Json.SourceGeneration
{
    /// <summary>
    /// Generates source code to optimize serialization and deserialization with JsonSerializer.
    /// </summary>
    [Generator]
    public sealed partial class JsonSourceGenerator : IIncrementalGenerator
    {
#if ROSLYN4_4_OR_GREATER
        public const string SourceGenerationSpecTrackingName = "SourceGenerationSpec";
#endif

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
#if LAUNCH_DEBUGGER
            System.Diagnostics.Debugger.Launch();
#endif
            IncrementalValueProvider<KnownTypeSymbols> knownTypeSymbols = context.CompilationProvider
                .Select((compilation, _) => new KnownTypeSymbols(compilation));

            IncrementalValuesProvider<(ContextGenerationSpec?, ImmutableArray<Diagnostic>)> contextGenerationSpecs = context.SyntaxProvider
                .ForAttributeWithMetadataName(
#if !ROSLYN4_4_OR_GREATER
                    context,
#endif
                    Parser.JsonSerializableAttributeFullName,
                    (node, _) => node is ClassDeclarationSyntax,
                    (context, _) => (ContextClass: (ClassDeclarationSyntax)context.TargetNode, context.SemanticModel))
                .Combine(knownTypeSymbols)
                .Select(static (tuple, cancellationToken) =>
                {
                    // Ensure the source generator parses using invariant culture.
                    // This prevents issues such as locale-specific negative signs (e.g., U+2212 in fi-FI)
                    // from being written to generated source files.
#pragma warning disable RS1035 // CultureInfo.CurrentCulture is banned in analyzers
                    CultureInfo originalCulture = CultureInfo.CurrentCulture;
                    CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                    try
                    {
#pragma warning restore RS1035
                        Parser parser = new(tuple.Right);
                        ContextGenerationSpec? contextGenerationSpec = parser.ParseContextGenerationSpec(tuple.Left.ContextClass, tuple.Left.SemanticModel, cancellationToken);
                        ImmutableArray<Diagnostic> diagnostics = parser.Diagnostics.ToImmutableArray();
                        return (contextGenerationSpec, diagnostics);
#pragma warning disable RS1035
                    }
                    finally
                    {
                        CultureInfo.CurrentCulture = originalCulture;
                    }
#pragma warning restore RS1035
                })
#if ROSLYN4_4_OR_GREATER
                .WithTrackingName(SourceGenerationSpecTrackingName)
#endif
                ;

            // Pipeline 1: Source generation only.
            // Uses Select to extract just the spec; the Select operator deduplicates by
            // comparing model equality, so source generation only re-fires on structural changes.
            context.RegisterSourceOutput(contextGenerationSpecs.Select(static (t, _) => t.Item1), EmitSource);

            // Pipeline 2: Diagnostics only.
            // Diagnostics use raw SourceLocation instances that are pragma-suppressible.
            // This pipeline re-fires whenever diagnostics change (e.g. positional shifts)
            // without triggering expensive source regeneration.
            // See https://github.com/dotnet/runtime/issues/92509 for context.
            context.RegisterSourceOutput(
                contextGenerationSpecs,
                static (context, tuple) =>
                {
                    foreach (Diagnostic diagnostic in tuple.Item2)
                    {
                        context.ReportDiagnostic(diagnostic);
                    }
                });
        }

        private void EmitSource(SourceProductionContext sourceProductionContext, ContextGenerationSpec? contextGenerationSpec)
        {
            if (contextGenerationSpec is null)
            {
                return;
            }

            OnSourceEmitting?.Invoke(ImmutableArray.Create(contextGenerationSpec));

            // Ensure the source generator emits number literals using invariant culture.
            // This prevents issues such as locale-specific negative signs (e.g., U+2212 in fi-FI)
            // from being written to generated source files.
#pragma warning disable RS1035 // CultureInfo.CurrentCulture is banned in analyzers
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                Emitter emitter = new(sourceProductionContext);
                emitter.Emit(contextGenerationSpec);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
#pragma warning restore RS1035
        }

        /// <summary>
        /// Instrumentation helper for unit tests.
        /// </summary>
        public Action<ImmutableArray<ContextGenerationSpec>>? OnSourceEmitting { get; init; }

        private partial class Emitter
        {
            private readonly SourceProductionContext _context;

            public Emitter(SourceProductionContext context)
                => _context = context;

            private partial void AddSource(string hintName, SourceText sourceText)
                => _context.AddSource(hintName, sourceText);
        }
    }
}
