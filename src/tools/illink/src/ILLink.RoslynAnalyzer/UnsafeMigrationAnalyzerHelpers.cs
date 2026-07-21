// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ILLink.RoslynAnalyzer
{
    /// <summary>
    /// Provides shared declaration, signature, documentation, and layout analysis for the unsafe-v2 migration tooling.
    /// The helpers encode the compatibility rules used by the <c>IL5005</c> and <c>IL5006</c> analyzers.
    /// </summary>
    internal static class UnsafeMigrationAnalyzerHelpers
    {
        // The analyzer builds against a Roslyn version that predates SyntaxKind.SafeKeyword.
        private static readonly SyntaxKind s_safeKeyword = SyntaxFacts.GetContextualKeywordKind("safe");

        internal static SyntaxTokenList GetModifiers(SyntaxNode declaration) =>
            declaration switch
            {
                MemberDeclarationSyntax member => member.Modifiers,
                LocalFunctionStatementSyntax localFunction => localFunction.Modifiers,
                AccessorDeclarationSyntax accessor => accessor.Modifiers,
                _ => default,
            };

        internal static bool HasModifier(SyntaxNode declaration, SyntaxKind modifier) =>
            GetModifiers(declaration).Any(modifier);

        internal static bool HasSafeModifier(SyntaxNode declaration) =>
            s_safeKeyword != SyntaxKind.None && GetModifiers(declaration).Any(s_safeKeyword);

        internal static SyntaxToken GetModifier(SyntaxNode declaration, SyntaxKind modifier)
        {
            foreach (SyntaxToken token in GetModifiers(declaration))
            {
                if (token.IsKind(modifier))
                    return token;
            }

            return default;
        }

        /// <summary>
        /// Gets source declarations for a symbol, normalizing field-like variable declarators to their shared declaration.
        /// </summary>
        internal static IEnumerable<SyntaxNode> GetDeclarations(ISymbol symbol, CancellationToken cancellationToken)
        {
            foreach (SyntaxReference reference in symbol.DeclaringSyntaxReferences)
            {
                SyntaxNode declaration = reference.GetSyntax(cancellationToken);
                if (declaration is VariableDeclaratorSyntax variable
                    && variable.Parent?.Parent is BaseFieldDeclarationSyntax field)
                {
                    if (field.Declaration.Variables[0] != variable)
                        continue;

                    declaration = field;
                }

                yield return declaration;
            }
        }

        /// <summary>
        /// Filters out declarations such as static constructors and destructors that cannot expose caller-unsafe contracts.
        /// </summary>
        internal static bool IsUnsafeContractMember(ISymbol symbol) =>
            symbol switch
            {
                IMethodSymbol { MethodKind: MethodKind.StaticConstructor or MethodKind.Destructor } => false,
                IMethodSymbol => true,
                IFieldSymbol { IsConst: false } => true,
                IPropertySymbol or IEventSymbol => true,
                _ => false,
            };

        /// <summary>
        /// Implements the legacy pointer-signature compatibility test used by both migration analyzers.
        /// </summary>
        internal static bool HasPointerOrFunctionPointerSignature(ISymbol symbol) =>
            // Match Roslyn's compatibility rule: only member signature types participate, not constraints.
            symbol switch
            {
                IMethodSymbol method => ContainsPointerOrFunctionPointer(method.ReturnType)
                    || method.Parameters.Any(static parameter => ContainsPointerOrFunctionPointer(parameter.Type)),
                IPropertySymbol property => ContainsPointerOrFunctionPointer(property.Type)
                    || property.Parameters.Any(static parameter => ContainsPointerOrFunctionPointer(parameter.Type)),
                IFieldSymbol { IsFixedSizeBuffer: false } field => ContainsPointerOrFunctionPointer(field.Type),
                IEventSymbol @event => ContainsPointerOrFunctionPointer(@event.Type),
                _ => false,
            };

        /// <summary>
        /// Finds safety documentation on this declaration, another partial part, or an associated property.
        /// </summary>
        internal static bool HasSafetyDocumentation(
            SyntaxNode declaration,
            ISymbol symbol,
            CancellationToken cancellationToken)
        {
            if (HasSafetyDocumentation(declaration))
                return true;

            // Documentation on another partial declaration or on an associated property also describes this contract.
            return GetDocumentationSymbols(symbol)
                .SelectMany(static relatedSymbol => relatedSymbol.DeclaringSyntaxReferences)
                .Select(reference => reference.GetSyntax(cancellationToken))
                .Any(HasSafetyDocumentation);
        }

        /// <summary>
        /// Determines whether removing unsafe must be suppressed to avoid recreating CS9392.
        /// </summary>
        internal static bool RequiresExplicitSafetyModifier(SyntaxNode declaration, ISymbol symbol)
        {
            if (declaration is AccessorDeclarationSyntax || symbol.ContainingType is not { } containingType)
                return false;

            // Removing unsafe from a field-backed declaration in these layouts would immediately reintroduce CS9392.
            bool hasInstanceField = symbol switch
            {
                IFieldSymbol { IsStatic: false, IsConst: false } => true,
                IPropertySymbol property => HasInstanceBackingField(property),
                IEventSymbol { IsStatic: false } when declaration is EventFieldDeclarationSyntax => true,
                IEventSymbol @event => HasInstanceBackingField(@event),
                _ => false,
            };

            return hasInstanceField && HasExplicitOrExtendedLayout(containingType);
        }

        internal static Location GetIdentifierLocation(SyntaxNode declaration) =>
            declaration switch
            {
                MethodDeclarationSyntax method => method.Identifier.GetLocation(),
                ConstructorDeclarationSyntax constructor => constructor.Identifier.GetLocation(),
                OperatorDeclarationSyntax @operator => @operator.OperatorToken.GetLocation(),
                ConversionOperatorDeclarationSyntax conversion => conversion.Type.GetLocation(),
                LocalFunctionStatementSyntax localFunction => localFunction.Identifier.GetLocation(),
                PropertyDeclarationSyntax property => property.Identifier.GetLocation(),
                IndexerDeclarationSyntax indexer => indexer.ThisKeyword.GetLocation(),
                AccessorDeclarationSyntax accessor => accessor.Keyword.GetLocation(),
                EventDeclarationSyntax @event => @event.Identifier.GetLocation(),
                BaseFieldDeclarationSyntax field when field.Declaration.Variables.FirstOrDefault() is { } variable => variable.Identifier.GetLocation(),
                _ => declaration.GetFirstToken().GetLocation(),
            };

        private static bool ContainsPointerOrFunctionPointer(ITypeSymbol type)
        {
            var types = new Stack<ITypeSymbol>();
            types.Push(type);

            while (types.Count > 0)
            {
                switch (types.Pop())
                {
                    case IPointerTypeSymbol or IFunctionPointerTypeSymbol:
                        return true;

                    case IArrayTypeSymbol array:
                        types.Push(array.ElementType);
                        break;

                    case INamedTypeSymbol namedType:
                        // Nested types can carry pointer-containing arguments on an enclosing type.
                        if (namedType.ContainingType is { } containingType)
                            types.Push(containingType);

                        foreach (ITypeSymbol typeArgument in namedType.TypeArguments)
                            types.Push(typeArgument);
                        break;
                }
            }

            return false;
        }

        private static IEnumerable<ISymbol> GetDocumentationSymbols(ISymbol symbol)
        {
            yield return symbol;

            // Partial declarations share one contract, and accessor contracts may be documented on the property.
            switch (symbol)
            {
                case IMethodSymbol method:
                    if (method.PartialDefinitionPart is { } methodDefinition)
                        yield return methodDefinition;
                    if (method.PartialImplementationPart is { } methodImplementation)
                        yield return methodImplementation;
                    if (method.AssociatedSymbol is { } associatedSymbol)
                    {
                        foreach (ISymbol relatedSymbol in GetDocumentationSymbols(associatedSymbol))
                            yield return relatedSymbol;
                    }
                    break;

                case IPropertySymbol property:
                    if (property.PartialDefinitionPart is { } propertyDefinition)
                        yield return propertyDefinition;
                    if (property.PartialImplementationPart is { } propertyImplementation)
                        yield return propertyImplementation;
                    break;

                case IEventSymbol @event:
                    if (@event.PartialDefinitionPart is { } eventDefinition)
                        yield return eventDefinition;
                    if (@event.PartialImplementationPart is { } eventImplementation)
                        yield return eventImplementation;
                    break;
            }
        }

        private static bool HasSafetyDocumentation(SyntaxNode declaration) =>
            declaration.GetLeadingTrivia().Any(static trivia =>
                trivia.GetStructure() is DocumentationCommentTriviaSyntax documentationComment
                && documentationComment.DescendantNodes().Any(static node =>
                    node is XmlElementSyntax { StartTag.Name.LocalName.ValueText: "safety" }
                        or XmlEmptyElementSyntax { Name.LocalName.ValueText: "safety" }));

        private static bool HasInstanceBackingField(ISymbol associatedSymbol) =>
            associatedSymbol.ContainingType.GetMembers()
                .OfType<IFieldSymbol>()
                .Any(field => !field.IsStatic
                    && SymbolEqualityComparer.Default.Equals(field.AssociatedSymbol, associatedSymbol));

        private static bool HasExplicitOrExtendedLayout(INamedTypeSymbol type) =>
            type.GetAttributes().Any(static attribute =>
                attribute.AttributeClass is { } attributeType
                && (attributeType.IsTypeOf("System.Runtime.InteropServices", "ExtendedLayoutAttribute")
                    || (attributeType.IsTypeOf("System.Runtime.InteropServices", nameof(StructLayoutAttribute))
                        && attribute.ConstructorArguments is [{ Value: int layoutKind }]
                        && layoutKind == (int)LayoutKind.Explicit)));
    }
}
#endif
