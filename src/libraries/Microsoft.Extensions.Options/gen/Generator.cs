// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
        private static readonly HashSet<string> _attributeNames = new()
        {
            SymbolLoader.OptionsValidatorAttribute,
        };

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            GeneratorUtilities.Initialize(context, _attributeNames, HandleAnnotatedTypes);
        }

        private static void HandleAnnotatedTypes(Compilation compilation, IEnumerable<SyntaxNode> nodes, SourceProductionContext context)
        {
            if (!SymbolLoader.TryLoad(compilation, out var symbolHolder))
            {
                // Not eligible compilation
                return;
            }

            var parser = new Parser(compilation, context.ReportDiagnostic, symbolHolder!, context.CancellationToken);

            var validatorTypes = parser.GetValidatorTypes(nodes.OfType<TypeDeclarationSyntax>());
            if (validatorTypes.Count > 0)
            {
                var emitter = new Emitter();
                var result = emitter.Emit(validatorTypes, context.CancellationToken);

                context.AddSource("Validators.g.cs", SourceText.From(result, Encoding.UTF8));
            }
        }
    }
}
