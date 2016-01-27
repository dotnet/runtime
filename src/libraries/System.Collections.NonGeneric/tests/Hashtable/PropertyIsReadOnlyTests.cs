// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Tests
{
    public class Hashtable_IsReadOnlyTests
    {
        [Fact]
        public void TestGetIsReadOnlyBasic()
        {
            string strValue;
            Hashtable dic1;

            //[] Vanila test case - Hashtable doesnt have means of getting a readonly HT
            dic1 = new Hashtable();
            for (int i = 0; i < 10; i++)
            {
                strValue = "string_" + i;
                dic1.Add(i, strValue);
            }

            for (int i = 0; i < 10; i++)
            {
                Assert.True(dic1.Contains(i));
            }

            Assert.False(dic1.IsReadOnly);

            //we'll make sure by doing a modifiable things!!
            dic1.Remove(0);
            Assert.False(dic1.Contains(0));
        }
    }
}
