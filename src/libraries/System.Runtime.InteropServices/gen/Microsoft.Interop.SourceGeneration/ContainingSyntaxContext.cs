// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public readonly struct ContainingSyntax(SyntaxTokenList modifiers, SyntaxKind typeKind, SyntaxToken identifier, TypeParameterListSyntax? typeParameters) : IEquatable<ContainingSyntax>
    {
        public SyntaxTokenList Modifiers { get; init; } = modifiers.StripTriviaFromTokens();

        public SyntaxToken Identifier { get; init; } = identifier.WithoutTrivia();

        public SyntaxKind TypeKind { get; init; } = typeKind;

        public TypeParameterListSyntax? TypeParameters { get; init; } = typeParameters;

        public override bool Equals(object obj) => obj is ContainingSyntax other && Equals(other);

        public bool Equals(ContainingSyntax other)
        {
            return Modifiers.SequenceEqual(other.Modifiers, SyntaxEquivalentComparer.Instance)
                && TypeKind == other.TypeKind
                && Identifier.IsEquivalentTo(other.Identifier)
                && SyntaxEquivalentComparer.Instance.Equals(TypeParameters, other.TypeParameters);
        }

        public override int GetHashCode() => throw new UnreachableException();
    }

    public sealed record ContainingSyntaxContext(ImmutableArray<ContainingSyntax> ContainingSyntax, string? ContainingNamespace)
    {
        public ContainingSyntaxContext(MemberDeclarationSyntax memberDeclaration)
            : this(GetContainingTypes(memberDeclaration), GetContainingNamespace(memberDeclaration))
        {
        }

        public ContainingSyntaxContext AddContainingSyntax(ContainingSyntax nestedType)
        {
            return this with { ContainingSyntax = ContainingSyntax.Insert(0, nestedType) };
        }

        private static ImmutableArray<ContainingSyntax> GetContainingTypes(MemberDeclarationSyntax memberDeclaration)
        {
            ImmutableArray<ContainingSyntax>.Builder containingTypeInfoBuilder = ImmutableArray.CreateBuilder<ContainingSyntax>();
            for (SyntaxNode? parent = memberDeclaration.Parent; parent is TypeDeclarationSyntax typeDeclaration; parent = parent.Parent)
            {
                containingTypeInfoBuilder.Add(
                    new ContainingSyntax(
                        typeDeclaration.Modifiers,
                        typeDeclaration.Kind(),
                        typeDeclaration.Identifier,
                        typeDeclaration.TypeParameterList));
            }

            return containingTypeInfoBuilder.ToImmutable();
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
            return ContainingSyntax.SequenceEqual(other.ContainingSyntax)
                && ContainingNamespace == other.ContainingNamespace;
        }

        public override int GetHashCode()
        {
            int code = ContainingNamespace?.GetHashCode() ?? 0;
            foreach (ContainingSyntax containingSyntax in ContainingSyntax)
            {
                code = HashCode.Combine(code, containingSyntax.Identifier.Value);
            }
            return code;
        }

        /// <summary>
        /// Wraps <paramref name="member"/> in its containing types and namespace.
        /// </summary>
        public MemberDeclarationSyntax WrapMemberInContainingSyntax(MemberDeclarationSyntax member)
        {
            MemberDeclarationSyntax wrappedMember = member;
            foreach (var containingType in ContainingSyntax)
            {
                TypeDeclarationSyntax type = TypeDeclaration(containingType.TypeKind, containingType.Identifier)
                    .WithModifiers(containingType.Modifiers)
                    .AddMembers(wrappedMember);
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

        /// <summary>
        /// Wraps <paramref name="members"/> in their containing types and namespace, reproducing the containing
        /// types with the modifiers the user wrote and nothing more.
        /// </summary>
        /// <remarks>
        /// Suitable when every generated member that names a pointer type carries its own <c>unsafe</c> modifier
        /// or sits inside an <c>unsafe</c> block, which makes the containing type's modifier unnecessary under
        /// the legacy rules and meaningless under the updated ones.
        /// </remarks>
        public MemberDeclarationSyntax WrapMembersInContainingSyntax(params MemberDeclarationSyntax[] members)
            => WrapMembersInContainingSyntaxWithUnsafeModifier(useUpdatedMemorySafetyRules: true, members);

        /// <summary>
        /// Wraps <paramref name="members"/> in their containing types and namespace, adding an <c>unsafe</c>
        /// modifier to the containing types unless <paramref name="useUpdatedMemorySafetyRules"/>.
        /// </summary>
        /// <remarks>
        /// Under the legacy rules the modifier is what makes a pointer type legal to name in a generated member
        /// whose own modifiers are copied from a user declaration that carries <c>unsafe</c> elsewhere. Under the
        /// updated rules a pointer type needs no unsafe context to be named and the modifier has no effect at
        /// all, so emitting it would only produce <c>CS9377</c> in code the user cannot edit.
        /// </remarks>
        public MemberDeclarationSyntax WrapMembersInContainingSyntaxWithUnsafeModifier(bool useUpdatedMemorySafetyRules, params MemberDeclarationSyntax[] members)
        {
            MemberDeclarationSyntax? wrappedMember = null;
            foreach (var containingType in ContainingSyntax)
            {
                TypeDeclarationSyntax type = TypeDeclaration(containingType.TypeKind, containingType.Identifier)
                    .WithModifiers(containingType.Modifiers)
                    .AddMembers(wrappedMember is not null ? new[] { wrappedMember } : members);
                if (!useUpdatedMemorySafetyRules)
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

        /// <inheritdoc cref="WrapMembersInContainingSyntaxWithUnsafeModifier" path="/remarks"/>
        public void WriteToWithUnsafeModifier<TState>(bool useUpdatedMemorySafetyRules, IndentedTextWriter writer, TState writeMembersState, Action<IndentedTextWriter, TState> writeMembers)
        {
            if (ContainingNamespace is not null)
            {
                writer.WriteLine($"namespace {ContainingNamespace}");
                writer.WriteLine('{');
                writer.Indent++;
            }

            // When creating syntax we walk from most nested type to least nested and then enclose this chain in a namespace.
            // With string writing things are exactly opposite: we are starting with a namespace and then print headers of types
            // from least nested to most nested one. Since syntax model was the original one we have containing syntaxes stored as
            // most convenient for it, so for string writing we have to walk them in the reverse order. When we eventually port
            // our source generation to string writing we should reverse the order of elements for the convenience of that model instead.
            for (int i = ContainingSyntax.Length - 1; i >= 0; i--)
            {
                ContainingSyntax syntax = ContainingSyntax[i];

                SyntaxTokenList modifiers = useUpdatedMemorySafetyRules
                    ? syntax.Modifiers
                    : syntax.Modifiers.AddToModifiers(SyntaxKind.UnsafeKeyword);
                writer.WriteLine($"{string.Join(" ", modifiers)} {syntax.TypeKind.GetDeclarationKeyword()} {syntax.Identifier}{syntax.TypeParameters}");
                writer.WriteLine('{');
                writer.Indent++;
            }

            writeMembers(writer, writeMembersState);

            for (int i = 0; i < ContainingSyntax.Length; i++)
            {
                writer.Indent--;
                writer.WriteLine('}');
            }

            if (ContainingNamespace is not null)
            {
                writer.Indent--;
                writer.WriteLine('}');
            }
        }
    }
}
