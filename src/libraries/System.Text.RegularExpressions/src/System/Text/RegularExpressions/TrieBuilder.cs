// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Text.RegularExpressions
{
    internal struct TrieBuilder
    {
        private List<TrieNode> _nodes = new List<TrieNode>() { TrieNode.CreateRoot() };

        private readonly int _nodeLimit = CompiledNodeLimit;

        private bool IsNodeLimitReached => _nodes.Count >= _nodeLimit;

        /// <summary>
        /// The maximum number of nodes in a trie, in the compiled and source-generated mode.
        /// </summary>
        /// <remarks>
        /// In the interpreted mode its value is halved.
        /// </remarks>
        private const int CompiledNodeLimit = 256;

        /// <summary>
        /// The maximum regex set size that will be accepted during the node traversal.
        /// </summary>
        /// <remarks>
        /// If the limit was 1, regex <c>[ab][cde]</c> would produce an empty trie,
        /// if it was 2 the trie would match <c>a</c> and <c>b</c>, and if it was 3
        /// the trie would match <c>ac</c>, <c>ad</c>, <c>ae</c>, <c>bc</c>, <c>bd</c>
        /// and <c>be</c>.
        /// </remarks>
        private const int SetLimit = 16;

        /// <summary>
        /// The maximum depth that a trie node can have. It is synonymous with the maximum length of a word in the trie.
        /// </summary>
        private const int DepthLimit = 12;

        private delegate int OneToOneNodeMapping<T>(ref TrieBuilder builder, int nodeIndex, T state, bool isFinal, out bool canContinue);

        private delegate NodeCollection ManyToManyNodeMapping<T>(ref TrieBuilder builder, NodeCollection nodes, T state, bool isFinal, out bool canContinue);

        public TrieBuilder(RegexNode regexNode)
        {
#if !REGEXGENERATOR
            if ((regexNode.Options & RegexOptions.Compiled) == 0)
            {
                _nodeLimit /= 2;
            }
#endif
        }

        /// <summary>
        /// Tries to create a trie from the leading fixed part of a <see cref="RegexNode"/>.
        /// </summary>
        public static List<TrieNode>? CreateFromPrefixIfPossible(RegexNode regexNode)
        {
            TrieBuilder builder = new TrieBuilder(regexNode);
            NodeCollection matchNodes = builder.Add(new NodeCollection(TrieNode.Root), regexNode, isFinal: true, out _);
            builder.AcceptMatches(matchNodes);

            // If we still have one trie node -the root one-, the regex node has no fixed leading part
            // (for example it starts with (a)*) and we cannot use the trie.
            // Furthermore if the root node is a match, it means that a regex may start with something not
            // in the trie (for example in a*|b|c, the trie will have b and c but not a) so we cannot use it either.
            if (builder._nodes.Count == 1 || builder._nodes[TrieNode.Root].IsMatch)
            {
                return null;
            }
            builder.RemoveUnreachableNodes();
            builder.ValidateInvariants();
            return builder._nodes;
        }

        /// <summary>
        /// Marks all nodes in <paramref name="nodes"/> as matching.
        /// </summary>
        private void AcceptMatches(NodeCollection nodes)
        {
            int count = nodes.Count;
            for (int i = 0; i < count; i++)
            {
                _nodes[nodes[i]].SetMatch();
            }
        }

        private void RemoveUnreachableNodes()
        {
            BitArray visited = new BitArray(_nodes.Count);
            visited.Set(TrieNode.Root, true);
            Stack<int> pending = new Stack<int>(DepthLimit);
            pending.Push(TrieNode.Root);
            int reachableNodeCount = 1;

            // Start from the root and mark the reachable nodes.
            while (pending.Count > 0)
            {
                int i = pending.Pop();
                visited.Set(i, true);
                foreach (KeyValuePair<char, int> child in _nodes[i].Children)
                {
                    if (!visited.Get(child.Value))
                    {
                        pending.Push(child.Value);
                        reachableNodeCount++;
                    }
                }
            }

            if (reachableNodeCount == _nodes.Count)
            {
                // All nodes are reachable, there's nothing to remove.
                return;
            }

            // Create a new list and put only the reachable nodes.
            List<TrieNode> reachableNodes = new List<TrieNode>(reachableNodeCount);
            int[] nodeIndexMapping = new int[_nodes.Count];
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (visited.Get(i))
                {
                    nodeIndexMapping[i] = reachableNodes.Count;
                    reachableNodes.Add(_nodes[i]);
                }
            }

            // Adjust the node indices to point to the reachable nodes.
            for (int i = 0; i < reachableNodes.Count; i++)
            {
                TrieNode node = reachableNodes[i];
                if (node.Parent != -1)
                {
                    node.Parent = nodeIndexMapping[node.Parent];
                }
                Dictionary<char, int> children = node.Children;
                foreach (KeyValuePair<char, int> child in children)
                {
                    children[child.Key] = nodeIndexMapping[child.Value];
                }
            }

            _nodes = reachableNodes;
        }

        /// <summary>
        /// A helper method that applies a one-to-one transformation in a <see cref="NodeCollection"/>.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="state"/>.</typeparam>
        /// <param name="nodes">The input <see cref="NodeCollection"/>.</param>
        /// <param name="state">A parameter passed to <paramref name="fAdd"/>.</param>
        /// <param name="fAdd">A function that accepts this trie, a node index of <paramref name="nodes"/>,
        /// <paramref name="state"/>, and returns a new node index. Typically it would call either
        /// <see cref="Add(ref TrieBuilder, int, char, bool, out bool)"/> or <see cref="Add(ref TrieBuilder, int, string, bool, out bool)"/>.</param>
        /// <param name="isFinal">Whether we are at the end of the traversal algorithm.</param>
        /// <param name="canContinue">Returns whether the traversal algorithm can continue past this transformation.</param>
        private NodeCollection AddOneToOneHelper<T>(NodeCollection nodes, T state, OneToOneNodeMapping<T> fAdd, bool isFinal, out bool canContinue)
        {
            int count = nodes.Count;
            switch (count)
            {
                case 0:
                    canContinue = false;
                    return NodeCollection.Empty;
                case 1:
                    int newNode = fAdd(ref this, nodes[0], state, isFinal, out canContinue);
                    return new NodeCollection(newNode);
                default:
                    canContinue = false;
                    HashSet<int> visited = new HashSet<int>();
                    List<int> result = new List<int>(count);
                    for (int i = 0; i < count; i++)
                    {
                        newNode = fAdd(ref this, nodes[i], state, isFinal, out bool canContinueInner);
                        if (!visited.Add(newNode))
                        {
                            continue;
                        }
                        // See comments in handling RegexNodeKind.Alternate below, to understand how it works.
                        if (canContinueInner)
                        {
                            canContinue = true;
                            result.Add(newNode);
                        }
                        else
                        {
                            _nodes[newNode].SetMatch();
                        }
                    }

                    return new NodeCollection(result);
            }
        }

        /// <summary>
        /// Adds a character after each node of the given collection.
        /// </summary>
        /// <param name="nodes">The collection of nodes</param>
        /// <param name="c">The character to add.</param>
        /// <param name="isFinal">Whether we are at the end of the traversal algorithm.</param>
        /// <param name="canContinue">Returns whether the traversal algorithm can look past the character.</param>
        /// <returns>The collection of node indices that each node of
        /// <paramref name="nodes"/> leads to, at <paramref name="c"/>.</returns>
        private NodeCollection Add(NodeCollection nodes, char c, bool isFinal, out bool canContinue)
        {
            return AddOneToOneHelper(nodes, c, Add, isFinal, out canContinue);
        }

        /// <summary>
        /// Adds a character after the given node.
        /// </summary>
        /// <param name="builder">The <see cref="TrieBuilder"/> <paramref name="nodeIndex"/> belongs to.</param>
        /// <param name="nodeIndex">The node's index.</param>
        /// <param name="c">The character to add.</param>
        /// <param name="isFinal">Whether we are at the end of the traversal algorithm.</param>
        /// <param name="canContinue">Returns whether the traversal algorithm can look past the character.</param>
        /// <returns>The index of the node that <paramref name="nodeIndex"/>
        /// leads to, at <paramref name="c"/>.</returns>
        private static int Add(ref TrieBuilder builder, int nodeIndex, char c, bool isFinal, out bool canContinue)
        {
            List<TrieNode> nodes = builder._nodes;
            TrieNode node = nodes[nodeIndex];
            if (builder.IsNodeLimitReached
                || node.Depth >= DepthLimit
                // Since we don't care which word we matched and return only the starting index of the leftmost match,
                // we don't have to add children after matching nodes. For example a trie for "the|their|them|they"
                // will just need to match "the".
                // And that's why we pass the isFinal parameter around. Without that, the nodes for each alternation
                // case would not be marked as matches, until all of them were processed. But now the algorithm knows
                // that nothing would come after this alternation, and can immediately mark "the" as matching, preventing
                // the other cases from being added.
                // We could have cleared the children from matching nodes and remove unreachable nodes after the traversal,
                // but that would be a waste of potential since the nodes we added and then removed still counted to the node
                // limit.
                // Patterns like "a(b|c*)" would still create unnecessary nodes because "a" would not be added as a match until
                // it was too late, so we still need to remove unreachable nodes afterwards. Not making them count towards the
                // node limit is not something easy though.
                || node.IsMatch)
            {
                canContinue = false;
                return nodeIndex;
            }

            if (!node.Children.TryGetValue(c, out int nextNodeIndex))
            {
                nextNodeIndex = nodes.Count;

                TrieNode newNode = new TrieNode()
                {
                    Parent = nodeIndex,
                    AccessingCharacter = c,
                    Depth = node.Depth + 1,
#if DEBUG || REGEXGENERATOR
                    Path = node.Path + c
#endif
                };

                node.Children.Add(c, nextNodeIndex);
                nodes.Add(newNode);
            }

            if (isFinal)
            {
                nodes[nextNodeIndex].SetMatch();
            }

            canContinue = true;
            return nextNodeIndex;
        }

        /// <summary>
        /// Adds a string after each node of the given collection.
        /// </summary>
        /// <param name="nodes">The collection of nodes</param>
        /// <param name="s">The string to add.</param>
        /// <param name="isFinal">Whether we are at the end of the traversal algorithm.</param>
        /// <param name="canContinue">Returns whether the traversal algorithm can look past the string.</param>
        /// <returns>The collection of node indices that each node of
        /// <paramref name="nodes"/> leads to, at <paramref name="s"/>.</returns>
        private NodeCollection Add(NodeCollection nodes, string s, bool isFinal, out bool canContinue)
        {
            return AddOneToOneHelper(nodes, s, Add, isFinal, out canContinue);
        }

        /// <summary>
        /// Adds a string after the given node.
        /// </summary>
        /// <param name="builder">The <see cref="TrieBuilder"/> <paramref name="nodeIndex"/> belongs to.</param>
        /// <param name="nodeIndex">The node's index.</param>
        /// <param name="s">The character to add.</param>
        /// <param name="isFinal">Whether we are at the end of the traversal algorithm.</param>
        /// <param name="canContinue">Returns whether the traversal algorithm can look past the string.</param>
        /// <returns>The index of the node that <paramref name="nodeIndex"/>
        /// leads to, at <paramref name="s"/>.</returns>
        private static int Add(ref TrieBuilder builder, int nodeIndex, string s, bool isFinal, out bool canContinue)
        {
            canContinue = true;
            if (s.Length > 0)
            {
                for (int i = 0; i < s.Length - 1 && canContinue; i++)
                {
                    nodeIndex = Add(ref builder, nodeIndex, s[i], false, out canContinue);
                }
                if (canContinue)
                {
                    nodeIndex = Add(ref builder, nodeIndex, s[s.Length - 1], isFinal, out canContinue);
                }
            }
            return nodeIndex;
        }

        /// <summary>
        /// Adds a character set after the nodes in the given <see cref="NodeCollection"/>.
        /// </summary>
        /// <param name="nodes">The collection of nodes.</param>
        /// <param name="setString">A string that describes the set.</param>
        /// <param name="isFinal">Whether we are at the end of the traversal algorithm.</param>
        /// <param name="canContinue">Returns whether the traversal algorithm can look past the set.</param>
        /// <returns>The collection of node indices that were created after <see cref="Add(ref TrieBuilder, int, char, bool, out bool)"/>ing each
        /// node of <paramref name="nodes"/>.</returns>
        private NodeCollection AddSet(NodeCollection nodes, string setString, bool isFinal, out bool canContinue)
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
                return Add(nodes, set[0], isFinal, out canContinue);
            }

            canContinue = false;
            NodeCollection[] results = new NodeCollection[setLength];
            for (int i = 0; i < setLength; i++)
            {
                NodeCollection result = Add(nodes, set[i], isFinal, out bool canContinueInner);

                // See comments in handling RegexNodeKind.Alternate below, for an explanation on how it works.
                if (canContinueInner)
                {
                    results[i] = result;
                    canContinue = true;
                }
                else
                {
                    results[i] = NodeCollection.Empty;
                    AcceptMatches(result);
                }
            }
            return new NodeCollection(results);
        }

        /// <summary>
        /// A helper method that applies a one-to-one transformation in a <see cref="NodeCollection"/>.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="state"/>.</typeparam>
        /// <param name="nodes">The input <see cref="NodeCollection"/>.</param>
        /// <param name="regexNode">The <see cref="RegexNode"/> of the loop.</param>
        /// <param name="state">A parameter passed to <paramref name="fAdd"/>.</param>
        /// <param name="fAdd">A function that accepts this trie builder, <paramref name="nodes"/>,
        /// <paramref name="state"/>, and returns a new node collection.</param>
        /// <param name="isFinal">Whether we are at the end of the traversal algorithm.</param>
        /// <param name="canContinue">Returns whether the traversal algorithm can continue past this transformation.</param>
        private NodeCollection AddLoopHelper<T>(NodeCollection nodes, RegexNode regexNode, T state, ManyToManyNodeMapping<T> fAdd, bool isFinal, out bool canContinue)
        {
            switch (regexNode.M, regexNode.N)
            {
                // We can process patterns of the form "x?" by handling if x was present and if it was not.
                // If we are at the end however, we don't need to add x to the trie. As an example imagine
                // we have "a(b|c)?". The trie would match "a", "ab" and "ac". But since all start with "a",
                // we can match only that, and not look into "(b|c)?" at all.
                case (0, 1) when !isFinal:
                    // We first add it to the trie.
                    NodeCollection resultIfExists = fAdd(ref this, nodes, state, isFinal, out bool canContinueIfExists);
                    // We can always continue by taking the case of x not being present.
                    canContinue = true;
                    if (canContinueIfExists)
                    {
                        // If x continues we combine the node collections both with and without x.
                        return new NodeCollection(new NodeCollection[] { nodes, resultIfExists });
                    }
                    else
                    {
                        // If x doesn't continue, we accept the node collection with it, and return the one without it.
                        AcceptMatches(resultIfExists);
                        return nodes;
                    }
                case (0, _):
                    // min being 0 means that the loop is of the form x* or x{0,k}.
                    // We can't extract a fixed part from that so we can't continue.
                    canContinue = false;
                    return nodes;
                case (int min, int max):
                    // a{3,} for example is equivalent to aaaa*. The first three a's can be
                    // added to the trie.
                    canContinue = true;
                    for (int i = 0; i < min - 1 && canContinue; i++)
                    {
                        nodes = fAdd(ref this, nodes, state, false, out canContinue);
                    }
                    if (canContinue)
                    {
                        nodes = fAdd(ref this, nodes, state, isFinal, out canContinue);
                    }
                    // If we managed to walk through all repetitions and the loop's count is fixed
                    // (like a{3}), we can continue past it. If it isn't we would have to handle all
                    // repetition cases (like aaaa, aaaaa and aaaaaa if we had a{3,6}); doesn't seem
                    // a good idea, it is prone to exploding the trie node count, and the trie is used
                    // for fixed patterns.
                    canContinue &= min == max;
                    return nodes;
            }
        }

        /// <summary>
        /// Adds the fixed part of the given <see cref="RegexNode"/> after the nodes in the given <see cref="NodeCollection"/>.
        /// </summary>
        /// <param name="nodes">The collection of nodes.</param>
        /// <param name="regexNode">The <see cref="RegexNode"/> to traverse.</param>
        /// <param name="isFinal">Whether we are at the end of the traversal algorithm.</param>
        /// <param name="canContinue">Returns whether the traversal algorithm can look past <paramref name="regexNode"/>.</param>
        private NodeCollection Add(NodeCollection nodes, RegexNode regexNode, bool isFinal, out bool canContinue)
        {
            if (!StackHelper.TryEnsureSufficientExecutionStack())
            {
                goto End;
            }

            while (regexNode.Kind is RegexNodeKind.Capture or RegexNodeKind.Atomic)
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
                    return Add(nodes, regexNode.Ch, isFinal, out canContinue);
                case RegexNodeKind.Multi:
                    // Easy, just add the string.
                    return Add(nodes, regexNode.Str!, isFinal, out canContinue);
                case RegexNodeKind.Set:
                    return AddSet(nodes, regexNode.Str!, isFinal, out canContinue);
                case RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic or RegexNodeKind.Onelazy:
                    static NodeCollection AddCharHelper(ref TrieBuilder builder, NodeCollection nodes, char c, bool isFinal, out bool canContinue)
                    {
                        return builder.Add(nodes, c, isFinal, out canContinue);
                    }
                    return AddLoopHelper(nodes, regexNode, regexNode.Ch, AddCharHelper, isFinal, out canContinue);
                case RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic or RegexNodeKind.Setlazy:
                    static NodeCollection AddSetHelper(ref TrieBuilder builder, NodeCollection nodes, string setString, bool isFinal, out bool canContinue)
                    {
                        return builder.AddSet(nodes, setString, isFinal, out canContinue);
                    }
                    return AddLoopHelper(nodes, regexNode, regexNode.Str!, AddSetHelper, isFinal, out canContinue);
                case RegexNodeKind.Loop or RegexNodeKind.Lazyloop:
                    static NodeCollection AddRegexNodeHelper(ref TrieBuilder builder, NodeCollection nodes, RegexNode regexNode, bool isFinal, out bool canContinue)
                    {
                        return builder.Add(nodes, regexNode, isFinal, out canContinue);
                    }
                    return AddLoopHelper(nodes, regexNode, regexNode.Child(0), AddRegexNodeHelper, isFinal, out canContinue);
                case RegexNodeKind.Concatenate:
                    int childCount = regexNode.ChildCount();
                    canContinue = true;
                    for (int i = 0; i < childCount - 1 && canContinue; i++)
                    {
                        nodes = Add(nodes, regexNode.Child(i), false, out canContinue);
                    }
                    if (canContinue)
                    {
                        nodes = Add(nodes, regexNode.Child(childCount - 1), isFinal, out canContinue);
                    }
                    return nodes;
                case RegexNodeKind.Alternate:
                    childCount = regexNode.ChildCount();
                    Debug.Assert(childCount != 0);
                    NodeCollection[] results = new NodeCollection[childCount];
                    canContinue = false;
                    for (int i = 0; i < childCount; i++)
                    {
                        NodeCollection result = Add(nodes, regexNode.Child(i), isFinal, out bool canContinueInner);
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
                    }
                    return new NodeCollection(results);
            }

        End:
            canContinue = false;
            return nodes;
        }

        [Conditional("DEBUG")]
        private void ValidateInvariants()
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                TrieNode node = _nodes[i];
                Debug.Assert(node.IsMatch == (node.Children.Count == 0), $"Node {i} must be childless if and only if it is a match node.");
            }
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
                        HashSet<int> visited = new HashSet<int>();
                        List<int> results = new List<int>();
                        foreach (NodeCollection nodeCollection in nodeCollections)
                        {
                            int count = nodeCollection.Count;
                            for (int i = 0; i < count; i++)
                            {
                                int nodeIndex = nodeCollection[i];
                                if (visited.Add(nodeIndex))
                                {
                                    results.Add(nodeIndex);
                                }
                            }
                        }
                        this = new NodeCollection(results);
                        break;
                }
            }
        }
    }
}
