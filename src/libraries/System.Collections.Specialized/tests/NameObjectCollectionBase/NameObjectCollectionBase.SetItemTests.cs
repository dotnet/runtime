// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameObjectCollectionBaseSetItemTests
    {
        [Fact]
        public void Set_ObjectAtIndex_ModifiesCollection()
        {
            var noc = new MyNameObjectCollection();
            for (int i = 0; i < 10; i++)
            {
                var foo1 = new Foo("Value_1");
                noc.Add("Name_" + i, foo1);

                var foo2 = new Foo("Value_2");
                noc[i] = foo2;
                Assert.Same(foo2, noc[i]);
            }
        }
    }
}
