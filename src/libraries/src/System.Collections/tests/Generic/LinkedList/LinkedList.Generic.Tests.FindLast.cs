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
        public void FindLast_T()
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            int seed = 21543;
            int arraySize = 16;
            T[] headItems, tailItems, prependDefaultHeadItems, prependDefaultTailItems;

            headItems = new T[arraySize];
            tailItems = new T[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                headItems[i] = CreateT(seed++);
                tailItems[i] = CreateT(seed++);
            }
            prependDefaultHeadItems = new T[headItems.Length + 1];
            prependDefaultHeadItems[0] = default(T);
            Array.Copy(headItems, 0, prependDefaultHeadItems, 1, headItems.Length);

            prependDefaultTailItems = new T[tailItems.Length + 1];
            prependDefaultTailItems[0] = default(T);
            Array.Copy(tailItems, 0, prependDefaultTailItems, 1, tailItems.Length);

            //[] Call FindLast an empty collection
            linkedList = new LinkedList<T>();
            Assert.Null(linkedList.FindLast(headItems[0])); //"Err_2899hjaied Expected FindLast to return false with a non null item on an empty collection"
            Assert.Null(linkedList.FindLast(default(T))); //"Err_5808ajiea Expected FindLast to return false with a null item on an empty collection"

            //[] Call FindLast on a collection with one item in it
            linkedList = new LinkedList<T>();
            linkedList.AddLast(headItems[0]);
            Assert.Null(linkedList.FindLast(headItems[1])); //"Err_2899hjaied Expected FindLast to return false with a non null item on an empty collection size=1"
            Assert.Null(linkedList.FindLast(default(T))); //"Err_5808ajiea Expected FindLast to return false with a null item on an empty collection size=1"
            VerifyFindLast(linkedList, new T[] { headItems[0] });

            //[] Call FindLast on a collection with two items in it
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            Assert.Null(linkedList.FindLast(headItems[2])); //"Err_5680ajoed Expected FindLast to return false with an non null item not in the collection size=2"
            Assert.Null(linkedList.FindLast(default(T))); //"Err_858196aieh Expected FindLast to return false with an null item not in the collection size=2"
            VerifyFindLast(linkedList, new T[] { headItems[0], headItems[1] });

            //[] Call FindLast on a collection with three items in it
            linkedList = new LinkedList<T>();
            linkedList.AddFirst(headItems[0]);
            linkedList.AddLast(headItems[1]);
            linkedList.AddLast(headItems[2]);
            Assert.Null(linkedList.FindLast(headItems[3])); //"Err_50878adie Expected FindLast to return false with an non null item not in the collection size=3"
            Assert.Null(linkedList.FindLast(default(T))); //"Err_3969887wiqpi Expected FindLast to return false with an null item not in the collection size=3"
            VerifyFindLast(linkedList, new T[] { headItems[0], headItems[1], headItems[2] });

            //[] Call FindLast on a collection with multiple items in it
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
            {
                linkedList.AddLast(headItems[i]);
            }

            Assert.Null(linkedList.FindLast(tailItems[0])); //"Err_5808ajdoi Expected FindLast to return false with an non null item not in the collection size=16"
            Assert.Null(linkedList.FindLast(default(T))); //"Err_5588aied Expected FindLast to return false with an null item not in the collection size=16"
            VerifyFindLast(linkedList, headItems);

            //[] Call FindLast on a collection with duplicate items in it
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);

            Assert.Null(linkedList.FindLast(tailItems[0])); //"Err_8548ajia Expected FindLast to return false with an non null item not in the collection size=16"
            Assert.Null(linkedList.FindLast(default(T))); //"Err_3108qoa Expected FindLast to return false with an null item not in the collection size=16"
            T[] tempItems = new T[headItems.Length + headItems.Length];
            Array.Copy(headItems, 0, tempItems, 0, headItems.Length);
            Array.Copy(headItems, 0, tempItems, headItems.Length, headItems.Length);
            VerifyFindLastDuplicates(linkedList, tempItems);

            //[] Call FindLast with default(T) at the beginning
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);
            linkedList.AddFirst(default(T));

            Assert.Null(linkedList.FindLast(tailItems[0])); //"Err_8548ajia Expected FindLast to return false with an non null item not in the collection default(T) at the beginning"
            VerifyFindLast(linkedList, prependDefaultHeadItems);

            //[] Call FindLast with default(T) in the middle
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);
            linkedList.AddLast(default(T));
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(tailItems[i]);

            Assert.Null(linkedList.FindLast(CreateT(seed++))); //"Err_78585ajhed Expected FindLast to return false with an non null item not in the collection default(T) in the middle"

            tempItems = new T[headItems.Length + prependDefaultTailItems.Length];
            Array.Copy(headItems, 0, tempItems, 0, headItems.Length);
            Array.Copy(prependDefaultTailItems, 0, tempItems, headItems.Length, prependDefaultTailItems.Length);

            VerifyFindLast(linkedList, tempItems);

            //[] Call FindLast on a collection with duplicate items in it
            linkedList = new LinkedList<T>();
            for (int i = 0; i < headItems.Length; ++i)
                linkedList.AddLast(headItems[i]);
            linkedList.AddLast(default(T));

            Assert.Null(linkedList.FindLast(tailItems[0])); //"Err_208089ajdi Expected FindLast to return false with an non null item not in the collection default(T) at the end"
            T[] temp = new T[headItems.Length + 1];
            temp[headItems.Length] = default(T);
            Array.Copy(headItems, temp, headItems.Length);
            VerifyFindLast(linkedList, temp);
        }
    }
}
