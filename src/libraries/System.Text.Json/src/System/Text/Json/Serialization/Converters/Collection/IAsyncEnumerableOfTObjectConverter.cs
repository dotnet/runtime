// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter for <cref>System.Collections.Generic.IAsyncEnumerable{TElement} where TElement is a reference type</cref>.
    /// </summary>
    internal sealed class IAsyncEnumerableOfTObjectConverter<TCollection, TElement>
        : IAsyncEnumerableOfTConverter<TCollection, TElement>
        where TCollection : IAsyncEnumerable<TElement>
        where TElement : class
    {
        // When leaving & re-entering the write stack frame, we need a place to store an IAsyncEnumerable<T>
        // where T is a reference type. The IAsyncEnumerable interface has type covariance on its generic
        // type argument, so an IAsyncEnumerable<object> reference can point at any kind of IAsyncEnumerable<T>.
        //
        // We cannot avoid some cast back, as WriteStackFrame has no knowledge of TElement. Casting the type
        // back in here simplifies the code in the caller's TryWrite implementation.

        protected sealed override void StoreEnumerator(ref WriteStack state, IAsyncEnumerator<TElement> enumerator)
        {
            state.Current.AsyncEnumerator = enumerator;
        }

        protected sealed override IAsyncEnumerator<TElement> RetrieveEnumerator(ref WriteStack state)
        {
            if (state.Current.AsyncEnumerator == null)
                throw new InvalidOperationException("Unable to retrieve IAsyncEnumerator<TClass> from write stack because no enumerator is present.");

            return (IAsyncEnumerator<TElement>)state.Current.AsyncEnumerator;
        }

        protected sealed override void ClearStoredEnumerator(ref WriteStack state)
        {
            state.Current.AsyncEnumerator = null;
        }
    }
}
