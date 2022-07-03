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
        /// calculates its nodes' suffix and dictionary links.
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
#if REGEXGENERATOR
            while (vertexQueue.Count > 0)
            {
                int currentVertex = vertexQueue.Dequeue();
#else
            while (vertexQueue.TryDequeue(out int currentVertex))
            {
#endif
                TrieNode node = trie[currentVertex];
                int suffixLink;
                int matchLength;

                if (node.IsRoot)
                {
                    suffixLink = TrieNode.Root;
                    matchLength = -1;
                    goto End;
                }

                // one character substrings
                if (node.Parent == TrieNode.Root)
                {
                    suffixLink = TrieNode.Root;
                    matchLength = node.IsMatch ? node.Depth : result[suffixLink].MatchLength;
                    goto End;
                }

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

            End:
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
            int lastMatch = -1;
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
                    // The algorithm effectively resets when we reach the root node.
                    // If we had found a match before, we return it.
                    if (currentState == TrieNode.Root)
                    {
                        if (lastMatch != -1)
                        {
                            return lastMatch;
                        }
                        break;
                    }

                    currentState = trie[currentState].SuffixLink;
                }

                int matchLength = trie[currentState].MatchLength;

                if (matchLength != -1)
                {
                    // Found a match. We mark it and continue searching hoping it is getting bigger.
                    int indexOfMatch = i + 1 - matchLength;

                    // We want to return the leftmost-longest match.
                    // If this match starts later than the match we might have found before,
                    // we cannot accept it because we want to return the leftmost match.
                    // The match can start at the same position as the previous one, which
                    // means that it is longer and we accept it.
                    // The unsigned integer comparison will also always
                    // succeed if the last match index is negative.
                    if ((uint)indexOfMatch <= (uint)lastMatch)
                    {
                        lastMatch = indexOfMatch;
                    }
                }
            }

            return lastMatch;
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
