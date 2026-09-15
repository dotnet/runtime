// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceGenerators;

/// <summary>Extracts containing declarations during source-generator input analysis.</summary>
public static class ContainingTypeUtilities
{
    public static IEnumerable<TypeDeclarationSyntax> EnumerateContainingTypes(TypeDeclarationSyntax? innermostType)
    {
        for (TypeDeclarationSyntax? current = innermostType; current is not null; current = current.Parent as TypeDeclarationSyntax)
        {
            yield return current;
        }
    }

    public static ImmutableArray<string> GetModifiers(SyntaxTokenList modifiers)
        => modifiers.Select(static modifier => modifier.Text).ToImmutableArray();

    public static DeclarationHeader GetDeclarationHeader(TypeDeclarationSyntax declaration)
    {
        return new DeclarationHeader(
            GetModifiers(declaration.Modifiers),
            GetTypeKindKeyword(declaration),
            declaration.Identifier.Text,
            declaration.Identifier.Text + GetTypeParameters(declaration.TypeParameterList));
    }

    public static DeclarationHeader GetDeclarationHeader(MethodDeclarationSyntax declaration)
    {
        return new DeclarationHeader(
            GetModifiers(declaration.Modifiers),
            "",
            declaration.Identifier.Text,
            declaration.Identifier.Text + GetTypeParameters(declaration.TypeParameterList));
    }

    public static bool TryGetContainingTypeDeclarations(
        TypeDeclarationSyntax innermostType,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out List<string>? declarations)
    {
        declarations = null;
        foreach (TypeDeclarationSyntax current in EnumerateContainingTypes(innermostType))
        {
            if (!current.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                declarations = null;
                return false;
            }

            INamedTypeSymbol? typeSymbol = semanticModel.GetDeclaredSymbol(current, cancellationToken);
            Debug.Assert(typeSymbol is not null);
            var header = new DeclarationHeader(
                GetModifiers(current.Modifiers),
                GetTypeKindKeyword(current),
                current.Identifier.Text,
                typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            (declarations ??= new()).Add(header.ToString());
        }
        Debug.Assert(declarations?.Count > 0);
        return true;
    }

    private static string GetTypeKindKeyword(TypeDeclarationSyntax declaration) => declaration.Kind() switch
    {
        SyntaxKind.ClassDeclaration => "class",
        SyntaxKind.InterfaceDeclaration => "interface",
        SyntaxKind.StructDeclaration => "struct",
        SyntaxKind.RecordDeclaration => "record",
        SyntaxKind.RecordStructDeclaration => "record struct",
        _ => throw new InvalidOperationException("Unexpected containing declaration kind."),
    };

    private static string? GetTypeParameters(TypeParameterListSyntax? typeParameters)
    {
        if (typeParameters is null)
        {
            return null;
        }

        return "<" + string.Join(", ", typeParameters.Parameters.Select(static parameter =>
            (parameter.AttributeLists.Count == 0 ? "" : string.Join(" ", parameter.AttributeLists.Select(static attributes => attributes.ToString())) + " ")
            + (parameter.VarianceKeyword.RawKind == 0 ? "" : parameter.VarianceKeyword.Text + " ")
            + parameter.Identifier.Text)) + ">";
    }
}
