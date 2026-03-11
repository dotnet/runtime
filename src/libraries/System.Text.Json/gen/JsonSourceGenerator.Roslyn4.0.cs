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
using SourceGenerators;

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

            IncrementalValuesProvider<(ContextGenerationSpec?, ImmutableEquatableArray<DiagnosticInfo>)> contextGenerationSpecs = context.SyntaxProvider
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
                        ImmutableEquatableArray<DiagnosticInfo> diagnostics = parser.Diagnostics.ToImmutableEquatableArray();
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

            context.RegisterSourceOutput(contextGenerationSpecs, ReportDiagnosticsAndEmitSource);
        }

        private void ReportDiagnosticsAndEmitSource(SourceProductionContext sourceProductionContext, (ContextGenerationSpec? ContextGenerationSpec, ImmutableEquatableArray<DiagnosticInfo> Diagnostics) input)
        {
            // Report any diagnostics ahead of emitting.
            foreach (DiagnosticInfo diagnostic in input.Diagnostics)
            {
                sourceProductionContext.ReportDiagnostic(diagnostic.CreateDiagnostic());
            }

            if (input.ContextGenerationSpec is null)
            {
                return;
            }

            OnSourceEmitting?.Invoke(ImmutableArray.Create(input.ContextGenerationSpec));

            // Ensure the source generator emits number literals using invariant culture.
            // This prevents issues such as locale-specific negative signs (e.g., U+2212 in fi-FI)
            // from being written to generated source files.
#pragma warning disable RS1035 // CultureInfo.CurrentCulture is banned in analyzers
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                Emitter emitter = new(sourceProductionContext);
                emitter.Emit(input.ContextGenerationSpec);
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
