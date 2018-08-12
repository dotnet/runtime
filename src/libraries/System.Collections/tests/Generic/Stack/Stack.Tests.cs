// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Collections.Tests
{
    public class Stack_ICollection_NonGeneric_Tests : ICollection_NonGeneric_Tests
    {
        #region ICollection Helper Methods

        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowType => typeof(ArgumentException);

        protected override void AddToCollection(ICollection collection, int numberOfItemsToAdd)
        {
            int seed = numberOfItemsToAdd * 34;
            for (int i = 0; i < numberOfItemsToAdd; i++)
                ((Stack<string>)collection).Push(CreateT(seed++));
        }

        protected override ICollection NonGenericICollectionFactory()
        {
            return new Stack<string>();
        }

        protected override bool Enumerator_Current_UndefinedOperation_Throws => true;

        protected override Type ICollection_NonGeneric_CopyTo_IndexLargerThanArrayCount_ThrowType => typeof(ArgumentOutOfRangeException);

        /// <summary>
        /// Returns a set of ModifyEnumerable delegates that modify the enumerable passed to them.
        /// </summary>
        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations)
        {
            if ((operations & ModifyOperation.Add) == ModifyOperation.Add)
            {
                yield return (IEnumerable enumerable) =>
                {
                    var casted = (Stack<string>)enumerable;
                    casted.Push(CreateT(2344));
                    return true;
                };
            }
            if ((operations & ModifyOperation.Remove) == ModifyOperation.Remove)
            {
                yield return (IEnumerable enumerable) =>
                {
                    var casted = (Stack<string>)enumerable;
                    if (casted.Count > 0)
                    {
                        casted.Pop();
                        return true;
                    }
                    return false;
                };
            }
            if ((operations & ModifyOperation.Clear) == ModifyOperation.Clear)
            {
                yield return (IEnumerable enumerable) =>
                {
                    var casted = (Stack<string>)enumerable;
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

        #endregion
    }
}
