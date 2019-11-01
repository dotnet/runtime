// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.ObjectModel.Tests
{
    public class KeyedCollectionTest
    {
        [Fact]
        public void StringComparer()
        {
            var collection =
                new TestKeyedCollectionOfIKeyedItem<string, int>(
                    System.StringComparer.OrdinalIgnoreCase)
                {
                    new KeyedItem<string, int>("foo", 0),
                    new KeyedItem<string, int>("bar", 1)
                };
            AssertExtensions.Throws<ArgumentException>("key", null, () => collection.Add(new KeyedItem<string, int>("Foo", 0)));
            AssertExtensions.Throws<ArgumentException>("key", null, () => collection.Add(new KeyedItem<string, int>("fOo", 0)));
            AssertExtensions.Throws<ArgumentException>("key", null, () => collection.Add(new KeyedItem<string, int>("baR", 0)));
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(int.MinValue)]
        public void Ctor_InvalidDictionaryCreationThreshold_ThrowsArgumentOutOfRangeException(int dictionaryCreationThreshold)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("dictionaryCreationThreshold", () => new TestKeyedCollectionOfIKeyedItem<string, int>(dictionaryCreationThreshold));
        }
    }

    public class IListTestKeyedCollectionIntInt :
        IListTestKeyedCollection<int, int>
    {
        private static int s_item = 1;

        /// <summary>
        ///     When overridden in a derived class, Gets a list of values that are not valid elements in the collection under test.
        /// </summary>
        /// <returns>An <see cref="IEnumerable" /> containing the invalid values.</returns>
        protected override IEnumerable GetInvalidValues()
        {
            yield return (long) 5;
            yield return "foo";
            yield return new object();
        }

        /// <summary>
        ///     When overridden in a derived class, Gets a new, unique item.
        /// </summary>
        /// <returns>The new item.</returns>
        protected override int CreateItem()
        {
            return s_item++;
        }

        protected override int GetKeyForItem(int item)
        {
            return item;
        }
    }

    public class IListTestKeyedCollectionStringString :
        IListTestKeyedCollection<string, string>
    {
        private static int s_item = 1;

        /// <summary>
        ///     When overridden in a derived class, Gets a list of values that are not valid elements in the collection under test.
        /// </summary>
        /// <returns>An <see cref="IEnumerable" /> containing the invalid values.</returns>
        protected override IEnumerable GetInvalidValues()
        {
            yield return (long) 5;
            yield return 5;
            yield return new object();
        }

        /// <summary>
        ///     When overridden in a derived class, Gets a new, unique item.
        /// </summary>
        /// <returns>The new item.</returns>
        protected override string CreateItem()
        {
            return (s_item++).ToString();
        }

        protected override string GetKeyForItem(string item)
        {
            return item;
        }
    }

    public class IListTestKeyedCollectionIntString :
        IListTestKeyedCollection<int, string>
    {
        private static int s_item = 1;

        /// <summary>
        ///     When overridden in a derived class, Gets a list of values that are not valid elements in the collection under test.
        /// </summary>
        /// <returns>An <see cref="IEnumerable" /> containing the invalid values.</returns>
        protected override IEnumerable GetInvalidValues()
        {
            yield return (long) 5;
            yield return 5;
            yield return new object();
        }

        /// <summary>
        ///     When overridden in a derived class, Gets a new, unique item.
        /// </summary>
        /// <returns>The new item.</returns>
        protected override string CreateItem()
        {
            return (s_item++).ToString();
        }

        protected override int GetKeyForItem(string item)
        {
            return item == null ? 0 : int.Parse(item);
        }
    }

    public class IListTestKeyedCollectionStringInt :
        IListTestKeyedCollection<string, int>
    {
        private static int s_item = 1;

        /// <summary>
        ///     When overridden in a derived class, Gets a list of values that are not valid elements in the collection under test.
        /// </summary>
        /// <returns>An <see cref="IEnumerable" /> containing the invalid values.</returns>
        protected override IEnumerable GetInvalidValues()
        {
            yield return (long) 5;
            yield return "bar";
            yield return new object();
        }

        /// <summary>
        ///     When overridden in a derived class, Gets a new, unique item.
        /// </summary>
        /// <returns>The new item.</returns>
        protected override int CreateItem()
        {
            return s_item++;
        }

        protected override string GetKeyForItem(int item)
        {
            return item.ToString();
        }
    }

    public class IListTestKeyedCollectionIntIntBadKey :
        IListTestKeyedCollectionBadKey<int, int>
    {
        private static int s_item = 1;

        /// <summary>
        ///     When overridden in a derived class, Gets a list of values that are not valid elements in the collection under test.
        /// </summary>
        /// <returns>An <see cref="IEnumerable" /> containing the invalid values.</returns>
        protected override IEnumerable GetInvalidValues()
        {
            yield return (long) 5;
            yield return "foo";
            yield return new object();
        }

        /// <summary>
        ///     When overridden in a derived class, Gets a new, unique item.
        /// </summary>
        /// <returns>The new item.</returns>
        protected override int CreateItem()
        {
            return s_item++;
        }

        protected override int GetKeyForItem(int item)
        {
            return item;
        }
    }

    public class IListTestKeyedCollectionStringStringBadKey :
        IListTestKeyedCollectionBadKey<string, string>
    {
        private static int s_item = 1;

        /// <summary>
        ///     When overridden in a derived class, Gets a list of values that are not valid elements in the collection under test.
        /// </summary>
        /// <returns>An <see cref="IEnumerable" /> containing the invalid values.</returns>
        protected override IEnumerable GetInvalidValues()
        {
            yield return (long) 5;
            yield return 5;
            yield return new object();
        }

        /// <summary>
        ///     When overridden in a derived class, Gets a new, unique item.
        /// </summary>
        /// <returns>The new item.</returns>
        protected override string CreateItem()
        {
            return (s_item++).ToString();
        }

        protected override string GetKeyForItem(string item)
        {
            return item;
        }
    }

    public class IListTestKeyedCollectionIntStringBadKey :
        IListTestKeyedCollectionBadKey<int, string>
    {
        private static int s_item = 1;

        /// <summary>
        ///     When overridden in a derived class, Gets a list of values that are not valid elements in the collection under test.
        /// </summary>
        /// <returns>An <see cref="IEnumerable" /> containing the invalid values.</returns>
        protected override IEnumerable GetInvalidValues()
        {
            yield return (long) 5;
            yield return 5;
            yield return new object();
        }

        /// <summary>
        ///     When overridden in a derived class, Gets a new, unique item.
        /// </summary>
        /// <returns>The new item.</returns>
        protected override string CreateItem()
        {
            return (s_item++).ToString();
        }

        protected override int GetKeyForItem(string item)
        {
            return item == null ? 0 : int.Parse(item);
        }
    }

    public class IListTestKeyedCollectionStringIntBadKey :
        IListTestKeyedCollectionBadKey<string, int>
    {
        private static int s_item = 1;

        /// <summary>
        ///     When overridden in a derived class, Gets a list of values that are not valid elements in the collection under test.
        /// </summary>
        /// <returns>An <see cref="IEnumerable" /> containing the invalid values.</returns>
        protected override IEnumerable GetInvalidValues()
        {
            yield return (long) 5;
            yield return "bar";
            yield return new object();
        }

        /// <summary>
        ///     When overridden in a derived class, Gets a new, unique item.
        /// </summary>
        /// <returns>The new item.</returns>
        protected override int CreateItem()
        {
            return s_item++;
        }

        protected override string GetKeyForItem(int item)
        {
            return item.ToString();
        }
    }

    public class KeyedCollectionTestsIntInt :
        KeyedCollectionTests<int, int>
    {
        private int _curValue = 1;

        public override int GetKeyForItem(int item)
        {
            return item;
        }

        public override int GenerateValue()
        {
            return _curValue++;
        }
    }

    public class KeyedCollectionTestsStringString :
        KeyedCollectionTests<string, string>
    {
        private int _curValue = 1;

        public override string GetKeyForItem(string item)
        {
            return item;
        }

        public override string GenerateValue()
        {
            return (_curValue++).ToString();
        }
    }

    public class KeyedCollectionTestsIntString :
        KeyedCollectionTests<int, string>
    {
        private int _curValue = 1;

        public override int GetKeyForItem(string item)
        {
            return int.Parse(item);
        }

        public override string GenerateValue()
        {
            return (_curValue++).ToString();
        }
    }

    public class KeyedCollectionTestsStringInt :
        KeyedCollectionTests<string, int>
    {
        private int _curValue = 1;

        public override string GetKeyForItem(int item)
        {
            return item.ToString();
        }

        public override int GenerateValue()
        {
            return _curValue++;
        }
    }
}
