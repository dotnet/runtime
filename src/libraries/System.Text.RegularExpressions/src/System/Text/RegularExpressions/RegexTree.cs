// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// RegexTree is just a wrapper for a node tree with some
// global information attached.

using System.Collections;

namespace System.Text.RegularExpressions
{
    internal sealed class RegexTree
    {
        internal RegexTree(RegexNode root, Hashtable caps, int[] capnumlist, int captop, Hashtable capnames, string[] capslist, RegexOptions opts)
        {
            _root = root;
            _caps = caps;
            _capnumlist = capnumlist;
            _capnames = capnames;
            _capslist = capslist;
            _captop = captop;
            _options = opts;
        }

        internal readonly RegexNode _root;
        internal readonly Hashtable _caps;
        internal readonly int[] _capnumlist;
        internal readonly Hashtable _capnames;
        internal readonly string[] _capslist;
        internal readonly RegexOptions _options;
        internal readonly int _captop;

#if DEBUG
        internal void Dump()
        {
            _root.Dump();
        }

        internal bool Debug
        {
            get
            {
                return (_options & RegexOptions.Debug) != 0;
            }
        }
#endif
    }
}
