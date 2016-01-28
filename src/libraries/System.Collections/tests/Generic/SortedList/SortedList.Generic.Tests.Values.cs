// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace System.Collections.Tests
{
    public class SortedList_Generic_Tests_Values : IList_Generic_Tests<string>
    {
        protected override bool DefaultValueAllowed { get { return true; } }
        protected override bool DuplicateValuesAllowed { get { return true; } }
        protected override bool IsReadOnly { get { return true; } }
        protected override int WaysToModify { get { return 0; } }

        protected override IList<string> GenericIListFactory()
        {
            return new SortedList<string, string>().Values;
        }

        protected override IList<string> GenericIListFactory(int count)
        {
            SortedList<string, string> list = new SortedList<string, string>();
            int seed = 13453;
            for (int i = 0; i < count; i++)
                list.Add(TFactory(seed++), TFactory(seed++));
            return list.Values;
        }

        protected override string TFactory(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes = new byte[stringLength];
            rand.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public override void Enumerator_MoveNext_AfterDisposal(int count)
        {
            // Disposal of the enumerator is treated the same as a Reset call
            IEnumerator<string> enumerator = GenericIEnumerableFactory(count).GetEnumerator();
            for (int i = 0; i < count; i++)
                enumerator.MoveNext();
            enumerator.Dispose();
            if (count > 0)
                Assert.True(enumerator.MoveNext());
        }
    }

    public class SortedList_Generic_Tests_Values_AsICollection : ICollection_NonGeneric_Tests
    {
        protected override bool NullAllowed { get { return true; } }
        protected override bool DuplicateValuesAllowed { get { return true; } }
        protected override bool IsReadOnly { get { return true; } }
        protected override bool Enumerator_Current_UndefinedOperation_Throws { get { return true; } }

        protected override ICollection NonGenericICollectionFactory()
        {
            return (ICollection)(new SortedList<string, string>().Values);
        }

        protected override ICollection NonGenericICollectionFactory(int count)
        {
            SortedList<string, string> list = new SortedList<string, string>();
            int seed = 13453;
            for (int i = 0; i < count; i++)
                list.Add(TFactory(seed++), TFactory(seed++));
            return (ICollection)(list.Values);
        }

        private string TFactory(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes = new byte[stringLength];
            rand.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        protected override void AddToCollection(ICollection collection, int numberOfItemsToAdd)
        {
            Debug.Assert(false);
        }

        protected override void ModifyEnumerable(IEnumerable enumerable, int modificationCode)
        {
            Debug.Assert(false);
        }
    }
}
