// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Text;

namespace System.Buffers
{
    internal ref struct AhoCorasickBuilder
    {
        private readonly ReadOnlySpan<string> _values;
        private readonly bool _ignoreCase;
        private ValueListBuilder<AhoCorasick.Node> _nodes;
        private ValueListBuilder<int> _parents;
        private Vector256<byte> _startingCharsAsciiBitmap;
        private int _maxValueLength; // Only used by the NLS fallback

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

            return new AhoCorasick(_nodes.AsSpan().ToArray(), _startingCharsAsciiBitmap, _maxValueLength);
        }

        public void Dispose()
        {
            _nodes.Dispose();
            _parents.Dispose();
        }

        private void BuildTrie(ref HashSet<string>? unreachableValues)
        {
            _nodes.Append(new AhoCorasick.Node());
            _parents.Append(0);

            foreach (string value in _values)
            {
                int nodeIndex = 0;
                ref AhoCorasick.Node node = ref _nodes[nodeIndex];

                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];

                    if (!node.TryGetChild(c, out int childIndex))
                    {
                        childIndex = _nodes.Length;
                        node.AddChild(c, childIndex);
                        _nodes.Append(new AhoCorasick.Node());
                        _parents.Append(nodeIndex);
                    }

                    node = ref _nodes[childIndex];
                    nodeIndex = childIndex;

                    if (node.MatchLength != 0)
                    {
                        // A previous value is an exact prefix of this one.
                        // We're looking for the index of the first match, not necessarily the longest one, we can skip this value.
                        // We've already normalized the values, so we can do ordinal comparisons here.
                        unreachableValues ??= new HashSet<string>(StringComparer.Ordinal);
                        unreachableValues.Add(value);
                        break;
                    }

                    if (i == value.Length - 1)
                    {
                        node.MatchLength = value.Length;
                        _maxValueLength = Math.Max(_maxValueLength, value.Length);
                        break;
                    }
                }
            }
        }

        private void AddSuffixLinks()
        {
            var queue = new Queue<(char Char, int Index)>();
            queue.Enqueue(((char)0, 0));

            while (queue.TryDequeue(out (char Char, int Index) trieNode))
            {
                ref AhoCorasick.Node node = ref _nodes[trieNode.Index];
                int parent = _parents[trieNode.Index];
                int suffixLink = _nodes[parent].SuffixLink;

                if (parent != 0)
                {
                    while (suffixLink >= 0)
                    {
                        ref AhoCorasick.Node suffixNode = ref _nodes[suffixLink];

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
                    node.SuffixLink = -1;

                    // If a node is a match, there's no need to assign suffix links to its children.
                    // If a child does not match, such that we would look at its suffix link, we already saw an earlier match node.
                }
                else
                {
                    node.SuffixLink = suffixLink;

                    if (suffixLink >= 0)
                    {
                        node.MatchLength = _nodes[suffixLink].MatchLength;
                    }

                    node.AddChildrenToQueue(queue);
                }
            }
        }

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
                IndexOfAnyAsciiSearcher.ComputeBitmap(startingChars.AsSpan(), out _startingCharsAsciiBitmap, out _);
            }

            startingChars.Dispose();
        }
    }
}
