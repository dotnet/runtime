// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class GetAllValuesTests
    {
        [Theory]
        [InlineData(0, typeof(object))]
        [InlineData(0, typeof(Foo))]
        [InlineData(10, typeof(object))]
        [InlineData(10, typeof(Foo))]
        public void GetAllValues(int count, Type type)
        {
            MyNameObjectCollection nameObjectCollection = Helpers.CreateNameObjectCollection(count);

            if (type == typeof(object))
            {
                VerifyGetAllValues(nameObjectCollection, nameObjectCollection.GetAllValues());
            }
            VerifyGetAllValues(nameObjectCollection, nameObjectCollection.GetAllValues(type));
        }

        private static void VerifyGetAllValues(NameObjectCollectionBase nameObjectCollection, Array values)
        {
            Assert.Equal(nameObjectCollection.Count, values.Length);

            for (int i = 0; i < values.Length; i++)
            {
                Assert.Equal(new Foo("Value_" + i), values.GetValue(i));
            }
        }
        
        [Fact]
        public static void GetAllValues_Invalid()
        {
            MyNameObjectCollection nameObjectCollection = new MyNameObjectCollection();
            AssertExtensions.Throws<ArgumentNullException>("type", () => nameObjectCollection.GetAllValues(null));

            nameObjectCollection.Add("name", new Foo("value"));
            Assert.Throws<ArrayTypeMismatchException>(() => nameObjectCollection.GetAllValues(typeof(string)));
        }
    }
}
