// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.ObjectModel.Tests
{
    public partial class KeyedCollectionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var collection = new StringKeyedCollection<int>();
            Assert.Empty(collection);
            Assert.Equal(EqualityComparer<string>.Default, collection.Comparer);
            Assert.Null(collection.Dictionary);
            Assert.Empty(collection.Items);
            Assert.IsType<List<int>>(collection.Items);
        }

        public static IEnumerable<object[]> Ctor_IEqualityComparer_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { StringComparer.OrdinalIgnoreCase };
        }

        [Theory]
        [MemberData(nameof(Ctor_IEqualityComparer_TestData))]
        public void Ctor_IEqualityComparer(IEqualityComparer<string> comparer)
        {
            var collection = new StringKeyedCollection<int>(comparer);
            Assert.Empty(collection);
            Assert.Same(comparer ?? EqualityComparer<string>.Default, collection.Comparer);
            Assert.Null(collection.Dictionary);
            Assert.Empty(collection.Items);
            Assert.IsType<List<int>>(collection.Items);
        }

        public static IEnumerable<object[]> Ctor_IEqualityComparer_Int_TestData()
        {
            yield return new object[] { null, 10 };
            yield return new object[] { null, 0 };
            yield return new object[] { null, -1 };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, 10 };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, 0 };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, -1 };
        }

        [Theory]
        [MemberData(nameof(Ctor_IEqualityComparer_Int_TestData))]
        public void Ctor_IEqualityComparer_Int(IEqualityComparer<string> comparer, int dictionaryCreationThreshold)
        {
            var collection = new StringKeyedCollection<int>(comparer, dictionaryCreationThreshold);
            Assert.Empty(collection);
            Assert.Same(comparer ?? EqualityComparer<string>.Default, collection.Comparer);
            Assert.Null(collection.Dictionary);
            Assert.Empty(collection.Items);
            Assert.IsType<List<int>>(collection.Items);
        }

        [Fact]
        public void Ctor_InvalidDictionaryCreationThreshold_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("dictionaryCreationThreshold", () => new StringKeyedCollection<int>(null, -2));
        }

        [Fact]
        public void InsertItem_InvokeSeveralTimesWithoutCustomThreshold_Success()
        {
            var collection = new StringKeyedCollection<int>();
            collection.GetKeyForItemHandler = item => (item * 2).ToString();

            // Add first.
            collection.InsertItem(0, 1);
            Assert.Equal(new int[] { 1 }, collection.Items.Cast<int>());
            Assert.Equal(new Dictionary<string, int>
                {
                    {"2", 1}
                }, collection.Dictionary
            );
            Assert.Equal(1, collection["2"]);

            // Add another - end.
            collection.InsertItem(1, 3);
            Assert.Equal(new int[] { 1, 3 }, collection.Items.Cast<int>());
            Assert.Equal(new Dictionary<string, int>
                {
                    {"2", 1},
                    {"6", 3}
                }, collection.Dictionary
            );
            Assert.Equal(1, collection["2"]);
            Assert.Equal(3, collection["6"]);

            // Add another - middle.
            collection.InsertItem(1, 2);
            Assert.Equal(new int[] { 1, 2, 3 }, collection.Items.Cast<int>());
            Assert.Equal(new Dictionary<string, int>
                {
                    {"2", 1},
                    {"4", 2},
                    {"6", 3}
                }, collection.Dictionary
            );
            Assert.Equal(1, collection["2"]);
            Assert.Equal(2, collection["4"]);
            Assert.Equal(3, collection["6"]);
        }

        [Fact]
        public void InsertItem_InvokeSeveralTimesWithCustomThreshold_Success()
        {
            var collection = new StringKeyedCollection<int>(null, 3);
            collection.GetKeyForItemHandler = item => (item * 2).ToString();

            // Add first.
            collection.InsertItem(0, 1);
            Assert.Equal(new int[] { 1 }, collection.Items.Cast<int>());
            Assert.Null(collection.Dictionary);
            Assert.Equal(1, collection["2"]);

            // Add another.
            collection.InsertItem(1, 3);
            Assert.Equal(new int[] { 1, 3 }, collection.Items.Cast<int>());
            Assert.Null(collection.Dictionary);
            Assert.Equal(1, collection["2"]);
            Assert.Equal(3, collection["6"]);

            // Meet threshold.
            collection.InsertItem(1, 2);
            Assert.Equal(new int[] { 1, 2, 3 }, collection.Items.Cast<int>());
            Assert.Null(collection.Dictionary);
            Assert.Equal(1, collection["2"]);
            Assert.Equal(2, collection["4"]);
            Assert.Equal(3, collection["6"]);

            // Exceed threshold.
            collection.InsertItem(3, 4);
            Assert.Equal(new int[] { 1, 2, 3, 4 }, collection.Items.Cast<int>());
            Assert.Equal(new Dictionary<string, int>
                {
                    { "2", 1 },
                    { "4", 2 },
                    { "6", 3 },
                    { "8", 4 }
                }, collection.Dictionary
            );
            Assert.Equal(1, collection["2"]);
            Assert.Equal(2, collection["4"]);
            Assert.Equal(3, collection["6"]);
            Assert.Equal(4, collection["8"]);

            // Add another.
            collection.InsertItem(4, 5);
            Assert.Equal(new int[] { 1, 2, 3, 4, 5 }, collection.Items.Cast<int>());
            Assert.Equal(new Dictionary<string, int>
                {
                    { "2", 1 },
                    { "4", 2 },
                    { "6", 3 },
                    { "8", 4 },
                    { "10", 5 }
                }, collection.Dictionary
            );
            Assert.Equal(1, collection["2"]);
            Assert.Equal(2, collection["4"]);
            Assert.Equal(3, collection["6"]);
            Assert.Equal(4, collection["8"]);
            Assert.Equal(5, collection["10"]);
        }

        [Fact]
        public void InsertItem_NullKeyForItemResult_Success()
        {
            var collection = new StringKeyedCollection<int>(null, 0);
            // Don't add even numbers.
            collection.GetKeyForItemHandler = item => item % 2 == 0 ? null : item.ToString();

            // Add null without dictionary - not added.
            collection.InsertItem(0, 2);
            Assert.Equal(new int[] { 2 }, collection.Items.Cast<int>());
            Assert.Null(collection.Dictionary);

            // Add non null without dictionary - added.
            collection.InsertItem(0, 1);
            Assert.Equal(new int[] { 1, 2 }, collection.Items.Cast<int>());
            Assert.Equal(new Dictionary<string, int>
                {
                    {"1", 1}
                }, collection.Dictionary
            );

            // Add null with dictionary - not added.
            collection.InsertItem(0, 4);
            Assert.Equal(new int[] { 4, 1, 2 }, collection.Items.Cast<int>());
            Assert.Equal(new Dictionary<string, int>
                {
                    {"1", 1}
                }, collection.Dictionary
            );

            // Add non null with dictionary - added.
            collection.InsertItem(0, 3);
            Assert.Equal(new int[] { 3, 4, 1, 2 }, collection.Items.Cast<int>());
            Assert.Equal(new Dictionary<string, int>
                {
                    {"1", 1},
                    {"3", 3}
                }, collection.Dictionary
            );
        }

        public static IEnumerable<object[]> InsertItem_AddSameKey_TestData()
        {
            // Exceeding threshold.
            yield return new object[] { null, 0, "first" };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, 0, "first" };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, 0, "FIRST" };

            // Meeting threshold.
            yield return new object[] { null, 1, "first" };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, 1, "first" };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, 1, "FIRST" };

            // Not meeting threshold.
            yield return new object[] { null, 2, "first" };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, 2, "first" };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, 2, "FIRST" };
        }

        [Theory]
        [MemberData(nameof(InsertItem_AddSameKey_TestData))]
        public void InsertItem_AddSameKey_ThrowsArgumentException(IEqualityComparer<string> comparer, int dictionaryCreationThreshold, string key)
        {
            var collection = new StringKeyedCollection<string>(comparer, dictionaryCreationThreshold);
            collection.GetKeyForItemHandler = item => item + "_key";
            collection.InsertItem(0, "first");
            AssertExtensions.Throws<ArgumentException>(dictionaryCreationThreshold > 1 ? "key" : null, null, () => collection.InsertItem(0, key));
        }

        public static IEnumerable<object[]> Contains_TestData()
        {
            yield return new object[] { null, "first_key", true };
            yield return new object[] { null, "FIRST_KEY", false };
            yield return new object[] { null, "NoSuchKey", false };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, "first_key", true };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, "FIRST_KEY", true };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, "NoSuchKey", false };
        }

        [Theory]
        [MemberData(nameof(Contains_TestData))]
        public void Contains_Invoke_ReturnsExpected(IEqualityComparer<string> comparer, string key, bool expected)
        {
            var collection = new StringKeyedCollection<string>(comparer, 3);
            collection.GetKeyForItemHandler = item => item + "_key";

            // Without dictionary.
            collection.InsertItem(0, "first");
            Assert.Equal(expected, collection.Contains(key));

            // With dictionary.
            collection.InsertItem(0, "second");
            collection.InsertItem(0, "third");
            collection.InsertItem(0, "fourth");
            Assert.Equal(expected, collection.Contains(key));
        }

        [Theory]
        [InlineData(3, true)]
        [InlineData(4, false)]
        public void Contains_NullKeyForItemResult_Success(int dictionaryCreationThreshold, bool expected)
        {
            var collection = new StringKeyedCollection<int>(null, dictionaryCreationThreshold);
            collection.GetKeyForItemHandler = i => i.ToString();
            collection.Add(1);
            collection.Add(2);
            collection.Add(3);
            collection.Add(4);

            // Don't get even numbers.
            collection.GetKeyForItemHandler = i => i % 2 == 0 ? null : i.ToString();

            // Get null key.
            Assert.Equal(expected, collection.Contains("2"));

            // Get non null key.
            Assert.True(collection.Contains("1"));
        }

        [Theory]
        [InlineData(3, false)]
        [InlineData(4, true)]
        public void Contains_DifferentKeyForItemResult_Success(int dictionaryCreationThreshold, bool expected)
        {
            var collection = new StringKeyedCollection<int>(null, dictionaryCreationThreshold);
            collection.GetKeyForItemHandler = i => i.ToString();
            collection.Add(1);
            collection.Add(2);
            collection.Add(3);
            collection.Add(4);

            collection.GetKeyForItemHandler = i => (i * 2).ToString();

            Assert.Equal(expected, collection.Contains("6"));
        }

        [Fact]
        public void Contains_NullKey_ThrowsArgumentNullException()
        {
            var collection = new StringKeyedCollection<string>();
            AssertExtensions.Throws<ArgumentNullException>("key", () => collection.Contains(null));
        }

        [Fact]
        public void Item_GetWithComparer_Success()
        {
            var collection = new StringKeyedCollection<string>(StringComparer.OrdinalIgnoreCase, 3);
            collection.GetKeyForItemHandler = item => item + "_key";

            // Without dictionary.
            collection.Add("first");
            Assert.Equal("first", collection["first_key"]);
            Assert.Equal("first", collection["FIRST_KEY"]);

            // With dictionary.
            collection.Add("second");
            collection.Add("third");
            collection.Add("fourth");
            Assert.Equal("first", collection["first_key"]);
            Assert.Equal("first", collection["FIRST_KEY"]);
        }

        [Fact]
        public void Item_GetNoSuchItem_ThrowsKeyNotFoundException()
        {
            var collection = new StringKeyedCollection<string>(null, 3);
            collection.GetKeyForItemHandler = item => item + "_key";

            // Without dictionary.
            collection.Add("first");
            Assert.Throws<KeyNotFoundException>(() => collection["NoSuchKey"]);

            // With dictionary.
            collection.Add("second");
            collection.Add("third");
            collection.Add("fourth");
            Assert.Throws<KeyNotFoundException>(() => collection["NoSuchKey"]);
        }

        public static IEnumerable<object[]> Remove_WithoutDictionary_TestData()
        {
            yield return new object[] { null, "first_key", true, new object[0] };
            yield return new object[] { null, "FIRST_KEY", false, new object[] { "first" } };
            yield return new object[] { null, "NoSuchKey", false, new object[] { "first" } };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, "first_key", true, new object[0] };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, "FIRST_KEY", true, new object[0] };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, "NoSuchKey", false, new object[] { "first" } };
        }

        [Theory]
        [MemberData(nameof(Remove_WithoutDictionary_TestData))]
        public void Remove_ValidKeyWithoutDictionary_Success(IEqualityComparer<string> comparer, string key, bool expected, string[] expectedItems)
        {
            var collection = new StringKeyedCollection<string>(comparer, 3);
            collection.GetKeyForItemHandler = i => i + "_key";
            collection.Add("first");
            Assert.Null(collection.Dictionary);
            
            Assert.Equal(expected, collection.Remove(key));
            Assert.Equal(expectedItems, collection.Items.Cast<string>());
            Assert.Null(collection.Dictionary);
        }

        public static IEnumerable<object[]> Remove_WithDictionary_TestData()
        {
            var fullDictionary = new Dictionary<string, string>
            {
                { "first_key", "first" },
                { "second_key", "second" },
                { "third_key", "third" },
                { "fourth_key", "fourth" }
            };
            var removedDictionary = new Dictionary<string, string>
            {
                { "second_key", "second" },
                { "third_key", "third" },
                { "fourth_key", "fourth" }
            };
            yield return new object[] { null, "first_key", true, new object[] { "second", "third", "fourth" }, removedDictionary };
            yield return new object[] { null, "FIRST_KEY", false, new object[] { "first", "second", "third", "fourth" }, fullDictionary };
            yield return new object[] { null, "NoSuchKey", false, new object[] { "first", "second", "third", "fourth" }, fullDictionary };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, "first_key", true, new object[] { "second", "third", "fourth" }, removedDictionary };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, "FIRST_KEY", true, new object[] {  "second", "third", "fourth" }, removedDictionary };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, "NoSuchKey", false, new object[] { "first", "second", "third", "fourth" }, fullDictionary };
        }

        [Theory]
        [MemberData(nameof(Remove_WithDictionary_TestData))]
        public void Remove_ValidKeyWithDictionary_Success(IEqualityComparer<string> comparer, string key, bool expected, string[] expectedItems, Dictionary<string, string> expectedDictionary)
        {
            var collection = new StringKeyedCollection<string>(comparer, 3);
            collection.GetKeyForItemHandler = i => i + "_key";
            collection.Add("first");
            collection.Add("second");
            collection.Add("third");
            collection.Add("fourth");
            Assert.NotNull(collection.Dictionary);
            
            Assert.Equal(expected, collection.Remove(key));
            Assert.Equal(expectedItems, collection.Items.Cast<string>());
            Assert.Equal(expectedDictionary, collection.Dictionary);
        }

        [Fact]
        public void Remove_NullKeyForItemResult_Success()
        {
            var collection = new StringKeyedCollection<int>(null, 3);
            collection.GetKeyForItemHandler = item => item.ToString();
            collection.Add(1);
            collection.Add(2);
            collection.Add(3);
            collection.Add(4);

            // Don't add/remove even numbers.
            collection.GetKeyForItemHandler = item => item % 2 == 0 ? null : item.ToString();

            // Remove null key.
            Assert.True(collection.Remove("2"));
            Assert.Equal(new int[] { 1, 3, 4 }, collection.Items.Cast<int>());
            Assert.Equal(new Dictionary<string, int>
                {
                    { "1", 1 },
                    { "2", 2 },
                    { "3", 3 },
                    { "4", 4 }
                }, collection.Dictionary
            );

            // Remove non-null key.
            Assert.True(collection.Remove("1"));
            Assert.Equal(new int[] { 3, 4 }, collection.Items.Cast<int>());
            Assert.Equal(new Dictionary<string, int>
                {
                    { "2", 2 },
                    { "3", 3 },
                    { "4", 4 }
                }, collection.Dictionary
            );
        }

        [Fact]
        public void Remove_DifferentKeyForItemResult_Success()
        {
            var collection = new StringKeyedCollection<int>();
            collection.GetKeyForItemHandler = item => item.ToString();
            collection.Add(1);
            collection.Add(2);

            collection.GetKeyForItemHandler = item => (item * 2).ToString();

            collection.RemoveItem(1);
            Assert.Equal(new int[] { 1 }, collection.Items.Cast<int>());
            Assert.Equal(new Dictionary<string, int>
                {
                    { "1", 1 },
                    { "2", 2 },
                }, collection.Dictionary
            );
        }

        [Fact]
        public void Remove_Invoke_ResetsCurrentThresholdCount()
        {
            var collection = new StringKeyedCollection<string>(null, 3);
            collection.GetKeyForItemHandler = item => item + "_key";
            collection.Add("first");
            collection.Remove("first_key");

            // Add more items - make sure the current count has been reset.
            collection.Add("first");
            Assert.Null(collection.Dictionary);

            collection.Add("second");
            Assert.Null(collection.Dictionary);

            collection.Add("third");
            Assert.Null(collection.Dictionary);

            collection.Add("fourth");
            Assert.NotEmpty(collection.Dictionary);
        }

        [Fact]
        public void Remove_NullKey_ThrowsArgumentNullException()
        {
            var collection = new StringKeyedCollection<string>();
            AssertExtensions.Throws<ArgumentNullException>("key", () => collection.Remove(null));
        }

        [Fact]
        public void RemoveItem_InvokeWithoutDictionary_Success()
        {
            var collection = new StringKeyedCollection<string>(null, 3);
            collection.GetKeyForItemHandler = item => item + "_key";
            collection.Add("first");
            Assert.Null(collection.Dictionary);

            collection.RemoveItem(0);
            Assert.Empty(collection);
            Assert.Null(collection.Dictionary);
        }

        [Fact]
        public void RemoveItem_InvokeWithDictionary_Success()
        {
            var collection = new StringKeyedCollection<string>(null, 3);
            collection.GetKeyForItemHandler = item => item + "_key";
            collection.Add("first");
            collection.Add("second");
            collection.Add("third");
            collection.Add("fourth");
            Assert.NotNull(collection.Dictionary);

            // Remove from start.
            collection.RemoveItem(0);
            Assert.Equal(new string[] { "second", "third", "fourth" }, collection.Items.Cast<string>());
            Assert.Equal(new Dictionary<string, string>
                {
                    { "second_key", "second" },
                    { "third_key", "third" },
                    { "fourth_key", "fourth" }
                }, collection.Dictionary
            );

            // Remove from middle.
            collection.RemoveItem(1);
            Assert.Equal(new string[] { "second", "fourth" }, collection.Items.Cast<string>());
            Assert.Equal(new Dictionary<string, string>
                {
                    { "second_key", "second" },
                    { "fourth_key", "fourth" }
                }, collection.Dictionary
            );

            // Remove from end.
            collection.RemoveItem(1);
            Assert.Equal(new string[] { "second" }, collection.Items.Cast<string>());
            Assert.Equal(new Dictionary<string, string>
                {
                    { "second_key", "second" }
                }, collection.Dictionary
            );

            // Remove last.
            collection.RemoveItem(0);
            Assert.Empty(collection);
            Assert.Empty(collection.Dictionary);
        }

        [Fact]
        public void RemoveItem_NullKeyForItemResult_Success()
        {
            var collection = new StringKeyedCollection<int>(null, 3);
            collection.GetKeyForItemHandler = item => item.ToString();
            collection.Add(1);
            collection.Add(2);
            collection.Add(3);
            collection.Add(4);

            // Don't add/remove even numbers.
            collection.GetKeyForItemHandler = item => item % 2 == 0 ? null : item.ToString();

            // Remove null key.
            collection.RemoveItem(1);
            Assert.Equal(new int[] { 1, 3, 4 }, collection.Items.Cast<int>());
            Assert.Equal(new Dictionary<string, int>
                {
                    { "1", 1 },
                    { "2", 2 },
                    { "3", 3 },
                    { "4", 4 }
                }, collection.Dictionary
            );

            // Remove non-null key.
            collection.RemoveItem(0);
            Assert.Equal(new int[] { 3, 4 }, collection.Items.Cast<int>());
            Assert.Equal(new Dictionary<string, int>
                {
                    { "2", 2 },
                    { "3", 3 },
                    { "4", 4 }
                }, collection.Dictionary
            );
        }

        [Fact]
        public void RemoveItem_DifferentKeyForItemResult_Success()
        {
            var collection = new StringKeyedCollection<int>();
            collection.GetKeyForItemHandler = item => item.ToString();
            collection.Add(1);
            collection.Add(2);

            collection.GetKeyForItemHandler = item => (item * 2).ToString();

            collection.RemoveItem(1);
            Assert.Equal(new int[] { 1 }, collection.Items.Cast<int>());
            Assert.Equal(new Dictionary<string, int>
                {
                    { "1", 1 },
                    { "2", 2 },
                }, collection.Dictionary
            );
        }

        [Fact]
        public void RemoveItem_Invoke_ResetsCurrentThresholdCount()
        {
            var collection = new StringKeyedCollection<string>(null, 3);
            collection.GetKeyForItemHandler = item => item + "_key";
            collection.Add("first");
            collection.RemoveItem(0);

            // Add more items - make sure the current count has been reset.
            collection.Add("first");
            Assert.Null(collection.Dictionary);

            collection.Add("second");
            Assert.Null(collection.Dictionary);

            collection.Add("third");
            Assert.Null(collection.Dictionary);

            collection.Add("fourth");
            Assert.NotEmpty(collection.Dictionary);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        public void RemoveItem_InvalidIndex_ThrowsArgumentOutOfRangeException(int index)
        {
            var collection = new StringKeyedCollection<string>(null, 3);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collection.RemoveItem(index));
        }

        [Fact]
        public void ClearItems_WithDictionary_Success()
        {
            var collection = new StringKeyedCollection<string>(null, 3);
            collection.GetKeyForItemHandler = item => item + "_key";
            collection.Add("first");
            collection.Add("second");
            collection.Add("third");
            collection.Add("fourth");
            Assert.NotNull(collection.Dictionary);

            collection.ClearItems();
            Assert.Empty(collection);
            Assert.Empty(collection.Dictionary);
        }

        [Fact]
        public void ClearItems_WithoutDictionary_Success()
        {
            var collection = new StringKeyedCollection<string>(null, 3);
            collection.GetKeyForItemHandler = item => item + "_key";
            collection.Add("first");

            collection.ClearItems();
            Assert.Empty(collection);
            Assert.Null(collection.Dictionary);
        }

        [Fact]
        public void ClearItems_Invoke_ResetsCurrentThresholdCount()
        {
            var collection = new StringKeyedCollection<string>(null, 3);
            collection.GetKeyForItemHandler = item => item + "_key";
            collection.Add("first");
            collection.ClearItems();

            // Add more items - make sure the current count has been reset.
            collection.Add("first");
            Assert.Null(collection.Dictionary);

            collection.Add("second");
            Assert.Null(collection.Dictionary);

            collection.Add("third");
            Assert.Null(collection.Dictionary);

            collection.Add("fourth");
            Assert.NotEmpty(collection.Dictionary);
        }

        public static IEnumerable<object[]> ChangeItemKey_TestData()
        {
            yield return new object[] { null, 0, "first", "first_key", new Dictionary<string, string> { { "first_key", "first" }, { "second_key", "second" } } };
            yield return new object[] { null, 0, "first", "FIRST_KEY", new Dictionary<string, string> { { "FIRST_KEY", "first" }, { "second_key", "second" } } };
            yield return new object[] { null, 0, "first", "SECOND_KEY", new Dictionary<string, string> { { "SECOND_KEY", "first" }, { "second_key", "second" } } };
            yield return new object[] { null, 0, "first", "other_key", new Dictionary<string, string> { { "other_key", "first" }, { "second_key", "second" } } };
            yield return new object[] { null, 0, "first", null, new Dictionary<string, string> { { "second_key", "second" } } };
            yield return new object[] { null, 3, "first", "first_key", null };

            yield return new object[] { StringComparer.OrdinalIgnoreCase, 0, "first", "first_key", new Dictionary<string, string> { { "first_key", "first" }, { "second_key", "second" } } };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, 0, "first", "FIRST_KEY", new Dictionary<string, string> { { "first_key", "first" }, { "second_key", "second" } } };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, 0, "first", null, new Dictionary<string, string> { { "second_key", "second" } } };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, 0, "first", "other_key", new Dictionary<string, string> { { "other_key", "first" }, { "second_key", "second" } } };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, 3, "first", "first_key", null };
        }

        [Theory]
        [MemberData(nameof(ChangeItemKey_TestData))]
        public void ChangeItemKey_Invoke_Success(IEqualityComparer<string> comparer, int dictionaryCreationThreshold, string item, string newKey, Dictionary<string, string> expectedDictionary)
        {
            var collection = new StringKeyedCollection<string>(comparer, dictionaryCreationThreshold);
            collection.GetKeyForItemHandler = i => i + "_key";
            collection.Add("first");
            collection.Add("second");

            collection.ChangeItemKey(item, newKey);
            Assert.Equal(new string[] { "first", "second" }, collection.Items.Cast<string>());
            Assert.Equal(expectedDictionary, collection.Dictionary);
        }

        [Fact]
        public void ChangeItemKey_NullNewKey_Success()
        {
            var collection = new StringKeyedCollection<int>(null, 0);
            collection.GetKeyForItemHandler = item => item.ToString();
            collection.Add(1);
            collection.Add(2);

            // Don't add even numbers.
            collection.GetKeyForItemHandler = item => item % 2 == 0 ? null : item.ToString();

            // Change null key.
            collection.ChangeItemKey(2, "6");
            Assert.Equal(new int[] { 1, 2 }, collection.Items.Cast<int>());
            Assert.Equal(new Dictionary<string, int>
                {
                    { "1", 1 },
                    { "2", 2 },
                    { "6", 2 }
                }, collection.Dictionary
            );

            // Change non-null key.
            collection.ChangeItemKey(1, "5");
            Assert.Equal(new int[] { 1, 2 }, collection.Items.Cast<int>());
            Assert.Equal(new Dictionary<string, int>
                {
                    { "5", 1 },
                    { "2", 2 },
                    { "6", 2 }
                }, collection.Dictionary
            );
        }

        [Fact]
        public void ChangeItemKey_OnThresholdOfCreation_Success()
        {
            var collection = new StringKeyedCollection<int>(null, 3);
            collection.GetKeyForItemHandler = item => item.ToString();
            collection.Add(1);
            collection.Add(2);
            collection.Add(3);
            Assert.Null(collection.Dictionary);

            collection.GetKeyForItemHandler = item => (item * 2).ToString();

            collection.ChangeItemKey(2, "10");
            Assert.Equal(new Dictionary<string, int>
                {
                    {"2", 1},
                    {"10", 2},
                    {"6", 3}
                }, collection.Dictionary
            );
        }

        public static IEnumerable<object[]> ChangeItemKey_DuplicateKey_TestData()
        {
            yield return new object[] { null, "second_key" };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, "second_key" };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, "SECOND_KEY" };
        }

        [Theory]
        [MemberData(nameof(ChangeItemKey_DuplicateKey_TestData))]
        public void ChangeItemKey_DuplicateKey_ThrowsArgumentException(IEqualityComparer<string> comparer, string newKey)
        {
            var collection = new StringKeyedCollection<string>(comparer, 3);
            collection.GetKeyForItemHandler = item => item + "_key";
            collection.Add("first");
            collection.Add("second");

            AssertExtensions.Throws<ArgumentException>("key", null, () => collection.ChangeItemKey("first", newKey));
        }

        [Fact]
        public void ChangeItemKey_NoSuchItem_ThrowsArgumentException()
        {
            var collection = new StringKeyedCollection<string>(StringComparer.OrdinalIgnoreCase, 3);
            collection.GetKeyForItemHandler = item => item + "_key";

            // Empty.
            AssertExtensions.Throws<ArgumentException>("item", null, () => collection.ChangeItemKey("NoSuchItem", "other_key"));
            AssertExtensions.Throws<ArgumentException>("item", null, () => collection.ChangeItemKey("FIRST", "other_key"));

            // Without dictionary.
            collection.Add("first");
            AssertExtensions.Throws<ArgumentException>("item", null, () => collection.ChangeItemKey("NoSuchItem", "other_key"));
            AssertExtensions.Throws<ArgumentException>("item", null, () => collection.ChangeItemKey("FIRST", "other_key"));

            // With dictionary.
            collection.Add("second");
            collection.Add("third");
            collection.Add("fourth");
            AssertExtensions.Throws<ArgumentException>("item", null, () => collection.ChangeItemKey("NoSuchItem", "other_key"));
            AssertExtensions.Throws<ArgumentException>("item", null, () => collection.ChangeItemKey("FIRST", "other_key"));
        }

        [Theory]
        [InlineData("newKey")]
        [InlineData("10")]
        [InlineData("12")]
        public void ChangeItemKey_DifferentKeyAfterCreation_ThrowsArgumentException(string newKey)
        {
            var collection = new StringKeyedCollection<int>(null, 3);
            collection.GetKeyForItemHandler = item => item.ToString();
            collection.Add(1);
            collection.Add(2);
            collection.Add(3);
            Assert.Null(collection.Dictionary);

            collection.GetKeyForItemHandler = item => (item * 2).ToString();

            // Without dictionary.
            collection.ChangeItemKey(2, "10");
            Assert.Equal(new Dictionary<string, int>
                {
                    {"2", 1},
                    {"10", 2},
                    {"6", 3}
                }, collection.Dictionary
            );

            // With dictionary.
            collection.Add(4);
            AssertExtensions.Throws<ArgumentException>("item", null, () => collection.ChangeItemKey(2, newKey));
            Assert.Equal(new Dictionary<string, int>
                {
                    {"2", 1},
                    {"10", 2},
                    {"6", 3},
                    {"8", 4}
                }, collection.Dictionary
            );
        }

        public static IEnumerable<object[]> SetItem_TestData()
        {
            // Exceeding threshold.
            yield return new object[] { null, 1, "first", new Dictionary<string, string> { { "first_key", "first" }, { "second_key", "second" }, { "third_key", "third" } } };
            yield return new object[] { null, 1, "FIRST", new Dictionary<string, string> { { "FIRST_key", "FIRST" }, { "second_key", "second" }, { "third_key", "third" } } };
            yield return new object[] { null, 1, "other", new Dictionary<string, string> { { "other_key", "other" }, { "second_key", "second" }, { "third_key", "third" } } };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, 1, "first", new Dictionary<string, string> { { "first_key", "first" }, { "second_key", "second" }, { "third_key", "third" } } };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, 1, "FIRST", new Dictionary<string, string> { { "first_key", "FIRST" }, { "second_key", "second" }, { "third_key", "third" } } };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, 1, "other", new Dictionary<string, string> { { "other_key", "other" }, { "second_key", "second" }, { "third_key", "third" } } };

            // Meeting threshold.
            yield return new object[] { null, 3, "first", null };
            yield return new object[] { null, 3, "FIRST", new Dictionary<String, String> { { "FIRST_key", "FIRST" }, { "second_key", "second" }, { "third_key", "third" } } };
            yield return new object[] { null, 3, "other", new Dictionary<String, String> { { "other_key", "other" }, { "second_key", "second" }, { "third_key", "third" } } };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, 3, "first", null };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, 3, "FIRST", null };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, 3, "other", new Dictionary<String, String> { { "other_key", "other" }, { "second_key", "second" }, { "third_key", "third" } } };

            // Not meeting threshold.
            yield return new object[] { null, 4, "first", null };
            yield return new object[] { null, 4, "FIRST", null };
            yield return new object[] { null, 4, "other", null };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, 4, "first", null };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, 4, "FIRST", null };
            yield return new object[] { StringComparer.OrdinalIgnoreCase, 4, "other", null };
        }

        [Theory]
        [MemberData(nameof(SetItem_TestData))]
        public void SetItem_SameValue_Success(IEqualityComparer<string> comparer, int dictionaryCreationThreshold, string value, Dictionary<string, string> expectedDictionary)
        {
            var collection = new StringKeyedCollection<string>(comparer, dictionaryCreationThreshold);
            collection.GetKeyForItemHandler = item => item + "_key";
            collection.Add("first");
            collection.Add("second");
            collection.Add("third");

            collection[0] = value;
            Assert.Equal(new string[] { value, "second", "third" }, collection.Items.Cast<string>());
            Assert.Equal(expectedDictionary, collection.Dictionary);
        }

        [Fact]
        public void SetItem_NullNewKey_RemovesKey()
        {
            var collection = new StringKeyedCollection<int>();
            collection.GetKeyForItemHandler = item => item.ToString();
            collection.Add(1);
            Assert.NotEmpty(collection.Dictionary);

            // Don't add even numbers.
            collection.GetKeyForItemHandler = item => item % 2 == 0 ? null : item.ToString();

            collection[0] = 2;
            Assert.Equal(new int[] { 2 }, collection.Items.Cast<int>());
            Assert.Empty(collection.Dictionary);
        }

        [Fact]
        public void SetItem_NullOldKey_DoesNotRemoveOldKey()
        {
            var collection = new StringKeyedCollection<int>();
            collection.GetKeyForItemHandler = item => item.ToString();
            collection.Add(2);
            Assert.NotEmpty(collection.Dictionary);

            // Don't add even numbers.
            collection.GetKeyForItemHandler = item => item % 2 == 0 ? null : item.ToString();

            collection[0] = 1;
            Assert.Equal(new int[] { 1 }, collection.Items.Cast<int>());
            Assert.Equal(new Dictionary<string, int>
                {
                    { "1", 1 },
                    { "2", 2 }
                }, collection.Dictionary
            );
        }

        [Fact]
        public void SetItem_NullNewAndOldKey_DoesNotAffectDictionary()
        {
            var collection = new StringKeyedCollection<int>();
            collection.GetKeyForItemHandler = item => item.ToString();
            collection.Add(2);
            Assert.NotEmpty(collection.Dictionary);

            // Don't add even numbers.
            collection.GetKeyForItemHandler = item => item % 2 == 0 ? null : item.ToString();

            collection[0] = 4;
            Assert.Equal(new int[] { 4 }, collection.Items.Cast<int>());
            Assert.Equal(new Dictionary<string, int>
                {
                    { "2", 2 }
                }, collection.Dictionary
            );
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        public void SetItem_InvalidIndex_ThrowsArgumentOutOfRangeException(int index)
        {
            var collection = new StringKeyedCollection<string>(null, 3);
            collection.GetKeyForItemHandler = item => item + "_key";
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collection.SetItem(index, "first"));
        }

        private class StringKeyedCollection<TItem> : KeyedCollection<string, TItem>
        {
            public StringKeyedCollection() : base()
            {
            }

            public StringKeyedCollection(IEqualityComparer<string> comparer) : base(comparer)
            {
            }

            public StringKeyedCollection(IEqualityComparer<string> comparer, int dictionaryCreationThreshold) : base(comparer, dictionaryCreationThreshold)
            {
            }

            public Func<TItem, string> GetKeyForItemHandler { get; set; }

            protected override string GetKeyForItem(TItem item)
            {
                return GetKeyForItemHandler(item);
            }

            public new IDictionary<string, TItem> Dictionary => base.Dictionary;

            public new IList<TItem> Items => base.Items;

            public new void InsertItem(int index, TItem item)
            {
                base.InsertItem(index, item);
            }

            public new void RemoveItem(int index)
            {
                base.RemoveItem(index);
            }

            public new void ClearItems()
            {
                base.ClearItems();
            }

            public new void ChangeItemKey(TItem item, string newKey)
            {
                base.ChangeItemKey(item, newKey);
            }

            public new void SetItem(int index, TItem item)
            {
                base.SetItem(index, item);
            }
        }
    }
}
