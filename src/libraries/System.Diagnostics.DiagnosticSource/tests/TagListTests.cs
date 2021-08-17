// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Diagnostics.Tests
{
    public class TagListTests
    {
        [Fact]
        public void TestConstruction()
        {
            for (int i = 0; i < 30; i++)
            {
                CreateTagList(i, out TagList tagList);
                ValidateTags(in tagList, i);
                Assert.False(tagList.IsReadOnly);

                KeyValuePair<string, object?>[] array = new KeyValuePair<string, object?>[tagList.Count];
                tagList.CopyTo(array);
                TagList list = new TagList(array.AsSpan());
                ValidateTags(in tagList, i);
            }
        }

        [Fact]
        public void TestInlineInitialization()
        {
            TagList list = new TagList
            {
                { "Some Key", "Some Value" },
                { "Some Other Key", 42 }
            };
            Assert.Equal("Some Key", list[0].Key);
            Assert.Equal("Some Value", list[0].Value);
            Assert.Equal("Some Other Key", list[1].Key);
            Assert.Equal(42, list[1].Value);
        }

        [Fact]
        public void TestClear()
        {
            for (int i = 0; i < 30; i++)
            {
                CreateTagList(i, out TagList tagList);
                Assert.Equal(i, tagList.Count);
                tagList.Clear();
                Assert.Equal(0, tagList.Count);
            }
        }

        [Fact]
        public void TestSearchOperations()
        {
            for (int i = 0; i < 30; i++)
            {
                CreateTagList(i, out TagList tagList);
                KeyValuePair<string, object?>[] array = new KeyValuePair<string, object?>[tagList.Count];
                tagList.CopyTo(array);

                for (int j = 0; j < array.Length; j++)
                {
                    Assert.True(tagList.Contains(array[j]));
                    Assert.Equal(j, tagList.IndexOf(array[j]));
                }

                Assert.False(tagList.Contains(new KeyValuePair<string, object?>("Not Exist Key", "Not Exist Value")));
                Assert.Equal(-1, tagList.IndexOf(new KeyValuePair<string, object?>("Not Exist Other Key", "Not Exist Other Value")));
            }
        }

        [Fact]
        public void TestCopyTo()
        {
            for (int i = 0; i < 20; i++)
            {
                CreateTagList(i, out TagList tagList);
                KeyValuePair<string, object?>[] array = new KeyValuePair<string, object?>[tagList.Count];
                tagList.CopyTo(array.AsSpan());
                ValidateTags(tagList, array);
                array = new KeyValuePair<string, object?>[tagList.Count];
                tagList.CopyTo(array);
                ValidateTags(tagList, array);
            }
        }

        [Fact]
        public void TestInsert()
        {
            TagList list = new TagList();
            Assert.Equal(0, list.Count);
            list.Insert(0, new KeyValuePair<string, object?>("Key0", 0));
            Assert.Equal(1, list.Count);
            Assert.Equal("Key0", list[0].Key);
            Assert.Equal(0, list[0].Value);

            // Insert at the end
            for (int i = 1; i < 20; i++)
            {
                list.Insert(i, new KeyValuePair<string, object?>("Key" + i, i));
                Assert.Equal(i + 1, list.Count);
                Assert.Equal("Key" + i, list[i].Key);
                Assert.Equal(i, list[i].Value);
            }

            // Insert at begining
            int count = list.Count;
            for (int i = 1; i < 10; i++)
            {
                list.Insert(0, new KeyValuePair<string, object?>("Key-" + i, i + count));
                Assert.Equal(count + i, list.Count);
                Assert.Equal("Key-" + i, list[0].Key);
                Assert.Equal(i + count, list[0].Value);
            }

            // Insert in the middle
            count = list.Count;
            int pos = count / 2;

            KeyValuePair<string, object?> firstItem = list[0];
            KeyValuePair<string, object?> lastItem = list[count - 1];

            for (int i = 1; i < 10; i++)
            {
                list.Insert(pos, new KeyValuePair<string, object?>("Key+" + i, i + count));
                Assert.Equal(count + i, list.Count);
                Assert.Equal("Key+" + i, list[pos].Key);
                Assert.Equal(i + count, list[pos].Value);

                Assert.Equal(firstItem.Key, list[0].Key);
                Assert.Equal(firstItem.Value, list[0].Value);
                Assert.Equal(lastItem.Key, list[list.Count - 1].Key);
                Assert.Equal(lastItem.Value, list[list.Count - 1].Value);
            }

            // Test insert when having less than 8 tags
            list = new TagList();
            Assert.Equal(0, list.Count);

            list.Insert(0, new KeyValuePair<string, object?>("Key!0", 0));
            Assert.Equal(1, list.Count);
            Assert.Equal("Key!0", list[0].Key);
            Assert.Equal(0, list[0].Value);

            list.Insert(1, new KeyValuePair<string, object?>("Key!1", 100));
            Assert.Equal(2, list.Count);
            Assert.Equal("Key!1", list[1].Key);
            Assert.Equal(100, list[1].Value);

            list.Insert(0, new KeyValuePair<string, object?>("Key!00", 1000));
            Assert.Equal(3, list.Count);
            Assert.Equal("Key!00", list[0].Key);
            Assert.Equal(1000, list[0].Value);

            list.Insert(3, new KeyValuePair<string, object?>("Key!300", 3000));
            Assert.Equal(4, list.Count);
            Assert.Equal("Key!300", list[3].Key);
            Assert.Equal(3000, list[3].Value);

            for (int i = 1; i < 10; i++)
            {
                list.Insert(2, new KeyValuePair<string, object?>("Key!200" +i , i * 200));
                Assert.Equal(4 + i, list.Count);
                Assert.Equal("Key!200" + i, list[2].Key);
                Assert.Equal(i * 200, list[2].Value);
            }
        }

        [Fact]
        public void TestRemove()
        {
            TagList list = new TagList();
            // Test first with up to 8 tags
            for (int i = 1; i <= 8; i++)
            {
                KeyValuePair<string, object?> kvp = new KeyValuePair<string, object?>("k" + i, "v" + i);
                list.Add(kvp);
                Assert.Equal(i, list.Count);
                Assert.True(list.Contains(kvp));
                Assert.Equal(i - 1, list.IndexOf(kvp));
            }

            // Now remove items

            int count = list.Count;
            for (int i = 1; i <= 8; i++)
            {
                KeyValuePair<string, object?> kvp = new KeyValuePair<string, object?>("k" + i, "v" + i);
                Assert.True(list.Remove(kvp));
                Assert.Equal(count - i, list.Count);
                Assert.False(list.Contains(kvp));
                Assert.Equal(-1, list.IndexOf(kvp));
            }

            Assert.Equal(0, list.Count);

            // Now we want to test more than 8 tags and test RemoveAt too
            for (int i = 1; i <= 20; i++)
            {
                KeyValuePair<string, object?> kvp1 = new KeyValuePair<string, object?>("k-" + i, "v" + i);
                KeyValuePair<string, object?> kvp2 = new KeyValuePair<string, object?>("k-" + i * 100, "v" + i * 100);
                KeyValuePair<string, object?> kvp3 = new KeyValuePair<string, object?>("k-" + i * 1000, "v" + i * 1000);

                // We add 3 then remove 2.
                list.Add(kvp1);
                list.Add(kvp2);
                list.Add(kvp3);

                // Now remove 1
                Assert.True(list.Contains(kvp3));
                Assert.True(list.Remove(kvp3));
                Assert.False(list.Contains(kvp3));

                int index = list.IndexOf(kvp2);
                Assert.True(index >= 0);
                Assert.True(list.Contains(kvp2));
                list.RemoveAt(index);
                Assert.False(list.Contains(kvp2));

                Assert.True(list.Contains(kvp1));
            }

            Assert.Equal(20, list.Count);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(8)]
        [InlineData(10)]
        [InlineData(100)]
        public void TestEnumerator(int count)
        {
            CreateTagList(count, out TagList tagList);
            KeyValuePair<string, object?>[] array = new KeyValuePair<string, object?>[tagList.Count];
            tagList.CopyTo(array);

                Assert.Equal(count, tagList.Count);
            int i = 0;
            foreach (KeyValuePair<string, object?> kvp in tagList)
            {
                Assert.Equal(array[i].Key, kvp.Key);
                Assert.Equal(array[i].Value, kvp.Value);
                i++;
            }
            Assert.Equal(i, tagList.Count);

            IEnumerator<KeyValuePair<string, object?>> enumerator = tagList.GetEnumerator();
            i = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(array[i].Key, enumerator.Current.Key);
                Assert.Equal(array[i].Value, enumerator.Current.Value);
                i++;
            }
            Assert.Equal(i, tagList.Count);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(7)]
        [InlineData(9)]
        [InlineData(20)]
        public void TestIndex(int count)
        {
            CreateTagList(count, out TagList tagList);
            Assert.Equal(count, tagList.Count);

            ValidateTags(in tagList, count); // It calls the indexer getter.

            for (int i = 0; i < count; i++)
            {
                tagList[i] = new KeyValuePair<string, object?>("NewKey" + i, i);
                Assert.Equal("NewKey" + i, tagList[i].Key);
                Assert.Equal(i, tagList[i].Value);
            }
        }

        [Fact]
        public void TestNegativeCases()
        {
            TagList list = new TagList { new KeyValuePair<string, object?>("1", 1), new KeyValuePair<string, object?>("2", 2) } ;
            KeyValuePair<string, object?> kvp = default;

            Assert.Throws<ArgumentOutOfRangeException>(() => kvp = list[2]);
            Assert.Throws<ArgumentOutOfRangeException>(() => list[2] = kvp);
            Assert.Throws<ArgumentOutOfRangeException>(() => kvp = list[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => list[-2] = kvp);

            KeyValuePair<string, object?>[] array = new KeyValuePair<string, object?>[1];
            Assert.Throws<ArgumentException>(() => list.CopyTo(array.AsSpan()));
            Assert.Throws<ArgumentException>(() => list.CopyTo(array, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.CopyTo(array, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.CopyTo(array, -1));
            Assert.Throws<ArgumentNullException>(() => list.CopyTo(null, 0));

            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, default));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(3, default));

            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(2));
        }

        private void ValidateTags(in TagList tagList, KeyValuePair<string, object?>[] array)
        {
            Assert.True(tagList.Count <= array.Length);
            for (int i = 0; i < tagList.Count; i++)
            {
                Assert.Equal(array[i].Key, tagList[i].Key);
                Assert.Equal(array[i].Value, tagList[i].Value);
            }
        }

        private void ValidateTags(in TagList tagList, int tagsCount)
        {
            Assert.Equal(tagsCount, tagList.Count);
            for (int i = 0; i < tagList.Count; i++)
            {
                Assert.Equal("Key"+i, tagList[i].Key);
                Assert.Equal("Value"+i, tagList[i].Value);
            }
        }

        private void CreateTagList(int tagsCount, out TagList tagList)
        {
            tagList = new TagList();
            for (int i = 0; i < tagsCount; i++)
            {
                tagList.Add("Key" + i, "Value" + i);
            }
        }
    }
}


