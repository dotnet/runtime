// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ILLink.CodeFix.UnsafeContext
{
    /// <summary>A statement whose declarations are hoisted out of a new unsafe block.</summary>
    /// <param name="Declarations">Typed declarations placed directly before the block, without trivia.</param>
    /// <param name="Remaining">The statements kept inside the block: assignments, or the statement with lifted variables.</param>
    internal sealed record DeclarationSplit(ImmutableArray<StatementSyntax> Declarations, ImmutableArray<StatementSyntax> Remaining);

    /// <summary>
    /// Splits declarations so that a narrow block does not hide locals used after it. The exact type of the original
    /// local is kept, initialization stays at its original execution point, and no default value is added, which
    /// preserves definite assignment. Local declarations, out variables and deconstruction declarations are split;
    /// ref, const, using and pattern locals are not.
    /// </summary>
    internal static class DeclarationSplitter
    {
        private static readonly SymbolDisplayFormat s_typeFormat = SymbolDisplayFormat.MinimallyQualifiedFormat
            .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        /// <summary>Splits <paramref name="locals"/> out of <paramref name="statement"/>, or returns <see langword="null"/> if any cannot be.</summary>
        public static DeclarationSplit? TrySplit(StatementSyntax statement, IEnumerable<ILocalSymbol> locals, PlanningContext context)
        {
            // The split parts are not unwrapped, so they would keep nested `unsafe(...)` wrappers.
            if (statement.DescendantNodes().Any(UnsafeContextFacts.IsUnsafeExpression))
                return null;

            HashSet<ILocalSymbol> pending = new(locals, SymbolEqualityComparer.Default);
            var declarations = ImmutableArray.CreateBuilder<StatementSyntax>();

            // Out variables are declared before the block and passed by name.
            Dictionary<SyntaxNode, SyntaxNode> replacements = [];
            foreach (DeclarationExpressionSyntax declaration in GetOutVariableDeclarations(statement))
            {
                var designation = (SingleVariableDesignationSyntax)declaration.Designation;
                if (context.Model.GetDeclaredSymbol(designation, context.CancellationToken) is not ILocalSymbol local || !pending.Remove(local))
                    continue;

                if (UnsafeContextSemantics.IsCapturedByCallerArgumentExpression(declaration, context.Model, context.CancellationToken))
                    return null;

                var call = declaration.Parent!.Parent!.Parent as ExpressionSyntax;
                if (GetTypeSyntax(declaration.Type, local, call, statement, context) is not { } type)
                    return null;

                declarations.Add(CreateDeclaration(type, [SyntaxFactory.VariableDeclarator(designation.Identifier.WithoutTrivia())]));
                replacements[declaration] = SyntaxFactory.IdentifierName(designation.Identifier.WithoutTrivia()).WithTriviaFrom(declaration);
            }

            // A deconstruction declaration becomes a deconstruction assignment to declarations hoisted before the block.
            if (statement is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: DeclarationExpressionSyntax or TupleExpressionSyntax } deconstruction }
                && deconstruction.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                if (LiftDeconstructionTarget(deconstruction.Left, pending, declarations, context) is not { } target)
                    return null;

                replacements[deconstruction.Left] = target.WithTriviaFrom(deconstruction.Left);
            }

            StatementSyntax rewritten = replacements.Count == 0 ? statement : statement.ReplaceNodes(replacements.Keys, (original, _) => replacements[original]);
            ImmutableArray<StatementSyntax> remaining = [rewritten];

            if (statement is LocalDeclarationStatementSyntax localDeclaration
                && localDeclaration.Declaration.Variables.Any(v => context.Model.GetDeclaredSymbol(v, context.CancellationToken) is ILocalSymbol local && pending.Contains(local)))
            {
                var split = TrySplitLocalDeclaration(localDeclaration, (LocalDeclarationStatementSyntax)rewritten, context);
                if (split is null)
                    return null;

                foreach (VariableDeclaratorSyntax variable in localDeclaration.Declaration.Variables)
                    pending.Remove((ILocalSymbol)context.Model.GetDeclaredSymbol(variable, context.CancellationToken)!);

                declarations.Add(split.Value.Declaration);
                remaining = split.Value.Assignments;
            }

            // Pattern variables and other declarations cannot be split by assignment.
            return pending.Count == 0 ? new DeclarationSplit(declarations.ToImmutable(), remaining) : null;
        }

        /// <summary>
        /// Rewrites a deconstruction target: newly declared locals with a nameable type are hoisted and assigned by
        /// name, existing targets are unchanged, and discards and other declarations keep their declaration form,
        /// so that a discard never binds to a symbol named <c>_</c>.
        /// </summary>
        private static ExpressionSyntax? LiftDeconstructionTarget(
            ExpressionSyntax target,
            HashSet<ILocalSymbol> pending,
            ImmutableArray<StatementSyntax>.Builder declarations,
            PlanningContext context)
        {
            switch (target)
            {
                case TupleExpressionSyntax tuple:
                    List<ArgumentSyntax> arguments = [];
                    foreach (ArgumentSyntax argument in tuple.Arguments)
                    {
                        if (LiftDeconstructionTarget(argument.Expression, pending, declarations, context) is not { } lifted)
                            return null;

                        arguments.Add(argument.WithExpression(lifted.WithTriviaFrom(argument.Expression)));
                    }

                    return tuple.WithArguments(SyntaxFactory.SeparatedList(arguments, tuple.Arguments.GetSeparators()));
                case DeclarationExpressionSyntax declaration:
                    return LiftDesignation(declaration.Type, declaration.Designation, pending, declarations, context);
                default:
                    return target;
            }
        }

        /// <summary>Hoists the local of a single designation, recursing into <c>var (a, b)</c>-style designations.</summary>
        private static ExpressionSyntax? LiftDesignation(
            TypeSyntax type,
            VariableDesignationSyntax designation,
            HashSet<ILocalSymbol> pending,
            ImmutableArray<StatementSyntax>.Builder declarations,
            PlanningContext context)
        {
            if (designation is ParenthesizedVariableDesignationSyntax parenthesized)
            {
                // `var (a, b)` declares every element with the inferred type.
                List<ArgumentSyntax> elements = [];
                foreach (VariableDesignationSyntax variable in parenthesized.Variables)
                {
                    if (LiftDesignation(type, variable, pending, declarations, context) is not { } lifted)
                        return null;

                    elements.Add(SyntaxFactory.Argument(lifted));
                }

                return SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(elements, parenthesized.Variables.GetSeparators()));
            }

            var inlineDeclaration = SyntaxFactory.DeclarationExpression(type.WithoutTrivia().WithTrailingTrivia(SyntaxFactory.Space), designation.WithoutTrivia());
            if (designation is not SingleVariableDesignationSyntax single
                || context.Model.GetDeclaredSymbol(single, context.CancellationToken) is not ILocalSymbol local)
            {
                return inlineDeclaration;
            }

            bool isPending = pending.Remove(local);
            TypeSyntax? typeSyntax = local.Type.IsRefLikeType ? null : GetTypeSyntax(type, local, initializer: null, single, context);
            if (typeSyntax is null)
                return isPending ? null : inlineDeclaration;

            declarations.Add(CreateDeclaration(typeSyntax, [SyntaxFactory.VariableDeclarator(single.Identifier.WithoutTrivia())]));
            return SyntaxFactory.IdentifierName(single.Identifier.WithoutTrivia());
        }

        // `out var x` / `out T x` arguments outside nested lambdas and local functions.
        private static IEnumerable<DeclarationExpressionSyntax> GetOutVariableDeclarations(StatementSyntax statement) =>
            statement
                .DescendantNodes(static n => n is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax)
                .OfType<DeclarationExpressionSyntax>()
                .Where(static d => d.Designation is SingleVariableDesignationSyntax
                    && d.Parent is ArgumentSyntax argument
                    && argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword));

        /// <summary>Splits <c>T a = x, b = y;</c> into <c>T a, b;</c> and the assignments <c>a = x; b = y;</c>.</summary>
        private static (StatementSyntax Declaration, ImmutableArray<StatementSyntax> Assignments)? TrySplitLocalDeclaration(
            LocalDeclarationStatementSyntax original,
            LocalDeclarationStatementSyntax rewritten,
            PlanningContext context)
        {
            if (original.IsConst || !original.UsingKeyword.IsKind(SyntaxKind.None))
                return null;

            TypeSyntax declaredType = original.Declaration.Type;
            bool isScoped = declaredType is ScopedTypeSyntax;
            if (declaredType is ScopedTypeSyntax scopedType)
                declaredType = scopedType.Type;

            if (declaredType is RefTypeSyntax)
                return null;

            var variables = original.Declaration.Variables;
            var locals = variables.Select(v => context.Model.GetDeclaredSymbol(v, context.CancellationToken) as ILocalSymbol).ToList();
            if (locals.Any(static l => l is null || l.IsRef || l.IsConst || l.IsFixed || l.IsUsing)
                || variables.Any(static v => v.Initializer?.Value is InitializerExpressionSyntax))
            {
                return null;
            }

            // A ref-like local would get a wider escape scope once separated from its initializer. Stack-allocated
            // spans are the exception: they become scoped values, which keeps the stack memory assignable.
            ITypeSymbol localType = locals[0]!.Type;
            if (localType.IsRefLikeType || localType is ITypeParameterSymbol { AllowsRefLikeType: true })
            {
                if (!variables.All(v => IsStackAllocatedSpan(locals[variables.IndexOf(v)]!, v, context)))
                    return null;

                isScoped = true;
            }
            else if (isScoped)
            {
                return null;
            }

            ExpressionSyntax? initializer = variables.Count == 1 ? variables[0].Initializer?.Value : null;
            if (GetTypeSyntax(declaredType, locals[0]!, initializer, original, context) is not { } type)
                return null;

            if (isScoped)
                type = SyntaxFactory.ScopedType(SyntaxFactory.Token(SyntaxKind.ScopedKeyword).WithTrailingTrivia(SyntaxFactory.Space), type);

            StatementSyntax declaration = CreateDeclaration(type, variables.Select(static v => SyntaxFactory.VariableDeclarator(v.Identifier.WithoutTrivia())));

            // The last assignment keeps the original semicolon and its trivia; the others end their line.
            SyntaxToken semicolon = SyntaxFactory.Token(default, SyntaxKind.SemicolonToken, [context.Formatting.EndOfLine]);
            List<VariableDeclaratorSyntax> initialized = [.. rewritten.Declaration.Variables.Where(static v => v.Initializer is not null)];
            ImmutableArray<StatementSyntax> assignments =
            [
                .. initialized.Select((variable, i) => SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(variable.Identifier.WithoutTrivia()).WithTrailingTrivia(SyntaxFactory.Space),
                        variable.Initializer!.EqualsToken,
                        variable.Initializer.Value),
                    i == initialized.Count - 1 ? rewritten.SemicolonToken : semicolon)),
            ];

            return (declaration, assignments);
        }

        // Creates `T a, b;` without trivia.
        private static StatementSyntax CreateDeclaration(TypeSyntax type, IEnumerable<VariableDeclaratorSyntax> variables)
        {
            List<VariableDeclaratorSyntax> list = [.. variables];
            var separators = Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.Space), list.Count - 1);
            return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(type.WithoutTrivia().WithTrailingTrivia(SyntaxFactory.Space), SyntaxFactory.SeparatedList(list, separators)));
        }

        // Whether the local is a Span<T>/ReadOnlySpan<T> initialized with a stackalloc.
        private static bool IsStackAllocatedSpan(ILocalSymbol local, VariableDeclaratorSyntax variable, PlanningContext context)
        {
            Compilation compilation = context.Model.Compilation;
            ITypeSymbol definition = local.Type.OriginalDefinition;
            return (SymbolEqualityComparer.Default.Equals(definition, compilation.GetTypeByMetadataName("System.Span`1"))
                    || SymbolEqualityComparer.Default.Equals(definition, compilation.GetTypeByMetadataName("System.ReadOnlySpan`1")))
                && variable.Initializer?.Value
                    .DescendantNodesAndSelf(static n => n is not AnonymousFunctionExpressionSyntax)
                    .Any(static n => n is StackAllocArrayCreationExpressionSyntax or ImplicitStackAllocArrayCreationExpressionSyntax) == true;
        }

        /// <summary>
        /// Returns the type for a split declaration: the explicit source type, or the exact inferred type of a
        /// <c>var</c> local when it is nameable and cannot differ between build configurations.
        /// </summary>
        private static TypeSyntax? GetTypeSyntax(TypeSyntax? declaredType, ILocalSymbol local, ExpressionSyntax? initializer, SyntaxNode position, PlanningContext context)
        {
            if (declaredType is not null && !declaredType.IsVar)
                return declaredType.WithoutTrivia();

            if (!IsNameable(local.Type) || IsConfigurationDependent(local.Type, initializer, context))
                return null;

            TypeSyntax type = SyntaxFactory.ParseTypeName(local.Type.ToMinimalDisplayString(context.Model, position.SpanStart, s_typeFormat));
            return type.ContainsDiagnostics ? null : type;
        }

        // Whether the type can be written in source: no anonymous or error types, also as type arguments.
        private static bool IsNameable(ITypeSymbol type) =>
            type switch
            {
                IErrorTypeSymbol => false,
                IArrayTypeSymbol array => IsNameable(array.ElementType),
                IPointerTypeSymbol pointer => IsNameable(pointer.PointedAtType),
                INamedTypeSymbol named => !named.IsAnonymousType && named.TypeArguments.All(IsNameable),
                _ => true,
            };

        /// <summary>
        /// Whether an inferred type may differ in another build configuration: it (or a type argument) is declared in
        /// an <c>#if</c> group or named through a <c>using</c> alias, or the initializer's member is in an <c>#if</c> group.
        /// </summary>
        private static bool IsConfigurationDependent(ITypeSymbol type, ExpressionSyntax? initializer, PlanningContext context) =>
            GetTypeParts(type).Any(part =>
                part.DeclaringSyntaxReferences.Any(context.IsConditionallyCompiled)
                || context.AliasTargets.Contains(part, SymbolEqualityComparer.Default))
            || (initializer is not null
                && context.Model.GetSymbolInfo(initializer, context.CancellationToken).Symbol is { } member
                && member.DeclaringSyntaxReferences.Any(context.IsConditionallyCompiled));

        // The type itself and, recursively, its element, pointed-at and argument types.
        private static IEnumerable<ITypeSymbol> GetTypeParts(ITypeSymbol type)
        {
            yield return type;
            IEnumerable<ITypeSymbol> nested = type switch
            {
                IArrayTypeSymbol array => [array.ElementType],
                IPointerTypeSymbol pointer => [pointer.PointedAtType],
                INamedTypeSymbol named => named.TypeArguments,
                _ => [],
            };

            foreach (ITypeSymbol part in nested.SelectMany(GetTypeParts))
                yield return part;
        }
    }
}
