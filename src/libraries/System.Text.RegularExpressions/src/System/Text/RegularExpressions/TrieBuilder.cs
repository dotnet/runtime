// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Text.RegularExpressions
{
    internal struct TrieBuilder
    {
        private readonly List<TrieNode> _nodes = new List<TrieNode>() { TrieNode.CreateRoot() };

        /// <summary>
        /// How many times the regex node traversal can branch.
        /// </summary>
        /// <remarks>
        /// This limit exists to prevent the trie from getting too big.
        /// With a limit of 1, regex <c>[ab]c[de]f[gh]</c> would produce a
        /// trie matching <c>ac</c> and <c>bc</c>, while with a limit of 2,
        /// it would match <c>acd</c>, <c>ace</c>, <c>bcd</c>, and <c>bcec</c>.
        /// </remarks>
        private const int BranchingDepthLimit = 1;

        /// <summary>
        /// What is the maximum regex set size that will be accepted during the node traversal.
        /// </summary>
        /// <remarks>
        /// If the limit was 1, regex <c>[ab][cde]</c> would produce an empty trie,
        /// if it was 2 the trie would match <c>a</c> and <c>b</c>, and if it was 3
        /// the trie would match <c>ac</c>, <c>ad</c>, <c>ae</c>, <c>bc</c>, <c>bd</c>
        /// and <c>be</c>.</remarks>
        private const int SetLimit = 2;

        public TrieBuilder() { }

        /// <summary>
        /// Creates a trie that matches <paramref name="words"/>.
        /// </summary>
        public static List<TrieNode> Create(ReadOnlySpan<string> words)
        {
            Debug.Assert(!words.IsEmpty);

            TrieBuilder builder = new TrieBuilder();
            for (int i = 0; i < words.Length; i++)
            {
                int endNodeIndex = builder.Add(TrieNode.Root, words[i]);
                builder._nodes[endNodeIndex].IsMatch = true;
            }

            return builder._nodes;
        }

        /// <summary>
        /// Tries to create a trie from the leading fixed part of a <see cref="RegexNode"/>.
        /// </summary>
        public static bool TryCreate(RegexNode regexNode, [NotNullWhen(true)] out List<TrieNode>? trie)
        {
            // RightToLeft is not supported.
            Debug.Assert((regexNode.Options & RegexOptions.RightToLeft) == 0);

            TrieBuilder builder = new TrieBuilder();
            int branchingDepth = 0;
            NodeCollection matchNodes = builder.Add(new NodeCollection(TrieNode.Root), regexNode, ref branchingDepth, out _);
            builder.AcceptMatches(matchNodes);

            // If we still have one trie node -the root one-, the regex node has no fixed leading part
            // (i.e. it starts with (a)*) and we cannot make a trie.
            if (builder._nodes.Count == 1)
            {
                trie = default;
                return false;
            }
            trie = builder._nodes;
            return true;
        }

        /// <summary>
        /// Marks all nodes in <paramref name="nodes"/> as matching.
        /// </summary>
        private void AcceptMatches(NodeCollection nodes)
        {
            int count = nodes.Count;
            for (int i = 0; i < count; i++)
            {
                _nodes[nodes[i]].IsMatch = true;
            }
        }

        /// <summary>
        /// A helper method that applies a one-to-one transformation in a <see cref="NodeCollection"/>.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="state"/>.</typeparam>
        /// <param name="nodes">The input <see cref="NodeCollection"/>.</param>
        /// <param name="state">A parameter passed to <paramref name="func"/>.</param>
        /// <param name="func">A function that accepts this trie, a node index of <paramref name="nodes"/>,
        /// <paramref name="state"/>, and returns a new node index. Typically it would call either
        /// <see cref="Add(int, char)"/> or <see cref="Add(int, string)"/>.</param>
        /// <returns></returns>
        private NodeCollection AddOneToOneHelper<T>(NodeCollection nodes, T state, Func<TrieBuilder, int, T, int> func)
        {
            int count = nodes.Count;
            switch (count)
            {
                case 0:
                    return NodeCollection.Empty;
                case 1:
                    int newNode = func(this, nodes[0], state);
                    return new NodeCollection(newNode);
                default:
                    List<int> result = new List<int>(count);
                    for (int i = 0; i < count; i++)
                    {
                        int nextNodeIndex = func(this, nodes[i], state);
                        result.Add(nextNodeIndex);
                    }
                    return new NodeCollection(result);
            }
        }

        /// <summary>
        /// Adds a character after each node of the given collection.
        /// </summary>
        /// <param name="nodes">The collection of nodes</param>
        /// <param name="c">The character to add.</param>
        /// <returns>The collection of node indices that each node of
        /// <paramref name="nodes"/> leads to, at <paramref name="c"/>.</returns>
        private NodeCollection Add(NodeCollection nodes, char c)
        {
            return AddOneToOneHelper(nodes, c, static (trie, idx, c) => trie.Add(idx, c));
        }

        /// <summary>
        /// Adds a character after the given node.
        /// </summary>
        /// <param name="nodeIndex">The node's index.</param>
        /// <param name="c">The character to add.</param>
        /// <returns>The index of the node that <paramref name="nodeIndex"/>
        /// leads to, at <paramref name="c"/>.</returns>
        private int Add(int nodeIndex, char c)
        {
            TrieNode node = _nodes[nodeIndex];
            if (!node.Children.TryGetValue(c, out int nextNodeIndex))
            {
                nextNodeIndex = _nodes.Count;

                TrieNode newNode = new TrieNode()
                {
                    Parent = nodeIndex,
                    AccessingCharacter = c,
                    Depth = node.Depth + 1,
#if DEBUG
                    Path = node.Path + c
#endif
                };
            }
            return nextNodeIndex;
        }

        /// <summary>
        /// Adds a string after each node of the given collection.
        /// </summary>
        /// <param name="nodes">The collection of nodes</param>
        /// <param name="s">The string to add.</param>
        /// <returns>The collection of node indices that each node of
        /// <paramref name="nodes"/> leads to, at <paramref name="s"/>.</returns>
        private NodeCollection Add(NodeCollection nodes, string s)
        {
            return AddOneToOneHelper(nodes, s, static (trie, idx, s) => trie.Add(idx, s));
        }

        /// <summary>
        /// Adds a string after the given node.
        /// </summary>
        /// <param name="nodeIndex">The node's index.</param>
        /// <param name="s">The character to add.</param>
        /// <returns>The index of the node that <paramref name="nodeIndex"/>
        /// leads to, at <paramref name="s"/>.</returns>
        private int Add(int nodeIndex, string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                nodeIndex = Add(nodeIndex, s[i]);
            }
            return nodeIndex;
        }

        /// <summary>
        /// Adds a character set after the nodes in the given <see cref="NodeCollection"/>.
        /// </summary>
        /// <param name="nodes">The collection of nodes.</param>
        /// <param name="setString">A string that describes the set.</param>
        /// <param name="branchingDepth">A reference that tracks how many consecutive alternations have been encountered.</param>
        /// <param name="canContinue">Returns whether the traversal algorithm can look past the set.</param>
        /// <returns>The collection of node indices that were created after <see cref="Add(int, char)"/>ing each
        /// node of <paramref name="nodes"/>.</returns>
        private NodeCollection AddSet(NodeCollection nodes, string setString, ref int branchingDepth, out bool canContinue)
        {
            Span<char> set = stackalloc char[SetLimit];
            int setLength = RegexCharClass.GetSetChars(setString, set);
            // The set is either empty (unlikely) or has more characters than the limit,
            // and we don't support negated character classes.
            if (setLength == 0 || RegexCharClass.IsNegated(setString))
            {
                canContinue = false;
                return nodes;
            }

            // We handle the case of the set length being 1 separately,
            // we don't need to increase the branching depth.
            if (setLength == 1)
            {
                canContinue = true;
                return Add(nodes, set[0]);
            }

            if (branchingDepth++ > BranchingDepthLimit)
            {
                canContinue = false;
                return nodes;
            }
            canContinue = true;
            NodeCollection[] nodeCollections = new NodeCollection[setLength];
            for (int i = 0; i < setLength; i++)
                nodeCollections[i] = Add(nodes, set[i]);
            return new NodeCollection(nodeCollections);
        }

        /// <summary>
        /// Adds the fixed part of the given <see cref="RegexNode"/> after the nodes in the given <see cref="NodeCollection"/>.
        /// </summary>
        /// <param name="nodes">The collection of nodes.</param>
        /// <param name="regexNode">The <see cref="RegexNode"/> to traverse.</param>
        /// <param name="branchingDepth">A reference that tracks how many consecutive alternations have been encountered.</param>
        /// <param name="canContinue">Returns whether the traversal algorithm can look past <paramref name="regexNode"/>.</param>
        /// <returns></returns>
        private NodeCollection Add(NodeCollection nodes, RegexNode regexNode, ref int branchingDepth, out bool canContinue)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                goto End;
            }

            while (regexNode.Kind is RegexNodeKind.Capture or RegexNodeKind.Group or RegexNodeKind.Atomic)
            {
                regexNode = regexNode.Child(0);
            }

            switch (regexNode.Kind)
            {
                case RegexNodeKind.Bol:
                case RegexNodeKind.Eol:
                case RegexNodeKind.Boundary:
                case RegexNodeKind.ECMABoundary:
                case RegexNodeKind.NonBoundary:
                case RegexNodeKind.NonECMABoundary:
                case RegexNodeKind.Beginning:
                case RegexNodeKind.Start:
                case RegexNodeKind.EndZ:
                case RegexNodeKind.End:
                case RegexNodeKind.Empty:
                case RegexNodeKind.UpdateBumpalong:
                case RegexNodeKind.PositiveLookaround:
                case RegexNodeKind.NegativeLookaround:
                    // Pass through zero-width nodes.
                    canContinue = true;
                    return nodes;
                case RegexNodeKind.One:
                    // Easy, just add the character.
                    canContinue = true;
                    return Add(nodes, regexNode.Ch);
                case RegexNodeKind.Multi:
                    // Easy, just add the string.
                    canContinue = true;
                    return Add(nodes, regexNode.Str!);
                case RegexNodeKind.Set:
                    return AddSet(nodes, regexNode.Str!, ref branchingDepth, out canContinue);
                case RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic or RegexNodeKind.Onelazy:
                    // These remarks also apply to the rest of the loop types.
                    int min = regexNode.M;
                    int max = regexNode.N;
                    // min being 0 means that the loop is of the form x*.
                    // We can't extract a fixed part from that so the traversal stops here.
                    if (min == 0)
                    {
                        goto End;
                    }
                    // a{3,} for example is equivalent to aaaa*. The first three a's can be
                    // added to the trie.
                    for (int i = 0; i < min; i++)
                    {
                        nodes = Add(nodes, regexNode.Ch);
                    }
                    // If the repetition count is fixed (like a{3}), we can continue past it.
                    // If it isn't we would have to handle all repetition cases (like aaaa,
                    // aaaaa and aaaaaa if we had a{3,6}); let's not do that yet.
                    canContinue = min == max;
                    return nodes;
                case RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic or RegexNodeKind.Setlazy:
                    min = regexNode.M;
                    max = regexNode.N;
                    if (min == 0)
                    {
                        goto End;
                    }
                    canContinue = true;
                    for (int i = 0; i < min && canContinue; i++)
                    {
                        nodes = AddSet(nodes, regexNode.Str!, ref branchingDepth, out canContinue);
                    }
                    // Traversing items of set loops (and the more general kinds of loops below) might
                    // signal us that we should stop. So to continue we need both to successfully pass
                    // through all counts of the loops, and the loop's count to be fixed.
                    canContinue &= min == max;
                    return nodes;
                case RegexNodeKind.Loop or RegexNodeKind.Lazyloop:
                    min = regexNode.M;
                    max = regexNode.N;
                    RegexNode loopItem = regexNode.Child(0);
                    if (min == 0)
                    {
                        goto End;
                    }
                    canContinue = true;
                    for (int i = 0; i < min && canContinue; i++)
                    {
                        nodes = Add(nodes, loopItem, ref branchingDepth, out canContinue);
                    }
                    canContinue &= min == max;
                    return nodes;
                case RegexNodeKind.Concatenate:
                    int childCount = regexNode.ChildCount();
                    canContinue = true;
                    for (int i = 0; i < childCount && canContinue; i++)
                    {
                        nodes = Add(nodes, regexNode.Child(i), ref branchingDepth, out canContinue);
                    }
                    return nodes;
                case RegexNodeKind.Alternate:
                    childCount = regexNode.ChildCount();
                    Debug.Assert(childCount != 0);
                    // If we have branched too many times we stop here.
                    if (branchingDepth++ > BranchingDepthLimit)
                    {
                        goto End;
                    }
                    NodeCollection[] results = new NodeCollection[childCount];
                    canContinue = false;
                    for (int i = 0; i < childCount; i++)
                    {
                        int branchingDepthLocal = branchingDepth;
                        NodeCollection result = Add(nodes, regexNode.Child(i), ref branchingDepthLocal, out bool canContinueInner);
                        if (canContinueInner)
                        {
                            // If we can continue after this node, we flow its results to the
                            // alternation node's results, and mark that we can continue past that.
                            results[i] = result;
                            canContinue = true;
                        }
                        else
                        {
                            // If we cannot, we don't flow the node's results, but mark them as matching.
                            // An example is a(b*|c+|d)e. The trie would match "a", "ac" and "ade". Only the
                            // last part of the alternation is allowed to continue, and "e" is added to it.
                            // b* stops immediately and we match "a" which lies before that, and c+ stops
                            // after adding one "c", and doesn't flow to the "e".
                            // Also in this particular case, when only one of the alternation nodes continues,
                            // we could technically not increase the branching depth, but that makes the logic
                            // a bit more complicated.
                            results[i] = NodeCollection.Empty;
                            AcceptMatches(result);
                        }
                        if (branchingDepthLocal > branchingDepth)
                        {
                            branchingDepth = branchingDepthLocal;
                        }
                    }
                    return new NodeCollection(results);
            }

        End:
            canContinue = false;
            return nodes;
        }

        /// <summary>
        /// A collection of node indexes.
        /// </summary>
        private readonly struct NodeCollection
        {
            // Avoid the allocation of the list if we are storing less than two items.
            private int SingleNode { get; init; }
            private List<int>? NodeList { get; init; }

            public static NodeCollection Empty => new() { SingleNode = -1, NodeList = null };

            public int Count => (SingleNode, NodeList) switch
            {
                (_, not null) => NodeList.Count,
                (-1, _) => 0,
                _ => 1
            };

            public int this[int i] => NodeList != null ? NodeList[i] : SingleNode;

            public NodeCollection(int node)
            {
                SingleNode = node;
                NodeList = null;
            }

            public NodeCollection(List<int> nodes)
            {
                SingleNode = 0;
                NodeList = nodes;
            }

            public NodeCollection(ReadOnlySpan<NodeCollection> nodeCollections)
            {
                switch (nodeCollections.Length)
                {
                    case 0:
                        this = Empty;
                        break;
                    case 1:
                        this = nodeCollections[0];
                        break;
                    default:
                        List<int> results = new List<int>();
                        foreach (NodeCollection nodeCollection in nodeCollections)
                        {
                            int count = nodeCollection.Count;
                            for (int i = 0; i < count; i++)
                            {
                                int nodeIndex = nodeCollection[i];
                                results.Add(nodeIndex);
                            }
                        }
                        this = new NodeCollection(results);
                        break;
                }
            }
        }
    }
}
