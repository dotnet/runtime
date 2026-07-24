// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix
{
    /// <summary>
    /// Fixes unsafe-context diagnostics by introducing the narrowest context that preserves source semantics.
    /// Unsafe statements are preferred so the generated audit comment can be expanded during review.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddUnsafeContextCodeFixProvider)), Shared]
    public sealed class AddUnsafeContextCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        public const string UnsafeOperationDiagnosticId = "CS9360";
        public const string UnsafeUninitializedStackAllocDiagnosticId = "CS9361";
        public const string UnsafeMemberOperationDiagnosticId = "CS9362";
        public const string UnsafeMemberOperationCompatDiagnosticId = "CS9363";
        public const string UnsafeConstructorConstraintDiagnosticId = "CS9376";

        internal const string SafetyAuditComment = "// SAFETY: Audit";

        private const string UnsafeExpressionPlaceholder = "__unsafeExpression";

        /// <summary>
        /// How many audited-but-safe statements may be absorbed when merging a new unsafe region with an adjacent
        /// generated one. Merging trades a slightly wider audit scope for far fewer unsafe blocks per member; keep
        /// this small so that migration does not silently blanket unrelated code.
        /// </summary>
        private const int MaxSafeStatementsBetweenMergedRegions = 1;

        private static readonly SymbolDisplayFormat s_localTypeDisplayFormat =
            SymbolDisplayFormat.MinimallyQualifiedFormat.WithMiscellaneousOptions(
                SymbolDisplayFormat.MinimallyQualifiedFormat.MiscellaneousOptions
                    | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
                    | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
                    | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static LocalizableString CodeFixTitle =>
            new LocalizableResourceString(
                nameof(Resources.AddUnsafeContextCodeFixTitle),
                Resources.ResourceManager,
                typeof(Resources));

        public override ImmutableArray<string> FixableDiagnosticIds =>
            [
                UnsafeOperationDiagnosticId,
                UnsafeUninitializedStackAllocDiagnosticId,
                UnsafeMemberOperationDiagnosticId,
                UnsafeMemberOperationCompatDiagnosticId,
                UnsafeConstructorConstraintDiagnosticId,
            ];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics[0];
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null)
                return;

            TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;
            SyntaxNode targetNode = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);
            string title = CodeFixTitle.ToString();

            // Attribute applications are deliberately not suppressible under the updated language rules.
            if (targetNode.AncestorsAndSelf().OfType<AttributeSyntax>().Any())
                return;

            if (TryGetConstructorRequiringUnsafe(diagnostic, targetNode) is { } constructor)
            {
                RegisterFix(
                    context,
                    title,
                    cancellationToken => MarkConstructorUnsafeAsync(
                        context.Document,
                        constructor,
                        cancellationToken));
                return;
            }

            if (diagnostic.Id == UnsafeConstructorConstraintDiagnosticId
                && targetNode.AncestorsAndSelf().OfType<UsingDirectiveSyntax>().FirstOrDefault() is { } usingDirective)
            {
                RegisterFix(
                    context,
                    title,
                    _ => Task.FromResult(AddUnsafeToUsingDirective(context.Document, root, usingDirective)));
                return;
            }

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (semanticModel is null)
                return;

            if (IsExpressionOnlyContext(targetNode, diagnosticSpan))
            {
                TryRegisterExpressionFix(context, root, semanticModel, targetNode, diagnosticSpan, title);
                return;
            }

            ArrowExpressionClauseSyntax? arrowExpression = targetNode.AncestorsAndSelf()
                .OfType<ArrowExpressionClauseSyntax>()
                .FirstOrDefault(arrow => arrow.Expression.FullSpan.Contains(diagnosticSpan));
            if (arrowExpression is not null)
            {
                if (arrowExpression.Parent is AnonymousFunctionExpressionSyntax
                    || arrowExpression.Expression.DescendantNodesAndSelf().OfType<AwaitExpressionSyntax>().Any()
                    || HasDirectiveWithinSpan(arrowExpression))
                {
                    TryRegisterExpressionFix(context, root, semanticModel, targetNode, diagnosticSpan, title);
                }
                else
                {
                    RegisterFix(
                        context,
                        title,
                        cancellationToken => ConvertExpressionBodyAsync(
                            context.Document,
                            root,
                            semanticModel,
                            arrowExpression,
                            cancellationToken));
                }
                return;
            }

            StatementSyntax? containingStatement = targetNode.AncestorsAndSelf()
                .OfType<StatementSyntax>()
                .FirstOrDefault(static statement => statement is not BlockSyntax);
            while (containingStatement?.Parent is LabeledStatementSyntax labeledStatement)
                containingStatement = labeledStatement;

            if (containingStatement is null)
            {
                TryRegisterExpressionFix(context, root, semanticModel, targetNode, diagnosticSpan, title);
                return;
            }

            if (containingStatement is LocalDeclarationStatementSyntax localDeclaration)
            {
                if (TryCreateSplitLocalDeclaration(
                    diagnostic,
                    localDeclaration,
                    semanticModel,
                    context.CancellationToken,
                    out LocalDeclarationStatementSyntax forwardDeclaration,
                    out StatementSyntax assignmentStatement))
                {
                    RegisterFix(
                        context,
                        title,
                        _ => Task.FromResult(ReplaceStatementWithStatements(
                            context.Document,
                            root,
                            localDeclaration,
                            [forwardDeclaration, CreateUnsafeStatement(assignmentStatement)])));
                    return;
                }

                if (TryRegisterExpressionFix(context, root, semanticModel, targetNode, diagnosticSpan, title))
                    return;

                if (!localDeclaration.AwaitKeyword.IsKind(SyntaxKind.None))
                    return;

                if (TryGetEnclosingSwitch(localDeclaration) is { } localSwitch)
                {
                    RegisterEnclosingSwitchFix(context, root, localSwitch, title);
                    return;
                }

                if (StatementRange.TryCreateForStatement(localDeclaration, out StatementRange localRange, out int localIndex))
                {
                    TryRegisterStatementRangeFix(
                        context,
                        root,
                        semanticModel,
                        localRange,
                        localIndex,
                        forceThroughContainerEnd: IsUsingDeclaration(localDeclaration),
                        title);
                }
                return;
            }

            bool cannotContainUnsafeStatement = containingStatement.DescendantNodesAndSelf()
                .OfType<YieldStatementSyntax>()
                .Any();
            bool topLevelScopeSensitive = containingStatement.Parent is GlobalStatementSyntax
                && GetDeclaredNames(containingStatement).Count > 0;
            bool containsAwaitStatement = containingStatement switch
            {
                CommonForEachStatementSyntax forEach =>
                    !forEach.AwaitKeyword.IsKind(SyntaxKind.None),
                UsingStatementSyntax usingStatement =>
                    !usingStatement.AwaitKeyword.IsKind(SyntaxKind.None),
                _ => false,
            };
            bool preferExpression = containingStatement is YieldStatementSyntax
                || containingStatement.DescendantNodesAndSelf().OfType<AwaitExpressionSyntax>().Any()
                || containsAwaitStatement
                || topLevelScopeSensitive
                || HasDirectiveWithinSpan(containingStatement)
                || WouldNarrowDeclaredVariableScope(
                    containingStatement,
                    semanticModel,
                    context.CancellationToken);

            if ((cannotContainUnsafeStatement || preferExpression)
                && TryRegisterExpressionFix(context, root, semanticModel, targetNode, diagnosticSpan, title))
            {
                return;
            }

            bool hasStatementList = StatementRange.TryCreateForStatement(
                containingStatement,
                out StatementRange statements,
                out int statementIndex);

            if (topLevelScopeSensitive && hasStatementList)
            {
                TryRegisterStatementRangeFix(
                    context,
                    root,
                    semanticModel,
                    statements,
                    statementIndex,
                    forceThroughContainerEnd: true,
                    title);
                return;
            }

            if (cannotContainUnsafeStatement || containsAwaitStatement)
                return;

            if (preferExpression && TryGetEnclosingSwitch(containingStatement) is { } containingSwitch)
            {
                RegisterEnclosingSwitchFix(context, root, containingSwitch, title);
                return;
            }

            if (hasStatementList)
            {
                TryRegisterStatementRangeFix(
                    context,
                    root,
                    semanticModel,
                    statements,
                    statementIndex,
                    forceThroughContainerEnd: false,
                    title);
                return;
            }

            if (preferExpression)
                return;

            // Embedded statements (such as the body of an 'if' without braces) have no statement list to extend.
            RegisterFix(
                context,
                title,
                _ => Task.FromResult(ReplaceStatementWithStatements(
                    context.Document,
                    root,
                    containingStatement,
                    [CreateUnsafeStatement(containingStatement)])));
        }

        private static void RegisterFix(
            CodeFixContext context,
            string title,
            System.Func<CancellationToken, Task<Document>> createChangedDocument) =>
            context.RegisterCodeFix(
                CodeAction.Create(title, createChangedDocument, title),
                context.Diagnostics);

        private static void RegisterEnclosingSwitchFix(
            CodeFixContext context,
            SyntaxNode root,
            SwitchStatementSyntax switchStatement,
            string title)
        {
            if (ContainsAwaitOrYield(switchStatement))
                return;

            RegisterFix(
                context,
                title,
                _ => Task.FromResult(ReplaceStatementWithStatements(
                    context.Document,
                    root,
                    switchStatement,
                    [CreateUnsafeStatement(switchStatement)])));
        }

        private static bool TryRegisterExpressionFix(
            CodeFixContext context,
            SyntaxNode root,
            SemanticModel semanticModel,
            SyntaxNode targetNode,
            TextSpan diagnosticSpan,
            string title)
        {
            if (TryGetUnsafeExpressionFix(
                targetNode,
                diagnosticSpan,
                semanticModel,
                context.CancellationToken) is not { } fix)
            {
                return false;
            }

            (ExpressionSyntax expression, ExpressionSyntax operand, ExpressionSyntax template) = fix;
            RegisterFix(
                context,
                title,
                _ => Task.FromResult(ReplaceWithUnsafeExpression(
                    context.Document,
                    root,
                    expression,
                    operand,
                    template)));
            return true;
        }

        private static bool TryRegisterStatementRangeFix(
            CodeFixContext context,
            SyntaxNode root,
            SemanticModel semanticModel,
            in StatementRange statements,
            int statementIndex,
            bool forceThroughContainerEnd,
            string title)
        {
            if (!TryGetStatementRangeToWrap(
                statements,
                statementIndex,
                semanticModel,
                context.CancellationToken,
                forceThroughContainerEnd,
                out int start,
                out int end))
            {
                return false;
            }

            (int mergedStart, int mergedEnd) = ExtendRangeOverGeneratedUnsafeStatements(statements, start, end);
            if (mergedStart != start || mergedEnd != end)
            {
                if (CanWrapStatementRange(statements, mergedStart, mergedEnd, semanticModel, context.CancellationToken))
                {
                    (start, end) = (mergedStart, mergedEnd);
                }
            }

            if (!CanWrapStatementRange(statements, start, end, semanticModel, context.CancellationToken))
                return false;

            StatementRange range = statements;
            RegisterFix(
                context,
                title,
                _ => Task.FromResult(WrapStatementRange(context.Document, root, range, start, end)));
            return true;
        }

        private static ConstructorDeclarationSyntax? TryGetConstructorRequiringUnsafe(
            Diagnostic diagnostic,
            SyntaxNode targetNode)
        {
            if (diagnostic.Id is not (UnsafeMemberOperationDiagnosticId or UnsafeMemberOperationCompatDiagnosticId))
                return null;

            if (targetNode.AncestorsAndSelf().OfType<ConstructorInitializerSyntax>().FirstOrDefault() is { } initializer
                && diagnostic.Location.SourceSpan.Contains(initializer.Span))
            {
                return initializer.Parent as ConstructorDeclarationSyntax;
            }

            if (targetNode.AncestorsAndSelf().OfType<ConstructorDeclarationSyntax>().FirstOrDefault() is { } constructor
                && constructor.Initializer is null
                && diagnostic.Location.SourceSpan.Contains(constructor.Span))
            {
                return constructor;
            }

            return null;
        }

        /// <summary>
        /// An unsafe constructor is the only way to establish an unsafe context for its initializer, so this widens
        /// the constructor's own contract and is documented like any other caller-unsafe declaration.
        /// </summary>
        private static async Task<Document> MarkConstructorUnsafeAsync(
            Document document,
            ConstructorDeclarationSyntax constructor,
            CancellationToken cancellationToken)
        {
            document = await UnsafeModifierCodeFixHelpers.SetUnsafeModifierAsync(
                document,
                constructor,
                cancellationToken).ConfigureAwait(false);
            return await UnsafeModifierCodeFixHelpers.AddPendingSafetyDocumentationAsync(
                document,
                cancellationToken).ConfigureAwait(false);
        }

        private static bool IsExpressionOnlyContext(SyntaxNode targetNode, TextSpan diagnosticSpan)
        {
            if (targetNode.AncestorsAndSelf().OfType<CatchFilterClauseSyntax>()
                .Any(filter => filter.FilterExpression.FullSpan.Contains(diagnosticSpan)))
            {
                return true;
            }

            if (targetNode.AncestorsAndSelf().OfType<ConstructorInitializerSyntax>()
                .Any(initializer => initializer.ArgumentList.FullSpan.Contains(diagnosticSpan)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Builds the unsafe expression template with the parse options of the document being fixed, so the generated
        /// syntax matches the language mode the project compiles with. Returns <see langword="null"/> when that mode
        /// cannot express an unsafe expression, or when the loaded compiler predates the syntax.
        /// </summary>
        private static ExpressionSyntax? TryParseUnsafeExpressionTemplate(SyntaxNode node)
        {
            ExpressionSyntax template = SyntaxFactory.ParseExpression(
                $"unsafe(/* SAFETY: Audit */ {UnsafeExpressionPlaceholder})",
                options: node.SyntaxTree.Options as CSharpParseOptions);
            return !template.ContainsDiagnostics && TryFindPlaceholder(template) is not null
                ? template
                : null;
        }

        private static IdentifierNameSyntax? TryFindPlaceholder(ExpressionSyntax expression) =>
            expression.DescendantNodesAndSelf()
                .OfType<IdentifierNameSyntax>()
                .FirstOrDefault(static identifier => identifier.Identifier.ValueText == UnsafeExpressionPlaceholder);

        /// <summary>
        /// Builds the expression-level fix, or returns <see langword="null"/> when no semantics-preserving unsafe
        /// expression can be written for the reported operation.
        /// </summary>
        private static (ExpressionSyntax Expression, ExpressionSyntax Operand, ExpressionSyntax Template)? TryGetUnsafeExpressionFix(
            SyntaxNode targetNode,
            TextSpan diagnosticSpan,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (TryGetUnsafeExpression(targetNode, diagnosticSpan) is not { } expression
                || !CanUseUnsafeExpression(expression)
                || TryParseUnsafeExpressionTemplate(expression) is not { } template)
            {
                return null;
            }

            // Reaching here from inside an unsafe expression means the compiler does not treat that expression as an
            // unsafe context for this operation, so wrapping it again would only nest without fixing anything.
            if (expression.AncestorsAndSelf().Any(node => node.RawKind == template.RawKind))
                return null;

            if (TryGetUnsafeExpressionOperand(semanticModel, expression, cancellationToken) is not { } operand)
                return null;

            return (expression, operand, template);
        }

        private static ExpressionSyntax? TryGetUnsafeExpression(SyntaxNode targetNode, TextSpan diagnosticSpan) =>
            PromoteUnsafeExpression(
                targetNode.AncestorsAndSelf()
                .OfType<ExpressionSyntax>()
                .FirstOrDefault(expression => expression.FullSpan.Contains(diagnosticSpan)),
                diagnosticSpan);

        private static ExpressionSyntax? PromoteUnsafeExpression(
            ExpressionSyntax? expression,
            TextSpan diagnosticSpan)
        {
            if (expression is null)
                return null;

            foreach (SyntaxNode ancestor in expression.AncestorsAndSelf())
            {
                switch (ancestor)
                {
                    case ConditionalAccessExpressionSyntax conditionalAccess
                        when conditionalAccess.WhenNotNull.FullSpan.Contains(diagnosticSpan):
                        expression = conditionalAccess;
                        break;

                    case BaseObjectCreationExpressionSyntax objectCreation
                        when objectCreation.Initializer?.FullSpan.Contains(diagnosticSpan) == true:
                        expression = objectCreation;
                        break;

                    case WithExpressionSyntax withExpression
                        when withExpression.Initializer.FullSpan.Contains(diagnosticSpan):
                        expression = withExpression;
                        break;

                    case CollectionExpressionSyntax collectionExpression
                        when collectionExpression.FullSpan.Contains(diagnosticSpan):
                        expression = collectionExpression;
                        break;
                }
            }

            return expression;
        }

        private static bool CanUseUnsafeExpression(ExpressionSyntax expression) =>
            !expression.DescendantNodesAndSelf().OfType<AwaitExpressionSyntax>().Any()
            && (expression.Parent switch
            {
                ExpressionStatementSyntax { Expression: var statementExpression }
                    when statementExpression == expression => false,
                ForStatementSyntax forStatement
                    when forStatement.Initializers.Contains(expression)
                        || forStatement.Incrementors.Contains(expression) => false,
                AttributeArgumentSyntax => false,
                // An unsafe expression cannot be the target of an assignment or an increment, and wrapping the right
                // side of a deconstruction does not put the generated Deconstruct call in an unsafe context.
                AssignmentExpressionSyntax assignment =>
                    assignment.Left != expression
                    && assignment.Left is not (DeclarationExpressionSyntax or TupleExpressionSyntax),
                PrefixUnaryExpressionSyntax prefix =>
                    !prefix.IsKind(SyntaxKind.PreIncrementExpression)
                    && !prefix.IsKind(SyntaxKind.PreDecrementExpression),
                PostfixUnaryExpressionSyntax => false,
                _ => true,
            });

        /// <summary>
        /// Produces the expression to place inside <c>unsafe(...)</c>. Conversions, property and indexer accessors,
        /// and method group conversions are bound by the enclosing binder, so they only enter the unsafe context when
        /// an explicit cast forces them inside it.
        /// </summary>
        private static ExpressionSyntax? TryGetUnsafeExpressionOperand(
            SemanticModel semanticModel,
            ExpressionSyntax expression,
            CancellationToken cancellationToken)
        {
            ExpressionSyntax operand = expression.WithoutLeadingTrivia().WithoutTrailingTrivia();
            if (TryGetRequiredCastType(semanticModel, expression, cancellationToken, out bool requiresCast) is { } castType)
            {
                if (TryParseTypeName(semanticModel, castType, expression.SpanStart) is not { } castTypeSyntax)
                    return null;

                if (NeedsParenthesesForCast(operand))
                    operand = SyntaxFactory.ParenthesizedExpression(operand);

                return SyntaxFactory.CastExpression(castTypeSyntax, operand);
            }

            return requiresCast ? null : operand;
        }

        private static ITypeSymbol? TryGetRequiredCastType(
            SemanticModel semanticModel,
            ExpressionSyntax expression,
            CancellationToken cancellationToken,
            out bool requiresCast)
        {
            requiresCast = true;
            TypeInfo typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);

            // A user-defined implicit conversion is applied to the result of the unsafe expression; casting to its
            // result type also pulls any receiver evaluation inside, so it is checked first.
            Conversion conversion = semanticModel.GetConversion(expression, cancellationToken);
            if (conversion.IsUserDefined
                && conversion.IsImplicit
                && conversion.MethodSymbol is { } conversionOperator)
            {
                return GetUserDefinedConversionResultType(
                    semanticModel,
                    expression,
                    conversionOperator,
                    cancellationToken);
            }

            ISymbol? symbol = semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol;
            if (typeInfo.Type is null && symbol is IMethodSymbol)
            {
                return typeInfo.ConvertedType is INamedTypeSymbol { TypeKind: TypeKind.Delegate } delegateType
                    ? delegateType
                    : null;
            }

            if (symbol is IPropertySymbol property)
            {
                // A cast would drop the reference, so a ref-returning accessor cannot be fixed this way.
                return property.RefKind == RefKind.None
                    ? WithFlowNullability(typeInfo, property.Type)
                    : null;
            }

            requiresCast = false;
            return null;
        }

        private static ITypeSymbol WithFlowNullability(TypeInfo typeInfo, ITypeSymbol type) =>
            typeInfo.Nullability.FlowState == NullableFlowState.NotNull && type.IsReferenceType
                ? type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
                : type;

        private static bool NeedsParenthesesForCast(ExpressionSyntax expression) =>
            expression is BinaryExpressionSyntax
                or AssignmentExpressionSyntax
                or ConditionalExpressionSyntax
                or IsPatternExpressionSyntax
                or SwitchExpressionSyntax
                or AnonymousFunctionExpressionSyntax
                or QueryExpressionSyntax
                or RangeExpressionSyntax
                or WithExpressionSyntax
                or ThrowExpressionSyntax
                // A cast followed by '&', '*', '+' or '-' is parsed as a binary operator when the cast type is also a
                // valid expression, so unary operands always get parentheses.
                or PrefixUnaryExpressionSyntax;

        /// <summary>
        /// Renders a type as source, returning <see langword="null"/> for types that cannot be named at that position.
        /// </summary>
        private static TypeSyntax? TryParseTypeName(
            SemanticModel semanticModel,
            ITypeSymbol type,
            int position)
        {
            if (type is IErrorTypeSymbol || type.SpecialType == SpecialType.System_Void || ContainsAnonymousType(type))
                return null;

            string displayString = type.ToMinimalDisplayString(semanticModel, position, s_localTypeDisplayFormat);
            TypeSyntax parsedType = SyntaxFactory.ParseTypeName(displayString);
            return parsedType.ContainsDiagnostics || parsedType.ToFullString() != displayString
                ? null
                : parsedType;
        }

        private static ITypeSymbol GetUserDefinedConversionResultType(
            SemanticModel semanticModel,
            ExpressionSyntax expression,
            IMethodSymbol conversionOperator,
            CancellationToken cancellationToken)
        {
            ITypeSymbol resultType = conversionOperator.ReturnType;
            ITypeSymbol? sourceType = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
            if (sourceType is INamedTypeSymbol sourceNamedType
                && sourceNamedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                && sourceNamedType.TypeArguments.Length == 1
                && conversionOperator.Parameters.Length == 1
                && SymbolEqualityComparer.Default.Equals(
                    sourceNamedType.TypeArguments[0],
                    conversionOperator.Parameters[0].Type)
                && resultType.IsValueType
                && resultType.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
            {
                INamedTypeSymbol nullableType = semanticModel.Compilation.GetSpecialType(
                    SpecialType.System_Nullable_T);
                if (nullableType.TypeKind != TypeKind.Error)
                    resultType = nullableType.Construct(resultType);
            }

            return resultType;
        }

        private static bool TryCreateSplitLocalDeclaration(
            Diagnostic diagnostic,
            LocalDeclarationStatementSyntax localDeclaration,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out LocalDeclarationStatementSyntax forwardDeclaration,
            out StatementSyntax assignmentStatement)
        {
            forwardDeclaration = null!;
            assignmentStatement = null!;

            if (diagnostic.Id == UnsafeConstructorConstraintDiagnosticId
                || localDeclaration.IsConst
                || IsUsingDeclaration(localDeclaration)
                || localDeclaration.Parent is not BlockSyntax and not SwitchSectionSyntax
                || HasDirectiveWithinSpan(localDeclaration)
                || localDeclaration.Declaration.Variables.Count != 1
                || IsRefType(localDeclaration.Declaration.Type))
            {
                return false;
            }

            VariableDeclaratorSyntax variable = localDeclaration.Declaration.Variables[0];
            if (variable.Initializer is not { } initializer
                || !initializer.Value.FullSpan.Contains(diagnostic.Location.SourceSpan)
                || initializer.Value.DescendantNodesAndSelf().OfType<AwaitExpressionSyntax>().Any())
            {
                return false;
            }

            var localSymbol = semanticModel.GetDeclaredSymbol(variable, cancellationToken) as ILocalSymbol;
            if (localSymbol is null || localSymbol.Type is IErrorTypeSymbol || ContainsAnonymousType(localSymbol.Type))
                return false;

            if (InitializerVariablesEscape(
                localDeclaration,
                initializer.Value,
                localSymbol,
                semanticModel,
                cancellationToken))
            {
                return false;
            }

            TypeSyntax type = localDeclaration.Declaration.Type;
            if (type.IsVar)
            {
                if (TryParseTypeName(semanticModel, localSymbol.Type, localDeclaration.SpanStart) is not { } inferredType)
                    return false;

                type = inferredType;
            }

            if (localSymbol.Type.IsRefLikeType)
            {
                if (!ContainsStackAllocThatFlowsToLocal(initializer.Value))
                    return false;

                if (type is not ScopedTypeSyntax)
                {
                    type = SyntaxFactory.ScopedType(
                        SyntaxFactory.Token(SyntaxKind.ScopedKeyword)
                            .WithTrailingTrivia(SyntaxFactory.Space),
                        type.WithoutLeadingTrivia());
                }
            }

            VariableDeclaratorSyntax declarationVariable = variable.WithInitializer(null);
            VariableDeclarationSyntax declaration = localDeclaration.Declaration
                .WithType(type.WithTriviaFrom(localDeclaration.Declaration.Type))
                .WithVariables([declarationVariable]);

            forwardDeclaration = localDeclaration
                .WithDeclaration(declaration)
                .WithLeadingTrivia(localDeclaration.GetLeadingTrivia())
                .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
                .WithAdditionalAnnotations(Formatter.Annotation);

            AssignmentExpressionSyntax assignment = SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(variable.Identifier.WithoutTrivia()),
                initializer.EqualsToken,
                initializer.Value.WithoutLeadingTrivia());
            assignmentStatement = SyntaxFactory.ExpressionStatement(assignment)
                .WithTrailingTrivia(localDeclaration.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            return true;
        }

        private static bool InitializerVariablesEscape(
            LocalDeclarationStatementSyntax localDeclaration,
            ExpressionSyntax initializer,
            ILocalSymbol declaredLocal,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            DataFlowAnalysis? analysis = semanticModel.AnalyzeDataFlow(initializer);
            if (analysis is null || !analysis.Succeeded)
                return true;

            var initializerVariables = new HashSet<ISymbol>(
                analysis.VariablesDeclared.Where(symbol => !SymbolEqualityComparer.Default.Equals(symbol, declaredLocal)),
                SymbolEqualityComparer.Default);
            if (initializerVariables.Count == 0
                || !StatementRange.TryCreateForStatement(
                    localDeclaration,
                    out StatementRange statements,
                    out int statementIndex))
            {
                return false;
            }

            IEnumerable<StatementSyntax> laterStatements = GetLaterStatements(
                statements,
                statementIndex,
                localDeclaration);
            return laterStatements.Any(statement => ReferencesAnySymbol(
                statement,
                initializerVariables,
                semanticModel,
                cancellationToken));
        }

        /// <summary>
        /// Enumerates the statements that could observe declarations made by <paramref name="statement"/>, including
        /// the remaining sections of an enclosing switch statement.
        /// </summary>
        private static IEnumerable<StatementSyntax> GetLaterStatements(
            StatementRange statements,
            int statementIndex,
            StatementSyntax statement)
        {
            for (int index = statementIndex + 1; index < statements.Count; index++)
                yield return statements[index];

            if (statement.Parent is SwitchSectionSyntax switchSection
                && switchSection.Parent is SwitchStatementSyntax switchStatement)
            {
                int sectionIndex = switchStatement.Sections.IndexOf(switchSection);
                foreach (SwitchSectionSyntax laterSection in switchStatement.Sections.Skip(sectionIndex + 1))
                {
                    foreach (StatementSyntax laterStatement in laterSection.Statements)
                        yield return laterStatement;
                }
            }
        }

        private static bool IsRefType(TypeSyntax type) =>
            type is RefTypeSyntax
                or ScopedTypeSyntax { Type: RefTypeSyntax };

        private static bool ContainsAnonymousType(ITypeSymbol type) =>
            type switch
            {
                INamedTypeSymbol { IsAnonymousType: true } => true,
                IArrayTypeSymbol array => ContainsAnonymousType(array.ElementType),
                INamedTypeSymbol namedType => namedType.TypeArguments.Any(ContainsAnonymousType),
                _ => false,
            };

        private static bool ContainsStackAllocThatFlowsToLocal(ExpressionSyntax expression) =>
            expression switch
            {
                StackAllocArrayCreationExpressionSyntax or ImplicitStackAllocArrayCreationExpressionSyntax => true,
                ParenthesizedExpressionSyntax parenthesized =>
                    ContainsStackAllocThatFlowsToLocal(parenthesized.Expression),
                CastExpressionSyntax cast =>
                    ContainsStackAllocThatFlowsToLocal(cast.Expression),
                ConditionalExpressionSyntax conditional =>
                    ContainsStackAllocThatFlowsToLocal(conditional.WhenTrue)
                        || ContainsStackAllocThatFlowsToLocal(conditional.WhenFalse),
                SwitchExpressionSyntax switchExpression =>
                    switchExpression.Arms.Any(static arm => ContainsStackAllocThatFlowsToLocal(arm.Expression)),
                _ => false,
            };

        private static bool IsUsingDeclaration(LocalDeclarationStatementSyntax declaration) =>
            !declaration.UsingKeyword.IsKind(SyntaxKind.None);

        private static bool WouldNarrowDeclaredVariableScope(
            StatementSyntax statement,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (!StatementRange.TryCreateForStatement(statement, out StatementRange statements, out int statementIndex))
                return false;

            DataFlowAnalysis? analysis = semanticModel.AnalyzeDataFlow(statement);
            if (analysis is null || !analysis.Succeeded)
                return false;

            var declaredSymbols = new HashSet<ISymbol>(
                analysis.VariablesDeclared,
                SymbolEqualityComparer.Default);
            AddSyntacticallyDeclaredSymbols(
                statement,
                declaredSymbols,
                semanticModel,
                cancellationToken);
            HashSet<string> declaredNames = GetDeclaredNames(statement);
            if (declaredSymbols.Count == 0 && declaredNames.Count == 0)
                return false;

            return GetLaterStatements(statements, statementIndex, statement).Any(laterStatement =>
                ReferencesAnySymbol(
                    laterStatement,
                    declaredSymbols,
                    semanticModel,
                    cancellationToken)
                || ReferencesAnyUnresolvedName(laterStatement, declaredNames, semanticModel, cancellationToken));
        }

        private static bool TryGetStatementRangeToWrap(
            in StatementRange statements,
            int triggerIndex,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            bool forceThroughContainerEnd,
            out int start,
            out int end)
        {
            start = triggerIndex;
            end = forceThroughContainerEnd ? statements.Count - 1 : triggerIndex;
            if (forceThroughContainerEnd)
                return true;

            while (true)
            {
                HashSet<ISymbol>? declaredSymbols = GetDeclaredSymbols(
                    statements,
                    start,
                    end,
                    semanticModel,
                    cancellationToken);
                if (declaredSymbols is null)
                {
                    end = statements.Count - 1;
                    return true;
                }

                // A using declaration disposes at the end of its container, so its unsafe region must reach there.
                for (int i = start; i <= end; i++)
                {
                    if (statements[i] is LocalDeclarationStatementSyntax declaration && IsUsingDeclaration(declaration))
                    {
                        end = statements.Count - 1;
                        return true;
                    }
                }

                var selectedStatements = new List<StatementSyntax>(end - start + 1);
                for (int i = start; i <= end; i++)
                    selectedStatements.Add(statements[i]);
                HashSet<string> declaredNames = GetDeclaredNames(selectedStatements);

                int expandedEnd = end;
                for (int i = end + 1; i < statements.Count; i++)
                {
                    if (ReferencesAnySymbol(statements[i], declaredSymbols, semanticModel, cancellationToken)
                        || ReferencesAnyUnresolvedName(statements[i], declaredNames, semanticModel, cancellationToken))
                    {
                        expandedEnd = i;
                    }
                }

                if (expandedEnd == end)
                    return true;

                end = expandedEnd;
            }
        }

        /// <summary>
        /// Grows a range so that it absorbs neighbouring unsafe regions that this fixer generated and has not yet been
        /// audited, which keeps a migrated member from accumulating a long run of tiny unsafe blocks.
        /// Statements between those regions are only absorbed when they declare nothing, because a declaration moved
        /// into the merged block would no longer be visible to the statements that follow it.
        /// </summary>
        private static (int Start, int End) ExtendRangeOverGeneratedUnsafeStatements(
            in StatementRange statements,
            int start,
            int end)
        {
            int gap = 0;
            for (int index = start - 1; index >= 0; index--)
            {
                if (IsGeneratedUnsafeStatement(statements[index]))
                {
                    start = index;
                    gap = 0;
                    continue;
                }

                if (gap == MaxSafeStatementsBetweenMergedRegions || GetDeclaredNames(statements[index]).Count > 0)
                    break;

                gap++;
            }

            gap = 0;
            for (int index = end + 1; index < statements.Count; index++)
            {
                if (IsGeneratedUnsafeStatement(statements[index]))
                {
                    end = index;
                    gap = 0;
                    continue;
                }

                if (gap == MaxSafeStatementsBetweenMergedRegions || GetDeclaredNames(statements[index]).Count > 0)
                    break;

                gap++;
            }

            return (start, end);
        }

        /// <summary>
        /// Recognizes an unsafe statement that this fixer produced and that still carries the unaudited marker.
        /// Blocks whose marker was replaced during review are left alone.
        /// </summary>
        private static bool IsGeneratedUnsafeStatement(StatementSyntax statement) =>
            statement is UnsafeStatementSyntax unsafeStatement
            && unsafeStatement.Block.Statements.Count > 0
            && HasSafetyAuditMarker(unsafeStatement.Block.Statements[0]);

        private static bool HasSafetyAuditMarker(SyntaxNode node) =>
            node.GetLeadingTrivia().Any(static trivia =>
                trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                && trivia.ToString() == SafetyAuditComment);

        private static StatementSyntax RemoveSafetyAuditMarker(StatementSyntax statement)
        {
            SyntaxTriviaList leadingTrivia = statement.GetLeadingTrivia();
            for (int index = 0; index < leadingTrivia.Count; index++)
            {
                if (!leadingTrivia[index].IsKind(SyntaxKind.SingleLineCommentTrivia)
                    || leadingTrivia[index].ToString() != SafetyAuditComment)
                {
                    continue;
                }

                int count = index + 1 < leadingTrivia.Count && leadingTrivia[index + 1].IsKind(SyntaxKind.EndOfLineTrivia)
                    ? 2
                    : 1;
                return statement.WithLeadingTrivia(
                    leadingTrivia.Take(index).Concat(leadingTrivia.Skip(index + count)));
            }

            return statement;
        }

        /// <summary>
        /// Replaces the line break that separated a flattened statement from its former closing brace with an elastic
        /// one, so the formatter lays the merged block out instead of inheriting the nested indentation.
        /// </summary>
        private static StatementSyntax NormalizeTrailingEndOfLine(StatementSyntax statement)
        {
            SyntaxTriviaList trailingTrivia = statement.GetTrailingTrivia();
            int count = trailingTrivia.Count;
            while (count > 0 && trailingTrivia[count - 1].IsKind(SyntaxKind.EndOfLineTrivia))
                count--;

            return statement.WithTrailingTrivia(
                trailingTrivia.Take(count).Concat([SyntaxFactory.ElasticCarriageReturnLineFeed]));
        }

        private static HashSet<ISymbol>? GetDeclaredSymbols(
            in StatementRange statements,
            int start,
            int end,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var symbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

            // Top-level statements are separate members, so they can only be analyzed one at a time.
            if (statements.IsTopLevel)
            {
                for (int i = start; i <= end; i++)
                {
                    DataFlowAnalysis? statementAnalysis = semanticModel.AnalyzeDataFlow(statements[i]);
                    if (statementAnalysis is null || !statementAnalysis.Succeeded)
                        return null;

                    symbols.UnionWith(statementAnalysis.VariablesDeclared);
                    AddSyntacticallyDeclaredSymbols(statements[i], symbols, semanticModel, cancellationToken);
                }

                return symbols;
            }

            DataFlowAnalysis? analysis = semanticModel.AnalyzeDataFlow(statements[start], statements[end]);
            if (analysis is null || !analysis.Succeeded)
                return null;

            symbols.UnionWith(analysis.VariablesDeclared);
            for (int i = start; i <= end; i++)
                AddSyntacticallyDeclaredSymbols(statements[i], symbols, semanticModel, cancellationToken);

            return symbols;
        }

        private static void AddSyntacticallyDeclaredSymbols(
            SyntaxNode node,
            HashSet<ISymbol> symbols,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            foreach (VariableDeclaratorSyntax variable in
                node.DescendantNodesAndSelf().OfType<VariableDeclaratorSyntax>())
            {
                if (semanticModel.GetDeclaredSymbol(variable, cancellationToken) is { } symbol)
                    symbols.Add(symbol);
            }

            foreach (SingleVariableDesignationSyntax designation in
                node.DescendantNodesAndSelf().OfType<SingleVariableDesignationSyntax>())
            {
                if (semanticModel.GetDeclaredSymbol(designation, cancellationToken) is { } symbol)
                    symbols.Add(symbol);
            }
        }

        private static bool ReferencesAnySymbol(
            SyntaxNode node,
            HashSet<ISymbol> symbols,
            SemanticModel semanticModel,
            CancellationToken cancellationToken) =>
            symbols.Count > 0
            && node.DescendantNodesAndSelf()
                .OfType<SimpleNameSyntax>()
                .Any(name =>
                    semanticModel.GetSymbolInfo(name, cancellationToken).Symbol is { } symbol
                    && symbols.Contains(symbol));

        private static HashSet<string> GetDeclaredNames(SyntaxNode node) =>
            GetDeclaredNames([node]);

        private static HashSet<string> GetDeclaredNames(IEnumerable<SyntaxNode> nodes)
        {
            var names = new HashSet<string>();
            foreach (SyntaxNode node in nodes)
            {
                foreach (VariableDeclaratorSyntax variable in
                    node.DescendantNodesAndSelf().OfType<VariableDeclaratorSyntax>())
                {
                    names.Add(variable.Identifier.ValueText);
                }

                foreach (SingleVariableDesignationSyntax designation in
                    node.DescendantNodesAndSelf().OfType<SingleVariableDesignationSyntax>())
                {
                    names.Add(designation.Identifier.ValueText);
                }
            }

            return names;
        }

        /// <summary>
        /// Falls back to matching identifier text for names the semantic model cannot resolve. The document contains
        /// compiler errors by definition here, so this keeps ranges conservative without widening them whenever an
        /// unrelated member happens to share a local's name.
        /// </summary>
        private static bool ReferencesAnyUnresolvedName(
            SyntaxNode node,
            HashSet<string> names,
            SemanticModel semanticModel,
            CancellationToken cancellationToken) =>
            names.Count > 0
            && node.DescendantNodesAndSelf()
                .OfType<SimpleNameSyntax>()
                .Any(name =>
                    names.Contains(name.Identifier.ValueText)
                    && semanticModel.GetSymbolInfo(name, cancellationToken).Symbol is null);

        private static bool HasDirectiveWithinSpan(SyntaxNode node) =>
            node.DescendantTrivia(descendIntoTrivia: true)
                .Any(trivia => trivia.IsDirective
                    && node.Span.Contains(trivia.SpanStart));

        private static Document AddUnsafeToUsingDirective(
            Document document,
            SyntaxNode root,
            UsingDirectiveSyntax usingDirective)
        {
            SyntaxToken unsafeKeyword = SyntaxFactory.Token(SyntaxKind.UnsafeKeyword)
                .WithTrailingTrivia(SyntaxFactory.Space);
            UsingDirectiveSyntax replacement = usingDirective
                .WithUnsafeKeyword(unsafeKeyword)
                .WithAdditionalAnnotations(Formatter.Annotation);
            return document.WithSyntaxRoot(root.ReplaceNode(usingDirective, replacement));
        }

        private static Document ReplaceWithUnsafeExpression(
            Document document,
            SyntaxNode root,
            ExpressionSyntax expression,
            ExpressionSyntax operand,
            ExpressionSyntax template)
        {
            IdentifierNameSyntax placeholder = TryFindPlaceholder(template)!;
            ExpressionSyntax replacement = template
                .ReplaceNode(placeholder, operand)
                .WithLeadingTrivia(expression.GetLeadingTrivia())
                .WithTrailingTrivia(expression.GetTrailingTrivia());
            return document.WithSyntaxRoot(root.ReplaceNode(expression, replacement));
        }

        private static bool CanWrapStatementRange(
            in StatementRange statements,
            int start,
            int end,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var selectedStatements = new List<StatementSyntax>(end - start + 1);
            for (int i = start; i <= end; i++)
                selectedStatements.Add(statements[i]);

            if (selectedStatements.Any(ContainsAwaitOrYield))
                return false;

            if (end > start && selectedStatements.Any(static statement => statement.ContainsDirectives))
                return false;

            var localFunctions = new HashSet<ISymbol>(
                selectedStatements
                    .SelectMany(static statement => statement.DescendantNodesAndSelf().OfType<LocalFunctionStatementSyntax>())
                    .Select(localFunction => semanticModel.GetDeclaredSymbol(localFunction, cancellationToken))
                    .OfType<ISymbol>(),
                SymbolEqualityComparer.Default);
            string[] labels = selectedStatements
                .SelectMany(static statement => statement.DescendantNodesAndSelf().OfType<LabeledStatementSyntax>())
                .Select(static label => label.Identifier.ValueText)
                .Distinct()
                .ToArray();
            if (localFunctions.Count == 0 && labels.Length == 0)
                return true;

            for (int i = 0; i < statements.Count; i++)
            {
                if (i >= start && i <= end)
                    continue;

                StatementSyntax statement = statements[i];

                // Moving a local function into a nested block hides it from the rest of the container.
                if (ReferencesAnySymbol(statement, localFunctions, semanticModel, cancellationToken))
                    return false;

                // A goto cannot jump into an unsafe block.
                if (labels.Length > 0
                    && statement.DescendantNodesAndSelf().OfType<GotoStatementSyntax>().Any(gotoStatement =>
                        gotoStatement.Expression is IdentifierNameSyntax identifier
                        && labels.Contains(identifier.Identifier.ValueText)))
                {
                    return false;
                }
            }

            return true;
        }

        private static SwitchStatementSyntax? TryGetEnclosingSwitch(StatementSyntax statement) =>
            statement.AncestorsAndSelf()
                .OfType<SwitchSectionSyntax>()
                .Select(static section => section.Parent)
                .OfType<SwitchStatementSyntax>()
                .FirstOrDefault();

        private static bool ContainsAwaitOrYield(SyntaxNode node) =>
            node.DescendantNodesAndSelf().Any(static descendant =>
                descendant is AwaitExpressionSyntax
                    or YieldStatementSyntax
                    or CommonForEachStatementSyntax { AwaitKeyword.RawKind: not 0 }
                    or UsingStatementSyntax { AwaitKeyword.RawKind: not 0 }
                    or LocalDeclarationStatementSyntax { AwaitKeyword.RawKind: not 0 });

        private static Task<Document> ConvertExpressionBodyAsync(
            Document document,
            SyntaxNode root,
            SemanticModel semanticModel,
            ArrowExpressionClauseSyntax arrowExpression,
            CancellationToken cancellationToken)
        {
            SyntaxNode declaration = arrowExpression.Parent!;
            ExpressionSyntax expression = arrowExpression.Expression;
            StatementSyntax statement;

            if (expression is ThrowExpressionSyntax throwExpression)
            {
                statement = SyntaxFactory.ThrowStatement(throwExpression.Expression);
            }
            else
            {
                ISymbol? symbol = declaration switch
                {
                    MemberDeclarationSyntax member => semanticModel.GetDeclaredSymbol(member, cancellationToken),
                    LocalFunctionStatementSyntax localFunction => semanticModel.GetDeclaredSymbol(localFunction, cancellationToken),
                    AccessorDeclarationSyntax accessor => semanticModel.GetDeclaredSymbol(accessor, cancellationToken),
                    _ => null,
                };
                ITypeSymbol? expressionType = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
                bool isVoidExpression = symbol is IMethodSymbol { ReturnsVoid: true }
                    || expressionType?.SpecialType == SpecialType.System_Void
                    || symbol is IMethodSymbol asyncMethod
                        && IsResultlessAsyncMethod(asyncMethod);
                statement = isVoidExpression
                    ? SyntaxFactory.ExpressionStatement(expression)
                    : SyntaxFactory.ReturnStatement(expression);
            }

            UnsafeStatementSyntax unsafeStatement = CreateUnsafeStatement(statement);
            BlockSyntax body = SyntaxFactory.Block(unsafeStatement)
                .WithAdditionalAnnotations(Formatter.Annotation);

            SyntaxNode replacement = declaration switch
            {
                MethodDeclarationSyntax method => method
                    .WithExpressionBody(null)
                    .WithSemicolonToken(default)
                    .WithBody(body),
                ConstructorDeclarationSyntax constructor => constructor
                    .WithExpressionBody(null)
                    .WithSemicolonToken(default)
                    .WithBody(body),
                DestructorDeclarationSyntax destructor => destructor
                    .WithExpressionBody(null)
                    .WithSemicolonToken(default)
                    .WithBody(body),
                OperatorDeclarationSyntax @operator => @operator
                    .WithExpressionBody(null)
                    .WithSemicolonToken(default)
                    .WithBody(body),
                ConversionOperatorDeclarationSyntax conversion => conversion
                    .WithExpressionBody(null)
                    .WithSemicolonToken(default)
                    .WithBody(body),
                LocalFunctionStatementSyntax localFunction => localFunction
                    .WithExpressionBody(null)
                    .WithSemicolonToken(default)
                    .WithBody(body),
                PropertyDeclarationSyntax property => property
                    .WithExpressionBody(null)
                    .WithSemicolonToken(default)
                    .WithAccessorList(CreateGetterAccessorList(unsafeStatement)),
                IndexerDeclarationSyntax indexer => indexer
                    .WithExpressionBody(null)
                    .WithSemicolonToken(default)
                    .WithAccessorList(CreateGetterAccessorList(unsafeStatement)),
                AccessorDeclarationSyntax accessor => accessor
                    .WithExpressionBody(null)
                    .WithSemicolonToken(default)
                    .WithBody(body),
                _ => declaration,
            };

            return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(
                declaration,
                replacement.WithAdditionalAnnotations(Formatter.Annotation))));
        }

        private static bool IsResultlessAsyncMethod(IMethodSymbol method)
        {
            if (!method.IsAsync)
                return false;

            if (method.ReturnsVoid)
                return true;

            if (method.ReturnType is INamedTypeSymbol
                {
                    Arity: 0,
                    Name: "Task" or "ValueTask",
                } taskType
                && taskType.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks")
            {
                return true;
            }

            foreach (AttributeData attribute in method.ReturnType.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString()
                        != "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute"
                    || attribute.ConstructorArguments.Length != 1
                    || attribute.ConstructorArguments[0].Value is not INamedTypeSymbol builderType)
                {
                    continue;
                }

                return builderType.GetMembers("SetResult")
                    .OfType<IMethodSymbol>()
                    .Any(static setResult => setResult.Parameters.IsEmpty);
            }

            return false;
        }

        private static AccessorListSyntax CreateGetterAccessorList(UnsafeStatementSyntax unsafeStatement) =>
            SyntaxFactory.AccessorList(
                SyntaxFactory.SingletonList(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithBody(SyntaxFactory.Block(unsafeStatement))));

        private static UnsafeStatementSyntax CreateUnsafeStatement(StatementSyntax statement)
        {
            StatementSyntax innerStatement = statement
                .WithoutLeadingTrivia()
                .WithoutTrailingTrivia()
                .WithLeadingTrivia(
                    SyntaxFactory.Comment(SafetyAuditComment),
                    SyntaxFactory.ElasticCarriageReturnLineFeed);
            return SyntaxFactory.UnsafeStatement(SyntaxFactory.Block(innerStatement))
                .WithLeadingTrivia(statement.GetLeadingTrivia())
                .WithTrailingTrivia(statement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static Document ReplaceStatementWithStatements(
            Document document,
            SyntaxNode root,
            StatementSyntax oldStatement,
            IReadOnlyList<StatementSyntax> replacements)
        {
            switch (oldStatement.Parent)
            {
                case BlockSyntax block:
                    {
                        int index = block.Statements.IndexOf(oldStatement);
                        SyntaxList<StatementSyntax> statements = block.Statements
                            .RemoveAt(index)
                            .InsertRange(index, replacements);
                        return document.WithSyntaxRoot(root.ReplaceNode(
                            block,
                            block.WithStatements(statements)));
                    }

                case SwitchSectionSyntax switchSection:
                    {
                        int index = switchSection.Statements.IndexOf(oldStatement);
                        SyntaxList<StatementSyntax> statements = switchSection.Statements
                            .RemoveAt(index)
                            .InsertRange(index, replacements);
                        return document.WithSyntaxRoot(root.ReplaceNode(
                            switchSection,
                            switchSection.WithStatements(statements)));
                    }

                default:
                    return document.WithSyntaxRoot(root.ReplaceNode(
                        oldStatement,
                        replacements.Count == 1
                            ? replacements[0]
                            : SyntaxFactory.Block(replacements)
                                .WithAdditionalAnnotations(Formatter.Annotation)));
            }
        }

        private static Document WrapStatementRange(
            Document document,
            SyntaxNode root,
            in StatementRange statements,
            int start,
            int end)
        {
            var statementsToWrap = new List<StatementSyntax>(end - start + 1);
            for (int i = start; i <= end; i++)
            {
                StatementSyntax statement = statements[i];

                // Merged regions are flattened so the result is one audit scope instead of nested unsafe blocks.
                if (IsGeneratedUnsafeStatement(statement))
                {
                    UnsafeStatementSyntax generated = (UnsafeStatementSyntax)statement;
                    StatementSyntax firstInnerStatement = RemoveSafetyAuditMarker(generated.Block.Statements[0]);
                    statementsToWrap.Add(
                        NormalizeTrailingEndOfLine(firstInnerStatement)
                            .WithLeadingTrivia(generated.GetLeadingTrivia()
                                .AddRange(firstInnerStatement.GetLeadingTrivia())));
                    statementsToWrap.AddRange(generated.Block.Statements.Skip(1).Select(NormalizeTrailingEndOfLine));
                    continue;
                }

                statementsToWrap.Add(statement);
            }

            StatementSyntax firstStatement = statementsToWrap[0];
            statementsToWrap[0] = firstStatement
                .WithoutLeadingTrivia()
                .WithLeadingTrivia(
                    SyntaxFactory.Comment(SafetyAuditComment),
                    SyntaxFactory.ElasticCarriageReturnLineFeed);

            UnsafeStatementSyntax unsafeStatement = SyntaxFactory.UnsafeStatement(
                SyntaxFactory.Block(statementsToWrap))
                .WithLeadingTrivia(firstStatement.GetLeadingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);

            SyntaxNode replacementContainer = statements.ReplaceRange(start, end, unsafeStatement);
            return document.WithSyntaxRoot(root.ReplaceNode(statements.Container, replacementContainer));
        }

        /// <summary>
        /// Provides uniform access to the statement list that contains a statement, including the top-level statement
        /// list of a compilation unit, whose statements are wrapped in <see cref="GlobalStatementSyntax"/> members.
        /// </summary>
        private readonly struct StatementRange
        {
            private readonly ImmutableArray<StatementSyntax> _statements;

            private StatementRange(SyntaxNode container, ImmutableArray<StatementSyntax> statements)
            {
                Container = container;
                _statements = statements;
            }

            internal SyntaxNode Container { get; }

            internal int Count => _statements.Length;

            internal StatementSyntax this[int index] => _statements[index];

            internal bool IsTopLevel => Container is CompilationUnitSyntax;

            internal static bool TryCreateForStatement(
                StatementSyntax statement,
                out StatementRange statements,
                out int statementIndex)
            {
                switch (statement.Parent)
                {
                    case BlockSyntax block:
                        statements = new StatementRange(block, [.. block.Statements]);
                        break;

                    case SwitchSectionSyntax switchSection:
                        statements = new StatementRange(switchSection, [.. switchSection.Statements]);
                        break;

                    case GlobalStatementSyntax { Parent: CompilationUnitSyntax compilationUnit }:
                        statements = new StatementRange(
                            compilationUnit,
                            [.. compilationUnit.Members.OfType<GlobalStatementSyntax>().Select(static global => global.Statement)]);
                        break;

                    default:
                        statements = default;
                        statementIndex = -1;
                        return false;
                }

                statementIndex = statements._statements.IndexOf(statement);
                return statementIndex >= 0;
            }

            /// <summary>
            /// Replaces the statements in <c>[start, end]</c> with a single statement.
            /// </summary>
            internal SyntaxNode ReplaceRange(int start, int end, StatementSyntax replacement)
            {
                if (Container is CompilationUnitSyntax compilationUnit)
                {
                    SyntaxList<MemberDeclarationSyntax> members = compilationUnit.Members;
                    GlobalStatementSyntax[] globalStatements = members.OfType<GlobalStatementSyntax>().ToArray();

                    // Resolve every member index before editing: removing a member rebuilds the list, and top-level
                    // statements are not guaranteed to be adjacent members.
                    int[] memberIndices = new int[end - start + 1];
                    for (int i = start; i <= end; i++)
                        memberIndices[i - start] = members.IndexOf(globalStatements[i]);

                    for (int i = memberIndices.Length - 1; i >= 0; i--)
                        members = members.RemoveAt(memberIndices[i]);

                    return compilationUnit.WithMembers(
                        members.Insert(memberIndices[0], SyntaxFactory.GlobalStatement(replacement)));
                }

                var replacementStatements = new List<StatementSyntax>(Count - (end - start));
                for (int i = 0; i < Count; i++)
                {
                    if (i == start)
                        replacementStatements.Add(replacement);
                    if (i < start || i > end)
                        replacementStatements.Add(_statements[i]);
                }

                SyntaxList<StatementSyntax> statements = SyntaxFactory.List(replacementStatements);
                return Container switch
                {
                    BlockSyntax block => block.WithStatements(statements),
                    SwitchSectionSyntax switchSection => switchSection.WithStatements(statements),
                    _ => Container,
                };
            }
        }
    }
}
#endif
