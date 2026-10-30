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

        private static async Task AddGeneratedComClassAsync(SolutionEditor solutionEditor, DocumentId documentId, SyntaxNode node, CancellationToken ct)
        {
            var editor = await solutionEditor.GetDocumentEditorAsync(documentId, ct).ConfigureAwait(false);

            var declaringType = editor.SemanticModel.GetDeclaredSymbol(node, ct) as INamedTypeSymbol;

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

            if (declaringType is not null)
            {
                var comVisibleAttributeType = editor.SemanticModel.Compilation.GetBestTypeByMetadataName(TypeNames.System_Runtime_InteropServices_ComVisibleAttribute);
                if (comVisibleAttributeType is not null)
                {
                    var comVisibleAttributes = declaringType.GetAttributes().Where(attr =>
                        SymbolEqualityComparer.Default.Equals(attr.AttributeClass, comVisibleAttributeType)
                        && attr.ConstructorArguments.Length == 1
                        && attr.ConstructorArguments[0].Value is true).ToArray();

                    foreach (var comVisibleAttr in comVisibleAttributes)
                    {
                        if (comVisibleAttr.ApplicationSyntaxReference is { } syntaxRef)
                        {
                            var comVisibleAttrSyntax = await syntaxRef.GetSyntaxAsync(ct).ConfigureAwait(false);
                            var attrDocumentId = solutionEditor.OriginalSolution.GetDocumentId(syntaxRef.SyntaxTree);
                            if (attrDocumentId is not null)
                            {
                                var attrEditor = await solutionEditor.GetDocumentEditorAsync(attrDocumentId, ct).ConfigureAwait(false);
                                attrEditor.RemoveNode(comVisibleAttrSyntax);
                            }
                        }
                    }
                }
            }
        }

        protected override Func<SolutionEditor, DocumentId, CancellationToken, Task> CreateFixForSelectedOptions(SyntaxNode node, ImmutableDictionary<string, Option> selectedOptions)
        {
            return (solutionEditor, documentId, ct) => AddGeneratedComClassAsync(solutionEditor, documentId, node, ct);
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
