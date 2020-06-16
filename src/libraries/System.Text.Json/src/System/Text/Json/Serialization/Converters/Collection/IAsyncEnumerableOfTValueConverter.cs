// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for <cref>System.Collections.Generic.IAsyncEnumerable{TElement} where TElement is a reference type</cref>.
    /// </summary>
    internal sealed class IAsyncEnumerableOfTValueConverter<TCollection, TElement>
        : IAsyncEnumerableOfTConverter<TCollection, TElement>
        where TCollection : IAsyncEnumerable<TElement>
        where TElement : struct
    {
        // When leaving & re-entering the write stack frame, we need a place to store an IAsyncEnumerator<T>
        // where T is a value type. This means that type covariance on the interface will not work. The
        // simplest place to stash this in the WriteStackFrame structure without extending the structure
        // is within the IEnumerator enumerator property, which can be any IEnumerator implementation,
        // including this one which simply stores a supplied value in its Current property.

        private class StoragePlaceEnumerator : IEnumerator
        {
            public StoragePlaceEnumerator(object valueToStore)
            {
                Current = valueToStore;
            }

            public object? Current { get; private set; }

            public bool MoveNext() => true;
            public void Reset() { }
        }

        protected override void StoreEnumerator(ref WriteStack state, IAsyncEnumerator<TElement> enumerator)
        {
            state.Current.CollectionEnumerator = new StoragePlaceEnumerator(enumerator);
        }

        protected override IAsyncEnumerator<TElement> RetrieveEnumerator(ref WriteStack state)
        {
            if (!(state.Current.CollectionEnumerator is StoragePlaceEnumerator storagePlaceEnumerator))
                throw new InvalidOperationException("Unable to retrieve IAsyncEnumerator<TStruct> from write stack because no enumerator is present.");

            Debug.Assert(storagePlaceEnumerator.Current is IAsyncEnumerable<TElement>);

            return (IAsyncEnumerator<TElement>)storagePlaceEnumerator.Current;
        }

        protected override void ClearStoredEnumerator(ref WriteStack state)
        {
            state.Current.CollectionEnumerator = null;
        }
    }
}
