// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// RegexTree is just a wrapper for a node tree with some
// global information attached.

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions
{
    internal sealed class RegexTree
    {
        public readonly RegexNode Root;
        public readonly Hashtable Caps;
        public readonly int[] CapNumList;
        public readonly int CapTop;
        public readonly Hashtable CapNames;
        public readonly string[] CapsList;
        public readonly RegexOptions Options;
        public readonly int MinRequiredLength;

        internal RegexTree(RegexNode root, Hashtable caps, int[] capNumList, int capTop, Hashtable capNames, string[] capsList, RegexOptions options, int minRequiredLength)
        {
            Root = root;
            Caps = caps;
            CapNumList = capNumList;
            CapTop = capTop;
            CapNames = capNames;
            CapsList = capsList;
            Options = options;
            MinRequiredLength = minRequiredLength;
        }

#if DEBUG
        [ExcludeFromCodeCoverage]
        public void Dump() => Root.Dump();

        [ExcludeFromCodeCoverage]
        public override string ToString() => Root.ToString();

        [ExcludeFromCodeCoverage]
        public bool Debug => (Options & RegexOptions.Debug) != 0;
#endif
    }
}
