// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.UnitTesting
{    
    public static class EnumerableAssert
    {
        public static void AreEqual<T>(IEnumerable<T> actual, params T[] expected)
        {
            AreEqual<T>((IEnumerable<T>)expected, (IEnumerable<T>)actual);
        }

        private static void AreEqual<T>(IEnumerable expected, IList<T> actual)
        {
            foreach (object value in expected)
            {
                bool removed = actual.Remove((T)value);

                Assert.True(removed);
            }

            Assert.Equal(0, actual.Count);
        }

        public static void AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            // First, test the IEnumerable implementation
            Assert.Equal(expected.Count(), actual.Count());
            AreEqual((IEnumerable)expected, actual.ToList());

            // Second, test the IEnumerable<T> implementation
            Assert.Equal(expected.Count(), actual.Count());

            List<T> actualList = actual.ToList();

            foreach (T value in expected)
            {
                bool removed = actualList.Remove(value);

                Assert.True(removed);
            }

            Assert.Equal(0, actualList.Count);
        }
        
        public static void AreEqual<TKey, TValue>(IDictionary<TKey, TValue> expected, IDictionary<TKey, TValue> actual)
        {
            Assert.Equal(expected.Count, actual.Count);

            foreach (KeyValuePair<TKey, TValue> kvp in expected)
            {
                TValue firstValue = kvp.Value;
                TValue secondValue = default(TValue);
                if (!actual.TryGetValue(kvp.Key, out secondValue))
                {
                    throw new NotImplementedException();
                }

                if ((firstValue is IDictionary<TKey, TValue>) && (secondValue is IDictionary<TKey, TValue>))
                {
                    AreEqual((IDictionary<TKey, TValue>)firstValue, (IDictionary<TKey, TValue>)secondValue);
                    continue;
                }

                Assert.Equal(kvp.Value, secondValue);
            }
        }
    }
}
