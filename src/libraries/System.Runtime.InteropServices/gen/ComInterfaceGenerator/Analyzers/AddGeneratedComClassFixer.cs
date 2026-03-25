// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.Interop.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class AddGeneratedComClassFixer : ConvertToSourceGeneratedInteropFixer
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AnalyzerDiagnostics.Ids.AddGeneratedComClassAttribute);

        protected override string BaseEquivalenceKey => nameof(AddGeneratedComClassFixer);

        private static async Task AddGeneratedComClassAsync(DocumentEditor editor, SyntaxNode node, CancellationToken ct)
        {
            editor.ReplaceNode(node, (node, gen) =>
            {
                var attribute = gen.Attribute(gen.TypeExpression(editor.SemanticModel.Compilation.GetBestTypeByMetadataName(TypeNames.GeneratedComClassAttribute)).WithAdditionalAnnotations(Simplifier.AddImportsAnnotation));
                var updatedNode = gen.AddAttributes(node, attribute);
                var declarationModifiers = gen.GetModifiers(updatedNode);
                if (!declarationModifiers.IsPartial)
                {
                    updatedNode = gen.WithModifiers(updatedNode, declarationModifiers.WithPartial(true));
                }
                return updatedNode;
            });

            MakeNodeParentsPartial(editor, node);

            var declaringType = editor.SemanticModel.GetDeclaredSymbol(node, ct) as INamedTypeSymbol;
            if (declaringType is not null)
            {
                var comVisibleAttributeType = editor.SemanticModel.Compilation.GetBestTypeByMetadataName(TypeNames.System_Runtime_InteropServices_ComVisibleAttribute);
                if (comVisibleAttributeType is not null)
                {
                    var comVisibleAttr = declaringType.GetAttributes().FirstOrDefault(attr =>
                        SymbolEqualityComparer.Default.Equals(attr.AttributeClass, comVisibleAttributeType)
                        && attr.ConstructorArguments.Length == 1
                        && attr.ConstructorArguments[0].Value is true);

                    if (comVisibleAttr?.ApplicationSyntaxReference is { } syntaxRef)
                    {
                        var comVisibleAttrSyntax = await syntaxRef.GetSyntaxAsync(ct).ConfigureAwait(false);
                        editor.RemoveNode(comVisibleAttrSyntax);
                    }
                }
            }
        }

        protected override Func<DocumentEditor, CancellationToken, Task> CreateFixForSelectedOptions(SyntaxNode node, ImmutableDictionary<string, Option> selectedOptions)
        {
            return (editor, ct) => AddGeneratedComClassAsync(editor, node, ct);
        }

        protected override string GetDiagnosticTitle(ImmutableDictionary<string, Option> selectedOptions)
        {
            bool allowUnsafe = selectedOptions.TryGetValue(Option.AllowUnsafe, out var allowUnsafeOption) && allowUnsafeOption is Option.Bool(true);

            return allowUnsafe
                ? SR.AddGeneratedComClassAttributeTitle
                : SR.AddGeneratedComClassAddUnsafe;
        }

        protected override ImmutableDictionary<string, Option> ParseOptionsFromDiagnostic(Diagnostic diagnostic)
        {
            return ImmutableDictionary<string, Option>.Empty;
        }

        protected override ImmutableDictionary<string, Option> CombineOptions(ImmutableDictionary<string, Option> fixAllOptions, ImmutableDictionary<string, Option> diagnosticOptions) => fixAllOptions;
    }
}
