// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    internal struct AhoCorasickNode
    {
        private static object EmptyChildrenSentinel => Array.Empty<int>();

        public int SuffixLink;
        public int MatchLength;

        // This is not a radix tree so we may have a lot of very sparse nodes (single child).
        // We save 1 child separately to avoid allocating a separate collection in such cases.
        private int _firstChildChar;
        private int _firstChildIndex;
        private object _children; // Either int[] or Dictionary<char, int>

        public AhoCorasickNode()
        {
            _firstChildChar = -1;
            _children = EmptyChildrenSentinel;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryGetChild(char c, out int index)
        {
            if (_firstChildChar == c)
            {
                index = _firstChildIndex;
                return true;
            }

            object children = _children;
            Debug.Assert(children is int[] || children is Dictionary<char, int>);

            if (children.GetType() == typeof(int[]))
            {
                int[] table = Unsafe.As<int[]>(children);
                if (c < (uint)table.Length)
                {
                    index = table[c];
                    if (index >= 0)
                    {
                        return true;
                    }
                }
            }
            else
            {
                return Unsafe.As<Dictionary<char, int>>(children).TryGetValue(c, out index);
            }

            index = 0;
            return false;
        }

        public void AddChild(char c, int index)
        {
            if (_firstChildChar < 0)
            {
                _firstChildChar = c;
                _firstChildIndex = index;
            }
            else
            {
                if (ReferenceEquals(_children, EmptyChildrenSentinel))
                {
                    _children = new Dictionary<char, int>();
                }

                ((Dictionary<char, int>)_children).Add(c, index);
            }
        }

        public readonly void AddChildrenToQueue(Queue<(char Char, int Index)> queue)
        {
            if (_firstChildChar >= 0)
            {
                queue.Enqueue(((char)_firstChildChar, _firstChildIndex));

                if (_children is Dictionary<char, int> children)
                {
                    foreach ((char childChar, int childIndex) in children)
                    {
                        queue.Enqueue((childChar, childIndex));
                    }
                }
                else
                {
                    Debug.Assert(ReferenceEquals(_children, EmptyChildrenSentinel));
                }
            }
        }

        public void OptimizeChildren()
        {
            if (_children is Dictionary<char, int> children)
            {
                children.Add((char)_firstChildChar, _firstChildIndex);

                float frequency = -2;

                // We have the _firstChildChar field that will always be checked first.
                // Improve throughput by setting it to the child character with the highest frequency.
                foreach ((char childChar, int childIndex) in children)
                {
                    float newFrequency = char.IsAscii(childChar) ? CharacterFrequencyHelper.AsciiFrequency[childChar] : -1;

                    if (newFrequency > frequency)
                    {
                        frequency = newFrequency;
                        _firstChildChar = childChar;
                        _firstChildIndex = childIndex;
                    }
                }

                children.Remove((char)_firstChildChar);

                if (TryCreateJumpTable(children, out int[]? table))
                {
                    _children = table;
                }
            }

            static bool TryCreateJumpTable(Dictionary<char, int> children, [NotNullWhen(true)] out int[]? table)
            {
                // We can use either a Dictionary<char, int> or int[] to map child characters to node indexes.
                // int[] is generally faster but consumes more memory for characters with high values.
                // We try to find the right balance between memory usage and lookup performance.
                // Currently we will sacrifice up to ~2x the memory consumption to use int[] for faster lookups.
                const int AcceptableSizeMultiplier = 2;

                Debug.Assert(children.Count > 0);

                int maxValue = -1;

                foreach ((char childChar, _) in children)
                {
                    maxValue = Math.Max(maxValue, childChar);
                }

                int tableSize = TableMemoryFootprintBytesEstimate(maxValue);
                int dictionarySize = DictionaryMemoryFootprintBytesEstimate(children.Count);

                if (tableSize > dictionarySize * AcceptableSizeMultiplier)
                {
                    // We would have a lot of empty entries. Avoid wasting too much memory.
                    table = null;
                    return false;
                }

                table = new int[maxValue + 1];
                Array.Fill(table, -1);

                foreach ((char childChar, int childIndex) in children)
                {
                    table[childChar] = childIndex;
                }

                return true;

                static int TableMemoryFootprintBytesEstimate(int maxValue)
                {
                    // An approximate number of bytes consumed by an
                    // int[] table with a known number of entries.
                    // Only used as a heuristic, so numbers don't have to be exact.
                    return 32 + (maxValue * sizeof(int));
                }

                static int DictionaryMemoryFootprintBytesEstimate(int childCount)
                {
                    // An approximate number of bytes consumed by a
                    // Dictionary<char, int> with a known number of entries.
                    // Only used as a heuristic, so numbers don't have to be exact.
                    return childCount switch
                    {
                        < 4 => 192,
                        < 8 => 272,
                        < 12 => 352,
                        _ => childCount * 25
                    };
                }
            }
        }
    }
}
