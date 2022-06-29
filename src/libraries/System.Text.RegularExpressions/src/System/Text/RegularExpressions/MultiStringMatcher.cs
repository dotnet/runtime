// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Text.RegularExpressions
{
    internal sealed class MultiStringMatcher
    {
        private readonly Trie _trie;

        public MultiStringMatcher(ReadOnlySpan<string> words)
        {
            _trie = BuildTrie(words);
        }

        private static Trie BuildTrie(ReadOnlySpan<string> words)
        {
            Trie trie = new Trie(words);

            BuildTrieLinks(trie);

            return trie;
        }

        private static void BuildTrieLinks(Trie trie)
        {
            Queue<int> vertexQueue = new Queue<int>();
            vertexQueue.Enqueue(TrieNode.Root);
            while (vertexQueue.TryDequeue(out int currentVertex))
            {
                CalculateSuffixAndDictionaryLinks(trie, currentVertex);

                foreach (int vertex in trie[currentVertex].Children.Values)
                {
                    vertexQueue.Enqueue(vertex);
                }
            }
        }

        private static void CalculateSuffixAndDictionaryLinks(Trie trie, int vertex)
        {
            TrieNode node = trie[vertex];
            if (vertex == TrieNode.Root)
            {
                node.SuffixLink = TrieNode.Root;
                node.DictionaryLink = TrieNode.Root;
                goto End;
            }

            // one character substrings
            if (node.Parent == TrieNode.Root)
            {
                node.SuffixLink = TrieNode.Root;
                node.DictionaryLink = node.IsMatch ? vertex : trie[node.SuffixLink].DictionaryLink;
                goto End;
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

            node.DictionaryLink = node.IsMatch ? vertex : trie[node.SuffixLink].DictionaryLink;

        End:;
#if DEBUG
            node.SuffixLinkPath = trie[node.SuffixLink].Path;
            node.DictionaryLinkPath = trie[node.DictionaryLink].Path;
#endif
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
                    if (_trie[currentState].Children.TryGetValue(c, out int nextState))
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

                    currentState = _trie[currentState].SuffixLink;
                }

                int dictLink = _trie[currentState].DictionaryLink;

                if (dictLink != TrieNode.Root)
                {
                    // Found a match. We mark it and continue searching hoping it is getting bigger.
                    // The depth of the node we are in is the length of the word we found.
                    int indexOfMatch = i + 1 - _trie[dictLink].Depth;

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
