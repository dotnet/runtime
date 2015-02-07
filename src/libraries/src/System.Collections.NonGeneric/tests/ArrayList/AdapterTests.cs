// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class AdapterTests
    {
        [Fact]
        public void TestNullIListParameter()
        {
            ArrayList lAdapter = null;

            //[] check for ArgumentNullException when argument null
            Assert.Throws<ArgumentNullException>(() => { lAdapter = ArrayList.Adapter(null); });
        }

        [Fact]
        public void TestPopulateChangesToList()
        {
            const string fromBefore = " from before";

            //[] make sure changes Through listAdapter show up in list
            // populate the list
            ArrayList tempList = CreateArrayList(count: 10, optStr: fromBefore);
            // wrap the list
            ArrayList lAdapter = ArrayList.Adapter(tempList);
            // make changes through listAdapter and make sure they are reflected in tempList
            lAdapter.Reverse(0, lAdapter.Count);

            int j = 9;
            for (int i = 0; i < lAdapter.Count; i++)
            {
                Assert.Equal(j.ToString() + fromBefore, lAdapter[i]);
                j--;
            }
        }

        [Fact]
        public void TestClearList()
        {
            //[] make sure changes Through list show up in listAdapter
            // populate the list
            ArrayList tempList = CreateArrayList(count: 10, optStr: " from before");

            // wrap the list
            ArrayList lAdapter = ArrayList.Adapter(tempList);

            // make changes through listAdapter and make sure they are reflected in tempList
            tempList.Clear();

            Assert.Equal(0, lAdapter.Count);
        }

        [Fact]
        public void TestEnumerators()
        {
            //[] test to see if enumerators are correctly enumerate through elements
            // populate the list
            ArrayList tempList = CreateArrayList(10);
            IEnumerator ienumList = tempList.GetEnumerator();

            // wrap the list
            ArrayList lAdapter = ArrayList.Adapter(tempList);
            IEnumerator ienumWrap = tempList.GetEnumerator();

            int j = 0;
            while (ienumList.MoveNext())
            {
                Assert.True(ienumList.Current.Equals(j.ToString()), "Error,  enumerator on list expected to return " + j + " but returned " + ienumList.Current);
                j++;
            }

            j = 0;
            while (ienumWrap.MoveNext())
            {
                Assert.True(ienumWrap.Current.Equals(j.ToString()), "Error,  enumerator on listadapter expected to return " + j + " but returned " + ienumWrap.Current);
                j++;
            }
        }

        [Fact]
        public void TestEnumeratorsModifiedList()
        {
            //[] test to see if enumerators are correctly getting invalidated with list modified through list
            // populate the list
            ArrayList tempList = CreateArrayList(10);
            IEnumerator ienumList = tempList.GetEnumerator();

            // wrap the list
            ArrayList lAdapter = ArrayList.Adapter(tempList);
            IEnumerator ienumWrap = tempList.GetEnumerator();

            // start enumeration
            ienumList.MoveNext();
            ienumWrap.MoveNext();

            // now modify list through tempList
            tempList.Add("Hey this is new element");

            // make sure accessing ienumList will throw
            Assert.Throws<InvalidOperationException>(() => ienumList.MoveNext());

            // make sure accessing ienumWrap will throw
            Assert.Throws<InvalidOperationException>(() => ienumWrap.MoveNext());
        }

        [Fact]
        public void TestEnumeratorsModifiedAdapter()
        {
            //[] test to see if enumerators are correctly getting invalidated with list modified through listAdapter

            // populate the list
            ArrayList tempList = CreateArrayList(10);
            IEnumerator ienumList = tempList.GetEnumerator();

            // wrap the list
            ArrayList lAdapter = ArrayList.Adapter(tempList);
            IEnumerator ienumWrap = tempList.GetEnumerator();

            // start enumeration
            ienumList.MoveNext();
            ienumWrap.MoveNext();

            // now modify list through adapter
            lAdapter.Add("Hey this is new element");

            // make sure accessing ienumList will throw
            Assert.Throws<InvalidOperationException>(() => ienumList.MoveNext());
            // make sure accessing ienumWrap will throw
            Assert.Throws<InvalidOperationException>(() => ienumWrap.MoveNext());
        }

        [Fact]
        public void TestInsertRange()
        {
            //[] to see if listadaptor modified using InsertRange works
            // populate the list
            ArrayList tempList = CreateArrayList(10);
            ArrayList lAdapter = ArrayList.Adapter(tempList);

            // now add a few more elements using insertrange
            ArrayList tempListSecond = CreateArrayList(10, 10);
            lAdapter.InsertRange(lAdapter.Count, tempListSecond);

            Assert.Equal(20, lAdapter.Count);
        }

        private static ArrayList CreateArrayList(int count, int start = 0, string optStr = null)
        {
            ArrayList arrayList = new ArrayList();
            for (int i = start; i < start + count; i++)
            {
                arrayList.Add(i.ToString() + optStr);
            }

            return arrayList;
        }
    }
}
