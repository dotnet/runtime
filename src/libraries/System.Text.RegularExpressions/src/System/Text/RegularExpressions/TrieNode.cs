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
#if DEBUG
                Path = ""
#endif
            };
        }

        public readonly Dictionary<char, int> Children = new();

        public bool IsMatch { get; set; }

        public int Parent { get; init; }

        public char AccessingCharacter { get; init; }

        public int Depth { get; init; }

        public int SuffixLink = -1;

        // Since we don't care about which string we matched, we use the length
        // of the match that occurs in this node, or inherited by its suffix link.
        public int MatchLength = -1;

        public string GetPath(List<TrieNode> trie)
        {
#if REGEXGENERATOR
            string path = StringExtensions.Create(Depth, (trie, this), (span, x) =>
#else
            string path = string.Create(Depth, (trie, this), (span, x) =>
#endif
            {
                TrieNode currentNode = x.Item2;
                for (int i = span.Length - 1; i >= 0; i--)
                {
                    span[i] = currentNode.AccessingCharacter;
                    currentNode = x.trie[currentNode.Parent];
                }
            });
#if DEBUG
            Debug.Assert(path == Path);
#endif
            return path;
        }

#if DEBUG
        public string? Path { get; init; }

        public int ChildCount => Children.Count;

        public override string? ToString() => Path;
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
