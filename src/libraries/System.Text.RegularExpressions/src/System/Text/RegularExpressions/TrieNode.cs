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

        private bool _isMatch;

        public bool IsMatch
        {
            get => _isMatch;
            set
            {
                if (Parent != -1)
                {
                    _isMatch = value;
                }
            }
        }

        public int Parent { get; init; }

        public char AccessingCharacter { get; init; }

        public int Depth { get; init; }

        public int SuffixLink = -1;

        public int DictionaryLink = -1;

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
            Debug.Assert(path == Path);
            return path;
        }

#if DEBUG
        public string? Path { get; init; }
        public string? SuffixLinkPath;
        public string? DictionaryLinkPath;

        public override string ToString() =>
            $"Path: {Path} Suffix Link: {SuffixLinkPath} Dictionary Link: {DictionaryLinkPath}";
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

        public static string GetCommonPrefix(this List<TrieNode> trie)
        {
            TrieNode currentNode = trie[TrieNode.Root];
            while (currentNode.Children is { Count: 1 } dict)
            {
                foreach (KeyValuePair<char, int> kvp in dict)
                {
                    currentNode = trie[kvp.Value];
                    break;
                }
            }
            return currentNode.GetPath(trie);
        }
    }
}
