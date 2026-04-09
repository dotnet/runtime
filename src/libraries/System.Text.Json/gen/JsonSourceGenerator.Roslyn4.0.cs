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

            // Project the combined pipeline result to just the equatable model, discarding diagnostics.
            // ContextGenerationSpec implements value equality, so Roslyn's Select operator will compare
            // successive model snapshots and only propagate changes downstream when the model structurally
            // differs. This ensures source generation is fully incremental: re-emitting code only when
            // the serialization spec actually changes, not on every keystroke or positional shift.
            IncrementalValuesProvider<ContextGenerationSpec?> sourceGenerationSpecs =
                contextGenerationSpecs.Select(static (t, _) => t.Item1);

            context.RegisterSourceOutput(sourceGenerationSpecs, EmitSource);

            // Project to just the diagnostics, discarding the model. ImmutableArray<Diagnostic> does not
            // implement value equality, so Roslyn's incremental pipeline uses reference equality for these
            // values — the callback fires on every compilation change. This is by design: diagnostic
            // emission is cheap, and we need fresh SourceLocation instances that are pragma-suppressible
            // (cf. https://github.com/dotnet/runtime/issues/92509).
            // No source code is generated from this pipeline — it exists solely to report diagnostics.
            IncrementalValuesProvider<ImmutableArray<Diagnostic>> diagnostics =
                contextGenerationSpecs.Select(static (t, _) => t.Item2);

            context.RegisterSourceOutput(diagnostics, EmitDiagnostics);
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

        private static void EmitDiagnostics(SourceProductionContext context, ImmutableArray<Diagnostic> diagnostics)
        {
            foreach (Diagnostic diagnostic in diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }
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
