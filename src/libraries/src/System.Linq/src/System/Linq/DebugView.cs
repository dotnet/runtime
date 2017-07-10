// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    // NOTE: DO NOT DELETE THE FOLLOWING DEBUG VIEW TYPES.
    // Although it might be tempting due to them not be referenced anywhere in this library,
    // Visual Studio (and potentially other debuggers) currently depends on their existence to
    // enable the "Results" view in watch windows.  Specifically:
    // - It creates the debug view by name (the view then has the same requirements as other views).
    // - It looks for the empty exception by name.
    // - It uses the exception's Empty property by name.

    /// <summary>
    /// This class provides the items view for the Enumerable
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class SystemCore_EnumerableDebugView<T>
    {
        public SystemCore_EnumerableDebugView(IEnumerable<T> enumerable)
        {
            _enumerable = enumerable ?? throw Error.ArgumentNull(nameof(enumerable));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                T[] array = _enumerable.ToArray();
                if (array.Length == 0)
                {
                    throw new SystemCore_EnumerableDebugViewEmptyException();
                }

                return array;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IEnumerable<T> _enumerable;
    }

    internal sealed class SystemCore_EnumerableDebugViewEmptyException : Exception
    {
        public string Empty => SR.EmptyEnumerable;
    }

    internal sealed class SystemCore_EnumerableDebugView
    {
        public SystemCore_EnumerableDebugView(IEnumerable enumerable)
        {
            _enumerable = enumerable ?? throw Error.ArgumentNull(nameof(enumerable));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object[] Items
        {
            get
            {
                List<object> tempList = new List<object>();
                foreach (object item in _enumerable)
                {
                    tempList.Add(item);
                }

                if (tempList.Count == 0)
                {
                    throw new SystemCore_EnumerableDebugViewEmptyException();
                }

                return tempList.ToArray();
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IEnumerable _enumerable;
    }

    internal sealed class SystemLinq_GroupingDebugView<TKey, TElement>
    {
        private readonly Grouping<TKey, TElement> _grouping;
        private TElement[] _cachedValues;

        public SystemLinq_GroupingDebugView(Grouping<TKey, TElement> grouping)
        {
            _grouping = grouping;
        }

        public TKey Key => _grouping.Key;

        // The name of this property must alphabetically follow `Key` so the elements appear last in the display.
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public TElement[] Values => _cachedValues ?? (_cachedValues = _grouping.ToArray());
    }

    internal sealed class SystemLinq_LookupDebugView<TKey, TElement>
    {
        private readonly Lookup<TKey, TElement> _lookup;
        private IGrouping<TKey, TElement>[] _cachedGroupings;

        public SystemLinq_LookupDebugView(Lookup<TKey, TElement> lookup)
        {
            _lookup = lookup;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public IGrouping<TKey, TElement>[] Groupings => _cachedGroupings ?? (_cachedGroupings = _lookup.ToArray());
    }
}
