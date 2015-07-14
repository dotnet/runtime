using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class ShortCircuitingTests
    {
        private class TrackingEnumerable : IEnumerable<int>
        {
            // Skipping tests of double calls on GetEnumerable. Just don't do them here!
            private readonly int _count;
            public TrackingEnumerable(int count)
            {
                _count = count;
            }
            public int Moves { get; private set; }
            public IEnumerator<int> GetEnumerator()
            {
                for (int i = 0; i < _count; ++i)
                    yield return ++Moves;
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class CountedFunction<T, TResult>
        {
            private readonly Func<T, TResult> _baseFunc;
            public int Calls { get; private set; }
            public CountedFunction(Func<T, TResult> baseFunc)
            {
                _baseFunc = baseFunc;
            }
            public Func<T, TResult> Func
            {
                get
                {
                    return x =>
                        {
                            ++Calls;
                            return _baseFunc(x);
                        };
                }
            }
        }

        [Fact]
        public void ListLastChecksAll()
        {
            var source = Enumerable.Range(0, 10).ToList();
            var pred = new CountedFunction<int, bool>(i => i < 7);
            Assert.Equal(6, source.Last(pred.Func));
            Assert.Equal(10, pred.Calls);
        }

        [Fact]
        public void MinDoubleChecksAll()
        {
            var tracker = new TrackingEnumerable(10);
            var source = tracker.Select(i => i == 5 ? double.NaN : (double)i);
            Assert.True(double.IsNaN(source.Min()));
            Assert.Equal(10, tracker.Moves);
        }

        [Fact]
        void MinNullableDoubleChecksAll()
        {
            var tracker = new TrackingEnumerable(10);
            var source = tracker.Select(i => (double?)(i == 5 ? double.NaN : (double)i));
            Assert.True(double.IsNaN(source.Min().GetValueOrDefault()));
            Assert.Equal(10, tracker.Moves);
        }

        [Fact]
        public void MinSingleChecksAll()
        {
            var tracker = new TrackingEnumerable(10);
            var source = tracker.Select(i => i == 5 ? float.NaN : (float)i);
            Assert.True(float.IsNaN(source.Min()));
            Assert.Equal(10, tracker.Moves);
        }

        [Fact]
        void MinNullableSingleChecksAll()
        {
            var tracker = new TrackingEnumerable(10);
            var source = tracker.Select(i => (float?)(i == 5 ? float.NaN : (float)i));
            Assert.True(float.IsNaN(source.Min().GetValueOrDefault()));
            Assert.Equal(10, tracker.Moves);
        }

        [Fact]
        void SingleWithPredicateChecksAll()
        {
            var tracker = new TrackingEnumerable(10);
            var pred = new CountedFunction<int, bool>(i => i > 2);
            Assert.Throws<InvalidOperationException>(() => tracker.Single(pred.Func));
            Assert.Equal(10, tracker.Moves);
            Assert.Equal(10, pred.Calls);
        }

        [Fact]
        void SingleOrDefaultWithPredicateChecksAll()
        {
            var tracker = new TrackingEnumerable(10);
            var pred = new CountedFunction<int, bool>(i => i > 2);
            Assert.Throws<InvalidOperationException>(() => tracker.SingleOrDefault(pred.Func));
            Assert.Equal(10, tracker.Moves);
            Assert.Equal(10, pred.Calls);
        }

        [Fact]
        void SingleWithPredicateDifferentToWhereFollowedBySingle()
        {
            var tracker0 = new TrackingEnumerable(10);
            var pred0 = new CountedFunction<int, bool>(i => i > 2);
            Assert.Throws<InvalidOperationException>(() => tracker0.Single(pred0.Func));
            var tracker1 = new TrackingEnumerable(10);
            var pred1 = new CountedFunction<int, bool>(i => i > 2);
            Assert.Throws<InvalidOperationException>(() => tracker1.Where(pred1.Func).Single());
            Assert.NotEqual(tracker0.Moves, tracker1.Moves);
            Assert.NotEqual(pred0.Calls, pred1.Calls);
        }

        [Fact]
        void SingleOrDefaultWithPredicateDifferentToWhereFollowedBySingleOrDefault()
        {
            var tracker0 = new TrackingEnumerable(10);
            var pred0 = new CountedFunction<int, bool>(i => i > 2);
            Assert.Throws<InvalidOperationException>(() => tracker0.SingleOrDefault(pred0.Func));
            var tracker1 = new TrackingEnumerable(10);
            var pred1 = new CountedFunction<int, bool>(i => i > 2);
            Assert.Throws<InvalidOperationException>(() => tracker1.Where(pred1.Func).SingleOrDefault());
            Assert.NotEqual(tracker0.Moves, tracker1.Moves);
            Assert.NotEqual(pred0.Calls, pred1.Calls);
        }

        [Fact]
        [ActiveIssue(2349)]
        public void ListLastDoesntCheckAll()
        {
            var source = Enumerable.Range(0, 10).ToList();
            var pred = new CountedFunction<int, bool>(i => i < 7);
            Assert.Equal(6, source.Last(pred.Func));
            Assert.Equal(4, pred.Calls);
        }

        [Fact]
        [ActiveIssue(2349)]
        public void MinDoubleDoesntCheckAll()
        {
            var tracker = new TrackingEnumerable(10);
            var source = tracker.Select(i => i == 5 ? double.NaN : (double)i);
            Assert.True(double.IsNaN(source.Min()));
            Assert.Equal(5, tracker.Moves);
        }

        [Fact]
        [ActiveIssue(2349)]
        void MinNullableDoubleDoesntCheckAll()
        {
            var tracker = new TrackingEnumerable(10);
            var source = tracker.Select(i => (double?)(i == 5 ? double.NaN : (double)i));
            Assert.True(double.IsNaN(source.Min().GetValueOrDefault()));
            Assert.Equal(5, tracker.Moves);
        }

        [Fact]
        [ActiveIssue(2349)]
        public void MinSingleDoesntCheckAll()
        {
            var tracker = new TrackingEnumerable(10);
            var source = tracker.Select(i => i == 5 ? float.NaN : (float)i);
            Assert.True(float.IsNaN(source.Min()));
            Assert.Equal(5, tracker.Moves);
        }

        [Fact]
        [ActiveIssue(2349)]
        void MinNullableSingleDoesntCheckAll()
        {
            var tracker = new TrackingEnumerable(10);
            var source = tracker.Select(i => (float?)(i == 5 ? float.NaN : (float)i));
            Assert.True(float.IsNaN(source.Min().GetValueOrDefault()));
            Assert.Equal(5, tracker.Moves);
        }

        [Fact]
        [ActiveIssue(2349)]
        void SingleWithPredicateDoesntCheckAll()
        {
            var tracker = new TrackingEnumerable(10);
            var pred = new CountedFunction<int, bool>(i => i > 2);
            Assert.Throws<InvalidOperationException>(() => tracker.Single(pred.Func));
            Assert.Equal(4, tracker.Moves);
            Assert.Equal(4, pred.Calls);
        }

        [Fact]
        [ActiveIssue(2349)]
        void SingleOrDefaultWithPredicateDoesntCheckAll()
        {
            var tracker = new TrackingEnumerable(10);
            var pred = new CountedFunction<int, bool>(i => i > 2);
            Assert.Throws<InvalidOperationException>(() => tracker.SingleOrDefault(pred.Func));
            Assert.Equal(4, tracker.Moves);
            Assert.Equal(4, pred.Calls);
        }

        [Fact]
        [ActiveIssue(2349)]
        void SingleWithPredicateWorksLikeWhereFollowedBySingle()
        {
            var tracker0 = new TrackingEnumerable(10);
            var pred0 = new CountedFunction<int, bool>(i => i > 2);
            Assert.Throws<InvalidOperationException>(() => tracker0.Single(pred0.Func));
            var tracker1 = new TrackingEnumerable(10);
            var pred1 = new CountedFunction<int, bool>(i => i > 2);
            Assert.Throws<InvalidOperationException>(() => tracker1.Where(pred1.Func).Single());
            Assert.Equal(tracker0.Moves, tracker1.Moves);
            Assert.Equal(pred0.Calls, pred1.Calls);
        }

        [Fact]
        [ActiveIssue(2349)]
        void SingleOrDefaultWithPredicateWorksLikeWhereFollowedBySingleOrDefault()
        {
            var tracker0 = new TrackingEnumerable(10);
            var pred0 = new CountedFunction<int, bool>(i => i > 2);
            Assert.Throws<InvalidOperationException>(() => tracker0.SingleOrDefault(pred0.Func));
            var tracker1 = new TrackingEnumerable(10);
            var pred1 = new CountedFunction<int, bool>(i => i > 2);
            Assert.Throws<InvalidOperationException>(() => tracker1.Where(pred1.Func).SingleOrDefault());
            Assert.Equal(tracker0.Moves, tracker1.Moves);
            Assert.Equal(pred0.Calls, pred1.Calls);
        }
    }
}