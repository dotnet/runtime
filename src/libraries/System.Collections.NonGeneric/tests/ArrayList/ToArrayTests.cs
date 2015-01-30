// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class ToArrayTests
    {
        [Fact]
        public void TestToArrayBasic()
        {
            ArrayList alst1;
            string strValue;
            object[] oArr;

            //[] Vanila test case - ToArray returns an array of this. We will not extensively test this method as
            // this is a thin wrapper on Array.Copy which is extensively tested
            alst1 = new ArrayList();
            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                alst1.Add(strValue);
            }

            oArr = alst1.ToArray();
            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                Assert.Equal(strValue, (string)oArr[i]);
            }

            //[]lets try an empty list
            alst1 = new ArrayList();
            oArr = alst1.ToArray();
            Assert.Equal(0, oArr.Length);
        }

        [Fact]
        public void TestArrayListWrappers()
        {
            ArrayList alst1;
            string strValue;
            Array arr1;

            //[] Vanila test case - ToArray returns an array of this. We will not extensively test this method as
            // this is a thin wrapper on Array.Copy which is extensively tested elsewhere
            alst1 = new ArrayList();
            for (int i = 0; i < 10; i++)
            {
                strValue = "String_" + i;
                alst1.Add(strValue);
            }

            ArrayList[] arrayListTypes = {
                                            alst1,
                                            ArrayList.Adapter(alst1),
                                            ArrayList.FixedSize(alst1),
                                            alst1.GetRange(0, alst1.Count),
                                            ArrayList.ReadOnly(alst1),
                                            ArrayList.Synchronized(alst1)};

            foreach (ArrayList arrayListType in arrayListTypes)
            {
                alst1 = arrayListType;
                arr1 = alst1.ToArray(typeof(string));

                for (int i = 0; i < 10; i++)
                {
                    strValue = "String_" + i;
                    Assert.Equal(strValue, (string)arr1.GetValue(i));
                }

                //[] this should be covered in Array.Copy, but lets do it for
                Assert.Throws<InvalidCastException>(() => { arr1 = alst1.ToArray(typeof(int)); });
                Assert.Throws<ArgumentNullException>(() => { arr1 = alst1.ToArray(null); });
            }

            //[]lets try an empty list
            alst1.Clear();
            arr1 = alst1.ToArray(typeof(Object));
            Assert.Equal(0, arr1.Length);
        }
    }
}
