// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ILLink.CodeFix.UnsafeContext
{
    /// <summary>
    /// Creates <c>unsafe(...)</c> candidates: one wrapper per clause covering every operation, from the narrowest
    /// covering expression outwards. A bare wrapper may not cover a consumer's implicit operation (e.g. a conversion
    /// or a property setter), so wider candidates are kept for validation to choose from.
    /// </summary>
    internal static class ExpressionWrapPlanner
    {
        /// <summary>Returns the wrapper candidates for targets of one clause, narrowest first.</summary>
        public static ImmutableArray<UnsafeContextEdit> Create(IReadOnlyCollection<UnsafeTarget> targets, UnsafeScope scope, SyntaxNode boundary, PlanningContext context)
        {
            // Placeholder wrappers from earlier passes in the same clause are merged into the new one.
            List<SyntaxNode> nodes = [.. targets.Select(static t => t.Node)];
            if (GetCommonAncestor(nodes) is { } common && PlaceholderContexts.GetClause(common) is { } commonClause)
                nodes.AddRange(PlaceholderContexts.GetWrappersInClause(commonClause));

            var edits = ImmutableArray.CreateBuilder<UnsafeContextEdit>();
            for (SyntaxNode? node = GetCommonAncestor(nodes); node is not null && node != boundary; node = node.Parent)
            {
                if (node is StatementSyntax or MemberDeclarationSyntax or AccessorDeclarationSyntax)
                    break;

                if (node is ExpressionSyntax expression
                    && CanWrap(expression, scope, context)
                    && PlaceholderContexts.GetClause(expression) is { } clause
                    && PlaceholderContexts.GetWrappersInClause(clause).All(wrapper => expression.Span.Contains(wrapper.Span)))
                {
                    edits.Add(new ExpressionWrapEdit(expression, clause));
                }
            }

            return edits.ToImmutable();
        }

        private static SyntaxNode? GetCommonAncestor(IEnumerable<SyntaxNode> nodes) =>
            nodes.Aggregate((SyntaxNode?)null, static (common, node) =>
                common is null ? node : common.AncestorsAndSelf().FirstOrDefault(a => a.Span.Contains(node.Span)));

        /// <summary>Whether <c>unsafe(expression)</c> is valid syntax with the same meaning, and puts nothing else in the context.</summary>
        private static bool CanWrap(ExpressionSyntax expression, UnsafeScope scope, PlanningContext context)
        {
            if (expression is RefExpressionSyntax
                    or DeclarationExpressionSyntax
                    or ThrowExpressionSyntax
                    or InitializerExpressionSyntax
                    or BaseExpressionSyntax
                    or MemberBindingExpressionSyntax
                    or ElementBindingExpressionSyntax
                    or ImplicitElementAccessSyntax
                    or AnonymousFunctionExpressionSyntax
                || UnsafeContextFacts.IsUnsafeExpression(expression)
                || (expression is TypeSyntax type && SyntaxFacts.IsInTypeOnlyContext(type)))
            {
                return false;
            }

            if (!IsValuePosition(expression, context.Model, context.CancellationToken)
                || !DirectiveFacts.AreBalanced(context.Root, expression.Span))
            {
                return false;
            }

            // A lambda or local function inside would get an unsafe body; only placeholder wrappers may be nested.
            if (expression.DescendantNodes().Any(static n => n is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)
                || !PlaceholderContexts.ContainsOnlyPlaceholderWrappers(expression))
            {
                return false;
            }

            if (expression == scope.Body
                && scope.Kind == UnsafeScopeKind.ExpressionBody
                && UnsafeContextSemantics.RequiresStatementExpressionBody(scope.Owner, context.Model, context.CancellationToken))
            {
                return false;
            }

            return !UnsafeContextSemantics.IsCapturedByCallerArgumentExpression(expression, context.Model, context.CancellationToken);
        }

        /// <summary>
        /// Whether the expression is used as a value: not a statement expression, an assignment or increment target,
        /// a method group, the right side of a member access, or a type/namespace qualifier.
        /// </summary>
        private static bool IsValuePosition(ExpressionSyntax expression, SemanticModel model, CancellationToken cancellationToken) =>
            expression.Parent switch
            {
                ExpressionStatementSyntax or NameColonSyntax or NameEqualsSyntax or QualifiedNameSyntax or TypeArgumentListSyntax => false,
                MemberAccessExpressionSyntax access when access.Name == expression => false,
                MemberAccessExpressionSyntax access when access.Expression == expression =>
                    model.GetSymbolInfo(expression, cancellationToken).Symbol is not (INamespaceOrTypeSymbol or IAliasSymbol),
                InvocationExpressionSyntax invocation when invocation.Expression == expression => false,
                AssignmentExpressionSyntax assignment when assignment.Left == expression => false,
                ConditionalAccessExpressionSyntax conditionalAccess when conditionalAccess.WhenNotNull == expression => false,
                PrefixUnaryExpressionSyntax prefix when prefix.Kind() is SyntaxKind.AddressOfExpression or SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression => false,
                PostfixUnaryExpressionSyntax => false,
                ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" } } } => false,
                _ => true,
            };
    }
}
