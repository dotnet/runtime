// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

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
        /// A string containing the possible first characters of the strings this
        /// <see cref="MultiStringMatcher"/> finds. It will be used during searching to call
        /// <see cref="MemoryExtensions.IndexOfAny{T}(ReadOnlySpan{T}, ReadOnlySpan{T})"/>.
        /// </summary>
        /// <remarks>
        /// This property's value will be <see langword="null"/> if
        /// the characters are too many for the search to be vectorized.
        /// </remarks>
        internal string? PossibleFirstCharacters { get; }

        /// <summary>
        /// Creates a <see cref="MultiStringMatcher"/> from a trie, and
        /// calculates its nodes' suffix links and match lengths.
        /// </summary>
        internal MultiStringMatcher(List<TrieNode> trie)
        {
            _trie = BuildTrieLinks(trie);
            _trie = RemoveUnreachableNodes(_trie);
            PossibleFirstCharacters = GetPossibleFirstCharacters(trie[TrieNode.Root].Children);
            ValidateInvariants();
        }

        private static string? GetPossibleFirstCharacters(Dictionary<char, int> nodeChildren)
        {
            if (nodeChildren.Count > 5)
            {
                return null;
            }

#if !NET
            return StringExtensions.Create(nodeChildren.Count, nodeChildren, static (span, nodeChildren) =>
#else
            return string.Create(nodeChildren.Count, nodeChildren, static (span, nodeChildren) =>
#endif
            {
                int i = 0;
                foreach (KeyValuePair<char, int> child in nodeChildren)
                {
                    span[i++] = child.Key;
                }
            });
        }

        private static TrieNodeWithLinks[] RemoveUnreachableNodes(TrieNodeWithLinks[] trie)
        {
            int[] nodeIndexMapping = new int[trie.Length];
            int reachableCount = 0;
            for (int i = 0; i < trie.Length; i++)
            {
                // If a node's children is null it means that it was not touched
                // during the trie link assignment, and we can remove it.
                if (trie[i].Children is not null)
                {
                    nodeIndexMapping[i] = reachableCount++;
                }
            }

            // All nodes are reachable; there's nothing to remove.
            if (reachableCount == trie.Length)
            {
                return trie;
            }

            TrieNodeWithLinks[] newTrie = new TrieNodeWithLinks[reachableCount];

            for (int i = 0; i < trie.Length; i++)
            {
                ref readonly TrieNodeWithLinks node = ref trie[i];
                Dictionary<char, int> children = node.Children;
                if (children is null)
                {
                    continue;
                }
                if (children.Count != 0)
                {
                    Dictionary<char, int> newChildren = new Dictionary<char, int>();
                    foreach (KeyValuePair<char, int> child in children)
                    {
                        newChildren.Add(child.Key, nodeIndexMapping[child.Value]);
                    }
                    children = newChildren;
                }

                newTrie[nodeIndexMapping[i]] = new TrieNodeWithLinks()
                {
#if DEBUG
                    Path = node.Path,
#endif
                    Children = children,
                    SuffixLink = nodeIndexMapping[node.SuffixLink],
                    MatchLength = node.MatchLength
                };
            }

            return newTrie;
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
                // We remove children from match nodes when building the trie, but we have to do it again.
                // Imagine the pattern bc|abcd. The trie's match nodes are "bc" and "abcd" as expected,
                // but here we will make "abc" a match node because it matches "bc" and we stop. We don't
                // need "abcd" in the trie. We clear "abc"'s children won't look at them.
                Dictionary<char, int> children = matchLength == -1 ? node.Children : TrieNode.s_cachedEmptyChildren;

                result[currentVertex] = new TrieNodeWithLinks()
                {
#if DEBUG || !SYSTEM_TEXT_REGULAREXPRESSIONS
                    Path = node.Path,
#endif
                    Children = children,
                    SuffixLink = suffixLink,
                    MatchLength = matchLength
                };

                foreach (KeyValuePair<char, int> vertex in children)
                {
                    vertexQueue.Enqueue(vertex.Value);
                }
            }

            return result;
        }

        [Conditional("DEBUG")]
        private void ValidateInvariants()
        {
            TrieNode.ValidateInvariants();
            ReadOnlySpan<TrieNodeWithLinks> trie = Trie;
            for (int i = 0; i < trie.Length; i++)
            {
                ref readonly TrieNodeWithLinks node = ref Trie[i];
                Debug.Assert((node.MatchLength != -1) == (node.Children.Count == 0), $"Node {i} must be childless if and only if it is a match node.");
            }
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
            ReadOnlySpan<char> possibleFirstCharacters = PossibleFirstCharacters.AsSpan();

            for (int i = 0; i < text.Length; i++)
            {
                // If we are at the root node, we can quickly skip to the first character we can work with, if
                // they are few enough to make the search vectorized. And if we didn't find any, the search fails.
                if (!possibleFirstCharacters.IsEmpty && currentState == TrieNode.Root)
                {
                    switch (text.Slice(i).IndexOfAny(possibleFirstCharacters))
                    {
                        case -1:
                            return -1;
                        case int firstCharPos:
                            i += firstCharPos;
                            break;
                    }
                }

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
#if DEBUG || !SYSTEM_TEXT_REGULAREXPRESSIONS
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
