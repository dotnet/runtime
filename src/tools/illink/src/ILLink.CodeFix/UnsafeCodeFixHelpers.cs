// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace ILLink.CodeFix
{
    internal static class UnsafeCodeFixHelpers
    {
        private const string SafetyComment = "// SAFETY: Audit";
        private const string SafetyDocumentation = "/// <safety>TODO: Audit.</safety>";
        private const string UnsafeExpressionPlaceholder = "__unsafe_operand__";

        public static async Task<Document> ApplyDeclarationUpdatesAsync(
            Document document,
            CancellationToken cancellationToken)
        {
            if (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } semanticModel ||
                await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root)
            {
                return document;
            }

            var updates = UnsafeMigrationAnalysis.GetDeclarationUpdates(semanticModel, cancellationToken);
            if (updates.IsEmpty)
                return document;

            var annotations = updates.ToDictionary(
                static update => update.Declaration,
                static _ => new SyntaxAnnotation());

            SyntaxNode changedRoot = root.ReplaceNodes(
                annotations.Keys,
                (original, rewritten) => rewritten.WithAdditionalAnnotations(annotations[original]));

            SyntaxGenerator generator = SyntaxGenerator.GetGenerator(document);
            foreach (var update in updates)
            {
                SyntaxNode declaration = changedRoot.GetAnnotatedNodes(annotations[update.Declaration]).Single();
                SyntaxNode replacement = declaration;

                if (update.AddUnsafeModifier && !UnsafeMigrationAnalysis.HasUnsafeModifier(declaration))
                    replacement = AddUnsafeModifier(replacement, generator);

                if (update.AddSafetyDocumentation && !UnsafeMigrationAnalysis.HasSafetyDocumentation(replacement))
                    replacement = AddSafetyDocumentation(replacement);

                replacement = replacement.WithLeadingTrivia(replacement.GetLeadingTrivia());

                changedRoot = changedRoot.ReplaceNode(declaration, replacement);
            }

            return document.WithSyntaxRoot(changedRoot);
        }

        private static SyntaxNode AddUnsafeModifier(
            SyntaxNode declaration,
            SyntaxGenerator generator)
            => declaration is AccessorDeclarationSyntax accessor
                ? accessor.WithModifiers(accessor.Modifiers.Insert(
                    0,
                    SyntaxFactory.Token(SyntaxKind.UnsafeKeyword)
                        .WithTrailingTrivia(SyntaxFactory.Space)))
                : generator.WithModifiers(
                    declaration,
                    generator.GetModifiers(declaration).WithIsUnsafe(true));

        public static UnsafeStatementSyntax CreateUnsafeStatement(params StatementSyntax[] statements)
        {
            if (statements.Length > 0)
            {
                statements[0] = statements[0].WithLeadingTrivia(
                    CreateSafetyCommentTrivia().AddRange(statements[0].GetLeadingTrivia()));
            }

            return SyntaxFactory.UnsafeStatement(SyntaxFactory.Block(statements))
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        public static ExpressionSyntax? CreateUnsafeExpression(ExpressionSyntax expression)
        {
            ExpressionSyntax template = SyntaxFactory.ParseExpression(
                $"unsafe(/* SAFETY: Audit */{UnsafeExpressionPlaceholder})");

            if (template.ContainsDiagnostics)
                return null;

            IdentifierNameSyntax? placeholder = template.DescendantNodesAndSelf()
                .OfType<IdentifierNameSyntax>()
                .SingleOrDefault(static identifier => identifier.Identifier.ValueText == UnsafeExpressionPlaceholder);

            if (placeholder is null)
                return null;

            return template.ReplaceNode(placeholder, expression.WithoutTrivia())
                .WithTriviaFrom(expression);
        }

        public static bool ContainsDirectives(SyntaxNode node)
            => node.DescendantTrivia(descendIntoTrivia: true).Any(static trivia => trivia.IsDirective);

        private static SyntaxNode AddSafetyDocumentation(SyntaxNode declaration)
        {
            SyntaxTriviaList leadingTrivia = declaration.GetLeadingTrivia();
            SyntaxTriviaList indentation = SyntaxFactory.TriviaList(
                leadingTrivia
                    .Reverse()
                    .TakeWhile(static trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                    .Reverse());
            SyntaxTriviaList documentation = SyntaxFactory.ParseLeadingTrivia($"{SafetyDocumentation}\r\n");
            return declaration.WithLeadingTrivia(
                leadingTrivia
                    .AddRange(documentation)
                    .AddRange(indentation));
        }

        private static SyntaxTriviaList CreateSafetyCommentTrivia()
            => SyntaxFactory.ParseLeadingTrivia($"{SafetyComment}\r\n");
    }
}
#endif
