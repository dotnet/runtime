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
    public class Generator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<TypeDeclarationSyntax> typeDeclarations = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    SymbolLoader.OptionsValidatorAttribute,
                    (node, _) => node is TypeDeclarationSyntax,
                    (context, _) => context.TargetNode as TypeDeclarationSyntax)
                .Where(static m => m is not null);

            IncrementalValueProvider<(Compilation, ImmutableArray<TypeDeclarationSyntax>)> compilationAndTypes =
                context.CompilationProvider.Combine(typeDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndTypes, static (spc, source) => HandleAnnotatedTypes(source.Item1, source.Item2, spc));
        }

        private static void HandleAnnotatedTypes(Compilation compilation, ImmutableArray<TypeDeclarationSyntax> types, SourceProductionContext context)
        {
            if (!SymbolLoader.TryLoad(compilation, out var symbolHolder))
            {
                // Not eligible compilation
                return;
            }

            var parser = new Parser(compilation, context.ReportDiagnostic, symbolHolder!, context.CancellationToken);

            var validatorTypes = parser.GetValidatorTypes(types);
            if (validatorTypes.Count > 0)
            {
                var emitter = new Emitter();
                var result = emitter.Emit(validatorTypes, context.CancellationToken);

                context.AddSource("Validators.g.cs", SourceText.From(result, Encoding.UTF8));
            }
        }
    }
}
