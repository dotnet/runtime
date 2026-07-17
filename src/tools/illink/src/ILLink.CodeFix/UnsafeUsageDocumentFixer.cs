// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix;

internal static class UnsafeUsageDocumentFixer
{
    private const int WholeBodyThreshold = 3;

    public static async Task<Document> FixDocumentAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        Document current = await UnsafeCodeFixHelpers.ApplyDeclarationUpdatesAsync(
            document,
            cancellationToken).ConfigureAwait(false);

        while (true)
        {
            if (await current.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } semanticModel ||
                await current.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root)
            {
                return current;
            }

            ImmutableArray<Location> locations =
                UnsafeMigrationAnalysis.GetUnsafeOperationLocations(
                    semanticModel,
                    UnsafeCodeFixHelpers.IsMSBuildPropertyValueTrue(
                        current,
                        MSBuildPropertyOptionNames.SkipLocalsInit),
                    cancellationToken);
            if (locations.IsEmpty)
                return current;

            SyntaxNode changedRoot = ApplyOperationFixes(root, semanticModel, locations);
            if (ReferenceEquals(changedRoot, root))
                return current;

            current = current.WithSyntaxRoot(changedRoot);
        }
    }

    private static SyntaxNode ApplyOperationFixes(
        SyntaxNode root,
        SemanticModel semanticModel,
        ImmutableArray<Location> locations)
    {
        BlockSyntax[] bodies = RemoveNestedNodes(
            locations
                .Select(location => FindExecutableBody(root.FindNode(
                    location.SourceSpan,
                    getInnermostNodeForTie: true)))
                .OfType<BlockSyntax>()
                .GroupBy(static body => body)
                .Where(static group => group.Count() >= WholeBodyThreshold)
                .Select(static group => group.Key)
                .Where(CanWrapInUnsafeBlock));

        if (bodies is [_, ..])
            return WrapBodies(root, bodies);

        HashSet<StatementSyntax> statementFixes = [];
        HashSet<ExpressionSyntax> expressionFixes = [];

        foreach (Location location in locations)
        {
            SyntaxNode target = root.FindNode(
                location.SourceSpan,
                getInnermostNodeForTie: true);

            if (target.AncestorsAndSelf().Any(static node => node is AttributeArgumentSyntax))
                continue;

            StatementSyntax? statement = target.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();
            ExpressionSyntax? expression = FindFixableExpression(target, location.SourceSpan);

            if (!MustUseUnsafeExpression(target, statement) &&
                statement is not null &&
                CanWrapInUnsafeBlock(statement) &&
                !DeclaresEscapingLocals(statement))
            {
                statementFixes.Add(statement);
                continue;
            }

            if (expression is not null && CanUseUnsafeExpression(expression, semanticModel))
                expressionFixes.Add(expression);
        }

        StatementSyntax[] statements = RemoveNestedNodes(statementFixes);
        expressionFixes.RemoveWhere(expression =>
            expression.AncestorsAndSelf().OfType<StatementSyntax>().Any(statementFixes.Contains));

        ExpressionSyntax[] expressions = RemoveNestedExpressions(expressionFixes);
        if (statements.Length == 0 && expressions.Length == 0)
            return root;

        SyntaxNode[] nodes =
        [
            .. statements.Cast<SyntaxNode>(),
            .. expressions
        ];

        return root.ReplaceNodes(
            nodes,
            static (original, rewritten) => original switch
            {
                StatementSyntax statement => WrapStatement(
                    (StatementSyntax)rewritten,
                    statement.Parent is BlockSyntax or SwitchSectionSyntax),

                ExpressionSyntax => UnsafeCodeFixHelpers.CreateUnsafeExpression((ExpressionSyntax)rewritten) ?? rewritten,
                _ => rewritten
            });
    }

    private static SyntaxNode WrapBodies(SyntaxNode root, BlockSyntax[] bodies)
        => root.ReplaceNodes(
            bodies,
            static (_, rewritten) =>
            {
                BlockSyntax body = (BlockSyntax)rewritten;
                return body.WithStatements(
                    [UnsafeCodeFixHelpers.CreateUnsafeStatement([.. body.Statements])])
                    .WithAdditionalAnnotations(Formatter.Annotation);
            });

    private static StatementSyntax WrapStatement(
        StatementSyntax statement,
        bool replaceDirectly)
    {
        UnsafeStatementSyntax unsafeStatement =
            UnsafeCodeFixHelpers.CreateUnsafeStatement(statement.WithoutTrivia());

        if (replaceDirectly)
        {
            return unsafeStatement
                .WithLeadingTrivia(statement.GetLeadingTrivia().AddRange(unsafeStatement.GetLeadingTrivia()))
                .WithTrailingTrivia(statement.GetTrailingTrivia());
        }

        return Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Block(unsafeStatement)
            .WithLeadingTrivia(statement.GetLeadingTrivia())
            .WithTrailingTrivia(statement.GetTrailingTrivia())
            .WithAdditionalAnnotations(Formatter.Annotation);
    }

    private static bool MustUseUnsafeExpression(
        SyntaxNode target,
        StatementSyntax? statement)
    {
        if (statement is null or LocalFunctionStatementSyntax or LocalDeclarationStatementSyntax ||
            target.AncestorsAndSelf().Any(static node =>
                node is CatchFilterClauseSyntax or ConstructorInitializerSyntax))
        {
            return true;
        }

        return HasInteriorDirectives(statement) ||
            statement.DescendantNodesAndSelf().Any(static node =>
                node is AwaitExpressionSyntax or YieldStatementSyntax);
    }

    private static bool CanUseUnsafeExpression(
        ExpressionSyntax expression,
        SemanticModel semanticModel)
    {
        if (expression.DescendantNodesAndSelf().Any(static node => node is AwaitExpressionSyntax) ||
            IsStatementLevelOrAssignmentTarget(expression))
        {
            return false;
        }

        TypeInfo typeInfo = semanticModel.GetTypeInfo(expression);
        return (typeInfo.Type ?? typeInfo.ConvertedType) is
        {
            SpecialType: not SpecialType.System_Void
        };
    }

    private static bool IsStatementLevelOrAssignmentTarget(ExpressionSyntax expression)
    {
        if (expression.Parent is ExpressionStatementSyntax)
            return true;

        for (ExpressionSyntax current = expression;
            current.Parent is ExpressionSyntax parent;
            current = parent)
        {
            if (parent is AssignmentExpressionSyntax { Left: { } left } && left == current)
                return true;
        }

        return false;
    }

    private static ExpressionSyntax? FindFixableExpression(
        SyntaxNode target,
        TextSpan diagnosticSpan)
        => target.AncestorsAndSelf()
            .OfType<ExpressionSyntax>()
            .Where(expression => expression.Span.Contains(diagnosticSpan))
            .FirstOrDefault(static expression => expression switch
            {
                IdentifierNameSyntax or GenericNameSyntax
                    when expression.Parent is InvocationExpressionSyntax { Expression: var invoked } &&
                        invoked == expression => false,

                SimpleNameSyntax
                    when expression.Parent is MemberAccessExpressionSyntax { Name: var name } &&
                        name == expression => false,

                _ => true
            });

    private static BlockSyntax? FindExecutableBody(SyntaxNode target)
        => target.AncestorsAndSelf().OfType<BlockSyntax>().FirstOrDefault(static block =>
            block.Parent is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or
                LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax);

    private static bool CanWrapInUnsafeBlock(SyntaxNode node)
        => !HasInteriorDirectives(node) &&
            !node.DescendantNodesAndSelf().Any(static descendant =>
                descendant is AwaitExpressionSyntax or YieldStatementSyntax);

    private static bool HasInteriorDirectives(SyntaxNode node)
    {
        TextSpan interior = TextSpan.FromBounds(
            node.GetFirstToken().SpanStart,
            node.GetLastToken().Span.End);

        return node.DescendantTrivia().Any(trivia =>
            trivia.IsDirective && interior.Contains(trivia.SpanStart));
    }

    private static bool DeclaresEscapingLocals(StatementSyntax statement)
        => statement.DescendantNodes().Any(static node =>
            node is SingleVariableDesignationSyntax
            {
                Parent: DeclarationExpressionSyntax or DeclarationPatternSyntax or
                    RecursivePatternSyntax or VarPatternSyntax
            });

    private static TNode[] RemoveNestedNodes<TNode>(IEnumerable<TNode> nodes)
        where TNode : SyntaxNode
    {
        HashSet<TNode> candidates = new(nodes);
        return
        [
            .. candidates.Where(candidate =>
                !candidate.Ancestors().OfType<TNode>().Any(candidates.Contains))
        ];
    }

    private static ExpressionSyntax[] RemoveNestedExpressions(
        IEnumerable<ExpressionSyntax> expressions)
    {
        List<ExpressionSyntax> selected = [];
        foreach (ExpressionSyntax expression in expressions
            .Distinct()
            .OrderByDescending(static expression => expression.Span.Length))
        {
            if (selected.All(selectedExpression => !selectedExpression.Span.Contains(expression.Span)))
                selected.Add(expression);
        }

        return [.. selected];
    }
}
#endif
