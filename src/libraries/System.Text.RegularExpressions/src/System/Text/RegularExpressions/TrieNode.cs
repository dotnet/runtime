// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Text.RegularExpressions
{
    internal struct Trie
    {
        private readonly List<TrieNode> _nodes = new List<TrieNode>() { TrieNode.CreateRoot() };

        public Trie() { }

        public TrieNode this[int index] => _nodes[index];

        public int Add(int nodeIndex, char c, bool isMatch)
        {
            TrieNode node = this[nodeIndex];
            if (!node.Children.TryGetValue(c, out int nextNodeIndex))
            {
                nextNodeIndex = _nodes.Count;

                TrieNode newNode = new TrieNode()
                {
                    Parent = nodeIndex,
                    AccessingCharacter = c,
                    Depth = node.Depth + 1,
                    IsMatch = isMatch,
#if DEBUG
                    Path = node.Path + c
#endif
                };
            }
            else
            {
                this[nextNodeIndex].IsMatch |= isMatch;
            }
            return nextNodeIndex;
        }

        public int Add(int nodeIndex, string s, bool isMatch)
        {
            for (int i = 0; i < s.Length; i++)
            {
                nodeIndex = Add(nodeIndex, s[i], i == s.Length - 1 && isMatch);
            }
            return nodeIndex;
        }
    }

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

        public Dictionary<char, int> Children = new();

        public bool IsMatch;

        public int Parent { get; init; }

        public char AccessingCharacter { get; init; }

        public int Depth { get; init; }

        public int SuffixLink = -1;

        public int DictionaryLink = -1;

#if DEBUG
        public string? Path { get; init; }
        public string? SuffixLinkPath;
        public string? DictionaryLinkPath;

        public override string ToString() =>
            $"Path: {Path} Suffix Link: {SuffixLinkPath} Dictionary Link: {DictionaryLinkPath}";
#endif
    }
}
