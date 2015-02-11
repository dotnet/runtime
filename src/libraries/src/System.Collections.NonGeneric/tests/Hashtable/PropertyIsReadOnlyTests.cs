// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;

namespace System.Collections.HashtableTests
{
    public class IsReadOnlyTests
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
