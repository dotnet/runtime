// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Linq
{
    public abstract partial class EnumerableExecutor
    {
        protected EnumerableExecutor() { }
    }
    public partial class EnumerableExecutor<T> : System.Linq.EnumerableExecutor
    {
        public EnumerableExecutor(System.Linq.Expressions.Expression expression) { }
    }
    public abstract partial class EnumerableQuery
    {
        protected EnumerableQuery() { }
    }
    public partial class EnumerableQuery<T> : System.Linq.EnumerableQuery, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Linq.IOrderedQueryable, System.Linq.IOrderedQueryable<T>, System.Linq.IQueryable, System.Linq.IQueryable<T>, System.Linq.IQueryProvider
    {
        public EnumerableQuery(System.Collections.Generic.IEnumerable<T> enumerable) { }
        public EnumerableQuery(System.Linq.Expressions.Expression expression) { }
        System.Type System.Linq.IQueryable.ElementType { get { return default(System.Type); } }
        System.Linq.Expressions.Expression System.Linq.IQueryable.Expression { get { return default(System.Linq.Expressions.Expression); } }
        System.Linq.IQueryProvider System.Linq.IQueryable.Provider { get { return default(System.Linq.IQueryProvider); } }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        System.Linq.IQueryable System.Linq.IQueryProvider.CreateQuery(System.Linq.Expressions.Expression expression) { return default(System.Linq.IQueryable); }
        System.Linq.IQueryable<S> System.Linq.IQueryProvider.CreateQuery<S>(System.Linq.Expressions.Expression expression) { return default(System.Linq.IQueryable<S>); }
        object System.Linq.IQueryProvider.Execute(System.Linq.Expressions.Expression expression) { return default(object); }
        S System.Linq.IQueryProvider.Execute<S>(System.Linq.Expressions.Expression expression) { return default(S); }
        public override string ToString() { return default(string); }
    }
    public static partial class Queryable
    {
        public static TSource Aggregate<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TSource, TSource>> func) { return default(TSource); }
        public static TAccumulate Aggregate<TSource, TAccumulate>(this System.Linq.IQueryable<TSource> source, TAccumulate seed, System.Linq.Expressions.Expression<System.Func<TAccumulate, TSource, TAccumulate>> func) { return default(TAccumulate); }
        public static TResult Aggregate<TSource, TAccumulate, TResult>(this System.Linq.IQueryable<TSource> source, TAccumulate seed, System.Linq.Expressions.Expression<System.Func<TAccumulate, TSource, TAccumulate>> func, System.Linq.Expressions.Expression<System.Func<TAccumulate, TResult>> selector) { return default(TResult); }
        public static bool All<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { return default(bool); }
        public static bool Any<TSource>(this System.Linq.IQueryable<TSource> source) { return default(bool); }
        public static bool Any<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { return default(bool); }
        public static System.Linq.IQueryable AsQueryable(this System.Collections.IEnumerable source) { return default(System.Linq.IQueryable); }
        public static System.Linq.IQueryable<TElement> AsQueryable<TElement>(this System.Collections.Generic.IEnumerable<TElement> source) { return default(System.Linq.IQueryable<TElement>); }
        public static decimal Average(this System.Linq.IQueryable<decimal> source) { return default(decimal); }
        public static double Average(this System.Linq.IQueryable<double> source) { return default(double); }
        public static double Average(this System.Linq.IQueryable<int> source) { return default(double); }
        public static double Average(this System.Linq.IQueryable<long> source) { return default(double); }
        public static System.Nullable<decimal> Average(this System.Linq.IQueryable<System.Nullable<decimal>> source) { return default(System.Nullable<decimal>); }
        public static System.Nullable<double> Average(this System.Linq.IQueryable<System.Nullable<double>> source) { return default(System.Nullable<double>); }
        public static System.Nullable<double> Average(this System.Linq.IQueryable<System.Nullable<int>> source) { return default(System.Nullable<double>); }
        public static System.Nullable<double> Average(this System.Linq.IQueryable<System.Nullable<long>> source) { return default(System.Nullable<double>); }
        public static System.Nullable<float> Average(this System.Linq.IQueryable<System.Nullable<float>> source) { return default(System.Nullable<float>); }
        public static float Average(this System.Linq.IQueryable<float> source) { return default(float); }
        public static decimal Average<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, decimal>> selector) { return default(decimal); }
        public static double Average<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, double>> selector) { return default(double); }
        public static double Average<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, int>> selector) { return default(double); }
        public static double Average<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, long>> selector) { return default(double); }
        public static System.Nullable<decimal> Average<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Nullable<decimal>>> selector) { return default(System.Nullable<decimal>); }
        public static System.Nullable<double> Average<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Nullable<double>>> selector) { return default(System.Nullable<double>); }
        public static System.Nullable<double> Average<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Nullable<int>>> selector) { return default(System.Nullable<double>); }
        public static System.Nullable<double> Average<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Nullable<long>>> selector) { return default(System.Nullable<double>); }
        public static System.Nullable<float> Average<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Nullable<float>>> selector) { return default(System.Nullable<float>); }
        public static float Average<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, float>> selector) { return default(float); }
        public static System.Linq.IQueryable<TResult> Cast<TResult>(this System.Linq.IQueryable source) { return default(System.Linq.IQueryable<TResult>); }
        public static System.Linq.IQueryable<TSource> Concat<TSource>(this System.Linq.IQueryable<TSource> source1, System.Collections.Generic.IEnumerable<TSource> source2) { return default(System.Linq.IQueryable<TSource>); }
        public static bool Contains<TSource>(this System.Linq.IQueryable<TSource> source, TSource item) { return default(bool); }
        public static bool Contains<TSource>(this System.Linq.IQueryable<TSource> source, TSource item, System.Collections.Generic.IEqualityComparer<TSource> comparer) { return default(bool); }
        public static int Count<TSource>(this System.Linq.IQueryable<TSource> source) { return default(int); }
        public static int Count<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { return default(int); }
        public static System.Linq.IQueryable<TSource> DefaultIfEmpty<TSource>(this System.Linq.IQueryable<TSource> source) { return default(System.Linq.IQueryable<TSource>); }
        public static System.Linq.IQueryable<TSource> DefaultIfEmpty<TSource>(this System.Linq.IQueryable<TSource> source, TSource defaultValue) { return default(System.Linq.IQueryable<TSource>); }
        public static System.Linq.IQueryable<TSource> Distinct<TSource>(this System.Linq.IQueryable<TSource> source) { return default(System.Linq.IQueryable<TSource>); }
        public static System.Linq.IQueryable<TSource> Distinct<TSource>(this System.Linq.IQueryable<TSource> source, System.Collections.Generic.IEqualityComparer<TSource> comparer) { return default(System.Linq.IQueryable<TSource>); }
        public static TSource ElementAt<TSource>(this System.Linq.IQueryable<TSource> source, int index) { return default(TSource); }
        public static TSource ElementAtOrDefault<TSource>(this System.Linq.IQueryable<TSource> source, int index) { return default(TSource); }
        public static System.Linq.IQueryable<TSource> Except<TSource>(this System.Linq.IQueryable<TSource> source1, System.Collections.Generic.IEnumerable<TSource> source2) { return default(System.Linq.IQueryable<TSource>); }
        public static System.Linq.IQueryable<TSource> Except<TSource>(this System.Linq.IQueryable<TSource> source1, System.Collections.Generic.IEnumerable<TSource> source2, System.Collections.Generic.IEqualityComparer<TSource> comparer) { return default(System.Linq.IQueryable<TSource>); }
        public static TSource First<TSource>(this System.Linq.IQueryable<TSource> source) { return default(TSource); }
        public static TSource First<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { return default(TSource); }
        public static TSource FirstOrDefault<TSource>(this System.Linq.IQueryable<TSource> source) { return default(TSource); }
        public static TSource FirstOrDefault<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { return default(TSource); }
        public static System.Linq.IQueryable<System.Linq.IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector) { return default(System.Linq.IQueryable<System.Linq.IGrouping<TKey, TSource>>); }
        public static System.Linq.IQueryable<System.Linq.IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { return default(System.Linq.IQueryable<System.Linq.IGrouping<TKey, TSource>>); }
        public static System.Linq.IQueryable<System.Linq.IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Linq.Expressions.Expression<System.Func<TSource, TElement>> elementSelector) { return default(System.Linq.IQueryable<System.Linq.IGrouping<TKey, TElement>>); }
        public static System.Linq.IQueryable<System.Linq.IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Linq.Expressions.Expression<System.Func<TSource, TElement>> elementSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { return default(System.Linq.IQueryable<System.Linq.IGrouping<TKey, TElement>>); }
        public static System.Linq.IQueryable<TResult> GroupBy<TSource, TKey, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Linq.Expressions.Expression<System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, TResult>> resultSelector) { return default(System.Linq.IQueryable<TResult>); }
        public static System.Linq.IQueryable<TResult> GroupBy<TSource, TKey, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Linq.Expressions.Expression<System.Func<TKey, System.Collections.Generic.IEnumerable<TSource>, TResult>> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { return default(System.Linq.IQueryable<TResult>); }
        public static System.Linq.IQueryable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Linq.Expressions.Expression<System.Func<TSource, TElement>> elementSelector, System.Linq.Expressions.Expression<System.Func<TKey, System.Collections.Generic.IEnumerable<TElement>, TResult>> resultSelector) { return default(System.Linq.IQueryable<TResult>); }
        public static System.Linq.IQueryable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Linq.Expressions.Expression<System.Func<TSource, TElement>> elementSelector, System.Linq.Expressions.Expression<System.Func<TKey, System.Collections.Generic.IEnumerable<TElement>, TResult>> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { return default(System.Linq.IQueryable<TResult>); }
        public static System.Linq.IQueryable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this System.Linq.IQueryable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Linq.Expressions.Expression<System.Func<TOuter, TKey>> outerKeySelector, System.Linq.Expressions.Expression<System.Func<TInner, TKey>> innerKeySelector, System.Linq.Expressions.Expression<System.Func<TOuter, System.Collections.Generic.IEnumerable<TInner>, TResult>> resultSelector) { return default(System.Linq.IQueryable<TResult>); }
        public static System.Linq.IQueryable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this System.Linq.IQueryable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Linq.Expressions.Expression<System.Func<TOuter, TKey>> outerKeySelector, System.Linq.Expressions.Expression<System.Func<TInner, TKey>> innerKeySelector, System.Linq.Expressions.Expression<System.Func<TOuter, System.Collections.Generic.IEnumerable<TInner>, TResult>> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { return default(System.Linq.IQueryable<TResult>); }
        public static System.Linq.IQueryable<TSource> Intersect<TSource>(this System.Linq.IQueryable<TSource> source1, System.Collections.Generic.IEnumerable<TSource> source2) { return default(System.Linq.IQueryable<TSource>); }
        public static System.Linq.IQueryable<TSource> Intersect<TSource>(this System.Linq.IQueryable<TSource> source1, System.Collections.Generic.IEnumerable<TSource> source2, System.Collections.Generic.IEqualityComparer<TSource> comparer) { return default(System.Linq.IQueryable<TSource>); }
        public static System.Linq.IQueryable<TResult> Join<TOuter, TInner, TKey, TResult>(this System.Linq.IQueryable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Linq.Expressions.Expression<System.Func<TOuter, TKey>> outerKeySelector, System.Linq.Expressions.Expression<System.Func<TInner, TKey>> innerKeySelector, System.Linq.Expressions.Expression<System.Func<TOuter, TInner, TResult>> resultSelector) { return default(System.Linq.IQueryable<TResult>); }
        public static System.Linq.IQueryable<TResult> Join<TOuter, TInner, TKey, TResult>(this System.Linq.IQueryable<TOuter> outer, System.Collections.Generic.IEnumerable<TInner> inner, System.Linq.Expressions.Expression<System.Func<TOuter, TKey>> outerKeySelector, System.Linq.Expressions.Expression<System.Func<TInner, TKey>> innerKeySelector, System.Linq.Expressions.Expression<System.Func<TOuter, TInner, TResult>> resultSelector, System.Collections.Generic.IEqualityComparer<TKey> comparer) { return default(System.Linq.IQueryable<TResult>); }
        public static TSource Last<TSource>(this System.Linq.IQueryable<TSource> source) { return default(TSource); }
        public static TSource Last<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { return default(TSource); }
        public static TSource LastOrDefault<TSource>(this System.Linq.IQueryable<TSource> source) { return default(TSource); }
        public static TSource LastOrDefault<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { return default(TSource); }
        public static long LongCount<TSource>(this System.Linq.IQueryable<TSource> source) { return default(long); }
        public static long LongCount<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { return default(long); }
        public static TSource Max<TSource>(this System.Linq.IQueryable<TSource> source) { return default(TSource); }
        public static TResult Max<TSource, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TResult>> selector) { return default(TResult); }
        public static TSource Min<TSource>(this System.Linq.IQueryable<TSource> source) { return default(TSource); }
        public static TResult Min<TSource, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TResult>> selector) { return default(TResult); }
        public static System.Linq.IQueryable<TResult> OfType<TResult>(this System.Linq.IQueryable source) { return default(System.Linq.IQueryable<TResult>); }
        public static System.Linq.IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector) { return default(System.Linq.IOrderedQueryable<TSource>); }
        public static System.Linq.IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { return default(System.Linq.IOrderedQueryable<TSource>); }
        public static System.Linq.IOrderedQueryable<TSource> OrderByDescending<TSource, TKey>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector) { return default(System.Linq.IOrderedQueryable<TSource>); }
        public static System.Linq.IOrderedQueryable<TSource> OrderByDescending<TSource, TKey>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { return default(System.Linq.IOrderedQueryable<TSource>); }
        public static System.Linq.IQueryable<TSource> Reverse<TSource>(this System.Linq.IQueryable<TSource> source) { return default(System.Linq.IQueryable<TSource>); }
        public static System.Linq.IQueryable<TResult> Select<TSource, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TResult>> selector) { return default(System.Linq.IQueryable<TResult>); }
        public static System.Linq.IQueryable<TResult> Select<TSource, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, int, TResult>> selector) { return default(System.Linq.IQueryable<TResult>); }
        public static System.Linq.IQueryable<TResult> SelectMany<TSource, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Collections.Generic.IEnumerable<TResult>>> selector) { return default(System.Linq.IQueryable<TResult>); }
        public static System.Linq.IQueryable<TResult> SelectMany<TSource, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, int, System.Collections.Generic.IEnumerable<TResult>>> selector) { return default(System.Linq.IQueryable<TResult>); }
        public static System.Linq.IQueryable<TResult> SelectMany<TSource, TCollection, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Collections.Generic.IEnumerable<TCollection>>> collectionSelector, System.Linq.Expressions.Expression<System.Func<TSource, TCollection, TResult>> resultSelector) { return default(System.Linq.IQueryable<TResult>); }
        public static System.Linq.IQueryable<TResult> SelectMany<TSource, TCollection, TResult>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, int, System.Collections.Generic.IEnumerable<TCollection>>> collectionSelector, System.Linq.Expressions.Expression<System.Func<TSource, TCollection, TResult>> resultSelector) { return default(System.Linq.IQueryable<TResult>); }
        public static bool SequenceEqual<TSource>(this System.Linq.IQueryable<TSource> source1, System.Collections.Generic.IEnumerable<TSource> source2) { return default(bool); }
        public static bool SequenceEqual<TSource>(this System.Linq.IQueryable<TSource> source1, System.Collections.Generic.IEnumerable<TSource> source2, System.Collections.Generic.IEqualityComparer<TSource> comparer) { return default(bool); }
        public static TSource Single<TSource>(this System.Linq.IQueryable<TSource> source) { return default(TSource); }
        public static TSource Single<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { return default(TSource); }
        public static TSource SingleOrDefault<TSource>(this System.Linq.IQueryable<TSource> source) { return default(TSource); }
        public static TSource SingleOrDefault<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { return default(TSource); }
        public static System.Linq.IQueryable<TSource> Skip<TSource>(this System.Linq.IQueryable<TSource> source, int count) { return default(System.Linq.IQueryable<TSource>); }
        public static System.Linq.IQueryable<TSource> SkipWhile<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { return default(System.Linq.IQueryable<TSource>); }
        public static System.Linq.IQueryable<TSource> SkipWhile<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, int, bool>> predicate) { return default(System.Linq.IQueryable<TSource>); }
        public static decimal Sum(this System.Linq.IQueryable<decimal> source) { return default(decimal); }
        public static double Sum(this System.Linq.IQueryable<double> source) { return default(double); }
        public static int Sum(this System.Linq.IQueryable<int> source) { return default(int); }
        public static long Sum(this System.Linq.IQueryable<long> source) { return default(long); }
        public static System.Nullable<decimal> Sum(this System.Linq.IQueryable<System.Nullable<decimal>> source) { return default(System.Nullable<decimal>); }
        public static System.Nullable<double> Sum(this System.Linq.IQueryable<System.Nullable<double>> source) { return default(System.Nullable<double>); }
        public static System.Nullable<int> Sum(this System.Linq.IQueryable<System.Nullable<int>> source) { return default(System.Nullable<int>); }
        public static System.Nullable<long> Sum(this System.Linq.IQueryable<System.Nullable<long>> source) { return default(System.Nullable<long>); }
        public static System.Nullable<float> Sum(this System.Linq.IQueryable<System.Nullable<float>> source) { return default(System.Nullable<float>); }
        public static float Sum(this System.Linq.IQueryable<float> source) { return default(float); }
        public static decimal Sum<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, decimal>> selector) { return default(decimal); }
        public static double Sum<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, double>> selector) { return default(double); }
        public static int Sum<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, int>> selector) { return default(int); }
        public static long Sum<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, long>> selector) { return default(long); }
        public static System.Nullable<decimal> Sum<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Nullable<decimal>>> selector) { return default(System.Nullable<decimal>); }
        public static System.Nullable<double> Sum<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Nullable<double>>> selector) { return default(System.Nullable<double>); }
        public static System.Nullable<int> Sum<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Nullable<int>>> selector) { return default(System.Nullable<int>); }
        public static System.Nullable<long> Sum<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Nullable<long>>> selector) { return default(System.Nullable<long>); }
        public static System.Nullable<float> Sum<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, System.Nullable<float>>> selector) { return default(System.Nullable<float>); }
        public static float Sum<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, float>> selector) { return default(float); }
        public static System.Linq.IQueryable<TSource> Take<TSource>(this System.Linq.IQueryable<TSource> source, int count) { return default(System.Linq.IQueryable<TSource>); }
        public static System.Linq.IQueryable<TSource> TakeWhile<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { return default(System.Linq.IQueryable<TSource>); }
        public static System.Linq.IQueryable<TSource> TakeWhile<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, int, bool>> predicate) { return default(System.Linq.IQueryable<TSource>); }
        public static System.Linq.IOrderedQueryable<TSource> ThenBy<TSource, TKey>(this System.Linq.IOrderedQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector) { return default(System.Linq.IOrderedQueryable<TSource>); }
        public static System.Linq.IOrderedQueryable<TSource> ThenBy<TSource, TKey>(this System.Linq.IOrderedQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { return default(System.Linq.IOrderedQueryable<TSource>); }
        public static System.Linq.IOrderedQueryable<TSource> ThenByDescending<TSource, TKey>(this System.Linq.IOrderedQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector) { return default(System.Linq.IOrderedQueryable<TSource>); }
        public static System.Linq.IOrderedQueryable<TSource> ThenByDescending<TSource, TKey>(this System.Linq.IOrderedQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, TKey>> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { return default(System.Linq.IOrderedQueryable<TSource>); }
        public static System.Linq.IQueryable<TSource> Union<TSource>(this System.Linq.IQueryable<TSource> source1, System.Collections.Generic.IEnumerable<TSource> source2) { return default(System.Linq.IQueryable<TSource>); }
        public static System.Linq.IQueryable<TSource> Union<TSource>(this System.Linq.IQueryable<TSource> source1, System.Collections.Generic.IEnumerable<TSource> source2, System.Collections.Generic.IEqualityComparer<TSource> comparer) { return default(System.Linq.IQueryable<TSource>); }
        public static System.Linq.IQueryable<TSource> Where<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, bool>> predicate) { return default(System.Linq.IQueryable<TSource>); }
        public static System.Linq.IQueryable<TSource> Where<TSource>(this System.Linq.IQueryable<TSource> source, System.Linq.Expressions.Expression<System.Func<TSource, int, bool>> predicate) { return default(System.Linq.IQueryable<TSource>); }
        public static System.Linq.IQueryable<TResult> Zip<TFirst, TSecond, TResult>(this System.Linq.IQueryable<TFirst> source1, System.Collections.Generic.IEnumerable<TSecond> source2, System.Linq.Expressions.Expression<System.Func<TFirst, TSecond, TResult>> resultSelector) { return default(System.Linq.IQueryable<TResult>); }
    }
}
