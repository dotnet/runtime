// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class ReadOnlyTests
    {
        [Fact]
        public void TestArrayListParameter()
        {
            ArrayList alst1;
            ArrayList alst2;
            ArrayList alst3;

            string strValue;
            object oValue;

            //[] Vanila test case - ReadOnly returns an ArrayList that cant be modified
            // The idea is that a developer will pass the readonly ArrayList to another user who will not be able to
            // modify it!! i.e. the developer can play the pupper Master and change the list afterwards

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

            //[]now we remove an object from the original list and check the ReadOnly obejct we have of it
            alst1.RemoveAt(9);
            Assert.Throws<ArgumentOutOfRangeException>(() => { oValue = alst1[9]; });

            //we cant access this in our readonly list object as well - the object underneath has been cut
            Assert.Throws<ArgumentOutOfRangeException>(() => { oValue = alst2[9]; });

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

            //[]we'll get a readonly from this readonly 
            alst3 = ArrayList.ReadOnly(alst2);
            Assert.True(alst2.IsReadOnly);
            Assert.True(alst3.IsReadOnly);

            //[]we still cant access the 2nd one :)
            //we cant access remove or add to the readonly list
            Assert.Throws<NotSupportedException>(() => alst2.RemoveAt(0));

            // []Try ReadOnly with a null ArrayList    
            Assert.Throws<ArgumentNullException>(() =>
            {
                ArrayList myArrayList = null;
                ArrayList.ReadOnly(myArrayList);
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

            // [] Vanila test case - ReadOnly returns an IList that cant be modified
            //    The idea is that a developer will pass the readonly ArrayList to another user who will not be able to
            //    modify it!! i.e. the developer can play the pupper Master and change the list afterwards

            alst1 = new ArrayList();
            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                alst1.Add(strValue);
            }

            ilst1 = ArrayList.ReadOnly((IList)alst1);

            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                Assert.Equal(strValue, (string)ilst1[i]);
            }

            //[]now we remove an object from the original list and check the readonly
            alst1.RemoveAt(9);
            Assert.Throws<ArgumentOutOfRangeException>(() => { oValue = alst1[9]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { oValue = ilst1[9]; });

            //[]we cant access remove or add to the readonly list
            Assert.Throws<NotSupportedException>(() => ilst1.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => ilst1.Remove("String_1"));
            Assert.Throws<NotSupportedException>(() => ilst1.Clear());
            Assert.Throws<NotSupportedException>(() => ilst1.Add("This sort of thing will not be allowed"));
            Assert.Throws<NotSupportedException>(() => ilst1.Insert(0, "This sort of thing will not be allowed"));
            Assert.Throws<NotSupportedException>(() =>
            {
                strValue = "Hello World";
                ilst1[0] = strValue;
            });

            //[]we should be able to get other object that implement IList from this method
            olst1 = new ArrayList();
            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                olst1.Add(strValue);
            }

            ilst1 = ArrayList.ReadOnly((IList)olst1);
            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                Assert.Equal(strValue, (string)ilst1[i]);
            }

            //[]we'll test some of the other methods in IList that should work!!
            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                Assert.True(ilst1.Contains(strValue));

                Assert.Equal(i, ilst1.IndexOf(strValue));
            }

            //[]lastly, the readonly test
            Assert.False(alst1.IsReadOnly);

            Assert.False(olst1.IsReadOnly);

            Assert.True(ilst1.IsReadOnly);

            // []Try ReadOnly with a null IList
            Assert.Throws<ArgumentNullException>(() =>
            {
                IList myIList = null;
                ArrayList.ReadOnly(myIList);
            });
        }
    }
}
