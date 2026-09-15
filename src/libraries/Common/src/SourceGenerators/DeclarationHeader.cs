// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;

namespace SourceGenerators;

/// <summary>Cacheable declaration text independent of Roslyn syntax nodes and symbols.</summary>
public readonly struct DeclarationHeader : IEquatable<DeclarationHeader>
{
    public DeclarationHeader(ImmutableArray<string> modifiers, string keyword, string identifier, string? name = null)
    {
        Modifiers = modifiers;
        Keyword = keyword;
        Identifier = identifier;
        Name = name ?? identifier;
    }

    public ImmutableArray<string> Modifiers { get; init; }
    public string Keyword { get; init; }
    public string Identifier { get; init; }

    /// <summary>The declaration name, including any generic parameters and their attributes.</summary>
    public string Name { get; init; }

    public bool Equals(DeclarationHeader other)
    {
        return Keyword == other.Keyword
            && Identifier == other.Identifier
            && Name == other.Name
            && Modifiers.SequenceEqual(other.Modifiers);
    }

    public override bool Equals(object? obj) => obj is DeclarationHeader other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = Keyword.GetHashCode();
            hash = hash * 31 + Identifier.GetHashCode();
            hash = hash * 31 + Name.GetHashCode();
            foreach (string modifier in Modifiers)
            {
                hash = hash * 31 + modifier.GetHashCode();
            }
            return hash;
        }
    }

    public override string ToString()
    {
        string modifiers = Modifiers.IsEmpty ? "" : string.Join(" ", Modifiers) + " ";
        string keyword = Keyword.Length == 0 ? "" : Keyword + " ";
        return modifiers + keyword + Name;
    }
}
