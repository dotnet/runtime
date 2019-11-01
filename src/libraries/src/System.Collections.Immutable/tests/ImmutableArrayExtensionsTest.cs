// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public class ImmutableArrayExtensionsTest
    {
        private static readonly ImmutableArray<int> s_emptyDefault = default(ImmutableArray<int>);
        private static readonly ImmutableArray<int> s_empty = ImmutableArray.Create<int>();
        private static readonly ImmutableArray<int> s_oneElement = ImmutableArray.Create(1);
        private static readonly ImmutableArray<int> s_manyElements = ImmutableArray.Create(1, 2, 3);
        private static readonly ImmutableArray<GenericParameterHelper> s_oneElementRefType = ImmutableArray.Create(new GenericParameterHelper(1));
        private static readonly ImmutableArray<string> s_twoElementRefTypeWithNull = ImmutableArray.Create("1", null);

        private static readonly ImmutableArray<int>.Builder s_emptyBuilder = ImmutableArray.Create<int>().ToBuilder();
        private static readonly ImmutableArray<int>.Builder s_oneElementBuilder = ImmutableArray.Create<int>(1).ToBuilder();
        private static readonly ImmutableArray<int>.Builder s_manyElementsBuilder = ImmutableArray.Create<int>(1, 2, 3).ToBuilder();

        [Fact]
        public void Select()
        {
            Assert.Equal(new[] { 4, 5, 6 }, ImmutableArrayExtensions.Select(s_manyElements, n => n + 3));
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ImmutableArrayExtensions.Select<int, bool>(s_manyElements, null));
        }

        [Fact]
        public void SelectEmptyDefault()
        {
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => ImmutableArrayExtensions.Select<int, bool>(s_emptyDefault, null));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => ImmutableArrayExtensions.Select(s_emptyDefault, n => true));
        }

        [Fact]
        public void SelectEmpty()
        {
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ImmutableArrayExtensions.Select<int, bool>(s_empty, null));
            Assert.False(ImmutableArrayExtensions.Select(s_empty, n => true).Any());
        }

        [Fact]
        public void SelectMany()
        {
            Func<int, IEnumerable<int>> collectionSelector = i => Enumerable.Range(i, 10);
            Func<int, int, int> resultSelector = (i, e) => e * 2;
            foreach (var arr in new[] { s_empty, s_oneElement, s_manyElements })
            {
                Assert.Equal(
                    Enumerable.SelectMany(arr, collectionSelector, resultSelector),
                    ImmutableArrayExtensions.SelectMany(arr, collectionSelector, resultSelector));
            }

            TestExtensionsMethods.ValidateDefaultThisBehavior(() => ImmutableArrayExtensions.SelectMany<int, int, int>(s_emptyDefault, null, null));
            AssertExtensions.Throws<ArgumentNullException>("collectionSelector", () =>
                ImmutableArrayExtensions.SelectMany<int, int, int>(s_manyElements, null, (i, e) => e));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () =>
                ImmutableArrayExtensions.SelectMany<int, int, int>(s_manyElements, i => new[] { i }, null));
        }

        [Fact]
        public void Where()
        {
            Assert.Equal(new[] { 2, 3 }, ImmutableArrayExtensions.Where(s_manyElements, n => n > 1));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ImmutableArrayExtensions.Where(s_manyElements, null));
        }

        [Fact]
        public void WhereEmptyDefault()
        {
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => ImmutableArrayExtensions.Where(s_emptyDefault, null));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => ImmutableArrayExtensions.Where(s_emptyDefault, n => true));
        }

        [Fact]
        public void WhereEmpty()
        {
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ImmutableArrayExtensions.Where(s_empty, null));
            Assert.False(ImmutableArrayExtensions.Where(s_empty, n => true).Any());
        }

        [Fact]
        public void Any()
        {
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ImmutableArrayExtensions.Any(s_oneElement, null));
            Assert.True(ImmutableArrayExtensions.Any(s_oneElement));
            Assert.True(ImmutableArrayExtensions.Any(s_manyElements, n => n == 2));
            Assert.False(ImmutableArrayExtensions.Any(s_manyElements, n => n == 4));
            Assert.True(ImmutableArrayExtensions.Any(s_oneElementBuilder));
        }

        [Fact]
        public void AnyEmptyDefault()
        {
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => ImmutableArrayExtensions.Any(s_emptyDefault));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => ImmutableArrayExtensions.Any(s_emptyDefault, n => true));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => ImmutableArrayExtensions.Any(s_emptyDefault, null));
        }

        [Fact]
        public void AnyEmpty()
        {
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ImmutableArrayExtensions.Any(s_empty, null));
            Assert.False(ImmutableArrayExtensions.Any(s_empty));
            Assert.False(ImmutableArrayExtensions.Any(s_empty, n => true));
        }

        [Fact]
        public void All()
        {
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ImmutableArrayExtensions.All(s_oneElement, null));
            Assert.False(ImmutableArrayExtensions.All(s_manyElements, n => n == 2));
            Assert.True(ImmutableArrayExtensions.All(s_manyElements, n => n > 0));
        }

        [Fact]
        public void AllEmptyDefault()
        {
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => ImmutableArrayExtensions.All(s_emptyDefault, n => true));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => ImmutableArrayExtensions.All(s_emptyDefault, null));
        }

        [Fact]
        public void AllEmpty()
        {
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ImmutableArrayExtensions.All(s_empty, null));
            Assert.True(ImmutableArrayExtensions.All(s_empty, n => { throw new ShouldNotBeInvokedException(); })); // predicate should never be invoked.
        }

        [Fact]
        public void SequenceEqual()
        {
            AssertExtensions.Throws<ArgumentNullException>("items", () => ImmutableArrayExtensions.SequenceEqual(s_oneElement, (IEnumerable<int>)null));

            foreach (IEqualityComparer<int> comparer in new[] { null, EqualityComparer<int>.Default })
            {
                Assert.True(ImmutableArrayExtensions.SequenceEqual(s_manyElements, s_manyElements, comparer));
                Assert.True(ImmutableArrayExtensions.SequenceEqual(s_manyElements, (IEnumerable<int>)s_manyElements.ToArray(), comparer));
                Assert.True(ImmutableArrayExtensions.SequenceEqual(s_manyElements, ImmutableArray.Create(s_manyElements.ToArray()), comparer));

                Assert.False(ImmutableArrayExtensions.SequenceEqual(s_manyElements, s_oneElement, comparer));
                Assert.False(ImmutableArrayExtensions.SequenceEqual(s_manyElements, (IEnumerable<int>)s_oneElement.ToArray(), comparer));
                Assert.False(ImmutableArrayExtensions.SequenceEqual(s_manyElements, ImmutableArray.Create(s_oneElement.ToArray()), comparer));
                Assert.False(ImmutableArrayExtensions.SequenceEqual(s_manyElements, (IEnumerable<int>)s_manyElements.Add(1).ToArray(), comparer));
                Assert.False(ImmutableArrayExtensions.SequenceEqual(s_manyElements.Add(1), s_manyElements.Add(2).ToArray(), comparer));
                Assert.False(ImmutableArrayExtensions.SequenceEqual(s_manyElements.Add(1), (IEnumerable<int>)s_manyElements.Add(2).ToArray(), comparer));
            }

            Assert.True(ImmutableArrayExtensions.SequenceEqual(s_manyElements, s_manyElements, (a, b) => true));

            Assert.False(ImmutableArrayExtensions.SequenceEqual(s_manyElements, s_oneElement, (a, b) => a == b));
            Assert.False(ImmutableArrayExtensions.SequenceEqual(s_manyElements.Add(1), s_manyElements.Add(2), (a, b) => a == b));
            Assert.True(ImmutableArrayExtensions.SequenceEqual(s_manyElements.Add(1), s_manyElements.Add(1), (a, b) => a == b));

            Assert.False(ImmutableArrayExtensions.SequenceEqual(s_manyElements, ImmutableArray.Create(s_manyElements.ToArray()), (a, b) => false));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ImmutableArrayExtensions.SequenceEqual(s_oneElement, s_oneElement, (Func<int, int, bool>)null));
        }

        [Fact]
        public void SequenceEqualEmptyDefault()
        {
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => ImmutableArrayExtensions.SequenceEqual(s_oneElement, s_emptyDefault));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => ImmutableArrayExtensions.SequenceEqual(s_emptyDefault, s_empty));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => ImmutableArrayExtensions.SequenceEqual(s_emptyDefault, s_emptyDefault));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ImmutableArrayExtensions.SequenceEqual(s_emptyDefault, s_emptyDefault, (Func<int, int, bool>)null));
        }

        [Fact]
        public void SequenceEqualEmpty()
        {
            AssertExtensions.Throws<ArgumentNullException>("items", () => ImmutableArrayExtensions.SequenceEqual(s_empty, (IEnumerable<int>)null));
            Assert.True(ImmutableArrayExtensions.SequenceEqual(s_empty, s_empty));
            Assert.True(ImmutableArrayExtensions.SequenceEqual(s_empty, s_empty.ToArray()));
            Assert.True(ImmutableArrayExtensions.SequenceEqual(s_empty, s_empty, (a, b) => true));
            Assert.True(ImmutableArrayExtensions.SequenceEqual(s_empty, s_empty, (a, b) => false));
        }

        [Fact]
        public void Aggregate()
        {
            AssertExtensions.Throws<ArgumentNullException>("func", () => ImmutableArrayExtensions.Aggregate(s_oneElement, null));
            AssertExtensions.Throws<ArgumentNullException>("func", () => ImmutableArrayExtensions.Aggregate(s_oneElement, 1, null));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => ImmutableArrayExtensions.Aggregate<int, int, int>(s_oneElement, 1, null, null));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => ImmutableArrayExtensions.Aggregate<int, int, int>(s_oneElement, 1, (a, b) => a + b, null));

            Assert.Equal(Enumerable.Aggregate(s_manyElements, (a, b) => a * b), ImmutableArrayExtensions.Aggregate(s_manyElements, (a, b) => a * b));
            Assert.Equal(Enumerable.Aggregate(s_manyElements, 5, (a, b) => a * b), ImmutableArrayExtensions.Aggregate(s_manyElements, 5, (a, b) => a * b));
            Assert.Equal(Enumerable.Aggregate(s_manyElements, 5, (a, b) => a * b, a => -a), ImmutableArrayExtensions.Aggregate(s_manyElements, 5, (a, b) => a * b, a => -a));
        }

        [Fact]
        public void AggregateEmptyDefault()
        {
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.Aggregate(s_emptyDefault, (a, b) => a + b));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.Aggregate(s_emptyDefault, 1, (a, b) => a + b));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.Aggregate<int, int, int>(s_emptyDefault, 1, (a, b) => a + b, a => a));
        }

        [Fact]
        public void AggregateEmpty()
        {
            Assert.Equal(0, ImmutableArrayExtensions.Aggregate(s_empty, (a, b) => a + b));
            Assert.Equal(1, ImmutableArrayExtensions.Aggregate(s_empty, 1, (a, b) => a + b));
            Assert.Equal(1, ImmutableArrayExtensions.Aggregate<int, int, int>(s_empty, 1, (a, b) => a + b, a => a));
        }

        [Fact]
        public void ElementAt()
        {
            // Basis for some assertions that follow
            Assert.Throws<IndexOutOfRangeException>(() => Enumerable.ElementAt(s_empty, 0));
            Assert.Throws<IndexOutOfRangeException>(() => Enumerable.ElementAt(s_manyElements, -1));

            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.ElementAt(s_emptyDefault, 0));
            Assert.Throws<IndexOutOfRangeException>(() => ImmutableArrayExtensions.ElementAt(s_empty, 0));
            Assert.Throws<IndexOutOfRangeException>(() => ImmutableArrayExtensions.ElementAt(s_manyElements, -1));
            Assert.Equal(1, ImmutableArrayExtensions.ElementAt(s_oneElement, 0));
            Assert.Equal(3, ImmutableArrayExtensions.ElementAt(s_manyElements, 2));
        }

        [Fact]
        public void ElementAtOrDefault()
        {
            Assert.Equal(Enumerable.ElementAtOrDefault(s_manyElements, -1), ImmutableArrayExtensions.ElementAtOrDefault(s_manyElements, -1));
            Assert.Equal(Enumerable.ElementAtOrDefault(s_manyElements, 3), ImmutableArrayExtensions.ElementAtOrDefault(s_manyElements, 3));

            Assert.Throws<InvalidOperationException>(() => Enumerable.ElementAtOrDefault(s_emptyDefault, 0));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.ElementAtOrDefault(s_emptyDefault, 0));

            Assert.Equal(0, ImmutableArrayExtensions.ElementAtOrDefault(s_empty, 0));
            Assert.Equal(0, ImmutableArrayExtensions.ElementAtOrDefault(s_empty, 1));
            Assert.Equal(1, ImmutableArrayExtensions.ElementAtOrDefault(s_oneElement, 0));
            Assert.Equal(3, ImmutableArrayExtensions.ElementAtOrDefault(s_manyElements, 2));
        }

        [Fact]
        public void First()
        {
            Assert.Equal(Enumerable.First(s_oneElement), ImmutableArrayExtensions.First(s_oneElement));
            Assert.Equal(Enumerable.First(s_oneElement, i => true), ImmutableArrayExtensions.First(s_oneElement, i => true));

            Assert.Equal(Enumerable.First(s_manyElements), ImmutableArrayExtensions.First(s_manyElements));
            Assert.Equal(Enumerable.First(s_manyElements, i => true), ImmutableArrayExtensions.First(s_manyElements, i => true));

            Assert.Equal(Enumerable.First(s_oneElementBuilder), ImmutableArrayExtensions.First(s_oneElementBuilder));
            Assert.Equal(Enumerable.First(s_manyElementsBuilder), ImmutableArrayExtensions.First(s_manyElementsBuilder));

            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.First(s_empty));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.First(s_empty, i => true));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.First(s_manyElements, i => false));
        }

        [Fact]
        public void FirstEmpty()
        {
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.First(s_empty));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.First(s_empty, n => true));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ImmutableArrayExtensions.First(s_empty, null));

            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.First(s_emptyBuilder));
        }

        [Fact]
        public void FirstEmptyDefault()
        {
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.First(s_emptyDefault));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.First(s_emptyDefault, n => true));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ImmutableArrayExtensions.First(s_emptyDefault, null));
        }

        [Fact]
        public void FirstOrDefault()
        {
            Assert.Equal(Enumerable.FirstOrDefault(s_oneElement), ImmutableArrayExtensions.FirstOrDefault(s_oneElement));
            Assert.Equal(Enumerable.FirstOrDefault(s_manyElements), ImmutableArrayExtensions.FirstOrDefault(s_manyElements));

            foreach (bool result in new[] { true, false })
            {
                Assert.Equal(Enumerable.FirstOrDefault(s_oneElement, i => result), ImmutableArrayExtensions.FirstOrDefault(s_oneElement, i => result));
                Assert.Equal(Enumerable.FirstOrDefault(s_manyElements, i => result), ImmutableArrayExtensions.FirstOrDefault(s_manyElements, i => result));
            }
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.FirstOrDefault(s_oneElement, null));

            Assert.Equal(Enumerable.FirstOrDefault(s_oneElementBuilder), ImmutableArrayExtensions.FirstOrDefault(s_oneElementBuilder));
            Assert.Equal(Enumerable.FirstOrDefault(s_manyElementsBuilder), ImmutableArrayExtensions.FirstOrDefault(s_manyElementsBuilder));
        }

        [Fact]
        public void FirstOrDefaultEmpty()
        {
            Assert.Equal(0, ImmutableArrayExtensions.FirstOrDefault(s_empty));
            Assert.Equal(0, ImmutableArrayExtensions.FirstOrDefault(s_empty, n => true));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ImmutableArrayExtensions.FirstOrDefault(s_empty, null));

            Assert.Equal(0, ImmutableArrayExtensions.FirstOrDefault(s_emptyBuilder));
        }

        [Fact]
        public void FirstOrDefaultEmptyDefault()
        {
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.FirstOrDefault(s_emptyDefault));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.FirstOrDefault(s_emptyDefault, n => true));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ImmutableArrayExtensions.FirstOrDefault(s_emptyDefault, null));
        }

        [Fact]
        public void Last()
        {
            Assert.Equal(Enumerable.Last(s_oneElement), ImmutableArrayExtensions.Last(s_oneElement));
            Assert.Equal(Enumerable.Last(s_oneElement, i => true), ImmutableArrayExtensions.Last(s_oneElement, i => true));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Last(s_oneElement, i => false));

            Assert.Equal(Enumerable.Last(s_manyElements), ImmutableArrayExtensions.Last(s_manyElements));
            Assert.Equal(Enumerable.Last(s_manyElements, i => true), ImmutableArrayExtensions.Last(s_manyElements, i => true));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Last(s_manyElements, i => false));

            Assert.Equal(Enumerable.Last(s_oneElementBuilder), ImmutableArrayExtensions.Last(s_oneElementBuilder));
            Assert.Equal(Enumerable.Last(s_manyElementsBuilder), ImmutableArrayExtensions.Last(s_manyElementsBuilder));
        }

        [Fact]
        public void LastEmpty()
        {
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Last(s_empty));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Last(s_empty, n => true));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.Last(s_empty, null));

            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Last(s_emptyBuilder));
        }

        [Fact]
        public void LastEmptyDefault()
        {
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.Last(s_emptyDefault));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.Last(s_emptyDefault, n => true));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ImmutableArrayExtensions.Last(s_emptyDefault, null));
        }

        [Fact]
        public void LastOrDefault()
        {
            Assert.Equal(Enumerable.LastOrDefault(s_oneElement), ImmutableArrayExtensions.LastOrDefault(s_oneElement));
            Assert.Equal(Enumerable.LastOrDefault(s_manyElements), ImmutableArrayExtensions.LastOrDefault(s_manyElements));

            foreach (bool result in new[] { true, false })
            {
                Assert.Equal(Enumerable.LastOrDefault(s_oneElement, i => result), ImmutableArrayExtensions.LastOrDefault(s_oneElement, i => result));
                Assert.Equal(Enumerable.LastOrDefault(s_manyElements, i => result), ImmutableArrayExtensions.LastOrDefault(s_manyElements, i => result));
            }
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.LastOrDefault(s_oneElement, null));

            Assert.Equal(Enumerable.LastOrDefault(s_oneElementBuilder), ImmutableArrayExtensions.LastOrDefault(s_oneElementBuilder));
            Assert.Equal(Enumerable.LastOrDefault(s_manyElementsBuilder), ImmutableArrayExtensions.LastOrDefault(s_manyElementsBuilder));
        }

        [Fact]
        public void LastOrDefaultEmpty()
        {
            Assert.Equal(0, ImmutableArrayExtensions.LastOrDefault(s_empty));
            Assert.Equal(0, ImmutableArrayExtensions.LastOrDefault(s_empty, n => true));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ImmutableArrayExtensions.LastOrDefault(s_empty, null));

            Assert.Equal(0, ImmutableArrayExtensions.LastOrDefault(s_emptyBuilder));
        }

        [Fact]
        public void LastOrDefaultEmptyDefault()
        {
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => ImmutableArrayExtensions.LastOrDefault(s_emptyDefault));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => ImmutableArrayExtensions.LastOrDefault(s_emptyDefault, n => true));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ImmutableArrayExtensions.LastOrDefault(s_emptyDefault, null));
        }

        [Fact]
        public void Single()
        {
            Assert.Equal(Enumerable.Single(s_oneElement), ImmutableArrayExtensions.Single(s_oneElement));
            Assert.Equal(Enumerable.Single(s_oneElement), ImmutableArrayExtensions.Single(s_oneElement, i => true));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Single(s_manyElements));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Single(s_manyElements, i => true));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Single(s_manyElements, i => false));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Single(s_oneElement, i => false));
        }

        [Fact]
        public void SingleEmpty()
        {
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Single(s_empty));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Single(s_empty, n => true));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ImmutableArrayExtensions.Single(s_empty, null));
        }

        [Fact]
        public void SingleEmptyDefault()
        {
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => ImmutableArrayExtensions.Single(s_emptyDefault));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => ImmutableArrayExtensions.Single(s_emptyDefault, n => true));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ImmutableArrayExtensions.Single(s_emptyDefault, null));
        }

        [Fact]
        public void SingleOrDefault()
        {
            Assert.Equal(Enumerable.SingleOrDefault(s_oneElement), ImmutableArrayExtensions.SingleOrDefault(s_oneElement));
            Assert.Equal(Enumerable.SingleOrDefault(s_oneElement), ImmutableArrayExtensions.SingleOrDefault(s_oneElement, i => true));
            Assert.Equal(Enumerable.SingleOrDefault(s_oneElement, i => false), ImmutableArrayExtensions.SingleOrDefault(s_oneElement, i => false));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.SingleOrDefault(s_manyElements));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.SingleOrDefault(s_manyElements, i => true));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ImmutableArrayExtensions.SingleOrDefault(s_oneElement, null));
        }

        [Fact]
        public void SingleOrDefaultEmpty()
        {
            Assert.Equal(0, ImmutableArrayExtensions.SingleOrDefault(s_empty));
            Assert.Equal(0, ImmutableArrayExtensions.SingleOrDefault(s_empty, n => true));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ImmutableArrayExtensions.SingleOrDefault(s_empty, null));
        }

        [Fact]
        public void SingleOrDefaultEmptyDefault()
        {
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => ImmutableArrayExtensions.SingleOrDefault(s_emptyDefault));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => ImmutableArrayExtensions.SingleOrDefault(s_emptyDefault, n => true));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ImmutableArrayExtensions.SingleOrDefault(s_emptyDefault, null));
        }

        [Fact]
        public void ToDictionary()
        {
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ImmutableArrayExtensions.ToDictionary(s_manyElements, (Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ImmutableArrayExtensions.ToDictionary(s_manyElements, (Func<int, int>)null, n => n));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ImmutableArrayExtensions.ToDictionary(s_manyElements, (Func<int, int>)null, n => n, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => ImmutableArrayExtensions.ToDictionary(s_manyElements, n => n, (Func<int, string>)null));
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => ImmutableArrayExtensions.ToDictionary(s_manyElements, n => n, (Func<int, string>)null, EqualityComparer<int>.Default));

            var stringToString = ImmutableArrayExtensions.ToDictionary(s_manyElements, n => n.ToString(), n => (n * 2).ToString());
            Assert.Equal(stringToString.Count, s_manyElements.Length);
            Assert.Equal("2", stringToString["1"]);
            Assert.Equal("4", stringToString["2"]);
            Assert.Equal("6", stringToString["3"]);

            var stringToInt = ImmutableArrayExtensions.ToDictionary(s_manyElements, n => n.ToString());
            Assert.Equal(stringToString.Count, s_manyElements.Length);
            Assert.Equal(1, stringToInt["1"]);
            Assert.Equal(2, stringToInt["2"]);
            Assert.Equal(3, stringToInt["3"]);

            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.ToDictionary(s_emptyDefault, n => n));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.ToDictionary(s_emptyDefault, n => n, n => n));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.ToDictionary(s_emptyDefault, n => n, EqualityComparer<int>.Default));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.ToDictionary(s_emptyDefault, n => n, n => n, EqualityComparer<int>.Default));
        }

        [Fact]
        public void ToArray()
        {
            Assert.Equal(0, ImmutableArrayExtensions.ToArray(s_empty).Length);
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.ToArray(s_emptyDefault));
            Assert.Equal(s_manyElements.ToArray(), ImmutableArrayExtensions.ToArray(s_manyElements));
        }
    }
}
