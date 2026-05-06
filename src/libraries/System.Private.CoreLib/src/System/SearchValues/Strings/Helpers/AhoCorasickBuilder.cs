// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Text;

namespace System.Buffers
{
    /// <summary>
    /// Separated out of <see cref="AhoCorasick"/> to allow us to defer some computation costs in case we decide not to build the full thing.
    /// </summary>
    internal ref struct AhoCorasickBuilder
    {
        private readonly ReadOnlySpan<string> _values;
        private readonly bool _ignoreCase;
        private ValueListBuilder<AhoCorasickNode> _nodes;
        private ValueListBuilder<int> _parents;
        private IndexOfAnyAsciiSearcher.AsciiState _startingAsciiChars;

        public AhoCorasickBuilder(ReadOnlySpan<string> values, bool ignoreCase, ref HashSet<string>? unreachableValues)
        {
            Debug.Assert(!values.IsEmpty);
            Debug.Assert(!string.IsNullOrEmpty(values[0]));

#if DEBUG
            // The input should have been sorted by length
            for (int i = 1; i < values.Length; i++)
            {
                Debug.Assert(values[i - 1].Length <= values[i].Length);
            }
#endif

            _values = values;
            _ignoreCase = ignoreCase;
            BuildTrie(ref unreachableValues);
        }

        public AhoCorasick Build()
        {
            AddSuffixLinks();

            Debug.Assert(_nodes[0].MatchLength == 0, "The root node shouldn't have a match.");

            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].OptimizeChildren();
            }

            if (IndexOfAnyAsciiSearcher.IsVectorizationSupported)
            {
                GenerateStartingAsciiCharsBitmap();
            }

            return new AhoCorasick(_nodes.AsSpan().ToArray(), _startingAsciiChars);
        }

        public void Dispose()
        {
            _nodes.Dispose();
            _parents.Dispose();
        }

        private void BuildTrie(ref HashSet<string>? unreachableValues)
        {
            _nodes.Append(new AhoCorasickNode());
            _parents.Append(0);

            foreach (string value in _values)
            {
                int nodeIndex = 0;
                ref AhoCorasickNode node = ref _nodes[nodeIndex];

                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];

                    if (!node.TryGetChild(c, out int childIndex))
                    {
                        childIndex = _nodes.Length;
                        node.AddChild(c, childIndex);
                        _nodes.Append(new AhoCorasickNode());
                        _parents.Append(nodeIndex);
                    }

                    node = ref _nodes[childIndex];
                    nodeIndex = childIndex;

                    if (node.MatchLength != 0)
                    {
                        // A previous value is an exact prefix of this one.
                        // We're looking for the index of the first match, not necessarily the longest one, so we can skip this value.
                        // We've already normalized the values, so we can do ordinal comparisons here.
                        unreachableValues ??= new HashSet<string>(StringComparer.Ordinal);
                        unreachableValues.Add(value);
                        break;
                    }

                    if (i == value.Length - 1)
                    {
                        node.MatchLength = value.Length;
                        break;
                    }
                }
            }
        }

        private void AddSuffixLinks()
        {
            // Besides the list of children which continue the current value, each node also contains a suffix link
            // which points to the node with the longest suffix of the current node.
            // When we're searching and can't find a child to extend the current string with, we will follow
            // suffix links to find the longest string that does match up until the current point.
            //
            // For example if we have strings "DOTNET" and "OTTER", we want
            // the 'O' and 'T' in "dotnet" to point into 'O' and 'T' in "OTTER".
            // If our text contains the word "dotter", we will walk it character by character.
            // Once we get to "DOTNET" and read the next character 'T', we can no longer continue with "DOTNET",
            // and will instead follow the suffix link to "ot" in "OTTER" where we can continue the search.
            //
            // We also remember when a node's suffix link points to the end of a different value, such that it is itself a match.
            // If we also had the word "POTTERY", the 'R' would contain a suffix link to the 'R' in "OTTER",
            // but also mark that it is already a length=5 match.
            //
            //       +---> D  O  T  N  E  T
            //       |        |  |
            //       |     +--+  |
            // root--+     |     |
            //       |     |  +--+
            //       |     v  v
            //       +---> O  T  T  E  R
            //       |     ^  ^  ^  ^  ^
            //       |     |  |  |  |  | -- this is also a length=5 match
            //       |     |  |  |  |  |
            //       +> P  O  T  T  E  R  Y

            var queue = new Queue<(char Char, int Index)>();
            queue.Enqueue(((char)0, 0));

            while (queue.TryDequeue(out (char Char, int Index) trieNode))
            {
                ref AhoCorasickNode node = ref _nodes[trieNode.Index];
                int parent = _parents[trieNode.Index];
                int suffixLink = _nodes[parent].SuffixLink;

                // If this node doesn't represent the first character of a value (doesn't immediately follow the root node),
                // it may have a have a non-zero suffix link.
                if (parent != 0)
                {
                    while (suffixLink >= 0)
                    {
                        ref AhoCorasickNode suffixNode = ref _nodes[suffixLink];

                        if (suffixNode.TryGetChild(trieNode.Char, out int childSuffixLink))
                        {
                            suffixLink = childSuffixLink;
                            break;
                        }

                        if (suffixLink == 0)
                        {
                            break;
                        }

                        suffixLink = suffixNode.SuffixLink;
                    }
                }

                if (node.MatchLength != 0)
                {
                    // This node represents the end of a match.
                    // Mark it in a special way we can recognize when searching.
                    node.SuffixLink = -1;

                    // If a node is a match, there is no need to assign suffix links to its children.
                    // If a child does not match, such that we would look at its suffix link,
                    // we have already saw an earlier match node that is definitely the earliest possible match.
                }
                else
                {
                    node.SuffixLink = suffixLink;

                    if (suffixLink >= 0)
                    {
                        // Remember if this node's suffix link points to a node that is itself a match.
                        node.MatchLength = _nodes[suffixLink].MatchLength;
                    }

                    node.AddChildrenToQueue(queue);
                }
            }
        }

        // If all the values start with ASCII characters, we can use IndexOfAnyAsciiSearcher
        // to quickly skip to the next possible starting location in the input.
        private void GenerateStartingAsciiCharsBitmap()
        {
            scoped ValueListBuilder<char> startingChars = new ValueListBuilder<char>(stackalloc char[128]);

            foreach (string value in _values)
            {
                char c = value[0];

                if (_ignoreCase)
                {
                    startingChars.Append(char.ToLowerInvariant(c));
                    startingChars.Append(char.ToUpperInvariant(c));
                }
                else
                {
                    startingChars.Append(c);
                }
            }

            if (Ascii.IsValid(startingChars.AsSpan()))
            {
                IndexOfAnyAsciiSearcher.ComputeAsciiState(startingChars.AsSpan(), out _startingAsciiChars);
            }

            startingChars.Dispose();
        }
    }
}
