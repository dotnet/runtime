// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
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
            Assert.True(expected.GetType().GetTypeInfo().IsSerializable, "Expected IsSerializable");

            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, expected);
                ms.Position = 0;
                IEnumerable actual = (IEnumerable)bf.Deserialize(ms);

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
        }
    }
}
