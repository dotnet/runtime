// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix
{
    internal static class UnsafeUsageDocumentFixer
    {
        private const int WholeBodyThreshold = 3;
        private const int MaxIterations = 8;

        private enum StatementFixKind
        {
            Wrap,
            ForwardDeclaration
        }

        private readonly struct StatementFix
        {
            private StatementFix(
                StatementFixKind kind,
                LocalDeclarationStatementSyntax? forwardDeclaration,
                StatementSyntax? assignment)
            {
                Kind = kind;
                ForwardDeclaration = forwardDeclaration;
                Assignment = assignment;
            }

            public StatementFixKind Kind { get; }

            public LocalDeclarationStatementSyntax? ForwardDeclaration { get; }

            public StatementSyntax? Assignment { get; }

            public static StatementFix Wrap() => new(StatementFixKind.Wrap, null, null);

            public static StatementFix Forward(
                LocalDeclarationStatementSyntax forwardDeclaration,
                StatementSyntax assignment)
                => new(StatementFixKind.ForwardDeclaration, forwardDeclaration, assignment);
        }

        public static async Task<Document> FixDocumentAsync(
            Document document,
            CancellationToken cancellationToken)
        {
            Document current = await UnsafeCodeFixHelpers.ApplyDeclarationUpdatesAsync(
                document,
                cancellationToken).ConfigureAwait(false);

            for (int iteration = 0; iteration < MaxIterations; iteration++)
            {
                if (await current.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } semanticModel ||
                    await current.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root)
                {
                    break;
                }

                var locations = UnsafeMigrationAnalysis.GetUnsafeOperationLocations(
                    semanticModel,
                    IsSkipLocalsInitEnabled(current),
                    includeCompilerDiagnostics: iteration == 0,
                    cancellationToken);
                if (locations.IsEmpty)
                    break;

                Document next = ApplyOperationFixes(current, root, semanticModel, locations, useWholeBodyHeuristic: true);
                if (ReferenceEquals(next, current))
                    break;

                current = next;
            }

            return current;
        }

        public static async Task<Document> FixDiagnosticAsync(
            Document document,
            Diagnostic diagnostic,
            CancellationToken cancellationToken)
        {
            if (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } semanticModel ||
                await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root)
            {
                return document;
            }

            return ApplyOperationFixes(
                document,
                root,
                semanticModel,
                [diagnostic.Location],
                useWholeBodyHeuristic: false);
        }

        private static Document ApplyOperationFixes(
            Document document,
            SyntaxNode root,
            SemanticModel semanticModel,
            ImmutableArray<Location> locations,
            bool useWholeBodyHeuristic)
        {
            if (useWholeBodyHeuristic)
            {
                var bodies = locations
                    .Select(location => FindExecutableBody(root.FindNode(
                        location.SourceSpan,
                        getInnermostNodeForTie: true)))
                    .OfType<BlockSyntax>()
                    .GroupBy(static body => body)
                    .Where(static group => group.Count() >= WholeBodyThreshold)
                    .Select(static group => group.Key)
                    .Where(CanWrapInUnsafeBlock)
                    .ToArray();

                bodies = RemoveNestedNodes(bodies);
                if (bodies.Length > 0)
                    return document.WithSyntaxRoot(WrapBodies(root, bodies));
            }

            var statementFixes = new Dictionary<StatementSyntax, StatementFix>();
            var expressionFixes = new HashSet<ExpressionSyntax>();
            var fallbackBodies = new HashSet<BlockSyntax>();

            foreach (Location location in locations)
            {
                SyntaxNode target = root.FindNode(
                    location.SourceSpan,
                    getInnermostNodeForTie: true);

                StatementSyntax? statement = target.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();
                ExpressionSyntax? expression = FindFixableExpression(target, location.SourceSpan);
                bool requiresExpression = RequiresUnsafeExpression(target, statement);

                if (!requiresExpression && statement is LocalDeclarationStatementSyntax localDeclaration)
                {
                    if (TryCreateForwardDeclarationFix(
                        localDeclaration,
                        semanticModel,
                        out StatementFix forwardFix))
                    {
                        AddStatementFix(statementFixes, statement, forwardFix);
                        continue;
                    }

                    requiresExpression = true;
                }

                if (!requiresExpression && statement is not null && CanWrapInUnsafeBlock(statement))
                {
                    AddStatementFix(statementFixes, statement, StatementFix.Wrap());
                    continue;
                }

                if (expression is not null && CanUseUnsafeExpression(expression, semanticModel))
                {
                    expressionFixes.Add(expression);
                    continue;
                }

                if (statement is not null && CanWrapInUnsafeBlock(statement))
                {
                    AddStatementFix(statementFixes, statement, StatementFix.Wrap());
                    continue;
                }

                if (FindExecutableBody(target) is BlockSyntax body && CanWrapInUnsafeBlock(body))
                    fallbackBodies.Add(body);
            }

            if (fallbackBodies.Count > 0)
            {
                BlockSyntax[] bodies = RemoveNestedNodes(fallbackBodies);
                return document.WithSyntaxRoot(WrapBodies(root, bodies));
            }

            StatementSyntax[] statements = statementFixes.Keys
                .Where(statement => !statement.Ancestors()
                    .OfType<StatementSyntax>()
                    .Any(statementFixes.ContainsKey))
                .ToArray();

            expressionFixes.RemoveWhere(expression =>
                expression.AncestorsAndSelf().OfType<StatementSyntax>().Any(statements.Contains));

            ExpressionSyntax[] expressions = RemoveNestedExpressions(expressionFixes);
            if (statements.Length == 0 && expressions.Length == 0)
                return document;

            SyntaxNode changedRoot = ApplyStatementAndExpressionFixes(
                root,
                statements,
                statementFixes,
                expressions);

            return document.WithSyntaxRoot(changedRoot);
        }

        private static SyntaxNode WrapBodies(SyntaxNode root, IReadOnlyCollection<BlockSyntax> bodies)
        {
            var annotations = bodies.ToDictionary(static body => body, static _ => new SyntaxAnnotation());
            SyntaxNode changedRoot = root.ReplaceNodes(
                annotations.Keys,
                (original, rewritten) => rewritten.WithAdditionalAnnotations(annotations[original]));

            foreach (BlockSyntax originalBody in bodies)
            {
                var body = (BlockSyntax)changedRoot.GetAnnotatedNodes(annotations[originalBody]).Single();
                var unsafeStatement = UnsafeCodeFixHelpers.CreateUnsafeStatement(body.Statements.ToArray());
                var replacement = body.WithStatements([unsafeStatement])
                    .WithAdditionalAnnotations(Formatter.Annotation);
                changedRoot = changedRoot.ReplaceNode(body, replacement);
            }

            return changedRoot;
        }

        private static SyntaxNode ApplyStatementAndExpressionFixes(
            SyntaxNode root,
            IReadOnlyCollection<StatementSyntax> statements,
            IReadOnlyDictionary<StatementSyntax, StatementFix> fixes,
            IReadOnlyCollection<ExpressionSyntax> expressions)
        {
            var statementAnnotations = statements.ToDictionary(
                static statement => statement,
                static _ => new SyntaxAnnotation());
            var expressionAnnotations = expressions.ToDictionary(
                static expression => expression,
                static _ => new SyntaxAnnotation());

            var annotations = statementAnnotations
                .Select(static pair => (Node: (SyntaxNode)pair.Key, pair.Value))
                .Concat(expressionAnnotations.Select(static pair => (Node: (SyntaxNode)pair.Key, pair.Value)))
                .ToDictionary(static pair => pair.Node, static pair => pair.Value);

            SyntaxNode changedRoot = root.ReplaceNodes(
                annotations.Keys,
                (original, rewritten) => rewritten.WithAdditionalAnnotations(annotations[original]));

            foreach (StatementSyntax originalStatement in statements)
            {
                var statement = (StatementSyntax)changedRoot
                    .GetAnnotatedNodes(statementAnnotations[originalStatement])
                    .Single();
                StatementFix fix = fixes[originalStatement];

                changedRoot = fix.Kind switch
                {
                    StatementFixKind.ForwardDeclaration => ReplaceWithForwardDeclaration(
                        changedRoot,
                        statement,
                        fix),
                    _ => changedRoot.ReplaceNode(statement, WrapStatement(statement))
                };
            }

            foreach (ExpressionSyntax originalExpression in expressions)
            {
                var expression = (ExpressionSyntax)changedRoot
                    .GetAnnotatedNodes(expressionAnnotations[originalExpression])
                    .Single();

                if (UnsafeCodeFixHelpers.CreateUnsafeExpression(expression) is ExpressionSyntax replacement)
                    changedRoot = changedRoot.ReplaceNode(expression, replacement);
            }

            return changedRoot;
        }

        private static SyntaxNode ReplaceWithForwardDeclaration(
            SyntaxNode root,
            StatementSyntax statement,
            StatementFix fix)
        {
            var forwardDeclaration = fix.ForwardDeclaration!
                .WithLeadingTrivia(statement.GetLeadingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            var unsafeStatement = UnsafeCodeFixHelpers.CreateUnsafeStatement(fix.Assignment!)
                .WithTrailingTrivia(statement.GetTrailingTrivia());

            return statement.Parent switch
            {
                BlockSyntax block => root.ReplaceNode(
                    block,
                    block.WithStatements(ReplaceStatement(
                        block.Statements,
                        statement,
                        [forwardDeclaration, unsafeStatement]))
                        .WithAdditionalAnnotations(Formatter.Annotation)),

                SwitchSectionSyntax section => root.ReplaceNode(
                    section,
                    section.WithStatements(ReplaceStatement(
                        section.Statements,
                        statement,
                        [forwardDeclaration, unsafeStatement]))
                        .WithAdditionalAnnotations(Formatter.Annotation)),

                GlobalStatementSyntax globalStatement
                    when globalStatement.Parent is CompilationUnitSyntax compilationUnit => root.ReplaceNode(
                        compilationUnit,
                        compilationUnit.WithMembers(ReplaceGlobalStatement(
                            compilationUnit.Members,
                            globalStatement,
                            [
                                SyntaxFactory.GlobalStatement(forwardDeclaration),
                                SyntaxFactory.GlobalStatement(unsafeStatement)
                            ]))
                            .WithAdditionalAnnotations(Formatter.Annotation)),

                _ => root
            };
        }

        private static SyntaxList<StatementSyntax> ReplaceStatement(
            SyntaxList<StatementSyntax> statements,
            StatementSyntax statement,
            IReadOnlyList<StatementSyntax> replacements)
        {
            int index = statements.IndexOf(statement);
            if (index < 0)
                return statements;

            return statements.RemoveAt(index).InsertRange(index, replacements);
        }

        private static SyntaxList<MemberDeclarationSyntax> ReplaceGlobalStatement(
            SyntaxList<MemberDeclarationSyntax> members,
            GlobalStatementSyntax statement,
            IReadOnlyList<GlobalStatementSyntax> replacements)
        {
            int index = members.IndexOf(statement);
            if (index < 0)
                return members;

            return members.RemoveAt(index).InsertRange(index, replacements);
        }

        private static StatementSyntax WrapStatement(StatementSyntax statement)
        {
            var unsafeStatement = UnsafeCodeFixHelpers.CreateUnsafeStatement(statement.WithoutTrivia());
            if (statement.Parent is BlockSyntax or SwitchSectionSyntax)
            {
                return unsafeStatement
                    .WithLeadingTrivia(statement.GetLeadingTrivia().AddRange(unsafeStatement.GetLeadingTrivia()))
                    .WithTrailingTrivia(statement.GetTrailingTrivia());
            }

            return SyntaxFactory.Block(unsafeStatement)
                .WithLeadingTrivia(statement.GetLeadingTrivia())
                .WithTrailingTrivia(statement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static bool TryCreateForwardDeclarationFix(
            LocalDeclarationStatementSyntax declaration,
            SemanticModel semanticModel,
            out StatementFix fix)
        {
            fix = default;

            if (declaration.IsConst ||
                declaration.UsingKeyword.RawKind != 0 ||
                declaration.AwaitKeyword.RawKind != 0 ||
                declaration.Declaration.Variables.Count != 1 ||
                declaration.Declaration.Type is RefTypeSyntax ||
                declaration.Declaration.Type.DescendantNodesAndSelf().OfType<RefTypeSyntax>().Any() ||
                UnsafeCodeFixHelpers.ContainsDirectives(declaration))
            {
                return false;
            }

            VariableDeclaratorSyntax variable = declaration.Declaration.Variables[0];
            if (variable.Initializer is not { } initializer)
                return false;

            TypeSyntax type = declaration.Declaration.Type;
            ILocalSymbol? localSymbol = semanticModel.GetDeclaredSymbol(variable) as ILocalSymbol;
            ITypeSymbol? localType = localSymbol?.Type ??
                semanticModel.GetTypeInfo(type).Type;

            if (localType?.IsAnonymousType == true)
            {
                return false;
            }

            if (type.IsVar)
            {
                if (localType is null or IErrorTypeSymbol)
                    return false;

                string typeName = localType.ToMinimalDisplayString(semanticModel, declaration.SpanStart);
                type = SyntaxFactory.ParseTypeName(typeName)
                    .WithTriviaFrom(type)
                    .WithAdditionalAnnotations(Simplifier.Annotation);
            }

            if ((localType?.IsRefLikeType == true || IsSpanTypeSyntax(type)) &&
                type is not ScopedTypeSyntax)
            {
                if (RefLikeLocalRequiresUnsafeExpression(
                    declaration,
                    variable.Identifier.ValueText,
                    localSymbol,
                    semanticModel))
                {
                    return false;
                }

                type = SyntaxFactory.ScopedType(type.WithoutTrivia())
                    .WithTriviaFrom(type);
            }

            var forwardVariable = variable.WithInitializer(null);
            var forwardDeclaration = declaration
                .WithDeclaration(declaration.Declaration
                    .WithType(type)
                    .WithVariables([forwardVariable]))
                .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

            var assignment = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(variable.Identifier),
                    initializer.Value.WithoutTrivia().NormalizeWhitespace(elasticTrivia: true)))
                .WithAdditionalAnnotations(Formatter.Annotation);

            fix = StatementFix.Forward(forwardDeclaration, assignment);
            return true;
        }

        private static bool IsSpanTypeSyntax(TypeSyntax type)
        {
            if (type is ScopedTypeSyntax scopedType)
                return IsSpanTypeSyntax(scopedType.Type);

            string name = type.ToString();
            return name.StartsWith("Span<", StringComparison.Ordinal) ||
                name.StartsWith("ReadOnlySpan<", StringComparison.Ordinal) ||
                name.StartsWith("System.Span<", StringComparison.Ordinal) ||
                name.StartsWith("System.ReadOnlySpan<", StringComparison.Ordinal) ||
                name.StartsWith("global::System.Span<", StringComparison.Ordinal) ||
                name.StartsWith("global::System.ReadOnlySpan<", StringComparison.Ordinal);
        }

        private static bool RefLikeLocalRequiresUnsafeExpression(
            LocalDeclarationStatementSyntax declaration,
            string localName,
            ILocalSymbol? local,
            SemanticModel semanticModel)
        {
            SyntaxNode scope = FindExecutableBody(declaration) ?? declaration.Parent ?? declaration;
            foreach (IdentifierNameSyntax identifier in scope.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Where(identifier => identifier.SpanStart > declaration.Span.End))
            {
                if (identifier.Identifier.ValueText != localName ||
                    local is not null &&
                    !SymbolEqualityComparer.Default.Equals(
                        semanticModel.GetSymbolInfo(identifier).Symbol,
                        local))
                {
                    continue;
                }

                ReturnStatementSyntax? returnStatement = identifier.Ancestors()
                    .OfType<ReturnStatementSyntax>()
                    .FirstOrDefault();
                if (returnStatement?.Expression is { } returnedExpression &&
                    semanticModel.GetTypeInfo(returnedExpression).Type?.IsRefLikeType == true)
                {
                    return true;
                }

                AssignmentExpressionSyntax? assignment = identifier.Ancestors()
                    .OfType<AssignmentExpressionSyntax>()
                    .FirstOrDefault();
                if (assignment is not null &&
                    assignment.Right.Span.Contains(identifier.Span) &&
                    IsRefLikeTarget(assignment.Left, scope, semanticModel))
                {
                    return true;
                }

                EqualsValueClauseSyntax? initializer = identifier.Ancestors()
                    .OfType<EqualsValueClauseSyntax>()
                    .FirstOrDefault();
                if (initializer?.Parent is VariableDeclaratorSyntax targetVariable &&
                    (semanticModel.GetDeclaredSymbol(targetVariable) is ILocalSymbol
                    {
                        Type.IsRefLikeType: true
                    } ||
                    targetVariable.Parent is VariableDeclarationSyntax targetDeclaration &&
                    IsSpanTypeSyntax(targetDeclaration.Type)))
                {
                    return true;
                }

                if (identifier.Ancestors().OfType<ArgumentSyntax>().Any(static argument =>
                    argument.RefKindKeyword.RawKind != 0))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsRefLikeTarget(
            ExpressionSyntax expression,
            SyntaxNode scope,
            SemanticModel semanticModel)
        {
            if (semanticModel.GetTypeInfo(expression).Type?.IsRefLikeType == true)
                return true;

            if (expression is not IdentifierNameSyntax identifier)
                return false;

            return scope.DescendantNodes()
                .OfType<VariableDeclaratorSyntax>()
                .Any(variable =>
                    variable.Identifier.ValueText == identifier.Identifier.ValueText &&
                    variable.Parent is VariableDeclarationSyntax declaration &&
                    IsSpanTypeSyntax(declaration.Type));
        }

        private static void AddStatementFix(
            Dictionary<StatementSyntax, StatementFix> fixes,
            StatementSyntax statement,
            StatementFix fix)
        {
            if (!fixes.ContainsKey(statement))
                fixes.Add(statement, fix);
        }

        private static bool RequiresUnsafeExpression(
            SyntaxNode target,
            StatementSyntax? statement)
        {
            if (statement is null ||
                statement is LocalFunctionStatementSyntax ||
                target.AncestorsAndSelf().Any(static node =>
                    node is CatchFilterClauseSyntax or ConstructorInitializerSyntax or AttributeArgumentSyntax))
            {
                return true;
            }

            if (UnsafeCodeFixHelpers.ContainsDirectives(statement) ||
                statement.DescendantNodesAndSelf().Any(static node =>
                    node is AwaitExpressionSyntax or YieldStatementSyntax))
            {
                return true;
            }

            return statement is LocalDeclarationStatementSyntax localDeclaration &&
                (localDeclaration.UsingKeyword.RawKind != 0 ||
                    localDeclaration.AwaitKeyword.RawKind != 0);
        }

        private static bool CanUseUnsafeExpression(
            ExpressionSyntax expression,
            SemanticModel semanticModel)
        {
            if (expression.DescendantNodesAndSelf().Any(static node => node is AwaitExpressionSyntax))
                return false;

            ITypeSymbol? type = semanticModel.GetTypeInfo(expression).Type;
            return type?.SpecialType != SpecialType.System_Void;
        }

        private static ExpressionSyntax? FindFixableExpression(
            SyntaxNode target,
            TextSpan diagnosticSpan)
        {
            foreach (ExpressionSyntax expression in target.AncestorsAndSelf().OfType<ExpressionSyntax>())
            {
                if (!expression.Span.Contains(diagnosticSpan))
                    continue;

                if (expression is IdentifierNameSyntax or GenericNameSyntax)
                {
                    if (expression.Parent is InvocationExpressionSyntax invocation &&
                        invocation.Expression == expression)
                    {
                        continue;
                    }

                    if (expression.Parent is MemberAccessExpressionSyntax memberAccess &&
                        memberAccess.Name == expression)
                    {
                        continue;
                    }
                }

                return expression;
            }

            return null;
        }

        private static BlockSyntax? FindExecutableBody(SyntaxNode target)
            => target.AncestorsAndSelf().OfType<BlockSyntax>().FirstOrDefault(static block =>
                block.Parent is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or
                    LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax);

        private static bool CanWrapInUnsafeBlock(SyntaxNode node)
            => !UnsafeCodeFixHelpers.ContainsDirectives(node) &&
                !node.DescendantNodesAndSelf().Any(static descendant =>
                    descendant is AwaitExpressionSyntax or YieldStatementSyntax);

        private static TNode[] RemoveNestedNodes<TNode>(IEnumerable<TNode> nodes)
            where TNode : SyntaxNode
        {
            TNode[] candidates = nodes.Distinct().ToArray();
            return candidates
                .Where(candidate => !candidate.Ancestors().OfType<TNode>().Any(candidates.Contains))
                .ToArray();
        }

        private static ExpressionSyntax[] RemoveNestedExpressions(IEnumerable<ExpressionSyntax> expressions)
        {
            var selected = new List<ExpressionSyntax>();
            foreach (ExpressionSyntax expression in expressions
                .Distinct()
                .OrderByDescending(static expression => expression.Span.Length))
            {
                if (!selected.Any(selectedExpression => selectedExpression.Span.Contains(expression.Span)))
                    selected.Add(expression);
            }

            return selected.ToArray();
        }

        private static bool IsSkipLocalsInitEnabled(Document document)
            => document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(
                $"build_property.{MSBuildPropertyOptionNames.SkipLocalsInit}",
                out string? value) &&
                string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }
}
#endif
