// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

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
#if DEBUG
                Path = ""
#endif
            };
        }

        public Dictionary<char, int> Children = new();

        public bool IsMatch;

        public int Parent { get; init; }

        public char AccessingCharacter { get; init; }

        public int Depth { get; init; }

        public int SuffixLink = -1;

        public int DictionaryLink = -1;

#if DEBUG
        public string? Path { get; init; }
        public string? SuffixLinkPath;
        public string? DictionaryLinkPath;

        public override string ToString() =>
            $"Path: {Path} Suffix Link: {SuffixLinkPath} Dictionary Link: {DictionaryLinkPath}";
#endif
    }
}
