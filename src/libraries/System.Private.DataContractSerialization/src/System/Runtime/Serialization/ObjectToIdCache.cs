// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Runtime.Serialization
{
    internal sealed class ObjectToIdCache
    {
        internal int m_currentCount;
        internal int[] m_ids;
        internal object?[] m_objs;

        public ObjectToIdCache()
        {
            m_currentCount = 1;
            m_ids = new int[GetPrime(1)];
            m_objs = new object[m_ids.Length];
        }

        public int GetId(object obj, ref bool newId)
        {
            bool isEmpty;
            int position = FindElement(obj, out isEmpty);
            if (!isEmpty)
            {
                newId = false;
                return m_ids[position];
            }
            if (!newId)
                return -1;

            int id = m_currentCount++;
            m_objs[position] = obj;
            m_ids[position] = id;
            if (m_currentCount >= (m_objs.Length - 1))
                Rehash();
            return id;
        }

        // (oldObjId, oldObj-id, newObj-newObjId) => (oldObj-oldObjId, newObj-id, newObjId )
        public int ReassignId(int oldObjId, object oldObj, object newObj)
        {
            bool isEmpty;
            int position = FindElement(oldObj, out isEmpty);
            if (isEmpty)
                return 0;
            int id = m_ids[position];
            if (oldObjId > 0)
                m_ids[position] = oldObjId;
            else
                RemoveAt(position);
            position = FindElement(newObj, out isEmpty);
            int newObjId = 0;
            if (!isEmpty)
                newObjId = m_ids[position];
            m_objs[position] = newObj;
            m_ids[position] = id;
            return newObjId;
        }

        private int FindElement(object obj, out bool isEmpty)
        {
            int position = ComputeStartPosition(obj);
            for (int i = position; i != (position - 1); i++)
            {
                if (m_objs[i] == null)
                {
                    isEmpty = true;
                    return i;
                }
                if (m_objs[i] == obj)
                {
                    isEmpty = false;
                    return i;
                }
                if (i == (m_objs.Length - 1))
                {
                    i = -1;
                }
            }
            // m_obj must ALWAYS have at least one slot empty (null).
            Debug.Fail("Object table overflow");
            throw XmlObjectSerializer.CreateSerializationException(SR.ObjectTableOverflow);
        }

        private void RemoveAt(int position)
        {
            int cacheSize = m_objs.Length;
            int lastVacantPosition = position;
            for (int next = (position == cacheSize - 1) ? 0 : position + 1; next != position; next++)
            {
                if (m_objs[next] == null)
                {
                    m_objs[lastVacantPosition] = null;
                    m_ids[lastVacantPosition] = 0;
                    return;
                }
                int nextStartPosition = ComputeStartPosition(m_objs[next]);
                // Determine whether the element at 'next' should be moved to fill the vacancy at 'lastVacantPosition'.
                // An element with home position h at slot 'next' must be moved when the probe sequence from h to 'next'
                // passes through 'lastVacantPosition' (i.e., 'lastVacantPosition' is in the circular range [h, next]).
                bool shouldMove = (lastVacantPosition <= next)
                    ? (nextStartPosition <= lastVacantPosition || nextStartPosition > next)
                    : (nextStartPosition <= lastVacantPosition && nextStartPosition > next);
                if (shouldMove)
                {
                    m_objs[lastVacantPosition] = m_objs[next];
                    m_ids[lastVacantPosition] = m_ids[next];
                    lastVacantPosition = next;
                }
                if (next == (cacheSize - 1))
                {
                    next = -1;
                }
            }
            // m_obj must ALWAYS have at least one slot empty (null).
            Debug.Fail("Object table overflow");
            throw XmlObjectSerializer.CreateSerializationException(SR.ObjectTableOverflow);
        }

        private int ComputeStartPosition(object? o)
        {
            return (RuntimeHelpers.GetHashCode(o) & 0x7FFFFFFF) % m_objs.Length;
        }

        private void Rehash()
        {
            int size = GetPrime(m_objs.Length + 1); // The lookup does an inherent doubling
            int[] oldIds = m_ids;
            object?[] oldObjs = m_objs;
            m_ids = new int[size];
            m_objs = new object[size];

            for (int j = 0; j < oldObjs.Length; j++)
            {
                object? obj = oldObjs[j];
                if (obj != null)
                {
                    int position = FindElement(obj, out _);
                    m_objs[position] = obj;
                    m_ids[position] = oldIds[j];
                }
            }
        }

        private static int GetPrime(int min)
        {
            ReadOnlySpan<int> primes =
            [
                3, 7, 17, 37, 89, 197, 431, 919, 1931, 4049, 8419, 17519, 36353,
                75431, 156437, 324449, 672827, 1395263, 2893249, 5999471,
                11998949, 23997907, 47995853, 95991737, 191983481, 383966977, 767933981, 1535867969,
                2146435069, 0x7FFFFFC7
                // 0x7FFFFFC7 == Array.MaxLength is not prime, but it is the largest possible array size.
                // There's nowhere to go from here. Using a const rather than the MaxLength property
                // so that the array contains only const values.
            ];

            foreach (int prime in primes)
            {
                if (prime >= min)
                {
                    return prime;
                }
            }

            return min;
        }
    }
}
