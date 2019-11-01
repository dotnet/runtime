// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Tests;
using Xunit;

namespace System.Collections.Tests
{
    public abstract partial class IEnumerable_NonGeneric_Tests : TestBase
    {
        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_SerializeDeserialize(int count)
        {
            IEnumerable expected = NonGenericIEnumerableFactory(count);
            if (!SupportsSerialization)
            {
                // No assert for !IsSerializable here, as some collections are only serializable sometimes,
                // e.g. HybridDictionary.Keys is serializable when using Hashtable.Keys but not when
                // using ListDictionary.Keys.
                return;
            }

            IEnumerable actual = BinaryFormatterHelpers.Clone(expected);
            if (Order == EnumerableOrder.Sequential)
            {
                Assert.Equal(expected, actual);
            }
            else
            {
                var expectedSet = new HashSet<object>(expected.Cast<object>());
                var actualSet = new HashSet<object>(actual.Cast<object>());
                Assert.Subset(expectedSet, actualSet);
                Assert.Subset(actualSet, expectedSet);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void CloneEnumerator_MatchesOriginal(int count)
        {
            IEnumerator e1 = NonGenericIEnumerableFactory(count).GetEnumerator();

            ICloneable c1 = e1 as ICloneable;
            if (c1 == null)
            {
                // Only test if the enumerator can be cloned
                return;
            }

            // Walk the enumerator, and at each step of the way, clone it.
            // For all cloned enumerators, make sure they match the original
            // for the remainder of the iteration.
            var enumerators = new List<IEnumerator>();
            while (e1.MoveNext())
            {
                foreach (IEnumerator e2 in enumerators)
                {
                    Assert.True(e2.MoveNext(), "Could not MoveNext the enumerator");
                    Assert.Equal(e1.Current, e2.Current);
                }

                if (enumerators.Count < 10) // arbitrary limit to time-consuming N^2 behavior
                {
                    enumerators.Add((IEnumerator)c1.Clone());
                }
            }
            foreach (IEnumerator e2 in enumerators)
            {
                Assert.False(e2.MoveNext(), "Expected to not be able to MoveNext the enumerator");
            }
        }
    }
}
