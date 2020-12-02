// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The RegexAhoCorasick object runs the Aho-Corasick algorithm to find multiple string matches in the pattern efficiently.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

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

        public void AddString(string s, int wordID)
        {
            int currentVertex = root;
            for (int i = 0; i < s.Length; ++i) // Iterating over the string's characters
            {
                char c = s[i];

                // Checking if a vertex with this edge exists in the trie:
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
            wordsLength.Add(s.Length);
        }

        public void PrepareAho()
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
            // Processing root (empty string)
            if (vertex == root)
            {
                trie[vertex].SuffixLink = root;
                trie[vertex].DictionaryLink = root;
                return;
            }
            // Processing children of the root (one character substrings)
            if (trie[vertex].Parent == root)
            {
                trie[vertex].SuffixLink = root;
                if (trie[vertex].Leaf) trie[vertex].DictionaryLink = vertex;
                else trie[vertex].DictionaryLink = trie[trie[vertex].SuffixLink].DictionaryLink;
                return;
            }
            // Cases above are degenerate cases as for prefix function calculation; the
            // value is always 0 and links to the root vertex.

            // To calculate the suffix link for the current vertex, we need the suffix
            // link for the parent of the vertex and the character that moved us to the
            // current vertex.
            int curBetterVertex = trie[trie[vertex].Parent].SuffixLink;
            char chVertex = trie[vertex].ParentCharacter;
            // From this vertex and its substring we will start to look for the maximum
            // prefix for the current vertex and its substring.

            while (true)
            {
                // If there is an edge with the needed char, we update our suffix link
                // and leave the cycle
                if (trie[curBetterVertex].Children.ContainsKey(chVertex))
                {
                    trie[vertex].SuffixLink = (int)trie[curBetterVertex].Children[chVertex];
                    break;
                }
                // Otherwise, we are jumping by suffix links until we reach the root
                // (equivalent of k == 0 in prefix function calculation) or we find a
                // better prefix for the current substring.
                if (curBetterVertex == root)
                {
                    trie[vertex].SuffixLink = root;
                    break;
                }
                curBetterVertex = trie[curBetterVertex].SuffixLink; // Go back by sufflink
            }
            // When we complete the calculation of the suffix link for the current
            // vertex, we should update the link to the end of the maximum length word
            // that can be produced from the current substring.
            if (trie[vertex].Leaf)
            {
                trie[vertex].DictionaryLink = vertex;
            }
            else
            {
                trie[vertex].DictionaryLink = trie[trie[vertex].SuffixLink].DictionaryLink;
            }
        }

        public int ProcessString(string text)
        {
            // Current state value
            int currentState = root;

            // Targeted result value
            int result = 0;

            for (int j = 0; j < text.Length; j++)
            {
                // Calculating new state in the trie
                while (true)
                {
                    // If we have the edge, then use it
                    if (trie[currentState].Children.ContainsKey(text[j]))
                    {
                        currentState = (int)trie[currentState].Children[text[j]];
                        break;
                    }
                    // Otherwise, jump by suffix links and try to find the edge with
                    // this char

                    // If there aren't any possible edges we will eventually ascend to
                    // the root, and at this point we stop checking.
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
                    // Checking all words that we can get from the current prefix
                    checkState = trie[checkState].DictionaryLink;

                    // If we are in the root vertex, there are no more matches
                    if (checkState == root) break;

                    // If the algorithm arrived at this row, it means we have found a
                    // pattern match. And we can make additional calculations like find
                    // the index of the found match in the text. Or check that the found
                    // pattern is a separate word and not just, e.g., a substring.
                    result++;
                    int indexOfMatch = j + 1 - wordsLength[trie[checkState].WordID];

                    // Trying to find all matched patterns of smaller length
                    checkState = trie[checkState].SuffixLink;
                }
            }

            return result;
        }
    }
}
