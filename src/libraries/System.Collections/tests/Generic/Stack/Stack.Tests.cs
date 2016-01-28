// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    public class Stack_ICollection_NonGeneric_Tests : ICollection_NonGeneric_Tests
    {
        #region ICollection Helper Methods

        protected override void AddToCollection(ICollection collection, int numberOfItemsToAdd)
        {
            int seed = numberOfItemsToAdd * 34;
            for (int i = 0; i < numberOfItemsToAdd; i++)
                ((Stack<string>)collection).Push(TFactory(seed++));
        }

        protected override ICollection NonGenericICollectionFactory()
        {
            return new Stack<string>();
        }

        protected override int WaysToModify { get { return 3; } } // add, remove, clear
        protected override bool Enumerator_Current_UndefinedOperation_Throws { get { return true; } }

        protected override void ModifyEnumerable(IEnumerable enumerable, int enumerationCode)
        {
            switch (enumerationCode)
            {
                case 0: // Add
                    ((Stack<string>)enumerable).Push(TFactory(4531));
                    break;
                case 1: // Remove
                    if (((Stack<string>)enumerable).Count > 0)
                        ((Stack<string>)enumerable).Pop();
                    else
                        ((Stack<string>)enumerable).Push(TFactory(4531));
                    break;
                case 2: // Clear
                    if (((Stack<string>)enumerable).Count > 0)
                        ((Stack<string>)enumerable).Clear();
                    else
                        ((Stack<string>)enumerable).Push(TFactory(4531));
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

        #endregion
    }
}
