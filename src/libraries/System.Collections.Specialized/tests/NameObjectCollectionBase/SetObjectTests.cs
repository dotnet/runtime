// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class SetObjectNameObjectCollectionBaseTests
    {
        [Fact]
        public void Set_ObjectAtIndex_ModifiesCollection()
        {
            var noc = new MyNameObjectCollection();
            for (int i = 0; i < 10; i++)
            {
                var foo1 = new Foo();
                noc.Add("Name_" + i, foo1);

                var foo2 = new Foo();
                noc[i] = foo2;
                Assert.Equal(foo2, noc[i]);
            }
        }
    }
}
