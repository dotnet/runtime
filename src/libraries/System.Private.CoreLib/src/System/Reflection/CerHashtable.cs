// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Threading;

namespace System.Reflection
{
    // Reliable hashtable thread safe for multiple readers and single writer. Note that the reliability goes together with thread
    // safety. Thread safety for multiple readers requires atomic update of the state that also makes the table
    // reliable in the presence of asynchronous exceptions.
    internal struct CerHashtable<K, V> where K : class
    {
        private sealed class Table
        {
            // Note that m_keys and m_values arrays are immutable to allow lock-free reads. A new instance
            // of CerHashtable has to be allocated to grow the size of the hashtable.
            internal K[] m_keys;
            internal V[] m_values;
            internal int m_count;

            internal Table(int size)
            {
                size = HashHelpers.GetPrime(size);
                m_keys = new K[size];
                m_values = new V[size];
            }

            internal void Insert(K key, V value)
            {
                int hashcode = GetHashCodeHelper(key);
                if (hashcode < 0)
                    hashcode = ~hashcode;

                K[] keys = m_keys;
                int index = hashcode % keys.Length;

                while (true)
                {
                    K hit = keys[index];

                    if (hit == null)
                    {
                        m_count++;
                        m_values[index] = value;

                        // This volatile write has to be last. It is going to publish the result atomically.
                        //
                        // Note that incrementing the count or setting the value does not do any harm without setting the key. The inconsistency will be ignored
                        // and it will go away completely during next rehash.
                        Volatile.Write(ref keys[index], key);

                        break;
                    }
                    else
                    {
                        Debug.Assert(!hit.Equals(key), "Key was already in CerHashtable!  Potential race condition (or bug) in the Reflection cache?");

                        index++;
                        if (index >= keys.Length)
                            index -= keys.Length;
                    }
                }
            }
        }

        private Table m_Table;

        private const int MinSize = 7;

        private static int GetHashCodeHelper(K key)
        {
            // For strings we don't want the key to differ across domains as CerHashtable might be shared.
            if (key is not string sKey)
            {
                return key.GetHashCode();
            }
            else
            {
                return sKey.GetNonRandomizedHashCode();
            }
        }

        private void Rehash(int newSize)
        {
            Table newTable = new Table(newSize);

            Table oldTable = m_Table;
            if (oldTable != null)
            {
                K[] keys = oldTable.m_keys;
                V[] values = oldTable.m_values;

                for (int i = 0; i < keys.Length; i++)
                {
                    K key = keys[i];

                    if (key != null)
                    {
                        newTable.Insert(key, values[i]);
                    }
                }
            }

            // Publish the new table atomically
            Volatile.Write(ref m_Table, newTable);
        }

        internal V this[K key]
        {
            get
            {
                Table table = Volatile.Read(ref m_Table);
                if (table == null)
                    return default!;

                int hashcode = GetHashCodeHelper(key);
                if (hashcode < 0)
                    hashcode = ~hashcode;

                K[] keys = table.m_keys;
                int index = hashcode % keys.Length;

                while (true)
                {
                    // This volatile read has to be first. It is reading the atomically published result.
                    K hit = Volatile.Read(ref keys[index]);

                    if (hit != null)
                    {
                        if (hit.Equals(key))
                            return table.m_values[index];

                        index++;
                        if (index >= keys.Length)
                            index -= keys.Length;
                    }
                    else
                    {
                        return default!;
                    }
                }
            }
            set
            {
                Table table = m_Table;

                if (table != null)
                {
                    int requiredSize = 2 * (table.m_count + 1);
                    if (requiredSize >= table.m_keys.Length)
                        Rehash(requiredSize);
                }
                else
                {
                    Rehash(MinSize);
                }

                m_Table.Insert(key, value);
            }
        }

        public unsafe V GetValue<TAlternativeKey>(int hashcode, in TAlternativeKey alternative, delegate*<in TAlternativeKey, K, bool> equals) where TAlternativeKey : allows ref struct
        {
            Table table = m_Table;
            if (table is null)
                return default!;
            if (hashcode < 0)
                hashcode = ~hashcode;
            K[] keys = table.m_keys;
            int index = hashcode % keys.Length;
            while (true)
            {
                K hit = Volatile.Read(ref keys[index]);
                if (hit != null)
                {
                    if (equals(alternative, hit))
                        return table.m_values[index];
                    index++;
                    if (index >= keys.Length)
                        index -= keys.Length;
                }
                else
                {
                    return default!;
                }
            }
        }
    }
}
