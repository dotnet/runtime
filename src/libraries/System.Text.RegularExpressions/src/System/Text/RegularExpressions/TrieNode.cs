// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions
{
    internal sealed class TrieNode
    {
        public const int Root = 0;

        public static TrieNode CreateRoot()
        {
            return new TrieNode()
            {
                Parent = -1,
                AccessingCharacter = '\0',
                Depth = 0,
#if DEBUG || REGEXGENERATOR
                Path = ""
#endif
            };
        }

        public readonly Dictionary<char, int> Children = new();

        public bool IsMatch { get; set; }

        public bool IsRoot => Depth == 0;

        public int Parent { get; init; }

        public char AccessingCharacter { get; init; }

        public int Depth { get; init; }

#if DEBUG
        public int ChildCount => Children.Count;
#endif

#if DEBUG || REGEXGENERATOR
        public string Path { get; init; } = "<unset>";

        public override string ToString() => Path;
#endif
    }

    internal static class TrieExtensions
    {
        public static int GetMatchCount(this List<TrieNode> trie)
        {
            int matchCount = 0;
            foreach (TrieNode node in trie)
            {
                if (node.IsMatch)
                {
                    matchCount++;
                }
            }
            return matchCount;
        }
    }
}
