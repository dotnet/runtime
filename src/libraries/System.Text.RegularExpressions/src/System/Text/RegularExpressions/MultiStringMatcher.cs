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
        internal readonly List<TrieNode> Trie;

        /// <summary>
        /// Creates a <see cref="MultiStringMatcher"/> from a trie, and
        /// calculates its nodes' suffix and dictionary links.
        /// </summary>
        /// <param name="trie"></param>
        internal MultiStringMatcher(List<TrieNode> trie)
        {
            BuildTrieLinks(trie);

            Trie = trie;
        }

        private static void BuildTrieLinks(List<TrieNode> trie)
        {
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
                CalculateSuffixAndDictionaryLinks(trie, currentVertex);

                foreach (KeyValuePair<char, int> vertex in trie[currentVertex].Children)
                {
                    vertexQueue.Enqueue(vertex.Value);
                }
            }
        }

        private static void CalculateSuffixAndDictionaryLinks(List<TrieNode> trie, int vertex)
        {
            TrieNode node = trie[vertex];
            if (vertex == TrieNode.Root)
            {
                node.SuffixLink = TrieNode.Root;
                return;
            }

            // one character substrings
            if (node.Parent == TrieNode.Root)
            {
                node.SuffixLink = TrieNode.Root;
                node.MatchLength = node.IsMatch ? node.Depth : trie[node.SuffixLink].MatchLength;
                return;
            }

            // To calculate the suffix link for the current vertex, we need the suffix
            // link for the parent and the character that moved us to the
            // current vertex.
            int curBetterVertex = trie[node.Parent].SuffixLink;
            char chVertex = node.AccessingCharacter;
            while (true)
            {
                // If there is an edge with the needed char, update the suffix link
                // and leave the cycle
                if (trie[curBetterVertex].Children.TryGetValue(chVertex, out int suffixLink))
                {
                    node.SuffixLink = suffixLink;
                    break;
                }
                // Jump by suffix links until we reach the root or find a better prefix for the current substring.
                if (curBetterVertex == TrieNode.Root)
                {
                    node.SuffixLink = TrieNode.Root;
                    break;
                }
                // Go up by suffixlink
                curBetterVertex = trie[curBetterVertex].SuffixLink;
            }

            node.MatchLength = node.IsMatch ? node.Depth : trie[node.SuffixLink].MatchLength;
        }

        public int Find(ReadOnlySpan<char> text)
        {
            int lastMatch = -1;
            int currentState = TrieNode.Root;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                while (true)
                {
                    if (Trie[currentState].Children.TryGetValue(c, out int nextState))
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

                    currentState = Trie[currentState].SuffixLink;
                }

                int matchLength = Trie[currentState].MatchLength;

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
}
