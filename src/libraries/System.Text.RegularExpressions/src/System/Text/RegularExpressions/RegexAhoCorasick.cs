// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The RegexAhoCorasick object once ready, runs the Aho-Corasick algorithm to find multiple string matches in the pattern efficiently.

using System.Collections.Generic;

namespace System.Text.RegularExpressions
{
    internal class TrieNode
    {
        public TrieNode()
        {
            Children = new Dictionary<char, int>();
            Leaf = false;
            Parent = -1;
            SuffixLink = -1;
            WordID = -1;
            DictionaryLink = -1;
        }

        public Dictionary<char, int> Children;

        public bool Leaf;

        public int Parent;

        public char ParentCharacter;

        public int SuffixLink;

        public int DictionaryLink;

        public int WordID;
    }

    internal class RegexAhoCorasick
    {
        private List<TrieNode> trie;
        private List<int> wordsLength;
        private int size;
#pragma warning disable CS0649
        private readonly int root;
#pragma warning restore CS0649

        public RegexAhoCorasick()
        {
            trie = new List<TrieNode>();
            wordsLength = new List<int>();
            root = 0;
            Init();
        }

        private void Init()
        {
            trie.Add(new TrieNode());
            size++;
        }

        public void AddString(string word, int wordID)
        {
            int currentVertex = root;
            for (int i = 0; i < word.Length; ++i)
            {
                char c = word[i];

                if (!trie[currentVertex].Children.ContainsKey(c))
                {
                    trie.Add(new TrieNode());

                    trie[size].SuffixLink = -1; // If not - add vertex
                    trie[size].Parent = currentVertex;
                    trie[size].ParentCharacter = c;
                    trie[currentVertex].Children[c] = size;
                    size++;
                }
                currentVertex = (int)trie[currentVertex].Children[c]; // Move to the new vertex in the trie
            }
            // Mark the end of the word and store its ID
            trie[currentVertex].Leaf = true;
            trie[currentVertex].WordID = wordID;
            wordsLength.Add(word.Length);
        }

        public void Initialize()
        {
            Queue<int> vertexQueue = new Queue<int>();
            vertexQueue.Enqueue(root);
            while (vertexQueue.Count > 0)
            {
                int currentVertex = vertexQueue.Dequeue();
                CalculateSuffixAndDictionaryLinks(currentVertex);

                foreach (char key in trie[currentVertex].Children.Keys)
                {
                    vertexQueue.Enqueue((int)trie[currentVertex].Children[key]);
                }
            }
        }

        public void CalculateSuffixAndDictionaryLinks(int vertex)
        {
            if (vertex == root)
            {
                trie[vertex].SuffixLink = root;
                trie[vertex].DictionaryLink = root;
                return;
            }

            // one character substrings
            if (trie[vertex].Parent == root)
            {
                trie[vertex].SuffixLink = root;
                if (trie[vertex].Leaf) trie[vertex].DictionaryLink = vertex;
                else trie[vertex].DictionaryLink = trie[trie[vertex].SuffixLink].DictionaryLink;
                return;
            }

            // To calculate the suffix link for the current vertex, we need the suffix
            // link for the parent and the character that moved us to the
            // current vertex.
            int curBetterVertex = trie[trie[vertex].Parent].SuffixLink;
            char chVertex = trie[vertex].ParentCharacter;
            while (true)
            {
                // If there is an edge with the needed char, update the suffix link
                // and leave the cycle
                if (trie[curBetterVertex].Children.ContainsKey(chVertex))
                {
                    trie[vertex].SuffixLink = (int)trie[curBetterVertex].Children[chVertex];
                    break;
                }
                // Jump by suffix links until we reach the root or find a better prefix for the current substring.
                if (curBetterVertex == root)
                {
                    trie[vertex].SuffixLink = root;
                    break;
                }
                // Go up by suffixlink
                curBetterVertex = trie[curBetterVertex].SuffixLink;
            }

            if (trie[vertex].Leaf)
            {
                trie[vertex].DictionaryLink = vertex;
            }
            else
            {
                trie[vertex].DictionaryLink = trie[trie[vertex].SuffixLink].DictionaryLink;
            }
        }

        public IEnumerable<(int indexOfMatch, int lengthOfMatch)> ProcessString(string text)
        {
            int currentState = root;
            int result = 0;

            for (int j = 0; j < text.Length; j++)
            {
                while (true)
                {
                    if (trie[currentState].Children.ContainsKey(text[j]))
                    {
                        currentState = (int)trie[currentState].Children[text[j]];
                        break;
                    }
                    if (currentState == root)
                    {
                        break;
                    }

                    currentState = trie[currentState].SuffixLink;
                }

                int checkState = currentState;

                // Trying to find all possible words from this prefix
                while (true)
                {
                    checkState = trie[checkState].DictionaryLink;

                    if (checkState == root) break;

                    // Found a match
                    result++;
                    int indexOfMatch = j + 1 - wordsLength[trie[checkState].WordID];
                    yield return (indexOfMatch, wordsLength[trie[checkState].WordID]);

                    // Try to find all matched patterns of smaller length
                    checkState = trie[checkState].SuffixLink;
                }
            }

            yield return (-1, -1);
        }
    }
}
