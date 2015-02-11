// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.HashtableTests
{
    public class CloneTests
    {
        [Fact]
        public void TestCloneBasic()
        {
            Hashtable hsh1;
            Hashtable hsh2;

            string strKey;
            string strValue;

            //[] empty Hashtable clone
            hsh1 = new Hashtable();
            hsh2 = (Hashtable)hsh1.Clone();

            Assert.Equal(0, hsh2.Count);
            Assert.False(hsh2.IsReadOnly, "Error, Expected value not returned, <<" + hsh1.IsReadOnly + ">> <<" + hsh2.IsReadOnly + ">>");
            Assert.False(hsh2.IsSynchronized, "Error, Expected value not returned, <<" + hsh1.IsSynchronized + ">> <<" + hsh2.IsSynchronized + ">>");

            //[] Clone should exactly replicate a collection to another object reference
            //afterwards these 2 should not hold the same object references
            hsh1 = new Hashtable();
            for (int i = 0; i < 10; i++)
            {
                strKey = "Key_" + i;
                strValue = "string_" + i;
                hsh1.Add(strKey, strValue);
            }

            hsh2 = (Hashtable)hsh1.Clone();
            for (int i = 0; i < 10; i++)
            {
                strValue = "string_" + i;

                Assert.True(strValue.Equals((string)hsh2["Key_" + i]), "Error, Expected value not returned, " + strValue);
            }

            //now we remove an object from the original list
            hsh1.Remove("Key_9");
            Assert.Equal(hsh1["Key_9"], null);

            strValue = "string_" + 9;
            Assert.True(strValue.Equals((string)hsh2["Key_9"]), "Error, Expected value not returned, <<" + hsh1[9] + ">>");

            //[]now we try other test cases
            //are all the 'other' properties of the Hashtable the same?
            hsh1 = new Hashtable(1000);
            hsh2 = (Hashtable)hsh1.Clone();

            Assert.Equal(hsh1.Count, hsh2.Count);
            Assert.Equal(hsh1.IsReadOnly, hsh2.IsReadOnly);
            Assert.Equal(hsh1.IsSynchronized, hsh2.IsSynchronized);
        }

        [Fact]
        public void TestCloneShallowCopyReferenceTypes()
        {
            //[]Clone is a shallow copy, so the objects of the objets reference should be the same
            string strValue;

            var hsh1 = new Hashtable();
            for (int i = 0; i < 10; i++)
            {
                hsh1.Add(i, new Foo());
            }

            var hsh2 = (Hashtable)hsh1.Clone();
            for (int i = 0; i < 10; i++)
            {
                strValue = "Hello World";
                Assert.True(strValue.Equals(((Foo)hsh2[i]).strValue), "Error, Expected value not returned, " + strValue);
            }

            strValue = "Good Bye";
            ((Foo)hsh1[0]).strValue = strValue;
            Assert.True(strValue.Equals(((Foo)hsh1[0]).strValue), "Error, Expected value not returned, " + strValue);

            //da test
            Assert.True(strValue.Equals(((Foo)hsh2[0]).strValue), "Error, Expected value not returned, " + strValue);

            //if we change the object, of course, the previous should not happen
            hsh2[0] = new Foo();

            strValue = "Good Bye";
            Assert.True(strValue.Equals(((Foo)hsh1[0]).strValue), "Error, Expected value not returned, " + strValue);

            strValue = "Hello World";
            Assert.True(strValue.Equals(((Foo)hsh2[0]).strValue), "Error, Expected value not returned, " + strValue);
        }

        [Fact]
        public void TestClonedHashtableSameAsOriginal()
        {
            string strKey;
            string strValue;

            //1) create a HT, add elements
            //2) create another HT out of the first
            //3) Remove elements from 2) and add
            //4) Clone 3)
            var hsh1 = new Hashtable();
            for (int i = 0; i < 10; i++)
            {
                strKey = "Key_" + i;
                strValue = "string_" + i;
                hsh1.Add(strKey, strValue);
            }

            var hsh2 = new Hashtable(hsh1);
            hsh2.Remove("Key_0");
            hsh2.Remove("Key_1");
            hsh2.Remove("Key_2");

            Assert.Equal(hsh2.Count, 7);
            hsh2["Key_10"] = "string_10";
            hsh2["Key_11"] = "string_11";
            hsh2["Key_12"] = "string_12";

            var hsh3 = (Hashtable)hsh2.Clone();

            Assert.Equal(10, hsh3.Count);

            for (int i = 3; i < 13; i++)
            {
                Assert.True(hsh3.Contains("Key_" + i));
                Assert.True(hsh3.ContainsKey("Key_" + i));
                Assert.True(hsh3.ContainsValue("string_" + i));
            }
        }

        [Fact]
        public void TestCanCastClonedHashtableToInterfaces()
        {
            ICollection icol1;
            IDictionary idic1;
            Hashtable iclone1;

            //[]we try to cast the returned object from Clone() to different types
            var hsh1 = new Hashtable();

            icol1 = (ICollection)hsh1.Clone();
            Assert.Equal(hsh1.Count, icol1.Count);

            idic1 = (IDictionary)hsh1.Clone();
            Assert.Equal(hsh1.Count, idic1.Count);

            iclone1 = (Hashtable)hsh1.Clone();
            Assert.Equal(hsh1.Count, iclone1.Count);
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
