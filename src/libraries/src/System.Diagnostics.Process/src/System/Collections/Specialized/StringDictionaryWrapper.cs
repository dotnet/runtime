// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Collections.Specialized
{
    // This class is an internal class used by System.Diagnostics.Proccess on property EnvironmentVariables which returns an StringDictionary. Since we need
    // EnvironmentVariables to return a StringDictionary, this is a wrapper to the Environment property in order to get the same comparer behavior on both properties.

    internal sealed class StringDictionaryWrapper : StringDictionary
    {
        private readonly DictionaryWrapper _contents;

        public StringDictionaryWrapper(DictionaryWrapper contents)
        {
            _contents = contents;
        }

        public override string this[string key]
        {
            get { return _contents[key]; }
            set { _contents[key] = value; }
        }

        public override int Count => _contents.Count;

        public override ICollection Keys => ((IDictionary)_contents).Keys;
        public override ICollection Values => ((IDictionary)_contents).Values;

        public override bool IsSynchronized => false;

        public override object SyncRoot => _contents.SyncRoot;

        public override void Add(string key, string value)
        {
            if (_contents.ContainsKey(key))
            {
                throw new ArgumentException();
            }

            _contents.Add(key, value);
        }

        public override void Clear() => _contents.Clear();

        public override bool ContainsKey(string key) => _contents.ContainsKey(key);

        public override bool ContainsValue(string value) => _contents.ContainsValue(value);

        public override void CopyTo(Array array, int index) => _contents.CopyTo(array, index);

        public override IEnumerator GetEnumerator()
        {
            foreach (KeyValuePair<string, string> keyValuePair in _contents)
            {
                yield return new DictionaryEntry(keyValuePair.Key, keyValuePair.Value);
            }
        }

        public override void Remove(string key) => _contents.Remove(key);
    }
}