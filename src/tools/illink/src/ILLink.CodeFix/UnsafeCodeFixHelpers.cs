// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace ILLink.CodeFix;

internal static class UnsafeCodeFixHelpers
{
    private const string SafetyComment = "// SAFETY: Audit";
    private const string SafetyDocumentation = "/// <safety>TODO: Audit.</safety>";
    private const string UnsafeExpressionPlaceholder = "__unsafe_operand__";

    private static readonly CSharpParseOptions s_unsafeParseOptions =
        new CSharpParseOptions(LanguageVersion.Preview)
            .WithFeatures([new("updated-memory-safety-rules", "")]);

    public static bool IsMigrationEnabled(Document document)
        => IsMSBuildPropertyValueTrue(
            document,
            MSBuildPropertyOptionNames.EnableUnsafeMigration);

    public static bool IsMSBuildPropertyValueTrue(
        Document document,
        string propertyName)
        => document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(
            $"build_property.{propertyName}",
            out string? value) &&
            string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

    public static async Task<Document> ApplyDeclarationUpdatesAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        if (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } semanticModel ||
            await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root)
        {
            return document;
        }

        ImmutableArray<UnsafeMigrationAnalysis.DeclarationUpdate> updates =
            UnsafeMigrationAnalysis.GetDeclarationUpdates(semanticModel, cancellationToken);
        if (updates.IsEmpty)
            return document;

        Dictionary<SyntaxNode, SyntaxAnnotation> annotations = updates.ToDictionary(
            static update => update.Declaration,
            static _ => new SyntaxAnnotation());

        SyntaxNode changedRoot = root.ReplaceNodes(
            annotations.Keys,
            (original, rewritten) => rewritten.WithAdditionalAnnotations(annotations[original]));

        SyntaxGenerator generator = SyntaxGenerator.GetGenerator(document);
        foreach (UnsafeMigrationAnalysis.DeclarationUpdate update in updates)
        {
            SyntaxNode declaration = changedRoot.GetAnnotatedNodes(annotations[update.Declaration]).Single();
            SyntaxNode replacement = declaration;

            if (update.AddUnsafeModifier && !UnsafeMigrationAnalysis.HasUnsafeModifier(declaration))
                replacement = AddUnsafeModifier(replacement, generator);

            if (update.AddSafetyDocumentation && !UnsafeMigrationAnalysis.HasSafetyDocumentation(replacement))
                replacement = AddSafetyDocumentation(replacement);

            changedRoot = changedRoot.ReplaceNode(declaration, replacement);
        }

        return document.WithSyntaxRoot(changedRoot);
    }

    public static UnsafeStatementSyntax CreateUnsafeStatement(params StatementSyntax[] statements)
    {
        if (statements is [var first, ..])
        {
            statements[0] = first.WithLeadingTrivia(
                CreateSafetyCommentTrivia().AddRange(first.GetLeadingTrivia()));
        }

        return SyntaxFactory.UnsafeStatement(SyntaxFactory.Block(statements))
            .WithAdditionalAnnotations(Formatter.Annotation);
    }

    public static ExpressionSyntax? CreateUnsafeExpression(ExpressionSyntax expression)
    {
        ExpressionSyntax template = SyntaxFactory.ParseExpression(
            $"unsafe(/* SAFETY: Audit */{UnsafeExpressionPlaceholder})",
            options: s_unsafeParseOptions);

        if (template.ContainsDiagnostics)
            return null;

        IdentifierNameSyntax? placeholder = template.DescendantNodesAndSelf()
            .OfType<IdentifierNameSyntax>()
            .SingleOrDefault(static identifier => identifier.Identifier.ValueText == UnsafeExpressionPlaceholder);

        return placeholder is null
            ? null
            : template.ReplaceNode(placeholder, expression.WithoutTrivia())
                .WithTriviaFrom(expression);
    }

    private static SyntaxNode AddUnsafeModifier(
        SyntaxNode declaration,
        SyntaxGenerator generator)
        => declaration switch
        {
            AccessorDeclarationSyntax accessor => accessor
                .WithModifiers(accessor.Modifiers.Add(
                    SyntaxFactory.Token(SyntaxKind.UnsafeKeyword)
                        .WithTrailingTrivia(SyntaxFactory.Space)))
                .WithKeyword(accessor.Keyword.WithLeadingTrivia(SyntaxFactory.TriviaList()))
                .WithLeadingTrivia(accessor.GetLeadingTrivia()),

            _ => generator.WithModifiers(
                    declaration,
                    generator.GetModifiers(declaration).WithIsUnsafe(true))
                .WithLeadingTrivia(declaration.GetLeadingTrivia())
        };

    private static SyntaxNode AddSafetyDocumentation(SyntaxNode declaration)
    {
        SyntaxTriviaList leadingTrivia = declaration.GetLeadingTrivia();
        SyntaxTriviaList indentation = SyntaxFactory.TriviaList(
            leadingTrivia
                .Reverse()
                .TakeWhile(static trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                .Reverse());
        SyntaxTriviaList documentation = SyntaxFactory.ParseLeadingTrivia(SafetyDocumentation)
            .Add(SyntaxFactory.ElasticCarriageReturnLineFeed);

        return declaration.WithLeadingTrivia(
            leadingTrivia
                .AddRange(documentation)
                .AddRange(indentation));
    }

    private static SyntaxTriviaList CreateSafetyCommentTrivia()
        => SyntaxFactory.TriviaList(
            SyntaxFactory.Comment(SafetyComment),
            SyntaxFactory.ElasticCarriageReturnLineFeed);
}
#endif
