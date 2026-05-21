// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SourceGenerators;

#pragma warning disable RS1035 // IIncrementalGenerator isn't available for the target configuration

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
#if LAUNCH_DEBUGGER
            System.Diagnostics.Debugger.Launch();
#endif

            // Unfortunately, there is no cancellation token that can be passed here
            // (the one in GeneratorInitializationContext is not safe to capture).
            // In practice this should still be ok as the generator driver itself will
            // cancel after every file it processes.
            context.RegisterForSyntaxNotifications(static () => new SyntaxContextReceiver(CancellationToken.None));
        }

        /// <summary>
        /// Generates source code to optimize serialization and deserialization with JsonSerializer.
        /// </summary>
        /// <param name="executionContext"></param>
        public void Execute(GeneratorExecutionContext executionContext)
        {
            // Ensure the source generator parses and emits using invariant culture.
            // This prevents issues such as locale-specific negative signs (e.g., U+2212 in fi-FI)
            // from being written to generated source files.
            // Note: RS1035 is already disabled at the file level for this Roslyn version.
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                if (executionContext.SyntaxContextReceiver is not SyntaxContextReceiver receiver)
                {
                    // nothing to do yet
                    return;
                }

                KnownTypeSymbols knownSymbols = new(executionContext.Compilation);

                // Stage 1a. Parse the identified JsonSerializerContext classes and store the model types.
                if (receiver.ContextClassDeclarations != null)
                {
                    Parser parser = new(knownSymbols);

                    List<ContextGenerationSpec>? contextGenerationSpecs = null;
                    foreach ((ClassDeclarationSyntax? contextClassDeclaration, SemanticModel semanticModel) in receiver.ContextClassDeclarations)
                    {
                        ContextGenerationSpec? contextGenerationSpec = parser.ParseContextGenerationSpec(contextClassDeclaration, semanticModel, executionContext.CancellationToken);
                        if (contextGenerationSpec is null)
                        {
                            continue;
                        }

                        (contextGenerationSpecs ??= new()).Add(contextGenerationSpec);
                    }

                    // Report any diagnostics gathered by the parser.
                    foreach (Diagnostic diagnostic in parser.Diagnostics)
                    {
                        executionContext.ReportDiagnostic(diagnostic);
                    }

                    if (contextGenerationSpecs is not null)
                    {
                        // Emit source code from the spec models.
                        OnSourceEmitting?.Invoke(contextGenerationSpecs.ToImmutableArray());
                        Emitter emitter = new(executionContext);
                        foreach (ContextGenerationSpec contextGenerationSpec in contextGenerationSpecs)
                        {
                            emitter.Emit(contextGenerationSpec);
                        }
                    }
                }

                // Stage 1b. Parse POCO types annotated with parameterless [JsonSerializable].
                if (receiver.PocoTypeDeclarations != null)
                {
                    Parser parser = new(knownSymbols);

                    foreach ((TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel) in receiver.PocoTypeDeclarations)
                    {
                        (PocoTypeGenerationSpec Poco, ContextGenerationSpec Context)? result = parser.ParsePocoTypeGenerationSpec(typeDeclaration, semanticModel, executionContext.CancellationToken);
                        if (result is not null)
                        {
                            Emitter emitter = new(executionContext);
                            // Emit the full backing context
                            emitter.Emit(result.Value.Context);
                            // Emit the static JsonTypeInfo property on the partial type
                            emitter.EmitPocoTypeProperty(result.Value.Poco);
                        }
                    }

                    foreach (Diagnostic diagnostic in parser.Diagnostics)
                    {
                        executionContext.ReportDiagnostic(diagnostic);
                    }
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        private sealed class SyntaxContextReceiver : ISyntaxContextReceiver
        {
            private readonly CancellationToken _cancellationToken;

            public SyntaxContextReceiver(CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
            }

            public List<(ClassDeclarationSyntax, SemanticModel)>? ContextClassDeclarations { get; private set; }
            public List<(TypeDeclarationSyntax, SemanticModel)>? PocoTypeDeclarations { get; private set; }

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is TypeDeclarationSyntax { AttributeLists.Count: > 0 } typeDeclaration)
                {
                    if (typeDeclaration is ClassDeclarationSyntax { BaseList.Types.Count: > 0 } classDeclaration)
                    {
                        // Could be a JsonSerializerContext-derived class
                        if (HasJsonSerializableAttribute(context, classDeclaration))
                        {
                            (ContextClassDeclarations ??= new()).Add((classDeclaration, context.SemanticModel));
                        }
                    }

                    // Also check for parameterless [JsonSerializable] on any type (POCO pattern)
                    if (HasParameterlessJsonSerializableAttribute(context, typeDeclaration))
                    {
                        (PocoTypeDeclarations ??= new()).Add((typeDeclaration, context.SemanticModel));
                    }
                }
            }

            private static bool HasJsonSerializableAttribute(GeneratorSyntaxContext context, ClassDeclarationSyntax classDeclaration)
            {
                foreach (AttributeListSyntax attributeListSyntax in classDeclaration.AttributeLists)
                {
                    foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                    {
                        if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is IMethodSymbol attributeSymbol)
                        {
                            string fullName = attributeSymbol.ContainingType.ToDisplayString();
                            if (fullName == Parser.JsonSerializableAttributeFullName)
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            private static bool HasParameterlessJsonSerializableAttribute(GeneratorSyntaxContext context, TypeDeclarationSyntax typeDeclaration)
            {
                foreach (AttributeListSyntax attributeListSyntax in typeDeclaration.AttributeLists)
                {
                    foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                    {
                        // Parameterless: no argument list, or empty argument list
                        if (attributeSyntax.ArgumentList is null or { Arguments.Count: 0 })
                        {
                            if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is IMethodSymbol attributeSymbol)
                            {
                                string fullName = attributeSymbol.ContainingType.ToDisplayString();
                                if (fullName == Parser.JsonSerializableAttributeFullName)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Instrumentation helper for unit tests.
        /// </summary>
        public Action<ImmutableArray<ContextGenerationSpec>>? OnSourceEmitting { get; init; }

        private partial class Emitter
        {
            private readonly GeneratorExecutionContext _context;

            public Emitter(GeneratorExecutionContext context)
                => _context = context;

            private partial void AddSource(string hintName, SourceText sourceText)
                => _context.AddSource(hintName, sourceText);
        }
    }
}
