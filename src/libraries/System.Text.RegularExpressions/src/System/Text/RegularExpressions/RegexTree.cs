// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Globalization;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// Provides the core data describing a parsed <see cref="RegexNode"/> tree, along with necessary
    /// information about captures in the tree and computed optimizations about its structure.
    /// </summary>
    internal sealed class RegexTree
    {
        /// <summary>The root node of the parsed <see cref="RegexNode"/> tree.</summary>
        public readonly RegexNode Root;
        public readonly Hashtable? Caps;
        public readonly int CapSize;
        public readonly Hashtable? CapNames;
        public readonly string[]? CapsList;
        public readonly RegexOptions Options;
        public readonly RegexFindOptimizations FindOptimizations;

        internal RegexTree(RegexNode root, Hashtable? capNames, string[]? capsList, Hashtable? caps, int capSize, RegexOptions options, CultureInfo culture)
        {
            Root = root;
            Caps = caps;
            CapSize = capSize;
            CapNames = capNames;
            CapsList = capsList;
            Options = options;
            FindOptimizations = new RegexFindOptimizations(root, options, culture);
        }
    }
}
