// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Validation;

namespace System.Collections.Immutable
{
    internal class DictionaryEnumerator<TKey, TValue> : IDictionaryEnumerator
    {
        private readonly IEnumerator<KeyValuePair<TKey, TValue>> _inner;

        internal DictionaryEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> inner)
        {
            Requires.NotNull(inner, "inner");

            _inner = inner;
        }

        public DictionaryEntry Entry
        {
            get { return new DictionaryEntry(_inner.Current.Key, _inner.Current.Value); }
        }

        public object Key
        {
            get { return _inner.Current.Key; }
        }

        public object Value
        {
            get { return _inner.Current.Value; }
        }

        public object Current
        {
            get { return this.Entry; }
        }

        public bool MoveNext()
        {
            return _inner.MoveNext();
        }

        public void Reset()
        {
            _inner.Reset();
        }
    }
}
