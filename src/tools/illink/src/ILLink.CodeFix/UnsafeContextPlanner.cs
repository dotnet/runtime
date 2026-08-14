// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix
{
    /// <summary>
    /// Decides how to introduce an <c>unsafe</c> context for a use site that the compiler rejected.
    /// </summary>
    /// <remarks>
    /// A block is always preferred, because it reads better and covers every operation in the statement at once.
    /// It is only usable when it cannot change the meaning of the surrounding code, which rules out shortening the
    /// scope of anything the statement declares, and rules out regions the language forbids inside an unsafe
    /// context. Everything else falls back to the <c>unsafe(...)</c> expression form, which never moves code.
    /// </remarks>
    internal static class UnsafeContextPlanner
    {
        /// <summary>
        /// The compiler diagnostics that ask for an unsafe context at a use site.
        /// </summary>
        internal static ImmutableArray<string> DiagnosticIds { get; } =
        [
            "CS9360", // This operation may only be used in an unsafe context
            "CS9361", // stackalloc expression without an initializer inside SkipLocalsInit
            "CS9362", // '{0}' must be used in an unsafe context because it is marked as 'unsafe'
            "CS9363", // '{0}' must be used in an unsafe context because it has pointers in its signature
            "CS9376", // An unsafe context is required for constructor '{0}' to satisfy the 'new()' constraint
        ];

        /// <summary>
        /// Plans the narrowest edit that gives <paramref name="diagnosticSpan"/> an unsafe context, or
        /// <see langword="null"/> when no edit is known to be safe.
        /// </summary>
        internal static UnsafeContextFix? Plan(
            SyntaxNode root,
            TextSpan diagnosticSpan,
            SemanticModel semanticModel)
        {
            SyntaxNode node = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);

            StatementSyntax? blockTarget = null;
            ImmutableArray<ISymbol> escaping = [];

            if (FindEnclosingStatement(node) is { } statement
                && CanIntroduceBlock(statement)
                && TryGetEscapingDeclarations(statement, semanticModel, out escaping))
            {
                blockTarget = statement;

                if (escaping.Length == 0)
                    return new UnsafeContextFix(UnsafeContextKind.Statement, statement);

                // Splitting the declaration keeps the local itself in the enclosing scope, so it is enough
                // exactly when the local is the only name that would have been trapped inside the block.
                if (escaping.Length == 1
                    && escaping[0] is ILocalSymbol local
                    && CanSplitDeclaration(statement, local, semanticModel))
                {
                    return new UnsafeContextFix(UnsafeContextKind.SplitDeclaration, statement);
                }
            }

            if (FindWrappableExpression(node, diagnosticSpan, semanticModel) is { } expression
                && CanIntroduceExpression(expression, semanticModel))
            {
                return new UnsafeContextFix(UnsafeContextKind.Expression, expression);
            }

            // Giving the 'out' variables declarations of their own frees the block to move, and is the last
            // resort because it is the only shape that adds a declaration to the source: where the expression
            // form fits it is both narrower and closer to what was written.
            if (blockTarget is not null
                && TryGetHoistableOutDeclarations(blockTarget, escaping, semanticModel, out ImmutableArray<DeclarationExpressionSyntax> hoisted))
            {
                return new UnsafeContextFix(UnsafeContextKind.HoistOutDeclarations, blockTarget, hoisted);
            }

            return null;
        }

        /// <summary>
        /// Returns the block body of the function that lexically contains <paramref name="node"/>, when that body
        /// can be turned into a single <c>unsafe</c> block.
        /// </summary>
        internal static BlockSyntax? FindWrappableBody(SyntaxNode node)
        {
            foreach (SyntaxNode ancestor in node.Ancestors())
            {
                if (GetBlockBody(ancestor) is { } body)
                {
                    // A constructor initializer, an attribute or a parameter default belongs to the member but
                    // sits outside its body, so wrapping the body would not cover the use site.
                    return body.Span.Contains(node.Span) && CanIntroduceBlock(body) ? body : null;
                }

                // An expression-bodied member has no body to wrap, and nothing outside it belongs to the function.
                if (ancestor is MemberDeclarationSyntax)
                    return null;
            }

            return null;
        }

        private static BlockSyntax? GetBlockBody(SyntaxNode node) =>
            node switch
            {
                BaseMethodDeclarationSyntax method => method.Body,
                AccessorDeclarationSyntax accessor => accessor.Body,
                LocalFunctionStatementSyntax localFunction => localFunction.Body,
                AnonymousFunctionExpressionSyntax anonymousFunction => anonymousFunction.Block,
                _ => null,
            };

        /// <summary>
        /// Walks out to the statement that would be wrapped, stopping at the clauses that hold an expression but
        /// no statement of their own.
        /// </summary>
        /// <remarks>
        /// The two barriers are the ones where the nearest enclosing statement is the wrong answer rather than a
        /// missing one. An expression-bodied local function is itself a statement, and wrapping it would hide the
        /// function from its call sites; a <c>catch</c> filter belongs to a <c>try</c> statement whose whole body
        /// would otherwise be dragged into the unsafe context.
        /// </remarks>
        private static StatementSyntax? FindEnclosingStatement(SyntaxNode node)
        {
            for (SyntaxNode? current = node; current is not null; current = current.Parent)
            {
                switch (current)
                {
                    case StatementSyntax statement:
                        return statement;

                    case ArrowExpressionClauseSyntax or CatchFilterClauseSyntax:
                        return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether a region can be replaced by an <c>unsafe</c> block without changing its meaning.
        /// </summary>
        private static bool CanIntroduceBlock(SyntaxNode region) =>
            region switch
            {
                // Moving a local function into a nested block hides it from its call sites.
                LocalFunctionStatementSyntax => false,
                BlockSyntax => true,
                StatementSyntax statement => IsReplaceableStatement(statement),
                _ => false,
            }
            // Preprocessor directives are attached to the tokens at the edges of the region, and re-indenting the
            // region moves them.
            && !region.ContainsDirectives
            // CS4004: an unsafe context reaches into nested lambdas, so any await under the region is affected.
            && !region.DescendantTokens().Any(static token => token.IsKind(SyntaxKind.AwaitKeyword))
            // CS9238: 'yield' is rejected inside an unsafe block, but only for the iterator that owns it.
            && !region
                .DescendantNodesAndSelf(static child => child is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax))
                .Any(static descendant => descendant is YieldStatementSyntax);

        private static bool IsReplaceableStatement(StatementSyntax statement) =>
            statement.Parent is BlockSyntax or SwitchSectionSyntax or GlobalStatementSyntax
                // Embedded statement positions, where the block simply takes the statement's place.
                or IfStatementSyntax or ElseClauseSyntax or WhileStatementSyntax or DoStatementSyntax
                or ForStatementSyntax or CommonForEachStatementSyntax or UsingStatementSyntax
                or LockStatementSyntax or FixedStatementSyntax or LabeledStatementSyntax;

        /// <summary>
        /// Collects the names declared by <paramref name="statement"/> that the code after it still uses, and
        /// which a block around the statement would therefore hide. Returns <see langword="false"/> when the
        /// question cannot be answered.
        /// </summary>
        /// <remarks>
        /// This covers local declarations as well as the names introduced by <c>out var</c> arguments and pattern
        /// designations, all of which are scoped to the enclosing block rather than to the statement.
        /// </remarks>
        private static bool TryGetEscapingDeclarations(
            StatementSyntax statement,
            SemanticModel semanticModel,
            out ImmutableArray<ISymbol> escaping)
        {
            if (semanticModel.AnalyzeDataFlow(statement) is not { Succeeded: true } flow)
            {
                escaping = [];
                return false;
            }

            if (flow.VariablesDeclared.IsEmpty)
            {
                escaping = [];
                return true;
            }

            HashSet<ISymbol> usedOutside = new(
                flow.ReadOutside.Concat(flow.WrittenOutside),
                SymbolEqualityComparer.Default);

            usedOutside.UnionWith(NamedOutside(statement, semanticModel));
            escaping = [.. flow.VariablesDeclared.Where(usedOutside.Contains)];

            return true;
        }

        /// <summary>
        /// Collects the symbols that a <c>nameof</c> outside <paramref name="statement"/> mentions.
        /// </summary>
        /// <remarks>
        /// <c>nameof</c> produces a constant, so data flow analysis does not report its operand as read. A local
        /// whose only later use is <c>nameof</c> would otherwise look free to move into a nested block.
        /// </remarks>
        private static IEnumerable<ISymbol> NamedOutside(StatementSyntax statement, SemanticModel semanticModel)
        {
            // The scope that bounds the declared names: the switch block rather than one of its sections, and the
            // compilation unit for top-level statements.
            if (statement.Ancestors()
                    .FirstOrDefault(static a => a is BlockSyntax or SwitchStatementSyntax or CompilationUnitSyntax) is not { } scope)
            {
                return [];
            }

            return scope.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(invocation =>
                    invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" }
                    && !statement.Span.Contains(invocation.Span))
                .SelectMany(static invocation => invocation.DescendantNodes().OfType<IdentifierNameSyntax>())
                .Select(name => semanticModel.GetSymbolInfo(name).Symbol)
                .Where(static symbol => symbol is not null)!;
        }

        /// <summary>
        /// Determines whether a local declaration can be split into a declaration and an assignment.
        /// </summary>
        private static bool CanSplitDeclaration(StatementSyntax statement, ILocalSymbol local, SemanticModel semanticModel)
        {
            if (statement is not LocalDeclarationStatementSyntax
                {
                    // The split needs to add a statement to a list, which rules out embedded positions.
                    Parent: BlockSyntax or SwitchSectionSyntax,
                    IsConst: false,
                } declaration
                || declaration.Declaration.Variables.Count != 1
                || declaration.Declaration.Variables[0] is not { Initializer: not null } variable
                // 'using' and 'await using' declarations own the lifetime of the local; moving the acquisition
                // into a nested block would move the disposal with it.
                || !declaration.UsingKeyword.IsKind(SyntaxKind.None)
                || !declaration.AwaitKeyword.IsKind(SyntaxKind.None)
                // A ref local cannot be declared without an initializer at all.
                || UnwrapScoped(declaration.Declaration.Type) is RefTypeSyntax)
            {
                return false;
            }

            // The escaping name has to be the one the split hoists. An 'out var' or a pattern designation inside
            // the initializer escapes too, and moving it into the block would hide it.
            if (!SymbolEqualityComparer.Default.Equals(local, semanticModel.GetDeclaredSymbol(variable)))
                return false;

            // A ref struct local carries a ref-safety scope that its initializer decides, and re-declaring it
            // without one cannot reproduce that. Leaving the initializer out makes the local escape to the caller,
            // which the initializer may not allow, while 'scoped' narrows it to the enclosing block, which is
            // narrower than the current method that 'stackalloc' implies. The expression form leaves the
            // declaration alone and keeps the inference exactly as it was.
            return CanForwardDeclare(local.Type) && !local.Type.IsRefLikeType;
        }

        /// <summary>
        /// Matches the escaping names against the <c>out</c> variables that <paramref name="statement"/> declares,
        /// and returns the declarations to hoist ahead of it when they account for every one of them.
        /// </summary>
        /// <remarks>
        /// An <c>out</c> variable is scoped as though it were declared just before the enclosing statement, so
        /// writing that declaration out leaves the scope exactly as it was, and the call still assigns it. Unlike
        /// a local declaration there is no initializer whose ref-safety scope could be lost, which is why a ref
        /// struct needs no exception here.
        /// </remarks>
        private static bool TryGetHoistableOutDeclarations(
            StatementSyntax statement,
            ImmutableArray<ISymbol> escaping,
            SemanticModel semanticModel,
            out ImmutableArray<DeclarationExpressionSyntax> hoisted)
        {
            hoisted = [];

            // The declarations are added to a statement list, which rules out embedded positions.
            if (statement.Parent is not (BlockSyntax or SwitchSectionSyntax))
                return false;

            Dictionary<ISymbol, DeclarationExpressionSyntax> declarations = new(SymbolEqualityComparer.Default);

            foreach (DeclarationExpressionSyntax declaration in GetOutDeclarations(statement))
            {
                if (declaration.Designation is SingleVariableDesignationSyntax designation
                    && semanticModel.GetDeclaredSymbol(designation) is ILocalSymbol outVariable
                    && CanForwardDeclare(outVariable.Type))
                {
                    declarations[outVariable] = declaration;
                }
            }

            // Anything else that escapes, a pattern designation for instance, has no declaration that could be
            // moved, and a block would still hide it.
            if (!escaping.All(declarations.ContainsKey))
                return false;

            hoisted = [.. escaping.Select(symbol => declarations[symbol])];
            return true;
        }

        /// <summary>
        /// Returns the <c>out</c> variables that <paramref name="statement"/> declares in its own right.
        /// </summary>
        /// <remarks>
        /// A declaration inside a nested function belongs to that function, so it can neither escape the statement
        /// nor be moved out of it: hoisting one would turn the variable into a captured local.
        /// </remarks>
        private static IEnumerable<DeclarationExpressionSyntax> GetOutDeclarations(StatementSyntax statement) =>
            statement
                .DescendantNodes(static node => node is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax))
                .OfType<DeclarationExpressionSyntax>()
                .Where(static declaration =>
                    declaration is { Designation: SingleVariableDesignationSyntax, Parent: ArgumentSyntax argument }
                    && argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword));

        /// <summary>
        /// Determines whether a type can be spelled in a declaration that stands on its own, which an inferred
        /// one has to be once its initializer is no longer next to it.
        /// </summary>
        private static bool CanForwardDeclare(ITypeSymbol? type) =>
            type is { TypeKind: not TypeKind.Error, IsAnonymousType: false };

        internal static TypeSyntax UnwrapScoped(TypeSyntax type) =>
            type is ScopedTypeSyntax scoped ? scoped.Type : type;

        /// <summary>
        /// Walks out from the node the diagnostic points at to the smallest expression that
        /// <c>unsafe(...)</c> can wrap and that actually covers the use site.
        /// </summary>
        /// <remarks>
        /// The diagnostic can land on a fragment such as the name in <c>receiver.Member</c>, which is not an
        /// expression at all, or on a property access, which <c>unsafe(...)</c> keeps as a place so that the
        /// accessor call happens outside the region.
        /// </remarks>
        private static ExpressionSyntax? FindWrappableExpression(
            SyntaxNode node,
            TextSpan diagnosticSpan,
            SemanticModel semanticModel)
        {
            if (node.AncestorsAndSelf().OfType<ExpressionSyntax>().FirstOrDefault() is not { } expression)
                return null;

            while (true)
            {
                if (expression.Parent is ExpressionSyntax parent && IsFragmentOf(expression, parent))
                {
                    expression = parent;
                    continue;
                }

                // Everything between a conditional access and its member binding is only meaningful together
                // with the receiver: '.X.Y' in 'a?.X.Y' is not an expression on its own.
                if (expression.Ancestors()
                        .OfType<ConditionalAccessExpressionSyntax>()
                        .FirstOrDefault(access => access.WhenNotNull.Span.Contains(expression.Span)) is { } conditionalAccess)
                {
                    expression = conditionalAccess;
                    continue;
                }

                // 'unsafe(receiver.Property)' leaves the accessor call outside the region, so the diagnostic
                // survives. Anything larger consumes the access inside the region and does establish the context.
                // Parentheses and the null-forgiving operator preserve the place, so they do not count as larger.
                bool coversAPlace = Unwrap(expression) is { } core
                    && core.Span == diagnosticSpan
                    && semanticModel.GetSymbolInfo(core).Symbol is IPropertySymbol;

                if (coversAPlace || !CanStandAlone(expression))
                {
                    if (EnclosingExpression(expression) is not { } enclosing)
                        return null;

                    expression = enclosing;
                    continue;
                }

                break;
            }

            return expression;
        }

        /// <summary>
        /// Determines whether an expression is legal outside the exact slot it occupies, and so can be moved
        /// inside <c>unsafe(...)</c>.
        /// </summary>
        /// <remarks>
        /// A type is spelled where a value is expected only as part of a larger expression, and an initializer
        /// list, along with the member assignments and index keys inside it, only means anything in the position
        /// it is written.
        /// </remarks>
        private static bool CanStandAlone(ExpressionSyntax expression) =>
            expression is not TypeSyntax
                and not InitializerExpressionSyntax
                and not ImplicitElementAccessSyntax
                and not AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax };

        /// <summary>
        /// Strips the expression forms that pass a place through unchanged.
        /// </summary>
        private static ExpressionSyntax Unwrap(ExpressionSyntax expression) =>
            expression switch
            {
                ParenthesizedExpressionSyntax parenthesized => Unwrap(parenthesized.Expression),
                PostfixUnaryExpressionSyntax suppression when suppression.IsKind(SyntaxKind.SuppressNullableWarningExpression) =>
                    Unwrap(suppression.Operand),
                _ => expression,
            };

        /// <summary>
        /// Returns the innermost expression that contains <paramref name="expression"/>, looking through the
        /// argument and initializer syntax that sits between expressions.
        /// </summary>
        private static ExpressionSyntax? EnclosingExpression(ExpressionSyntax expression)
        {
            for (SyntaxNode? node = expression.Parent; node is not null; node = node.Parent)
            {
                switch (node)
                {
                    case ExpressionSyntax enclosing:
                        return enclosing;

                    case StatementSyntax or ArrowExpressionClauseSyntax or EqualsValueClauseSyntax
                        or ConstructorInitializerSyntax or CatchFilterClauseSyntax or MemberDeclarationSyntax:
                        return null;
                }
            }

            return null;
        }

        private static bool IsFragmentOf(ExpressionSyntax expression, ExpressionSyntax parent) =>
            parent switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name == expression,
                MemberBindingExpressionSyntax memberBinding => memberBinding.Name == expression,
                ConditionalAccessExpressionSyntax conditionalAccess => conditionalAccess.WhenNotNull == expression,
                InvocationExpressionSyntax invocation => invocation.Expression == expression,
                ElementAccessExpressionSyntax elementAccess => elementAccess.Expression == expression,
                ObjectCreationExpressionSyntax objectCreation => objectCreation.Type == expression,
                _ => false,
            };

        /// <summary>
        /// Determines whether <c>unsafe(...)</c> can be spelled around an expression.
        /// </summary>
        /// <remarks>
        /// The parser resolves a leading <c>unsafe (</c> as the start of a declaration, reading the parenthesized
        /// text as a tuple type, so the keyword cannot open any construct where a declaration is also allowed. The
        /// statement form is always available in those positions, because a statement that starts with an
        /// expression declares nothing.
        /// </remarks>
        private static bool CanIntroduceExpression(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            // CS4004: the expression form is the fallback when a block would trap an await, so the await must not
            // be inside the expression being wrapped either.
            if (expression.DescendantTokens().Any(static token => token.IsKind(SyntaxKind.AwaitKeyword)))
                return false;

            // CS0201: 'unsafe(...)' is not one of the expression forms the language accepts where a statement
            // expression is required.
            if (RequiresStatementExpression(expression, semanticModel))
                return false;

            SyntaxToken first = expression.GetFirstToken();

            for (SyntaxNode node = expression; node.Parent is { } parent && parent.GetFirstToken() == first; node = parent)
            {
                bool opensDeclarationPosition = parent switch
                {
                    ExpressionStatementSyntax => true,
                    ForStatementSyntax forStatement => forStatement.Initializers.Count > 0 && forStatement.Initializers[0] == node,
                    UsingStatementSyntax usingStatement => usingStatement.Expression == node,
                    _ => false,
                };

                if (opensDeclarationPosition)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the language requires a statement expression where <paramref name="expression"/>
        /// sits, which rules out every form except a call, an assignment, an increment, a decrement, an
        /// <c>await</c> and an object creation.
        /// </summary>
        private static bool RequiresStatementExpression(ExpressionSyntax expression, SemanticModel semanticModel) =>
            expression.Parent switch
            {
                ExpressionStatementSyntax => true,
                ForStatementSyntax forStatement =>
                    forStatement.Initializers.Contains(expression) || forStatement.Incrementors.Contains(expression),
                ArrowExpressionClauseSyntax arrow => !ProducesValue(arrow.Parent, semanticModel),
                AnonymousFunctionExpressionSyntax lambda when lambda.ExpressionBody == expression =>
                    !ProducesValue(lambda, semanticModel),
                _ => false,
            };

        /// <summary>
        /// Determines whether the function whose expression body is being written has a value to return.
        /// </summary>
        private static bool ProducesValue(SyntaxNode? owner, SemanticModel semanticModel)
        {
            switch (owner)
            {
                case null:
                    return true;

                case ConstructorDeclarationSyntax or DestructorDeclarationSyntax:
                    return false;

                case AccessorDeclarationSyntax accessor:
                    return accessor.IsKind(SyntaxKind.GetAccessorDeclaration);

                case PropertyDeclarationSyntax or IndexerDeclarationSyntax:
                    return true;
            }

            IMethodSymbol? function = owner is AnonymousFunctionExpressionSyntax
                ? semanticModel.GetSymbolInfo(owner).Symbol as IMethodSymbol
                : semanticModel.GetDeclaredSymbol(owner) as IMethodSymbol;

            // An async function whose task carries no result has nothing to return either. An unresolved symbol
            // is treated the same way, because the restrictive answer is the safe one.
            return function is { ReturnsVoid: false }
                && !(function.IsAsync && function.ReturnType is INamedTypeSymbol { TypeArguments.IsEmpty: true });
        }

        /// <summary>
        /// Drops the planned edits that another edit already covers, keeping the outermost one.
        /// </summary>
        internal static ImmutableArray<UnsafeContextFix> Coalesce(IEnumerable<UnsafeContextFix> fixes)
        {
            List<UnsafeContextFix> kept = [];

            foreach (UnsafeContextFix candidate in fixes
                .OrderBy(static fix => fix.Target.SpanStart)
                .ThenByDescending(static fix => fix.Target.Span.Length))
            {
                if (!kept.Any(existing => existing.Target.Span.Contains(candidate.Target.Span)))
                    kept.Add(candidate);
            }

            return [.. kept];
        }
    }
}
#endif
