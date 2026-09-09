// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SourceGenerators;

namespace Microsoft.Interop;

/// <summary>Formats the names and literals used in generated C# source.</summary>
public static class CodeWriterHelpers
{
    /// <summary>Formats a quoted C# string literal.</summary>
    /// <param name="value">The value of the literal.</param>
    /// <returns>The escaped, quoted literal.</returns>
    public static string StringLiteral(string value) => Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(value, quote: true);

    /// <summary>Escapes identifiers that coincide with C# keywords.</summary>
    /// <param name="identifier">The identifier to escape.</param>
    /// <returns>The identifier as C# source text.</returns>
    public static string EscapeIdentifier(string identifier)
    {
        return SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None
            || SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None
            ? "@" + identifier
            : identifier;
    }

    /// <summary>Extracts modifier text from an input declaration.</summary>
    /// <param name="modifiers">The modifiers in the input declaration.</param>
    /// <returns>The modifier keywords without trivia.</returns>
    public static ImmutableArray<string> GetModifiers(SyntaxTokenList modifiers)
    {
        return ContainingTypeUtilities.GetModifiers(modifiers);
    }

    /// <summary>Adds a modifier before any trailing ref and partial keywords.</summary>
    /// <param name="modifiers">The existing modifiers.</param>
    /// <param name="modifier">The modifier to add.</param>
    /// <returns>The updated modifiers.</returns>
    public static ImmutableArray<string> AddModifier(ImmutableArray<string> modifiers, string modifier)
    {
        if (modifiers.Contains(modifier))
        {
            return modifiers;
        }

        int partialIndex = modifiers.IndexOf("partial");
        int refIndex = modifiers.IndexOf("ref");
        int index = (partialIndex, refIndex) switch
        {
            (-1, -1) => modifiers.Length,
            (-1, _) => refIndex,
            (_, -1) => partialIndex,
            _ => Math.Min(partialIndex, refIndex)
        };
        return modifiers.Insert(index, modifier);
    }
}
