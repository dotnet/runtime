// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Collections.Generic
{
    internal class SortedSetDebugView<T>
    {
        private readonly SortedSet<T> _sortedSet;

        public SortedSetDebugView(SortedSet<T> sortedSet)
        {
            if (sortedSet == null)
            {
                throw new ArgumentNullException("set");
            }

            this._sortedSet = sortedSet;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                return _sortedSet.ToArray();
            }
        }
    }
}
