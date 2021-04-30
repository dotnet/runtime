// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.SourceGeneration.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace System.Text.Json.SourceGeneration
{
    /// <summary>
    /// Generates source code to optimize serialization and deserialization with JsonSerializer.
    /// </summary>
    [Generator]
    public sealed partial class JsonSourceGenerator : ISourceGenerator
    {
        /// <summary>
        /// Helper for unit tests.
        /// </summary>
        public Dictionary<string, Type>? SerializableTypes => _rootTypes?.ToDictionary(p => p.Key, p => p.Value.Type);
        private Dictionary<string, TypeMetadata>? _rootTypes;

        /// <summary>
        /// Registers a syntax resolver to receive compilation units.
        /// </summary>
        /// <param name="context"></param>
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        /// <summary>
        /// Generates source code to optimize serialization and deserialization with JsonSerializer.
        /// </summary>
        /// <param name="executionContext"></param>
        public void Execute(GeneratorExecutionContext executionContext)
        {
            Parser parser = new(executionContext.Compilation);
            SyntaxReceiver receiver = (SyntaxReceiver)executionContext.SyntaxReceiver;
            _rootTypes = parser.GetRootSerializableTypes(receiver.CompilationUnits);

            Emitter emitter = new(executionContext, _rootTypes);
            emitter.Emit();
        }

        internal sealed class SyntaxReceiver : ISyntaxReceiver
        {
            public List<CompilationUnitSyntax>? CompilationUnits { get; private set; }

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is CompilationUnitSyntax compilationUnit)
                {
                    (CompilationUnits ??= new List<CompilationUnitSyntax>()).Add(compilationUnit);
                }
            }
        }
    }
}
