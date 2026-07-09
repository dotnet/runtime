// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix
{
    /// <summary>
    /// Builds the syntax that introduces an <c>unsafe</c> context (block or <c>unsafe(...)</c> expression) for a
    /// single operation flagged by IL5006, choosing the minimal-but-safe form.
    /// </summary>
    internal static class UnsafeContextStrategy
    {
        internal const string BlockComment = "// SAFETY: Audit";
        internal const string ExpressionComment = "/* SAFETY: Audit */";

        private static readonly CSharpParseOptions s_previewOptions = new CSharpParseOptions(LanguageVersion.Preview)
            .WithFeatures([new KeyValuePair<string, string>("updated-memory-safety-rules", "")]);

        /// <summary>Applies the fix for a single flagged operation, returning the rewritten root, or null if unfixable.</summary>
        internal static SyntaxNode? Fix(SyntaxNode root, SemanticModel semanticModel, TextSpan span)
        {
            var operation = GetOperationExpression(root.FindNode(span, getInnermostNodeForTie: true));
            if (operation is null)
                return null;

            var statement = operation.FirstAncestorOrSelf<StatementSyntax>();

            if (UseExpressionForm(operation, statement, semanticModel))
            {
                // A void operation can't be wrapped as an 'unsafe(...)' expression. If it is the body of an
                // expression-bodied member, convert that member to a block body with an 'unsafe' block instead.
                if (ReturnsVoid(operation, semanticModel))
                    return TryConvertExpressionBodyToBlock(root, operation);

                return WrapExpression(root, operation, semanticModel);
            }

            // A block is only usable if we have a statement to wrap.
            if (statement is null)
                return WrapExpression(root, operation, semanticModel);

            // A local declaration can't be wrapped wholesale without shrinking the variable's scope. Split it:
            // keep the bare declaration in the outer scope and move only its initializer into the 'unsafe' block.
            if (statement is LocalDeclarationStatementSyntax)
                return TrySplitLocalDeclaration(root, statement, operation, semanticModel)
                    ?? WrapExpression(root, operation, semanticModel);

            return WrapStatement(root, statement);
        }

        /// <summary>Finds the smallest expression that fully represents the unsafe operation (whole call, deref, etc.).</summary>
        private static ExpressionSyntax? GetOperationExpression(SyntaxNode node)
        {
            if (node.AncestorsAndSelf().OfType<ExpressionSyntax>().FirstOrDefault() is not { } expr)
                return null;

            while (true)
            {
                switch (expr.Parent)
                {
                    case MemberAccessExpressionSyntax member when member.Name == expr:
                        expr = member; continue;
                    case MemberBindingExpressionSyntax binding when binding.Name == expr:
                        expr = binding; continue;
                    case InvocationExpressionSyntax invocation when invocation.Expression == expr:
                        expr = invocation; continue;
                    case ElementAccessExpressionSyntax element when element.Expression == expr:
                        expr = element; continue;
                    case PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.PointerIndirectionExpression } deref when deref.Operand == expr:
                        expr = deref; continue;
                    default:
                        return expr;
                }
            }
        }

        private static bool UseExpressionForm(ExpressionSyntax operation, StatementSyntax? statement, SemanticModel semanticModel)
        {
            // No statement to wrap (field/property/constructor initializer).
            if (statement is null)
                return true;

            // Structural positions where an unsafe block cannot appear.
            foreach (var ancestor in operation.AncestorsAndSelf())
            {
                if (ancestor == statement)
                    break;
                switch (ancestor)
                {
                    case AwaitExpressionSyntax:
                    case CatchFilterClauseSyntax:
                    case ConstructorInitializerSyntax:
                    case LambdaExpressionSyntax:
                    case AnonymousMethodExpressionSyntax:
                    case QueryExpressionSyntax:
                        return true;
                }
            }

            // Wrapping the statement in a block would be illegal or would shorten a scope.
            if (statement.DescendantNodes().Any(static n => n is AwaitExpressionSyntax or YieldStatementSyntax))
                return true;
            if (statement is LocalDeclarationStatementSyntax { UsingKeyword.RawKind: not (int)SyntaxKind.None })
                return true;
            if (statement is LocalDeclarationStatementSyntax { Declaration.Type: RefTypeSyntax or ScopedTypeSyntax })
                return true;
            if (statement.GetLeadingTrivia().Any(static t => t.IsDirective))
                return true;

            // Declaration expressions ('out var x') and pattern variables ('is T y') leak into the enclosing
            // scope; moving the statement into a block would hide them from later references. The expression
            // form keeps them in scope. Don't descend into nested functions, whose declarations don't leak.
            if (statement.DescendantNodes(static n => n is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax))
                    .Any(static n => n is DeclarationExpressionSyntax or SingleVariableDesignationSyntax))
                return true;

            // A local declaration whose initializer (not its type) needs the unsafe context reads best as a
            // split: the declaration stays in the outer scope and only the initializer moves into an 'unsafe'
            // block, which keeps the variable in scope and gives the safety comment its own clean line. Prefer
            // that block form over an embedded 'unsafe(...)'.
            if (IsSplittableLocalDeclaration(statement, operation, semanticModel))
                return false;

            // The operation is a value embedded inside the statement: an expression keeps the scope minimal.
            // If it sits at the very start of the statement, an 'unsafe(...)' expression cannot be used there
            // (the parser reads 'unsafe' as a block), so fall through to the block form.
            return operation.SpanStart != statement.SpanStart && !ReturnsVoid(operation, semanticModel);
        }

        // A local declaration whose initializer needs an unsafe context can be rewritten as a bare declaration
        // plus an 'unsafe' block that assigns it, instead of an embedded 'unsafe(...)' expression. This is only
        // valid when the declaration itself is legal outside an unsafe context and splitting won't change the
        // meaning of the code.
        private static bool IsSplittableLocalDeclaration(StatementSyntax? statement, ExpressionSyntax operation, SemanticModel semanticModel)
        {
            if (statement is not LocalDeclarationStatementSyntax local ||
                !local.UsingKeyword.IsKind(SyntaxKind.None) ||          // 'using' declaration: block would end its scope early
                local.Modifiers.Any(SyntaxKind.ConstKeyword) ||         // 'const' can't be assigned separately
                local.Declaration.Variables.Count != 1)
                return false;

            var declaredType = local.Declaration.Type;
            var variable = local.Declaration.Variables[0];
            if (variable.Initializer is not { Value: { } value })
                return false;

            // 'ref'/'scoped' locals already prefer the expression form; 'var' would need its inferred type
            // reconstructed to form a bare declaration.
            if (declaredType is RefTypeSyntax or ScopedTypeSyntax || declaredType.IsVar)
                return false;

            // Only split when the flagged operation lives in the initializer (the only part moved into the block).
            if (!value.Span.Contains(operation.Span))
                return false;

            // The declared type must be legal without an unsafe context; a pointer/function-pointer declaration
            // would itself require 'unsafe', so a bare declaration wouldn't compile.
            var symbol = semanticModel.GetDeclaredSymbol(variable);
            if (symbol is not ILocalSymbol { Type: { } type } ||
                type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer)
                return false;

            // The statement must sit directly in a statement list so it can be replaced by two statements.
            if (statement.Parent is not (BlockSyntax or SwitchSectionSyntax))
                return false;

            // Splitting gives a ref-struct local (identified by its stackalloc initializer) a 'scoped' declaration
            // whose escape scope is narrower than the 'T x = stackalloc ...' form the compiler infers inline.
            // That's only safe when the value stays local; if a value derived from it flows to a wider scope
            // (returned, assigned outward, or passed by ref/out) keep the expression form so the code compiles.
            return !IsRefStructLocal(type, variable) || !ReferenceEscapes(local, variable, symbol, semanticModel);
        }

        // A local is treated as ref-struct-like (so it needs 'scoped' when split, and escape-checking) when its
        // type is ref-like or its initializer contains a stackalloc: the only IL5006 case that produces a
        // ref-struct local is a stackalloc, and its target is always Span/ReadOnlySpan.
        private static bool IsRefStructLocal(ITypeSymbol type, VariableDeclaratorSyntax variable) =>
            type.IsRefLikeType ||
            (variable.Initializer?.Value.DescendantNodesAndSelf().Any(static n =>
                n is StackAllocArrayCreationExpressionSyntax or ImplicitStackAllocArrayCreationExpressionSyntax) ?? false);

        // Conservatively (and purely syntactically, since the migration's reference-swapped compilation can't be
        // trusted for type resolution) reports whether any value derived from the local escapes to a wider scope.
        private static bool ReferenceEscapes(LocalDeclarationStatementSyntax declaration, VariableDeclaratorSyntax variable, ISymbol symbol, SemanticModel semanticModel)
        {
            SyntaxNode? container = declaration.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>();
            container ??= declaration.FirstAncestorOrSelf<AccessorDeclarationSyntax>();
            container ??= declaration.FirstAncestorOrSelf<LocalFunctionStatementSyntax>();
            if (container is null)
                return true;

            var name = variable.Identifier.ValueText;
            foreach (var reference in container.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                if (reference.Identifier.ValueText == name &&
                    SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(reference).Symbol, symbol) &&
                    ReferenceFlowsOut(reference))
                    return true;
            }

            return false;
        }

        // True when a value projected from the reference (via member/element access, invocation, cast, ...) is
        // returned, assigned to another target, or passed by ref/out. Passing the variable (or a projection) as
        // a by-value argument does not let it escape, so those references are considered safe.
        private static bool ReferenceFlowsOut(ExpressionSyntax reference)
        {
            var derived = reference;
            while (derived.Parent is ExpressionSyntax parent)
            {
                if ((parent is MemberAccessExpressionSyntax member && member.Expression == derived) ||
                    (parent is ElementAccessExpressionSyntax element && element.Expression == derived) ||
                    (parent is InvocationExpressionSyntax invocation && invocation.Expression == derived) ||
                    (parent is ConditionalAccessExpressionSyntax conditional && conditional.Expression == derived) ||
                    parent is ParenthesizedExpressionSyntax or CastExpressionSyntax)
                {
                    derived = parent;
                    continue;
                }

                break;
            }

            return derived.Parent switch
            {
                ReturnStatementSyntax { Expression: { } returned } when returned == derived => true,
                ArrowExpressionClauseSyntax arrow when arrow.Expression == derived => true,
                AssignmentExpressionSyntax assignment when assignment.Right == derived => true,
                ArgumentSyntax { RefKindKeyword.RawKind: not (int)SyntaxKind.None } => true,
                _ => false,
            };
        }

        // Rewrites 'T x = <needs-unsafe>;' as 'T x;' followed by 'unsafe { x = <needs-unsafe>; }'. Ref-struct
        // locals get 'scoped' so the stack-referencing value assigned inside the block can't escape further
        // than the inline initializer form would have allowed. Returns null if the declaration isn't splittable.
        private static SyntaxNode? TrySplitLocalDeclaration(SyntaxNode root, StatementSyntax statement, ExpressionSyntax operation, SemanticModel semanticModel)
        {
            if (!IsSplittableLocalDeclaration(statement, operation, semanticModel))
                return null;

            var local = (LocalDeclarationStatementSyntax)statement;
            var variable = local.Declaration.Variables[0];
            var value = variable.Initializer!.Value;
            var type = ((ILocalSymbol)semanticModel.GetDeclaredSymbol(variable)!).Type;

            // Built with explicit spacing rather than Formatter.Annotation, which would strip the blank line
            // inserted between the declaration and the block. A ref-struct local (its initializer is a
            // stackalloc) gets 'scoped' so the stack-referencing value assigned inside the block can't escape
            // further than the inline form allowed.
            bool needsScoped = IsRefStructLocal(type, variable);

            TypeSyntax declaredType = local.Declaration.Type.WithoutTrivia().WithTrailingTrivia(SyntaxFactory.Space);
            if (needsScoped)
                declaredType = SyntaxFactory.ScopedType(
                    SyntaxFactory.Token(SyntaxKind.ScopedKeyword).WithTrailingTrivia(SyntaxFactory.Space), declaredType);

            // Use only the statement's indentation (not its full leading trivia, which may carry a preceding
            // blank line) for the block, so exactly one blank line separates the declaration from the block.
            var leading = statement.GetLeadingTrivia();
            var indent = leading.Count > 0 && leading[leading.Count - 1].IsKind(SyntaxKind.WhitespaceTrivia)
                ? SyntaxFactory.TriviaList(leading[leading.Count - 1])
                : default;

            var newLine = SyntaxFactory.ElasticCarriageReturnLineFeed;
            var declarationStatement = SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        declaredType,
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(variable.Identifier.WithoutTrivia()))))
                .WithLeadingTrivia(leading)
                .WithTrailingTrivia(newLine, newLine);

            var assignment = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(variable.Identifier.WithoutTrivia()),
                    value.WithoutLeadingTrivia().WithoutTrailingTrivia()));

            var unsafeBlock = MakeUnsafeBlock([assignment])
                .WithLeadingTrivia(indent)
                .WithTrailingTrivia(statement.GetTrailingTrivia());

            return root.ReplaceNode(statement, new SyntaxNode[] { declarationStatement, unsafeBlock });
        }

        private static bool ReturnsVoid(ExpressionSyntax operation, SemanticModel semanticModel) =>
            semanticModel.GetTypeInfo(operation).Type is { SpecialType: SpecialType.System_Void };

        private static SyntaxNode WrapExpression(SyntaxNode root, ExpressionSyntax operation, SemanticModel semanticModel)
        {
            if (ReturnsVoid(operation, semanticModel))
                return root; // Cannot wrap a void value in an unsafe expression.

            return MakeUnsafeExpression(operation) is { } wrapped ? root.ReplaceNode(operation, wrapped) : root;
        }

        private static SyntaxNode WrapStatement(SyntaxNode root, StatementSyntax statement)
        {
            var wrapped = MakeUnsafeBlock([statement.WithoutLeadingTrivia().WithoutTrailingTrivia()])
                .WithLeadingTrivia(statement.GetLeadingTrivia())
                .WithTrailingTrivia(statement.GetTrailingTrivia());
            return root.ReplaceNode(statement, wrapped);
        }

        // Converts a void expression-bodied member ('void M() => VoidCall();') to a block body wrapping the call
        // in an 'unsafe' block. Returns the original root unchanged if the shape isn't a supported member body.
        private static SyntaxNode TryConvertExpressionBodyToBlock(SyntaxNode root, ExpressionSyntax operation)
        {
            if (operation.FirstAncestorOrSelf<ArrowExpressionClauseSyntax>() is not { Parent: { } member } arrow)
                return root;

            var block = SyntaxFactory.Block(MakeUnsafeBlock([SyntaxFactory.ExpressionStatement(arrow.Expression.WithoutTrivia())]));

            SyntaxNode? converted = member switch
            {
                MethodDeclarationSyntax m => m.WithExpressionBody(null).WithSemicolonToken(default).WithBody(block),
                LocalFunctionStatementSyntax lf => lf.WithExpressionBody(null).WithSemicolonToken(default).WithBody(block),
                AccessorDeclarationSyntax a => a.WithExpressionBody(null).WithSemicolonToken(default).WithBody(block),
                OperatorDeclarationSyntax op => op.WithExpressionBody(null).WithSemicolonToken(default).WithBody(block),
                DestructorDeclarationSyntax d => d.WithExpressionBody(null).WithSemicolonToken(default).WithBody(block),
                _ => null,
            };

            return converted is null ? root : root.ReplaceNode(member, converted.WithAdditionalAnnotations(Formatter.Annotation));
        }

        private static ExpressionSyntax? MakeUnsafeExpression(ExpressionSyntax inner)
        {
            var text = inner.WithoutLeadingTrivia().WithoutTrailingTrivia().ToFullString();
            var wrapped = SyntaxFactory.ParseExpression($"unsafe({text})", options: s_previewOptions);

            // Bail out rather than corrupt the source if the 'unsafe(...)' expression did not round-trip.
            if (wrapped.ContainsDiagnostics)
                return null;

            // No Formatter.Annotation: the expression is replaced in place and formatting the new 'unsafe'
            // keyword would insert an unwanted space before '(' ("unsafe (x)").
            return wrapped
                .WithLeadingTrivia(inner.GetLeadingTrivia().Add(SyntaxFactory.Comment(ExpressionComment)).Add(SyntaxFactory.Space))
                .WithTrailingTrivia(inner.GetTrailingTrivia());
        }

        private static UnsafeStatementSyntax MakeUnsafeBlock(IEnumerable<StatementSyntax> statements)
        {
            var inner = statements.Select(static s => s.WithoutLeadingTrivia()).ToList();
            inner[0] = inner[0].WithLeadingTrivia(SyntaxFactory.Comment(BlockComment), SyntaxFactory.ElasticCarriageReturnLineFeed);
            return SyntaxFactory.UnsafeStatement(SyntaxFactory.Block(inner)).WithAdditionalAnnotations(Formatter.Annotation);
        }
    }
}
#endif
