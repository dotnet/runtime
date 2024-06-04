// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Extensions.Options.Generators
{
    [Generator]
    public class OptionsValidatorGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<(TypeDeclarationSyntax? TypeSyntax, SemanticModel SemanticModel)> typeDeclarations = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    SymbolLoader.OptionsValidatorAttribute,
                    (node, _) => node is TypeDeclarationSyntax,
                    (context, _) => (TypeSyntax:context.TargetNode as TypeDeclarationSyntax, SemanticModel: context.SemanticModel))
                .Where(static m => m.TypeSyntax is not null);

            IncrementalValueProvider<(Compilation, ImmutableArray<(TypeDeclarationSyntax? TypeSyntax, SemanticModel SemanticModel)>)> compilationAndTypes =
                context.CompilationProvider.Combine(typeDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndTypes, static (spc, source) => HandleAnnotatedTypes(source.Item1, source.Item2, spc));
        }

        private static void HandleAnnotatedTypes(Compilation compilation, ImmutableArray<(TypeDeclarationSyntax? TypeSyntax, SemanticModel SemanticModel)> types, SourceProductionContext context)
        {
            if (types.Length == 0)
            {
                return;
            }

            if (!SymbolLoader.TryLoad(compilation, out var symbolHolder))
            {
                // Not eligible compilation
                return;
            }

            OptionsSourceGenContext optionsSourceGenContext = new(compilation);

            var parser = new Parser(compilation, context.ReportDiagnostic, symbolHolder!, optionsSourceGenContext, context.CancellationToken);

            var validatorTypes = parser.GetValidatorTypes(types);
            if (validatorTypes.Count > 0)
            {
                var emitter = new Emitter(compilation, symbolHolder!, optionsSourceGenContext);
                var result = emitter.Emit(validatorTypes, context.CancellationToken);

                context.AddSource("Validators.g.cs", SourceText.From(result, Encoding.UTF8));
            }
        }
    }
}
