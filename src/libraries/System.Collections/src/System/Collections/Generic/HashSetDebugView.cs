// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Collections.Generic
{
    internal sealed class HashSetDebugView<T>
    {
        private readonly HashSet<T> _set;

        public HashSetDebugView(HashSet<T> set)
        {
            if (set == null)
            {
                throw new ArgumentNullException("set");
            }

            _set = set;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                return _set.ToArray();
            }
        }
    }
}
