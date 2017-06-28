// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameObjectCollectionBaseCopyToTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 5)]
        [InlineData(10, 0)]
        [InlineData(10, 5)]
        public void CopyTo(int count, int index)
        {
            MyNameObjectCollection nameObjectCollection = Helpers.CreateNameObjectCollection(count);
            ICollection collection = nameObjectCollection;

            string[] copyArray = new string[index + collection.Count + index];
            collection.CopyTo(copyArray, index);

            for (int i = 0; i < index; i++)
            {
                Assert.Null(copyArray[i]);
            }
            for (int i = 0; i < count; i++)
            {
                Assert.Equal(nameObjectCollection.GetKey(i), copyArray[i + index]);
            }
            for (int i = index + collection.Count; i < copyArray.Length; i++)
            {
                Assert.Null(copyArray[i]);
            }

            // Clearing the nameObjectCollection should not affect the keys copy
            int previousCount = copyArray.Length;
            nameObjectCollection.Clear();
            Assert.Equal(previousCount, copyArray.Length);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void CopyTo_Invalid(int count)
        {
            MyNameObjectCollection nameObjectCollection = Helpers.CreateNameObjectCollection(count);
            ICollection collection = nameObjectCollection;

            AssertExtensions.Throws<ArgumentNullException>("array", () => collection.CopyTo(null, 0));
            AssertExtensions.Throws<ArgumentException>("array", null, () => collection.CopyTo(new string[count, count], 0));

            if (count > 0)
            {
                AssertExtensions.Throws<ArgumentException>(null, () => collection.CopyTo(new string[0], 0));
                AssertExtensions.Throws<ArgumentException>(null, () => collection.CopyTo(new string[count - 1], 0));

                Assert.Throws<InvalidCastException>(() => collection.CopyTo(new Foo[count], 0));
            }

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collection.CopyTo(new string[count], -1));
            AssertExtensions.Throws<ArgumentException>(null, () => collection.CopyTo(new string[count], 1));
            AssertExtensions.Throws<ArgumentException>(null, () => collection.CopyTo(new string[count], count + 1));
        }
    }
}
