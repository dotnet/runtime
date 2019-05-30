// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public class DesignerCollectionTests
    {
        public static IEnumerable<object[]> Ctor_TestData()
        {
            if (!PlatformDetection.IsFullFramework)
            {
                yield return new object[] { null };
            }

            yield return new object[] { new IDesignerHost[] { new TestDesignerHost(), null, new TestDesignerHost() } };
        }

        [Theory]
        [MemberData(nameof(Ctor_TestData))]
        public void Ctor_DesignersArray(IDesignerHost[] designers)
        {
            var collection = new DesignerCollection(designers);
            Assert.Equal(designers?.Length ?? 0, collection.Count);
            Assert.Equal(designers ?? Enumerable.Empty<IDesignerHost>(), collection.Cast<IDesignerHost>());
        }

        [Theory]
        [MemberData(nameof(Ctor_TestData))]
        public void Ctor_DesignersIList(IDesignerHost[] designers)
        {
            var collection = new DesignerCollection((IList)designers);
            Assert.Equal(designers?.Length ?? 0, collection.Count);
            Assert.Equal(designers ?? Enumerable.Empty<IDesignerHost>(), collection.Cast<IDesignerHost>());
        }

        [Fact]
        public void ICollection_PropertiesAndMethods_ReturnsExpected()
        {
            ICollection collection = new DesignerCollection(new TestDesignerHost[1]);
            Assert.Equal(1, collection.Count);
            Assert.Equal(1, collection.Cast<IDesignerHost>().Count());

            Assert.Null(collection.SyncRoot);
            Assert.False(collection.IsSynchronized);
        }

        [Fact]
        public void Indexer_Get_ReturnsExpected()
        {
            var designers = new IDesignerHost[] { new TestDesignerHost(), new TestDesignerHost() };
            var collection = new DesignerCollection(designers);
            Assert.Same(designers[0], collection[0]);
        }

        [Fact]
        public void CopyTo_Invoke_Success()
        {
            var designers = new IDesignerHost[] { new TestDesignerHost(), new TestDesignerHost() };
            ICollection collection = new DesignerCollection(designers);

            IDesignerHost[] destination = new IDesignerHost[4];
            collection.CopyTo(destination, 1);

            Assert.Equal(new IDesignerHost[] { null, designers[0], designers[1], null }, destination);
        }

        [Fact]
        public void CopyTo_NullIList_Nop()
        {
            ICollection collection = new DesignerCollection((IList)null);
            var array = new object[] { 1, 2, 3 };
            collection.CopyTo(array, 1);
            Assert.Equal(new object[] { 1, 2, 3}, array);
        }
    }
}
