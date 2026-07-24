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
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix
{
    /// <summary>
    /// Propagates intentional caller-unsafe contracts through partials, overrides, and interface implementations.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SynchronizeUnsafeContractCodeFixProvider)), Shared]
    public sealed class SynchronizeUnsafeContractCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        public const string UnsafeOverrideDiagnosticId = "CS9364";
        public const string UnsafeImplicitImplementationDiagnosticId = "CS9365";
        public const string UnsafeExplicitImplementationDiagnosticId = "CS9366";
        public const string PartialUnsafeMismatchDiagnosticId = "CS0764";
        public const string PartialSafeMismatchDiagnosticId = "CS9390";

        private static LocalizableString UnsafeCodeFixTitle =>
            new LocalizableResourceString(
                nameof(Resources.SynchronizeUnsafeContractCodeFixTitle),
                Resources.ResourceManager,
                typeof(Resources));

        public override ImmutableArray<string> FixableDiagnosticIds =>
            [
                UnsafeOverrideDiagnosticId,
                UnsafeImplicitImplementationDiagnosticId,
                UnsafeExplicitImplementationDiagnosticId,
                PartialUnsafeMismatchDiagnosticId,
                PartialSafeMismatchDiagnosticId,
            ];

        public override FixAllProvider GetFixAllProvider() => ContractFixAllProvider.Instance;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics[0];
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null)
                return;

            SyntaxNode targetNode = root.FindNode(
                diagnostic.Location.SourceSpan,
                getInnermostNodeForTie: true);
            if (targetNode.AncestorsAndSelf().OfType<VariableDeclaratorSyntax>()
                .FirstOrDefault(static variable =>
                    variable.Parent?.Parent is EventFieldDeclarationSyntax) is { } eventVariable
                && eventVariable.Parent?.Parent is EventFieldDeclarationSyntax
                {
                    Declaration.Variables.Count: > 1,
                } eventField
                && eventField.ContainsDirectives)
            {
                return;
            }

            if (UnsafeModifierCodeFixHelpers.FindDeclaration(targetNode) is not { } declaration
                || GetDeclaredContractSymbol(
                    semanticModel,
                    targetNode,
                    declaration,
                    context.CancellationToken) is not { } declaredSymbol)
            {
                return;
            }

            ISymbol symbol = GetContractSymbolForDiagnostic(
                diagnostic,
                declaredSymbol,
                context.CancellationToken);
            if (!IsSupportedContractSymbol(symbol))
                return;

            HashSet<ISymbol> closure = await GetContractClosureAsync(
                context.Document.Project.Solution,
                symbol,
                context.CancellationToken).ConfigureAwait(false);
            if (!CanPropagateUnsafeContract(
                context.Document.Project.Solution,
                closure,
                context.CancellationToken))
            {
                return;
            }

            string unsafeTitle = UnsafeCodeFixTitle.ToString();
            context.RegisterCodeFix(
                CodeAction.Create(
                    unsafeTitle,
                    cancellationToken => ApplyModifierAsync(
                        context.Document.Project.Solution,
                        closure,
                        cancellationToken),
                    unsafeTitle),
                diagnostic);
        }

        private sealed class ContractFixAllProvider : FixAllProvider
        {
            internal static ContractFixAllProvider Instance { get; } = new();

            public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
            {
                ImmutableArray<Diagnostic> diagnostics =
                    await GetDiagnosticsInScopeAsync(fixAllContext).ConfigureAwait(false);
                if (diagnostics.IsEmpty)
                    return null;

                string title = UnsafeCodeFixTitle.ToString();
                return CodeAction.Create(
                    title,
                    cancellationToken => PropagateDiagnosticsAsync(
                        fixAllContext.Solution,
                        diagnostics,
                        cancellationToken),
                    fixAllContext.CodeActionEquivalenceKey ?? title);
            }

            private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsInScopeAsync(
                FixAllContext context) =>
                context.Scope switch
                {
                    FixAllScope.Document => await context
                        .GetDocumentDiagnosticsAsync(context.Document!)
                        .ConfigureAwait(false),
                    FixAllScope.Project => await context
                        .GetAllDiagnosticsAsync(context.Project!)
                        .ConfigureAwait(false),
                    FixAllScope.Solution => ImmutableArray.CreateRange(
                        (await Task.WhenAll(
                            context.Solution.Projects.Select(context.GetAllDiagnosticsAsync))
                            .ConfigureAwait(false))
                        .SelectMany(static diagnostics => diagnostics)),
                    _ => [],
                };
        }

        private static async Task<Solution> PropagateDiagnosticsAsync(
            Solution solution,
            ImmutableArray<Diagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            var closure = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            foreach (Diagnostic diagnostic in diagnostics)
            {
                if (await GetDiagnosticSymbolAsync(
                    solution,
                    diagnostic,
                    cancellationToken).ConfigureAwait(false) is not { } symbol)
                {
                    continue;
                }

                HashSet<ISymbol> diagnosticClosure = await GetContractClosureAsync(
                    solution,
                    symbol,
                    cancellationToken).ConfigureAwait(false);
                if (CanPropagateUnsafeContract(
                    solution,
                    diagnosticClosure,
                    cancellationToken))
                {
                    closure.UnionWith(diagnosticClosure);
                }
            }

            return await ApplyModifierAsync(
                solution,
                closure,
                cancellationToken).ConfigureAwait(false);
        }

        private static async Task<ISymbol?> GetDiagnosticSymbolAsync(
            Solution solution,
            Diagnostic diagnostic,
            CancellationToken cancellationToken)
        {
            Document? document = solution.GetDocument(diagnostic.Location.SourceTree);
            if (document is null)
                return null;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null)
                return null;

            SyntaxNode targetNode = root.FindNode(
                diagnostic.Location.SourceSpan,
                getInnermostNodeForTie: true);
            if (targetNode.AncestorsAndSelf().OfType<VariableDeclaratorSyntax>()
                .FirstOrDefault(static variable =>
                    variable.Parent?.Parent is EventFieldDeclarationSyntax) is { } eventVariable
                && eventVariable.Parent?.Parent is EventFieldDeclarationSyntax
                {
                    Declaration.Variables.Count: > 1,
                } eventField
                && eventField.ContainsDirectives)
            {
                return null;
            }

            if (UnsafeModifierCodeFixHelpers.FindDeclaration(targetNode) is not { } declaration
                || GetDeclaredContractSymbol(
                    semanticModel,
                    targetNode,
                    declaration,
                    cancellationToken) is not { } declaredSymbol)
            {
                return null;
            }

            ISymbol symbol = GetContractSymbolForDiagnostic(
                diagnostic,
                declaredSymbol,
                cancellationToken);
            return IsSupportedContractSymbol(symbol) ? symbol : null;
        }

        private static ISymbol GetContractSymbolForDiagnostic(
            Diagnostic diagnostic,
            ISymbol declaredSymbol,
            CancellationToken cancellationToken)
        {
            ISymbol symbol = UnsafeContractHelpers.NormalizeContractSymbol(declaredSymbol);
            if (diagnostic.Id is not (PartialUnsafeMismatchDiagnosticId or PartialSafeMismatchDiagnosticId)
                || symbol is not IPropertySymbol property)
            {
                return symbol;
            }

            IPropertySymbol[] propertyParts = UnsafeContractHelpers.GetPartialParts(property)
                .OfType<IPropertySymbol>()
                .ToArray();
            if (propertyParts.Length < 2)
                return symbol;

            if (HasMismatch(propertyParts, HasPropertyUnsafe))
                return symbol;

            if (property.GetMethod is not null
                && HasMismatch(propertyParts, propertyPart =>
                    HasAccessorUnsafe(propertyPart, MethodKind.PropertyGet)))
            {
                return property.GetMethod;
            }

            if (property.SetMethod is not null
                && HasMismatch(propertyParts, propertyPart =>
                    HasAccessorUnsafe(propertyPart, MethodKind.PropertySet)))
            {
                return property.SetMethod;
            }

            return symbol;

            bool HasPropertyUnsafe(IPropertySymbol propertyPart) =>
                UnsafeContractHelpers.GetDirectDeclarations(propertyPart, cancellationToken)
                    .Any(declaration =>
                        UnsafeMigrationSyntaxHelpers.HasModifier(
                            declaration,
                            SyntaxKind.UnsafeKeyword));

            bool HasAccessorUnsafe(IPropertySymbol propertyPart, MethodKind accessorKind)
            {
                IMethodSymbol? accessor = accessorKind == MethodKind.PropertyGet
                    ? propertyPart.GetMethod
                    : propertyPart.SetMethod;
                return accessor is not null
                    && UnsafeContractHelpers.GetDirectDeclarations(accessor, cancellationToken)
                        .Any(declaration =>
                            declaration is AccessorDeclarationSyntax
                            && UnsafeMigrationSyntaxHelpers.HasModifier(
                                declaration,
                                SyntaxKind.UnsafeKeyword));
            }

            static bool HasMismatch(
                IEnumerable<IPropertySymbol> parts,
                System.Func<IPropertySymbol, bool> hasModifier)
            {
                bool? first = null;
                foreach (IPropertySymbol part in parts)
                {
                    bool current = hasModifier(part);
                    if (first is null)
                    {
                        first = current;
                    }
                    else if (first.Value != current)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private static ISymbol? GetDeclaredContractSymbol(
            SemanticModel semanticModel,
            SyntaxNode targetNode,
            SyntaxNode declaration,
            CancellationToken cancellationToken)
        {
            VariableDeclaratorSyntax? eventVariable = targetNode.AncestorsAndSelf()
                .OfType<VariableDeclaratorSyntax>()
                .FirstOrDefault(static variable =>
                    variable.Parent?.Parent is EventFieldDeclarationSyntax);
            return eventVariable is not null
                ? semanticModel.GetDeclaredSymbol(eventVariable, cancellationToken)
                : semanticModel.GetDeclaredSymbol(declaration, cancellationToken);
        }

        private static bool IsSupportedContractSymbol(ISymbol symbol) =>
            symbol is IMethodSymbol or IPropertySymbol or IEventSymbol;

        private static bool CanPropagateUnsafeContract(
            Solution solution,
            IEnumerable<ISymbol> symbols,
            CancellationToken cancellationToken)
        {
            foreach (ISymbol symbol in symbols)
            {
                bool hasEditableDeclaration = false;
                foreach (SyntaxNode declaration in UnsafeContractHelpers.GetDeclarations(
                    symbol,
                    cancellationToken))
                {
                    if (solution.GetDocument(declaration.SyntaxTree) is null)
                        continue;

                    if (declaration is VariableDeclaratorSyntax
                        {
                            Parent.Parent: EventFieldDeclarationSyntax eventField,
                        })
                    {
                        if (eventField.Declaration.Variables.Count > 1
                            && eventField.ContainsDirectives)
                        {
                            return false;
                        }

                        hasEditableDeclaration = true;
                        continue;
                    }

                    if (declaration is ArrowExpressionClauseSyntax
                        || UnsafeModifierCodeFixHelpers.FindDeclaration(declaration) is not null)
                    {
                        hasEditableDeclaration = true;
                    }
                }

                if (!hasEditableDeclaration)
                    return false;
            }

            return true;
        }

        private static async Task<HashSet<ISymbol>> GetContractClosureAsync(
            Solution solution,
            ISymbol seed,
            CancellationToken cancellationToken)
        {
            var closure = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var queue = new Queue<ISymbol>();

            Enqueue(seed);
            while (queue.Count > 0)
            {
                ISymbol symbol = queue.Dequeue();

                foreach (ISymbol part in UnsafeContractHelpers.GetPartialParts(symbol))
                    Enqueue(part);

                if (UnsafeContractHelpers.GetOverriddenMember(symbol) is { } overriddenMember)
                    Enqueue(overriddenMember);

                foreach (ISymbol interfaceMember in UnsafeContractHelpers.GetImplementedInterfaceMembers(symbol))
                    Enqueue(interfaceMember);

                foreach (ISymbol overridingMember in await SymbolFinder.FindOverridesAsync(
                    symbol,
                    solution,
                    cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    Enqueue(overridingMember);
                }

                if (symbol.ContainingType?.TypeKind == TypeKind.Interface)
                {
                    foreach (ISymbol implementation in await SymbolFinder.FindImplementationsAsync(
                        symbol,
                        solution,
                        cancellationToken: cancellationToken).ConfigureAwait(false))
                    {
                        Enqueue(implementation);
                    }
                }
            }

            return closure;

            void Enqueue(ISymbol symbol)
            {
                symbol = UnsafeContractHelpers.NormalizeContractSymbol(symbol);
                if (closure.Add(symbol))
                    queue.Enqueue(symbol);
            }
        }

        private static async Task<Solution> ApplyModifierAsync(
            Solution solution,
            IEnumerable<ISymbol> symbols,
            CancellationToken cancellationToken)
        {
            var targetsByDocument = new Dictionary<DocumentId, HashSet<TextSpan>>();
            var eventTargetsByDocument =
                new Dictionary<DocumentId, Dictionary<TextSpan, HashSet<string>>>();
            foreach (ISymbol symbol in symbols)
            {
                foreach (SyntaxNode declaration in UnsafeContractHelpers.GetDeclarations(symbol, cancellationToken))
                {
                    Document? document = solution.GetDocument(declaration.SyntaxTree);
                    if (document is null)
                        continue;

                    if (declaration is VariableDeclaratorSyntax
                        {
                            Parent.Parent: EventFieldDeclarationSyntax eventField,
                        } eventVariable)
                    {
                        if (!eventTargetsByDocument.TryGetValue(
                            document.Id,
                            out Dictionary<TextSpan, HashSet<string>>? eventFields))
                        {
                            eventFields = new Dictionary<TextSpan, HashSet<string>>();
                            eventTargetsByDocument.Add(document.Id, eventFields);
                        }

                        if (!eventFields.TryGetValue(
                            eventField.Span,
                            out HashSet<string>? eventNames))
                        {
                            eventNames = new HashSet<string>();
                            eventFields.Add(eventField.Span, eventNames);
                        }

                        eventNames.Add(eventVariable.Identifier.ValueText);
                        continue;
                    }

                    if (!targetsByDocument.TryGetValue(document.Id, out HashSet<TextSpan>? spans))
                    {
                        spans = new HashSet<TextSpan>();
                        targetsByDocument.Add(document.Id, spans);
                    }

                    spans.Add(declaration.Span);
                }
            }

            var documentIds = new HashSet<DocumentId>(targetsByDocument.Keys);
            documentIds.UnionWith(eventTargetsByDocument.Keys);
            foreach (DocumentId documentId in documentIds)
            {
                Document document = solution.GetDocument(documentId)!;
                var targets = new List<ModifierTarget>();
                if (targetsByDocument.TryGetValue(documentId, out HashSet<TextSpan>? spans))
                {
                    targets.AddRange(spans.Select(static span =>
                        new ModifierTarget(span)));
                }
                if (eventTargetsByDocument.TryGetValue(
                    documentId,
                    out Dictionary<TextSpan, HashSet<string>>? eventFields))
                {
                    targets.AddRange(eventFields.Select(static eventField =>
                        new ModifierTarget(eventField.Key)
                        {
                            EventNames = eventField.Value,
                        }));
                }
                foreach (ModifierTarget target in
                    targets.OrderByDescending(static target => target.Span.Start))
                {
                    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    if (root is null)
                        continue;

                    SyntaxNode node = root.FindNode(target.Span, getInnermostNodeForTie: true);
                    if (target.EventNames is not null)
                    {
                        EventFieldDeclarationSyntax? eventField = node.AncestorsAndSelf()
                            .OfType<EventFieldDeclarationSyntax>()
                            .FirstOrDefault();
                        if (eventField is not null)
                        {
                            document = await SetEventVariablesUnsafeAsync(
                                document,
                                root,
                                eventField,
                                target.EventNames,
                                cancellationToken).ConfigureAwait(false);
                        }
                        continue;
                    }

                    ArrowExpressionClauseSyntax? expressionBody = node.AncestorsAndSelf()
                        .OfType<ArrowExpressionClauseSyntax>()
                        .FirstOrDefault();
                    if (expressionBody is not null)
                    {
                        document = SetExpressionBodiedGetterUnsafe(
                            document,
                            root,
                            expressionBody);
                        continue;
                    }

                    if (UnsafeModifierCodeFixHelpers.FindDeclaration(node) is not { } declaration)
                        continue;

                    document = await UnsafeModifierCodeFixHelpers.SetUnsafeModifierAsync(
                        document,
                        declaration,
                        cancellationToken).ConfigureAwait(false);
                }

                solution = document.Project.Solution;
            }

            return solution;
        }

        private static Document SetExpressionBodiedGetterUnsafe(
            Document document,
            SyntaxNode root,
            ArrowExpressionClauseSyntax expressionBody)
        {
            SyntaxToken unsafeModifier = SyntaxFactory.Token(SyntaxKind.UnsafeKeyword)
                .WithTrailingTrivia(SyntaxFactory.ElasticSpace);
            AccessorDeclarationSyntax getter = SyntaxFactory.AccessorDeclaration(
                SyntaxKind.GetAccessorDeclaration)
                .WithModifiers([unsafeModifier])
                .WithExpressionBody(expressionBody)
                .WithSemicolonToken(expressionBody.Parent switch
                {
                    PropertyDeclarationSyntax property => property.SemicolonToken,
                    IndexerDeclarationSyntax indexer => indexer.SemicolonToken,
                    _ => default,
                });

            SyntaxNode replacement = expressionBody.Parent switch
            {
                PropertyDeclarationSyntax property => property
                    .WithExpressionBody(null)
                    .WithSemicolonToken(default)
                    .WithAccessorList(SyntaxFactory.AccessorList(
                        SyntaxFactory.SingletonList(getter))),
                IndexerDeclarationSyntax indexer => indexer
                    .WithExpressionBody(null)
                    .WithSemicolonToken(default)
                    .WithAccessorList(SyntaxFactory.AccessorList(
                        SyntaxFactory.SingletonList(getter))),
                _ => expressionBody.Parent!,
            };
            return document.WithSyntaxRoot(root.ReplaceNode(
                expressionBody.Parent!,
                replacement.WithAdditionalAnnotations(Formatter.Annotation)));
        }

        private static async Task<Document> SetEventVariablesUnsafeAsync(
            Document document,
            SyntaxNode root,
            EventFieldDeclarationSyntax eventField,
            HashSet<string> eventNames,
            CancellationToken cancellationToken)
        {
            SeparatedSyntaxList<VariableDeclaratorSyntax> variables = eventField.Declaration.Variables;
            if (variables.Count > 1 && eventField.ContainsDirectives)
                return document;

            if (variables.Count == 1
                || variables.All(variable => eventNames.Contains(variable.Identifier.ValueText)))
            {
                return await UnsafeModifierCodeFixHelpers.SetUnsafeModifierAsync(
                    document,
                    eventField,
                    cancellationToken).ConfigureAwait(false);
            }

            if (eventField.Parent is not TypeDeclarationSyntax containingType)
                return document;

            var splitEvents = new List<MemberDeclarationSyntax>(variables.Count);
            for (int i = 0; i < variables.Count; i++)
            {
                VariableDeclaratorSyntax variable = variables[i];
                EventFieldDeclarationSyntax splitEvent = eventField
                    .WithDeclaration(eventField.Declaration.WithVariables(
                        SyntaxFactory.SingletonSeparatedList(variable)))
                    .WithLeadingTrivia(i == 0
                        ? eventField.GetLeadingTrivia()
                        : SyntaxFactory.TriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed))
                    .WithTrailingTrivia(i == variables.Count - 1
                        ? eventField.GetTrailingTrivia()
                        : variables.GetSeparator(i).LeadingTrivia
                            .AddRange(variables.GetSeparator(i).TrailingTrivia)
                            .Add(SyntaxFactory.ElasticCarriageReturnLineFeed));

                if (eventNames.Contains(variable.Identifier.ValueText))
                    splitEvent = SetEventFieldUnsafe(splitEvent);

                splitEvents.Add(splitEvent.WithAdditionalAnnotations(Formatter.Annotation));
            }

            int memberIndex = containingType.Members.IndexOf(eventField);
            SyntaxList<MemberDeclarationSyntax> members = containingType.Members
                .RemoveAt(memberIndex)
                .InsertRange(memberIndex, splitEvents);
            TypeDeclarationSyntax replacement = containingType.WithMembers(members);
            return document.WithSyntaxRoot(root.ReplaceNode(containingType, replacement));
        }

        private static EventFieldDeclarationSyntax SetEventFieldUnsafe(
            EventFieldDeclarationSyntax eventField)
        {
            SyntaxToken safeModifier = UnsafeMigrationSyntaxHelpers.GetSafeModifier(eventField);
            if (safeModifier != default)
            {
                return eventField.ReplaceToken(
                    safeModifier,
                    SyntaxFactory.Token(SyntaxKind.UnsafeKeyword).WithTriviaFrom(safeModifier));
            }

            if (UnsafeMigrationSyntaxHelpers.HasModifier(eventField, SyntaxKind.UnsafeKeyword))
                return eventField;

            SyntaxTokenList modifiers = eventField.Modifiers;
            int insertionIndex = modifiers.IndexOf(SyntaxKind.ExternKeyword);
            int partialIndex = modifiers.IndexOf(SyntaxKind.PartialKeyword);
            if (partialIndex >= 0 && (insertionIndex < 0 || partialIndex < insertionIndex))
                insertionIndex = partialIndex;
            if (insertionIndex < 0)
                insertionIndex = modifiers.Count;

            SyntaxToken unsafeModifier = SyntaxFactory.Token(SyntaxKind.UnsafeKeyword)
                .WithTrailingTrivia(SyntaxFactory.ElasticSpace);
            if (modifiers.Count == 0)
            {
                unsafeModifier = unsafeModifier.WithLeadingTrivia(eventField.EventKeyword.LeadingTrivia);
                eventField = eventField.WithEventKeyword(
                    eventField.EventKeyword.WithLeadingTrivia(default(SyntaxTriviaList)));
            }
            else if (insertionIndex == 0)
            {
                unsafeModifier = unsafeModifier.WithLeadingTrivia(modifiers[0].LeadingTrivia);
                modifiers = modifiers.Replace(
                    modifiers[0],
                    modifiers[0].WithLeadingTrivia(default(SyntaxTriviaList)));
            }

            return eventField.WithModifiers(modifiers.Insert(insertionIndex, unsafeModifier));
        }

        private sealed class ModifierTarget
        {
            internal ModifierTarget(TextSpan span)
            {
                Span = span;
            }

            internal TextSpan Span { get; }
            internal HashSet<string>? EventNames { get; init; }
        }
    }
}
#endif
