// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class ComponentCollectionTests
    {
        public static IEnumerable<object[]> Components_TestData()
        {
            yield return new object[] { new IComponent[0] };
            yield return new object[] { new IComponent[] { new Component(), new Component() } };
        }

        [Theory]
        [MemberData(nameof(Components_TestData))]
        public void Ctor_Components(IComponent[] components)
        {
            var collection = new ComponentCollection(components);
            Assert.Equal(components.Length, collection.Count);

            for (int i = 0; i < collection.Count; i++)
            {
                Assert.Equal(components[i], collection[i]);
            }
        }

        public static IEnumerable<object[]> Indexer_Name_TestData()
        {
            var namedComponent = new Component { Site = new MockSite { Name = "Name" } };

            yield return new object[] { new IComponent[0], null, null };
            yield return new object[] { new IComponent[0], "name", null };
            yield return new object[] { new IComponent[] { new Component() }, "name", null };
            yield return new object[] { new IComponent[] { new Component { Site = new MockSite() } }, "name", null };
            yield return new object[] { new IComponent[] { namedComponent }, "Name", namedComponent };
            yield return new object[] { new IComponent[] { namedComponent }, "name", namedComponent };
            yield return new object[] { new IComponent[] { namedComponent }, "nosuchname", null };
        }

        [Theory]
        [MemberData(nameof(Indexer_Name_TestData))]
        public void Indexer_Name_ReturnsExpected(IComponent[] components, string name, IComponent expected)
        {
            var collection = new ComponentCollection(components);
            Assert.Equal(expected, collection[name]);
        }

        [Theory]
        [MemberData(nameof(Components_TestData))]
        public void CopyTo_ValidArray_Success(IComponent[] components)
        {
            var collection = new ComponentCollection(components);
            IComponent[] array = new IComponent[collection.Count + 2];
            collection.CopyTo(array, 1);

            Assert.Null(array[0]);
            for (int i = 0; i < components.Length; i++)
            {
                Assert.Equal(components[i], array[i + 1]);
            }
            Assert.Null(array[array.Length - 1]);
        }
    }
}
