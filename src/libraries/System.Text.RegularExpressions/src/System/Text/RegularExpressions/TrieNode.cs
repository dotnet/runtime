// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.RegularExpressions
{
    internal sealed class TrieNode
    {
        public const int Root = 0;

        public static TrieNode CreateRoot()
        {
            return new TrieNode()
            {
                Parent = -1,
                AccessingCharacter = '\0',
                Depth = 0,
#if DEBUG || !SYSTEM_TEXT_REGULAREXPRESSIONS
                Path = ""
#endif
            };
        }

        // A cached empty children dictionary.
        internal static readonly Dictionary<char, int> s_cachedEmptyChildren = new();

        public Dictionary<char, int> Children { get; private set; } = new();

        private bool _isMatch;

        public bool IsMatch => _isMatch;

        public bool IsRoot => Depth == 0;

        public int Parent { get; set; }

        public char AccessingCharacter { get; init; }

        public int Depth { get; init; }

#if DEBUG
        public int ChildCount => Children.Count;
#endif

#if DEBUG || !SYSTEM_TEXT_REGULAREXPRESSIONS
        public string Path { get; init; } = "<unset>";

        public override string ToString() => Path;
#endif

        public void SetMatch()
        {
            Children = s_cachedEmptyChildren;
            _isMatch = true;
        }

        [Conditional("DEBUG")]
        public static void ValidateInvariants()
        {
            Debug.Assert(s_cachedEmptyChildren.Count == 0);
        }
    }

    internal static class TrieExtensions
    {
        public static int GetMatchCount(this List<TrieNode> trie)
        {
            int matchCount = 0;
            foreach (TrieNode node in trie)
            {
                if (node.IsMatch)
                {
                    matchCount++;
                }
            }
            return matchCount;
        }
    }
}
