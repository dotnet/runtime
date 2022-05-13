// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//#define LAUNCH_DEBUGGER
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace System.Text.Json.SourceGeneration
{
    /// <summary>
    /// Generates source code to optimize serialization and deserialization with JsonSerializer.
    /// </summary>
    [Generator]
    public sealed partial class JsonSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(static (s, _) => Parser.IsSyntaxTargetForGeneration(s), static (s, c) => Parser.GetSemanticTargetForGeneration(s, c))
                .Where(static c => c is not null);

            IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses =
                context.CompilationProvider.Combine(classDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndClasses, (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        private void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> contextClasses, SourceProductionContext sourceProductionContext)
        {
#if LAUNCH_DEBUGGER
            if (!Diagnostics.Debugger.IsAttached)
            {
                Diagnostics.Debugger.Launch();
            }
#endif
            if (contextClasses.IsDefaultOrEmpty)
            {
                return;
            }

            JsonSourceGenerationContext context = new JsonSourceGenerationContext(sourceProductionContext);
            Parser parser = new(compilation, context);
            SourceGenerationSpec? spec = parser.GetGenerationSpec(contextClasses, sourceProductionContext.CancellationToken);
            if (spec != null)
            {
                _rootTypes = spec.ContextGenerationSpecList[0].RootSerializableTypes;

                Emitter emitter = new(context, spec);
                emitter.Emit();
            }
        }

        /// <summary>
        /// Helper for unit tests.
        /// </summary>
        public Dictionary<string, Type>? GetSerializableTypes() => _rootTypes?.ToDictionary(p => p.Type.FullName, p => p.Type);
        private List<TypeGenerationSpec>? _rootTypes;
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
