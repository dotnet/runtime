// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Immutable.Test
{
    public class ImmutableArrayExtensionsTest
    {
        private static readonly ImmutableArray<int> emptyDefault = default(ImmutableArray<int>);
        private static readonly ImmutableArray<int> empty = ImmutableArray.Create<int>();
        private static readonly ImmutableArray<int> oneElement = ImmutableArray.Create(1);
        private static readonly ImmutableArray<int> manyElements = ImmutableArray.Create(1, 2, 3);
        private static readonly ImmutableArray<GenericParameterHelper> oneElementRefType = ImmutableArray.Create(new GenericParameterHelper(1));
        private static readonly ImmutableArray<string> twoElementRefTypeWithNull = ImmutableArray.Create("1", null);

        private static readonly ImmutableArray<int>.Builder emptyBuilder = ImmutableArray.Create<int>().ToBuilder();
        private static readonly ImmutableArray<int>.Builder oneElementBuilder = ImmutableArray.Create<int>(1).ToBuilder();
        private static readonly ImmutableArray<int>.Builder manyElementsBuilder = ImmutableArray.Create<int>(1, 2, 3).ToBuilder(); 

        [Fact]
        public void Select()
        {
            Assert.Equal(new[] { 4, 5, 6 }, ImmutableArrayExtensions.Select(manyElements, n => n + 3));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.Select<int, bool>(manyElements, null));
        }

        [Fact]
        public void SelectEmptyDefault()
        {
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.Select<int, bool>(emptyDefault, null));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.Select(emptyDefault, n => true));
        }

        [Fact]
        public void SelectEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.Select<int, bool>(empty, null));
            Assert.False(ImmutableArrayExtensions.Select(empty, n => true).Any());
        }

        [Fact]
        public void Where()
        {
            Assert.Equal(new[] { 2, 3 }, ImmutableArrayExtensions.Where(manyElements, n => n > 1));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.Where(manyElements, null));
        }

        [Fact]
        public void WhereEmptyDefault()
        {
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.Where(emptyDefault, null));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.Where(emptyDefault, n => true));
        }

        [Fact]
        public void WhereEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.Where(empty, null));
            Assert.False(ImmutableArrayExtensions.Where(empty, n => true).Any());
        }

        [Fact]
        public void Any()
        {
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.Any(oneElement, null));
            Assert.True(ImmutableArrayExtensions.Any(oneElement));
            Assert.True(ImmutableArrayExtensions.Any(manyElements, n => n == 2));
            Assert.False(ImmutableArrayExtensions.Any(manyElements, n => n == 4));
            Assert.True(ImmutableArrayExtensions.Any(oneElementBuilder));
        }

        [Fact]
        public void AnyEmptyDefault()
        {
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.Any(emptyDefault));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.Any(emptyDefault, n => true));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.Any(emptyDefault, null));
        }

        [Fact]
        public void AnyEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.Any(empty, null));
            Assert.False(ImmutableArrayExtensions.Any(empty));
            Assert.False(ImmutableArrayExtensions.Any(empty, n => true));
        }

        [Fact]
        public void All()
        {
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.All(oneElement, null));
            Assert.False(ImmutableArrayExtensions.All(manyElements, n => n == 2));
            Assert.True(ImmutableArrayExtensions.All(manyElements, n => n > 0));
        }

        [Fact]
        public void AllEmptyDefault()
        {
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.All(emptyDefault, n => true));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.All(emptyDefault, null));
        }

        [Fact]
        public void AllEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.All(empty, null));
            Assert.True(ImmutableArrayExtensions.All(empty, n => { Assert.True(false); return false; })); // predicate should never be invoked.
        }

        [Fact]
        public void SequenceEqual()
        {
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.SequenceEqual(oneElement, (IEnumerable<int>)null));

            foreach (IEqualityComparer<int> comparer in new[] { null, EqualityComparer<int>.Default })
            {
                Assert.True(ImmutableArrayExtensions.SequenceEqual(manyElements, manyElements, comparer));
                Assert.True(ImmutableArrayExtensions.SequenceEqual(manyElements, (IEnumerable<int>)manyElements.ToArray(), comparer));
                Assert.True(ImmutableArrayExtensions.SequenceEqual(manyElements, ImmutableArray.Create(manyElements.ToArray()), comparer));

                Assert.False(ImmutableArrayExtensions.SequenceEqual(manyElements, oneElement, comparer));
                Assert.False(ImmutableArrayExtensions.SequenceEqual(manyElements, (IEnumerable<int>)oneElement.ToArray(), comparer));
                Assert.False(ImmutableArrayExtensions.SequenceEqual(manyElements, ImmutableArray.Create(oneElement.ToArray()), comparer));
                Assert.False(ImmutableArrayExtensions.SequenceEqual(manyElements, (IEnumerable<int>)manyElements.Add(1).ToArray(), comparer));
                Assert.False(ImmutableArrayExtensions.SequenceEqual(manyElements.Add(1), manyElements.Add(2).ToArray(), comparer));
                Assert.False(ImmutableArrayExtensions.SequenceEqual(manyElements.Add(1), (IEnumerable<int>)manyElements.Add(2).ToArray(), comparer));
            }

            Assert.True(ImmutableArrayExtensions.SequenceEqual(manyElements, manyElements, (a, b) => true));

            Assert.False(ImmutableArrayExtensions.SequenceEqual(manyElements, oneElement, (a, b) => a == b));
            Assert.False(ImmutableArrayExtensions.SequenceEqual(manyElements.Add(1), manyElements.Add(2), (a, b) => a == b));
            Assert.True(ImmutableArrayExtensions.SequenceEqual(manyElements.Add(1), manyElements.Add(1), (a, b) => a == b));

            Assert.False(ImmutableArrayExtensions.SequenceEqual(manyElements, ImmutableArray.Create(manyElements.ToArray()), (a, b) => false));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.SequenceEqual(oneElement, oneElement, (Func<int, int, bool>)null));
        }

        [Fact]
        public void SequenceEqualEmptyDefault()
        {
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.SequenceEqual(oneElement, emptyDefault));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.SequenceEqual(emptyDefault, empty));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.SequenceEqual(emptyDefault, emptyDefault));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.SequenceEqual(emptyDefault, emptyDefault, (Func<int, int, bool>)null));
        }

        [Fact]
        public void SequenceEqualEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.SequenceEqual(empty, (IEnumerable<int>)null));
            Assert.True(ImmutableArrayExtensions.SequenceEqual(empty, empty));
            Assert.True(ImmutableArrayExtensions.SequenceEqual(empty, empty.ToArray()));
            Assert.True(ImmutableArrayExtensions.SequenceEqual(empty, empty, (a, b) => true));
            Assert.True(ImmutableArrayExtensions.SequenceEqual(empty, empty, (a, b) => false));
        }

        [Fact]
        public void Aggregate()
        {
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.Aggregate(oneElement, null));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.Aggregate(oneElement, 1, null));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.Aggregate<int, int, int>(oneElement, 1, null, null));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.Aggregate<int, int, int>(oneElement, 1, (a, b) => a + b, null));

            Assert.Equal(Enumerable.Aggregate(manyElements, (a, b) => a * b), ImmutableArrayExtensions.Aggregate(manyElements, (a, b) => a * b));
            Assert.Equal(Enumerable.Aggregate(manyElements, 5, (a, b) => a * b), ImmutableArrayExtensions.Aggregate(manyElements, 5, (a, b) => a * b));
            Assert.Equal(Enumerable.Aggregate(manyElements, 5, (a, b) => a * b, a => -a), ImmutableArrayExtensions.Aggregate(manyElements, 5, (a, b) => a * b, a => -a));
        }

        [Fact]
        public void AggregateEmptyDefault()
        {
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.Aggregate(emptyDefault, (a, b) => a + b));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.Aggregate(emptyDefault, 1, (a, b) => a + b));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.Aggregate<int, int, int>(emptyDefault, 1, (a, b) => a + b, a => a));
        }

        [Fact]
        public void AggregateEmpty()
        {
            Assert.Equal(0, ImmutableArrayExtensions.Aggregate(empty, (a, b) => a + b));
            Assert.Equal(1, ImmutableArrayExtensions.Aggregate(empty, 1, (a, b) => a + b));
            Assert.Equal(1, ImmutableArrayExtensions.Aggregate<int, int, int>(empty, 1, (a, b) => a + b, a => a));
        }

        [Fact]
        public void ElementAt()
        {
            // Basis for some assertions that follow
            Assert.Throws<IndexOutOfRangeException>(() => Enumerable.ElementAt(empty, 0));
            Assert.Throws<IndexOutOfRangeException>(() => Enumerable.ElementAt(manyElements, -1));

            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.ElementAt(emptyDefault, 0));
            Assert.Throws<IndexOutOfRangeException>(() => ImmutableArrayExtensions.ElementAt(empty, 0));
            Assert.Throws<IndexOutOfRangeException>(() => ImmutableArrayExtensions.ElementAt(manyElements, -1));
            Assert.Equal(1, ImmutableArrayExtensions.ElementAt(oneElement, 0));
            Assert.Equal(3, ImmutableArrayExtensions.ElementAt(manyElements, 2));
        }

        [Fact]
        public void ElementAtOrDefault()
        {
            Assert.Equal(Enumerable.ElementAtOrDefault(manyElements, -1), ImmutableArrayExtensions.ElementAtOrDefault(manyElements, -1));
            Assert.Equal(Enumerable.ElementAtOrDefault(manyElements, 3), ImmutableArrayExtensions.ElementAtOrDefault(manyElements, 3));

            Assert.Throws<InvalidOperationException>(() => Enumerable.ElementAtOrDefault(emptyDefault, 0));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.ElementAtOrDefault(emptyDefault, 0));

            Assert.Equal(0, ImmutableArrayExtensions.ElementAtOrDefault(empty, 0));
            Assert.Equal(0, ImmutableArrayExtensions.ElementAtOrDefault(empty, 1));
            Assert.Equal(1, ImmutableArrayExtensions.ElementAtOrDefault(oneElement, 0));
            Assert.Equal(3, ImmutableArrayExtensions.ElementAtOrDefault(manyElements, 2));
        }

        [Fact]
        public void First()
        {
            Assert.Equal(Enumerable.First(oneElement), ImmutableArrayExtensions.First(oneElement));
            Assert.Equal(Enumerable.First(oneElement, i => true), ImmutableArrayExtensions.First(oneElement, i => true));

            Assert.Equal(Enumerable.First(manyElements), ImmutableArrayExtensions.First(manyElements));
            Assert.Equal(Enumerable.First(manyElements, i => true), ImmutableArrayExtensions.First(manyElements, i => true));

            Assert.Equal(Enumerable.First(oneElementBuilder), ImmutableArrayExtensions.First(oneElementBuilder));
            Assert.Equal(Enumerable.First(manyElementsBuilder), ImmutableArrayExtensions.First(manyElementsBuilder));

            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.First(empty));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.First(empty, i => true));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.First(manyElements, i => false));
        }

        [Fact]
        public void FirstEmpty()
        {
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.First(empty));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.First(empty, n => true));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.First(empty, null));

            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.First(emptyBuilder)); 
        }

        [Fact]
        public void FirstEmptyDefault()
        {
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.First(emptyDefault));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.First(emptyDefault, n => true));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.First(emptyDefault, null));
        }

        [Fact]
        public void FirstOrDefault()
        {
            Assert.Equal(Enumerable.FirstOrDefault(oneElement), ImmutableArrayExtensions.FirstOrDefault(oneElement));
            Assert.Equal(Enumerable.FirstOrDefault(manyElements), ImmutableArrayExtensions.FirstOrDefault(manyElements));

            foreach (bool result in new[] { true, false })
            {
                Assert.Equal(Enumerable.FirstOrDefault(oneElement, i => result), ImmutableArrayExtensions.FirstOrDefault(oneElement, i => result));
                Assert.Equal(Enumerable.FirstOrDefault(manyElements, i => result), ImmutableArrayExtensions.FirstOrDefault(manyElements, i => result));
            }
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.FirstOrDefault(oneElement, null));

            Assert.Equal(Enumerable.FirstOrDefault(oneElementBuilder), ImmutableArrayExtensions.FirstOrDefault(oneElementBuilder));
            Assert.Equal(Enumerable.FirstOrDefault(manyElementsBuilder), ImmutableArrayExtensions.FirstOrDefault(manyElementsBuilder));
        }

        [Fact]
        public void FirstOrDefaultEmpty()
        {
            Assert.Equal(0, ImmutableArrayExtensions.FirstOrDefault(empty));
            Assert.Equal(0, ImmutableArrayExtensions.FirstOrDefault(empty, n => true));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.FirstOrDefault(empty, null));

            Assert.Equal(0, ImmutableArrayExtensions.FirstOrDefault(emptyBuilder));
        }

        [Fact]
        public void FirstOrDefaultEmptyDefault()
        {
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.FirstOrDefault(emptyDefault));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.FirstOrDefault(emptyDefault, n => true));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.FirstOrDefault(emptyDefault, null));
        }

        [Fact]
        public void Last()
        {
            Assert.Equal(Enumerable.Last(oneElement), ImmutableArrayExtensions.Last(oneElement));
            Assert.Equal(Enumerable.Last(oneElement, i => true), ImmutableArrayExtensions.Last(oneElement, i => true));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Last(oneElement, i => false));

            Assert.Equal(Enumerable.Last(manyElements), ImmutableArrayExtensions.Last(manyElements));
            Assert.Equal(Enumerable.Last(manyElements, i => true), ImmutableArrayExtensions.Last(manyElements, i => true));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Last(manyElements, i => false));

            Assert.Equal(Enumerable.Last(oneElementBuilder), ImmutableArrayExtensions.Last(oneElementBuilder));
            Assert.Equal(Enumerable.Last(manyElementsBuilder), ImmutableArrayExtensions.Last(manyElementsBuilder));
        } 


        [Fact]
        public void LastEmpty()
        {
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Last(empty));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Last(empty, n => true));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.Last(empty, null));
        
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Last(emptyBuilder)); 
        } 


        [Fact]
        public void LastEmptyDefault()
        {
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.Last(emptyDefault));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.Last(emptyDefault, n => true));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.Last(emptyDefault, null));
        }

        [Fact]
        public void LastOrDefault()
        {
            Assert.Equal(Enumerable.LastOrDefault(oneElement), ImmutableArrayExtensions.LastOrDefault(oneElement));
            Assert.Equal(Enumerable.LastOrDefault(manyElements), ImmutableArrayExtensions.LastOrDefault(manyElements));

            foreach (bool result in new[] { true, false })
            {
                Assert.Equal(Enumerable.LastOrDefault(oneElement, i => result), ImmutableArrayExtensions.LastOrDefault(oneElement, i => result));
                Assert.Equal(Enumerable.LastOrDefault(manyElements, i => result), ImmutableArrayExtensions.LastOrDefault(manyElements, i => result));
            }
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.LastOrDefault(oneElement, null));

            Assert.Equal(Enumerable.LastOrDefault(oneElementBuilder), ImmutableArrayExtensions.LastOrDefault(oneElementBuilder));
            Assert.Equal(Enumerable.LastOrDefault(manyElementsBuilder), ImmutableArrayExtensions.LastOrDefault(manyElementsBuilder));
        }

        [Fact]
        public void LastOrDefaultEmpty()
        {
            Assert.Equal(0, ImmutableArrayExtensions.LastOrDefault(empty));
            Assert.Equal(0, ImmutableArrayExtensions.LastOrDefault(empty, n => true));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.LastOrDefault(empty, null));

            Assert.Equal(0, ImmutableArrayExtensions.LastOrDefault(emptyBuilder));
        }

        [Fact]
        public void LastOrDefaultEmptyDefault()
        {
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.LastOrDefault(emptyDefault));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.LastOrDefault(emptyDefault, n => true));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.LastOrDefault(emptyDefault, null));
        }

        [Fact]
        public void Single()
        {
            Assert.Equal(Enumerable.Single(oneElement), ImmutableArrayExtensions.Single(oneElement));
            Assert.Equal(Enumerable.Single(oneElement), ImmutableArrayExtensions.Single(oneElement, i => true));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Single(manyElements));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Single(manyElements, i => true));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Single(manyElements, i => false));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Single(oneElement, i => false));
        }

        [Fact]
        public void SingleEmpty()
        {
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Single(empty));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.Single(empty, n => true));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.Single(empty, null));
        }

        [Fact]
        public void SingleEmptyDefault()
        {
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.Single(emptyDefault));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.Single(emptyDefault, n => true));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.Single(emptyDefault, null));
        }

        [Fact]
        public void SingleOrDefault()
        {
            Assert.Equal(Enumerable.SingleOrDefault(oneElement), ImmutableArrayExtensions.SingleOrDefault(oneElement));
            Assert.Equal(Enumerable.SingleOrDefault(oneElement), ImmutableArrayExtensions.SingleOrDefault(oneElement, i => true));
            Assert.Equal(Enumerable.SingleOrDefault(oneElement, i => false), ImmutableArrayExtensions.SingleOrDefault(oneElement, i => false));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.SingleOrDefault(manyElements));
            Assert.Throws<InvalidOperationException>(() => ImmutableArrayExtensions.SingleOrDefault(manyElements, i => true));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.SingleOrDefault(oneElement, null));
        }

        [Fact]
        public void SingleOrDefaultEmpty()
        {
            Assert.Equal(0, ImmutableArrayExtensions.SingleOrDefault(empty));
            Assert.Equal(0, ImmutableArrayExtensions.SingleOrDefault(empty, n => true));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.SingleOrDefault(empty, null));
        }

        [Fact]
        public void SingleOrDefaultEmptyDefault()
        {
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.SingleOrDefault(emptyDefault));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.SingleOrDefault(emptyDefault, n => true));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.SingleOrDefault(emptyDefault, null));
        }

        [Fact]
        public void ToDictionary()
        {
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.ToDictionary(manyElements, (Func<int, int>)null));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.ToDictionary(manyElements, (Func<int, int>)null, n => n));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.ToDictionary(manyElements, (Func<int, int>)null, n => n, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.ToDictionary(manyElements, n => n, (Func<int, string>)null));
            Assert.Throws<ArgumentNullException>(() => ImmutableArrayExtensions.ToDictionary(manyElements, n => n, (Func<int, string>)null, EqualityComparer<int>.Default));

            var stringToString = ImmutableArrayExtensions.ToDictionary(manyElements, n => n.ToString(), n => (n * 2).ToString()); 
            Assert.Equal(stringToString.Count, manyElements.Length); 
            Assert.Equal("2", stringToString["1"]); 
            Assert.Equal("4", stringToString["2"]); 
            Assert.Equal("6", stringToString["3"]); 

            var stringToInt = ImmutableArrayExtensions.ToDictionary(manyElements, n => n.ToString()); 
            Assert.Equal(stringToString.Count, manyElements.Length); 
            Assert.Equal(1, stringToInt["1"]); 
            Assert.Equal(2, stringToInt["2"]); 
            Assert.Equal(3, stringToInt["3"]);

            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.ToDictionary(emptyDefault, n => n));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.ToDictionary(emptyDefault, n => n, n => n));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.ToDictionary(emptyDefault, n => n, EqualityComparer<int>.Default));
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.ToDictionary(emptyDefault, n => n, n => n, EqualityComparer<int>.Default));
        }

        [Fact]
        public void ToArray()
        {
            Assert.Equal(0, ImmutableArrayExtensions.ToArray(empty).Length);
            Assert.Throws<NullReferenceException>(() => ImmutableArrayExtensions.ToArray(emptyDefault));
            Assert.Equal(manyElements.ToArray(), ImmutableArrayExtensions.ToArray(manyElements));
        }
    }
}
