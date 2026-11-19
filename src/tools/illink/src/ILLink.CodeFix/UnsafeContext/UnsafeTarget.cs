// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix.UnsafeContext
{
    internal enum UnsafeScopeKind
    {
        /// <summary>A statement body of a method, accessor, local function or lambda.</summary>
        Block,

        /// <summary>An expression body (<c>=&gt; expr</c>) of a callable.</summary>
        ExpressionBody,

        /// <summary>A position without statements: field/property initializers and constructor or base arguments.</summary>
        Initializer,
    }

    /// <summary>
    /// The innermost callable body or initializer an operation is evaluated in. Lambdas and local functions are
    /// separate scopes, so an outer context never covers their bodies.
    /// </summary>
    /// <param name="Owner">The declaration owning <paramref name="Body"/>, e.g. a method, accessor, lambda or field.</param>
    /// <param name="Body">The block of a statement body, otherwise the expression or argument list that may be wrapped.</param>
    internal sealed record UnsafeScope(UnsafeScopeKind Kind, SyntaxNode Owner, SyntaxNode Body)
    {
        /// <summary>The top-level member whose diagnostics validate edits made in this scope.</summary>
        public MemberDeclarationSyntax Member { get; } = Owner.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().First();

        /// <summary>
        /// Finds the scope <paramref name="node"/> is evaluated in, or <see langword="null"/> where no inner context
        /// can help, e.g. attribute arguments, parameter defaults and signatures.
        /// </summary>
        public static UnsafeScope? Find(SyntaxNode node)
        {
            for (SyntaxNode? current = node.Parent; current is not null; current = current.Parent)
            {
                switch (current)
                {
                    case AnonymousFunctionExpressionSyntax function:
                        return FromBody(function, function.Block, function.ExpressionBody, node);
                    case LocalFunctionStatementSyntax localFunction:
                        return FromBody(localFunction, localFunction.Body, localFunction.ExpressionBody?.Expression, node);
                    case AccessorDeclarationSyntax accessor:
                        return FromBody(accessor, accessor.Body, accessor.ExpressionBody?.Expression, node);
                    case ConstructorDeclarationSyntax { Initializer.ArgumentList: var arguments } constructor when arguments.Span.Contains(node.Span):
                        return new UnsafeScope(UnsafeScopeKind.Initializer, constructor, arguments);
                    case BaseMethodDeclarationSyntax method:
                        return FromBody(method, method.Body, method.ExpressionBody?.Expression, node);
                    case PropertyDeclarationSyntax { Initializer.Value: var value } property when value.Span.Contains(node.Span):
                        return new UnsafeScope(UnsafeScopeKind.Initializer, property, value);
                    case PropertyDeclarationSyntax property:
                        return FromBody(property, body: null, property.ExpressionBody?.Expression, node);
                    case IndexerDeclarationSyntax indexer:
                        return FromBody(indexer, body: null, indexer.ExpressionBody?.Expression, node);
                    case VariableDeclaratorSyntax { Initializer.Value: var value, Parent.Parent: BaseFieldDeclarationSyntax field } when value.Span.Contains(node.Span):
                        return new UnsafeScope(UnsafeScopeKind.Initializer, field, value);
                    case PrimaryConstructorBaseTypeSyntax { ArgumentList: var arguments, Parent.Parent: TypeDeclarationSyntax type } when arguments.Span.Contains(node.Span):
                        return new UnsafeScope(UnsafeScopeKind.Initializer, type, arguments);
                    case MemberDeclarationSyntax or AttributeSyntax or ParameterSyntax:
                        return null;
                }
            }

            return null;
        }

        private static UnsafeScope? FromBody(SyntaxNode owner, BlockSyntax? body, ExpressionSyntax? expressionBody, SyntaxNode node) =>
            body?.Span.Contains(node.Span) == true ? new UnsafeScope(UnsafeScopeKind.Block, owner, body)
            : expressionBody?.Span.Contains(node.Span) == true ? new UnsafeScope(UnsafeScopeKind.ExpressionBody, owner, expressionBody)
            : null;
    }

    /// <summary>A diagnosed operation that needs an unsafe context, mapped to the syntax owning the unsafe check.</summary>
    /// <param name="Node">The operation, e.g. the element access of a diagnostic on its brackets.</param>
    internal sealed record UnsafeTarget(Diagnostic Diagnostic, SyntaxNode Node, UnsafeScope Scope)
    {
        public TextSpan Span => Node.Span;

        /// <summary>Maps a fixable diagnostic of <paramref name="root"/> to its operation and scope.</summary>
        public static UnsafeTarget? Create(SyntaxNode root, Diagnostic diagnostic)
        {
            if (!UnsafeContextFacts.IsFixableDiagnosticId(diagnostic.Id) || diagnostic.Location.SourceTree != root.SyntaxTree)
                return null;

            SyntaxNode node = GetOperationNode(root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true));
            return UnsafeScope.Find(node) is { } scope ? new UnsafeTarget(diagnostic, node, scope) : null;
        }

        /// <summary>Climbs from the reported syntax to the operation that owns the unsafe check.</summary>
        private static SyntaxNode GetOperationNode(SyntaxNode node)
        {
            // `p[i]` and invocations can be reported on their argument lists.
            if (node is BaseArgumentListSyntax { Parent: ExpressionSyntax owner })
                node = owner;

            // CS9376 is reported on the constructed type, e.g. `new G<T>()`; the creation is the checked operation.
            if (node is TypeSyntax typeSyntax && SyntaxFacts.IsInTypeOnlyContext(typeSyntax))
            {
                SyntaxNode type = node;
                while (type.Parent is TypeSyntax or TypeArgumentListSyntax)
                    type = type.Parent;

                if (type.Parent is ExpressionSyntax consumer && (consumer is not TypeSyntax typeConsumer || !SyntaxFacts.IsInTypeOnlyContext(typeConsumer)))
                    node = consumer;
            }

            while (node.Parent is MemberAccessExpressionSyntax access && access.Name == node)
                node = access;

            return node.Parent is InvocationExpressionSyntax invocation && invocation.Expression == node ? invocation : node;
        }
    }
}
