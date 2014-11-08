// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Validation;

namespace System.Collections.Immutable
{
    internal class DictionaryEnumerator<TKey, TValue> : IDictionaryEnumerator
    {
        private readonly IEnumerator<KeyValuePair<TKey, TValue>> inner;

        internal DictionaryEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> inner)
        {
            Requires.NotNull(inner, "inner");

            this.inner = inner;
        }

        public DictionaryEntry Entry
        {
            get { return new DictionaryEntry(this.inner.Current.Key, this.inner.Current.Value); }
        }

        public object Key
        {
            get { return this.inner.Current.Key; }
        }

        public object Value
        {
            get { return this.inner.Current.Value; }
        }

        public object Current
        {
            get { return this.Entry; }
        }

        public bool MoveNext()
        {
            return this.inner.MoveNext();
        }

        public void Reset()
        {
            this.inner.Reset();
        }
    }
}
