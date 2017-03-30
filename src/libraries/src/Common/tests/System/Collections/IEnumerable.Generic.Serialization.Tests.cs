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
    public abstract partial class IEnumerable_Generic_Tests<T> : TestBase<T>
    {
        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_SerializeDeserialize(int count)
        {
            IEnumerable<T> expected = GenericIEnumerableFactory(count);
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, expected);
                ms.Position = 0;
                IEnumerable<T> actual = (IEnumerable<T>)bf.Deserialize(ms);

                if (Order == EnumerableOrder.Sequential)
                {
                    Assert.Equal(expected, actual);
                }
                else
                {
                    var expectedSet = new HashSet<T>(expected);
                    var actualSet = new HashSet<T>(actual);
                    Assert.Subset(expectedSet, actualSet);
                    Assert.Subset(actualSet, expectedSet);
                }
            }
        }
    }
}
