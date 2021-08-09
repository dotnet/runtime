// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace System.Text.Json.SourceGeneration
{
    /// <summary>
    /// Generates source code to optimize serialization and deserialization with JsonSerializer.
    /// </summary>
    [Generator]
    public sealed partial class JsonSourceGenerator : IIncrementalGenerator
    {
        private const string SystemTextJsonSourceGenerationName = "System.Text.Json.SourceGeneration";
        private const string IJsonOnSerializedFullName = "System.Text.Json.Serialization.IJsonOnSerialized";
        private const string IJsonOnSerializingFullName = "System.Text.Json.Serialization.IJsonOnSerializing";


        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider((s, _) => s is ClassDeclarationSyntax, (s, _) => (ClassDeclarationSyntax)s.Node);

            var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndClasses, (spc, source) => Execute(source.Left, source.Right, spc));
        }

        private void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> contextClasses, SourceProductionContext context)
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

            Parser parser = new(compilation, context);
            SourceGenerationSpec? spec = parser.GetGenerationSpec(contextClasses);
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
}
