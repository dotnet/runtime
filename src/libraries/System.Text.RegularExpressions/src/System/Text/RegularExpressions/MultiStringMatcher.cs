// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// Efficiently finds a set of strings in text.
    /// Currently uses the Aho-Corasick algorithm.
    /// </summary>
    internal sealed class MultiStringMatcher
    {
        private readonly TrieNodeWithLinks[] _trie;

        internal ReadOnlySpan<TrieNodeWithLinks> Trie => _trie;

        /// <summary>
        /// Creates a <see cref="MultiStringMatcher"/> from a trie, and
        /// calculates its nodes' suffix links and match lengths.
        /// </summary>
        internal MultiStringMatcher(List<TrieNode> trie)
        {
            _trie = BuildTrieLinks(trie);
        }

        private static TrieNodeWithLinks[] BuildTrieLinks(List<TrieNode> trie)
        {
            TrieNodeWithLinks[] result = new TrieNodeWithLinks[trie.Count];

            Queue<int> vertexQueue = new Queue<int>();
            vertexQueue.Enqueue(TrieNode.Root);

            while (vertexQueue.Count > 0)
            {
                int currentVertex = vertexQueue.Dequeue();
                TrieNode node = trie[currentVertex];
                int suffixLink;
                int matchLength;

                if (node.IsRoot)
                {
                    suffixLink = TrieNode.Root;
                    matchLength = -1;
                }
                else if (node.Parent == TrieNode.Root)
                {
                    suffixLink = TrieNode.Root;
                    matchLength = node.IsMatch ? node.Depth : result[suffixLink].MatchLength;
                }
                else
                {
                    // To calculate the suffix link for the current vertex, we need the suffix
                    // link for the parent and the character that moved us to the current vertex.
                    int curBetterVertex = result[node.Parent].SuffixLink;
                    char chVertex = node.AccessingCharacter;
                    while (true)
                    {
                        // If there is an edge with the needed char, update the suffix link
                        // and leave the cycle
                        if (trie[curBetterVertex].Children.TryGetValue(chVertex, out suffixLink))
                        {
                            break;
                        }

                        // Jump by suffix links until we reach the root or find a better prefix for the current substring.
                        if (curBetterVertex == TrieNode.Root)
                        {
                            suffixLink = TrieNode.Root;
                            break;
                        }

                        // Go up by suffixlink
                        curBetterVertex = result[curBetterVertex].SuffixLink;
                    }

                    matchLength = node.IsMatch ? node.Depth : result[suffixLink].MatchLength;
                }

                result[currentVertex] = new TrieNodeWithLinks()
                {
#if DEBUG || REGEXGENERATOR
                    Path = node.Path,
#endif
                    Children = node.Children,
                    SuffixLink = suffixLink,
                    MatchLength = matchLength
                };

                foreach (KeyValuePair<char, int> vertex in node.Children)
                {
                    vertexQueue.Enqueue(vertex.Value);
                }
            }

            return result;
        }

        internal int GetMatchCount()
        {
            int count = 0;
            foreach (ref readonly TrieNodeWithLinks node in Trie)
            {
                if (node.MatchLength != -1)
                {
                    count++;
                }
            }
            return count;
        }

        public int Find(ReadOnlySpan<char> text)
        {
            int currentState = TrieNode.Root;
            ReadOnlySpan<TrieNodeWithLinks> trie = Trie;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                while (true)
                {
                    if (trie[currentState].Children.TryGetValue(c, out int nextState))
                    {
                        currentState = nextState;
                        break;
                    }

                    if (currentState == TrieNode.Root)
                    {
                        break;
                    }

                    currentState = trie[currentState].SuffixLink;
                }

                int matchLength = trie[currentState].MatchLength;

                if (matchLength != -1)
                {
                    // Found a match. We calculate the beginning of it and return it.
                    return i + 1 - matchLength;
                }
            }

            return -1;
        }
    }

    /// <summary>
    /// A trie node with the bare minimum fields that are needed for matching text with the Aho-Corasick algorithm.
    /// </summary>
    internal readonly struct TrieNodeWithLinks
    {
#if DEBUG || REGEXGENERATOR
        // We need it for easier inspection while debugging, and for the source generator's comments.
        public string Path { get; init; }
#endif
        public Dictionary<char, int> Children { get; init; }
        public int SuffixLink { get; init; }
        // Since we don't care about which string we matched, we use the length
        // of the match that occurs in this node, or inherited by its suffix link.
        public int MatchLength { get; init; }
    }
}
