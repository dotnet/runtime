using System.Collections.Generic;
using System.Collections;

#if !ES_BUILD_AGAINST_DOTNET_V35
using Contract = System.Diagnostics.Contracts.Contract;
#else
using Contract = Microsoft.Diagnostics.Contracts.Internal.Contract;
#endif

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// EventPayload class holds the list of parameters and their corresponding values for user defined types passed to 
    /// EventSource APIs.
    /// Preserving the order of the elements as they were found inside user defined types is the most important characteristic of this class.
    /// </summary>
    internal class EventPayload : IDictionary<string, object>
    {
        internal EventPayload(List<string> payloadNames, List<object> payloadValues) 
        {
            Contract.Assert(payloadNames.Count == payloadValues.Count);

            m_names = payloadNames;
            m_values = payloadValues;
        }

        public ICollection<string> Keys { get { return m_names; } }
        public ICollection<object> Values { get { return m_values; } }

        public object this[string key]
        {
            get
            {
                if (key == null)
                    throw new System.ArgumentNullException("key");

                int position = 0;
                foreach(var name in m_names)
                { 
                    if (name == key)
                    {
                        return m_values[position];
                    }
                    position++;
                }

                throw new System.Collections.Generic.KeyNotFoundException();
            }
            set
            {
                throw new System.NotSupportedException();
            }
        }

        public void Add(string key, object value)
        {
            throw new System.NotSupportedException();
        }

        public void Add(KeyValuePair<string, object> payloadEntry)
        {
            throw new System.NotSupportedException();
        }

        public void Clear()
        {
            throw new System.NotSupportedException();
        }

        public bool Contains(KeyValuePair<string, object> entry)
        {
            return ContainsKey(entry.Key);
        }

        public bool ContainsKey(string key)
        {
            if (key == null)
                throw new System.ArgumentNullException("key");

            foreach (var item in m_names)
            {
                if (item == key)
                    return true;
            }
            return false;
        }

        public int Count { get { return m_names.Count; } }

        public bool IsReadOnly { get { return true; } }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            throw new System.NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            var instance = this as IEnumerable<KeyValuePair<string, object>>;
            return instance.GetEnumerator();
        }

        public void CopyTo(KeyValuePair<string, object>[] payloadEntries, int count)
        {
            throw new System.NotSupportedException();
        }
       
        public bool Remove(string key)
        {
            throw new System.NotSupportedException();
        }

        public bool Remove(KeyValuePair<string, object> entry)
        {
            throw new System.NotSupportedException();
        }
       
        public bool TryGetValue(string key, out object value)
        {
            if (key == null)
                throw new System.ArgumentNullException("key");

            int position = 0;
            foreach (var name in m_names)
            {
                if (name == key)
                {
                    value =  m_values[position];
                    return true;
                }
                position++;
            }

            value = default(object);
            return false;
        }

        #region private
        private List<string> m_names;
        private List<object> m_values;
        #endregion
    }
}
