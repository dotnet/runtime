// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Interface:  KeyValuePair
** 
** 
**
**
** Purpose: Generic key-value pair for dictionary enumerators.
**
** 
===========================================================*/
namespace System.Collections.Generic {
    
    using System;
    using System.ComponentModel;
    using System.Text;

    // Provides the Create factory method for KeyValuePair<TKey, TValue>.
    public static class KeyValuePair
    {
        // Creates a new KeyValuePair<TKey, TValue> from the given values.
        public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)
        {
            return new KeyValuePair<TKey, TValue>(key, value);
        }
    }

    // A KeyValuePair holds a key and a value from a dictionary.
    // It is used by the IEnumerable<T> implementation for both IDictionary<TKey, TValue>
    // and IReadOnlyDictionary<TKey, TValue>.
    [Serializable]
    public struct KeyValuePair<TKey, TValue> {
        private TKey key;
        private TValue value;

        public KeyValuePair(TKey key, TValue value) {
            this.key = key;
            this.value = value;
        }

        public TKey Key {
            get { return key; }
        }

        public TValue Value {
            get { return value; }
        }

        public override string ToString() {
            StringBuilder s = StringBuilderCache.Acquire();
            s.Append('[');
            if( Key != null) {
                s.Append(Key.ToString());
            }
            s.Append(", ");
            if( Value != null) {
               s.Append(Value.ToString());
            }
            s.Append(']');
            return StringBuilderCache.GetStringAndRelease(s);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Deconstruct(out TKey key, out TValue value)
        {
            key = Key;
            value = Value;
        }
    }
}
