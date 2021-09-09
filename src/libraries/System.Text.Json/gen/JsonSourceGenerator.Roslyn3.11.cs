// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//#define LAUNCH_DEBUGGER
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json.Reflection;
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
    public sealed partial class JsonSourceGenerator : ISourceGenerator
    {
        /// <summary>
        /// Registers a syntax resolver to receive compilation units.
        /// </summary>
        /// <param name="context"></param>
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxContextReceiver());
        }

        /// <summary>
        /// Generates source code to optimize serialization and deserialization with JsonSerializer.
        /// </summary>
        /// <param name="executionContext"></param>
        public void Execute(GeneratorExecutionContext executionContext)
        {
#if LAUNCH_DEBUGGER
            if (!Diagnostics.Debugger.IsAttached)
            {
                Diagnostics.Debugger.Launch();
            }
#endif
            if (executionContext.SyntaxContextReceiver is not SyntaxContextReceiver receiver || receiver.ClassDeclarationSyntaxList == null)
            {
                // nothing to do yet
                return;
            }

            JsonSourceGenerationContext context = new JsonSourceGenerationContext(executionContext);
            Parser parser = new(executionContext.Compilation, context);
            SourceGenerationSpec? spec = parser.GetGenerationSpec(receiver.ClassDeclarationSyntaxList);
            if (spec != null)
            {
                _rootTypes = spec.ContextGenerationSpecList[0].RootSerializableTypes;

                Emitter emitter = new(context, spec);
                emitter.Emit();
            }
        }

        private sealed class SyntaxContextReceiver : ISyntaxContextReceiver
        {
            public List<ClassDeclarationSyntax>? ClassDeclarationSyntaxList { get; private set; }

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (Parser.IsSyntaxTargetForGeneration(context.Node))
                {
                    ClassDeclarationSyntax classSyntax = Parser.GetSemanticTargetForGeneration(context);
                    if (classSyntax != null)
                    {
                        (ClassDeclarationSyntaxList ??= new List<ClassDeclarationSyntax>()).Add(classSyntax);
                    }
                }
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
        private readonly GeneratorExecutionContext _context;

        public JsonSourceGenerationContext(GeneratorExecutionContext context)
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
