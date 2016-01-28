// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    public class LinkedList_ICollection_NonGeneric_Tests : ICollection_NonGeneric_Tests
    {
        protected override void AddToCollection(ICollection collection, int numberOfItemsToAdd)
        {
            int seed = numberOfItemsToAdd * 34;
            for (int i = 0; i < numberOfItemsToAdd; i++)
                ((LinkedList<string>)collection).AddLast(TFactory(seed++));
        }

        protected override ICollection NonGenericICollectionFactory()
        {
            return new LinkedList<string>();
        }

        protected override int WaysToModify { get { return 5; } }
        protected override bool Enumerator_Current_UndefinedOperation_Throws { get { return true; } }

        protected override void ModifyEnumerable(IEnumerable enumerable, int enumerationCode)
        {
            LinkedList<string> casted = ((LinkedList<string>)enumerable);
            switch (enumerationCode)
            {
                case 0: // Add
                    casted.AddFirst(TFactory(4531));
                    break;
                case 1: // Add
                    casted.AddLast(TFactory(4531));
                    break;
                case 2: // Remove
                    if (casted.Count > 0)
                        casted.RemoveFirst();
                    else
                        casted.AddLast(TFactory(4531));
                    break;
                case 3: // Remove
                    if (casted.Count > 0)
                        casted.RemoveLast();
                    else
                        casted.AddLast(TFactory(4531));
                    break;
                case 4: // Clear
                    if (casted.Count > 0)
                        casted.Clear();
                    else
                        casted.AddLast(TFactory(4531));
                    break;
            }
        }

        protected string TFactory(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes = new byte[stringLength];
            rand.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void LinkedList_ICollection_NonGeneric_CopyTo_ExactlyEnoughSpaceInTypeCorrectArray(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            string[] array = new string[count];
            collection.CopyTo(array, 0);
            int i = 0;
            foreach (object obj in collection)
                Assert.Equal(array[i++], obj);
        }
    }
}