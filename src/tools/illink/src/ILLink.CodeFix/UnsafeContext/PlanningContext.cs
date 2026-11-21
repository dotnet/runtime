// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix.UnsafeContext
{
    /// <summary>The preferred shape of the introduced contexts.</summary>
    internal enum UnsafeContextPolicy
    {
        /// <summary>Blocks first, falling back to <c>unsafe(...)</c> expressions (the default).</summary>
        BlockFirst,

        /// <summary><c>unsafe(...)</c> expressions first, falling back to blocks.</summary>
        ExpressionFirst,

        /// <summary>One block around the whole body of each callable, falling back to the default plan.</summary>
        BodyWide,
    }

    /// <summary>Document-level state shared by the planners of one fix.</summary>
    internal sealed class PlanningContext(SyntaxNode root, SemanticModel model, UnsafeContextPolicy policy, CancellationToken cancellationToken)
    {
        private readonly Dictionary<SyntaxTree, ImmutableArray<TextSpan>> _conditionalSpans = [];

        public SyntaxNode Root { get; } = root;

        public SemanticModel Model { get; } = model;

        public UnsafeContextPolicy Policy { get; } = policy;

        public CancellationToken CancellationToken { get; } = cancellationToken;

        public UnsafeContextFormatting Formatting { get; } = new UnsafeContextFormatting(root);

        /// <summary>Whether a declaration lies inside an <c>#if</c> group of its file.</summary>
        public bool IsConditionallyCompiled(SyntaxReference reference)
        {
            if (!_conditionalSpans.TryGetValue(reference.SyntaxTree, out ImmutableArray<TextSpan> spans))
                _conditionalSpans[reference.SyntaxTree] = spans = DirectiveFacts.GetConditionalSpans(reference.SyntaxTree);

            return spans.Any(span => span.Contains(reference.Span.Start));
        }

        /// <summary>The types named by <c>using</c> aliases in this document.</summary>
        public ImmutableArray<ITypeSymbol> AliasTargets
        {
            get
            {
                if (field.IsDefault)
                {
                    field = [.. Root
                        .DescendantNodes(static n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
                        .OfType<UsingDirectiveSyntax>()
                        .Where(static u => u.Alias is not null)
                        .Select(u => Model.GetDeclaredSymbol(u, CancellationToken)?.Target)
                        .OfType<ITypeSymbol>()];
                }

                return field;
            }
        }
    }

    /// <summary>Semantic checks shared by the declaration splitter and the expression planner.</summary>
    internal static class UnsafeContextSemantics
    {
        /// <summary>
        /// Returns the compiler diagnostics located in <paramref name="member"/>. The bodies of partial members are
        /// bound with their defining part, so span-limited queries can miss them; those use the whole tree.
        /// </summary>
        public static ImmutableArray<Diagnostic> GetDiagnostics(SemanticModel model, MemberDeclarationSyntax member, CancellationToken cancellationToken)
        {
            if (!member.Modifiers.Any(SyntaxKind.PartialKeyword))
                return model.GetDiagnostics(member.Span, cancellationToken);

            return model.GetDiagnostics(cancellationToken: cancellationToken)
                .Where(d => member.Span.Contains(d.Location.SourceSpan))
                .ToImmutableArray();
        }

        /// <summary>
        /// Whether <paramref name="node"/> is inside an argument whose text a <c>CallerArgumentExpression</c> parameter
        /// of the selected method captures; changing that text would change the captured string.
        /// </summary>
        public static bool IsCapturedByCallerArgumentExpression(SyntaxNode node, SemanticModel model, CancellationToken cancellationToken)
        {
            for (SyntaxNode? current = node; current is not null and not StatementSyntax and not MemberDeclarationSyntax; current = current.Parent)
            {
                if (current is not ArgumentSyntax argument || argument.Parent?.Parent is not { } call)
                    continue;

                ImmutableArray<IArgumentOperation> arguments = model.GetOperation(call, cancellationToken) switch
                {
                    IInvocationOperation invocation => invocation.Arguments,
                    IObjectCreationOperation creation => creation.Arguments,
                    IPropertyReferenceOperation property => property.Arguments,
                    _ => [],
                };

                IParameterSymbol? parameter = arguments.FirstOrDefault(a => a.Syntax == argument)?.Parameter;
                if (parameter is not null && arguments.Any(a => a.ArgumentKind == ArgumentKind.DefaultValue && Captures(a.Parameter, parameter.Name)))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Whether an expression body must be a statement expression: void-returning callables and async callables
        /// returning a non-generic task. <c>unsafe(E)</c> is not a statement expression.
        /// </summary>
        public static bool RequiresStatementExpressionBody(SyntaxNode owner, SemanticModel model, CancellationToken cancellationToken)
        {
            var method = owner switch
            {
                AnonymousFunctionExpressionSyntax function => model.GetSymbolInfo(function, cancellationToken).Symbol as IMethodSymbol,
                BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or AccessorDeclarationSyntax =>
                    model.GetDeclaredSymbol(owner, cancellationToken) as IMethodSymbol,
                _ => null,
            };

            return method is not null
                && (method.ReturnsVoid || (method.IsAsync && method.ReturnType is INamedTypeSymbol { Arity: 0 }));
        }

        private static bool Captures(IParameterSymbol? capturing, string parameterName) =>
            capturing is not null && capturing.GetAttributes().Any(attribute =>
                attribute.AttributeClass?.Name == "CallerArgumentExpressionAttribute"
                && attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value as string == parameterName);
    }
}
