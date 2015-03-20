// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;


namespace System.Runtime.Serialization
{
    internal class ObjectToIdCache
    {
        internal int m_currentCount;
        internal int[] m_ids;
        internal Object[] m_objs;

        public ObjectToIdCache()
        {
            m_currentCount = 1;
            m_ids = new int[GetPrime(1)];
            m_objs = new Object[m_ids.Length];
        }

        public int GetId(object obj, ref bool newId)
        {
            bool isEmpty;
            int pos = FindElement(obj, out isEmpty);
            if (!isEmpty)
            {
                newId = false;
                return m_ids[pos];
            }
            if (!newId)
                return -1;
            int id = m_currentCount++;
            m_objs[pos] = obj;
            m_ids[pos] = id;
            if (m_currentCount >= (m_objs.Length - 1))
                Rehash();
            return id;
        }


        // (oldObjId, oldObj-id, newObj-newObjId) => (oldObj-oldObjId, newObj-id, newObjId )
        public int ReassignId(int oldObjId, object oldObj, object newObj)
        {
            bool isEmpty;
            int pos = FindElement(oldObj, out isEmpty);
            if (isEmpty)
                return 0;
            int id = m_ids[pos];
            if (oldObjId > 0)
                m_ids[pos] = oldObjId;
            else
                RemoveAt(pos);
            pos = FindElement(newObj, out isEmpty);
            int newObjId = 0;
            if (!isEmpty)
                newObjId = m_ids[pos];
            m_objs[pos] = newObj;
            m_ids[pos] = id;
            return newObjId;
        }

        private int FindElement(object obj, out bool isEmpty)
        {
            int hashcode = RuntimeHelpers.GetHashCode(obj);
            int pos = ((hashcode & 0x7FFFFFFF) % m_objs.Length);
            for (int i = pos; i != (pos - 1); i++)
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
                    i = -1;
            }
            // m_obj must ALWAYS have atleast one slot empty (null).
            DiagnosticUtility.DebugAssert("Object table overflow");
            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ObjectTableOverflow)));
        }

        private void RemoveAt(int pos)
        {
            int hashcode = RuntimeHelpers.GetHashCode(m_objs[pos]);
            for (int i = pos, j; i != (pos - 1); i = j)
            {
                j = (i + 1) % m_objs.Length;
                if (m_objs[j] == null || RuntimeHelpers.GetHashCode(m_objs[j]) != hashcode)
                {
                    m_objs[pos] = m_objs[i];
                    m_ids[pos] = m_ids[i];
                    m_objs[i] = null;
                    m_ids[i] = 0;
                    return;
                }
            }
            // m_obj must ALWAYS have atleast one slot empty (null).
            DiagnosticUtility.DebugAssert("Object table overflow");
            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ObjectTableOverflow)));
        }

        private void Rehash()
        {
            int size = GetPrime(m_objs.Length * 2);
            int[] oldIds = m_ids;
            object[] oldObjs = m_objs;
            m_ids = new int[size];
            m_objs = new Object[size];

            for (int j = 0; j < oldObjs.Length; j++)
            {
                object obj = oldObjs[j];
                if (obj != null)
                {
                    bool found;
                    int pos = FindElement(obj, out found);
                    m_objs[pos] = obj;
                    m_ids[pos] = oldIds[j];
                }
            }
        }

        private static int GetPrime(int min)
        {
            for (int i = 0; i < primes.Length; i++)
            {
                int prime = primes[i];
                if (prime >= min) return prime;
            }

            //outside of our predefined table. 
            //compute the hard way. 
            for (int i = (min | 1); i < Int32.MaxValue; i += 2)
            {
                if (IsPrime(i))
                    return i;
            }
            return min;
        }

        private static bool IsPrime(int candidate)
        {
            if ((candidate & 1) != 0)
            {
                int limit = (int)Math.Sqrt(candidate);
                for (int divisor = 3; divisor <= limit; divisor += 2)
                {
                    if ((candidate % divisor) == 0)
                        return false;
                }
                return true;
            }
            return (candidate == 2);
        }

        /// <SecurityNote>
        /// Review - Static fields are marked SecurityCritical or readonly to prevent
        ///          data from being modified or leaked to other components in appdomain.
        /// </SecurityNote>
        internal static readonly int[] primes =
        {
            3, 7, 17, 37, 89, 197, 431, 919, 1931, 4049, 8419, 17519, 36353,
            75431, 156437, 324449, 672827, 1395263, 2893249, 5999471,
        };
    }
}

