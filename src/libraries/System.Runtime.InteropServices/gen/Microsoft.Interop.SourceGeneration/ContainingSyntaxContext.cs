// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public readonly record struct ContainingTypeInfo(SyntaxTokenList Modifiers, SyntaxKind TypeKind, SyntaxToken Identifier, TypeParameterListSyntax? TypeParameters)
    {
        public bool Equals(ContainingTypeInfo other)
        {
            return Modifiers.SequenceEqual(other.Modifiers, SyntaxEquivalentComparer.Instance)
                && TypeKind == other.TypeKind
                && Identifier.IsEquivalentTo(other.Identifier)
                && SyntaxEquivalentComparer.Instance.Equals(TypeParameters, other.TypeParameters);
        }

        public override int GetHashCode() => throw new UnreachableException();
    }

    public sealed record ContainingSyntaxContext(ImmutableArray<ContainingTypeInfo> ContainingTypes, string? ContainingNamespace)
    {
        public ContainingSyntaxContext(MemberDeclarationSyntax memberDeclaration)
            : this(GetContainingTypes(memberDeclaration), GetContainingNamespace(memberDeclaration))
        {
        }

        private static ImmutableArray<ContainingTypeInfo> GetContainingTypes(MemberDeclarationSyntax memberDeclaration)
        {
            ImmutableArray<ContainingTypeInfo>.Builder containingTypeInfoBuilder = ImmutableArray.CreateBuilder<ContainingTypeInfo>();
            for (SyntaxNode? parent = memberDeclaration.Parent; parent is TypeDeclarationSyntax typeDeclaration; parent = parent.Parent)
            {

                containingTypeInfoBuilder.Add(new ContainingTypeInfo(StripTriviaFromModifiers(typeDeclaration.Modifiers), typeDeclaration.Kind(), typeDeclaration.Identifier.WithoutTrivia(),
                    typeDeclaration.TypeParameterList));
            }

            return containingTypeInfoBuilder.ToImmutable();

            static SyntaxTokenList StripTriviaFromModifiers(SyntaxTokenList tokenList)
            {
                SyntaxToken[] strippedTokens = new SyntaxToken[tokenList.Count];
                for (int i = 0; i < tokenList.Count; i++)
                {
                    strippedTokens[i] = tokenList[i].WithoutTrivia();
                }
                return new SyntaxTokenList(strippedTokens);
            }
        }

        private static string GetContainingNamespace(MemberDeclarationSyntax memberDeclaration)
        {
            StringBuilder? containingNamespace = null;
            for (SyntaxNode? parent = memberDeclaration.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>(); parent is BaseNamespaceDeclarationSyntax ns; parent = parent.Parent)
            {
                if (containingNamespace is null)
                {
                    containingNamespace = new StringBuilder(ns.Name.ToString());
                }
                else
                {
                    string namespaceName = ns.Name.ToString();
                    containingNamespace.Insert(0, namespaceName + ".");
                }
            }

            return containingNamespace?.ToString();
        }

        public bool Equals(ContainingSyntaxContext other)
        {
            return ContainingTypes.SequenceEqual(other.ContainingTypes)
                && ContainingNamespace == other.ContainingNamespace;
        }

        public override int GetHashCode() => throw new UnreachableException();

        public MemberDeclarationSyntax WrapMemberInContainingSyntaxWithUnsafeModifier(MemberDeclarationSyntax member)
        {
            bool addedUnsafe = false;
            MemberDeclarationSyntax wrappedMember = member;
            foreach (var containingType in ContainingTypes)
            {
                TypeDeclarationSyntax type = TypeDeclaration(containingType.TypeKind, containingType.Identifier)
                    .WithModifiers(containingType.Modifiers)
                    .AddMembers(wrappedMember);
                if (!addedUnsafe)
                {
                    type = type.WithModifiers(type.Modifiers.AddToModifiers(SyntaxKind.UnsafeKeyword));
                }
                if (containingType.TypeParameters is not null)
                {
                    type = type.AddTypeParameterListParameters(containingType.TypeParameters.Parameters.ToArray());
                }
                wrappedMember = type;
            }
            if (ContainingNamespace is not null)
            {
                wrappedMember = NamespaceDeclaration(ParseName(ContainingNamespace)).AddMembers(wrappedMember);
            }
            return wrappedMember;
        }
    }
}
