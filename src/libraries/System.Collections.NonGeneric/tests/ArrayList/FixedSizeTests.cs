// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class FixedSizeTests
    {
        [Fact]
        public void TestArrayListParameter()
        {
            ArrayList alst1;
            ArrayList alst2;

            String strValue;
            Object oValue;

            //[] Vanila test case - FixedSize returns an IList that cant be flexed
            // The idea is that a developer will pass the fixed IList to another user who will not be able to
            //shrink or expand it!!

            alst1 = new ArrayList();
            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                alst1.Add(strValue);
            }

            alst2 = ArrayList.FixedSize(alst1);

            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                Assert.Equal(strValue, (String)alst2[i]);
            }

            //[]now we remove an object from the original list
            alst1.RemoveAt(9);
            Assert.Throws<ArgumentOutOfRangeException>(() => { oValue = alst1[9]; });

            //[]we cant access this in our fixed list obejct as well - the object underneath has been cut
            Assert.Throws<ArgumentOutOfRangeException>(() => { oValue = alst2[9]; });

            //[]we cant access remove or add to the fixed list
            Assert.Throws<NotSupportedException>(() => alst2.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => alst2.Clear());
            Assert.Throws<NotSupportedException>(() => alst2.Add("This sort of thing will not be allowed"));
            Assert.Throws<NotSupportedException>(() => alst2.Insert(0, "This sort of thing will not be allowed"));

            //[]but we can change the already existing items
            alst1 = new ArrayList();
            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                alst1.Add(strValue);
            }

            alst2 = ArrayList.FixedSize(alst1);

            strValue = "Hello World";
            alst2[0] = strValue;
            Assert.Equal(strValue, (string)alst2[0]);

            // [] Try passing in a null ArrayList
            Assert.Throws<ArgumentNullException>(() =>
                {
                    ArrayList nullArrayList = null;
                    ArrayList.FixedSize(nullArrayList);
                });
        }

        [Fact]
        public void TestIListParameter()
        {
            ArrayList alst1;
            IList ilst1;
            ArrayList olst1;

            string strValue;
            object oValue;

            //[] Vanila test case - FixedSize returns an IList that cant be flexed
            // The idea is that a developer will pass the fixed IList to another user who will not be able to
            //shrink or expand it!!
            alst1 = new ArrayList();
            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                alst1.Add(strValue);
            }

            ilst1 = ArrayList.FixedSize((IList)alst1);

            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                Assert.Equal(strValue, (string)ilst1[i]);
            }

            //[]now we remove an object from the original list
            alst1.RemoveAt(9);
            Assert.Throws<ArgumentOutOfRangeException>(() => { oValue = alst1[9]; });

            //[]we cant access this in our fixed list obejct as well - the object underneath has been cut
            Assert.Throws<ArgumentOutOfRangeException>(() => { oValue = ilst1[9]; });

            //[]we cant access remove or add to the fixed list
            Assert.Throws<NotSupportedException>(() => ilst1.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => ilst1.Clear());
            Assert.Throws<NotSupportedException>(() => ilst1.Add("This sort of thing will not be allowed"));
            Assert.Throws<NotSupportedException>(() => ilst1.Insert(0, "This sort of thing will not be allowed"));
            Assert.Throws<NotSupportedException>(() => ilst1.Remove("String_1"));

            //[]but we can change the already existing items
            alst1 = new ArrayList();
            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                alst1.Add(strValue);
            }

            ilst1 = ArrayList.FixedSize((IList)alst1);

            strValue = "Hello World";
            ilst1[0] = strValue;
            Assert.Equal(strValue, (string)ilst1[0]);

            //[]we should be able to get other object that implement IList from FixedSize

            olst1 = new ArrayList();
            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                olst1.Add(strValue);
            }

            ilst1 = ArrayList.FixedSize((IList)olst1);
            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                Assert.Equal(strValue, (string)ilst1[i]);
            }

            //[]we'll test some of the other methods in IList that should work!!
            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                Assert.True(ilst1.Contains(strValue), "Expected value not returned, " + strValue);
                Assert.Equal(i, ilst1.IndexOf(strValue));
            }

            // [] Try passing in a null IList
            Assert.Throws<ArgumentNullException>(() =>
            {
                IList nullIList = null;
                ArrayList.FixedSize(nullIList);
            });
        }
    }
}
