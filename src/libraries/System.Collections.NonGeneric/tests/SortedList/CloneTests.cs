// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;

namespace System.Collections.SortedListTests
{
    public class CloneTests
    {
        [Fact]
        public void TestCloneBasic()
        {
            SortedList hsh1;
            SortedList hsh2;

            string strKey;
            string strValue;

            // Vanila test case - Clone should exactly replicate a collection to another object reference
            //afterwards these 2 should not hold the same object references
            hsh1 = new SortedList();
            for (int i = 0; i < 10; i++)
            {
                strKey = "Key_" + i;
                strValue = "String_" + i;
                hsh1.Add(strKey, strValue);
            }

            hsh2 = (SortedList)hsh1.Clone();
            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                Assert.Equal(strValue, (string)hsh2["Key_" + i]);
            }

            //now we remove an object from the original list
            hsh1.Remove("Key_9");
            Assert.Null(hsh1["Key_9"]);

            Assert.Equal("String_9", (string)hsh2["Key_9"]);

            //now we try other test cases
            //are all the 'other' properties of the SortedList the same?
            hsh1 = new SortedList(1000);
            hsh2 = (SortedList)hsh1.Clone();

            Assert.Equal(hsh1.Count, hsh2.Count);
            Assert.Equal(hsh1.IsReadOnly, hsh2.IsReadOnly);
            Assert.Equal(hsh1.IsSynchronized, hsh2.IsSynchronized);

            //Clone is a shallow copy, so the objects of the objets reference should be the same
            hsh1 = new SortedList();
            for (int i = 0; i < 10; i++)
            {
                hsh1.Add(i, new Foo());
            }

            hsh2 = (SortedList)hsh1.Clone();
            for (int i = 0; i < 10; i++)
            {
                strValue = "Hello World";
                Assert.Equal(strValue, ((Foo)hsh2[i]).strValue);
            }

            strValue = "Good Bye";
            ((Foo)hsh1[0]).strValue = strValue;

            Assert.Equal(strValue, ((Foo)hsh1[0]).strValue);

            //da test
            Assert.Equal(strValue, ((Foo)hsh2[0]).strValue);

            //if we change the object, of course, the previous should not happen
            hsh2[0] = new Foo();
            strValue = "Good Bye";
            Assert.Equal(strValue, ((Foo)hsh1[0]).strValue);

            strValue = "Hello World";
            Assert.Equal(strValue, ((Foo)hsh2[0]).strValue);
        }
    }

    internal class Foo
    {
        internal string strValue;
        internal Foo()
        {
            strValue = "Hello World";
        }
    }
}
