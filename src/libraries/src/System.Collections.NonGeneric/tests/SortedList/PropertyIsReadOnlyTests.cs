// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Tests
{
    public class SortedList_IsReadOnlyTests
    {
        [Fact]
        public void TestGetIsReadOnlyBasic()
        {
            String strValue;
            SortedList dic1;

            dic1 = new SortedList();
            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                dic1.Add(i, strValue);
            }

            for (int i = 0; i < 10; i++)
            {
                Assert.True(dic1.Contains(i));
            }

            //we'll do the ReadOnly test
            Assert.False(dic1.IsReadOnly);

            //we'll make sure by doing a modifiable things!!
            dic1.Remove(0);
            Assert.False(dic1.Contains(0));
        }
    }
}
