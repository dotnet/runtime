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

        private const string UnsafeExpressionPlaceholder = "__unsafeExpression";

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

            SyntaxNode targetNode = root.FindNode(
                diagnostic.Location.SourceSpan,
                getInnermostNodeForTie: true);
            string title = CodeFixTitle.ToString();

            // Attribute applications are deliberately not suppressible under the updated language rules.
            if (targetNode.AncestorsAndSelf().OfType<AttributeSyntax>().Any())
                return;

            if (TryGetConstructorRequiringUnsafe(diagnostic, targetNode) is { } constructor)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title,
                        cancellationToken => UnsafeModifierCodeFixHelpers.SetUnsafeModifierAsync(
                            context.Document,
                            constructor,
                            cancellationToken),
                        title),
                    diagnostic);
                return;
            }

            if (diagnostic.Id == UnsafeConstructorConstraintDiagnosticId
                && targetNode.AncestorsAndSelf().OfType<UsingDirectiveSyntax>().FirstOrDefault() is { } usingDirective)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title,
                        _ => Task.FromResult(AddUnsafeToUsingDirective(context.Document, root, usingDirective)),
                        title),
                    diagnostic);
                return;
            }

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (semanticModel is null)
                return;

            if (IsExpressionOnlyContext(targetNode, diagnostic.Location.SourceSpan)
                && TryGetUnsafeExpression(targetNode, diagnostic.Location.SourceSpan) is { } expressionOnly)
            {
                RegisterExpressionFix(
                    context,
                    diagnostic,
                    root,
                    semanticModel,
                    expressionOnly,
                    title);
                return;
            }

            ArrowExpressionClauseSyntax? arrowExpression = targetNode.AncestorsAndSelf()
                .OfType<ArrowExpressionClauseSyntax>()
                .FirstOrDefault(arrow => arrow.Expression.FullSpan.Contains(diagnostic.Location.SourceSpan));
            if (arrowExpression is not null)
            {
                if (arrowExpression.Parent is AnonymousFunctionExpressionSyntax
                    || arrowExpression.Expression.DescendantNodesAndSelf().OfType<AwaitExpressionSyntax>().Any()
                    || HasDirectiveWithinSpan(arrowExpression))
                {
                    if (TryGetUnsafeExpression(targetNode, diagnostic.Location.SourceSpan) is { } expression)
                    {
                        RegisterExpressionFix(
                            context,
                            diagnostic,
                            root,
                            semanticModel,
                            expression,
                            title);
                    }
                }
                else
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title,
                            cancellationToken => ConvertExpressionBodyAsync(
                                context.Document,
                                root,
                                semanticModel,
                                arrowExpression,
                                cancellationToken),
                            title),
                        diagnostic);
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
                if (TryGetUnsafeExpression(targetNode, diagnostic.Location.SourceSpan) is { } expression)
                {
                    RegisterExpressionFix(
                        context,
                        diagnostic,
                        root,
                        semanticModel,
                        expression,
                        title);
                }
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
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title,
                            _ => Task.FromResult(ReplaceStatementWithStatements(
                                context.Document,
                                root,
                                localDeclaration,
                                [forwardDeclaration, CreateUnsafeStatement(assignmentStatement)])),
                            title),
                        diagnostic);
                    return;
                }

                if (TryGetUnsafeExpression(targetNode, diagnostic.Location.SourceSpan) is { } localExpression
                    && CanUseUnsafeExpression(localExpression))
                {
                    RegisterExpressionFix(
                        context,
                        diagnostic,
                        root,
                        semanticModel,
                        localExpression,
                        title);
                    return;
                }

                if (!localDeclaration.AwaitKeyword.IsKind(SyntaxKind.None))
                    return;

                if (TryGetEnclosingSwitch(localDeclaration) is { } localSwitch)
                {
                    if (!ContainsAwaitOrYield(localSwitch))
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title,
                                _ => Task.FromResult(ReplaceStatementWithStatements(
                                    context.Document,
                                    root,
                                    localSwitch,
                                    [CreateUnsafeStatement(localSwitch)])),
                                title),
                            diagnostic);
                    }
                    return;
                }

                if (TryGetStatementRangeToWrap(
                    localDeclaration,
                    semanticModel,
                    context.CancellationToken,
                    forceThroughContainerEnd: IsUsingDeclaration(localDeclaration),
                    out SyntaxNode localContainer,
                    out int localStart,
                    out int localEnd)
                    && CanWrapStatementRange(
                        localContainer,
                        localStart,
                        localEnd,
                        semanticModel,
                        context.CancellationToken))
                {
                    RegisterStatementRangeFix(
                        context,
                        diagnostic,
                        root,
                        localContainer,
                        localStart,
                        localEnd,
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
                && TryGetUnsafeExpression(targetNode, diagnostic.Location.SourceSpan) is { } statementExpression
                && CanUseUnsafeExpression(statementExpression))
            {
                RegisterExpressionFix(
                    context,
                    diagnostic,
                    root,
                    semanticModel,
                    statementExpression,
                    title);
                return;
            }

            if (topLevelScopeSensitive
                && containingStatement.Parent is GlobalStatementSyntax
                {
                    Parent: CompilationUnitSyntax compilationUnit,
                } globalStatement)
            {
                GlobalStatementSyntax[] globalStatements = compilationUnit.Members
                    .OfType<GlobalStatementSyntax>()
                    .ToArray();
                int start = System.Array.IndexOf(globalStatements, globalStatement);
                int end = globalStatements.Length - 1;
                if (start >= 0
                    && CanWrapStatementRange(
                        compilationUnit,
                        start,
                        end,
                        semanticModel,
                        context.CancellationToken))
                {
                    RegisterStatementRangeFix(
                        context,
                        diagnostic,
                        root,
                        compilationUnit,
                        start,
                        end,
                        title);
                }
                return;
            }

            if (cannotContainUnsafeStatement || containsAwaitStatement)
                return;

            if (preferExpression && TryGetEnclosingSwitch(containingStatement) is { } containingSwitch)
            {
                if (!ContainsAwaitOrYield(containingSwitch))
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title,
                            _ => Task.FromResult(ReplaceStatementWithStatements(
                                context.Document,
                                root,
                                containingSwitch,
                                [CreateUnsafeStatement(containingSwitch)])),
                            title),
                        diagnostic);
                }
                return;
            }

            if (preferExpression)
            {
                if (TryGetStatementRangeToWrap(
                        containingStatement,
                        semanticModel,
                        context.CancellationToken,
                        forceThroughContainerEnd: topLevelScopeSensitive,
                        out SyntaxNode container,
                        out int start,
                        out int end)
                    && CanWrapStatementRange(
                        container,
                        start,
                        end,
                        semanticModel,
                        context.CancellationToken))
                {
                    RegisterStatementRangeFix(context, diagnostic, root, container, start, end, title);
                }
                return;
            }

            if (TryGetStatementList(
                    containingStatement,
                    out SyntaxNode singleContainer,
                    out _,
                    out int singleIndex)
                && !CanWrapStatementRange(
                    singleContainer,
                    singleIndex,
                    singleIndex,
                    semanticModel,
                    context.CancellationToken))
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    _ => Task.FromResult(ReplaceStatementWithStatements(
                        context.Document,
                        root,
                        containingStatement,
                        [CreateUnsafeStatement(containingStatement)])),
                    title),
                diagnostic);
        }

        private static void RegisterExpressionFix(
            CodeFixContext context,
            Diagnostic diagnostic,
            SyntaxNode root,
            SemanticModel semanticModel,
            ExpressionSyntax expression,
            string title)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    _ => Task.FromResult(ReplaceWithUnsafeExpression(
                        context.Document,
                        root,
                        semanticModel,
                        expression,
                        context.CancellationToken)),
                    title),
                diagnostic);
        }

        private static void RegisterStatementRangeFix(
            CodeFixContext context,
            Diagnostic diagnostic,
            SyntaxNode root,
            SyntaxNode container,
            int start,
            int end,
            string title)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    _ => Task.FromResult(WrapStatementRange(
                        context.Document,
                        root,
                        container,
                        start,
                        end)),
                    title),
                diagnostic);
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
                _ => true,
            });

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
                type = SyntaxFactory.ParseTypeName(
                    localSymbol.Type.ToMinimalDisplayString(
                        semanticModel,
                        localDeclaration.SpanStart,
                        s_localTypeDisplayFormat));
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

            ISymbol[] initializerVariables = analysis.VariablesDeclared
                .Where(symbol => !SymbolEqualityComparer.Default.Equals(symbol, declaredLocal))
                .ToArray();
            if (initializerVariables.Length == 0
                || !TryGetStatementList(
                    localDeclaration,
                    out SyntaxNode container,
                    out SyntaxList<StatementSyntax> statements,
                    out int statementIndex))
            {
                return false;
            }

            IEnumerable<StatementSyntax> laterStatements = Enumerable
                .Range(statementIndex + 1, statements.Count - statementIndex - 1)
                .Select(index => GetStatement(container, statements, index));
            if (localDeclaration.Parent is SwitchSectionSyntax switchSection
                && switchSection.Parent is SwitchStatementSyntax switchStatement)
            {
                int sectionIndex = switchStatement.Sections.IndexOf(switchSection);
                laterStatements = laterStatements.Concat(
                    switchStatement.Sections
                        .Skip(sectionIndex + 1)
                        .SelectMany(static section => section.Statements));
            }

            return laterStatements.Any(statement => ReferencesAnySymbol(
                statement,
                initializerVariables,
                semanticModel,
                cancellationToken));
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
            if (!TryGetStatementList(statement, out _, out SyntaxList<StatementSyntax> statements, out int statementIndex))
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

            IEnumerable<StatementSyntax> laterStatements = statements.Skip(statementIndex + 1);
            if (statement.Parent is SwitchSectionSyntax switchSection
                && switchSection.Parent is SwitchStatementSyntax switchStatement)
            {
                int sectionIndex = switchStatement.Sections.IndexOf(switchSection);
                laterStatements = laterStatements.Concat(
                    switchStatement.Sections
                        .Skip(sectionIndex + 1)
                        .SelectMany(static section => section.Statements));
            }

            return laterStatements.Any(laterStatement =>
                ReferencesAnySymbol(
                    laterStatement,
                    declaredSymbols,
                    semanticModel,
                    cancellationToken)
                || ReferencesAnyName(laterStatement, declaredNames));
        }

        private static bool TryGetStatementRangeToWrap(
            StatementSyntax triggerStatement,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            bool forceThroughContainerEnd,
            out SyntaxNode container,
            out int start,
            out int end)
        {
            if (!TryGetStatementList(triggerStatement, out container, out SyntaxList<StatementSyntax> statements, out start))
            {
                end = -1;
                return false;
            }

            end = forceThroughContainerEnd ? statements.Count - 1 : start;
            if (forceThroughContainerEnd)
                return true;

            while (true)
            {
                HashSet<ISymbol>? declaredSymbols = GetDeclaredSymbols(
                    container,
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

                if (statements.Skip(start).Take(end - start + 1).OfType<LocalDeclarationStatementSyntax>()
                    .Any(IsUsingDeclaration))
                {
                    end = statements.Count - 1;
                    return true;
                }

                int expandedEnd = end;
                var selectedStatements = new List<StatementSyntax>(end - start + 1);
                for (int i = start; i <= end; i++)
                    selectedStatements.Add(GetStatement(container, statements, i));
                HashSet<string> declaredNames = GetDeclaredNames(selectedStatements);
                for (int i = end + 1; i < statements.Count; i++)
                {
                    if (ReferencesAnySymbol(
                        GetStatement(container, statements, i),
                        declaredSymbols,
                        semanticModel,
                        cancellationToken)
                        || ReferencesAnyName(
                            GetStatement(container, statements, i),
                            declaredNames))
                    {
                        expandedEnd = i;
                    }
                }

                if (expandedEnd == end)
                    return true;

                end = expandedEnd;
            }
        }

        private static bool TryGetStatementList(
            StatementSyntax statement,
            out SyntaxNode container,
            out SyntaxList<StatementSyntax> statements,
            out int statementIndex)
        {
            switch (statement.Parent)
            {
                case BlockSyntax block:
                    container = block;
                    statements = block.Statements;
                    statementIndex = statements.IndexOf(statement);
                    return statementIndex >= 0;

                case SwitchSectionSyntax switchSection:
                    container = switchSection;
                    statements = switchSection.Statements;
                    statementIndex = statements.IndexOf(statement);
                    return statementIndex >= 0;

                case GlobalStatementSyntax
                    {
                        Parent: CompilationUnitSyntax compilationUnit,
                    }:
                    container = compilationUnit;
                    statements = SyntaxFactory.List(
                        compilationUnit.Members
                            .OfType<GlobalStatementSyntax>()
                            .Select(static globalStatement => globalStatement.Statement));
                    statementIndex = statements.IndexOf(statement);
                    return statementIndex >= 0;

                default:
                    container = null!;
                    statements = default;
                    statementIndex = -1;
                    return false;
            }
        }

        private static HashSet<ISymbol>? GetDeclaredSymbols(
            SyntaxNode container,
            SyntaxList<StatementSyntax> statements,
            int start,
            int end,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var symbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            if (container is CompilationUnitSyntax)
            {
                for (int i = start; i <= end; i++)
                {
                    StatementSyntax statement = GetStatement(container, statements, i);
                    DataFlowAnalysis? statementAnalysis = semanticModel.AnalyzeDataFlow(statement);
                    if (statementAnalysis is null || !statementAnalysis.Succeeded)
                        return null;

                    symbols.UnionWith(statementAnalysis.VariablesDeclared);
                    AddSyntacticallyDeclaredSymbols(
                        statement,
                        symbols,
                        semanticModel,
                        cancellationToken);
                }

                return symbols;
            }

            DataFlowAnalysis? analysis = semanticModel.AnalyzeDataFlow(statements[start], statements[end]);
            if (analysis is null || !analysis.Succeeded)
                return null;

            symbols.UnionWith(analysis.VariablesDeclared);
            for (int i = start; i <= end; i++)
            {
                AddSyntacticallyDeclaredSymbols(
                    GetStatement(container, statements, i),
                    symbols,
                    semanticModel,
                    cancellationToken);
            }
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
            IEnumerable<ISymbol> symbols,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var symbolSet = new HashSet<ISymbol>(symbols, SymbolEqualityComparer.Default);
            if (symbolSet.Count == 0)
                return false;

            return node.DescendantNodesAndSelf()
                .OfType<SimpleNameSyntax>()
                .Any(name =>
                    semanticModel.GetSymbolInfo(name, cancellationToken).Symbol is { } symbol
                    && symbolSet.Contains(symbol));
        }

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

        private static bool ReferencesAnyName(
            SyntaxNode node,
            HashSet<string> names) =>
            names.Count > 0
            && node.DescendantNodesAndSelf()
                .OfType<SimpleNameSyntax>()
                .Any(name => names.Contains(name.Identifier.ValueText));

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
            SemanticModel semanticModel,
            ExpressionSyntax expression,
            CancellationToken cancellationToken)
        {
            ExpressionSyntax operand = expression.WithoutLeadingTrivia().WithoutTrailingTrivia();
            Conversion conversion = semanticModel.GetConversion(expression, cancellationToken);
            if (conversion.IsUserDefined
                && conversion.IsImplicit
                && conversion.MethodSymbol is { } conversionOperator)
            {
                ITypeSymbol convertedType = GetUserDefinedConversionResultType(
                    semanticModel,
                    expression,
                    conversionOperator,
                    cancellationToken);
                TypeSyntax convertedTypeSyntax = SyntaxFactory.ParseTypeName(
                    convertedType.ToMinimalDisplayString(
                        semanticModel,
                        expression.SpanStart,
                        s_localTypeDisplayFormat));
                operand = SyntaxFactory.CastExpression(convertedTypeSyntax, operand);
            }

            ExpressionSyntax template = SyntaxFactory.ParseExpression(
                $"unsafe(/* SAFETY: Audit */ {UnsafeExpressionPlaceholder})");
            IdentifierNameSyntax placeholder = template.DescendantNodesAndSelf()
                .OfType<IdentifierNameSyntax>()
                .Single(identifier => identifier.Identifier.ValueText == UnsafeExpressionPlaceholder);

            ExpressionSyntax replacement = template
                .ReplaceNode(
                    placeholder,
                    operand)
                .WithLeadingTrivia(expression.GetLeadingTrivia())
                .WithTrailingTrivia(expression.GetTrailingTrivia());
            return document.WithSyntaxRoot(root.ReplaceNode(expression, replacement));
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

        private static bool CanWrapStatementRange(
            SyntaxNode container,
            int start,
            int end,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            SyntaxList<StatementSyntax> statements = container switch
            {
                BlockSyntax block => block.Statements,
                SwitchSectionSyntax switchSection => switchSection.Statements,
                CompilationUnitSyntax compilationUnit => SyntaxFactory.List(
                    compilationUnit.Members
                        .OfType<GlobalStatementSyntax>()
                        .Select(static globalStatement => globalStatement.Statement)),
                _ => default,
            };
            IEnumerable<StatementSyntax> selectedStatements = statements
                .Select((statement, index) => GetStatement(container, statements, index))
                .Skip(start)
                .Take(end - start + 1)
                .ToArray();

            if (selectedStatements.Any(ContainsAwaitOrYield))
                return false;

            if (end > start && selectedStatements.Any(static statement => statement.ContainsDirectives))
                return false;

            ISymbol[] localFunctions = selectedStatements
                .SelectMany(static statement => statement.DescendantNodesAndSelf().OfType<LocalFunctionStatementSyntax>())
                .Select(localFunction => semanticModel.GetDeclaredSymbol(localFunction, cancellationToken))
                .OfType<ISymbol>()
                .ToArray();
            if (localFunctions.Length > 0
                && statements.Where((_, index) => index < start || index > end)
                    .Any(statement => ReferencesAnySymbol(
                        statement,
                        localFunctions,
                        semanticModel,
                        cancellationToken)))
            {
                return false;
            }

            string[] labels = selectedStatements
                .SelectMany(static statement => statement.DescendantNodesAndSelf().OfType<LabeledStatementSyntax>())
                .Select(static label => label.Identifier.ValueText)
                .Distinct()
                .ToArray();
            if (labels.Length > 0
                && statements.Where((_, index) => index < start || index > end)
                    .SelectMany(static statement => statement.DescendantNodesAndSelf().OfType<GotoStatementSyntax>())
                    .Any(gotoStatement =>
                        gotoStatement.Expression is IdentifierNameSyntax identifier
                        && labels.Contains(identifier.Identifier.ValueText)))
            {
                return false;
            }

            return true;
        }

        private static StatementSyntax GetStatement(
            SyntaxNode container,
            SyntaxList<StatementSyntax> statements,
            int index) =>
            container is CompilationUnitSyntax compilationUnit
                ? compilationUnit.Members
                    .OfType<GlobalStatementSyntax>()
                    .ElementAt(index)
                    .Statement
                : statements[index];

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
                    SyntaxFactory.Comment("// SAFETY: Audit"),
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
            SyntaxNode container,
            int start,
            int end)
        {
            SyntaxList<StatementSyntax> statements = container switch
            {
                BlockSyntax block => block.Statements,
                SwitchSectionSyntax switchSection => switchSection.Statements,
                CompilationUnitSyntax compilationUnit => SyntaxFactory.List(
                    compilationUnit.Members
                        .OfType<GlobalStatementSyntax>()
                        .Select(static globalStatement => globalStatement.Statement)),
                _ => default,
            };
            StatementSyntax[] statementsToWrap = statements
                .Skip(start)
                .Take(end - start + 1)
                .ToArray();

            StatementSyntax firstStatement = statementsToWrap[0];
            StatementSyntax firstInnerStatement = firstStatement
                .WithoutLeadingTrivia()
                .WithLeadingTrivia(
                    SyntaxFactory.Comment("// SAFETY: Audit"),
                    SyntaxFactory.ElasticCarriageReturnLineFeed);
            statementsToWrap[0] = firstInnerStatement;

            UnsafeStatementSyntax unsafeStatement = SyntaxFactory.UnsafeStatement(
                SyntaxFactory.Block(statementsToWrap))
                .WithLeadingTrivia(firstStatement.GetLeadingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);

            var replacementList = new List<StatementSyntax>(statements.Count - (end - start));
            for (int i = 0; i < statements.Count; i++)
            {
                if (i == start)
                    replacementList.Add(unsafeStatement);
                if (i < start || i > end)
                    replacementList.Add(statements[i]);
            }
            SyntaxList<StatementSyntax> replacementStatements = SyntaxFactory.List(replacementList);
            SyntaxNode replacementContainer = container switch
            {
                BlockSyntax block => block.WithStatements(replacementStatements),
                SwitchSectionSyntax switchSection => switchSection.WithStatements(replacementStatements),
                CompilationUnitSyntax compilationUnit => ReplaceGlobalStatements(
                    compilationUnit,
                    start,
                    end,
                    unsafeStatement),
                _ => container,
            };
            return document.WithSyntaxRoot(root.ReplaceNode(container, replacementContainer));
        }

        private static CompilationUnitSyntax ReplaceGlobalStatements(
            CompilationUnitSyntax compilationUnit,
            int start,
            int end,
            UnsafeStatementSyntax unsafeStatement)
        {
            GlobalStatementSyntax[] globalStatements = compilationUnit.Members
                .OfType<GlobalStatementSyntax>()
                .ToArray();
            int memberIndex = compilationUnit.Members.IndexOf(globalStatements[start]);
            SyntaxList<MemberDeclarationSyntax> members = compilationUnit.Members;
            for (int i = start; i <= end; i++)
                members = members.RemoveAt(memberIndex);

            return compilationUnit.WithMembers(members.Insert(
                memberIndex,
                SyntaxFactory.GlobalStatement(unsafeStatement)));
        }
    }
}
#endif
