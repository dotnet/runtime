// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class CloneTests
    {
        [Fact]
        public void TestCloneBasic()
        {
            ArrayList alst1;
            ArrayList alst2;

            String strValue;
            Object oValue;

            //[] Vanila test case - Clone should exactly replicate a collection to another object reference
            //afterwards these 2 should not hold the same object references

            alst1 = new ArrayList();
            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                alst1.Add(strValue);
            }

            alst2 = (ArrayList)alst1.Clone();

            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                Assert.Equal(strValue, (String)alst2[i]);
            }

            //now we remove an object from the original list
            alst1.RemoveAt(9);
            Assert.Throws<ArgumentOutOfRangeException>(() => { oValue = alst1[9]; });

            strValue = "String_" + 9;
            Assert.Equal(strValue, (String)alst2[9]);

            //[]now we try other test cases
            //are all the 'other' properties of the arraylist the same?

            alst1 = new ArrayList(1000);
            alst2 = (ArrayList)alst1.Clone();

            // Capacity is not expected to be the same
            Assert.NotEqual(alst1.Capacity, alst2.Capacity);
            Assert.Equal(alst1.Count, alst2.Count);
            Assert.Equal(alst1.IsReadOnly, alst2.IsReadOnly);
            Assert.Equal(alst1.IsSynchronized, alst2.IsSynchronized);

            //[]Clone is a shallow copy, so the objects of the objets reference should be the same
            alst1 = new ArrayList();
            for (int i = 0; i < 10; i++)
            {
                alst1.Add(new Foo());
            }

            alst2 = (ArrayList)alst1.Clone();
            strValue = "Hello World";
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(strValue, ((Foo)alst2[i]).strValue);
            }

            strValue = "Good Bye";
            ((Foo)alst1[0]).strValue = strValue;
            Assert.Equal(strValue, ((Foo)alst1[0]).strValue);
            Assert.Equal(strValue, ((Foo)alst2[0]).strValue);

            //[]if we change the object, of course, the previous should not happen
            alst2[0] = new Foo();

            strValue = "Good Bye";
            Assert.Equal(strValue, ((Foo)alst1[0]).strValue);

            strValue = "Hello World";
            Assert.Equal(strValue, ((Foo)alst2[0]).strValue);
        }
    }

    internal class Foo
    {
        internal String strValue;

        internal Foo()
        {
            strValue = "Hello World";
        }
    }
}
