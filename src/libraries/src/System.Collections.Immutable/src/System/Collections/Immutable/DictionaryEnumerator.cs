// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Collections.Immutable
{
    internal sealed class DictionaryEnumerator<TKey, TValue> : IDictionaryEnumerator
    {
        private readonly IEnumerator<KeyValuePair<TKey, TValue>> _inner;

        internal DictionaryEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> inner)
        {
            Requires.NotNull(inner, nameof(inner));

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
