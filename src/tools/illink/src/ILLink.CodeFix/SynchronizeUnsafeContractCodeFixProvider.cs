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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix
{
    /// <summary>
    /// Fixes compiler diagnostics <c>CS9364</c>, <c>CS9365</c> and <c>CS9366</c>, reported when an
    /// <c>unsafe</c> member overrides or implements a member that is not caller-unsafe.
    /// </summary>
    /// <remarks>
    /// Two fixes are offered because the compiler cannot tell which side is wrong. Removing <c>unsafe</c> from
    /// the derived member is correct when it was an unsafe-v1 lexical scope, and is always available. Marking
    /// the base member <c>unsafe</c> is correct when the contract was genuinely under-annotated, and is only
    /// offered when that member is declared in source.
    /// </remarks>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SynchronizeUnsafeContractCodeFixProvider)), Shared]
    public sealed class SynchronizeUnsafeContractCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        public const string UnsafeCannotOverrideSafeDiagnosticId = "CS9364";
        public const string UnsafeCannotImplicitlyImplementSafeDiagnosticId = "CS9365";
        public const string UnsafeCannotExplicitlyImplementSafeDiagnosticId = "CS9366";

        // The keys are deliberately independent of the member being fixed, so that "fix all occurrences"
        // groups every use of the same action rather than one group per member name.
        private const string RemoveEquivalenceKey = nameof(SynchronizeUnsafeContractCodeFixProvider) + ".Remove";
        private const string AddToBaseEquivalenceKey = nameof(SynchronizeUnsafeContractCodeFixProvider) + ".AddToBase";
        private const string ReplaceWithSafeEquivalenceKey = nameof(SynchronizeUnsafeContractCodeFixProvider) + ".ReplaceWithSafe";

        private static LocalizableString RemoveTitle =>
            new LocalizableResourceString(
                nameof(Resources.RemoveUnsafeFromDerivedMemberCodeFixTitle),
                Resources.ResourceManager,
                typeof(Resources));

        private static LocalizableString AddToBaseTitle =>
            new LocalizableResourceString(
                nameof(Resources.AddUnsafeToBaseMemberCodeFixTitle),
                Resources.ResourceManager,
                typeof(Resources));

        private static LocalizableString ReplaceWithSafeTitle =>
            new LocalizableResourceString(
                nameof(Resources.ReplaceUnsafeWithSafeCodeFixTitle),
                Resources.ResourceManager,
                typeof(Resources));

        public override ImmutableArray<string> FixableDiagnosticIds =>
            [
                UnsafeCannotOverrideSafeDiagnosticId,
                UnsafeCannotImplicitlyImplementSafeDiagnosticId,
                UnsafeCannotExplicitlyImplementSafeDiagnosticId,
            ];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics[0];
            if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } root)
                return;

            SyntaxNode targetNode = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            if (FindUnsafeDeclaration(targetNode) is not { } derivedDeclaration)
                return;

            string removeTitle = RemoveTitle.ToString();
            bool isExtern = UnsafeMigrationSyntaxHelpers.HasModifier(derivedDeclaration, SyntaxKind.ExternKeyword);
            if (isExtern)
            {
                // An extern member must keep an explicit marker, so removing 'unsafe' would only trade
                // CS9364/CS9365/CS9366 for CS9389. Narrowing the contract to 'safe' is the equivalent edit.
                if (UnsafeMigrationSyntaxHelpers.SafeKeywordKind != SyntaxKind.None)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            ReplaceWithSafeTitle.ToString(),
                            cancellationToken => UnsafeModifierCodeFixHelpers.ReplaceUnsafeWithSafeAsync(
                                context.Document,
                                derivedDeclaration,
                                cancellationToken),
                            ReplaceWithSafeEquivalenceKey),
                        diagnostic);
                }
            }
            else
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        removeTitle,
                        cancellationToken => UnsafeModifierCodeFixHelpers.RemoveUnsafeModifierAsync(
                            context.Document,
                            derivedDeclaration,
                            cancellationToken),
                        RemoveEquivalenceKey),
                    diagnostic);
            }

            if (await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false) is not { } semanticModel
                || GetDeclaredSymbol(semanticModel, targetNode, derivedDeclaration, context.CancellationToken) is not { } derivedSymbol)
            {
                return;
            }

            // Only base members declared in source can be annotated, and only ones that are missing the modifier
            // on every one of their declarations.
            List<(DocumentId DocumentId, SyntaxNode Declaration)> baseDeclarations = [];
            foreach (ISymbol baseSymbol in GetBaseContracts(derivedSymbol))
            {
                if (GetEditableDeclarations(baseSymbol, context.Document.Project.Solution, context.CancellationToken) is not { Count: > 0 } declarations
                    || declarations.Any(static pair => UnsafeMigrationSyntaxHelpers.HasSafeModifier(pair.Declaration)))
                {
                    return;
                }

                baseDeclarations.AddRange(declarations);
            }

            if (baseDeclarations.Count == 0)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    AddToBaseTitle.ToString(),
                    cancellationToken => AddUnsafeToBaseAsync(context.Document.Project.Solution, baseDeclarations, cancellationToken),
                    AddToBaseEquivalenceKey),
                diagnostic);
        }

        /// <summary>
        /// Resolves the symbol whose contract the diagnostic is about.
        /// </summary>
        /// <remarks>
        /// A field or field-like event declaration declares one symbol per variable and has no symbol of its
        /// own, so the declarator the diagnostic points at is what has to be asked.
        /// </remarks>
        private static ISymbol? GetDeclaredSymbol(
            SemanticModel semanticModel,
            SyntaxNode targetNode,
            SyntaxNode declaration,
            CancellationToken cancellationToken)
        {
            if (declaration is BaseFieldDeclarationSyntax)
            {
                return targetNode.AncestorsAndSelf().OfType<VariableDeclaratorSyntax>().FirstOrDefault() is { } variable
                    ? semanticModel.GetDeclaredSymbol(variable, cancellationToken)
                    : null;
            }

            return semanticModel.GetDeclaredSymbol(declaration, cancellationToken);
        }

        private static async Task<Solution> AddUnsafeToBaseAsync(
            Solution solution,
            List<(DocumentId DocumentId, SyntaxNode Declaration)> declarations,
            CancellationToken cancellationToken)
        {
            // All declarations in a document are edited in a single pass so that no node has to be re-resolved
            // against a tree whose spans have already shifted.
            foreach (IGrouping<DocumentId, (DocumentId DocumentId, SyntaxNode Declaration)> group in declarations.GroupBy(static pair => pair.DocumentId))
            {
                if (solution.GetDocument(group.Key) is not { } document)
                    continue;

                var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
                var seenSpans = new HashSet<TextSpan>();
                bool edited = false;
                foreach ((_, SyntaxNode declaration) in group)
                {
                    // Two contracts can share one declaration, for example a member that implements both
                    // 'I<int>.M' and 'I<string>.M', and the editor cannot replace the same node twice.
                    if (!seenSpans.Add(declaration.Span)
                        || UnsafeMigrationSyntaxHelpers.HasModifier(declaration, SyntaxKind.UnsafeKeyword))
                    {
                        continue;
                    }

                    editor.ReplaceNode(
                        declaration,
                        UnsafeModifierCodeFixHelpers.AddModifier(declaration, SyntaxKind.UnsafeKeyword));
                    edited = true;
                }

                if (edited)
                    solution = editor.GetChangedDocument().Project.Solution;
            }

            return solution;
        }

        private static List<(DocumentId DocumentId, SyntaxNode Declaration)> GetEditableDeclarations(
            ISymbol symbol,
            Solution solution,
            CancellationToken cancellationToken)
        {
            var declarations = new List<(DocumentId, SyntaxNode)>();

            // A partial member is two symbols, each seeing only its own declaration, but both parts must agree
            // on the modifier or the edit trades one diagnostic for CS0764.
            foreach (ISymbol part in GetSymbolAndPartialParts(symbol))
            {
                foreach (SyntaxReference reference in part.DeclaringSyntaxReferences)
                {
                    if (solution.GetDocumentId(reference.SyntaxTree) is not { } documentId)
                        return [];

                    SyntaxNode declaration = reference.GetSyntax(cancellationToken);
                    if (declaration is VariableDeclaratorSyntax variable && variable.Parent?.Parent is BaseFieldDeclarationSyntax field)
                        declaration = field;

                    declarations.Add((documentId, declaration));
                }
            }

            return declarations;
        }

        private static IEnumerable<ISymbol> GetSymbolAndPartialParts(ISymbol symbol)
        {
            yield return symbol;

            ISymbol? otherPart = symbol switch
            {
                IMethodSymbol method => (ISymbol?)method.PartialDefinitionPart ?? method.PartialImplementationPart,
                IPropertySymbol property => (ISymbol?)property.PartialDefinitionPart ?? property.PartialImplementationPart,
                IEventSymbol @event => (ISymbol?)@event.PartialDefinitionPart ?? @event.PartialImplementationPart,
                _ => null,
            };

            if (otherPart is not null)
                yield return otherPart;
        }

        /// <summary>
        /// Finds the declaration that carries the <c>unsafe</c> modifier the compiler objected to.
        /// </summary>
        /// <remarks>
        /// For a property or event, the diagnostic is reported on the accessor while <c>unsafe</c> may be on the
        /// containing declaration, so the search continues outward until a declaration with the modifier is found.
        /// </remarks>
        private static SyntaxNode? FindUnsafeDeclaration(SyntaxNode node) =>
            node.AncestorsAndSelf()
                .Where(static ancestor => ancestor is BaseMethodDeclarationSyntax
                    or BasePropertyDeclarationSyntax
                    or BaseFieldDeclarationSyntax
                    or AccessorDeclarationSyntax)
                .FirstOrDefault(static ancestor => UnsafeMigrationSyntaxHelpers.HasModifier(ancestor, SyntaxKind.UnsafeKeyword));

        /// <summary>
        /// Collects every member whose contract the derived member has to match.
        /// </summary>
        /// <remarks>
        /// The compiler compares an override against the original definition of its chain, so annotating the
        /// root is enough and the members in between can stay as they are. A member can also override one member
        /// while implementing another, in which case both have to be annotated for the derived member to be
        /// legal, and both diagnostics are reported on it.
        /// </remarks>
        private static ImmutableArray<ISymbol> GetBaseContracts(ISymbol symbol)
        {
            var contracts = new List<ISymbol>();
            var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default) { symbol };

            var chain = new List<ISymbol> { symbol };
            for (ISymbol current = symbol; GetOverriddenMember(current) is { } overridden; current = overridden)
                chain.Add(overridden);

            if (seen.Add(chain[chain.Count - 1]))
                contracts.Add(chain[chain.Count - 1]);

            // An interface implementation is attributed to the member that declares it rather than to the most
            // derived override, so every member of the chain has to be asked about its own interfaces.
            foreach (ISymbol member in chain)
            {
                foreach (ISymbol interfaceMember in GetImplementedInterfaceMembers(member))
                {
                    if (seen.Add(interfaceMember))
                        contracts.Add(interfaceMember);
                }
            }

            return [.. contracts];
        }

        private static IEnumerable<ISymbol> GetImplementedInterfaceMembers(ISymbol symbol)
        {
            // An explicit implementation names its interface member directly; its own name is qualified, so the
            // scan below would not match it.
            foreach (ISymbol explicitImplementation in GetExplicitInterfaceImplementations(symbol))
                yield return explicitImplementation;

            if (symbol.ContainingType is not { } containingType)
                yield break;

            // An implicit implementation has no syntactic link to the interface, so the containing type's
            // interfaces are searched for members this symbol satisfies.
            foreach (INamedTypeSymbol interfaceType in containingType.AllInterfaces)
            {
                foreach (ISymbol interfaceMember in interfaceType.GetMembers())
                {
                    if (interfaceMember.Kind == symbol.Kind
                        && interfaceMember.Name == symbol.Name
                        && SymbolEqualityComparer.Default.Equals(
                            containingType.FindImplementationForInterfaceMember(interfaceMember),
                            symbol))
                    {
                        yield return interfaceMember;
                    }
                }
            }
        }

        private static ISymbol? GetOverriddenMember(ISymbol symbol) =>
            symbol switch
            {
                IMethodSymbol method => method.OverriddenMethod,
                IPropertySymbol property => property.OverriddenProperty,
                IEventSymbol @event => @event.OverriddenEvent,
                _ => null,
            };

        private static ImmutableArray<ISymbol> GetExplicitInterfaceImplementations(ISymbol symbol) =>
            symbol switch
            {
                IMethodSymbol method => ImmutableArray<ISymbol>.CastUp(method.ExplicitInterfaceImplementations),
                IPropertySymbol property => ImmutableArray<ISymbol>.CastUp(property.ExplicitInterfaceImplementations),
                IEventSymbol @event => ImmutableArray<ISymbol>.CastUp(@event.ExplicitInterfaceImplementations),
                _ => ImmutableArray<ISymbol>.Empty,
            };
    }
}
#endif
