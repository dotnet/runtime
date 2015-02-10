// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Reflection;

namespace System.Collections.SortedListTests
{

    public class MultiMethodsTests
    {
        [Fact]
        public void Test01()
        {
            SortedList slst1;
            Int32 iNumberOfElements;
            String strValue;
            Array ar1;

            // ctor(Int32)
            iNumberOfElements = 10;
            slst1 = new SortedList(iNumberOfElements);

            for (int i = iNumberOfElements - 1; i >= 0; i--)
            {
                slst1.Add(50 + i, "Value_" + i);
            }

            Assert.Equal(slst1.Count, iNumberOfElements);

            //we will assume that all the keys are sorted as the name implies. lets see
            //We intially filled this lit with descending and much higher keys. now we access the keys through index
            for (int i = 0; i < slst1.Count; i++)
            {
                strValue = "Value_" + i;
                Assert.True(strValue.Equals(slst1[slst1.GetKey(i)]), "Error, Expected value not returned, " + strValue + " " + slst1[slst1.GetKey(i)]);
            }

            //paramter stuff
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                             {
                                 iNumberOfElements = -5;
                                 slst1 = new SortedList(iNumberOfElements);
                             }
            );

            slst1 = new SortedList();
            Assert.Throws<ArgumentNullException>(() =>
                             {
                                 slst1.Add(null, 5);
                             }
            );

            iNumberOfElements = 10;
            slst1 = new SortedList();

            for (int i = iNumberOfElements - 1; i >= 0; i--)
            {
                slst1.Add(i, null);
            }

            for (int i = 0; i < slst1.Count; i++)
            {
                strValue = "Value_" + i;
                Assert.Null(slst1.GetByIndex(i));
            }

            //Contains()
            iNumberOfElements = 10;
            slst1 = new SortedList(iNumberOfElements);
            for (int i = iNumberOfElements - 1; i >= 0; i--)
            {
                slst1.Add(50 + i, "Value_" + i);
            }

            for (int i = 0; i < slst1.Count; i++)
            {
                strValue = "Value_" + i;
                Assert.True(slst1.Contains(50 + i));
            }

            Assert.False(slst1.Contains(1));
            Assert.False(slst1.Contains(-1));

            //paramter stuff
            Assert.Throws<ArgumentNullException>(() =>
                             {
                                 slst1.Contains(null);
                             }
            );

            //get/set_Item
            slst1 = new SortedList();
            for (int i = iNumberOfElements - 1; i >= 0; i--)
            {
                slst1[50 + i] = "Value_" + i;
            }

            Assert.Equal(slst1.Count, iNumberOfElements);

            for (int i = 0; i < slst1.Count; i++)
            {
                strValue = "Value_" + i;
                Assert.True(strValue.Equals(slst1[i + 50]), "Error, Expected value not returned, " + strValue + " " + slst1[i + 50]);
            }

            //already existent ones
            strValue = "Value_1";
            Assert.True(strValue.Equals(slst1[51]), "Error, Expected value not returned, " + slst1[51]);

            strValue = "Different value";
            slst1[51] = strValue;
            Assert.True(strValue.Equals(slst1[51]), "Error, Expected value not returned, " + slst1[51]);

            //paramter stuff
            Assert.Throws<ArgumentNullException>(() =>
                             {
                                 slst1[null] = "Not a chance";
                             }
            );

            strValue = null;
            slst1[51] = strValue;
            Assert.Null(slst1[51]);

            //SetByIndex - this changes the value at this specific index. Note that SortedList
            //does not have the equicalent Key changing means as this is a SortedList and will be done
            //automatically!!!
            iNumberOfElements = 10;
            slst1 = new SortedList();

            for (int i = iNumberOfElements - 1; i >= 0; i--)
            {
                slst1.Add(50 + i, "Value_" + i);
            }

            for (int i = 0; i < slst1.Count; i++)
            {
                strValue = "Value_" + i + 50;
                slst1.SetByIndex(i, strValue);
            }

            for (int i = 0; i < slst1.Count; i++)
            {
                strValue = "Value_" + i + 50;
                Assert.True(strValue.Equals(slst1.GetByIndex(i)), "Error, Expected value not returned, " + strValue + " " + slst1.GetByIndex(i));
            }
            //paramter stuff

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                             {
                                 slst1.SetByIndex(-1, strValue);
                             }
            );

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                             {
                                 slst1.SetByIndex(slst1.Count, strValue);
                             }
            );

            //CopyTo() - copies the values
            iNumberOfElements = 10;
            slst1 = new SortedList();

            for (int i = iNumberOfElements - 1; i >= 0; i--)
            {
                slst1.Add(50 + i, "Value_" + i);
            }

            ar1 = new DictionaryEntry[iNumberOfElements];
            slst1.CopyTo(ar1, 0);
            for (int i = 0; i < slst1.Count; i++)
            {
                strValue = "Value_" + i;
                Assert.True(strValue.Equals(((DictionaryEntry)ar1.GetValue(i)).Value), "Error, Expected value not returned, " + strValue + " " + ((DictionaryEntry)ar1.GetValue(i)).Value);
            }

            //paramter stuff
            Assert.Throws<InvalidCastException>(() =>
                             {
                                 ar1 = new String[iNumberOfElements];
                                 slst1.CopyTo(ar1, 0);
                             }
            );

            Assert.Throws<ArgumentNullException>(() =>
                             {
                                 slst1.CopyTo(null, 0);
                             }
            );

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                             {
                                 ar1 = new DictionaryEntry[iNumberOfElements];
                                 slst1.CopyTo(ar1, -1);
                             }
            );

            Assert.Throws<ArgumentException>(() =>
                             {
                                 ar1 = new String[iNumberOfElements];
                                 slst1.CopyTo(ar1, 1);
                             }
            );

            ar1 = new DictionaryEntry[2 * iNumberOfElements];
            slst1.CopyTo(ar1, iNumberOfElements);
            for (int i = 0; i < slst1.Count; i++)
            {
                strValue = "Value_" + i;

                Assert.True(strValue.Equals(((DictionaryEntry)ar1.GetValue(iNumberOfElements + i)).Value), "Error, Expected value not returned, <" + strValue + "> <" + ((DictionaryEntry)ar1.GetValue(iNumberOfElements + i)).Value + "><" + ((DictionaryEntry)ar1.GetValue(i)).Value + ">");
            }
        }
    }
}
