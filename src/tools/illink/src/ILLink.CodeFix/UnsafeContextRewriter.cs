// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace ILLink.CodeFix
{
    /// <summary>
    /// Applies the edits planned by <see cref="UnsafeContextPlanner"/>.
    /// </summary>
    /// <remarks>
    /// Introducing the region is all this does. Justifying it is a separate obligation, reported by
    /// <c>IL5009</c> and stubbed out by its own code fix.
    /// </remarks>
    internal static class UnsafeContextRewriter
    {
        private static readonly SymbolDisplayFormat s_typeFormat =
            SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
                SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
                | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        internal static async Task<Document> ApplyAsync(
            Document document,
            IEnumerable<UnsafeContextFix> fixes,
            CancellationToken cancellationToken)
        {
            ImmutableArray<UnsafeContextFix> coalesced = UnsafeContextPlanner.Coalesce(fixes);
            if (coalesced.IsEmpty)
                return document;

            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            foreach (UnsafeContextFix fix in coalesced)
            {
                switch (fix.Kind)
                {
                    case UnsafeContextKind.Statement:
                        WrapStatement(editor, (StatementSyntax)fix.Target);
                        break;

                    case UnsafeContextKind.SplitDeclaration:
                        SplitDeclaration(editor, (LocalDeclarationStatementSyntax)fix.Target, cancellationToken);
                        break;

                    case UnsafeContextKind.HoistOutDeclarations:
                        HoistOutDeclarations(editor, (StatementSyntax)fix.Target, fix.Hoisted, cancellationToken);
                        break;

                    case UnsafeContextKind.Body:
                        WrapBody(editor, (BlockSyntax)fix.Target);
                        break;

                    case UnsafeContextKind.Expression:
                        WrapExpression(editor, (ExpressionSyntax)fix.Target, document.Project.ParseOptions);
                        break;
                }
            }

            return editor.GetChangedDocument();
        }

        private static void WrapStatement(DocumentEditor editor, StatementSyntax statement) =>
            editor.ReplaceNode(
                statement,
                CreateUnsafeBlock(Detach(statement))
                    .WithTriviaFrom(statement)
                    .WithAdditionalAnnotations(Formatter.Annotation));

        private static void WrapBody(DocumentEditor editor, BlockSyntax body)
        {
            if (body.Statements.Count == 0)
                return;

            editor.ReplaceNode(
                body,
                body.WithStatements(SyntaxFactory.SingletonList<StatementSyntax>(CreateUnsafeBlock(body.Statements)))
                    .WithAdditionalAnnotations(Formatter.Annotation));
        }

        /// <summary>
        /// Rewrites <c>T local = initializer;</c> as <c>T local;</c> followed by an unsafe block that assigns it.
        /// </summary>
        private static void SplitDeclaration(
            DocumentEditor editor,
            LocalDeclarationStatementSyntax declaration,
            CancellationToken cancellationToken)
        {
            VariableDeclaratorSyntax variable = declaration.Declaration.Variables[0];
            if (editor.SemanticModel.GetDeclaredSymbol(variable, cancellationToken) is not ILocalSymbol local)
                return;

            SyntaxToken identifier = variable.Identifier.WithoutTrivia();

            LocalDeclarationStatementSyntax forwardDeclaration = declaration
                .WithDeclaration(SyntaxFactory.VariableDeclaration(
                    ForwardDeclaredType(declaration.Declaration.Type, local),
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(identifier))))
                .WithLeadingTrivia(declaration.GetLeadingTrivia())
                .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
                .WithAdditionalAnnotations(Formatter.Annotation);

            ExpressionStatementSyntax assignment = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(identifier),
                    // The initializer keeps its own trivia: a line comment in it is only terminated by the line
                    // break that follows it.
                    variable.Initializer!.Value));

            editor.InsertAfter(
                declaration,
                CreateUnsafeBlock(assignment)
                    .WithTrailingTrivia(declaration.GetTrailingTrivia())
                    .WithAdditionalAnnotations(Formatter.Annotation));
            editor.ReplaceNode(declaration, forwardDeclaration);
        }

        /// <summary>
        /// Rewrites <c>M(out var x);</c> as <c>T x;</c> followed by an unsafe block calling <c>M(out x)</c>, so
        /// that the variable stays visible to the statements that follow it.
        /// </summary>
        private static void HoistOutDeclarations(
            DocumentEditor editor,
            StatementSyntax statement,
            ImmutableArray<DeclarationExpressionSyntax> declarations,
            CancellationToken cancellationToken)
        {
            List<StatementSyntax> hoisted = [];

            foreach (DeclarationExpressionSyntax declaration in declarations.OrderBy(static d => d.SpanStart))
            {
                if (editor.SemanticModel.GetDeclaredSymbol(Designation(declaration), cancellationToken) is not ILocalSymbol local)
                    return;

                hoisted.Add(SyntaxFactory
                    .LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(
                        ForwardDeclaredType(declaration.Type, local),
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(Identifier(declaration)))))
                    .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
                    .WithAdditionalAnnotations(Formatter.Annotation));
            }

            if (hoisted.Count == 0)
                return;

            hoisted[0] = hoisted[0].WithLeadingTrivia(statement.GetLeadingTrivia());

            StatementSyntax call = statement.ReplaceNodes(
                declarations,
                (original, _) => SyntaxFactory.IdentifierName(Identifier(original)).WithTriviaFrom(original));

            editor.InsertBefore(statement, hoisted);
            editor.ReplaceNode(
                statement,
                CreateUnsafeBlock(Detach(call))
                    .WithTrailingTrivia(statement.GetTrailingTrivia())
                    .WithAdditionalAnnotations(Formatter.Annotation));
        }

        private static SingleVariableDesignationSyntax Designation(DeclarationExpressionSyntax declaration) =>
            (SingleVariableDesignationSyntax)declaration.Designation;

        private static SyntaxToken Identifier(DeclarationExpressionSyntax declaration) =>
            Designation(declaration).Identifier.WithoutTrivia();

        /// <summary>
        /// Builds the type for a local that no longer has an initializer to infer from.
        /// </summary>
        private static TypeSyntax ForwardDeclaredType(TypeSyntax declaredType, ILocalSymbol local)
        {
            TypeSyntax type = UnsafeContextPlanner.UnwrapScoped(declaredType);

            type = type.IsVar
                ? SyntaxFactory.ParseTypeName(local.Type.ToDisplayString(s_typeFormat))
                    .WithAdditionalAnnotations(Simplifier.Annotation)
                : Detach(type);

            return type.WithTrailingTrivia(SyntaxFactory.ElasticSpace);
        }

        private static void WrapExpression(DocumentEditor editor, ExpressionSyntax expression, ParseOptions? parseOptions)
        {
            if (CreateUnsafeExpression(expression, parseOptions) is { } unsafeExpression)
                editor.ReplaceNode(expression, unsafeExpression);
        }

        /// <summary>
        /// Builds <c>unsafe(expression)</c>.
        /// </summary>
        /// <remarks>
        /// The <c>unsafe(...)</c> expression is newer than the Roslyn this fixer compiles against, so the node
        /// cannot be constructed with <c>SyntaxFactory</c>. It is instead parsed from a template using the
        /// project's own parse options, which is also what keeps the fixer inert on a host that does not know the
        /// form yet.
        /// </remarks>
        private static ExpressionSyntax? CreateUnsafeExpression(ExpressionSyntax expression, ParseOptions? parseOptions)
        {
            ExpressionSyntax template = SyntaxFactory.ParseExpression("unsafe(0)", options: parseOptions);
            if (template.ContainsDiagnostics
                || !template.GetFirstToken().IsKind(SyntaxKind.UnsafeKeyword)
                || template.ChildNodes().OfType<ExpressionSyntax>().SingleOrDefault() is not { } placeholder)
            {
                return null;
            }

            return template
                .ReplaceNode(placeholder, Detach(expression))
                .WithTriviaFrom(expression);
        }

        private static UnsafeStatementSyntax CreateUnsafeBlock(SyntaxList<StatementSyntax> statements) =>
            SyntaxFactory.UnsafeStatement(SyntaxFactory.Block(statements));

        private static UnsafeStatementSyntax CreateUnsafeBlock(StatementSyntax statement) =>
            SyntaxFactory.UnsafeStatement(SyntaxFactory.Block(statement));

        private static TNode Detach<TNode>(TNode node) where TNode : SyntaxNode =>
            node.WithLeadingTrivia().WithTrailingTrivia();
    }
}
#endif
