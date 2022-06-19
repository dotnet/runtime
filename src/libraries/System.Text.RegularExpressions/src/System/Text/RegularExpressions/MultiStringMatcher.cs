// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Text.RegularExpressions
{
    internal sealed class TrieNode
    {
        public const int Root = 0;

        public TrieNode(int parent, string word, int wordId, int charIndex)
        {
            Parent = parent;
            AccessingCharacter = word[charIndex];
            WordId = charIndex == word.Length - 1 ? wordId : -1;
            Depth = charIndex + 1;
#if DEBUG
            Path = word.AsSpan(0, charIndex + 1).ToString();
#endif
        }

        public TrieNode()
        {
            Parent = -1;
            AccessingCharacter = '\0';
            WordId = -1;
            Depth = 0;
#if DEBUG
            Path = "<root>";
#endif
        }

        public Dictionary<char, int> Children = new();

        public bool IsMatch => WordId != -1;

        public readonly int Parent;

        public readonly char AccessingCharacter;

        public readonly int WordId = -1;

        public readonly int Depth;

        public int SuffixLink = -1;

        public int DictionaryLink = -1;

#if DEBUG
        public readonly string Path;
        public string? SuffixLinkPath;
        public string? DictionaryLinkPath;

        public override string ToString() =>
            $"Path: {Path} Suffix Link: {SuffixLinkPath} Dictionary Link: {DictionaryLinkPath}";
#endif
    }
    internal sealed class MultiStringMatcher
    {

        private readonly List<TrieNode> _trie;
        private readonly string[] _words;

        public ReadOnlySpan<string> Words => _words;

        public MultiStringMatcher(ReadOnlySpan<string> words)
        {
            _trie = BuildTrie(words);
            _words = words.ToArray();
        }

        private static List<TrieNode> BuildTrie(ReadOnlySpan<string> words)
        {
            List<TrieNode> trie = new() { new TrieNode() };

            for (int i = 0; i < words.Length; i++)
            {
                AddStringToTrie(trie, words[i], i);
            }

            // That gives us the assurance that no new trie nodes will be created.
            BuildTrieLinks(CollectionsMarshal.AsSpan(trie));

            return trie;
        }

        private static void AddStringToTrie(List<TrieNode> trie, string word, int wordID)
        {
            int currentVertex = TrieNode.Root;
            for (int i = 0; i < word.Length; i++)
            {
                TrieNode currentNode = trie[currentVertex];
                char c = word[i];
                if (!currentNode.Children.TryGetValue(c, out int nextVertex))
                {
                    TrieNode newNode = new TrieNode(currentVertex, word, wordID, i);
                    currentNode.Children[c] = nextVertex = trie.Count;
                    trie.Add(newNode);
                }
                currentVertex = nextVertex; // Move to the new vertex in the trie
            }
        }

        private static void BuildTrieLinks(ReadOnlySpan<TrieNode> trie)
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

        private static void CalculateSuffixAndDictionaryLinks(ReadOnlySpan<TrieNode> trie, int vertex)
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
                    int wordId = _trie[dictLink].WordId;
                    int indexOfMatch = i + 1 - _words[wordId].Length;

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
