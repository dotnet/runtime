// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public partial class ToLookupTests : EnumerableTests
    {
        private static void AssertMatches<K, T>(IEnumerable<K> keys, IEnumerable<T> elements, System.Linq.ILookup<K, T> lookup)
        {
            Assert.NotNull(lookup);
            Assert.NotNull(keys);
            Assert.NotNull(elements);

            int num = 0;
            using (IEnumerator<K> keyEnumerator = keys.GetEnumerator())
            using (IEnumerator<T> elEnumerator = elements.GetEnumerator())
            {
                while (keyEnumerator.MoveNext())
                {
                    Assert.True(lookup.Contains(keyEnumerator.Current));

                    foreach (T e in lookup[keyEnumerator.Current])
                    {
                        Assert.True(elEnumerator.MoveNext());
                        Assert.Equal(e, elEnumerator.Current);
                    }
                    ++num;
                }
                Assert.False(elEnumerator.MoveNext());
            }
            Assert.Equal(num, lookup.Count);
        }

        [Fact]
        public void SameResultsRepeatCall()
        {
            var q1 = from x1 in new string[] { "Alen", "Felix", null, null, "X", "Have Space", "Clinton", "" }
                     select x1;

            var q2 = from x2 in new int[] { 55, 49, 9, -100, 24, 25, -1, 0 }
                     select x2;

            var q = from x3 in q1
                    from x4 in q2
                    select new { a1 = x3, a2 = x4 };

            Assert.Equal(q.ToLookup(e => e.a1), q.ToLookup(e => e.a1));
        }

        [Fact]
        public void Empty()
        {
            AssertMatches(Enumerable.Empty<int>(), Enumerable.Empty<int>(), Enumerable.Empty<int>().ToLookup(i => i));
            Assert.False(Enumerable.Empty<int>().ToLookup(i => i).Contains(0));
            Assert.Empty(Enumerable.Empty<int>().ToLookup(i => i)[0]);
        }

        [Fact]
        public void NullKeyIncluded()
        {
            string[] key = { "Chris", "Bob", null, "Tim" };
            int[] element = { 50, 95, 55, 90 };
            var source = key.Zip(element, (k, e) => new { Name = k, Score = e });

            AssertMatches(key, source, source.ToLookup(e => e.Name));
        }

        [Fact]
        public void OneElementCustomComparer()
        {
            string[] key = { "Chris" };
            int[] element = { 50 };
            var source = new [] { new {Name = "risCh", Score = 50} };

            AssertMatches(key, source, source.ToLookup(e => e.Name, new AnagramEqualityComparer()));
        }

        [Fact]
        public void UniqueElementsElementSelector()
        {
            string[] key = { "Chris", "Prakash", "Tim", "Robert", "Brian" };
            int[] element = { 50, 100, 95, 60, 80 };
            var source = new []
            {
                new { Name = key[0], Score = element[0] },
                new { Name = key[1], Score = element[1] },
                new { Name = key[2], Score = element[2] },
                new { Name = key[3], Score = element[3] },
                new { Name = key[4], Score = element[4] }
            };

            AssertMatches(key, element, source.ToLookup(e => e.Name, e => e.Score));
        }

        [Fact]
        public void DuplicateKeys()
        {
            string[] key = { "Chris", "Prakash", "Robert" };
            int[] element = { 50, 80, 100, 95, 99, 56 };
            var source = new[]
            {
                new { Name = key[0], Score = element[0] },
                new { Name = key[1], Score = element[2] },
                new { Name = key[2], Score = element[5] },
                new { Name = key[1], Score = element[3] },
                new { Name = key[0], Score = element[1] },
                new { Name = key[1], Score = element[4] }
            };

            AssertMatches(key, element, source.ToLookup(e => e.Name, e => e.Score, new AnagramEqualityComparer()));
        }

        [Fact]
        public void RunOnce()
        {
            string[] key = { "Chris", "Prakash", "Robert" };
            int[] element = { 50, 80, 100, 95, 99, 56 };
            var source = new[]
            {
                new { Name = key[0], Score = element[0] },
                new { Name = key[1], Score = element[2] },
                new { Name = key[2], Score = element[5] },
                new { Name = key[1], Score = element[3] },
                new { Name = key[0], Score = element[1] },
                new { Name = key[1], Score = element[4] }
            };

            AssertMatches(key, element, source.RunOnce().ToLookup(e => e.Name, e => e.Score, new AnagramEqualityComparer()));
        }

        [Fact]
        public void Count()
        {
            string[] key = { "Chris", "Prakash", "Robert" };
            int[] element = { 50, 80, 100, 95, 99, 56 };
            var source = new[]
            {
                new { Name = key[0], Score = element[0] },
                new { Name = key[1], Score = element[2] },
                new { Name = key[2], Score = element[5] },
                new { Name = key[1], Score = element[3] },
                new { Name = key[0], Score = element[1] },
                new { Name = key[1], Score = element[4] }
            };

            Assert.Equal(3, source.ToLookup(e => e.Name, e => e.Score).Count());
        }

        [Fact]
        public void EmptySource()
        {
            string[] key = { };
            int[] element = { };
            var source = key.Zip(element, (k, e) => new { Name = k, Score = e });

            AssertMatches(key, element, source.ToLookup(e => e.Name, e => e.Score, new AnagramEqualityComparer()));
        }

        [Fact]
        public void SingleNullKeyAndElement()
        {
            string[] key = { null };
            string[] element = { null };
            string[] source = new string[] { null };

            AssertMatches(key, element, source.ToLookup(e => e, e => e, EqualityComparer<string>.Default));
        }

        [Fact]
        public void NullSource()
        {
            IEnumerable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.ToLookup(i => i / 10));
        }

        [Fact]
        public void NullSourceExplicitComparer()
        {
            IEnumerable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.ToLookup(i => i / 10, EqualityComparer<int>.Default));
        }

        [Fact]
        public void NullSourceElementSelector()
        {
            IEnumerable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.ToLookup(i => i / 10, i => i + 2));
        }

        [Fact]
        public void NullSourceElementSelectorExplicitComparer()
        {
            IEnumerable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.ToLookup(i => i / 10, i => i + 2, EqualityComparer<int>.Default));
        }

        [Fact]
        public void NullKeySelector()
        {
            Func<int, int> keySelector = null;
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => Enumerable.Range(0, 1000).ToLookup(keySelector));
        }

        [Fact]
        public void NullKeySelectorExplicitComparer()
        {
            Func<int, int> keySelector = null;
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => Enumerable.Range(0, 1000).ToLookup(keySelector, EqualityComparer<int>.Default));
        }

        [Fact]
        public void NullKeySelectorElementSelector()
        {
            Func<int, int> keySelector = null;
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => Enumerable.Range(0, 1000).ToLookup(keySelector, i => i + 2));
        }

        [Fact]
        public void NullKeySelectorElementSelectorExplicitComparer()
        {
            Func<int, int> keySelector = null;
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => Enumerable.Range(0, 1000).ToLookup(keySelector, i => i + 2, EqualityComparer<int>.Default));
        }

        [Fact]
        public void NullElementSelector()
        {
            Func<int, int> elementSelector = null;
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => Enumerable.Range(0, 1000).ToLookup(i => i / 10, elementSelector));
        }

        [Fact]
        public void NullElementSelectorExplicitComparer()
        {
            Func<int, int> elementSelector = null;
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => Enumerable.Range(0, 1000).ToLookup(i => i / 10, elementSelector, EqualityComparer<int>.Default));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ApplyResultSelectorForGroup(int enumType)
        {
            //Create test data
            var roles = new List<Role>
            {
                new Role { Id = 1 },
                new Role { Id = 2 },
                new Role { Id = 3 },
            };

            var memberships = Enumerable.Range(0, 50).Select(i => new Membership
            {
                Id = i,
                Role = roles[i % 3],
                CountMe = i % 3 == 0
            });

            //Run actual test
            var grouping = memberships.GroupBy(
                m => m.Role,
                (role, mems) => new RoleMetadata
                {
                    Role = role,
                    CountA = mems.Count(m => m.CountMe),
                    CountrB = mems.Count(m => !m.CountMe)
                });

            IEnumerable<RoleMetadata> result;
            switch (enumType)
            {
                case 1:
                    result = grouping.ToList();
                    break;
                case 2:
                    result = grouping.ToArray();
                    break;
                default:
                    result = grouping;
                    break;
            }

            var expected = new[]
            {
                new RoleMetadata {Role = new Role {Id = 1}, CountA = 17, CountrB = 0 },
                new RoleMetadata {Role = new Role {Id = 2}, CountA = 0, CountrB = 17 },
                new RoleMetadata {Role = new Role {Id = 3}, CountA = 0, CountrB = 16 }
            };

            Assert.Equal(expected, result);
        }

        [Fact]
        public void ApplyResultSelector()
        {
            Lookup<int, int> lookup = (Lookup<int, int>)new int[] { 1, 2, 2, 3, 3, 3 }.ToLookup(i => i);
            IEnumerable<int> sums = lookup.ApplyResultSelector((key, elements) =>
            {
                Assert.Equal(key, elements.Count());
                return elements.Sum();
            });
            Assert.Equal([1, 4, 9], sums);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        public void LookupImplementsICollection(int count)
        {
            Assert.IsAssignableFrom<ICollection<IGrouping<string, int>>>(Enumerable.Range(0, count).ToLookup(i => i.ToString()));
            Assert.IsAssignableFrom<ICollection<IGrouping<string, int>>>(Enumerable.Range(0, count).ToLookup(i => i.ToString(), StringComparer.OrdinalIgnoreCase));
            Assert.IsAssignableFrom<ICollection<IGrouping<string, int>>>(Enumerable.Range(0, count).ToLookup(i => i.ToString(), i => i));
            Assert.IsAssignableFrom<ICollection<IGrouping<string, int>>>(Enumerable.Range(0, count).ToLookup(i => i.ToString(), i => i, StringComparer.OrdinalIgnoreCase));

            var collection = (ICollection<IGrouping<string, int>>)Enumerable.Range(0, count).ToLookup(i => i.ToString());
            Assert.Equal(count, collection.Count);
            Assert.True(collection.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => collection.Add(null));
            Assert.Throws<NotSupportedException>(() => collection.Remove(null));
            Assert.Throws<NotSupportedException>(() => collection.Clear());

            if (count > 0)
            {
                IGrouping<string, int> first = collection.First();
                IGrouping<string, int> last = collection.Last();
                Assert.True(collection.Contains(first));
                Assert.True(collection.Contains(last));
            }
            Assert.False(collection.Contains(new NopGrouping()));

            IGrouping<string, int>[] items = new IGrouping<string, int>[count];
            collection.CopyTo(items, 0);
            Assert.Equal(collection.Select(i => i), items);
            Assert.Equal(items, Enumerable.Range(0, count).ToLookup(i => i.ToString()).ToArray());
            Assert.Equal(items, Enumerable.Range(0, count).ToLookup(i => i.ToString()).ToList());
        }

        private sealed class NopGrouping : IGrouping<string, int>
        {
            public string Key => "";
            public IEnumerator<int> GetEnumerator() => ((IList<int>)Array.Empty<int>()).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public class Membership
        {
            public int Id { get; set; }
            public Role Role { get; set; }

            public bool CountMe { get; set; }
        }

        public class Role : IEquatable<Role>
        {
            public int Id { get; set; }

            public bool Equals(Role other) => other != null && Id == other.Id;

            public override bool Equals(object obj) => Equals(obj as Role);

            public override int GetHashCode() => Id;
        }

        public class RoleMetadata : IEquatable<RoleMetadata>
        {
            public Role Role { get; set; }
            public int CountA { get; set; }
            public int CountrB { get; set; }

            public bool Equals(RoleMetadata other)
                => other != null && Role.Equals(other.Role) && CountA == other.CountA && CountrB == other.CountrB;

            public override bool Equals(object obj) => Equals(obj as RoleMetadata);

            public override int GetHashCode() => Role.GetHashCode() * 31 + CountA + CountrB;
        }
    }
}
