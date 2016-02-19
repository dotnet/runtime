// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Specialized;

namespace System.Net
{
    // 
    // This internal class implements a static multi-threaded dictionary for user-registered SPNs.
    // An SPN is mapped based on a Uri prefix that contains scheme, host and port.
    //

    // TODO #3114: Replace by Dictionary or ConcurrentDictionary
    internal class SpnDictionary : StringDictionary
    {
        // A Hashtable can support one writer and multiple readers concurrently.
        // Maps Uri keys to SpnToken values. The SpnTokens should not be exposed publicly.
        private Hashtable _syncTable = Hashtable.Synchronized(new Hashtable());
        private ValueCollection _valuesWrapper;

        public override int Count
        {
            get
            {
                return _syncTable.Count;
            }
        }

        //
        // We are thread safe
        //
        public override bool IsSynchronized
        {
            get
            {
                return true;
            }
        }
        
        //
        // Internal lookup, bypasses security checks
        //
        internal SpnToken InternalGet(string canonicalKey)
        {
            int lastLength = 0;
            string key = null;

            // This lock is required to avoid getting InvalidOperationException
            // because the collection was modified during enumeration. By design 
            // a Synchronized Hashtable throws if modifications occur while an 
            // enumeration is in progress. Manually locking the Hashtable to 
            // prevent modification during enumeration is the best solution. 
            // Catching the exception and retrying could potentially never
            // succeed in the face of significant updates.
            lock (_syncTable.SyncRoot)
            {
                foreach (object o in _syncTable.Keys)
                {
                    string s = (string)o;
                    if (s != null && s.Length > lastLength)
                    {
                        if (String.Compare(s, 0, canonicalKey, 0, s.Length, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            lastLength = s.Length;
                            key = s;
                        }
                    }
                }
            }

            return (key != null) ? (SpnToken)_syncTable[key] : null;
        }

        internal void InternalSet(string canonicalKey, SpnToken spnToken)
        {
            _syncTable[canonicalKey] = spnToken;
        }

        //
        // Public lookup method
        //
        public override string this[string key]
        {
            get
            {
                key = GetCanonicalKey(key);
                SpnToken token = InternalGet(key);
                return (token == null ? null : token.Spn);
            }
            set
            {
                key = GetCanonicalKey(key);
                // Value may be null
                InternalSet(key, new SpnToken(value));
            }
        }

        public override ICollection Keys
        {
            get
            {
                return _syncTable.Keys;
            }
        }

        public override object SyncRoot
        {
            get
            {
                return _syncTable;
            }
        }

        public override ICollection Values
        {
            get
            {
                if (_valuesWrapper == null)
                {
                    _valuesWrapper = new ValueCollection(this);
                }

                return _valuesWrapper;
            }
        }

        public override void Add(string key, string value)
        {
            key = GetCanonicalKey(key);
            _syncTable.Add(key, new SpnToken(value));
        }

        public override void Clear()
        {
            _syncTable.Clear();
        }

        public override bool ContainsKey(string key)
        {
            key = GetCanonicalKey(key);
            return _syncTable.ContainsKey(key);
        }

        public override bool ContainsValue(string value)
        {
            foreach (SpnToken spnToken in _syncTable.Values)
            {
                if (spnToken.Spn == value)
                {
                    return true;
                }
            }
            return false;
        }

        // We have to unwrap the SpnKey and just expose the Spn
        public override void CopyTo(Array array, int index)
        {
            CheckCopyToArguments(array, index, Count);

            int offset = 0;
            foreach (object entry in this)
            {
                array.SetValue(entry, offset + index);
                offset++;
            }
        }

        public override IEnumerator GetEnumerator()
        {
            foreach (string key in _syncTable.Keys)
            {
                // We must unwrap the SpnToken and not expose it publicly
                SpnToken spnToken = (SpnToken)_syncTable[key];
                yield return new DictionaryEntry(key, spnToken.Spn);
            }
        }

        public override void Remove(string key)
        {
            key = GetCanonicalKey(key);
            _syncTable.Remove(key);
        }

        //
        // We want to serialize on updates on one thread.
        //
        private static string GetCanonicalKey(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            try
            {
                Uri uri = new Uri(key);
                key = uri.GetComponents(UriComponents.Scheme | UriComponents.Host | UriComponents.Port | UriComponents.Path, UriFormat.SafeUnescaped);
            }
            catch (UriFormatException e)
            {
                throw new ArgumentException(SR.Format(SR.net_mustbeuri, "key"), nameof(key), e);
            }

            return key;
        }

        private static void CheckCopyToArguments(Array array, int index, int count)
        {
            // Copied from HashTable.CopyTo
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if ((array.Length - index) < count)
            {
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);
            }
        }

        // Wrap HashTable.Values so we can unwrap the SpnTokens.
        private class ValueCollection : ICollection
        {
            private SpnDictionary _spnDictionary;

            internal ValueCollection(SpnDictionary spnDictionary)
            {
                _spnDictionary = spnDictionary;
            }

            public void CopyTo(Array array, int index)
            {
                CheckCopyToArguments(array, index, Count);

                int offset = 0;
                foreach (object entry in this)
                {
                    array.SetValue(entry, offset + index);
                    offset++;
                }
            }

            public int Count
            {
                get { return _spnDictionary._syncTable.Values.Count; }
            }

            public bool IsSynchronized
            {
                get { return true; }
            }

            public object SyncRoot
            {
                get { return _spnDictionary._syncTable.SyncRoot; }
            }

            public IEnumerator GetEnumerator()
            {
                foreach (SpnToken spnToken in _spnDictionary._syncTable.Values)
                {
                    yield return (spnToken != null ? spnToken.Spn : null);
                }
            }
        }
    }

    internal class SpnToken
    {
        private readonly string _spn;
        
        internal string Spn
        {
            get { return _spn; }
        }

        internal SpnToken(string spn)
        {
            _spn = spn;
        }
    }
}
