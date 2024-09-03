// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Speech.Internal;

namespace System.Speech.Recognition
{
    [Serializable]
    [DebuggerDisplay("{_keyName} = {Value}, Children = {_dictionary.Count}")]
    [DebuggerTypeProxy(typeof(SemanticValueDebugDisplay))]
    public sealed class SemanticValue : IDictionary<string, SemanticValue>
    {
        #region Constructors

#pragma warning disable 6504, 56507
        public SemanticValue(string keyName, object value, float confidence)
        {
            Helpers.ThrowIfNull(keyName, nameof(keyName));

            _dictionary = new Dictionary<string, SemanticValue>();
            _confidence = confidence;
            _keyName = keyName;
            _value = value;
        }
#pragma warning restore 6504, 56507
        public SemanticValue(object value)
            : this(string.Empty, value, -1f)
        {
        }

        #endregion

        #region Public Methods
        public override bool Equals(object obj)
        {
            SemanticValue refObj = obj as SemanticValue;
            if (refObj == null || refObj.Count != Count || refObj.Value == null && Value != null || (refObj.Value != null && !refObj.Value.Equals(Value)))
            {
                return false;
            }

            foreach (KeyValuePair<string, SemanticValue> kv in _dictionary)
            {
                if (!refObj.ContainsKey(kv.Key) || !refObj[kv.Key].Equals(this[kv.Key]))
                {
                    return false;
                }
            }
            return true;
        }
        public override int GetHashCode()
        {
            return Count;
        }

        #endregion

        #region Public Properties

        // The value returned from the script / tags etc.
        // This can be cast to a more useful type {currently it will be string or int until we have .NET grammars}.
        public object Value
        {
            get
            {
                return _value;
            }
            internal set
            {
                _value = value;
            }
        }

        // Confidence score associated with the semantic item.
        public float Confidence
        {
            get
            {
                return _confidence;
            }
        }

        #endregion

        #region IDictionary implementation
        // Expose the common query methods directly.
        public SemanticValue this[string key]
        {
            get { return _dictionary[key]; }
            set { throw new InvalidOperationException(SR.Get(SRID.CollectionReadOnly)); }
        }
        public bool Contains(KeyValuePair<string, SemanticValue> item)
        {
            return (_dictionary.ContainsKey(item.Key) && _dictionary.ContainsValue(item.Value));
        }
        public bool ContainsKey(string key)
        {
            return _dictionary.ContainsKey(key);
        }
        public int Count
        {
            get { return _dictionary.Count; }
        }

        // Other less common methods on IDictionary are also hidden from intellisense.

        // Read-only collection so throw on these methods. Also make then hidden through explicit interface declaration.
        void ICollection<KeyValuePair<string, SemanticValue>>.Add(KeyValuePair<string, SemanticValue> key)
        {
            throw new NotSupportedException(SR.Get(SRID.CollectionReadOnly));
        }

        void IDictionary<string, SemanticValue>.Add(string key, SemanticValue value)
        {
            throw new NotSupportedException(SR.Get(SRID.CollectionReadOnly));
        }

        void ICollection<KeyValuePair<string, SemanticValue>>.Clear()
        {
            throw new NotSupportedException(SR.Get(SRID.CollectionReadOnly));
        }

        bool ICollection<KeyValuePair<string, SemanticValue>>.Remove(KeyValuePair<string, SemanticValue> key)
        {
            throw new NotSupportedException(SR.Get(SRID.CollectionReadOnly));
        }

        bool IDictionary<string, SemanticValue>.Remove(string key)
        {
            throw new NotSupportedException(SR.Get(SRID.CollectionReadOnly));
        }
        void ICollection<KeyValuePair<string, SemanticValue>>.CopyTo(KeyValuePair<string, SemanticValue>[] array, int index)
        {
            ((ICollection<KeyValuePair<string, SemanticValue>>)_dictionary).CopyTo(array, index);
        }
        IEnumerator<KeyValuePair<string, SemanticValue>> IEnumerable<KeyValuePair<string, SemanticValue>>.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        bool ICollection<KeyValuePair<string, SemanticValue>>.IsReadOnly
        {
            get { return true; }
        }

        ICollection<string> IDictionary<string, SemanticValue>.Keys
        {
            get { return _dictionary.Keys; }
        }

        ICollection<SemanticValue> IDictionary<string, SemanticValue>.Values
        {
            get { return _dictionary.Values; }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, SemanticValue>>)this).GetEnumerator();
        }

        bool IDictionary<string, SemanticValue>.TryGetValue(string key, out SemanticValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        #endregion

        #region Internal Properties

        internal string KeyName
        {
            get
            {
                return _keyName;
            }
        }

        #endregion

        #region Internal Fields

        internal Dictionary<string, SemanticValue> _dictionary;
        internal bool _valueFieldSet;

        #endregion

        #region Private Fields

        // Used by the debugger display attribute
        private string _keyName;
        private float _confidence;
        private object _value;

        #endregion

        #region Private Types

        // Used by the debugger display attribute
        internal class SemanticValueDebugDisplay
        {
            public SemanticValueDebugDisplay(SemanticValue value)
            {
                _value = value.Value;
                _dictionary = value._dictionary;
                _name = value.KeyName;
                _confidence = value.Confidence;
            }

            public object Value
            {
                get
                {
                    return _value;
                }
            }

            public object Count
            {
                get
                {
                    return _dictionary.Count;
                }
            }

            public object KeyName
            {
                get
                {
                    return _name;
                }
            }

            public object Confidence
            {
                get
                {
                    return _confidence;
                }
            }
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public SemanticValue[] AKeys
            {
                get
                {
                    SemanticValue[] keys = new SemanticValue[_dictionary.Count];
                    int i = 0;
                    foreach (KeyValuePair<string, SemanticValue> kv in _dictionary)
                    {
                        keys[i++] = kv.Value;
                    }
                    return keys;
                }
            }

            private object _name;
            private object _value;
            private float _confidence;
            private Dictionary<string, SemanticValue> _dictionary;
        }

        #endregion
    }
}
