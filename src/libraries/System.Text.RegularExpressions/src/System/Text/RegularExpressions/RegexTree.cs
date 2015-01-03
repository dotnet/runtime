// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// RegexTree is just a wrapper for a node tree with some
// global information attached.

using System.Collections.Generic;

namespace System.Text.RegularExpressions
{
    internal sealed class RegexTree
    {
        internal RegexTree(RegexNode root, Dictionary<Int32, Int32> caps, Int32[] capnumlist, int captop, Dictionary<String, Int32> capnames, String[] capslist, RegexOptions opts)
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
        internal readonly Dictionary<Int32, Int32> _caps;
        internal readonly Int32[] _capnumlist;
        internal readonly Dictionary<String, Int32> _capnames;
        internal readonly String[] _capslist;
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
