// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.RoslynAnalyzer
{
    /// <summary>
    /// Provides source and symbol helpers shared by unsafe-contract analyzers and code fixes.
    /// </summary>
    internal static class UnsafeContractHelpers
    {
        internal static ISymbol NormalizeContractSymbol(ISymbol symbol) =>
            symbol is IMethodSymbol
            {
                MethodKind: MethodKind.EventAdd or MethodKind.EventRemove,
                AssociatedSymbol: IEventSymbol @event,
            }
                ? @event
                : symbol;

        internal static IEnumerable<ISymbol> GetPartialParts(ISymbol symbol)
        {
            symbol = NormalizeContractSymbol(symbol);
            var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

            foreach (ISymbol part in GetPartialPartsCore(symbol))
            {
                if (seen.Add(part))
                    yield return part;
            }
        }

        internal static IEnumerable<SyntaxNode> GetDeclarations(
            ISymbol symbol,
            CancellationToken cancellationToken)
        {
            var seen = new HashSet<(SyntaxTree Tree, TextSpan Span)>();
            foreach (ISymbol part in GetPartialParts(symbol))
            {
                foreach (SyntaxReference reference in part.DeclaringSyntaxReferences)
                {
                    SyntaxNode declaration = GetDeclarationForSymbol(
                        part,
                        reference.GetSyntax(cancellationToken));
                    if (seen.Add((declaration.SyntaxTree, declaration.Span)))
                        yield return declaration;
                }
            }
        }

        internal static IEnumerable<SyntaxNode> GetDirectDeclarations(
            ISymbol symbol,
            CancellationToken cancellationToken)
        {
            symbol = NormalizeContractSymbol(symbol);
            foreach (SyntaxReference reference in symbol.DeclaringSyntaxReferences)
            {
                yield return GetDeclarationForSymbol(
                    symbol,
                    reference.GetSyntax(cancellationToken));
            }
        }

        internal static ISymbol? GetOverriddenMember(ISymbol symbol) =>
            NormalizeContractSymbol(symbol) switch
            {
                IMethodSymbol method => method.OverriddenMethod,
                IPropertySymbol property => property.OverriddenProperty,
                IEventSymbol @event => @event.OverriddenEvent,
                _ => null,
            };

        internal static IEnumerable<ISymbol> GetImplementedInterfaceMembers(ISymbol symbol)
        {
            symbol = NormalizeContractSymbol(symbol);
            INamedTypeSymbol? containingType = symbol.ContainingType;
            if (containingType is null)
                yield break;

            var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            foreach (INamedTypeSymbol @interface in containingType.AllInterfaces)
            {
                foreach (ISymbol interfaceMember in GetInterfaceContractSlots(@interface))
                {
                    ISymbol? implementation = containingType.FindImplementationForInterfaceMember(interfaceMember);
                    if (implementation is not null
                        && ContractSymbolsMatch(implementation, symbol)
                        && seen.Add(interfaceMember))
                    {
                        yield return interfaceMember;
                    }
                }
            }
        }

        internal static IEnumerable<ISymbol> GetInterfaceContractSlots(INamedTypeSymbol @interface)
        {
            foreach (ISymbol member in @interface.GetMembers())
            {
                if (member is IMethodSymbol { AssociatedSymbol: not null })
                    continue;

                if (member is IPropertySymbol property)
                {
                    yield return property;
                    if (property.GetMethod is not null)
                        yield return property.GetMethod;
                    if (property.SetMethod is not null)
                        yield return property.SetMethod;
                    continue;
                }

                yield return member;
            }
        }

        private static IEnumerable<ISymbol> GetPartialPartsCore(ISymbol symbol)
        {
            yield return symbol;

            switch (symbol)
            {
                case IMethodSymbol { AssociatedSymbol: IPropertySymbol property } accessor:
                    foreach (IPropertySymbol propertyPart in GetPropertyParts(property))
                    {
                        IMethodSymbol? accessorPart = accessor.MethodKind == MethodKind.PropertyGet
                            ? propertyPart.GetMethod
                            : propertyPart.SetMethod;
                        if (accessorPart is not null)
                            yield return accessorPart;
                    }
                    break;

                case IMethodSymbol method:
                    if (method.PartialDefinitionPart is { } methodDefinition)
                        yield return methodDefinition;
                    if (method.PartialImplementationPart is { } methodImplementation)
                        yield return methodImplementation;
                    break;

                case IPropertySymbol property:
                    foreach (IPropertySymbol propertyPart in GetPropertyParts(property))
                        yield return propertyPart;
                    break;

                case IEventSymbol @event:
                    if (@event.PartialDefinitionPart is { } eventDefinition)
                        yield return eventDefinition;
                    if (@event.PartialImplementationPart is { } eventImplementation)
                        yield return eventImplementation;
                    break;
            }
        }

        private static IEnumerable<IPropertySymbol> GetPropertyParts(IPropertySymbol property)
        {
            yield return property;
            if (property.PartialDefinitionPart is { } propertyDefinition)
                yield return propertyDefinition;
            if (property.PartialImplementationPart is { } propertyImplementation)
                yield return propertyImplementation;
        }

        private static bool ContractSymbolsMatch(ISymbol left, ISymbol right)
        {
            foreach (ISymbol leftPart in GetPartialParts(NormalizeContractSymbol(left)))
            {
                foreach (ISymbol rightPart in GetPartialParts(NormalizeContractSymbol(right)))
                {
                    if (SymbolEqualityComparer.Default.Equals(leftPart, rightPart)
                        || SymbolEqualityComparer.Default.Equals(
                            leftPart.OriginalDefinition,
                            rightPart.OriginalDefinition))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static SyntaxNode FindDeclaration(SyntaxNode node) =>
            node.AncestorsAndSelf().FirstOrDefault(static ancestor =>
                ancestor is VariableDeclaratorSyntax
                    {
                        Parent.Parent: EventFieldDeclarationSyntax,
                    }
                    or BaseMethodDeclarationSyntax
                    or BasePropertyDeclarationSyntax
                    or EventDeclarationSyntax
                    or EventFieldDeclarationSyntax
                    or AccessorDeclarationSyntax)
                ?? node;

        private static SyntaxNode GetDeclarationForSymbol(ISymbol symbol, SyntaxNode syntax)
        {
            SyntaxNode declaration = FindDeclaration(syntax);
            if (symbol is IMethodSymbol { MethodKind: MethodKind.PropertyGet })
            {
                return declaration switch
                {
                    PropertyDeclarationSyntax { ExpressionBody: { } expressionBody } => expressionBody,
                    IndexerDeclarationSyntax { ExpressionBody: { } expressionBody } => expressionBody,
                    _ => declaration,
                };
            }

            return declaration;
        }
    }
}
#endif
