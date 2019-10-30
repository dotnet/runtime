// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Tests
{
    public class LinkedList_ICollection_NonGeneric_Tests : ICollection_NonGeneric_Tests
    {
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowType => typeof(ArgumentException);

        protected override void AddToCollection(ICollection collection, int numberOfItemsToAdd)
        {
            int seed = numberOfItemsToAdd * 34;
            for (int i = 0; i < numberOfItemsToAdd; i++)
                ((LinkedList<string>)collection).AddLast(CreateT(seed++));
        }

        protected override ICollection NonGenericICollectionFactory()
        {
            return new LinkedList<string>();
        }

        protected override bool Enumerator_Current_UndefinedOperation_Throws => true;

        /// <summary>
        /// Returns a set of ModifyEnumerable delegates that modify the enumerable passed to them.
        /// </summary>
        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations)
        {
            if ((operations & ModifyOperation.Add) == ModifyOperation.Add)
            {
                yield return (IEnumerable enumerable) =>
                {
                    LinkedList<string> casted = ((LinkedList<string>)enumerable);
                    casted.AddFirst(CreateT(4531));
                    return true;
                };
                yield return (IEnumerable enumerable) =>
                {
                    LinkedList<string> casted = ((LinkedList<string>)enumerable);
                    casted.AddLast(CreateT(4531));
                    return true;
                };
            }
            if ((operations & ModifyOperation.Remove) == ModifyOperation.Remove)
            {
                yield return (IEnumerable enumerable) =>
                {
                    LinkedList<string> casted = ((LinkedList<string>)enumerable);
                    if (casted.Count > 0)
                    {
                        casted.RemoveFirst();
                        return true;
                    }
                    return false;
                };
                yield return (IEnumerable enumerable) =>
                {
                    LinkedList<string> casted = ((LinkedList<string>)enumerable);
                    if (casted.Count > 0)
                    {
                        casted.RemoveLast();
                        return true;
                    }
                    return false;
                };
            }
            if ((operations & ModifyOperation.Clear) == ModifyOperation.Clear)
            {
                yield return (IEnumerable enumerable) =>
                {
                    LinkedList<string> casted = ((LinkedList<string>)enumerable);
                    if (casted.Count > 0)
                    {
                        casted.Clear();
                        return true;
                    }
                    return false;
                };
            }
        }

        protected string CreateT(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes = new byte[stringLength];
            rand.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
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
