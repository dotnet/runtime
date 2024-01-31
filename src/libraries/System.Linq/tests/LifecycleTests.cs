// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Tests
{
    public class LifecycleTests : EnumerableTests
    {
        [Fact]
        public void UnaryOperations_SourceEnumeratorDisposed()
        {
            // Using Assert.All instead of a Theory to avoid overloading the system with hundreds of thousands of distinct test cases.
            IEnumerable<(Source source, Unary unary1, Unary unary2, Sink sink)> inputs =
                from source in Sources()
                from unary1 in UnaryOperations()
                from unary2 in UnaryOperations()
                from sink in Sinks()
                select (source, unary1, unary2, sink);

            foreach (var input in inputs)
            {
                var (source, unary1, unary2, sink) = input;
                var e = new LifecycleTrackingEnumerable<int>(source.Work);

                // source -> unary1 -> unary2 -> sink
                bool argError = false;
                try
                {
                    sink.Work(unary2.Work(unary1.Work(e)));
                }
                catch (Exception exc) when (exc is ArgumentException || exc is InvalidOperationException)
                {
                    argError = true;
                }

                // We expect the source's enumerator should have been constructed 0 or 1 times,
                // once if there's no short-circuiting involved.  Then the enumerator's Dispose
                // should have been invoked the same number of times.
                bool shortCircuits = argError || ShortCircuits(source, unary1, unary2, sink);
                Assert.InRange(e.EnumeratorCtorCalls, shortCircuits ? 0 : 1, 1);
                Assert.Equal(e.EnumeratorCtorCalls, e.EnumeratorDisposeCalls);
            }
        }

        [Fact]
        public void BinaryOperations_SourceEnumeratorsDisposed()
        {
            // Using Assert.All instead of a Theory to avoid overloading the system with hundreds of thousands of distinct test cases.
            IEnumerable<(Source source, Unary unary, Binary binary, Sink sink)> inputs =
                from source in Sources()
                from unary in UnaryOperations()
                from binary in BinaryOperations()
                from sink in Sinks()
                select (source, unary, binary, sink);

            foreach (var input in inputs)
            {
                var (source, unary, binary, sink) = input;
                var es = new[] { new LifecycleTrackingEnumerable<int>(source.Work), new LifecycleTrackingEnumerable<int>(source.Work) };

                // ((source -> unary), (source -> unary)) -> binary -> sink
                bool argError = false;
                try
                {
                    sink.Work(binary.Work(unary.Work(es[0]), unary.Work(es[1])));
                }
                catch (Exception exc) when (exc is ArgumentException || exc is InvalidOperationException)
                {
                    argError = true;
                }

                // We expect the source's enumerator should have been constructed 0 or 1 times,
                // once if there's no short-circuiting involved.  Then the enumerator's Dispose
                // should have been invoked the same number of times.
                bool shortCircuits = argError || ShortCircuits(source, binary, unary, sink);
                Assert.All(es, e =>
                {
                    Assert.InRange(e.EnumeratorCtorCalls, shortCircuits ? 0 : 1, 1);
                    Assert.Equal(e.EnumeratorCtorCalls, e.EnumeratorDisposeCalls);
                });
            }
        }

        private static bool ShortCircuits(params Operation[] ops) => ops.Any(o => o.ShortCircuits);

        private static IEnumerable<Source> Sources()
        {
            foreach (int size in new[] { 0, 1, 2 })
            {
                yield return new Source($"Enumerable{size}", NumberRangeGuaranteedNotCollectionType(0, size));
            }
        }

        private static int s_nextValue = 42;

        private static IEnumerable<Unary> UnaryOperations()
        {
            yield return new Unary(nameof(Enumerable.Append), e => e.Append(Interlocked.Increment(ref s_nextValue)));
            yield return new Unary(nameof(Enumerable.AsEnumerable), e => e.AsEnumerable());
            yield return new Unary(nameof(Enumerable.Cast), e => e.Cast<int>());
            yield return new Unary(nameof(Enumerable.Distinct), e => e.Distinct());
            yield return new Unary(nameof(Enumerable.DefaultIfEmpty), e => e.DefaultIfEmpty());
            yield return new Unary(nameof(Enumerable.GroupBy), e => e.GroupBy(i => i).Select(g => g.Key));
            yield return new Unary(nameof(Enumerable.GroupBy), e => e.GroupBy(i => i, i => i).Select(g => g.Key));
            yield return new Unary(nameof(Enumerable.OfType), e => e.OfType<int>());
            yield return new Unary(nameof(Enumerable.OrderBy), e => e.OrderBy(i => i));
            yield return new Unary(nameof(Enumerable.OrderByDescending), e => e.OrderByDescending(i => i));
            yield return new Unary(nameof(Enumerable.Prepend), e => e.Prepend(Interlocked.Increment(ref s_nextValue)));
            yield return new Unary(nameof(Enumerable.Reverse), e => e.Reverse());
            yield return new Unary(nameof(Enumerable.Select), e => e.Select(i => i));
            yield return new Unary(nameof(Enumerable.Select), e => e.Select((i, index) => i));
            yield return new Unary(nameof(Enumerable.SelectMany), e => e.SelectMany(i => new[] { i }));
            yield return new Unary(nameof(Enumerable.SelectMany), e => e.SelectMany((i, index) => new[] { i }));
            yield return new Unary(nameof(Enumerable.Skip), e => e.Skip(1));
            yield return new Unary(nameof(Enumerable.SkipWhile), e => e.SkipWhile(i => true));
            yield return new Unary(nameof(Enumerable.SkipWhile), e => e.SkipWhile(i => false));
            yield return new Unary(nameof(Enumerable.SkipLast), e => e.SkipLast(1));
            yield return new Unary(nameof(Enumerable.Take), e => e.Take(int.MaxValue - 1));
            yield return new Unary(nameof(Enumerable.TakeLast), e => e.TakeLast(int.MaxValue - 1));
            yield return new Unary(nameof(Enumerable.TakeWhile), e => e.TakeWhile(i => true));
            yield return new Unary(nameof(Enumerable.TakeWhile), e => e.TakeWhile(i => false), shortCircuits: true);
            yield return new Unary(nameof(Enumerable.ThenBy), e => e.OrderBy(i => i).ThenBy(i => i));
            yield return new Unary(nameof(Enumerable.ThenByDescending), e => e.OrderByDescending(i => i).ThenByDescending(i => i));
            yield return new Unary(nameof(Enumerable.Where), e => e.Where(i => true));
            yield return new Unary(nameof(Enumerable.Where), e => e.Where((i, index) => false));
            yield return new Unary("identity", e => e);
        }

        private static IEnumerable<Binary> BinaryOperations()
        {
            yield return new Binary(nameof(Enumerable.Concat), (e1, e2) => e1.Concat(e2));
            yield return new Binary(nameof(Enumerable.Except), (e1, e2) => e1.Except(e2));
            yield return new Binary(nameof(Enumerable.GroupJoin), (e1, e2) => e1.GroupJoin(e2, i => i, i => i, (i, e3) => i), shortCircuits: true);
            yield return new Binary(nameof(Enumerable.Intersect), (e1, e2) => e1.Intersect(e2));
            yield return new Binary(nameof(Enumerable.Join), (e1, e2) => e1.Join(e2, i => i, i => i, (i1, i2) => i1), shortCircuits: true);
            yield return new Binary(nameof(Enumerable.Union), (e1, e2) => e1.Union(e2));
            yield return new Binary(nameof(Enumerable.Zip), (e1, e2) => e1.Zip(e2).Select(i => i.First), shortCircuits: true);
            yield return new Binary(nameof(Enumerable.Zip), (e1, e2) => e1.Zip(e2, (i, j) => i), shortCircuits: true);
        }

        private static IEnumerable<Sink> Sinks()
        {
            yield return new Sink(nameof(Enumerable.All), e => e.All(i => true));
            yield return new Sink(nameof(Enumerable.Aggregate), e => e.Aggregate(0, (i, j) => i + j));
            yield return new Sink(nameof(Enumerable.Aggregate), e => e.Aggregate(0, (i, j) => i + j, i => i));
            yield return new Sink(nameof(Enumerable.Aggregate), e => e.Aggregate((i, j) => i + j));
            yield return new Sink(nameof(Enumerable.Average), e => e.Average());
            yield return new Sink(nameof(Enumerable.Average), e => e.Average(i => i));
            yield return new Sink(nameof(Enumerable.Any), e => e.Any(), shortCircuits: true);
            yield return new Sink(nameof(Enumerable.Any), e => e.Any(i => false));
            yield return new Sink(nameof(Enumerable.Contains), e => e.Contains(-1));
            yield return new Sink(nameof(Enumerable.Contains), e => e.Contains(0), shortCircuits: true);
            yield return new Sink(nameof(Enumerable.Count), e => e.Count());
            yield return new Sink(nameof(Enumerable.Count), e => e.Count(i => true));
            yield return new Sink(nameof(Enumerable.ElementAt), e => e.ElementAt(0), shortCircuits: true);
            yield return new Sink(nameof(Enumerable.ElementAtOrDefault), e => e.ElementAtOrDefault(0), shortCircuits: true);
            yield return new Sink(nameof(Enumerable.First), e => e.First(), shortCircuits: true);
            yield return new Sink(nameof(Enumerable.First), e => e.First(i => false));
            yield return new Sink(nameof(Enumerable.FirstOrDefault), e => e.FirstOrDefault(), shortCircuits: true);
            yield return new Sink(nameof(Enumerable.FirstOrDefault), e => e.FirstOrDefault(i => false));
            yield return new Sink(nameof(Enumerable.Last), e => e.Last());
            yield return new Sink(nameof(Enumerable.Last), e => e.Last(i => true));
            yield return new Sink(nameof(Enumerable.LastOrDefault), e => e.LastOrDefault());
            yield return new Sink(nameof(Enumerable.LastOrDefault), e => e.LastOrDefault(i => true));
            yield return new Sink(nameof(Enumerable.LongCount), e => e.LongCount());
            yield return new Sink(nameof(Enumerable.LongCount), e => e.LongCount(i => true));
            yield return new Sink(nameof(Enumerable.Max), e => e.Max());
            yield return new Sink(nameof(Enumerable.Max), e => e.Max(i => i));
            yield return new Sink(nameof(Enumerable.Min), e => e.Min());
            yield return new Sink(nameof(Enumerable.Min), e => e.Min(i => i));
            yield return new Sink(nameof(Enumerable.SequenceEqual), e => e.SequenceEqual(Enumerable.Range(0, 1)), shortCircuits: true);
            yield return new Sink(nameof(Enumerable.Single), e => e.Single(), shortCircuits: true);
            yield return new Sink(nameof(Enumerable.Single), e => e.Single(i => false));
            yield return new Sink(nameof(Enumerable.SingleOrDefault), e => e.SingleOrDefault());
            yield return new Sink(nameof(Enumerable.SingleOrDefault), e => e.SingleOrDefault(i => true));
            yield return new Sink(nameof(Enumerable.SingleOrDefault), e => e.SingleOrDefault(i => false));
            yield return new Sink(nameof(Enumerable.Sum), e => e.Sum());
            yield return new Sink(nameof(Enumerable.Sum), e => e.Sum(i => i));
            yield return new Sink(nameof(Enumerable.ToArray), e => e.ToArray());
            yield return new Sink(nameof(Enumerable.ToDictionary), e => e.ToDictionary(i => i));
            yield return new Sink(nameof(Enumerable.ToDictionary), e => e.ToDictionary(i => i, i => i));
            yield return new Sink(nameof(Enumerable.ToHashSet), e => e.ToHashSet());
            yield return new Sink(nameof(Enumerable.ToList), e => e.ToList());
            yield return new Sink(nameof(Enumerable.ToLookup), e => e.ToLookup(i => i));
            yield return new Sink(nameof(Enumerable.ToLookup), e => e.ToLookup(i => i, i => i));
            yield return new Sink("foreach", e => { foreach (int item in e) { } });
            yield return new Sink("nop", e => { }, shortCircuits: true);
        }

        private sealed class LifecycleTrackingEnumerable<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> _source;
            private int _enumeratorCtorCalls;
            private int _enumeratorDisposeCalls;

            public LifecycleTrackingEnumerable(IEnumerable<T> source) => _source = source;

            public int EnumeratorCtorCalls => _enumeratorCtorCalls;
            public int EnumeratorDisposeCalls => _enumeratorDisposeCalls;

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly LifecycleTrackingEnumerable<T> _enumerable;
                private readonly IEnumerator<T> _enumerator;

                public Enumerator(LifecycleTrackingEnumerable<T> enumerable)
                {
                    _enumerable = enumerable;

                    Interlocked.Increment(ref _enumerable._enumeratorCtorCalls);
                    _enumerator = enumerable._source.GetEnumerator();
                }

                public void Dispose()
                {
                    Interlocked.Increment(ref _enumerable._enumeratorDisposeCalls);
                    _enumerator.Dispose();
                }

                public T Current => _enumerator.Current;
                
                object IEnumerator.Current => ((IEnumerator)_enumerator).Current;

                public bool MoveNext() => _enumerator.MoveNext();

                public void Reset() => throw new NotSupportedException($"LINQ operators should not invoke {nameof(Reset)}.");
            }
        }

        private abstract class Operation
        {
            public Operation(string name, bool shortCircuits)
            {
                Name = name;
                ShortCircuits = shortCircuits;
            }

            public string Name { get; }

            public bool ShortCircuits { get; }

            public override string ToString() => Name;
        }

        private abstract class Operation<TWork> : Operation
        {
            public Operation(string name, TWork operation, bool shortCircuits) : base(name, shortCircuits) => Work = operation;

            public TWork Work { get; }
        }

        private sealed class Source : Operation<IEnumerable<int>>
        {
            public Source(string name, IEnumerable<int> enumerable, bool shortCircuits = false) : base(name, enumerable, shortCircuits) { }
        }

        private sealed class Unary : Operation<Func<IEnumerable<int>, IEnumerable<int>>>
        {
            public Unary(string name, Func<IEnumerable<int>, IEnumerable<int>> unary, bool shortCircuits = false) : base(name, unary, shortCircuits) { }
        }

        private sealed class Binary : Operation<Func<IEnumerable<int>, IEnumerable<int>, IEnumerable<int>>>
        {
            public Binary(string name, Func<IEnumerable<int>, IEnumerable<int>, IEnumerable<int>> binary, bool shortCircuits = false) : base(name, binary, shortCircuits) { }
        }

        private sealed class Sink : Operation<Action<IEnumerable<int>>>
        {
            public Sink(string name, Action<IEnumerable<int>> sink, bool shortCircuits = false) : base(name, sink, shortCircuits) { }
        }
    }
}
