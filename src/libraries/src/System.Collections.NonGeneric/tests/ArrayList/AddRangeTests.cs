// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class AddRangeTests
    {
        [Fact]
        public void TestAddRangeBasic()
        {
            //--------------------------------------------------------------------------
            // Variable definitions.
            //--------------------------------------------------------------------------
            ArrayList arrList1 = null;
            ArrayList arrList2 = null;
            ArrayList olst1;
            Queue que;

            string[] strHeroes =
            {
                "Batman",
                "Superman",
                "SpiderMan",
                "Wonder Woman",
                "Green Lantern",
                "Flash",
                "Steel"
            };

            string[] strIdentities =
            {
                "Bruce Wayne",
                "Clark Kent",
                "Peter Parker",
                "Diana Prince",
                "Kyle Rayner",
                "Wally West",
                "John Henry Irons"
            };

            //
            // Construct array lists.
            //
            arrList1 = new ArrayList();
            arrList2 = new ArrayList();

            Assert.NotNull(arrList1);
            Assert.NotNull(arrList2);

            // Add items to the lists.
            for (int ii = 0; ii < strHeroes.Length; ++ii)
            {
                arrList1.Add(strHeroes[ii]);
                arrList2.Add(strIdentities[ii]);
            }

            // Verify items added to list.
            Assert.Equal(strHeroes.Length, arrList1.Count);
            Assert.Equal(strIdentities.Length, arrList2.Count);

            //
            // []  Append the second list to the first list.
            //
            // Append the list.
            arrList1.AddRange(arrList2);
            // Verify the size.
            Assert.Equal(strHeroes.Length + strIdentities.Length, arrList1.Count);

            //
            // []  Attempt invalid AddRange - null
            //
            // Append the list.
            Assert.Throws<ArgumentNullException>(() => arrList1.AddRange(null));

            // [] Different ICollection types
            arrList1 = new ArrayList();
            for (int i = 0; i < 10; i++)
                arrList1.Add(i);

            olst1 = new ArrayList();
            for (int i = 0; i < 10; i++)
                olst1.Add(i + 10);

            arrList1.AddRange(olst1);

            for (int i = 0; i < arrList1.Count; i++)
            {
                Assert.Equal(i, (int)arrList1[i]);
            }

            que = new Queue();
            for (int i = 0; i < 10; i++)
                que.Enqueue(i + 20);
            arrList1.AddRange(que);

            for (int i = 0; i < arrList1.Count; i++)
            {
                Assert.Equal(i, (int)arrList1[i]);
            }

            //[] we will copy the arraylist to itself
            arrList1.AddRange(arrList1);
            for (int i = 0; i < arrList1.Count / 2; i++)
            {
                Assert.Equal(i, (int)arrList1[i]);
            }

            for (int i = arrList1.Count / 2; i < arrList1.Count; i++)
            {
                Assert.Equal((i - arrList1.Count / 2), (int)arrList1[i]);
            }

            //[] ICollection has different type objects to the existing ArrayList
            arrList1 = new ArrayList();
            for (int i = 0; i < 10; i++)
                arrList1.Add(i);

            que = new Queue();
            for (int i = 10; i < 20; i++)
                que.Enqueue("String_" + i);
            arrList1.AddRange(que);

            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(i, (int)arrList1[i]);
            }

            for (int i = 10; i < 20; i++)
            {
                Assert.Equal("String_" + i, (string)arrList1[i]);
            }

            //[]Team review feedback - Add an empty ICollection
            arrList1 = new ArrayList();
            que = new Queue();
            arrList1.AddRange(que);

            Assert.Equal(0, arrList1.Count);
        }
    }
}
