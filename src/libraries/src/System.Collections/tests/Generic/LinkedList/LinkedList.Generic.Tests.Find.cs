// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Tests
{
    /// <summary>
    /// Contains tests that ensure the correctness of the LinkedList class.
    /// </summary>
    public abstract partial class LinkedList_Generic_Tests<T> : ICollection_Generic_Tests<T>
    {
        [Fact]
        public void Find_T()
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int arraySize = 16;
            int seed = 21543;
            T[] headItems, tailItems;

            headItems = new T[arraySize];
            tailItems = new T[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                headItems[i] = CreateT(seed++);
                tailItems[i] = CreateT(seed++);
            }

            //[] Call Find an empty collection
            linkedList = new LinkedList<T>();
            Assert.Null(linkedList.Find(headItems[0])); //"Err_2899hjaied Expected Find to return false with a non null item on an empty collection"
            Assert.Null(linkedList.Find(default(T))); //"Err_5808ajiea Expected Find to return false with a null item on an empty collection"

            //[] Call Find on a collection with one item in it
            linkedList = new LinkedList<T>();
            linkedList.AddLast(headItems[0]);
            Assert.Null(linkedList.Find(headItems[1])); //"Err_2899hjaied Expected Find to return false with a non null item on an empty collection size=1"
            Assert.Null(linkedList.Find(default(T))); //"Err_5808ajiea Expected Find to return false with a null item on an empty collection size=1"
            VerifyFind(linkedList, new T[] { headItems[0] });

            //[] Call Find on a collection with two items in it
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            Assert.Null(linkedList.Find(headItems[2])); //"Err_5680ajoed Expected Find to return false with an non null item not in the collection size=2"
            Assert.Null(linkedList.Find(default(T))); //"Err_858196aieh Expected Find to return false with an null item not in the collection size=2"
            VerifyFind(linkedList, new T[] { headItems[0], headItems[1] });

            //[] Call Find on a collection with three items in it
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            linkedList.AddLast(headItems[2]);
            Assert.Null(linkedList.Find(headItems[3])); //"Err_50878adie Expected Find to return false with an non null item not in the collection size=3"
            Assert.Null(linkedList.Find(default(T))); //"Err_3969887wiqpi Expected Find to return false with an null item not in the collection size=3"
            VerifyFind(linkedList, new T[] { headItems[0], headItems[1], headItems[2] });

            //[] Call Find on a collection with multiple items in it
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);

            Assert.Null(linkedList.Find(tailItems[0])); //"Err_5808ajdoi Expected Find to return false with an non null item not in the collection size=16"
            Assert.Null(linkedList.Find(default(T))); //"Err_5588aied Expected Find to return false with an null item not in the collection size=16"
            VerifyFind(linkedList, headItems);

            //[] Call Find on a collection with duplicate items in it
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);

            Assert.Null(linkedList.Find(tailItems[0])); //"Err_8548ajia Expected Find to return false with an non null item not in the collection size=16"
            Assert.Null(linkedList.Find(default(T))); //"Err_3108qoa Expected Find to return false with an null item not in the collection size=16"
            T[] tempItems = new T[headItems.Length + headItems.Length];
            Array.Copy(headItems, 0, tempItems, 0, headItems.Length);
            Array.Copy(headItems, 0, tempItems, headItems.Length, headItems.Length);
            VerifyFindDuplicates(linkedList, tempItems);


            //[] Call Find with default(T) at the beginning
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);
            linkedList.AddFirst(default(T));

            Assert.Null(linkedList.Find(tailItems[0])); //"Err_8548ajia Expected Find to return false with an non null item not in the collection default(T) at the beginning"

            tempItems = new T[headItems.Length + 1];
            tempItems[0] = default(T);
            Array.Copy(headItems, 0, tempItems, 1, headItems.Length);

            VerifyFind(linkedList, tempItems);

            //[] Call Find with default(T) in the middle
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);
            linkedList.AddLast(default(T));
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(tailItems[i]);

            Assert.Null(linkedList.Find(CreateT(seed++))); //"Err_78585ajhed Expected Find to return false with an non null item not in the collection default(T) in the middle"

            // prepending tempitems2 to tailitems into tempitems
            tempItems = new T[tailItems.Length + 1];
            tempItems[0] = default(T);
            Array.Copy(tailItems, 0, tempItems, 1, tailItems.Length);

            T[] tempItems2 = new T[headItems.Length + tempItems.Length];
            Array.Copy(headItems, 0, tempItems2, 0, headItems.Length);
            Array.Copy(tempItems, 0, tempItems2, headItems.Length, tempItems.Length);

            VerifyFind(linkedList, tempItems2);

            //[] Call Find on a collection with duplicate items in it
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);
            linkedList.AddLast(default(T));

            Assert.Null(linkedList.Find(tailItems[0])); //"Err_208089ajdi Expected Find to return false with an non null item not in the collection default(T) at the end"
            tempItems = new T[headItems.Length + 1];
            tempItems[headItems.Length] = default(T);
            Array.Copy(headItems, 0, tempItems, 0, headItems.Length);
            VerifyFind(linkedList, tempItems);
        }
    }
}
