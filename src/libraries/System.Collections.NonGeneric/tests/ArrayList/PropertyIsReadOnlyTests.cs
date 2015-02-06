// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class IsReadOnlyTests
    {
        [Fact]
        public void TestGetIsReadOnlyBasic()
        {
            ArrayList alst1;
            ArrayList alst2;
            ArrayList alst3;

            string strValue;
            object oValue;

            //[] Vanila test case - ReadOnly returns an ArrayList that cant be modified
            // The idea is that a developer will pass the fixed ArrayList to another user who will not be able to
            // modify it!! 

            alst1 = new ArrayList();
            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                alst1.Add(strValue);
            }

            alst2 = ArrayList.ReadOnly(alst1);

            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                Assert.Equal(strValue, (string)alst2[i]);
            }

            //[]now we remove an object from the original list. we cant access that object in the ReadOnly ArrayList
            alst1.RemoveAt(9);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    oValue = alst1[9];
                });

            //we cant access this in our readonly list object as well - the object underneath has been cut
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                oValue = alst2[9];
            });

            //[]we cant access remove or add to the readonly list
            Assert.Throws<NotSupportedException>(() => alst2.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => alst2.Remove("String_1"));
            Assert.Throws<NotSupportedException>(() => alst2.Clear());
            Assert.Throws<NotSupportedException>(() => alst2.Add("This sort of thing will not be allowed"));
            Assert.Throws<NotSupportedException>(() => alst2.Insert(0, "This sort of thing will not be allowed"));
            Assert.Throws<NotSupportedException>(() =>
            {
                strValue = "Hello World";
                alst2[0] = strValue;
            });

            //[]we'll do the ReadOnly test
            Assert.False(alst1.IsReadOnly);
            Assert.True(alst2.IsReadOnly);

            //[]we'll get a readonly from this readonly ArrayList
            alst3 = ArrayList.ReadOnly(alst2);
            Assert.True(alst2.IsReadOnly);
            Assert.True(alst3.IsReadOnly);
            if (!alst3.IsReadOnly)
            {
                Assert.False(true, "Error, Expected value not returned, " + alst3.IsReadOnly);
            }

            //[]we still cant access the 2nd one :)
            //we cant access remove or add to the readonly list
            Assert.Throws<NotSupportedException>(() => alst2.RemoveAt(0));
        }
    }
}
