// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            Diagnostics.Debugger.Launch();
#endif
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
                .ForAttributeWithMetadataName(
#if !ROSLYN4_4_OR_GREATER
                    context,
#endif
                    Parser.JsonSerializableAttributeFullName,
                    (node, _) => node is ClassDeclarationSyntax,
                    (context, _) => (ClassDeclarationSyntax)context.TargetNode);

            IncrementalValueProvider<SourceGenerationSpec?> sourceGenSpec = context.CompilationProvider
                .Combine(classDeclarations.Collect())
                .Select(static (tuple, cancellationToken) =>
                {
                    Parser parser = new(tuple.Left);
                    return parser.GetGenerationSpec(tuple.Right, cancellationToken);
                })
#if ROSLYN4_4_OR_GREATER
                .WithTrackingName(SourceGenerationSpecTrackingName);
#else
                ;
#endif

            context.RegisterSourceOutput(sourceGenSpec, EmitSource);
        }

        private void EmitSource(SourceProductionContext sourceProductionContext, SourceGenerationSpec? sourceGenSpec)
        {
            OnSourceEmitting?.Invoke(sourceGenSpec);

            if (sourceGenSpec is null)
            {
                return;
            }

            JsonSourceGenerationContext context = new JsonSourceGenerationContext(sourceProductionContext);
            Emitter emitter = new(context, sourceGenSpec);
            emitter.Emit();
        }

        /// <summary>
        /// Instrumentation helper for unit tests.
        /// </summary>
        public Action<SourceGenerationSpec?>? OnSourceEmitting { get; init; }
    }

    internal readonly struct JsonSourceGenerationContext
    {
        private readonly SourceProductionContext _context;

        public JsonSourceGenerationContext(SourceProductionContext context)
        {
            _context = context;
        }

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            _context.ReportDiagnostic(diagnostic);
        }

        public void AddSource(string hintName, SourceText sourceText)
        {
            _context.AddSource(hintName, sourceText);
        }
    }
}
